using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.NetClient;
using DeepCrystal.Grid;
using DeepCrystal.NetServer;
using DeepCrystal.Threading.Dataflow;
//using DeepFrozen.RPC.Remote.GridImpl;
using NetUV.Core.Buffers;
using NetUV.Core.Channels;
using NetUV.Core.Handles;
using NetUV.Core.Native;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DeepFrozen.Server.NetUV.Grid
{
//     public class UVRpcAppFactory : GridRpcAppFactory
//     {
//         public UVRpcAppFactory()
//         {
//             new UVGridAdapterFactory();
//         }
//     }
    public class UVGridAdapterFactory : GridFactory
    {
        public UVGridAdapterFactory()
        {
        }
        public override IGridAdapter CreateAdapter(IExternalizableFactory codec, Properties cfg)
        {
            return new UVGridAdapter(codec, cfg);
        }
    }

    public class UVGridAdapter : UVBase, IGridAdapter
    {
        private GridHost listen;
        private string localAddress;
        private ConcurrentDictionary<string, GridProxy> tcp_pool = new ConcurrentDictionary<string, GridProxy>();
        private ServerProtocolPool messagePool;
        //public ActionBlockExecutor Executor => this.executor;
        public UVGridAdapter(IExternalizableFactory codec, Properties cfg, EventLoop eventLoop = null) : base(cfg, eventLoop)
        {
            this.messagePool = new ServerProtocolPool(codec, false);
            this.listen = new GridHost(this);
            //this.log.SetLevelFlag(LoggerLevel.RELEASE);
        }
        public Task<bool> StartAsync(string localAddress)
        {
            this.localAddress = localAddress;
            IPUtil.TryParseHostPort(localAddress, out var host, out var port);
            this.listen.SetListenPort(port);
            return this.listen.StartAsync();
        }
        protected override void Disposing()
        {
            foreach (var s in tcp_pool.Values.ToArray())
            {
                s.Dispose();
            }
            this.tcp_pool.Clear();
            this.listen.Dispose();
            base.Disposing();
        }
        public IGridProxy GetProxy(string remoteAddress)
        {
            var ret = tcp_pool.GetOrAdd(remoteAddress, r => new GridProxy(this, remoteAddress));
            return ret;
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        GridProxy uv_onOutputHandshake(GridProxyOutput proxy, string remoteAddress)
        {
            log.Debug("uv_onOutputHandshake : " + remoteAddress);
            var p = tcp_pool.GetOrAdd(remoteAddress, r => new GridProxy(this, remoteAddress));
            p.uv_bindOutput(proxy);
            return p;
        }
        GridProxy uv_onInputHandshake(GridProxyInput proxy, string remoteAddress)
        {
            log.Debug("uv_onInputHandshake : " + remoteAddress);
            var p = tcp_pool.GetOrAdd(remoteAddress, r => new GridProxy(this, remoteAddress));
            p.uv_bindInput(proxy);
            return p;
        }
        public SendMessage AllocSend()
        {
            return messagePool.AllocSend();
        }
        public RecvMessage AllocRecv()
        {
            return messagePool.AllocRecv();
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        #region Events
        private HandleBinary event_OnHandleBinary;
        private HandleMessage event_OnHandleMessage;
        private HandleBinaryAsync event_OnHandleBinaryAsync;
        private HandleMessageAsync event_OnHandleMessageAsync;
        event HandleBinary IGridAdapter.OnHandleBinary
        {
            add { event_OnHandleBinary += value; }
            remove { event_OnHandleBinary -= value; }
        }
        event HandleMessage IGridAdapter.OnHandleMessage
        {
            add { event_OnHandleMessage += value; }
            remove { event_OnHandleMessage -= value; }
        }
        event HandleBinaryAsync IGridAdapter.OnHandleBinaryAsync
        {
            add { event_OnHandleBinaryAsync += value; }
            remove { event_OnHandleBinaryAsync -= value; }
        }
        event HandleMessageAsync IGridAdapter.OnHandleMessageAsync
        {
            add { event_OnHandleMessageAsync += value; }
            remove { event_OnHandleMessageAsync -= value; }
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------
        class GridHost : UVHost
        {
            internal readonly UVGridAdapter adapter;
            public GridHost(UVGridAdapter adapter) : base(adapter.config, adapter.EventLoop)
            {
                this.adapter = adapter;
            }
            protected override void OnDisposing() { }
            protected override void OnDisposed() { }
            protected override void uv_OnStarting() { }
            protected override void uv_OnStarted() { }
            protected override void uv_OnClosing(string reason) { }
            protected override void uv_OnClosed(string reason) { }
            protected override void uv_onConnection(Tcp client)
            {
                new GridProxyInput(adapter, client);
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        internal class GridProxy : Disposable, IGridProxy
        {
            internal readonly UVGridAdapter adapter;
            internal readonly string remoteAddress;
            internal readonly ActionBlockExecutor executor;
            internal readonly Logger log;
            private GridProxyInput input;
            private GridProxyOutput output;
            private bool uv_connecting = false;
            internal GridProxy(UVGridAdapter adapter, string remoteAddress)
            {
                this.AsSynchronizedDisposing();
                this.adapter = adapter;
                this.remoteAddress = remoteAddress;
                this.executor = new ActionBlockExecutor();
                this.log = LoggerFactory.GetLogger("GridProxy:" + remoteAddress);
                this.connectAsync();
            }
            protected override void Disposing()
            {
                this.onDisposeListening();
                adapter.RunTaskInUV(() =>
                {
                    this.input?.uv_Dispose();
                    this.output?.uv_Dispose();
                });
                Task.Run(async () =>
                {
                    this.executor.Complete();
                    await this.executor.Completion;
                }).Wait();
            }
            public override string ToString()
            {
                return log.Name;
            }
            //-----------------------------------------------------------------------------------------------------------------------------
            IGridAdapter IGridProxy.Adapter => this.adapter;
            string IGridProxy.RemoteAddress => this.remoteAddress;
            object IGridProxy.UserTag { get; set; }
            void IGridProxy.Send(ISerializable msg)
            {
                var send = adapter.AllocSend();
                send.InitWithMessage(MessageType.MSG_NOTIFY, 0, msg);
                Send(send);
            }
            void IGridProxy.Send(BinaryMessage msg)
            {
                var send = adapter.AllocSend();
                send.InitWithMessage(MessageType.MSG_NOTIFY, 0, msg);
                Send(send);
            }
            Task<bool> IGridProxy.SendAsync(ISerializable msg)
            {
                var send = adapter.AllocSend();
                send.InitWithMessage(MessageType.MSG_NOTIFY, 0, msg);
                var tcs = new TaskCompletionSource<bool>();
                Send(send, (rst, err) =>
                {
                    this.executor.Post(() =>
                    {
                        if (err != null) tcs.TrySetException(err);
                        else tcs.TrySetResult(rst);
                    });
                });
                return tcs.Task;
            }
            Task<bool> IGridProxy.SendAsync(BinaryMessage msg)
            {
                var send = adapter.AllocSend();
                send.InitWithMessage(MessageType.MSG_NOTIFY, 0, msg);
                var tcs = new TaskCompletionSource<bool>();
                Send(send, (rst, err) =>
                {
                    this.executor.Post(() =>
                    {
                        if (err != null) tcs.TrySetException(err);
                        else tcs.TrySetResult(rst);
                    });
                });
                return tcs.Task;
            }
            Task<ISerializable> IGridProxy.SendRequestAsync(ISerializable msg)
            {
                var sendID = (uint)Interlocked.Increment(ref request_indexer);
                var send = adapter.AllocSend();
                send.InitWithMessage(MessageType.MSG_REQUEST_C2S, sendID, msg);
                var tcs = new TaskCompletionSource<ISerializable>();
                if (request_msg.TryAdd(sendID, tcs))
                {
                    Send(send, (rst, err) =>
                    {
                        if (err != null)
                        {
                            this.executor.Post(() =>
                            {
                                request_msg.TryRemove(sendID, out var tts);
                                tcs.TrySetException(err);
                            });
                        }
                        else if (rst == false)
                        {
                            this.executor.Post(() =>
                            {
                                request_msg.TryRemove(sendID, out var tts);
                                tcs.TrySetCanceled();
                            });
                        }
                    });
                }
                else
                {
                    tcs.TrySetCanceled();
                }
                return tcs.Task;
            }
            Task<BinaryMessage> IGridProxy.SendRequestAsync(BinaryMessage msg)
            {
                var sendID = (uint)Interlocked.Increment(ref request_indexer);
                var send = adapter.AllocSend();
                send.InitWithMessage(MessageType.MSG_REQUEST_C2S, sendID, msg);
                var tcs = new TaskCompletionSource<BinaryMessage>();
                if (request_bin.TryAdd(sendID, tcs))
                {
                    Send(send, (rst, err) =>
                    {
                        if (err != null)
                        {
                            this.executor.Post(() =>
                            {
                                request_bin.TryRemove(sendID, out var tts);
                                tts.TrySetException(err);
                            });
                        }
                        else if (rst == false)
                        {
                            this.executor.Post(() =>
                            {
                                request_bin.TryRemove(sendID, out var tts);
                                tts.TrySetCanceled();
                            });
                        }
                    });
                }
                else
                {
                    tcs.TrySetCanceled();
                }
                return tcs.Task;
            }
            //-----------------------------------------------------------------------------------------------------------------------------
            internal Task<bool> connectAsync()
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                this.adapter.RunTaskInUV(() =>
                {
                    uv_Reconnect(r =>
                    {
                        tcs.TrySetResult(r);
                    });
                });
                return tcs.Task;
            }
            internal void uv_bindInput(GridProxyInput input)
            {
                if (this.input != input)
                {
                    this.input?.uv_Dispose();
                }
                this.input = input;
                if (this.input != null)
                {
                    this.uv_Reconnect((r) => { });
                    this.input.uv_Listen(uv_Recv);
                }
            }
            internal void uv_bindOutput(GridProxyOutput output)
            {
                if (this.output != output)
                {
                    this.output?.uv_Dispose();
                }
                if (output == null)
                {
                    executor.Post(() =>
                    {
                        this.onDisposeListening();
                    });
                }
                this.output = output;
            }
            internal void uv_Reconnect(Action<bool> done)
            {
                if (output != null && output.uv_IsActive)
                {
                    done(true);
                }
                else
                {
                    if (this.uv_connecting == false)
                    {
                        this.uv_connecting = true;
                        log.Warn("uv_Reconnect : " + remoteAddress);
                        new GridProxyOutput(adapter, remoteAddress, r =>
                        {
                            this.uv_connecting = false;
                            done(r);
                        });
                    }
                    else
                    {
                        done(true);
                    }
                }
            }
            private void Send(SendMessage send, Action<bool, Exception> done = null)
            {
                //只允许主线程调用，否则造成死锁
                if (output == null)
                {
                    Task.Run(async () =>
                    {
                        while (output == null)
                        {
                            await connectAsync();
                            Thread.Yield();
                        }
                    }).Wait();
                }
                adapter.RunTaskInUV(() =>
                {
                    if (output == null)
                    {
                        done?.Invoke(false, null);
                    }
                    else
                    {
                        output.uv_output_Send(send, done);
                    }
                });
            }
            private void uv_Recv(GridProxyInput input, RecvMessage recv, Exception err)
            {
                this.executor.Post(() =>
                {
                    if (recv.MsgType == MessageType.MSG_RESPONSE_S2C)
                    {
                        if (request_msg.TryRemove(recv.MsgSendID, out var tcs_msg))
                        {
                            try
                            {
                                var rsp_msg = recv.ReadBody();
                                tcs_msg.TrySetResult(rsp_msg);
                            }
                            catch (Exception err2)
                            {
                                tcs_msg.TrySetException(err2);
                            }
                        }
                        else if (request_bin.TryRemove(recv.MsgSendID, out var tcs_bin))
                        {
                            try
                            {
                                var rsp_bin = recv.ReadBodyBinary();
                                tcs_bin.TrySetResult(rsp_bin);
                            }
                            catch (Exception err2)
                            {
                                tcs_bin.TrySetException(err2);
                            }
                        }
                        else
                        {
                            log.Error("Drop response : " + recv.MsgSendID);
                        }
                    }
                    else if (recv.MsgType == MessageType.MSG_REQUEST_C2S)
                    {
                        var sendID = recv.MsgSendID;
                        if (adapter.event_OnHandleBinaryAsync != null)
                        {
                            var bin = recv.ReadBodyBinary();
                            adapter.event_OnHandleBinaryAsync.Invoke(this, bin).ContinueWith(t =>
                            {
                                if (t.Exception != null)
                                {
                                    log.Error("event_OnHandleBinaryAsync : " + t.Exception.Message, t.Exception);
                                }
                                var rst = t.GetResultAs();
                                var send = adapter.AllocSend();
                                send.InitWithMessage(MessageType.MSG_RESPONSE_S2C, sendID, rst);
                                Send(send);
                            });
                        }
                        else if (adapter.event_OnHandleMessageAsync != null)
                        {
                            var msg = recv.ReadBody();
                            adapter.event_OnHandleMessageAsync.Invoke(this, msg).ContinueWith(t =>
                            {
                                if (t.Exception != null)
                                {
                                    log.Error("event_OnHandleBinaryAsync : " + t.Exception.Message, t.Exception);
                                }
                                var rst = t.GetResultAs();
                                var send = adapter.AllocSend();
                                send.InitWithMessage(MessageType.MSG_RESPONSE_S2C, sendID, rst);
                                Send(send);
                            });
                        }
                        else
                        {
                            log.Error("Drop request : " + sendID);
                        }
                    }
                    else if (recv.MsgType == MessageType.MSG_NOTIFY)
                    {
                        if (adapter.event_OnHandleBinary != null)
                        {
                            var bin = recv.ReadBodyBinary();
                            adapter.event_OnHandleBinary.Invoke(this, bin);
                        }
                        if (adapter.event_OnHandleMessage != null)
                        {
                            var msg = recv.ReadBody();
                            adapter.event_OnHandleMessage.Invoke(this, msg);
                        }
                    }
                    else
                    {
                        log.Error("Unsupport : " + recv.MsgType);
                    }
                });
            }
            //-----------------------------------------------------------------------------------------------------------------------------
            private int request_indexer = 0;
            private ConcurrentDictionary<uint, TaskCompletionSource<ISerializable>> request_msg = new ConcurrentDictionary<uint, TaskCompletionSource<ISerializable>>();
            private ConcurrentDictionary<uint, TaskCompletionSource<BinaryMessage>> request_bin = new ConcurrentDictionary<uint, TaskCompletionSource<BinaryMessage>>();
            private void onDisposeListening()
            {
                log.Warn("onDisposeListening");
                foreach (var tcs in request_msg.Values.ToImmutableArray())
                {
                    tcs.TrySetCanceled();
                }
                request_msg.Clear();
                foreach (var tcs in request_bin.Values.ToImmutableArray())
                {
                    tcs.TrySetCanceled();
                }
                request_bin.Clear();
            }
            //-----------------------------------------------------------------------------------------------------------------------------
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        internal class GridProxyOutput
        {
            internal readonly UVGridAdapter adapter;
            internal readonly string remoteAddress;
            private Tcp tcp;
            private GridProxy prx;
            internal GridProxyOutput(UVGridAdapter adapter, string remoteAddress, Action<bool> done)
            {
                this.adapter = adapter;
                this.remoteAddress = remoteAddress;
                this.uv_output_Start(done);
            }
            internal bool uv_IsActive { get => tcp != null && tcp.IsActive && !tcp.IsClosing; }
            void uv_onError(Tcp tcp, Exception err)
            {
                if (err is OperationException oe && oe.ErrorCode == ErrorCode.ECONNRESET)
                {
                    adapter.log.Debug($"GridProxyOutput : {remoteAddress} : {err.Message}", err);
                }
                else
                {
                    adapter.log.Error($"GridProxyOutput : {remoteAddress} : {err.Message}", err);
                }
                adapter.log.Trace(new StackTrace().ToString());
                prx?.uv_bindOutput(null);
                prx = null;
            }
            internal void uv_Dispose()
            {
                adapter.log.Trace($"GridProxyOutput : {remoteAddress} : uv_Dispose");
                this.tcp?.Dispose();
            }
            private void uv_output_Start(Action<bool> done)
            {
                var loop = adapter.EventLoop.Loop;
                var config = adapter.config;
                try
                {
                    var localEndPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                    var remoteEndPoint = IPUtil.ToIPEndPoint(remoteAddress);
                    var tcp = loop.CreateTcp();
                    if (config.TryGetAsBool(nameof(NoDelay), out var boolValue))
                    {
                        tcp = tcp.NoDelay(boolValue);
                    }
                    if (config.TryGetAsBool(nameof(KeepAlive), out boolValue) && config.TryGetAsInt(nameof(KeepAliveInterval), out var intValue))
                    {
                        tcp = tcp.KeepAlive(boolValue, intValue);
                    }
                    config.TryGetAsBool(nameof(DualStack), out boolValue);
                    tcp = tcp.ConnectTo(localEndPoint, remoteEndPoint, (c, e) =>
                    {
                        uv_output_Connected(c, e, done);
                    }, boolValue);
                    if (config.TryGetAsInt(nameof(RecvBufferSize), out var recvBufferSize))
                    {
                        tcp.SetReceiveBufferSize(recvBufferSize);
                    }
                    if (config.TryGetAsInt(nameof(SendBufferSize), out var sendBufferSize))
                    {
                        tcp.SetSendBufferSize(sendBufferSize);
                    }
                    this.tcp = tcp;
                }
                catch (Exception err)
                {
                    done(false);
                    uv_onError(null, err);
                }
            }
            void uv_output_Connected(Tcp output, Exception err, Action<bool> done)
            {
                if (err != null)
                {
                    done(false);
                    uv_onError(output, err);
                    return;
                }
                else
                {
                    try
                    {
                        var send = adapter.AllocSend();
                        send.InitWithSystemMessage(new SystemHandshake() { local_info = adapter.localAddress });
                        this.uv_output_Send(send, (r, e) =>
                        {
                            done(r);
                            if (r) { prx = adapter.uv_onOutputHandshake(this, this.remoteAddress); }
                        });
                    }
                    catch (Exception err2)
                    {
                        done(false);
                        uv_onError(output, err2);
                    }
                    try
                    {
                        output.OnRead(uv_output_onDataReceived, uv_onError, uv_output_OnComplete);
                    }
                    catch (Exception err3)
                    {
                        uv_onError(output, err3);
                    }
                }
            }
            void uv_output_OnComplete(Tcp input)
            {
                adapter.log.Debug($"GridProxyOutput : {remoteAddress} : uv_output_OnComplete");
                prx?.uv_bindOutput(null);
                prx = null;
            }
            void uv_output_onDataReceived(Tcp input, ReadableBuffer data)
            {
                //adapter.log.Debug($"GridProxyOutput : {remoteAddress} : uv_output_onDataReceived");
            }
            internal void uv_output_Send(SendMessage send, Action<bool, Exception> done = null)
            {
                if (!uv_IsActive)
                {
                    //adapter.log.Debug(new StackTrace().ToString());
                    done?.Invoke(false, new Exception("Tcp Disposed"));
                    return;
                }
                var output = this.tcp;
                try
                {
                    output.QueueWriteStream(send.Buffer, 0, send.BufferLength, uv_onWriteComplete);
                }
                catch (Exception err)
                {
                    done?.Invoke(false, err);
                    uv_onError(output, err);
                    send.Dispose();
                }
                void uv_onWriteComplete(StreamHandle handle, Exception err)
                {
                    //adapter.log.Debug($"GridProxyOutput : {remoteAddress} : uv_onWriteComplete");
                    try
                    {
                        if (err != null)
                        {
                            done?.Invoke(false, err);
                            uv_onError(output, err);
                        }
                        else
                        {
                            done?.Invoke(true, err);
                            this.uv_output_onSentComplete(output, send);
                        }
                    }
                    finally
                    {
                        send.Dispose();
                    }
                }
            }
            void uv_output_onSentComplete(Tcp output, SendMessage send)
            {

            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        internal class GridProxyInput
        {
            internal readonly UVGridAdapter adapter;
            internal readonly ProtocolDecodeingSlim decoding;
            private Tcp tcp;
            private GridProxy prx;
            private string remoteAddress;
            private Action<GridProxyInput, RecvMessage, Exception> uv_onReceived;
            public GridProxyInput(UVGridAdapter adapter, Tcp tcp)
            {
                this.adapter = adapter;
                this.decoding = new ProtocolDecodeingSlim(adapter.AllocRecv, uv_input_onProtocolReceived);
                this.tcp = tcp;
                this.uv_input_StartRecv(tcp);
            }
            internal bool uv_IsActive { get => tcp != null && tcp.IsActive && !tcp.IsClosing; }
            internal void uv_Listen(Action<GridProxyInput, RecvMessage, Exception> act)
            {
                uv_onReceived = act;
            }
            internal void uv_Dispose()
            {
                adapter.log.Trace($"GridProxyInput : {remoteAddress} : uv_Dispose");
                decoding.Dispose();
                this.uv_onReceived = null;
                this.tcp?.Dispose();
            }
            void uv_onError(Tcp tcp, Exception err)
            {
                if (err is OperationException oe && oe.ErrorCode == ErrorCode.ECONNRESET)
                {
                    adapter.log.Debug($"GridProxyInput : {remoteAddress} : {err.Message}", err);
                }
                else
                {
                    adapter.log.Error($"GridProxyInput : {remoteAddress} : {err.Message}", err);
                }
                adapter.log.Trace(new StackTrace().ToString());
                prx?.uv_bindInput(null);
            }
            void uv_input_StartRecv(Tcp input)
            {
                try
                {
                    input.OnRead(uv_input_onDataReceived, uv_onError, uv_input_OnComplete);
                }
                catch (Exception err)
                {
                    uv_onError(input, err);
                }
            }
            void uv_input_OnComplete(Tcp input)
            {
                if (input.IsActive == false)
                {
                    //uv_onError(input, new Exception("uv_input_OnComplete"));
                }
            }
            void uv_input_onDataReceived(Tcp input, ReadableBuffer data)
            {
                try
                {
                    int count = data.Count;
                    if (count > 0)
                    {
                        decoding.OnReceived(input, data);
                    }
                }
                catch (Exception err)
                {
                    uv_onError(input, err);
                }
            }
            void uv_input_onProtocolReceived(Tcp input, RecvMessage recv, Exception err)
            {
                try
                {
                    if (input != null)
                    {
                        if (err != null)
                        {
                            recv.Dispose();
                            this.uv_onError(input, err);
                        }
                        else if (recv.PkgType == PackageType.PKG_HANDSHAKE)
                        {
                            var handshake = recv.ReadBodySystemMessage() as SystemHandshake;
                            this.remoteAddress = handshake.local_info;
                            this.prx = adapter.uv_onInputHandshake(this, remoteAddress);
                            recv.Dispose();
                        }
                        else
                        {
                            uv_onReceived?.Invoke(this, recv, err);
                        }
                    }
                }
                catch (Exception err2)
                {
                    uv_onError(input, err2);
                }
            }
        }
    }
    //-----------------------------------------------------------------------------------------------------------------------------
}
