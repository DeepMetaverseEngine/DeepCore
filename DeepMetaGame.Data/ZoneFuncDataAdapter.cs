using DeepCore.FuncData;
using DeepCore.Lua;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeepMetaGame.Data
{
    [Reflectible]
    public class ZoneFuncDataAdapter
    {
        public string LuaFuncDataSuffix { get; }
        public ZoneFuncDataAdapter(ZoneDataFactory factory, ILuaAdapter luaAdapter, string luaDataSuffix)
        {
            LuaFuncDataSuffix = luaDataSuffix;
            new LuaFuncDataManager(TemplateManager.DataFactory.PersistCodec, new LuaTemplateLoader(false, luaAdapter));
            factory.OnInitPluginsData += SDataFactory_OnInitPluginsData;
            factory.OnEditorInit += SDataFactory_OnEditorInit;
            factory.OnEditorPluginSaved += SDataFactory_OnEditorPluginSaved;
            factory.OnEditorSaving += SDataFactory_OnEditorSaving;
        }
        /// <summary>
        /// 编辑器初始化加载数据源
        /// </summary>
        /// <param name="editor_root"></param>
        protected virtual void SDataFactory_OnEditorInit(DirectoryInfo editor_root)
        {
            FuncDataManager.Instance.LoadAllTemplates(editor_root.FullName + LuaFuncDataSuffix);
        }
        /// <summary>
        /// 数据初始化加载Func数据源
        /// </summary>
        /// <param name="data_root"></param>
        protected virtual Task SDataFactory_OnInitPluginsData(EditorTemplates data_root)
        {
            FuncDataManager.Instance.LoadAffects(data_root.DataRoot + "/func_affect.xml");
            FuncDataManager.Instance.LoadAllTemplates(data_root.EditorRoot + LuaFuncDataSuffix);
            return Task.CompletedTask;
        }
        protected virtual void SDataFactory_OnEditorSaving(EditorTemplatesData datas, DirectoryInfo data_root, bool check)
        {
            foreach (var e in datas.AllTemplates(true))
            {
                FuncDataManager.Instance.FillFromFuncID(e);
            }
        }
        protected virtual void SDataFactory_OnEditorPluginSaved(EditorTemplatesData datas, DirectoryInfo data_root, bool check)
        {
            FuncDataManager.Instance.SavingFromEditor(datas.AllTemplates(true), new FileInfo(data_root + "/func_affect.xml"));
        }
    }
}
