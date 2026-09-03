using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Runtime;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Host.ZoneRuntime
{
    public abstract class InstanceBattle : AbstractBattle
    {
        protected InstanceBattle(EditorTemplates datas, ZoneSlaveFactory slaveFactory) : base(datas, slaveFactory)
        {
        }

        public abstract event Action<InstanceBattle, InstanceZone> OnZoneStart;
        public abstract event Action<InstanceBattle, InstanceZone> OnCrateZone;
    }
}
