using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DeepCore.Pomelo
{
    [Reflectible]
    public class PomeloFactory
    {
        private static PomeloFactory s_instance = new PomeloFactory();
        public static PomeloFactory Instance { get { return s_instance; } }
        public PomeloFactory()
        {
            s_instance = this;
        }

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
    }
    
}
