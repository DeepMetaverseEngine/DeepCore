using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static DeepCore.TemplateLoader;

namespace DeepCore
{
    //-------------------------------------------------------------------------------------------------------------------------------------------------
    public struct XlsFileInfo
    {
        public string FileName;
        public string[] Sheets;
        public XlsFileInfo(string fileName, string[] sheets)
        {
            this.FileName = fileName;
            this.Sheets = sheets;
        }
        public XlsFileInfo(string fileName)
        {
            this.FileName = fileName;
            this.Sheets = null;
        }
        public override string ToString()
        {
            if (Sheets == null || Sheets.Length == 0)
            {
                return $"{FileName}";
            }
            return $"{FileName} [{CUtils.ArrayToString(Sheets)}] ";
        }
    }
    public struct CacheFileInfo
    {
        public List<XlsFileInfo> XlsFiles { get; }
        public CacheFileInfo(params XlsFileInfo[] files)
        {
            XlsFiles = new List<XlsFileInfo>(files);
        }
        public override string ToString()
        {
            if (XlsFiles == null) { return "null"; }
            if (XlsFiles.Count == 0) { return ""; }
            return CUtils.ListToString(XlsFiles);
        }
        public string ToVisibleName()
        {
            if (XlsFiles == null) { return "null"; }
            if (XlsFiles.Count == 0) { return ""; }
            return Path.GetFileName(XlsFiles[0].FileName);
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------

    public delegate bool TyGetFullPathDelegate(string root, string path, out string fullPath);
    public delegate void OnTableLoadedDelegate(TableBase table);
    public delegate void OnDataLoadedDelegate(TableBase table, string fileName, string sheetName, object key, object data);
    public delegate void OnDataCenterLoadDelegate(TemplateDataCenter dc, object from, IRangeValue progress = null);
    public delegate void ForEachSheetAction<ST>(ST st, string fileName, string sheetName, ICollection datas);
    public delegate void ForEachSheetAction<ST, T>(ST st, string fileName, string sheetName, ICollection<T> datas);
    public delegate void ForEachDataAction<ST>(ST st, string fileName, string sheetName, object keyValue, object data);
    public delegate void ForEachDataAction<ST, T>(ST st, string fileName, string sheetName, object keyValue, T data);

    //-------------------------------------------------------------------------------------------------------------------------------------------------

    //-------------------------------------------------------------------------------------------------------------------------------------------------
    public partial class TemplateDataCenter : Disposable
    {
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public static event TyGetFullPathDelegate TyGetFullPath;
        public event OnDataCenterLoadDelegate OnReload;
        public event OnTableLoadedDelegate OnTableLoaded;
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        private readonly ArrayList<TableBase> templatesList = new();
        private readonly HashMap<string, TableBase> templatesMap = new();
        private readonly HashMap<Type, HashMap<string, TableBase>> templatesTypeMap = new();
        //private Queue<TableLoadEvent> pendingEvents = new Queue<TableLoadEvent>();
        private Semaphore semaphore = new Semaphore(Math.Max(BATCH_CONCURRENT, 2), Math.Max(BATCH_CONCURRENT, 2));
        public string Name { get; }
        public string DataRoot { get; }
        public IExternalizableFactory Codec { get; }
        protected internal Logger log { get; }
        public TemplateLoader DataLoader { get; }
        public IReadOnlyList<TableBase> Tables => templatesList;
        public int TablesCount => templatesList.Count;
        public int LoadingThreadID { get; private set; }
        public TemplateDataCenter(TemplateLoader loader, IExternalizableFactory codec, string name, string dataRoot)
        {
            this.Name = name;
            this.log = LoggerFactory.GetLogger(name);
            this.Codec = codec;
            this.DataLoader = loader;
            this.DataRoot = Resource.FormatPath(dataRoot);
            if (ENABLE_LOAD_FROM_BIN && loader != null)
                this.BinDataLoader = new BinaryTemplateLoader(Codec, loader, false);
            this.RegistTables();
            this.RegistModules();
        }
        public TemplateDataCenter(IExternalizableFactory codec, string name, string dataRoot) : this(TemplateLoader.Instance, codec, name, dataRoot)
        {
        }
        protected virtual void RegistTables()
        {
            var nop = new object[0];
            var methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.TryGetAttribute<RegistTableActionAttribute>(out var _));
            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(this, nop);
                }
                catch (Exception ex)
                {
                    ex.PrintStackTrace();
                }
            }
        }
        protected virtual void FinalTables()
        {
            var nop = new object[0];
            var methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.TryGetAttribute<RegistOnLoadActionAttribute>(out var _));
            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(this, nop);
                }
                catch (Exception ex)
                {
                    ex.PrintStackTrace();
                }
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        protected override void Disposing()
        {
            semaphore.Dispose();
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        private void FireEvents(IRangeValue progress, object from, IEnumerable<TableBase> caches)
        {
            try
            {
                OnReload?.Invoke(this, from, progress);
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
            foreach (var cache in caches)
            {
                try
                {
                    this.OnTableLoaded?.Invoke(cache);
                }
                catch (Exception err)
                {
                    log.Error(err);
                    throw;
                }
                cache.FireEvents();
            }
            FinalTables();
            //             foreach (var e in pendingEvents)
            //             {
            //                 e.table.FireEvents(e.fileName, e.sheetName, e.key, e.data);
            //             }
        }
        //         internal void QueueEvent(TableLoadEvent e) { }
        //         internal struct TableLoadEvent
        //         {
        //             internal TableBase table;
        //             internal string fileName;
        //             internal string sheetName;
        //             internal object key;
        //             internal object data;
        //         }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public static bool ENABLE_BATCH_LOAD = false;
        public static int BATCH_CONCURRENT = 2;
        public static bool ENABLE_LOAD_FROM_BIN = false;
        public bool EnableDuplicateKey = false;
        public bool Verbose = false; //是否开启详细日志

        protected void Reload(IRangeValue progress, params TableBase[] caches)
        {
            var stopwatch = Stopwatch.StartNew();
            LoadingThreadID = Thread.CurrentThread.ManagedThreadId;
            try
            {
                if (progress != null)
                {
                    progress.SetRange(0, caches.Length, 0);
                }
                if (ENABLE_BATCH_LOAD)
                {
                    var tasks = new List<Task>(caches.Length);
                    foreach (var cache in caches)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                if (Verbose) log.InfoFormat("Reload : {0}", cache);
                                semaphore.WaitOne();
                                if (progress != null)
                                {
                                    progress.SetText(cache.FileInfo.ToVisibleName());
                                }
                                try
                                {
                                    cache.Reload();
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                                if (progress != null)
                                {
                                    progress.Add(1);
                                }
                            }
                            catch (Exception err)
                            {
                                if (cache.IgnoreError)
                                    log.Error(err);
                                else
                                    throw new Exception($"Load Template Error ! >>>{cache}<<< {err.Message}", err);
                            }

                        }));
                    }
                    Task.WhenAll(tasks).Wait();
                }
                else
                {
                    foreach (var cache in caches)
                    {
                        try
                        {
                            if (progress != null)
                            {
                                progress.SetText(cache.FileInfo.ToVisibleName());
                            }
                            if (Verbose) log.InfoFormat($"Reload : {cache}");
                            cache.Reload();
                            if (progress != null)
                            {
                                progress.Add(1);
                            }
                        }
                        catch (Exception err)
                        {
                            if (cache.IgnoreError)
                                log.Error(err);
                            else
                                throw new Exception($"Load Template Error ! >>>{cache}<<< {err.Message}", err);
                        }
                    }
                }
                FireEvents(progress, this.DataRoot, caches);
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
            finally
            {
                log.Info($"Reload Comlete Use : {stopwatch.Elapsed}");
                stopwatch.Stop();
                LoadingThreadID = 0;
            }
        }
        protected async Task ReloadAsync(IRangeValue progress, params TableBase[] caches)
        {
            var stopwatch = Stopwatch.StartNew();
            LoadingThreadID = Thread.CurrentThread.ManagedThreadId;
            try
            {
                if (progress != null)
                {
                    progress.SetRange(0, caches.Length, 0);
                }
                if (ENABLE_BATCH_LOAD)
                {
                    var tasks = new List<Task>();
                    foreach (var cache in caches)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                semaphore.WaitOne();
                                if (progress != null)
                                {
                                    progress.SetText(cache.FileInfo.ToVisibleName());
                                }
                                if (Verbose) log.InfoFormat("Reload : {0}", cache);
                                try
                                {
                                    await cache.ReloadAsync();
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                                if (progress != null)
                                {
                                    progress.SetText(cache.FileInfo.ToVisibleName());
                                    progress.Add(1);
                                }
                            }
                            catch (Exception err)
                            {
                                if (cache.IgnoreError)
                                    log.Error(err);
                                throw new Exception($"Load Template Error ! >>>{cache}<<< {err.Message}", err);
                            }
                        }));
                        await Task.WhenAll(tasks);
                    }
                }
                else
                {
                    foreach (var cache in caches)
                    {
                        try
                        {
                            if (progress != null)
                            {
                                progress.SetText(cache.FileInfo.ToVisibleName());
                            }
                            if (Verbose) log.InfoFormat("Reload : {0}", cache);
                            await cache.ReloadAsync();
                            if (progress != null)
                            {
                                progress.Add(1);
                            }
                        }
                        catch (Exception err)
                        {
                            if (cache.IgnoreError)
                                log.Error(err);
                            else
                                throw new Exception($"Load Template Error ! >>>{cache}<<< {err.Message}", err);
                        }
                    }
                }
                FireEvents(progress, this.DataRoot, caches);
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
            finally
            {
                log.Info($"ReloadAsync Comlete Use : {stopwatch.Elapsed}");
                stopwatch.Stop();
                LoadingThreadID = 0;
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public Task ReloadAllAsync(IRangeValue progress = null)
        {
            return ReloadAsync(progress, templatesList.ToArray());
        }
        public void ReloadAll(IRangeValue progress = null)
        {
            Reload(progress, templatesList.ToArray());
        }
        public void Reload(params TableBase[] caches)
        {
            Reload(null, caches);
        }
        public Task ReloadAsync(params TableBase[] caches)
        {
            return ReloadAsync(null, caches);
        }
        public void Cleanup()
        {
            TableBase templateCache = null;
            try
            {
                foreach (var cache in templatesList)
                {
                    if (Verbose) log.InfoFormat("Clean : {0}", cache);
                    templateCache = cache;
                    cache.Clear();
                }
            }
            catch (Exception err)
            {
                throw new Exception($"Cleanup Template Error ! >>>{templateCache}<<< {err.Message}", err);
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public string ToFullPath(string xlsFile)
        {
            if (TyGetFullPath != null && TyGetFullPath(DataRoot, xlsFile, out var fullPath))
            {
                return fullPath;
            }
            return Resource.FormatPath($"{DataRoot}/{xlsFile}");
        }
        public string ToSubPath(string xlsFile)
        {
            if (xlsFile.StartsWith(DataRoot))
            {
                return xlsFile.Substring(DataRoot.Length);
            }
            return xlsFile;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public TableBase<K, T> ListenSheet<K, T>(string xlsFile, string sheetName, string keyField, Action<TableBase<K, T>> onLoaded, TableBase<K, T> _ = null) where T : new()
        {
            var cache = ListenSheet<K, T>(xlsFile, sheetName, keyField);
            cache.OnLoaded += ((e) => onLoaded(e as TableBase<K, T>));
            return cache;
        }
        public TableBase<K, T> ListenSheets<K, T>(string xlsFile, string[] sheetName, string keyField, Action<TableBase<K, T>> onLoaded, TableBase<K, T> _ = null) where T : new()
        {
            var cache = ListenSheets<K, T>(xlsFile, sheetName, keyField);
            cache.OnLoaded += ((e) => onLoaded(e as TableBase<K, T>));
            return cache;
        }
        public TableBase<K, T> Listen<K, T>(string xlsFile, string keyField, Action<TableBase<K, T>> onLoaded, TableBase<K, T> _ = null) where T : new()
        {
            var cache = Listen<K, T>(xlsFile, keyField);
            cache.OnLoaded += ((e) => onLoaded(e as TableBase<K, T>));
            return cache;
        }
        public TableBase<K, T> Listen<K, T>(CacheFileInfo xlsFile, string keyField, Action<TableBase<K, T>> onLoaded, TableBase<K, T> _ = null) where T : new()
        {
            var cache = Listen<K, T>(xlsFile, keyField);
            cache.OnLoaded += ((e) => onLoaded(e as TableBase<K, T>));
            return cache;
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public TableBase<K, T> ListenSheet<K, T>(string xlsFile, string sheetName, string keyField, TableBase<K, T> _ = null) where T : new()
        {
            return InternalListenMap<K, T>(new CacheFileInfo(new XlsFileInfo() { FileName = xlsFile, Sheets = [sheetName] }), keyField);
        }
        public TableBase<K, T> ListenSheets<K, T>(string xlsFile, string[] sheetName, string keyField, TableBase<K, T> _ = null) where T : new()
        {
            return InternalListenMap<K, T>(new CacheFileInfo(new XlsFileInfo() { FileName = xlsFile, Sheets = sheetName }), keyField);
        }
        public TableBase<K, T> Listen<K, T>(string xlsFile, string keyField, TableBase<K, T> _ = null) where T : new()
        {
            return InternalListenMap<K, T>(new CacheFileInfo(new XlsFileInfo() { FileName = xlsFile, }), keyField);
        }
        public TableBase<K, T> Listen<K, T>(string[] files, string keyField, TableBase<K, T> _ = null) where T : new()
        {
            return InternalListenMap<K, T>(new CacheFileInfo(Array.ConvertAll(files, f => new XlsFileInfo(f))), keyField);
        }
        public TableBase<K, T> Listen<K, T>(XlsFileInfo[] files, string keyField, TableBase<K, T> _ = null) where T : new()
        {
            return InternalListenMap<K, T>(new CacheFileInfo(files), keyField);
        }
        public TableBase<K, T> Listen<K, T>(CacheFileInfo files, string keyField, TableBase<K, T> _ = null) where T : new()
        {
            return InternalListenMap<K, T>(files, keyField);
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public ListTableBase<T> ListenSheetList<T>(string xlsFile, string sheetName, Action<ListTableBase<T>> onLoaded, ListTableBase<T> _ = null) where T : new()
        {
            var cache = ListenSheetList<T>(xlsFile, sheetName);
            cache.OnLoaded += ((e) => onLoaded(e as ListTableBase<T>));
            return cache;
        }
        public ListTableBase<T> ListenSheetsList<T>(string xlsFile, string[] sheetName, Action<ListTableBase<T>> onLoaded, ListTableBase<T> _ = null) where T : new()
        {
            var cache = ListenSheetsList<T>(xlsFile, sheetName);
            cache.OnLoaded += ((e) => onLoaded(e as ListTableBase<T>));
            return cache;
        }
        public ListTableBase<T> ListenList<T>(string xlsFile, Action<ListTableBase<T>> onLoaded, ListTableBase<T> _ = null) where T : new()
        {
            var cache = ListenList<T>(xlsFile);
            cache.OnLoaded += ((e) => onLoaded(e as ListTableBase<T>));
            return cache;
        }
        public ListTableBase<T> ListenList<T>(CacheFileInfo xlsFile, Action<ListTableBase<T>> onLoaded, ListTableBase<T> _ = null) where T : new()
        {
            var cache = ListenList<T>(xlsFile);
            cache.OnLoaded += ((e) => onLoaded(e as ListTableBase<T>));
            return cache;
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public ListTableBase<T> ListenSheetList<T>(string xlsFile, string sheetName, ListTableBase<T> _ = null) where T : new()
        {
            return InternalListenList<T>(new CacheFileInfo(new XlsFileInfo() { FileName = xlsFile, Sheets = [sheetName] }));
        }
        public ListTableBase<T> ListenSheetsList<T>(string xlsFile, string[] sheetName, ListTableBase<T> _ = null) where T : new()
        {
            return InternalListenList<T>(new CacheFileInfo(new XlsFileInfo() { FileName = xlsFile, Sheets = sheetName }));
        }
        public ListTableBase<T> ListenList<T>(string xlsFile, ListTableBase<T> _ = null) where T : new()
        {
            return InternalListenList<T>(new CacheFileInfo(new XlsFileInfo() { FileName = xlsFile, }));
        }
        public ListTableBase<T> ListenList<T>(string[] files, ListTableBase<T> _ = null) where T : new()
        {
            return InternalListenList<T>(new CacheFileInfo(Array.ConvertAll(files, f => new XlsFileInfo(f))));
        }
        public ListTableBase<T> ListenList<T>(XlsFileInfo[] files, ListTableBase<T> _ = null) where T : new()
        {
            return InternalListenList<T>(new CacheFileInfo(files));
        }
        public ListTableBase<T> ListenList<T>(CacheFileInfo files, ListTableBase<T> _ = null) where T : new()
        {
            return InternalListenList<T>(files);
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        protected virtual TableBase<K, T> InternalListenMap<K, T>(CacheFileInfo files, string keyField) where T : new()
        {
            var key = $"{files}";
            var cache = new TableBase<K, T>(this, key, keyField, files);
            RegistTable(cache);
            return cache;
        }
        protected virtual ListTableBase<T> InternalListenList<T>(CacheFileInfo files) where T : new()
        {
            var key = $"{files}";
            var cache = new ListTableBase<T>(this, key, files);
            RegistTable(cache);
            return cache;
        }
        public virtual TableBase ListenWithType(CacheFileInfo files, Type valueType, string keyField)
        {
            var key = $"{files}";
            var keyType = valueType.GetField(keyField).FieldType;
            var tableType = typeof(TableBase<,>).MakeGenericType(keyType, valueType);
            var cache = (TableBase)DeepActivator.CreateInstance(tableType,
                new object[] { this, key, keyField, files });
            RegistTable(cache);
            return cache;
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public void RegistTable(TableBase cache)
        {
            try
            {
                templatesMap.Add(cache.Name, cache);
                var tmap = templatesTypeMap.GetOrNew(cache.DataType);
                templatesList.Add(cache);
                tmap.Add(cache.Name, cache);
            }
            catch (Exception err)
            {
                log.Error($"表被重复监听！！！{cache.Name}", err);
                throw;
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------

        public TableBase GetTable(string name)
        {
            return templatesMap.Get(name);
        }
        public TableBase<K, T> GetTable<K, T>(string name) where T : new()
        {
            if (templatesTypeMap.TryGetValue(typeof(T), out var tmap))
            {
                return tmap.Get(name) as TableBase<K, T>;
            }
            return null;
        }
        public TableBase<K, T> GetTable<K, T>() where T : new()
        {
            if (templatesTypeMap.TryGetValue(typeof(T), out var tmap))
            {
                return tmap.First().Value as TableBase<K, T>;
            }
            return null;
        }
        public bool TryGetData<K, T>(K key, out T value) where T : new()
        {
            if (templatesTypeMap.TryGetValue(typeof(T), out var tmap))
            {
                foreach (var t in tmap.Values)
                {
                    if (t is TableBase<K, T> table)
                    {
                        if (table.TryGetValue(key, out value))
                        {
                            return true;
                        }
                    }
                }
            }
            value = default;
            return false;
        }
        public T GetData<K, T>(K key) where T : new()
        {
            if (TryGetData(key, out T v))
            {
                return v;
            }
            return default;
        }
        public List<T> GetAllData<T>() where T : new()
        {
            var ret = new List<T>();
            if (templatesTypeMap.TryGetValue(typeof(T), out var tmap))
            {
                foreach (var t in tmap.Values)
                {
                    if (t is TableBase table)
                    {
                        foreach (var e in table.Datas)
                        {
                            ret.Add((T)e);
                        }
                    }
                }
            }
            return ret;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public FileSystemInfo GetFileSystemInfo(CacheFileInfo file)
        {
            if (file.XlsFiles == null) { return null; }
            else if (file.XlsFiles.Count == 0) { return null; }
            else if (file.XlsFiles.Count == 1)
            {
                var fullPath = ToFullPath(file.XlsFiles[0].FileName);
                if (Directory.Exists(fullPath))
                {
                    return new DirectoryInfo(fullPath);
                }
                else if (File.Exists(fullPath))
                {
                    return new FileInfo(fullPath);
                }
                return null;
            }
            else
            {
                var fullPath = ToFullPath(file.XlsFiles[0].FileName);
                if (Directory.Exists(fullPath))
                {
                    return new DirectoryInfo(fullPath).Parent;
                }
                else if (File.Exists(fullPath))
                {
                    return new FileInfo(fullPath).Directory;
                }
                return null;
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------
        #region Lang
        private DeepCore.Properties langProperties = null;
        private int langArrayStartIndex = 1;
        public DeepCore.Properties LangProperties => langProperties;
        public virtual void SetLangProperties(DeepCore.Properties lang, int arrayStartIndex = 1)
        {
            this.langProperties = lang;
            this.langArrayStartIndex = arrayStartIndex;
        }
        internal protected virtual void BeginFillLangFields(TableBase table, string fileName, string sheetName, object keyValue, object data)
        {
            if (langProperties != null)
            {
                try
                {
                    foreach (var f in table.DataType.GetFields())
                    {
                        if (f.IsStatic == false && f.IsPublic)
                        {
                            var langKey = ToLangKey(table, fileName, sheetName, keyValue, f);
                            if (f.FieldType == typeof(string))
                            {
                                if (langProperties.TryGetValue(langKey, out var lang) && !string.IsNullOrEmpty(lang))
                                {
                                    f.SetValue(data, lang);
                                }
                            }
                            else if (f.FieldType.IsArray && f.FieldType.GetElementType() == typeof(string))
                            {
                                var array = (Array)f.GetValue(data);
                                if (array != null)
                                {
                                    for (int i = 0; i < array.GetLength(0); i++)
                                    {
                                        langKey = ToLangKey(table, fileName, sheetName, keyValue, f, langArrayStartIndex + i);
                                        if (langProperties.TryGetValue(langKey, out var lang) && !string.IsNullOrEmpty(lang))
                                        {
                                            array.SetValue(lang, i);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"填充语言表失败在：{table} File:{fileName} Sheet:{sheetName} Key:{keyValue}", ex);
                }
            }
        }
        public virtual string ToLangKey(TableBase table, string fileName, string sheetName, object keyValue, FieldInfo field, object elementIndex = null)
        {
            if (elementIndex != null && field.FieldType.IsArray)
            {
                return $"{Path.GetFileName(fileName)}/{sheetName}/{keyValue}.{field.Name}[{elementIndex}]";
            }
            return $"{Path.GetFileName(fileName)}/{sheetName}/{keyValue}.{field.Name}";
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        #region JsonBin

        public BinaryTemplateLoader BinDataLoader { get; private set; }
        internal void LoadTemplates(string xlsFile, string[] sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData onLoad)
        {
            if (BinDataLoader != null)
            {
                try
                {
                    if (BinDataLoader.TryLoadTemplates(this, xlsFile, sheetName, dataType, keyField, keyType, createNew, onLoad))
                    {
                        return;
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                    log.Error($"RETRIEVAL SRC [{CUtils.ArrayToString(sheetName)}]");
                }
            }
            DataLoader?.LoadTemplates(this, xlsFile, sheetName, dataType, keyField, keyType, createNew, onLoad);
        }
        internal async Task LoadTemplatesAsync(string xlsFile, string[] sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData onLoad)
        {
            try
            {
                if (BinDataLoader != null)
                {
                    if (await BinDataLoader.TryLoadTemplatesAsync(this, xlsFile, sheetName, dataType, keyField, keyType, createNew, onLoad))
                    {
                        return;
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err);
                log.Error($"RETRIEVAL SRC [{CUtils.ArrayToString(sheetName)}]");
            }
            await DataLoader?.LoadTemplatesAsync(this, xlsFile, sheetName, dataType, keyField, keyType, createNew, onLoad);
        }

        public void SaveToBin(IRangeValue progress = null)
        {
            SaveToBin(this.Tables, progress);
        }
        public void SaveToBin(IEnumerable<TableBase> tables, IRangeValue progress = null)
        {
            progress?.SetRange(0, tables.Count(), 0);
            if (this.DataLoader != null)
            {
                var bin = new BinaryTemplateLoader(Codec, this.DataLoader, false);
                bin.SaveToBin(tables, progress);
            }
        }
        //         public void EnableLoadFromBin()
        //         {
        //             this.BinDataLoader = new BinaryTemplateLoader(Codec, JSON_BIN_SUFFIX, false);
        //         }

        #endregion        
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        #region Modules

        private HashMap<Type, DataCenterModule> modules = new();
        private void RegistModules()
        {
            foreach (var field in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var mt = field.FieldType;
                if (typeof(DataCenterModule).IsAssignableFrom(mt))
                {
                    var m = field.GetValue(this);
                    if (m is DataCenterModule dm)
                    {
                        log.Info("Regist Module : " + mt.FullName);
                        try
                        {
                            this.RegistModule(dm);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Regist Module '{mt}' Error : {ex.Message}", ex);
                        }
                    }
                }
            }
            foreach (var field in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var mt = field.PropertyType;
                if (typeof(DataCenterModule).IsAssignableFrom(mt))
                {
                    var m = field.GetValue(this);
                    if (m is DataCenterModule dm)
                    {
                        log.Info("Regist Module : " + mt.FullName);
                        try
                        {
                            this.RegistModule(dm);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Regist Module '{mt}' Error : {ex.Message}", ex);
                        }
                    }
                }
            }
        }
        public DataCenterModule RegistModule(DataCenterModule module)
        {
            modules.Add(module.GetType(), module);
            module.InternalRegist(this);
            return module;
        }
        public M RegistModule<M>(M module) where M : DataCenterModule
        {
            return RegistModule(module);
        }
        public M RegistModule<M>() where M : DataCenterModule, new()
        {
            var m = new M();
            return RegistModule(m);
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public object GetKey(object data, string keyField)
        {
            if (data == null) return null;
            if (string.IsNullOrEmpty(keyField)) return null;
            try
            {
                var dataType = data.GetType();
                var f = dataType.GetField(keyField);
                return f.GetValue(data);
            }
            catch
            {
                throw;
            }
        }
        public void SetKey(object data, string keyField, object keyValue)
        {
            if (data == null) return;
            if (string.IsNullOrEmpty(keyField)) return;
            try
            {
                var dataType = data.GetType();
                var f = dataType.GetField(keyField);
                f.SetValue(data, keyValue);
            }
            catch
            {
                throw;
            }
        }
        public void SortTemplateList<T>(List<T> ret, string keyField)
        {
            var dataType = typeof(T);
            if (typeof(IComparable<T>).IsAssignableFrom(dataType))
            {
                ret.Sort((x, y) =>
                {
                    var ca = x as IComparable<T>;
                    var cb = y as IComparable<T>;
                    return ca.CompareTo(y);
                });
            }
            else if (typeof(IComparable).IsAssignableFrom(dataType))
            {
                ret.Sort((x, y) =>
                {
                    var ca = x as IComparable;
                    var cb = y as IComparable;
                    return ca.CompareTo(y);
                });
            }
            else if (!string.IsNullOrEmpty(keyField))
            {
                var f = dataType.GetField(keyField);
                if (f.FieldType == (typeof(int)))
                {
                    ret.Sort((x, y) =>
                    {
                        var o1 = f.GetValue(x);
                        var o2 = f.GetValue(y);
                        var i1 = (int)o1;
                        var i2 = (int)o2;
                        return i1.CompareTo(i2);
                    });
                }
                else if (f.FieldType == (typeof(string)))
                {
                    ret.Sort((x, y) =>
                    {
                        var o1 = f.GetValue(x);
                        var o2 = f.GetValue(y);
                        if (o1 == null)
                        {
                            return 1;
                        }
                        if (o2 == null)
                        {
                            return -1;
                        }
                        var i1 = (string)o1;
                        var i2 = (string)o2;
                        return i1.CompareTo(i2);
                    });
                }
            }
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 注册函数自动加载
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RegistTableActionAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class RegistOnLoadActionAttribute : Attribute
    {
    }

    public abstract class DataCenterModule
    {
        private TemplateDataCenter dc;
        public TemplateDataCenter Owner => dc;
        public Logger log => dc?.log;
        internal void InternalRegist(TemplateDataCenter dc)
        {
            this.dc = dc;
            Regist(dc);
        }
        protected abstract void Regist(TemplateDataCenter dc);
    }
    public abstract class DataCenterModule<DC> : DataCenterModule where DC : TemplateDataCenter
    {
        sealed protected override void Regist(TemplateDataCenter dc)
        {
            this.Regist(dc as DC);
        }
        protected abstract void Regist(DC dc);
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------

    public abstract class TableBase : IEnumerable
    {
        public readonly TemplateDataCenter DataCenter;
        public string Name { get; }
        public string KeyFieldName { get; }
        public CacheFileInfo FileInfo { get; }
        public Type KeyType { get; }
        public Type DataType { get; }
        public FieldInfo KeyField { get; }
        public abstract IDictionaryEnumerator DataMap { get; }
        public abstract IEnumerable Datas { get; }
        IEnumerator IEnumerable.GetEnumerator() => Datas.GetEnumerator();
        public abstract int DataCount { get; }
        public bool IgnoreError { get; set; } = false;
        protected internal Logger log { get => DataCenter.log; }

        public event OnTableLoadedDelegate OnLoaded;
        //public event OnDataLoadedDelegate OnDataLoaded;

        internal TableBase(TemplateDataCenter datacenter, Type keyType, Type dataType, string keyField, string key, CacheFileInfo file)
        {
            this.DataCenter = datacenter;
            this.FileInfo = file;
            this.Name = key;
            this.DataType = dataType;
            this.KeyType = keyType;
            this.KeyFieldName = keyField;
            this.KeyField = string.IsNullOrEmpty(keyField) ? null : DataType.GetField(keyField);
        }

        public override string ToString()
        {
            return $"TableBase : {Name}";
        }
        internal protected void FireEvents()
        {
            try
            {
                this.OnLoaded?.Invoke(this);
            }
            catch (Exception err)
            {
                DataCenter.log.Error(err);
                throw (new System.Exception(err.Message + Environment.NewLine + ToString(), err));
            }
        }
        public abstract object NewData(Type type);
        public abstract void Clear();

        protected abstract void OnBeginReload();
        protected abstract void OnTemplateDataLoaded(string fileName, string sheetName, object keyValue, object data);
        protected abstract void OnEndReload();

        public abstract void ForEachDatas<ST>(ST st, ForEachDataAction<ST> action);
        public abstract void ForEachSheets<ST>(ST st, ForEachSheetAction<ST> action);
        public object GetKey(object data)
        {
            try
            {
                return KeyField?.GetValue(data);
            }
            catch { return null; }
        }
        public void Reload()
        {
            OnBeginReload();
            foreach (var file in FileInfo.XlsFiles)
            {
                var fullPath = DataCenter.ToFullPath(file.FileName);
                DataCenter.LoadTemplates(fullPath, file.Sheets, DataType, KeyFieldName, KeyType, NewData, TemplateDataLoaded);
            }
            OnEndReload();
        }
        public async Task ReloadAsync()
        {
            OnBeginReload();
            foreach (var file in FileInfo.XlsFiles)
            {
                var fullPath = DataCenter.ToFullPath(file.FileName);
                await DataCenter.LoadTemplatesAsync(fullPath, file.Sheets, DataType, KeyFieldName, KeyType, NewData, TemplateDataLoaded);
            }
            OnEndReload();
        }
        public void Reload(TableBaseMeta meta)
        {
            OnBeginReload();
            foreach (var file in meta.Files)
            {
                var fileName = DataCenter.ToFullPath(file.Key);
                foreach (var sheet in file.Value.Sheets)
                {
                    var sheetName = sheet.Key;
                    var datas = sheet.Value;
                    foreach (var data in sheet.Value.Datas)
                    {
                        TemplateDataLoaded(fileName, sheetName, data.Key, data.Value);
                    }
                }
            }
            OnEndReload();
        }

        public TableBaseMeta ToMeta()
        {
            var meta = new TableBaseMeta();
            meta.TableName = this.Name;
            this.ForEachDatas(meta, (meta, fileName, sheetName, key, data) =>
            {
                var file = DataCenter.ToSubPath(fileName);
                var filemap = meta.Files.GetOrNew(file);
                filemap.FileName = file;
                var datamap = filemap.Sheets.GetOrNew(sheetName);
                datamap.SheetName = sheetName;
                datamap.Datas.Add(key, data);
            });
            return meta;
        }


        private void TemplateDataLoaded(string fileName, string sheetName, object keyValue, object data)
        {
            DataCenter.BeginFillLangFields(this, fileName, sheetName, keyValue, data);
            OnTemplateDataLoaded(fileName, sheetName, keyValue, data);
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------
    public class TableBase<K, T> : TableBase, IEnumerable<T>, IReadOnlyDictionary<K, T> where T : new()
    {
        protected readonly ListDictionary<string, ListDictionary<string, ArrayList<T>>> LoadedSheetsList;
        protected readonly HashMap<K, T> LoadedDatas;
        protected readonly ArrayList<T> LoadedDatasList;
        public IReadOnlyDictionary<K, T> TemplatesMap => LoadedDatas;
        public IReadOnlyList<T> TemplatesList => LoadedDatasList;
        public T First { get => LoadedDatasList.Count > 0 ? LoadedDatasList[0] : default(T); }
        public override IDictionaryEnumerator DataMap { get => LoadedDatas.GetEnumerator(); }
        public override IEnumerable Datas { get => LoadedDatasList; }
        public override int DataCount { get => LoadedDatasList.Count; }
        public IEnumerable<K> Keys => LoadedDatas.Keys;
        public IEnumerable<T> Values => LoadedDatas.Values;
        public int Count => LoadedDatas.Count;
        public T this[K key] => LoadedDatas[key];
        public TableBase(TemplateDataCenter datacenter, string key, string keyField, CacheFileInfo file)
            : base(datacenter, typeof(K), typeof(T), keyField, key, file)
        {
            this.LoadedSheetsList = new();
            this.LoadedDatas = new HashMap<K, T>();
            this.LoadedDatasList = new ArrayList<T>();
        }
        public override void Clear()
        {
            this.LoadedDatas.Clear();
            this.LoadedDatasList.Clear();
            this.LoadedSheetsList.Clear();
        }
        public void Sort(Comparison<T> comparison)
        {
            if (LoadedDatasList != null)
            {
                LoadedDatasList.Sort(comparison);
            }
            if (LoadedSheetsList != null)
            {
                foreach (var files in LoadedSheetsList.Values)
                {
                    foreach (var sheet in files.Values)
                    {
                        sheet.Sort(comparison);
                    }
                }
            }
        }
        public override object NewData(Type type) => new T();
        protected override void OnBeginReload()
        {
            LoadedDatas.Clear();
            LoadedDatasList.Clear();
            LoadedSheetsList.Clear();
        }
        protected override void OnTemplateDataLoaded(string fileName, string sheetName, object keyValue, object data)
        {
            LoadedDatasList.Add((T)data);
            LoadedSheetsList.GetOrNew(fileName).GetOrNew(sheetName ?? string.Empty).Add((T)data);
            if (KeyField != null && keyValue != null)
            {
                K key = default;
                try
                {
                    key = CUtils.ConvertTo<K>(keyValue);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{Name} : Error converting key value '{keyValue}' to type '{typeof(K)}' in file: {fileName}, sheet: {sheetName}", ex);
                }
                if (LoadedDatas.ContainsKey(key))
                {
                    if (DataCenter.EnableDuplicateKey)
                    {
                        if (DataCenter.Verbose) log.Warn($"{Name} : Duplicate key found! Key: {key} in file: {fileName}, sheet: {sheetName}");
                    }
                    else
                    {
                        throw new Exception($"{Name} : Duplicate key found! Key: {key} in file: {fileName}, sheet: {sheetName}");
                    }
                }
                LoadedDatas.Put(key, (T)data);
            }
        }
        protected override void OnEndReload()
        {
            if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)) || typeof(IComparable).IsAssignableFrom(typeof(T)))
            {
                DataCenter.SortTemplateList(LoadedDatasList, KeyFieldName);
                foreach (var file in LoadedSheetsList)
                {
                    foreach (var sheet in file.Value)
                    {
                        DataCenter.SortTemplateList(sheet.Value, KeyFieldName);
                    }
                }
            }
            DataCenter.DataLoader?.GC();
        }
        public override void ForEachDatas<ST>(ST st, ForEachDataAction<ST> action)
        {
            int i = 0;
            foreach (var file in LoadedSheetsList)
            {
                foreach (var sheet in file.Value)
                {
                    foreach (var data in sheet.Value)
                    {
                        action(st, file.Key, sheet.Key, GetKey(data) ?? i, data);
                        i++;
                    }
                }
            }
        }
        public override void ForEachSheets<ST>(ST st, ForEachSheetAction<ST> action)
        {
            foreach (var file in LoadedSheetsList)
            {
                foreach (var sheet in file.Value)
                {
                    action(st, file.Key, sheet.Key, sheet.Value);
                }
            }
        }
        public void ForEachDatas<ST>(ST st, ForEachDataAction<ST, T> action)
        {
            int i = 0;
            foreach (var file in LoadedSheetsList)
            {
                foreach (var sheet in file.Value)
                {
                    foreach (var data in sheet.Value)
                    {
                        action(st, file.Key, sheet.Key, GetKey(data) ?? i, data);
                        i++;
                    }
                }
            }
        }
        public void ForEachSheets<ST>(ST st, ForEachSheetAction<ST, T> action)
        {
            foreach (var file in LoadedSheetsList)
            {
                foreach (var sheet in file.Value)
                {
                    action(st, file.Key, sheet.Key, sheet.Value);
                }
            }
        }

        public bool TryGetSheetList(string sheetName, List<T> sheets)
        {
            foreach (var file in LoadedSheetsList)
            {
                if (file.Value.TryGetValue(sheetName, out var sheet))
                {
                    sheets.AddRange(sheet);
                }
            }
            return sheets.Count > 0;
        }
        public List<T> GetSheetList(string sheetName)
        {
            var sheets = new List<T>();
            TryGetSheetList(sheetName, sheets);
            return sheets;
        }

        //----------------------------------------------------------------------------------------------------------------
        public T Get(K key)
        {
            if (this.TryGetValue(key, out var value))
            {
                return value;
            }
            return default;
        }
        public bool TryGetValue(K key, out T value)
        {
            if (this.TemplatesMap.TryGetValue(key, out value))
            {
                return true;
            }
            else
            {
                //log.Warn($"{this}.TryGetValue({key}) Not Exist !");
                return false;
            }
        }
        public bool TryGetNext(T value, out int nextIndex, out T next)
        {
            if (this.TemplatesList.TryIndexOf(value, out var index))
            {
                nextIndex = index + 1;
                if (nextIndex < TemplatesList.Count)
                {
                    next = TemplatesList[nextIndex];
                    return true;
                }
            }
            else
            {
                //log.Warn($"{this}.TryGetNext({value}) Not Exist !");
            }
            nextIndex = -1;
            next = default;
            return false;
        }
        public bool ContainsKey(K key)
        {
            return LoadedDatas.ContainsKey(key);
        }
        IEnumerator<KeyValuePair<K, T>> IEnumerable<KeyValuePair<K, T>>.GetEnumerator() => LoadedDatas.GetEnumerator();
        public IEnumerator<T> GetEnumerator() => LoadedDatasList.GetEnumerator();
        //----------------------------------------------------------------------------------------------------------------
    }
    public class ListTableBase<T> : TableBase<int, T> where T : new()
    {
        public ListTableBase(TemplateDataCenter datacenter, string key, CacheFileInfo file) : base(datacenter, key, string.Empty, file)
        {
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------
}