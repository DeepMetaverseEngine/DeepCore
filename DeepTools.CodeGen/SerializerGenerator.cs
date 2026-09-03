using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Xml;

namespace DeepTools.CodeGen
{
    public class SerializerGenerator
    {
        private readonly XmlNode t_template;
        private readonly TypeDefineGroup t_define;
        private AssemblyLoader dlls = new AssemblyLoader(AppDomain.CurrentDomain);
        private SortedSet<Type> typesadd = new SortedSet<Type>(new ComparisonComparer<Type>((a, b) => a.FullName.CompareTo(b.FullName)));
        private ListDictionary<Type, Type> types = new ListDictionary<Type, Type>();
        private HashMap<Type, int> types_id = new HashMap<Type, int>();
        private StringFilters filter;
        private string t_code_namespace = "Codec";
        private int t_msg_id_begin = 0;
        private int t_msg_id_plus = 0;
        private static Logger log = new LazyLogger("GEN");
        public SerializerGenerator(XmlDocument template)
        {
            this.t_define = new TypeDefineGroup(template);
            this.t_template = XmlUtil.FindChild<XmlNode>(template.DocumentElement, "CODE_TEMPLATE", true);
            //             XmlUtil.ForEachChilds(template.DocumentElement, "DEFINE", (t_define) =>
            //             {
            //                 XmlUtil.ForEachChilds(t_define, (e) =>
            //                 {
            // //                     if (e.Name == "FIELD")
            // //                     {
            // //                         PutFieldTypeDefine(e);
            // //                     }
            // //                     else
            // //                     if (e.Name == "USING")
            // //                     {
            // //                         t_using_map.Add(e.InnerText.Trim(), e.InnerText.Trim());
            // //                     }
            // //                     else if (e.Name == "INCLUDE_CLASS")
            // //                     {
            // //                         t_includes.Add(e.InnerText.Trim(), null);
            // //                     }
            // //                     else if (e.Name == "EXCLUDE_CLASS")
            // //                     {
            // //                         t_excludes.Add(e.InnerText.Trim(), null);
            // //                     }
            // //                     else if (e.Name == "FIELD_DEPEND_ON")
            // //                     {
            // //                         t_field_depend_on = e;
            // //                     }
            //                 }, false);
            //             }, true);
        }

        //         protected virtual void PutFieldTypeDefine(XmlNode e)
        //         {
        //             //var _attr = XmlUtil.GetAttribute(e, "FieldType");
        //             t_define_fields.Add(e);
        // //             if (template_gen.IsAttributeEnable(e, "Interface"))
        // //             {
        // //                 t_define_fields_interface.Add(_attr, e);
        // //             }
        // //             else if (template_gen.IsAttributeEnable(e, "Element"))
        // //             {
        // //                 t_define_fields_element.Add(_attr, e);
        // //             }
        // //             else if (template_gen.IsAttributeEnable(e, "Enum"))
        // //             {
        // //                 t_define_fields_enum.Add(e);
        // //             }
        // //             else if (template_gen.IsAttributeEnable(e, "Unmanaged"))
        // //             {
        // //                 t_define_fields_unmanaged.Add(e);
        // //             }
        // //             else if (template_gen.IsAttributeEnable(e, "Any"))
        // //             {
        // //                 t_define_fields_any.Add(e);
        // //             }
        //         }

        public void SetCodeNamespace(string ns)
        {
            t_code_namespace = ns;
        }
        public void SetBeginMessageID(int i)
        {
            t_msg_id_begin = i;
        }
        //         public void SetPlusMessageID(int i)
        //         {
        //             t_msg_id_plus = i;
        //         }
        public void SetFilter(StringFilters f)
        {
            filter = f;
        }

        public bool AddDll(FileInfo dll)
        {
            return dlls.LoadDll(dll) != null;
        }
        public void LoadClasses(string load)
        {
            foreach (var clas in load.Split(';'))
            {
                try
                {
                    var type = dlls.FindType(clas);
                    DeepActivator.CreateInstance(type);
                    //Console.WriteLine("LoadClass : " + clas);
                }
                catch (Exception err)
                {
                    err.PrintStackTrace("LoadClass : " + clas + " : " + err.Message);
                }
            }
        }
        public void AddTypes(IEnumerable<Type> types) { typesadd.AddRange(types); }
        protected void GenID() { }
        public List<Type> AssembyTypes(Predicate<Type> filter)
        {
            var ret = new List<Type>();
            foreach (var asm in dlls.LoadedAssembies)
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if ((type.IsPublic || type.IsNestedPublic) && filter(type))
                        {
                            ret.Add(type);
                        }
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace("Add Assembly Error : " + asm.FullName);
                }
            }
            return ret;
        }
        protected bool SyncType(Type type)
        {
            try
            {
                if (types.ContainsKey(type))
                {
                }
                else
                {
                    if (IsProtoClass(type))
                    {
                        types.Add(type, type);
                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace("Add Type Error : " + type.FullName);
            }
            return false;
        }
        protected void SyncTypes()
        {
            t_define.SyncTypes();
            types.Clear();
            types_id.Clear();
            foreach (var type in typesadd)
            {
                types.Put(type, type);
            }
            foreach (var asm in dlls.LoadedAssembies)
            {
                try
                {
                    //Console.WriteLine("Add Assembly : " + asm.FullName);
                    foreach (var type in asm.GetTypes())
                    {
                        SyncType(type);
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace("Add Assembly Error : " + asm.FullName);
                }
            }
            types.Sort((a, b) => { return a.FullName.CompareTo(b.FullName); });
            var id_gen = new StringHash(types.Keys);
            foreach (var type in types.Keys)
            {
                if (!type.IsAbstract)
                {
                    //                     var cas = type.GetCustomAttributes(typeof(MessageTypeAttribute), false);
                    //                     var attr = cas != null && cas.Length > 0 ? (MessageTypeAttribute)cas[0] : null;
                    //                     int id = (attr != null) ? attr.MessageTypeID : id_gen.IncrementAndGet();
                    //                     id += t_msg_id_plus;
                    //                     types_id.Add(type, id);
                    var id = id_gen.GetID(type);
                    types_id.Add(type, id);
                    log.Info(string.Format("Sync Type ID : {0} : {1}(0x{2})", type.FullName, id, id.ToString("X8")));
                }
                else
                {
                    types_id.Add(type, 0);
                }
            }
        }
        protected virtual bool IsProtoClass(Type type)
        {
            if (type.IsAnonymousType()) { return false; }
            if (typesadd.Contains(type))
            {
                return true;
            }
            if (type.IsClass && (type.IsPublic || type.IsNestedPublic))
            {
                string name = type.FullName;
                var ign = type.GetCustomAttributes(typeof(IgnoreGenerateAttribute), false);
                if (ign == null || ign.Length == 0)
                {
                    if (filter == null || filter.Accept(type.FullName))
                    {
                        if (t_define.IsInclude(type))
                        {
                            return true;
                        }
                        //                         foreach (var exc in t_excludes)
                        //                         {
                        //                             if (exc.Value != null && exc.Value.IsAssignableFrom(type))
                        //                             {
                        //                                 return false;
                        //                             }
                        //                         }
                        //                         foreach (var i in type.GetInterfaces())
                        //                         {
                        //                             if (i.FullName != null)
                        //                             {
                        //                                 if (t_excludes.ContainsKey(i.FullName))
                        //                                 {
                        //                                     return false;
                        //                                 }
                        //                             }
                        //                         }
                        //                         foreach (var inc in t_includes)
                        //                         {
                        //                             if (inc.Value != null && inc.Value.IsAssignableFrom(type))
                        //                             {
                        //                                 return true;
                        //                             }
                        //                         }
                        //                         foreach (var i in type.GetInterfaces())
                        //                         {
                        //                             if (i.FullName != null)
                        //                             {
                        //                                 if (t_includes.ContainsKey(i.FullName))
                        //                                 {
                        //                                     return true;
                        //                                 }
                        //                             }
                        //                         }
                    }
                }
            }
            return false;
        }

        private DirectoryInfo out_dir;
        private FileInfo out_file;
        private string out_extension = ".cs";
        private SortedSet<string> out_files = new SortedSet<string>();
        private Dictionary<string, string> input_environments = new Dictionary<string, string>();
        public IReadOnlyCollection<string> OutputFiles { get { return out_files; } }
        public void SetOutExtension(string extension)
        {
            this.out_extension = extension ?? ".cs";
        }
        public void SetOutFile(string of)
        {
            this.out_file = of != null ? new FileInfo(of) : null;
        }
        public void SetOutDirectory(string od)
        {
            this.out_dir = od != null ? new DirectoryInfo(od) : null;
        }
        public void SetEnvironments(Dictionary<string, string> environments)
        {
            input_environments.PutAll(environments);
        }

        public void Execute()
        {
            log.Info("Execute : ");
            try
            {
                SyncTypes();

                if (out_file != null)
                {
                    var template = t_template.Clone();
                    template_gen.SetChildInnerText(template, "CODE_NAME_SPACE", t_code_namespace);
                    if (input_environments.Count > 0)
                    {
                        foreach (var env in input_environments)
                        {
                            template_gen.SetChildInnerText(template, env.Key, env.Value);
                        }
                    }
                    XmlUtil.ForEachChilds(template, (e) =>
                    {
                        if (e.Name == "CLASS")
                        {
                            var code = new StringBuilder();
                            foreach (var type in types.Keys)
                            {
                                try
                                {
                                    var t_cls = e.Clone();
                                    if (GenClass(t_cls, type, out var outputName))
                                    {
                                        code.AppendLine(t_cls.InnerText);
                                    }
                                }
                                catch (Exception err)
                                {
                                    err.PrintStackTrace();
                                    code.AppendLine("// Gen Type Error : " +
                                        type.FullName + Environment.NewLine +
                                        err.Message + Environment.NewLine +
                                        err.StackTrace);
                                }
                            }
                            e.InnerText = code.ToString();
                        }
                    });
                    if (out_dir != null && out_dir.Exists)
                    {
                        template_gen.SetChildInnerText(template, "CODE_VERSION", GenMD5.GetProjectCodeMD5(out_dir, out_extension));
                    }
                    WriteAllText(out_file.FullName, template.InnerText);
                    log.Info("Output: -> " + out_file.FullName);
                }
                else if (out_dir != null)
                {
                    foreach (var type in types.Keys)
                    {
                        string path = out_dir.FullName + Path.DirectorySeparatorChar + GenTypeName(type, null) + out_extension;
                        try
                        {
                            var template = t_template.Clone();
                            template_gen.SetChildInnerText(template, "CODE_NAME_SPACE", t_code_namespace);
                            XmlUtil.ForEachChilds(template, (e) =>
                            {
                                if (e.Name == "CLASS")
                                {
                                    GenClass(e, type, out var outputName);
                                    if (outputName != null)
                                    {
                                        path = out_dir.FullName + Path.DirectorySeparatorChar + outputName + out_extension;
                                    }
                                }
                            });
                            WriteAllText(path, template.InnerText);
                            log.Info("Output: -> " + path);
                        }
                        catch (Exception err)
                        {
                            WriteAllText(path, "// Gen Type Error : " +
                                type.FullName + Environment.NewLine +
                                err.Message + Environment.NewLine +
                                err.StackTrace);
                            err.PrintStackTrace("Error: -> " + path);
                            throw;
                        }
                    }
                }
                else
                {

                }
            }
            catch (Exception err)
            {
                if (out_file != null)
                {
                    WriteAllText(out_file.FullName, "Gen Type Error : " +
                               err.Message + Environment.NewLine +
                               err.StackTrace);
                }
                else if (out_dir != null)
                {
                    WriteAllText(out_dir + "/_error.cs", "Gen Type Error : " +
                               err.Message + Environment.NewLine +
                               err.StackTrace);
                }
                err.PrintStackTrace();
            }
        }

        protected void WriteAllText(string file, string content)
        {
            try
            {
                CFiles.CreateFile(file);
                File.WriteAllText(file, content);
                out_files.Add(file);
            }
            catch (Exception err)
            {
                err.PrintStackTrace("Error WriteAllText: -> " + file);
                throw;
            }
        }

        //----------------------------------------------------------------------------------------------------------------
        #region GenCode
        public static bool IsAccepetClass(XmlNode t_class, Type type)
        {
            try
            {
                if (t_class.IsAttributeEnable("IgnoreAbstract") && type.IsAbstract)
                {
                    return false;
                }
                if (t_class.IsAttributeEnable("IgnoreGenericArgs") && type.GetGenericArguments().Length > 0)
                {
                    return false;
                }
                if (t_class.IsAttributeEnable("IgnoreNonPublic") && !type.IsPublic)
                {
                    return false;
                }
                if (t_class.IsAttributeEnable("IgnoreNonEmptyConstructor"))
                {
                    if (type.GetConstructor(Type.EmptyTypes) == null)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                throw;
            }
        }

        private void RemoveUnacceptable(XmlNode t_class, Type type)
        {
            foreach (XmlNode node in t_class.ChildNodes.ToList())
            {
                if (!IsAccepetClass(node, type))
                {
                    node.ParentNode.RemoveChild(node);
                }
                else
                {
                    RemoveUnacceptable(node, type);
                }
            }
        }

        protected virtual bool GenClass(XmlNode t_class, Type type, out string outputName)
        {
            outputName = null;
            if (!IsAccepetClass(t_class, type))
            {
                t_class.InnerText = "";
                return false;
            }
            RemoveUnacceptable(t_class, type);
            HashMap<string, object> f_value_map = new HashMap<string, object>();
            template_gen.SetChildInnerText(t_class, "FILE_NAME", type.Assembly.ToTypeDefineFullName());
            template_gen.SetChildInnerText(t_class, "NAME_SPACE", type.Namespace);
            template_gen.SetChildInnerText(t_class, "CLASS_NAME", (se, format) => GenTypeName(type, se));
            template_gen.SetChildInnerText(t_class, "CLASS_SHORT_NAME", type.Name);
            template_gen.SetChildInnerText(t_class, "CLASS_ATTR_TYPE", (se, format) => GenAttribute(type, se, _at => _at.GetType().Name));
            template_gen.SetChildInnerText(t_class, "CLASS_ATTR", (se, format) => GenAttribute(type, se, _at => _at.ToString()));
            template_gen.SetChildInnerText(t_class, "CLASS_TYPE_VALUE", (se, format) => { f_value_map.Put(se.Name, type); return ""; });
            if (type.HasElementType)
            {
                template_gen.SetChildInnerText(t_class, "ELEMENT_NAME", (se, format) => GenTypeName(type.GetElementType(), se));
                template_gen.SetChildInnerText(t_class, "ELEMENT_SHORT_NAME", type.GetElementType().Name);
            }
            if (type.IsGenericParameter)
            {
                var gargs = type.GetGenericArguments();
                for (int i = 0; i < gargs.Length; i++)
                {
                    template_gen.SetChildInnerText(t_class, string.Format("GENERIC_ARG{0}_NAME", i), (se, format) => GenTypeName(gargs[i], se));
                    template_gen.SetChildInnerText(t_class, string.Format("GENERIC_ARG{0}_SHORT_NAME", i), gargs[i].Name);
                }
            }
            template_gen.SetChildInnerText(t_class, "MESSAGE_ID", types_id.Get(type), (value, format) => { return (format != null) ? value.ToString(format) : value.ToString(); });
            template_gen.SetChildInnerText(t_class, "MESSAGE_ID_VALUE", (se, format) => { f_value_map.Put(se.Name, types_id.Get(type)); return ""; });

            template_gen.SetChildInnerText(t_class, "BASE_CLASS_NAME", (se, format) => GenTypeName(type.BaseType, se));
            template_gen.SetChildInnerText(t_class, "BASE_CLASS_SHORT_NAME", type.BaseType.Name);
            template_gen.SetChildInnerText(t_class, "BASE_CLASS_TYPE_VALUE", (se, format) => { f_value_map.Put(se.Name, type.BaseType); return ""; });

            bool haveBase = types_id.ContainsKey(type.BaseType);
            if (haveBase)
            {
                template_gen.SetChildInnerText(t_class, "BASE_MESSAGE_ID", types_id.Get(type.BaseType), (value, format) => { return (format != null) ? value.ToString(format) : value.ToString(); });
                template_gen.SetChildInnerText(t_class, "BASE_MESSAGE_ID_VALUE", (se, format) => { f_value_map.Put(se.Name, types_id.Get(type.BaseType)); return ""; });
            }
            else
            {
                template_gen.SetChildInnerText(t_class, "BASE_INPUT", "");
                template_gen.SetChildInnerText(t_class, "BASE_OUTPUT", "");
            }
            template_gen.SetChildInnerText(t_class, "DYNAMIC_CALL", (se, format) => GenDynamicInvoke(se, f_value_map));
            var output_name = XmlUtil.FindChild<XmlElement>(t_class, "OUTPUT_NAME", true);
            if (output_name != null)
            {
                outputName = output_name.InnerText;
            }
            XmlUtil.ForEachChilds(t_class, (e) =>
            {
                switch (e.Name)
                {
                    case "FIELDS":
                        GenFields(e, type, haveBase);
                        break;
                    case "INPUT":
                        GenFieldsIO(e, type, "READ", haveBase);
                        break;
                    case "OUTPUT":
                        GenFieldsIO(e, type, "WRITE", haveBase);
                        break;
                    case "BASE_INPUT":
                        FormatInnerText(e);
                        break;
                    case "BASE_OUTPUT":
                        FormatInnerText(e);
                        break;
                }
            });
            template_gen.SetChildInnerText(t_class, "IF_DYNAMIC_CALL", (se, format) => GenDynamicInvokeCondition(se, f_value_map, true));
            template_gen.SetChildInnerText(t_class, "IF_NOT_DYNAMIC_CALL", (se, format) => GenDynamicInvokeCondition(se, f_value_map, false));
            FormatInnerText(t_class);
            return true;
        }


        protected virtual void GenFields(XmlNode t_fields, Type type, bool haveBase)
        {
            var decleard = t_fields.IsAttributeEnable("Decleard");
            var not_null = t_fields.IsAttributeEnable("NotNull");
            var is_static = t_fields.IsAttributeEnable("IsStatic");
            var lines = new List<string>();
            {
                var fields = new FieldGroupMap(type, decleard, is_static, haveBase);
                foreach (var group in fields.Groups)
                {
                    t_define.TryGetDependOn(out var begin, out var end);
                    if (group.IsDepends && begin != null)
                    {
                        lines.Add(GetDependString(group, this, begin));
                    }
                    foreach (var f in group.Fields)
                    {
                        if (GenField(f, t_fields, out var field_line))
                        {
                            if (not_null && string.IsNullOrWhiteSpace(field_line))
                            {
                                //Console.WriteLine("WhiteSpace : " + field_line);
                            }
                            else
                            {
                                lines.Add(field_line);
                            }
                        }
                        else
                        {
                            //Console.Error.WriteLine("ERROR : " + field_line);
                        }
                    }
                    if (group.IsDepends && end != null)
                    {
                        lines.Add(GetDependString(group, this, end));
                    }
                }
            }
            if (!not_null || lines.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var field_line in lines)
                {
                    sb.AppendLine(field_line);
                }
                t_fields.InnerText = sb.ToString();
                FormatInnerText(t_fields);
            }
            else
            {
                t_fields.InnerText = "";
            }
        }

        protected virtual void GenFieldsIO(XmlNode t_io, Type type, string codec, bool havebase)
        {
            var decleard = t_io.IsAttributeEnable("Decleard");
            var sb = new StringBuilder();
            var fields = new FieldGroupMap(type, decleard, false, havebase);
            foreach (var group in fields.Groups)
            {
                t_define.TryGetDependOn(out var begin, out var end);
                if (group.IsDepends && begin != null)
                {
                    sb.AppendLine(GetDependString(group, this, begin));
                }
                foreach (var f in group.Fields)
                {
                    if (f.IsPublic && !f.IsStatic)
                    {
                        if (GenFieldMethod(f, codec, out var f_text))
                        {
                            sb.AppendLine(f_text);
                        }
                        else
                        {
                            //Console.Error.WriteLine("ERROR : " + f_text);
                        }
                    }
                }
                if (group.IsDepends && end != null)
                {
                    sb.AppendLine(GetDependString(group, this, end));
                }
            }
            t_io.InnerText = sb.ToString();
            FormatInnerText(t_io);
        }

        protected virtual void GenFieldChildInnerText(FieldInfo field, XmlNode t_field)
        {
            HashMap<string, object> f_value_map = new HashMap<string, object>();
            template_gen.SetChildInnerText(t_field, "F_NAME", field.Name);
            template_gen.SetChildInnerText(t_field, "F_TYPE_NAME", (se, format) => GenTypeName(field.FieldType, se));
            template_gen.SetChildInnerText(t_field, "F_TYPE_VALUE", (se, format) => { f_value_map.Put(se.Name, field.FieldType); return ""; });
            template_gen.SetChildInnerText(t_field, "F_ATTR_TYPE", (se, format) => GenAttribute(field, se, _at => _at.GetType().Name));
            template_gen.SetChildInnerText(t_field, "F_ATTR", (se, format) => GenAttribute(field, se, _at => _at.ToString()));
            if (field.IsStatic)
            {
                template_gen.SetChildInnerText(t_field, "F_VALUE", (se, format) =>
                {
                    var value = field.GetValue(null);
                    f_value_map.Put(se.Name, value);
                    return value + "";
                });
            }
            if (field.FieldType.HasElementType)
            {
                template_gen.SetChildInnerText(t_field, "F_E_TYPE_NAME", (se, format) => GenTypeName(field.FieldType.GetElementType(), se));
                template_gen.SetChildInnerText(t_field, "F_E_TYPE_VALUE", (se, format) => { f_value_map.Put(se.Name, field.FieldType.GetElementType()); return ""; });
            }
            else if (field.FieldType.IsGenericType)
            {
                Type[] gts = field.FieldType.GetGenericArguments();
                for (int i = 0; i < gts.Length; i++)
                {
                    template_gen.SetChildInnerText(t_field, string.Format("F_G{0}_TYPE_NAME", i), (se, format) => GenTypeName(gts[i], se));
                    template_gen.SetChildInnerText(t_field, string.Format("F_G{0}_TYPE_VALUE", i), (se, format) => { f_value_map.Put(se.Name, gts[i]); return ""; });
                }
            }
            XmlUtil.ForEachChilds(t_field, (e) =>
            {
                if (e.Name == "F_TYPE_DEFINE")
                {
                    if (GenFieldMethod(field, XmlUtil.GetAttribute(e, "Method"), out var f_text))
                    {
                        e.InnerText = f_text;
                    }
                }
            }, true);
            template_gen.SetChildInnerText(t_field, "DYNAMIC_CALL", (se, format) => GenDynamicInvoke(se, f_value_map));
        }

        protected virtual bool GenField(FieldInfo field, XmlNode t_fields, out string text)
        {
            if (field.IsPublic)
            {
                var is_static = t_fields.IsAttributeEnable("IsStatic");
                if (field.IsStatic == is_static)
                {
                    {
                        var t_attr = XmlUtil.GetAttribute(t_fields, "IsAttribute");
                        if (t_attr != null && PropertyUtil.GetAttributeByName(field, t_attr) == null)
                        {
                            text = null;
                            return false;
                        }
                    }
                    {
                        var t_not_attr = XmlUtil.GetAttribute(t_fields, "NotAttribute");
                        if (t_not_attr != null && PropertyUtil.GetAttributeByName(field, t_not_attr) != null)
                        {
                            text = null;
                            return false;
                        }
                    }
                    try
                    {
                        if (ValidateFieldDefine(field, t_fields))
                        {
                            t_fields = t_fields.Clone();
                            GenFieldChildInnerText(field, t_fields);
                            text = FormatInnerText(t_fields);
                            return true;
                        }
                    }
                    catch (Exception err)
                    {
                        err.PrintStackTrace();
                        text = ("/* gen field error : " + field.Name + " (" + field.FieldType.FullName + ") " + err.Message + " */");
                        return false;
                    }
                }
            }
            text = null;
            return false;
        }

        protected virtual bool GenFieldMethod(FieldInfo field, string codec, out string text)
        {
            try
            {
                if (GetFieldDefine(field, codec, out var t_method))
                {
                    text = (GenFieldDefineMetod(field, codec, t_method));
                    return true;
                }
                text = ("/* no field define : " + field.Name + " (" + field.FieldType.FullName + ")" + " */");
                return false;
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
                text = ("/* gen field error : " + field.Name + " (" + field.FieldType.FullName + ") " + err.Message + " */");
                return false;
            }
        }
        protected virtual string GenFieldDefineMetod(FieldInfo field, string method, XmlNode t_method)
        {
            var t_io = t_method.Clone();
            GenFieldChildInnerText(field, t_io);
            if (field.FieldType.HasElementType)
            {
                template_gen.SetChildInnerText(t_io, "F_E_" + method, GenFieldDefineElement(field.FieldType.GetElementType(), method));
            }
            else if (field.FieldType.IsGenericType)
            {
                Type[] gts = field.FieldType.GetGenericArguments();
                for (int i = 0; i < gts.Length; i++)
                {
                    template_gen.SetChildInnerText(t_io, string.Format("F_G{0}_{1}", i, method), GenFieldDefineElement(gts[i], method));
                }
            }
            return FormatInnerText(t_io);
        }
        protected virtual string GenFieldDefineElement(Type ftype, string method, int deep = 1)
        {
            if (GetTypeDefine(ftype, "E_" + method, out var t_method))
            {
                var t_r = t_method.Clone();
                template_gen.SetChildInnerText(t_r, "F_TYPE_NAME", (se, format) => GenTypeName(ftype, se));
                template_gen.SetChildInnerText(t_r, "F_DEEP", deep);
                if (ftype.HasElementType)
                {
                    var etype = ftype.GetElementType();
                    template_gen.SetChildInnerText(t_r, "F_E_TYPE_NAME", (se, format) => GenTypeName(etype, se));
                    template_gen.SetChildInnerText(t_r, "F_E_" + method, GenFieldDefineElement(etype, method, deep + 1));
                }
                else if (ftype.IsGenericType)
                {
                    Type[] gts = ftype.GetGenericArguments();
                    for (int i = 0; i < gts.Length; i++)
                    {
                        template_gen.SetChildInnerText(t_r, string.Format("F_G{0}_TYPE_NAME", i), (se, format) => GenTypeName(gts[i], se));
                        template_gen.SetChildInnerText(t_r, string.Format("F_G{0}_{1}", i, method), GenFieldDefineElement(gts[i], method, deep + 1));
                    }
                }
                return FormatInnerText(t_r);
            }
            return ("/* no element field define */");
        }

        protected virtual string GenTypeName(Type type, XmlNode se)
        {
            try
            {
                if (se != null && se.IsAttributeEnable("IsReflection"))
                {
                    return type.FullName;
                }
                if (GetTypeDefine(type, "GET_TYPE_NAME", out var t_method))
                {
                    var t_io = t_method.Clone();
                    template_gen.SetChildInnerText(t_io, "F_TYPE_NAME", type.FullName.Replace('+', '.'));
                    if (type.HasElementType)
                    {
                        template_gen.SetChildInnerText(t_io, "F_E_TYPE_NAME", (sse, format) => GenTypeName(type.GetElementType(), sse));
                    }
                    else if (type.IsGenericType)
                    {
                        Type[] gts = type.GetGenericArguments();
                        for (int i = 0; i < gts.Length; i++)
                        {
                            template_gen.SetChildInnerText(t_io, string.Format("F_G{0}_TYPE_NAME", i), (sse, format) => GenTypeName(gts[i], sse));
                        }
                    }
                    return FormatInnerText(t_io).Trim();
                }
                return ReplaceUsingName(type, se);//type.FullName.Replace('+', '.');
            }
            catch (Exception err)
            {
                err.PrintStackTrace(("/*  gen field error : " + type.FullName + " : " + err.Message + " */"));
                return ("/*  gen field error : " + type.FullName + " : " + err.Message + " */");
            }
        }
        protected virtual string ReplaceUsingName(Type type, XmlNode se)
        {
            //             var using_text = t_using_map.Get(type.Namespace);
            //             if (using_text != null)
            //             {
            //                 return type.ToTypeDefineName();
            //             }
            if (se != null && se.IsAttributeEnable("EmptyGenericArgs"))
            {
                return type.ToTypeDefineFullNameNoArgs();
            }
            return type.ToTypeDefineFullName();
        }
        protected virtual string GenAttribute(MemberInfo member, XmlNode se, Func<Attribute, string> to_string)
        {
            try
            {
                foreach (Attribute attr in member.GetCustomAttributes())
                {
                    var t_attr = XmlUtil.GetAttribute(se, "Attribute");
                    if (t_attr != null && string.Equals(t_attr, attr.GetType().Name))
                    {
                        return to_string(attr);
                    }
                }
                return "";
            }
            catch (Exception err)
            {
                err.PrintStackTrace(("/* gen attribute error : " + member.ToString() + " : " + err.Message + " */"));
                return ("/* gen attribute error : " + member.ToString() + " : " + err.Message + " */");
            }
        }
        protected virtual string GenDynamicInvoke(XmlNode se, HashMap<string, object> f_value_map)
        {
            var method = XmlUtil.GetAttribute(se, "InvokeMethod");
            try
            {
                if (method != null)
                {
                    var list = new List<object>();
                    {
                        XmlUtil.ForEachChilds(se, (child) =>
                        {
                            if (f_value_map.TryGetValue(child.Name, out var param))
                            {
                                list.Add(param);
                            }
                            else
                            {
                                list.Add(child.InnerText);
                            }
                        });
                        return ReflectionUtil.CallStaticMethod(method, list.ToArray()) + "";
                    }
                }
                return "";
            }
            catch (Exception err)
            {
                var msg = ($"/* gen dynamic invoke error : XML={se.Name} : Method={method} : Params=({f_value_map.MapToString(" ", ",")}) : Error={err.Message} */");
                err.PrintStackTrace(msg);
                return msg;
            }
        }
        protected virtual string GenDynamicInvokeCondition(XmlNode se, HashMap<string, object> f_value_map, bool condition)
        {
            var method = XmlUtil.GetAttribute(se, "InvokeMethod");
            try
            {
                if (method != null)
                {
                    var list = new List<object>();
                    {
                        foreach (XmlAttribute attr in se.Attributes)
                        {
                            if (attr.Name != "InvokeMethod")
                            {
                                if (f_value_map.TryGetValue(attr.Value, out var param))
                                {
                                    list.Add(param);
                                }
                                else
                                {
                                    list.Add(attr.Value);
                                }
                            }
                        }
                        if (condition == ReflectionUtil.CallStaticMethod<bool>(method, list.ToArray()))
                        {
                            return FormatInnerText(se);
                        }
                    }
                }
                return string.Empty;
            }
            catch (Exception err)
            {
                var msg = ($"/* gen dynamic invoke condition error : XML={se.Name} : Method={method} : Params=({f_value_map.MapToString(" ", ",")}) : Error={err.Message} */");
                err.PrintStackTrace(msg);
                return msg;
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------
        #region Utils

        protected virtual string FormatInnerText(XmlNode e)
        {
            return template_gen.FormatInnerText<string>(e, e.InnerText);
        }
        protected virtual bool GetTypeDefine(Type ftype, string method, out XmlNode t_method)
        {
            var action = new Func<XmlNode, XmlNode, bool>((f, m) => ValidateTypeDefine(ftype, f));
            return t_define.GetMetodDefine(ftype, method, out t_method, action);
        }
        protected virtual bool GetFieldDefine(FieldInfo field, string method, out XmlNode t_method)
        {
            var ftype = field.FieldType;
            var action = new Func<XmlNode, XmlNode, bool>((f, m) => ValidateTypeDefine(ftype, f) && ValidateFieldDefine(field, f));
            return t_define.GetMetodDefine(ftype, method, out t_method, action);
        }
        //         protected virtual bool GetMetodDefine(Type ftype, string method, out XmlNode t_method, Func<XmlNode, XmlNode, bool> action)
        //         {
        //             try
        //             {
        //                 XmlNode t_field = null;
        //                 var fname = ftype.FullName;
        //                 if (t_define_fields.Get(ftype, method, out t_field, out t_method, action))
        //                 {
        //                     return true;
        //                 }
        //                 else if (t_define_fields.GetInterface(ftype, method, out t_field, out t_method, action))
        //                 {
        //                     return true;
        //                 }
        //                 else if (ftype.IsEnum)
        //                 {
        //                     if (t_define_fields.GetEnum(method, out t_field, out t_method, action))
        //                     {
        //                         return true;
        //                     }
        //                 }
        //                 else if (ftype.IsValueType)
        //                 {
        //                     if (t_define_fields.GetValueType(method, out t_field, out t_method, action))
        //                     {
        //                         return true;
        //                     }
        //                 }
        //                 return t_define_fields.GetAny(method, out t_field, out t_method, action);
        //             }
        //             catch (Exception err)
        //             {
        //                 throw new Exception($"Field Type Error '{ftype.FullName}' : {err.Message}", err);
        //             }
        //         }
        protected virtual bool ValidateTypeDefine(Type type, XmlNode t_field)
        {
            var validate = XmlUtil.GetAttribute(t_field, "ValidateType");
            if (!string.IsNullOrEmpty(validate))
            {
                try
                {
                    return ReflectionUtil.CallStaticMethod<bool>(validate, type);
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                    return false;
                }
            }
            return true;
        }
        protected virtual bool ValidateFieldDefine(FieldInfo field, XmlNode t_field)
        {
            var validate = XmlUtil.GetAttribute(t_field, "ValidateField");
            if (!string.IsNullOrEmpty(validate))
            {
                try
                {
                    return ReflectionUtil.CallStaticMethod<bool>(validate, field);
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                    return false;
                }
            }
            return true;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------------------
        static private XmlCodeTemplate template_gen;
        static public void SetCodeTemplate(Assembly assembly, bool use_build_in)
        {
            template_gen = new XmlCodeTemplate(assembly, use_build_in);
        }
        static public XmlDocument LoadTemplate(string name)
        {
            return template_gen.LoadTemplate(name);
        }

        public static string GetDependString(FieldGroup fg, SerializerGenerator gen, XmlNode begin_or_end)
        {
            if (fg.Depends != null)
            {
                begin_or_end = begin_or_end.Clone();
                try
                {
                    var depend_on = begin_or_end["DEPEND_ON"];
                    if (depend_on != null)
                    {
                        var op = depend_on.GetAttribute("OP");
                        var depend_txt = CUtils.ListToString<FieldDepend>(fg.Depends, (o) =>
                        {
                            var dp = depend_on.Clone() as XmlElement;
                            template_gen.SetChildInnerText(dp, "D_FIELD", o.Depend.MemberName);
                            template_gen.SetChildInnerText(dp, "D_EXPECT", o.Depend.Expect);
                            return dp.InnerText;
                        }, op);
                        template_gen.SetChildInnerText(begin_or_end, "DEPEND_ON", depend_txt);
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                    template_gen.SetChildInnerText(begin_or_end, "DEPEND_ON", err.Message);
                }
                return begin_or_end.InnerText;
            }
            return "";
        }
    }
}
