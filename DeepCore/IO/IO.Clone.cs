using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.IO
{
    public static class CloneUtils
    {
        public static bool IsValueType(Type type)
        {
            if (type.IsPrimitive || type.IsEnum || type.IsValueType)
            {
                return true;
            }
            else if (type == typeof(string))
            {
                return true;
            }
            return false;
        }

        public static T Clone<T>(this AbstractCollectionPool pool, IExternalizableFactory factory, T src) where T : ISerializable
        {
            using (var ms = pool.AllocWrap<AutoMemoryStream>())
            {
                ms.Value.SetFactory(factory);
                return ms.Value.Clone(src);
            }
        }

        public static bool TryClone(this IExternalizableFactory factory, object src, out object value)
        {
            if (src == null)
            {
                value = null;
                return false;
            }
            using (var auto = IOStreamObjectPool.AllocAutoRelease(factory))
            {
                auto.Output.PutObj(src);
                auto.Flip();
                value = auto.Input.GetObjAny();
            }
            return true;
        }

        public static T Clone<T>(this T src, IExternalizableFactory factory) where T : ISerializable
        {
            if (TryClone(factory, src, out var ret))
            {
                return (T)ret;
            }
            return default(T);
        }
        public static T Clone<T>(this IExternalizableFactory factory, T src) where T : ISerializable
        {
            if (TryClone(factory, src, out var ret))
            {
                return (T)ret;
            }
            return default(T);
        }
        public static T[] CloneArray<T>(this IExternalizableFactory factory, T[] src)
        {
            if (src == null) return null;
            var etype = typeof(T);
            if (typeof(ISerializable).IsAssignableFrom(etype))
            {
                var ret = new T[src.Length];
                for (int i = ret.Length - 1; i >= 0; --i)
                {
                    ret[i] = (T)factory.Clone((ISerializable)src[i]);
                }
                return ret;
            }
            else if (IsValueType(etype))
            {
                return (T[])src.Clone();
            }
            else
            {
                var ret = new T[src.Length];
                for (int i = ret.Length - 1; i >= 0; --i)
                {
                    ret[i] = (T)factory.CloneAny(src[i]);
                }
                return ret;
            }
        }
        public static ArrayList<T> CloneList<T>(this IExternalizableFactory factory, IList<T> src)
        {
            if (src == null) return null;
            var etype = typeof(T);
            if (typeof(ISerializable).IsAssignableFrom(etype))
            {
                var ret = new ArrayList<T>(src.Count);
                foreach (var e in src)
                {
                    ret.Add((T)factory.Clone((ISerializable)e));
                }
                return ret;
            }
            else if (IsValueType(etype))
            {
                return new ArrayList<T>(src);
            }
            else
            {
                var ret = new ArrayList<T>(src.Count);
                foreach (var e in src)
                {
                    ret.Add((T)factory.CloneAny(e));
                }
                return ret;
            }
        }
        public static HashMap<K, V> CloneMap<K, V>(this IExternalizableFactory factory, IDictionary<K, V> src)
        {
            if (src == null) return null;
            var etype = typeof(V);
            if (typeof(ISerializable).IsAssignableFrom(etype))
            {
                var ret = new HashMap<K, V>(src.Count);
                foreach (var e in src)
                {
                    ret.Add(e.Key, (V)factory.Clone((ISerializable)e.Value));
                }
                return ret;
            }
            else if (IsValueType(etype))
            {
                return new HashMap<K, V>(src);
            }
            else
            {
                var ret = new HashMap<K, V>(src.Count);
                foreach (var e in src)
                {
                    ret.Add(e.Key, (V)factory.CloneAny(e.Value));
                }
                return ret;
            }
        }
        public static object CloneAny(this IExternalizableFactory factory, object src)
        {
            if (src == null) return null;
            if (src is ICloneable)
            {
                return ((ICloneable)src).Clone();
            }
            var type = src.GetType();
            if (src is ISerializable)
            {
                return factory.Clone((ISerializable)src);
            }
            if (IsValueType(type))
            {
                return src;
            }
            if (type.IsArray)
            {
                var array = (Array)src;
                var etype = type.GetElementType();
                if (typeof(ISerializable).IsAssignableFrom(etype))
                {
                    var ret = Array.CreateInstance(etype, array.Length);
                    for (int i = ret.Length - 1; i >= 0; --i)
                    {
                        ret.SetValue(factory.Clone((ISerializable)array.GetValue(i)), i);
                    }
                    return ret;
                }
                else if (IsValueType(etype))
                {
                    return array.Clone();
                }
                else
                {
                    var ret = Array.CreateInstance(etype, array.Length);
                    for (int i = ret.Length - 1; i >= 0; --i)
                    {
                        ret.SetValue(factory.CloneAny(array.GetValue(i)), i);
                    }
                    return ret;
                }
            }
            if (src is IList)
            {
                var list = (IList)src;
                var etype = type.GetGenericArguments()[0];
                if (typeof(ISerializable).IsAssignableFrom(etype))
                {
                    //var ret = (IList)DeepActivator.CreateInstance(type);
                    var ret = ReflectionUtil.CreateGenericInstance<IList>(typeof(ArrayList<>), type.GetGenericArguments());
                    foreach (var e in list)
                    {
                        ret.Add(factory.Clone((ISerializable)e));
                    }
                    return ret;
                }
                else if (IsValueType(etype) && (list is ICloneable))
                {
                    return ((ICloneable)list).Clone();
                }
                else
                {
                    //var ret = (IList)DeepActivator.CreateInstance(type);
                    var ret = ReflectionUtil.CreateGenericInstance<IList>(typeof(ArrayList<>), type.GetGenericArguments());
                    foreach (var e in list)
                    {
                        ret.Add(factory.CloneAny(e));
                    }
                    return ret;
                }
            }
            if (src is IDictionary)
            {
                var map = (IDictionary)src;
                var etype = type.GetGenericArguments()[1];
                if (typeof(ISerializable).IsAssignableFrom(etype))
                {
                    //var ret = (IDictionary)DeepActivator.CreateInstance(type);
                    var ret = ReflectionUtil.CreateGenericInstance<IDictionary>(typeof(HashMap<,>), type.GetGenericArguments());
                    foreach (DictionaryEntry e in map)
                    {
                        ret.Add(e.Key, factory.Clone((ISerializable)e.Value));
                    }
                    return ret;
                }
                else if (IsValueType(etype) && (map is ICloneable))
                {
                    return ((ICloneable)map).Clone();
                }
                else
                {
                    //var ret = (IDictionary)DeepActivator.CreateInstance(type);
                    var ret = ReflectionUtil.CreateGenericInstance<IDictionary>(typeof(HashMap<,>), type.GetGenericArguments());
                    foreach (DictionaryEntry e in map)
                    {
                        ret.Add(e.Key, factory.CloneAny(e.Value));
                    }
                    return ret;
                }
            }
            var dst = DeepActivator.CreateInstance(type);
            PropertyUtil.CopyFieldsTo(src, dst);
            return dst;
        }

    }
}
