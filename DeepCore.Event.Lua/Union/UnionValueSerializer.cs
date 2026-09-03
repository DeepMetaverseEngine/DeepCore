//#define SUPPORT_PROPERTY

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepCore
{
    public static class UnionValueSerializer
    {
        internal static Type ReflectionGetType(string fullName)
        {
            return ReflectionUtil.GetType(fullName);
        }

        internal static object ReflectionCreateInstance(Type t, params object[] args)
        {
            return ReflectionUtil.CreateInstance(t, args);
        }

        private static UnionValue SerializeList(IList list, Type[] keepTypes)
        {
            //var ret = new UnionValue(new UnionValueArray(), list.GetType().FullName);
            var ret = UnionValue.NewArray;
            for (var i = 0; i < list.Count; i++)
            {
                var t = list[i];
                ret.Arr.Add(Serialize(t, keepTypes));
            }

            return ret;
        }

        private static UnionValue SerializeDict(IDictionary dict, Type[] keepTypes)
        {
            //var ret = new UnionValue(new UnionValueMap(), dict.GetType().FullName);
            var ret = UnionValue.NewMap;
            foreach (DictionaryEntry entry in dict)
            {
                ret[Serialize(entry.Key, keepTypes)] = Serialize(entry.Value, keepTypes);
            }

            return ret;
        }

        private static object[] ZERO_ARGS = new object[0];

        private static UnionValue SerializeFieldsProperties(object obj, Type[] keepTypes)
        {
            var t = obj.GetType();


            //var ret = new UnionValue(new UnionValueMap(), t.FullName);
            var ret = UnionValue.NewMap;
#if SUPPORT_PROPERTY
            var all = t.GetMembers(BindingFlags.Instance | BindingFlags.Public);
#else
            var all = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
#endif
            foreach (var m in all)
            {
#if SUPPORT_PROPERTY
                if (m.MemberType == MemberTypes.Field)
                {
                    var f = (FieldInfo)m;
                    var next = Serialize(f.GetValue(obj));
                    ret[f.Name] = next;
                }
                else if (m.MemberType == MemberTypes.Property)
                {
                    var p = (PropertyInfo)m;
                    if (p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                    {
                        var value = p.GetGetMethod().Invoke(obj, ZERO_ARGS);
                        //var value = p.GetValue(obj);
                        var next = Serialize(value);
                        ret[p.Name] = next;
                    }
                }
#else
                var next = Serialize(m.GetValue(obj), keepTypes);
                ret[m.Name] = next;
#endif
            }

            return ret;
        }

        public static UnionValue Serialize(object obj, Type[] keepTypes = null)
        {
            if (obj == null)
            {
                return UnionValue.Null;
            }

            if (obj is UnionValue)
            {
                return (UnionValue) obj;
            }

            try
            {
                var ret = UnionValue.ToUnionValue(obj);
                if (!ret.IsNull)
                {
                    return ret;
                }

                if (keepTypes != null)
                {
                    var t = obj.GetType();
                    if (keepTypes.Any(t1 => t1.IsAssignableFrom(t)))
                    {
                        return new UnionValue(obj);
                    }
                }

                if (obj is IList)
                {
                    return SerializeList((IList) obj, keepTypes);
                }

                if (obj is IDictionary)
                {
                    return SerializeDict((IDictionary) obj, keepTypes);
                }

                return SerializeFieldsProperties(obj, keepTypes);
            }
            catch (Exception)
            {
                return new UnionValue(obj);
            }
        }


        public static T Deserialize<T>(UnionValue v, Type[] keepTypes = null)
        {
            var ret = Deserialize(v, typeof(T), keepTypes);
            if (ret != null)
            {
                return (T) ret;
            }

            return default(T);
        }

        public static object Deserialize(UnionValue v, Type type, Type[] keepTypes = null)
        {
            if (typeof(UnionValue).IsAssignableFrom(type))
            {
                return v;
            }

            if (type.IsInstanceOfType(v.Value))
            {
                return v.Value;
            }

            if (UnionValue.IsNativeType(type))
            {
                return v.IsNative || typeof(DateTime).IsAssignableFrom(type) ? Convert.ChangeType(v, type) : null;
            }

            if (keepTypes != null)
            {
                if (keepTypes.Any(t1 => t1.IsAssignableFrom(type)))
                {
                    return v.Value;
                }
            }

            if (!v.IsArray && !v.IsMap)
            {
                return null;
            }

            object ret;
            if (type.IsArray)
            {
                ret = DeserializeArray(v, type, keepTypes);
            }
            else if (type.GetInterface(typeof(IList).Name) != null)
            {
                ret = DeserializeList(v, type, keepTypes);
            }
            else if (type.GetInterface(typeof(IDictionary).Name) != null)
            {
                ret = DeserializeDict(v, type, keepTypes);
            }
            else
            {
                ret = DeserializeFieldsProperties(v, type, keepTypes);
            }

            return ret;
        }

        private static object DeserializeArray(UnionValue v, Type type, Type[] keepTypes)
        {
            var elementType = type.GetElementType();
            var arr = (Array) ReflectionCreateInstance(type, v.ElementCount);
            for (var i = 0; i < v.Arr.Count; i++)
            {
                var next = Deserialize(v.Arr[i], elementType, keepTypes);
                arr.SetValue(next, i);
            }

            return arr;
        }

        private static object DeserializeList(UnionValue v, Type type, Type[] keepTypes)
        {
            var elementType = type.GetGenericArguments()[0];
            var arr = (IList) ReflectionCreateInstance(type);
            for (var i = 0; i < v.Arr.Count; i++)
            {
                var next = Deserialize(v.Arr[i], elementType, keepTypes);
                arr.Add(next);
            }

            return arr;
        }

        private static object DeserializeDict(UnionValue v, Type type, Type[] keepTypes)
        {
            var keyType = type.GetGenericArguments()[0];
            var elementType = type.GetGenericArguments()[1];
            var map = (IDictionary) ReflectionCreateInstance(type);
            foreach (var entry in v.Map)
            {
                var key = Deserialize(entry.Key, keyType, keepTypes);
                var value = Deserialize(entry.Value, elementType, keepTypes);
                map.Add(key, value);
            }

            return map;
        }

        private static object DeserializeFieldsProperties(UnionValue v, Type type, Type[] keepTypes)
        {
            var ret = ReflectionCreateInstance(type);
            var all = type.GetMembers();
            foreach (var m in all)
            {
                var ele = v.GetElement(m.Name);
                if (!ele.IsNull)
                {
                    if (m.MemberType == MemberTypes.Field)
                    {
                        var f = (FieldInfo) m;
                        var vv = Deserialize(ele, f.FieldType, keepTypes);
                        if (vv != null)
                        {
                            f.SetValue(ret, vv);
                        }
                    }
                    else if (m.MemberType == MemberTypes.Property)
                    {
                        var p = (PropertyInfo) m;
                        if (p.CanWrite && p.GetIndexParameters().Length == 0)
                        {
                            var vv = Deserialize(ele, p.PropertyType, keepTypes);
                            if (vv != null)
                            {
                                p.GetSetMethod().Invoke(ret, new object[] {vv});
                            }


                            //p.SetValue(ret, ele.Deserialize(p.PropertyType));
                        }
                    }
                }
            }

            return ret;
        }


        public static void WriteToStream(IOutputStream output, UnionValue v)
        {
            if (v.IsArray)
            {
                output.PutList(v.Arr.InnerArray, WriteToStream);
            }
            else
            {
                output.PutArray<UnionValue>((UnionValue[])null, null);
            }

            if (v.IsMap)
            {
                output.PutMap(v.Map.InnerMap, WriteToStream, WriteToStream);
            }
            else
            {
                output.PutMap<UnionValue, UnionValue>(null, null, null);
            }

            output.PutRawData(v.IsNative ? v.Value : null);
        }

        public static UnionValue ReadFromStream(IInputStream input)
        {
            var list = input.GetArray(ReadFromStream);
            var map = input.GetMap(ReadFromStream, ReadFromStream);
            var nativeV = input.GetRawData();
            if (list != null)
            {
                var ret = new UnionValueArray(list);
                return new UnionValue(ret);
            }

            if (map != null)
            {
                var ret = new UnionValueMap(map);
                return new UnionValue(ret);
            }

            if (nativeV != null)
            {
                return UnionValue.Create(nativeV);
            }

            return UnionValue.Null;
        }
    }
}