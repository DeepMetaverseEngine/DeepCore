using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace DeepCore.NetClient
{
    public class ProtocolPool
    {
        public virtual MemoryInputStream CreateInputStream(DeepCore.IO.MemoryStream stream, IExternalizableFactory codec)
        {
            return new MemoryInputStream(stream, codec) { Statistics = true };
        }
        public virtual MemoryOutputStream CreateOutputStream(DeepCore.IO.MemoryStream stream, IExternalizableFactory codec)
        {
            return new MemoryOutputStream(stream, codec) { Statistics = true };
        }
        /// <summary>
        /// 压缩流，从第四字节开始
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public virtual bool CompressStream(DeepCore.IO.MemoryStream buffer, ISendMessage msg) { return false; }
        public virtual bool DecompressStream(DeepCore.IO.MemoryStream buffer, IRecvMessage msg) { return false; }

        public static ProtocolPool Instance { get; private set; } = new ProtocolPool();
        public ProtocolPool()
        {
            Instance = this;
        }
    }

    public class FixedHeadProtocolDecoding : Disposable
    {
        public delegate void DecodeHead(Stream stream, out int pkgLength);
        public delegate void DecodeBody(Stream stream, int pkgLength);

        private readonly int maxRequestLength;
        private readonly int fixedHeadSize;
        private DecodeHead decodeHead;
        private DecodeBody decodeBody;
        private DeepCore.IO.MemoryStream recv = new DeepCore.IO.MemoryStream();
        private int readed = 0;
        private int pkgLength = 0;
        public FixedHeadProtocolDecoding(int fixedHeadSize, int maxRequestLength = int.MaxValue)
        {
            this.fixedHeadSize = fixedHeadSize;
            this.maxRequestLength = maxRequestLength;
        }
        public event DecodeHead OnDecodeHead { add => decodeHead += value; remove => decodeHead -= value; }
        public event DecodeBody OnDecodeBody { add => decodeBody += value; remove => decodeBody -= value; }
        protected override void Disposing()
        {
            decodeBody = null;
            decodeHead = null;
            recv = null;
        }
        public void OnReceived(DeepCore.IO.MemoryStream mem)
        {
            OnReceived(mem.GetBuffer(), 0, (int)mem.Length);
        }
        public void OnReceived(ArraySegment<byte> mem)
        {
            OnReceived(mem.Array, mem.Offset, mem.Count);
        }
        private void ProcessHead(Stream stream, out int pkgLength)
        {
            var pos = stream.Position;
            try
            {
                stream.Position = 0;
                decodeHead.Invoke(stream, out pkgLength);
            }
            finally
            {
                stream.Position = pos;
            }
        }
        private void ProcessBody(Stream stream, int pkgLength)
        {
            var pos = stream.Position;
            try
            {
                stream.Position = IRecvMessage.FIXED_HEAD_SIZE;
                decodeBody.Invoke(stream, pkgLength);
            }
            finally
            {
                stream.Position = pos;
            }
        }
        public void OnReceived(byte[] data, int offset, int count)
        {
            while (count > 0)
            {
                //首次Fill//
                if (readed < fixedHeadSize)
                {
                    int need = fixedHeadSize - readed;
                    if (count < need)
                    {
                        //头不够//
                        recv.Write(data, offset, count);
                        offset += count;
                        readed += count;
                        count = 0;
                        return;
                    }
                    else
                    {
                        recv.Write(data, offset, need);
                        offset += need;
                        readed += need;
                        count -= need;
                        ProcessHead(recv, out pkgLength);
                        recv.SetLength(IRecvMessage.FIXED_HEAD_SIZE + pkgLength);
                        if (pkgLength >= this.maxRequestLength)
                        {
                            throw new Exception(string.Format("PkgLength:{0} out of limit:{1} {2}", 
                                pkgLength, 
                                this.maxRequestLength,
                                recv));
                        }
                    }
                }
                else
                {
                    int need = (int)(recv.Length - readed);
                    if (count < need)
                    {
                        //身体不够//
                        recv.Write(data, offset, count);
                        offset += count;
                        readed += count;
                        count = 0;
                        return;
                    }
                    else
                    {
                        recv.Write(data, offset, need);
                        offset += need;
                        readed += need;
                        count -= need;
                        // Finish //
                        ProcessBody(recv, pkgLength);
                        recv.Position = 0;
                        recv.SetLength(0);
                        pkgLength = 0;
                        readed = 0;
                    }
                }
            }

        }
    }


    public class ProtocolDecoding<T> : Disposable where T : IRecvMessage
    {
        private readonly int maxRequestLength;
        private Func<T> alloc;
        private Action<T, Exception> complete;
        private T recv = null;
        private int readed = 0;

        public ProtocolDecoding(int maxRequestLength, Func<T> alloc, Action<T, Exception> complete)
        {
            this.maxRequestLength = maxRequestLength;
            this.alloc = alloc;
            this.complete = complete;
        }
        protected override void Disposing()
        {
            try { if (recv != null) { complete.Invoke(recv, null); } } catch { }
            complete = null;
            alloc = null;
            recv = null;
        }
        public void OnReceived(DeepCore.IO.MemoryStream mem)
        {
            OnReceived(mem.GetBuffer(), 0, (int)mem.Length);
        }
        public void OnReceived(ArraySegment<byte> mem)
        {
            OnReceived(mem.Array, mem.Offset, mem.Count);
        }
        public void OnReceived(byte[] data, int offset, int count)
        {
            try
            {
                while (count > 0)
                {
                    if (recv == null)
                    {
                        readed = 0;
                        recv = alloc();
                    }
                    //首次Fill//
                    if (readed < IRecvMessage.FIXED_HEAD_SIZE)
                    {
                        int need = IRecvMessage.FIXED_HEAD_SIZE - readed;
                        if (count < need)
                        {
                            //头不够//
                            recv.FillBuffer(data, offset, count);
                            offset += count;
                            readed += count;
                            count = 0;
                            return;
                        }
                        else
                        {
                            recv.FillBuffer(data, offset, need);
                            offset += need;
                            readed += need;
                            count -= need;
                            recv.ReadHead();
                            recv.BufferLength = IRecvMessage.FIXED_HEAD_SIZE + recv.PkgLength;
                            if (recv.PkgLength >= this.maxRequestLength)
                            {
                                throw new Exception(string.Format("PkgLength:{0} out of limit:{1} {2}",
                                    recv.PkgLength,
                                    this.maxRequestLength,
                                    recv));
                            }
                        }
                    }
                    else
                    {
                        int need = recv.BufferLength - readed;
                        if (count < need)
                        {
                            //身体不够//
                            recv.FillBuffer(data, offset, count);
                            offset += count;
                            readed += count;
                            count = 0;
                            return;
                        }
                        else
                        {
                            recv.FillBuffer(data, offset, need);
                            offset += need;
                            readed += need;
                            count -= need;
                            // Finish //
                            recv.BeginBody();
                            complete.Invoke(recv, null);
                            recv = null;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                complete.Invoke(recv, err);
                recv = null;
            }
        }
    }

    public class ProtocolDecodingQueue<T> : Disposable where T : IRecvMessage
    {
        private ProtocolDecoding<T> decoding;
        private Queue<DeepCore.IO.MemoryStream> recvQueue;

        public ProtocolDecodingQueue(int maxRequestLength, Func<T> alloc, Action<T, Exception> complete)
        {
            this.decoding = new ProtocolDecoding<T>(maxRequestLength, alloc, complete);
            this.recvQueue = new Queue<DeepCore.IO.MemoryStream>();
        }
        protected override void Disposing()
        {
            decoding.Dispose();
            lock (recvQueue)
            {
                foreach (var mem in recvQueue)
                {
                    mem.Dispose();
                }
                recvQueue.Clear();
            }
        }
        public void Enqueue(DeepCore.IO.MemoryStream mem)
        {
            lock (recvQueue) { recvQueue.Enqueue(mem); }
        }
        public void DecodeQueue()
        {
            while (true)
            {
                DeepCore.IO.MemoryStream mem;
                lock (recvQueue)
                {
                    if (recvQueue.TryDequeue(out mem) == false)
                    {
                        return;
                    }
                }
                try
                {
                    decoding.OnReceived(mem.GetBuffer(), 0, (int)mem.Length);
                }
                finally { mem.Dispose(); }
            }
        }

    }
}
