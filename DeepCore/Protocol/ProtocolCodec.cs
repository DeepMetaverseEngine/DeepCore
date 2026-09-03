using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCore.Reflection;
using System;
using System.IO;

namespace DeepCore.Protocol
{
    [Reflectible]
    public interface INetPackageCodec
    {
        /// <summary>
        /// 将网络流中的数据解析成对象
        /// </summary>
        /// <param name="input"></param>
        /// <param name="message"></param>
        /// <returns>返回true则表示此次对象解析完成，进行下一次解析</returns>
        bool DoDecode(Stream input, out object message);

        /// <summary>
        /// 将对象编码传输至网络流
        /// </summary>
        /// <param name="output"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool DoEncode(Stream output, object message);

        /// <summary>
        /// 将网络流中的数据解析成对象
        /// </summary>
        /// <param name="input"></param>
        /// <param name="message"></param>
        /// <returns>返回true则表示此次对象解析完成，进行下一次解析</returns>
        bool DoDecode(ArraySegment<byte> input, out object message);

        /// <summary>
        /// 将对象编码传输至网络流
        /// </summary>
        /// <param name="message"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        bool DoEncode(object message, out ArraySegment<byte> output);
    }


    /*
    public class SimpleExternalizableCodec : INetPackageCodec
    {
        private Logger log = LoggerFactory.GetLogger("SimpleExternalizableCodec");
        private IExternalizableFactory factory;

        public DeepCore.IO.IExternalizableFactory Factory
        {
            get { return factory; }
        }
        public SimpleExternalizableCodec(IExternalizableFactory externalizableFactory)
        {
            this.factory = externalizableFactory;
        }

        public bool DoDecode(Stream input, out object message)
        {
            InputStream reader = new InputStream(input, factory);
            int typeInt = reader.GetS32();
            Type type = factory.GetType(typeInt);
            if (type == null)
            {
                log.Error("Unknow Protocol : >>>0x" + typeInt.ToString("X") + "<<<");
                message = null;
                return false;
            }
            else
            {
                IMessage nm = (IMessage)ReflectionUtil.CreateInstance(type);
                reader.DecodeExternalizable(nm);
                message = nm;
                return true;
            }
        }

        public bool DoEncode(Stream output, object message)
        {
            IMessage nm = (IMessage)message;
            OutputStream writer = new OutputStream(output, factory);
            int typeInt = factory.GetTypeID(message.GetType());
            if (typeInt == 0)
            {
                log.Error("Unknow Protocol : >>>0x" + typeInt.ToString("X") + "<<< - " + message.GetType().FullName);
                return false;
            }
            writer.PutS32(typeInt);
            writer.EncodeExternalizable(nm);
            return true;
        }

        public  bool DoDecode(ArraySegment<byte> input, out object message)
        {
            using (var buffer = AllocStream(input))
            using (var stream = AllocInputAutoRelease(buffer))
            {
                return doDecode(stream, out message);
            }
        }
        public  bool DoEncode(object message, out ArraySegment<byte> output)
        {
            IMessage nm = (IMessage)message;
            OutputStream writer = new OutputStream(output, factory);
            int typeInt = factory.GetTypeID(message.GetType());
            if (typeInt == 0)
            {
                log.Error("Unknow Protocol : >>>0x" + typeInt.ToString("X") + "<<< - " + message.GetType().FullName);
                return false;
            }
            writer.PutS32(typeInt);
            writer.EncodeExternalizable(nm);
        }
    }
    */
}
