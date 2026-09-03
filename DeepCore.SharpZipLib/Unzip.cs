using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.MPQ;
using DeepCore.MPQ.Updater;
using System;
using System.IO;

namespace DeepCore.SharpZipLib
{
    public class SharpZipLibMPQDriver : MPQUnziper
    {
        public SharpZipLibMPQDriver() 
        {
        }

        public override bool RunUnzipSingle(MPQUpdater updater, MPQUpdater.RemoteFileInfo zip, MPQUpdater.RemoteFileInfo mpq, AtomicLong process)
        {
            return Unzip.SharpZipLib_RunUnzipMPQ(updater, zip, mpq, process);
        }
    }
    public class Unzip
    {
        public static int BUFF_SIZE = 1024 * 1024 * 16;

        public static bool SharpZipLib_DecompressZ(ArraySegment<byte> src, ArraySegment<byte> dst)
        {
            using (var input = new DeepCore.IO.MemoryStream(src.Array, src.Offset, src.Count))
            {
                using (var zstream = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream(input,
                    new ICSharpCode.SharpZipLib.Zip.Compression.Inflater(false), Math.Min(dst.Count, BUFF_SIZE)))
                {
                    IOUtil.ReadToEnd(zstream, dst.Array, dst.Offset, dst.Count);
                }
            }
            return true;
        }

        public static bool SharpZipLib_RunUnzipMPQ(MPQUpdater updater, MPQUpdater.RemoteFileInfo zip_file, MPQUpdater.RemoteFileInfo mpq_file, AtomicLong current_unzip_bytes)
        {
            using (FileStream fis = new FileStream(zip_file.file.FullName, FileMode.Open, FileAccess.Read))
            {
                using (FileStream fos = new FileStream(mpq_file.file.FullName, FileMode.Create, FileAccess.Write))
                {
                    try
                    {
                        if (MPQUpdater.ZIP_EXT.ToLower().EndsWith(".zip"))
                        {
                            using (var zipf = new ICSharpCode.SharpZipLib.Zip.ZipFile(fis))
                            {
                                var e = zipf.GetEnumerator();
                                if (e.MoveNext())
                                {
                                    var ze = (ICSharpCode.SharpZipLib.Zip.ZipEntry)(e.Current);
                                    Stream zipin = zipf.GetInputStream(ze);
                                    if (IOUtil.ReadTo(zipin, fos, mpq_file.size, (int readed) =>
                                    {
                                        current_unzip_bytes += readed;
                                        return !updater.IsDisposing;
                                    }, 1024 * 1024) == false)
                                    { return false; }
                                }
                                zipf.Close();
                            }
                        }
                        else if (MPQUpdater.ZIP_EXT.ToLower().EndsWith(".mgz"))
                        {
                            var gstream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(fis);
                            if (IOUtil.ReadTo(gstream, fos, mpq_file.size, (int readed) =>
                            {
                                current_unzip_bytes += readed;
                                return !updater.IsDisposing;
                            }, 1024 * 1024) == false)
                            { return false; }
                            gstream.Close();
                        }
                        else if (MPQUpdater.ZIP_EXT.ToLower().EndsWith(".z"))
                        {
                            var gstream = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream(fis);
                            if (IOUtil.ReadTo(gstream, fos, mpq_file.size, (int readed) =>
                            {
                                current_unzip_bytes += readed;
                                return !updater.IsDisposing;
                            }, 1024 * 1024) == false)
                            { return false; }
                            gstream.Close();
                        }
                        return true;
                    }
                    finally
                    {
                        fos.Close();
                        fis.Close();
                    }
                }
            }
        }
    }
}
