using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DeepCore.Net.Utils
{
    public class ReceiveBuffer
    {
        private DeepCore.IO.MemoryStream stream;
        private long w_pos;

        public ReceiveBuffer(int capacity)
        {
            this.stream = new DeepCore.IO.MemoryStream(capacity);
            this.w_pos = 0;
        }
        public DeepCore.IO.MemoryStream Stream { get { return stream; } }
        public long Remaining
        {
            get
            {
                return w_pos - stream.Position;
            }
        }
        /// <summary>
        /// 向缓冲区写入数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Put(byte[] data, int offset, int count)
        {
            long oldpos = stream.Position;
            try
            {
                stream.Position = w_pos;
                IOUtil.WriteToEnd(stream, data, offset, count);
                w_pos += count;
            }
            finally
            {
                stream.Position = oldpos;
            }
        }

        public void Begin()
        {
            stream.Position = 0;
        }

        /// <summary>
        /// 将读取的数据制空
        /// </summary>
        public void Over()
        {
            if (stream.Position > 0 )
            {
                if (stream.Position == w_pos)
                {
                    stream.Position = 0;
                    w_pos = 0;
                }
                else if (stream.Position < w_pos)
                {
                    int lp = (int)stream.Position;
                    int ep = (int)w_pos;
                    int rm = ep - lp;

                    byte[] buffer = stream.GetBuffer();
                    // 将后半段向前移动
                    int cc = Math.Min(lp, rm);
                    System.Buffer.BlockCopy(buffer, lp, buffer, 0, cc);
                    if (lp == cc && rm > 0)
                    {
                        // 如果后半段还有更多
                        int c2 = cc << 1;
                        System.Buffer.BlockCopy(buffer, c2, buffer, lp, ep - c2);
                    }
                    stream.Position = 0;
                    w_pos -= lp;
                }
            }
        }
    }
}
