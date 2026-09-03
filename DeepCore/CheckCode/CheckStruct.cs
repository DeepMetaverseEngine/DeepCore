using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DeepCore.CheckCode
{

    public class CheckCodeStruct
    {
        class CodeStructNode
        {
            internal Type MemberType
            {
                get
                {
                    if (Member is FieldInfo)
                    {
                        return (Member as FieldInfo).FieldType;
                    } 
                    if (Member is PropertyInfo)
                    {
                        return (Member as PropertyInfo).PropertyType;
                    }
                    return null;
                }
            }
            internal readonly Type OwnerType;
            internal readonly MemberInfo Member;
            internal CodeStructNode(Type ownerType, MemberInfo member)
            {
                this.OwnerType = ownerType;
                this.Member = member;
            }
        }

        public Type BaseType { get; private set; }
        public Type[] DeclaredTypes { get; private set; }

        private List<CodeStructNode> StructNodes = new List<CodeStructNode>();

        public CheckCodeStruct(Type baseType, params Type[] declears)
        {
            this.BaseType = baseType;
            this.DeclaredTypes = declears;

            foreach (Type subtype in ReflectionUtil.GetSubTypes(baseType))
            {
                foreach (FieldInfo field in subtype.GetFields())
                {
                    if (IsDeclaredType(field.FieldType))
                    {
                        StructNodes.Add(new CodeStructNode(subtype, field));
                    }
                }
                foreach (PropertyInfo prop in subtype.GetProperties())
                {
                    if (IsDeclaredType(prop.PropertyType))
                    {
                        StructNodes.Add(new CodeStructNode(subtype, prop));
                    }
                }
            }
        }

        public bool IsDeclaredType(Type type)
        {
            foreach (Type de in DeclaredTypes)
            {
                if (de.IsAssignableFrom(type))
                {
                    return true;
                }
            }
            return false;
        }

        public string GenInfo()
        {
            StringBuilder sb = new StringBuilder();
            foreach (CodeStructNode node in StructNodes)
            {
                sb.AppendLine(node.OwnerType.FullName);
                sb.AppendLine("\t" + node.Member.Name + " : " + node.MemberType.Name);
            }
            return sb.ToString();
        }
    }
}
