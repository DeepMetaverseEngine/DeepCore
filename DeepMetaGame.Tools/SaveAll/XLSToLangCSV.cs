using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using DeepCore;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepTools.LanguageXLS;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static DeepCore.Protocol.MessageCodeManager;

namespace DeepMetaGame.Tools.SaveAll
{
    public class XLSToLangCSV
    {
        public DirectoryInfo XlsDir { get; }
        public DirectoryInfo LangDir { get; }
        public XLSToLangCSV(DirectoryInfo xlsDir, DirectoryInfo langDir)
        {
            this.XlsDir = xlsDir;
            this.LangDir = langDir;
        }
        public static string Usage
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine("# 语言表工作流程 ");
                sb.AppendLine("### 1. 抽取所有XLS里的语言字段{langKey=1}，输出到CSV文件，策划将CSV文件合并到自己的lang.xlsx中，并按照列维护所有语种。");
                sb.AppendLine("### 2. 将策划维护的lang.xlsx输出对应语种的Properties文件，供游戏内加载使用。");
                return sb.ToString().Trim();
            }
        }
        protected virtual LangLine NewLine()
        {
            return new LangLine();
        }
        public void Run()
        {
            // Error Code
            {
                var errorCode = MessageCodeManager.Instance;
                var newFile = FetchAppendCSV(new DirectoryInfo($"{LangDir}\\csv"), "lang_error_code", out var existKeys);
                using (var writer = new StringWriter())
                using (var csv_writer = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false,
                    UseNewObjectForNullReferenceMembers = true,
                    ShouldQuote = (args) => true,
                }))
                {
                    var langLines = new List<LangLine>();
                    if (errorCode != null)
                    {
                        var errorCodes = errorCode.Save();
                        foreach (var record in errorCodes)
                        {
                            var langKey = $"ErrorCode.{record.Key}";
                            if (existKeys.ContainsKey(langKey))
                            {
                                continue;
                            }
                            var line = NewLine();
                            {
                                line.LangKey = langKey;
                                line.zh_CN = record.Value;
                            }
                            langLines.Add(line);
                        }
                    }
                    if (langLines.Count > 0)
                    {
                        csv_writer.WriteRecords(langLines);
                        File.WriteAllText(newFile.FullName, writer.ToString(), Encoding.UTF8);
                    }
                }
            }
            // XLS Data
            {
                var newFile = FetchAppendCSV(new DirectoryInfo($"{LangDir}\\csv"), "lang_xls", out var existKeys);
                // 翻译 lang_xls.csv  
                {
                    var langLines = new List<LangLine>();
                    // 生成 lang_xls.csv
                    try
                    {
                        // xlslang lang  -id:% ~dp0 / tiny_xls / -of:% ROOTPATH % lang\lang.csv - key:id - encoding:utf - 8 - filter_text:-localization / -append:1 - lang_format:"<doc><PATH><FILE><SHEET_NAME/></FILE><DATA_TYPE/></PATH>_<ROW_ID/>.<COLUMN_NAME/></doc>"
                        // xlslang lang  -id:% ~dp0 / tiny_xls / -of:% ROOTPATH % lang\lang.csv - key:id - encoding:utf - 8 - filter_text:+localization / -append:1 - lang_format:"<doc><ROW_ID/></doc>"
                        var args = new string[] { "lang",
                            $"-id:{XlsDir}",
                            $"-of:{LangDir}\\.temp_lang_xls.csv",
                            "-encoding:utf-8" };
                        XLSLang.Gen(args.ArrayAppend(["-lang_format:<doc><FILE_NAME/>/<SHEET_NAME/>/<ROW_ID/>.<COLUMN_NAME/></doc>"]));
                        using (var reader = new StreamReader($"{LangDir}\\.temp_lang_xls.csv"))
                        using (var lang_xls_csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            HasHeaderRecord = false,
                            UseNewObjectForNullReferenceMembers = true,
                            MissingFieldFound = null,
                        }))
                        {
                            //lang_xls_csv.Configuration.HasHeaderRecord = false;
                            var records = lang_xls_csv.GetRecords<LangLine>();
                            foreach (var record in records)
                            {
                                if (existKeys.ContainsKey(record.LangKey))
                                {
                                    continue;
                                }
                                langLines.Add(record);
                            }
                        }
                    }
                    finally
                    {
                        CFiles.Delete($"{LangDir}\\.temp_lang_xls.csv");
                    }
                    if (langLines.Count > 0)
                    {
                        using (var writer = new StringWriter())
                        using (var csv_writer = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            HasHeaderRecord = false,
                            UseNewObjectForNullReferenceMembers = true,
                            ShouldQuote = (args) => true,
                        }))
                        {
                            foreach (var record in langLines)
                            {
                                csv_writer.WriteRecord(record);
                                csv_writer.NextRecord();
                            }
                            CFiles.WriteAllText(newFile.FullName, writer.ToString(), Encoding.UTF8);
                        }
                    }
                }
            }
            // Game Editor
            {

            }
        }

        static public FileInfo FetchAppendCSV(DirectoryInfo dir, string prefix, out HashMap<string, LangLine> existKeys)
        {
            existKeys = new HashMap<string, LangLine>();
            var existFiles = CFiles.ListAllFiles(dir, file => file.Name.StartsWith(prefix));
            foreach (var existFile in existFiles)
            {
                using (var reader = new StreamReader(existFile.FullName))
                using (var lang_xls_csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false,
                    UseNewObjectForNullReferenceMembers = true,
                    MissingFieldFound = null,
                }))
                {
                    var records = lang_xls_csv.GetRecords<LangLine>();
                    foreach (var record in records)
                    {
                        if (!existKeys.TryAdd(record.LangKey, record))
                        {
                            Console.WriteLine($"Add LangKey冲突: {record.LangKey}");
                        }

                    }
                }
            }
            var suffix = CUtils.FormatTime(DateTime.Now);
            return new FileInfo($"{dir.FullName}\\{prefix}_{suffix}.csv");
        }
    }

    public class XLSToLangCSV<T> : XLSToLangCSV where T : LangLine, new()
    {
        public XLSToLangCSV(DirectoryInfo xlsDir, DirectoryInfo langDir) : base(xlsDir, langDir)
        {
        }
        protected override LangLine NewLine()
        {
            return new T();
        }
    }

    public class LangLine
    {
        [Index(0)] public string LangKey { get; set; }
        [Index(1)] public string Dummy { get; set; } = string.Empty;
        [Index(2)] public string zh_CN { get; set; }
        [Index(3)] public string en_US { get; set; }
        [Index(4)] public string zh_TW { get; set; }
    }
}
