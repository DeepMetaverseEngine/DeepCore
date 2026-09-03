using DeepCore;
using DeepCore.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using static DeepCore.TemplateLoader;

namespace CatJson
{
    public class CatJsonTemplateLoader : TemplateLoader
    {
        new public static CatJsonTemplateLoader Instance { get; private set; }
        public override string FILE_SUFFIX => ".json";
        public CatJsonTemplateLoader(bool instance) : base(instance)
        {
            if (instance == true)
                Instance = this;
        }

        protected override void LoadTemplatesImpl(TemplateDataCenter center, string xlsFile, string[] sheets, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData onLoad)
        {
            xlsFile = Resource.FormatPath(xlsFile);
            var count = 0;
            if (Resource.ExistData(xlsFile))
            {
                var jsonbin = Resource.LoadAllText(xlsFile);
                if (jsonbin == null)
                    throw new Exception($"Template File Not Found : {xlsFile}");
                LoadTemplatesBin(center, xlsFile, jsonbin, null, dataType, keyField, keyType, createNew, onLoad);
                return;
            }
            else if (sheets == null || sheets.Length == 0)//xleFile目录下按sheetname分了多个文件，没有指定就是读取所有
            {
                foreach (var sheetFile in Resource.ListFiles(xlsFile))
                {
                    if (sheetFile.EndsWith(".json", CUtils.StringComparisonIgnoreCase))
                    {
                        var sheetName = sheetFile.Substring(0, sheetFile.Length - 5);
                        var jsonbin = LoadJsonFile(xlsFile, sheetName);
                        count += LoadTemplatesBin(center, xlsFile, jsonbin, sheetName, dataType, keyField, keyType, createNew, onLoad);
                    }
                }
            }
            else
            {
                foreach (var sheetName in sheets)
                {
                    var jsonbin = LoadJsonFile(xlsFile, sheetName);
                    count += LoadTemplatesBin(center, xlsFile, jsonbin, sheetName, dataType, keyField, keyType, createNew, onLoad);
                }
            }
        }
        protected override async Task LoadTemplatesImplAsync(TemplateDataCenter center, string xlsFile, string[] sheets, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData onLoad)
        {
            xlsFile = Resource.FormatPath(xlsFile);
            if (await Resource.ExistDataAsync(xlsFile))
            {
                var jsonbin = await Resource.LoadAllTextAsync(xlsFile);
                if (jsonbin == null)
                    throw new Exception($"Template File Not Found : {xlsFile}");
                LoadTemplatesBin(center, xlsFile, jsonbin, null, dataType, keyField, keyType, createNew, onLoad);
                return;
            }
            else if (sheets == null || sheets.Length == 0)//xleFile目录下按sheetname分了多个文件，没有指定就是读取所有
            {
                foreach (var sheetFile in await Resource.ListFilesAsync(xlsFile))
                {
                    if (sheetFile.EndsWith(".json", CUtils.StringComparisonIgnoreCase))
                    {
                        var sheetName = sheetFile.Substring(0, sheetFile.Length - 5);
                        var jsonbin = await LoadJsonFileAsync(xlsFile, sheetName);
                        LoadTemplatesBin(center, xlsFile, jsonbin, sheetName, dataType, keyField, keyType, createNew, onLoad);
                    }
                }
            }
            else
            {
                foreach (var sheetName in sheets)
                {
                    var jsonbin = await LoadJsonFileAsync(xlsFile, sheetName);
                    LoadTemplatesBin(center, xlsFile, jsonbin, sheetName, dataType, keyField, keyType, createNew, onLoad);
                }
            }
        }

        public override void GC()
        {

        }

        private string LoadJsonFile(string xlsFile, string sheetName)
        {
            var jsonpath = Resource.FormatPath(string.Format("{0}/{1}.json", xlsFile, sheetName));
            var ret = Resource.LoadAllText(jsonpath);
            if (ret == null)
                throw new Exception($"Template File Not Found : {jsonpath}");
            return ret;
        }

        private async Task<string> LoadJsonFileAsync(string xlsFile, string sheetName)
        {
            var jsonpath = Resource.FormatPath(string.Format("{0}/{1}.json", xlsFile, sheetName));
            var ret = await Resource.LoadAllTextAsync(jsonpath);
            if (ret == null)
                throw new Exception($"Template File Not Found : {jsonpath}");
            return ret;
        }


        private int LoadTemplatesBin(TemplateDataCenter center, string xlsFile, string jsonbin, string sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> onCreate, OnLoadTempaletData onLoad)
        {
            int count = 0;
            try
            {
                //string jsonpath = null;
                //var jsonbin = LoadJsonFile(xlsFile, sheetName, out jsonpath);
                lock (CatJson.JsonParser.Default)
                {
                    var jp = CatJson.JsonParser.Default;
                    var tg1 = typeof(Dictionary<,>);
                    var tg2 = typeof(Dictionary<,>);

                    //var t2 = tg2.MakeGenericType(keyType, typeof(CatJson.JsonObject));
                    var t2 = tg2.MakeGenericType(keyType, dataType);
                    var t1 = tg1.MakeGenericType(typeof(string), t2);

                    if (jp.ParseJson(jsonbin, t1) is IDictionary jobj)
                    {
                        if (jobj["Data"] is IDictionary dict)
                        {
                            object d = null;
                            object keyValue = null;
                            foreach (DictionaryEntry item in dict)
                            {
                                count++;
                                keyValue = item.Key;
                                d = item.Value;
                                var dkey = keyValue;
                                try
                                {
                                    //SetKey(d,keyField, keyValue);
                                    if (!string.IsNullOrEmpty(keyField))
                                    {
                                        dkey = center.GetKey(d, keyField);
                                        if (!Object.Equals(dkey, keyValue))
                                        {
                                            log.Error($"这两个数据的Key为什么不一样: 你要的是 {dataType.Name}.{keyField}={dkey} ，但是 keyValue={keyValue} ??? {xlsFile}[{sheetName}]");
                                        }
                                        onLoad(xlsFile, sheetName, dkey, d);
                                    }
                                    else
                                    {
                                        onLoad(xlsFile, sheetName, keyValue, d);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception($"key={item.Key};value={item.Value}", ex);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载JSON表失败:{xlsFile}[{sheetName}]:{ex.Message}", ex);
            }
            return count;
        }

        private byte[] LoadJsonFileAsBinary(string xlsFile, string sheetName, out string jsonpath)
        {
            jsonpath = Resource.FormatPath(string.Format("{0}/{1}.json", xlsFile, sheetName));
            var ret = Resource.LoadData(jsonpath);

            if (ret == null)
                throw new Exception($"Template File Not Found : {jsonpath}");

            return ret;
        }



        public static ListDictionary<K, V> LoadData<K, V>(string jsonbin)
        {
            var ret = new ListDictionary<K, V>();
            //string jsonpath = null;
            //var jsonbin = LoadJsonFile(xlsFile, sheetName, out jsonpath);
            lock (CatJson.JsonParser.Default)
            {
                var jp = CatJson.JsonParser.Default;
                var tg1 = typeof(Dictionary<,>);
                var tg2 = typeof(Dictionary<,>);
                //var t2 = tg2.MakeGenericType(keyType, typeof(CatJson.JsonObject));
                var t2 = tg2.MakeGenericType(typeof(K), typeof(V));
                var t1 = tg1.MakeGenericType(typeof(string), t2);
                if (jp.ParseJson(jsonbin, t1) is IDictionary jobj)
                {
                    if (jobj["Data"] is IDictionary dict)
                    {
                        object d = null;
                        object keyValue = null;
                        foreach (DictionaryEntry item in dict)
                        {
                            keyValue = item.Key;
                            d = item.Value;
                            ret.Add((K)keyValue, (V)d);
                        }
                    }
                }
            }
            return ret;
        }
        public static string SaveData<K, V>(ListDictionary<K, V> data)
        {
            lock (CatJson.JsonParser.Default)
            {
                return CatJson.JsonParser.Default.ToJson(new FuckCatJson<K, V>() { Data = data });
            }
        }
    }



    public class FuckCatJson<K, V>
    {
        public ListDictionary<K, V> Data = new ListDictionary<K, V>();
    }
}
