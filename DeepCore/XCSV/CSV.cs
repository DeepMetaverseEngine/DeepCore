using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DeepCore.XCSV
{
    public class CsvTable
    {
        public string[][] Cells { get; private set; }
        public int MaxRow { get; private set; }
        public int MaxColumn { get; private set; }
        public void LoadFromText(string csv_text)
        {
            List<List<string>> lines = new List<List<string>>();
            {
                StringBuilder sb = new StringBuilder();
                List<string> line = new List<string>();
                int last_iq = -1;
                for (int i = 0; i < csv_text.Length; i++)
                {
                    char c = csv_text[i];
                    if (c == '\n')
                    {
                        line.Add(sb.ToString());
                        lines.Add(line);
                        line = new List<string>();
                        sb = new StringBuilder();
                    }
                    else if (c == '"')
                    {
                        if (last_iq >= 0)
                        {
                            last_iq = -1;
                        }
                        else
                        {
                            last_iq = i;
                        }
                    }
                    else if (last_iq >= 0)
                    {
                        sb.Append(c);
                    }
                    else if (c == ',')
                    {
                        line.Add(sb.ToString());
                        sb = new StringBuilder();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                line.Add(sb.ToString());
                lines.Add(line);
            }
            {
                this.Cells = new string[lines.Count][];
                this.MaxRow = lines.Count; 
                this.MaxColumn = 0;
                for (int r = 0; r < lines.Count; ++r)
                {
                    List<string> line = lines[r];
                    Cells[r] = new string[line.Count];
                    MaxColumn = Math.Max(MaxColumn, line.Count);
                    for (int c = 0; c < line.Count; ++c)
                    {
                        Cells[r][c] = line[c].Trim();
                    }
                }
            }
        }
        public string GetCell(int column, int row)
        {
            if (row >= 0 && row < Cells.Length)
            {
                if (column >= 0 && column < Cells[row].Length)
                {
                    return Cells[row][column];
                }
            }
            return null;
        }
        public int GetColumnCount(int row)
        {
            if (row >= 0 && row < Cells.Length)
            {
                return Cells[row].Length;
            }
            return -1;
        }
        public bool IsEmptyRow(int row)
        {
            if (row >= 0 && row < Cells.Length)
            {
                for (int c = 0; c < Cells[row].Length; c++)
                {
                    if (!string.IsNullOrEmpty(Cells[row][c]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public static class CsvParser
    {
        private static Log.Logger log = Log.LoggerFactory.GetLogger("CsvParser");

        /// <summary>
        /// 解析CSV对象组，仅支持二维
        /// </summary>
        /// <typeparam name="T">列表对象</typeparam>
        /// <param name="csv_text">CSV文本</param>
        /// <param name="row_index">起始行</param>
        /// <param name="column_index">起始列</param>
        /// <param name="f_split">内容分隔符</param>
        /// <param name="o_split">组分隔符</param>
        /// <returns></returns>
        public static List<T> LoadCSVObjectList<T>(string csv_text, int row_index = 0, int column_index = 0, char f_split = ',', char o_split = ',')
        {
            List<T> ret = new List<T>();
            CsvTable table = new CsvTable();
            table.LoadFromText(csv_text);
            Type type = typeof(T);
            HashMap<string, FieldInfo> fields = new HashMap<string, FieldInfo>();
            foreach (FieldInfo field in type.GetFields())
            {
                fields.Add(field.Name, field);
            }
            string[] header = new string[table.MaxColumn];
            // 头字段定义 //
            for (int c = column_index; c < table.MaxColumn; c++)
            {
                header[c] = table.GetCell(c, row_index);
            }
            // 解析实体对象 //
            for (int r = row_index + 1; r < table.MaxRow; r++)
            {
                if (!table.IsEmptyRow(r))
                {
                    T obj = (T)ReflectionUtil.CreateInstance(type);
                    for (int c = column_index; c < table.MaxColumn; c++)
                    {
                        string ftext = table.GetCell(c, r);
                        string fieldName = header[c];
                        FieldInfo field;
                        if (ftext != null && fields.TryGetValue(fieldName, out field))
                        {
                            try
                            {
                                object fv = TextStreamToAny(ftext, field.FieldType, f_split, o_split);
                                if (fv != null)
                                {
                                    field.SetValue(obj, fv);
                                }
                            }
                            catch (Exception err)
                            {
                                log.Error(err.Message, err);
                            }
                        }
                    }
                    ret.Add(obj);
                }
            }
            return ret;
        }
        public static Dictionary<K, T> LoadCSVObjectMap<K, T>(string csv_text, string keyName, int row_index = 0, int column_index = 0, char f_split = ',', char o_split = ',')
        {
            Dictionary<K, T> ret = new Dictionary<K, T>();
            CsvTable table = new CsvTable();
            table.LoadFromText(csv_text);
            Type type = typeof(T);
            HashMap<string, FieldInfo> fields = new HashMap<string, FieldInfo>();
            FieldInfo keyField = null;
            foreach (FieldInfo field in type.GetFields())
            {
                fields.Add(field.Name, field);
                if (string.Equals(field.Name, keyName))
                {
                    keyField = field;
                }
            }
            if (keyField == null)
            {
                throw new Exception(string.Format("keyName[{0}] not found !", keyName));
            }
            string[] header = new string[table.MaxColumn];
            // 头字段定义 //
            for (int c = column_index; c < table.MaxColumn; c++)
            {
                header[c] = table.GetCell(c, row_index);
            }
            // 解析实体对象 //
            for (int r = row_index + 1; r < table.MaxRow; r++)
            {
                if (!table.IsEmptyRow(r))
                {
                    T obj = (T)ReflectionUtil.CreateInstance(type);
                    object key = null;
                    for (int c = column_index; c < table.MaxColumn; c++)
                    {
                        string ftext = table.GetCell(c, r);
                        string fieldName = header[c];
                        FieldInfo field;
                        if (ftext != null && fields.TryGetValue(fieldName, out field))
                        {
                            try
                            {
                                object fv = TextStreamToAny(ftext, field.FieldType, f_split, o_split);
                                if (fv != null)
                                {
                                    field.SetValue(obj, fv);
                                }
                                if (string.Equals(fieldName, keyName))
                                {
                                    key = fv;
                                }
                            }
                            catch (Exception err)
                            {
                                log.Error(err.Message, err);
                            }
                        }
                    }
                    if (key != null)
                    {
                        ret.Add((K)key, obj);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// 文本序列转化为任何对象
        /// </summary>
        /// <param name="ftext">文本</param>
        /// <param name="type">解析类型</param>
        /// <param name="f_split">内容分隔符</param>
        /// <param name="o_split">组分隔符</param>
        /// <returns></returns>
        public static object TextStreamToAny(string ftext, Type type, char f_split = ',', char o_split = ',')
        {
            if (type.IsArray)
            {
                return TextStreamToList(ftext, type, f_split, o_split);
            }
            else if (type.GetInterface(typeof(IDictionary).Name) != null)
            {
                return null;
            }
            else if (type.GetInterface(typeof(IList).Name) != null)
            {
                return TextStreamToList(ftext, type, f_split, o_split);
            }
            else
            {
                return Parser.StringToObject(ftext, type);
            }
        }

        /// <summary>
        /// 文本序列转化为对象列表
        /// </summary>
        /// <param name="ftext">文本</param>
        /// <param name="type">解析类型</param>
        /// <param name="f_split">内容分隔符</param>
        /// <param name="o_split">组分隔符</param>
        /// <returns></returns>
        public static object TextStreamToList(string text, Type type, char f_split = ',', char o_split = ',')
        {
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                string[] groups = text.Split((IsTypePrimitive(elementType) ? f_split : o_split));
                Array array = Array.CreateInstance(elementType, groups.Length);
                for (int i = 0; i < groups.Length; i++)
                {
                    array.SetValue(TextStreamToObject(groups[i], elementType), i);
                }
                return array;
            }
            else if (type.GetInterface(typeof(IList).Name) != null)
            {
                Type elementType = type.GetGenericArguments()[0];
                string[] groups = text.Split((IsTypePrimitive(elementType) ? f_split : o_split));
                var list = ReflectionUtil.CreateGenericArrayList(elementType);
                for (int i = 0; i < groups.Length; i++)
                {
                    list.Add(TextStreamToObject(groups[i], elementType));
                }
                return list;
            }
            return null;
        }
        /// <summary>
        /// 文本序列转化为对象
        /// </summary>
        /// <param name="ftext">文本</param>
        /// <param name="type">解析类型</param>
        /// <param name="f_split">内容分隔符</param>
        /// <returns></returns>
        public static object TextStreamToObject(string text, Type type, char f_split = ',')
        {
            if (IsTypePrimitive(type))
            {
                return Parser.StringToObject(text, type);
            }
            else if (type.IsClass)
            {
                string[] fields = text.Split(f_split);
                object ret = DeepActivator.CreateInstance(type);
                int i = 0;
                foreach (FieldInfo field in type.GetFields())
                {
                    field.SetValue(ret, Parser.StringToObject(fields[i], field.FieldType));
                    i++;
                }
                return ret;
            }
            return null;
        }
        /// <summary>
        /// 基础类型或者字符串
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsTypePrimitive(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type==(typeof(string));
        }
    }
}
