using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace DeepTools.CodeGen
{

    public class XmlCodeTemplate
    {
        private readonly Assembly asm;
        private readonly bool use_build_in;
        private DirectoryInfo template_dir;

        public DirectoryInfo TemplateDir
        {
            get { return template_dir; }
        }
        public XmlCodeTemplate(Assembly asm, bool use_build_in)
        {
            this.asm = asm;
            this.use_build_in = use_build_in;
        }
        public virtual XmlDocument LoadTemplate(string name)
        {
            try
            {
                if (use_build_in == false)
                {
                    if (File.Exists(name))
                    {
                        template_dir = new FileInfo(name).Directory;
                        var text = File.ReadAllText(name, CUtils.UTF8);
                        return LoadXML(text);
                    }
                    if (template_dir != null)
                    {
                        if (File.Exists(template_dir.FullName + Path.DirectorySeparatorChar + name))
                        {
                            var text = File.ReadAllText(template_dir.FullName + Path.DirectorySeparatorChar + name, CUtils.UTF8);
                            return LoadXML(text);
                        }
                    }
                }
                var temp_data = IOUtil.LoadFromAssembly(asm, name);
                if (temp_data != null)
                {
                    var text = IOUtil.ReadAllText(new DeepCore.IO.MemoryStream(temp_data));
                    return LoadXML(text);
                }
            }
            catch (Exception err)
            {
                Console.Error.WriteLine("LoadTempalte Error : " + name + Environment.NewLine + err.Message);
            }
            return null;
        }
        protected virtual XmlDocument LoadXML(string text)
        {
            return IncludeXml(XmlUtil.FromString(text, true));
        }
        protected virtual XmlDocument IncludeXml(XmlDocument template)
        {
            if (template != null)
            {
                XmlUtil.ForEachChilds(template.DocumentElement, (e) =>
                {
                    if (e.Name == "INCLUDE")
                    {
                        var def = this.LoadTemplate(e.InnerText);
                        if (def != null)
                        {
                            e.InnerXml = def.DocumentElement.InnerXml;
                        }
                    }
                }, false);
            }
            return template;
        }

        /// <summary>
        /// <CLASS Indent="            " Trim="True">RegistClass(0x<MESSAGE_ID Format="X8"/>, typeof(<CLASS_NAME/>), R_<CLASS_NAME ReplaceKey="." ReplaceValue="_"/>, W_<CLASS_NAME ReplaceKey="." ReplaceValue="_"/>); //<MESSAGE_ID/></CLASS>
        ///   var replace_k = XmlUtil.GetAttribute(e, "ReplaceKey");
        ///   var replace_v = XmlUtil.GetAttribute(e, "ReplaceValue");
        ///   var prefix = XmlUtil.GetAttribute(e, "Prefix");
        ///   var suffix = XmlUtil.GetAttribute(e, "Suffix");
        ///   var indent = XmlUtil.GetAttribute(e, "Indent");
        ///   var format = XmlUtil.GetAttribute(e, "Format");
        ///   var to_lower = IsAttributeEnable(e, "ToLower");
        ///   var to_upper = IsAttributeEnable(e, "ToUpper");
        ///   var trim = IsAttributeEnable(e, "Trim");
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="value"></param>
        /// <param name="to_string">in value, in format, out innerText</param>
        /// <returns></returns>
        public virtual string FormatInnerText(XmlNode e, Func<XmlNode, string, string> to_string)
        {
            var replace_k = XmlUtil.GetAttribute(e, "ReplaceKey");
            var replace_v = XmlUtil.GetAttribute(e, "ReplaceValue");
            var prefix = XmlUtil.GetAttribute(e, "Prefix");
            var suffix = XmlUtil.GetAttribute(e, "Suffix");
            var indent = XmlUtil.GetAttribute(e, "Indent");
            var format = XmlUtil.GetAttribute(e, "Format");
            var to_lower = XmlCodeUtil.IsAttributeEnable(e, "ToLower");
            var to_upper = XmlCodeUtil.IsAttributeEnable(e, "ToUpper");
            var trim = XmlCodeUtil.IsAttributeEnable(e, "Trim");
            e.InnerText = to_string(e, format);
            if (trim)
            {
                e.InnerText = e.InnerText.Trim();
            }
            if (replace_k != null && replace_v != null)
            {
                e.InnerText = e.InnerText.Replace(replace_k, replace_v);
            }
            if (prefix != null)
            {
                e.InnerText = prefix + e.InnerText;
            }
            if (suffix != null)
            {
                e.InnerText = e.InnerText + suffix;
            }
            if (to_lower)
            {
                e.InnerText = e.InnerText.ToLower();
            }
            if (to_upper)
            {
                e.InnerText = e.InnerText.ToUpper();
            }
            if (indent != null)
            {
                e.InnerText = CUtils.ProcessAllLines(e.InnerText, (line) => { return indent + line; });
            }
            return e.InnerText;
        }
        public virtual string FormatInnerText<T>(XmlNode e, T value, Func<T, string, string> to_string = null)
        {
            return FormatInnerText(e, (se, format) =>
            {
                if (to_string != null)
                {
                    return to_string(value, format);
                }
                else if (value != null)
                {
                    return value.ToString();
                }
                return se.InnerText;
            });
        }

        public virtual void SetChildInnerText<T>(XmlNode parent, string childName, T value, Func<T, string, string> to_string = null)
        {
            XmlUtil.ForEachChilds(parent, (e) =>
            {
                if (e.Name == childName)
                {
                    FormatInnerText(e, value, to_string);
                }
            }, true);
        }
        public virtual void SetChildInnerText(XmlNode parent, string childName, Func<XmlNode, string, string> to_string)
        {
            XmlUtil.ForEachChilds(parent, (e) =>
            {
                if (e.Name == childName)
                {
                    FormatInnerText(e, to_string);
                }
            }, true);
        }
    }
    public static class XmlCodeUtil
    {

        //------------------------------------------------------------------------------------------------------
        public static bool IsAttributeEnable(this XmlNode e, string attr)
        {
            var v = XmlUtil.GetAttribute(e, attr);
            bool ret;
            if (v != null)
            {
                if (bool.TryParse(v, out ret))
                    return ret;
                if (int.TryParse(v, out var reti))
                    return reti != 0;
            }
            return false;
        }

        public static XmlNode CombineNode(XmlNode src, XmlNode dst)
        {
            if (src == null) { return dst; }
            foreach (XmlNode esub in dst.ChildNodes)
            {
                src.AppendChild(esub.Clone());
            }
            return src;
        }
        public static XmlNode CombineNode(this HashMap<string, XmlNode> t_fields, string name, XmlNode e)
        {
            if (t_fields.TryGetValue(name, out var _ft))
            {
                foreach (XmlNode esub in e.ChildNodes)
                {
                    _ft.AppendChild(esub.Clone());
                }
                return _ft;
            }
            else
            {
                t_fields.Add(name, e);
                return e;
            }
        }
        public static void Add(this HashMap<string, List<XmlNode>> t_define_fields, string fieldType, XmlNode node)
        {
            var list = t_define_fields.GetOrAdd(fieldType, (fn) => { return new List<XmlNode>(); });
            list.Add(node);
        }
        public static bool GetMethod(this List<XmlNode> list, string methodName, out XmlNode t_field, out XmlNode t_method, Func<XmlNode, XmlNode, bool> validate = null)
        {
            if (list != null)
            {
                foreach (var f in list)
                {
                    var m = XmlUtil.FindChild<XmlNode>(f, methodName);
                    if (m != null)
                    {
                        if (validate == null || validate(f, m))
                        {
                            t_field = f;
                            t_method = m;
                            return true;
                        }
                    }
                }
            }
            t_field = null;
            t_method = null;
            return false;
        }
        //------------------------------------------------------------------------------------------------------
    }
    public class TypeDefineGroup
    {
        private HashMap<string, List<XmlNode>> t_define_fields = new();
        private HashMap<string, List<XmlNode>> t_define_fields_interface = new();
        private HashMap<string, List<XmlNode>> t_define_fields_element = new();
        private List<XmlNode> t_define_fields_enum = new();
        private List<XmlNode> t_define_fields_unmanaged = new();
        private List<XmlNode> t_define_fields_any = new();

        private HashMap<string, string> t_using_map = new();
        private HashMap<string, Type> t_includes = new();
        private HashMap<string, Type> t_excludes = new();
        private XmlNode t_field_depend_on;

        public TypeDefineGroup(XmlDocument template)
        {
            var t_template = XmlUtil.FindChild<XmlNode>(template.DocumentElement, "CODE_TEMPLATE", true);
            XmlUtil.ForEachChilds(template.DocumentElement, "DEFINE", (t_define) =>
            {
                XmlUtil.ForEachChilds(t_define, (e) =>
                {
                    if (e.Name == "FIELD")
                    {
                        var _attr = XmlUtil.GetAttribute(e, "FieldType");
                        t_define_fields.Add(_attr, e);
                        if (e.IsAttributeEnable("Interface"))
                        {
                            t_define_fields_interface.Add(_attr, e);
                        }
                        else if (e.IsAttributeEnable("Element"))
                        {
                            t_define_fields_element.Add(_attr, e);
                        }
                        else if (e.IsAttributeEnable("Enum"))
                        {
                            t_define_fields_enum.Add(e);
                        }
                        else if (e.IsAttributeEnable("Unmanaged"))
                        {
                            t_define_fields_unmanaged.Add(e);
                        }
                        else if (e.IsAttributeEnable("Any"))
                        {
                            t_define_fields_any.Add(e);
                        }
                    }
                    else if (e.Name == "USING")
                    {
                        t_using_map.Add(e.InnerText.Trim(), e.InnerText.Trim());
                    }
                    else if (e.Name == "INCLUDE_CLASS")
                    {
                        if (!e.InnerText.IsNullOrWhiteSpace())
                        {
                            t_includes.Add(e.InnerText.Trim(), null);
                        }
                    }
                    else if (e.Name == "EXCLUDE_CLASS")
                    {
                        if (!e.InnerText.IsNullOrWhiteSpace())
                        {
                            t_excludes.Add(e.InnerText.Trim(), null);
                        }
                    }
                    else if (e.Name == "FIELD_DEPEND_ON")
                    {
                        t_field_depend_on = e;
                    }
                }, false);
            }, true);
        }

        public void SyncTypes()
        {
            try
            {
                foreach (var e in new List<KeyValuePair<string, Type>>(t_includes))
                {
                    if (!string.IsNullOrWhiteSpace(e.Key))
                    {
                        if (e.Key.StartsWith("[") && e.Key.EndsWith("]"))
                        {
                            try { t_includes.Put(e.Key, ReflectionUtil.GetType(e.Key.Substring(1, e.Key.Length - 2))); } catch { }
                        }
                        else
                        {
                            try { t_includes.Put(e.Key, ReflectionUtil.GetType(e.Key)); } catch { }
                        }
                    }
                }
                foreach (var e in new List<KeyValuePair<string, Type>>(t_excludes))
                {
                    if (!string.IsNullOrWhiteSpace(e.Key))
                    {
                        if (e.Key.StartsWith("[") && e.Key.EndsWith("]"))
                        {
                            try { t_excludes.Put(e.Key, ReflectionUtil.GetType(e.Key.Substring(1, e.Key.Length - 2))); } catch { }
                        }
                        else
                        {
                            try { t_excludes.Put(e.Key, ReflectionUtil.GetType(e.Key)); } catch { }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                Console.WriteLine(err.StackTrace);
            }
        }

        public bool IsInclude(Type type)
        {
            if (t_excludes.Count > 0 || t_includes.Count > 0)
            {
                foreach (var exc in t_excludes)
                {
                    if (exc.Value != null)
                    {
                        if (typeof(Attribute).IsAssignableFrom(exc.Value))
                            if (type.TryGetAttributeByType(exc.Value, out var attr))
                                return false;
                        if (exc.Value.IsAssignableFrom(type))
                            return false;
                    }
                }
                foreach (var i in type.GetInterfaces())
                {
                    if (i.FullName != null)
                    {
                        if (t_excludes.ContainsKey(i.FullName))
                        {
                            return false;
                        }
                    }
                }
                foreach (var inc in t_includes)
                {
                    if (inc.Value != null)
                    {
                        if (typeof(Attribute).IsAssignableFrom(inc.Value))
                            if (type.TryGetAttributeByType(inc.Value, out var attr))
                                return true;
                        if (inc.Value.IsAssignableFrom(type))
                            return true;
                    }
                }
                foreach (var i in type.GetInterfaces())
                {
                    if (i.FullName != null)
                    {
                        if (t_includes.ContainsKey(i.FullName))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool TryGetDependOn(out XmlElement begin, out XmlElement end)
        {
            if (t_field_depend_on != null)
            {
                begin = t_field_depend_on["BEGIN"];
                end = t_field_depend_on["END"];
                return true;
            }
            begin = null;
            end = null;
            return false;
        }

        protected bool GetEnum(string methodName, out XmlNode t_field, out XmlNode t_method, Func<XmlNode, XmlNode, bool> validate = null)
        {
            return t_define_fields_enum.GetMethod(methodName, out t_field, out t_method, validate);
        }
        protected bool GetValueType(string methodName, out XmlNode t_field, out XmlNode t_method, Func<XmlNode, XmlNode, bool> validate = null)
        {
            return t_define_fields_unmanaged.GetMethod(methodName, out t_field, out t_method, validate);
        }
        protected bool GetAny(string methodName, out XmlNode t_field, out XmlNode t_method, Func<XmlNode, XmlNode, bool> validate = null)
        {
            return t_define_fields_any.GetMethod(methodName, out t_field, out t_method, validate);
        }
        protected bool GetInterface(Type fieldType, string methodName, out XmlNode t_field, out XmlNode t_method, Func<XmlNode, XmlNode, bool> validate = null)
        {
            if (!fieldType.IsArray)
            {
                var interfaces = fieldType.GetInterfaces();
                foreach (var inter in interfaces)
                {
                    if (!string.IsNullOrEmpty(inter.FullName) && t_define_fields_interface.TryGetValue(inter.FullName, out var list))
                    {
                        return list.GetMethod(methodName, out t_field, out t_method, validate);
                    }
                }
            }
            t_field = null;
            t_method = null;
            return false;
        }
        protected bool Get(Type fieldType, string methodName, out XmlNode t_field, out XmlNode t_method, Func<XmlNode, XmlNode, bool> validate = null)
        {
            var typeName = fieldType?.FullName ?? "";
            if (!t_define_fields.TryGetValue(typeName, out var list))
            {
                if (fieldType.IsArray)
                {
                    typeName = typeof(System.Array).FullName;
                    if (t_define_fields_element.TryGetValue(typeName, out list))
                    {
                        return list.GetMethod(methodName, out t_field, out t_method, validate);
                    }
                }
                else if (fieldType.IsGenericType && typeName.TryIndexOf("`", out var _left))
                {
                    typeName = typeName.Substring(0, _left);
                    if (t_define_fields_element.TryGetValue(typeName, out list))
                    {
                        return list.GetMethod(methodName, out t_field, out t_method, validate);
                    }
                }
            }
            if (!list.GetMethod(methodName, out t_field, out t_method, validate))
            {
                if (fieldType.IsArray)
                {
                    typeName = typeof(System.Array).FullName;
                    if (t_define_fields_element.TryGetValue(typeName, out list))
                    {
                        return list.GetMethod(methodName, out t_field, out t_method, validate);
                    }
                }
                else if (fieldType.IsGenericType && typeName.TryIndexOf("`", out var _left))
                {
                    typeName = typeName.Substring(0, _left);
                    if (t_define_fields_element.TryGetValue(typeName, out list))
                    {
                        return list.GetMethod(methodName, out t_field, out t_method, validate);
                    }
                }
            }
            return list.GetMethod(methodName, out t_field, out t_method, validate);
        }

        public bool GetMetodDefine(Type ftype, string method, out XmlNode t_method, Func<XmlNode, XmlNode, bool> action)
        {
            try
            {
                var fname = ftype.FullName;
                if (Get(ftype, method, out var t_field, out t_method, action))
                {
                    return true;
                }
                else if (GetInterface(ftype, method, out t_field, out t_method, action))
                {
                    return true;
                }
                else if (ftype.IsEnum)
                {
                    if (GetEnum(method, out t_field, out t_method, action))
                    {
                        return true;
                    }
                }
                else if (ftype.IsValueType)
                {
                    if (GetValueType(method, out t_field, out t_method, action))
                    {
                        return true;
                    }
                }
                return GetAny(method, out t_field, out t_method, action);
            }
            catch (Exception err)
            {
                throw new Exception($"Field Type Error '{ftype.FullName}' : {err.Message}", err);
            }
        }


        //         public void Add(XmlNode node)
        //         {
        //             this.Add("", node);
        //         }
        //         public bool Get(string methodName, out XmlNode t_field, out XmlNode t_method, Func<XmlNode, XmlNode, bool> validate = null)
        //         {
        //             return this.Get(null, methodName, out t_field, out t_method, validate);
        //         }
    }
}
