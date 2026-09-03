using DeepCore;
using DeepCore.Concurrent;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepEditor.Common;
using DeepEditorConsole;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Tools;
using System.Collections.Concurrent;
using System.Text;
using System.Xml;

namespace DeepEditor
{
    public class EditorRuntime : ProjectRuntime
    {
        public static EditorRuntime Runtime { get; private set; }
        public DirectoryInfo DataDir { get; }
        public IGameEditor GameEditor { get; }
        public DirectoryInfo EditorRootDir { get; }
        public IExternalizableFactory Codec => ZoneDataFactory.Factory.PersistCodec;
        public EditorRuntime(DirectoryInfo dataDir, IGameEditor gameEditor)
        {
            Runtime = this;
            this.DataDir = dataDir;
            this.DataDir.Refresh();
            this.EditorRootDir = DataDir.Parent;
            this.GameEditor = gameEditor;
            this.LoadCFG();

            this.typeDict = new Dictionary<Type, DirectoryInfo>()
            {
                {typeof(UnitInfo), new DirectoryInfo(DataDir_units)},
                {typeof(SkillTemplate), new DirectoryInfo(DataDir_skills)},
                {typeof(SpellTemplate), new DirectoryInfo(DataDir_spells)},
                {typeof(BuffTemplate), new DirectoryInfo(DataDir_buffs)},
                {typeof(ItemTemplate), new DirectoryInfo(DataDir_items)},
                {typeof(AuraTemplate), new DirectoryInfo(DataDir_auras)},
                {typeof(UnitEventTemplate), new DirectoryInfo(DataDir_unit_events)},
                {typeof(BattleUITemplate), new DirectoryInfo(DataDir_guis)},
                {typeof(CardTemplate), new DirectoryInfo(DataDir_cards)},
                {typeof(SceneData), new DirectoryInfo(DataDir_scenes)},
            };
        }
        private Dictionary<Type, DirectoryInfo> typeDict;
        public string DataDir_units { get { return this.DataDir + "/units"; } }
        public string DataDir_skills { get { return this.DataDir + "/skills"; } }
        public string DataDir_spells { get { return this.DataDir + "/spells"; } }
        public string DataDir_buffs { get { return this.DataDir + "/buffs"; } }
        public string DataDir_items { get { return this.DataDir + "/items"; } }
        public string DataDir_auras { get { return this.DataDir + "/auras"; } }
        public string DataDir_unit_events { get { return this.DataDir + "/unit_events"; } }
        public string DataDir_guis { get { return this.DataDir + "/guis"; } }
        public string DataDir_cards { get { return this.DataDir + "/cards"; } }
        public string DataDir_scenes { get { return this.DataDir + "/scenes"; } }

        public string DataDir_res_properties { get { return this.DataDir + "/resource_properties"; } }

        public FileInfo SaveCfgFile { get { return new FileInfo(this.DataDir + "/config.xml"); } }
        public FileInfo SaveCfgExtFile { get { return new FileInfo(this.DataDir + "/config_ext.xml"); } }
        public FileInfo SaveCfgGlobalFile { get { return new FileInfo(this.DataDir + "/config_global.xml"); } }
        public FileInfo SaveTerrainDefinitionFile { get { return new FileInfo(this.DataDir + "/terrain_definition.xml"); } }
        public FileInfo SaveUnitActionDefinitionFile { get { return new FileInfo(this.DataDir + "/unit_action_definition.xml"); } }
        public FileInfo SaveResourcePropertiesMapFile { get { return new FileInfo(this.DataDir + "/resource_properties.xml"); } }
        public FileInfo SaveCardAffectsFile { get { return new FileInfo(this.DataDir + "/card_affects.xml"); } }

        public DirectoryInfo GetTemplateDirectory(Type type)
        {
            return typeDict[type];
        }

        public Config CFG { get; set; }
        public ICommonConfig ExtCFG { get; set; }
        public IGlobalConfig GlobalCFG { get; set; }
        public TerrainDefinitionMap DefaultTerrainDefinition { get; set; }
        public UnitActionDefinitionMap DefaultUnitActionDefinition { get; set; }
        public TerrainDefinitionMap.MapBlockBrush DefaultTerrainBrush
        {
            get
            {
                foreach (var b in DefaultTerrainDefinition.Brushes)
                {
                    if (b.IsBlock)
                    {
                        return b;
                    }
                }
                return null;
            }
        }
        public ResourcePropertiesMap ResourcePropertiesMap { get; set; }

        public void LoadCFG()
        {
            try
            {
                var default_ext = ZoneDataFactory.Factory.CreateCommonCFG();
                var default_global = ZoneDataFactory.Factory.CreateGlobalCFG();
                this.CFG = LoadXmlAs(SaveCfgFile, new Config());
                this.ExtCFG = LoadXmlAs(SaveCfgExtFile, default_ext);
                this.GlobalCFG = LoadXmlAs(SaveCfgGlobalFile, default_global);
                if (this.ExtCFG?.GetType() != default_ext?.GetType())
                {
                    this.ExtCFG = default_ext;
                }
                if (this.GlobalCFG?.GetType() != default_global?.GetType())
                {
                    this.GlobalCFG = default_global;
                }
                this.DefaultTerrainDefinition = LoadXmlAs(SaveTerrainDefinitionFile, new TerrainDefinitionMap());
                this.DefaultUnitActionDefinition = LoadXmlAs(SaveUnitActionDefinitionFile, new UnitActionDefinitionMap());
                this.ResourcePropertiesMap = LoadAllResourceMap();// LoadXmlAs(SaveResourcePropertiesMapFile, new ResourcePropertiesMap());
                //LoadResourceMap();
            }
            catch
            {
                throw;
            }
        }
        public void SaveCFG(object obj, FileInfo file)
        {
            SaveXml(obj, file);
        }
        public void SaveCFG()
        {
            SaveXml(this.CFG, SaveCfgFile);
            SaveXml(this.ExtCFG, SaveCfgExtFile);
            SaveXml(this.GlobalCFG, SaveCfgGlobalFile);
            SaveXml(this.DefaultTerrainDefinition, SaveTerrainDefinitionFile);
            SaveXml(this.DefaultUnitActionDefinition, SaveUnitActionDefinitionFile);
            SaveXml(this.ResourcePropertiesMap, SaveResourcePropertiesMapFile);
        }
        public ResourcePropertiesMap LoadAllResourceMap()
        {
            var map = new ResourcePropertiesMap();
            if (Directory.Exists(DataDir_res_properties))
            {
                foreach (var f in new DirectoryInfo(DataDir_res_properties).GetFiles())
                {
                    if (f.Extension.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var res = LoadResTuple(f);
                            if (res.Properties != null)
                            {
                                map.PropertiesMap.Put(res.ResID, res.Properties);
                            }
                        }
                        catch { }
                    }
                }
            }
            return map;
        }
        public ResourcePropertiesTuple LoadResTuple(FileInfo path)
        {
            try
            {
                return LoadXmlAs<ResourcePropertiesTuple>(path);
            }
            catch (Exception err) { err.PrintStackTrace(); }
            return null;
        }
        public void SaveResTuple(string resID, IResourceProperties prop)
        {
            try
            {
                var path = new FileInfo($"{DataDir_res_properties}/{CMD5.CalculateMD5(resID)}.xml");
                if (prop == null)
                {
                    ResourcePropertiesMap.PropertiesMap.Remove(resID);
                    CFiles.Delete(path);
                }
                else
                {
                    CFiles.CreateFile(path);
                    ResourcePropertiesMap.PropertiesMap.Put(resID, prop);
                    SaveXml(new ResourcePropertiesTuple()
                    {
                        ResID = resID,
                        Properties = prop
                    }, path);
                }
            }
            catch (Exception err) { err.PrintStackTrace(); }

        }
        public void SaveResourceMap()
        {
            this.ResourcePropertiesMap = LoadAllResourceMap();
            SaveXml(ResourcePropertiesMap, SaveResourcePropertiesMapFile);
        }

        //---------------------------------------------------------------------------------------------------

        public static byte[] SaveBinNode(IExternalizableFactory factory, object mData)
        {
            if (mData is ISerializable)
            {
                using (DeepCore.IO.MemoryStream ms = new DeepCore.IO.MemoryStream(1024 * 1024))
                {
                    OutputStream output = new OutputStream(ms, factory);
                    output.PutObj(mData);
                    ms.Flush();
                    byte[] bin = new byte[ms.Position];
                    Array.Copy(ms.GetBuffer(), bin, bin.Length);
                    return bin;
                }
            }
            else
            {
                throw new Exception($"{mData.GetType()} is not a ISerializable");
            }
        }
        public static object LoadXmlNode(IExternalizableFactory factory, byte[] bin, Type type)
        {
            using (XmlReader xml = XmlReader.Create(new DeepCore.IO.MemoryStream(bin)))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xml);
                var data = new XmlSerializer(false) { Factory = factory }.XmlToObject(type, doc);
                return data;
            }
        }
        public static T LoadXmlNode<T>(IExternalizableFactory factory, byte[] bin)
        {
            using (XmlReader xml = XmlReader.Create(new DeepCore.IO.MemoryStream(bin)))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xml);
                var data = new XmlSerializer(false) { Factory = factory }.XmlToObject<T>(doc);
                return data;
            }
        }
    }

    //---------------------------------------------------------------------------------------------------

    public interface IGameEditor
    {
        void StartWithLoading();
        void EndWithLoading();
        void LoadAllNodes<T>(IRangeValue progress, LoadingAction loading = null, LoadedAction loaded = null) where T : TemplateData, new();
        void ReloadAllNodes<T>(IRangeValue progress, LoadingAction loading = null, LoadedAction loaded = null) where T : TemplateData, new();
        EditorTemplates GenDataCenter(EditorTemplatesData alldata, IRangeValue p);
        void BakeCardCartridges(EditorTemplatesData alldata, IRangeValue p);
        FileInfo SaveAllNodes<T>(IRangeValue progress, string checkDir, SavingAction saving = null, SavedAction saved = null) where T : TemplateData, new();
        void ShowErrorMessage(Exception err);
    }
    //---------------------------------------------------------------------------------------------------

    public class EditorLoadDataTask
    {
        public void Run(EditorRuntime editor, IRangeValue p)
        {
            var GameEditor = editor.GameEditor;
            GameEditor.StartWithLoading();
            {
                //                 IExternalizableFactory codec = ZoneDataFactory.Factory.PersistCodec;
                //                 if (codec is MessageFactoryGenerator)
                //                 {
                //                     File.WriteAllText(GameEditor.EditorRootDir + "/codec.txt", (codec as MessageFactoryGenerator).ListAll());
                //                 }
                ZoneDataFactory.Factory.EditorInit(editor.EditorRootDir);
            }

            if (File.Exists(editor.EditorRootDir + "\\alias.xml"))
            {
                XmlSerializer.LoadAlias(XmlUtil.LoadXML(editor.EditorRootDir + "\\alias.xml"));
            }
            try
            {
                var t1 = Task.Run(() => { GameEditor.LoadAllNodes<UnitInfo>(p); });
                var t2 = Task.Run(() => { GameEditor.LoadAllNodes<SkillTemplate>(p); });
                var t3 = Task.Run(() => { GameEditor.LoadAllNodes<SpellTemplate>(p); });
                var t4 = Task.Run(() => { GameEditor.LoadAllNodes<BuffTemplate>(p); });
                var t5 = Task.Run(() => { GameEditor.LoadAllNodes<AuraTemplate>(p); });
                var t6 = Task.Run(() => { GameEditor.LoadAllNodes<ItemTemplate>(p); });
                var t7 = Task.Run(() => { GameEditor.LoadAllNodes<UnitEventTemplate>(p); });
                var t8 = Task.Run(() => { GameEditor.LoadAllNodes<CardTemplate>(p); });
                var t9 = Task.Run(() => { GameEditor.LoadAllNodes<BattleUITemplate>(p); });
                var t10 = Task.Run(() =>
                {
                    GameEditor.LoadAllNodes<SceneData>(p, (xmlfile) => { }, (xmlfile, data) =>
                    {
                        if (data is SceneData info && xmlfile.FullName.TryLastIndexOf('.', out var ld))
                        {
                            var hostfile = new FileInfo(xmlfile.FullName.Substring(0, ld) + ".host");
                            if (hostfile.Exists)
                            {
                                var hostbin = FileSystemWorkSpace.ReadAllBytes(hostfile);
                                var host = EditorRuntime.LoadXmlNode<SceneData.SceneHostData>(ZoneDataFactory.Factory.PersistCodec, hostbin);
                                info.Host = host;
                            }
                        }
                    });
                });
                Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10).Wait();
            }
            finally
            {
                XmlSerializer.ClearTypeAlias();
            }
            GameEditor.EndWithLoading();
        }
    }

    public class EditorReloadDataTask
    {
        public EditorReloadDataTask()
        {
            try
            {
                EditorRuntime.Runtime.LoadCFG();
            }
            catch (Exception ex)
            {
                ex.PrintStackTrace();
            }
        }
        public void ThreadRun(EditorRuntime editor, IRangeValue p)
        {
            var GameEditor = editor.GameEditor;
            var t1 = Task.Run(() => { GameEditor.ReloadAllNodes<UnitInfo>(p); });
            var t2 = Task.Run(() => { GameEditor.ReloadAllNodes<SkillTemplate>(p); });
            var t3 = Task.Run(() => { GameEditor.ReloadAllNodes<SpellTemplate>(p); });
            var t4 = Task.Run(() => { GameEditor.ReloadAllNodes<BuffTemplate>(p); });
            var t5 = Task.Run(() => { GameEditor.ReloadAllNodes<AuraTemplate>(p); });
            var t6 = Task.Run(() => { GameEditor.ReloadAllNodes<ItemTemplate>(p); });
            var t7 = Task.Run(() => { GameEditor.ReloadAllNodes<UnitEventTemplate>(p); });
            var t8 = Task.Run(() => { GameEditor.ReloadAllNodes<CardTemplate>(p); });
            var t9 = Task.Run(() => { GameEditor.ReloadAllNodes<BattleUITemplate>(p); });
            var t10 = Task.Run(() =>
            {
                GameEditor.ReloadAllNodes<SceneData>(p, (xmlfile) => { }, (xmlfile, data) =>
                {
                    if (data is SceneData info && xmlfile.FullName.TryLastIndexOf('.', out var ld))
                    {
                        var hostfile = new FileInfo(xmlfile.FullName.Substring(0, ld) + ".host");
                        if (hostfile.Exists)
                        {
                            var hostbin = FileSystemWorkSpace.ReadAllBytes(hostfile);
                            var host = EditorRuntime.LoadXmlNode<SceneData.SceneHostData>(ZoneDataFactory.Factory.PersistCodec, hostbin);
                            info.Host = host;
                        }
                    }
                });
            });
            Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10).Wait();
        }
    }

    public class EditorSaveDataTask
    {
        public EditorTemplates Run(EditorRuntime editor, CardRuntimeAdapter cardRuntime, bool check, EditorTemplatesData all_nodes, IRangeValue p)
        {
            p?.SetMax(all_nodes.FileCount);
            p?.SetText("生成序列号");
            SerialData.GenAllSerialNumber(all_nodes);
            try
            {
                var GameEditor = editor.GameEditor;
                var gen_md5_files = new ConcurrentQueue<FileInfo>();

                p?.SetText("预处理编辑器插件:" + ZoneDataFactory.Factory.GetType().Name);
                ZoneDataFactory.Factory.EditorSaving(all_nodes, editor.DataDir, check); // += 1

                var checkDir = editor.EditorRootDir.FullName + "/.conflict";
                // save all xml
                {
                    var runtime = cardRuntime;
                    {
                        EditorRuntime.Runtime.SaveCFG();
                    }
                    p?.SetText("保存模板");
                    if (check)
                    {
                        var alltemp = all_nodes.AllTemplates();
                        var total = alltemp.Count;
                        var index = 0;
                        foreach (var temp in alltemp)
                        {
                            p?.SetText($"清理FuncID: ({index}/{total}) {temp}");
                            runtime.CleanUpAffectBinding(temp, true);
                            index++;
                        }
                        foreach (var card in all_nodes.Cards.Values)
                        {
                            if (card.Fields.Count > 0)
                            {
                                p?.SetText($"填充词缀: {card}");
                                runtime.SaveOnlyForSelfTemplate(all_nodes.AllTemplates(), card, check);
                            }
                        }
                    }
                    var tasks = new List<Task>();
                    tasks.Add(Task.Run(() => { gen_md5_files.Enqueue(GameEditor.SaveAllNodes<UnitInfo>(p, checkDir)); }));
                    tasks.Add(Task.Run(() => { gen_md5_files.Enqueue(GameEditor.SaveAllNodes<SkillTemplate>(p, checkDir)); }));
                    tasks.Add(Task.Run(() => { gen_md5_files.Enqueue(GameEditor.SaveAllNodes<SpellTemplate>(p, checkDir)); }));
                    tasks.Add(Task.Run(() => { gen_md5_files.Enqueue(GameEditor.SaveAllNodes<BuffTemplate>(p, checkDir)); }));
                    tasks.Add(Task.Run(() => { gen_md5_files.Enqueue(GameEditor.SaveAllNodes<AuraTemplate>(p, checkDir)); }));
                    tasks.Add(Task.Run(() => { gen_md5_files.Enqueue(GameEditor.SaveAllNodes<ItemTemplate>(p, checkDir)); }));
                    tasks.Add(Task.Run(() => { gen_md5_files.Enqueue(GameEditor.SaveAllNodes<UnitEventTemplate>(p, checkDir)); }));
                    tasks.Add(Task.Run(() => { gen_md5_files.Enqueue(GameEditor.SaveAllNodes<BattleUITemplate>(p, checkDir)); }));
                    tasks.Add(Task.Run(() =>
                    {
                        if (check)
                        {
                            var cardAffects = new CardAffectBindingTemplates();
                            foreach (var card in all_nodes.Cards.Values)
                            {
                                if (card.Fields.Count > 0)
                                {
                                    p?.SetText($"烘培词缀: {card}");
                                    // 获取Card影响列表
                                    var runtimeCardAffect = runtime.RefreshAffectBindings(all_nodes.AllTemplates(), card, check);
                                    if (runtimeCardAffect != null)
                                    {
                                        // 烘焙影响列表
                                        foreach (var runtimeAffect in runtimeCardAffect.Affects)
                                        {
                                            if (runtimeAffect.Value.Templates.Count > 0)
                                            {
                                                var tempType = runtimeAffect.Key;
                                                foreach (var runtimeTemplate in runtimeAffect.Value.Templates)
                                                {
                                                    var tempID = runtimeTemplate.Key;
                                                    var templist = cardAffects.CardToTemplates.GetOrNew(card.ID).GetOrNew(tempType);
                                                    if (!templist.Contains(tempID)) templist.Add(tempID);
                                                    var cardlist = cardAffects.TemplatesToCard.GetOrNew(tempType).GetOrNew(tempID);
                                                    if (!cardlist.Contains(card.ID)) cardlist.Add(card.ID);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            {
                                // Step 1: 构建子模板引用图 subRefGraph[parentType][parentID] -> [(childType,childID),...]
                                var subRefGraph = new Dictionary<Type, Dictionary<int, List<(Type, int)>>>();
                                foreach (var template in all_nodes.AllTemplates())
                                {
                                    var children = new List<(Type, int)>();
                                    foreach (var r in PropertyUtil.CollectFieldAttributeValues<TemplateIDAttribute, int>(template))
                                        if (r.FieldValue > 0)
                                            children.Add((r.AttributeData.TemplateType, r.FieldValue));
                                    foreach (var r in PropertyUtil.CollectFieldAttributeValues<TemplatesIDAttribute, ArrayList<int>>(template))
                                        if (r.FieldValue != null)
                                            foreach (var id in r.FieldValue)
                                                if (id > 0)
                                                    children.Add((r.AttributeData.TemplateType, id));
                                    if (children.Count == 0) continue;
                                    if (!subRefGraph.TryGetValue(template.GetType(), out var subIdMap))
                                        subRefGraph[template.GetType()] = subIdMap = new Dictionary<int, List<(Type, int)>>();
                                    subIdMap[template.ID] = children;
                                }
                                // Step 2: 反转图 parentGraph[childType][childID] -> {(parentType,parentID),...}
                                var parentGraph = new Dictionary<Type, Dictionary<int, HashSet<(Type, int)>>>();
                                foreach (var typeKV in subRefGraph)
                                {
                                    var parentType = typeKV.Key;
                                    foreach (var idKV in typeKV.Value)
                                    {
                                        var parentID = idKV.Key;
                                        foreach (var (childType, childID) in idKV.Value)
                                        {
                                            if (!parentGraph.TryGetValue(childType, out var pIdMap))
                                                parentGraph[childType] = pIdMap = new Dictionary<int, HashSet<(Type, int)>>();
                                            if (!pIdMap.TryGetValue(childID, out var pSet))
                                                pIdMap[childID] = pSet = new HashSet<(Type, int)>();
                                            pSet.Add((parentType, parentID));
                                        }
                                    }
                                }
                                // Step 3: 传递闭包 — 沿 parentGraph 向祖先传播 CardID
                                var snapshot = new List<(Type type, int id, List<int> cardIDs)>();
                                foreach (var typeKV in cardAffects.TemplatesToCard)
                                    foreach (var idKV in typeKV.Value)
                                        snapshot.Add((typeKV.Key, idKV.Key, new List<int>(idKV.Value)));
                                foreach (var (startType, startID, cardIDs) in snapshot)
                                {
                                    var visited = new HashSet<(Type, int)> { (startType, startID) };
                                    var bfsQueue = new Queue<(Type, int)>();
                                    bfsQueue.Enqueue((startType, startID));
                                    while (bfsQueue.Count > 0)
                                    {
                                        var (curType, curID) = bfsQueue.Dequeue();
                                        if (!parentGraph.TryGetValue(curType, out var curIdMap)) continue;
                                        if (!curIdMap.TryGetValue(curID, out var parents)) continue;
                                        foreach (var ancestor in parents)
                                        {
                                            if (!visited.Add(ancestor)) continue;
                                            bfsQueue.Enqueue(ancestor);
                                            var cardList = cardAffects.TemplatesToCard.GetOrNew(ancestor.Item1).GetOrNew(ancestor.Item2);
                                            foreach (var cardID in cardIDs)
                                                if (!cardList.Contains(cardID))
                                                    cardList.Add(cardID);
                                        }
                                    }
                                }
                            }
                            editor.SaveXml(cardAffects, editor.SaveCardAffectsFile);
                        }
                        gen_md5_files.Enqueue(GameEditor.SaveAllNodes<CardTemplate>(p, checkDir, (xmlpath, t) =>
                        {
                            p?.SetText($"保存词缀: {t}");
                        }));
                    }));
                    tasks.Add(Task.Run(() =>
                    {
                        gen_md5_files.Enqueue(GameEditor.SaveAllNodes<SceneData>(p, checkDir, (xmlpath, zone) =>
                        {
                            try
                            {
                                var sd = zone as SceneData;
                                if (!sd.Terrain.ZoneData.CheckHasFlag())
                                {
                                    sd.Terrain.CleanTerrain();
                                }
                                p?.SetText($"保存场景 Saving: {zone}");
                                ZoneDataFactory.Factory.EditorSavingSceneData(all_nodes, zone as SceneData, editor.DataDir);
                            }
                            catch (Exception err)
                            {
                                GameEditor.ShowErrorMessage(err);
                                throw;
                            }
                        }, (xmlpath, zone) =>
                        {
                            if (zone is SceneData info && xmlpath.FullName.TryLastIndexOf('.', out var ld))
                            {
                                var hostfile = new FileInfo(xmlpath.FullName.Substring(0, ld) + ".host");
                                if (hostfile.Exists)
                                {
                                    hostfile.Delete();
                                }
                                //var hostbin = XmlUtil.SaveTemplateXML(ZoneDataFactory.Factory.PersistCodec, info.Host);
                                // G2DTreeNode.SaveXML(ZoneDataFactory.Factory.PersistCodec, info.Host);
                                //FileSystemWorkSpace.WriteAllBytes(hostfile, hostbin);
                            }
                            try
                            {
                                p?.SetText($"保存场景 Saved: {zone}");
                                ZoneDataFactory.Factory.EditorSavedSceneData(all_nodes, zone as SceneData, editor.DataDir);
                            }
                            catch (Exception err)
                            {
                                GameEditor.ShowErrorMessage(err);
                                throw;
                            }
                        }));
                    }));
                    Task.WaitAll(tasks.ToArray());
                }
                // add base md5 files
                {
                    p?.SetText("生成MD5");
                    gen_md5_files.Enqueue(EditorRuntime.Runtime.SaveCfgFile);
                    gen_md5_files.Enqueue(EditorRuntime.Runtime.SaveCfgExtFile);
                    gen_md5_files.Enqueue(EditorRuntime.Runtime.SaveCfgGlobalFile);
                    gen_md5_files.Enqueue(EditorRuntime.Runtime.SaveTerrainDefinitionFile);
                    gen_md5_files.Enqueue(EditorRuntime.Runtime.SaveUnitActionDefinitionFile);
                    gen_md5_files.Enqueue(EditorRuntime.Runtime.SaveResourcePropertiesMapFile);
                }
                // save all game plugins

                p?.SetText("解析插件数据:" + ZoneDataFactory.Factory.GetType().Name);
                ZoneDataFactory.Factory.EditorPluginSaved(all_nodes, editor.DataDir, check); // += 1

                if (check)
                {
                    p?.SetText("检查数据完整性:" + ZoneDataFactory.Factory.GetType().Name);
                    ZoneDataFactory.Factory.EditorCheckExistDatas(all_nodes, editor.DataDir);
                }
                p?.SetText("生成资源版本号");
                {
                    {
                        var root = editor.DataDir;
                        StringBuilder lines = new StringBuilder();
                        {
                            var array = gen_md5_files.ToArray();
                            Array.Sort(array, (a, b) => a.FullName.CompareTo(b.FullName));
                            foreach (FileInfo sub in array)
                            {
                                if (sub.Exists)
                                {
                                    string md5 = CMD5.CalculateMD5(sub);
                                    long size = sub.Length;
                                    lines.AppendLine(string.Format(string.Format("{0} : {1,12} : {2}", md5, size, sub.FullName.Substring(root.FullName.Length))));
                                }
                            }
                            lines.Insert(0, CMD5.CalculateMD5(lines.ToString(), CUtils.UTF8) + "\r\n");
                        }
                        File.WriteAllText(editor.DataDir + "\\ver.md5", lines.ToString(), CUtils.UTF8);
                    }
                }
                p?.SetText("生成数据表格");
                var dataRoot = editor.GameEditor.GenDataCenter(all_nodes, p);
                if (check)
                {
                    p?.SetText("烘焙词缀代码");
                    GameEditor.BakeCardCartridges(all_nodes, p);
                }
                return dataRoot;
            }
            finally
            {
                p?.SetText("完成");
                System.Threading.Thread.Sleep(100);
            }
        }


    }

    //---------------------------------------------------------------------------------------------------
}
