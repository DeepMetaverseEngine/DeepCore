using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Helper;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class WayPointAstarZoneComponent : ZoneComponent
    {
        private WayPointAstar waypoint_path;
        public WayPointAstar FlagAstar { get => waypoint_path; }
        protected override void OnAdded()
        {
            base.OnAdded();
            this.waypoint_path = ZoneDataFactory.Factory.CreateWayPointAstar(this.Zone.Data);
        }


    }



    public class ZoneLocalPlayComponent : ZoneComponent
    {
        public int? force = null;
        public int? actorTemplateID = null;
        public InstancePlayer ActorPlayer { get; protected set; }
        protected override void OnAdded()
        {
            base.OnAdded();
            this.Zone.QueueTask(this, static (t, z) =>
            {
                z.DoAddLocalPlayer(z.Zone);
            });
        }
        protected override void OnDispose(InstanceZone owner)
        {
            base.OnDispose(owner);
            this.ActorPlayer = null;
        }
        protected virtual InstancePlayer DoAddLocalPlayer(InstanceZone zone)
        {
            var actor = Zone.InitPlayerStartRegions(force, actorTemplateID);
            if (actor != null)
            {
                OnAddLocalPlayer(actor);
            }
            return actor;
        }
        protected virtual void OnAddLocalPlayer(InstancePlayer actor)
        {
            this.ActorPlayer = actor;
            var zone = actor.Parent;
            var loc = actor.AllocLockActorEvent(actor.Name,
                      zone.CFG.CLIENT_SYNC_UNIT_MIN_RANGE,
                      zone.CFG.CLIENT_SYNC_UNIT_MAX_RANGE,
                      1000 / zone.CFG.SYSTEM_FPS);
            zone.PostEvent(loc);
        }
    }
}
