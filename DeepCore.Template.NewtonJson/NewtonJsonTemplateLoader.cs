using DeepCore;
using DeepCore.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using static DeepCore.TemplateLoader;

namespace DeepCore.Template.NewtonJson
{
    public class NewtonJsonTemplateLoader : TemplateLoader
    {
        new public static NewtonJsonTemplateLoader Instance { get; private set; }
        public JsonSerializerSettings SerializerSettings { get; set; }
        public override string FILE_SUFFIX => ".json";
        public NewtonJsonTemplateLoader(bool instance) : base(instance)
        {
            if (instance == true)
                Instance = this;
            this.SerializerSettings = new JsonSerializerSettings();
            this.SerializerSettings.Converters.Add(new StructConverter());
        }

        public class StructConverter : JsonConverter
        {
            public override bool CanWrite => false;
            public override bool CanConvert(Type objectType)
            {
                return objectType.IsValueType && !objectType.IsPrimitive && !objectType.IsEnum;
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                //获取JObject对象，该对象对应着我们要反序列化的json
                var jobj = serializer.Deserialize<string>(reader);
                if (Parser.TryStringToObject(jobj, objectType, out var value))
                {
                    return value;
                }
                return existingValue;
            }
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
            }
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
                var tableType = typeof(FuckCatJson<,>).MakeGenericType(keyType, dataType);
                var fuck = (FuckCatJson)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonbin, tableType, SerializerSettings);
                if (fuck != null)
                {
                    object d = null;
                    object keyValue = null;
                    foreach (DictionaryEntry item in fuck.Map)
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
                                    log.Warn($"这两个数据的Key为什么不一样: 你要的是 {dataType.Name}.{keyField}={dkey} ，但是 keyValue={keyValue} ??? {xlsFile}[{sheetName}]");
                                }
                                onLoad(xlsFile, sheetName, dkey, d);
                            }
                            else
                            {
                                if (keyType != null && !keyType.IsInstanceOfType(keyValue))
                                {
                                    keyValue = Convert.ChangeType(keyValue, keyType);
                                }
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


    }


    public abstract class FuckCatJson
    {
        public abstract IDictionary Map { get; }
    }
    public class FuckCatJson<K, V> : FuckCatJson
    {
        public override IDictionary Map => Data;
        public SortedDictionary<K, V> Data = new();
    }
}
