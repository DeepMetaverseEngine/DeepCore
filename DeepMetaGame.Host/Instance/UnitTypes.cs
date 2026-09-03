using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Geometry.Terrain;
using DeepCore.Reflection;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneGeometry;

namespace DeepCore.Game3D.Host.Instance
{
    [Reflectible]
    public interface IPositionObject 
    {
        public float X { get => Position.X; }
        public float Y { get => Position.Y; }
        public float Z { get => Position.Z; }

        EditorScene Parent { get; }
        /// <summary>
        /// 方向
        /// </summary>
        float Direction { get; }
        /// <summary>
        /// 半径
        /// </summary>
        float BodySize { get; }
        float BodyHeight { get; }
        Geometry.Vector3 Position { get; }
    }

    [Reflectible]
    public interface IEntityObject : IPositionObject
    {
        IZoneShape ZoneShape { get; set; }
        bool StaticBlockable { get; }
        bool IsStaticBlock { get; }
        ITerrainLayer CurrentLayer { get; }
        Geometry.VoxelCylinder VoxelBody { get; }
        ZoneSpaceDivision.ZoneSpaceUserTag SpaceUserTag { get; }
    }

    //     [Reflectible]
    //     public interface IPlayerUnit
    //     {
    //         InstanceZone Parent { get; }
    //         bool IsGuard { get; }
    //         bool IsReady { get; }
    // 
    //         QuestData GetQuest(string questID);
    //         bool IsQuestAccepted(string questID);
    //         void DoAcceptQuest(string questID, string args);
    //         void DoCommitQuest(string questID, string args);
    //         void DoDropQuest(string questID, string args);
    //         void DoUpdateQuestStatus(string questID, string key, string value);
    // 
    //     }

    //     /// <summary>
    //     /// 召唤单位
    //     /// </summary>
    //     [Reflectible]
    //     public interface ISummonedUnit
    //     {
    //         InstanceZone Parent { get; }
    //         /// <summary>
    //         /// 主人
    //         /// </summary>
    //         InstanceUnit SummonerUnit { get;  }
    //     }

    //     /// <summary>
    //     /// 所有自动电脑AI
    //     /// </summary>
    //     [Reflectible]
    //     public interface IGuardUnit
    //     {
    //         InstanceZone Parent { get; }
    //         void AttackTo(ZoneWayPoint start);
    //         void SetOrginPosition(Geometry.Vector3? pos);
    //         bool FollowAndAttack(InstanceUnit target, AttackReason reason);
    //         void GuardUnit(InstanceUnit vip);
    //     }
    // 
    //     [Reflectible]
    //     public interface IManualUnit
    //     {
    //         InstanceZone Parent { get; }
    //         void FocuseAttack(InstanceUnit targget);
    //         void QueueIdle(float timeSEC, StateStopHandler over = null);
    //         void QueueDoAction(float timeSEC, string actionName, StateStopHandler over = null);
    //         void QueueMoveTo(Geometry.Vector3 pos, StateStopHandler over = null);
    //         void QueueLaunchSkill(int skillID, bool random, StateStopHandler over = null);
    //         void Wait(float timeSEC, System.Action over = null);
    //     }
    // 
    //     [Reflectible]
    //     public interface IBuildingUnit
    //     {
    //         InstanceZone Parent { get; }
    // 
    //     }
}
