using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using DeepCore.Formula;

namespace DeepCore.Reflection
{
    /// <summary>
    /// 编辑器支持，描述性文件
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class DescAttribute : System.Attribute
    {
        public string Desc = string.Empty;
        public string Category = string.Empty;
        public bool Editable = true;
        public string Detail = string.Empty;
        public DescAttribute()
        {
        }
        public DescAttribute(string desc)
            : this(desc, "", true, null)
        {
        }
        public DescAttribute(string desc, bool displayName)
        : this(desc, "", true, null)
        {

        }
        public DescAttribute(string desc, string category)
            : this(desc, category, true, null)
        {
        }
        public DescAttribute(string desc, string category, bool editable)
            : this(desc, category, editable, null)
        {
        }
        public DescAttribute(string desc, string category, bool editable, string detail)
        {
            this.Desc = desc;
            this.Category = category;
            this.Editable = editable;
            this.Detail = detail;
        }
        public override string ToString()
        {
            return Desc;
        }
        public static string GetDesc(MemberInfo type)
        {
            if (type.TryGetAttribute<DescAttribute>(out var desc)) { return desc.Desc; }
            return type.Name;
        }
    }

    /// <summary>
    /// 标记此类以及子类需要被反射
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ReflectibleAttribute : System.Attribute
    {

    }

    /// <summary>
    /// 编辑器支持，描述性文件
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class KeyAttribute : System.Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class SequenceAttribute : System.Attribute
    {
        public int Index { get; }
        public SequenceAttribute(int i) { this.Index = i; }
    }

    //     /// <summary>
    //     /// 标记一个属性可动态调用（获取/设置）(DynamicTypeFactory)
    //     /// </summary>
    //     [AttributeUsage(AttributeTargets.Property)]
    //     public class DynamicPropertyAttribute : Attribute { }

    /// <summary>
    /// 标记一个方法可动态调用获取(DynamicTypeFactory)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DynamicGetMethodAttribute : Attribute { }

    /// <summary>
    /// 标记一个方法可动态调用设置(DynamicTypeFactory)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DynamicSetMethodAttribute : Attribute { }

    /// <summary>
    /// 某个字段依赖于某个开关（字段或者属性）
    /// 多个为 And 关系
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class DependOnPropertyAttribute : System.Attribute
    {
        readonly BooleanOP op = BooleanOP.AND;
        readonly string property_name;
        readonly bool expect;
        public DependOnPropertyAttribute(string property_name, bool expect = true, BooleanOP op = BooleanOP.AND)
        {
            this.property_name = property_name;
            this.expect = expect;
            this.op = op;
        }
        public string MemberName { get { return property_name; } }
        public bool Expect { get { return expect; } }
        public BooleanOP OP { get { return op; } }


        public static List<FieldInfo> ListDependFields(Type type, MemberInfo member)
        {
            var ret = new List<FieldInfo>();
            ListDependFields(ret, type, member);
            return ret;
            void ListDependFields(List<FieldInfo> list, Type type, MemberInfo member)
            {
                if (member.TryGetAttributes<DependOnPropertyAttribute>(out var pdds))
                {
                    foreach (var pdd in pdds)
                    {
                        {
                            var fd = type.GetField(pdd.MemberName);
                            if (fd != null)
                            {
                                list.Add(fd);
                                ListDependFields(list, type, fd);
                                continue;
                            }
                        }
                        {
                            var pd = type.GetProperty(pdd.MemberName);
                            if (pd != null)
                            {
                                ListDependFields(list, type, pd);
                                continue;
                            }
                        }
                        throw new Exception($"Can not find MemberName '{pdd.MemberName}' in '{member.Name}' @{type.FullName}");
                    }
                }
            }
        }


        /// <summary>
        /// 确认一个变量名在依赖列表里
        /// </summary>
        public static bool TryFindInDepend(Type type, MemberInfo member, string fieldName, out FieldInfo fieldInfo)
        {
            fieldInfo = null;
            if (member == null) return false;
            return TryFindInDepend(member, type, member, fieldName, out fieldInfo);
            bool TryFindInDepend(MemberInfo rootMember, Type type, MemberInfo member, string fieldName, out FieldInfo fieldInfo)
            {
                //             if (rootMember == member)
                //             {
                //                 throw new Exception($"Depend on recursion : '{member.Name}' @{type.FullName}");
                //             }
                if (member.TryGetAttributes<DependOnPropertyAttribute>(out var pdds))
                {
                    foreach (var pdd in pdds)
                    {
                        {
                            var fd = type.GetField(pdd.MemberName);
                            if (fd != null)
                            {
                                if (fd.Name == fieldName)
                                {
                                    fieldInfo = fd;
                                    return true;
                                }
                                if (TryFindInDepend(rootMember, type, fd, fieldName, out fieldInfo))
                                {
                                    return true;
                                }
                                continue;
                            }
                        }
                        {
                            var pd = type.GetProperty(pdd.MemberName);
                            if (pd != null)
                            {
                                if (pd.Name == fieldName)
                                {
                                    throw new Exception($"Depend filed is not FieldInfo '{member.Name}' @{type.FullName}");
                                }
                                if (TryFindInDepend(rootMember, type, pd, fieldName, out fieldInfo))
                                {
                                    return true;
                                }
                                continue;
                            }
                        }
                        throw new Exception($"Can not find MemberName '{pdd.MemberName}' in '{member.Name}' @{type.FullName}");
                    }
                }
                fieldInfo = null;
                return false;
            }
        }


        public static bool IsDepend(FieldInfo field, object data)
        {
            var depends = field.GetCustomAttributes(typeof(DependOnPropertyAttribute), true);
            return IsDepend(Array.ConvertAll(depends, (d) => { return d as DependOnPropertyAttribute; }), data);
        }
        public static bool IsDepend(PropertyInfo field, object data)
        {
            var depends = field.GetCustomAttributes(typeof(DependOnPropertyAttribute), true);
            return IsDepend(Array.ConvertAll(depends, (d) => { return d as DependOnPropertyAttribute; }), data);
        }
        public static bool IsDepend(DependOnPropertyAttribute[] depends, object data)
        {
            if (depends != null && depends.Length > 0)
            {
                bool ret = GetDepend(depends[0], data);
                for (int i = 1; i < depends.Length; i++)
                {
                    var exist = GetDepend(depends[i], data);
                    ret = FormulaHelper.Calculate(ret, depends[i].OP, exist);
                }
                return ret;
            }
            else
            {
                return true;
            }
        }
        private static bool GetDepend(DependOnPropertyAttribute depend, object data)
        {
            var dv = PropertyUtil.GetFieldOrPropertyOrMethodValue<bool>(data, depend.MemberName);
            return depend.expect == dv;
        }
    }

    /// <summary>
    /// 编辑器支持，描述列表的成员类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ListDescAttribute : System.Attribute
    {
        private Type[] elementTypes;

        public ListDescAttribute(params Type[] types)
        {
            var typeSet = new HashMap<Type, Type>();
            foreach (Type type in types)
            {
                if (type.IsAbstract)
                {
                    foreach (Type sub in ReflectionUtil.GetNoneVirtualSubTypes(type))
                    {
                        typeSet.Add(sub, sub);
                    }
                }
                else
                {
                    typeSet.Add(type, type);
                }
            }
            this.elementTypes = typeSet.Keys.ToArray();
        }

        public Type[] GetElementTypes(Type fieldType)
        {
            if (elementTypes != null && elementTypes.Length > 0)
            {
                return elementTypes;
            }
            else
            {
                return ReflectionUtil.GetNoneVirtualSubTypes(fieldType).ToArray();
            }
        }
    }

    /// <summary>
    /// 编辑器支持，描述一个类的主键是什么
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableClassAttribute : System.Attribute
    {
        private string id_field;

        public TableClassAttribute(string key)
        {
            this.id_field = key;
        }

        public string PrimaryKey
        {
            get { return id_field; }
        }
    }

    /// <summary>
    /// 编辑器支持，此类型是可折叠
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
    public class ExpandableAttribute : System.Attribute
    {
        public bool IsExpandable = true;
    }

    /// <summary>
    /// 编辑器支持，字段是否可删除或改变
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NotNullAttribute : System.Attribute
    {

    }

    /// <summary>
    /// 编辑器支持，字段是否可删除或改变
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReadOnlyAttribute : System.Attribute
    {

    }

    /// <summary>
    /// 列取当前字段可能的值
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class OptionalValueAttribute : System.Attribute
    {
        public object[] Values { get; private set; }

        public OptionalValueAttribute(params object[] args)
        {
            this.Values = args;
        }
    }

    /// <summary>
    /// 不在编辑器内显示
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class HideInInspectorAttribute : System.Attribute
    {

    }
    //----------------------------------------------------------------------------
    #region Type

    /// <summary>
    /// 标识 Field 字段为颜色
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class Int32ColorAttribute : System.Attribute
    {

    }
    [AttributeUsage(AttributeTargets.Field)]
    public class HexIntegerAttribute : System.Attribute
    {

    }

    /// <summary>
    /// 标识 Field 字段为文件
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FilePathAttribute : System.Attribute
    {
        public bool IsImage = false;
    }

    /// <summary>
    /// 标识 Field 字段为目录
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DirectoryPathAttribute : System.Attribute
    {

    }


    /// <summary>
    /// 标记对象字段名字，用于生成Language.csv
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LocalizationTextAttribute : System.Attribute
    {
        public LocalizationTextAttribute()
        {
        }
    }

    #endregion
}
