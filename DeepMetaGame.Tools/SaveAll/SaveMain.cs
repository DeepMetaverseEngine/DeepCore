using DeepCore;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Slave;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCore.Reflection;
using DeepCore.Template.NewtonJson;
using DeepCore.Voxel.Data;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepMetaGame.Tools.SaveAll
{
    public abstract class SaveMain
    {
        public IExternalizableFactory Codec { get; private set; }
        public ZoneDataFactory DataFactory { get; private set; }
        public ZoneHostFactory HostFactory { get; private set; }
        public ZoneSlaveFactory SlaveFactory { get; private set; }
        public MessageCodeManager CodeManager { get; private set; }
        public EditorTemplates DataRoot { get; private set; }
        public Logger log { get; } = new LazyLogger(typeof(SaveMain));
        //------------------------------------------------------------------------------------------------

        protected virtual DeepActivator CreateReflections()
        {
            return null;
        }
        protected abstract IExternalizableFactory CreateCodec();
        protected abstract ZoneDataFactory CreateDataFactory();
        protected abstract ZoneHostFactory CreateHostFactory();
        protected abstract ZoneSlaveFactory CreateSlaveFactory();
        protected virtual SaveEditorRuntime CreateSaveEditor(DirectoryInfo editor_root)
        {
            return new SaveEditorRuntime(new DirectoryInfo($"{editor_root}/data"));
        }
        //------------------------------------------------------------------------------------------------
        public int Main(DirectoryInfo editor_root, params string[] args)
        {
            log.Info("--------------------------------------------------------");       
            this.CreateReflections();
            try
            {
                VoxelWorldManager.Instance.ToString();
                this.Codec = ZoneDataFactory.Codec = this.CreateCodec();
                TemplateDataCenter.ENABLE_LOAD_FROM_BIN = false;
                new NewtonJsonTemplateLoader(true);
                this.DataFactory = this.CreateDataFactory();
                this.SlaveFactory = this.CreateSlaveFactory();
                this.HostFactory = this.CreateHostFactory();
                this.CodeManager = new MessageCodeManager(ZoneDataFactory.Codec);
                var root = editor_root;
                {
                    var save = this.CreateSaveEditor(editor_root);
                    var pargs = Properties.ParseArgs(args, "=");
                    if (args.Length > 0)
                    {
                        switch (args[0].ToLower().Trim())
                        {
                            case "xls2lang":
                                save.GenXLSToLangCSV();
                                return 0;
                            case "lang2prop":
                                save.GenLangProperties();
                                return 0;
                            case "xls":
                                save.BakeXLS2Json();
                                return 0;
                        }
                    }
                    save.SaveAll(pargs);
                    this.DataRoot = save.DataRoot;
                    this.OnFinished(save);
                    this.OnDone?.Invoke(save);
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
                log.Warn(Usage);
                System.Environment.Exit(-1);
                return -1;
            }
            log.Info("数据烘焙完成。");
            log.Info("--------------------------------------------------------");
            log.PushColor();
            log.Color = ConsoleColor.Cyan;
            log.Info(Usage);
            log.PopColor();
            return 0;
        }

        //------------------------------------------------------------------------------------------------
        protected virtual void OnFinished(SaveEditorRuntime save)
        {
            // Override this method to handle the completion of the save process.
        }
        public event Action<SaveEditorRuntime> OnDone;
        public static string Usage
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine("Usage:");
                sb.AppendLine("  执行自动加载编辑器数据并保存，烘焙所有XLS表格。");
                sb.AppendLine("可选额外操作命令:");
                sb.AppendLine("  xls       : 只将所有XLS转换为Json或Lua。");
                sb.AppendLine("  xls2lang  : 从XLS表格抽取语言数据，输出到CSV文件，供策划维护使用。");
                sb.AppendLine("  lang2prop : 从XLS表格抽取语言数据，输出到Properties文件，供游戏内加载使用。");
                return sb.ToString().Trim();
            }
        }
        //------------------------------------------------------------------------------------------------
    }
    //------------------------------------------------------------------------------------------------
    public class SaveMain<ACTIVITOR, CODEC, DATA, HOST, SLAVE, DC> : SaveMain
        where ACTIVITOR : DeepActivator, new()
        where CODEC : class, IExternalizableFactory, new()
        where DATA : ZoneDataFactory, new()
        where HOST : ZoneHostFactory, new()
        where SLAVE : ZoneSlaveFactory, new()
        where DC: EditorDataCenter
    {
        new public CODEC Codec => base.Codec as CODEC;
        new public DATA DataFactory => base.DataFactory as DATA;
        new public HOST HostFactory => base.HostFactory as HOST;
        new public SLAVE SlaveFactory => base.SlaveFactory as SLAVE;
        public DC DataCenter => base.DataRoot?.DataCenter as DC;
        protected override DeepActivator CreateReflections() => new ACTIVITOR();
        protected override IExternalizableFactory CreateCodec() => new CODEC();
        protected override ZoneDataFactory CreateDataFactory() => new DATA();
        protected override ZoneHostFactory CreateHostFactory() => new HOST();
        protected override ZoneSlaveFactory CreateSlaveFactory() => new SLAVE();

    }
}
