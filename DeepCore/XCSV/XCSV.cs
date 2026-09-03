using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace DeepCore.XCSV
{
    public struct XCSVMeta
    {
        public XmlDocument Doc;
        public FileInfo XlsFile;
    }

    public class WorkBook
    {
        public Sheet[] Sheets 
        { 
            get; 
            private set; 
        }
        public string Name
        {
            get;
            private set;
        }

        public static WorkBook LoadFromXML(XmlDocument doc)
        {
            WorkBook ret = new WorkBook();

            XmlElement xml = doc.DocumentElement;
            List<Sheet> sheets = new List<Sheet>(xml.ChildNodes.Count);
            foreach (XmlElement xsheet in xml.ChildNodes)
            {
                List<Row> rows = new List<Row>(xsheet.ChildNodes.Count);
                Sheet sheet = new Sheet(ret, xsheet.GetAttribute("name"), rows);
                foreach (XmlElement xrow in xsheet.ChildNodes)
                {
                    List<Cell> cells = new List<Cell>(xrow.ChildNodes.Count);
                    Row row = new Row(sheet, Parser.ParseInt(xrow.GetAttribute("r")), cells);
                    foreach (XmlElement xcell in xrow.ChildNodes)
                    {
                        cells.Add(new Cell(
                            sheet,
                            Parser.ParseInt(xcell.GetAttribute("r")),
                            Parser.ParseInt(xcell.GetAttribute("c")),
                            xcell.InnerText));
                    }
                    rows.Add(row);
                }
                sheets.Add(sheet);
            }
            ret.Sheets = sheets.ToArray();
            ret.Name = xml.GetAttribute("name");
            return ret;
        }
    }

    public class Sheet
    {
        public readonly WorkBook OwnWorkBook;
        public readonly string Name;
        public readonly List<Row> Rows;

        internal Sheet(WorkBook ownWB, string name, List<Row> rows)
        {
            this.OwnWorkBook = ownWB;
            this.Name = name;
            this.Rows = rows;
        }

    }

    public class Row
    {
        public readonly Sheet OwnSheet;
        public readonly int RowIndex;
        public readonly List<Cell> Cells;

        internal Row(Sheet ownSheet, int r, List<Cell> cells)
        {
            this.OwnSheet = ownSheet;
            this.RowIndex = r;
            this.Cells = cells;
        }

    }

    public class Cell
    {
        public readonly Sheet OwnSheet;
        public readonly int RowIndex;
        public readonly int ColumnIndex;
        public readonly string Text;

        internal Cell(Sheet ownSheet, int r, int c, string text)
        {
            this.OwnSheet = ownSheet;
            this.RowIndex = r;
            this.ColumnIndex = c;
            this.Text = text;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
