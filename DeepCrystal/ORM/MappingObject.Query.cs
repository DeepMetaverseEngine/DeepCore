using DeepCore;
using DeepCore.ORM;
using DeepCore.Threading;
using DeepCrystal.ORM.Generic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCrystal.ORM.Query
{
    //-----------------------------------------------------------------------------------------------------------------------------

    public class QueryMapping : AsyncDisposable
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder($"ORM:QueryMapping");
        protected readonly string prefixName;
        protected readonly Type type;
        protected readonly IMappingAdapter adapter;
        protected readonly IMappingDatabase db;
        protected readonly ITaskExecutor exe;
        public QueryMapping(Type typeName, string prefix, ITaskExecutor exe = null, IMappingAdapter ad = null)
        {
            Alloc.RecordConstructor(typeName);
            this.type = typeName;
            this.prefixName = prefix;
            this.exe = exe ?? ITaskExecutor.Default;
            this.adapter = ad != null ? ad : ORMFactory.Instance.DefaultAdapter;
            this.db = adapter.CreateExecutableDatabase(exe);
        }
        ~QueryMapping()
        {
            try
            {
                this.Dispose();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message + Environment.NewLine + err.StackTrace);
            }
            finally
            {
                Alloc.RecordDestructor(type);
            }
        }
        protected sealed override void RecordDisposing()
        {
            Alloc.RecordDispose(type);
        }
        protected override void Disposing()
        {
            db.Dispose();
        }
        protected override async ValueTask DisposingAsync()
        {
            await db.DisposeAsync();
        }
    }

    //-----------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 用于批量查询
    /// </summary>
    public class QueryMappingReference<T> : QueryMapping where T : IObjectMapping, new()
    {
        public QueryMappingReference(ITaskExecutor exe = null, IMappingAdapter ad = null) : this(string.Empty, exe, ad) { }
        public QueryMappingReference(string prefix, ITaskExecutor exe = null, IMappingAdapter ad = null) : base(typeof(T), prefix, exe, ad)
        {
            this.caches = new(static (a, b) => (int)(b.UpdateTimeUTC.Ticks - a.UpdateTimeUTC.Ticks));
        }
        protected override void Disposing()
        {
            Cleanup();
            base.Disposing();
        }
        protected override async ValueTask DisposingAsync()
        {
            Cleanup();
            await base.DisposingAsync();
        }
        public void Cleanup()
        {
            try
            {
                lock (caches)
                {
                    foreach (var ch in caches)
                    {
                        ch.Value.Dispose();
                    }
                    caches.Clear();
                }
            }
            catch { }
        }
        protected virtual async Task<T> LoadDataInternalAsync(string key)
        {
            var mapping = CachePop(key);
            try
            {
                var ret = await mapping.LoadDataAsync();
                return ret;
            }
            finally
            {
                CacheRecycle(key, mapping);
            }
        }
        public virtual async Task<T> LoadDataAsync(string key)
        {
            if (key == null) return (default(T));
            key = prefixName == null ? key : (prefixName + key);
            return await LoadDataInternalAsync(key);
        }
        public virtual async Task<T[]> LoadManyAsync(string[] keys)
        {
            var ret = new T[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var data = await this.LoadDataAsync(keys[i]);
                ret[i] = data;
            }
            return ret;
        }
        public virtual async Task<HashMap<string, T>> LoadManyMapAsync(string[] keys)
        {
            var ret = new HashMap<string, T>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                var data = await this.LoadDataAsync(keys[i]);
                ret.Add(keys[i], data);
            }
            return ret;
        }


        #region Cache

        protected int cacheLimit = 10;
        protected ValueSortedMap<string, MappingReference<T>> caches;
        public virtual int CacheLimit
        {
            get => cacheLimit;
            set
            {
                lock (caches)
                {
                    if (cacheLimit != value)
                    {
                        cacheLimit = value;
                        if (cacheLimit > 0)
                        {
                            while (caches.Count >= cacheLimit && caches.TryPopLast(out var last))
                            {
                                last.Value.Dispose();
                            }
                        }
                        else
                        {
                            foreach (var ch in caches)
                            {
                                ch.Value.Dispose();
                            }
                            caches.Clear();
                        }
                    }
                }
            }
        }
        protected virtual MappingReference<T> CachePop(string key)
        {
            try
            {
                lock (caches)
                {
                    if (cacheLimit > 0)
                    {
                        if (caches.TryRemove(key, out var mapping))
                        {
                            return mapping;
                        }
                        mapping = new MappingReference<T>(key, exe, adapter) { IsReadOnly = true };
                        return mapping;
                    }
                    else
                    {
                        return new MappingReference<T>(key, exe, adapter) { IsReadOnly = true };
                    }
                }
            }
            catch (Exception ex)
            {
                ex.PrintStackTrace();
                return new MappingReference<T>(key, exe, adapter) { IsReadOnly = true };
            }
        }
        protected virtual void CacheRecycle(string key, MappingReference<T> mapping)
        {
            lock (caches)
            {
                while (caches.Count >= cacheLimit && caches.TryPopLast(out var last))
                {
                    last.Value.Dispose();
                }
                if (cacheLimit > 0)
                {
                    caches.Put(key, mapping);
                }
                else
                {
                    mapping.Dispose();
                }
            }
        }
        #endregion
    }

#if false

    /// <summary>
    /// 查询加缓存功能。
    /// 查询的数据如果在一定时间内如果过期，则数据的重新查询，否则从缓存直接返回。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CacheMappingReference<T> : QueryMappingReference<T> where T : IObjectMapping, new()
    {
        protected ConcurrentDictionary<string, MappingReference<T>> caches;
        protected TimeSpan timeout = TimeSpan.FromMinutes(5);
        public TimeSpan Timeout
        {
            get => timeout;
            set
            {
                if (value.TotalMinutes < 5)
                {
                    timeout = TimeSpan.FromMinutes(5);
                }
                else
                {
                    timeout = value;
                }
            }
        }
        public CacheMappingReference(string prefix, ITaskExecutor exe = null, IMappingAdapter ad = null)
            : base(prefix, exe, ad)
        {
        }
        public T GetCache(string key)
        {
            if (key == null) return default(T);
            key = prefixName == null ? key : (prefixName + key);
            if (caches.TryGetValue(key, out var mapping))
            {
                return (T)mapping.Data;
            }
            return default(T);
        }
        protected bool TryGetCache(string key, out MappingReference<T> ret)
        {
            ret = null;
            if (key == null) return false;
            key = prefixName == null ? key : (prefixName + key);
            if (caches.TryGetValue(key, out var mapping))
            {
                ret = (MappingReference<T>)mapping;
                return true;
            }
            return false;
        }
        protected virtual bool IsExpire(MappingReference<T> mapping)
        {
            //默认5分钟超时
            return (DateTime.UtcNow - mapping.UpdateTimeUTC) > timeout;
        }
        /// <summary>
        /// 获取数据，如果缓存没过期，则直接获取，如果缓存过期则重新加载
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<T> GetDataAsync(string key, bool reload = false)
        {
            if (key == null) return Task.FromResult(default(T));
            key = prefixName == null ? key : (prefixName + key);
            if (TryGetCache(key, out var mapping))
            {
                if (!reload && !IsExpire(mapping))
                {
                    return Task.FromResult(mapping.Data);
                }
                return LoadDataInternalAsync(mapping);
            }
            else
            {
                mapping = caches.GetOrAdd(key, (k) => new MappingReference<T>(key, exe, adapter) { IsReadOnly = true });
                return LoadDataInternalAsync(mapping);
            }
        }
        /// <summary>
        /// 获取数据，如果缓存没过期，则直接获取，如果缓存过期则重新加载
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<HashMap<string, T>> GetManyMapAsync(string[] keys, bool reload = false)
        {
            if (keys == null) return null;
            if (keys.Length == 0) return new HashMap<string, T>(0);
            var ret = new HashMap<string, T>(keys.Length);
            var queryList = new List<string>(keys);
            {
                if (!reload)
                {
                    for (int i = queryList.Count - 1; i >= 0; --i)
                    {
                        var key = queryList[i];
                        //如果存在缓存，并且缓存未过期
                        if (TryGetCache(key, out var mapping) && !IsExpire(mapping))
                        {
                            ret.Add(key, mapping.Data);
                            queryList.RemoveAt(i);
                        }
                    }
                }
                foreach (var key in queryList)
                {
                    var mapping = caches.GetOrAdd(key, (k) => new MappingReference<T>(prefixName == null ? k : (prefixName + k), exe, adapter) { IsReadOnly = true });
                    var data = await LoadDataInternalAsync(mapping);
                    ret.Add(key, data);
                }
            }
            return ret;
        }
        /// <summary>
        /// 获取数据，如果缓存没过期，则直接获取，如果缓存过期则重新加载
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="reload"></param>
        /// <returns></returns>
        public async Task<T[]> GetManyAsync(string[] keys, bool reload = false)
        {
            if (keys == null) return null;
            if (keys.Length == 0) return new T[0];
            var map = await GetManyMapAsync(keys, reload);
            var ret = new T[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                ret[i] = map.Get(keys[i]);
            }
            return ret;
        }
    }
#endif

}
