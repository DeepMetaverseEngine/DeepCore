using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DeepCore.Xml
{
    public enum XmlProperty : uint
    {
        Mark = 0,
        IgnoreClone = 0x0001,
        NoSerialize = 0x0002,
    }
    /// <summary>
    /// 标记当前Field或者Property是否参与Xml序列化
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class XmlSerializableAttribute : System.Attribute
    {
        private readonly XmlProperty Prop;
        public XmlSerializableAttribute(XmlProperty attr = XmlProperty.Mark)
        {
            this.Prop = attr;
        }
        public bool IgnoreClone { get { return (Prop & XmlProperty.IgnoreClone) != 0; } }
        public bool NoSerialize { get { return (Prop & XmlProperty.NoSerialize) != 0; } }
    }


    //----------------------------------------------------------------------------------------------------------

    public class XmlSerializer
    {
        public static string COMMENT_KEY = "Comment";
        public delegate bool TrySetField(XmlSerializer ser, object data, XmlElement e);
        public delegate void ErrorHandler(Exception e);
        private readonly bool cloning = false;
        private ErrorHandler event_error = null;
        private TrySetField event_field_mapping = null;
        public event ErrorHandler OnError { add { event_error += value; } remove { event_error -= value; } }
        public event TrySetField OnTrySetField { add { event_field_mapping += value; } remove { event_field_mapping -= value; } }
        public IExternalizableFactory Factory;
        public XmlSerializer(bool clone = false)
        {
            this.cloning = clone;
        }

        public T XmlToObject<T>(XmlDocument doc)
        {
            object ret = this.XmlToObject(typeof(T), doc);
            if (ret != null)
            {
                return (T)ret;
            }
            return default(T);
        }
        public object XmlToObject(XmlDocument doc)
        {
            XmlElement e = (XmlElement)doc.DocumentElement;
            XmlSerializer.TryGetTypeFromAttribute(e, "type", null, out var type, true, null);
            if (type == null) throw new Exception("type Attribute not found");
            return XmlToObject(type, doc);
        }
        public object XmlToObject(Type type, XmlDocument doc)
        {
            XmlElement e = (XmlElement)doc.DocumentElement;
            object data = this.DecodeFromXml(e, type, null);
            return data;
        }
        public T XmlToObject<T>(XmlElement e)
        {
            object ret = this.XmlToObject(typeof(T), e);
            if (ret != null)
            {
                return (T)ret;
            }
            return default(T);
        }
        public object XmlToObject(Type type, XmlElement e)
        {
            object data = this.DecodeFromXml(e, type, null);
            return data;
        }


        public XmlDocument ObjectToXml(object data)
        {
            return ObjectToXml(data, "data");
        }
        public XmlDocument ObjectToXml(object data, string root_name)
        {
            Type type = data.GetType();
            if (root_name == null)
            {
                root_name = "data";
            }
            XmlDocument doc = new XmlDocument();
            XmlElement e = doc.CreateElement(root_name);
            doc.AppendChild(e);
            this.EncodeToXML(e, data, null);
            return doc;
        }


        protected virtual bool AcceptField(Type type, FieldInfo field)
        {
            var attr = PropertyUtil.GetAttribute<XmlSerializableAttribute>(field);
            if (attr != null)
            {
                if (!cloning && attr.NoSerialize)
                {
                    return false;
                }
                else if (cloning && attr.IgnoreClone)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            if (!field.IsStatic && !field.IsLiteral && field.IsPublic)
            {
                return true;
            }
            return false;
        }
        protected virtual bool AcceptProperty(Type type, PropertyInfo property, bool read)
        {
            var attr = PropertyUtil.GetAttribute<XmlSerializableAttribute>(property);
            if (attr != null)
            {
                if (!cloning && attr.NoSerialize)
                {
                    return false;
                }
                else if (cloning && attr.IgnoreClone)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            if (read) { return true; }
            return false;
        }

        //-----------------------------------------------------------------------------------------------------------
        protected virtual void EncodeToXML(XmlElement data_element, object data, Type decleardType)
        {
            if (data != null)
            {
                Type type = data.GetType();
                if (decleardType != type)
                {
                    SetTypeToAttribute(data_element, "type", type);
                }
                if (data is Type runtimeType)
                {
                    data_element.InnerText = runtimeType.FullName;
                }
                else if (type.IsPrimitive)
                {
                    data_element.InnerText = Parser.ObjectToString(data);
                }
                else if (type == (typeof(string)))
                {
                    data_element.InnerText = Parser.ObjectToString(data);
                }
                else if (type == (typeof(decimal)))
                {
                    data_element.InnerText = Parser.ObjectToString(data);
                }
                else if (type.IsEnum)
                {
                    data_element.InnerText = Parser.ObjectToString(data);
                }
                else if (type == (typeof(DateTime)) || type == (typeof(TimeSpan)))
                {
                    data_element.InnerText = Parser.ObjectToString(data);
                }
                else if (type == typeof(byte[]))
                {
                    data_element.InnerText = CUtils.BinToHex((byte[])data);
                }
                else if (type.IsArray)
                {
                    EncodeToXMLElementArray(data_element, (Array)data);
                }
                else if (type.GetInterface(typeof(IDictionary).Name) != null)
                {
                    EncodeToXMLElementMap(data_element, (IDictionary)data);
                }
                else if (type.GetInterface(typeof(IList).Name) != null)
                {
                    EncodeToXMLElementList(data_element, (IList)data);
                }
                else if (type.GetInterface(typeof(IEnumerable).Name) != null)
                {
                    EncodeToXMLElementEnumerable(data_element, (IEnumerable)data);
                }
                else
                {
                    EncodeToXMLElementFields(data_element, data);
                }
            }
        }
        protected virtual void EncodeToXMLElementFields(XmlElement data_element, object data)
        {
            var type = data.GetType();
            if (data is IXmlBeforeExternalizable before)
            {
                before.BeforeEncode(data_element);
            }
            var doc = data_element.OwnerDocument;
            //SetTypeToAttribute(data_element, "type", type);
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = PropertyUtil.SortFields(PropertyUtil.GetFields(type, bindingFlags));
            foreach (var field in fields)
            {
                if (AcceptField(type, field))
                {
                    if (DependOnPropertyAttribute.IsDepend(field, data))
                    {
                        object fd = field.GetValue(data);
                        if (fd == null && field.TryGetAttribute<NotNullAttribute>(out var notnull))
                        {
                            fd = ReflectionUtil.CreateDefaultInstance(field.FieldType);
                            field.SetValue(data, fd);
                        }
                        XmlElement fe = doc.CreateElement(field.Name);
                        data_element.AppendChild(fe);
                        if (fd != null)
                        {
                            EncodeToXML(fe, fd, field.FieldType);
                            if (field.TryGetAttribute<DescAttribute>(out var desc))
                            {
                                //                                 var comment = doc.CreateComment(desc.Desc);
                                //                                 data_element.AppendChild(comment);
                                fe.SetAttribute(COMMENT_KEY, desc.Desc);
                            }
                        }
                        else
                        {
                            fe.SetAttribute("IsNull", "true");
                        }
                    }
                }
            }
            var properties = PropertyUtil.SortProperties(PropertyUtil.GetProperties(type, bindingFlags));
            foreach (var property in properties)
            {
                if (AcceptProperty(type, property, false))
                {
                    if (DependOnPropertyAttribute.IsDepend(property, data))
                    {
                        object fd = property.GetValue(data, null);
                        if (fd == null && property.TryGetAttribute<NotNullAttribute>(out var notnull))
                        {
                            fd = ReflectionUtil.CreateDefaultInstance(property.PropertyType);
                            property.SetValue(data, fd);
                        }
                        XmlElement fe = doc.CreateElement("property." + property.Name);
                        data_element.AppendChild(fe);
                        if (fd != null)
                        {                           
                            EncodeToXML(fe, fd, property.PropertyType); 
                            if (property.TryGetAttribute<DescAttribute>(out var desc))
                            {
                                //                                 var comment = doc.CreateComment(desc.Desc);
                                //                                 data_element.AppendChild(comment);
                                fe.SetAttribute(COMMENT_KEY, desc.Desc);
                            }
                        }
                        else
                        {
                            fe.SetAttribute("IsNull", "true");
                        }
                    }
                }
            }
            if (data is IXmlAfterExternalizable after)
            {
                after.AfterEncode(data_element);
            }
        }
        protected virtual void EncodeToXMLElementArray(XmlElement data_element, Array array)
        {
            var doc = data_element.OwnerDocument;
            var type = array.GetType();
            var e_type = type.GetElementType();
            var rank = type.GetArrayRank();
            SetTypeToAttribute(data_element, "element_type", e_type);
            data_element.SetAttribute("rank", rank + "");
            if (rank == 1)
            {
                data_element.SetAttribute("length", array.Length.ToString());
            }
            else
            {
                int[] ranges = new int[rank];
                for (int i = 0; i < rank; i++)
                {
                    ranges[i] = array.GetLength(i);
                }
                data_element.SetAttribute("ranges", Parser.ObjectToString(ranges));
            }
            foreach (object k in array)
            {
                XmlElement ei = doc.CreateElement("element");
                data_element.AppendChild(ei);
                EncodeToXML(ei, k, e_type);
            }
        }
        protected virtual void EncodeToXMLElementMap(XmlElement data_element, IDictionary map)
        {
            var doc = data_element.OwnerDocument;
            var type = map.GetType();
            var ktype = typeof(object);
            var vtype = typeof(object);
            if (type.IsGenericType)
            {
                ktype = type.GetGenericArguments()[0];
                vtype = type.GetGenericArguments()[1];
                SetTypeToAttribute(data_element, "key_type", ktype);
                SetTypeToAttribute(data_element, "value_type", vtype);
            }
            var keys = new ArrayList(map.Keys);
            {
                keys.Sort(new ToStringComparer());
                foreach (object k in keys)
                {
                    object v = map[k];
                    XmlElement epair = doc.CreateElement("element");
                    data_element.AppendChild(epair);
                    XmlElement ek = doc.CreateElement("key");
                    XmlElement ev = doc.CreateElement("value");
                    epair.AppendChild(ek);
                    epair.AppendChild(ev);
                    EncodeToXML(ek, k, ktype);
                    EncodeToXML(ev, v, vtype);
                }
            }
        }
        protected virtual void EncodeToXMLElementList(XmlElement data_element, IList list)
        {
            var doc = data_element.OwnerDocument;
            var type = list.GetType();
            var etype = typeof(object);
            if (type.IsGenericType)
            {
                etype = type.GetGenericArguments()[0];
                SetTypeToAttribute(data_element, "element_type", etype);
            }
            foreach (object k in list)
            {
                XmlElement ei = doc.CreateElement("element");
                data_element.AppendChild(ei);
                EncodeToXML(ei, k, etype);
            }
        }
        protected virtual void EncodeToXMLElementEnumerable(XmlElement data_element, IEnumerable list)
        {
            var doc = data_element.OwnerDocument;
            var type = list.GetType();
            var etype = typeof(object);
            if (type.IsGenericType)
            {
                etype = type.GetGenericArguments()[0];
                SetTypeToAttribute(data_element, "element_type", etype);
            }
            foreach (object k in list)
            {
                XmlElement ei = doc.CreateElement("element");
                data_element.AppendChild(ei);
                EncodeToXML(ei, k, etype);
            }
        }
        //-----------------------------------------------------------------------------------------------------------
        public virtual T DecodeFromXml<T>(XmlElement data_element)
        {
            return (T)DecodeFromXml(data_element, typeof(T), null);
        }
        public virtual object DecodeFromXml(XmlElement data_element, Type declearType, object root)
        {
            try
            {
                if (TryGetTypeFromAttribute(data_element, "type", declearType, out var type, true, root))
                {
                    {
                        if (TryConvertType(data_element, declearType, out var data, root))
                        {
                            return data;
                        }
                    }
                    if (type.FullName == "System.RuntimeType")
                    {
                        return ReflectionUtil.GetType(data_element.InnerText);
                    }
                    else if (type == (typeof(string)))
                    {
                        return data_element.InnerText;
                    }
                    else if (type == (typeof(decimal)))
                    {
                        return Parser.StringToObject(data_element.InnerText, type);
                    }
                    else if (type.IsPrimitive)
                    {
                        return Parser.StringToObject(data_element.InnerText, type);
                    }
                    else if (type.IsEnum)
                    {
                        return Parser.StringToObject(data_element.InnerText, type);
                    }
                    else if (type == (typeof(DateTime)) || type == (typeof(TimeSpan)))
                    {
                        return Parser.StringToObject(data_element.InnerText, type);
                    }
                    else if (type == typeof(byte[]))
                    {
                        return CUtils.HexToBin(data_element.InnerText);
                    }
                    else if (type.IsArray)
                    {
                        return DecodeFromXMLElementArray(data_element, type, root);
                    }
                    else if (type.IsInterfaceOf(typeof(IDictionary)))
                    {
                        return DecodeFromXMLElementMap(data_element, type, root);
                    }
                    else if (type.IsInterfaceOf(typeof(IList)))
                    {
                        return DecodeFromXMLElementList(data_element, type, root);
                    }
                    else if (type.IsInterfaceOf(typeof(IEnumerable)))
                    {
                        return DecodeFromXMLElementEnumerable(data_element, type, root);
                    }
                    else if (data_element.HasChildNodes || data_element.HasAttributes)
                    {
                        if (type.IsAbstract)
                        {
                            if (TryDefaultConvertType(data_element, type, new Exception("Unknow Type : " + type.FullName), out var data, root))
                            {
                                return DecodeFromXMLElementFields(data_element, data, root ?? data);
                            }
                        }
                        else
                        {
                            var data = ReflectionUtil.CreateInstance(type);
                            return DecodeFromXMLElementFields(data_element, data, root ?? data);
                        }
                    }
                }
                else
                {
                    if (TryDefaultConvertType(data_element, type, new Exception("Unknow Type : " + type.FullName), out var data, root))
                    {
                        return DecodeFromXMLElementFields(data_element, data, root ?? data);
                    }
                }
            }
            catch (Exception err)
            {
                XmlUtil.log.Warn(err.Message, err);
                if (event_error != null) event_error(err);
            }
            return null;
        }
        protected virtual object DecodeFromXMLElementFields(XmlElement data_element, object data, object root)
        {
            try
            {
                var type = data.GetType();
                var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                if (data is IXmlBeforeExternalizable before)
                {
                    before.BeforeDecode(data_element);
                }
                var put_fields = new List<(XmlElement, MemberInfo)>();
                foreach (XmlNode fe in data_element.ChildNodes)
                {
                    try
                    {
                        if (fe is XmlElement fee)
                        {
                            if (event_field_mapping != null && event_field_mapping.Invoke(this, data, fee))
                            {

                            }
                            else if (fe.Name.StartsWith("property."))
                            {
                                var property = PropertyUtil.GetProperty(type, fe.Name.Substring("property.".Length), bindingFlags);
                                if (property != null && AcceptProperty(type, property, true))
                                {
                                    if (fe.TryGetAttributeAs<bool>("IsNull", out var isnull))
                                    {
                                        property.SetValue(data, null, null);
                                    }
                                    else if (TryConvertField(fee, data, property, out var fd, root))
                                    {
                                        property.SetValue(data, fd, null);
                                    }
                                    else if (property.PropertyType.IsPrimitiveData())
                                    {
                                        fd = DecodeFromXml(fee, property.PropertyType, root);
                                        property.SetValue(data, fd, null);
                                    }
                                    else
                                    {
                                        put_fields.Add((fee, property));
                                    }
                                }
                                continue;
                            }
                            else
                            {
                                {
                                    var fii = PropertyUtil.GetField(type, fe.Name, bindingFlags);
                                    if (fii != null)
                                    {
                                        if (AcceptField(type, fii))
                                        {
                                            if (fe.TryGetAttributeAs<bool>("IsNull", out var isnull))
                                            {
                                                fii.SetValue(data, null);
                                            }
                                            else if (TryConvertField(fee, data, fii, out var fd, root))
                                            {
                                                fii.SetValue(data, fd);
                                            }
                                            else if (fii.FieldType.IsPrimitiveData())
                                            {
                                                fd = DecodeFromXml(fee, fii.FieldType, root);
                                                fii.SetValue(data, fd);
                                            }
                                            else
                                            {
                                                put_fields.Add((fee, fii));
                                            }
                                        }
                                        continue;
                                    }
                                }
                                {
                                    var pri = PropertyUtil.GetProperty(type, fe.Name, bindingFlags);
                                    if (pri != null)
                                    {
                                        if (AcceptProperty(type, pri, true))
                                        {
                                            if (fe.TryGetAttributeAs<bool>("IsNull", out var isnull))
                                            {
                                                pri.SetValue(data, null);
                                            }
                                            else if (TryConvertField(fee, data, pri, out var fd, root))
                                            {
                                                pri.SetValue(data, fd);
                                            }
                                            else if (pri.PropertyType.IsPrimitiveData())
                                            {
                                                fd = DecodeFromXml(fee, pri.PropertyType, root);
                                                pri.SetValue(data, fd);
                                            }
                                            else
                                            {
                                                put_fields.Add((fee, pri));
                                            }
                                        }
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        XmlUtil.log.Warn(err.Message, err);
                        if (event_error != null) event_error(err);
                    }
                }
                if (data is IXmlAfterExternalizable after)
                {
                    after.AfterDecode(data_element);
                }
                // 先将基础字段解析出来后，解析复杂数据，便于打印Root
                foreach (var put_field in put_fields)
                {
                    var fee = put_field.Item1;
                    if (put_field.Item2 is PropertyInfo property)
                    {
                        var fd = DecodeFromXml(fee, property.PropertyType, root);
                        property.SetValue(data, fd);
                    }
                    else if (put_field.Item2 is FieldInfo field)
                    {
                        var fd = DecodeFromXml(fee, field.FieldType, root);
                        field.SetValue(data, fd);
                    }
                }
                return data;
            }
            catch (Exception err)
            {
                XmlUtil.log.Warn(err.Message, err);
                if (event_error != null) event_error(err);
            }
            return null;
        }
        protected virtual Array DecodeFromXMLElementArray(XmlElement data_element, Type type, object root)
        {
            var rank = Parser.ParseInt(data_element.GetAttribute("rank"));
            TryGetTypeFromAttribute(data_element, "element_type", type.GetElementType(), out var etype, false, root);
            if (data_element.HasAttribute("ranges"))
            {
                var ranges = Parser.StringToObject<int[]>(data_element.GetAttribute("ranges"));
                var array = Array.CreateInstance(etype, ranges);
                var total_index = 0;
                foreach (XmlNode fe in data_element.ChildNodes)
                {
                    if (fe is XmlElement fee)
                    {
                        if (!TryConvertField(fee, array, total_index, out var fdd, root))
                        {
                            fdd = DecodeFromXml(fee, etype, root);
                        }
                        int[] indices = CUtils.GetArrayRankIndex(ranges, total_index);
                        array.SetValue(fdd, indices);
                        total_index++;
                    }
                }
                return array;
            }
            else if (data_element.HasAttribute("length"))
            {
                var length = Parser.ParseInt(data_element.GetAttribute("length"));
                var array = Array.CreateInstance(etype, length);
                var index = 0;
                foreach (XmlNode fe in data_element.ChildNodes)
                {
                    if (fe is XmlElement fee)
                    {
                        if (!TryConvertField(fee, array, index, out var fdd, root))
                        {
                            fdd = DecodeFromXml(fee, etype, root);
                        }
                        array.SetValue(fdd, index);
                        index++;
                    }
                }
                return array;
            }
            else
            {
                return Array.CreateInstance(etype, 0); ;
            }
        }
        protected virtual IDictionary DecodeFromXMLElementMap(XmlElement data_element, Type type, object root)
        {
            type.IsGenericMap(out var k_type, out var v_type);
            TryGetTypeFromAttribute(data_element, "key_type", k_type, out k_type, false, root);
            TryGetTypeFromAttribute(data_element, "value_type", v_type, out v_type, false, root);
            IDictionary map;
            if (!type.IsAbstract && !type.IsInterface)
            {
                map = (IDictionary)DeepActivator.CreateInstance(type);
            }
            else
            {
                map = ReflectionUtil.CreateGenericInstance<IDictionary>(typeof(HashMap<,>), k_type, v_type);
            }
            foreach (XmlNode fe in data_element.ChildNodes)
            {
                if (fe is XmlElement epair)
                {
                    XmlElement ek = epair.ChildNodes[0] as XmlElement;
                    XmlElement ev = epair.ChildNodes[1] as XmlElement;
                    object k = DecodeFromXml(ek, k_type, root);
                    if (!TryConvertField(ev, map, k, out var v, root))
                    {
                        v = DecodeFromXml(ev, v_type, root);
                    }
                    map.Add(k, v);
                }
            }
            return map;
        }
        protected virtual IList DecodeFromXMLElementList(XmlElement data_element, Type type, object root)
        {
            type.IsGenericList(out var e_type);
            TryGetTypeFromAttribute(data_element, "element_type", e_type, out e_type, false, root);
            IList list;
            if (!type.IsAbstract && !type.IsInterface)
            {
                list = (IList)DeepActivator.CreateInstance(type);
            }
            else
            {
                list = ReflectionUtil.CreateGenericInstance<IList>(typeof(ArrayList<>), e_type);
            }
            int index = 0;
            foreach (XmlNode fe in data_element.ChildNodes)
            {
                if (fe is XmlElement ei)
                {
                    if (!TryConvertField(ei, list, index, out var fd, root))
                    {
                        fd = DecodeFromXml(ei, e_type, root);
                    }
                    list.Add(fd);
                    index++;
                }
            }
            return list;
        }
        protected virtual IEnumerable DecodeFromXMLElementEnumerable(XmlElement data_element, Type type, object root)
        {
            type.IsGenericList(out var e_type);
            TryGetTypeFromAttribute(data_element, "element_type", e_type, out e_type, false, root);
            IList list = ReflectionUtil.CreateGenericInstance<IList>(typeof(ArrayList<>), e_type);
            int index = 0;
            foreach (XmlNode fe in data_element.ChildNodes)
            {
                if (fe is XmlElement ei)
                {
                    if (!TryConvertField(ei, list, index, out var fd, root))
                    {
                        fd = DecodeFromXml(ei, e_type, root);
                    }
                    list.Add(fd);
                    index++;
                }
            }
            return list;
        }
        //-----------------------------------------------------------------------------------------------------------
        public static void SetTypeToAttribute(XmlElement e, string name, Type type)
        {
            foreach (var a in alias_typeEncoder)
            {
                if (a(type, out var typeName))
                {
                    e.SetAttribute(name, typeName);
                    return;
                }
            }
            e.SetAttribute(name, type.FullName);
            if (type.TryGetAttribute<DescAttribute>(out var desc))
            {
                e.SetAttribute(COMMENT_KEY, desc.Desc);
            }
        }
        public static bool TryGetTypeFromAttribute(XmlElement e, string name, Type defaultType, out Type outType, bool verbos, object root)
        {
            outType = defaultType;
            var vtype = e.GetAttribute(name);
            if (!String.IsNullOrEmpty(vtype))
            {
                foreach (var a in alias_typeDecoder)
                {
                    if (a(vtype, defaultType, out var rType))
                    {
                        outType = rType;
                        return true;
                    }
                }
                if (TryGetAliasType(vtype, out var retType))
                {
                    if (defaultType.IsAssignableFrom(retType))
                    {
                        outType = retType;
                        return true;
                    }
                }
                if ((defaultType.IsAbstract || defaultType.IsInterface))
                {
                    //if (verbos) 
                    {
                        XmlUtil.log.Warn($"Can Not Found Type : {vtype} : Declare Type is : {defaultType} Root is : {root} : Root type is : {root?.GetType()}");
                    }
                    return false;
                }
            }
            return true;
        }

        //---------------------------------------------------------------------------------------------------
        #region 编辑器重构功能
        static XmlSerializer()
        {
            var encode = new TypeEncodeAlias((Type type, out string typeName) =>
            {
                if (type.IsGenericType)
                {
                    typeName = EncodeTypeName(type);
                }
                else if (type.IsArray)
                {
                    typeName = EncodeTypeName(type);
                }
                else
                {
                    typeName = null;
                }
                return typeName != null;
            });
            var decode = new TypeDecodeAlias((string typeName, Type declearType, out Type outType) =>
            {
                outType = DecodeTypeName(typeName, declearType);
                return outType != null;
            });
            AddTypeEncodeAlias(encode, decode);
        }
        public static string EncodeTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var sb = new StringBuilder();
                {
                    var gt = type.GetGenericTypeDefinition();
                    sb.Append(gt.FullName);
                    Type[] g_args = type.GetGenericArguments();
                    if (g_args.Length > 0)
                    {
                        sb.Append("[");
                        for (int i = 0; i < g_args.Length; i++)
                        {
                            sb.Append("[").Append(EncodeTypeName(g_args[i])).Append("]");
                            if (i < g_args.Length - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append("]");
                    }
                    return sb.ToString();
                }
            }
            else if (type.IsArray)
            {
                return EncodeTypeName(type.GetElementType()) + "[]";
            }
            return type.FullName;
        }
        public static Type DecodeTypeName(string name, Type declearType)
        {
#if FALSE
            try
            {
                if (name.EndsWith("[]"))
                {
                    var elementType = DecodeTypeName(name.Substring(0, name.Length - 2));
                    var arrayType = elementType.MakeArrayType();
                    return arrayType;
                }
                else if (name.TryIndexOf('`', out var gIndex))
                {
                    if (name.TryIndexOf('[', out var gL, gIndex + 1))
                    {
                        var count = int.Parse(name.Substring(gIndex + 1, gL - gIndex - 1));
                        var gtypeName = name.Substring(0, gL);
                        var gtype = ReflectionUtil.GetType(gtypeName);
                        var gargs = name.Substring(gL + 1, name.Length - gL - 2);
                        if (count > 1)
                        {
                            var split = gargs.Split(',');
                            var g_args = split.ConvertAll(g =>
                            {
                                var c = g.Substring(1, g.Length - 2);
                                return DecodeTypeName(c);
                            });
                            return gtype.MakeGenericType(g_args);
                        }
                        else
                        {
                            var g_arg = gargs.Substring(1, gargs.Length - 2);
                            return gtype.MakeGenericType(DecodeTypeName(g_arg));
                        }
                    }
                }
            }
            catch
            {
            }
#endif
            var asmType = ReflectionUtil.GetType(name);
            if (declearType != null && declearType.IsAssignableFrom(asmType))
            {
                return asmType;
            }
            return asmType;
        }

        public delegate bool TypeConverter(XmlSerializer ser, XmlElement dataElement, Type decleardType, out object data, object root);
        public delegate bool FieldConverter(XmlSerializer ser, XmlElement dataElement, object owner, object field, out object data, object root);
        public delegate bool TypeEncodeAlias(Type type, out string typeName);
        public delegate bool TypeDecodeAlias(string typeName, Type decleardType, out Type outType);
        public delegate bool DefaultConverter(XmlSerializer ser, XmlElement dataElement, Type decleardType, Exception err, out object data, object root);
        private static HashMap<string, string> alias_prefix = new HashMap<string, string>();
        private static HashMap<string, string> alias_name = new HashMap<string, string>();
        private static List<TypeConverter> alias_tconverter = new List<TypeConverter>();
        private static List<DefaultConverter> alias_default_tconverter = new List<DefaultConverter>();
        private static List<FieldConverter> alias_fconverter = new List<FieldConverter>();
        private static List<TypeEncodeAlias> alias_typeEncoder = new List<TypeEncodeAlias>();
        private static List<TypeDecodeAlias> alias_typeDecoder = new List<TypeDecodeAlias>();

        public static void LoadAlias(XmlDocument doc)
        {
            doc.DocumentElement.ForEachChilds<XmlElement>(e =>
            {
                if (e.Name == "SetTypeAlias")
                {
                    SetTypeAlias(e["Key"].InnerText, e["Value"].InnerText);
                }
                else if (e.Name == "SetTypeAliasPrefix")
                {
                    SetTypeAliasPrefix(e["Key"].InnerText, e["Value"].InnerText);
                }
            });
        }

        public static void SetTypeAliasPrefix(string prefix, string replace)
        {
            alias_prefix[prefix] = replace;
        }
        public static void SetTypeAlias(string name, string replace)
        {
            alias_name[name] = replace;
        }
        public static void AddTypeConverter(TypeConverter func)
        {
            alias_tconverter.Add(func);
        }
        public static void AddDefaultConverter(DefaultConverter func)
        {
            alias_default_tconverter.Add(func);
        }
        public static void AddFieldConverter(FieldConverter func)
        {
            alias_fconverter.Add(func);
        }
        public static void AddTypeEncodeAlias(TypeEncodeAlias encode, TypeDecodeAlias decode)
        {
            alias_typeEncoder.Add(encode);
            alias_typeDecoder.Add(decode);
        }
        public static void ClearTypeAlias()
        {
            alias_prefix.Clear();
            alias_name.Clear();
        }
        public static bool TryGetAliasType(string name, out Type ret)
        {
            if (alias_name.Count > 0)
            {
                if (alias_name.TryGetValue(name, out var new_name))
                {
                    ret = ReflectionUtil.GetType(new_name);
                    if (ret != null)
                    {
                        return true;
                    }
                }
            }
            if (alias_prefix.Count > 0)
            {
                foreach (var e in alias_prefix)
                {
                    if (name.StartsWith(e.Key))
                    {
                        var new_name = e.Value + name.Substring(e.Key.Length);
                        ret = ReflectionUtil.GetType(new_name);
                        if (ret != null)
                        {
                            return true;
                        }
                    }
                }
            }
            ret = ReflectionUtil.GetType(name);
            if (ret != null)
            {
                return true;
            }
            return false;
        }
        private bool TryConvertType(XmlElement dataElement, Type decleardType, out object data, object root)
        {
            if (alias_tconverter.Count > 0)
            {
                foreach (var converter in alias_tconverter)
                {
                    if (converter.Invoke(this, dataElement, decleardType, out data, root))
                    {
                        return true;
                    }
                }
            }
            data = null;
            return false;
        }
        private bool TryDefaultConvertType(XmlElement dataElement, Type decleardType, Exception err, out object data, object root)
        {
            if (alias_default_tconverter.Count > 0)
            {
                foreach (var converter in alias_default_tconverter)
                {
                    if (converter.Invoke(this, dataElement, decleardType, err, out data, root))
                    {
                        return true;
                    }
                }
            }
            data = null;
            return false;
        }
        private bool TryConvertField(XmlElement dataElement, object owner, object field, out object data, object root)
        {
            if (alias_fconverter.Count > 0)
            {
                foreach (var converter in alias_fconverter)
                {
                    if (converter.Invoke(this, dataElement, owner, field, out data, root))
                    {
                        return true;
                    }
                }
            }
            data = null;
            return false;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------
    }


    //----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Xml序列化与反序列化
    /// </summary>
    public static class XmlUtil
    {
        public const string CHAR_SPACE = "&#032;";
        public const string CHAR_TAB = "&#009;";

        private static Json.JsonParser json = new Json.JsonParser();
        internal static Logger log = new LazyLogger(nameof(XmlUtil));

        static public string ToString(XmlDocument doc)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = CUtils.UTF8;
            return ToString(doc, settings);
        }
        static public string ToXmlString(this XmlDocument doc)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = CUtils.UTF8;
            return ToXmlString(doc, settings);
        }
        static public string ToXmlString(this XmlElement doc)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = CUtils.UTF8;
            return ToXmlString(doc, settings);
        }

        static public string ToString(XmlDocument doc, XmlWriterSettings settings)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter xml = XmlWriter.Create(sw, settings))
                {
                    doc.Save(xml);
                    xml.Flush();
                }
                return sw.ToString();
            }
        }
        static public string ToXmlString(this XmlDocument doc, XmlWriterSettings settings)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter xml = XmlWriter.Create(sw, settings))
                {
                    doc.Save(xml);
                    xml.Flush();
                }
                return sw.ToString();
            }
        }
        static public string ToXmlString(this XmlElement doc, XmlWriterSettings settings)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter xml = XmlWriter.Create(sw, settings))
                {
                    doc.WriteTo(xml);
                    xml.Flush();
                }
                return sw.ToString();
            }
        }


        static public XmlDocument FromString(string xmltext, bool preserveWhitespace = false)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = preserveWhitespace;
            doc.LoadXml(xmltext);
            return doc;
        }
        static public bool TryFromString(string xmltext, out XmlDocument doc, bool preserveWhitespace = false)
        {
            try
            {
                doc = new XmlDocument();
                doc.PreserveWhitespace = preserveWhitespace;
                doc.LoadXml(xmltext);
                return doc != null;
            }
            catch
            {
                doc = null;
                return false;
            }
        }

        static public XmlDocument LoadXML(byte[] data)
        {
            using (var ms = new DeepCore.IO.MemoryStream(data))
            {
                return LoadXML(ms);
            }
        }
        static public XmlDocument LoadXML(Stream input, bool autoDisposeStream = false, XmlReaderSettings setting = null)
        {
            try
            {
                using (XmlReader xml = XmlReader.Create(input, new XmlReaderSettings() { IgnoreComments = true, }))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(xml);
                    return doc;
                }
            }
            finally
            {
                if (autoDisposeStream)
                {
                    input.Close();
                    input.Dispose();
                }
            }
        }

        static public XmlDocument LoadXML(string path)
        {
            var stream = Resource.LoadData(path);
            if (stream != null)
            {
                {
                    return LoadXML(stream);
                }
            }
            return null;
        }
        static public XmlDocument LoadXML(string path, bool macro)
        {
            var stream = Resource.LoadData(path);
            if (stream != null)
            {
                {
                    return LoadXML(path, macro, stream);
                }
            }
            return null;
        }
        static public XmlDocument LoadXML(string path, bool macro, byte[] stream)
        {
            return LoadXML(path, macro, new DeepCore.IO.MemoryStream(stream));
        }
        static public XmlDocument LoadXML(string path, bool macro, Stream stream)
        {
            var ret = LoadXML(stream);
            if (macro)
            {
                ret.ProcessMacroDefine();
                ret.IncludeXML((e) =>
                {
                    try
                    {
                        var sub_file = e.InnerText;
                        var sub_stream = Resource.LoadData(path);
                        if (sub_stream != null)
                        {
                            {
                                return LoadXML(sub_file, macro, sub_stream);
                            }
                        }
                        sub_file = Resource.ParentPath(path) + Resource.DEFAULT_SPLIT + sub_file;
                        sub_stream = Resource.LoadData(path);
                        if (sub_stream != null)
                        {
                            {
                                return LoadXML(sub_file, macro, sub_stream);
                            }
                        }
                    }
                    catch { }
                    return null;
                });
            }
            return ret;
        }
        static public XmlDocument LoadXML(FileInfo path, bool macro)
        {
            if (path.Exists)
            {
                var stream = Resource.LoadData(path.FullName);
                if (stream != null)
                {
                    var ret = LoadXML(stream);
                    if (macro)
                    {
                        ret.ProcessMacroDefine();
                        ret.IncludeXML((e) =>
                        {
                            try
                            {
                                var sub_path = e.InnerText.Trim();
                                if (File.Exists(sub_path))
                                {
                                    return LoadXML(new FileInfo(sub_path), macro);
                                }
                                sub_path = path.Directory.FullName + Path.DirectorySeparatorChar + e.InnerText;
                                if (File.Exists(sub_path))
                                {
                                    return LoadXML(new FileInfo(sub_path), macro);
                                }
                            }
                            catch { }
                            return null;
                        });
                    }
                    return ret;
                }
            }
            return null;
        }


        static public async Task<XmlDocument> LoadXMLAsync(string path)
        {
            var stream = await Resource.LoadDataAsync(path);
            if (stream != null)
            {
                {
                    return LoadXML(stream);
                }
            }
            return null;
        }

        static public T LoadXMLObject<T>(string path)
        {
            var xml = LoadXML(path);
            if (xml != null)
            {
                return XmlToObject<T>(xml);
            }
            return default(T);
        }
        static public void SaveXMLObject(string path, object data)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                XmlUtil.SaveXML(fs, ObjectToXml(data), false);
            }
        }

        static public void SaveXML(string path, XmlDocument xml)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                XmlUtil.SaveXML(fs, xml, false);
            }
        }
        static public void SaveXML(Stream output, XmlDocument doc, bool autoDisposeStream)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = Encoding.UTF8;
                using (XmlWriter xml = XmlWriter.Create(output, settings))
                {
                    doc.Save(xml);
                    xml.Flush();
                }
            }
            finally
            {
                if (autoDisposeStream)
                {
                    output.Close();
                    output.Dispose();
                }
            }
        }
        static public void SaveToXML(Stream output, object mData, bool autoDisposeStream = false)
        {
            try
            {
                Type type = mData.GetType();
                XmlDocument doc = XmlUtil.ObjectToXml(mData);
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = Encoding.UTF8;
                using (XmlWriter xml = XmlWriter.Create(output, settings))
                {
                    doc.Save(xml);
                    xml.Flush();
                }
            }
            finally
            {
                if (autoDisposeStream)
                {
                    output.Close();
                    output.Dispose();
                }
            }
        }
        static public void SaveXmlTo(this XmlDocument xml, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                XmlUtil.SaveXML(fs, xml, false);
            }
        }
        static public byte[] SaveXmlToBin(this XmlDocument xml)
        {
            using (var fs = new DeepCore.IO.MemoryStream())
            {
                XmlUtil.SaveXML(fs, xml, false);
                return fs.ToArray();
            }
        }
        static public byte[] ObjectToXmlBin(object mData)
        {
            Type type = mData.GetType();
            XmlDocument doc = ObjectToXml(mData);
            return SaveXmlToBin(doc);
        }


        static public bool EaualsAndDump(this XmlSerializer ser, object a, object b, FileInfo fileA, FileInfo fileB)
        {
            var XmlA = ser.ObjectToXml(a).ToXmlString();
            var XmlB = ser.ObjectToXml(b).ToXmlString();
            if (XmlA != XmlB)
            {
                DeepCore.IO.CFiles.CreateDir(fileA.Directory);
                DeepCore.IO.CFiles.CreateDir(fileB.Directory);
                File.WriteAllText(fileA.FullName, XmlA);
                File.WriteAllText(fileB.FullName, XmlB);
                return false;
            }
            return true;
        }

        static public void AppendChilds(this XmlNode xml, Properties prop, bool replace = true, string subSplit = ".")
        {
            foreach (var e in prop)
            {
                var sIndex = e.Key.IndexOf(subSplit);
                if (sIndex > 0)
                {
                    var subName = e.Key.Substring(0, sIndex);
                    var sub = xml[subName];
                    if (sub == null || replace == false)
                    {
                        sub = xml.OwnerDocument.CreateElement(subName);
                        xml.AppendChild(sub);
                    }
                    AppendChilds(sub, prop.SubProperties(e.Key.Substring(0, sIndex + 1)), replace, subSplit);
                }
                else
                {
                    var sub = xml[e.Key];
                    if (sub == null || replace == false)
                    {
                        sub = xml.OwnerDocument.CreateElement(e.Key);
                        xml.AppendChild(sub);
                    }
                    sub.InnerText = e.Value;
                }
            }
        }

        //----------------------------------------------------------------------------------------------------
        #region Locad Macro

        public static XmlDocument IncludeXML(this XmlDocument template, Func<XmlNode, XmlDocument> loadInclude)
        {
            if (template != null)
            {
                XmlUtil.ForEachChilds(template.DocumentElement, (e) =>
                {
                    if (e.Name.StringEqualsIgnoreCase("INCLUDE"))
                    {
                        var p = e.ParentNode;
                        var doc = loadInclude(e);
                        if (doc != null)
                        {
                            p.RemoveChild(e);
                            var inc = doc.DocumentElement;
                            ProcessMacroParameter(inc, e);
                            foreach (XmlNode inc_sub in inc.ChildNodes)
                            {
                                var add = p.OwnerDocument.ImportNode(inc_sub, true);
                                p.AppendChild(add);
                            }
                        }
                    }
                }, true);
            }
            return template;
        }
        public static XmlDocument IncludeXML(this XmlDocument template)
        {
            return IncludeXML(template, (node) =>
            {
                var name = node.InnerText;
                var text = Resource.LoadAllText(name);
                if (text != null)
                {
                    return LoadXML(text);
                }
                return null;
            });
        }
        /// <param name="function">模板</param>
        /// <param name="invoker">参数给予者</param>
        public static void ProcessMacroParameter(this XmlNode function, XmlNode invoker)
        {
            var params_map = new HashMap<string, string>();
            foreach (XmlNode macro in function.ChildNodes.ToList())
            {
                if (macro.Name.StringEqualsIgnoreCase("parameter"))
                {
                    try
                    {
                        var pname = macro.GetAttribute("name", true, true);
                        var pdefault = macro.GetAttribute("default", true, true);
                        if (!string.IsNullOrEmpty(pname))
                        {
                            params_map.Put(pname, pdefault ?? string.Empty);
                            macro.ParentNode.RemoveChild(macro);
                        }
                    }
                    catch { }
                }
            }
            if (params_map.Count > 0)
            {
                foreach (var param_key in new List<string>(params_map.Keys))
                {
                    var param_value = invoker.GetAttribute(param_key, false, false);
                    if (param_value != null)
                    {
                        params_map[param_key] = param_value;
                    }
                }
                foreach (var param in params_map)
                {
                    ReplaceAllText(function, "${" + param.Key + "}", param.Value);
                }
            }
        }
        public static void ProcessMacroDefine(this XmlDocument doc)
        {
            var function = doc.DocumentElement;
            var define_map = new HashMap<string, XmlNode>();
            foreach (XmlNode macro in function.ChildNodes.ToList())
            {
                if (macro.Name.StringEqualsIgnoreCase("define"))
                {
                    try
                    {
                        var pname = macro.GetAttribute("name", true, true);
                        if (!string.IsNullOrEmpty(pname))
                        {
                            define_map.Put(pname, macro);
                            macro.ParentNode.RemoveChild(macro);
                        }
                    }
                    catch { }
                }
            }
            if (define_map.Count > 0)
            {
                foreach (var define in define_map)
                {
                    var define_value = define.Value.GetAttribute("value", false, true);
                    if (define_value != null)
                    {
                        ReplaceAllText(function, "${" + define.Key + "}", define_value);
                    }
                    else
                    {
                        XmlUtil.ForEachChilds(function, (e) =>
                        {
                            if (e.Name.StringEqualsIgnoreCase("define_ref") && (e.GetAttribute("name", true, true) == define.Key))
                            {
                                var p = e.ParentNode;
                                var inc = define.Value;
                                p.RemoveChild(e);
                                foreach (XmlNode inc_sub in inc.ChildNodes)
                                {
                                    var add = p.OwnerDocument.ImportNode(inc_sub, true);
                                    p.AppendChild(add);
                                }
                            }
                            else if (e.Name.StringEqualsIgnoreCase("define_call") && (e.GetAttribute("name", true, true) == define.Key))
                            {
                                var p = e.ParentNode;
                                var inc = define.Value.Clone();
                                var params_map = new HashMap<string, KeyValuePair<string, string>>();
                                //获取形参//
                                foreach (XmlAttribute param_def in inc.Attributes)
                                {
                                    if (param_def.Name.StartsWith("param", CUtils.StringComparisonIgnoreCase))
                                    {
                                        params_map.Put(param_def.Name, new KeyValuePair<string, string>(param_def.Value, param_def.Value));
                                    }
                                }
                                //获取实参//
                                foreach (XmlAttribute param_call in e.Attributes)
                                {
                                    if (param_call.Name.StartsWith("param", CUtils.StringComparisonIgnoreCase))
                                    {
                                        if (params_map.TryGetValue(param_call.Name, out var pair))
                                        {
                                            ReplaceAllText(inc, "#{" + pair.Key + "}", param_call.Value);
                                        }
                                    }
                                }
                                p.RemoveChild(e);
                                foreach (XmlNode inc_sub in inc.ChildNodes)
                                {
                                    var add = p.OwnerDocument.ImportNode(inc_sub, true);
                                    p.AppendChild(add);
                                }
                            }

                        }, true);
                    }
                }
            }
        }

        public static void ReplaceAllText(this XmlNode node, string src, string dst)
        {
            ForEachChilds(node, (e) =>
            {
                if (e is XmlElement && e.Attributes != null)
                {
                    foreach (XmlAttribute attr in e.Attributes)
                    {
                        attr.Value = attr.Value.Replace(src, dst);
                    }
                }
                if (e is XmlText txt)
                {
                    txt.Value = txt.Value.Replace(src, dst);
                }
            }, true);
        }

        #endregion

        //----------------------------------------------------------------------------------------------------------

        #region _converter_

        static public T XmlTextToObject<T>(string xml)
        {
            return XmlToObject<T>(FromString(xml));
        }
        static public object XmlTextToObject(string xml, Type type)
        {
            return XmlToObject(FromString(xml), type);
        }
        static public object XmlTextToObject(string xml)
        {
            return XmlToObject(FromString(xml));
        }
        static public T XmlToObject<T>(XmlDocument doc)
        {
            return new XmlSerializer(false).XmlToObject<T>(doc);
        }
        static public object XmlToObject(XmlDocument doc)
        {
            return new XmlSerializer(false).XmlToObject(doc);
        }
        static public object XmlToObject(XmlDocument doc, Type type)
        {
            return new XmlSerializer(false).XmlToObject(type, doc);
        }
        static public T XmlToObject<T>(XmlElement e)
        {
            return new XmlSerializer(false).XmlToObject<T>(e);
        }
        static public object XmlToObject(XmlElement e, Type type)
        {
            return new XmlSerializer(false).XmlToObject(type, e);
        }

        static public XmlDocument ObjectToXml(object data)
        {
            return new XmlSerializer(false).ObjectToXml(data);
        }
        static public XmlDocument ObjectToXml(object data, string root_name)
        {
            return new XmlSerializer(false).ObjectToXml(data, root_name);
        }
        static public string ObjectToXmlString(object data)
        {
            return new XmlSerializer(false).ObjectToXml(data).ToXmlString();
        }
        static public string ObjectToXmlString(object data, string root_name)
        {
            return new XmlSerializer(false).ObjectToXml(data, root_name).ToXmlString();
        }
        static public string ObjectToXmlString<T>(T data)
        {
            return new XmlSerializer(false).ObjectToXml(data).ToXmlString();
        }
        static public string ObjectToXmlString<T>(T data, string root_name)
        {
            return new XmlSerializer(false).ObjectToXml(data, root_name).ToXmlString();
        }

        static public T CloneObject<T>(T src)
        {
            if (src == null) return default(T);
            Type type = src.GetType();
            if (type.IsPrimitive)
            {
                return src;
            }
            else if (type.IsEnum)
            {
                return src;
            }
            else if (type == (typeof(string)))
            {
                return src;
            }
            else if (type.IsClass || type.IsArray)
            {
                var ser = new XmlSerializer(true);
                XmlDocument xml = ser.ObjectToXml(src, "cloning");
                T obj = (T)ser.XmlToObject(type, xml);
                return obj;
            }
            return src;
        }

        public static string ObjectToJson(object data)
        {
            return json.EncodeObject(data, null);
        }
        public static object JsonToObject(string text, Type type)
        {
            return json.DecodeObject(text, type);
        }
        public static T JsonToObject<T>(string text)
        {
            return (T)json.DecodeObject(text, typeof(T));
        }

        #endregion

        //----------------------------------------------------------------------------------------------------------

        #region _utils_
        //----------------------------------------------------------------------------------------------------
        static public string GetTextValue(this XmlElement e)
        {
            if (e.FirstChild != null && e.FirstChild.NodeType == XmlNodeType.Text)
            {
                return (e.FirstChild as XmlText).Data;
            }
            else { return null; }
        }

        public static List<XmlNode> ToList(this XmlNodeList list)
        {
            var ret = new List<XmlNode>(list.Count);
            foreach (var sub in list)
            {
                if (sub is XmlNode snode)
                {
                    ret.Add(snode);
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------------------------------------
        public static bool TryGetAttribute(this XmlNode e, string key, out string value, bool NotNull = true, bool IgnoreCase = false)
        {
            if (e.Attributes != null)
            {
                XmlAttribute attr = e.Attributes[key];
                if (attr == null && IgnoreCase)
                {
                    foreach (XmlAttribute eattr in e.Attributes)
                    {
                        if (eattr.Name.StringEqualsIgnoreCase(key))
                        {
                            attr = eattr;
                            break;
                        }
                    }
                }
                if (attr != null)
                {
                    if (NotNull && string.IsNullOrEmpty(attr.Value))
                    {
                        value = null;
                        return false;
                    }
                    value = attr.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }
        public static bool TryGetAttributeAs<T>(this XmlNode e, string key, out T value, bool NotNull = true, bool IgnoreCase = false)
        {
            if (TryGetAttribute(e, key, out var attr, NotNull, IgnoreCase))
            {
                if (string.IsNullOrEmpty(attr))
                {
                    value = default(T);
                    return false;
                }
                return Parser.TryStringToObject<T>(attr, out value);
            }
            value = default(T);
            return false;
        }
        public static string GetAttribute(this XmlNode e, string key, bool NotNull = true, bool IgnoreCase = false)
        {
            if (TryGetAttribute(e, key, out var attr, NotNull, IgnoreCase))
            {
                return attr;
            }
            return null;
        }
        public static T GetAttributeAs<T>(this XmlNode e, string key, bool NotNull = true, bool IgnoreCase = false)
        {
            if (TryGetAttribute(e, key, out var attr, NotNull, IgnoreCase))
            {
                return Parser.StringToObject<T>(attr);
            }
            return default(T);
        }

        //----------------------------------------------------------------------------------------------------
        public static string GetXmlNodeText(this XmlNode e)
        {
            return e.InnerText;
        }
        public static T GetXmlNodeTextAs<T>(this XmlNode e)
        {
            return Parser.StringToObject<T>(e.InnerText);
        }
        public static XmlElement GetXmlElement(this XmlNode e, string name)
        {
            return e[name];
        }

        //----------------------------------------------------------------------------------------------------
        public static bool TryFindChild<T>(this XmlNode e, string childName, out T ret, bool deep = false) where T : XmlNode
        {
            ret = FindChild<T>(e, childName, deep);
            return ret != null;
        }
        public static bool TryFindChild<T>(this XmlNode e, Predicate<T> condition, out T ret, bool deep = false) where T : XmlNode
        {
            ret = FindChild<T>(e, condition, deep);
            return ret != null;
        }
        public static T FindChild<T>(this XmlNode e, string childName, bool deep = false) where T : XmlNode
        {
            T ret = null;
            ForEachChilds(e, (s) =>
            {
                if (s is T)
                {
                    if (s.Name == childName)
                    {
                        ret = s as T;
                        return true;
                    }
                }
                return false;
            }, deep);
            return ret;
        }
        public static T FindChild<T>(this XmlNode e, Predicate<T> condition, bool deep = false) where T : XmlNode
        {
            T ret = null;
            ForEachChilds(e, (s) =>
            {
                if (s is T && condition(s as T))
                {
                    ret = s as T;
                    return true;
                }
                return false;
            }, deep);
            return ret;
        }
        public static List<T> FindChilds<T>(this XmlNode e, string childName, bool deep = false) where T : XmlNode
        {
            var ret = new List<T>();
            ForEachChilds(e, (s) =>
            {
                if (s is T t)
                {
                    if (s.Name == childName)
                    {
                        ret.Add(t);
                    }
                }
            }, deep);
            return ret;
        }
        public static List<T> FindChilds<T>(this XmlNode e, Predicate<T> condition, bool deep = false) where T : XmlNode
        {
            var ret = new List<T>();
            ForEachChilds(e, (s) =>
            {
                if (s is T t && condition(t))
                {
                    ret.Add(t);
                }
            }, deep);
            return ret;
        }
        public static void ForEachChilds(this XmlNode node, Action<XmlNode> action, bool deep = false)
        {
            foreach (XmlNode e in node.ChildNodes.ToList())
            {
                action(e);
            }
            if (deep)
            {
                foreach (XmlNode e in node.ChildNodes.ToList())
                {
                    ForEachChilds(e, action, deep);
                }
            }
        }
        public static void ForEachChilds(this XmlNode node, string subname, Action<XmlNode> action, bool deep = false)
        {
            foreach (XmlNode e in node.ChildNodes)
            {
                if (e.Name == subname) { action(e); }
            }
            if (deep)
            {
                foreach (XmlNode e in node.ChildNodes)
                {
                    ForEachChilds(e, subname, action, deep);
                }
            }
        }
        public static void ForEachChilds<T>(this XmlNode node, Action<T> action, bool deep = false) where T : XmlNode
        {
            foreach (XmlNode e in node.ChildNodes.ToList())
            {
                if (e is T et) action(et);
            }
            if (deep)
            {
                foreach (XmlNode e in node.ChildNodes.ToList())
                {
                    ForEachChilds(e, action, deep);
                }
            }
        }
        public static void ForEachChilds<T>(this XmlNode node, string subname, Action<T> action, bool deep = false) where T : XmlNode
        {
            foreach (XmlNode e in node.ChildNodes.ToList())
            {
                if (e is T et)
                {
                    if (e.Name == subname) { action(et); }
                }
            }
            if (deep)
            {
                foreach (XmlNode e in node.ChildNodes.ToList())
                {
                    ForEachChilds(e, subname, action, deep);
                }
            }

        }

        /// <summary>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action">返回true终止迭代!</param></param>
        /// <param name="deep"></param>
        public static bool ForEachChilds(this XmlNode node, string subname, BreakPredicate<XmlNode> action, bool deep = false)
        {
            foreach (XmlNode e in node.ChildNodes.ToList())
            {
                if (e.Name == subname) { if (action(e)) { return true; } }
            }
            if (deep)
            {
                foreach (XmlNode e in node.ChildNodes.ToList())
                {
                    if (ForEachChilds(e, subname, action, deep))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action">返回true终止迭代!</param></param>
        /// <param name="deep"></param>
        public static bool ForEachChilds(this XmlNode node, BreakPredicate<XmlNode> action, bool deep = false)
        {
            foreach (XmlNode e in node.ChildNodes.ToList())
            {
                if (action(e)) { return true; }
            }
            if (deep)
            {
                foreach (XmlNode e in node.ChildNodes.ToList())
                {
                    if (ForEachChilds(e, action, deep))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action">返回true终止迭代!</param></param>
        /// <param name="deep"></param>
        public static bool ForEachChilds<T>(this XmlNode node, string subname, BreakPredicate<T> action, bool deep = false) where T : XmlNode
        {
            foreach (XmlNode e in node.ChildNodes.ToList())
            {
                if (e is T et) if (e.Name == subname) { if (action(et)) { return true; } }
            }
            if (deep)
            {
                foreach (XmlNode e in node.ChildNodes.ToList())
                {
                    if (ForEachChilds(e, subname, action, deep))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action">返回true终止迭代!</param></param>
        /// <param name="deep"></param>
        public static bool ForEachChilds<T>(this XmlNode node, BreakPredicate<T> action, bool deep = false) where T : XmlNode
        {
            foreach (XmlNode e in node.ChildNodes.ToList())
            {
                if (e is T et) if (action(et)) { return true; }
            }
            if (deep)
            {
                foreach (XmlNode e in node.ChildNodes.ToList())
                {
                    if (ForEachChilds(e, action, deep))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Templates
        public static string DataToXmlText(object data)
        {
            StringBuilder output = new StringBuilder();
            try
            {
                XmlDocument doc = XmlUtil.ObjectToXml(data);
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = new UTF8Encoding(false);
                using (XmlWriter xml = XmlWriter.Create(output, settings))
                {
                    doc.Save(xml);
                    xml.Flush();
                }
            }
            catch (Exception err)
            {
                output.AppendLine(err.Message);
                output.AppendLine(err.StackTrace);
            }
            return output.ToString();
        }
        public static byte[] SaveTemplateXML(IExternalizableFactory factory, object mData)
        {
            using (DeepCore.IO.MemoryStream output = new DeepCore.IO.MemoryStream(1024 * 1024))
            {
                Type type = mData.GetType();
                XmlDocument doc = new XmlSerializer(false) { Factory = factory }.ObjectToXml(mData);
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = CUtils.UTF8;
                using (XmlWriter xml = XmlWriter.Create(output, settings))
                {
                    doc.Save(xml);
                    xml.Flush();
                }
                output.Flush();
                byte[] xml_bin = output.ToArray();
                return (xml_bin);
            }
        }
        public static string SaveTemplateXMLText(IExternalizableFactory factory, object mData)
        {
            byte[] xml_bin = SaveTemplateXML(factory, mData);
            return CUtils.UTF8.GetString(xml_bin);
        }
        public static bool ValidateBin(object data, IExternalizableFactory factory, out string srcxml, out string binxml)
        {
            try
            {
                srcxml = SaveTemplateXMLText(factory, data);
                if (data is ISerializable ser)
                {
                    data = factory.Clone(ser);
                }
                byte[] bin;
                using (var ms = new DeepCore.IO.MemoryStream(1024 * 1024))
                {
                    OutputStream output = new OutputStream(ms, factory);
                    output.PutObj(data);
                    ms.Flush();
                    bin = ms.ToArray();
                }
                using (var ms = new DeepCore.IO.MemoryStream(bin))
                {
                    InputStream input = new InputStream(ms, factory);
                    var ret = input.GetObjAny();
                    binxml = SaveTemplateXMLText(factory, ret);
                    if (srcxml.Equals(binxml))
                    {
                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                binxml = srcxml = (err.Message) + "\r\n" + (err.StackTrace);
                Console.WriteLine(binxml);
                throw;
            }
            return false;
        }
        #endregion
        //----------------------------------------------------------------------------------------------------
    }

}
