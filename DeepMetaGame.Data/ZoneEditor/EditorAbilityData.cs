using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;

namespace DeepMetaGame.Data.ZoneEditor
{
    [TableClass(nameof(Name))]
    public abstract class EditorAbilityData : IDataAbility
    {
        [Desc("Name", "", true)]
        public string Name;
        sealed public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            if (GetType().TryGetAttribute<DescAttribute>(out var desc))
            {
                return desc.Desc;
            }
            return base.ToString();
        }
    }
    public abstract class SceneAbilityData : EditorAbilityData { }
    public abstract class UnitAbilityData : EditorAbilityData { }
    public abstract class ItemAbilityData : EditorAbilityData { }
    public abstract class RegionAbilityData : EditorAbilityData { }
    public abstract class DecorationAbilityData : EditorAbilityData { }
    public abstract class PointAbilityData : EditorAbilityData { }
    public abstract class AreaAbilityData : EditorAbilityData { }

    //----------------------------------------------------------------------------------------------------------------------------
    #region Objects Ability

    [Expandable]
    [MessageType(BattleConstants.PlayerStartAbilityData)]
    [Desc("玩家出生点")]
    public class PlayerStartAbilityData : RegionAbilityData
    {
        [Desc("阵营")]
        public int START_Force;

        [Desc("测试主角ID", "测试", true)]
        [TemplateID(typeof(UnitInfo))]
        public int TestActorTemplateID = 1;

        [TemplateLevel]
        public int TestActorLevel;

        [Desc("单位朝向")]
        public float FaceDirection; 
        [Desc(Desc = "单位朝向360", Editable = true)]
        public float FaceDirection360
        {
            get => CMath.RadianToAngle(FaceDirection);
            set { FaceDirection = CMath.AngleToRadian(value); }
        }

        [Desc("飞行配置")]
        public UnitFlyOpt FlyOpt;


    }

    [Expandable]
    [MessageType(BattleConstants.SpawnUnitAbilityData)]
    [Desc("怪物刷新点")]
    public class SpawnUnitAbilityData : RegionAbilityData
    {
        [MessageType(BattleConstants.SpawnUnit)]
        [Desc("怪物刷新ID和等级")]
        public class SpawnUnit : IBaseFuncData
        {
            [Desc("单位模板")]
            [TemplateID(typeof(UnitInfo))]
            public int UnitTemplateID;
            [Desc("单位等级")]
            [TemplateLevel]
            public int UnitLevel = 0;
            [Desc("此单位每次刷新概率百分比")]
            public float Percent = 100f;
            [Desc("单个单位覆盖类型")]
            public UnitType? OverrideType;
            public override string ToString()
            {
                return UnitTemplateID.ToString();
            }
        }
        [MessageType(BattleConstants.SpawnUnitGroup)]
        [Desc("怪物刷新组")]
        public class SpawnGroup : IBaseFuncData
        {
            [Desc("单位模板组")]
            [TemplateGroup(typeof(UnitInfo))]
            public string UnitGroupPath;
            [Desc("单位等级")]
            [TemplateLevel]
            public int UnitLevel = 0;
            public override string ToString()
            {
                return UnitGroupPath;
            }
        }
        [Desc("怪物类型模板ID和等级组", "基础")]
        [ListDesc(typeof(SpawnUnit))]
        public ArrayList<SpawnUnit> UnitTemplates = new ArrayList<SpawnUnit>();

        [Desc("怪物刷新组(目录下所有单位)", "基础")]
        [ListDesc(typeof(SpawnUnit))]
        public SpawnGroup UnitGroup;

        [Desc("重置单位类型", "基础")]
        public UnitType OverrideType = UnitType.TYPE_MONSTER;
        //         [Desc("怪物类型模板ID组(兼容老版本)", "基础")]
        //         [TemplatesID(typeof(UnitInfo))]
        //         public ArrayList<int> UnitTemplatesID = new ArrayList<int>();
        //         [Desc("怪物类型模板ID组(兼容老版本)", "基础")]
        //         [TemplatesID(typeof(UnitInfo))]
        //         public ArrayList<int> UnitTemplatesGroupID
        //         {
        //             get => UnitTemplatesID;
        //             set { UnitTemplatesID = value; }
        //         }

        [Desc("怪物等级", "可选")]
        [TemplateLevel]
        public int UnitLevel;
        [Desc("延迟启动时间(毫秒)", "可选")]
        public int StartTimeDelayMS;
        [Desc("刷新间隔时间(毫秒)", "基础")]
        public int IntervalMS = 5000;
        [Desc("一次刷新数量", "基础")]
        public int OnceCount = 5;
        [Desc("总刷新数量上限（0表示无上限）", "可选")]
        public int TotalLimit;
        [Desc("存活数量上限（0表示无上限）", "可选")]
        public int AliveLimit;
        [Desc("每次刷新必须所有怪物死亡", "基础")]
        public bool WithoutAlive;
        [Desc("怪物初始阵营", "基础")]
        public byte Force;
        [Desc("怪物初始标记", "可选")]
        public string UnitTag;
        [Desc("怪物名字", "可选")]
        public string UnitName;
        [Desc("怪物没有存活时，重置刷新点计时（间隔时间用StartTimeDelayMS控制）", "基础")]
        public bool ResetOnWithoutAlive;
        [Desc("随机生成", "可选")]
        public bool RandomSpawn = true;


        [SceneObjectID(typeof(PointData))]
        [Desc("怪物初始路点", "路点")]
        public string StartPointName;

        [SceneObjectID(typeof(DecorationData))]
        [Desc("复制空气墙碰撞", "碰撞")]
        public string CopyDecorationShape;

        [Desc("初始朝向(大于等于0有效)", "可选")]
        public float StartDirection = -1;

        [Desc("怪物产生时区域触发的特效", "资源")]
        public LaunchEffect SpawnEffect;

        [Desc("怪物产生时触单位发的特效", "资源")]
        public LaunchEffect SpawnObjectEffect;

        [Desc("一次刷新多个怪物时初始阵型", "阵型")]
        public TeamFormation TFormation;

        [Desc("生成在地表上", "阵型")]
        public bool OnTheGround = true;




    }

    [Expandable]
    [MessageType(BattleConstants.UnitTransportAbilityData)]
    [Desc("传送点功能")]
    public class UnitTransportAbilityData : RegionAbilityData
    {
        [SceneObjectID(typeof(SceneObjectData))]
        [Desc("传送到路点位置")]
        public string NextPosition;

        [DependOnProperty(nameof(AcceptUnitTypeForAll), false)]
        [Desc("接受的单位类型", "过滤")]
        public UnitType AcceptUnitType = UnitType.TYPE_PLAYER;
        [Desc("接受所有类型的单位，此处为True，AcceptUnitType失效", "过滤")]
        public bool AcceptUnitTypeForAll = false;

        [DependOnProperty(nameof(AcceptForceForAll), false)]
        [Desc("接受的阵营", "过滤")]
        public byte AcceptForce = 0;
        [Desc("接受所有阵营的单位，此处为True，AcceptForce失效", "过滤")]
        public bool AcceptForceForAll = true;

        [Desc("是否允许AOI位面传送", "过滤")]
        public bool AcceptAoiStatus = true;


        [Desc("传送时的特效", "资源")]
        public LaunchEffect TransportEffect;


    }

    [Expandable]
    [MessageType(BattleConstants.SceneTransportAbilityData)]
    [Desc("跨场景传送点功能")]
    public class SceneTransportAbilityData : RegionAbilityData
    {
        [TemplateID(typeof(SceneData))]
        [Desc("下一个场景ID")]
        public int NextSceneID;
        [Desc("下一个场景路点位置")]
        public string NextScenePosition;
        [DependOnProperty(nameof(AcceptForceForAll), false)]
        [Desc("接受的阵营", "过滤")]
        public byte AcceptForce = 0;
        [Desc("接受所有阵营的单位，此处为True，AcceptForce失效", "过滤")]
        public bool AcceptForceForAll = true;
        [Desc("是否允许AOI位面传送", "过滤")]
        public bool AcceptAoiStatus = true;
        [Desc("传送时的特效", "资源")]
        public LaunchEffect TransportEffect;


    }

    [Expandable]
    [MessageType(BattleConstants.SpawnItemAbilityData)]
    [Desc("物品刷新点")]
    public class SpawnItemAbilityData : RegionAbilityData
    {
        [MessageType(BattleConstants.SpawnItem)]
        [Desc("物品刷新ID")]
        public class SpawnItem : IBaseFuncData
        {
            [Desc("物品模板")]
            [TemplateID(typeof(ItemTemplate))]
            public int ItemTemplateID;
            [Desc("此物品每次刷新概率百分比")]
            public float Percent = 100f;
            public override string ToString()
            {
                return ItemTemplateID.ToString();
            }
        }
        [Desc("物品类型模板ID和等级组", "基础")]
        public ArrayList<SpawnItem> ItemTemplates = new ArrayList<SpawnItem>();

        [Desc("延迟启动时间(毫秒)", "可选")]
        public int StartTimeDelayMS;
        [Desc("刷新间隔时间(毫秒)", "基础")]
        public int IntervalMS = 5000;
        [Desc("一次刷新数量", "基础")]
        public int OnceCount = 5;
        [Desc("总刷新数量上限（0表示无上限）", "可选")]
        public int TotalLimit;
        [Desc("存活数量上限（0表示无上限）", "可选")]
        public int AliveLimit;
        [Desc("每次刷新必须所有物品消亡", "基础")]
        public bool WithoutAlive;
        [Desc("物品初始阵营", "基础")]
        public byte Force;
        [Desc("物品初始标记", "可选")]
        public string UnitTag;
        [Desc("物品名字", "可选")]
        public string UnitName;

        [Desc("物品产生时区域触发的特效", "资源")]
        public LaunchEffect SpawnEffect;
        [Desc("物品产生时物品触发的特效", "资源")]
        public LaunchEffect SpawnObjectEffect;

        [Desc("初始朝向(大于等于0有效)", "可选")]
        public float StartDirection = -1;

        [Desc("道具没有存活时，重置刷新点计时（间隔时间用StartTimeDelayMS控制）", "基础")]
        public bool ResetOnWithoutAlive;
        [Desc("随机生成", "可选")]
        public bool RandomSpawn = true;
        [Desc("生成在地表上", "阵型")]
        public bool OnTheGround = true;

        [Desc("一次刷新多个怪物时初始阵型", "阵型")]
        public TeamFormation TFormation;
    }



    [Expandable]
    [MessageType(BattleConstants.CameraFocusAbilityData)]
    [Desc("摄像机聚焦")]
    public class CameraFocusAbilityData : RegionAbilityData { }


    [Expandable]
    [MessageType(BattleConstants.CameraPositionAbilityData)]
    [Desc("摄像机位置")]
    public class CameraPositionAbilityData : RegionAbilityData { }

    [Expandable]
    [MessageType(BattleConstants.CameraTargetAbilityData)]
    [Desc("摄像机目标")]
    public class CameraTargetAbilityData : RegionAbilityData { }



    [Expandable]
    [MessageType(BattleConstants.PointHoldAbility)]
    [Desc("经过路点待机")]
    public class PointHoldAbility : PointAbilityData
    {
        [Desc("切换路点待机最小时间(毫秒)", "路点")]
        public int HoldMinTimeMS = 1000;
        [Desc("切换路点待机最大时间(毫秒)", "路点")]
        public int HoldMaxTimeMS = 5000;
    }

    #endregion
    //----------------------------------------------------------------------------------------------------------------------------
    #region Scene Ability
    [Expandable]
    [MessageType(BattleConstants.SceneUIAbility)]
    [Desc("场景UI能力")]
    public class SceneUIAbility : SceneAbilityData
    {
        [Desc("鼠标目标资源", "资源", true)][ResourceID(ResourceType.Object)] public string MouseRayCastResource;
        [Desc("鼠标目标特效", "资源", true)] public LaunchEffect MouseRayCastEffect;
    }

    #endregion
    //----------------------------------------------------------------------------------------------------------------------------
}
