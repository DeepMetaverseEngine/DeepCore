using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCrystal.RPC
{
    /// <summary>
    /// 服务间共享内存
    /// </summary>
    public interface ISharedMemory
    {
        /// <summary>
        /// 字典类服务间共享内存，只可以存储可序列化对象，同进程可保证实时存取，跨进程不保证实时存取。
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        ISharedDictionary<string, V> GetDictionary<V>(string key);
    }

    /// <summary>
    /// 字典类服务间共享内存，只可以存储可序列化对象，同进程可保证实时存取，跨进程不保证实时存取。
    /// </summary>
    public interface ISharedDictionary : IDictionary
    {
        string DictionaryName { get; }
    }


    public delegate void SharedDictionaryValueChange<TKey,TValue>(ISharedDictionary<TKey, TValue> dict, TKey key);
    /// <summary>
    /// 字典类服务间共享内存，只可以存储可序列化对象，同进程可保证实时存取，跨进程不保证实时存取。
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface ISharedDictionary<TKey, TValue> : ISharedDictionary, IDictionary<TKey, TValue>
    {
        bool IsEmpty { get; }

        TValue Get(TKey key);

        void Subscribe(IService service, TKey key, SharedDictionaryValueChange<TKey, TValue> handler);
        void Subscribe(IService service,SharedDictionaryValueChange<TKey, TValue> handler);
        void Unsubscribe(SharedDictionaryValueChange<TKey, TValue> handler);
        void Unsubscribe(string key, SharedDictionaryValueChange<string, TValue> handler);
        TValue GetOrAdd(TKey key, TValue value);

        TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);

        KeyValuePair<TKey, TValue>[] ToArray();

        bool TryRemove(TKey key, out TValue value);

        void AddOrUpdate(TKey key, TValue value);

        TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory);

        TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory);

        bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue);
        void SetKeyDirty(TKey key);
#if REMOTE_SYNC
        /// <summary>
        /// 保证其他节点已同步
        /// </summary>
        Task AddAsync(TKey key, TValue value);
        /// <summary>
        /// 保证其他节点已同步
        /// </summary>
        Task<TValue> SetAsync(TKey key, TValue value);
        /// <summary>
        /// 保证其他节点已同步
        /// </summary>
        Task<TValue> GetOrAddAsync(TKey key, TValue value);
        /// <summary>
        /// 保证其他节点已同步
        /// </summary>
        Task<TValue> GetOrAddAsync(TKey key, Func<TKey, TValue> valueFactory);
        /// <summary>
        /// 保证其他节点已同步
        /// </summary>
        Task<TValue> RemoveAsync(TKey key);
        /// <summary>
        /// 保证其他节点已同步
        /// </summary>
        Task AddOrUpdateAsync(TKey key, TValue value);
        /// <summary>
        /// 保证其他节点已同步
        /// </summary>
        Task<TValue> AddOrUpdateAsync(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory);
        /// <summary>
        /// 保证其他节点已同步
        /// </summary>
        Task<TValue> AddOrUpdateAsync(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory);
#endif
    }
}