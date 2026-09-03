using DeepCore;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.ZoneGeometry
{
    public interface IZone
    {
        Config CFG { get; }
        ITerrainSurface Terrain3D { get; }
        SingleThreadCollectionPool ObjectPool { get; }

        //bool TryTouchSpell(IZoneSpell spell, out Vector3 newNormal);

        sealed public bool IsHost { get => this is IHostZone; }
        sealed public TimeExpire AllocTimeExpire(double expireMS) => this.ObjectPool.AllocOrCreateAutoRelease<TimeExpire>(static t => new TimeExpire()).Init(expireMS);
    }


    public interface IHostZone : IZone
    {
        TimeTaskMS<ST> AddTimeTask<ST>(float intervalMS, float delayMS, int repeat, ST st, TickHandler<ST> handler);
    }


}
