using System;
using System.Collections.Generic;
using DeepCore;
using System.Reflection;
using DeepCore.Reflection;
using DeepCore.Log;

namespace DeepCore.Reflection
{
    public class FieldManager
    {
        private HashMap<Type, FieldsType> AllMaps = new HashMap<Type, FieldsType>();
        public void AddFieldsType(FieldsType fm)
        {
            AllMaps.Add(fm.ObjectType, fm);
        }
        public FieldsType GetFields(Type objectType)
        {
            var submap = AllMaps.Get(objectType);
            if (submap != null)
            {
                return submap;
            }
            return null;
        }
    }
    public class FieldsType
    {
        protected static Logger log = new LazyLogger(nameof(FieldsType));
        public Type ObjectType { get; }

        private readonly HashSet<Type> compatibilityFieldTypes = new HashSet<Type>();
        private readonly HashMap<string, MemberDescAttribute> getFields = new();
        private readonly HashMap<string, MemberDescAttribute> setFields = new();

        public IReadOnlyDictionary<string, MemberDescAttribute> GetFields => getFields;
        public IReadOnlyDictionary<string, MemberDescAttribute> SetFields => setFields;

        public FieldsType(Type objType, params Type[] compatibilityFieldTypes)
        {
            this.ObjectType = objType;
            this.compatibilityFieldTypes.AddRange(compatibilityFieldTypes);

            foreach (var ff in ObjectType.GetFields())
            {
                try
                {
                    if (!ff.IsLiteral && !ff.IsStatic && ff.IsPublic && this.compatibilityFieldTypes.Contains(ff.FieldType))
                    {
                        var fda = new MemberDescAttribute(ff, ff.FieldType);
                        getFields.Put(ff.Name, fda);
                        setFields.Put(ff.Name, fda);
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            foreach (var ff in ObjectType.GetProperties())
            {
                try
                {
                    if (this.compatibilityFieldTypes.Contains(ff.PropertyType))
                    {
                        var fda = new MemberDescAttribute(ff, ff.PropertyType);
                        if (ff.CanRead && !ff.GetMethod.IsStatic && ff.GetMethod.IsPublic)
                        {
                            getFields.Put(ff.Name, fda);
                        }
                        if (ff.CanWrite && !ff.SetMethod.IsStatic && ff.SetMethod.IsPublic)
                        {
                            setFields.Put(ff.Name, fda);
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            foreach (var ff in ObjectType.GetMethods())
            {
                try
                {
                    if (!ff.IsStatic && ff.IsPublic)
                    {
                        var paras = ff.GetParameters();
                        if (ff.ReturnType != typeof(void))
                        {
                            if (paras.Length == 0 && this.compatibilityFieldTypes.Contains(ff.ReturnType))
                            {
                                getFields.Put(ff.Name, new MemberDescAttribute(ff, ff.ReturnType));
                            }
                        }
                        else
                        {
                            if (paras.Length == 1 && this.compatibilityFieldTypes.Contains(paras[0].ParameterType))
                            {
                                setFields.Put(ff.Name, new MemberDescAttribute(ff, paras[0].ParameterType));
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
        }
        public override string ToString()
        {
            return ObjectType.FullName;
        }

        public bool TryGetValueAs<T>(object owner, string fieldName, out T ret)
        {
            if (getFields.TryGetValue(fieldName, out var get))
            {
                var o = get.GetValue(owner);
                try
                {
                    return CUtils.TryConvertTo<T>(o, out ret);
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            ret = default(T);
            return false;
        }
        public bool TryGetValue(object owner, string fieldName, out object ret)
        {
            if (getFields.TryGetValue(fieldName, out var get))
            {
                ret = get.GetValue(owner);
                return true;//(T)Convert.ChangeType(dv, typeof(T));
            }
            ret = null;
            return false;
        }
        public T GetValueAs<T>(object owner, string fieldName)
        {
            if (getFields.TryGetValue(fieldName, out var get))
            {
                var dv = get.GetValue(owner);
                try
                {
                    var ret = CUtils.ConvertTo<T>(dv);
                    return ret;
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            return default(T);
        }
        public object GetValue(object owner, string fieldName)
        {
            if (getFields.TryGetValue(fieldName, out var get))
            {
                var dv = get.GetValue(owner);
                return dv;//(T)Convert.ChangeType(dv, typeof(T));
            }
            return null;
        }

        public void SetValue(object owner, string fieldName, object value)
        {
            if (setFields.TryGetValue(fieldName, out var set))
            {
                try
                {
                    var dv = CUtils.ConvertTo(value, set.FieldType);
                    set.SetValue(owner, value);
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
        }



        public class MemberDescAttribute : IComparable<MemberDescAttribute>
        {
            public readonly DescAttribute Desc;
            public readonly MemberInfo DataMember;
            public readonly Type FieldType;
            private readonly IDynamicMemberInfo DynamicField;
            public string Name { get => DataMember.Name; }
            public Type DeclaringType { get => DataMember.DeclaringType; }
            public MemberDescAttribute(MemberInfo member, Type fieldType)
            {
                DataMember = member;
                FieldType = fieldType;
                Desc = PropertyUtil.GetAttribute<DescAttribute>(member);
                if (member is FieldInfo field)
                {
                    DynamicTypeFactory.Instance.CreateMemberInfo(field, out DynamicField);
                }
                else if (member is PropertyInfo property)
                {
                    DynamicTypeFactory.Instance.CreateMemberInfo(property, out DynamicField);
                }
                else if (member is MethodInfo method)
                {
                    DynamicTypeFactory.Instance.CreateMemberInfo(method, out DynamicField);
                }
                else
                {
                    throw new Exception("Error MemberDescAttribute : " + member);
                }
            }
            public override string ToString()
            {
                if (DataMember is MethodInfo)
                {
                    return ($"{DataMember.DeclaringType.Name} -> {Name}(); {(Desc != null ? " - " + Desc.Desc : string.Empty)}");
                }
                else
                {
                    return ($"{DataMember.DeclaringType.Name} -> {Name}; {(Desc != null ? " - " + Desc.Desc : string.Empty)}");
                }
            }
            public int CompareTo(MemberDescAttribute other)
            {
                return DataMember.Name.CompareTo(other.DataMember.Name);
            }

            public object GetValue(object owner)
            {
                try
                {
                    if (DynamicField != null)
                    {
                        return DynamicField.GetValue(owner);
                    }
                    else if (DataMember is FieldInfo field)
                    {
                        return field.GetValue(owner);
                    }
                    else if (DataMember is PropertyInfo property)
                    {
                        return property.GetMethod.Invoke(owner, new object[0]);
                    }
                    else if (DataMember is MethodInfo method)
                    {
                        return method.Invoke(owner, new object[0]);
                    }
                }
                catch (Exception err) { PropertyUtil.log.Error(err.Message, err); }
                return null;
            }

            public void SetValue(object owner, object value)
            {
                try
                {
                    if (DynamicField != null)
                    {
                        DynamicField.SetValue(owner, value);
                    }
                    else if (DataMember is FieldInfo field)
                    {
                        field.SetValue(owner, value);
                    }
                    else if (DataMember is PropertyInfo property)
                    {
                        property.SetMethod.Invoke(owner, new object[] { value });
                    }
                    else if (DataMember is MethodInfo method)
                    {
                        method.Invoke(owner, new object[] { value });
                    }
                }
                catch (Exception err) { PropertyUtil.log.Error(err.Message, err); }
            }

        }

    }
}
