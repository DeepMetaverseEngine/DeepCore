using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DeepCore.IO
{
    public class CachedGZipStream : Stream
    {
        private int _bufferIndex = 0;
        private int _bufferLoaded = 0;
        private readonly byte[] _buffer;
        private readonly byte[] _buffer2;
        private readonly Stream stream;
        private readonly CompressionMode compressionMode;
        public CachedGZipStream(Stream stream, CompressionMode compressionMode, int bufferSize = 1024 * 1024)
        {
            this.stream = stream;
            this.compressionMode = compressionMode;
            this._buffer = new byte[bufferSize];
            this._buffer2 = new byte[bufferSize];
        }
        public override bool CanSeek => false;
        public override bool CanRead => stream.CanRead;
        public override bool CanWrite => stream.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Flush()
        {
            WriteCache();
        }
        public override int Read(byte[] dst, int offset, int count)
        {
            var remain = count;
            var dstIndex = offset;
            while (remain > 0)
            {
                var available = _bufferLoaded - _bufferIndex;
                if (available > 0)
                {
                    var readed = 0;
                    if (available >= remain)
                    {
                        readed = remain;
                    }
                    else if (available < remain)
                    {
                        readed = available;
                    }
                    Buffer.BlockCopy(_buffer, _bufferIndex, dst, dstIndex, readed);
                    remain -= readed;
                    dstIndex += readed;
                    _bufferIndex += readed;
                }
                else
                {
                    LoadCache();
                }
            }
            return count;
        }
        public override void Write(byte[] src, int offset, int count)
        {
            var remain = count;
            var srcIndex = offset;
            while (remain > 0)
            {
                var available = _buffer.Length - _bufferIndex;
                if (available > 0)
                {
                    var writed = 0;
                    if (available >= remain)
                    {
                        writed = remain;
                    }
                    else if (available < remain)
                    {
                        writed = available;
                    }
                    Buffer.BlockCopy(src, srcIndex, _buffer, _bufferIndex, writed);
                    remain -= writed;
                    srcIndex += writed;
                    _bufferIndex += writed;
                }
                else
                {
                    WriteCache();
                }
            }
        }
        private void LoadCache()
        {
            if (_bufferIndex > _bufferLoaded)
            {
                throw new Exception($"Buffer Overflow !!! {_bufferIndex} > {_bufferLoaded}");
            }
            else if (_bufferIndex == _bufferLoaded)
            {
                using (var load = new BeginLoadPosition(stream))
                {
                    IOUtil.ReadToEnd(stream, _buffer2, 0, load.Length);
                    using (var gz = new GZipStream(new DeepCore.IO.MemoryStream(_buffer2, 0, load.Length), compressionMode, true))
                    {
                        IOUtil.ReadToEnd(gz, _buffer, 0, _buffer.Length);
                        _bufferIndex = 0;
                        _bufferLoaded = _buffer.Length;
                    }

                }

            }
        }

        private void WriteCache()
        {
            if (_bufferIndex > _buffer.Length)
            {
                throw new Exception($"Buffer Overflow !!! {_bufferIndex} > {_buffer.Length}");
            }
            else if (_bufferIndex > 0)
            {
                using (new BeginSavePosition(stream))
                {
                    using (var ms = new DeepCore.IO.MemoryStream(_buffer2))
                    {
                        using (var gz = new GZipStream(ms, compressionMode, true))
                        {
                            gz.Write(_buffer, 0, _buffer.Length);
                            gz.Flush();
                            Array.Clear(_buffer, 0, _buffer.Length);
                            _bufferIndex = 0;
                        }
                        IOUtil.WriteToEnd(stream, _buffer2, 0, (int)ms.Position);
                    }
                }

            }
        }

    }
}
