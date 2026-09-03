
using DeepCore;
using DeepCore.AI;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneEditor;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Template
{
    [MessageType(BattleConstants.UnitInfo)]
    [Desc("单位模板数据")]
    public class UnitInfo : CustomEventTemplateData
    {
        //------------------------------------------------------------------------------------------
        [Desc(Category = "1.基础", Desc = "单位类型")]
        public UnitType UType = UnitType.TYPE_MONSTER;
        [Desc(Category = "1.基础", Desc = "是否为精英")]
        public bool IsElite;
        [Desc(Category = "1.基础", Desc = "是否为动态创建单位，比如PVP或者根据玩家等级创建的单位")]
        public bool IsDynamic = false;
        //------------------------------------------------------------------------------------------

        [Desc(Category = "2.体型", Desc = "身体高度")]
        public float BodyHeight = 1.8f;
        [Desc(Category = "2.体型", Desc = "身体尺寸（半径）")]
        public float BodySize = .5f;
        [Desc(Category = "2.体型", Desc = "身体受攻击尺寸（BodySize + BodyHitSizeAppend）（半径）")]
        public float BodySizeHitAppend = .1f;
        [Desc(Category = "2.体型", Desc = "体重")]
        public int Weight = 1;
        [Desc(Category = "2.体型", Desc = "捡取范围（半径）")]
        public float PickRange = 1f;
        [Desc(Category = "2.体型", Desc = "无碰撞")]
        public bool NoTouch = false;
        [Desc(Category = "2.体型", Desc = "地图形状，只对建筑物对地图有影响类型生效")]
        public Shape FillZoneShape = Shape.ROUND;
        public enum Shape
        {
            ROUND = 0,
            RECTANGLE = 1,
        }

        //------------------------------------------------------------------------------------------

        [Desc(Category = "3.战斗", Desc = "血量")]
        public int HealthPoint = 100;
        [Desc(Category = "3.战斗", Desc = "法力值")]
        public int ManaPoint = 100;
        [Desc(Category = "3.战斗", Desc = "耐力值")]
        public int StaminaPoint = 100;

        //------------------------------------------------------------------------------------------

        [Desc(Category = "4.生命周期", Desc = "若是召唤类，则表示存活时间(毫秒)")]
        public int LifeTimeMS;
        [Desc(Category = "4.生命周期", Desc = "单位出生延时时间，通常播放出生动画(毫秒)")]
        public int SpawnTimeMS;
        [Desc(Category = "4.生命周期", Desc = "单位死亡持续时间，即死亡后多长时间内可继续鞭尸(毫秒)")]
        public int DeadTimeMS;
        [Desc(Category = "4.生命周期", Desc = "死亡后多久复活，0表示不能复活(毫秒)")]
        public int RebirthTimeMS;
        [Desc(Category = "4.生命周期", Desc = "受击时间(毫秒)")]
        public int DamageTimeMS = 10;

        //------------------------------------------------------------------------------------------
        [Desc(Category = "8.事件", Desc = "绑定单位事件行为树的ID")]
        [TemplatesID(typeof(UnitEventTemplate)), Expandable]
        public ArrayList<int> Events = new ArrayList<int>();
        //------------------------------------------------------------------------------------------

        [Desc(Category = "9.扩展", Desc = "能力列表，所有继承自IUnitTemplateAbility的子类")]
        [NotNull]
        public ArrayList<IUnitTemplateAbility> Abilities = new ArrayList<IUnitTemplateAbility>();

        [Desc(Category = "9.扩展", Desc = "扩展属性")]
        [Expandable]
        [NotNull]
        public IUnitProperties Properties;
        public override IPropertiesData PropertiesData => this.Properties;

        //--------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------
        public UnitInfo()
        {
            Abilities.Add(new UnitResourceAbility());
            Abilities.Add(new UnitGuardAbility());
            Abilities.Add(new UnitRecoverAbility());
            Abilities.Add(new UnitMotionAbility());
            Abilities.Add(new UnitSkillAbility());
            Abilities.Add(new UnitInventoryAbility());
            Properties = ZoneDataFactory.Factory.CreateProperties<IUnitProperties>(this);
        }




    }
    [Desc("单位类型")]
    public enum UnitType : byte
    {
        [Desc("没有类型")] TYPE_NA = 0,
        [Desc("玩家")] TYPE_PLAYER = 1,
        [Desc("NPC")] TYPE_NPC = 2,
        [Desc("怪物")] TYPE_MONSTER = 3,
        [Desc("宠物")] TYPE_PET = 4,
        [Desc("召唤物")] TYPE_SUMMON = 5,
        [Desc("建筑")] TYPE_BUILDING = 6,
        [Desc("脚本控制")] TYPE_MANUAL = 7,
        [Desc("挂载物/机关")] TYPE_ATTACHMENT = 8,
        [Desc("中立无敌意")] TYPE_NEUTRALITY = 9,
        [Desc("跟随单位")] TYPE_FOLLOW_UNIT = 10,
        [Desc("采集物")] TYPE_PICK_ITEM = 11,
        [Desc("掉落物")] TYPE_DROP_ITEM = 12,
        [Desc("行为树控制单位")] TYPE_BEHAVIOR_TREE = 13,
        [Desc("载具")] TYPE_VEHICLE = 100,
        [Desc("玩家镜像")] TYPE_PLAYERMIRROR = 101,
        [Desc("客户端本地NPC")] TYPE_LOCAL_NPC = 103,
    }
    //------------------------------------------------------------------------------------------
    public abstract class IUnitTemplateAbility : IDataAbility
    {
    }
    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitResourceAbility)]
    [Desc("1.资源能力")]
    public class UnitResourceAbility : IUnitTemplateAbility
    {
        [Desc("对应的模型文件名", "2.资源")]
        [ResourceID(ResourceType.Object)] 
        public string FileName;
        [Desc("对应的模型资源ID", "2.资源")]
        public int FileResId
        {
            get
            {
                if (Parser.TryParseInt(FileName, out var resId))
                    return resId;
                return 0;
            }
        }

        [Desc("怪物产生时触发的特效", "2.资源")]
        public LaunchEffect SpawnEffect;

        [Desc("怪物死亡时触发的特效", "2.资源")]
        public LaunchEffect DeadEffect;

        [Desc("怪物烂掉时触发的特效", "2.资源")]
        public LaunchEffect RemovedEffect;

        [Desc("怪物击碎时触发的特效", "2.资源")]
        public LaunchEffect CrushEffect;

        [Desc("怪物受击时触发的特效", "2.资源")]
        public LaunchEffect DamageEffect;

        [Desc("单位升级时触发的特效", "2.资源")]
        public LaunchEffect LevelUpEffect;

        [Desc("身体特效", "2.资源")]
        public LaunchEffect BodyEffect;

        [Desc("缩放比率", "2.资源")]
        public float BodyScale = 1;

        [Desc("覆盖动作列表", "2.资源")]
        public UnitActionDefinitionMap OverrideActionMap = null;

        [Desc("皮肤名称", "3.AVATAR")]
        public string SkinName;
        [Desc("皮肤附件（AVATAR）", "3.AVATAR")]
        public string[] SkinAvatar;
    }
    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitGuardAbility)]
    [Desc("2.警戒能力")]
    public class UnitGuardAbility : IUnitTemplateAbility
    {
        [Desc("警戒距离范围", "战斗 - 警戒")]
        public float GuardRange = 10;
        [Desc("警戒范围角度(0-360)，0为圆形", "战斗 - 警戒")]
        public float GuardRangeAngle = 0;
        [Desc("原有警戒范围，的超出警戒范围（GuardRange + GuardRangeLimitAppend）", "战斗 - 警戒")]
        public float GuardRangeLimitAppend = 5;
        [Desc("传递警戒范围，如果此单位进入战斗，则传给相邻单位Add，0表示不传递", "战斗 - 警戒")]
        public float GuardRangeGroup = 5;

    }
    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitRecoverAbility)]
    [Desc("3.恢复能力")]
    public class UnitRecoverAbility : IUnitTemplateAbility
    {

        [DependOnProperty(nameof(RecoveryIntervalMS))]
        public bool IdleRecover { get { return RecoveryIntervalMS > 0; } }

        [Desc("恢复间隔(毫秒)，脱离战斗后回血间隔时间", "4.战斗 - 恢复")]
        public int RecoveryIntervalMS = 1000;
        [Desc("血量恢复/恢复间隔", "4.战斗 - 恢复")]
        [DependOnProperty(nameof(IdleRecover))]
        public int HealthRecoveryPoint = 1;
        [Desc("法力恢复/恢复间隔", "4.战斗 - 恢复")]
        [DependOnProperty(nameof(IdleRecover))]
        public int ManaRecoveryPoint = 1;
        [Desc("耐力恢复/恢复间隔", "4.战斗 - 恢复")]
        [DependOnProperty(nameof(IdleRecover))]
        public int StaminaRecoveryPoint = 10;
    }
    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitMotionAbility)]
    [Desc("4.移动能力")]
    public class UnitMotionAbility : IUnitTemplateAbility
    {
        [Desc("是否可移动", "5.移动")] public bool IsMoveable = true;

        [Desc("是否可转向", "5.移动")] public bool IsTurnable = true;

        [Desc("移动速度(距离/每秒)", "5.移动")] public float MoveSpeedSEC = 6f;

        [Desc("转脸速度（弧度/秒）", "5.移动")] public float TurnSpeedSEC = 0;
        [Desc("转脸速度（角度/秒）", "5.移动")] public float TurnSpeedSEC360 { get => CMath.RadianToAngle(TurnSpeedSEC); set => TurnSpeedSEC = CMath.AngleToRadian(value); }

        [Desc("身体跟随转动速度(弧度)", "5.移动")] public float BodyTurnSpeedSEC = 0f;
        [Desc("身体跟随转动速度(角度)", "5.移动")] public float BodyTurnSpeedSEC360 { get => CMath.RadianToAngle(BodyTurnSpeedSEC); set => BodyTurnSpeedSEC = CMath.AngleToRadian(value); }
     
        [Desc("移动速度动画速度播放比率", "5.速率")] public float MoveAnimateRate = 1f; 
        [Desc("缩放动画速度播放比率", "5.速率")] public float ScaleAnimateRate = 0.38f;

        [Desc("控制朝向方式", "5.移动")] public ControlType  Control = ControlType.FaceToMoveDirection;

        public enum ControlType
        {
            /// <summary>
            /// 正对相机前方
            /// </summary>
           [Desc("正对相机前方")] FaceToCameraFront = 0,
            /// <summary>
            /// 面朝鼠标目标
            /// </summary>
            [Desc("面朝鼠标目标")] FaceToMouseTarget = 1,
            /// <summary>
            /// 面朝移动方向
            /// </summary>
            [Desc("面朝移动方向")] FaceToMoveDirection = 2,
        }

        [Desc("飞行单位", "5.移动")] public bool IsFlyingObject { get => IsNoneGravity; set => IsNoneGravity = value; }
        [Desc("无重力", "5.移动")] public bool IsNoneGravity = false;
        [DependOnProperty(nameof(IsNoneGravity))][Desc("飞行停靠弧度", "5.移动")] public float FlyingStandbyRange;

        [Desc("跳跃上升初速度", "5.移动")] public float JumpZSpeed = 6f;
        [Desc("跳跃移动速度", "5.移动")] public float JumpMoveSpeed = 6f;

        [Desc("移动时可以挤开别人", "5.移动")] public bool IsMoveImpact = false;
    }
    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitSkillAbility)]
    [Desc("5.技能能力")]
    public class UnitSkillAbility : IUnitTemplateAbility
    {
        [Desc("此单位普通攻击技能", "技能")]
        public LaunchSkill BaseSkillID = new LaunchSkill();

        [NotNull]
        [Desc("此单位绑定的所有技能ID", "技能")]
        public ArrayList<LaunchSkill> Skills = new ArrayList<LaunchSkill>();

        [Desc("法术发射高度（炮口高度）", "炮口")]
        public float LaunchSpellHeight;
        [Desc("法术发射弧度（炮口弧度）", "炮口")]
        public float LaunchSpellAngle = 0;
        [Desc("法术发射角度（炮口角度）", "炮口")]
        public float LaunchSpellAngle360
        {
            get => CMath.RadianToAngle(LaunchSpellAngle);
            set => LaunchSpellAngle = CMath.AngleToRadian(value);
        }


        [Desc("法术发射半径（炮口半径）", "炮口")]
        public float LaunchSpellRadius = 0;

        [Desc("死亡时释放法术（自爆）", "战斗")]
        public LaunchSpell DeadLaunchSpell = null;

        public LaunchSkill GetSkillByID(int skillID)
        {
            if (BaseSkillID != null && skillID == BaseSkillID.SkillID)
            {
                return BaseSkillID;
            }
            if (Skills != null)
            {
                for (int i = 0; i < Skills.Count; i++)
                {
                    if (Skills[i].SkillID == skillID)
                    {
                        return Skills[i];
                    }
                }
            }
            return null;
        }
    }

    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitDropItemAbility)]
    [Desc("6.掉落能力")]
    public class UnitDropItemAbility : IUnitTemplateAbility
    {
        [Desc("掉落道具", "掉落")]
        public ArrayList<DropItemList> DropItemsSet = new ArrayList<DropItemList>();
        [Desc("被杀死直接获得金币", "掉落")]
        public int DropMoney;
        [Desc("被杀死产生经验", "7.掉落")]
        public int GenExp;
    }
    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitInventoryAbility)]
    [Desc("7.背包能力")]
    public class UnitInventoryAbility : IUnitTemplateAbility
    {
        [Desc(Category = "背包", Desc = "单位背包数量")]
        public int InventorySize = 1;

        [Desc(Category = "背包", Desc = "进入战斗携带的道具列表（比如血瓶）")]
        public ArrayList<InventoryItem> InventoryList = new ArrayList<InventoryItem>();

        [Desc(Category = "背包", Desc = "进入战斗携带的词缀列表")]
        [ListDesc(typeof(CardSlot))]
        public ArrayList<CardSlot> Cards = new ArrayList<CardSlot>();
    }
    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitSpawnAbility)]
    [Desc("8.生产能力")]
    public class UnitSpawnAbility : IUnitTemplateAbility
    {
        [Desc("生成单位", "生产")]
        public ArrayList<SpawnUnitAbilityData> SpawnUnit;
        [Desc("生成物品", "生产")]
        public ArrayList<SpawnItemAbilityData> SpawnItem;

    }

    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitResourceBodyAbility)]
    [Desc("9.资源形体描述")]
    public class UnitResourceBodyAbility : IUnitTemplateAbility
    {
        [Desc("摇头，控制上半身(坦克炮座左右动)")]
        public string PartHeadYaw;
        [Desc("点头，控制上半身(坦克炮管上下动)")]
        public string PartHeadPitch;
    }
    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitAttachmentAbility)]
    [Desc("A.单位挂载物")]
    public class UnitAttachmentAbility : IUnitTemplateAbility
    {
        [Desc("挂靠单位")]
        public List<UnitAttachment> UnitDockings = new List<UnitAttachment>();
    } 
    //------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.UnitDragAndDropAbility)]
    [Desc("A.单位鼠标拖拽能力")]
    public class UnitDragAndDropAbility : IUnitTemplateAbility
    {

    }
    //------------------------------------------------------------------------------------------

}
