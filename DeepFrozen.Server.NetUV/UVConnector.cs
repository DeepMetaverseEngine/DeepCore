using DeepCore;
using DeepCore.IO;
using DeepCore.NetClient;
using DeepCrystal.NetServer;
using DeepCrystal.Threading.Dataflow;
using NetUV.Core.Buffers;
using NetUV.Core.Channels;
using NetUV.Core.Handles;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DeepFrozen.Server.NetUV
{
    public abstract class UVConnector : UVBase
    {
        public enum CloseReason
        {
            Unknow = -1,
            CloseByUser = 0,
            CloseByException = 1,
            CloseByComplete = 2,
            CloseByKickByServer = 3,
            CloseByTimeout = 4,
        }

        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(UVConnector));
        protected IPEndPoint localEndPoint;
        protected IPEndPoint remoteEndPoint;
        private long total_recv_bytes;
        private long total_sent_bytes;
        protected Tcp tcp;
        private int connected = 0;
        private int closing = 0;
        private readonly ProtocolDecoding<RecvMessage> decoding;
        private readonly ActionBlockExecutor executor;
        public ActionBlockExecutor MainExecutor { get => executor; }
        public ServerProtocolPool MessagePool { get; }
        public bool IsConnected { get => connected == 1 && closing == 0; }
        public bool IsClosing { get => closing == 1; }
        public long TotalRecvBytes { get { return total_recv_bytes; } }
        public long TotalSentBytes { get { return total_sent_bytes; } }
        public UVConnector(Properties cfg, ServerProtocolPool messagePool, EventLoop eventLoop = null) : base(cfg, eventLoop)
        {
            Alloc.RecordConstructor(this.GetType());
            this.MessagePool = messagePool;
            if (config.TryGetAsInt(nameof(MaxRequestLength), out var intValue))
            {
                this.MaxRequestLength = intValue;
            }
            this.decoding = new ProtocolDecoding<RecvMessage>(MaxRequestLength, messagePool.AllocRecv, main_onProtocolReceived);
            this.executor = new ActionBlockExecutor();
        }
        ~UVConnector()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(this.GetType());
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        protected override void Disposing()
        {
            Task.Run(async () =>
            {
                try
                {
                    await CloseAsync();
                    this.executor.Complete();
                    await this.executor.Completion;
                    this.decoding.Dispose();
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
            }).Wait();
            base.Disposing();
        }
        public Task<bool> ConnectAsync(IPEndPoint endpoint)
        {
            var tcs = new TaskCompletionSource<bool>();
            this.localEndPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            this.remoteEndPoint = endpoint;
            RunTaskInUV(() =>
            {
                uv_InternalConnect((c, e) =>
                {
                    executor.Post(()=>
                    {
                        if (c) { tcs.TrySetResult(c); }
                        else if (e != null) { tcs.TrySetException(e); }
                        else { tcs.TrySetResult(c); }
                    });
                });
            });
            return tcs.Task;
        }
        public Task<bool> CloseAsync(CloseReason reason = CloseReason.CloseByUser, string msg = null)
        {
            if (!IsClosing)
            {
                var tcs = new TaskCompletionSource<bool>();
                RunTaskInUV(() =>
                {
                    uv_InternalClose(reason, msg, (result, error) =>
                    {
                        executor.Post(() =>
                        {
                            if (result) { tcs.TrySetResult(result); }
                            else if (error != null) { tcs.TrySetException(error); }
                            else { tcs.TrySetResult(result); }
                        });
                    });
                });
                return tcs.Task;
            }
            else
            {
                return Task.FromResult(false);
            }
        }
        protected bool Send(ISerializable msg, MessageType msgType, uint sendID, Action<bool, Exception> done = null)
        {
            if (!IsConnected)
            {
                done?.Invoke(false, null);
                return false;
            }
            var send = MessagePool.AllocSend();
            try
            {
                send.InitWithMessage(msgType, sendID, msg);
                RunTaskInUV(() => { this.uv_InternalSend(send, done); });
                return true;
            }
            catch (Exception err)
            {
                done?.Invoke(false, err);
                send.Dispose();
            }
            return false;
        }
        protected bool Send(BinaryMessage msg, MessageType msgType, uint sendID, Action<bool, Exception> done = null)
        {
            if (!IsConnected)
            {
                done?.Invoke(false, null);
                return false;
            }
            var send = MessagePool.AllocSend();
            try
            {
                send.InitWithMessage(msgType, sendID, msg);
                RunTaskInUV(() => { this.uv_InternalSend(send, done); });
                return true;
            }
            catch (Exception err)
            {
                done?.Invoke(false, err);
                send.Dispose();
            }
            return false;
        }
        protected void SendSystemMessage(SystemMessage sys, Action<bool, Exception> done = null)
        {
            if (!IsConnected)
            {
                done?.Invoke(false, null);
                return;
            }
            var send = MessagePool.AllocSend();
            try
            {
                send.InitWithSystemMessage(sys);
                RunTaskInUV(() => { this.uv_InternalSend(send, done); });
            }
            catch (Exception err)
            {
                done?.Invoke(false, err);
                send.Dispose();
            }
        }
        //--------------------------------------------------------------------------------------------------------------------
        // uv thread

        protected virtual bool uv_InternalConnect(Action<bool, Exception> cb)
        {
            if (this.tcp != null)
            {
                cb.Invoke(false, null);
                return false;
            }
            Interlocked.Exchange(ref closing, 0);
            var loop = EventLoop.Loop;
            uv_onStarting();
            try
            {
                this.tcp = loop.CreateTcp();
                if (config.TryGetAsBool(nameof(NoDelay), out var boolValue))
                {
                    this.NoDelay = boolValue;
                    this.tcp = tcp.NoDelay(this.NoDelay);
                }
                if (config.TryGetAsBool(nameof(KeepAlive), out boolValue) && config.TryGetAsInt(nameof(KeepAliveInterval), out var intValue))
                {
                    this.KeepAlive = boolValue;
                    this.KeepAliveInterval = intValue;
                    this.tcp = tcp.KeepAlive(this.KeepAlive, this.KeepAliveInterval);
                }
                if (config.TryGetAsBool(nameof(DualStack), out boolValue))
                {
                    this.DualStack = boolValue;
                }
                this.tcp = tcp.ConnectTo(localEndPoint, remoteEndPoint, (c, e) =>
                {
                    cb.Invoke(e == null, e);
                    uv_OnConnected(c, e);
                }, DualStack);
                if (config.TryGetAsInt(nameof(RecvBufferSize), out var recvBufferSize))
                {
                    this.RecvBufferSize = recvBufferSize;
                    this.tcp.SetReceiveBufferSize(recvBufferSize);
                }
                if (config.TryGetAsInt(nameof(SendBufferSize), out var sendBufferSize))
                {
                    this.SendBufferSize = sendBufferSize;
                    this.tcp.SetSendBufferSize(sendBufferSize);
                }
                return true;
            }
            catch (Exception err)
            {
                cb.Invoke(false, err);
                uv_onError(null, err);
                return false;
            }
            finally
            {
                this.log.Info($"started on {this.remoteEndPoint}");
                uv_onStarted();
            }
        }
        protected virtual void uv_InternalClose(CloseReason reason, string msg, Action<bool, Exception> done = null)
        {
            if (this.tcp == null)
            {
                done?.Invoke(false, null);
                return;
            }
            try
            {
                Interlocked.Exchange(ref connected, 0);
                if (Interlocked.CompareExchange(ref closing, 1, 0) == 0)
                {
                    tcp.CloseHandle(tcp =>
                    {
                        uv_onClosed(reason, msg);
                        done?.Invoke(true, null);
                    });
                }
                else
                {
                    done?.Invoke(false, null);
                }
            }
            catch (Exception err)
            {
                done?.Invoke(false, err);
            }

        }
        protected virtual void uv_onStarting() { }
        protected virtual void uv_onStarted() { }
        protected virtual void uv_onError(Tcp tcp, Exception err)
        {
            log.Error(err.Message, err);
            this.uv_InternalClose(CloseReason.CloseByException, err.Message);
        }
        protected virtual void uv_onClosed(CloseReason reason, string msg)
        {
            Interlocked.Exchange(ref connected, 0);
            Interlocked.Exchange(ref closing, 1);
            tcp?.Dispose();
            tcp = null;
        }
        protected virtual void uv_OnConnected(Tcp client, Exception exception)
        {
            if (exception != null)
            {
                uv_onError(tcp, exception);
                return;
            }
            Interlocked.Exchange(ref connected, 1);
            try
            {
                client.OnRead(uv_onDataReceived, uv_onError, uv_onComplete);
            }
            catch (Exception err)
            {
                uv_onError(tcp, err);
            }
        }
        protected virtual void uv_onDataReceived(Tcp stream, ReadableBuffer data)
        {
            try
            {
                int count = data.Count;
                if (count > 0)
                {
                    this.total_recv_bytes += count;
                    var buffer = new ArraySegment<byte>(new byte[count]);
                    data.ReadBytes(buffer.Array, count);
                    uv_onDataReceived(stream, buffer);
                }
            }
            catch (Exception err)
            {
                uv_onError(tcp, err);
            }
        }
        protected virtual void uv_onDataReceived(Tcp stream, ArraySegment<byte> buffer)
        {
            if (executor.Post(main_doDecode, buffer) == false)
            {
                this.uv_InternalClose(CloseReason.CloseByComplete, null);
            }
        }
        protected virtual void uv_onComplete(Tcp handle)
        {
            if (!handle.IsActive)
            {
                uv_InternalClose(CloseReason.CloseByComplete, null);
            }
        }
        protected virtual void uv_InternalSend(SendMessage send, Action<bool, Exception> done)
        {
            if (IsConnected)
            {
                try
                {
                    tcp.QueueWriteStream(send.Buffer, 0, send.BufferLength, uv_onWriteComplete);
                }
                catch (Exception err)
                {
                    done?.Invoke(false, err);
                    send.Dispose();
                    this.uv_onError(tcp, err);
                }
            }
            else
            {
                done?.Invoke(false, null);
                send.Dispose();
            }
            void uv_onWriteComplete(StreamHandle handle, Exception err)
            {
                try
                {
                    if (err != null)
                    {
                        done?.Invoke(false, err);
                        this.uv_onError(tcp, err);
                    }
                    else
                    {
                        done?.Invoke(true, null);
                        this.total_sent_bytes += send.BufferLength;
                        this.uv_onSentComplete(send);
                    }
                }
                finally
                {
                    send.Dispose();
                }
            }
        }
        protected abstract void uv_onSentComplete(SendMessage send);
        //--------------------------------------------------------------------------------------------------------------------
        // action block main thread
        protected virtual void main_onError(Exception err)
        {
            log.Error(err.Message, err);
        }
        protected virtual void main_doDecode(ArraySegment<byte> mem)
        {
            if (IsClosing) return;
            try
            {
                decoding.OnReceived(mem);
            }
            catch (Exception err)
            {
                main_onError(err);
            }
        }
        protected abstract void main_onProtocolReceived(RecvMessage recv, Exception error);
        //--------------------------------------------------------------------------------------------------------------------
    }




}
