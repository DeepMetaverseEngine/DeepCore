using DeepCore;
using DeepCore.IO;
using DeepCore.NetClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCrystal.NetServer
{

    public class ServerProtocolPool : IDisposable
    {
        private readonly IExternalizableFactory codec;
        private readonly ObjectPool<SendMessage> sendPool;
        private readonly ObjectPool<RecvMessage> recvPool;
        public ServerProtocolPool(IExternalizableFactory codec, bool singleThread = false)
        {
            this.codec = codec;
            if (singleThread)
            {
                this.sendPool = new SingleThreadObjectPool<SendMessage>();
                this.recvPool = new SingleThreadObjectPool<RecvMessage>();
            }
            else
            {
                this.sendPool = new ConcurrentObjectPool<SendMessage>();
                this.recvPool = new ConcurrentObjectPool<RecvMessage>();
            }
        }
        public void Dispose()
        {
            sendPool.Clear();
            recvPool.Clear();
        }
        private SendMessage CreateSend(ObjectPool pool) { return new SendMessage(sendPool, this.codec); }
        private RecvMessage CreateRecv(ObjectPool pool) { return new RecvMessage(recvPool, this.codec); }
        public SendMessage AllocSend()
        {
            return sendPool.Get(this, static (t, p) => t.CreateSend(p));
        }
        public RecvMessage AllocRecv()
        {
            var recv = recvPool.Get(this, static (t, p) => t.CreateRecv(p));
            recv.BufferLength = (IRecvMessage.FIXED_HEAD_SIZE);
            return recv;
        }
    }
    public class SendMessage : ISendMessage, IDisposable
    {
        internal ObjectPool<SendMessage> pool;
        internal SendMessage(ObjectPool<SendMessage> pool, IExternalizableFactory codec) : base(codec)
        {
            this.pool = pool;
        }
        protected override void Disposing()
        {
            base.Disposing();
            pool.Release(this);
        }
    }
    public class RecvMessage : IRecvMessage, IDisposable
    {
        internal ObjectPool<RecvMessage> pool;
        internal RecvMessage(ObjectPool<RecvMessage> pool, IExternalizableFactory codec) : base(codec)
        {
            this.pool = pool;
        }
        public void FillBuffer<ST>(ST st, ReadBuffer<ST> read, int count)
        {
            base.buffer.Expand(count);
            read(st, base.buffer.GetBuffer(), (int)base.buffer.Position, count);
            base.buffer.Position += count;
        }
        protected override void Disposing()
        {
            base.Disposing();
            pool.Release(this);
        }
    }

    public delegate void ReadBuffer<ST>(ST st, byte[] buffer, int pos, int count);

}
