using DeepCore;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.Game3D.Slave;
using DeepCore.Log;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneEditor;

namespace DeepMetaGame.Tools.BattleEmulator
{
    public class HostEmulator
    {
        public Logger log { get; }
        public bool IsFixedUpdate { get; set; } = false;
        //------------------------------------------------------------------------------------------------
        public ZoneHostFactory HostFactory { get; }
        public ZoneSlaveFactory SlaveFactory { get; }
        public EditorTemplates DataRoot { get; }
        public InstanceBattle Battle { get; }
        public Properties Args { get; }
        //------------------------------------------------------------------------------------------------
        public HostEmulator(ZoneHostFactory host, ZoneSlaveFactory slave, EditorTemplates dataroot, SceneData scene, Properties pargs)
            : this(host, slave, dataroot, pargs, new LocalBattleSinglePlay(dataroot, host, slave, scene,
                      pargs.GetStructAs<int>("-force"),
                      pargs.GetStructAs<int>("-actorTemplateID")))
        {
        }
        public HostEmulator(ZoneHostFactory host, ZoneSlaveFactory slave, EditorTemplates dataroot, Properties pargs, InstanceBattle battle)
        {
            this.log = new LazyLogger("Emu:" + battle);
            this.Args = pargs;
            this.DataRoot = dataroot;
            this.HostFactory = host;
            this.SlaveFactory = slave;
            this.Battle = battle;
            this.Battle.Layer.GameOver += Layer_GameOver;
            this.Battle.Start();
            this.mFixedUpdateInterval = 1000f / dataroot.Templates.DefaultConfig.SYSTEM_FPS;
            this.mLastUpdateTime = CUtils.TickTimeMS;
            this.mIsRunning = true;
        }
        protected virtual void Layer_GameOver(DeepCore.Game3D.Slave.Layer.LayerZone layer, int winForce, string msg)
        {
            this.Exit();
        }
        //------------------------------------------------------------------------------------------------
        private bool mIsRunning = false;
        private float mFixedUpdateInterval;
        private double mLastUpdateTime;
        private double mLastUsedTime;
        public void MainLoop()
        {
            try
            {
                while (mIsRunning)
                {
                    Update(DeepCore.CUtils.TickTimeMS);
                    var delay = (float)(mFixedUpdateInterval - mLastUsedTime);
                    if (delay > 0)
                    {
                        Thread.Sleep((int)delay);
                    }
                    else
                    {
                        Thread.Sleep(0);
                    }
                }
                Battle.Dispose();
            }
            catch (Exception err)
            {
                log.Error(err);
            }
        }
        public void Update(double currentTime)
        {
            var curTime = currentTime;
            try
            {
                if (mLastUpdateTime == 0)
                {
                    mLastUpdateTime = curTime;
                }
                var intervalMS = (float)(curTime - mLastUpdateTime);
                var intervalLimit = (mFixedUpdateInterval * 2);
                this.mLastUpdateTime = curTime;
                try
                {
                    intervalMS = IsFixedUpdate ? mFixedUpdateInterval : Math.Min(intervalMS, intervalLimit);
                    Battle.BeginUpdate(intervalMS);
                    Battle.Update();
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            finally
            {
                this.mLastUsedTime = DeepCore.CUtils.TickTimeMS - curTime;
            }
        }
        public void Exit()
        {
            this.mIsRunning = false;
        }
        //------------------------------------------------------------------------------------------------
    }
}
