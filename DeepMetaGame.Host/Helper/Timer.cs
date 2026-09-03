using DeepCore.Game3D.Host.Instance;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Host.Helper
{
    public class ZoneTimeInterval : TimeInterval
    {
        public InstanceZone Zone { get; private set; }
        public static ZoneTimeInterval Alloc(InstanceZone zone, float expireMS)
        {
            return zone.ObjectPool.AllocOrCreateAutoRelease<ZoneTimeInterval>(static t => new ZoneTimeInterval()).Init(zone, expireMS);
        }
        public ZoneTimeInterval(InstanceZone zone, float intervalMS) : base(intervalMS)
        {
            this.Zone = zone;
        }
        private ZoneTimeInterval() { }
        private ZoneTimeInterval Init(InstanceZone zone, float expireMS)
        {
            this.Zone = zone;
            base.Init(expireMS);
            return this;
        }
        public bool Update()
        {
            return base.Update(Zone.UpdateIntervalMS);
        }
    }

    public class ZoneTimeExpire : TimeExpire
    {
        public InstanceZone Zone { get; private set; }
        public static ZoneTimeExpire Alloc(InstanceZone zone, float expireMS)
        {
            return zone.ObjectPool.AllocOrCreateAutoRelease<ZoneTimeExpire>(static t=>new ZoneTimeExpire()).Init(zone, expireMS);
        }
        public ZoneTimeExpire(InstanceZone zone, float expireMS) : base(expireMS)
        {
            this.Zone = zone;
        }
        private ZoneTimeExpire() { }
        private ZoneTimeExpire Init(InstanceZone zone, float expireMS)
        {
            this.Zone = zone;
            base.Init(expireMS);
            return this;
        }
        public bool Update()
        {
            return base.Update(Zone.UpdateIntervalMS);
        }
    }
}
