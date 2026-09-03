using DeepCore;
using DeepCore.Astar;
using DeepCore.Concurrent;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Xml;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.SceneGraph;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DeepMetaGame.Data.ZoneEditor
{
    public class EditorTemplates : Disposable
    {
        public static bool RUNTIME_IN_SERVER = false;
        public const string ListFileExt = "/dir.list";

        public static readonly Encoding UTF8 = new UTF8Encoding(false);
        private static readonly Logger log = new LazyLogger("EditorTemplates");
        public static bool ENABLE_SHOW_LOAD_PROGRESS = true;
        public static bool ENABLE_GZIP_META = true;
        public static string META_SUFFIX => (ENABLE_GZIP_META ? "/data.gz.bytes" : "/data.bytes");

        private readonly string dataDir;
        private readonly IExternalizableFactory codec;
        private readonly ZoneDataFactory factory;
        private readonly TemplateManager templates;
        private readonly bool is_client_mode;

        public bool Verbose = false;
        public bool UseMMP = true;

        public static EditorTemplates Instance { get; private set; }
        public ZoneDataFactory DataFactory => factory;
        public bool IsClientData { get { return is_client_mode; } }
        public string DataRoot { get { return dataDir; } }
        public string EditorRoot { get { return dataDir + "/.."; } }
        public TemplateManager Templates { get { return templates; } }
        public EditorDataCenter DataCenter { get; private set; }
        public EditorTemplatesData AllTemplatesExcludeScenes { get { return new EditorTemplatesData(templates, new List<SceneData>()); } }
        public bool IsLoaded { get; private set; }
        protected internal EditorTemplates(ZoneDataFactory factory, string data_dir, bool client_mode = false)
        {
            EditorTemplates.Instance = this;
            this.dataDir = data_dir;
            this.factory = factory;
            this.codec = factory.PersistCodec;
            this.is_client_mode = client_mode;
            this.templates = factory.CreateTemplateManager(this);
            this.DataCenter = factory.CreateDataCenter(this);
        }
        protected override void Disposing()
        {
            scenes_locker.Dispose();
            DataCenter?.Dispose();
        }

        public void Flush(bool force)
        {
            using (scenes_locker.EnterWait())
            {
                scenes.Clear();
            }
            templates.Flush(force);
        }

        /// <summary>
        /// 判断当前模板是否加载
        /// </summary>
        /// <param name="type"></param>
        /// <param name="templateID"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public bool IsTemplateLoaded(Type type, int templateID, out TemplateData template)
        {
            return templates.IsTemplateLoaded(type, templateID, out template);
        }
        //-------------------------------------------------------------------------------------------------------------------
        #region Load ALL

        public delegate void OnLoadAction(EditorTemplates editorRoot, IRangeValue progress = null);
        public event OnLoadAction OnLoad;
        private void FireEvents(IRangeValue progress = null)
        {
            try
            {
                this.DataCenter.OnEditorTemplatesLoad(this, progress);
                OnLoad?.Invoke(this, progress);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }

        /// <summary>
        /// 重新读取所有模板
        /// </summary>
        public virtual void LoadAllTemplates(bool force = false, IRangeValue progress = null)
        {
            var stopwatch1 = Stopwatch.StartNew();
            try
            {
                ListIndex list = default;
                log.Info($"------------------------------------------------------------------");
                log.Info($"TemplateDataCenter.ENABLE_LOAD_FROM_BIN : {TemplateDataCenter.ENABLE_LOAD_FROM_BIN}");
                {
                    var stopwatch = Stopwatch.StartNew();
                    factory.BeginPluginsData(this, progress).Wait();
                    log.Info($"BeginPluginsData Use : {stopwatch.Elapsed}");
                }
                {
                    var stopwatch = Stopwatch.StartNew();
                    LoadAllConfig();
                    log.Info($"LoadAllConfig Use : {stopwatch.Elapsed}");
                }
                {
                    var stopwatch = Stopwatch.StartNew();
                    DataCenter.Cleanup();
                    list = LoadAllList();
                    //                     if (progress != null)
                    //                     {
                    //                         progress?.SetRange(0, progress.Max + DataCenter.TablesCount + list.TotalCount, 0);
                    //                     }
                    log.Info($"LoadAllList Use : {stopwatch.Elapsed}");
                }
                {
                    var stopwatch = Stopwatch.StartNew();
                    DataCenter.ReloadAll(progress);
                    log.Info($"DataCenter ReloadAll Use : {stopwatch.Elapsed}");
                }
                if (!is_client_mode || force)
                {
                    var stopwatch = Stopwatch.StartNew();
                    LoadAllTemplateData(list, progress);
                    log.Info($"LoadAllTemplateData Use : {stopwatch.Elapsed}");
                }
                else
                {
                    progress?.Add(list.TotalCount);
                }
                {
                    var stopwatch = Stopwatch.StartNew();
                    factory.InitPluginsData(this, progress).Wait();
                    log.Info($"InitPluginsData Use : {stopwatch.Elapsed}");
                }
                //                 int sncount = templates.RehashAll();
                //                 if (Verbose) log.Info("RehashAll : " + sncount);
                IsLoaded = true;
                FireEvents();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new Exception(err.Message, err);
                // throw;
            }
            finally
            {
                stopwatch1.Stop();
                log.Info($"LoadAllTemplates Total Use : {stopwatch1.Elapsed}");
                log.Info($"------------------------------------------------------------------");
            }
        }

        /// <summary>
        /// 重新读取所有模板
        /// </summary>
        public virtual async Task LoadAllTemplatesAsync(bool force = false, IRangeValue progress = null)
        {
            var stopwatch1 = Stopwatch.StartNew();
            try
            {
                ListIndex list = default;
                log.Info($"------------------------------------------------------------------");
                log.Info($"TemplateDataCenter.ENABLE_LOAD_FROM_BIN : {TemplateDataCenter.ENABLE_LOAD_FROM_BIN}");
                {
                    var stopwatch = Stopwatch.StartNew();
                    await factory.BeginPluginsData(this, progress);
                    log.Info($"BeginPluginsData Use : {stopwatch.Elapsed}");
                    stopwatch.Reset();
                }
                {
                    var stopwatch = Stopwatch.StartNew();
                    await LoadAllConfigAsync();
                    log.Info($"LoadAllConfigAsync Use : {stopwatch.Elapsed}");
                    stopwatch.Reset();
                }
                {
                    DataCenter.Cleanup();
                    var stopwatch = Stopwatch.StartNew();
                    list = await LoadAllListAsync();
                    //                     if (progress != null)
                    //                     {
                    //                         progress.SetMax(progress.Max + DataCenter.TablesCount + list.TotalCount);
                    //                     }
                    log.Info($"LoadAllListAsync Use : {stopwatch.Elapsed}");
                    stopwatch.Reset();
                }
                {
                    var stopwatch = Stopwatch.StartNew();
                    await DataCenter.ReloadAllAsync(progress);
                    log.Info($"DataCenter ReloadAllAsync Use : {stopwatch.Elapsed}");
                    stopwatch.Reset();
                }
                if (!is_client_mode || force)
                {
                    var stopwatch = Stopwatch.StartNew();
                    await LoadAllTemplateDataAsync(list, progress);
                    log.Info($"LoadAllTemplateDataAsync Use : {stopwatch.Elapsed}");
                    stopwatch.Reset();
                }
                else
                {
                    progress?.Add(list.TotalCount);
                }
                {
                    var stopwatch = Stopwatch.StartNew();
                    await factory.InitPluginsData(this, progress);
                    log.Info($"InitPluginsData Use : {stopwatch.Elapsed}");
                    stopwatch.Reset();
                }
                //                 int sncount = templates.RehashAll();
                //                 if (Verbose) log.Info("RehashAll : " + sncount);
                IsLoaded = true;
                FireEvents();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new Exception(err.Message, err);
            }
            finally
            {
                stopwatch1.Stop();
                log.Info($"LoadAllTemplatesAsync Total Use : {stopwatch1.Elapsed}");
                log.Info($"------------------------------------------------------------------");
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------
        #region META

        /// <summary>
        /// 重新读取所有模板
        /// </summary>
        public virtual void LoadAllTemplatesMeta(EditorTemplatesMeta meta)
        {
            var stopwatch1 = Stopwatch.StartNew();
            try
            {
                log.Info($"------------------------------------------------------------------");
                log.Info($"TemplateDataCenter.ENABLE_LOAD_FROM_BIN : {TemplateDataCenter.ENABLE_LOAD_FROM_BIN}");
                {
                    factory.BeginPluginsData(this, null).Wait();
                }
                {
                    templates.DefaultConfig = meta.DefaultConfig;
                    templates.DefaultExtConfig = meta.DefaultExtCFG;
                    templates.GlobalConfig = meta.GlobalCFG;
                    templates.ResourceVersion = meta.ResourceVersion;
                    templates.DefaultTerrainDefinition = meta.DefaultTerrainDefinitions;
                    templates.DefaultUnitActionDefinition = meta.DefaultUnitActionDefinitions;
                    templates.ResourcePropertiesMap = meta.ResourcePropertiesMap;
                    templates.CardAffects = meta.CardAffects;
                }
                {
                    DataCenter.Cleanup();
                }
                {
                    DataCenter.ReloadMeta(meta.BattleDataCenter);
                }
                {
                    meta.ForEachTemplatesData(this, (st, t) =>
                    {
                        t.IsOriginal = true;
                    });
                    templates.ReloadMeta(meta);
                }
                {
                    //scenes.PutAll(meta.Scenes);
                }
                {
                    factory.InitPluginsData(this, null).Wait();
                }
                IsLoaded = true;
                FireEvents();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new Exception(err.Message, err);
            }
            finally
            {
                stopwatch1.Stop();
                log.Info($"LoadAllTemplatesMeta Total Use : {stopwatch1.Elapsed}");
                log.Info($"------------------------------------------------------------------");
            }
        }

        /// <summary>
        /// 重新读取所有模板
        /// </summary>
        public void LoadAllTemplatesMeta()
        {
            var bytes = Resource.LoadData(dataDir + META_SUFFIX);
            LoadAllTemplatesMetaFromBin(bytes);
        }

        /// <summary>
        /// 重新读取所有模板
        /// </summary>
        public async Task LoadAllTemplatesMetaAsync(IRangeValue p = null)
        {
            var bytes = await Resource.LoadDataAsync(dataDir + META_SUFFIX);
            LoadAllTemplatesMetaFromBin(bytes);
        }

        /// <summary>
        /// 重新读取所有模板
        /// </summary>
        public void LoadAllTemplatesMetaFromBin(byte[] bytes)
        {
            var meta = LoadMeta(bytes);
            LoadAllTemplatesMeta(meta);
        }

        public EditorTemplatesMeta ToMeta()
        {
            var meta = new EditorTemplatesMeta();
            meta.DefaultConfig = templates.DefaultConfig;
            meta.DefaultExtCFG = templates.DefaultExtConfig;
            meta.GlobalCFG = templates.GlobalConfig;
            meta.ResourceVersion = templates.ResourceVersion;
            meta.DefaultTerrainDefinitions = templates.DefaultTerrainDefinition;
            meta.DefaultUnitActionDefinitions = templates.DefaultUnitActionDefinition;
            meta.ResourcePropertiesMap = templates.ResourcePropertiesMap;
            meta.CardAffects = templates.CardAffects;
            meta.Units.PutAllToMap(templates.AllUnits);
            meta.Skills.PutAllToMap(templates.AllSkills);
            meta.Spells.PutAllToMap(templates.AllSpells);
            meta.Buffs.PutAllToMap(templates.AllBuffs);
            meta.Auras.PutAllToMap(templates.AllAuras);
            meta.Items.PutAllToMap(templates.AllItems);
            meta.UnitEvents.PutAllToMap(templates.AllUnitEvents);
            meta.Cards.PutAllToMap(templates.AllCards);
            meta.BattleUIs.PutAllToMap(templates.AllBattleUI);
            //meta.Scenes.PutAll(scenes);
            meta.BattleDataCenter = DataCenter.ToMeta();
            return meta;
        }

        public virtual byte[] SaveToMeta()
        {
            var meta = this.ToMeta();
            var bytes = SaveDataToBin(meta, new WarpExternalizableFactory(codec) { IsConsistency = true, UseVLQ = false });
            {
                var fileBin = new FileInfo(dataDir + "/data.bytes");
                CFiles.WriteAllBytes(fileBin.FullName, bytes);
                var fileXml = new FileInfo(dataDir + "/data.bytes.xml");
                var xmltext = XmlUtil.ObjectToXml(meta);
                XmlUtil.SaveXML(fileXml.FullName, xmltext);
            }
            if (ENABLE_GZIP_META)
            {
                bytes = GZipCompress.Compress(bytes);
                var file = new FileInfo(dataDir + META_SUFFIX);
                CFiles.WriteAllBytes(file.FullName, bytes);
            }
            return bytes;
        }
        public virtual EditorTemplatesMeta LoadMeta(byte[] bytes)
        {
            if (ENABLE_GZIP_META)
            {
                bytes = GZipCompress.Decompress(bytes);
            }
            var meta = LoadDataFromBin<EditorTemplatesMeta>(bytes, codec);
            return meta;
        }


        #endregion

        //-------------------------------------------------------------------------------------------------------------------
        #region Load Single Template
        public T LoadTemplate<T>(int templateID) where T : TemplateData
        {
            if (templateID == 0)
            {
                return null;
            }
            var data = LoadTemplateData(templateID, typeof(T));
            if (data is T t)
            {
                return t;
            }
            return null;
        }
        public async Task<T> LoadTemplateAsync<T>(int templateID) where T : TemplateData
        {
            if (templateID == 0)
            {
                return null;
            }
            var data = await LoadTemplateDataAsync(templateID, typeof(T));
            if (data is T t)
            {
                return t;
            }
            return null;
        }
        public bool TryLoadTemplateData(int templateID, Type templateType, out TemplateData data)
        {
            data = LoadTemplateData(templateID, templateType);
            return data != null;
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Scenes
        private readonly SemaphoreSlim scenes_locker = new SemaphoreSlim(1, 1);
        private readonly HashMap<int, SceneData> scenes = new HashMap<int, SceneData>();
        public IReadOnlyDictionary<int, SceneData> CacheScenes
        {
            get
            {
                using (scenes_locker.EnterWait())
                {
                    return new HashMap<int, SceneData>(scenes);
                }
            }
        }
        public int CacheScenesCount
        {
            get
            {
                using (scenes_locker.EnterWait())
                {
                    return scenes.Count;
                }
            }
        }

        /// <summary>
        /// 根据ID加载场景
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cache">是否缓存起来（占用内存，一般服务端用）</param>
        /// <param name="client_data">是否是客户端使用</param>
        /// <param name="clone">是否克隆(仅cache)</param>
        /// <returns></returns>
        public SceneData LoadScene(int id, bool cache = true, bool client_data = false, bool clone = true)
        {
            if (client_data)
            {
                cache = false;
            }
            var ret = default(SceneData);
            using (scenes_locker.EnterWait())
            {
                if (scenes.TryGetValue(id, out ret))
                {
                }
                else
                {
                    ret = LoadSceneData(dataDir + "/scenes/" + id + ".xml", client_data);
                    if (cache)
                    {
                        scenes.Put(id, ret);
                    }
                }
            }
            if (ret != null)
            {
                if (clone)
                {
                    ret = IOUtil.CloneObject(codec, ret);
                }
            }
            return ret;
        }
        public async Task<SceneData> LoadSceneAsync(int id, bool cache = true, bool client_data = false, bool clone = true)
        {
            if (client_data)
            {
                cache = false;
            }
            var ret = default(SceneData);
            using (await scenes_locker.EnterWaitAsync())
            {
                if (scenes.TryGetValue(id, out ret))
                {
                }
                else
                {
                    ret = await LoadSceneDataAsync(dataDir + "/scenes/" + id + ".xml", client_data);
                    if (cache)
                    {
                        scenes.Put(id, ret);
                    }
                }
            }
            if (ret != null)
            {
                if (clone)
                {
                    ret = IOUtil.CloneObject(codec, ret);
                }
            }
            return ret;
        }
        /// <summary>
        /// 是否缓存场景
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ExistSceneInCache(int id)
        {
            using (scenes_locker.EnterWait())
            {
                return scenes.ContainsKey(id);
            }
        }

        /// <summary>
        /// 缓存所有场景数据，一般服务器用。消耗内存较大
        /// </summary>
        public HashMap<int, SceneData> CacheAllScenes()
        {
            var ret = new HashMap<int, SceneData>();
            if (CacheScenesCount == 0)
            {
                foreach (int scene_id in ListScenes())
                {
                    var sd = LoadScene(scene_id, true, false, false);
                    ret.Add(sd.ID, sd);
                }
                return ret;
            }
            else
            {
                using (scenes_locker.EnterWait())
                {
                    ret.PutAll(scenes);
                }
            }
            return ret;
        }



        public List<SceneData> LoadAllScenes()
        {
            var ret = new List<SceneData>();
            foreach (var id in ListScenes())
            {
                ret.Add(LoadScene(id, true, false, false));
            }
            return ret;
        }
        public async Task<List<SceneData>> LoadAllScenesAsync()
        {
            var ret = new List<SceneData>();
            foreach (var id in await ListScenesAsync())
            {
                ret.Add(await LoadSceneAsync(id, true, false, false));
            }
            return ret;
        }

        public List<int> ListScenes()
        {
            List<int> ret = new List<int>();
            foreach (string file in LoadList("/scenes"))
            {
                try
                {
                    string sub = file.Replace('\\', '/');
                    int begin = sub.LastIndexOf('/') + 1;
                    int end = sub.LastIndexOf('.');
                    sub = sub.Substring(begin, end - begin);
                    ret.Add(Parser.ParseInt(sub));
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                    throw err;
                }
            }
            return ret;
        }
        public async Task<List<int>> ListScenesAsync()
        {
            List<int> ret = new List<int>();
            foreach (string file in await LoadListAsync("/scenes"))
            {
                try
                {
                    string sub = file.Replace('\\', '/');
                    int begin = sub.LastIndexOf('/') + 1;
                    int end = sub.LastIndexOf('.');
                    sub = sub.Substring(begin, end - begin);
                    ret.Add(Parser.ParseInt(sub));
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                    throw err;
                }
            }
            return ret;
        }

        public SceneGraphData LoadSceneGraphData()
        {
            var xml = XmlUtil.LoadXML($"{dataDir}/scene_graph.xml");
            if (xml != null)
            {
                return XmlUtil.XmlToObject<SceneGraphData>(xml);
            }
            return null;
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------


        //---------------------------------------------------------------------------------------------------
        #region Loader
        public virtual byte[] LoadDataFromDataRoot(string subfile)
        {
            return Resource.LoadData($"{dataDir}/{subfile}");
        }
        public virtual Task<byte[]> LoadDataFromDataRootAsync(string subfile)
        {
            return Resource.LoadDataAsync($"{dataDir}/{subfile}");
        }
        public virtual string LoadTextFromDataRoot(string subfile)
        {
            return Resource.LoadAllText($"{dataDir}/{subfile}");
        }
        public virtual Task<string> LoadTextFromDataRootAsync(string subfile)
        {
            return Resource.LoadAllTextAsync($"{dataDir}/{subfile}");
        }
        public virtual T LoadXMLFromDataRoot<T>(string subfile)
        {
            var xml = XmlUtil.LoadXML($"{dataDir}/{subfile}");
            if (xml != null)
            {
                return XmlUtil.XmlToObject<T>(xml);
            }
            return default(T);
        }
        public virtual async Task<T> LoadXMLFromDataRootAsync<T>(string subfile)
        {
            var xml = await XmlUtil.LoadXMLAsync($"{dataDir}/{subfile}");
            if (xml != null)
            {
                return XmlUtil.XmlToObject<T>(xml);
            }
            return default(T);
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <returns></returns>
        protected void LoadCFG()
        {
            templates.DefaultConfig = LoadXmlAs(dataDir + "/config.xml", new Config());
            templates.DefaultExtConfig = LoadXmlAs(dataDir + "/config_ext.xml", factory.CreateCommonCFG());
            templates.GlobalConfig = LoadXmlAs(dataDir + "/config_global.xml", factory.CreateGlobalCFG());
            templates.ResourcePropertiesMap = LoadXmlAs(dataDir + "/resource_properties.xml", new ResourcePropertiesMap());
            templates.CardAffects = LoadXmlAs(dataDir + "/card_affects.xml", new CardAffectBindingTemplates());
        }
        protected async Task LoadCFGAsync()
        {
            templates.DefaultConfig = await LoadXmlAsAsync(dataDir + "/config.xml", new Config());
            templates.DefaultExtConfig = await LoadXmlAsAsync(dataDir + "/config_ext.xml", factory.CreateCommonCFG());
            templates.GlobalConfig = await LoadXmlAsAsync(dataDir + "/config_global.xml", factory.CreateGlobalCFG());
            templates.ResourcePropertiesMap = await LoadXmlAsAsync(dataDir + "/resource_properties.xml", new ResourcePropertiesMap());
            templates.CardAffects = await LoadXmlAsAsync(dataDir + "/card_affects.xml", new CardAffectBindingTemplates());
        }
        protected string LoadResVersion()
        {
            //             var res = Resource.LoadAllText(dir + "/res.list");
            //             if (res != null)
            //             {
            //                 templates.ResourcesList = res.Split('\n');
            //             }
            var md5 = Resource.LoadAllText(dataDir + "/ver.md5");
            if (md5 != null)
            {
                var lines = md5.Split('\n');
                templates.ResourceVersion = lines[0].Trim();
            }
            return md5;
        }
        protected async Task<string> LoadResVersionAsync()
        {
            //             var res = await Resource.LoadAllTextAsync(dir + "/res.list");
            //             if (res != null)
            //             {
            //                 templates.ResourcesList = res.Split('\n');
            //             }
            var md5 = await Resource.LoadAllTextAsync(dataDir + "/ver.md5");
            if (md5 != null)
            {
                var md5_lines = md5.Split('\n');
                templates.ResourceVersion = md5_lines[0].Trim();
            }
            return md5;
        }

        protected void LoadTerrainDefinitionMap()
        {
            var td = LoadXmlAs<TerrainDefinitionMap>(dataDir + "/terrain_definition.xml");
            if (td != null)
            {
                templates.DefaultTerrainDefinition = td;
            }
        }
        protected async Task LoadTerrainDefinitionMapAsync()
        {
            var td = await LoadXmlAsAsync<TerrainDefinitionMap>(dataDir + "/terrain_definition.xml");
            if (td != null)
            {
                templates.DefaultTerrainDefinition = td;
            }
        }
        protected void LoadUnitActionDefinitionMap()
        {
            var td = LoadXmlAs<UnitActionDefinitionMap>(dataDir + "/unit_action_definition.xml");
            if (td != null)
            {
                templates.DefaultUnitActionDefinition = td;
            }
        }
        protected async Task LoadUnitActionDefinitionMapAsync()
        {
            var td = await LoadXmlAsAsync<UnitActionDefinitionMap>(dataDir + "/unit_action_definition.xml");
            if (td != null)
            {
                templates.DefaultUnitActionDefinition = td;
            }
        }



        protected string[] LoadList(string subdir)
        {
            string base_dir = dataDir + subdir;
            List<string> ret = new List<string>();
            string listtxt = Resource.LoadAllText(base_dir + ListFileExt);
            if (listtxt != null)
            {
                foreach (string line in listtxt.Split('\n'))
                {
                    string[] lv = line.Split(';');
                    if (lv.Length > 1)
                    {
                        ret.Add(base_dir + "/" + lv[lv.Length - 1].Trim() + ".xml");
                    }
                }
            }
            return ret.ToArray();
        }
        protected async Task<string[]> LoadListAsync(string subdir)
        {
            var base_dir = dataDir + subdir;
            var ret = new List<string>();
            var listtxt = await Resource.LoadAllTextAsync(base_dir + ListFileExt);
            foreach (string line in listtxt.Split('\n'))
            {
                string[] lv = line.Split(';');
                if (lv.Length > 1)
                {
                    ret.Add(base_dir + "/" + lv[lv.Length - 1].Trim() + ".xml");
                }
            }
            return ret.ToArray();
        }
        public struct ListIndex
        {
            public string[] units;
            public string[] skills;
            public string[] spells;
            public string[] buffs;
            public string[] auras;
            public string[] items;
            public string[] cards;
            public string[] unit_events;
            public string[] guis;
            public int TotalCount
            {
                get
                {
                    int ret = units.Length + skills.Length + spells.Length + buffs.Length + auras.Length + items.Length + cards.Length + guis.Length;
                    if (unit_events != null) { ret += unit_events.Length; }
                    return ret;
                }
            }
        }

        protected ListIndex LoadAllList()
        {
            return new ListIndex()
            {
                units = LoadList("/units"),
                skills = LoadList("/skills"),
                spells = LoadList("/spells"),
                buffs = LoadList("/buffs"),
                auras = LoadList("/auras"),
                items = LoadList("/items"),
                cards = LoadList("/cards"),
                unit_events = (!IsClientData) ? LoadList("/unit_events") : null,
                guis = LoadList("/guis"),
            };
        }
        protected async Task<ListIndex> LoadAllListAsync()
        {
            return new ListIndex()
            {
                units = await LoadListAsync("/units"),
                skills = await LoadListAsync("/skills"),
                spells = await LoadListAsync("/spells"),
                buffs = await LoadListAsync("/buffs"),
                auras = await LoadListAsync("/auras"),
                items = await LoadListAsync("/items"),
                cards = await LoadListAsync("/cards"),
                unit_events = (!IsClientData) ? await LoadListAsync("/unit_events") : null,
                guis = await LoadListAsync("/guis"),
            };
        }

        protected async Task LoadAllConfigAsync()
        {
            await LoadCFGAsync();
            await LoadResVersionAsync();
            await LoadTerrainDefinitionMapAsync();
            await LoadUnitActionDefinitionMapAsync();
        }
        protected void LoadAllConfig()
        {
            LoadCFG();
            LoadResVersion();
            LoadTerrainDefinitionMap();
            LoadUnitActionDefinitionMap();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------



        protected void LoadAllTemplateData(ListIndex list, IRangeValue progress)
        {
            if (progress != null)
            {
                progress?.SetRange(0, list.TotalCount, 0);
            }
            if (TemplateDataCenter.ENABLE_BATCH_LOAD)
            {
                var t1 = Task.Run(() =>
                {
                    foreach (string file in list.units)
                    {
                        LoadTemplateData<UnitInfo>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t2 = Task.Run(() =>
                {
                    foreach (string file in list.skills)
                    {
                        LoadTemplateData<SkillTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t3 = Task.Run(() =>
                {
                    foreach (string file in list.spells)
                    {
                        LoadTemplateData<SpellTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t4 = Task.Run(() =>
                {
                    foreach (string file in list.buffs)
                    {
                        LoadTemplateData<BuffTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t5 = Task.Run(() =>
                {
                    foreach (string file in list.auras)
                    {
                        LoadTemplateData<AuraTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t6 = Task.Run(() =>
                {
                    foreach (string file in list.items)
                    {
                        LoadTemplateData<ItemTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t7 = Task.Run(() =>
                {
                    foreach (string file in list.cards)
                    {
                        LoadTemplateData<CardTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t8 = Task.Run(() =>
                {
                    if (list.unit_events != null)
                    {
                        foreach (string file in list.unit_events)
                        {
                            LoadTemplateData<UnitEventTemplate>(file);
                            progress?.SetText(Path.GetFileName(file)).Add(1);
                        }
                    }
                });
                var t9 = Task.Run(() =>
                {
                    foreach (string file in list.guis)
                    {
                        LoadTemplateData<BattleUITemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9).Wait();
            }
            else
            {
                foreach (string file in list.units)
                {
                    LoadTemplateData<UnitInfo>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.skills)
                {
                    LoadTemplateData<SkillTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.spells)
                {
                    LoadTemplateData<SpellTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.buffs)
                {
                    LoadTemplateData<BuffTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.auras)
                {
                    LoadTemplateData<AuraTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.items)
                {
                    LoadTemplateData<ItemTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.cards)
                {
                    LoadTemplateData<CardTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                if (list.unit_events != null)
                {
                    foreach (string file in list.unit_events)
                    {
                        LoadTemplateData<UnitEventTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                }
                foreach (string file in list.guis)
                {
                    LoadTemplateData<BattleUITemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
            }
        }

        protected async Task LoadAllTemplateDataAsync(ListIndex list, IRangeValue progress)
        {
            if (progress != null)
            {
                progress?.SetRange(0, list.TotalCount, 0);
            }
            if (TemplateDataCenter.ENABLE_BATCH_LOAD)
            {
                var t1 = Task.Run(async () =>
                {
                    foreach (string file in list.units)
                    {
                        await LoadTemplateDataAsync<UnitInfo>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t2 = Task.Run(async () =>
                {
                    foreach (string file in list.skills)
                    {
                        await LoadTemplateDataAsync<SkillTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t3 = Task.Run(async () =>
                {
                    foreach (string file in list.spells)
                    {
                        await LoadTemplateDataAsync<SpellTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t4 = Task.Run(async () =>
                {
                    foreach (string file in list.buffs)
                    {
                        await LoadTemplateDataAsync<BuffTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t5 = Task.Run(async () =>
                {
                    foreach (string file in list.auras)
                    {
                        await LoadTemplateDataAsync<AuraTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t6 = Task.Run(async () =>
                {
                    foreach (string file in list.items)
                    {
                        await LoadTemplateDataAsync<ItemTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t7 = Task.Run(async () =>
                {
                    foreach (string file in list.cards)
                    {
                        await LoadTemplateDataAsync<CardTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                var t8 = Task.Run(async () =>
                {
                    if (list.unit_events != null)
                    {
                        foreach (string file in list.unit_events)
                        {
                            await LoadTemplateDataAsync<UnitEventTemplate>(file);
                            progress?.SetText(Path.GetFileName(file)).Add(1);
                        }
                    }
                });
                var t9 = Task.Run(async () =>
                {
                    foreach (string file in list.guis)
                    {
                        await LoadTemplateDataAsync<BattleUITemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                });
                await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9);
            }
            else
            {
                foreach (string file in list.units)
                {
                    await LoadTemplateDataAsync<UnitInfo>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.skills)
                {
                    await LoadTemplateDataAsync<SkillTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.spells)
                {
                    await LoadTemplateDataAsync<SpellTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.buffs)
                {
                    await LoadTemplateDataAsync<BuffTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.auras)
                {
                    await LoadTemplateDataAsync<AuraTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.items)
                {
                    await LoadTemplateDataAsync<ItemTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                foreach (string file in list.cards)
                {
                    await LoadTemplateDataAsync<CardTemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
                if (list.unit_events != null)
                {
                    foreach (string file in list.unit_events)
                    {
                        await LoadTemplateDataAsync<UnitEventTemplate>(file);
                        progress?.SetText(Path.GetFileName(file)).Add(1);
                    }
                }
                foreach (string file in list.guis)
                {
                    await LoadTemplateDataAsync<BattleUITemplate>(file);
                    progress?.SetText(Path.GetFileName(file)).Add(1);
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        protected TemplateData LoadTemplateData(int templateID, Type templateType)
        {
            if (templateID == 0)
            {
                return null;
            }
            try
            {
                if (IsTemplateLoaded(templateType, templateID, out var template))
                {
                    return template;
                }
                if (templateType.Equals(typeof(UnitInfo)))
                {
                    return LoadTemplateData<UnitInfo>(dataDir + "/units/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(SkillTemplate)))
                {
                    return LoadTemplateData<SkillTemplate>(dataDir + "/skills/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(SpellTemplate)))
                {
                    return LoadTemplateData<SpellTemplate>(dataDir + "/spells/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(BuffTemplate)))
                {
                    return LoadTemplateData<BuffTemplate>(dataDir + "/buffs/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(AuraTemplate)))
                {
                    return LoadTemplateData<AuraTemplate>(dataDir + "/auras/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(ItemTemplate)))
                {
                    return LoadTemplateData<ItemTemplate>(dataDir + "/items/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(UnitEventTemplate)))
                {
                    return LoadTemplateData<UnitEventTemplate>(dataDir + "/unit_events/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(CardTemplate)))
                {
                    return LoadTemplateData<CardTemplate>(dataDir + "/cards/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(BattleUITemplate)))
                {
                    return LoadTemplateData<BattleUITemplate>(dataDir + "/guis/" + templateID + ".xml");
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            return null;
        }
        protected async Task<TemplateData> LoadTemplateDataAsync(int templateID, Type templateType)
        {
            if (templateID == 0)
            {
                return null;
            }
            try
            {
                if (IsTemplateLoaded(templateType, templateID, out var template))
                {
                    return template;
                }
                if (templateType.Equals(typeof(UnitInfo)))
                {
                    return await LoadTemplateDataAsync<UnitInfo>(dataDir + "/units/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(SkillTemplate)))
                {
                    return await LoadTemplateDataAsync<SkillTemplate>(dataDir + "/skills/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(SpellTemplate)))
                {
                    return await LoadTemplateDataAsync<SpellTemplate>(dataDir + "/spells/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(BuffTemplate)))
                {
                    return await LoadTemplateDataAsync<BuffTemplate>(dataDir + "/buffs/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(AuraTemplate)))
                {
                    return await LoadTemplateDataAsync<AuraTemplate>(dataDir + "/auras/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(ItemTemplate)))
                {
                    return await LoadTemplateDataAsync<ItemTemplate>(dataDir + "/items/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(UnitEventTemplate)))
                {
                    return await LoadTemplateDataAsync<UnitEventTemplate>(dataDir + "/unit_events/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(CardTemplate)))
                {
                    return await LoadTemplateDataAsync<CardTemplate>(dataDir + "/cards/" + templateID + ".xml");
                }
                else if (templateType.Equals(typeof(BattleUITemplate)))
                {
                    return await LoadTemplateDataAsync<BattleUITemplate>(dataDir + "/guis/" + templateID + ".xml");
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            return null;
        }
        protected T LoadTemplateData<T>(string file) where T : TemplateData
        {
            try
            {
                T info = LoadData<T>(file);
                if (info != null)
                {
                    info.IsOriginal = true;
                    templates.AddTemplateData(info);
                    if (Verbose) log.Info("LoadTemplate : " + info.GetType() + " : " + info);
                    return info as T;
                }
                else
                {
                    log.Error("LoadTemplate Error : " + file);
                }
            }
            catch (Exception err)
            {
                throw new Exception("LoadTemplate : " + file + "\n" + err.Message, err);
            }
            return null;
        }
        protected async Task<T> LoadTemplateDataAsync<T>(string file) where T : TemplateData
        {
            try
            {
                T info = await LoadDataAsync<T>(file);
                if (info != null)
                {
                    info.IsOriginal = true;
                    templates.AddTemplateData(info);
                    if (Verbose) log.Info("LoadTemplate : " + info.GetType() + " : " + info);
                    return info as T;
                }
                else
                {
                    log.Error("LoadTemplate Error : " + file);
                }
            }
            catch (Exception err)
            {
                throw new Exception("LoadTemplate : " + file + "\n" + err.Message, err);
            }
            return null;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        protected virtual T LoadData<T>(string file) where T : class, ISerializable
        {
            T info = null;
            try
            {
                if (file.EndsWith(".xml"))
                {
                    if (TemplateDataCenter.ENABLE_LOAD_FROM_BIN)
                    {
                        string bin = file + ".bin";
                        try
                        {
                            if (Resource.TryOpenStream(bin, out var stream))
                            {
                                using (stream)
                                {
                                    info = LoadDataFromBin<T>(stream, codec);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            info = null;
                            log.Error("LoadData Binary Error : " + bin + "\n" + err.Message, err);
                            log.Warn($"Try To Load Xml : {file}");
                            //throw new Exception("LoadData Error : " + bin + "\n" + err.Message, err);
                        }
                    }
                    if (info == null)
                    {
                        info = LoadXmlAs<T>(file);
                    }
                    if (info != null)
                    {
                        int sncount = templates.RehashTemplate(info);
                        if (Verbose) log.Info($"RehashTemplate : {info} : {sncount}");
                    }
                }
            }
            catch (Exception err)
            {
                throw new Exception($"LoadScene Error : {file}", err);
            }
            return info;
        }
        protected virtual async Task<T> LoadDataAsync<T>(string file) where T : class, ISerializable
        {
            T info = null;
            try
            {
                if (file.EndsWith(".xml"))
                {
                    if (TemplateDataCenter.ENABLE_LOAD_FROM_BIN)
                    {
                        string bin_file = file + ".bin";
                        try
                        {
                            var stream = await Resource.LoadDataAsync(bin_file);
                            if (stream != null)
                            {
                                info = LoadDataFromBin<T>(new DeepCore.IO.MemoryStream(stream), codec);
                            }
                        }
                        catch (Exception err)
                        {
                            info = null;
                            log.Error("LoadData Binary Error : " + bin_file + "\n" + err.Message, err);
                            log.Warn($"Try To Load Xml : {file}");
                            //throw new Exception("LoadData Error : " + bin_file + "\n" + err.Message, err);
                        }
                    }
                    if (info == null)
                    {
                        info = await LoadXmlAsAsync<T>(file);
                    }
                    if (info != null)
                    {
                        int sncount = templates.RehashTemplate(info);
                        if (Verbose) log.Info($"RehashTemplate : {info} : {sncount}");
                    }
                }
            }
            catch (Exception err)
            {
                throw new Exception($"LoadScene Error : {file}", err);
            }
            return info;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------
        protected virtual SceneData LoadSceneData(string file, bool client_data)
        {
            SceneData info = null;
            try
            {
                string bin_file = file + ".bin";
                if (TemplateDataCenter.ENABLE_LOAD_FROM_BIN && Resource.TryOpenStream(bin_file, out var stream))
                {
                    try
                    {
                        using (stream)
                        {
                            InputStream input = new InputStream(stream, codec);
                            int typeID = input.GetS32();
                            Type type = codec.GetType(typeID);
                            if (typeof(SceneData).Equals(type))
                            {
                                info = new SceneData();
                                if (client_data)
                                {
                                    info.ReadExternalByClient(input);
                                }
                                else
                                {
                                    info.ReadExternal(input);
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        info = null;
                        log.Error($"LoadScene Binary Error : {bin_file}", err);
                        log.Warn($"Try To Load Xml : {file}");
                    }
                }
                if (info == null)
                {
                    info = LoadXmlAs<SceneData>(file);
                    if (info != null && file.TryLastIndexOf('.', out var ld))
                    {
                        var hostfile = file.Substring(0, ld) + ".host";
                        if (Resource.ExistData(hostfile))
                        {
                            var host = LoadXmlAs<SceneData.SceneHostData>(hostfile);
                            if (host != null)
                            {
                                info.Host = host;
                            }
                        }
                    }
                }
                if (info != null)
                {
                    info.IsOriginal = true;
                    int sncount = templates.RehashScene(info);
                    if (Verbose) log.Info($"RehashScene : {info} : {sncount}");
                    if (Verbose) log.Info("LoadScene : " + info.GetType() + " : " + info);
                }
                else
                {
                    log.Error("LoadScene Error : " + file);
                }
                return info;
            }
            catch (Exception err)
            {
                throw new Exception($"LoadScene Error : {file}", err);
            }
        }
        protected virtual async Task<SceneData> LoadSceneDataAsync(string file, bool client_data)
        {
            SceneData info = null;
            try
            {
                string bin_file = file + ".bin";
                if (TemplateDataCenter.ENABLE_LOAD_FROM_BIN)
                {
                    try
                    {
                        var stream = await Resource.LoadDataAsync(bin_file);
                        if (stream != null)
                        {
                            var input = new InputStream(new DeepCore.IO.MemoryStream(stream), codec);
                            int typeID = input.GetS32();
                            Type type = codec.GetType(typeID);
                            if (typeof(SceneData).Equals(type))
                            {
                                info = new SceneData();
                                if (client_data)
                                {
                                    info.ReadExternalByClient(input);
                                }
                                else
                                {
                                    info.ReadExternal(input);
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        info = null;
                        log.Error($"LoadScene Binary Error : {bin_file}", err);
                        log.Warn($"Try To Load Xml : {file}");
                    }
                }
                if (info == null)
                {
                    info = await LoadXmlAsAsync<SceneData>(file);
                    if (info != null && file.TryLastIndexOf('.', out var ld))
                    {
                        var hostfile = file.Substring(0, ld) + ".host";
                        if (await Resource.ExistDataAsync(hostfile))
                        {
                            var host = await LoadXmlAsAsync<SceneData.SceneHostData>(hostfile);
                            if (host != null)
                            {
                                info.Host = host;
                            }
                        }

                    }
                }
                if (info != null)
                {
                    info.IsOriginal = true;
                    int sncount = templates.RehashScene(info);
                    if (Verbose) log.Info($"RehashScene : {info} : {sncount}");
                    if (Verbose) log.Info("LoadScene : " + info.GetType() + " : " + info);
                }
                else
                {
                    log.Error("LoadScene Error : " + file);
                }
                return info;
            }
            catch (Exception err)
            {
                throw new Exception($"LoadScene Error : {file}", err);
            }
        }

        #endregion
        //---------------------------------------------------------------------------------------------------
        #region DataUtil
        public static T LoadXmlAs<T>(byte[] data, T default_value = default)
        {
            try
            {
                var xml = XmlUtil.LoadXML(data);
                if (xml != null)
                    return XmlUtil.XmlToObject<T>(xml);
            }
            catch (Exception err)
            {
                string msg = "LoadXml Error : " + data + "\n" + err.Message;
                if (log != null)
                {
                    log.Error(msg, err);
                }
                else
                {
                    Console.WriteLine(msg + "\r\n" + err.StackTrace);
                }
            }
            return default_value;
        }
        public static T LoadXmlAs<T>(string path, T default_value = default)
        {
            try
            {
                XmlDocument xml = XmlUtil.LoadXML(path);
                if (xml != null)
                    return XmlUtil.XmlToObject<T>(xml);
            }
            catch (Exception err)
            {
                string msg = "LoadXml Error : " + path + "\n" + err.Message;
                if (log != null)
                {
                    log.Error(msg, err);
                }
                else
                {
                    Console.WriteLine(msg + "\r\n" + err.StackTrace);
                }
            }
            return default_value;
        }
        public static async Task<T> LoadXmlAsAsync<T>(string path, T default_value = default)
        {
            try
            {
                var xml = await XmlUtil.LoadXMLAsync(path);
                if (xml != null)
                    return XmlUtil.XmlToObject<T>(xml);
            }
            catch (Exception err)
            {
                string msg = "LoadXml Error : " + path + "\n" + err.Message;
                log.Error(msg, err);
            }
            return default_value;
        }

        protected static T LoadDataFromBin<T>(Stream stream, IExternalizableFactory factory) where T : ISerializable
        {
            using (InputStream input = new InputStream(stream, factory))
            {
                return input.GetObj<T>();
            }
        }
        protected static T LoadDataFromBin<T>(byte[] bytes, IExternalizableFactory factory) where T : ISerializable
        {
            using (InputStream input = new InputStream(new DeepCore.IO.MemoryStream(bytes), factory))
            {
                return input.GetObj<T>();
            }
        }
        protected static byte[] SaveDataToBin(ISerializable data, IExternalizableFactory factory)
        {
            using (var ms = new DeepCore.IO.MemoryStream())
            {
                using (var output = new OutputStream(ms, factory))
                {
                    output.PutObj(data);
                }
                return ms.ToArray();
            }
        }


        public static string DataToXmlText(object data)
        {
            StringBuilder output = new StringBuilder();
            try
            {
                XmlDocument doc = XmlUtil.ObjectToXml(data);
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = UTF8;
                using (XmlWriter xml = XmlWriter.Create(output, settings))
                {
                    doc.Save(xml);
                    xml.Flush();
                }
            }
            catch (Exception err)
            {
                output.AppendLine(err.Message);
                output.AppendLine(err.StackTrace);
            }
            return output.ToString();
        }

        public static bool ValidateBin(IExternalizable data, IExternalizableFactory factory, out string xml, out string retxml)
        {
            try
            {
                xml = DataToXmlText(data);
                byte[] bin;
                using (var ms = new DeepCore.IO.MemoryStream(1024 * 1024))
                {
                    OutputStream output = new OutputStream(ms, factory);
                    output.PutExt(data);
                    ms.Flush();
                    bin = ms.ToArray();
                }
                using (var ms = new DeepCore.IO.MemoryStream(bin))
                {
                    InputStream input = new InputStream(ms, factory);
                    object ret = input.GetExtAny();
                    retxml = DataToXmlText(ret);
                    if (xml.Equals(retxml))
                    {
                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                retxml = xml = err.Message + "\r\n" + err.StackTrace;
            }
            return false;
        }

        public static byte[] DataToBin(IExternalizable data, IExternalizableFactory factory)
        {
            using (var ms = new DeepCore.IO.MemoryStream(1024 * 1024))
            {
                OutputStream output = new OutputStream(ms, factory);
                output.PutExt(data);
                ms.Flush();
                byte[] bin = new byte[ms.Position];
                Array.Copy(ms.GetBuffer(), bin, bin.Length);
                return bin;
            }
        }
        public static byte[] SaveDataToBin(FileInfo file, IExternalizable data, IExternalizableFactory factory)
        {
            byte[] bin = DataToBin(data, factory);
            File.WriteAllBytes(file.FullName, bin);
            return bin;
        }

        public static T DataFromXML<T>(string text) where T : IExternalizable
        {
            XmlDocument xml = XmlUtil.FromString(text);
            if (xml != null)
            {
                return XmlUtil.XmlToObject<T>(xml);
            }
            return default;
        }


        #endregion
        //---------------------------------------------------------------------------------------------------
    }


}
