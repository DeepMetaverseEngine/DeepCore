using DeepCore;
using DeepMetaGame.Data.Message;
using System;
using System.Collections.Generic;
using System.Text;
using static DeepMetaGame.Data.Message.ClientStruct;

namespace DeepMetaGame.Data.Misc
{
    //-------------------------------------------------------------------------------------------------------------------------------
    public delegate void SetEnvironmentVarDelegate<O>(O st, string key, EnvironmentVar var, object value, bool syncToClient);
    public class EnvironmentVar
    {
        public static bool ALWAYS_SYNC_ENVIRONMENT_VAR = false;

        public string Key { get; private set; }
        public bool SyncToClient { get; private set; }
        public object Value { get; set; }

        public EnvironmentVar(string key, bool sync, object obj)
        {
            Key = key;
            SyncToClient = sync;
            Value = obj;
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------
    public class EnvironmentVarMap<O>
    {
        public readonly O Owner;
        private HashMap<string, EnvironmentVar> Map = new HashMap<string, EnvironmentVar>(1);
        public EnvironmentVarMap(O owner)
        {
            this.Owner = owner;
        }
        //-----------------------------------------------------------------------------------------------------//
        public event SetEnvironmentVarDelegate<O> OnEnvironmentVarChangeHandler;
        //-----------------------------------------------------------------------------------------------------//
        public void SetEnvironmentVar(string key, object value, bool syncToClient)
        {
            if (!string.IsNullOrEmpty(key))
            {
                EnvironmentVar var = Map.Get(key);
                if (var != null)
                {
                    if (var.Value != value)
                    {
                        var.Value = value;
                        OnEnvironmentVarChangeHandler?.Invoke(Owner, key, var, value, syncToClient);
                    }
                }
                else
                {
                    var = new EnvironmentVar(key, syncToClient, value);
                    Map.Add(key, var);
                    OnEnvironmentVarChangeHandler?.Invoke(Owner, key, var, value, syncToClient);
                }
            }
        }
        //-----------------------------------------------------------------------------------------------------//
        public bool TryGetEnvironmentVar(string key, out object value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (Map.TryGetValue(key, out var ret))
                {
                    value = ret.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }
        public bool TryGetEnvironmentVarAs<T>(string key, out T value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (Map.TryGetValue(key, out var ret))
                {
                    try
                    {
                        value = CUtils.ConvertTo<T>(ret.Value);
                        return true;
                    }
                    catch (Exception err)
                    {
                        err.PrintStackTrace();
                    }
                }
            }
            value = default(T);
            return false;
        }
        public T GetEnvironmentVarAs<T>(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (Map.TryGetValue(key, out var var))
                {
                    try
                    {
                        // 使用 Convert.ChangeType 进行安全转换
                        return CUtils.ConvertTo<T>(var.Value);
                    }
                    catch (Exception err)
                    {
                        err.PrintStackTrace();
                    }
                }
            }

            return default(T);
        }
        //-----------------------------------------------------------------------------------------------------//
        public int ListEnvironmentVars(List<EnvironmentVar> list)
        {
            list.AddRange(Map.Values);
            return Map.Count;
        }
        public List<EnvironmentVar> ListEnvironmentVars()
        {
            return new List<EnvironmentVar>(Map.Values);
        }
        public void Clear()
        {
            this.Map.Clear();
        }
        public bool TryGetValue(string key, out EnvironmentVar value)
        {
            return Map.TryGetValue(key, out value);
        }
        public IEnumerable<EnvironmentVar> Values { get => Map.Values; }
        public IEnumerable<string> Keys { get => Map.Keys; }
        public int Count { get => Map.Count; }
        //-----------------------------------------------------------------------------------------------------//

    }
    //-------------------------------------------------------------------------------------------------------------------------------
    public interface IEnvironmentDecoder
    {
        public object DecodeZoneVar(object value);
    }
    public class LayerEnvironmentMap
    {
        protected HashMap<string, object> mEnvironmentVarMap = new HashMap<string, object>();
        public IEnumerable<string> Keys { get => mEnvironmentVarMap.Keys; }
        public IEnvironmentDecoder Decoder { get; }
        public LayerEnvironmentMap(IEnvironmentDecoder decoder)
        {
            this.Decoder = decoder;
        }

        public bool TrySet(ZoneEnvironmentVar value, out string k, out object v)
        {
            k = value?.Key;
            if (!string.IsNullOrEmpty(k))
            {
                v = Decoder.DecodeZoneVar(value.Value);
                mEnvironmentVarMap[value.Key] = v;
                return true;
            }
            v = null;
            return false;
        }

        /// <summary>
        /// 获得当前单位服务端可同步环境变量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetEnvironmentVar(string key)
        {
            return mEnvironmentVarMap.Get(key);
        }
        public bool TryGetEnvironmentVar(string key, out object value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (mEnvironmentVarMap.TryGetValue(key, out var ret))
                {
                    value = ret;
                    return true;
                }
            }
            value = null;
            return false;
        }
        public T GetEnvironmentVarAs<T>(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (mEnvironmentVarMap.TryGetValue(key, out var var))
                {
                    try
                    {
                        // 使用 Convert.ChangeType 进行安全转换
                        return CUtils.ConvertTo<T>(var);
                    }
                    catch (Exception err)
                    {
                        err.PrintStackTrace();
                    }
                }
            }
            return default(T);
        }
        public bool TryGetEnvironmentVarAs<T>(string key, out T value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (mEnvironmentVarMap.TryGetValue(key, out var ret))
                {
                    try
                    {
                        value = CUtils.ConvertTo<T>(ret);
                        return true;
                    }
                    catch (Exception err)
                    {
                        err.PrintStackTrace();
                    }
                }
            }
            value = default(T);
            return false;
        }

        /// <summary>
        /// 获得当前单位服务端可同步环境变量列表
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IEnumerable<string> ListEnvironmentVars()
        {
            return mEnvironmentVarMap.Keys;
        }
        public IEnumerable<KeyValuePair<string, object>> ListEnvironmentValues()
        {
            return mEnvironmentVarMap;
        }
        public void Clear()
        {
            this.mEnvironmentVarMap.Clear();
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------
}
