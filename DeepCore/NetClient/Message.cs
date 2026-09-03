using DeepCore.IO;
using System;

namespace DeepCore.NetClient
{
    //----------------------------------------------------------------------------------------------------
    public class MessagePool : IDisposable
    {
        private readonly IExternalizableFactory codec;
        private readonly ObjectPool<SendMessage> s_SendPool;
        private readonly ObjectPool<RecvMessage> s_RecvPool;

        public MessagePool(IExternalizableFactory codec, bool singleThread=false)
        {
            this.codec = codec;
            if (singleThread)
            {
                this.s_SendPool = new SingleThreadObjectPool<SendMessage>();
                this.s_RecvPool = new SingleThreadObjectPool<RecvMessage>();
            }
            else
            {
                this.s_SendPool = new ConcurrentObjectPool<SendMessage>();
                this.s_RecvPool = new ConcurrentObjectPool<RecvMessage>();
            }
        }
        public void Dispose()
        {
            s_SendPool.Clear();
            s_RecvPool.Clear();
        }

        private SendMessage newSend(ObjectPool pool) { return new SendMessage(this); }
        private RecvMessage newRecv(ObjectPool pool) { return new RecvMessage(this); }

        public SendMessage AllocSend()
        {
            SendMessage ret = s_SendPool.Get(this, static (t, p) => t.newSend(p));
            ret.BufferLength = SendMessage.FIXED_HEAD_SIZE;
            ret.BufferPosition = 0;
            return ret;
        }
        public RecvMessage AllocRecv()
        {
            RecvMessage ret = s_RecvPool.Get(this, static (t, p) => t.newRecv(p));
            ret.BufferLength = RecvMessage.FIXED_HEAD_SIZE;
            ret.BufferPosition = 0;
            return ret;
        }

        public class SendMessage : ISendMessage, IDisposable
        {
            private readonly MessagePool pool;
            public IClientAdapter adapter;
            public Action<SendMessage> callback;
            public object token;
            public SendMessage(MessagePool pool) : base(pool.codec)
            {
                this.pool = pool;
            }
            protected override void Disposing()
            {
                this.token = null;
                this.callback = null;
                this.adapter = null;
                base.Disposing();
                pool.s_SendPool.Release(this);
            }
        }
        public class RecvMessage : IRecvMessage, IDisposable
        {
            private readonly MessagePool pool;
            public IClientAdapter adapter;
            public Action<RecvMessage> callback;
            public object token;
            public RecvMessage(MessagePool pool) : base(pool.codec)
            {
                this.pool = pool;
            }
            protected override void Disposing()
            {
                this.token = null;
                this.callback = null;
                this.adapter = null;
                base.Disposing();
                pool.s_RecvPool.Release(this);
            }
        }
    }

    //----------------------------------------------------------------------------------------------------

    public class PushHandler : Disposable, IPushHandler
    {
        INetClient IPushHandler.Client => Client;
        public INetClient Client { get { return client; } }
        public bool IsBinary { get { return callback_bin != null; } }
        public bool IsClear { get; private set; }
        public bool IsRecursion { get; private set; }
        public int Route { get { return route; } }
        public readonly INetClient client;
        public readonly int route;
        private Action<IPushHandler, ISerializable> callback;
        private Action<IPushHandler, BinaryMessage> callback_bin;

        public PushHandler(INetClient client, int route, Action<IPushHandler, ISerializable> cb, Action<IPushHandler, BinaryMessage> cbb, bool recursion)
        {
            this.client = client;
            this.route = route;
            this.callback = cb;
            this.callback_bin = cbb;
            this.IsClear = false;
            this.IsRecursion = recursion;
        }
        internal void Invoke(ISerializable data)
        {
            try
            {
                if (!IsClear) callback(this, data);
            }
            catch (Exception err)
            {
                client.log.Error($"PushHandler Invoke Error : MsgType={data?.GetType()}", err);
                client.onError(err);
            }
        }
        internal void InvokeBin(BinaryMessage data)
        {
            try
            {
                if (!IsClear) callback_bin(this, data);
            }
            catch (Exception err)
            {
                client.log.Error($"PushHandler Invoke Error : MsgType={data.Route}", err);
                client.onError(err);
            }
        }
        protected override void Disposing()
        {
            this.IsClear = true;
            this.callback = null;
            this.callback_bin = null;
        }
        public void Clear()
        {
            client.remove_push(this);
        }
    }


    //----------------------------------------------------------------------------------------------------

    public class RpcRequestHandler : Disposable, IRpcRequestHandler
    {
        INetClient IRpcRequestHandler.Client => Client;
        public INetClient Client { get { return client; } }
        public bool IsBinary { get { return callback_bin != null; } }
        public bool IsClear { get; private set; }
        internal readonly INetClient client;
        private Func<ISerializable, Action<ISerializable>, bool> callback;
        private Func<BinaryMessage, Action<BinaryMessage>, bool> callback_bin;

        internal RpcRequestHandler(INetClient client, Func<ISerializable, Action<ISerializable>, bool> cb, Func<BinaryMessage, Action<BinaryMessage>, bool> cbb)
        {
            this.client = client;
            this.callback = cb;
            this.callback_bin = cbb;
            this.IsClear = false;
        }
        protected override void Disposing()
        {
            this.IsClear = true;
            this.callback = null;
            this.callback_bin = null;
        }
        internal bool Invoke(ISerializable data, uint sendID)
        {
            try
            {
                if (!IsClear)
                {
                    return callback(data, (rsp) =>
                    {
                        client.send(rsp, MessageType.MSG_RPC_RESPONSE_C2S, sendID);
                    });
                }
            }
            catch (Exception err)
            {
                client.log.Error($"RpcRequestHandler Invoke Error : MsgType={data?.GetType()} SendID={sendID}", err);
                client.onError(err);
            }
            return false;
        }
        internal bool InvokeBin(BinaryMessage data, uint sendID)
        {
            try
            {
                if (!IsClear)
                {
                    return callback_bin(data, (rsp) =>
                    {
                        client.send(rsp, MessageType.MSG_RPC_RESPONSE_C2S, sendID);
                    });
                }
            }
            catch (Exception err)
            {
                client.log.Error($"RpcRequestHandler Invoke Error : MsgType={data.Route} SendID={sendID}", err);
                client.onError(err);
            }
            return false;
        }
        public void Clear()
        {
            client.remove_request(this);
        }
    }

    //----------------------------------------------------------------------------------------------------


    public struct RequestHandler
    {
        public INetClient Client { get { return client; } }
        public uint SendID { get { return send_id; } }
        public bool IsBinary { get { return callback_bin != null; } }
        public double StartTimeMS { get { return start_time_ms; } }
        public bool Infinity { get { return infinity; } }

        public readonly string Route;
        private readonly INetClient client;
        private readonly uint send_id;
        private readonly Action<NetException, ISerializable> callback;
        private readonly Action<NetException, BinaryMessage> callback_bin;
        private readonly double start_time_ms;
        private readonly bool infinity;
        internal RequestHandler(INetClient client, string route, uint send_id, bool infinity, Action<NetException, ISerializable> callback, Action<NetException, BinaryMessage> callback_bin)
        {
            this.client = client;
            this.Route = route;
            this.send_id = send_id;
            this.callback = callback;
            this.callback_bin = callback_bin;
            this.infinity = infinity;
            this.start_time_ms = CUtils.TickTimeMS;
        }

        internal void Invoke(ISerializable data)
        {
            try
            {
                callback(null, data);
            }
            catch (Exception err)
            {
                client.log.Error($"RequestHandler Invoke Error : MsgType={data?.GetType()}", err);
                client.onError(err);
            }
        }
        internal void InvokeBin(BinaryMessage data)
        {
            try
            {
                callback_bin(null, data);
            }
            catch (Exception err)
            {
                client.log.Error($"RequestHandler Invoke Error : MsgType={data.Route}", err);
                client.onError(err);
            }
        }
        internal void Invoke(NetException err)
        {
            try
            {
                if (callback != null) callback(err, null);
                else if (callback_bin != null) callback_bin(err, BinaryMessage.NULL);
            }
            catch (Exception err2)
            {
                client.onError(err2);
            }
        }
        internal bool CheckTimeout(double timeout_ms, double current_time_ms)
        {
            if (infinity) return false;
            if (start_time_ms + timeout_ms < current_time_ms)
            {
                return true;
            }
            return false;
        }
    }
    //     public struct TResponse<T> where T : ISerializable
    //     {
    //         public T rsp;
    //         public PomeloException err;
    //         public object state;
    //         public bool IsSuccess { get { return err == null && rsp is Response r && r.IsSuccess; } }
    //         public override string ToString()
    //         {
    //             return err != null ? err.Message : $"{rsp}";
    //         }
    //     }
}