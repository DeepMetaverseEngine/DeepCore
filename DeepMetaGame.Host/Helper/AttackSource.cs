using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.IO;
using DeepCore.XCSV;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Net.Security;

namespace DeepCore.Game3D.Host.Helper
{
    public class TAttackSource : InstanceStatus
    {
        public InstanceZone Zone { get; private set; }
        public uint AttackGUID { get; private set; }
        /// <summary>
        /// 攻击属性.
        /// </summary>
        public AttackProp Attack { get; private set; }
        /// <summary>
        /// 技能模板.
        /// </summary>
        public SkillTemplate FromSkill { get; private set; }
        /// <summary>
        /// 法术模板.
        /// </summary>
        public SpellTemplate FromSpell { get; private set; }
        /// <summary>
        /// BUFF模板.
        /// </summary>
        public BuffTemplate FromBuff { get; private set; }
        /// <summary>
        /// 技能实际状态.
        /// </summary>
        public InstanceUnit.EquipSkill FromSkillState { get; private set; }
        /// <summary>
        /// BUFF实际状态.
        /// </summary>
        public InstanceUnit.EquipBuff FromBuffState { get; private set; }
        /// <summary>
        /// 法术实际状态.
        /// </summary>
        public InstanceSpell FromSpellUnit { get; private set; }

        /// <summary>
        /// 扩展自定义数据
        /// </summary>
        public byte CustomData;

        public object Tag;

        protected override void Disposing()
        {
            this.Zone = default;
            this.AttackGUID = 0;
            this.Attack = default;
            this.FromSkill = default;
            this.FromSpell = default;
            this.FromBuff = default;
            this.FromSkillState?.Release();
            this.FromSkillState = default;
            this.FromBuffState?.Release();
            this.FromBuffState = default;
            this.FromSpellUnit?.Release();
            this.FromSpellUnit = default;
            this.CustomData = default;
            this.Tag = default;
        }
        public static TAttackSource AllocWithSkill(InstanceUnit.EquipSkill skill, AttackProp attack)
        {
            var alloc = new TAttackSource();
            alloc.Zone = skill.Zone;
            alloc.AttackGUID = skill.Zone.genAttackGUID();
            alloc.Attack = attack;

            alloc.FromSkill = skill.Data;
            alloc.FromSkillState = skill;
            alloc.FromSkillState.Retain();

            alloc.FromSpell = null;
            alloc.FromSpellUnit = null;

            alloc.FromBuff = null;
            alloc.FromBuffState = null;

            return alloc;
        }
        public static TAttackSource AllocWithBuff(InstanceUnit.EquipBuff buff, AttackProp attack)
        {
            var alloc = new TAttackSource();
            alloc.Zone = buff.Zone;
            alloc.AttackGUID = buff.Zone.genAttackGUID();
            alloc.Attack = attack;

            alloc.FromSkill = null;
            alloc.FromSkillState = null;

            alloc.FromSpell = null;
            alloc.FromSpellUnit = null;

            alloc.FromBuff = buff.Data;
            alloc.FromBuffState = buff;
            alloc.FromBuffState.Retain();
            return alloc;
        }
        public static TAttackSource AllocWithSpell(InstanceSpell spell, AttackProp attack)
        {
            var alloc = new TAttackSource();
            alloc.Zone = spell.Zone;
            alloc.AttackGUID = spell.Zone.genAttackGUID();
            alloc.Attack = attack;

            alloc.FromSkill = null;
            alloc.FromSkillState = null;

            alloc.FromSpell = spell.Info;
            alloc.FromSpellUnit = spell;
            alloc.FromSpellUnit.Retain();

            alloc.FromBuff = null;
            alloc.FromBuffState = null;
            return alloc;
        }
        public static TAttackSource AllocWithAttack(in TAttackSource src, AttackProp attack)
        {
            var alloc = new TAttackSource();
            alloc.Zone = src.Zone;
            alloc.AttackGUID = src.Zone.genAttackGUID();
            alloc.Attack = attack;

            alloc.FromSkill = src.FromSkill;
            alloc.FromSkillState = src.FromSkillState;
            alloc.FromSkillState?.Retain();

            alloc.FromSpell = src.FromSpell;
            alloc.FromSpellUnit = src.FromSpellUnit;
            alloc.FromSpellUnit?.Retain();

            alloc.FromBuff = src.FromBuff;
            alloc.FromBuffState = src.FromBuffState;
            alloc.FromBuffState?.Retain();

            alloc.Tag = src.Tag;
            alloc.CustomData = src.CustomData;

            return src;
        }
        public TemplateData FromWeapon
        {
            get
            {
                if (FromSkill != null) { return FromSkill; }
                if (FromSpell != null) { return FromSpell; }
                if (FromBuff != null) { return FromBuff; }
                return null;
            }
        }

        public SkillTemplate.CastTarget FromExpectTarget
        {
            get
            {
                if (FromSkill != null)
                {
                    return FromSkill.ExpectTarget;
                }
                if (FromSpell != null)
                {
                    return FromSpell.ExpectTarget;
                }
                return SkillTemplate.CastTarget.NA;
            }
        }

        public bool TryGetSrourceSkill(out InstanceUnit.EquipSkill temp)
        {
            if (FromSkillState != null && !FromSkillState.IsDisposing)
            {
                temp = FromSkillState;
                return temp != null;
            }
            if (FromSpellUnit != null && FromSpellUnit.FromSkillTemplateID != null && !FromSpellUnit.FromSkillTemplateID.IsDisposing)
            {
                temp = FromSpellUnit.FromSkillTemplateID;
                return temp != null;
            }
            if (FromBuffState != null && FromBuffState.FromSkillID != null && !FromBuffState.FromSkillID.IsDisposing)
            {
                temp = FromBuffState.FromSkillID;
                return temp != null;
            }
            temp = null;
            return false;

        }

    }

    public struct TAttackResult
    {
        /// <summary>
        /// 是否发送协议
        /// </summary>
        public bool OutSendEvent;

        /// <summary>
        /// 是否命中
        /// </summary>
        public bool OutHitted;

        /// <summary>
        /// 是否产生硬直.
        /// </summary>
        public bool OutIsDamage;

        /// <summary>
        /// 是否击溃.
        /// </summary>
        public bool OutIsCrush;

        /// <summary>
        /// 是否暴击.
        /// </summary>
        public bool OutIsCritical;

        /// <summary>
        /// 是否击飞.
        /// </summary>
        public bool OutHasFly;

        /// <summary>
        /// 是否击倒.
        /// </summary>
        public bool OutHasKnockDown;

        /// <summary>
        /// 击倒时间.
        /// </summary>
        public int OutKnockDownTimeMS;
        /// <summary>
        /// 是否对死亡单位启效
        /// </summary>
        public bool OutCanWhiplashDeadBody;
        /// <summary>
        /// 打击特效.
        /// </summary>
        public LaunchEffect OutHitEffect;
        public float OutWeight;
        public StartMove OutHitMove;
        /// <summary>
        /// 实际掉血，如果伤害为100,实际血量为10,则该值为10
        /// </summary>
        public long OutReducedHP;

        /// <summary>
        /// 用于表示攻击特殊状态：招架、闪避、反伤.
        /// </summary>
        public string OutClientState;

        /// <summary>
        /// 扩展模块用于特殊计算.
        /// </summary>
        public ISerializable OutExtendsResult;

        /// <summary>
        /// 扩展自定义数据
        /// </summary>
        public byte CustomData;

        public object Tag;

        public TAttackResult(in TAttackSource src, InstanceUnit target)
        {
            this.CustomData = src.CustomData;
            this.OutSendEvent = true;
            this.OutIsCrush = CUtils.RandomPercent(target.RandomN, src.Attack.CrushPercent);
            this.OutIsCritical = src.Attack.MaskMustCritical;
            this.OutHitEffect = src.Attack.Effect;
            this.OutWeight = (src.Attack.Weight - target.Weight);
            this.OutIsDamage = (OutWeight >= 0) && (src.Attack.MaskDamage) && (!target.IsNoneBlock);
            this.OutHitted = true;
            if (OutIsDamage)
            {
                this.OutHasFly = src.Attack.IsHitFly;
                this.OutHasKnockDown = src.Attack.MaskKnockDown;
                this.OutKnockDownTimeMS = src.Attack.KnockOutTimeMS;
                this.OutHitMove = src.Attack.HitMove;
            }
            else
            {
                this.OutHasFly = false;
                this.OutHasKnockDown = false;
                this.OutKnockDownTimeMS = 0;
                this.OutHitMove = null;
            }
            if (src.FromSpellUnit != null && src.FromSpellUnit.ChainInfo != null)
            {
                src.FromSpellUnit.ChainInfo.AddTarget(target);
            }
        }
        public void AddClientState(int index)
        {
            char ef = Convert.ToChar(index);
            OutClientState = OutClientState == null ? new string(ef, 1) : OutClientState + ef;
        }

    }

}

