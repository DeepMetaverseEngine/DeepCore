using DeepCore;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Host.ZoneServer.Interface;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Voxel.Data;
using DeepCrystal.RPC;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using Gate.Server.Protocol;
using System;

namespace Gate.Server
{
    [Reflectible]
    public class MMOServerManager : GateServerManager
    {
        new public static MMOServerManager Instance { get; private set; }
        public MMOServerManager(GateServerConfig cfg) : base(cfg)
        {
            new SharedVoxelWorldManager();
            Instance = this;
        }
        protected override void OnInit()
        {
            base.OnInit();
            Battle = CreateBattleManager();
            this.LoadBattle();
        }
        protected override void OnInitEnd()
        {
            base.OnInitEnd();
        }
        protected override void Disposing()
        {
            base.Disposing();
            Battle.DataRoot.Dispose();
        }
        //-----------------------------------------------------------------------------------------------
        #region ServerName    
        protected override ServerNameManager CreateServerName() => new MMOServerNameManager();
        public static MMOServerNameManager MMOServerName { get; private set; }
        public class MMOServerNameManager : ServerNameManager
        {
            public const string AreaManagerType /*   */ = "AreaManager";
            public const string AreaServiceType /*   */ = "AreaService";

            public MMOServerNameManager()
            {
                MMOServerName = this;
            }

            public override void GetServiceMapping(Properties mappings)
            {
                base.GetServiceMapping(mappings);
                mappings.Put(ServerNameManager.LogicServiceType, typeof(Gate.Server.Service.Logic.MMOLogicService).FullName);
                mappings.Put(ServerNameManager.SessionServiceType, typeof(Gate.Server.Service.Session.MMOSessionService).FullName);
                mappings.Put(MMOServerNameManager.AreaManagerType, typeof(Gate.Server.Service.Area.AreaManager).FullName);
                mappings.Put(MMOServerNameManager.AreaServiceType, typeof(Gate.Server.Service.Area.AreaService).FullName);
            }

            public virtual RemoteAddress GetAreaService(string areaNumber, string svcNode = null)
            {
                return new RemoteAddress($"Area:{areaNumber}", svcNode, AreaServiceType);
            }
            public virtual RemoteAddress GetAreaManagerService(string svcNode = null)
            {
                return new RemoteAddress($"AreaManager", svcNode, AreaManagerType);
            }

        }
        #endregion
        //-----------------------------------------------------------------------------------------------
        #region Battle

        protected virtual BattleManager CreateBattleManager()
        {
            return new BattleManager();
        }
        protected virtual void LoadBattle()
        {
            //if (Battle.DataRoot.IsLoaded == false)
            {
                Battle.DataRoot.LoadAllTemplates(true);
            }
            //if (Battle.DataRoot.CacheScenesCount == 0)
            {
                Battle.DataRoot.CacheAllScenes();
            }
        }
        public static BattleManager Battle { get; private set; }
        public static EditorTemplates BattleDataRoot => Battle.DataRoot;
        public static TemplateManager BattleTemplates => BattleDataRoot.Templates;
        public class BattleManager
        {
            public EditorTemplates DataRoot { get; set; }
            public BattleCodec Codec { get; set; }
            //             public  ZoneSlaveFactory SlaveFactory { get; set; }
            //             public  ZoneHostFactory HostFactory { get; set; }
            public BattleManager()
            {
                Battle = this;
                Init();
            }
            protected virtual void Init()
            {
                if (!string.IsNullOrEmpty(Config.BattleEditorDir))
                {
                    //ZoneDataFactory.GameEditorRoot = Config.BattleEditorDir;
                }
                if (!string.IsNullOrEmpty(Config.BattleCodec))
                {
                    ZoneDataFactory.Codec = ReflectionUtil.CreateInterface<IExternalizableFactory>(Config.BattleCodec);
                }
                if (!string.IsNullOrEmpty(Config.BattleDataFactory))
                {
                    if (ZoneDataFactory.Factory == null || !string.Equals(ZoneDataFactory.Factory.GetType().FullName, Config.BattleDataFactory, StringComparison.OrdinalIgnoreCase))
                    {
                        ReflectionUtil.CreateInstance(Config.BattleDataFactory);
                    }
                }
                //                 if (!string.IsNullOrEmpty(Config.BattleHostFactory))
                //                 {
                //                     HostFactory = ReflectionUtil.CreateInterface<ZoneHostFactory>(Config.BattleHostFactory);
                //                 }
                //                 if (!string.IsNullOrEmpty(Config.BattleSlaveFactory))
                //                 {
                //                     SlaveFactory= ReflectionUtil.CreateInterface<ZoneSlaveFactory>(Config.BattleSlaveFactory);
                //                 }
                //if (!string.IsNullOrEmpty(Config.BattleEditorDir))
                {
                    this.DataRoot = ZoneDataFactory.Factory.CreateEditorTemplates($"{Config.BattleEditorDir}/data", false);
                    this.Codec = new BattleCodec(DataRoot.Templates, false);
                }
            }
            public virtual SceneData GetSceneAsCache(int templateID, bool clone = false)
            {
                return DataRoot?.LoadScene(templateID, true, false, clone);
            }
            public virtual ZoneNode CreateZoneNode(IZoneNodeServer server, CreateZoneNodeRequest input)
            {
                return null;
                // return hostFactory.CreateServerZoneNode(server, DataRoot);
            }
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------------
    }

}
