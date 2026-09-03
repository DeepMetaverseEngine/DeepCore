using DeepCore;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Slave;
using DeepMetaGame.Data;

namespace Gate.Server.Launcher
{

    public class MMOMainLoop : GateMainLoop
    {
        public string BattleRoot;
        public Type BattleCodec = ZoneDataFactory.Codec?.GetType();
        public Type BattleDataFactory = ZoneDataFactory.Factory?.GetType();
//         public Type BattleHostFactory = HostFactory?.GetType();
//         public Type BattleSlaveFactory = SlaveFactory?.GetType();

        public MMOMainLoop() : base(new GateSingleNodeLauncher())
        {
        }

        public override void MainLoopWithProperties(Properties _pargs)
        {
            {
                this.ServerConfig.BattleEditorDir = BattleRoot;
                // Already init with properties
                if (ZoneDataFactory.Codec == null)
                {
                    this.ServerConfig.BattleCodec = BattleCodec.FullName;
                    this.ServerConfig.BattleDataFactory = BattleDataFactory.FullName;
//                     this.ServerConfig.BattleHostFactory = BattleHostFactory.FullName;
//                     this.ServerConfig.BattleSlaveFactory = BattleSlaveFactory.FullName;
                }
            }
            {
                this.ClientConfig.BattleEditorDir = this.ServerConfig.BattleEditorDir;
                // Already init with server factory
                if (ZoneDataFactory.Codec == null)
                {
                    this.ClientConfig.BattleCodec = this.ServerConfig.BattleCodec;
                    this.ClientConfig.BattleDataFactory = this.ServerConfig.BattleDataFactory;
//                     this.ClientConfig.BattleHostFactory = this.ServerConfig.BattleHostFactory;
//                     this.ClientConfig.BattleSlaveFactory = this.ServerConfig.BattleSlaveFactory;
                }
            }
            base.MainLoopWithProperties(_pargs);
        }



    }
}
