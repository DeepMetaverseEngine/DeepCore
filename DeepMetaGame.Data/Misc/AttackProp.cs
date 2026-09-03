using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;

namespace DeepMetaGame.Data.Misc
{
    /// <summary>
    /// 技能或者法术产生的攻击属性
    /// </summary>
    [MessageType(BattleConstants.AttackProp)]
    [Desc("技能或者法术产生的攻击属性")]
    [Expandable]
    public class AttackProp : ISNData, IPropertiesOwner
    {
        /// <summary>
        /// 攻击力
        /// </summary>
        [Desc("攻击力")]
        public int Attack = 1;

        [Desc("产生受击", "Mask")]
        public bool MaskDamage = false;
        [Desc("产生击倒", "Mask")]
        public bool MaskKnockDown = false;
        [Desc("此次攻击必命中，即使命中为0", "Mask")]
        public bool MaskMustHit = false;
        [Desc("此次攻击必暴击，即使暴击为0", "Mask")]
        public bool MaskMustCritical = false;



        [Desc("击中后影响体重(使目标受击必须大于体重)", "受击")]
        [DependOnProperty(nameof(MaskDamage))]
        public int Weight = 1;
        [Desc("单位被(受击、击倒、眩晕、混乱)持续时间(毫秒)" +
            "\n * 此时间如果为0，首先选用单位DamageTimeMS，否则然后采用Config环境参数OBJECT_DAMAGE_TIME_MS替代之。" +
            "\n * HitMove运动时间不计算在KnockOutTimeMS内。", "受击")]
        [DependOnProperty(nameof(MaskDamage))]
        public int KnockOutTimeMS = 0;
        [Desc("受击动作名", "受击")]
        [DependOnProperty(nameof(MaskDamage))]
        public string DamageActionName = null;
        [Desc("受击时，不再遭受其他攻击", "受击")]
        [DependOnProperty(nameof(MaskDamage))]
        public bool IsDamageProtect = false;

        [Desc("被击中后向后移动距离(包括击飞控制)", "受击")]
        [DependOnProperty(nameof(MaskDamage))]
        public StartMove HitMove;

        [DependOnProperty(nameof(MaskDamage))] public bool IsHitMove { get { return MaskDamage && HitMove != null; } }
        [DependOnProperty(nameof(MaskDamage))] public bool IsHitFly { get => MaskDamage && HitMove != null && HitMove.HasFly; }
        public enum HitMoveType : byte
        {
            [Desc("根据攻击者位置（台球碰撞）")]
            BySenderPosition,
            [Desc("根据攻击者朝向（单向）")]
            BySenderDirection,
            [Desc("根据攻击者朝向的左右边（摩西分海）")]
            BySenderLeftRight,
            [Desc("位移至攻击者中心（向里吸）")]
            ToSenderCenter,
            [Desc("位移至攻击者身边（向里吸）")]
            ToSenderBodySize,
        }
        [Desc("受击位移计算方式", "受击")]
        [DependOnProperty(nameof(IsHitMove))]
        public HitMoveType HitMoveMType = HitMoveType.BySenderPosition;
        [Desc("受击位移根据Spell施法者计算", "受击")]
        [DependOnProperty(nameof(IsHitMove))]
        public bool HitMoveBySpellLauncher = false;
        [Desc("受击位移时对其他单位产生伤害", "受击")]
        [DependOnProperty(nameof(IsHitMove))]
        public AttackProp HitMoveBodyAttack;
        [Desc("基于被攻击者身体范围的增加范围", "受击")]
        [DependOnProperty(nameof(IsHitMove))]
        public float HitMoveBodyAttackSize = 1;
        [Desc("被击飞落地后产生伤害，或者位移结束产生伤害", "受击")]
        [DependOnProperty(nameof(IsHitFly))]
        public AttackProp FlyFallenDownAttack;


        [Desc("被击中特效", "被击中")]
        public LaunchEffect Effect;
        [Desc("被击中触发一个BUFF", "被击中")]
        public LaunchBuff Buff;
        [Desc("被击中触发一个Spell", "被击中")]
        public LaunchSpell Spell;


        [Desc("百分比概率击碎目标", "击碎")]
        public float CrushPercent = 0;
        [Desc("击碎目标效果", "击碎")]
        public LaunchEffect CrushEffect;

        [Desc("定帧时长", "客户端")]
        public int StopFrameMS = 0;
        [Desc("定帧类型", "客户端")]
        public string StopFrameAction;

        /// <summary>
        /// 用户自定义扩展属性
        /// </summary>
        [Desc("用户自定义扩展属性", "扩展")]
        [Expandable]
        [NotNull]
        public IAttackProperties Properties;
        public IPropertiesData PropertiesData => this.Properties;

        public AttackProp()
        {
            Properties = ZoneDataFactory.Factory.CreateProperties<IAttackProperties>(this);
        }
        /// <summary>
        /// 是否产生受击
        /// </summary>
        /// <returns></returns>
        public bool IsDamage()
        {
            return MaskDamage || MaskKnockDown;
        }
        public override string ToString()
        {
            return "攻击";
        }


    }

}
