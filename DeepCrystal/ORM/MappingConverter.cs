using DeepCore.IO;
using DeepCore.ORM;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DeepCrystal.Threading;
using DeepCore;
using DeepCore.Xml;

namespace DeepCrystal.ORM
{
    public class MappingConverter
    {
        #region Singleton
        private static MappingConverter s_instance;
        public static MappingConverter Instance { get { return s_instance == null ? new MappingConverter() : s_instance; } }
        protected MappingConverter() { s_instance = this; }
        #endregion

        public static string MAPPING_SEPARATOR { get; set; } = ":";
        public static string MAPPING_REFERENCE_FORMAT { get; set; } = "{0}Mapping";
        public static string WRAPPER_REFERENCE_FORMAT { get; set; } = "{0}Wrapper";
        public static string MAPPING_REFERENCE_BASE_FORMAT { get; set; } = "MappingReference<{0}>";
        public static string WRAPPER_REFERENCE_BASE_FORMAT { get; set; } = "WrapperStruct<{0}>";
        //---------------------------------------------------------------------------------------------------
        public virtual bool IsPersistField(FieldInfo field)
        {
            var attr = PropertyUtil.GetAttribute<PersistFieldAttribute>(field);
            if (attr != null)
            {
                return true;
            }
            return false;
        }
        public virtual string GetFieldAttribute(Type fieldType)
        {
            if (IsAutoFlushType(fieldType)) { return string.Empty; }
            return $"[Obsolete(\"{fieldType.ToTypeDefineName()} : Not a Mapping Type\")]";
        }
        public virtual bool IsAutoFlushType(Type fieldType)
        {
            if (fieldType.IsPrimitive || fieldType.Namespace == "System") { return true; }
            if (fieldType.IsEnum) { return true; }
            if (fieldType.IsValueType) { return true; }
            if (fieldType.IsInterfaceOf(typeof(IObjectMapping))) { return true; }
            if (fieldType.IsInterfaceOf(typeof(IStructMapping))) { return true; }
            if (fieldType.IsArray && fieldType.HasElementType)
            {
                if (IsAutoFlushType(fieldType.GetElementType())) { return true; }
            }
            else if (typeof(IList).IsAssignableFrom(fieldType) && fieldType.IsGenericType)
            {
                if (IsAutoFlushType(fieldType.GetGenericArguments()[0])) { return true; }
            }
            else if (typeof(IDictionary).IsAssignableFrom(fieldType) && fieldType.IsGenericType)
            {
                if (IsAutoFlushType(fieldType.GetGenericArguments()[1])) { return true; }
            }
            return false;
        }
        public virtual bool IsMappingObject(Type type)
        {
            if (type.IsInterfaceOf(typeof(IObjectMapping)))
            {
                return true;
            }
            else if (IsMappingObjectCollection(type))
            {
                return true;
            }
            else if (IsMappingStructCollection(type))
            {
                return true;
            }
            return false;
        }
        public virtual bool IsMappingObjectCollection(Type type)
        {
            if (type.IsArray && type.HasElementType)
            {
                if (IsMappingObject(type.GetElementType()))
                {
                    return true;
                }
            }
            else if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
            {
                var g_args = type.GetGenericArguments();
                if (IsMappingObject(g_args[0]))
                {
                    return true;
                }
            }
            else if (typeof(IDictionary).IsAssignableFrom(type) && type.IsGenericType)
            {
                var g_args = type.GetGenericArguments();
                if (IsMappingObject(g_args[1]))
                {
                    return true;
                }
            }
            return false;
        }
        public virtual bool IsMappingStructCollection(Type type)
        {
            if (type.IsArray && type.HasElementType)
            {
                if (type.GetElementType().IsInterfaceOf(typeof(IStructWrapper)))
                {
                    return false;
                }
                if (type.GetElementType().IsInterfaceOf(typeof(IStructMapping)))
                {
                    return true;
                }
            }
            else if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
            {
                var g_args = type.GetGenericArguments();
                if (g_args[0].IsInterfaceOf(typeof(IStructWrapper)))
                {
                    return false;
                }
                if (g_args[0].IsInterfaceOf(typeof(IStructMapping)))
                {
                    return true;
                }
            }
            else if (typeof(IDictionary).IsAssignableFrom(type) && type.IsGenericType)
            {
                var g_args = type.GetGenericArguments();
                if (g_args[1].IsInterfaceOf(typeof(IStructWrapper)))
                {
                    return false;
                }
                if (g_args[1].IsInterfaceOf(typeof(IStructMapping)))
                {
                    return true;
                }
            }
            return false;
        }


        //---------------------------------------------------------------------------------------------------

        public virtual bool IsWrapperType(Type type)
        {
            if (type.IsInterfaceOf(typeof(IStructMapping)))
            {
                return true;
            }
            if (type.IsInterfaceOf(typeof(IPrimitiveWrapper)))
            {
                return false;
            }
            if (type.IsArray)
            {
                return false;
            }
            if (typeof(IList).IsAssignableFrom(type))
            {
                return true;
            }
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return true;
            }
            return false;
        }


        protected virtual bool TryGetMappingType(Type type, out Type mappingType)
        {
            if (typeof(IObjectMapping).IsAssignableFrom(type))
            {
                mappingType = typeof(DeepCrystal.ORM.Generic.MappingReference<>);
                mappingType = mappingType.MakeGenericType(type);
                return true;
            }
            if (IsMappingObject(type))
            {
                if (type.GetInterface(typeof(IList).Name) != null)
                {
                    var g_args = type.GetGenericArguments();
                    if (TryGetMappingType(g_args[0], out var g_m_type))
                    {
                        mappingType = typeof(DeepCrystal.ORM.Generic.MappingList<,>);
                        mappingType = mappingType.MakeGenericType(g_args[0], g_m_type);
                        return true;
                    }
                    if (TryGetWrapperType(g_args[0], out var g_w_type))
                    {
                        mappingType = typeof(DeepCrystal.ORM.Generic.MappingList<,>);
                        mappingType = mappingType.MakeGenericType(g_args[0], g_w_type);
                        return true;
                    }
                }
                if (type.IsInterfaceOf(typeof(IDictionary)))
                {
                    var g_args = type.GetGenericArguments();
                    if (TryGetMappingType(g_args[1], out var g_m_type))
                    {
                        mappingType = typeof(DeepCrystal.ORM.Generic.MappingDictionary<,,>);
                        mappingType = mappingType.MakeGenericType(g_args[0], g_args[1], g_m_type);
                        return true;
                    }
                    if (TryGetWrapperType(g_args[1], out var g_w_type))
                    {
                        mappingType = typeof(DeepCrystal.ORM.Generic.MappingDictionary<,,>);
                        mappingType = mappingType.MakeGenericType(g_args[0], g_args[1], g_w_type);
                        return true;
                    }
                }
            }
            mappingType = null;
            return false;
        }

        public virtual bool TryGetWrapperType(Type type, out Type wtype)
        {
            if (type.IsInterfaceOf(typeof(IStructMapping)))
            {
                var w_type = typeof(WrapperStruct<>);
                wtype = w_type.MakeGenericType(type);
                return true;
            }
            if (type.IsInterfaceOf(typeof(IPrimitiveWrapper)))
            {
                wtype = null;
                return false;
            }
            if (type.IsArray)
            {
                if (type.GetElementType().IsValueType)
                {
                    wtype = null;
                    return false;
                }
                else
                {
                    wtype = null;
                    return false;
                }
            }
            // TODO ARRAY
            if (type.IsInterfaceOf(typeof(IList)))
            {
                var g_args = type.GetGenericArguments();
                if (TryGetWrapperType(g_args[0], out var e_type))
                {
                    var w_type = typeof(WrapperList<,>);
                    wtype = w_type.MakeGenericType(g_args[0], e_type);
                }
                else
                {
                    var w_type = typeof(SimpleWrapperList<>);
                    wtype = w_type.MakeGenericType(g_args);
                }
                return true;
            }
            if (type.IsInterfaceOf(typeof(IDictionary)))
            {
                var g_args = type.GetGenericArguments();
                if (TryGetWrapperType(g_args[1], out var v_type))
                {
                    var w_type = typeof(WrapperHashMap<,,>);
                    wtype = w_type.MakeGenericType(g_args[0], g_args[1], v_type);
                }
                else
                {
                    var w_type = typeof(SimpleWrapperHashMap<,>);
                    wtype = w_type.MakeGenericType(g_args);
                }
                return true;
            }
            wtype = null;
            return false;
        }
        public virtual string GetSubMappingName(string ownerKey, string fieldName, Type type)
        {
            var sub_key = ownerKey + MAPPING_SEPARATOR + fieldName;
            return sub_key;
        }

        public virtual MappingObject CreateSubMapping(MappingObject owner, string fieldName, Type type)
        {
            if (TryGetMappingType(type, out var mappingType))
            {
                var sub_key = GetSubMappingName(owner.Key, fieldName, type);
                var sub = (MappingObject)DeepActivator.CreateInstance(mappingType,
                    bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                   args: new object[] { sub_key, owner.Executor, owner.Adapter },
                   culture: null,
                   activationAttributes: null);
                sub.SetParent(owner);
                return sub;
            }
            return null;
        }
        public virtual IWrapper CreateWrapper(Type type, IMappingNode owner, object data = null)
        {
            if (data != null)
            {
                type = data.GetType();
            }
            if (TryGetWrapperType(type, out var wtype))
            {
                var sub = (IWrapper)DeepActivator.CreateInstance(wtype,
                    bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, null, null);
                if (data != null)
                {
                    sub.Data = data;
                }
                sub.setParent(owner);
                return sub;
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------


        public virtual string GetMappingName(Type type)
        {
            try
            {
                if (typeof(IObjectMapping).IsAssignableFrom(type))
                {
                    return string.Format(MAPPING_REFERENCE_FORMAT, type.ToTypeDefineFullName());
                }
                if (IsMappingObject(type))
                {
                    if (type.GetInterface(typeof(IList).Name) != null)
                    {
                        var g_args = type.GetGenericArguments();
                        if (TryGetMappingType(g_args[0], out var g_m_type))
                        {
                            var mtype = typeof(DeepCrystal.ORM.Generic.MappingList<,>);
                            return $"{mtype.Namespace}.{mtype.ToNoGenericName()}<{g_args[0].ToTypeDefineFullName()}, {GetMappingName(g_args[0])}>";
                        }
                        if (TryGetWrapperType(g_args[0], out var g_w_type))
                        {
                            var mtype = typeof(DeepCrystal.ORM.Generic.MappingList<,>);
                            return $"{mtype.Namespace}.{mtype.ToNoGenericName()}<{g_args[0].ToTypeDefineFullName()}, {GetWrapperName(g_args[0])}>";
                        }
                    }
                    if (type.IsInterfaceOf(typeof(IDictionary)))
                    {
                        var g_args = type.GetGenericArguments();
                        if (TryGetMappingType(g_args[1], out var g_m_type))
                        {
                            var mtype = typeof(DeepCrystal.ORM.Generic.MappingDictionary<,,>);
                            return $"{mtype.Namespace}.{mtype.ToNoGenericName()}<{g_args[0].ToTypeDefineFullName()}, {g_args[1].ToTypeDefineFullName()}, {GetMappingName(g_args[1])}>";
                        }
                        else if (TryGetWrapperType(g_args[1], out var g_w_type))
                        {
                            var mtype = typeof(DeepCrystal.ORM.Generic.MappingDictionary<,,>);
                            return $"{mtype.Namespace}.{mtype.ToNoGenericName()}<{g_args[0].ToTypeDefineFullName()}, {g_args[1].ToTypeDefineFullName()}, {GetWrapperName(g_args[1])}>";
                        }
                    }
                }

                return "/* No Mapping Type : " + type.FullName + " */";
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                Console.WriteLine(err.StackTrace);
                return err.Message;
            }
        }
        public virtual string GetWrapperName(Type type)
        {
            try
            {
                if (type.IsInterfaceOf(typeof(IStructMapping)))
                {
                    return string.Format(WRAPPER_REFERENCE_FORMAT, type.ToTypeDefineFullName());
                }
                if (TryGetWrapperType(type, out var wtype))
                {
                    if (type.IsInterfaceOf(typeof(IList)))
                    {
                        var g_args = type.GetGenericArguments();
                        if (TryGetWrapperType(g_args[0], out var e_type))
                        {
                            var mtype = typeof(WrapperList<,>);
                            return $"{mtype.Namespace}.{mtype.ToNoGenericName()}<{g_args[0].ToTypeDefineFullName()}, {GetWrapperName(g_args[0])}>";
                        }
                        else
                        {
                            var mtype = typeof(SimpleWrapperList<>);
                            return $"{mtype.Namespace}.{mtype.ToNoGenericName()}<{g_args[0].ToTypeDefineFullName()}>";
                        }
                    }
                    if (type.IsInterfaceOf(typeof(IDictionary)))
                    {
                        var g_args = type.GetGenericArguments();
                        if (TryGetWrapperType(g_args[1], out var v_type))
                        {
                            var mtype = typeof(WrapperHashMap<,,>);
                            return $"{mtype.Namespace}.{mtype.ToNoGenericName()}<{g_args[0].ToTypeDefineFullName()}, {g_args[1].ToTypeDefineFullName()}, {GetWrapperName(g_args[1])}>";
                        }
                        else
                        {
                            var mtype = typeof(SimpleWrapperHashMap<,>);
                            return $"{mtype.Namespace}.{mtype.ToNoGenericName()}<{g_args[0].ToTypeDefineFullName()}, {g_args[1].ToTypeDefineFullName()}>";
                        }
                    }
                }
                return "/* No Wrapper Type : " + type.FullName + " */";
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                Console.WriteLine(err.StackTrace);
                return err.Message;
            }
        }

        public virtual string GetBaseMappingName(Type type)
        {
            if (typeof(IObjectMapping).IsAssignableFrom(type.BaseType))
            {
                return GetMappingName(type.BaseType);
            }
            if (typeof(IObjectMapping).IsAssignableFrom(type))
            {
                return string.Format(MAPPING_REFERENCE_BASE_FORMAT, type.ToTypeDefineFullName());
            }
            return "/* No Mapping Type : " + type.FullName + " */";
        }
        public virtual string GetBaseWrapperName(Type type)
        {
            if (type.BaseType.IsInterfaceOf(typeof(IStructMapping)))
            {
                return GetWrapperName(type.BaseType);
            }
            if (type.IsInterfaceOf(typeof(IStructMapping)))
            {
                return string.Format(WRAPPER_REFERENCE_BASE_FORMAT, type.ToTypeDefineFullName());
            }
            return "/* No Mapping Type : " + type.FullName + " */";
        }

        public virtual bool IsRootMapping(Type type)
        {
            if (typeof(IObjectMapping).IsAssignableFrom(type.BaseType))
            {
                return false;
            }
            if (type.BaseType.IsInterfaceOf(typeof(IStructMapping)))
            {
                return false;
            }
            return true;
        }
    }

    public class MappingObjectXmlSerializer : XmlSerializer
    {
        public MappingObjectXmlSerializer(bool clone = false) : base(clone)
        {
        }

        protected override bool AcceptField(Type type, FieldInfo field)
        {
            if (type.IsInterfaceOf(typeof(IObjectMapping)))
            {
                var attr = field.GetAttribute<PersistFieldAttribute>();
                return (attr != null);
            }
            return true;
        }
        protected override bool AcceptProperty(Type type, PropertyInfo property, bool read)
        {
            if (read) return true;
            return false;
        }
    }
}
