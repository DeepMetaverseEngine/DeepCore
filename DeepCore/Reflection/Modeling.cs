using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using DeepCore.Xml;

namespace DeepCore.Reflection.Modeling
{
    public class UmlNode : IComparable<UmlNode>
    {
        public string Name { get; private set; }
        public HashMap<string, object> Attributes { get; private set; }
        public UmlDocument Root { get; private set; }

        protected UmlNode(UmlDocument root, string name)
        {
            this.Root = root;
            this.Name = name;
            this.Attributes = new HashMap<string, object>();
        }

        public UmlNode ParentNode { get { return mParent; } }
        public IEnumerable<UmlNode> ChildNodes { get { return mChildNodes; } }

        private UmlNode mParent;
        private List<UmlNode> mChildNodes = new List<UmlNode>();
        private HashMap<string, UmlNode> mChildNodesMap = new HashMap<string, UmlNode>();
        public UmlNode this[string key] { get { return mChildNodesMap.Get(key); } }

        internal void AppendChild(UmlNode child)
        {
            if (child.mParent != null)
            {
                throw new Exception(string.Format("Child already have a parent : child={0} parent={1}", child.Name, child.mParent.Name));
            }
            child.mParent = this;
            this.mChildNodes.Add(child);
            this.mChildNodes.Sort();
            this.mChildNodesMap.Add(child.Name, child);
        }
        internal void ClearChilds()
        {
            foreach (UmlNode node in mChildNodes)
            {
                node.mParent = null;
            }
            this.mChildNodes.Clear();
            this.mChildNodesMap.Clear();
        }
        public UmlNode GetChild(string key)
        {
            return this.mChildNodesMap.Get(key);
        }
        public virtual int CompareTo(UmlNode other)
        {
            return this.Name.CompareTo(other.Name);
        }

    }

    public class UmlDocument : UmlNode
    {
        public object Data;
        public readonly UmlValueNode DocumentElement;
        private TableClassAttribute mDataTypeDesc;

        public TableClassAttribute DataTypeDesc { get { return mDataTypeDesc; } }

        public UmlDocument(object data)
            : base(null, data.GetType().Name)
        {
            this.mDataTypeDesc = PropertyUtil.GetAttribute<TableClassAttribute>(data.GetType());
            this.Data = data;
            this.DocumentElement = CreateValueNode("Document", data.GetType(), null);
            this.DocumentElement.BindValue(data, null);
            this.AppendChild(DocumentElement);
        }

        public UmlValueNode GetPath(string path)
        {
            string[] paths = path.Split('.');
            UmlNode nd = this.DocumentElement;
            for (int i = 0; i < paths.Length; i++)
            {
                if (nd != null)
                {
                    nd = nd[paths[i]];
                }
                else
                {
                    return null;
                }
            }
            return nd as UmlValueNode;
        }
        public virtual UmlValueNode CreateValueNode(string name, Type type, object owner)
        {
            if (type.IsArray)
            {
                return new UmlArrayNode(this, name, type, owner);
            }
            else if (type.IsPrimitive)
            {
                return new UmlLeafValueNode(this, name, type, owner);
            }
            else if (type.IsEnum)
            {
                return new UmlLeafValueNode(this, name, type, owner);
            }
            else if (type == (typeof(string)))
            {
                return new UmlLeafValueNode(this, name, type, owner);
            }
            else if (type.IsClass)
            {
                if (type.GetInterface(typeof(IDictionary).Name) != null)
                {
                    return new UmlMapNode(this, name, type, owner);
                }
                else if (type.GetInterface(typeof(IList).Name) != null)
                {
                    return new UmlListNode(this, name, type, owner);
                }
                else
                {
                    return new UmlFieldsValueNode(this, name, type, owner);
                }
            }
            else
            {
                return new UmlFieldsValueNode(this, name, type, owner);
            }
        }
    }

    public abstract class UmlValueNode : UmlNode
    {
        public UmlValueNode ParentValueNode { get { return ParentNode as UmlValueNode; } }
        public object OwnerIndexer { get { return mOwnerIndexer; } }
        public object OwnerObject { get { return mOwnerObject; } }
        public Type ValueType { get { return mValueType; } }
        public object Value { get { return mValue; } }
        public MemberInfo FieldMember { get => OwnerIndexer as MemberInfo; }
        public Type DeclearValueType
        {
            get
            {
                if (OwnerIndexer is FieldInfo ff)
                {
                    return ff.FieldType;
                }
                if (OwnerIndexer is PropertyInfo pp)
                {
                    return pp.PropertyType;
                }
                return null;
            }
        }
        public DescAttribute FieldDesc
        {
            get
            {
                if (OwnerIndexer is FieldInfo)
                {
                    return PropertyUtil.GetAttribute<DescAttribute>(OwnerIndexer as FieldInfo);
                }
                if (OwnerIndexer is PropertyInfo)
                {
                    return PropertyUtil.GetAttribute<DescAttribute>(OwnerIndexer as PropertyInfo);
                }
                return null;
            }
        }
        public bool IsRoot
        {
            get { return this.OwnerObject == null; }
        }
        public bool IsLeaf
        {
            get
            {
                if (mValueType.IsPrimitive)
                {
                    return true;
                }
                if (mValueType.IsEnum)
                {
                    return true;
                }
                if (mValueType == (typeof(string)))
                {
                    return true;
                }
                if (mValueType.IsClass)
                {
                    return false;
                }
                return false;
            }
        }

        private readonly Type mValueType;
        private readonly object mOwnerObject;
        private object mOwnerIndexer;
        private object mValue;

        protected internal UmlValueNode(UmlDocument root, string name, Type type, object owner)
            : base(root, name)
        {
            this.mValueType = type;
            this.mOwnerObject = owner;
        }
        internal void BindValue(object value, object key)
        {
            this.mValue = value;
            this.mOwnerIndexer = key;
            this.ClearChilds();
            this.GenChilds();
        }
        protected abstract void GenChilds();

        /// <summary>
        /// 设置当前节点值（此操作 将改变当前节点结构）
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(object value)
        {
            if (ParentNode is UmlValueNode)
            {
                UmlValueNode parent = ParentNode as UmlValueNode;
                parent.SetFieldValue(this, value);
            }
        }
        /// <summary>
        /// 设置当前节点值（此操作 将改变当前节点结构）
        /// </summary>
        public abstract void SetFieldValue(UmlValueNode child, object value);



        public string GetUMLPath(string sparator = ";")
        {
            StringBuilder line = new StringBuilder();
            UmlValueNode it = this;
            while (!it.IsRoot)
            {
                UmlValueNode vt = it as UmlValueNode;
                line.Insert(0, sparator);
                line.Insert(0, vt.Name);
                if (it.ParentNode is UmlValueNode pnode)
                {
                    it = pnode;
                }
                else
                {
                    break;
                }
            }
            // line.Insert(0, "");
            return line.ToString();
        }
    }

    public class UmlFieldsValueNode : UmlValueNode
    {
        protected internal UmlFieldsValueNode(UmlDocument root, string name, Type type, object owner)
            : base(root, name, type, owner)
        {
        }

        protected override void GenChilds()
        {
            if (Value != null && !IsLeaf)
            {
                foreach (FieldInfo field in ValueType.GetFields())
                {
                    if (!field.IsStatic && !field.IsLiteral && !field.IsInitOnly && field.IsPublic)
                    {
                        Type ft = field.FieldType;
                        object fv = field.GetValue(Value);
                        if (fv != null) { ft = fv.GetType(); }
                        UmlValueNode fe = Root.CreateValueNode(field.Name, ft, Value);
                        fe.BindValue(fv, field);
                        AppendChild(fe);
                    }
                }
                foreach (PropertyInfo field in ValueType.GetProperties())
                {
                    if (field.CanRead && field.CanWrite &&
                        !field.GetMethod.IsStatic && field.GetMethod.IsPublic &&
                        !field.SetMethod.IsStatic && field.SetMethod.IsPublic &&
                        field.PropertyType.IsPrimitiveData())
                    {
                        if (field.TryGetAttribute<DescAttribute>(out var desc) && desc.Editable)
                        {
                            try
                            {
                                object fv = field.GetMethod.Invoke(Value, new object[0]);
                                Type ft = field.PropertyType;
                                if (fv != null) { ft = fv.GetType(); }
                                UmlValueNode fe = Root.CreateValueNode(field.Name, ft, Value);
                                fe.BindValue(fv, field);
                                AppendChild(fe);
                            }
                            catch { }
                        }
                    }
                }
            }
        }
        public override void SetFieldValue(UmlValueNode child, object value)
        {
            child = GetChild(child.Name) as UmlValueNode;
            if (child != null)
            {
                FieldInfo field = child.OwnerIndexer as FieldInfo;
                field.SetValue(this.Value, value);
                child.BindValue(value, field);
            }
        }
    }
    public class UmlLeafValueNode : UmlValueNode
    {
        protected internal UmlLeafValueNode(UmlDocument root, string name, Type type, object owner)
            : base(root, name, type, owner)
        {
        }

        protected override void GenChilds()
        {
        }
        public override void SetFieldValue(UmlValueNode child, object value)
        {
        }
    }

    public class UmlArrayNode : UmlValueNode
    {
        readonly public Type ElementType;
        readonly public int Rank;
        public ListDescAttribute ListDesc
        {
            get
            {
                if (OwnerIndexer is FieldInfo)
                {
                    return PropertyUtil.GetAttribute<ListDescAttribute>(OwnerIndexer as FieldInfo);
                }
                return null;
            }
        }
        public Array ArrayValue
        {
            get
            {
                if (Value != null) { return (Array)Value; }
                return null;
            }
        }

        protected internal UmlArrayNode(UmlDocument root, string name, Type type, object owner)
            : base(root, name, type, owner)
        {
            this.ElementType = ValueType.GetElementType();
            this.Rank = ValueType.GetArrayRank();
        }
        protected override void GenChilds()
        {
            if (Value != null)
            {
                Array array = (Array)Value;
                int i = 0;
                foreach (object v in array)
                {
                    if (v != null)
                    {
                        Type vtype = v.GetType();
                        UmlValueNode ei = Root.CreateValueNode(i.ToString(), vtype, Value);
                        ei.BindValue(v, i);
                        AppendChild(ei);
                    }
                    i++;
                }
            }
        }
        public override void SetFieldValue(UmlValueNode child, object value)
        {
            child = GetChild(child.Name) as UmlValueNode;
            if (child != null)
            {
                int i = (int)child.OwnerIndexer;
                Array array = (Array)this.Value;
                array.SetValue(value, i);
                child.BindValue(value, i);
            }
        }
    }

    public class UmlListNode : UmlValueNode
    {
        readonly public Type GenericElementType;
        public ListDescAttribute ListDesc
        {
            get
            {
                if (OwnerIndexer is FieldInfo)
                {
                    return PropertyUtil.GetAttribute<ListDescAttribute>(OwnerIndexer as FieldInfo);
                }
                return null;
            }
        }
        public IList ListValue
        {
            get
            {
                if (Value != null) { return (IList)Value; }
                return null;
            }
        }

        protected internal UmlListNode(UmlDocument root, string name, Type type, object owner)
            : base(root, name, type, owner)
        {
            if (ValueType.IsGenericType)
            {
                GenericElementType = ValueType.GetGenericArguments()[0];
            }
        }
        protected override void GenChilds()
        {
            if (Value != null)
            {
                IList list = (IList)Value;
                int i = 0;
                foreach (object v in list)
                {
                    if (v != null)
                    {
                        Type vtype = v.GetType();
                        UmlValueNode ei = Root.CreateValueNode(i.ToString(), vtype, Value);
                        ei.BindValue(v, i);
                        AppendChild(ei);
                    }
                    i++;
                }
            }
        }
        public override void SetFieldValue(UmlValueNode child, object value)
        {
            child = GetChild(child.Name) as UmlValueNode;
            if (child != null)
            {
                int i = (int)child.OwnerIndexer;
                IList list = (IList)this.Value;
                list[i] = value;
                child.BindValue(value, i);
            }
        }
    }

    public class UmlMapNode : UmlValueNode
    {
        readonly public Type GenericKeyType;
        readonly public Type GenericValueType;
        public IDictionary MapValue
        {
            get
            {
                if (Value != null) { return (IDictionary)Value; }
                return null;
            }
        }

        protected internal UmlMapNode(UmlDocument root, string name, Type type, object owner)
            : base(root, name, type, owner)
        {
            if (ValueType.IsGenericType)
            {
                GenericKeyType = ValueType.GetGenericArguments()[0];
                GenericValueType = ValueType.GetGenericArguments()[1];
            }
            else
            {
                GenericKeyType = null;
                GenericValueType = null;
            }
        }

        protected override void GenChilds()
        {
            if (Value != null)
            {
                IDictionary map = (IDictionary)Value;
                foreach (DictionaryEntry e in map)
                {
                    Type vtype = e.Value.GetType();
                    UmlValueNode ei = Root.CreateValueNode(e.Key.ToString(), vtype, this.Value);
                    ei.BindValue(e.Value, e.Key);
                    AppendChild(ei);
                }
            }
        }
        public override void SetFieldValue(UmlValueNode child, object value)
        {
            child = GetChild(child.Name) as UmlValueNode;
            if (child != null)
            {
                IDictionary map = (IDictionary)this.Value;
                map[child.OwnerIndexer] = value;
                child.BindValue(value, child.OwnerIndexer);
            }
        }
    }


    //---------------------------------------------------------------------------------



    //---------------------------------------------------------------------------------

    public static class UmlUtils
    {


        static class UmlListSameElements
        {
            /// <summary>
            /// 保留结构相同部分
            /// </summary>
            /// <param name="list"></param>
            /// <returns></returns>
            static public void ListSameElements(UmlDocument[] list)
            {



            }

        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// 将两个结构内部配平，
        /// 比如A对象内的一个数组为2个，B对象内部数组为4个，那么调用后，A对象内的数组将补齐为4个
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns>是否发生变化</returns>
        static public bool StructEquationBalancer(object src, object dst)
        {
            return UmlBalancer.StructEquationBalancer(src, dst);
        }
        static class UmlBalancer
        {
            static public bool StructEquationBalancer(object src, object dst)
            {
                if (src == dst)
                {
                    return false;
                }
                if (src.GetType() != dst.GetType())
                {
                    return false;
                }
                bool changed = false;

                foreach (FieldInfo fi in src.GetType().GetFields())
                {
                    if (!fi.IsStatic && !fi.IsLiteral && !fi.IsInitOnly && fi.IsPublic)
                    {
                        Type type = fi.FieldType;
                        if (type.IsPrimitive)
                        {
                        }
                        else if (type.IsEnum)
                        {
                        }
                        else if (type == (typeof(string)))
                        {
                        }
                        else if (type.IsArray)
                        {
                            Array sfva = fi.GetValue(src) as Array;
                            Array dfva = fi.GetValue(dst) as Array;
                            if (BalanceArray(ref sfva, ref dfva))
                            {
                                fi.SetValue(src, sfva);
                                fi.SetValue(dst, dfva);
                                changed = true;
                            }
                        }
                        else if (type.IsClass)
                        {
                            object sfv = fi.GetValue(src);
                            object dfv = fi.GetValue(dst);
                            if (sfv == null && dfv != null)
                            {
                                // 其中一个不为空
                                sfv = XmlUtil.CloneObject(dfv);
                                fi.SetValue(src, sfv);
                                changed = true;
                            }
                            else if (sfv != null && dfv == null)
                            {
                                // 其中一个不为空
                                dfv = XmlUtil.CloneObject(sfv);
                                fi.SetValue(dst, dfv);
                                changed = true;
                            }
                            else if (sfv == null && dfv == null)
                            {
                                // 两个都为空
                            }
                            else if (type.GetInterface(typeof(IDictionary).Name) != null)
                            {
                                if (BalanceMap((IDictionary)sfv, (IDictionary)dfv))
                                {
                                    changed = true;
                                }
                            }
                            else if (type.GetInterface(typeof(IList).Name) != null)
                            {
                                if (BalanceList((IList)sfv, (IList)dfv))
                                {
                                    changed = true;
                                }
                            }
                            else
                            {
                                if (StructEquationBalancer(sfv, dfv))
                                {
                                    changed = true;
                                }
                            }
                        }
                    }
                }

                return changed;
            }

            //---------------------------------------------------------------------------------

            private static bool BalanceMap(IDictionary src, IDictionary dst)
            {
                return false;
            }
            /// <summary>
            /// 将两个数组配平，少的自动补齐
            /// </summary>
            /// <param name="src"></param>
            /// <param name="dst"></param>
            /// <returns></returns>
            private static bool BalanceList(IList src, IList dst)
            {
                bool changed = false;
                if (src.Count != dst.Count)
                {
                    if (src.Count < dst.Count)
                    {
                        for (int i = src.Count; i < dst.Count; i++)
                        {
                            src.Add(XmlUtil.CloneObject(dst[i]));
                        }
                    }
                    else
                    {
                        for (int i = dst.Count; i < src.Count; i++)
                        {
                            dst.Add(XmlUtil.CloneObject(src[i]));
                        }
                    }
                    changed = true;
                }
                for (int i = 0; i < src.Count; i++)
                {
                    object ssi = src[i];
                    object ddi = dst[i];
                    if (StructEquationBalancer(ssi, ddi))
                    {
                        changed = true;
                    }
                }
                return changed;
            }
            /// <summary>
            /// 将两个数组配平，少的自动补齐
            /// </summary>
            /// <param name="src"></param>
            /// <param name="dst"></param>
            /// <returns></returns>
            private static bool BalanceArray(ref Array src, ref Array dst)
            {
                if (src == null && dst != null)
                {
                    // 其中一个不为空
                    src = XmlUtil.CloneObject(dst);
                    return true;
                }
                else if (src != null && dst == null)
                {
                    // 其中一个不为空
                    dst = XmlUtil.CloneObject(src);
                    return true;
                }
                else if (src == null && dst == null)
                {
                    // 两个都为空
                    return false;
                }
                bool changed = false;
                ArrayList lis_s = new ArrayList(src);
                ArrayList lis_d = new ArrayList(dst);
                if (src.Length != dst.Length)
                {
                    if (lis_s.Count < lis_d.Count)
                    {
                        int[] ranges = CUtils.GetArrayRanges(dst);
                        Array array = Array.CreateInstance(dst.GetType(), ranges);
                        FillArray(lis_d, lis_s, array, ranges);
                        src = array;
                    }
                    else
                    {
                        int[] ranges = CUtils.GetArrayRanges(src);
                        Array array = Array.CreateInstance(src.GetType(), ranges);
                        FillArray(lis_s, lis_d, array, ranges);
                        dst = array;
                    }
                    changed = true;
                }
                for (int i = 0; i < src.Length; i++)
                {
                    object ssi = lis_s[i];
                    object ddi = lis_d[i];
                    if (StructEquationBalancer(ssi, ddi))
                    {
                        changed = true;
                    }
                }
                return changed;
            }
            /// <summary>
            /// 将ret按照more标准配平
            /// </summary>
            /// <param name="more"></param>
            /// <param name="less"></param>
            /// <param name="ret"></param>
            /// <param name="ranges"></param>
            private static void FillArray(ArrayList more, ArrayList less, Array ret, int[] ranges)
            {
                for (int i = less.Count; i < more.Count; i++)
                {
                    less.Add(XmlUtil.CloneObject(more[i]));
                }
                int total_index = 0;
                foreach (object fe in less)
                {
                    int[] indices = CUtils.GetArrayRankIndex(ranges, total_index);
                    ret.SetValue(fe, indices);
                    total_index++;
                }
            }

        }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// 深度判断结构是否一致
        /// </summary>
        /// <param name="r0"></param>
        /// <param name="r1"></param>
        /// <returns></returns>
        static public bool StructEquals(List<UmlNode> r0, List<UmlNode> r1)
        {
            if (r0.Count != r1.Count)
            {
                return false;
            }
            for (int i = r0.Count - 1; i >= 0; --i)
            {
                if (r0[i].GetType() != r1[i].GetType())
                {
                    return false;
                }
                if (r0[i] is UmlValueNode)
                {
                    UmlValueNode r0_i = r0[i] as UmlValueNode;
                    UmlValueNode r1_i = r1[i] as UmlValueNode;
                    if (r0_i.OwnerIndexer != r1_i.OwnerIndexer)
                    {
                        return false;
                    }
                }
                if (!string.Equals(r0[i].Name, r1[i].Name))
                {
                    return false;
                }
            }
            return true;
        }

        //---------------------------------------------------------------------------------

        static public List<UmlNode> ListElements(UmlDocument root)
        {
            List<UmlNode> array = new List<UmlNode>();
            ListElements(root.DocumentElement, array);
            return array;
        }

        static public void ListElements(UmlNode data_element, List<UmlNode> array)
        {
            array.Add(data_element);
            foreach (UmlNode fe in data_element.ChildNodes)
            {
                ListElements(fe, array);
            }
        }


        //---------------------------------------------------------------------------------
    }
}
