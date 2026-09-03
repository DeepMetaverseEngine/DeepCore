using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using NetUV.Core.Channels;
using NetUV.Core.Handles;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepFrozen.Server.NetUV
{
    public class UVBase : Disposable
    {
        protected internal readonly Properties config;
        protected internal readonly Logger log;
        private readonly EventLoop eventLoop;
        private readonly EventLoop ownerLoop;
        public EventLoop EventLoop { get => eventLoop; }

        public UVBase(IDictionary<string, string> cfg, EventLoop eventLoop = null)
        {
            if (eventLoop == null)
            {
                ownerLoop = eventLoop = new EventLoop();
            }
            this.eventLoop = eventLoop;
            this.config = new Properties(cfg);
            if (config.TryGetValue(nameof(Name), out var stringValue))
            {
                this.Name = stringValue;
            }
            else
            {
                this.Name = GetType().Name;
            }
            this.log = LoggerFactory.GetLogger(Name);
            this.AsSynchronizedDisposing();
        }
        //------------------------------------------------------------------------------------------
        public string Name { get; }
        public bool NoDelay { get; protected set; } = false;
        public bool KeepAlive { get; protected set; } = false;
        /// <summary>
        /// Gets the keep alive interval, in seconds.
        /// </summary>
        public int KeepAliveInterval { get; protected set; } = 30;
        public int BackLog { get; protected set; } = 128;
        public bool DualStack { get; protected set; } = false;
        public bool SimultaneousAccepts { get; protected set; } = true;
        public int MaxConnections { get; protected set; } = 0;
        public int MaxRequestLength { get; protected set; } = 4 * 1024 * 1024;
        public int RecvBufferSize { get; protected set; } = 16384;
        public int SendBufferSize { get; protected set; } = 16384;
        //------------------------------------------------------------------------------------------
        public void SetNoDelay(bool value)
        {
            config[nameof(NoDelay)] = value.ToString();
            NoDelay = value;
        }
        public void SetKeepAlive(bool value)
        {
            config[nameof(KeepAlive)] = value.ToString();
            KeepAlive = value;
        }
        /// <summary>
        /// Set the keep alive interval, in seconds.
        /// </summary>
        public void SetKeepAliveInterval(int value)
        {
            config[nameof(KeepAliveInterval)] = value.ToString();
            KeepAliveInterval = value;
        }
        public void SetBackLog(int value)
        {
            config[nameof(BackLog)] = value.ToString();
            BackLog = value;
        }
        public void SetDualStack(bool value)
        {
            config[nameof(DualStack)] = value.ToString();
            DualStack = value;
        }
        public void SetSimultaneousAccepts(bool value)
        {
            config[nameof(SimultaneousAccepts)] = value.ToString();
            SimultaneousAccepts = value;
        }
        public void SetMaxConnections(int value)
        {
            config[nameof(MaxConnections)] = value.ToString();
            MaxConnections = value;
        }
        public void SetMaxRequestLength(int value)
        {
            config[nameof(MaxRequestLength)] = value.ToString();
            MaxRequestLength = value;
        }
        public void SetRecvBufferSize(int value)
        {
            config[nameof(RecvBufferSize)] = value.ToString();
            RecvBufferSize = value;
        }
        public void SetSendBufferSize(int value)
        {
            config[nameof(SendBufferSize)] = value.ToString();
            SendBufferSize = value;
        }
        //------------------------------------------------------------------------------------------
        protected override void Disposing()
        {
            if (ownerLoop != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await eventLoop.ShutdownGracefullyAsync();
                        await eventLoop.TerminationCompletion;
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    finally
                    {
                        log.Warn($"eventloop closed");
                    }
                }).Wait();
            }
        }
        public void RunTaskInUV(Action action)
        {
            eventLoop.ExecuteAsync(action);
        }
        public void RunTaskInUV<R>(R state, Action<R> action)
        {
            eventLoop.ExecuteAsync(st => action((R)st), state);
        }
        public Task RunTaskInUVAsync(Action action)
        {
            return eventLoop.ExecuteAsync(action);
        }
        public Task RunTaskInUVAsync<R>(R state, Action<R> action)
        {
            return eventLoop.ExecuteAsync(static st =>
            {
                var tuple = (ValueTuple<R, Action<R>>)(st);
                var state = tuple.Item1;
                var action = tuple.Item2;
                action((R)st);
            }, new ValueTuple<R, Action<R>>(state, action));
        }

        public async Task<T> RunTaskInUVAsync<T>(Func<T> action)
        {
            var ret = default(T);
            await eventLoop.ExecuteAsync(() => { ret = action(); });
            return ret;
        }
        public async Task<T> RunTaskInUVAsync<R, T>(R state, Func<R, T> action)
        {
            var ret = default(T);
            await eventLoop.ExecuteAsync(st => { ret = action((R)st); }, state);
            return ret;
        }
    }
}
