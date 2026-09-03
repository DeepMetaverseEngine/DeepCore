using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;

namespace DeepCore.SharpZipLib
{
    public static class ZipUtil
    {
        //-----------------------------------------------------------------------------------
        public class OpenStream : Stream
        {
            private readonly ZipFile zfile;
            private readonly ZipEntry zentry;
            private readonly Stream etream;
            private long pos = 0;
            public OpenStream(ZipFile zfile, ZipEntry zentry)
            {
                this.zfile = zfile;
                this.zentry = zentry;
                this.etream = zfile.GetInputStream(zentry);
            }
            public string EntryName { get => zentry.Name; }
            public override bool CanRead => etream.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => zentry.Size;
            public override long Position { get => pos; set => new NotSupportedException(); }
            public override void Flush()
            {
                throw new NotSupportedException();
            }
            public override int Read(byte[] buffer, int offset, int count)
            {
                var readed = etream.Read(buffer, offset, count);
                pos += readed;
                return readed;
            }
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }
            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    zfile.Close();
                }
            }
        }
        //-----------------------------------------------------------------------------------
        public static ZipFile CreateZipFile(string path)
        {
            ZipFile zipfile = null;
            if (Directory.Exists(path))
            {
                zipfile = new ZipFile(path);
            }
            else
            {
                zipfile = ZipFile.Create(path);
            }
            return zipfile;
        }

        //-----------------------------------------------------------------------------------
        public static OpenStream LoadZipFirstEntry(string file)
        {
            var zfile = new ZipFile(file);
            foreach (ZipEntry e in zfile)
            {
                return new OpenStream(zfile, e);
            }
            zfile.Close();
            return null;
        }
        public static OpenStream LoadZipFirstEntry(byte[] bytes)
        {
            var zfile = new ZipFile(new MemoryStream(bytes));
            foreach (ZipEntry e in zfile)
            {
                return new OpenStream(zfile, e);
            }
            zfile.Close();
            return null;
        }
        //-----------------------------------------------------------------------------------
        public static OpenStream LoadZipEntry(string file, Predicate<ZipEntry> select)
        {
            var zfile = new ZipFile(file);
            foreach (ZipEntry e in zfile)
            {
                if (select(e))
                {
                    return new OpenStream(zfile, e);
                }
            }
            zfile.Close();
            return null;
        }
        public static OpenStream LoadZipEntry(byte[] bytes, Predicate<ZipEntry> select)
        {
            var zfile = new ZipFile(new MemoryStream(bytes));
            foreach (ZipEntry e in zfile)
            {
                if (select(e))
                {
                    return new OpenStream(zfile, e);
                }
            }
            zfile.Close();
            return null;
        }
        //-----------------------------------------------------------------------------------
        public static bool ExistEntry(string file, Predicate<ZipEntry> select)
        {
            using (var zfile = new ZipFile(file))
            {
                foreach (ZipEntry e in zfile)
                {
                    if (select(e))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool ExistEntry(byte[] bytes, Predicate<ZipEntry> select)
        {
            using (var zfile = new ZipFile(new MemoryStream(bytes)))
            {
                foreach (ZipEntry e in zfile)
                {
                    if (select(e))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        //-----------------------------------------------------------------------------------
        public static bool ForEachZipEntry(string file, BreakPredicate<ZipEntry, OpenStream> select)
        {
            var zfile = new ZipFile(file);
            try
            {
                foreach (ZipEntry e in zfile)
                {
                    if (select(e, new OpenStream(zfile, e)))
                    {
                        return true;
                    }
                }
            }
            finally
            {
                zfile.Close();
            }
            return false;
        }
        public static bool ForEachZipEntry(byte[] bytes, BreakPredicate<ZipEntry, OpenStream> select)
        {
            var zfile = new ZipFile(new MemoryStream(bytes));
            try
            {
                foreach (ZipEntry e in zfile)
                {
                    if (select(e, new OpenStream(zfile, e)))
                    {
                        return true;
                    }
                }
            }
            finally
            {
                zfile.Close();
            }
            return false;
        }
        //-----------------------------------------------------------------------------------
        public static void Main(string[] args)
        {
            // Perform some simple parameter checking.  More could be done
            // like checking the target file name is ok, disk space, and lots
            // of other things, but for a demo this covers some obvious traps.
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: CreateZipFile Path ZipFile");
                return;
            }

            if (!Directory.Exists(args[0]))
            {
                Console.WriteLine("Cannot find directory '{0}'", args[0]);
                return;
            }

            try
            {
                // Depending on the directory this could be very large and would require more attention
                // in a commercial package.
                string[] filenames = Directory.GetFiles(args[0]);

                // 'using' statements guarantee the stream is closed properly which is a big source
                // of problems otherwise.  Its exception safe as well which is great.
                using (var s = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream(File.Create(args[1])))
                {

                    s.SetLevel(9); // 0 - store only to 9 - means best compression

                    byte[] buffer = new byte[4096];

                    foreach (string file in filenames)
                    {

                        // Using GetFileName makes the result compatible with XP
                        // as the resulting path is not absolute.
                        var entry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(Path.GetFileName(file));

                        // Setup the entry data as required.

                        // Crc and size are handled by the library for seakable streams
                        // so no need to do them here.

                        // Could also use the last write time or similar for the file.
                        entry.DateTime = DateTime.Now;
                        s.PutNextEntry(entry);

                        using (FileStream fs = File.OpenRead(file))
                        {

                            // Using a fixed size buffer here makes no noticeable difference for output
                            // but keeps a lid on memory usage.
                            int sourceBytes;
                            do
                            {
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                s.Write(buffer, 0, sourceBytes);
                            } while (sourceBytes > 0);
                        }
                    }

                    // Finish/Close arent needed strictly as the using statement does this automatically

                    // Finish is important to ensure trailing information for a Zip file is appended.  Without this
                    // the created file would be invalid.
                    s.Finish();

                    // Close is important to wrap things up and unlock the file.
                    s.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception during processing {0}", ex);

                // No need to rethrow the exception as for our purposes its handled.
            }
        }
    }
}
