using DeepCore.IO;
using DeepCore.Log;
using DeepCore.MPQ.Updater;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore.MPQ
{
    public class MPQFileSystem : Disposable
    {
        private Dictionary<string, MPQFileEntry> indexer = new Dictionary<string, MPQFileEntry>();
        private Dictionary<string, MPQStream> mpq_files = new Dictionary<string, MPQStream>();
        private MPQDirectoryInfo dir_root = new MPQDirectoryInfo(string.Empty, null);
        public static int ConcurrentCount = System.Environment.ProcessorCount - 1;
        public static char DirectorySeparatorChar = '/';
        protected Logger log = LoggerFactory.GetLogger("MPQFS");

        public MPQFileSystem()
        {
            base.AsSynchronizedDisposing();
        }
        protected override void Disposing()
        {
            indexer.Clear();
            foreach (MPQStream path in mpq_files.Values)
            {
                path.Dispose();
            }
            mpq_files.Clear();
        }
        //----------------------------------------------------------------------------------------------------------------------
        #region Init 
        /// <summary>
        /// 搜索并加载目录里的所有MPQ文件
        /// </summary>
        /// <param name="mpq_dir"></param>
        /// <returns></returns>
        public bool Init(DirectoryInfo mpq_dir)
        {
            if (mpq_dir.Exists)
            {
                var dir = LoadDir(new FileInfo(mpq_dir.FullName + Path.DirectorySeparatorChar + ".dir"));
                foreach (FileInfo file in mpq_dir.GetFiles())
                {
                    if (file.Extension.ToLower().EndsWith(MPQ.Updater.MPQUpdater.MPQ_EXT))
                    {
                        if (LoadMPQ(file, dir) == false)
                        {
                            return false;
                        }
                    }
                }
                foreach (DirectoryInfo sub_dir in mpq_dir.GetDirectories())
                {
                    if (!Init(sub_dir))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 将自动更新的MPQ加载到文件系统
        /// </summary>
        /// <param name="updater"></param>
        /// <returns></returns>
        public bool Init(MPQUpdater updater)
        {
            var dir = LoadDir(new FileInfo(updater.LocalSaveRoot.FullName + Path.DirectorySeparatorChar + ".dir"));
            foreach (FileInfo rmf in updater.GetAllFiles())
            {
                if (rmf.FullName.ToLower().EndsWith(MPQUpdater.MPQ_EXT))
                {
                    if (LoadMPQ(rmf, dir) == false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private HashMap<string, string> LoadDir(FileInfo dir_file)
        {
            var dir = new HashMap<string, string>();
            if (dir_file.Exists)
            {
                string[] lines = File.ReadAllLines(dir_file.FullName);
                foreach (var line in lines)
                {
                    dir.Add(line.Trim(), line);
                }
            }
            return dir;
        }
        private bool LoadMPQ(FileInfo fileinfo, HashMap<string, string> dir)
        {
            List<MPQFileEntry> entries = new List<MPQFileEntry>(100);
            MPQStream mpq_stream = new MPQStream(fileinfo);
            if (mpq_stream.initEntrys(entries))
            {
                mpq_files[fileinfo.FullName] = mpq_stream;
                foreach (MPQFileEntry e in entries)
                {
                    if (dir == null || dir.Count == 0 || dir.ContainsKey(e.Key))
                    {
                        putEntry(e);
                    }
                    else
                    {
                        //目录里已经不存在但是老的MPQ里存在Entry//
                        log.Warn(string.Format("MPQFileSystem : 目录里已经不存在但是老的MPQ里存在: ignore entry \"{0}\" not exist in .dir file!!!", e.Key));
                    }
                }
                return true;
            }
            else
            {
                mpq_stream.Dispose();
                throw new Exception("Cannot Init MPQ file : " + fileinfo.FullName);
            }
        }
        public bool LoadMPQ(FileInfo fileinfo)
        {
            return LoadMPQ(fileinfo, null);
        }
        public List<MPQFileEntry> LoadEntries(FileInfo fileinfo)
        {
            List<MPQFileEntry> entries = new List<MPQFileEntry>(100);
            using (MPQStream mpq_stream = new MPQStream(fileinfo))
            {
                if (mpq_stream.initEntrys(entries))
                {
                    return entries;
                }
            }
            throw new Exception("Cannot Init MPQ file : " + fileinfo.FullName);
        }
        //         virtual protected long hashCode(string data)
        //         {
        //             return data.GetHashCode();
        //         }

        private bool putEntry(MPQFileEntry re)
        {
            //             re.hash = hashCode(re.key);
            //             // 首个HASH
            //             if (!indexer.TryGetValue(re.hash, out var ets))
            //             {
            //                 ets = new Dictionary<string, MPQFileEntry>(1);
            //                 ets[re.key] = re;
            //                 indexer[re.hash] = ets;
            //                 dir_root.AddEntryDir(re);
            //                 return true;
            //             }

            // 首个文件
            if (!indexer.TryGetValue(re.key, out var exist))
            {
                indexer[re.key] = re;
                dir_root.AddEntryDir(re);
                return true;
            }
            // 如果当前文件较新，则更新
            if (exist.f_date < re.f_date)
            {
                indexer[re.key] = re;
                dir_root.AddEntryDir(re);
                return true;
            }
            // 当前文件较老，忽略
            return false;
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------
#if false
        #region Obsolete
        [Obsolete] public bool init(DirectoryInfo mpq_dir) { return this.Init(mpq_dir); }
        [Obsolete] public bool init(MPQUpdater updater) { return this.Init(updater); }
        [Obsolete] public MPQFileEntry findEntry(string path) { return this.FindEntry(path); }
        [Obsolete] public byte[] getEntryData(MPQFileEntry e) { return this.GetEntryData(e); }
        [Obsolete] public byte[] getData(String name) { return this.GetData(name); }
        [Obsolete] public void getEntryDataAsync(MPQFileEntry e, Action<byte[], Exception> cb) { this.GetEntryDataAsync(e, cb); }
        [Obsolete] public void getDataAsync(String name, Action<byte[], Exception> cb) { this.GetDataAsync(name, cb); }
        [Obsolete] public Task<byte[]> getEntryDataAsync(MPQFileEntry e) { return this.GetEntryDataAsync(e); }
        [Obsolete] public Task<byte[]> getDataAsync(String name) { return this.GetDataAsync(name); }
        [Obsolete] public Stream openEntryStream(MPQFileEntry e) { return this.OpenEntryStream(e); }
        [Obsolete] public Stream openStream(String name) { return this.OpenStream(name); }
        #endregion
#endif
        //----------------------------------------------------------------------------------------------------------------------
        #region Runtime Load

        public MPQFileEntry FindEntry(string path)
        {
            path = Resource.FormatPath(path);
            if (!path.StartsWith(DirectorySeparatorChar))
            {
                path = DirectorySeparatorChar + path;
            }
            if (indexer.TryGetValue(path, out var ret))
            {
                return ret;
            }
            return null;
        }

        public byte[] GetEntryData(MPQFileEntry e)
        {
            if (e != null)
            {
                var data = new byte[e.f_size];
                e.fs.ReadAll(e, data, 0, data.Length);
                return data;
            }
            return null;
        }
        public byte[] GetData(String name)
        {
            var e = FindEntry(name);
            var ed = GetEntryData(e);
            return ed;
        }
        public void GetEntryDataAsync(MPQFileEntry e, Action<byte[], Exception> cb)
        {
            if (e != null)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        byte[] data = new byte[e.f_size];
                        e.fs.ReadAll(e, data, 0, data.Length);
                        cb(data, null);
                    }
                    catch (Exception err)
                    {
                        cb(null, err);
                    }
                });
            }
            else
            {
                cb(null, null);
            }
        }
        public void GetDataAsync(String name, Action<byte[], Exception> cb)
        {
            var e = FindEntry(name);
            GetEntryDataAsync(e, cb);
        }
        public Task<byte[]> GetEntryDataAsync(MPQFileEntry e)
        {
            if (e != null)
            {
                return Task.Run(() =>
                {
                    byte[] data = new byte[e.f_size];
                    e.fs.ReadAll(e, data, 0, data.Length);
                    return data;
                });
            }
            else
            {
                return Task.FromResult<byte[]>(null);
            }
        }
        public Task<byte[]> GetDataAsync(String name)
        {
            var e = FindEntry(name);
            return GetEntryDataAsync(e);
        }

        public Stream OpenEntryStream(MPQFileEntry e)
        {
            if (e != null)
            {
                return e.fs.OpenStream(e);
            }
            return null;
        }
        public Stream OpenStream(String name)
        {
            MPQFileEntry e = FindEntry(name);
            Stream ed = OpenEntryStream(e);
            return ed;
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------
        #region Entry Dir

        public List<MPQFileEntry> ListEntrys()
        {
            List<MPQFileEntry> ret = new List<MPQFileEntry>();
            foreach (MPQFileEntry e in indexer.Values)
            {
                ret.Add(e);
            }
            return ret;
        }

        public MPQDirectoryInfo RootDirectory
        {
            get => dir_root;
        }

        public MPQDirectoryInfo GetDirectory(string fullPath)
        {
            var path = fullPath.Split(DirectorySeparatorChar);
            var parent = dir_root;
            for (int i = 1; i < path.Length; i++)
            {
                if (parent == null)
                {
                    return null;
                }
                if (path[i].Length == 0)
                {
                    return parent;
                }
                parent = parent.GetDirectory(path[i]);
            }
            return parent;
        }
        public abstract class MPQPathNode
        {
            public MPQDirectoryInfo Parent { get; }
            public string Name { get; }
            public bool IsRoot { get => Parent == null; }
            public string FullPath
            {
                get
                {
                    var txt = DirectorySeparatorChar + this.Name;
                    var parent = this.Parent;
                    while (parent != null && !parent.IsRoot)
                    {
                        txt = DirectorySeparatorChar + parent.Name + txt;
                        parent = parent.Parent;
                    }
                    return txt;
                }
            }
            public MPQPathNode(string name, MPQDirectoryInfo parent)
            {
                this.Parent = parent;
                this.Name = name;
            }
        }
        public class MPQDirectoryInfo : MPQPathNode
        {
            internal HashMap<string, MPQFileInfo> files = new HashMap<string, MPQFileInfo>();
            internal HashMap<string, MPQDirectoryInfo> dirs = new HashMap<string, MPQDirectoryInfo>();
            public MPQDirectoryInfo(string name, MPQDirectoryInfo parent) : base(name, parent)
            {
            }
            internal MPQFileInfo AddEntryDir(MPQFileEntry e)
            {
                var parent = this;
                var path = e.key.Split(DirectorySeparatorChar);
                var fname = path[path.Length - 1];
                for (int i = 1; i < path.Length - 1; i++)
                {
                    parent = parent.dirs.GetOrAdd(path[i], dname => new MPQDirectoryInfo(dname, parent));
                }
                var ret = new MPQFileInfo(e, fname, parent);
                parent.files.Put(fname, ret);
                return ret;
            }
            public MPQFileInfo GetFile(string name)
            {
                return files.Get(name);
            }
            public MPQFileInfo[] GetFiles()
            {
                return files.Values.ToArray();
            }
            public MPQDirectoryInfo GetDirectory(string name)
            {
                if (name == ".") return this;
                if (name == "..") return Parent;
                return dirs.Get(name);
            }
            public MPQDirectoryInfo[] GetDirectories()
            {
                return dirs.Values.ToArray();
            }
        }

        public class MPQFileInfo : MPQPathNode
        {
            public MPQFileEntry Entry { get; }
            public MPQFileInfo(MPQFileEntry e, string name, MPQDirectoryInfo parent) : base(name, parent)
            {
                this.Entry = e;
            }
        }


        #endregion
        //----------------------------------------------------------------------------------------------------------------------
        #region StreamWrapper

        internal class MPQStream : Disposable
        {
            public static byte[] FS_HEAD_START = { (byte)'M', (byte)'F', (byte)'F', (byte)'S' };
            public static byte[] FS_ENTRY_START = { (byte)'M', (byte)'F', (byte)'E', (byte)'T' };
            public static byte[] FS_TRUNK_START = { (byte)'M', (byte)'F', (byte)'T', (byte)'K' };
            public static byte[] FS_END = { (byte)'M', (byte)'F', (byte)'E', (byte)'D' };
            public static byte[] VERSION = { 2, 0, 0, 1 };

            private FileInfo info;
            private readonly int stackCount = Math.Max(1, ConcurrentCount);
            private System.Threading.SemaphoreSlim indexer;
            private List<FileStream> allocate = new List<FileStream>();
            private long trunk_start;
            private Stack<FileStream> stack;
            private FileStream sharedFileStream;
            public long TrunkStart
            {
                get { return trunk_start; }
            }

            public FileInfo MPQFile
            {
                get { return info; }
            }



            private struct AutoReleaseLock : IDisposable
            {
                private readonly object _locker;
                public AutoReleaseLock(object locker)
                {
                    _locker = locker;
                    Monitor.Enter(_locker);
                }
                public void Dispose()
                {
                    Monitor.Exit(_locker);
                }
            }



            public MPQStream(FileInfo file)
            {
                this.AsSynchronizedDisposing();
                this.info = file;
                this.indexer = new SemaphoreSlim(stackCount, stackCount);
                this.stack = new Stack<FileStream>(stackCount);
                for (int i = 0; i < stackCount; i++)
                {
                    var fs = AllocFileStream();
                    this.stack.Push(fs);
                }
                this.sharedFileStream = AllocFileStream();
            }
            private FileStream AllocFileStream()
            {
                var fs = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                this.allocate.Add(fs);
                return fs;
            }
            protected override void Disposing()
            {
                try
                {
                    while (indexer.CurrentCount < stackCount)
                    {
                        Thread.Sleep(1);
                    }
                    indexer.Dispose();
                    lock (stack) stack.Clear();
                    foreach (var fis in allocate)
                    {
                        try
                        {
                            fis.Close();
                            fis.Dispose();
                        }
                        catch (Exception err)
                        {
                            err.PrintStackTrace();
                        }
                    }
                    allocate.Clear();
                    sharedFileStream = null;
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                }
            }
            private IDisposable TryTakeSharedStream(out FileStream stream)
            {
                stream = sharedFileStream;
                return new AutoReleaseLock(sharedFileStream);
            }



            internal bool initEntrys(List<MPQFileEntry> entries)
            {
                var fis = this.allocate[0];
                fis.Position = 0;
                using (BinaryReader bis = new BinaryReader(fis, Encoding.UTF8, true))
                {
                    byte[] head_trunk = IOUtil.ReadExpect(fis, MPQStream.FS_HEAD_START.Length); // head
                    if (headEquals(head_trunk, MPQStream.FS_HEAD_START))
                    {
                        head_trunk = IOUtil.ReadExpect(fis, MPQStream.VERSION.Length);// version
                        long total_size = bis.ReadInt64();
                        head_trunk = IOUtil.ReadExpect(fis, MPQStream.FS_ENTRY_START.Length);// entry start
                        if (headEquals(head_trunk, MPQStream.FS_ENTRY_START))
                        {
                            int entry_count = bis.ReadInt32();
                            for (int i = 0; i < entry_count; i++)
                            {
                                MPQFileEntry re = new MPQFileEntry(this, bis);
                                entries.Add(re);
                            }
                            head_trunk = IOUtil.ReadExpect(fis, MPQStream.FS_TRUNK_START.Length);// trunk start
                            if (headEquals(head_trunk, MPQStream.FS_TRUNK_START))
                            {
                                // record file trunk start
                                this.trunk_start = fis.Position;
                                return true;
                            }
                        }
                    }
                }
                return false;
                bool headEquals(byte[] a, byte[] b)
                {
                    for (int i = 0; i < a.Length; i++)
                    {
                        if (a[i] != b[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            private FileStream popFileStream(MPQFileEntry src)
            {
                indexer.Wait();
                lock (stack)
                {
                    var fis = this.stack.Pop();
                    fis.Position = this.trunk_start + src.f_start;
                    return fis;
                }
            }

            private void pushFileStream(FileStream obj)
            {
                lock (stack)
                {
                    this.stack.Push(obj);
                }
                indexer.Release();
            }
            internal int ReadAll(MPQFileEntry src, byte[] dst, int dst_pos, int length)
            {
                if (src.fs == this)
                {
                    var fis = popFileStream(src);
                    try
                    {
                        IOUtil.ReadToEnd(fis, dst, dst_pos, length);
                    }
                    catch (Exception err)
                    {
                        throw new Exception("MPQStream read error : " + err.Message, err);
                    }
                    finally
                    {
                        pushFileStream(fis);
                    }
                    return length;
                }
                throw new Exception("MPQStream read error");
            }
            internal EntryStream OpenStream(MPQFileEntry src)
            {
                if (src.fs == this)
                {
                    return new EntryStream(src);
                }
                throw new Exception("MPQStream read error");
            }

            /// <summary>
            /// 外部读取用流
            /// </summary>
            internal class EntryStream : Stream
            {
                private long pos = 0;
                private MPQFileEntry entry;
                public EntryStream(MPQFileEntry e)
                {
                    this.entry = e;
                }
                public override long Position
                {
                    get { return pos; }
                    set { pos = value; }
                }
                public override long Length
                {
                    get { return entry.Size; }
                }
                public override bool CanRead
                {
                    get { return true; }
                }
                public override bool CanSeek
                {
                    get { return true; }
                }
                public override bool CanWrite
                {
                    get { return false; }
                }
                protected override void Dispose(bool disposing)
                {
                    entry = null;
                    base.Dispose(disposing);
                }
                public override int Read(byte[] buffer, int offset, int count)
                {
                    if (entry == null)
                    {
                        throw new ObjectDisposedException(nameof(EntryStream));
                    }
                    using (entry.fs.TryTakeSharedStream(out var stream))
                    {
                        stream.Position = entry.fs.trunk_start + this.entry.f_start + pos;
                        long avaliable = entry.Size - pos;
                        //                     if (count > avaliable)
                        //                     {
                        //                         throw new IOException($"EOF of MPQEntry : file={entry.key} avaliable={avaliable} count={count}");
                        //                     }
                        if (pos < 0 || pos > entry.Size)
                        {
                            throw new IOException($"EOF Position Out Of Range : file={entry.key} pos={pos}");
                        }
                        if (avaliable > 0)
                        {
                            count = (int)Math.Min(avaliable, count);
                            int readed = stream.Read(buffer, offset, count);
                            pos += readed;
                            return readed;
                        }
                        else if (avaliable == 0)
                        {
                            return 0;
                        }
                    }
                    throw new IOException("EOF of MPQEntry");
                }
                /*
                 * 如果 offset 为负，则要求新位置位于 origin 指定的位置之前，其间隔相差 offset 指定的字节数。如果 offset 为零 (0)，则要求新位置位于由 origin 指定的位置处。
                 * 如果 offset 为正，则要求新位置位于 origin 指定的位置之后，其间隔相差 offset 指定的字节数.
                 * Stream. Seek(-3,Origin.End);  表示在流末端往前数第3个位置
                 * Stream. Seek(0,Origin.Begin); 表示在流的开头位置
                 * Stream. Seek(3,Orig`in.Current); 表示在流的当前位置往后数第三个位置
                 * 查找之后会返回一个流中的一个新位置。其实说道这大家就能理解Seek方法的精妙之处了吧
                 */
                public override long Seek(long offset, SeekOrigin origin)
                {
                    switch (origin)
                    {
                        case SeekOrigin.Begin:
                            pos = 0 + offset;
                            break;
                        case SeekOrigin.Current:
                            pos = pos + offset;
                            break;
                        case SeekOrigin.End:
                            pos = entry.Size + offset;
                            break;
                    }
                    return pos;
                }
                public override void SetLength(long value) { throw new NotImplementedException(); }
                public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
                public override void Flush() { }
            }
        }

        public class MPQFileEntry
        {
            internal static DateTime JAVA_START_DATE = new DateTime(1970, 1, 1, 0, 0, 0);

            internal long hash;         // 文件名HASH
                                        //internal int index;		    // HASH对应所在Entry位置
            internal String key;            // 文件名
                                            //internal int key_size;	    // 文件名长度
                                            //internal String key_md5;	// 文件名MD5
            internal long f_start;           // 文件内容开始位置
            internal int f_size;            // 文件内容尺寸
            internal long f_date;           // 文件日期（1970-1-1起始秒） 
                                            //internal String f_md5;		// 文件内容MD5

            internal readonly MPQFileSystem.MPQStream fs;

            internal MPQFileEntry(MPQFileSystem.MPQStream fs, BinaryReader bis)
            {
                this.fs = fs;
                /*
                 * hash 		= LittleIODeserialize.getLong	(is);
                 * index	 	= LittleIODeserialize.getInt	(is);
                 * key			= LittleIODeserialize.getString	(is, "UTF-8");
                 * key_size 	= LittleIODeserialize.getInt	(is);
                 * key_md5 	    = LittleIODeserialize.getString	(is, "UTF-8");
                 * f_start 	    = LittleIODeserialize.getInt	(is);
                 * f_size 		= LittleIODeserialize.getInt	(is);
                 * f_date 		= LittleIODeserialize.getLong	(is);
                 * f_md5 		= LittleIODeserialize.getString	(is, "UTF-8");
                 */
                this.hash = bis.ReadInt64();
                var index = bis.ReadInt32();
                this.key = readUTF(bis);
                var ksize = bis.ReadInt32();
                var kmd5 = readUTF(bis);
                this.f_start = bis.ReadInt64();
                this.f_size = bis.ReadInt32();
                this.f_date = bis.ReadInt64();
                var f_md5 = readUTF(bis);
            }

            public string Key
            {
                get { return key; }
            }
            public int Size
            {
                get { return f_size; }
            }
            public DateTime Date
            {
                get { return JAVA_START_DATE.AddSeconds(f_date); }
            }

            public override string ToString()
            {
                return key + "(" + f_size + ")";
            }
            [Obsolete] public byte[] getFileData() { return GetFileData(); }
            public byte[] GetFileData()
            {
                byte[] data = new byte[this.f_size];
                this.fs.ReadAll(this, data, 0, data.Length);
                return data;
            }

            private static string readUTF(BinaryReader bis)
            {
                int len = bis.ReadUInt16();
                byte[] bytes = new byte[len];
                int readed = bis.Read(bytes, 0, len);
                while (readed < len)
                {
                    readed += bis.Read(bytes, readed, len - readed);
                }
                return Encoding.UTF8.GetString(bytes, 0, len);
            }

            public bool Equals(MPQFileEntry b)
            {
                if (b == null) return false;
                if (!b.key.Equals(this.key)) return false;
                if (!b.f_size.Equals(this.f_size)) return false;
                if (!b.f_date.Equals(this.f_date)) return false;
                if (!b.f_start.Equals(this.f_start)) return false;
                return true;
            }

        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------
    }

}
