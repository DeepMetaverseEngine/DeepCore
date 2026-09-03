using DeepCore;
using DeepCore.Concurrent;
using DeepCore.Log;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.ZoneEditor
{
    public class EditorDataCenter : TemplateDataCenter
    {
        public EditorDataCenter(string name, string templateRootDir) : base(ZoneDataFactory.Factory.PersistCodec, name, templateRootDir)
        {
        }
        protected internal virtual void OnEditorTemplatesLoad(EditorTemplates templates, IRangeValue progress = null) { }
        //---------------------------------------------------------------------------------------------
        //private HashMap<Type, TableBase> Tables = new HashMap<Type, TableBase>();
//         public TableBase<K, T> RegistTable<K, T>(string xlsFile, string keyField) where T : new()
//         {
//             var manager = (Listen<K, T>(xlsFile, keyField));
//             //       RegistTable(typeof(T), manager);
//             return manager;
//         }
//         public TableBase<K, T> RegistTable<K, T>(string xlsFile) where T : new()
//         {
//             var manager = (Listen<K, T>(xlsFile, null as string));
//             //          RegistTable(typeof(T), manager);
//             return manager;
//         }
//         public TableBase<K, T> RegistTableSheet<K, T>(string xlsFile, string sheetName, string keyField) where T : new()
//         {
//             var manager = (ListenSheet<K, T>(xlsFile, sheetName, keyField));
//             //           RegistTable(typeof(T), manager);
//             return manager;
//         }
//         public TableBase<K, T> RegistTableSheet<K, T>(string xlsFile, string sheetName) where T : new()
//         {
//             var manager = (ListenSheet<K, T>(xlsFile, sheetName, null));
//             //            RegistTable(typeof(T), manager);
//             return manager;
//         }
        //         public virtual void RegistTable(Type type, TableBase table)
        //         {
        //             Tables.Add(type, table);
        //         }
        //         public virtual void CleanTables()
        //         {
        //             foreach (var t in Tables.Values)
        //             {
        //                 try
        //                 {
        //                     t.Dispose();
        //                 }
        //                 catch { }
        //             }
        //             Tables.Clear();
        //             base.Cleanup();
        //         }
        //---------------------------------------------------------------------------------------------
        public virtual long GetUnitNeedExp(SceneData zone, UnitInfo unit, int level)
        {
            var exp = 0L;
            for (int lv = 0; lv < level; lv++)
            {
                exp = (long)(exp + (100 * (lv)));
            }
            return exp;
        }
        //---------------------------------------------------------------------------------------------
    }

    //----------------------------------------------------------------------------------------------------

    //     public abstract class TableBase : Disposable
    //     {
    //         public CacheData CacheData;
    //         public TableBase(CacheData c)
    //         {
    //             CacheData = c;
    //         }
    //     }
    // 
    //     //----------------------------------------------------------------------------------------------------
    // 
    //     public class TableBase<K, T> : TableBase where T : new()
    //     {
    //         public CacheData<K, T> Cache { get; }
    //         public IReadOnlyDictionary<K, T> TemplatesMap => Cache.TemplatesMap;
    //         public IReadOnlyList<T> TemplatesList => Cache.TemplatesList;
    //         public TableBase(CacheData<K, T> cache) : base(cache)
    //         {
    //             Cache = cache;
    //         }
    //         protected override void Disposing()
    //         {
    // 
    //         }
    //     }

    //----------------------------------------------------------------------------------------------------
}
