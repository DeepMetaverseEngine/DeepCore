using DeepCore.Concurrent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace DeepCore.IO
{
    public class Downloader
    {
        public float CurrentBytePerSec { get { return currentBytesPerSec; } }
        public long BytesReaded { get { return total_readed; } }
        public Exception Error { get { return error; } }
        public bool IsDone { get { return done; } }
        public int BytesLimitPerSec { get; set; }

        private readonly Stream input;
        private readonly byte[] io_buffer;

        private float currentBytesPerSec;
        private long total_readed = 0;
        private Exception error = null;
        private bool done = false;
        private bool exit = false;

        private Action<ArraySegment<byte>> event_BytesLoaded;
        public event Action<ArraySegment<byte>> BytesLoaded
        {
            add { event_BytesLoaded += value; }
            remove { event_BytesLoaded -= value; }
        }

        public Downloader(Stream input, int bufferSize = 4096)
        {
            this.input = input;
            this.io_buffer = new byte[bufferSize];
        }
        public void Exit()
        {
            exit = true;
        }

        public void Run()
        {
            try
            {
                int readed = 0;//实际读取字节//
                int expect = BytesLimitPerSec > 0 ? Math.Min(io_buffer.Length, BytesLimitPerSec) : io_buffer.Length;
                float expectBytesPerMS = BytesLimitPerSec / 1000f;//预期每毫秒读取字节//
                float expectBytes;//预期读取字节//
                Stopwatch watch = new Stopwatch();
                while (!exit)
                {
                    watch.Start();
                    try
                    {
                        readed = input.Read(io_buffer, 0, expect);
                        total_readed += readed;
                        if (readed <= 0) return;
                        ProcessBytes(io_buffer, 0, readed);
                        //Console.WriteLine("Read:" + CUtils.ToBytesSizeString(readed) + " BPS=" + currentBytesPerSec);
                        if (BytesLimitPerSec > 0)
                        {
                            expectBytes = expectBytesPerMS * Math.Max(watch.ElapsedMilliseconds, 1);
                            if (readed > expectBytes)
                            {
                                int waittime = (int)((readed - expectBytes) / expectBytesPerMS);
                                //Console.WriteLine("Wait:" + waittime);
                                Thread.Sleep(waittime);
                            }
                        }
                        this.currentBytesPerSec = (readed * 1000f / Math.Max(watch.ElapsedMilliseconds, 1));
                    }
                    finally
                    {
                        watch.Reset();
                    }
                }
            }
            catch (Exception err)
            {
                error = err;
                err.PrintStackTrace();
            }
            finally
            {
                done = true;
            }
        }

        protected virtual void ProcessBytes(byte[] buffer, int offset, int count)
        {
            if (event_BytesLoaded != null)
            {
                event_BytesLoaded.Invoke(new ArraySegment<byte>(buffer, offset, count));
            }
        }

    }


    public class DownloaderStream : Stream
    {
        public float CurrentBytePerSec { get { return downloader.CurrentBytePerSec; } }
        public int CurrentBufferSize { get { return currentBufferSize.Value; } }
        public long BytesReaded { get { return downloader.BytesReaded; } }
        public Exception Error { get { return downloader.Error; } }
        public bool IsDone { get { return downloader.IsDone; } }
        public int BytesLimitPerSec
        {
            get { return downloader.BytesLimitPerSec; }
            set { downloader.BytesLimitPerSec = value; }
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return 0; } }
        public override long Position { get { return 0; } set { } }

        private readonly Stream source;
        private readonly Queue<MemoryStream> buffers = new Queue<MemoryStream>();
        private readonly Downloader downloader;
        private readonly int maxBufferSize;
        private readonly bool leaveOpen;
        private bool disposed = false;
        private bool running = false;
        private AtomicInteger currentBufferSize = new AtomicInteger(0);
        private System.Threading.Thread thread;


        public DownloaderStream(Stream src, int bufferSize = 1024 * 100, bool leaveOpen = true, int trunkSize = 4096)
        {
            this.source = src;
            this.downloader = new Downloader(src, trunkSize);
            this.downloader.BytesLoaded += Downloader_BytesLoaded;
            this.thread = new System.Threading.Thread(Downloader_Run);
            this.thread.Name = "DownloaderStreamTask";
            this.maxBufferSize = bufferSize;
            this.leaveOpen = leaveOpen;
        }
        public void Start()
        {
            running = true;
            thread.Start();
        }
        private void Downloader_Run()
        {
            downloader.Run();
        }
        private void Downloader_BytesLoaded(ArraySegment<byte> obj)
        {
            lock (buffers)
            {
                if (disposed)
                {
                    downloader.Exit();
                    Monitor.PulseAll(buffers);
                    return;
                }
                //申请内存块，存入读缓冲区//
                var buffer = new MemoryStream();
                IOUtil.WriteToEnd(buffer, obj.Array, obj.Offset, obj.Count);
                buffer.Position = 0;
                buffer.SetLength(obj.Count);
                buffers.Enqueue(buffer);
                Monitor.Pulse(buffers);
            }
            currentBufferSize += obj.Count;
            //缓冲区满，暂停下载//
            while (currentBufferSize.Value >= maxBufferSize)
            {
                Thread.Sleep(10);
                lock (buffers)
                {
                    if (disposed)
                    {
                        downloader.Exit();
                        Monitor.PulseAll(buffers);
                        return;
                    }
                }
            }
        }
        public override int Read(byte[] buf, int offset, int count)
        {
            if (!running)
                throw new Exception("Downloader not start !!!");
            int readed = 0;
            lock (buffers)
            {
                while (buffers.Count == 0)
                {
                    if (downloader.IsDone) return 0;
                    if (disposed) return 0;
                    Monitor.Wait(buffers, 10);
                }
                MemoryStream buffer = buffers.Peek();
                if (buffer == null) return 0;
                readed = buffer.Read(buf, offset, count);
                if (buffer.Position == buffer.Length)
                {
                    buffers.Dequeue().Dispose();
                }
            }
            if (readed > 0)
            {
                currentBufferSize -= readed;
            }
            return readed;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            disposed = true;
            downloader.Exit();
            thread.Join();
            lock (buffers)
            {
                while (buffers.Count > 0)
                {
                    var ms = buffers.Dequeue();
                    ms.Dispose();
                }
                Monitor.PulseAll(buffers);
            }
            if (leaveOpen == false)
            {
                source.Close();
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
