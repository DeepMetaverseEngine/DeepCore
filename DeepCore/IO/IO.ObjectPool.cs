using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepCore.IO
{
    public class IOStreamPool : Disposable
    {
        public IExternalizableFactory Factory { get; }
        public bool IsSingleThread { get; }
        public AbstractCollectionPool ObjectPool { get; set; }
        //-----------------------------------------------------------------------------------------------------------------------
        protected readonly ObjectPool<WrapperIn> input_pool;
        protected readonly ObjectPool<WrapperOut> output_pool;
        protected readonly MemoryStreamObjectPool stream_pool;
        public IOStreamPool(IExternalizableFactory factory, bool singleThread = false)
        {
            this.Factory = factory;
            this.IsSingleThread = singleThread;
            if (singleThread)
            {
                this.input_pool = new SingleThreadObjectPool<WrapperIn>();
                this.output_pool = new SingleThreadObjectPool<WrapperOut>();
            }
            else
            {
                this.AsSynchronizedDisposing();
                this.input_pool = new ConcurrentObjectPool<WrapperIn>();
                this.output_pool = new ConcurrentObjectPool<WrapperOut>();
            }
            this.stream_pool = new MemoryStreamObjectPool(singleThread);
        }
        protected override void Disposing()
        {
            (this.input_pool as SingleThreadObjectPool<WrapperIn>)?.Dispose();
            (this.output_pool as SingleThreadObjectPool<WrapperOut>)?.Dispose();
            this.stream_pool?.Dispose();
        }
        protected virtual WrapperIn CreateWrapperIn(ObjectPool pool)
        {
            return new WrapperIn(this, Factory);
        }
        protected virtual WrapperOut CreateWrapperOut(ObjectPool pool)
        {
            return new WrapperOut(this, Factory);
        }
        //-----------------------------------------------------------------------------------------------------------------------
        public MemoryStream AllocStream()
        {
            return stream_pool.AllocAutoRelease();
        }
        public MemoryStream AllocStream(ArraySegment<byte> init)
        {
            return stream_pool.AllocAutoRelease(init);
        }
        public MemoryStream AllocStream(byte[] init)
        {
            return stream_pool.AllocAutoRelease(init);
        }
        public InputStream AllocInputAutoRelease(Stream stream, bool autoReleaseStream = false)
        {
            WrapperIn ret = input_pool.Get(this, static (t, p) => t.CreateWrapperIn(p)) as WrapperIn;
            ret.SetStream(stream, autoReleaseStream);
            ret.ObjectPool = this.ObjectPool;
            return ret;
        }
        public OutputStream AllocOutputAutoRelease(Stream stream, bool autoReleaseStream = false)
        {
            WrapperOut ret = output_pool.Get(this, static (t, p) => t.CreateWrapperOut(p)) as WrapperOut;
            ret.SetStream(stream, autoReleaseStream);
            //ret.ObjectPool = this.ObjectPool;
            return ret;
        }
        //-----------------------------------------------------------------------------------------------------------------------
        public bool TryDecodeBinary<T>(BinaryMessage input, out T message)
        {
            var codec = Factory.GetCodec(input.Route);
            if (codec != null)
            {
                using (var buffer = this.AllocStream(input.DataSegment))
                using (var stream = this.AllocInputAutoRelease(buffer, false))
                {
                    buffer.Position = 0;
                    message = (T)codec.DoCreate(codec.MessageType);
                    stream.DecodeSerializable(codec, in message);
                    return true;
                }
            }
            message = default(T);
            return false;
        }
        public object DecodeBinary(ArraySegment<byte> input, TypeCodec codec)
        {
            using (var buffer = this.AllocStream(input))
            using (var stream = this.AllocInputAutoRelease(buffer, false))
            {
                buffer.Position = 0;
                var message = codec.DoCreate(codec.MessageType);
                stream.DecodeSerializable(codec, in message);
                return message;
            }
        }
        public object DecodeBinary(BinaryMessage bin)
        {
            var codec = Factory.GetCodec(bin.Route);
            if (codec != null)
            {
                return DecodeBinary(bin.DataSegment, codec);
            }
            else
            {
                return null;
            }
        }
        public T DecodeBinary<T>(BinaryMessage bin) where T : ISerializable
        {
            var codec = Factory.GetCodec(bin.Route);
            if (codec != null)
            {
                return (T)DecodeBinary(bin.DataSegment, codec);
            }
            else
            {
                return default(T);
            }
        }
        public object DecodeBinary(BinaryMessage bin, TypeCodec codec)
        {
            return DecodeBinary(bin.DataSegment, codec);
        }

        public bool TryEncodeBinary(object message, out BinaryMessage binary)
        {
            var codec = Factory.GetCodec(message.GetType());
            if (codec != null)
            {
                using (var buffer = this.AllocStream())
                using (var stream = this.AllocOutputAutoRelease(buffer, false))
                {
                    stream.EncodeSerializable(codec, message);
                    binary = BinaryMessage.FromBuffer(codec.MessageID, codec.MessageType, buffer);
                }
                return true;
            }
            binary = BinaryMessage.NULL;
            return false;
        }
        public BinaryMessage EncodeBinary(object message, TypeCodec codec)
        {
            using (var buffer = this.AllocStream())
            using (var stream = this.AllocOutputAutoRelease(buffer, false))
            {
                stream.EncodeSerializable(codec, message);
                return BinaryMessage.FromBuffer(codec.MessageID, codec.MessageType, buffer);
            }
        }

        public T CloneSerializable<T>(T msg) where T : ISerializable
        {
            if (msg == null) return default(T);
            if (msg is IRpcNoneSerializable) { return msg; }
            //             var codec = Factory.GetCodec(msg.GetType());
            //             if (codec != null)
            //             {
            //                 return (T)codec.DoClone(Factory, msg);
            //             }
            return msg.Clone<T>(Factory);
        }
        public ISerializable CloneSerializable(ISerializable msg)
        {
            if (msg == null) return null;
            if (msg is IRpcNoneSerializable) { return msg; }
            //             var codec = Factory.GetCodec(msg.GetType());
            //             if (codec != null)
            //             {
            //                 return (ISerializable)codec.DoClone(Factory, msg);
            //             }
            return msg.Clone<ISerializable>(Factory);
        }

        public T ToSerializable<T>(BinaryMessage bin) where T : ISerializable
        {
            if (TryDecodeBinary<T>(bin, out var message))
            {
                return message;
            }
            return default(T);
        }
        public ISerializable ToSerializable(BinaryMessage bin)
        {
            if (TryDecodeBinary<ISerializable>(bin, out var message))
            {
                return message;
            }
            return null;
        }
        public BinaryMessage ToBinary(ISerializable msg)
        {
            if (TryEncodeBinary(msg, out var binary))
            {
                return binary;
            }
            return binary;
        }

        public object FromBinaryNoHead(byte[] bin)
        {
            using (var buffer = this.AllocStream(bin))
            using (var stream = this.AllocInputAutoRelease(buffer, false))
            {
                return stream.GetObjAny();
            }
        }
        public byte[] ToBinaryNoHead(object msg)
        {
            using (var buffer = this.AllocStream())
            using (var stream = this.AllocOutputAutoRelease(buffer, false))
            {
                stream.PutObj(msg);
                var binary = new byte[buffer.Position];
                Buffer.BlockCopy(buffer.GetBuffer(), 0, binary, 0, binary.Length);
                return binary;
            }
        }

        //----------------------------------------------------------------------------------------------------------------


        public class WrapperIn : InputStream
        {
            public object state;
            private readonly IOStreamPool pool;
            private bool auto_release_stream;
            private Stream stream;
            public WrapperIn(IOStreamPool pool, IExternalizableFactory factory) : base(null, factory)
            {
                this.pool = pool;
            }
            internal void SetStream(Stream stream, bool autoReleaseStream)
            {
                this.stream = stream;
                this.auto_release_stream = autoReleaseStream;
                base.SetStream(stream);
            }
            protected override void Dispose(bool dispose)
            {
                if (auto_release_stream) stream.Dispose();
                this.stream = null;
                base.SetStream(null);
                this.pool.input_pool.Release(this);
            }
        }
        public class WrapperOut : OutputStream
        {
            public object state;
            private readonly IOStreamPool pool;
            private bool auto_release_stream;
            private Stream stream;
            public WrapperOut(IOStreamPool pool, IExternalizableFactory factory) : base(null, factory)
            {
                this.pool = pool;
            }
            internal void SetStream(Stream stream, bool autoReleaseStream)
            {
                this.stream = stream;
                this.auto_release_stream = autoReleaseStream;
                base.SetStream(stream);
            }
            protected override void Dispose(bool dispose)
            {
                if (auto_release_stream) stream.Dispose();
                this.stream = null;
                base.SetStream(null);
                this.pool.output_pool.Release(this);
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------

    }


    public static class IOStreamObjectPool
    {
        private static ObjectPool<In> s_input_pool = new ConcurrentObjectPool<In>();
        private static ObjectPool<Out> s_output_pool = new ConcurrentObjectPool<Out>();
        private static In CreateIn(ObjectPool pool) { return new In(); }
        private static Out CreateOut(ObjectPool pool) { return new Out(); }
        public static MemoryInputStream AllocInputAutoRelease(IExternalizableFactory factory, Stream stream, bool autoReleaseStream = false)
        {
            In ret = s_input_pool.Get(factory, static (t, p) => CreateIn(p)) as In;
            ret.SetStream(factory, stream, autoReleaseStream);
            return ret;
        }
        public static MemoryOutputStream AllocOutputAutoRelease(IExternalizableFactory factory, Stream stream, bool autoReleaseStream = false)
        {
            Out ret = s_output_pool.Get(factory, static (t, p) => CreateOut(p)) as Out;
            ret.SetStream(factory, stream, autoReleaseStream);
            return ret;
        }
        public static MemoryInputStream AllocInputAutoRelease(IExternalizableFactory factory)
        {
            In ret = s_input_pool.Get(factory, static (t, p) => CreateIn(p)) as In;
            ret.SetStream(factory, new MemoryStream(), true);
            return ret;
        }
        public static MemoryOutputStream AllocOutputAutoRelease(IExternalizableFactory factory)
        {
            Out ret = s_output_pool.Get(factory, static (t, p) => CreateOut(p)) as Out;
            ret.SetStream(factory, new MemoryStream(), true);
            return ret;
        }
        internal class In : MemoryInputStream
        {
            private bool auto_release_stream;
            private Stream stream;
            public In() : base(null, null)
            {
            }
            internal void SetStream(IExternalizableFactory factory, Stream stream, bool autoReleaseStream)
            {
                this.stream = stream;
                this.auto_release_stream = autoReleaseStream;
                base.SetFactory(factory);
                base.SetStream(stream);
            }
            protected override void Dispose(bool dispose)
            {
                if (auto_release_stream) stream.Dispose();
                this.stream = null;
                this.SetFactory(null);
                base.SetStream(null);
                s_input_pool.Release(this);
            }
        }
        internal class Out : MemoryOutputStream
        {
            private bool auto_release_stream;
            private Stream stream;
            public Out() : base(null, null)
            {
            }
            internal void SetStream(IExternalizableFactory factory, Stream stream, bool autoReleaseStream)
            {
                this.stream = stream;
                this.auto_release_stream = autoReleaseStream;
                base.SetFactory(factory);
                base.SetStream(stream);
            }
            protected override void Dispose(bool dispose)
            {
                if (auto_release_stream) stream.Dispose();
                this.stream = null;
                this.SetFactory(null);
                base.SetStream(null);
                s_output_pool.Release(this);
            }
        }

        public static AutoStream AllocAutoRelease(IExternalizableFactory factory)
        {
            var buffer = new MemoryStream();
            var input = (In)AllocInputAutoRelease(factory, buffer, false);
            var output = (Out)AllocOutputAutoRelease(factory, buffer, false);
            return new AutoStream(buffer, input, output);
        }
        public static AutoStream AllocAutoRelease(IExternalizableFactory factory, byte[] data)
        {
            var buffer = new MemoryStream(data);
            var input = (In)AllocInputAutoRelease(factory, buffer, false);
            var output = (Out)AllocOutputAutoRelease(factory, buffer, false);
            return new AutoStream(buffer, input, output);
        }
        public struct AutoStream : IDisposable
        {
            private MemoryStream buffer;
            private MemoryInputStream input;
            private MemoryOutputStream output;
            public MemoryStream Buffer { get => buffer; }
            public MemoryInputStream Input { get => input; }
            public MemoryOutputStream Output { get => output; }
            internal AutoStream(MemoryStream buffer, In input, Out output)
            {
                this.buffer = buffer;
                this.input = input;
                this.output = output;
            }
            public void Flip()
            {
                buffer.Position = 0;
            }
            void IDisposable.Dispose()
            {
                output.Dispose();
                input.Dispose();
                buffer.Dispose();
            }
        }
    }

}
