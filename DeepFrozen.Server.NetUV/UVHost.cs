using DeepCore;
using DeepCore.Log;
using NetUV.Core.Buffers;
using NetUV.Core.Channels;
using NetUV.Core.Handles;
using NetUV.Core.Native;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DeepFrozen.Server.NetUV
{
    public abstract class UVHost : UVBase
    {
        private IPEndPoint endPoint;
        private Tcp server;
        private int stopped = 0;
        private int started = 0;
        public int ListenPort { get; protected set; } = 19000;
        public IPEndPoint HostEndPoint { get => endPoint; }
        public string HostAddress { get => $"127.0.0.1:{ListenPort}"; }

        public UVHost(IDictionary<string, string> cfg, EventLoop eventLoop = null) : base(cfg, eventLoop)
        {
        }
        public void SetListenPort(int value)
        {
            if (value > 0)
            {
                ListenPort = value;
                config["Port"] = value.ToString();
                config["Listen"] = value.ToString();
                config["ListenPort"] = value.ToString();
            }
        }
        protected override void Disposing()
        {
            if (server != null)
            {
                this.OnDisposing();
                Task.Run(async () =>
                {
                    await EventLoop.ExecuteAsync(() => { this.server.Dispose(); });
                    this.OnDisposed();
                }).Wait();
            }
            base.Disposing();
        }
        public virtual Task<bool> StartAsync()
        {
            if (Interlocked.CompareExchange(ref started, 1, 0) != 0)
            {
                return Task.FromResult(false);
            }
            if (config.TryGetAsInt("Port", out var intValue))
            {
                ListenPort = intValue;
            }
            if (config.TryGetAsInt("Listen", out intValue))
            {
                ListenPort = intValue;
            }
            if (config.TryGetAsInt("ListenPort", out intValue))
            {
                ListenPort = intValue;
            }
            if (config.TryGetAsInt(nameof(MaxConnections), out intValue))
            {
                this.MaxConnections = intValue;
            }
            if (config.TryGetAsInt(nameof(MaxRequestLength), out intValue))
            {
                this.MaxRequestLength = intValue;
            }
            this.endPoint = new IPEndPoint(IPAddress.Any, ListenPort);
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.RunTaskInUV(EventLoop.Loop , (state) =>
            {
                var ret = uv_InternalStart((Loop)state);
                //Task.Run(() => tcs.TrySetResult(ret));
                tcs.TrySetResult(ret);
            });
            return tcs.Task;
        }
        public virtual Task<bool> StopAsync(string reason)
        {
            if (Interlocked.CompareExchange(ref stopped, 1, 0) != 0)
            {
                return Task.FromResult(false);
            }
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            RunTaskInUV(this.server, (st) =>
            {
                var ret = uv_InternalStop((Tcp)st, reason);
                //Task.Run(() => tcs.TrySetResult(ret));
                tcs.TrySetResult(ret);
            });
            return tcs.Task;
        }
        internal bool uv_InternalStart(Loop state)
        {
            uv_OnStarting();
            try
            {
                var loop = (Loop)state;
                this.server = loop.CreateTcp();
                if (config.TryGetAsBool(nameof(SimultaneousAccepts), out var boolValue))
                {
                    this.SimultaneousAccepts = boolValue;
                    this.server.SimultaneousAccepts(this.SimultaneousAccepts);
                }
                if (config.TryGetAsBool(nameof(NoDelay), out boolValue))
                {
                    this.NoDelay = boolValue;
                    this.server.NoDelay(this.NoDelay);
                }
                if (config.TryGetAsBool(nameof(KeepAlive), out boolValue) && config.TryGetAsInt(nameof(KeepAliveInterval), out var intValue))
                {
                    this.KeepAlive = boolValue;
                    this.KeepAliveInterval = intValue;
                    this.server.KeepAlive(this.KeepAlive, this.KeepAliveInterval);
                }
                if (config.TryGetAsInt(nameof(BackLog), out intValue) && config.TryGetAsBool(nameof(DualStack), out boolValue))
                {
                    this.BackLog = intValue;
                    this.DualStack = boolValue;
                    this.server.Listen(this.endPoint, this.uv_InternalSessionConnection, this.BackLog, this.DualStack);
                }
                else
                {
                    this.server.Listen(this.endPoint, this.uv_InternalSessionConnection);
                }
                if (config.TryGetAsInt(nameof(RecvBufferSize), out var recvBufferSize))
                {
                    this.RecvBufferSize = recvBufferSize;
                    this.server.SetReceiveBufferSize(recvBufferSize);
                }
                if (config.TryGetAsInt(nameof(SendBufferSize), out var sendBufferSize))
                {
                    this.SendBufferSize = sendBufferSize;
                    this.server.SetSendBufferSize(sendBufferSize);
                }
                this.log.Info($"{Name} started on {this.endPoint}");
                return true;
            }
            catch (Exception err)
            {
                uv_OnError(err);
                return false;
            }
            finally
            {
                uv_OnStarted();
                log.Warn($"{Name} started!");
            }
        }
        internal virtual bool uv_InternalStop(Tcp tcp, string reason)
        {
            uv_OnClosing(reason);
            if (tcp != null)
            {
                uv_InternalClearSession(reason);
                try
                {
                    log.Warn($"{Name} shutting down...");
                    tcp.Shutdown((c, err) =>
                    {
                        if (err != null)
                        {
                            uv_OnError(err);
                        }
                        log.Warn($"{Name} shutdown");
                    });
                }
                catch (Exception err)
                {
                    uv_OnError(err);
                }
                try
                {
                    log.Warn($"{Name} closing...");
                    tcp.CloseHandle((uv_tcp) =>
                    {
                        log.Warn($"{Name} closed");
                    });
                    tcp.Dispose();
                }
                catch (Exception err)
                {
                    uv_OnError(err);
                }
            }
            uv_OnClosed(reason);
            return true;
        }
        internal virtual void uv_InternalClearSession(string reason) { }
        internal virtual void uv_InternalSessionConnection(Tcp client, Exception error)
        {
            if (error != null)
            {
                uv_OnError(error);
                client.CloseHandle((handle) => handle.Dispose());
            }
            else
            {
                uv_onConnection(client);
            }
        }

        //---------------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------------
        protected virtual void uv_OnError(Exception err) { log.Error(err); }
        protected abstract void uv_OnStarting();
        protected abstract void uv_OnStarted();
        protected abstract void uv_OnClosing(string reason);
        protected abstract void uv_OnClosed(string reason);
        protected abstract void uv_onConnection(Tcp client);
        protected abstract void OnDisposing();
        protected abstract void OnDisposed();
    }



    //---------------------------------------------------------------------------------------------------------------
}
