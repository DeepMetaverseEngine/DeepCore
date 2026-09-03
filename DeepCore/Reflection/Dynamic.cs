using DeepCore.IO;
using DeepCore.Log;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace DeepCore.Reflection
{
    public interface IDynamicTypeInfo
    {
        Type DataType { get; }
        object CreateInstance(params object[] args);
        object CreateInstance();
        IDynamicFieldInfo[] GetFields();
        IDynamicFieldInfo GetField(string fieldName);
        IDynamicPropertyInfo[] GetProperties();
        IDynamicPropertyInfo GetProperty(string fieldName);
        IDynamicMethodInfo[] GetMethods();
        IDynamicMethodInfo GetMethod(string fieldName);
    }
    public interface IDynamicMemberInfo
    {
        bool IsDynamicFieldType { get; }
        string Name { get; }
        Type MemberType { get; }
        MemberInfo Member { get; }
        IDynamicTypeInfo DynamicType { get; }
        bool CanRead { get; }
        bool CanWrite { get; }
        object GetValue(object owner);
        void SetValue(object owner, object fieldValue);
    }
    public interface IDynamicMemberInfo<T> : IDynamicMemberInfo where T : MemberInfo
    {
        T Field { get; }
    }
    public interface IDynamicFieldInfo : IDynamicMemberInfo<FieldInfo>
    {
    }
    public interface IDynamicPropertyInfo : IDynamicMemberInfo<PropertyInfo>
    {
    }
    public interface IDynamicMethodInfo : IDynamicMemberInfo<MethodInfo>
    {
        object Invoke(object owner, params object[] args);
    }

    [Reflectible]
    public abstract class DynamicTypeFactory
    {
        public static DynamicTypeFactory Instance { get; private set; } = new DefaultDynamicTypeFactory();
        public DynamicTypeFactory() { DynamicTypeFactory.Instance = this; }
        public virtual bool IsDynamicType(Type type)
        {
            if (type.IsPrimitive) return false;
            if (type == typeof(string)) return false;
            if (type.IsArray) return false;
            if (type.IsInterfaceOf(typeof(System.Collections.IEnumerable)))
            {
                return false;
            }
            if (type.IsClass)
            {
                return true;
            }
            return false;
        }
        public virtual bool IsDynamicMember(Type type, MemberInfo info)
        {
            if (info is FieldInfo ff) return IsDynamicField(type, ff);
            if (info is PropertyInfo pp) return IsDynamicProperty(type, pp);
            if (info is MethodInfo mm) return IsDynamicMethod(type, mm);
            return false;
        }
        public virtual bool IsDynamicField(Type type, FieldInfo field)
        {
            return (!field.IsStatic && !field.IsLiteral && field.IsPublic);
        }
        public virtual bool IsDynamicProperty(Type type, PropertyInfo field)
        {
            return (field.CanWrite && field.CanRead &&
                field.GetMethod.IsPublic && field.SetMethod.IsPublic &&
                !field.GetMethod.IsAbstract && !field.SetMethod.IsAbstract &&
                !field.GetMethod.IsStatic && !field.SetMethod.IsStatic);
        }
        public virtual bool IsDynamicMethod(Type type, MethodInfo field)
        {
            if (!field.IsAbstract)
            {
                if (field.ReturnType != null && field.GetParameters().Length == 0)
                {
                    return field.TryGetAttribute<DynamicGetMethodAttribute>(out var desc);
                }
                else if (field.ReturnType == null && field.GetParameters().Length == 1)
                {
                    return field.TryGetAttribute<DynamicSetMethodAttribute>(out var desc);
                }
            }
            return false;
        }

        private List<Type> GetTypeH(Type type)
        {
            var ret = new List<Type>();
            while (type != null)
            {
                ret.Insert(0, type);
                type = type.BaseType;
            }
            return ret;
        }

        public List<IDynamicFieldInfo> GetFields(Type typeH)
        {
            var ret = new List<IDynamicFieldInfo>();
            foreach (var type in GetTypeH(typeH))
            {
                foreach (var info in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (IsDynamicField(type, info))
                    {
                        if (CreateFieldInfo(info, out var df)) { ret.Add(df); }
                    }
                }
            }
            return ret;
        }
        public List<IDynamicPropertyInfo> GetProperties(Type typeH)
        {
            var ret = new List<IDynamicPropertyInfo>();
            foreach (var type in GetTypeH(typeH))
            {
                foreach (var info in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (IsDynamicProperty(type, info))
                    {
                        if (CreatePropertyInfo(info, out var df)) { ret.Add(df); }
                    }
                }
            }
            return ret;
        }
        public List<IDynamicMethodInfo> GetMethods(Type typeH)
        {
            var ret = new List<IDynamicMethodInfo>();
            foreach (var type in GetTypeH(typeH))
            {
                foreach (var info in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (IsDynamicMethod(type, info))
                    {
                        if (CreateMethodInfo(info, out var df)) { ret.Add(df); }
                    }
                }
            }
            return ret;
        }

        public abstract IDynamicTypeInfo GetTypeInfo(Type type);
        public abstract bool CreateFieldInfo(FieldInfo field, out IDynamicFieldInfo info);
        public abstract bool CreatePropertyInfo(PropertyInfo field, out IDynamicPropertyInfo info);
        public abstract bool CreateMethodInfo(MethodInfo field, out IDynamicMethodInfo info);

        public bool CreateMemberInfo(MemberInfo field, out IDynamicMemberInfo info)
        {
            if (field is FieldInfo fi && CreateFieldInfo(fi, out var finfo))
            {
                info = finfo;
                return true;
            }
            if (field is PropertyInfo pi && CreatePropertyInfo(pi, out var pinfo))
            {
                info = pinfo;
                return true;
            }
            if (field is MethodInfo mi && CreateMethodInfo(mi, out var minfo))
            {
                info = minfo;
                return true;
            }
            info = null;
            return false;
        }
    }
    //--------------------------------------------------------------------------------------------------------------------
    class DefaultDynamicTypeFactory : DynamicTypeFactory
    {
        private static Logger log = new LazyLogger("DefaultDynamicTypeFactory");
        private HashMap<Type, IDynamicTypeInfo> types = new HashMap<Type, IDynamicTypeInfo>();
        public DefaultDynamicTypeFactory() { }
        public override IDynamicTypeInfo GetTypeInfo(Type type)
        {
            if (types.TryGetValue(type, out var ret))
            {
                return ret;
            }
            if (IsDynamicType(type))
            {
                lock (types)
                {
                    return types.GetOrAdd(type, static (t) =>
                    {
                        return new DefaultDynamicTypeInfo(t);
                    });
                }
            }
            if (type.IsArray)
            {
                return new ArrayDynamicTypeInfo(type);
            }
            return null;
        }
        public override bool CreateFieldInfo(FieldInfo field, out IDynamicFieldInfo info)
        {
            info = new DefaultFieldInfo(field);
            return true;
        }
        public override bool CreatePropertyInfo(PropertyInfo field, out IDynamicPropertyInfo info)
        {
            info = new DefaultPropertyInfo(field);
            return true;
        }
        public override bool CreateMethodInfo(MethodInfo field, out IDynamicMethodInfo info)
        {
            info = new DefaultMethodInfo(field);
            return true;
        }
        class DefaultDynamicTypeInfo : IDynamicTypeInfo
        {
            private IDynamicFieldInfo[] fields;
            private IDynamicPropertyInfo[] properties;
            private IDynamicMethodInfo[] methods;
            private HashMap<string, IDynamicFieldInfo> fields_map;
            private HashMap<string, IDynamicPropertyInfo> property_map;
            private HashMap<string, IDynamicMethodInfo> methods_map;
            public Type DataType { get; private set; }
            public DefaultDynamicTypeInfo(Type type)
            {
                this.DataType = type;

                this.fields = DynamicTypeFactory.Instance.GetFields(type).ToArray();
                this.fields_map = new HashMap<string, IDynamicFieldInfo>();
                foreach (var f in this.fields)
                {
                    if (!fields_map.TryAdd(f.Name, f))
                    {
                        log.Warn($"重复的字段：{type.FullName}.{f.Name}");
                    }
                    //fields_map.Add(f.Name, f);
                }

                this.properties = DynamicTypeFactory.Instance.GetProperties(type).ToArray();
                this.property_map = new HashMap<string, IDynamicPropertyInfo>();
                foreach (var p in this.properties)
                {
                    if (!property_map.TryAdd(p.Name, p))
                    {
                        log.Warn($"重复的属性：{type.FullName}.{p.Name}");
                    }
                    //property_map.Add(p.Name, p);
                }

                this.methods = DynamicTypeFactory.Instance.GetMethods(type).ToArray();
                this.methods_map = new HashMap<string, IDynamicMethodInfo>();
                foreach (var m in this.methods)
                {
                    if (!methods_map.TryAdd(m.Name, m))
                    {
                        log.Warn($"重复的方法：{type.FullName}.{m.Name}");
                    }
                    //methods_map.Add(m.Name, m);
                }
            }
            public object CreateInstance()
            {
                return ReflectionUtil.CreateInstance(DataType);
            }
            public object CreateInstance(params object[] args)
            {
                return ReflectionUtil.CreateInstance(DataType, args);
            }
            public IDynamicFieldInfo GetField(string fieldName)
            {
                return fields_map.Get(fieldName);
            }
            public IDynamicFieldInfo[] GetFields()
            {
                return fields;
            }
            public IDynamicPropertyInfo GetProperty(string fieldName)
            {
                return property_map.Get(fieldName);
            }
            public IDynamicPropertyInfo[] GetProperties()
            {
                return properties;
            }
            public IDynamicMethodInfo[] GetMethods()
            {
                return methods;
            }
            public IDynamicMethodInfo GetMethod(string fieldName)
            {
                return methods_map.Get(fieldName);
            }
        }
        abstract class DefaultMemberInfo<T> : IDynamicMemberInfo<T> where T : MemberInfo
        {
            private IDynamicTypeInfo ftype;
            private bool ftype_get = false;
            public bool IsDynamicFieldType { get; }
            public Type MemberType { get; }
            public MemberInfo Member { get; }
            public T Field { get; }
            public string Name => Field.Name;
            public abstract bool CanRead { get; }
            public abstract bool CanWrite { get; }
            public IDynamicTypeInfo DynamicType
            {
                get
                {
                    if (!ftype_get)
                    {
                        lock (this)
                        {
                            if (!ftype_get)
                            {
                                ftype_get = true;
                                ftype = DynamicTypeFactory.Instance.GetTypeInfo(MemberType);
                            }
                        }
                    }
                    return ftype;
                }
            }
            public DefaultMemberInfo(T field, Type memberType)
            {
                this.Member = field;
                this.Field = field;
                this.MemberType = memberType;
                this.IsDynamicFieldType = DynamicTypeFactory.Instance.IsDynamicType(memberType);
            }
            public abstract object GetValue(object owner);
            public abstract void SetValue(object owner, object fieldValue);
        }
        class DefaultFieldInfo : DefaultMemberInfo<FieldInfo>, IDynamicFieldInfo
        {
            public override bool CanRead { get => true; }
            public override bool CanWrite { get => true; }
            public DefaultFieldInfo(FieldInfo field) : base(field, field.FieldType)
            {
            }
            public override object GetValue(object owner)
            {
                return Field.GetValue(owner);
            }
            public override void SetValue(object owner, object fieldValue)
            {
                Field.SetValue(owner, fieldValue);
            }
        }
        class DefaultPropertyInfo : DefaultMemberInfo<PropertyInfo>, IDynamicPropertyInfo
        {
            public override bool CanRead { get => Field.CanRead; }
            public override bool CanWrite { get => Field.CanWrite; }
            public DefaultPropertyInfo(PropertyInfo field) : base(field, field.PropertyType)
            {
            }
            public override object GetValue(object owner)
            {
                if (Field.CanRead)
                {
                    return Field.GetValue(owner);
                }
                return null;
            }
            public override void SetValue(object owner, object fieldValue)
            {
                if (Field.CanWrite)
                {
                    Field.SetValue(owner, fieldValue);
                }
            }
        }
        class DefaultMethodInfo : DefaultMemberInfo<MethodInfo>, IDynamicMethodInfo
        {
            private static object[] zero_params = new object[0];
            public override bool CanRead { get; }
            public override bool CanWrite { get; }
            public DefaultMethodInfo(MethodInfo field) : base(field, field.ReturnType != null ? field.ReturnType : field.GetParameters()[0].ParameterType)
            {
                this.CanRead = field.ReturnType != typeof(void);
                this.CanWrite = field.GetParameters().Length > 0;
            }
            public override object GetValue(object owner)
            {
                if (CanRead)
                {
                    return Field.Invoke(owner, zero_params);
                }
                return null;
            }
            public override void SetValue(object owner, object fieldValue)
            {
                if (CanWrite)
                {
                    Field.Invoke(owner, new object[] { fieldValue });
                }
            }
            public object Invoke(object owner, params object[] args)
            {
                return Field.Invoke(owner, args);
            }
        }
        class ArrayDynamicTypeInfo : IDynamicTypeInfo
        {
            public Type DataType { get; private set; }
            public ArrayDynamicTypeInfo(Type type)
            {
                this.DataType = type;
            }
            public object CreateInstance(params object[] args)
            {
                if (args.Length == 0) return Array.CreateInstance(DataType.GetElementType(), 0);
                if (args.Length == 1) return Array.CreateInstance(DataType.GetElementType(), (int)args[0]);
                return Array.CreateInstance(DataType.GetElementType(), Array.ConvertAll(args, arg => (int)arg));
            }
            public object CreateInstance()
            {
                return Array.CreateInstance(DataType.GetElementType(), 0);
            }
            public IDynamicFieldInfo GetField(string fieldName)
            {
                throw new NotImplementedException();
            }
            public IDynamicFieldInfo[] GetFields()
            {
                throw new NotImplementedException();
            }
            public IDynamicPropertyInfo[] GetProperties()
            {
                throw new NotImplementedException();
            }
            public IDynamicPropertyInfo GetProperty(string fieldName)
            {
                throw new NotImplementedException();
            }
            public IDynamicMethodInfo[] GetMethods()
            {
                throw new NotImplementedException();
            }
            public IDynamicMethodInfo GetMethod(string fieldName)
            {
                throw new NotImplementedException();
            }
        }
    }
    //--------------------------------------------------------------------------------------------------------------------


}
