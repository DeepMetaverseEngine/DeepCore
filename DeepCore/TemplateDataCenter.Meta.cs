using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Json;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.XCSV;
using DeepCore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static DeepCore.TemplateLoader;
using static System.Net.WebRequestMethods;

namespace DeepCore
{

    public partial class TemplateDataCenter
    {
        public void ReloadMeta(DataCenterMeta meta)
        {
            ReloadFromMeta(meta);
        }
        protected void ReloadFromMeta(DataCenterMeta meta)
        {
            var stopwatch = Stopwatch.StartNew();
            LoadingThreadID = Thread.CurrentThread.ManagedThreadId;
            try
            {
                foreach (var cache in Tables)
                {
                    try
                    {
                        if (meta.Tables.TryGetValue(cache.Name, out var table))
                        {
                            if (Verbose) log.InfoFormat($"Reload Meta : {cache}");
                            cache.Reload(table);
                        }
                    }
                    catch (Exception err)
                    {
                        if (cache.IgnoreError)
                            log.Error(err);
                        else
                            throw new Exception($"Load Template Error ! >>>{cache}<<< {err.Message}", err);
                    }
                }
                FireEvents(null, meta, templatesList);
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
            finally
            {
                log.Info($"Reload Comlete Use : {stopwatch.Elapsed}");
                stopwatch.Stop();
                LoadingThreadID = 0;
            }
        }
        public virtual DataCenterMeta ToMeta()
        {
            var meta = new DataCenterMeta();
            foreach (var table in this.Tables)
            {
                meta.Tables.Add(table.Name, table.ToMeta());
            }
            return meta;
        }

        public void SaveToMeta(string path)
        {
            var meta = this.ToMeta();
            using (var ms = new DeepCore.IO.MemoryStream())
            {
                using (var output = new OutputStream(ms, new WarpExternalizableFactory(Codec) { IsConsistency = true, UseVLQ = false }))
                {
                    output.PutObj(meta);
                }
                if (path.EndsWith(".gz.bytes"))
                {
                    var bytes = ms.ToArray();
                    var gzbytes = GZipCompress.Compress(bytes);
                    log.Info($"GZip Meta Data : {bytes.Length} -> {gzbytes.Length} {((bytes.Length * 100f) / gzbytes.Length).ToString("F2")}%");
                    bytes = gzbytes;
                    CFiles.WriteAllBytes(path, bytes);
                    return;
                }
                else if (path.EndsWith(".xml"))
                {
                    var xmltext = XmlUtil.ObjectToXml(meta);
                    XmlUtil.SaveXML(path, xmltext);
                    return;
                }
                else
                {
                    var bytes = ms.ToArray();
                    CFiles.WriteAllBytes(path, bytes);
                    return;
                }
            }
        }
        public async Task<DataCenterMeta> LoadMetaAsync(string path)
        {
            if (path.EndsWith(".gz.bytes", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await Resource.LoadDataAsync(path);
                if (bytes != null)
                {
                    bytes = GZipCompress.Decompress(bytes);
                    using (InputStream input = new InputStream(new DeepCore.IO.MemoryStream(bytes), Codec))
                    {
                        var meta = input.GetObj<DataCenterMeta>();
                        if (meta != null)
                        {
                            ReloadMeta(meta);
                        }
                        return meta;
                    }
                }
            }
            else if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await Resource.LoadDataAsync(path);
                if (bytes != null)
                {
                    var xml = XmlUtil.LoadXML(bytes);
                    var meta = XmlUtil.XmlToObject<DataCenterMeta>(xml);
                    ReloadMeta(meta);
                    return meta;
                }
            }
            else
            {
                var bytes = await Resource.LoadDataAsync(path);
                if (bytes != null)
                {
                    using (InputStream input = new InputStream(new DeepCore.IO.MemoryStream(bytes), Codec))
                    {
                        var meta = input.GetObj<DataCenterMeta>();
                        if (meta != null)
                        {
                            ReloadMeta(meta);
                        }
                        return meta;
                    }
                }
            }
            return null;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------

    [MessageType(DeepCoreConstants.DataCenterMeta)]
    public class DataCenterMeta : IExternalizable
    {
        public HashMap<string, TableBaseMeta> Tables = new HashMap<string, TableBaseMeta>();
        public ISerializable AppendData;
        public void ReadExternal(IInputStream input)
        {
            this.Tables = input.GetMap(
                static input => input.GetUTF(),
                static input => input.GetExt<TableBaseMeta>(),
                Tables);
            this.AppendData = input.GetSer();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutMap(Tables,
                static (o, v) => o.PutUTF(v),
                static (o, v) => o.PutExt(v));
            output.PutSer(AppendData);
        }
    }


    [MessageType(DeepCoreConstants.TableBaseMeta)]
    public class TableBaseMeta : IExternalizable
    {
        public string TableName;
        public HashMap<string, TableFileMeta> Files = new HashMap<string, TableFileMeta>();
        public void ReadExternal(IInputStream input)
        {
            this.TableName = input.GetUTF();
            try
            {
                this.Files = input.GetMap(
                    static input => input.GetUTF(),
                    static input => input.GetExt<TableFileMeta>(),
                    Files);
            }
            catch (Exception e)
            {
                throw new Exception($"Load Table Error In Table >>>{TableName}<<<\n{e.Message}", e);
            }
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(this.TableName);
            output.PutMap(Files,
                static (o, v) => o.PutUTF(v),
                static (o, v) => o.PutExt(v));
        }
    }


    [MessageType(DeepCoreConstants.TableFileMeta)]
    public class TableFileMeta : IExternalizable
    {
        public string FileName;
        public HashMap<string, TableSheetMeta> Sheets = new HashMap<string, TableSheetMeta>();
        public void ReadExternal(IInputStream input)
        {
            this.FileName = input.GetUTF();
            try
            {
                this.Sheets = input.GetMap(
                    static input => input.GetUTF(),
                    static input => input.GetExt<TableSheetMeta>(),
                    Sheets);
            }
            catch (Exception e)
            {
                throw new Exception($"Load Table Error In File >>>{FileName}<<<\n{e.Message}", e);
            }
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(this.FileName);
            output.PutMap(Sheets,
                static (o, v) => o.PutUTF(v),
                static (o, v) => o.PutExt(v));
        }
    }

    [MessageType(DeepCoreConstants.TableSheetMeta)]
    public class TableSheetMeta : IExternalizable
    {
        public string SheetName;
        public HashMap<object, object> Datas = new HashMap<object, object>();
        public void ReadExternal(IInputStream input)
        {
            this.SheetName = input.GetUTF();
            try
            {
                this.Datas = input.GetMap(
                        static input => input.GetRawData(),
                        static input => input.GetRawData(),
                        Datas);
            }
            catch (Exception e)
            {
                throw new Exception($"Load Table Error In Sheet >>>{SheetName}<<<\n{e.Message}", e);
            }

        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(this.SheetName);
            output.PutMap(Datas,
                static (o, v) => o.PutRawData(v),
                static (o, v) => o.PutRawData(v));
        }
    }
}