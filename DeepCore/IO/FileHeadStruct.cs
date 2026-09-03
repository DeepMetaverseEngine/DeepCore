using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepCore.IO
{
    public abstract class FileHeadStruct
    {
        public abstract IExternalizableFactory Codec { get; }
        public abstract string Head { get; }
        public int Version { get; set; }
        public object Chunk { get; set; }

        public static FT Load<FT>(IInputStream input) where FT : FileHeadStruct, new()
        {
            input.ARRAY_LIMIT = input.BYTES_LIMIT = int.MaxValue;
            var ret = new FT();
            ret.Read(input);
            return ret;
        }
        public static FT Load<FT>(Stream stream) where FT : FileHeadStruct, new()
        {
            var ret = new FT();
            var input = new InputStream(stream, ret.Codec);
            input.ARRAY_LIMIT = input.BYTES_LIMIT = int.MaxValue;
            ret.Read(input);
            return ret;
        }
        public static FT Load<FT>(byte[] data) where FT : FileHeadStruct, new()
        {
            using (var stream = new IO.MemoryStream(data))
            {
                return Load<FT>(stream);
            }
        }
        public static FT Load<FT>(FileInfo file) where FT : FileHeadStruct, new()
        {
            using (var stream = file.OpenRead())
            {
                return Load<FT>(stream);
            }
        }

        public static void Save(FileHeadStruct chunk, IOutputStream output)
        {
            output.ARRAY_LIMIT = output.BYTES_LIMIT = int.MaxValue;
            chunk.Write(output);
        }
        public static void Save(FileHeadStruct chunk, Stream stream)
        {
            Save(chunk, new OutputStream(stream, chunk.Codec));
        }
        public static byte[] Save(FileHeadStruct chunk)
        {
            using (var stream = new IO.MemoryStream())
            {
                Save(chunk, stream);
                return stream.ToArray();
            }
        }
        public static void Save(FileHeadStruct chunk, FileInfo file)
        {
            using (var stream = file.OpenWrite())
            {
                Save(chunk, stream);
            }
        }


        protected void Read(IInputStream input)
        {
            if (input.TryValidateFileHeadASCII(Head, out var head))
            {
                this.Version = input.GetS32();
                this.Chunk = ReadContent(input);
            }
            else
            {
                throw new Exception($"Read {Head} HEAD Error : {head}");
            }
        }
        protected void Write(IOutputStream output)
        {
            output.SaveFileHeadASCII(Head);
            output.PutS32(Version);
            WriteContent(output, Chunk);
        }

        protected abstract object ReadContent(IInputStream input);
        protected abstract void WriteContent(IOutputStream output, object content);
    }


    public abstract class FileHeadStruct<T> : FileHeadStruct
    {
        new public T Chunk
        {
            get { return (T)base.Chunk; }
            set { base.Chunk = value; }
        }
        sealed protected override void WriteContent(IOutputStream output, object content)
        {
            WriteTContent(output, (T)content);
        }
        sealed protected override object ReadContent(IInputStream input)
        {
            return ReadTContent(input);
        }
        protected abstract void WriteTContent(IOutputStream output, T content);
        protected abstract T ReadTContent(IInputStream input);
    }


    public abstract class ExternalizableFileHeadStruct<T> : FileHeadStruct<T> where T : IExternalizable
    {
        protected override void WriteTContent(IOutputStream output, T content)
        {
            output.PutExt(content);
        }
        protected override T ReadTContent(IInputStream input)
        {
            return input.GetExt<T>();
        }
    }
}
