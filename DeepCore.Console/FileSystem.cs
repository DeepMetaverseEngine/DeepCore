using DeepCore;
using DeepCore.Log;
using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;

namespace DeepEditor.Common
{
    public static class FileSystem
    {
        public static int DeleteToRecycleBin(string path)
        {
            if (File.Exists(path))
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    path,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);
            }
            else if (Directory.Exists(path))
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                    path,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);
            }
            return 0;
        }

    }

    public class FileSystemWorkSpace : Disposable
    {
        public static FileSystemWorkSpace Instance { get; private set; }
        private readonly Logger log = new LazyLogger("FS");
        private readonly HashMap<string, (FileInfo, DateTime)> lastfixed = new();
        private readonly HashMap<string, FileInfo> saving = new();
        private readonly DirectoryInfo root;
        private FileSystemWatcher _fileWatcher;
        public FileSystemWorkSpace(DirectoryInfo dir)
        {
            Instance = this;
            this.root = dir;
        }
        public void Start()
        {
            // 创建 FileSystemWatcher 实例
            _fileWatcher = new FileSystemWatcher();
            // 设置监控的目录路径
            _fileWatcher.Path = root.FullName;
            _fileWatcher.IncludeSubdirectories = true;
            // 设置需要监控的事件类型
            _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            // 订阅事件
            _fileWatcher.Changed += OnFileWriteChanged;
            _fileWatcher.Created += OnFileWriteCreated;
            //             _fileWatcher.Created += OnChanged;
            //             _fileWatcher.Deleted += OnChanged;
            //             _fileWatcher.Renamed += OnRenamed;
            // 启用事件监听
            _fileWatcher.EnableRaisingEvents = true;
        }
        protected override void Disposing()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }
        }
        public void Reset()
        {
            lock (lastfixed)
            {
                foreach (var f in lastfixed.ToArray())
                {
                    var file = f.Value.Item1;
                    var time = f.Value.Item2;
                    file.Refresh();
                    time = file.LastWriteTime;
                    lastfixed.Put(f.Key, (file, time));
                }
            }
        }
        public T Loading<T>(FileInfo file, Func<FileInfo, T> loadAction)
        {
            lock (lastfixed)
            {
                file.Refresh();
                lastfixed.Put(file.FullName, (file, file.LastWriteTime));
                saving.Put(file.FullName, file);
            }
            try
            {
                return loadAction(file);
            }
            finally
            {
                lock (lastfixed)
                {
                    file.Refresh();
                    saving.Remove(file.FullName);
                    lastfixed.Put(file.FullName, (file, file.LastWriteTime));
                }
            }
        }
        public void Saving<T>(FileInfo file, T data, Action<FileInfo, T> saveAction)
        {
            lock (lastfixed)
            {
                file.Refresh();
                lastfixed.Put(file.FullName, (file, file.LastWriteTime));
                saving.Put(file.FullName, file);
            }
            try
            {
                saveAction(file, data);
            }
            finally
            {
                lock (lastfixed)
                {
                    file.Refresh();
                    saving.Remove(file.FullName);
                    lastfixed.Put(file.FullName, (file, file.LastWriteTime));
                }
            }
        }

        private bool TryGetFileLastTime(string fullpath, out FileInfo file, out DateTime lastTime)
        {
            lock (lastfixed)
            {
                if (saving.TryGetValue(fullpath, out var exist))
                {
                    file = null;
                    lastTime = default;
                    return false;
                }
                if (lastfixed.TryGetValue(fullpath, out var tuple))
                {
                    file = tuple.Item1;
                    lastTime = tuple.Item2;
                    file.Refresh();
                    return true;
                }
            }
            file = null;
            lastTime = default;
            return false;
        }

        private void OnFileWriteCreated(object sender, FileSystemEventArgs e)
        {
            Changed?.Invoke(sender, e);
        }
        private void OnFileWriteChanged(object sender, FileSystemEventArgs e)
        {
            if (TryGetFileLastTime(e.FullPath, out var file, out var lastTime))
            {
                var now = file.LastWriteTime;
                if (lastTime < now)
                {
                    //log.Info($"文件或文件夹发生变更: {e.FullPath}, 类型: {e.ChangeType}");
                    Changed?.Invoke(sender, e);
                    lock (lastfixed)
                    {
                        lastfixed.Put(file.FullName, (file, file.LastWriteTime));
                    }
                }
            }
        }
        public event FileSystemEventHandler Changed;



        public static byte[] ReadAllBytes(FileInfo file)
        {
            if (Instance != null)
            {
                return Instance.Loading(file, file => File.ReadAllBytes(file.FullName));
            }
            else
            {
                return File.ReadAllBytes(file.FullName);
            }
        }
        public static void WriteAllBytes(FileInfo file, byte[] data)
        {
            if (Instance != null)
            {
                Instance.Saving(file, data, (file, data) => File.WriteAllBytes(file.FullName, data));
            }
            else
            {
                File.WriteAllBytes(file.FullName, data);
            }
        }
    }

}
