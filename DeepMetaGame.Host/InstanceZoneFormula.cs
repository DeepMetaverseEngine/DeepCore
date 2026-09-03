using DeepCore.Game3D.Host.Helper;
using DeepCore.Geometry;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using static DeepCore.EventTrigger.Data.IntegerValue;
using static DeepMetaGame.Data.Template.SkillTemplate;

namespace DeepCore.Game3D.Host.Instance
{
    public class InstanceZoneFormula : Disposable
    {
        public static T Alloc<T>(InstanceZone owner) where T : InstanceZoneFormula, new()
        {
            return owner.ObjectPool.Alloc<T>().Init(owner) as T;
        }
        public InstanceZone Zone { get; private set; }
        public virtual InstanceZoneFormula Init(InstanceZone owner)
        {
            this.Zone = owner;
            return this;
        }
        protected virtual internal void OnInit() { }
        protected override void Disposing()
        {
            Zone = null;
        }
        //--------------------------------------------------------------------------------------------
        #region HIT_AND_DAMAGE


        /// <summary>
        /// 测试是否可见，单位间可交互（AOI）
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public virtual bool IsVisibleAOI(InstanceZoneObject src, InstanceZoneObject dst)
        {
            if (dst is InstanceUnit)
            {
                return (dst as InstanceUnit).IsVisible;
            }
            return true;
        }

        public virtual bool IsExpectTarget(InstanceUnit src, InstanceUnit target, SkillTemplate.CastTarget expectTarget)
        {
            switch (expectTarget)
            {
                case SkillTemplate.CastTarget.Enemy:
                    return src.Force != target.Force;

                case SkillTemplate.CastTarget.Enemy_Monster:
                    return src.Force != target.Force && src.UType == UnitType.TYPE_MONSTER;

                case SkillTemplate.CastTarget.Enemy_Player:
                    return src.Force != target.Force && src.UType == UnitType.TYPE_PLAYER;


                case SkillTemplate.CastTarget.PetForMaster:
                    if (src is InstancePet)
                    {
                        return (src as InstancePet).Master == target;
                    }
                    return false;
                case SkillTemplate.CastTarget.AlliesExcludeSelf:
                    return (src != target) && (src.Force == target.Force);
                //case SkillTemplate.CastTarget.AlliesExcludeSelf:
                //    return (src != target) && (src.Force == target.Force);

                case SkillTemplate.CastTarget.AlliesIncludeSelf:
                    return (src.Force == target.Force);

                case SkillTemplate.CastTarget.EveryOne:
                    return true;
                case SkillTemplate.CastTarget.EveryOneExcludeSelf:
                    return (src != target);

                case SkillTemplate.CastTarget.Self:
                    return src == target;
                case SkillTemplate.CastTarget.NA:
                default:
                    return false;
            }
            //return false;
        }

        /// <summary>
        /// 是否为友军
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual bool IsAllies(InstanceUnit src, InstanceUnit target, AttackReason reason = AttackReason.Look)
        {
            var ret = false;
            var unit = src;
            if (unit != null && unit.Parent != null)
            {
                return unit.Parent.Formula.IsAttackable(unit, target, CastTarget.AlliesIncludeSelf, reason, unit.Info);
            }
            return ret;
        }
        /// <summary>
        /// 测试是否可攻击
        /// </summary>
        /// <param name="src"></param>
        /// <param name="target"></param>
        /// <param name="expectTarget"></param>
        /// <param name="reason"></param>
        /// <param name="weapon"></param>
        /// <returns></returns>
        public virtual bool IsAttackable(InstanceUnit src, InstanceUnit target, SkillTemplate.CastTarget expectTarget, AttackReason reason, TemplateData weapon = null)
        {
            if (!IsVisibleAOI(src, target))
            {
                return false;
            }
            if (!target.IsActive)
                return false;
            if (!target.IsVisible)
                return false;
            if (target.IsInvincible)
                return false;
            if (!target.IsAttackable)
                return false;
            return IsExpectTarget(src, target, expectTarget);
        }
        /// <summary>
        /// 测试是否可攻击
        /// </summary>
        /// <param name="src"></param>
        /// <param name="target"></param>
        /// <param name="expectTarget"></param>
        /// <param name="reason"></param>
        /// <param name="weapon"></param>
        /// <returns></returns>
        public virtual bool IsAttackableBySkill(InstanceUnit src, InstanceUnit target, InstanceUnit.EquipSkill equipSkill, AttackReason reason)
        {
            return IsAttackable(src, target, equipSkill.Data.ExpectTarget, reason, equipSkill.Data);
        }


        /// <summary>
        /// 单位受击公式，可指定伤害以及控制效果
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="attack"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual long OnHit(InstanceUnit attacker, TAttackSource attack, ref TAttackResult result, InstanceUnit target)
        {
            var atk = attack.Attack.Attack;
            var hp = target.MaxHP;
            var damage = attacker.RandomN.NextFloat(hp / 10, hp / 5) * atk;
            return (long)damage;
        }


        public virtual void OnKillDropItem(InstanceUnit unit, InstanceUnit dead, UnitDropItemAbility drop)
        {
            unit.AddExp(drop.GenExp);
            unit.AddMoney(drop.DropMoney);
        }
        public virtual bool TryLevelUP(InstanceUnit unit, long oldExp, long newExp)
        {
            var ret = false;
            while (true)
            {
                var needExp = Zone.DataRoot.DataCenter.GetUnitNeedExp(Zone.Data, unit.Info, unit.Level + 1);
                if (newExp > needExp)
                {
                    unit.Level += 1;
                    ret = true;
                }
                else if (newExp == needExp)
                {
                    unit.Level += 1;
                    ret = true;
                    break;
                }
                else
                {
                    break;
                }
            }
            return ret;

        }



        //--------------------------------------------------------------------------------------------


        /// <summary>
        /// 开始释放技能
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="skill"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual bool TryLaunchSkill(InstanceUnit attacker, InstanceUnit.EquipSkill skill, ref InstanceUnit.TLaunchSkillParam param) { return true; }
        /// <summary>
        /// 尝试重置技能
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public virtual bool TryResetSkill(InstanceUnit owner) { return true; }
        /// <summary>
        /// 尝试释放法术
        /// </summary>
        /// <param name="launcher"></param>
        /// <param name="spell"></param>
        /// <returns></returns>
        public virtual bool TryLaunchSpell(InstanceUnit launcher, ref SpellTemplate spell) { return true; }
        /// <summary>
        /// 尝试添加BUFF
        /// </summary>
        /// <param name="add"></param>
        /// <returns></returns>
        public virtual bool TryAddBuff(ref TAddBuff add) { return true; }

        /// <summary>
        /// 尝试按层删除BUFF
        /// </summary>
        /// <param name="bs"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public virtual bool TryRemoveBuff(InstanceUnit.EquipBuff bs, int level, byte result) { return true; }

        /// <summary>
        /// 尝试删除BUFF
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        public virtual bool TryRemoveBuff(InstanceUnit.EquipBuff bs, byte result) { return true; }
        /// <summary>
        /// 尝试召唤
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="summon"></param>
        /// <param name="summonUnit"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual bool TrySummonUnit(InstanceUnit owner, SummonUnit summon, ref UnitInfo summonUnit, ref string name) { return true; }
        /// <summary>
        /// 尝试使用道具
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="item"></param>
        /// <param name="item_creater"></param>
        /// <returns></returns>
        public virtual bool TryUseItem(InstanceUnit owner, ItemTemplate item, InstanceUnit item_creater) { return true; }
        /// <summary>
        /// 尝试释放光环
        /// </summary>
        /// <param name="target"></param>
        /// <param name="aura"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public virtual bool TryLaunchAura(InstanceUnit target, AuraTemplate aura, int level) { return true; }
        /// <summary>
        /// 尝试目标单位进入光环
        /// </summary>
        /// <param name="aura"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual bool TryEnterAura(InstanceUnit.EquipAura aura, InstanceUnit target) { return true; }

        public virtual bool TryPutCard(InstanceUnit owner, CardSlot slot, CardTemplate card) { return true; }

        public virtual bool TryAddSpell(InstanceSpell spell, TAddSpell add) { return true; }

        #endregion

        //--------------------------------------------------------------------------------------------

        public virtual void SortSeekingTarget(Random RandomN, SkillTemplate skill, Vector3 pos, LaunchSkill.SeekingExpect expect, List<InstanceUnit> list)
        {
            switch (expect)
            {
                case LaunchSkill.SeekingExpect.Random:
                    CUtils.RandomList(RandomN, list);
                    break;
                case LaunchSkill.SeekingExpect.Nearest:
                    list.Sort(new ObjectSorterNearest<InstanceUnit>(pos));
                    break;
                case LaunchSkill.SeekingExpect.Farthest:
                    list.Sort(new ObjectSorterFarthest<InstanceUnit>(pos));
                    break;
                case LaunchSkill.SeekingExpect.HP_Max:
                    list.Sort(new UnitSorterMaxHP());
                    break;
                case LaunchSkill.SeekingExpect.HP_Min:
                    list.Sort(new UnitSorterMinHP());
                    break;
                case LaunchSkill.SeekingExpect.HP_Ratio_Min:
                    list.Sort(new UnitSorterMinHPRatio());
                    break;
            }
        }
        public virtual void SortSeekingTarget(Random RandomN, SpellTemplate spell, Vector3 pos, LaunchSkill.SeekingExpect expect, List<InstanceUnit> list)
        {
            switch (expect)
            {
                case LaunchSkill.SeekingExpect.Random:
                    CUtils.RandomList(RandomN, list);
                    break;
                case LaunchSkill.SeekingExpect.Nearest:
                    list.Sort(new ObjectSorterNearest<InstanceUnit>(pos));
                    break;
                case LaunchSkill.SeekingExpect.Farthest:
                    list.Sort(new ObjectSorterFarthest<InstanceUnit>(pos));
                    break;
                case LaunchSkill.SeekingExpect.HP_Max:
                    list.Sort(new UnitSorterMaxHP());
                    break;
                case LaunchSkill.SeekingExpect.HP_Min:
                    list.Sort(new UnitSorterMinHP());
                    break;
                case LaunchSkill.SeekingExpect.HP_Ratio_Min:
                    list.Sort(new UnitSorterMinHPRatio());
                    break;
            }
        }

        //--------------------------------------------------------------------------------------------
        #region STATUS

        public virtual void OnBuffBegin(InstanceUnit unit, InstanceUnit.EquipBuff buff, InstanceUnit sender) { }
        public virtual void OnBuffTick(InstanceUnit unit, InstanceUnit.EquipBuff buff, int time) { }
        public virtual void OnBuffEnd(InstanceUnit unit, InstanceUnit.EquipBuff buff, byte result) { }
        public virtual void OnGotInventoryItem(InstanceUnit unit, ItemTemplate item) { }
        public virtual void OnLostInventoryItem(InstanceUnit unit, ItemTemplate item) { }
        public virtual void OnUseItem(InstanceUnit unit, ItemTemplate item, InstanceUnit item_creater) { }

        #endregion
        //--------------------------------------------------------------------------------------------
        // public virtual void UnitHitEventOverride(InstanceUnit attacker, AttackSource source, InstanceUnit instanceUnit, int reduceHp, int oldHp) { }



    }
}
