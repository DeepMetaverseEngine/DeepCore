using DeepCore;
using DeepCore.Log;
using DeepCore.Xml;
using DeepTools.CodeGen;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace DeepTools.LanguageXLS.XLS
{
    public class XConfig
    {
        public int row_head_index = 1;
        public int row_output_start_index = 1;
        public string primary_key_comment = "_key_";
        public string lang_comment = "lang";
        public int array_start_index = 1;
        public string array_L = "{";
        public string array_R = "}";
        public string array_SP = ",";
        public string combine_L = "{";
        public string combine_R = "}";
        public string combine_TL = "[\"";
        public string combine_TR = "\"]";
        public string combine_EQ = "=";
        public string combine_SP = ",";

        public void ParseFrom(DeepCore.Properties prop)
        {
            if (prop.TryGetValue($"-x.head_index", out var _head_index)) this.row_head_index = Parser.ParseInt(_head_index);
            if (prop.TryGetValue($"-x.row_output_start_index", out var _row_output_start_index)) this.row_output_start_index = Parser.ParseInt(_row_output_start_index);
            if (prop.TryGetValue($"-x.commet_lang", out var _flag)) this.lang_comment = _flag;
            if (prop.TryGetValue($"-x.commet_key", out var _okey)) this.primary_key_comment = _okey;
            if (prop.TryGetValue($"-x.array_start", out var _astart)) this.array_start_index = Parser.ParseInt(_astart);
            if (prop.TryGetValue($"-x.array_L", out var _prop_v)) this.array_L = _prop_v;
            if (prop.TryGetValue($"-x.array_R", out _prop_v)) this.array_R = _prop_v;
            if (prop.TryGetValue($"-x.array_SP", out _prop_v)) this.array_SP = _prop_v;
            if (prop.TryGetValue($"-x.combine_L", out _prop_v)) this.combine_L = _prop_v;
            if (prop.TryGetValue($"-x.combine_R", out _prop_v)) this.combine_R = _prop_v;
            if (prop.TryGetValue($"-x.combine_TL", out _prop_v)) this.combine_TL = _prop_v;
            if (prop.TryGetValue($"-x.combine_TR", out _prop_v)) this.combine_TR = _prop_v;
            if (prop.TryGetValue($"-x.combine_EQ", out _prop_v)) this.combine_EQ = _prop_v;
            if (prop.TryGetValue($"-x.combine_SP", out _prop_v)) this.combine_SP = _prop_v;
        }
    }
    public abstract class XObject
    {
        public object Tag;
        public XLSLoader Loader { get; }
        public Logger log => Loader.log;
        public XObject(XLSLoader loader)
        {
            this.Loader = loader;
        }
        public abstract XConfig Config { get; }
        public abstract string DebugString();
    }
    public abstract class XCell : XObject
    {
        public XCell(XLSLoader loader) : base(loader) { }
        public abstract string HeadName { get; }
        public abstract string HeadType { get; }
        public abstract string LuaFormatDataCellText(XmlDocument lang_format);
        public abstract string LuaFormatHeadName();
        public abstract string LuaFormatHeadType();
    }
    public abstract class XValueCell : XCell
    {
        public XValueCell(XLSLoader loader) : base(loader) { }
        internal abstract void SetSubCellHead(string head);
    }


    public class XLSTable : XObject
    {
        public override XConfig Config { get; }
        public IWorkbook Source { get; }
        public XLSSheet[] Sheets { get { return sheets.ToArray(); } }
        public FileInfo XLSFile { get; }

        private List<XLSSheet> sheets = new List<XLSSheet>();

        public XLSTable(XLSLoader loader, FileInfo xls_file, IWorkbook workbook, XConfig cfg) : base(loader)
        {
            this.Config = cfg;
            this.XLSFile = xls_file;
            this.Source = workbook;
            for (int si = 0; si < workbook.NumberOfSheets; si++)
            {
                var sheet = workbook.GetSheetAt(si);
                if (sheet != null)
                {
                    IRow head_row = sheet.GetRow(cfg.row_head_index);
                    IRow type_row = sheet.GetRow(cfg.row_head_index + 1);
                    if (head_row != null && type_row != null)
                    {
                        ICell primary_cell = head_row.Cells.Find((c) =>
                        {
                            if (c.CellComment?.String?.String != null)
                            {
                                return c.CellComment.String.String.ToUpper().Contains(cfg.primary_key_comment.ToUpper());
                            }
                            return false;
                        });
                        //if (primary_cell != null)
                        {
                            var xsheet = new XLSSheet(this, sheet, head_row, type_row, primary_cell);
                            sheets.Add(xsheet);
                        }
                    }
                }
            }
        }
        public override string DebugString()
        {
            return XLSFile.FullName;
        }
        public XLSSheet GetSheet(string name)
        {
            return sheets.Find((a) => { return a.SheetName == name; });
        }
        public void TranslateToLangKey(XmlDocument format)
        {
            foreach (var sheet in this.Sheets)
            {
                foreach (var row in sheet.DataRows)
                {
                    foreach (var cell in row.Cells)
                    {
                        if (cell is XLSCell xcell)
                        {
                            xcell.TranslateToLangKey(format);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 合并为复合类型
        /// </summary>
        public void Combine()
        {
            foreach (var sheet in this.Sheets)
            {
                sheet.Combine();
            }
        }
    }

    public class XLSSheet : XObject
    {
        public override XConfig Config => Table.Config;
        public ISheet Source { get; }
        public IRow SourceHeadRow { get; }
        public IRow SourceTypeRow { get; }
        public XLSPrimaryCell PrimaryCell { get; }
        public XLSTable Table { get; }
        public string SheetName { get; }
        public int FirstRowNum { get; }
        public XLSDataRow[] DataRows { get { return rows.ToArray(); } }

        private HashMap<int, XLSHeadCell> heads = new HashMap<int, XLSHeadCell>();
        private HashMap<string, XLSHeadCell> heads_name = new HashMap<string, XLSHeadCell>();
        private List<XLSDataRow> rows = new List<XLSDataRow>();

        public XLSSheet(XLSTable table, ISheet sheet, IRow head_row, IRow type_row, ICell primary_cell) : base(table.Loader)
        {
            this.Source = sheet;
            this.SourceHeadRow = head_row;
            this.SourceTypeRow = type_row;
            this.PrimaryCell = primary_cell != null ? new XLSPrimaryCell(this, primary_cell) : null;
            this.Table = table;
            this.SheetName = sheet.SheetName;
            this.FirstRowNum = type_row.RowNum + 1;
            {
                for (int ci = this.SourceHeadRow.FirstCellNum; ci < this.SourceHeadRow.LastCellNum; ci++)
                {
                    ICell head_cell = this.SourceHeadRow.GetCell(ci);
                    ICell type_cell = this.SourceTypeRow.GetCell(ci);
                    if (head_cell != null && type_cell != null &&
                        head_cell.CellType == CellType.String &&
                        type_cell.CellType == CellType.String &&
                        !string.IsNullOrEmpty(head_cell.StringCellValue) &&
                        !string.IsNullOrEmpty(type_cell.StringCellValue))
                    {
                        var xhead = new XLSHeadCell(this, head_cell, type_cell);
                        heads.Add(ci, xhead);
                        heads_name.Add(xhead.Name, xhead);
                    }
                }
            }
            for (int ri = type_row.RowNum + 1; ri <= sheet.LastRowNum; ri++)
            {
                IRow data_row = sheet.GetRow(ri);
                if (!XLSUtils.IsRowEmpty(data_row))
                {
                    var xrow = new XLSDataRow(this, data_row, primary_cell);
                    if (xrow.IsValidRow())
                    {
                        rows.Add(xrow);
                    }
                }
            }
        }
        public bool TryGetHead(int column, out XLSHeadCell head)
        {
            return heads.TryGetValue(column, out head);
        }
        public bool TryGetHead(string columnName, out XLSHeadCell head)
        {
            return heads_name.TryGetValue(columnName, out head);
        }
        public XLSDataRow GetRow(int index)
        {
            return rows[index];
        }
        public override string DebugString()
        {
            return string.Format("@file={0} @sheet={1}", Table.XLSFile.FullName, SheetName);
        }
        internal void Combine()
        {
            foreach (var row in rows)
            {
                row.Combine();
            }
        }
    }

    public class XLSDataRow : XObject
    {
        public override XConfig Config => Sheet.Config;
        public IRow Source { get; }
        public XLSSheet Sheet { get; }
        public XLSCell PrimaryCell { get; }
        public int RowIndex { get; }
        public XCell[] Cells { get { return cells.ToArray(); } }
        private List<XValueCell> cells = new List<XValueCell>();

        public XLSDataRow(XLSSheet sheet, IRow row, ICell primary_cell) : base(sheet.Loader)
        {
            this.Source = row;
            this.Sheet = sheet;
            this.RowIndex = row.RowNum - sheet.FirstRowNum;
            for (int ci = sheet.SourceHeadRow.FirstCellNum; ci < sheet.SourceHeadRow.LastCellNum; ci++)
            {
                ICell head_cell = sheet.SourceHeadRow.GetCell(ci);
                ICell type_cell = sheet.SourceTypeRow.GetCell(ci);
                if (head_cell != null && type_cell != null &&
                    head_cell.CellType == CellType.String &&
                    type_cell.CellType == CellType.String &&
                    !string.IsNullOrEmpty(head_cell.StringCellValue) &&
                    !string.IsNullOrEmpty(type_cell.StringCellValue))
                {
                    ICell data_cell = row.GetCell(ci);
                    //var xhead = new XLSHeadCell(this, head_cell, type_cell);
                    sheet.TryGetHead(ci, out var xhead);
                    var xcell = new XLSCell(this, xhead, data_cell);
                    cells.Add(xcell);
                    if (primary_cell != null && ci == primary_cell.ColumnIndex)
                    {
                        this.PrimaryCell = xcell;
                    }
                }
            }
            //             if (PrimaryCell == null)
            //             {
            //                 throw new Exception("Primary Cell Is Null! " + this.DebugString());
            //             }
        }
        public bool IsValidRow()
        {
            if (PrimaryCell != null && string.IsNullOrEmpty(PrimaryCell.Value))
            {
                return false;
            }
            return true;
        }
        public XCell GetCell(int index)
        {
            return cells[index];
        }
        internal void Combine()
        {
            XLSCombineCell.CombineCells(this, cells);
        }

        public override string DebugString()
        {
            return string.Format("@file={0} @sheet={1} row={2}", Sheet.Table.XLSFile.FullName, Sheet.SheetName, Source.RowNum);
        }
    }


    public class XLSPrimaryCell : XCell
    {
        public override XConfig Config => Sheet.Config;
        public ICell Source { get; }
        public XLSSheet Sheet { get; }
        public string Name { get; }
        public string DataType { get; }
        public DeepCore.Properties CommentProp { get { return prop; } }
        public override string HeadName { get { return Name; } }
        public override string HeadType { get { return DataType; } }

        private DeepCore.Properties prop;

        public XLSPrimaryCell(XLSSheet sheet, ICell cell) : base(sheet.Loader)
        {
            this.Source = cell;
            this.Sheet = sheet;
            this.Name = cell.StringCellValue;
            this.prop = DeepCore.Properties.ParseText(cell.CellComment.String.String.ToLower(), new PropertiesFormat() { Separator = ":", LinkNextLine = null, Comment = null });
            this.DataType = prop.Get("type");
            if (DataType == null) DataType = "";
        }
        public override string DebugString()
        {
            return string.Format("@file={0} @sheet={1} row={2} column={3}",
                           Sheet.Table.XLSFile.FullName,
                           Sheet.SheetName,
                           Source.RowIndex,
                           Source.ColumnIndex);
        }
        public override string LuaFormatHeadName()
        {
            return Name;
        }
        public override string LuaFormatHeadType()
        {
            return DataType;
        }
        public override string LuaFormatDataCellText(XmlDocument lang_format)
        {
            return Name;
        }
        public override string ToString()
        {
            return Name;
        }
    }


    public class XLSHeadCell : XCell
    {
        public override XConfig Config => Sheet.Config;
        public ICell SourceHeadCell { get; }
        public ICell SourceTypeCell { get; }
        public XLSSheet Sheet { get; }
        //public XLSDataRow Row { get; }
        public string Name { get; }
        public string NameComment { get; }
        public string Type { get; }
        public override string HeadName { get { return Name; } }
        public override string HeadType { get { return Type; } }

        public XLSHeadCell(XLSSheet sheet, ICell head_cell, ICell type_cell) : base(sheet.Loader)
        {
            if (head_cell.CellType != CellType.String) head_cell.SetCellType(CellType.String);
            if (head_cell.CellType != CellType.String) type_cell.SetCellType(CellType.String);
            this.SourceHeadCell = head_cell;
            this.SourceTypeCell = type_cell;
            this.Sheet = sheet;
            //this.Row = row;
            this.Name = head_cell.ToString();
            this.NameComment = (head_cell.CellComment != null && head_cell.CellComment.String != null) ? (head_cell.CellComment.String.String) : null;
            this.Type = type_cell.ToString();
            if (!HEAD_TYPES.Contains(this.Type.ToUpper()))
            {
                log.Warn($"字段类型'{this.Type}'@'{this.Name}'不存在，该字段将当做字符串类型处理，允许的字段类型有'{CUtils.ArrayToString(HEAD_TYPES)}'。");
            }
        }

        private readonly string[] HEAD_TYPES = { "STRING", "RAW", "JSON", "NUMBER", "NUMBER_ARRAY", "STRING_ARRAY", "INT", "INT_ARRAY" };

        public bool IsRawCell
        {
            get { return Type.ToUpper() == "RAW"; }
        }
        public bool IsJsonCell
        {
            get { return Type.ToUpper() == "JSON"; }
        }
        public bool IsNumberCell
        {
            get { return Type.ToUpper() == "NUMBER" || Type.ToUpper() == "INT"; }
        }
        public bool IsNumberArray
        {
            get { return Type.ToUpper() == "NUMBER_ARRAY" || Type.ToUpper() == "INT_ARRAY"; }
        }
        public bool IsStringArray
        {
            get { return Type.ToUpper() == "STRING_ARRAY"; }
        }
        public bool IsLangCell
        {
            get { return NameComment != null && NameComment.ToUpper().Contains(Sheet.Table.Config.lang_comment.ToUpper()); }
        }

        public override string LuaFormatHeadName()
        {
            return Name;
        }
        public override string LuaFormatHeadType()
        {
            return Type;
        }
        public override string LuaFormatDataCellText(XmlDocument lang_format)
        {
            return Name;
        }
        public override string DebugString()
        {
            return string.Format("@file={0} @sheet={1} row={2} column={3}",
                           Sheet.Table.XLSFile.FullName,
                           Sheet.SheetName,
                           SourceHeadCell.RowIndex,
                           SourceHeadCell.ColumnIndex);
        }
    }

    public class XLSCell : XValueCell
    {
        public override XConfig Config => Sheet.Config;
        public XLSHeadCell HeadCell { get; }
        public XLSSheet Sheet { get; }
        public XLSDataRow Row { get; }
        public ICell Source { get; }
        public string Value { get; private set; }
        public override string HeadName { get { return headName; } }
        public override string HeadType { get { return HeadCell.HeadType; } }
        private string headName;

        public XLSCell(XLSDataRow row, XLSHeadCell head, ICell cell) : base(row.Loader)
        {
            this.Source = cell;
            this.Sheet = row.Sheet;
            this.Row = row;
            this.HeadCell = head;
            this.headName = head.Name;
            if (cell != null)
            {
                try
                {
                    if (cell.CellType == CellType.String)
                    {
                        this.Value = cell.StringCellValue.Replace("\n", @"\n");
                    }
                    else if (cell.CellType == CellType.Numeric)
                    {
                        this.Value = cell.ToString();
                    }
                    else
                    {
                        cell.SetCellType(CellType.String);
                        this.Value = cell.StringCellValue;
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                    //cell.SetCellType(CellType.String);
                    this.Value = err.Message;//cell.StringCellValue.Replace("\n", @"\n");
                }
            }
            else
            {
                this.Value = string.Empty;
            }
        }
        public override string ToString()
        {
            return string.Format("{0} = {1}", HeadName, Value);
        }
        public override string DebugString()
        {
            return string.Format("@file={0} @sheet={1} row={2} column={3}",
                           Sheet.Table.XLSFile.FullName,
                           Sheet.SheetName,
                           Row.Source.RowNum,
                           HeadCell.SourceHeadCell.ColumnIndex);
        }
        public override string LuaFormatHeadName()
        {
            return HeadName;
        }
        public override string LuaFormatHeadType()
        {
            return HeadCell.LuaFormatHeadType();
        }
        public override string LuaFormatDataCellText(XmlDocument lang_format = null)
        {
            bool is_raw = this.HeadCell.IsRawCell;
            bool is_json = this.HeadCell.IsJsonCell;
            bool is_num = this.HeadCell.IsNumberCell;
            bool is_lang = this.HeadCell.IsLangCell;
            bool is_numArray = this.HeadCell.IsNumberArray;
            bool is_txtArray = this.HeadCell.IsStringArray;

            if (is_lang && lang_format != null)
            {
                return ("\"" + this.GetLangKey(lang_format) + "\"");
            }
            else if (this.Value != null)
            {
                if (is_num)
                {
                    if (string.IsNullOrEmpty(this.Value.Trim()))
                    {
                        return ("0");
                    }
                    else
                    {
                        return (this.Value.Trim());
                    }
                }
                else if (is_numArray)
                {
                    if (string.IsNullOrEmpty(this.Value.Trim()))
                    {
                        return ($"{Config.array_L}{Config.array_R}");
                    }
                    else
                    {
                        try
                        {
                            var txt = Value.ToString();
                            var array = Array.ConvertAll(txt.Split(Config.array_SP), t =>
                            {
                                var nt = t.Trim();
                                nt = nt.ReplaceAll(@"\n", string.Empty);
                                return Parser.ParseDouble(nt.Trim());
                            });
                            return Config.array_L + CUtils.ArrayToString(array, Config.array_SP) + Config.array_R;
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"数组解析失败: >>>{Value}<<<", e);
                        }
                    }
                }
                else if (is_txtArray)
                {
                    if (string.IsNullOrEmpty(this.Value.Trim()))
                    {
                        return ($"{Config.array_L}{Config.array_R}");
                    }
                    else
                    {
                        try
                        {
                            var txt = Value.ToString();
                            var array = Array.ConvertAll(txt.Split(Config.array_SP), t =>
                            {
                                var nt = t.Trim();
                                nt = nt.ReplaceAll(@"\n", string.Empty);
                                return nt.Trim();
                            });
                            return Config.array_L + CUtils.ArrayToString(array, Config.array_SP, "\"", "\"") + Config.array_R;
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"数组解析失败: >>>{Value}<<<");
                        }
                    }
                }
                else if (is_json)
                {
                    var raw = $"{this.Value}";
                    if (string.IsNullOrEmpty(raw))
                    {
                        return ("{}");
                    }
                    else
                    {
                        return (raw);
                    }
                }
                else if (is_raw)
                {
                    var raw = $"{this.Value}";
                    if (string.IsNullOrEmpty(raw))
                    {
                        return ("\"\"");
                    }
                    else if (raw.StartsWith("\"") && raw.EndsWith("\""))
                    {
                        return (raw);
                    }
                    else
                    {
                        return ("\"" + raw + "\"");
                    }
                }
                else
                {
                    return ("\"" + this.Value + "\"");
                }
            }

            if (is_num)
            {
                return ("0");
            }
            else if (is_numArray)
            {
                return ($"{Config.array_L}{Config.array_R}");
            }
            return ("\"\"");

        }
        internal override void SetSubCellHead(string key)
        {
            this.headName = key;
        }
        public string GetLangKey(XmlDocument lang_format)
        {
            var gen = new XmlCodeTemplate(GetType().Assembly, false);
            var xml = lang_format.DocumentElement.Clone();
            var column_name = this.HeadCell.Name;
            var row_id = $"{this.Row.RowIndex}";
            var data_type = "";// this.HeadCell.Type;
            var primary_cell = this.Row.PrimaryCell;
            if (primary_cell != null && this.Sheet.PrimaryCell != null)
            {
                row_id = primary_cell.Value;
                data_type = this.Sheet.PrimaryCell.DataType;
            }
            else
            {

            }
            gen.SetChildInnerText(xml, "DATA_TYPE", data_type);
            gen.SetChildInnerText(xml, "FILE_NAME", this.Sheet.Table.XLSFile.Name);
            gen.SetChildInnerText(xml, "SHEET_NAME", this.Sheet.SheetName);
            gen.SetChildInnerText(xml, "COLUMN_NAME", column_name);
            gen.SetChildInnerText(xml, "ROW_ID", row_id);
            if (string.IsNullOrEmpty(data_type))
            {
                var xml_data_type = XmlUtil.FindChild<XmlNode>(xml, "DATA_TYPE", true);
                if (xml_data_type != null)
                {
                    xml_data_type.ParentNode.RemoveChild(xml_data_type);
                }
            }
            else
            {
                var xml_file = XmlUtil.FindChild<XmlNode>(xml, "FILE", true);
                if (xml_file != null)
                {
                    xml_file.ParentNode.RemoveChild(xml_file);
                }
            }
            //             var outline = lang_format;
            //             outline = outline.Replace("{DATA_TYPE}", this.Sheet.PrimaryCell.DataType);
            //             outline = outline.Replace("{FILE_NAME}", Sheet.Table.XLSFile.Name);
            //             outline = outline.Replace("{SHEET_NAME}", this.Sheet.SheetName);
            //             outline = outline.Replace("{COLUMN_NAME}", column_name + "");
            //             outline = outline.Replace("{ROW_ID}", row_id + "");
            //             return outline;
            return xml.InnerText;
        }
        public void TranslateToLangKey(XmlDocument lang_format)
        {
            if (this.HeadCell.IsLangCell)
            {
                this.Value = GetLangKey(lang_format);
            }
        }
    }

    public class XLSCombineCell : XValueCell
    {
        public enum CombineCollectionType
        {
            LIST, MAP,
        }
        public override XConfig Config => Sheet.Config;
        public XLSSheet Sheet { get; }
        public XLSDataRow Row { get; }
        public CombineCollectionType CollectionType { get; }
        public XValueCell[] SubCells { get { return cells.ToArray(); } }
        public override string HeadName { get { return headName; } }
        public override string HeadType { get { return CollectionType.ToString(); } }
        private string headName;
        private List<XValueCell> cells = new List<XValueCell>();

        public XLSCombineCell(XLSDataRow row, string headName, CombineCollectionType ctype) : base(row.Loader)
        {
            this.Row = row;
            this.Sheet = row.Sheet;
            this.headName = headName;
            this.CollectionType = ctype;
        }
        public override string ToString()
        {
            return HeadName;
        }
        public override string LuaFormatHeadName()
        {
            return HeadName;
        }
        public override string LuaFormatHeadType()
        {
            return HeadType;
        }
        public override string LuaFormatDataCellText(XmlDocument lang_format)
        {
            StringBuilder sb = new StringBuilder();

            if (CollectionType == CombineCollectionType.LIST)
            {
                sb.Append(Config.array_L);
                var array = cells.ToArray();
                Array.Sort(array, (a, b) => { return Parser.ParseInt(a.LuaFormatHeadName()) - Parser.ParseInt(b.LuaFormatHeadName()); });
                sb.Append(CUtils.ArrayToString(array, cell =>
                {
                    return cell.LuaFormatDataCellText(lang_format);
                }, Config.array_SP));
                sb.Append(Config.array_R);
            }
            else
            {
                sb.Append(Config.combine_L);
                sb.Append(CUtils.ListToString(cells, cell =>
                {
                    return $"{Config.combine_TL}{cell.LuaFormatHeadName()}{Config.combine_TR}{Config.combine_EQ}{cell.LuaFormatDataCellText(lang_format)}";
                }, Config.array_SP));
                sb.Append(Config.combine_R);
            }
            return sb.ToString();
        }
        public override string DebugString()
        {
            return string.Format("@file={0} @sheet={1} row={2} column={3}",
                           Sheet.Table.XLSFile.FullName,
                           Sheet.SheetName,
                           Row.Source.RowNum,
                           HeadName);
        }
        public void CheckEmpty()
        {
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (cell == null)
                {
                    throw new Exception($"Combine Sheet '{Sheet.SheetName}', Cell '{this.HeadName}' Index [{i + Config.array_start_index}] Is Empty");
                }
            }
        }
        internal override void SetSubCellHead(string key)
        {
            this.headName = key;
        }
        internal void AppendSubCell(XValueCell cell, string subHeadName, int subIndex = -1)
        {
            switch (CollectionType)
            {
                case CombineCollectionType.MAP:
                    cell.SetSubCellHead(subHeadName);
                    break;
                case CombineCollectionType.LIST:
                    cell.SetSubCellHead(subHeadName);
                    break;
            }
            if (subIndex >= 0)
            {
                if (cells.Count < subIndex + 1)
                {
                    CUtils.SetListSize(cells, subIndex + 1);
                }
                this.cells[subIndex] = cell;
            }
            else
            {
                this.cells.Add(cell);
            }
        }
        public static void CombineCells(XLSDataRow row, List<XValueCell> cells)
        {
            var head_map = CombineCellInternal(row, cells);
            if (head_map.Count > 0)
            {
                foreach (var cell in head_map.Values)
                {
                    CombineCells(row, cell.cells);
                }
            }
            head_map = CombineCellInternal(row, cells);
            if (head_map.Count > 0)
            {
                foreach (var cell in head_map.Values)
                {
                    CombineCells(row, cell.cells);
                }
            }
        }

        private static HashMap<string, XLSCombineCell> CombineCellInternal(XLSDataRow row, List<XValueCell> cells)
        {
            var head_map = new HashMap<string, XLSCombineCell>();
            foreach (var cell in cells.ToArray())
            {
                try
                {
                    int ci, di = cell.HeadName.IndexOf('.');
                    if (di > 0)
                    {
                        var prefix = cell.HeadName.Substring(0, di);
                        var suffix = cell.HeadName.Substring(di + 1);
                        var combine = head_map.Get(prefix);
                        if (combine == null)
                        {
                            combine = new XLSCombineCell(row, prefix, XLSCombineCell.CombineCollectionType.MAP);
                            cells.Insert(cells.IndexOf(cell), combine);
                            head_map.Add(prefix, combine);
                        }
                        combine.AppendSubCell(cell, suffix);
                        cells.Remove(cell);
                        continue;
                    }
                    ci = cell.HeadName.IndexOf('[');
                    di = cell.HeadName.IndexOf(']');
                    if (ci > 0 && ci < di)
                    {
                        var prefix = cell.HeadName.Substring(0, ci);
                        var suffix = cell.HeadName.Substring(ci + 1, di - ci - 1);
                        int sub_index;
                        if (Parser.TryParseInt(suffix, out sub_index) == false) throw new Exception("数组下标非法！");
                        if (row.Config.array_start_index != 0)
                        {
                            sub_index = sub_index - row.Config.array_start_index;
                        }
                        var combine = head_map.Get(prefix);
                        if (combine == null)
                        {
                            combine = new XLSCombineCell(row, prefix, XLSCombineCell.CombineCollectionType.LIST);
                            cells.Insert(cells.IndexOf(cell), combine);
                            head_map.Add(prefix, combine);
                        }
                        combine.AppendSubCell(cell, suffix, sub_index);
                        cells.Remove(cell);
                        continue;
                    }
                }
                catch (Exception err)
                {
                    throw new Exception(err.Message + cell?.DebugString(), err);
                }
            }
            foreach (var cell in head_map.Values)
            {
                cell.CheckEmpty();
            }
            return head_map;
        }

    }

}
