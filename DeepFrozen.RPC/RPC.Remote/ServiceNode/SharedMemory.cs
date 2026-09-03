using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCrystal.RPC;
using System.Threading;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Remote.ServiceNode
{
    [MessageType(0xFFFFFF0)]
    public class SyncShareDictMessage : IExternalizable
    {
        public string DictName;
        public string Field;
        public DataOperation Op;
        public object Value;

        public enum DataOperation : byte
        {
            Set,
            Remove,
            Clear
        }

        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(DictName);
            output.PutUTF(Field);
            output.PutEnum8(Op);
            output.PutRawData(Value);
        }


        public void ReadExternal(IInputStream input)
        {
            DictName = input.GetUTF();
            Field = input.GetUTF();
            Op = input.GetEnum8<DataOperation>();
            Value = input.GetRawData();
        }
    }

    [MessageType(0xFFFFFF1)]
    public class ManySyncShareDictMessage : IExternalizable
    {
        public SyncShareDictMessage[] Messages;
        public string ServerNodeName;

        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(ServerNodeName);
            output.PutArray(Messages,
                static (output, v) => output.PutObj(v));
        }

        public void ReadExternal(IInputStream input)
        {
            ServerNodeName = input.GetUTF();
            Messages = input.GetArray(
                static input => input.GetObj<SyncShareDictMessage>());
        }
    }

    internal class SharedDictionary<V> : ISharedDictionary<string, V>
    {
        private readonly ConcurrentDictionary<string, V> mInternalDict = new ConcurrentDictionary<string, V>();

        public class SubscribeData
        {
            private SharedDictionaryValueChange<string, V> mHandler;

            private readonly List<Invoker> mInvokers = new List<Invoker>();

            public struct Invoker
            {
                public SharedDictionaryValueChange<string, V> Handler;
                public IService Service;

                public void Invoke(ISharedDictionary<string, V> dict, string key)
                {
                    Service.Execute(invoker => { invoker.Handler.Invoke(dict, key); }, this);
                }
            }

            public string Key { get; }

            public SubscribeData(SharedDictionaryValueChange<string, V> handler, string key)
            {
                mHandler = handler;
                Key = key;
            }


            public void Add(IService service, SharedDictionaryValueChange<string, V> handler)
            {
                lock (mInvokers)
                {
                    mInvokers.Add(new Invoker { Service = service, Handler = handler });
                }
            }

            public void Remove(SharedDictionaryValueChange<string, V> handler)
            {
                lock (mInvokers)
                {
                    for (var i = 0; i < mInvokers.Count; i++)
                    {
                        if (mInvokers[i].Handler != handler)
                        {
                            continue;
                        }

                        mInvokers.RemoveAt(i);
                        break;
                    }
                }
            }

            public void Invoke(ISharedDictionary<string, V> dict, string key)
            {
                lock (mInvokers)
                {
                    for (var i = 0; i < mInvokers.Count; i++)
                    {
                        mInvokers[i].Invoke(dict, key);
                    }
                }
            }
        }

        private readonly ConcurrentDictionary<string, SubscribeData> mSubs = new ConcurrentDictionary<string, SubscribeData>();
        private readonly SharedMemory mSharedMemory;

        private void AddSyncMessage(SyncShareDictMessage.DataOperation operation, string field, object value)
        {
            var msg = new SyncShareDictMessage { DictName = DictionaryName, Field = field, Op = operation, Value = value };
            if (operation == SyncShareDictMessage.DataOperation.Clear)
            {
                foreach (var entry in mSubs.ToArray())
                {
                    entry.Value.Invoke(this, entry.Value.Key);
                }
            }
            else if (field != null)
            {
                if (mSubs.TryGetValue(NULL_KEY, out var globalSubs))
                {
                    globalSubs.Invoke(this, field);
                }

                if (mSubs.TryGetValue(field, out var subs))
                {
                    subs.Invoke(this, field);
                }
            }

            mSharedMemory.PostSyncMessage(msg);
        }

        public SharedDictionary(SharedMemory sharedMemory, string dictionaryName)
        {
            mSharedMemory = sharedMemory;
            DictionaryName = dictionaryName;
        }

        public IEnumerator<KeyValuePair<string, V>> GetEnumerator()
        {
            throw new NotSupportedException("not support Enumerator");
            return mInternalDict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public bool ContainsKey(string key)
        {
            return mInternalDict.ContainsKey(key);
        }

        public string DictionaryName { get; }


        public ICollection<string> Keys => mInternalDict.Keys;
        public ICollection<V> Values => mInternalDict.Values;

        #region IDictionary<TKey, TValue>

        ICollection IDictionary.Keys => ((IDictionary)mInternalDict).Keys;
        ICollection IDictionary.Values => ((IDictionary)mInternalDict).Values;

        bool IDictionary.IsReadOnly => ((IDictionary)mInternalDict).IsReadOnly;

        bool IDictionary.IsFixedSize => ((IDictionary)mInternalDict).IsFixedSize;

        bool ICollection<KeyValuePair<String, V>>.IsReadOnly => ((ICollection<KeyValuePair<String, V>>)mInternalDict).IsReadOnly;

        bool ICollection.IsSynchronized => ((ICollection)mInternalDict).IsSynchronized;

        object ICollection.SyncRoot => ((ICollection)mInternalDict).SyncRoot;

        object IDictionary.this[object key]
        {
            get => this[key.ToString()];
            set => this[key.ToString()] = (V)value;
        }

        void IDictionary.Remove(object key)
        {
            TryRemove(key.ToString(), out var ret);
        }

        void IDictionary.Add(object key, object value)
        {
            if (!TryAdd(key.ToString(), (V)value))
            {
                throw new ArgumentException($"KeyAlreadyExisted {key}");
            }
        }

        public void CopyTo(Array array, int index)
        {
            ((IDictionary)mInternalDict).CopyTo(array, index);
        }


        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            throw new NotSupportedException("not support Enumerator");
            return ((IDictionary)mInternalDict).GetEnumerator();
        }

        bool IDictionary.Contains(object key)
        {
            return ((IDictionary)mInternalDict).Contains(key);
        }

        void ICollection<KeyValuePair<string, V>>.Add(
            KeyValuePair<string, V> keyValuePair)
        {
            TryAdd(keyValuePair.Key, keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<string, V>>.Contains(
            KeyValuePair<string, V> keyValuePair)
        {
            return ((ICollection<KeyValuePair<string, V>>)mInternalDict).Contains(keyValuePair);
        }

        void ICollection<KeyValuePair<string, V>>.CopyTo(
            KeyValuePair<string, V>[] array,
            int index)
        {
            ((ICollection<KeyValuePair<string, V>>)mInternalDict).CopyTo(array, index);
        }

        bool ICollection<KeyValuePair<string, V>>.Remove(
            KeyValuePair<string, V> keyValuePair)
        {
            return TryRemove(keyValuePair.Key, out var ret);
        }

        void IDictionary<string, V>.Add(string key, V value)
        {
            TryAdd(key, value);
        }

        bool IDictionary<string, V>.Remove(string key)
        {
            return TryRemove(key, out var ret);
        }

        #endregion

        public void Clear()
        {
            mInternalDict.Clear();
            AddSyncMessage(SyncShareDictMessage.DataOperation.Clear, null, null);
        }


        public int Count => mInternalDict.Count;


        public bool IsEmpty => mInternalDict.IsEmpty;

        public V this[string key]
        {
            get => mInternalDict[key];
            set => AddOrUpdate(key, value, (k, v) => value);
        }

        public V Get(string key)
        {
            TryGetValue(key, out var ret);
            return ret;
        }


        private const string NULL_KEY = "___SharedDictionaryNullKey______SharedDictionaryNullKey___";

        public void Subscribe(IService service, string key, SharedDictionaryValueChange<string, V> handler)
        {
            var target = mSubs.GetOrAdd(key, s => new SubscribeData(handler, key));
            target.Add(service, handler);
        }

        public void Subscribe(IService service, SharedDictionaryValueChange<string, V> handler)
        {
            Subscribe(service, NULL_KEY, handler);
        }

        public void Unsubscribe(string key, SharedDictionaryValueChange<string, V> handler)
        {
            if (mSubs.TryGetValue(key, out var target))
            {
                target.Remove(handler);
            }
        }

        public void Unsubscribe(SharedDictionaryValueChange<string, V> handler)
        {
            Unsubscribe(null, handler);
        }

        public V GetOrAdd(string key, V value)
        {
            return GetOrAdd(key, (k) => value);
        }

        public V GetOrAdd(string key, Func<string, V> valueFactory)
        {
            if (TryGetValue(key, out V ret))
            {
                return ret;
            }

            ret = valueFactory.Invoke(key);
            return TryAdd(key, ret) ? ret : default;
        }

        public KeyValuePair<string, V>[] ToArray() => mInternalDict.ToArray();

        public bool TryGetValue(string key, out V value) => mInternalDict.TryGetValue(key, out value);

        public bool TryRemove(string key, out V value)
        {
            if (!mInternalDict.TryRemove(key, out value))
            {
                return false;
            }

            AddSyncMessage(SyncShareDictMessage.DataOperation.Remove, key, null);
            return true;
        }


        public void AddOrUpdate(string key, V value)
        {
            AddOrUpdate(key, value, (k, v) => value);
        }

        public bool TryAdd(string key, V value)
        {
            if (!mInternalDict.TryAdd(key, value))
            {
                return false;
            }

            AddSyncMessage(SyncShareDictMessage.DataOperation.Set, key, value);
            return true;
        }

        public V AddOrUpdate(string key, V addValue, Func<string, V, V> updateValueFactory)
        {
            var ret = mInternalDict.AddOrUpdate(key, addValue, updateValueFactory);
            AddSyncMessage(SyncShareDictMessage.DataOperation.Set, key, ret);
            return ret;
        }

        public bool TryUpdate(string key, V newValue, V comparisonValue)
        {
            if (mInternalDict.TryUpdate(key, newValue, comparisonValue))
            {
                AddSyncMessage(SyncShareDictMessage.DataOperation.Set, key, newValue);
                return true;
            }

            return false;
        }

        public V AddOrUpdate(string key, Func<string, V> addValueFactory, Func<string, V, V> updateValueFactory)
        {
            var ret = mInternalDict.AddOrUpdate(key, addValueFactory, updateValueFactory);
            AddSyncMessage(SyncShareDictMessage.DataOperation.Set, key, ret);
            return ret;
        }

        public void SetKeyDirty(string key)
        {
            if (TryGetValue(key, out var ret))
            {
                AddSyncMessage(SyncShareDictMessage.DataOperation.Set, key, ret);
            }
        }
    }


    internal class SharedMemory : ISharedMemory
    {
        private readonly HashSet<Type> mSupportTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(ushort),
            typeof(ushort),
            typeof(short),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(char),
            typeof(string),
            typeof(byte[]),
            typeof(DateTime),
            typeof(TimeSpan),
        };

        private bool SupportType(Type type)
        {
            if (mSupportTypes.Contains(type))
            {
                return true;
            }

            return type.IsEnum || type.IsInterfaceOf(typeof(ISerializable));
        }

        private readonly ConcurrentDictionary<string, ISharedDictionary> mSharedDictionary = new ConcurrentDictionary<string, ISharedDictionary>();
        private readonly Queue<SyncShareDictMessage> mCacheMessages = new Queue<SyncShareDictMessage>();

        private Timer mTicker;

        private readonly HashSet<int> mSyncRoutes = new HashSet<int>();

        public bool IsSyncRoute(int route)
        {
            return mSyncRoutes.Contains(route);
        }

        private RpcServiceNode mNode;

        public SharedMemory(RpcServiceNode node)
        {
            var factory = node.RpcCodec.Factory;
            if (factory is MessageFactoryGenerator messageFactoryGenerator)
            {
                var tc1 = messageFactoryGenerator.GetTypeID(typeof(SyncShareDictMessage));
                if (tc1 == IOStream.INVALID_MESSAGE_CODE)
                {
                    messageFactoryGenerator.RegistExternalizable<SyncShareDictMessage>();
                    tc1 = typeof(SyncShareDictMessage).GetAttribute<MessageTypeAttribute>().MessageTypeID;
                }
                var tc2 = messageFactoryGenerator.GetTypeID(typeof(ManySyncShareDictMessage));
                if (tc2 == IOStream.INVALID_MESSAGE_CODE)
                {
                    messageFactoryGenerator.RegistExternalizable<ManySyncShareDictMessage>();
                    tc2 = typeof(ManySyncShareDictMessage).GetAttribute<MessageTypeAttribute>().MessageTypeID;
                }
                mSyncRoutes.Add(tc1);
                mSyncRoutes.Add(tc2);
            }

            mNode = node;
        }


        public static TimeSpan MinSyncPeriodSeconds = TimeSpan.FromSeconds(1);

        public void SetSyncPeriod(TimeSpan period)
        {
            mTicker?.Dispose();
            mTicker = null;
            if (period >= MinSyncPeriodSeconds)
            {
                mTicker = new Timer(ThreadTick, null, TimeSpan.FromSeconds(0), period);
            }
        }

        private void SyncData(SyncShareDictMessage dictMessage)
        {
            var dict = mSharedDictionary.GetOrAdd(dictMessage.DictName, s =>
            {
                var d = typeof(ConcurrentDictionary<,>);
                d = d.MakeGenericType(typeof(string), dictMessage.Value.GetType());
                return (ISharedDictionary)ReflectionUtil.CreateInstance(d);
            });
            switch (dictMessage.Op)
            {
                case SyncShareDictMessage.DataOperation.Set:
                    dict[dictMessage.Field] = dictMessage.Value;
                    break;
                case SyncShareDictMessage.DataOperation.Remove:
                    dict.Remove(dictMessage.Field);
                    break;
                case SyncShareDictMessage.DataOperation.Clear:
                    dict.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void HandleSyncMessage(ISerializable obj)
        {
            if (obj is ManySyncShareDictMessage manySyncShareDictMessage && manySyncShareDictMessage.ServerNodeName != mNode.NodeName)
            {
                foreach (var message in manySyncShareDictMessage.Messages)
                {
                    SyncData(message);
                }
            }
        }

        private void ThreadTick(object state)
        {
            lock (mCacheMessages)
            {
                if (mCacheMessages.Count <= 0)
                {
                    return;
                }

                var manySyncShareDictMessage = new ManySyncShareDictMessage
                {
                    Messages = mCacheMessages.ToArray(),
                    ServerNodeName = mNode.NodeName
                };
                mNode.Application.BroadcastAppMessage(manySyncShareDictMessage);
                mCacheMessages.Clear();
            }
        }


        internal void PostSyncMessage(SyncShareDictMessage dictMessage)
        {
            if (mTicker != null)
            {
                lock (mCacheMessages)
                {
                    mCacheMessages.Enqueue(dictMessage);
                }
            }
            else
            {
                mNode.Application.BroadcastAppMessage(new ManySyncShareDictMessage
                {
                    Messages = new[] { dictMessage },
                    ServerNodeName = mNode.NodeName
                });
            }
        }

        public ISharedDictionary<string, V> GetDictionary<V>(string key)
        {
            if (!SupportType(typeof(V)))
            {
                throw new NotSupportedException();
            }

            return (ISharedDictionary<string, V>)mSharedDictionary.GetOrAdd(key, s => new SharedDictionary<V>(this, s));
        }
    }
}