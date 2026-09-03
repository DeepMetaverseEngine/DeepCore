using DeepCore;
using DeepCore.Log;
using DeepCrystal.SharpMinaServer;
using NetUV.Core.Buffers;
using NetUV.Core.Channels;
using NetUV.Core.Handles;
using NetUV.Core.Native;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace DeepFrozen.Server.NetUV
{
    public abstract class UVAcceptor : UVHost
    {
        protected readonly ConcurrentDictionary<string, UVAbstractSession> sessions;
        public UVAcceptor(IDictionary<string, string> cfg, EventLoop eventLoop = null) : base(cfg, eventLoop)
        {
            this.sessions = new ConcurrentDictionary<string, UVAbstractSession>();
        }
        protected override void Disposing()
        {
            this.sessions.Clear();
            base.Disposing();
        }
        sealed protected override void uv_onConnection(Tcp client)
        {
            try
            {
                if (MaxConnections > 0 && sessions.Count >= MaxConnections)
                {
                    client.CloseHandle((handle) => handle.Dispose());
                }
                else
                {
                    var session = CreateSession(client);
                    if (sessions.TryAdd(session.ID, session))
                    {
                        var validate = uv_OnConnection(session);
                        if (validate)
                        {
                            session.uv_InternalStart();
                        }
                        else
                        {
                            session.Dispose();
                        }
                    }
                    else
                    {
                        session.uv_InternalStop($"Session Key Already Exist : {session.ID}");
                    }
                }
            }
            catch (Exception err)
            {
                uv_OnError(err);
            }
        }
        protected abstract bool uv_OnConnection(UVAbstractSession client);
        //---------------------------------------------------------------------------------------------------------------
        sealed internal override void uv_InternalSessionConnection(Tcp client, Exception error)
        {
            base.uv_InternalSessionConnection(client, error);
        }
        sealed internal override bool uv_InternalStop(Tcp tcp, string reason)
        {
            return base.uv_InternalStop(tcp, reason);
        }
        sealed internal override void uv_InternalClearSession(string reason)
        {
            foreach (var s in new List<UVAbstractSession>(sessions.Values))
            {
                s.uv_Disconnect(reason);
            }
            this.sessions.Clear();
        }
        //---------------------------------------------------------------------------------------------------------------
        internal void RemoveSession(UVAbstractSession session)
        {
            sessions.TryRemove(session.ID, out var ss);
        }
        public S GetSessionAs<S>(string sessionID) where S : UVAbstractSession
        {
            return sessions[sessionID] as S;
        }
        public bool TryGetSessionAs<S>(string sessionID, out S ss) where S : UVAbstractSession
        {
            if (sessions.TryGetValue(sessionID, out var _ss))
            {
                ss = _ss as S;
                return true;
            }
            ss = null;
            return false;
        }
        public int GetSessionsAs<S>(IList<S> ret) where S : UVAbstractSession
        {
            int count = 0;
            var list = new List<UVAbstractSession>(sessions.Values);
            foreach (S s in list)
            {
                count++;
                ret.Add(s);
            }
            return count;
        }
        public int ForEachSessionAs<ST, S>(ST st, Action<ST, S> action) where S : UVAbstractSession
        {
            var list = new List<UVAbstractSession>(sessions.Values);
            foreach (S s in list)
            {
                action(st, s);
            }
            return list.Count;
        }
        public bool HasSession(UVAbstractSession session)
        {
            if (session == null) { return false; }
            return sessions.ContainsKey(session.ID);
        }
        protected abstract UVAbstractSession CreateSession(Tcp client);
        public int SessionCount => this.sessions.Count;
    }

    //---------------------------------------------------------------------------------------------------------------

    public abstract class UVAcceptor<SS> : UVAcceptor where SS : UVAbstractSession
    {
        public UVAcceptor(IDictionary<string, string> cfg, EventLoop eventLoop = null) : base(cfg, eventLoop)
        {
        }
        public SS GetSession(string sessionID)
        {
            return base.GetSessionAs<SS>(sessionID);
        }
        public bool TryGetSession(string sessionID, out SS ss)
        {
            return base.TryGetSessionAs<SS>(sessionID, out ss);
        }
        public int GetSessions(IList<SS> ret)
        {
            return base.GetSessionsAs<SS>(ret);
        }
        public int ForEachSession<ST>(ST st, Action<ST, SS> action)
        {
            var list = new List<UVAbstractSession>(sessions.Values);
            foreach (SS s in list)
            {
                action(st, s);
            }
            return list.Count;
        }
    }

    //---------------------------------------------------------------------------------------------------------------

    public abstract class UVAbstractSession : Disposable
    {
        protected readonly UVAcceptor server;
        protected readonly Tcp client;
        protected readonly string uuid;
        protected readonly DeepCore.HashMap<string, object> attributes;
        protected EndPoint endpoint;
        private string closeReason;
        private long total_sent_bytes;
        private long total_recv_bytes;
        private bool closing = false;
        protected Logger log { get => server.log; }
        //---------------------------------------------------------------------------------
        public string ID { get { return uuid; } }
        public bool IsConnected { get { return !closing && client.IsActive && !client.IsClosing && client.IsValid; } }
        public long TotalRecvBytes { get { return total_recv_bytes; } }
        public long TotalSentBytes { get { return total_sent_bytes; } }
        public EndPoint RemoteAddress { get { return endpoint; } }
        public IDictionary<string, object> Attributes { get { return attributes; } }
        public object UserTag { get; set; }
        public UVAcceptor Host => server;
        public Tcp Client => client;
        public bool Closing => closing;
        public string CloseReason => closeReason;
        //---------------------------------------------------------------------------------
        public UVAbstractSession(UVAcceptor server, Tcp tcp)
        {
            this.endpoint = tcp.GetPeerEndPoint();
            this.server = server;
            this.client = tcp;
            this.uuid = Guid.NewGuid().ToString();
            this.attributes = new DeepCore.HashMap<string, object>();
        }
        protected override void Disposing()
        {
            try
            {
                this.OnDisposeListening();
                this.OnDisposeEvents();
                this.server.RemoveSession(this);
                Task.Run(async () =>
                {
                    try
                    {
                        await this.server.EventLoop.ExecuteAsync(() => this.client.Dispose());
                        await this.OnDisposingAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                });
            }
            catch (Exception err)
            {
                log.Error(err);
            }
        }
        protected virtual void uv_OnStart() { }
        internal void uv_Disconnect(string reason, Action<bool> cb = null)
        {
            if (closing)
            {
                cb?.Invoke(false);
                return;
            }
            this.closeReason = reason;
            this.closing = true;
            this.uv_OnDisconnecting(reason, (StreamHandle handle, Exception handle_err) =>
            {
                if (handle_err != null)
                {
                    cb?.Invoke(false);
                    log.Error(handle_err.Message, handle_err);
                }
                else
                {
                    try
                    {
                        client.CloseHandle((Tcp handle) =>
                        {
                            cb?.Invoke(true);
                            this.uv_onClientClosed(handle);
                        });
                    }
                    catch (Exception err)
                    {
                        cb?.Invoke(false);
                        log.Error(err.Message, err);
                    }
                }
            });
        }
        internal Task<bool> uv_StopAsync(string reason)
        {
            var tcs = new TaskCompletionSource<bool>();
            uv_Disconnect(reason, (r) => tcs.TrySetResult(r));
            return tcs.Task;
        }
        internal void uv_InternalStart()
        {
            uv_OnStart();
            client.OnRead(this.uv_onDataReceived, this.uv_onClientError, handle =>
            {
                if (!handle.IsActive)
                {
                    uv_InternalStop("read end");
                }
            });
        }
        internal void uv_InternalStop(string reason)
        {
            if (!closing)
            {
                this.closeReason = reason;
                this.closing = true;
                client.CloseHandle(uv_onClientClosed);
            }
        }
        private void uv_onClientClosed(Tcp handle)
        {
            this.closing = true;
            this.uv_OnDisconnected(this.closeReason, () => this.Dispose());
        }
        private void uv_onClientError(Tcp handle, Exception error)
        {
            this.uv_InternalStop(error.Message);
            if (error is OperationException oe)
            {
                if (oe.ErrorCode == ErrorCode.ECANCELED)
                {
                    return;
                }
                if (oe.ErrorCode == ErrorCode.ECONNRESET)
                {
                    return;
                }
            }
            OnError(error);
        }
        private void uv_onDataReceived(Tcp client, ReadableBuffer data)
        {
            try
            {
                int count = data.Count;
                if (count > 0)
                {
                    this.total_recv_bytes += count;
                    uv_OnDataReceived(data);
                }
            }
            catch (Exception err)
            {
                this.uv_onClientError(client, err);
            }
        }

        protected void InternalSend(ArraySegment<byte> send, Action<bool> done = null)
        {
            server.RunTaskInUV(() =>
            {
                if (!closing)
                {
                    try
                    {
                        client.QueueWriteStream(send.Array, send.Offset, send.Count, (handle, err) =>
                        {
                            this.total_sent_bytes += send.Count;
                            if (err != null)
                            {
                                done?.Invoke(false);
                                uv_onClientError(client, err);
                            }
                            else
                            {
                                done?.Invoke(true);
                            }
                        });
                    }
                    catch (Exception err)
                    {
                        done?.Invoke(false);
                        this.uv_onClientError(client, err);
                    }
                }
                else
                {
                    done?.Invoke(false);
                }
            });
        }
        protected void InternalSend<ST>(ST send, Func<ST, ArraySegment<byte>> uvWriteAction, Action<ST, bool> done = null)
        {
            server.RunTaskInUV(() =>
            {
                if (!closing)
                {
                    try
                    {
                        var buffer = uvWriteAction(send);
                        if (buffer.Count > 0)
                        {
                            client.QueueWriteStream(buffer.Array, buffer.Offset, buffer.Count, (handle, err) =>
                            {
                                this.total_sent_bytes += buffer.Count;
                                if (err != null)
                                {
                                    done?.Invoke(send, false);
                                    uv_onClientError(client, err);
                                }
                                else
                                {
                                    done?.Invoke(send, true);
                                }
                            });
                        }
                        else
                        {
                            done?.Invoke(send, false);
                        }
                    }
                    catch (Exception err)
                    {
                        done?.Invoke(send, false);
                        this.uv_onClientError(client, err);
                    }
                }
                else
                {
                    done?.Invoke(send, false);
                }
            });
        }


        protected abstract void uv_OnDisconnecting(string reason, Action<StreamHandle, Exception> complete);
        protected abstract void uv_OnDisconnected(string reason, Action complete);
        protected abstract void uv_OnDataReceived(ReadableBuffer data);

        protected abstract Task OnDisposingAsync();
        protected abstract void OnDisposeListening();
        protected abstract void OnDisposeEvents();
        protected abstract void OnError(Exception err);

        public void Disconnect(string reason)
        {
            server.RunTaskInUV(() =>
            {
                uv_Disconnect(reason, (r) => { });
            });
        }
        public Task<bool> DisconnectAsync(string reason)
        {
            var tcs = new TaskCompletionSource<bool>();
            server.RunTaskInUV(() =>
            {
                uv_Disconnect(reason, (r) => tcs.TrySetResult(r));
            });
            return tcs.Task;
        }

        public object GetAttribute(string key)
        {
            return attributes[key];
        }
        public void SetAttribute(string key, object value)
        {
            attributes[key] = value;
        }
        public void RemoveAttribute(string key)
        {
            attributes.Remove(key);
        }
        public bool ContainsAttribute(string key)
        {
            return attributes.ContainsKey(key);
        }
        public IEnumerable<string> GetAttributeKeys()
        {
            return attributes.Keys;
        }

    }

    //---------------------------------------------------------------------------------------------------------------
}
