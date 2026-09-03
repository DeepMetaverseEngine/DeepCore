using DeepCore.Log;
using DeepCore.Protocol;
using DeepCore.Reflection.Modeling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using static DeepCore.Statistics.TimeStatisticsRecoder;

namespace DeepCore.Reflection
{
    public static class PropertyUtil
    {
        static public IComparer<FieldInfo> BaseFieldSorter = new FieldComparer();
        static public IComparer<PropertyInfo> BasePropertySorter = new PropertyComparer();

        public static Logger log = new LazyLogger(nameof(PropertyUtil));

        public delegate void ForEachOwnerAction(object root, object owner, object fieldValue, FieldInfo fieldInfo);
        public static void ForEachFieldsOwner(object root, ForEachOwnerAction action)
        {
            ForEachFieldsOwner(root, root, action);
        }
        private static void ForEachFieldsOwner(object root, object owner, ForEachOwnerAction action)
        {
            if (owner != null)
            {
                var type = owner.GetType();
                if (type.IsArray)
                {
                    Array array = (Array)owner;
                    foreach (var e in array)
                    {
                        if (e != null && !e.GetType().IsPrimitiveData())
                        {
                            ForEachFieldsOwner(root, e, action);
                        }
                    }
                }
                else if (type.IsClass)
                {
                    if (type.GetInterface(typeof(IDictionary).Name) != null)
                    {
                        var map = (IDictionary)owner;
                        foreach (var e in map.Values)
                        {
                            if (e != null && !e.GetType().IsPrimitiveData())
                            {
                                ForEachFieldsOwner(root, e, action);
                            }
                        }
                    }
                    else if (type.GetInterface(typeof(IList).Name) != null)
                    {
                        var list = (IList)owner;
                        foreach (var e in list)
                        {
                            if (e != null && !e.GetType().IsPrimitiveData())
                            {
                                ForEachFieldsOwner(root, e, action);
                            }
                        }
                    }
                    else
                    {
                        var fields = owner.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
                        foreach (var f in fields)
                        {
                            var fv = f.GetValue(owner);
                            action.Invoke(root, owner, fv, f);
                            if (fv != null)
                            {
                                if (!f.FieldType.IsPrimitiveData())
                                {
                                    ForEachFieldsOwner(root, fv, action);
                                }
                            }
                        }
                    }
                }
            }
        }

        static public FieldInfo[] GetFields(Type type,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
            bool recursion_base = true)
        {
            var list = new List<FieldInfo>();
            {
                GetFields(type, list, flags, recursion_base);
                return list.ToArray();
            }
        }

        static public FieldInfo[] GetFieldsBySequence(this Type type, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            var fields = type.GetFields(flags);
            Array.Sort(fields, (a, b) =>
            {
                var at = a.GetAttribute<SequenceAttribute>();
                var bt = b.GetAttribute<SequenceAttribute>();
                if (at != null && bt != null)
                {
                    return at.Index - bt.Index;
                }
                if (at != null) return -100;
                if (bt != null) return 100;
                return 0;
            });
            return fields;
        }

        static public void GetFields(Type type, List<FieldInfo> list,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
            bool recursion_base = true)
        {
            if (recursion_base && type.BaseType != null)
            {
                GetFields(type.BaseType, list, flags, recursion_base);
            }
            foreach (var field in type.GetFields(flags))
            {
                if (!list.TryFind(f => f.Name == field.Name, out var fr)) list.Add(field);
            }
        }

        static public FieldInfo GetField(Type type, string fieldName,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public,
            bool recursion_base = true)
        {
            var field = type.GetField(fieldName, flags);
            if (field == null && type.BaseType != null)
            {
                field = GetField(type.BaseType, fieldName, flags, recursion_base);
            }
            return field;
        }


        static public PropertyInfo[] GetProperties(Type type,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
            bool recursion_base = true)
        {
            var list = new List<PropertyInfo>();
            {
                GetProperties(type, list, flags, recursion_base);
                return list.ToArray();
            }
        }

        static public void GetProperties(Type type, List<PropertyInfo> list,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
            bool recursion_base = true)
        {
            if (recursion_base && type.BaseType != null)
            {
                GetProperties(type.BaseType, list, flags, recursion_base);
            }
            foreach (var field in type.GetProperties(flags))
            {
                if (!list.TryFind(f => f.Name == field.Name, out var fr)) list.Add(field);
            }
        }

        static public PropertyInfo GetProperty(Type type, string fieldName,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public,
            bool recursion_base = true)
        {
            var field = type.GetProperty(fieldName, flags);
            if (field == null && type.BaseType != null)
            {
                field = GetProperty(type.BaseType, fieldName, flags, recursion_base);
            }
            return field;
        }



        static public FieldInfo[] SortFields(FieldInfo[] fields)
        {
            Array.Sort(fields, BaseFieldSorter);
            return fields;
        }
        static public PropertyInfo[] SortProperties(PropertyInfo[] properties)
        {
            Array.Sort(properties, BasePropertySorter);
            return properties;
        }

        //-------------------------------------------------------------------------------------------------------------------------

        public static Attribute GetAttributeByType(Type field, Type attributeType, bool inherit = true)
        {
            return field.GetAttributeByType(attributeType, inherit);
        }
        public static Attribute GetAttributeByName(Type field, string name, bool inherit = true)
        {
            return field.GetAttributeByName(name, inherit);
        }
        public static T GetAttribute<T>(Type member, bool inherit = true) where T : System.Attribute
        {
            return member.GetAttribute<T>(inherit);
        }
        public static bool TryGetAttribute<T>(Type member, out T attr, bool inherit = true) where T : System.Attribute
        {
            attr = member.GetAttribute<T>(inherit);
            return attr != null;
        }

        //-------------------------------------------------------------------------------------------------------------------------

        public static Attribute GetAttributeByType(ICustomAttributeProvider field, Type attributeType, bool inherit = true)
        {
            return field.GetAttributeByType(attributeType, inherit);
        }
        public static Attribute GetAttributeByName(ICustomAttributeProvider field, string name, bool inherit = true)
        {
            return field.GetAttributeByName(name, inherit);
        }
        public static T GetAttribute<T>(ICustomAttributeProvider member, bool inherit = true) where T : System.Attribute
        {
            return member.GetAttribute<T>(inherit);
        }
        public static bool TryGetAttribute<T>(ICustomAttributeProvider member, out T attr, bool inherit = true) where T : System.Attribute
        {
            attr = member.GetAttribute<T>(inherit);
            return attr != null;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        static public string ToDesc(this Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }
            if (type.TryGetAttribute<DescAttribute>(out var desc))
            {
                return desc.Desc;
            }
            return type.Name;
        }
        static public string ToEnumDesc(this object value)
        {
            var desc = GetEnumAttribute<DescAttribute>(value);
            if (desc != null) return desc.Desc;
            return value.ToString();
        }
        static public T GetEnumAttribute<T>(object value) where T : Attribute
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name == null)
            {
                return null;
            }
            FieldInfo field = type.GetField(name);
            if (field != null)
            {
                return GetAttribute<T>(field);
            }
            return null;
        }
        static public T GetEnumAttribute<T>(Type enumType, object value) where T : Attribute
        {
            Type type = enumType;
            string name = Enum.GetName(type, value);
            if (name == null)
            {
                return null;
            }
            FieldInfo field = type.GetField(name);
            if (field != null)
            {
                return GetAttribute<T>(field);
            }
            return null;
        }

        static public string GetEnumDescriptionText(object value)
        {
            var desc = GetEnumAttribute<DescAttribute>(value);
            if (desc != null) return desc.Desc;
            return value.ToString();
        }
        static public string GetEnumDescriptionText<E>(object value)
        {
            var desc = GetEnumAttribute<DescAttribute>(typeof(E), value);
            if (desc != null) return desc.Desc;
            return value.ToString();
        }
        static public E GetEnumFromDescription<E>(Type enumType, string descText)
        {
            var values = Enum.GetValues(enumType);
            foreach (var value in values)
            {
                var desc = GetEnumAttribute<DescAttribute>(value);
                if (desc.Desc == descText)
                {
                    return (E)value;
                }
            }
            throw new Exception($"Can Not Found Enum With Desc:\"{descText}\"");
        }
        static public object GetEnumFromDescription(Type enumType, string descText)
        {
            var values = Enum.GetValues(enumType);
            foreach (var value in values)
            {
                var desc = GetEnumAttribute<DescAttribute>(value);
                if (desc?.Desc == descText)
                {
                    return value;
                }
            }
            return Enum.Parse(enumType, descText, true);
        }

        public static bool TryGetEnumValue<T>(this Type type, string name, out T ret) where T : unmanaged
        {
            if (type.IsEnum)
            {
                var field = type.GetField(name);
                if (field != null)
                {
                    var id = field.GetValue(null);
                    ret = (T)id;
                    return true;
                }
            }
            ret = default;
            return false;
        }
        public static bool TryGetEnumValue<T>(this Type type, string name, out FieldInfo field, out T ret) where T : unmanaged
        {
            if (type.IsEnum)
            {
                field = type.GetField(name);
                if (field != null)
                {
                    var id = field.GetValue(null);
                    ret = (T)id;
                    return true;
                }
            }
            field = null;
            ret = default;
            return false;
        }
        public static bool TryGetEnumValueAndAttribute<T, A>(this Type type, string name, out T ret, out A attr) where T : unmanaged where A : Attribute
        {
            if (type.IsEnum)
            {
                var field = type.GetField(name);
                if (field != null && field.TryGetAttribute<A>(out attr))
                {
                    var id = field.GetValue(null);
                    ret = (T)id;
                    return true;
                }
            }
            ret = default;
            attr = null;
            return false;
        }


        //-------------------------------------------------------------------------------------------------------------------------

        public static DescAttribute GetDesc(Type type)
        {
            return GetAttribute<DescAttribute>(type);
        }
        public static DescAttribute GetDesc(MemberInfo field)
        {
            return GetAttribute<DescAttribute>(field);
        }
        public static ListDescAttribute GetListDesc(MemberInfo field)
        {
            return GetAttribute<ListDescAttribute>(field);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        public static ValueTuple<FieldInfo, T>[] GetFieldsWithAttribute<T>(Type type) where T : Attribute
        {
            var list = new List<ValueTuple<FieldInfo, T>>();
            foreach (var field in type.GetFields())
            {
                var fieldAttr = PropertyUtil.GetAttribute<T>(field);
                if (fieldAttr != null)
                {
                    list.Add((field, fieldAttr));
                }
            }
            return list.ToArray();
        }
        public static ValueTuple<M, T>[] GetMembersWithAttribute<M, T>(Type type) where M : MemberInfo where T : Attribute
        {
            var members = type.GetMembers();
            var list = new List<ValueTuple<M, T>>(members.Length);
            foreach (var item in members)
            {
                if (item is M field)
                {
                    var fieldAttr = PropertyUtil.GetAttribute<T>(field);
                    if (fieldAttr != null)
                    {
                        list.Add((field, fieldAttr));
                    }
                }
            }
            return list.ToArray();
        }

        //-------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="field"></param>
        /// <param name="fieldValue"></param>
        /// <returns>break if true</returns>
        public delegate bool CollectFieldValue(object owner, object field, object fieldValue);
        static public void CollectFieldValues(object data, CollectFieldValue action)
        {
            if (data != null)
            {
                Type type = data.GetType();
                if (data is string) return;
                else if (type.IsPrimitive) return;
                else if (type.IsEnum) return;
                else if (type.IsArray)
                {
                    Array array = (Array)data;
                    int i = 0;
                    foreach (object o in array)
                    {
                        action(data, i, o);
                        CollectFieldValues(o, action);
                        i++;
                    }
                }
                else if (data is IDictionary)
                {
                    IDictionary map = data as IDictionary;
                    foreach (var k in map.Keys)
                    {
                        var v = map[k];
                        action(data, k, v);
                        CollectFieldValues(v, action);
                    }
                }
                else if (data is ICollection)
                {
                    ICollection list = data as ICollection;
                    int i = 0;
                    foreach (object o in list)
                    {
                        action(data, i, o);
                        CollectFieldValues(o, action);
                        i++;
                    }
                }
                else if (type.IsClass)
                {
                    foreach (FieldInfo field in type.GetFields())
                    {
                        if (!field.IsStatic)
                        {
                            object fv = field.GetValue(data);
                            if (action(data, field, fv))
                            {
                                return;
                            }
                            if (fv != null)
                            {
                                CollectFieldValues(fv, action);
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 将一个对象里面所有的Attribute的标记的Field值，全部取出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        static public void CollectFieldTypeValues<T>(object data, List<T> collection)
        {
            if (data != null)
            {
                Type fieldType = typeof(T);
                Type type = data.GetType();

                if (data is T)
                {
                    collection.Add((T)data);
                }
                if (type.IsClass && !type.IsPrimitive)
                {
                    if (data is IDictionary)
                    {
                        IDictionary map = data as IDictionary;
                        foreach (object o in map.Values)
                        {
                            CollectFieldTypeValues<T>(o, collection);
                        }
                    }
                    else if (data is ICollection)
                    {
                        ICollection list = data as ICollection;
                        foreach (object o in list)
                        {
                            CollectFieldTypeValues<T>(o, collection);
                        }
                    }
                    else if (type.IsArray)
                    {
                        Array array = (Array)data;
                        foreach (object o in array)
                        {
                            CollectFieldTypeValues<T>(o, collection);
                        }
                    }
                    else
                    {
                        foreach (FieldInfo field in type.GetFields())
                        {
                            if (!field.IsStatic)
                            {
                                object fd = field.GetValue(data);
                                if (fd != null)
                                {
                                    CollectFieldTypeValues<T>(fd, collection);
                                }
                            }
                        }
                    }
                }
            }
        }

        static public List<T> CollectFieldTypeValues<T>(object data)
        {
            var collection = new List<T>();
            CollectFieldTypeValues(data, collection); collection.TrimExcess();
            return collection;
        }


        /// <summary>
        /// 将一个对象里面所有的Attribute的标记的Field值，全部取出
        /// </summary>
        /// <param name="data"></param>
        /// <param name="attributeType"></param>
        /// <param name="collection"></param>
        static public List<FieldAttributeValue<A>> CollectFieldAttributeValues<A>(object data) where A : Attribute
        {
            List<FieldAttributeValue<A>> ret = new List<FieldAttributeValue<A>>();
            CollectFieldAttributeValues(data, data, ret);
            return ret;
        }
        static public List<FieldAttributeValue<A>> CollectFieldAttributeValues<A>(ICollection datas) where A : Attribute
        {
            List<FieldAttributeValue<A>> ret = new List<FieldAttributeValue<A>>();
            foreach (var root in datas)
            {
                CollectFieldAttributeValues(root, root, ret);
            }
            return ret;
        }
        static private void CollectFieldAttributeValues<A>(object root, object data, List<FieldAttributeValue<A>> collection) where A : Attribute
        {
            if (data != null)
            {
                Type attributeType = typeof(A);
                Type type = data.GetType();

                if (data is IDictionary)
                {
                    IDictionary map = data as IDictionary;
                    foreach (object o in map.Values)
                    {
                        CollectFieldAttributeValues<A>(root, o, collection);
                    }
                }
                else if (data is ICollection)
                {
                    ICollection list = data as ICollection;
                    foreach (object o in list)
                    {
                        CollectFieldAttributeValues<A>(root, o, collection);
                    }
                }
                else if (type.IsArray)
                {
                    Array array = (Array)data;
                    foreach (object o in array)
                    {
                        CollectFieldAttributeValues<A>(root, o, collection);
                    }
                }
                else if (type.IsClass)
                {
                    foreach (FieldInfo field in type.GetFields())
                    {
                        if (!field.IsStatic)
                        {
                            object fd = field.GetValue(data);
                            if (fd != null)
                            {
                                var attr = field.GetAttributeByType(attributeType) as A;
                                if (attr != null)
                                {
                                    var fv = new FieldAttributeValue<A>(root, field, attr, fd, data);
                                    collection.Add(fv);
                                }
                                else
                                {
                                    CollectFieldAttributeValues<A>(root, fd, collection);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 将一个对象里面所有的Attribute的标记的Field值，全部取出
        /// </summary>
        /// <param name="data"></param>
        /// <param name="attributeType"></param>
        /// <param name="collection"></param>
        /// 
        static public List<FieldAttributeValue<A, T>> CollectFieldAttributeValues<A, T>(object data) where A : Attribute
        {
            List<FieldAttributeValue<A, T>> ret = new List<FieldAttributeValue<A, T>>();
            CollectFieldAttributeValues(data, data, ret);
            return ret;
        }
        static public List<FieldAttributeValue<A, T>> CollectFieldAttributeValues<A, T>(ICollection datas) where A : Attribute
        {
            List<FieldAttributeValue<A, T>> ret = new List<FieldAttributeValue<A, T>>();
            foreach (var root in datas)
            {
                CollectFieldAttributeValues(root, root, ret);
            }
            return ret;
        }
        static private void CollectFieldAttributeValues<A, T>(object root, object data, List<FieldAttributeValue<A, T>> collection) where A : Attribute
        {
            var list = new List<FieldAttributeValue<A>>();
            CollectFieldAttributeValues<A>(root, data, list);
            foreach (var fv in list)
            {
                if (fv.FieldValue is T)
                {
                    collection.Add(new FieldAttributeValue<A, T>(root, fv.Field, fv.AttributeData as A, (T)fv.FieldValue, fv.FieldOwner));
                }
            }
        }


        public static T GetFieldOrPropertyOrMethodValue<T>(object componentData, string fieldName)
        {
            var ret = GetFieldOrPropertyOrMethodValue(componentData, fieldName);
            if (ret is T tr) { return tr; }
            return default(T);
        }

        public static object GetFieldOrPropertyOrMethodValue(object componentData, string fieldName)
        {
            Type componentType = componentData.GetType();
            FieldInfo depend_field = componentType.GetField(fieldName);
            if (depend_field != null)
            {
                try
                {
                    var ret = depend_field.GetValue(componentData);
                    return ret;
                }
                catch (Exception err) { log.Error(err.Message, err); }
            }
            PropertyInfo depend_property = componentType.GetProperty(fieldName);
            if (depend_property != null)
            {
                try
                {
                    var ret = depend_property.GetValue(componentData, null);
                    return ret;
                }
                catch (Exception err) { log.Error(err.Message, err); }
            }
            MethodInfo depend_method = componentType.GetMethod(fieldName);
            if (depend_method != null)
            {
                try
                {
                    var ret = depend_method.Invoke(componentData, ZERO_ARGS);
                    return ret;
                }
                catch (Exception err) { log.Error(err.Message, err); }
            }
            return null;
        }

        public readonly static object[] ZERO_ARGS = new object[] { };

        public static void SetMemberValue(MemberInfo field, object obj, object value)
        {
            if (field is FieldInfo)
            {
                (field as FieldInfo).SetValue(obj, value);
            }
            else if (field is PropertyInfo)
            {
                var set = (field as PropertyInfo).GetSetMethod();
                if (set != null)
                {
                    set.Invoke(obj, new object[] { value });
                }
            }
        }
        public static object GetMemberValue(MemberInfo field, object obj)
        {
            if (field is FieldInfo)
            {
                return (field as FieldInfo).GetValue(obj);
            }
            else if (field is PropertyInfo)
            {
                var get = (field as PropertyInfo).GetGetMethod();
                if (get != null)
                {
                    return get.Invoke(obj, ZERO_ARGS);
                }
            }
            return null;
        }

        public static void CopyFieldsTo(object src, object dst, Func<FieldInfo,object ,object,bool> func=null)
        {
            if (src == dst) return;
            var stype = src.GetType();
            var dtype = dst.GetType();
            //             if (src.GetType() != dst.GetType())
            //             {
            //                 throw new Exception("Type not same !!!");
            //             }
            foreach (var sfield in stype.GetFields())
            {
                if (!sfield.IsStatic)
                {
                    var dfield = dtype.GetField(sfield.Name);
                    if (dfield != null && !dfield.IsStatic && sfield.FieldType == dfield.FieldType)
                    {
                        if (func == null || func(dfield, src, dst))
                        {
                            var sv = sfield.GetValue(src);
                            dfield.SetValue(dst, sv);
                        }
                    }
                }
            }
        }
        public static void CopyFieldTo(string fieldName, object src, object dst)
        {
            if (src == dst) return;
            var type = src.GetType();
            //             if (src.GetType() != dst.GetType())
            //             {
            //                 throw new Exception("Type not same !!!");
            //             }
            var field = type.GetField(fieldName);
            if (field != null && !field.IsStatic)
            {
                var sv = field.GetValue(src);
                field.SetValue(dst, sv);
            }
        }
    }
    public class FieldOwnerValue
    {
        public object RootData { get; }
        public FieldInfo Field { get; }
        public object FieldOwner { get; }
        public object FieldValue { get; }
        public FieldOwnerValue(object root, FieldInfo field, object fieldData, object fieldOwner)
        {
            this.RootData = root;
            this.Field = field;
            this.FieldOwner = fieldOwner;
            this.FieldValue = fieldData;
        }
    }
    public class FieldAttributeValue : FieldOwnerValue
    {
        public Attribute AttributeData { get; }
        public FieldAttributeValue(object root, FieldInfo field, Attribute attr, object fieldData, object fieldOwner)
            : base(root, field, fieldData, fieldOwner)
        {
            this.AttributeData = attr;
        }
        public void SetValue(object value)
        {
            Field.SetValue(FieldOwner, value);
        }

    }
    public class FieldAttributeValue<A> : FieldAttributeValue where A : Attribute
    {
        new public A AttributeData { get => base.AttributeData as A; }
        public FieldAttributeValue(object root, FieldInfo field, A attr, object fieldData, object fieldOwner)
            : base(root, field, attr, fieldData, fieldOwner) { }
    }
    public class FieldAttributeValue<A, T> : FieldAttributeValue<A> where A : Attribute
    {
        new public T FieldValue { get => (T)base.FieldValue; }
        public FieldAttributeValue(object root, FieldInfo field, A attr, T fieldData, object fieldOwner)
            : base(root, field, attr, fieldData, fieldOwner) { }
        public void SetValue(T value)
        {
            Field.SetValue(FieldOwner, value);
        }
    }

    public class TypeDescAttribute : IComparable<TypeDescAttribute>
    {
        public readonly DescAttribute Desc;
        public readonly Type OwnerType;
        public TypeDescAttribute(Type type)
        {
            OwnerType = type;
            Desc = PropertyUtil.GetAttribute<DescAttribute>(type);
            //if (Desc == null) throw new Exception("None Desc Attribute : " + type.FullName);
        }
        public override string ToString()
        {
            return Desc != null ? Desc.Desc : OwnerType.Name;
        }
        public int CompareTo(TypeDescAttribute other)
        {
            if (OwnerType.TryGetAttribute<DescAttribute>(out var attrA) && other.OwnerType.TryGetAttribute<DescAttribute>(out var attrB))
            {
                return attrA.Desc.CompareTo(attrB.Desc);
            }
            return OwnerType.FullName.CompareTo(other.OwnerType.FullName);
        }


        /// <summary>
        /// 如果此类型有DescAttribute签名，则返回Desc
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetDescText(Type type)
        {
            DescAttribute desc = PropertyUtil.GetAttribute<DescAttribute>(type);
            if (desc != null)
            {
                return desc.Desc;
            }
            return "";
        }
        /// <summary>
        /// 如果此类型有DescAttribute签名，则返回Catgory
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetCatgoryName(Type type)
        {
            DescAttribute desc = PropertyUtil.GetAttribute<DescAttribute>(type);
            if (desc != null)
            {
                return desc.Category;
            }
            return "";
        }

    }
    public class DescAttributeMember<T> : IComparable<DescAttributeMember<T>> where T : MemberInfo
    {
        public readonly DescAttribute Desc;
        public readonly T Member;
        public DescAttributeMember(T type)
        {
            Member = type;
            Desc = PropertyUtil.GetAttribute<DescAttribute>(type);
            //if (Desc == null) throw new Exception("None Desc Attribute : " + type.FullName);
        }
        public override string ToString()
        {
            return Desc != null ? Desc.Desc : Member.Name;
        }
        public int CompareTo(DescAttributeMember<T> other)
        {
            if (Member.TryGetAttribute<DescAttribute>(out var attrA) && other.Member.TryGetAttribute<DescAttribute>(out var attrB))
            {
                return attrA.Desc.CompareTo(attrB.Desc);
            }
            return Member.Name.CompareTo(other.Member.Name);
        }
    }

    public class AttributeMember<A, T> : IComparable<AttributeMember<A, T>> where A : Attribute where T : MemberInfo
    {
        public readonly A Attr;
        public readonly T Member;
        public AttributeMember(T type)
        {
            Member = type;
            Attr = PropertyUtil.GetAttribute<A>(type);
        }
        public AttributeMember(A a, T type)
        {
            Member = type;
            Attr = a;
        }
        public override string ToString()
        {
            return Attr != null ? Attr.ToString() : Member.ToString();
        }
        public int CompareTo(AttributeMember<A, T> other)
        {
            if (Member.TryGetAttribute<A>(out var attrA) && other.Member.TryGetAttribute<A>(out var attrB))
            {
                return attrA.ToString().CompareTo(attrB.ToString());
            }
            return Member.Name.CompareTo(other.Member.Name);
        }
    }
}