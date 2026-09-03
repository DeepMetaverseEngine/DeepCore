using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.XCSV;
using DeepCore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore
{
    public class BinaryTemplateLoader : TemplateLoader
    {
        new public static BinaryTemplateLoader Instance { get; private set; }
        public IExternalizableFactory Codec { get; }
        public override string FILE_SUFFIX { get; }
        public BinaryTemplateLoader(IExternalizableFactory facoty, TemplateLoader baseLoader, bool instance) : base(instance)
        {
            this.Codec = facoty;
            this.FILE_SUFFIX = $"{baseLoader.FILE_SUFFIX}.bin";
            if (instance)
            {
                BinaryTemplateLoader.Instance = this;
            }
            this.log.Color = ConsoleColor.Magenta;
        }
        public override void GC()
        {
        }
        protected override void LoadTemplatesImpl(TemplateDataCenter center, string fileName, string[] sheets, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData load)
        {
            TryLoadTemplates(center, fileName, sheets, dataType, keyField, keyType, createNew, load);
        }
        protected override Task LoadTemplatesImplAsync(TemplateDataCenter center, string fileName, string[] sheets, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData load)
        {
            return TryLoadTemplatesAsync(center, fileName, sheets, dataType, keyField, keyType, createNew, load);
        }

        public bool TryLoadTemplates(TemplateDataCenter center, string fileName, string[] sheets, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData load)
        {
            if ((typeof(ISerializable)).IsAssignableFrom(dataType))
            {
                if (sheets == null || sheets.Length == 0)//xleFile目录下按sheetname分了多个文件，没有指定就是读取所有
                {
                    if (Resource.TryGetLoaderWithPath(fileName, out var fullpath, out var loader))
                    {
                        int suffixCount = 0;
                        foreach (var sheetFile in loader.ListFiles(fullpath))
                        {
                            if (sheetFile.EndsWith(FILE_SUFFIX, CUtils.StringComparisonIgnoreCase))
                            {
                                var sheetName = sheetFile.Substring(0, sheetFile.Length - FILE_SUFFIX.Length);
                                var filepath = $"{fullpath}/{sheetFile}";
                                LoadFromBin(dataType, filepath, (this, fullpath, sheetName, dataType, keyField, keyType, load, center), createNew, static (st, index, data) =>
                                {
                                    var dc = st.Item1;
                                    st.load.Invoke(st.fullpath, st.sheetName, st.center.GetKey(data, st.keyField) ?? index, data);
                                });
                                suffixCount++;
                            }
                        }
                        return suffixCount > 0;
                    }
                    return false;
                }
                else
                {
                    foreach (var sheetName in sheets)
                    {
                        var filepath = $"{fileName}/{sheetName}{FILE_SUFFIX}";
                        if (Resource.TryGetLoaderWithPath(filepath, out var fullpath, out var loader))
                        {
                            LoadFromBin(dataType, fullpath, (this, fullpath, sheetName, dataType, keyField, keyType, load, center), createNew, static (st, index, data) =>
                            {
                                var dc = st.Item1;
                                st.load.Invoke(st.fullpath, st.sheetName, st.center.GetKey(data, st.keyField) ?? index, data);
                            });
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        public async Task<bool> TryLoadTemplatesAsync(TemplateDataCenter center, string fileName, string[] sheets, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData load)
        {
            if ((typeof(ISerializable)).IsAssignableFrom(dataType))
            {
                if (sheets == null || sheets.Length == 0)//xleFile目录下按sheetname分了多个文件，没有指定就是读取所有
                {
                    if (Resource.TryGetLoaderWithPath(fileName, out var fullpath, out var loader))
                    {
                        int suffixCount = 0;
                        foreach (var sheetFile in await loader.ListFilesAsync(fullpath))
                        {
                            if (sheetFile.EndsWith(FILE_SUFFIX, CUtils.StringComparisonIgnoreCase))
                            {
                                var sheetName = sheetFile.Substring(0, sheetFile.Length - FILE_SUFFIX.Length);
                                var filepath = $"{fullpath}/{sheetFile}";
                                await LoadFromBinAsync(dataType, filepath, (this, fullpath, sheetName, dataType, keyField, keyType, load, center), createNew, static (st, index, data) =>
                                {
                                    var dc = st.Item1;
                                    st.load.Invoke(st.fullpath, st.sheetName, st.center.GetKey(data, st.keyField) ?? index, data);
                                    return Task.CompletedTask;
                                });
                                suffixCount++;
                            }
                        }
                        return suffixCount > 0;
                    }
                    return false;
                }
                else
                {
                    foreach (var sheetName in sheets)
                    {
                        var filepath = $"{fileName}/{sheetName}{FILE_SUFFIX}";
                        if (Resource.TryGetLoaderWithPath(filepath, out var fullpath, out var loader))
                        {
                            await LoadFromBinAsync(dataType, fullpath, (this, fileName, sheetName, dataType, keyField, keyType, load, center), createNew, static (st, index, data) =>
                            {
                                var dc = st.Item1;
                                st.load.Invoke(st.fileName, st.sheetName, st.center.GetKey(data, st.keyField) ?? index, data);
                                return Task.CompletedTask;
                            });
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void LoadFromBin<ST>(Type dataType, string filepath, ST st, Func<Type, object> createNew, Action<ST, int, object> onload)
        {
            if (EnableLog) log.Info("LoadFromBin : " + filepath);
            Codec.ReadAllBytes(filepath, (this, st, dataType, createNew, onload), static (st, input) =>
            {
                var dc = st.Item1;
                var codec = dc.Codec.GetCodec(st.dataType);
                int count = input.GetS32();
                for (int i = 0; i < count; i++)
                {
                    var exist = input.GetBool();
                    if (exist)
                    {
                        var data = input.Decode(codec, (ISerializable)st.createNew(st.dataType));
                        st.onload.Invoke(st.st, i, data);
                    }
                }
            });
        }
        public Task LoadFromBinAsync<ST>(Type dataType, string filepath, ST st, Func<Type, object> createNew, Func<ST, int, object, Task> onload)
        {
            if (EnableLog) log.Info("LoadFromBin : " + filepath);
            return Codec.ReadAllBytesAsync(filepath, (this, st, dataType, createNew, onload), static async (st, input) =>
            {
                var dc = st.Item1;
                var codec = dc.Codec.GetCodec(st.dataType);
                int count = input.GetS32();
                for (int i = 0; i < count; i++)
                {
                    var exist = input.GetBool();
                    if (exist)
                    {
                        var data = input.Decode(codec, (ISerializable)st.createNew(st.dataType));
                        await st.onload.Invoke(st.st, i, data);
                    }
                }
            });
        }
        public void LoadFromBin<ST, T>(string filepath, ST st, Action<ST, T> onload) where T : ISerializable, new()
        {
            LoadFromBin(typeof(T), filepath, (st, onload), static t => new T(), static (st, index, d) => { st.onload(st.st, (T)d); });
        }
        public async Task LoadFromBinAsync<ST, T>(string filepath, ST st, Func<ST, T, Task> onload) where T : ISerializable, new()
        {
            await LoadFromBinAsync(typeof(T), filepath, (st, onload), static t => new T(), static (st, index, d) => { return st.onload(st.st, (T)d); });
        }
        public List<T> LoadFromBin<T>(string filepath) where T : ISerializable, new()
        {
            var ret = new List<T>();
            LoadFromBin(typeof(T), filepath, (ret), static t => new T(), static (st, index, d) => { st.Add((T)d); });
            return ret;
        }
        public async Task<List<T>> LoadFromBinAsync<T>(string filepath) where T : ISerializable, new()
        {
            var ret = new List<T>();
            await LoadFromBinAsync(typeof(T), filepath, (ret), static t => new T(), static (st, index, d) => { st.Add((T)d); return Task.CompletedTask; });
            return ret;
        }


        public void SaveToBin(IEnumerable<TableBase> tables, IRangeValue progress = null)
        {
            foreach (var table in tables)
            {
                if (!(typeof(ISerializable)).IsAssignableFrom(table.DataType))
                {
                    throw new Exception($">>>{table.DataType}<<< Not A ISerializable : {table}");
                }
                if (table.DataCount <= 0)
                {
                    log.Warn($"Table Is Empty : {table}");
                    continue;
                }
                var codec = Codec.GetCodec(table.DataType);
                //table.SaveToBinary($"{GameEditor.EditorRootDir}/templates/bin/{table.Name}", ".bin");
                table.ForEachSheets(this, (st, file, sheet, datas) =>
                {
                    var filepath = $"{file}/{sheet}{FILE_SUFFIX}";
                    SaveToBin(table.DataType, filepath, datas);
                });
                progress?.Add(1);
            }
        }

        public void SaveToBin(Type dataType, string filepath, ICollection datas)
        {
            log.Info("SaveToBin : " + filepath);
            var codec = Codec.GetCodec(dataType);
            Codec.WriteAllBytes(filepath, this, (dc, os) =>
            {
                os.PutS32(datas.Count);
                foreach (ISerializable data in datas)
                {
                    os.PutBool(data != null);
                    if (data != null)
                    {
                        os.Encode(codec, data);
                        {
                            var sxml = XmlUtil.ObjectToXmlString(data);
                            var dst = IOUtil.Clone(Codec, data);
                            var dxml = XmlUtil.ObjectToXmlString(dst);
                            if (sxml != dxml)
                            {
                                throw new Exception($"这两个数据序列化反序列化格式不一致: {dataType.FullName}");
                            }
                        }
                    }
                }
            });
        }

    }
}