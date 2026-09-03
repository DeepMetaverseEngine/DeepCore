using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Host.ZonePreview;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Layer;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Host.ZoneRuntime
{
    public class PreviewBattle : LocalBattle
    {
        protected readonly SceneData data;
        public PreviewBattle(EditorTemplates datas, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory, SceneData sd) : base(datas, hostFactory, slaveFactory)
        {
            this.data = sd;
        }
        protected override EditorScene CreateZone()
        {
            var zone = base.HostFactory.CreateZone(this, DataRoot, data);
            zone.Components.AddComponent<ZonePreviewComponent>();
            return zone;
        }
        protected override void Layer_MessageReceived(LayerZone layer, IBattleMessage msg)
        {
            if (msg is ZonePauseNotify pause)
            {
                return;
            }
            base.Layer_MessageReceived(layer, msg);
        }
    }
    public class PreviewBattle<P> : PreviewBattle where P : ZonePreviewComponent
    {
        public PreviewBattle(EditorTemplates datas, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory, SceneData sd) 
            : base(datas, hostFactory, slaveFactory, sd)
        {
        }
        protected override EditorScene CreateZone()
        {
            var zone = base.HostFactory.CreateZone(this, DataRoot, data);
            zone.Components.AddComponent<P>();
            return zone;
        }
    }
}
