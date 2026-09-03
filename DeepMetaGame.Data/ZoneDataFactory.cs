using DeepCore;
using DeepCore.Concurrent;
using DeepCore.FuncData;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

namespace DeepMetaGame.Data
{
    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public interface IBattleFactory
    {
        ZoneDataFactory DataFactory { get; }
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
    [Reflectible]
    public abstract class ZoneDataFactory : IBattleFactory
    {
        protected static Logger log = new LazyLogger(nameof(ZoneDataFactory));
        public static ZoneDataFactory Factory { get; private set; }
        static ZoneDataFactory() { new SimpleDataFactory(); }
        //         private static HashMap<string, IBattleFactory> factoryMap = new HashMap<string, IBattleFactory>();
        //         public static T GetOrCreateFactory<T>() where T : IBattleFactory
        //         {
        // 
        // 
        //         }
        //         public static IBattleFactory GetOrCreateFactory(string typeString)
        //         {
        //             var type = ReflectionUtil.CreateInstance<IBattleFactory>(typeString);
        // 
        //         }
        //         public static IBattleFactory GetOrCreateFactory(string factory)
        //         {
        //             if (factoryMap.TryGetValue(typeof(T), out var))
        //             {
        // 
        //             }
        // 
        //                 if (factoryMap.TryGetValue(typeof(T), out var))
        // 
        //         }
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------

        //         public static bool SetClipboardTransform(string name, string text)
        //         {
        //             try
        //             {
        //                 string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.);
        // 
        //                 var root = typeof(ZoneDataFactory).Assembly.LocationDirectory();
        //                 CFiles.WriteAllText(Path.Combine(root.FullName, ".clipboard_transform", name), text);
        //                 return true;
        //             }
        //             catch (Exception err)
        //             {
        //                 //err.PrintStackTrace();
        //             }
        //             return false;
        //         }
        //         public static string GetClipboardTransform(string name)
        //         {
        //             try
        //             {
        //                 var root = typeof(ZoneDataFactory).Assembly.LocationDirectory();
        //                 return File.ReadAllText(Path.Combine(root.FullName, ".clipboard_transform", name));
        //             }
        //             catch (Exception err)
        //             {
        //                 //err.PrintStackTrace();
        //             }
        //             return null;
        //         }
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        ZoneDataFactory IBattleFactory.DataFactory => this;
        protected ZoneDataFactory()
        {
            Factory = this;
#if FALSE
            //用于数据升级，或者重构类结构，将原始XML数据格式，解析为新的结构，防止数据丢失
            XmlSerializer.AddFieldConverter((XmlSerializer ser, XmlElement xmlElement, object owner, object field, out object data) =>
            {
                if (owner is FuncTable.FuncFields fields && field is FieldInfo prop && prop.Name == "Fields")
                {
                    try
                    {
                        var oldMap = ser.DecodeFromXml<HashMap<string, int>>(xmlElement);
                        if (oldMap != null)
                        {
                            var newMap = new HashMap<string, FuncTable.FuncFieldIndex>();
                            foreach (var old in oldMap)
                            {
                                newMap.Add(old.Key, new FuncTable.FuncFieldIndex()
                                {
                                    Index = old.Value,
                                    IsExclude = old.Value < 0,
                                    IsInclude = old.Value >= 0,
                                    OP = FuncTable.FieldOperation.SET,
                                });
                            }
                            data = newMap;
                            return true;
                        }
                    }
                    catch { }
                }
                data = null;
                return false;
            });

            //             XmlSerializer.AddFieldConverter((XmlSerializer ser, XmlElement xmlElement, object owner, object field, out object data) =>
            //             {
            //                 if (owner is IFuncData && field is PropertyInfo prop && prop.Name == "FuncID")
            //                 {
            //                     var oldArray = ser.DecodeFromXml<int[]>(xmlElement);
            //                     if (oldArray != null)
            //                     {
            //                         data = new FuncTable() { FuncID = oldArray };
            //                         return true;
            //                     }
            //                 }
            //                 data = null;
            //                 return false;
            //             });
            XmlSerializer.AddFieldConverter((XmlSerializer ser, XmlElement xmlElement, object owner, object field, out object data) =>
            {
                if (owner is IFuncData && field is PropertyInfo prop && prop.Name == "FuncID")
                {
                    try
                    {
                        var oldArray = ser.DecodeFromXml<int[]>(xmlElement);
                        if (oldArray != null && oldArray.Length > 0)
                        {
                            data = new FuncTable()
                            {
                                FuncID = Array.ConvertAll(oldArray, id => new FuncTable.FuncFields()
                                {
                                    ID = id,
                                    Fields = new HashMap<string, int>(),
                                })
                            };
                            return true;
                        }
                    }
                    catch { }
                }
                data = null;
                return false;
            });
            XmlSerializer.AddFieldConverter((XmlSerializer ser, XmlElement xmlElement, object owner, object field, out object data) =>
            {
                if (owner is FuncTable table && field is FieldInfo prop && prop.Name == "FuncID")
                {
                    try
                    {
                        var oldArray = ser.DecodeFromXml<int[]>(xmlElement);
                        if (oldArray != null && oldArray.Length > 0)
                        {
                            data = Array.ConvertAll(oldArray, id => new FuncTable.FuncFields()
                            {
                                ID = id,
                                Fields = new HashMap<string, int>(),
                            });
                            return true;
                        }
                    }
                    catch { }
                }
                data = null;
                return false;
            });
#endif
            KeepPropertiesData();
        }
        public virtual EditorTemplates CreateEditorTemplates(string data_root, bool client_mode = false)
        {
            return new EditorTemplates(this, data_root, client_mode);
        }
        public virtual TemplateManager CreateTemplateManager(EditorTemplates root)
        {
            return new TemplateManager(root);
        }
        public virtual EditorDataCenter CreateDataCenter(EditorTemplates root)
        {
            return new EditorDataCenter(GetType().Name, $"{root.EditorRoot}/templates/templates_lua/");
        }
        public virtual WayPointAstar CreateWayPointAstar(SceneData data)
        {
            return new WayPointAstar(data);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------


        internal async Task BeginPluginsData(EditorTemplates data_root, IRangeValue progress)
        {
            if (event_OnBeginPluginsData != null) await event_OnBeginPluginsData.Invoke(data_root, progress);
        }
        internal async Task InitPluginsData(EditorTemplates data_root, IRangeValue progress)
        {
            if (event_OnInitPluginsData != null) await event_OnInitPluginsData.Invoke(data_root, progress);
        }

        //         //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //         #region FuncID
        // 
        //         protected abstract ZoneFuncDataAdapter InitLuaFactory();
        // 
        //         #endregion
        public virtual ITerrainWorld CreateVoxelWorld(object owner, EditorTemplates data_root, string voxelFileName, SceneData data, ZoneInfo zoneInfo)
        {
            var path = data_root.EditorRoot + voxelFileName;
            return TerrainFactory.Instance.GetOrCreateVoxelWorld(path, data);
        }
        public virtual ZoneSpaceTransverter CreateSpaceTransverter() => new ZoneSpaceTransverter.Space3D();
        public virtual ISpellMotion CreateSpellMotion(IZoneSpell spell) => spell.IsHost ? new HostSpellMotion() : new SlaveSpellMotion();
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region PropertiesData

        public virtual void KeepPropertiesData()
        {
            //用于数据升级，或者重构类结构，将原始XML数据格式，解析为新的结构，防止数据丢失
            XmlSerializer.AddFieldConverter((XmlSerializer ser, XmlElement xmlElement, object owner, object field, out object data, object root) =>
            {
                if (owner is IPropertiesOwner powner && field is FieldInfo prop)
                {
                    if (typeof(IPropertiesData).IsAssignableFrom(prop.FieldType))
                    {
                        try
                        {
                            var exist = prop.GetValue(owner);
                            var saved = ser.DecodeFromXml(xmlElement, exist?.GetType() ?? prop.FieldType, root);
                            if (saved != null && exist != null)
                            {
                                if (saved.GetType() != exist.GetType())
                                {
                                    log.Warn($"KeepPropertiesData : {saved} => {exist}");
                                    ReflectionUtil.TryChangeType(saved, exist.GetType(), prop.FieldType, out var dst);
                                    data = dst;
                                    return true;
                                }
                            }
                            data = saved;
                            return true;
                        }
                        catch { }
                    }
                }
                data = null;
                return false;
            });
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------


        public Type[] PropertiesType { get => propMap.Values.ToArray(); }
        public virtual ICommonConfig CreateCommonCFG()
        {
            if (propMap.TryGetValue(typeof(ICommonConfig), out var type))
            {
                try
                {
                    return (ICommonConfig)DeepActivator.CreateInstance(type);
                }
                catch
                {
                    log.Error($"Can Not Create Properties '{type}' With '{typeof(ICommonConfig)}'");
                    throw;
                }
            }
            else
            {
                log.Warn($"Can Not Create Common Config, No Registed Type : {typeof(ICommonConfig)}");
                return null;
            }
        }
        public virtual IGlobalConfig CreateGlobalCFG()
        {
            if (propMap.TryGetValue(typeof(IGlobalConfig), out var type))
            {
                try
                {
                    return (IGlobalConfig)DeepActivator.CreateInstance(type);
                }
                catch
                {
                    log.Error($"Can Not Create Properties '{type}' With '{typeof(IGlobalConfig)}'");
                    throw;
                }
            }
            else
            {
                log.Warn($"Can Not Create Common Config, No Registed Type : {typeof(IGlobalConfig)}");
                return null;
            }
        }
        public virtual IResourceProperties CreateResourceProperties()
        {
            if (propMap.TryGetValue(typeof(IResourceProperties), out var type))
            {
                try
                {
                    return (IResourceProperties)DeepActivator.CreateInstance(type);
                }
                catch
                {
                    log.Error($"Can Not Create Properties '{type}' With '{typeof(IResourceProperties)}'");
                    throw;
                }
            }
            else
            {
                //log.Warn($"Can Not Create Common Config, No Registed Type : {typeof(IResourceProperties)}");
                return null;
            }
        }
        protected virtual IPropertiesData CreateProperties(IPropertiesOwner owner, Type type)
        {
            try
            {
                return (IPropertiesData)DeepActivator.CreateInstance(type);
            }
            catch
            {
                log.Error($"Can Not Create Properties '{type}' With '{owner?.GetType()}'");
                throw;
            }
        }
        public T CreateProperties<T>(IPropertiesOwner owner) where T : class, IPropertiesData
        {
            if (propMap.TryGetValue(typeof(T), out var type))
            {
                var ret = CreateProperties(owner, type);
                if (ret is T t)
                {
                    return t;
                }
            }
            log.Warn($"Can Not Create Properties '{typeof(T)}' With '{owner?.GetType()}'");
            return default;
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private static Type[] basic_property_type = [
             typeof(IGlobalConfig),
             typeof(ICommonConfig),
             typeof(ISceneProperties),
             typeof(IUnitProperties),
             typeof(IItemProperties),
             typeof(ISkillProperties),
             typeof(ISpellProperties),
             typeof(IBuffProperties),
             typeof(IAuraProperties),
             typeof(ICardProperties),
             typeof(IEventProperties),
             typeof(IAttackProperties),
             typeof(IEffectProperties),
             typeof(IKeyFrameProperties),
             typeof(IResourceProperties),
        ];
        private ListDictionary<Type, Type> propMap = new ListDictionary<Type, Type>();
        public void RegistPropertiesType(params Type[] types)
        {
            RegistPropertiesTypes(types);
        }
        public void RegistPropertiesTypes(IEnumerable<Type> types)
        {
            var list = ReflectionUtil.GetNoneVirtualTypes(types);
            foreach (var type in list)
            {
                if (typeof(IPropertiesData).IsAssignableFrom(type))
                {
                    try
                    {
                        foreach (var basic in basic_property_type)
                        {
                            if (basic.IsAssignableFrom(type)) { Put(basic, type); }
                        }

                    }
                    catch
                    {
                        log.Error($"Can Not Regist Properties '{type}'");
                        throw;
                    }
                }
            }
            void Put(Type a, Type type)
            {
                if (propMap.TryGetValue(a, out var old))
                {
                    log.Warn($"Override New Properties Type : {type} => {old}");
                }
                else
                {
                    // log.Info($"Regist Properties Type : {type}");
                }
                propMap.Put(a, type);
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Codec
        private static IExternalizableFactory codec;
        private IExternalizableFactory msg_codec;
        private IExternalizableFactory save_codec;
        public static IExternalizableFactory Codec
        {
            get { return codec; }
            set { codec = value; }
        }
        public IExternalizableFactory MessageCodec
        {
            get
            {
                if (msg_codec == null)
                {
                    var c = new WarpExternalizableFactory(Codec);
                    c.UseVLQ = true;
                    c.IsConsistency = false;
                    msg_codec = c;
                }
                return msg_codec;
            }
        }
        public IExternalizableFactory PersistCodec
        {
            get
            {
                if (save_codec == null)
                {
                    var c = new WarpExternalizableFactory(Codec);
                    c.UseVLQ = false;
                    c.IsConsistency = true;
                    save_codec = c;
                }
                return save_codec;
            }
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------

        //public bool IsEditorMode { get; private set; } = false;
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Events

        public void EditorInit(DirectoryInfo editor_root)
        {
            //IsEditorMode = true;
            TemplateManager.IsEditor = true;
            EnvironmentVar.ALWAYS_SYNC_ENVIRONMENT_VAR = true;
            event_OnEditorInit?.Invoke(editor_root);
        }
        public void EditorSaving(EditorTemplatesData datas, DirectoryInfo data_root, bool check)
        {
            event_OnEditorSaving?.Invoke(datas, data_root, check);
        }
        public void EditorPluginSaved(EditorTemplatesData datas, DirectoryInfo data_root, bool check)
        {
            event_OnEditorPluginSaved?.Invoke(datas, data_root, check);
        }
        public void EditorSavingSceneData(EditorTemplatesData datas, SceneData scene, DirectoryInfo data_root)
        {
            //             if (datas.CFG.SPACE_OVERRIDE_SPACE_DIV_SIZE > 1)
            //             {
            //                 scene.SpaceDivW = datas.CFG.SPACE_OVERRIDE_SPACE_DIV_SIZE;
            //             }
            event_OnEditorSavingSceneData?.Invoke(datas, scene, data_root);
        }
        public void EditorSavedSceneData(EditorTemplatesData datas, SceneData scene, DirectoryInfo data_root)
        {
            event_OnEditorSavedSceneData?.Invoke(datas, scene, data_root);
        }
        public void EditorCheckExistDatas(EditorTemplatesData datas, DirectoryInfo data_root)
        {
            event_OnEditorCheckExistDatas?.Invoke(datas, data_root);
        }

        private OnInitPluginsDataHandler event_OnBeginPluginsData;
        private OnInitPluginsDataHandler event_OnInitPluginsData;
        private OnEditorInitHandler event_OnEditorInit;
        private OnEditorSavingHandler event_OnEditorSaving;
        private OnEditorPluginSavedHandler event_OnEditorPluginSaved;
        private OnEditorSavingSceneDataHandler event_OnEditorSavingSceneData;
        private OnEditorSavedSceneDataHandler event_OnEditorSavedSceneData;
        private OnEditorCheckExistDatasHandler event_OnEditorCheckExistDatas;

        public event OnInitPluginsDataHandler OnBeginPluginsData { add { event_OnBeginPluginsData += value; } remove { event_OnBeginPluginsData -= value; } }
        public event OnInitPluginsDataHandler OnInitPluginsData { add { event_OnInitPluginsData += value; } remove { event_OnInitPluginsData -= value; } }
        public event OnEditorInitHandler OnEditorInit { add { event_OnEditorInit += value; } remove { event_OnEditorInit -= value; } }
        public event OnEditorSavingHandler OnEditorSaving { add { event_OnEditorSaving += value; } remove { event_OnEditorSaving -= value; } }
        public event OnEditorPluginSavedHandler OnEditorPluginSaved { add { event_OnEditorPluginSaved += value; } remove { event_OnEditorPluginSaved -= value; } }
        public event OnEditorSavingSceneDataHandler OnEditorSavingSceneData { add { event_OnEditorSavingSceneData += value; } remove { event_OnEditorSavingSceneData -= value; } }
        public event OnEditorSavedSceneDataHandler OnEditorSavedSceneData { add { event_OnEditorSavedSceneData += value; } remove { event_OnEditorSavedSceneData -= value; } }
        public event OnEditorCheckExistDatasHandler OnEditorCheckExistDatas { add { event_OnEditorCheckExistDatas += value; } remove { event_OnEditorCheckExistDatas -= value; } }


        /// <summary>
        /// 游戏加载初始化
        /// </summary>
        public delegate Task OnInitPluginsDataHandler(EditorTemplates data_root, IRangeValue progress);

        /// <summary>
        /// 【编辑器】初始化
        /// </summary>
        public delegate void OnEditorInitHandler(DirectoryInfo editor_root);

        /// <summary>
        /// 【编辑器】预处理编辑器插件
        /// </summary>
        public delegate void OnEditorSavingHandler(EditorTemplatesData datas, DirectoryInfo data_root, bool check);
        /// <summary>
        /// 【编辑器】基础数据保存完毕时
        /// </summary>
        public delegate void OnEditorPluginSavedHandler(EditorTemplatesData datas, DirectoryInfo data_root, bool check);
        /// <summary>
        /// 【编辑器】当场景存储时
        /// </summary>
        /// <returns></returns>
        public delegate void OnEditorSavingSceneDataHandler(EditorTemplatesData datas, SceneData scene, DirectoryInfo data_root);
        /// <summary>
        /// 【编辑器】当场景已存储
        /// </summary>
        /// <returns></returns>
        public delegate void OnEditorSavedSceneDataHandler(EditorTemplatesData datas, SceneData scene, DirectoryInfo data_root);

        /// <summary>
        /// 【编辑器】检查数据完整性
        /// </summary>
        public delegate void OnEditorCheckExistDatasHandler(EditorTemplatesData datas, DirectoryInfo data_root);

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
    //---------------------------------------------------------------------------------------------
    [Reflectible] public interface IGlobalConfig : IPropertiesData { }
    [Reflectible] public interface ICommonConfig : IPropertiesData { }
    [Reflectible] public interface IUnitProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface ISkillProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface ISpellProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface IAuraProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface IBuffProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface ICardProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface IEventProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface IItemProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface ISceneProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface IAttackProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface IEffectProperties : IPropertiesData, IFuncData { }
    [Reflectible] public interface IKeyFrameProperties : IPropertiesData, IFuncData { }
    //---------------------------------------------------------------------------------------------
    [Reflectible]
    public interface IResourceProperties : IPropertiesData, IFuncData
    {
    }

    //---------------------------------------------------------------------------------------------

    [Reflectible]
    public interface IZoneEventData : ISerializable, DeepCore.EventTrigger.IEventData
    {

    }
    [Reflectible]
    public interface IZoneEnvironmentVar : ISerializable, DeepCore.EventTrigger.IEnvironmentVar
    {
    }


    //---------------------------------------------------------------------------------------------
    public static class ActionDefineExt
    {
        public static bool IsMoving(this UnitActionStatus st) => ActionDefine.Instance.IsMoving(st);
        public static bool IsMoveable(this UnitActionStatus st) => ActionDefine.Instance.IsMoveable(st);
        public static bool IsControlMoveable(this UnitActionStatus st) => ActionDefine.Instance.IsControlMoveable(st);
        public static bool IsControllable(this UnitActionStatus st) => ActionDefine.Instance.IsControllable(st);
        public static bool NotControllable(this UnitActionStatus st) => ActionDefine.Instance.NotControllable(st);
        public static bool IsCanLaunchSkill(this UnitActionStatus st) => ActionDefine.Instance.IsCanLaunchSkill(st);
    }

    public class ActionDefine
    {
        public static ActionDefine Instance { get; private set; } = new ActionDefine();
        public ActionDefine()
        {
            Instance = this;
        }
        public virtual UnitActionStatus GetStartMoveStatus(UnitInfo unit, UnitMotionAbility motion, float moveSpeedSEC)
        {
            if (motion == null)
            {
                return UnitActionStatus.Move;
            }
            if (moveSpeedSEC < motion.MoveSpeedSEC / 2f)
            {
                return UnitActionStatus.Walk;
            }
            return UnitActionStatus.Move;
        }
        public virtual bool IsLoop(UnitActionStatus st)
        {
            if (st == UnitActionStatus.Move ||
                st == UnitActionStatus.Walk ||
                st == UnitActionStatus.Chaos ||
                st == UnitActionStatus.Escape ||
                st == UnitActionStatus.Climb ||
                st == UnitActionStatus.Ride ||
                st == UnitActionStatus.Swim ||
                st == UnitActionStatus.Idle)
            {
                return true;
            }
            return false;
        }
        public virtual bool IsMoving(UnitActionStatus st)
        {
            if (st == UnitActionStatus.Move ||
                st == UnitActionStatus.Walk)
            {
                return true;
            }
            return false;
        }
        public virtual bool IsMoveable(UnitActionStatus st)
        {
            if (st == UnitActionStatus.Move ||
                st == UnitActionStatus.Walk ||
                st == UnitActionStatus.Jump ||
                st == UnitActionStatus.Chaos ||
                st == UnitActionStatus.Escape ||
                st == UnitActionStatus.Climb ||
                st == UnitActionStatus.Ride ||
                st == UnitActionStatus.Swim)
            {
                return true;
            }
            return false;
        }
        public virtual bool IsControlMoveable(UnitActionStatus st)
        {
            if (st == UnitActionStatus.Move ||
                st == UnitActionStatus.Walk ||
                st == UnitActionStatus.Jump)
            {
                return true;
            }
            return false;
        }
        public virtual bool IsControllable(UnitActionStatus st)
        {
            switch (st)
            {
                case UnitActionStatus.Idle:
                case UnitActionStatus.Move:
                case UnitActionStatus.Walk:
                case UnitActionStatus.Jump:
                case UnitActionStatus.Pick:
                case UnitActionStatus.Climb:
                case UnitActionStatus.Swim:
                case UnitActionStatus.ClientCustom:
                case UnitActionStatus.Somersault:
                    return true;
            }
            return false;
        }
        public virtual bool NotControllable(UnitActionStatus st)
        {
            return !st.IsControllable();
        }
        public virtual bool IsCanLaunchSkill(UnitActionStatus st)
        {
            switch (st)
            {
                case UnitActionStatus.Idle:
                case UnitActionStatus.Move:
                case UnitActionStatus.Walk:
                case UnitActionStatus.Pick:
                case UnitActionStatus.ClientCustom:
                case UnitActionStatus.Skill:
                    return true;
            }
            return false;
        }
    }
}
