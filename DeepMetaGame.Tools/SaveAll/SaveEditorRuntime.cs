using DeepCore;
using DeepCore.Concurrent;
using DeepCore.Game3D.Host;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Xml;
using DeepEditor;
using DeepEditor.Common;
using DeepEditorConsole;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Tools.FuncData;
using DeepTools.LanguageXLS;
using NPOI.SS.Formula.Functions;
using System.Formats.Asn1;
using System.Globalization;
using System.Text;

namespace DeepMetaGame.Tools.SaveAll
{
    public class SaveEditorConfig
    {
        /// <summary>
        /// XLS导出格式
        /// </summary>
        public string templatesFormat = "json";

        /// <summary>
        /// 所有表格目录
        /// </summary>
        public DirectoryInfo templatesInputDir;
        /// <summary>
        /// 所有表格输出目录
        /// </summary>
        public DirectoryInfo templatesOutputDir;

        /// <summary>
        /// 语言表工作目录
        /// </summary>
        public DirectoryInfo langWorkDir;

        /// <summary>
        /// 策划维护的XLS语言表
        /// </summary>
        public FileInfo langWorkXlsFile;
        /// <summary>
        /// 语言Properties输出目录，供游戏内加载使用
        /// </summary>
        public DirectoryInfo langPropertiesOutputDir;

        /// <summary>
        /// 核心战斗模块DLL
        /// </summary>
        public string inputHostAssemblyName;
        /// <summary>
        /// 词缀CS代码烘焙项目目录
        /// </summary>
        public FileInfo cardBakeCSProjFile = null;
        /// <summary>
        /// 词缀CS代码烘焙目录
        /// </summary>
        public DirectoryInfo cardBakeDir = null;
    }

    public class SaveEditorRuntime : IGameEditor
    {
        public static Encoding UTF8 = new UTF8Encoding(false);
        public const string ListFileExt = "/dir.list";
        public const string ListTreeExt = "/dir.tree";
        public const string ListMd5Ext = "/dir.md5";
        private EditorTemplatesData alldatas = new EditorTemplatesData();
        private static Logger log = new LazyLogger("SaveEditorRuntime");
        public EditorTemplatesData AllDatas => alldatas;
        public EditorTemplates DataRoot { get; private set; }
        public SaveEditorConfig SaveConfig { get; }
        public EditorRuntime GameEditor { get; }
        public DirectoryInfo BakeCardDir { get; }
        public FileInfo BakeCardCSProjFile { get; }
        public string InputHostAssemblyName { get; }
        /// <summary>
        /// 所有表格目录
        /// </summary>
        public DirectoryInfo XlsInputDir => SaveConfig.templatesInputDir ?? new DirectoryInfo($"{GameEditor.EditorRootDir}\\templates\\excel");
        /// <summary>
        /// 所有表格目录
        /// </summary>
        public DirectoryInfo XlsOutputDir => SaveConfig.templatesOutputDir ?? new DirectoryInfo($"{GameEditor.EditorRootDir}\\templates\\json");
        /// <summary>
        /// 语言表工作目录
        /// </summary>
        public DirectoryInfo LangWorkDir => SaveConfig.langWorkDir ?? new DirectoryInfo($"{GameEditor.EditorRootDir}\\templates\\lang");
        /// <summary>
        /// 策划维护的XLS语言表
        /// </summary>
        public FileInfo LangWorkXlsFile => SaveConfig.langWorkXlsFile ?? new FileInfo($"{GameEditor.EditorRootDir}\\templates\\lang\\lang.xlsx");
        /// <summary>
        /// 语言Properties输出目录，供游戏内加载使用
        /// </summary>
        public DirectoryInfo LangPropertiesOutputDir => SaveConfig.langPropertiesOutputDir ?? new DirectoryInfo($"{GameEditor.EditorRootDir}\\templates\\lang");
        //----------------------------------------------------------------------------------------------------------------------------------------------------
        public SaveEditorRuntime(DirectoryInfo dataDir, SaveEditorConfig cfg = null)
        {
            this.SaveConfig = cfg ?? new SaveEditorConfig();
            this.GameEditor = new EditorRuntime(dataDir, this);
            if (this.SaveConfig.cardBakeDir != null)
            {
                this.BakeCardDir = this.SaveConfig.cardBakeDir;
            }
            else
            {
                this.BakeCardDir = new DirectoryInfo(GameEditor.EditorRootDir + "/code/Cards");
            }
            if (this.SaveConfig.cardBakeCSProjFile != null)
            {
                this.BakeCardCSProjFile = this.SaveConfig.cardBakeCSProjFile;
            }
            else
            {
                this.BakeCardCSProjFile = new FileInfo(GameEditor.EditorRootDir + "/code/GameEditor.Code.csproj");
            }
            if (!string.IsNullOrEmpty(this.SaveConfig.inputHostAssemblyName))
            {
                this.InputHostAssemblyName = this.SaveConfig.inputHostAssemblyName;
            }
            else
            {
                this.InputHostAssemblyName = Path.GetFileNameWithoutExtension(ZoneDataFactory.Factory.GetType().Assembly.Location);
            }
            TemplateDataCenter.ENABLE_LOAD_FROM_BIN = false;
            this.alldatas.Put(
                GameEditor.CFG,
                GameEditor.ExtCFG,
                GameEditor.GlobalCFG,
                GameEditor.DefaultTerrainDefinition,
                GameEditor.DefaultUnitActionDefinition,
                GameEditor.ResourcePropertiesMap);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------
        public virtual void ShowErrorMessage(Exception err) { err.PrintStackTrace(); }
        public virtual void StartWithLoading() { }
        public virtual void EndWithLoading() { }
        void IGameEditor.LoadAllNodes<T>(IRangeValue progress, LoadingAction loading, LoadedAction loaded)
        {
            LoadAllTableNodes<T>(GameEditor.GetTemplateDirectory(typeof(T)), progress, loading, loaded);
        }
        void IGameEditor.ReloadAllNodes<T>(IRangeValue progress, LoadingAction loading, LoadedAction loaded)
        {
            LoadAllTableNodes<T>(GameEditor.GetTemplateDirectory(typeof(T)), progress, loading, loaded);
        }
        FileInfo IGameEditor.SaveAllNodes<T>(IRangeValue progress, string checkDir, SavingAction saving, SavedAction saved)
        {
            var dir = GameEditor.GetTemplateDirectory(typeof(T));
            var datas = alldatas.GetTemplateDatas(typeof(T));
            SaveAllTableNodes<T>(dir, datas, progress, saving, saved);
            if (!string.IsNullOrEmpty(checkDir))
            {
                foreach (var node in datas)
                {
                    string srcxml;
                    string retxml;
                    if (!XmlUtil.ValidateBin(node, GameEditor.Codec, out srcxml, out retxml))
                    {
                        string sfile = checkDir + $"/{typeof(T).Name}" + "_conflict_" + node.ID + ".src.txt";
                        string dfile = checkDir + $"/{typeof(T).Name}" + "_conflict_" + node.ID + ".bin.txt";
                        CFiles.WriteAllText(sfile, srcxml, CUtils.UTF8);
                        CFiles.WriteAllText(dfile, retxml, CUtils.UTF8);
                        Console.WriteLine(checkDir + "/" + node.ID + ".xml" + " : Save Load 二进制序列化不匹配 ！" +
                            node.GetType() +
                            "\n比较文件已存储到: " + dfile);
                    }
                }
            }
            return new FileInfo(dir + ListMd5Ext);
        }
        /// <summary>
        /// 烘焙词缀系统的CS源代码
        /// </summary>
        public virtual void BakeCardCartridges(EditorTemplatesData alldata, IRangeValue p)
        {
            log.Info("### 烘焙词缀系统的CS源代码 ###");
            var cardRuntime = new CardRuntimeGenerator(alldata, this);
            cardRuntime.Run(p);
        }
        /// <summary>
        /// 烘焙XLS表格到JSON格式，供数据中心加载使用
        /// </summary>
        public void BakeXLS2Json()
        {
            this.OnBakeXLS(this.SaveConfig.templatesFormat, this.XlsInputDir, this.XlsOutputDir);
        }
        protected virtual void OnBakeXLS(string templatesFormat, DirectoryInfo xlsInputDir, DirectoryInfo xlsOutputDir)
        {
            log.Info("### 烘焙XLS表格到JSON格式，供数据中心加载使用 ###");
            var args = new string[] { templatesFormat,
                    $"-id:{xlsInputDir.FullName}",
                    $"-od:{xlsOutputDir.FullName}",
                    "-filter_text:-~"};
            XLSLang.Gen(args);
        }
        public virtual EditorTemplates GenDataCenter(EditorTemplatesData alldata, IRangeValue p)
        {
            {
                this.BakeXLS2Json();
            }
            var DataRoot = ZoneDataFactory.Factory.CreateEditorTemplates(GameEditor.DataDir.FullName, false);
            {
                DataRoot.DataCenter.OnReload += (dc, from, p) =>
                {
                    DataRoot.DataCenter.SaveToBin();
                };
                DataRoot.LoadAllTemplates();
                DataRoot.SaveToMeta();
            }
            //             {
            //                 this.GenXLSToLangCSV();
            //             }
            {
                this.GenLangProperties();
            }
            return DataRoot;
        }
        /// <summary>
        /// 抽取语言表，输出到CSV文件，供策划维护使用
        /// </summary>
        /// <param name="xlsInputDir"></param>
        /// <param name="langWorkDir"></param>
        public void GenXLSToLangCSV()
        {
            this.GenXLSToLangCSV(this.XlsInputDir, this.LangWorkDir);
        }
        protected virtual void GenXLSToLangCSV(DirectoryInfo xlsInputDir, DirectoryInfo langWorkDir)
        {
            log.Info("### 抽取语言表，输出到CSV文件，供策划维护使用 ###");
            var langGen = new XLSToLangCSV(xlsInputDir, langWorkDir);
            langGen.Run();
            log.Info("--------------------------------------------------------");
            log.PushColor();
            log.Color = ConsoleColor.Cyan;
            log.Info(XLSToLangCSV.Usage);
            log.PopColor();
            log.Info("--------------------------------------------------------");
        }
        /// <summary>
        /// 输出Lang Properties文件，供游戏内加载使用
        /// </summary>
        public void GenLangProperties()
        {
            this.GenLangProperties(this.LangWorkXlsFile, this.LangPropertiesOutputDir);
        }
        protected virtual void GenLangProperties(FileInfo langXlsFile, DirectoryInfo langPropertiesOutputDir)
        {
            log.Info("### 输出Lang Properties文件，供游戏内加载使用 ###");
            var args = new string[]{"local",
                $"-if:{langXlsFile.FullName}",
                $"-od:{langPropertiesOutputDir.FullName}",
                "-encoding:utf-8" };
            XLSLang.Gen(args);
            log.Info("--------------------------------------------------------");
            log.PushColor();
            log.Color = ConsoleColor.Cyan;
            log.Info(XLSToLangCSV.Usage);
            log.PopColor();
            log.Info("--------------------------------------------------------");
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------
        protected virtual void LoadAllTableNodes<T>(DirectoryInfo dir, IRangeValue progress, LoadingAction loading, LoadedAction loaded)
        {
            if (File.Exists(dir + ListFileExt))
            {
                var list = File.ReadAllLines(dir + ListFileExt, UTF8);
                var idpath = new HashMap<string, string>();
                foreach (string sub in list)
                {
                    try
                    {
                        string[] kv = sub.Split(';');
                        idpath.Add(kv[1], kv[0]);
                    }
                    catch (Exception) { }
                }
                var listdata = new List<T>();
                foreach (string subname in Directory.GetFiles(dir.FullName))
                {
                    var sub = new FileInfo(subname);
                    loading?.Invoke(sub);
                    if (sub.Extension.EndsWith(".xml"))
                    {
                        byte[] xml = FileSystemWorkSpace.ReadAllBytes(sub);
                        try
                        {
                            var nodeData = EditorRuntime.LoadXmlNode<T>(GameEditor.Codec, xml);
                            if (nodeData != null)
                            {
                                log.Info("Loaded XML node: " + nodeData);
                                loaded?.Invoke(sub, nodeData);
                                listdata.Add(nodeData);
                            }
                        }
                        catch (Exception err)
                        {
                            ShowErrorMessage(err);
                        }
                    }
                }
                alldatas.PutRange(listdata);
            }
        }
        protected virtual void SaveAllTableNodes<T>(DirectoryInfo dir, List<TemplateData> datas, IRangeValue progress, SavingAction saving, SavedAction saved)
        {
            if (!Directory.Exists(dir.FullName))
            {
                Directory.CreateDirectory(dir.FullName);
            }
            HashMap<int, string> savedMd5 = new HashMap<int, string>();
            HashMap<int, int> savedSize = new HashMap<int, int>();
            foreach (var data in datas)
            {
                AtomicSave(data);
            }
            // save md5
            StringBuilder sb = new StringBuilder();
            datas.Sort((a, b) => a.ID.CompareTo(b.ID));
            foreach (var sub in datas)
            {
                string md5 = savedMd5.Get(sub.ID);
                int size = savedSize.Get(sub.ID);
                sb.AppendLine(string.Format(string.Format("{0} : {1,12} : {2}", md5, size, sub.ID + ".xml")));
                log.Info("Save XML node: " + sub);
            }
            File.WriteAllText(dir + ListMd5Ext, sb.ToString(), UTF8);
            void AtomicSave(TemplateData sub)
            {
                var xmlpath = new FileInfo(dir + "/" + sub.ID + ".xml");
                saving?.Invoke(xmlpath, sub);
                {
                    var binpath = new FileInfo(xmlpath.FullName + ".bin");
                    byte[] bin = EditorRuntime.SaveBinNode(GameEditor.Codec, sub);
                    if (bin != null)
                    {
                        File.WriteAllBytes(binpath.FullName, bin);
                    }
                }
                byte[] xml_bin = XmlUtil.SaveTemplateXML(GameEditor.Codec, sub);
                var SavedXmlMD5 = CMD5.CalculateMD5(xml_bin);
                var SavedXmlLength = xml_bin.Length;
                FileSystemWorkSpace.WriteAllBytes(xmlpath, xml_bin);
                savedMd5.Put(sub.ID, SavedXmlMD5);
                savedSize.Put(sub.ID, SavedXmlLength);
                saved?.Invoke(xmlpath, sub);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------
        public class EditorCardRuntime : CardRuntimeAdapter
        {
            private SaveEditorRuntime runtime;
            public EditorCardRuntime(SaveEditorRuntime runtime)
            {
                this.runtime = runtime;
            }
            public override Logger Log => log;
            public override IReadOnlyCollection<CardTemplate> AllOriginCards => new List<CardTemplate>(runtime.alldatas.Cards.Values);
            public override IReadOnlyCollection<TemplateData> GetAllTemplatesData() => runtime.alldatas.AllTemplates();
            public override bool TryGetOriginCard(int tableName, out CardTemplate card)
            {
                return runtime.alldatas.Cards.TryGetValue(tableName, out card);
            }
            public override bool TryGetOriginTemplate(Type templateType, int templateID, out TemplateData temp)
            {
                return runtime.alldatas.TryGetTemplateData(templateType, templateID, out temp);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------
        public void SaveAll(Properties pargs, IRangeValue progress = null)
        {
            log.Info("--------------------------------------------------------");
            try
            {
                var deep = true;
                if (pargs.TryGetAsBool("deep", out var _deep))
                {
                    deep = _deep;
                }
                new EditorLoadDataTask().Run(GameEditor, progress);
                this.DataRoot = new EditorSaveDataTask().Run(GameEditor, new EditorCardRuntime(this), deep, alldatas, progress);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
            log.Info("--------------------------------------------------------");            
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
