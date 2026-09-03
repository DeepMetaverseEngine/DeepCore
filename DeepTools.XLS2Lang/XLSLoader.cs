using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Xml;
using DeepTools.CodeGen;
using DeepTools.LanguageXLS.XLS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

/// <summary>
/// 
/// </summary>
namespace DeepTools.LanguageXLS
{
    public class XLSLoader : IDisposable
    {
        public const string DEFAULT_LANG_FORMAT = "<doc><PATH><FILE><DIR_PREFIX/><FILE_NAME/>_<SHEET_NAME/></FILE><DATA_TYPE/></PATH>_<ROW_ID/>.<COLUMN_NAME/></doc>";
        internal readonly Logger log;
        protected readonly DirectoryInfo root_dir;
        protected readonly FileInfo xls_file;
        protected readonly NPOI.SS.UserModel.IWorkbook Workbook;
        protected readonly XmlCodeTemplate template_gen;
        private XConfig cfg = new XConfig();
        private string template_file;
        //         private int row_head_index = 1;
        //         private string commet_lang = "lang";
        //         private string commet_okey = "_key_";
        private XmlDocument lang_format = XmlUtil.FromString(DEFAULT_LANG_FORMAT);
        private char separator_char = '/';

        private bool output_lang;
        private int hash_start;
        private Encoding encoding = CUtils.UTF8;
        public XConfig Config => cfg;

        public XLSLoader(DirectoryInfo root_dir, FileInfo xls_file)
        {
            this.log = LoggerFactory.GetLogger("[" + xls_file.Name + "]");
            log.Info("Load:  " + xls_file.FullName);
            this.root_dir = root_dir;
            this.xls_file = xls_file;
            this.template_gen = new XmlCodeTemplate(typeof(XLSLoader).Assembly, false);
            byte[] data = LoadData(xls_file);
            if (data == null)
            {
                throw new Exception("Can not read xls file : " + xls_file);
            }
            try
            {
                this.Workbook = NPOI.SS.UserModel.WorkbookFactory.Create(new DeepCore.IO.MemoryStream(data));
            }
            catch (Exception err)
            {
                throw new Exception(err.Message + " : " + xls_file, err);
            }
            //             if (xls_file.Extension.ToLower().EndsWith(".xlsx"))
            //             {
            //                 Workbook = (new NPOI.XSSF.UserModel.XSSFWorkbook(new MemoryStream(data)));
            //             }
            //             else if (xls_file.Extension.ToLower().EndsWith(".xls"))
            //             {
            //                 Workbook = (new NPOI.HSSF.UserModel.HSSFWorkbook(new MemoryStream(data)));
            //             }
            //             else
            //             {
            //                 throw new Exception("Not xls file : " + xls_file);
            //             }
        }
        protected virtual byte[] LoadData(FileInfo file)
        {
            try
            {
                return File.ReadAllBytes(file.FullName);
            }
            catch
            {
                var dst = file.FullName + ".tmp";
                System.IO.File.Copy(file.FullName, dst);
                try
                {
                    return Resource.LoadData(dst);
                }
                finally
                {
                    System.IO.File.Delete(dst);
                }
            }
        }

        public virtual void Dispose()
        {
            Workbook.Close();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        public static string Usage => UsageConfig.Usage;
        public class UsageConfig
        {
            public static string Usage
            {
                get
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("[lang 语言抽取工具参数]");
                    sb.AppendLine("  -id           输入文件夹");
                    sb.AppendLine("  -of           输出文件");
                    sb.AppendLine("  -templ        代码模板文件（可选参数）");
                    sb.AppendLine("  -append       附加到输出文件（可选参数）");
                    sb.AppendLine("[lua Lua转换工具参数]");
                    sb.AppendLine("  -id           输入文件夹");
                    sb.AppendLine("  -od           输出文件夹");
                    sb.AppendLine("  -templ        代码模板文件（可选参数）");
                    sb.AppendLine("  -olang        输出Lua文件，自动将多语言转换为Key（可选参数）");
                    sb.AppendLine("[local 本地化导出]");
                    sb.AppendLine("  -if           输入文件");
                    sb.AppendLine("  -od           输出文件夹");
                    sb.AppendLine("[md5 生成Lua版本文件]");
                    sb.AppendLine("  -id           输入文件夹");
                    sb.AppendLine("  -of           输出文件");
                    sb.AppendLine("[其他可选参数]");
                    sb.AppendLine("  -file_ext     文件名后缀，默认(.xls$|.xlsx$|.xlsm$)");
                    sb.AppendLine("  -filter_text  文件检索过滤器，字符串");
                    sb.AppendLine("  -filter_file  文件检索过滤器，文件名");
                    sb.AppendLine("  -head_index   行字段名标记");
                    sb.AppendLine("  -lang_format  导出语言索引格式: " + XLSLoader.DEFAULT_LANG_FORMAT);
                    sb.AppendLine("  -encoding     导出字符编码");
                    sb.AppendLine("  -oext         导出文件名后缀");
                    sb.AppendLine("  -hash_start   导出哈希开始数字");
                    sb.AppendLine("[格式可选参数]");
                    sb.AppendLine("  -x.prefix_sp    导出语言Key格式，目录间隔符号");
                    sb.AppendLine("  -x.array_start  读取数组时，起始下标索引");
                    sb.AppendLine("  -x.commet_lang  行字段名注释标记，注释标记为多语言");
                    sb.AppendLine("  -x.commet_key   行字段名注释标记，注释标记输出Lua文件为索引表");
                    sb.AppendLine("  -x.array_L      导出数组左括号，默认 {");
                    sb.AppendLine("  -x.array_R      导出数组右括号，默认 }");
                    sb.AppendLine("  -x.array_SP     导出数组分隔符，默认 ,");
                    sb.AppendLine("  -x.combine_L    导出结构体左括号，默认 {");
                    sb.AppendLine("  -x.combine_R    导出结构体右括号，默认 }");
                    sb.AppendLine("  -x.combine_EQ   导出结构体赋值符，默认 =");
                    sb.AppendLine("  -x.combine_SP   导出结构体分隔符，默认 ,");
                    return sb.ToString();
                }
            }
            public string input_file;
            public string input_dir;
            public string input_file_ext;

            public string output_file;
            public string output_dir;
            public bool out_append;
            public string out_ext;
            public int hash_start;

            public XConfig cfg = new XConfig();
            public string template_file;
            public XmlDocument lang_format = XmlUtil.FromString(DEFAULT_LANG_FORMAT);
            public char separator_char = '/';
            public bool output_lang;
            public Encoding encoding = CUtils.UTF8;

            public void Parse(DeepCore.Properties prop)
            {
                this.input_file = prop.Get("-if");
                this.input_dir = prop.Get("-id");
                if (!prop.TryGetValue("-file_ext", out input_file_ext))
                {
                    this.input_file_ext = ".xls$|.xlsx$|.xlsm$";
                }
                this.output_file = prop.Get("-of");
                this.output_dir = prop.Get("-od");
                this.out_append = prop.GetAsBool("-append");
                if (!prop.TryGetValue("-oext", out var out_ext))
                {
                    this.out_ext = ".lua";
                }
                cfg.ParseFrom(prop);

                var _templ = prop.Get("-templ");
                var _format = prop.Get("-lang_format");
                var _separator = prop.Get("-prefix_sp");
                var _enc = prop.Get("-encoding");
                var _olang = prop.Get("-olang");


                if (_templ != null) this.template_file = _templ;
                if (_format != null) this.lang_format = XmlUtil.FromString(_format); //new XmlDocument(_format);
                if (_separator != null && _separator.Length > 0) this.separator_char = _separator[0];
                if (_olang != null) this.output_lang = true;
                if (_enc != null) this.encoding = Encoding.GetEncoding(_enc);
            }
        }

        public void SetProperties(DeepCore.Properties prop)
        {
            cfg.ParseFrom(prop);

            var _templ = prop.Get("-templ");
            var _format = prop.Get("-lang_format");
            var _separator = prop.Get("-prefix_sp");
            var _enc = prop.Get("-encoding");
            var _olang = prop.Get("-olang");

            if (prop.TryGetAsInt("hash_start", out var _hash_start))
            {
                this.hash_start = _hash_start;
            }

            if (_templ != null) this.template_file = _templ;
            if (_format != null) this.lang_format = XmlUtil.FromString(_format); //new XmlDocument(_format);
            if (_separator != null && _separator.Length > 0) this.separator_char = _separator[0];
            if (_olang != null) this.output_lang = true;
            if (_enc != null) this.encoding = Encoding.GetEncoding(_enc);
        }
        public void SetTemplate(string templ)
        {
            if (templ != null) this.template_file = templ;
        }
        public void SetRowIndex(string head_index)
        {
            if (head_index != null) this.cfg.row_head_index = Parser.ParseInt(head_index);
        }
        public void SetArrayStart(string array_start)
        {
            if (array_start != null) this.cfg.array_start_index = Parser.ParseInt(array_start);
        }
        public void SetLangCommentFlag(string flag)
        {
            if (flag != null) this.cfg.lang_comment = flag;
        }
        public void SetLangCommentOutMapFlag(string okey)
        {
            if (okey != null) this.cfg.primary_key_comment = okey;
        }
        public void SetLangKeyFlag(string format)
        {
            if (format != null) this.lang_format = XmlUtil.FromString(format); //new Regex(format);
        }
        public void SetPrefixSeparator(string sp)
        {
            if (sp != null && sp.Length > 0) this.separator_char = sp[0];
        }
        public void SetOutputLuaLangKey(string olang)
        {
            if (olang != null) this.output_lang = true;
        }
        public void SetEncoding(string enc)
        {
            if (enc != null) this.encoding = Encoding.GetEncoding(enc);
        }
        public Encoding Encoding
        {
            get { return encoding; }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public string GetFilePrefix()
        {
            string prefix = xls_file.FullName.Substring(root_dir.FullName.Length);
            prefix = prefix.Substring(0, prefix.Length - xls_file.Name.Length);
            return prefix;
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        #region Xls2Lang

        public void LangProcessXLS(XmlNode cells, StringBuilder output)
        {
            var table = new XLSTable(this, xls_file, Workbook, cfg);
            foreach (var sheet in table.Sheets)
            {
                LangProcessSheet(sheet, cells, output);
            }
        }
        protected void LangProcessSheet(XLSSheet sheet, XmlNode code, StringBuilder output)
        {
            // 扫描所有列头是否包含Lang关键字 //
            foreach (var data_row in sheet.DataRows)
            {
                LangProcessRow(data_row, code, output);
            }
        }

        protected void LangProcessRow(XLSDataRow data_row, XmlNode code, StringBuilder output)
        {
            foreach (XLSCell cell in data_row.Cells)
            {
                try
                {
                    if (cell.HeadCell.IsLangCell)
                    {
                        if (!string.IsNullOrEmpty(cell.Value))
                        {
                            LangProcessCell(cell, code, output);
                        }
                    }
                }
                catch (Exception err)
                {
                    throw new Exception(string.Format("@file={0} @sheet={1} row={2} column={3} : {4}",
                        xls_file,
                        data_row.Sheet.SheetName,
                        data_row.Source.RowNum,
                        cell.HeadCell.SourceHeadCell.ColumnIndex,
                        err.Message), err);
                }
            }
        }

        protected void LangProcessCell(XLSCell cell, XmlNode code, StringBuilder output)
        {
            var key = cell.GetLangKey(lang_format);
            var value = cell.Value;
            log.Info("Process:  " + key + "  ->  " + value);
            code = code.Clone();
            template_gen.SetChildInnerText(code, "KEY", key);
            template_gen.SetChildInnerText(code, "VALUE", value);
            output.AppendLine(code.InnerText);
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        #region Xls2Lua

        public void LuaProcessXLS(DeepCore.Properties prop, DirectoryInfo output_dir)
        {
            var template = template_gen.LoadTemplate(template_file);
            var t_code = template.DocumentElement.Clone();
            var t_cfg = t_code["XCONFIG"];
            if (t_cfg != null)
            {
                this.cfg = XmlUtil.XmlToObject<XConfig>(t_cfg);
                t_code.RemoveChild(t_cfg);
            }
            var table = new XLSTable(this, xls_file, Workbook, cfg);
            if (output_lang)
            {
                table.TranslateToLangKey(lang_format);
            }
            table.Combine();
            foreach (var sheet in table.Sheets)
            {
                if (sheet != null && sheet.DataRows.Length > 0)
                {
                    LuaProcessSheet(prop, sheet, output_dir);
                }
            }
        }
        protected void LuaProcessSheet(DeepCore.Properties prop, XLSSheet sheet, DirectoryInfo output_dir)
        {
            if (!prop.TryGetValue("-oext", out var out_ext))
            {
                out_ext = ".lua";
            }
            var template = template_gen.LoadTemplate(template_file);
            var output_file = new FileInfo(
                output_dir.FullName + Path.DirectorySeparatorChar +
                GetFilePrefix() + Path.DirectorySeparatorChar +
                xls_file.Name + Path.DirectorySeparatorChar +
                sheet.SheetName + out_ext);
            var t_code = template.DocumentElement.Clone();
            var keys = new HashSet<string>();
            var t_cfg = t_code["XCONFIG"];
            if (t_cfg != null)
            {
                t_code.RemoveChild(t_cfg);
            }
            XmlUtil.ForEachChilds(t_code, (e) =>
            {
                // 扫描所有列头 //
                if (e.Name == "HEAD_ROW")
                {
                    if (e["HEAD_KEY"] != null)
                    {
                        var tt_key = e["HEAD_KEY"];
                        if (tt_key["KEY"] != null)
                        {
                            tt_key["KEY"].InnerText = cfg.primary_key_comment;
                        }
                        if (tt_key["COMMENT_KEY"] != null)
                        {
                            tt_key["COMMENT_KEY"].InnerText = cfg.primary_key_comment;
                        }
                        if (tt_key["COMMENT_LANG"] != null)
                        {
                            tt_key["COMMENT_LANG"].InnerText = cfg.lang_comment;
                        }
                    }
                    if (e["PRIMARY_CELL"] != null)
                    {
                        var tt_cell = e["PRIMARY_CELL"];
                        tt_cell.InnerText = LuaProcessPrimaryCell(sheet, tt_cell);
                    }
                    if (e["CELL"] != null)
                    {
                        var tt_cell = e["CELL"];
                        tt_cell.InnerText = LuaProcessHeadRow(sheet, tt_cell);
                    }
                }
                else if (e.Name == "DATA_ROW")
                {
                    // 扫描所有数据 //
                    var t_row_index = sheet.Table.Config.row_output_start_index;
                    var t_row = e["ROW"];
                    e.RemoveChild(t_row);
                    for (var i = 0; i < sheet.DataRows.Length; i++)
                    {
                        var data_row = sheet.DataRows[i];
                        var tt_row = t_row.CloneNode(true);
                        {
                            {
                                var tt_key = tt_row["DATA_KEY"];
                                var key_node = tt_key["KEY"];
                                if (key_node != null)
                                {
                                    var key_txt = $"{t_row_index}";
                                    if (data_row.PrimaryCell != null)
                                    {
                                        key_txt = data_row.PrimaryCell.LuaFormatDataCellText();
                                    }
                                    key_node.InnerText = key_txt;
                                }
                                var key_value_node = tt_key["KEY_VALUE"];
                                if (key_value_node != null)
                                {
                                    var key_txt = t_row_index.ToString();
                                    if (data_row.PrimaryCell != null)
                                    {
                                        key_txt = $"{data_row.PrimaryCell.Value}";
                                    }
                                    if (keys.Contains(key_txt))
                                    {
                                        throw new Exception("重复的Key: " + key_txt + " : " + xls_file.FullName);
                                    }
                                    keys.Add(key_txt);
                                    key_value_node.InnerText = key_txt;
                                }
                                var key_text_node = tt_key["KEY_TEXT"];
                                if (key_text_node != null)
                                {
                                    var key_txt = $"\"{t_row_index}\"";
                                    if (data_row.PrimaryCell != null)
                                    {
                                        key_txt = $"\"{data_row.PrimaryCell.Value}\"";
                                    }
                                    key_text_node.InnerText = key_txt;
                                }
                            }
                            var tt_cell = tt_row["CELL"];
                            tt_cell.InnerText = LuaProcessDataRow(data_row, tt_cell);
                            if (t_row.TryGetAttribute("SPLIT", out var split))
                            {
                                if (i < sheet.DataRows.Length - 1)
                                {
                                    tt_row.AppendChild(tt_row.OwnerDocument.CreateTextNode(split));
                                }
                            }
                            e.AppendChild(tt_row);
                            t_row_index++;
                        }
                    }
                }
            });
            CFiles.CreateFile(output_file);
            File.WriteAllText(output_file.FullName, t_code.InnerText, this.encoding);
            log.Info("Process:  " + GetFilePrefix() + "  ->  " + output_file.Name);

        }
        protected string LuaProcessPrimaryCell(XLSSheet sheet, XmlNode t_cell)
        {
            StringBuilder code = new StringBuilder();
            {
                var cell = sheet.PrimaryCell;
                try
                {
                    var format = t_cell.Clone();
                    if (format["TEXT"] != null) format["TEXT"].InnerText = cell.LuaFormatHeadName();
                    if (format["TYPE"] != null) format["TYPE"].InnerText = cell.LuaFormatHeadType();
                    code.Append(format.InnerText);
                }
                catch (Exception err)
                {
                    throw new Exception(string.Format("{0} : {1}", cell.DebugString(), err.Message), err);
                }
            }
            return code.ToString();
        }
        protected string LuaProcessHeadRow(XLSSheet sheet, XmlNode t_cell)
        {
            var head_row = sheet.DataRows[0];
            StringBuilder code = new StringBuilder();
            for (var i = 0; i < head_row.Cells.Length; i++)
            {
                var cell = head_row.Cells[i];
                try
                {
                    var format = t_cell.Clone();
                    if (format["TEXT"] != null) format["TEXT"].InnerText = cell.LuaFormatHeadName();
                    if (format["TYPE"] != null) format["TYPE"].InnerText = cell.LuaFormatHeadType();
                    code.Append(format.InnerText);
                    if (t_cell.TryGetAttribute("SPLIT", out var split))
                    {
                        if (i < head_row.Cells.Length - 1)
                        {
                            code.Append(split);
                        }
                    }
                }
                catch (Exception err)
                {
                    throw new Exception(string.Format("{0} : {1}", cell.DebugString(), err.Message), err);
                }
            }
            return code.ToString();
        }
        protected string LuaProcessDataRow(XLSDataRow row, XmlNode t_cell)
        {
            StringBuilder code = new StringBuilder();
            for (var i = 0; i < row.Cells.Length; i++)
            {
                var cell = row.Cells[i];
                try
                {
                    var format = t_cell.Clone();
                    format["TEXT"].InnerText = cell.LuaFormatDataCellText(output_lang ? lang_format : null);
                    if (format["TYPE"] != null) format["TYPE"].InnerText = cell.LuaFormatHeadType();
                    if (format["HEAD_TEXT"] != null) format["HEAD_TEXT"].InnerText = cell.LuaFormatHeadName();
                    if (format["HEAD_TYPE"] != null) format["HEAD_TYPE"].InnerText = cell.LuaFormatHeadType();
                    code.Append(format.InnerText);
                    if (t_cell.TryGetAttribute("SPLIT", out var split))
                    {
                        if (i < row.Cells.Length - 1)
                        {
                            code.Append(split);
                        }
                    }
                }
                catch (Exception err)
                {
                    throw new Exception(string.Format("{0} : {1}", cell.DebugString(), err.Message), err);
                }
            }
            return code.ToString();
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        #region Local

        public void LocalProcessXLS(DirectoryInfo root)
        {
            string sheetName = null;
            int ri = 0;
            int ci = 0;
            SortedDictionary<string, StringBuilder> outputs = new SortedDictionary<string, StringBuilder>();
            try
            {
                var workbook = Workbook;
                for (int si = 0; si < workbook.NumberOfSheets; si++)
                {
                    var sheet = workbook.GetSheetAt(si);
                    if (sheet != null)
                    {
                        sheetName = sheet.SheetName;
                        var head_row = sheet.GetRow(sheet.FirstRowNum);
                        if (head_row == null) continue;
                        for (ci = head_row.FirstCellNum + 1; ci < head_row.LastCellNum; ci++)
                        {
                            var head_cell = head_row.GetCell(ci);
                            if (head_cell == null) break;
                            StringBuilder output;
                            if (!outputs.TryGetValue(head_cell.ToString(), out output))
                            {
                                output = new StringBuilder();
                                outputs.Add(head_cell.ToString(), output);
                            }
                            for (ri = sheet.FirstRowNum + 1; ri <= sheet.LastRowNum; ri++)
                            {
                                var row = sheet.GetRow(ri);
                                if (row != null)
                                {
                                    var d_text = row.GetCell(ci);
                                    var h_text = row.GetCell(head_row.FirstCellNum);
                                    if (d_text != null && h_text != null)
                                    {
                                        output.AppendLine(h_text + " = " + d_text);
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (var e in outputs)
                {
                    FileInfo output_file = new FileInfo(root.FullName + Path.DirectorySeparatorChar + e.Key + Path.DirectorySeparatorChar + "lang.properties");
                    CFiles.CreateFile(output_file);
                    File.WriteAllText(output_file.FullName, e.Value.ToString(), encoding);
                    log.Info("Local Output: " + output_file.FullName);
                }
            }
            catch (Exception err)
            {
                throw new Exception(string.Format("@file={0} @sheet={1} row={2} column={3} : {4}", xls_file, sheetName, ri, ci, err.Message), err);
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------------------


    }
}
