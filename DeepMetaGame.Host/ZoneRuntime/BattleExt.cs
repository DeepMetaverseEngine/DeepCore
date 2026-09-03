using DeepCore.Game3D.Host.Instance;
using DeepCore.IO;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DeepCore.Game3D.Host.ZoneRuntime
{
    public static class BattleExt
    {
        public static TAddUnit? TryAddPlayer(this InstanceZone mZone, RegionData startRegion, PlayerStartAbilityData tgd, int? actorTemplateID = null)
        {
            var zone = mZone;
            var info = mZone.Templates.GetUnit(tgd.TestActorTemplateID);
            if (actorTemplateID.HasValue)
            {
                var ainfo = mZone.Templates.GetUnit(actorTemplateID.Value);
                if (ainfo != null)
                {
                    info = ainfo;
                }
            }
            if (info != null)
            {
                var pcount = mZone.AllPlayersCount;
                info = mZone.CloneData(info);
                info.UType = UnitType.TYPE_PLAYER;
                var name = pcount == 0 ? tgd.Name : tgd.Name + pcount;
                return new TAddUnit()
                {
                    info = info,
                    editor_name = name,
                    player_uuid = name,
                    force = (byte)tgd.START_Force,
                    level = tgd.TestActorLevel,
                    pos = new DeepCore.Geometry.Vector3(startRegion.X, startRegion.Y, startRegion.Z),
                    direction = tgd.FaceDirection,
                };
            }
            return null;
        }

        public static bool TryGetStartRegion<ST>(this SceneData sd, ST st, TryGetPredicate<ST, RegionData, PlayerStartAbilityData> action, out RegionData region, out PlayerStartAbilityData start)
        {
            var regions = sd.GetStartRegionsList();
            foreach (var rg in regions)
            {
                start = rg.ForEachAs((st, rg, action), static (st, t) =>
                {
                    if (st.action(st.st, st.rg, t))
                    {
                        return true;
                    }
                    return false;
                }, default(PlayerStartAbilityData));
                if (start != null)
                {
                    region = rg;
                    return true;
                }
            }
            region = null;
            start = null;
            return false;
        }
        public static bool ForEachStartRegions<ST>(this SceneData sd, ST st, ForEachPredicate<ST, RegionData, PlayerStartAbilityData> action)
        {
            var regions = sd.GetStartRegionsList();
            foreach (var rg in regions)
            {
                foreach (var ab in rg.GetAbilities())
                {
                    if (ab is PlayerStartAbilityData s && action(st, rg, s))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static InstancePlayer InitPlayerStartRegions(this InstanceZone mZone, int? force = null, int? actorTemplateID = null)
        {
            var ret = default(InstancePlayer);
            var regions = mZone.Data.GetStartRegionsList();
            //             if (force.HasValue)
            //             {
            //                 var startRegion = regions.Get(force.Value);
            //                 if (startRegion != null)
            //                 {
            //                     var add = InitPlayerStartRegions(mZone, startRegion, actorTemplateID);
            //                     if (add != null)
            //                     {
            // 
            //                     }
            //                 }
            //             }
            foreach (var rg in regions)
            {
                var add = InitPlayerStartRegions(mZone, rg, actorTemplateID);
                if (add != null)
                {
                    if (force.HasValue)
                    {
                        if (add.Force == force.Value)
                        {
                            ret = add;
                        }
                    }
                    if (ret == null)
                    {
                        ret = add;
                    }
                }
            }
            return ret;
        }
        public static InstancePlayer InitPlayerStartRegions(this InstanceZone mZone, RegionData startRegion, int? actorTemplateID = null)
        {
            var tgd = startRegion.GetAbilityOf<PlayerStartAbilityData>();
            if (tgd != null)
            {
                var add = TryAddPlayer(mZone, startRegion, tgd, actorTemplateID);
                if (add != null)
                {
                    var actor = mZone.AddUnit(add.Value) as InstancePlayer;
                    if (actor != null)
                    {
                        return actor;
                    }
                }
            }
            return null;
        }
    }
}


namespace DeepCore.Game3D.Slave.Layer
{
    public static class LayerExt
    {
        public static InstanceUnit AsHost(this LayerUnit unit)
        {
            return unit.EventSender as InstanceUnit;
        }
        public static InstanceSpell AsHost(this LayerSpell unit)
        {
            return unit.EventSender as InstanceSpell;
        }
        public static InstanceItem AsHost(this LayerItem unit)
        {
            return unit.EventSender as InstanceItem;
        }
    }
}