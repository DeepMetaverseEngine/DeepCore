using DeepCore.Concurrent;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Slave;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor;
using Gate.Client.Battle;
using Gate.Data.Protocol;
using System;
using System.Threading.Tasks;

namespace Gate.Client
{
    [Reflectible]
    public class MMOClientManager : GateClientManager
    {
        new public static MMOClientManager Instance { get; private set; }
        public MMOClientManager()
        {
            Instance = this;
        }

        protected override async Task OnInitAsync(IRangeValue range)
        {
            await base.OnInitAsync(range);
            log.Info("CreateBattleManager");
            this.Battle = await CreateBattleManager(range);
        }
        protected override async Task OnInitEndAsync(IRangeValue p)
        {
            await base.OnInitEndAsync(p);
            log.Info("LoadBattleAsync");
            await this.LoadBattleAsync(p);
        }
        protected override void Disposing()
        {
            base.Disposing();
            Battle?.DataRoot?.Dispose();

        }
        #region Battle

        protected virtual Task<BattleManager> CreateBattleManager(IRangeValue range)
        {
            var mgr = new BattleManager();
            return Task.FromResult(mgr);
        }
        protected virtual async Task LoadBattleAsync(IRangeValue range)
        {
            await Battle.DataRoot.LoadAllTemplatesMetaAsync(range);
        }
        public BattleManager Battle { get; protected set; }
        public class BattleManager
        {
            public EditorTemplates DataRoot { get; set; }
            public BattleCodec Codec { get; set; }
            //             public ZoneSlaveFactory SlaveFactory { get; set; }
            //             public ZoneHostFactory HostFactory { get; set; }
            public BattleManager()
            {
                Init();
            }
            protected virtual void Init()
            {
//                 if (!string.IsNullOrEmpty(Instance.Config.BattleEditorDir))
//                 {
//                     ZoneDataFactory.GameEditorRoot = Instance.Config.BattleEditorDir;
//                 }
                if (!string.IsNullOrEmpty(Instance.Config.BattleCodec))
                {
                    if (ZoneDataFactory.Codec == null || !string.Equals(ZoneDataFactory.Codec.GetType().FullName, Instance.Config.BattleCodec, StringComparison.OrdinalIgnoreCase))
                    {
                        ZoneDataFactory.Codec = ReflectionUtil.CreateInterface<IExternalizableFactory>(Instance.Config.BattleCodec);
                    }
                }
                if (!string.IsNullOrEmpty(Instance.Config.BattleDataFactory))
                {
                    if (ZoneDataFactory.Factory == null || !string.Equals(ZoneDataFactory.Factory.GetType().FullName, Instance.Config.BattleDataFactory, StringComparison.OrdinalIgnoreCase))
                    {
                        ReflectionUtil.CreateInstance(Instance.Config.BattleDataFactory);
                    }
                }
                //                 if (!string.IsNullOrEmpty(Instance.Config.BattleSlaveFactory))
                //                 {
                //                     SlaveFactory = ReflectionUtil.CreateInstance(Instance.Config.BattleSlaveFactory) as ZoneSlaveFactory;
                //                 }
                //                 if (!string.IsNullOrEmpty(Instance.Config.BattleHostFactory))
                //                 {
                //                     HostFactory = ReflectionUtil.CreateInstance(Instance.Config.BattleHostFactory) as ZoneHostFactory;
                //                 }
                // if (!string.IsNullOrEmpty(Instance.Config.BattleEditorDir))
                {
                    Instance.log.Info("BattleManager CreateEditorTemplates");
                    this.DataRoot = ZoneDataFactory.Factory.CreateEditorTemplates($"{Instance.Config.BattleEditorDir}/data", Instance.Config.BattleIsClientModel);
                    Instance.log.Info("BattleManager SetLangProperties");
                    this.DataRoot.DataCenter.SetLangProperties(Instance.LangProperties);
                    Instance.log.Info("BattleManager BattleCodec");
                    this.Codec = new BattleCodec(DataRoot.Templates, true);
                }
            }
            public virtual GateBattle CreateBattle(GateClient client, ClientEnterZoneNotify sd)
            {
                return new GateBattle(client, null, sd);
            }
            public virtual void ReloadTemplates(bool force = false)
            {
                if (this.DataRoot != null && (!this.DataRoot.IsLoaded || force))
                {
                    Instance.log.Info("BattleManager ReloadTemplates");
                    this.DataRoot?.Flush(true);
                    this.DataRoot?.LoadAllTemplates();
                }
            }
            public virtual async Task ReloadTemplatesAsync(bool force = false)
            {
                if (this.DataRoot != null && (!this.DataRoot.IsLoaded || force))
                {
                    Instance.log.Info("BattleManager ReloadTemplatesAsync");
                    this.DataRoot.Flush(true);
                    await this.DataRoot.LoadAllTemplatesAsync();
                }
            }
        }
        #endregion
    }
}
