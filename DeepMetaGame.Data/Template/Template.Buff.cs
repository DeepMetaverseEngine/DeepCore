
using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneEditor;
using System;

namespace DeepMetaGame.Data.Template
{
    //---------------------------------------------------------------------------------//


    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//


    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//


    //---------------------------------------------------------------------------------//
    /// <summary>
    /// BUFF类数据结构
    /// </summary>
    [MessageType(BattleConstants.BuffTemplate)]
    [Desc("BUFF类数据结构")]
    public class BuffTemplate : CustomEventTemplateData
    {

        [Desc(Category = "1.Base", Desc = "需要同步客户端")]
        public bool ClientVisible = true;
        [DependOnProperty(nameof(IsEquip), false)]
        [Desc(Category = "1.Base", Desc = "生命周期(毫秒)")]
        public int LifeTimeMS = 30000;

        //--------------------------------------------------------------

        [Desc(Category = "2.关键帧", Desc = "每间隔多少毫秒就触发一次")]
        public int HitIntervalMS;
        [Desc(Category = "2.关键帧", Desc = "第0帧是否有效")]
        public bool FirstTimeEnable = true;
        [Desc(Category = "2.关键帧", Desc = "每间隔时间触发一次的时候起效")]
        public KeyFrame HitKeyFrame;
        [Desc(Category = "2.关键帧", Desc = "所有关键帧")]
        public ArrayList<KeyFrame> KeyFrames;
        [Desc(Category = "2.关键帧", Desc = "Buff结束关键帧")]
        public KeyFrame EndKeyFrame;

        //--------------------------------------------------------------

        [Desc(Category = "3.BUFF", Desc = "是否有害(Debuff)，否则为有益(Buff)")]
        public bool IsHarmful = false;
        [Desc(Category = "3.BUFF", Desc = "Buff是否可以主动取消")]
        public bool IsCancelBySelf = false;
        [Desc(Category = "3.BUFF", Desc = "Buff是否允许多个多人实例")]
        public bool IsDuplicating = false;
        [Desc(Category = "3.BUFF", Desc = "Buff施放者死亡立即移除")]
        public bool IsRemoveOnSenderRemoved = false;
        [Desc(Category = "3.BUFF", Desc = "Buff宿主死亡立即移除")]
        public bool IsRemoveOnOwnerDead = true;
        [Desc(Category = "3.BUFF", Desc = "Buff来源技能停用立即移除")]
        public bool IsRemoveOnSkillDeactivated = false;
        [Desc(Category = "3.BUFF", Desc = "是否为永久Buff")]
        public bool IsEquip = false;
        [Desc(Category = "3.BUFF", Desc = "是否为被动系，则不显示在面板")]
        public bool IsPassive = false;

        //--------------------------------------------------------------

        //--------------------------------------------------------------

        [Desc(Category = "5.互斥", Desc = "互斥类型，同类型不能出现两个(非0有效)")]
        public int ExclusiveCatgory;
        [Desc(Category = "5.互斥", Desc = "互斥优先级，如果优先级相等，则替换")]
        public int ExclusivePriority;
        [Desc(Category = "5.互斥", Desc = "等级互斥，如果为True，则高等级BUFF覆盖低等级BUFF")]
        public bool ExclusiveLevel = false;

        //--------------------------------------------------------------
        [Desc(Category = "9.扩展", Desc = "能力")]
        [NotNull]
        public ArrayList<IBuffTemplateAbility> Abilities = new();
        [Desc(Category = "9.扩展", Desc = "Buff用户自定义扩展属性")]
        [Expandable]
        [NotNull]
        public IBuffProperties Properties;
        public override IPropertiesData PropertiesData => this.Properties;
        //--------------------------------------------------------------------------------------------
        public BuffTemplate()
        {
            Properties = ZoneDataFactory.Factory.CreateProperties<IBuffProperties>(this);
        }
        //--------------------------------------------


        //--------------------------------------------

        /// <summary>
        /// BUFF伤害或特效关键帧
        /// </summary>
        [MessageType(BattleConstants.BuffTemplateKeyFrame)]
        [Desc("BUFF伤害或特效关键帧")]
        [Expandable]
        public class KeyFrame : BaseKeyFrame
        {
            /// <summary>
            /// 触发的特效
            /// </summary>
            [Desc("触发的特效")]
            public LaunchEffect Effect;

            /// <summary>
            /// 触发新的法术
            /// </summary>
            [Desc("触发新的法术")]
            public LaunchSpell Spell;

            /// <summary>
            /// 攻击伤害
            /// </summary>
            [Desc("攻击伤害")]
            public AttackProp Attack;

            /// <summary>
            /// 直接使用道具
            /// </summary>
            [Desc("直接使用道具")]
            public UseItem Item;


            public override string ToString()
            {
                return "Frame: @" + FrameMS;
            }


        }

    }

    //---------------------------------------------------------------------------------//
    public abstract class IBuffTemplateAbility : IDataAbility
    {
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.BuffStateChangeAbility)]
    [Desc("1.状态能力")]
    public class BuffStateChangeAbility : IBuffTemplateAbility
    {
        [Desc("设置主状态（即不会被其他动作打断）", "状态")]
        public UnitActionStatus LockMainStateAction = UnitActionStatus.NA;
        [Desc("设置子状态（即不会被其他动作打断）", "状态")]
        public string LockSubStateAction;

        [Desc("是否进入霸体状态，不会被打断", "状态")]
        public bool IsNoneBlock = false;

        [Desc("是否隐身", "状态")]
        public bool IsInvisible = false;

        [Desc("是否无敌", "状态")]
        public bool IsInvincible = false;

        [Desc("是否免疫伤害", "状态")]
        public bool IsNoDamage = false;

        [Desc("是否沉默（只能释放BaseSkill）", "状态")]
        public bool IsSilent = false;

        [Desc("锁住移动（动不了但能放技能）", "状态")]
        public bool IsLockMotion = false;

        [Desc("产生眩晕", "状态")]
        public bool MakeStun = false;

    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.BuffSpeedChangeAbility)]
    [Desc("2.速度改变")]
    public class BuffSpeedChangeAbility : IBuffTemplateAbility
    {
        [Desc("移动速度比率")]
        public float FastMoveRate = 1f;
        [Desc("技能CD速度比率")]
        public float FastCastRate = 1f;
        [Desc("技能动作速度比率")]
        public float FastActionRate = 1f;
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.BuffEffectAbility)]
    [Desc("3.特效能力")]
    public class BuffEffectAbility : IBuffTemplateAbility
    {

        [Desc("BUFF期间绑定特效（客户端用）", "特效")]
        public LaunchEffect BindingEffect;

        [Desc("BUFF期间绑定特效集合（客户端用）", "特效")]
        public ArrayList<LaunchEffect> BindingEffectList = new ArrayList<LaunchEffect>();
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.BuffOverlayAbility)]
    [Desc("4.堆叠能力")]
    public class BuffOverlayAbility : IBuffTemplateAbility
    {
        [Desc(Category = "4.堆叠", Desc = "最高可堆叠层数")]
        public int MaxOverlay = 5;
        [Desc("BUFF期间每层绑定特效（每层对应一个特效，客户端用）", "特效")]
        public ArrayList<LaunchEffect> OverlayBindingEffect = new ArrayList<LaunchEffect>();
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.BuffAvatarChangeAbility)]
    [Desc("5.变身能力")]
    public class BuffAvatarChangeAbility : IBuffTemplateAbility
    {
        //--------------------------------------------------------------
        [Desc("允许换皮", "变身 - 换皮")]
        public bool MakeAvatar = false;
        [DependOnProperty(nameof(MakeAvatar))]
        [Desc("改变皮肤", "变身 - 换皮")]
        public string SkinName = null;
        [DependOnProperty(nameof(MakeAvatar))]
        [Desc("改变皮肤", "变身 - 换皮")]
        public string[] SkinAvatar = null;

        //--------------------------------------------------------------
        [Desc("允许变身", "变身 - 造型")]
        public bool ChangeBodyRes = false;
        [Desc("单位变身的模型文件名", "变身 - 造型")]
        [DependOnProperty(nameof(ChangeBodyRes))]
        [ResourceID(ResourceType.Object)] public string UnitFileName;
        [Desc("单位变身的模型文件资源Id", "变身 - 造型")]
        public int UnitFileResId { get { if (Parser.TryParseInt(UnitFileName, out var resId)) return resId; return 0; } }

        //--------------------------------------------------------------

        [Desc("增加缩放（在原有基础上的缩放增加）", "变身 - 缩放")]
        public float BodyScaleAppend = 0;

        //--------------------------------------------------------------
        [Desc("允许改变技能", "变身 - 技能")]
        public bool UnitChangeSkills = false;
        [Desc("此单位变身普通攻击技能", "变身 - 技能")]
        [DependOnProperty(nameof(UnitChangeSkills))]
        public LaunchSkill UnitBaseSkillID;
        [Desc("此单位变身绑定的所有技能ID", "变身 - 技能")]
        [DependOnProperty(nameof(UnitChangeSkills))]
        public ArrayList<LaunchSkill> UnitSkills = new ArrayList<LaunchSkill>();
        [Desc("变身改变技能时，保留的技能ID", "变身 - 技能")]
        [TemplatesID(typeof(SkillTemplate)), Expandable]
        [DependOnProperty(nameof(UnitChangeSkills))]
        public ArrayList<int> UnitKeepSkillsID = new ArrayList<int>();
    }
    //---------------------------------------------------------------------------------//

}
