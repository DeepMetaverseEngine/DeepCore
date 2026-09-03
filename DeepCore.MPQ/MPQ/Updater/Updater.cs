using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Threading;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace DeepCore.MPQ.Updater
{
    public class MPQUpdater : Disposable
    {
        private readonly Logger log = new LazyLogger("MPQUpdater");
        public static string ZIP_EXT = ".z";
        public static string MPQ_EXT = ".mpq";
        public static string VERSION_TEXT_BEGIN = "BEGIN";
        public static string VERSION_TEXT_END = "END";

        protected static readonly char Separator = Path.DirectorySeparatorChar;

        protected readonly MPQDriver driver;

        protected Uri version_url;
        protected FileInfo version_file;
        private string[] url_roots;

        protected string version_text = "";

        protected DirectoryInfo save_root;
        protected DirectoryInfo bundle_root;
        protected MessageActionQueue<MPQUpdater> events;
        protected AtomicReference<Status> status = new AtomicReference<Status>(Status.NA);

        protected List<RemoteFileInfo> remoteFiles = new List<RemoteFileInfo>();
        protected HashMap<string, RemoteFileInfo> bundleFiles = new HashMap<string, RemoteFileInfo>();

        protected long total_download_bytes;
        protected AtomicLong current_download_bytes = new AtomicLong(0);
        protected string current_download_file;
        protected string current_error_msg;

        protected long total_unzip_bytes;
        protected AtomicLong current_unzip_bytes = new AtomicLong(0);
        protected string current_unzip_file;

        protected bool is_check_md5 = false;
        protected bool is_running = false;
        protected bool is_exit = false;
        protected Thread workthread;
        protected bool is_done = false;

        protected long last_update_time;
        protected long last_update_download_bytes = 0;
        protected long last_update_unzip_bytes = 0;
        protected long current_download_speed_BPS = 0;
        protected long current_unzip_spped_BPS = 0;


        public DirectoryInfo LocalSaveRoot { get { return save_root; } }
        public bool DoNotUnzip { get; set; }
        public bool DoNotDownloadZip { get; set; }


        public delegate bool CheckVaildHandler(RemoteFileInfo info);
        public delegate void EventHandler(MPQUpdater sender, MPQUpdaterEvent e);
        protected CheckVaildHandler event_OnCheckVaild;
        protected EventHandler event_OnEvent;
        public event CheckVaildHandler OnCheckVaild { add { event_OnCheckVaild += value; } remove { event_OnCheckVaild -= value; } }
        public event EventHandler OnEvent { add { event_OnEvent += value; } remove { event_OnEvent -= value; } }
        public event Action Completed;

        /// <summary>
        /// 创建自动更新程序
        /// </summary>
        /// <param name="remote_version_prefix">远程下载地址根目录（多个备选）</param>
        /// <param name="remote_version_url">下载资源类型后缀</param>
        /// <param name="local_save_root">本地存储目录</param>
        /// <param name="local_bundle_root">本地包内资源目录</param>
        /// <param name="validate_md5">是否验证MD5</param>
        public MPQUpdater(
            string[] remote_version_prefix,
            Uri remote_version_url,
            DirectoryInfo local_save_root,
            DirectoryInfo local_bundle_root,
            bool validate_md5)
        {
            if (local_save_root.FullName == local_bundle_root.FullName) throw new Exception($"包内MPQ目录和下载MPQ目录，不能是同一个：{local_save_root.FullName}");
            this.events = new MessageActionQueue<MPQUpdater>();
            this.DoNotUnzip = false;
            this.DoNotDownloadZip = false;
            this.DownloadTimeoutSEC = 60;
            if (remote_version_prefix.Length == 0)
            {
                throw new Exception("remote_version_prefix length is 0 !!!");
            }
            if (local_save_root.FullName.Equals(local_bundle_root.FullName))
            {
                throw new Exception("save root cannot be bundle root !!!");
            }
            if (!local_save_root.Exists)
            {
                local_save_root.Create();
            }
            var version_suffix = "update_version.txt";
            this.save_root = local_save_root;// new DirectoryInfo(Path.GetFullPath(local_save_root));
            this.bundle_root = local_bundle_root;// new DirectoryInfo(Path.GetFullPath(local_bundle_root));
            this.version_file = new FileInfo(Path.GetFullPath(local_save_root.FullName + Separator + version_suffix));
            this.url_roots = remote_version_prefix;
            this.version_url = remote_version_url;
            this.is_check_md5 = validate_md5;
            this.driver = MPQDriverFactory.CreateDriver(local_save_root, local_bundle_root);
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 检测有无资源需要下载
        /// </summary>
        /// <param name="remote_version_url">远程列表文件地址(http://localhost/xxxx/version.txt)</param>
        /// <param name="version_suffix">下载资源类型后缀</param>
        /// <param name="local_save_root">本地存储目录</param>
        /// <returns></returns>
        public static bool CheckNeedUpdate(
            Uri remote_version_url,
            DirectoryInfo local_save_root)
        {
            var version_suffix = "update_version.txt";
            using (var driver = MPQDriverFactory.CreateDownloader(remote_version_url))
            {
                FileInfo version_file = new FileInfo(Path.GetFullPath(local_save_root.FullName + Separator + version_suffix));
                string remote_text = driver.DownloadString(remote_version_url).Trim();
                if (remote_text.StartsWith(VERSION_TEXT_BEGIN) && remote_text.EndsWith(VERSION_TEXT_END))
                {
                    string local_text = File.ReadAllText(version_file.FullName, CUtils.UTF8);
                    if (string.Equals(remote_text, local_text))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 检测需要下载多少资源
        /// </summary>
        /// <param name="remote_version_url">远程列表文件地址(http://localhost/xxxx/version.txt)</param>
        /// <param name="version_suffix">下载资源类型后缀</param>
        /// <param name="local_save_root">本地存储目录</param>
        /// <param name="local_bundle_root">本地包内资源目录</param>
        /// <returns></returns>
        public static UpdateInfo CheckNeedUpdate(
            Uri remote_version_url,
            DirectoryInfo local_save_root,
            DirectoryInfo local_bundle_root)
        {
            UpdateInfo update = new UpdateInfo(
                 remote_version_url,
                 local_save_root,
                 local_bundle_root);
            update.run();
            return update;
        }

        /// <summary>
        /// 检测需要下载多少资源
        /// </summary>
        /// <param name="remote_version_url">远程列表文件地址(http://localhost/xxxx/version.txt)</param>
        /// <param name="version_suffix">下载资源类型后缀</param>
        /// <param name="local_save_root">本地存储目录</param>
        /// <param name="local_bundle_root">本地包内资源目录</param>
        /// <param name="beforeCheck">资源校验前资源检索</param>
        /// <param name="checkVaild">资源检验</param>
        /// <returns></returns>
        public static UpdateInfo CheckNeedUpdate(
            Uri remote_version_url,
            DirectoryInfo local_save_root,
            DirectoryInfo local_bundle_root,
            Action<List<RemoteFileInfo>> beforeCheck,
            Predicate<RemoteFileInfo> checkVaild)
        {
            UpdateInfo update = new UpdateInfo(
                 remote_version_url,
                 local_save_root,
                 local_bundle_root,
                 beforeCheck,
                 checkVaild);
            update.run();
            return update;
        }



        //--------------------------------------------------------------------------------------------------------------------------------------------


        /**
         * 开始自动更新 
         */
        public void Start()
        {
            if (this.IsRunning)
            {
                return;
            }
            this.is_running = true;
            this.workthread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    lock (remoteFiles)
                    {
                        this.remoteFiles.Clear();
                        this.bundleFiles.Clear();
                    }
                    new RunTask(this).Run((list) =>
                    {
                        lock (remoteFiles)
                        {
                            foreach (RemoteFileInfo rf in list)
                            {
                                if (rf.file.Exists)
                                {
                                    this.remoteFiles.Add(rf);
                                }
                            }
                        }
                    });
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
                finally
                {
                    Completed?.Invoke();
                }
            }));
            this.workthread.Name = "MPQUpdater";
            this.workthread.Start();
        }
        public Task<MPQUpdater> RunAsync()
        {
            if (this.IsRunning)
            {
                return Task.FromResult(this);
            }
            var tcs = new TaskCompletionSource<MPQUpdater>();
            this.is_running = true;
            this.workthread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    lock (remoteFiles)
                    {
                        this.remoteFiles.Clear();
                        this.bundleFiles.Clear();
                    }
                    new RunTask(this).Run((list) =>
                    {
                        lock (remoteFiles)
                        {
                            foreach (RemoteFileInfo rf in list)
                            {
                                if (rf.file.Exists)
                                {
                                    this.remoteFiles.Add(rf);
                                }
                            }
                        }
                        tcs.TrySetResult(this);
                    });
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                    tcs.TrySetException(err);
                }
                finally
                {
                    Completed?.Invoke();
                }
            }));
            this.workthread.Name = "MPQUpdater";
            this.workthread.Start();
            return tcs.Task;
        }

        public bool Update()
        {
            long now_time = Environment.TickCount;
            if (Math.Abs(now_time - last_update_time) >= 1000)
            {
                double delta_time = now_time - last_update_time;
                double delta_download = current_download_bytes.Value - last_update_download_bytes;
                double delta_unzip = current_unzip_bytes.Value - last_update_unzip_bytes;

                this.current_download_speed_BPS = (long)(delta_download / delta_time * 1000.0);
                this.current_unzip_spped_BPS = (long)(delta_unzip / delta_time * 1000.0);

                this.last_update_time = now_time;
                this.last_update_download_bytes = current_download_bytes.Value;
                this.last_update_unzip_bytes = current_unzip_bytes.Value;
            }
            events.ProcessMessages(this);
            return IsRunning;
        }

        private void QueueEvent(MPQUpdaterEvent evt)
        {
            if (evt.EventType == MPQUpdaterEventType.TYPE_ERROR)
            {
                this.current_error_msg = evt.ToString();
                this.status.Value = Status.Error;
            }
            events.Enqueue<MPQUpdaterEvent>(evt, DoEvent);
        }
        private void DoEvent(MPQUpdater u, MPQUpdaterEvent e)
        {
            event_OnEvent?.Invoke(u, e);
        }

        //----------------------------------------------------------------------------------------------------------
        public enum Status
        {
            Error = -1,
            NA = 0,
            Checking = 1,
            Downloading = 2,
            Unzipping = 3,
            Done = 4,
        }
        public delegate void VersionTextEntryAction(string key, string md5, long fsize, string userdata);
        public delegate void VersionTextSubEntryAction(string parent, string key, string md5, long fsize, string userdata);
        public static bool TryForEachVersionTextEntrys(string text, out DateTime utc_time, VersionTextEntryAction action, VersionTextSubEntryAction subaction)
        {
            text = text.Trim();
            utc_time = DateTime.MinValue;
            if (text.StartsWith(VERSION_TEXT_BEGIN) && text.EndsWith(VERSION_TEXT_END))
            {
                char[] spc = { ':' };
                string[] lines = text.Split('\n');
                foreach (string line in lines)
                {
                    if (line.TryIndexOf("TIME_UTC", out var ts) && CUtils.TryParseTime(line.Substring(ts + 8).Trim(), out var _time, DateTimeKind.Utc))
                    {
                        utc_time = _time;
                        continue;
                    }
                    string[] kv = line.Split(spc, 4);
                    if (kv.Length >= 3)
                    {
                        string key = kv[2].Trim().Replace('\\', Separator);
                        string md5 = kv[0].Trim();
                        long fsize = Parser.ParseLong(kv[1].Trim());
                        string userdata = (kv.Length >= 4) ? kv[3] : null;
                        action(key, md5, fsize, userdata);
                    }
                }
                return true;
            }
            else if (XmlUtil.TryFromString(text, out var xversion) && XmlUtil.TryFindChild(xversion.DocumentElement, "Entries", out XmlElement xentries))
            {
                foreach (var line in xentries)
                {
                    if (line is XmlElement line_e && line_e.Name.Equals("Entry"))
                    {
                        var key = XmlUtil.GetAttribute(line_e, "key", true, true).Replace('\\', Separator); ;
                        var md5 = XmlUtil.GetAttribute(line_e, "md5", true, true);
                        var fsize = XmlUtil.GetAttribute(line_e, "size", true, true);
                        var userdata = XmlUtil.GetAttribute(line_e, "user", false, true);
                        if (key != null && md5 != null && long.TryParse(fsize, out var fsizeL))
                        {
                            if (XmlUtil.TryFindChild(line_e, "SubEntries", out XmlElement xsubentries))
                            {
                                foreach (var subline in xsubentries)
                                {
                                    if (subline is XmlElement subline_e && subline_e.Name.Equals("SubEntry"))
                                    {
                                        var sub_key = XmlUtil.GetAttribute(subline_e, "key", true, true).Replace('\\', Separator); ;
                                        var sub_md5 = XmlUtil.GetAttribute(subline_e, "md5", true, true);
                                        var sub_fsize = XmlUtil.GetAttribute(subline_e, "size", true, true);
                                        var sub_userdata = XmlUtil.GetAttribute(subline_e, "user", false, true);
                                        if (sub_key != null && sub_md5 != null && long.TryParse(sub_fsize, out var sub_fsizeL))
                                        {
                                            subaction(key, sub_key, sub_md5, sub_fsizeL, sub_userdata);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                action(key, md5, fsizeL, userdata);
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }
        public class UpdateInfo
        {
            public Uri RemoteVersionURL { get; private set; }
            public DirectoryInfo LocalSaveRoot { get; private set; }
            public DirectoryInfo LocalBundleRoot { get; private set; }

            public long TotalDownloadBytes { get; private set; }
            public long CurrentDownloadBytes { get; private set; }
            public long NeedDownload { get { return TotalDownloadBytes - CurrentDownloadBytes; } }
            public string VersionText { get; private set; }
            private Predicate<RemoteFileInfo> mOnCheckVaild;
            private Action<List<RemoteFileInfo>> mBeforeCheck;
            private HashMap<string, RemoteFileInfo> bundleFiles = new HashMap<string, RemoteFileInfo>();

            internal UpdateInfo(
                Uri remote_version_url,
                DirectoryInfo local_save_root,
                DirectoryInfo local_bundle_root)
            {
                this.TotalDownloadBytes = 0;
                this.CurrentDownloadBytes = 0;

                this.RemoteVersionURL = remote_version_url;
                this.LocalSaveRoot = local_save_root;
                this.LocalBundleRoot = local_bundle_root;
            }

            internal UpdateInfo(
                Uri remote_version_url,
                DirectoryInfo local_save_root,
                DirectoryInfo local_bundle_root,
                Action<List<RemoteFileInfo>> beforeCheck,
                Predicate<RemoteFileInfo> checkVaild)
            {
                this.TotalDownloadBytes = 0;
                this.CurrentDownloadBytes = 0;

                this.RemoteVersionURL = remote_version_url;
                this.LocalSaveRoot = local_save_root;
                this.LocalBundleRoot = local_bundle_root;
                this.mOnCheckVaild = checkVaild;
                this.mBeforeCheck = beforeCheck;
            }

            internal void run()
            {
                HashMap<string, RemoteFileInfo> _all_files = new HashMap<string, RemoteFileInfo>();
                HashMap<string, RemoteFileInfo> _zip_files = new HashMap<string, RemoteFileInfo>();
                HashMap<string, RemoteFileInfo> _mpq_files = new HashMap<string, RemoteFileInfo>();
                HashMap<string, RemoteFileInfo> _bin_files = new HashMap<string, RemoteFileInfo>();

                var removeFileList = new List<RemoteFileInfo>();
                using (var driver = MPQDriverFactory.CreateDownloader(RemoteVersionURL))
                {
                    this.VersionText = driver.DownloadString(RemoteVersionURL);
                    this.VersionText = this.VersionText.Trim();

                    TryForEachVersionTextEntrys(VersionText, out var time_utc, (key, md5, fsize, userdata) =>
                    {
                        //如果本地bundle有此文件//
                        if (File.Exists(LocalBundleRoot.FullName + Separator + key))
                        {
                            FileInfo localfile = new FileInfo(Path.GetFullPath(LocalBundleRoot.FullName + Separator + key));
                            RemoteFileInfo localinf = new RemoteFileInfo(md5, fsize, key, localfile, userdata);
                            if (localfile.Length != fsize)
                            {
                                // 表示Bundle和服务端不一样了，ignore //
                            }
                            else
                            {
                                // 存入列表，忽略下载，skip download //
                                bundleFiles[localinf.key] = localinf;
                                removeFileList.Add(localinf);
                                return;
                            }
                        }
                        FileInfo file = new FileInfo(Path.GetFullPath(LocalSaveRoot.FullName + Separator + key));
                        RemoteFileInfo inf = new RemoteFileInfo(md5, fsize, key, file, userdata);
                        removeFileList.Add(inf);
                    },
                    (parent, key, md5, size, userdata) =>
                    {
                        // 子文件

                    });

                    if (mBeforeCheck != null)
                    {
                        mBeforeCheck.Invoke(removeFileList);
                    }

                    foreach (var inf in removeFileList)
                    {
                        if (!bundleFiles.ContainsKey(inf.key))
                        {
                            if (mOnCheckVaild == null || mOnCheckVaild.Invoke(inf))
                            {
                                var fullName = inf.file.FullName;
                                _all_files[fullName] = inf;

                                if (inf.key.ToLower().EndsWith(ZIP_EXT))
                                {
                                    _zip_files[fullName] = inf;
                                    this.TotalDownloadBytes += inf.size;
                                }
                                else if (inf.key.ToLower().EndsWith(MPQ_EXT))
                                {
                                    _mpq_files[fullName] = inf;
                                    //this.TotalUnzipBytes += inf.size;
                                }
                                else
                                {
                                    _bin_files[fullName] = inf;
                                    this.TotalDownloadBytes += inf.size;
                                }
                            }
                        }
                    }

                    List<RemoteFileInfo> trydownlist = new List<RemoteFileInfo>();
                    trydownlist.AddRange(_zip_files.Values);
                    trydownlist.AddRange(_bin_files.Values);
                    foreach (RemoteFileInfo inf in trydownlist)
                    {
                        long need_bytes = inf.size;
                        long exist_size = 0;
                        {
                            // 检测本地是否已经有了，包内 //
                            if (exist_bundle(inf))
                            {
                                this.CurrentDownloadBytes += need_bytes;
                                continue;
                            }
                            // 确认对应的MPQ文件是否完整 //
                            if (inf.file.FullName.EndsWith(ZIP_EXT))
                            {
                                string mpq_name = inf.file.FullName.Substring(0, inf.file.FullName.LastIndexOf(ZIP_EXT));
                                RemoteFileInfo mpq_file = _mpq_files.Get(mpq_name);
                                if (mpq_file != null)
                                {
                                    if (mpq_file.IsCompletion())
                                    {
                                        this.CurrentDownloadBytes += need_bytes;
                                        continue;
                                    }
                                }
                            }
                        }
                        // 如果已经存在未下载完成的 //
                        if (inf.file.Exists)
                        {
                            exist_size = inf.file.Length;
                            need_bytes = inf.size - exist_size;
                        }
                        this.CurrentDownloadBytes += exist_size;
                    }
                }
            }

            private bool exist_bundle(RemoteFileInfo inf)
            {
                // 确认对应的MPQ文件是否完整 //
                if (inf.key.EndsWith(ZIP_EXT))
                {
                    string mpq_key = inf.key.Substring(0, inf.key.LastIndexOf(ZIP_EXT));
                    RemoteFileInfo loc_file = bundleFiles.Get(mpq_key);
                    if (loc_file != null && loc_file.file.Exists)
                    {
                        return true;
                    }
                }
                else
                {
                    RemoteFileInfo loc_file = bundleFiles.Get(inf.key);
                    if (loc_file != null && loc_file.file.Exists)
                    {
                        return true;
                    }
                }
                return false;
            }

        }

        //----------------------------------------------------------------------------------------------------------
        class RunTask
        {
            private Logger log;
            private readonly MPQUpdater updater;

            public RunTask(MPQUpdater updater)
            {
                this.log = updater.log;
                this.updater = updater;
            }

            public void Run(Action<ICollection<RemoteFileInfo>> done)
            {
                updater.is_running = true;
                updater.is_done = false;
                updater.total_download_bytes = 0;
                updater.current_download_bytes.Value = 0;
                updater.total_unzip_bytes = 0;
                updater.current_unzip_bytes.Value = 0;
                var _old_files_md5 = new HashMap<string, string>();
                var _all_files = new HashMap<string, RemoteFileInfo>();
                var _zip_files = new HashMap<string, RemoteFileInfo>();
                var _mpq_files = new HashMap<string, RemoteFileInfo>();
                var _bin_files = new HashMap<string, RemoteFileInfo>();
                var _download_over_list = new List<RemoteFileInfo>();
                //                 this._old_files_md5.Clear();
                //                 this._all_files.Clear();
                //                 this._zip_files.Clear();
                //                 this._mpq_files.Clear();
                //                 this._bin_files.Clear();
                //                 this._download_over_list.Clear();
                try
                {
                    log.Info(string.Format($"============================"));
                    log.Info(string.Format($"= 检测上次下载的 : {updater.version_file}"));
                    log.Info(string.Format($"============================"));
                    #region 检测上次下载的
                    {
                        updater.status.Value = Status.Checking;
                        if (updater.version_file.Exists)
                        {
                            updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_VALIDATING, "VALIDATING"));
                            try
                            {
                                string old_version_text = File.ReadAllText(updater.version_file.FullName);
                                TryForEachVersionTextEntrys(old_version_text, out var time_utc, (key, md5, fsize, userdata) =>
                                {
                                    string path = updater.save_root.FullName + Separator + key;
                                    FileInfo localfile = new FileInfo(Path.GetFullPath(path));
                                    //如果本地有此文件//
                                    if (localfile.Exists)
                                    {
                                        //参与MD5计算//
                                        if (localfile.Length == fsize)
                                        {
                                            _old_files_md5.Add(key, md5);
                                        }
                                    }
                                },
                                (parent, key, md5, size, userdata) =>
                                {
                                    // 子文件

                                });
                            }
                            catch (Exception err)
                            {
                                log.Error(err.Message, err);
                            }
                        }
                    }
                    #endregion
                    log.Info(string.Format($"============================"));
                    log.Info(string.Format($"= 下载更新列表 : {updater.version_url}"));
                    log.Info(string.Format($"============================"));
                    #region 下载更新列表
                    {
                        updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_VALIDATING, "VALIDATING"));
                        try
                        {
                            using (var downloader = MPQDriverFactory.CreateDownloader(updater.version_url))
                            {
                                updater.version_text = downloader.DownloadString(updater.version_url);
                                updater.version_text = updater.version_text.Trim();
                                log.Info("\n" + updater.version_text);
                                var processUpdateFiles = TryForEachVersionTextEntrys(updater.version_text, out var time_utc, (key, md5, fsize, userdata) =>
                                {
                                    if (updater.is_exit) { return; }
                                    //如果本地bundle有此文件//
                                    if (File.Exists(updater.bundle_root.FullName + Separator + key))
                                    {
                                        FileInfo localfile = new FileInfo(Path.GetFullPath(updater.bundle_root.FullName + Separator + key));
                                        RemoteFileInfo localinf = new RemoteFileInfo(md5, fsize, key, localfile, userdata);
                                        if (localfile.Length != fsize)
                                        {
                                            // 表示Bundle和服务端不一样了，ignore //
                                            log.Info(string.Format("检测Bundle资源不一致 : {0}" +
                                                "\n  bundle size = {1}" +
                                                "\n  remote size = {2}",
                                                localfile.FullName,
                                                localfile.Length,
                                                fsize));
                                        }
                                        else
                                        {
                                            // 存入列表，忽略下载，skip download //
                                            updater.bundleFiles[localinf.key] = localinf;
                                            log.Info(string.Format("文件已包括在Bundle目录中。忽略下载 : {0}", localinf.key));
                                            return;
                                        }
                                    }

                                    FileInfo file = new FileInfo(Path.GetFullPath(updater.save_root.FullName + Separator + key));
                                    RemoteFileInfo inf = new RemoteFileInfo(md5, fsize, key, file, userdata);

                                    if (updater.event_OnCheckVaild == null || updater.event_OnCheckVaild.Invoke(inf))
                                    {
                                        _all_files[file.FullName] = inf;

                                        if (updater.DoNotDownloadZip)
                                        {
                                            if (key.ToLower().EndsWith(ZIP_EXT))
                                            {
                                            }
                                            else if (key.ToLower().EndsWith(MPQ_EXT))
                                            {
                                                _mpq_files[file.FullName] = inf;
                                                updater.total_download_bytes += inf.size;
                                            }
                                            else
                                            {
                                                _bin_files[file.FullName] = inf;
                                                updater.total_download_bytes += inf.size;
                                            }
                                        }
                                        else
                                        {

                                            if (key.ToLower().EndsWith(ZIP_EXT))
                                            {
                                                _zip_files[file.FullName] = inf;
                                                updater.total_download_bytes += inf.size;
                                            }
                                            else if (key.ToLower().EndsWith(MPQ_EXT))
                                            {
                                                _mpq_files[file.FullName] = inf;
                                                updater.total_unzip_bytes += inf.size;
                                            }
                                            else
                                            {
                                                _bin_files[file.FullName] = inf;
                                                updater.total_download_bytes += inf.size;
                                            }
                                        }
                                    }

                                },
                                (parent, key, md5, size, userdata) =>
                                {
                                    // 子文件


                                });
                                if (!processUpdateFiles)
                                {
                                    updater.status.Value = Status.Error;
                                    updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_ERROR, "CHECK_VERSION : Begin End \n" + updater.version_text));
                                    return;
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                            updater.status.Value = Status.Error;
                            updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_ERROR, "CHECK_VERSION : ", err));
                            return;
                        }
                    }
                    #endregion
                    log.Info(string.Format($"============================"));
                    log.Info(string.Format($"= 删除下载列表中没有的文件 : {updater.save_root}"));
                    log.Info(string.Format($"============================"));
                    #region 删除下载列表中没有的文件
                    {
                        try
                        {
                            List<FileInfo> exists = CFiles.ListAllFiles(updater.save_root);
                            foreach (FileInfo ff in exists)
                            {
                                if (updater.is_exit) return;
                                if (!CFiles.FileEquals(ff, updater.version_file))
                                {
                                    RemoteFileInfo remote;
                                    string old_md5;
                                    if (!_all_files.TryGetValue(ff.FullName, out remote))
                                    {
                                        log.Info(string.Format("Save文件不存在于远程列表中。\n  删除 : {0}", ff.FullName));
                                        ff.Delete();
                                        continue;
                                    }
                                    if (ff.Length > remote.size)
                                    {
                                        log.Info(string.Format("Save文件尺寸大于远程，不可能。\n  删除重新下载 : {0}", ff.FullName));
                                        ff.Delete();
                                        continue;
                                    }
                                    if (_old_files_md5.TryGetValue(remote.key, out old_md5))
                                    {
                                        if (!remote.md5.ToLower().Equals(old_md5.ToLower()))
                                        {
                                            log.Info(string.Format("Save文件之前的MD5和本次不一致。\n  删除重新下载 : {0}", ff.FullName));
                                            ff.Delete();
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                            updater.status.Value = Status.Error;
                            updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_ERROR, "CLEANUP : ", err));
                            return;
                        }
                        finally
                        {
                            CFiles.CreateFile(updater.version_file);
                            File.WriteAllText(updater.version_file.FullName, updater.version_text, CUtils.UTF8);
                        }
                    }
                    #endregion
                    log.Info(string.Format($"============================"));
                    log.Info(string.Format($"= 下载资源 : {_all_files.Count}"));
                    log.Info(string.Format($"============================"));
                    #region 下载资源
                    {
                        updater.status.Value = Status.Downloading;
                        List<RemoteFileInfo> trydownlist = new List<RemoteFileInfo>();
                        if (updater.DoNotDownloadZip)
                        {
                            trydownlist.AddRange(_mpq_files.Values);
                        }
                        else
                        {
                            trydownlist.AddRange(_zip_files.Values);
                        }
                        trydownlist.AddRange(_bin_files.Values);
                        List<RemoteFileInfo> downloadlist = new List<RemoteFileInfo>();
                        foreach (RemoteFileInfo inf in trydownlist)
                        {
                            if (updater.is_exit) return;
                            try
                            {
                                long need_bytes = inf.size;
                                long exist_size = 0;
                                {
                                    // 检测本地是否已经有了，包内 //
                                    {
                                        // 确认对应的MPQ文件是否完整 //
                                        if (inf.key.EndsWith(ZIP_EXT))
                                        {
                                            string mpq_key = inf.key.Substring(0, inf.key.LastIndexOf(ZIP_EXT));
                                            RemoteFileInfo loc_file = updater.bundleFiles.Get(mpq_key);
                                            if (loc_file != null && loc_file.file.Exists)
                                            {
                                                log.Info(string.Format("文件已包括在Bundle目录中。忽略下载 : {0}", inf.key));
                                                updater.current_download_bytes += need_bytes;
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            RemoteFileInfo loc_file = updater.bundleFiles.Get(inf.key);
                                            if (loc_file != null && loc_file.file.Exists)
                                            {
                                                log.Info(string.Format("文件已包括在Bundle目录中。忽略下载 : {0}", inf.key));
                                                updater.current_download_bytes += need_bytes;
                                                continue;
                                            }
                                        }
                                    }
                                    // 确认对应的MPQ文件是否完整 //
                                    if (inf.file.FullName.EndsWith(ZIP_EXT))
                                    {
                                        string mpq_name = inf.file.FullName.Substring(0, inf.file.FullName.LastIndexOf(ZIP_EXT));
                                        RemoteFileInfo mpq_file = _mpq_files.Get(mpq_name);
                                        if (mpq_file != null)
                                        {
                                            if (mpq_file.IsCompletion())
                                            {
                                                log.Info(string.Format("压缩文件对应MPQ已经完整。忽略下载 : {0}", inf.file.FullName));
                                                updater.current_download_bytes += need_bytes;
                                                continue;
                                            }
                                        }
                                    }
                                }
                                // 如果已经存在未下载完成的 //
                                if (inf.file.Exists)
                                {
                                    exist_size = inf.file.Length;
                                    need_bytes = inf.size - exist_size;
                                }
                                updater.current_download_bytes += exist_size;
                                // 如果文件已存在并且尺寸一样 //
                                if (need_bytes <= 0)
                                {
                                    log.Info(string.Format("文件已存在，并且尺寸一致。忽略下载 : {0}", inf.file.FullName));
                                    continue;
                                }
                                else
                                {
                                    downloadlist.Add(inf);
                                }
                            }
                            catch (Exception err)
                            {
                                log.Error(err.Message, err);
                                updater.status.Value = Status.Error;
                                updater.QueueEvent(
                                    new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_ERROR,
                                    string.Format("Validate : {0}", inf.file.Name), err));
                                return;
                            }
                        }
                        if (downloadlist.Count > 0)
                        {
                            updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_DOWNLOADING, "DOWNLOADING"));
                            foreach (RemoteFileInfo inf in downloadlist)
                            {
                                if (updater.is_exit) return;
                                try
                                {
                                    long need_bytes = inf.size;
                                    long exist_size = 0;
                                    // 如果已经存在未下载完成的 //
                                    if (inf.file.Exists)
                                    {
                                        exist_size = inf.file.Length;
                                        need_bytes = inf.size - exist_size;
                                    }
                                    else
                                    {
                                        CFiles.CreateFile(inf.file);
                                    }
                                    // 如果需要的空间无法满足 //
                                    long save_avaliable = updater.driver.GetAvaliableSpace(updater.save_root.FullName);
                                    if (need_bytes >= save_avaliable)
                                    {
                                        updater.QueueEvent(new MPQUpdaterEvent(
                                            MPQUpdaterEventType.TYPE_NOT_ENOUGH_SPACE,
                                            "Space not available!" +
                                               " \n free=" + save_avaliable +
                                               " \n need=" + need_bytes));
                                        return;
                                    }
                                    // 开始下载 //
                                    updater.current_download_file = inf.key;
                                    try
                                    {
                                        log.Info(string.Format("开始下载: {0} ", inf.key));
                                        using (var downloader = MPQDriverFactory.CreateDownloader(updater.version_url))
                                        {
                                            long old_current_download_bytes = updater.current_download_bytes.Value;
                                            if (downloader.RunDownloadSingle(updater, inf, exist_size, need_bytes, updater.current_download_bytes) == false)
                                            {
                                                return;
                                            }
                                            if ((old_current_download_bytes + need_bytes) != updater.current_download_bytes.Value)
                                            {
                                                updater.current_download_bytes.Value = old_current_download_bytes + need_bytes;
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        _download_over_list.Add(inf);
                                        updater.current_download_file = null;
                                    }
                                }
                                catch (Exception err)
                                {
                                    log.Error(err.Message, err);
                                    updater.status.Value = Status.Error;
                                    updater.QueueEvent(
                                        new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_ERROR,
                                        string.Format("DOWNLOAD_ZIPS : {0}", inf.file.Name), err));
                                    return;
                                }
                            }
                        }
                    }
                    #endregion
                    log.Info(string.Format("============================"));
                    log.Info(string.Format("= 下载完毕检测MD5 ="));
                    log.Info(string.Format("============================"));
                    #region 下载完毕检测MD5
                    {
                        updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_VALIDATING, "VALIDATING"));
                        try
                        {
                            if (updater.is_check_md5)
                            {
                                foreach (RemoteFileInfo downloaded in _download_over_list)
                                {
                                    if (updater.is_exit) return;

                                    if (updater.driver.RunGetFileMD5(downloaded.file.FullName, out var fmd5))
                                    {
                                        if (!fmd5.ToLower().Equals(downloaded.md5.ToLower()))
                                        {
                                            log.Error(string.Format("MD5不匹配，删除重新下 : {0}", downloaded.key));
                                            downloaded.file.Delete();
                                            // MD5不匹配，删除重新下 //
                                            throw new Exception("file md5 not validate : " + downloaded.key);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                            updater.status.Value = Status.Error;
                            updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_ERROR, "DOWNLOAD_OVER : ", err));
                            return;
                        }
                    }
                    #endregion
                    log.Info(string.Format("============================"));
                    log.Info(string.Format("= 解压资源"));
                    log.Info(string.Format("============================"));
                    #region 解压资源
                    if (updater.DoNotUnzip == false)
                    {
                        updater.status.Value = Status.Unzipping;
                        {
                            List<RemoteFileInfo> tryunziplist = new List<RemoteFileInfo>();
                            foreach (RemoteFileInfo inf in _zip_files.Values)
                            {
                                if (updater.is_exit) return;
                                try
                                {
                                    string mpq_name = inf.file.FullName.Substring(0, inf.file.FullName.LastIndexOf(ZIP_EXT));
                                    RemoteFileInfo mpqf = _mpq_files.Get(mpq_name);
                                    if (mpqf != null)
                                    {
                                        // 确认对应的MPQ文件是否完整
                                        if (mpqf.IsCompletion())
                                        {
                                            updater.current_unzip_bytes += mpqf.size;
                                            log.Info(string.Format("文件完整，不需要解压缩: {0} ", mpqf.file.FullName));
                                            continue;
                                        }
                                        // 开始解压缩
                                        if (inf.file.Exists)
                                        {
                                            tryunziplist.Add(inf);
                                        }
                                    }
                                }
                                catch (Exception err)
                                {
                                    log.Error(err.Message, err);
                                    updater.status.Value = Status.Error;
                                    updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_ERROR, "UNZIP : " + inf.file.Name, err));
                                    return;
                                }
                            }
                            if (tryunziplist.Count > 0)
                            {
                                updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_UNZIP, "UNZIP"));
                                foreach (RemoteFileInfo inf in tryunziplist)
                                {
                                    if (updater.is_exit) return;
                                    try
                                    {
                                        string mpq_name = inf.file.FullName.Substring(0, inf.file.FullName.LastIndexOf(ZIP_EXT));
                                        RemoteFileInfo mpqf = _mpq_files.Get(mpq_name);
                                        if (mpqf != null)
                                        {
                                            // 如果需要的空间无法满足
                                            long save_avaliable = updater.driver.GetAvaliableSpace(updater.save_root.FullName);
                                            long need_bytes = mpqf.size;
                                            if (need_bytes >= save_avaliable)
                                            {
                                                updater.status.Value = Status.Error;
                                                updater.QueueEvent(new MPQUpdaterEvent(
                                                    MPQUpdaterEventType.TYPE_NOT_ENOUGH_SPACE,
                                                    "Space not available!" +
                                                       " \n free=" + save_avaliable +
                                                       " \n need=" + need_bytes));
                                                return;
                                            }
                                            // 开始解压缩
                                            if (inf.file.Exists)
                                            {
                                                updater.current_unzip_file = inf.key;
                                                try
                                                {
                                                    log.Info(string.Format("开始解压缩: {0} -> {1}", inf.key, mpqf.file.Extension));
                                                    using (var unziper = MPQDriverFactory.CreateUnziper(updater.save_root))
                                                    {
                                                        long _need_bytes = mpqf.size;
                                                        long _old_current_unzip_bytes = updater.current_unzip_bytes.Value;
                                                        if (unziper.RunUnzipSingle(updater, inf, mpqf, updater.current_unzip_bytes) == false)
                                                        {
                                                            return;
                                                        }
                                                        if ((_old_current_unzip_bytes + _need_bytes) != updater.current_unzip_bytes.Value)
                                                        {
                                                            updater.current_unzip_bytes.Value = _old_current_unzip_bytes + _need_bytes;
                                                        }
                                                    }
                                                    if (!mpqf.file.Exists)
                                                    {
                                                        log.Error(string.Format("解压缩失败: {0}", inf.file.Extension));
                                                        updater.status.Value = Status.Error;
                                                        updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_ERROR, "UNZIP : " + inf.file.Name));
                                                        return;
                                                    }
                                                }
                                                finally
                                                {
                                                    inf.file.Delete();
                                                    updater.current_unzip_file = null;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception err)
                                    {
                                        log.Error(err.Message, err);
                                        updater.status.Value = Status.Error;
                                        updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_ERROR, "UNZIP : " + inf.file.Name, err));
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    log.Info(string.Format("============================"));
                    log.Info(string.Format("= 完成 ="));
                    log.Info(string.Format("============================"));

                    updater.status.Value = Status.Done;
                    updater.is_done = true;

                    done.Invoke(_all_files.Values);

                    updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_COMPLETE, "COMPLETE"));
                }
                catch (Exception err)
                {
                    updater.status.Value = Status.Error;
                    log.Error(err.Message, err);
                    updater.QueueEvent(new MPQUpdaterEvent(MPQUpdaterEventType.TYPE_ERROR, "ERROR :", err));
                }
                finally
                {
                    updater.is_running = false;
                }
            }


            //————————————————————————————————————————






        }

        //----------------------------------------------------------------------------------------------------------
        protected override void Disposing()
        {
            is_exit = true;
            event_OnCheckVaild = null;
            try
            {
                if (workthread != null)
                {
                    workthread.Join();
                }
            }
            catch (System.Exception err)
            {
                log.Error(err.Message, err);
                //Console.WriteLine(e);
            }
            events.Dispose();
        }


        public ICollection<FileInfo> GetAllFiles()
        {
            List<FileInfo> files = new List<FileInfo>();
            lock (remoteFiles)
            {
                foreach (RemoteFileInfo rf in remoteFiles)
                {
                    files.Add(rf.file);
                }
                foreach (RemoteFileInfo rf in bundleFiles.Values)
                {
                    files.Add(rf.file);
                }
            }
            return files;
        }
        public List<RemoteFileInfo> GetAllRemoteFiles()
        {
            List<RemoteFileInfo> files = new List<RemoteFileInfo>();
            lock (remoteFiles)
            {
                foreach (RemoteFileInfo rf in remoteFiles)
                {
                    files.Add(rf);
                }
                foreach (RemoteFileInfo rf in bundleFiles.Values)
                {
                    files.Add(rf);
                }
            }
            return files;
        }

        //----------------------------------------------------------------------------------------------------------
        public void GetTextInfo(out float percent, out string stat)
        {
            var status = new StringBuilder();
            status.Append($"{CurrentStatus}");
            if (CurrentStatus == MPQUpdater.Status.Downloading)
            {
                percent = CurrentDownloadBytes / (float)TotalDownloadBytes;
                status.AppendLine($" : {CurrentDownloadFile}  ");
                status.AppendLine($"Speed : {CUtils.ToBytesString(CurrentDownloadSpeed)}/S  ");
                status.AppendLine($"Total : {CUtils.ToBytesString(CurrentDownloadBytes)}/{CUtils.ToBytesString(TotalDownloadBytes).ToUpper()}  ");
            }
            else if (CurrentStatus == MPQUpdater.Status.Unzipping)
            {
                percent = CurrentUnzipBytes / (float)TotalUnzipBytes;
                status.AppendLine($" : {CurrentUnzipFile}  ");
                status.AppendLine($"Speed : {CUtils.ToBytesString(CurrentUnzipSpeed)}/S ");
                status.AppendLine($"Total : {CUtils.ToBytesString(CurrentUnzipBytes)}/{CUtils.ToBytesString(TotalUnzipBytes)}  ");
            }
            else if (CurrentStatus == Status.Done)
            {
                percent = 1f;
                status.AppendLine($" : Finish  ");
            }
            else if (CurrentStatus == Status.Error)
            {
                percent = 0f;
                status.AppendLine($" : {CurrentErrorMessage}  ");
            }
            else
            {
                percent = 0f;
            }
            stat = status.ToString();
        }

        public int DownloadTimeoutSEC
        {
            get;
            set;
        }

        public string[] UrlRoots { get { return url_roots; } }

        public string VersionText
        {
            get { return version_text; }
        }

        public string CurrentDownloadFile
        {
            get { return current_download_file; }
        }
        public long TotalDownloadBytes
        {
            get { return total_download_bytes; }
        }
        public long CurrentDownloadBytes
        {
            get { return current_download_bytes.Value; }
        }
        public long CurrentDownloadSpeed
        {
            get { return current_download_speed_BPS; }
        }
        public string CurrentUnzipFile
        {
            get { return current_unzip_file; }
        }
        public long TotalUnzipBytes
        {
            get { return total_unzip_bytes; }
        }
        public long CurrentUnzipBytes
        {
            get { return current_unzip_bytes.Value; }
        }
        public long CurrentUnzipSpeed
        {
            get { return current_unzip_spped_BPS; }
        }
        public string CurrentErrorMessage
        {
            get { return current_error_msg; }
        }

        public long TotalProcessBytes => total_download_bytes + total_unzip_bytes;
        public long CurrentProcessBytes => current_download_bytes.Value + current_unzip_bytes.Value;

        public bool IsDone
        {
            get { return is_done; }
        }

        public bool IsRunning
        {
            get { return is_running; }
        }

        public Status CurrentStatus
        {
            get { return status.Value; }
        }

        //-----------------------------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------------------

        public class RemoteFileInfo
        {
            private FileInfo _file;

            public readonly string md5;
            public readonly string key;
            public readonly long size;
            public readonly string userdata;

            public FileInfo file
            {
                get { _file.Refresh(); return _file; }
            }

            public RemoteFileInfo(string md5, long size, string key, FileInfo file, string userdata = null)
            {
                this.md5 = md5;
                this.key = key;
                this.size = size;
                this._file = file;
                this.userdata = userdata;
            }

            public bool IsCompletion()
            {
                if (file.Exists && file.Length == size)
                {
                    return true;
                }
                return false;
            }
        }
    }
    public enum MPQUpdaterEventType
    {
        TYPE_COMPLETE = 1,
        TYPE_VALIDATING = 2,
        TYPE_DOWNLOADING = 3,
        TYPE_UNZIP = 4,
        TYPE_ERROR = -1,
        TYPE_NOT_ENOUGH_SPACE = -2,
    }
    public class MPQUpdaterEvent
    {
        public MPQUpdaterEventType EventType { get; private set; }
        public string Message { get; private set; }
        public Exception Cause { get; private set; }

        public MPQUpdaterEvent(MPQUpdaterEventType type, string message, Exception err = null)
        {
            this.EventType = type;
            this.Message = message;
            this.Cause = err;
        }

        override public string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Code=").Append(EventType);
            if (Message != null)
                sb.AppendLine().Append(Message);
            if (Cause != null)
                sb.AppendLine().Append(Cause.Message);
            return sb.ToString();
        }
    }




}
