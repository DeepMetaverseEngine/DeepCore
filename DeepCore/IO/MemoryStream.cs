// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#define SYSTEM_IO_MEMORY_STREAM
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore.IO
{
#if SYSTEM_IO_MEMORY_STREAM
    public class MemoryStream : System.IO.MemoryStream
    {
        public MemoryStream()
        {
        }
        public MemoryStream(int capacity) : base(capacity)
        {
        }
        public MemoryStream(byte[] buffer) : this(buffer, 0, buffer.Length, true)
        {
        }
        public MemoryStream(byte[] buffer, bool writable) : this(buffer, 0, buffer.Length, writable)
        {
        }
        public MemoryStream(byte[] buffer, int index, int count) : this(buffer, index, count, true)
        {
        }
        public MemoryStream(byte[] buffer, int index, int count, bool writable) : base(buffer, index, count, writable, true)
        {
        }
        public void Expand(int count)
        {
            this.Capacity = (int)Math.Max(this.Capacity, this.Position + count);
        }
        public void ExpandTo(int capacity)
        {
            this.Capacity = Math.Max(this.Capacity, capacity);
        }
    }
#else
    // A MemoryStream represents a Stream in memory (ie, it has no backing store).
    // This stream may reduce the need for temporary buffers and files in
    // an application.
    //
    // There are two ways to create a MemoryStream.  You can initialize one
    // from an unsigned byte array, or you can create an empty one.  Empty
    // memory streams are resizable, while ones created with a byte array provide
    // a stream "view" of the data.
    public class MemoryStream : Stream
    {
        private byte[] _buffer;    // Either allocated internally or externally.
        private readonly int _origin;       // For user-provided arrays, start at this origin
        private int _position;     // read/write head.
        private int _length;       // Number of bytes within the memory stream
        private int _capacity;     // length of usable portion of buffer for stream
        // Note that _capacity == _buffer.Length for non-user-provided byte[]'s

        private Task<int> _lastReadTask; // The last successful task returned from ReadAsync

        private const int MemStreamMaxLength = int.MaxValue;



        public MemoryStream()
            : this(0)
        {
        }

        public MemoryStream(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "SR.ArgumentOutOfRange_NegativeCapacity");

            _buffer = capacity != 0 ? new byte[capacity] : Array.Empty<byte>();
            _capacity = capacity;
            _origin = 0;      // Must be 0 for byte[]'s created by MemoryStream
        }

        public MemoryStream(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "SR.ArgumentNull_Buffer");

            _buffer = buffer;
            _length = _capacity = buffer.Length;
            _origin = 0;
        }

        public MemoryStream(byte[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "SR.ArgumentNull_Buffer");
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - index < count)
                throw new ArgumentException("SR.Argument_InvalidOffLen");

            _buffer = buffer;
            _origin = _position = index;
            _length = _capacity = index + count;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;


        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    // Don't set buffer to null - allow TryGetBuffer, GetBuffer & ToArray to work.
                    _lastReadTask = null;
                }
            }
            finally
            {
                // Call base.Close() to cleanup async IO resources
                base.Dispose(disposing);
            }
        }

        // returns a bool saying whether we allocated a new array.
        private bool EnsureCapacity(int value)
        {
            // Check for overflow
            if (value < 0)
                throw new IOException("SR.IO_StreamTooLong");

            if (value > _capacity)
            {
                int newCapacity = Math.Max(value, 256);

                // We are ok with this overflowing since the next statement will deal
                // with the cases where _capacity*2 overflows.
                if (newCapacity < _capacity * 2)
                {
                    newCapacity = _capacity * 2;
                }

                // We want to expand the array up to Array.MaxByteArrayLength
                // And we want to give the user the value that they asked for
                if ((uint)(_capacity * 2) > MemStreamMaxLength)
                {
                    newCapacity = Math.Max(value, MemStreamMaxLength);
                }

                Capacity = newCapacity;
                return true;
            }
            return false;
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            try
            {
                Flush();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }


        public virtual byte[] GetBuffer()
        {
            return _buffer;
        }

        public virtual bool TryGetBuffer(out ArraySegment<byte> buffer)
        {
            buffer = new ArraySegment<byte>(_buffer, offset: _origin, count: (_length - _origin));
            return true;
        }

        // -------------- PERF: Internal functions for fast direct access of MemoryStream buffer (cf. BinaryReader for usage) ---------------

        // PERF: Internal sibling of GetBuffer, always returns a buffer (cf. GetBuffer())
        internal byte[] InternalGetBuffer()
        {
            return _buffer;
        }

        // PERF: True cursor position, we don't need _origin for direct access
        internal int InternalGetPosition()
        {
            return _position;
        }

        // PERF: Expose internal buffer for BinaryReader instead of going via the regular Stream interface which requires to copy the data out
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> InternalReadSpan(int count)
        {
            int origPos = _position;
            int newPos = origPos + count;

            if ((uint)newPos > (uint)_length)
            {
                _position = _length;
                throw new Exception();
            }

            var span = new ReadOnlySpan<byte>(_buffer, origPos, count);
            _position = newPos;
            return span;
        }

        // PERF: Get actual length of bytes available for read; do sanity checks; shift position - i.e. everything except actual copying bytes
        internal int InternalEmulateRead(int count)
        {
            int n = _length - _position;
            if (n > count)
                n = count;
            if (n < 0)
                n = 0;

            Debug.Assert(_position + n >= 0, "_position + n >= 0");  // len is less than 2^31 -1.
            _position += n;
            return n;
        }
        public void Expand(int count)
        {
            this.Capacity = (int)Math.Max(this.Capacity, this.Position + count);
        }

        // Gets & sets the capacity (number of bytes allocated) for this stream.
        // The capacity cannot be set to a value less than the current length
        // of the stream.
        //
        public virtual int Capacity
        {
            get
            {
                return _capacity - _origin;
            }
            set
            {
                // Only update the capacity if the MS is expandable and the value is different than the current capacity.
                // Special behavior if the MS isn't expandable: we don't throw if value is the same as the current capacity
                if (value < Length)
                    throw new ArgumentOutOfRangeException(nameof(value), "SR.ArgumentOutOfRange_SmallCapacity");

                // MemoryStream has this invariant: _origin > 0 => !expandable (see ctors)
                if (value != _capacity)
                {
                    if (value > 0)
                    {
                        byte[] newBuffer = new byte[value];
                        if (_length > 0)
                        {
                            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
                        }
                        _buffer = newBuffer;
                    }
                    else
                    {
                        _buffer = Array.Empty<byte>();
                    }
                    _capacity = value;
                }
            }
        }

        public override long Length
        {
            get
            {
                return _length - _origin;
            }
        }

        public override long Position
        {
            get
            {
                return _position - _origin;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "SR.ArgumentOutOfRange_NeedNonNegNum");


                if (value > MemStreamMaxLength)
                    throw new ArgumentOutOfRangeException(nameof(value), "SR.ArgumentOutOfRange_StreamLength");
                _position = _origin + (int)value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "SR.ArgumentNull_Buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - offset < count)
                throw new ArgumentException("SR.Argument_InvalidOffLen");


            int n = _length - _position;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;

            Debug.Assert(_position + n >= 0, "_position + n >= 0");  // len is less than 2^31 -1.

            if (n <= 8)
            {
                int byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _buffer[_position + byteCount];
            }
            else
                Buffer.BlockCopy(_buffer, _position, buffer, offset, n);
            _position += n;

            return n;
        }

        public int Read(Span<byte> buffer)
        {
            if (GetType() != typeof(MemoryStream))
            {
                // MemoryStream is not sealed, and a derived type may have overridden Read(byte[], int, int) prior
                // to this Read(Span<byte>) overload being introduced.  In that case, this Read(Span<byte>) overload
                // should use the behavior of Read(byte[],int,int) overload.
                return StreamExt.Read(this, buffer);
            }

            int n = Math.Min(_length - _position, buffer.Length);
            if (n <= 0)
                return 0;

            // TODO https://github.com/dotnet/coreclr/issues/15076:
            // Read(byte[], int, int) has an n <= 8 optimization, presumably based
            // on benchmarking.  Determine if/where such a cut-off is here and add
            // an equivalent optimization if necessary.
            new Span<byte>(_buffer, _position, n).CopyTo(buffer);

            _position += n;
            return n;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "SR.ArgumentNull_Buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - offset < count)
                throw new ArgumentException("SR.Argument_InvalidOffLen");

            // If cancellation was requested, bail early
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            try
            {
                int n = Read(buffer, offset, count);
                var t = _lastReadTask;
                Debug.Assert(t == null || t.Status == TaskStatus.RanToCompletion,
                    "Expected that a stored last task completed successfully");
                return (t != null && t.Result == n) ? t : (_lastReadTask = Task.FromResult<int>(n));
            }
            catch (OperationCanceledException oce)
            {
                return Task.FromCanceled<int>(oce.CancellationToken);
            }
            catch (Exception exception)
            {
                return Task.FromException<int>(exception);
            }
        }
        public override int ReadByte()
        {
            if (_position >= _length)
                return -1;

            return _buffer[_position++];
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // This implementation offers better performance compared to the base class version.

            //StreamHelpers.ValidateCopyToArgs(this, destination, bufferSize);

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to ReadAsync() which a subclass might have overridden.
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into ReadAsync) when we are not sure.
            if (GetType() != typeof(MemoryStream))
                return base.CopyToAsync(destination, bufferSize, cancellationToken);

            // If cancelled - return fast:
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            // Avoid copying data from this buffer into a temp buffer:
            //   (require that InternalEmulateRead does not throw,
            //    otherwise it needs to be wrapped into try-catch-Task.FromException like memStrDest.Write below)

            int pos = _position;
            int n = InternalEmulateRead(_length - _position);

            // If we were already at or past the end, there's no copying to do so just quit.
            if (n == 0)
                return Task.CompletedTask;

            // If destination is not a memory stream, write there asynchronously:
            if (!(destination is MemoryStream memStrDest))
                return destination.WriteAsync(_buffer, pos, n, cancellationToken);

            try
            {
                // If destination is a MemoryStream, CopyTo synchronously:
                memStrDest.Write(_buffer, pos, n);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }


        public override long Seek(long offset, SeekOrigin loc)
        {
            if (offset > MemStreamMaxLength)
                throw new ArgumentOutOfRangeException(nameof(offset), "SR.ArgumentOutOfRange_StreamLength");

            switch (loc)
            {
                case SeekOrigin.Begin:
                    {
                        int tempPosition = unchecked(_origin + (int)offset);
                        if (offset < 0 || tempPosition < _origin)
                            throw new IOException("SR.IO_SeekBeforeBegin");
                        _position = tempPosition;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        int tempPosition = unchecked(_position + (int)offset);
                        if (unchecked(_position + offset) < _origin || tempPosition < _origin)
                            throw new IOException("SR.IO_SeekBeforeBegin");
                        _position = tempPosition;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        int tempPosition = unchecked(_length + (int)offset);
                        if (unchecked(_length + offset) < _origin || tempPosition < _origin)
                            throw new IOException("SR.IO_SeekBeforeBegin");
                        _position = tempPosition;
                        break;
                    }
                default:
                    throw new ArgumentException("SR.Argument_InvalidSeekOrigin");
            }

            Debug.Assert(_position >= 0, "_position >= 0");
            return _position;
        }

        // Sets the length of the stream to a given value.  The new
        // value must be nonnegative and less than the space remaining in
        // the array, int.MaxValue - origin
        // Origin is 0 in all cases other than a MemoryStream created on
        // top of an existing array and a specific starting offset was passed
        // into the MemoryStream constructor.  The upper bounds prevents any
        // situations where a stream may be created on top of an array then
        // the stream is made longer than the maximum possible length of the
        // array (int.MaxValue).
        //
        public override void SetLength(long value)
        {
            if (value < 0 || value > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "SR.ArgumentOutOfRange_StreamLength");

            // Origin wasn't publicly exposed above.
            Debug.Assert(MemStreamMaxLength == int.MaxValue);  // Check parameter validation logic in this method if this fails.
            if (value > (int.MaxValue - _origin))
                throw new ArgumentOutOfRangeException(nameof(value), "SR.ArgumentOutOfRange_StreamLength");

            int newLength = _origin + (int)value;
            bool allocatedNewArray = EnsureCapacity(newLength);
            if (!allocatedNewArray && newLength > _length)
                Array.Clear(_buffer, _length, newLength - _length);
            _length = newLength;
            if (_position > newLength)
                _position = newLength;
        }

        public virtual byte[] ToArray()
        {
            int count = _length - _origin;
            if (count == 0)
                return Array.Empty<byte>();
            byte[] copy = new byte[count];
            Buffer.BlockCopy(_buffer, _origin, copy, 0, count);
            return copy;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "SR.ArgumentNull_Buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - offset < count)
                throw new ArgumentException("SR.Argument_InvalidOffLen");

            int i = _position + count;
            // Check for overflow
            if (i < 0)
                throw new IOException("SR.IO_StreamTooLong");

            if (i > _length)
            {
                bool mustZero = _position > _length;
                if (i > _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray)
                    {
                        mustZero = false;
                    }
                }
                if (mustZero)
                {
                    Array.Clear(_buffer, _length, i - _length);
                }
                _length = i;
            }
            if ((count <= 8) && (buffer != _buffer))
            {
                int byteCount = count;
                while (--byteCount >= 0)
                {
                    _buffer[_position + byteCount] = buffer[offset + byteCount];
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            }
            _position = i;
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() != typeof(MemoryStream))
            {
                // MemoryStream is not sealed, and a derived type may have overridden Write(byte[], int, int) prior
                // to this Write(Span<byte>) overload being introduced.  In that case, this Write(Span<byte>) overload
                // should use the behavior of Write(byte[],int,int) overload.
                StreamExt.Write(this, buffer);
                return;
            }

            // Check for overflow
            int i = _position + buffer.Length;
            if (i < 0)
                throw new IOException("SR.IO_StreamTooLong");

            if (i > _length)
            {
                bool mustZero = _position > _length;
                if (i > _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray)
                    {
                        mustZero = false;
                    }
                }
                if (mustZero)
                {
                    Array.Clear(_buffer, _length, i - _length);
                }
                _length = i;
            }

            buffer.CopyTo(new Span<byte>(_buffer, _position, buffer.Length));
            _position = i;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "SR.ArgumentNull_Buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - offset < count)
                throw new ArgumentException("SR.Argument_InvalidOffLen");

            // If cancellation is already requested, bail early
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            try
            {
                Write(buffer, offset, count);
                return Task.CompletedTask;
            }
            catch (OperationCanceledException oce)
            {
                return Task.FromCanceled(oce.CancellationToken);
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        public override void WriteByte(byte value)
        {
            if (_position >= _length)
            {
                int newLength = _position + 1;
                bool mustZero = _position > _length;
                if (newLength >= _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(newLength);
                    if (allocatedNewArray)
                    {
                        mustZero = false;
                    }
                }
                if (mustZero)
                {
                    Array.Clear(_buffer, _length, _position - _length);
                }
                _length = newLength;
            }
            _buffer[_position++] = value;
        }

        // Writes this MemoryStream to another stream.
        public virtual void WriteTo(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), "SR.ArgumentNull_Stream");

            stream.Write(_buffer, _origin, _length - _origin);
        }
    }
#endif
}
