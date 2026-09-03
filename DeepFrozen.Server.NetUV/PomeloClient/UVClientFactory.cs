using DeepCore;
using DeepCore.IO;
using DeepCore.NetClient;
using DeepCore.PomeloClient;
using DeepCore.Threading;
using DeepCrystal.NetServer;
using DeepFrozen.Server.NetUV;
using NetUV.Core.Channels;
using System;
using System.Collections.Concurrent;

namespace PomeloClient.NetUV
{
    public class UVClientFactory : PomeloClientFactory, IDisposable
    {
        private System.Threading.Timer updateTimer;
        private TaskCompletionSourcePool tcsPool;
        private UVBase eventLoop;
        private ConcurrentDictionary<IExternalizableFactory, ServerProtocolPool> pools;

        new public static UVClientFactory Instance
        {
            get; private set;
        }
        public UVClientFactory()
        {
            Instance = this;
            pools = new ConcurrentDictionary<IExternalizableFactory, ServerProtocolPool>();
            eventLoop = new UVBase(new Properties(), new EventLoop());
            updateTimer = new System.Threading.Timer(TimerUpdate, this, 1000, 1000);
            tcsPool = new TaskCompletionSourcePool("UVClientFactory:", CollectionPool.Shared);
        }
        public void Dispose()
        {
            updateTimer.Dispose();
            tcsPool.Dispose();
            eventLoop.Dispose();
            foreach (var p in pools.Values) { p.Dispose(); }
            pools.Clear();
        }
        private void TimerUpdate(object st)
        {
            tcsPool.Update();
        }
        public override IClientAdapter CreateAdapter(INetClient client)
        {
            var cfg = new Properties();
            var pool = pools.GetOrAdd(client.Codec, c => new ServerProtocolPool(c));
            cfg[nameof(UVClientTCP.NoDelay)] = PomeloClientFactory.Config.NoDelay.ToString();
            cfg[nameof(UVClientTCP.RecvBufferSize)] = PomeloClientFactory.Config.BufferSize.ToString();
            cfg[nameof(UVClientTCP.SendBufferSize)] = PomeloClientFactory.Config.BufferSize.ToString();
            cfg[nameof(UVClientTCP.MaxRequestLength)] = PomeloClientFactory.Config.MaxPackageSize.ToString();
            return new UVClientTCP(client, cfg, pool, eventLoop.EventLoop);
        }
        public UVClient CreateClient(IExternalizableFactory codec, string name = null)
        {
            return new UVClient(codec, this, this.tcsPool, name);
        }
    }
    public class UVClient : PomeloClientAsync
    {
        internal UVClient(IExternalizableFactory codec, UVClientFactory factory, TaskCompletionSourcePool tcsPool, string name = null)
            : base(codec, name, 1000, tcsPool)
        {
        }
    }




}
