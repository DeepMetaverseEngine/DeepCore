using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Xml;
using DeepTools.CodeGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static DeepCore.Colors;

namespace DeepTools.LanguageXLS
{
    public class XLSGenerator
    {
        public DeepCore.Properties prop { get; }
        public event Action<Exception> onerror;
        public XLSGenerator(DeepCore.Properties prop)
        {
            this.prop = prop;
        }

        public void JsonProcessXLS(IRangeValue progress = null)
        {
            prop["-oext"] = ".json";
            prop["-templ"] = "xls-json-code.xml";
            LuaProcessXLS(progress);
        }
        public void LuaProcessXLS(IRangeValue progress = null)
        {
            var od = prop.Get("-od");
            if (od == null) throw new Exception("No output dir");
            var file_ext = "";
            if (!prop.TryGetValue("-file_ext", out file_ext))
            {
                file_ext = ".xls$|.xlsx$|.xlsm$";
            }
            var output_dir = new DirectoryInfo(od);
            DirectoryInfo root_dir;
            var inputs = GetInputFiles(prop, file_ext, out root_dir);
            progress?.SetRange(0, inputs.Count, 0);
            foreach (var file in inputs)
            {
                try
                {
                    using (var xls = new XLSLoader(root_dir, file))
                    {
                        xls.SetTemplate("xls-lua-code.xml");
                        xls.SetProperties(prop);
                        xls.LuaProcessXLS(prop, output_dir);
                        //                         var cftt = XmlUtil.ObjectToXmlString(xls.Config);
                        //                         CFiles.WriteAllText($"{output_dir}/cfg.xml",cftt);
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace("Load XLS Error : " + file + " : ");
                    onerror?.Invoke(err);
                }
                finally
                {
                    progress?.Add(1);
                }
            }
        }

        public void LangProcessXLS(IRangeValue progress = null)
        {
            //语言抽取//
            var output_file = prop.Get("-of");
            var append = prop.GetAsBool("-append");
            DirectoryInfo root_dir;
            var inputs = GetInputFiles(prop, ".xls$|.xlsx$", out root_dir);
            Encoding encoding = CUtils.UTF8;
            var template_gen = new XmlCodeTemplate(typeof(XLSLoader).Assembly, false);
            var template = template_gen.LoadTemplate("xls-lang-code.xml");
            var cells = XmlUtil.FindChild<XmlNode>(template.DocumentElement, "CELLS");
            var output_text = new StringBuilder();
            progress?.SetRange(0, inputs.Count, 0);
            foreach (var file in inputs)
            {
                try
                {
                    using (var xls = new XLSLoader(root_dir, file))
                    {
                        xls.SetProperties(prop);
                        xls.LangProcessXLS(cells, output_text);
                        encoding = xls.Encoding;
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace("Load XLS Error : " + file + " : ");
                    onerror?.Invoke(err);
                }
                finally
                {
                    progress?.Add(1);
                }
            }
            cells.InnerText = output_text.ToString();
            CFiles.CreateFile(new FileInfo(output_file));
            if (append)
            {
                File.AppendAllText(output_file, template.InnerText, encoding);
            }
            else
            {
                File.WriteAllText(output_file, template.InnerText, encoding);
            }
        }

        public void LocalProcessXLS()
        {
            var input_file = prop.Get("-if");
            var output_dir = prop.Get("-od");
            try
            {
                using (var xls = new XLSLoader(new FileInfo(input_file).Directory, new FileInfo(input_file)))
                {
                    xls.SetProperties(prop);
                    xls.LocalProcessXLS(new DirectoryInfo(output_dir));
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace("Load XLS Error : " + input_file + " : ");
                onerror?.Invoke(err);
            }
        }

        public void ProcessMD5()
        {
            var input_dir = prop.Get("-id");
            var output_file = prop.Get("-of");
            var enc = prop.Get("-encoding");
            try
            {
                var encoding = CUtils.UTF8;
                if (enc != null)
                    encoding = Encoding.GetEncoding(enc);
                DirectoryInfo root_dir;
                var inputs = GetInputFiles(prop, ".lua$", out root_dir);
                var output = GenMD5(root_dir, inputs, new FileInfo(output_file));
                CFiles.CreateFile(new FileInfo(output_file));
                File.WriteAllText(output_file, output, encoding);
            }
            catch (Exception err)
            {
                err.PrintStackTrace("Load XLS Error : " + input_dir + " : ");
                onerror?.Invoke(err);
            }
        }

        private string GenMD5(DirectoryInfo root, List<FileInfo> inputs, FileInfo output)
        {
            SortedDictionary<string, string> map = new SortedDictionary<string, string>();
            StringBuilder allmd5 = new StringBuilder();
            foreach (var file in inputs)
            {
                var md5 = CMD5.CalculateMD5(File.ReadAllBytes(file.FullName));
                map.Add(file.FullName.Substring(root.FullName.Length).Replace('\\', '/'), md5);
                allmd5.AppendLine(md5);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("return {");
            //sb.AppendLine("[\"time\"] = \"" + DateTime.Now.ToString() + "\",");
            sb.AppendLine("[\"version\"] = \"" + CMD5.CalculateMD5(allmd5.ToString(), CUtils.UTF8) + "\",");
            foreach (var e in map)
            {
                sb.AppendLine(string.Format("[\"{0}\"] = \"{1}\",", e.Key, e.Value));
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
        private List<FileInfo> GetInputFiles(DeepCore.Properties prop, string regex, out DirectoryInfo root_dir)
        {
            var input_dir = prop.Get("-id");
            var ext = new Regex(regex);
            List<FileInfo> inputs = new List<FileInfo>();
            if (input_dir != null)
            {
                root_dir = new DirectoryInfo(input_dir);
                var filter_file = prop.Get("-filter_file");
                var filter_string = prop.Get("-filter_text");
                {
                    FileFilters filter = null;
                    if (filter_string != null)
                    {
                        filter = new FileFilters(filter_string);
                    }
                    else if (filter_file != null)
                    {
                        filter = new FileFilters(File.ReadAllText(filter_file));
                    }
                    CFiles.ListAllFiles(inputs, root_dir, (file) =>
                    {
                        if (ext.IsMatch(file.FullName.ToLower()))
                        {
                            if (filter == null || filter.AcceptFile(file))
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                    inputs.Sort((a, b) => { return String.Compare(a.FullName, b.FullName); });
                }
            }
            else
            {
                root_dir = null;
            }
            return inputs;
        }
    }


}
