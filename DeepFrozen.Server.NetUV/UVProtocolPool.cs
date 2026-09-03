using DeepCore;
using DeepCore.IO;
using DeepCore.NetClient;
using DeepCrystal.NetServer;
using NetUV.Core.Buffers;
using NetUV.Core.Handles;
using System;

namespace DeepFrozen.Server.NetUV
{

    public class ProtocolDecodeingSlim : IDisposable
    {
        private Func<RecvMessage> alloc;
        private Action<Tcp, RecvMessage, Exception> complete;
        private RecvMessage recv = null;
        private int readed = 0;

        public ProtocolDecodeingSlim(Func<RecvMessage> alloc, Action<Tcp, RecvMessage, Exception> complete)
        {
            this.alloc = alloc;
            this.complete = complete;
        }

        public void Dispose()
        {
            if (recv != null)
            {
                complete(null, recv, null);
                recv = null;
            }
        }
        public void OnReceived(Tcp tcp, ReadableBuffer buffer)
        {
            var count = buffer.Count;
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
                            recv.FillBuffer(buffer, static (buffer, data, offset, count) => buffer.ReadBytes(data, offset, count), count);
                            readed += count;
                            count = 0;
                            return;
                        }
                        else
                        {
                            recv.FillBuffer(buffer, static (buffer, data, offset, count) => buffer.ReadBytes(data, offset, count), need);
                            readed += need;
                            count -= need;
                            recv.ReadHead();
                            recv.BufferLength = IRecvMessage.FIXED_HEAD_SIZE + recv.PkgLength;
                        }
                    }
                    else
                    {
                        int need = recv.BufferLength - readed;
                        if (count < need)
                        {
                            //身体不够//
                            recv.FillBuffer(buffer, static (buffer, data, offset, count) => buffer.ReadBytes(data, offset, count), count);
                            readed += count;
                            count = 0;
                            return;
                        }
                        else
                        {
                            recv.FillBuffer(buffer, static (buffer, data, offset, count) => buffer.ReadBytes(data, offset, count), need);
                            readed += need;
                            count -= need;
                            // Finish //
                            recv.BeginBody();
                            complete.Invoke(tcp, recv, null);
                            recv = null;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                complete.Invoke(tcp, recv, err);
                recv = null;
            }
        }
    }

}
