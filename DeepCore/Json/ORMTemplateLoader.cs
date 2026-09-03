using DeepCore;
using DeepCore.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCore.Json
{
    public class ORMTemplateLoader : TemplateLoader
    {
        new public static ORMTemplateLoader Instance { get; private set; }
        public override string FILE_SUFFIX => ".orm";
        public ORMTemplateLoader(bool instance) : base(instance)
        {
            if (instance == true)
                Instance = this;
        }

        protected override void LoadTemplatesImpl(TemplateDataCenter center, string xlsFile, string[] sheets, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData onLoad)
        {
            xlsFile = Resource.FormatPath(xlsFile);
            if (sheets == null || sheets.Length == 0)//xleFile目录下按sheetname分了多个文件，没有指定就是读取所有
            {
                foreach (var sheetFile in Resource.ListFiles(xlsFile))
                {
                    if (sheetFile.EndsWith(FILE_SUFFIX, CUtils.StringComparisonIgnoreCase))
                    {
                        var sheetName = sheetFile.Substring(0, sheetFile.Length - 5);
                        var jsonbin = LoadJsonFile(xlsFile, sheetName);
                        LoadTemplatesBin(xlsFile, jsonbin, sheetName, dataType, keyField, keyType, createNew, onLoad);
                    }
                }
            }
            else
            {
                foreach (var sheetName in sheets)
                {
                    var jsonbin = LoadJsonFile(xlsFile, sheetName);
                    LoadTemplatesBin(xlsFile, jsonbin, sheetName, dataType, keyField, keyType, createNew, onLoad);
                }
            }
        }
        protected override async Task LoadTemplatesImplAsync(TemplateDataCenter center, string xlsFile, string[] sheets, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData onLoad)
        {
            xlsFile = Resource.FormatPath(xlsFile);
            if (sheets == null || sheets.Length == 0)//xleFile目录下按sheetname分了多个文件，没有指定就是读取所有
            {
                foreach (var sheetFile in Resource.ListFiles(xlsFile))
                {
                    if (sheetFile.EndsWith(FILE_SUFFIX, CUtils.StringComparisonIgnoreCase))
                    {
                        var sheetName = sheetFile.Substring(0, sheetFile.Length - 5);
                        var jsonbin = await LoadJsonFileAsync(xlsFile, sheetName);
                        LoadTemplatesBin(xlsFile, jsonbin, sheetName, dataType, keyField, keyType, createNew, onLoad);
                    }
                }
            }
            else
            {
                foreach (var sheetName in sheets)
                {
                    var jsonbin = await LoadJsonFileAsync(xlsFile, sheetName);
                    LoadTemplatesBin(xlsFile, jsonbin, sheetName, dataType, keyField, keyType, createNew, onLoad);
                }
            }
        }

        public override void GC()
        {

        }

        private string LoadJsonFile(string xlsFile, string sheetName)
        {
            var jsonpath = Resource.FormatPath(string.Format("{0}/{1}.{2}", xlsFile, sheetName, FILE_SUFFIX));
            var ret = Resource.LoadAllText(jsonpath);
            if (ret == null)
                throw new Exception($"Template File Not Found : {jsonpath}");
            return ret;
        }

        private async Task<string> LoadJsonFileAsync(string xlsFile, string sheetName)
        {
            var jsonpath = Resource.FormatPath(string.Format("{0}/{1}.{2}", xlsFile, sheetName, FILE_SUFFIX));
            var ret = await Resource.LoadAllTextAsync(jsonpath);
            if (ret == null)
                throw new Exception($"Template File Not Found : {jsonpath}");
            return ret;
        }


        private int LoadTemplatesBin(string xlsFile, string jsonbin, string sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> onCreate, OnLoadTempaletData onLoad)
        {
            int count = 0;
            try
            {
                if (JsonUtil.TryDecodeObject(jsonbin, dataType, out var data))
                {
                    var f = dataType.GetField(keyField);
                    onLoad(xlsFile, sheetName, f.GetValue(data), data);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载JSON表失败:{xlsFile}:{ex.Message}", ex);
            }
            return count;
        }


    }
}
