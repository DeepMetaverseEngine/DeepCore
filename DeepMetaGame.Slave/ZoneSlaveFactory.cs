using DeepCore.Game3D.Slave.Helper;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.Reflection;
using DeepCore.Space;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using static DeepCore.Game3D.Slave.Layer.LayerUnit;

namespace DeepCore.Game3D.Slave
{
    [Reflectible]
    public abstract class ZoneSlaveFactory : IBattleFactory
    {
        //         private static ZoneSlaveFactory instance;
        //         public static ZoneSlaveFactory Factory
        //         {
        //             get
        //             {
        //                 if (instance == null)
        //                 {
        //                     instance = new Simple.SimpleZoneSlaveFactory();
        //                 }
        //                 return instance;
        //             }
        //         }

        /// <summary>
        /// 编辑器根地址
        /// </summary>
        //public string GameEditorRoot { get => DataFactory.GameEditorRoot; }
        public ZoneDataFactory DataFactory { get; }

        protected ZoneSlaveFactory()
        {
            this.DataFactory = ZoneDataFactory.Factory;
            //instance = this;
        }

        //---------------------------------------------------------------------------------------------

        public delegate void OnLayerCreateHandler(LayerZone zone);
        public event OnLayerCreateHandler OnLayerCreate;
        public LayerZone CreateZoneLayer(EditorTemplates templates, ILayerZoneListener listener)
        {
            var z = CreateClientZoneLayer(templates, listener);
            OnLayerCreate?.Invoke(z);
            return z;
        }
        /// <summary>
        /// 创建客户端代理场景
        /// </summary>
        /// <param name="templates"></param>
        /// <param name="listener"></param>
        /// <returns></returns>
        protected virtual LayerZone CreateClientZoneLayer(EditorTemplates templates, ILayerZoneListener listener)
        {
            return new LayerZone(templates, this, listener);
        }
        public virtual ILayerZoneTerrain CreateClientTerrain(ClientEnterScene enter, LayerZone zone)
        {
            var path = zone.DataRoot.EditorRoot + zone.Data.VoxelFileName;
            //var wd = TerrainFactory.Instance.GetOrCreateVoxelWorld(path, zone.Data);
            var wd = ZoneDataFactory.Factory.CreateVoxelWorld(zone, zone.DataRoot, zone.Data.VoxelFileName, zone.Data, zone.Data.ZoneData);
            return new VoxelClientTerrain3D(zone, wd);
        }

        public virtual LayerSpell CreateClientSpell(LayerZone parent, SpellTemplate info, SyncSpellInfo syn, AddSpellEvent add = null)
        {
            return LayerSpell.Alloc(info, syn, parent, add);
            //return new LayerSpell(info, syn, parent, add);
        }
        public virtual LayerItem CreateClientItem(LayerZone parent, ItemTemplate info, SyncItemInfo syn, AddItemEvent add = null)
        {
            return LayerItem.Alloc(info, syn, parent, add);
            //return new LayerItem(info, syn, parent, add);
        }
        public virtual LayerUnit CreateClientUnit(LayerZone parent, UnitInfo info, SyncUnitInfo syn, AddUnitEvent add = null)
        {
            return new LayerUnit(info, syn, parent, add, add?.sender);
        }
        public virtual LayerPlayer CreateClientActor(LayerZone parent, UnitInfo info, LockActorEvent add)
        {
            info = parent.CloneData(info);
            info.Properties = add.GameServerProp;
            return new LayerPlayer(info, add, parent);
        }
        public virtual BuffState AllocBuffState(BuffTemplate buff, LayerUnit owner, uint senderID, bool isEquip)
        {
            return BuffState.Alloc(buff, owner, senderID, isEquip);
        }
        public virtual AuraState AllocAuraState(LayerUnit unit, AuraTemplate data)
        {
            return AuraState.Alloc(unit, data);
        }
        public virtual ISkillAction AllocSkillAction(LayerUnit unit, LayerUnit.SkillState ss)
        {
            if (unit is LayerPlayer actor)
            {
                if (actor.IsClientControlMove)
                {
                    return LayerPlayer.PreSkillByClient.Alloc(actor, ss);
                }
                else
                {
                    return LayerPlayer.PreSkillByServer.Alloc(actor, ss);
                }
            }
            else
            {
                if (unit.Parent.ActorSyncMode == SyncMode.MoveByClient_PreSkillByClient)
                {
                    return UnitPreSkillAction.Alloc(unit, ss);
                }
                else
                {
                    return UnitForceSkillAction.Alloc(unit, ss);
                }
            }
        }
        //---------------------------------------------------------------------------------------------

        /// <summary>
        /// 创建客户端显示名称规则
        /// </summary>
        /// <param name="info"></param>
        /// <param name="syncInfo"></param>
        /// <returns></returns>
        public virtual string ToClientDisplayName(UnitInfo info, SyncUnitInfo syncInfo)
        {
            return info.Name;
        }
        public virtual object DecodeZoneVar(LayerZone layer, object value)
        {
            if (value is ZoneVarObject obj)
            {
                return layer.GetObject(obj.ObjID);
            }
            if (value is ZoneVarObjectBuff buff)
            {
                return layer.GetUnit(buff.ObjID)?.GetBuff(buff.BuffID);
            }
            if (value is ZoneVarObjectSkill skill)
            {
                return layer.GetUnit(skill.ObjID)?.GetSkillState(skill.SkillID);
            }
            if (value is ZoneVarObjectAura aura)
            {
                return layer.GetUnit(aura.ObjID)?.GetAura(aura.AuraID);
            }
            if (value is ZoneVarTemplate temp)
            {
                layer.Templates.TryGetTemplate(temp.TemplateType, temp.TemplateID, out var tt);
                return tt;
            }
            return value;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------

    public interface ILayerZoneTerrain : IDisposable, ITerrainSurface
    {
        bool ITerrainSurface.TryGetVoxelLayerByPos(in Vector3 pos, out float upward, out float top)
        {
            if (TryGetVoxelUpRange(pos, out var down, out upward, out top))
            {
                return true;
            }
            return false;
        }

        // 
        //         bool ITerrainSurface.TryMoveSpellOnFloor(ref Geometry.Vector3 pos, float direction, float distance)
        //         {
        //             if (TryGetVoxelLayerByPos(in pos, out var layer, true))
        //             {
        //                 if (TryMoveSpellOnFloor(ref pos, ref layer, direction, distance))
        //                 {
        //                     return true;
        //                 }
        //             }
        //             return false;
        //         }

        /// <summary>
        /// 移动时可通过的高度差，阶梯高度
        /// </summary>
        float StepIntercept { get; }
        /// <summary>
        /// 场景总宽
        /// </summary>
        float TotalWidth { get; }
        /// <summary>
        /// 场景总长
        /// </summary>
        float TotalHeight { get; }
        /// <summary>
        /// 体素格尺寸
        /// </summary>
        float GridCellSize { get; }

        float ResourceStartX { get; }
        float ResourceStartY { get; }

        bool TouchMapByPos(LayerUnit u, Geometry.Vector3 pos);
        bool TryMoveTo(ref Geometry.Vector3 pos);
        //bool TryMoveSpellOnFloor(ref Geometry.Vector3 pos, float direction, float distance);
        bool TryGetVoxelUpRange(Vector3 pos, out float downward, out float upward, out float top);
        bool TryGetVoxelDownRange(Geometry.Vector3 pos, out float downward);
        bool TryGetVoxelUpRange(Geometry.Vector3 pos, out float upward);
        bool TryGetVoxelTopRange(Geometry.Vector3 pos, out float top);
        bool FillMapBlockByShape(IShape shape, bool block);

        ILayerWayPoint FindPath(Geometry.Vector3 src, Geometry.Vector3 dst);
        ILayerUnitPosition CreateUnitPosition(LayerUnit unit);

    }
    public interface ILayerUnitPosition
    {
        float X { get; }
        float Y { get; }
        float Z { get; }
        /// <summary>
        /// 当前所在体素上沿
        /// </summary>
        float Upward { get; }
        Geometry.Vector3 Position { get; }
        /// <summary>
        /// 是否浮空
        /// </summary>
        bool IsInAir { get; }
        /// <summary>
        /// 当前重力
        /// </summary>
        float Gravity { get; set; }
        /// <summary>
        /// 当前Z方向速度
        /// </summary>
        float SpeedZ { get; set; }
        void SetPos(float x, float y, float z);
        void SetPos(in Geometry.Vector3 target);

        void StartJump(float zspeed, float gravity);
        void Fly(float zOffset);

        bool Update(in Geometry.Vector3 remotePos, float intervalMS);
        bool FixPos(in Geometry.Vector3 remotePos, float intervalMS, float speedSEC);


        TryMoveToMapBorderResult TryMoveToMapBorder(float addX, float addY);
        void Move(float addX, float addY);
        void ForceSetPos(in Geometry.Vector3 pos);
    }

    public interface ILayerPlayerPosition : ILayerUnitPosition
    {
    }

    public interface ILayerWayPoint
    {
        ILayerWayPoint Next { get; }
        ILayerWayPoint Prev { get; }
        ILayerWayPoint Tail { get; }
        float X { get; }
        float Y { get; }
        float Z { get; }
        Geometry.Vector3 Position { get; }
        bool PosEquals(ILayerWayPoint w);
        float GetTotalDistance();
    }

}
