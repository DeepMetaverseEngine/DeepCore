using DeepCore.Game3D.Host.Helper;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepCore.Game3D.Host.Instance.Components;

namespace DeepCore.Game3D.Host.Instance
{

    //--------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 场景内机关
    /// </summary>
    public class InstanceAttachment : InstanceUnit
    {
        public override bool IsActive => false;
        public override bool IsAttackable { get { return false; } }
        public override bool IntersectMap { get { return false; } }
        public override bool IntersectObj { get { return false; } }
        public InstanceUnit Master { get; }
        public UnitAutoAttackComponent AutoAttack { get; }
        public InstanceAttachment(InstanceZone zone, TAddUnit add)
            : base(zone, add)
        {
            this.Master = add.summoner;
            if (AGuard && !IsNoneSkill)
            {
                this. AutoAttack = this.Components.AddComponent<UnitAutoAttackComponent>();
                //comp.IsRandomSkillForAll = true;
                //comp.IsFaceToTarget = true;
            }
        }
        //---------------------------------------------------------------------------------------------------------
        #region ToMaster
        protected override bool TryAddExp(ref long value)
        {
            if (Master != null)
            {
                Master.AddExp(value);
                return false;
            }
            return base.TryAddExp(ref value);
        }
        protected override bool TryAddMoney(ref long value)
        {
            if (Master != null)
            {
                Master.AddMoney(value);
                return false;
            }
            return base.TryAddMoney(ref value);
        }
        protected override void LogAttack(InstanceUnit target, long reduceHP)
        {
            if (Master != null)
            {
                Master.Statistic.onAttack(target, reduceHP);
            }
            else
            {
                base.LogAttack(target, reduceHP);
            }
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------
        protected override void onUpdateAI()
        {
            base.onUpdateAI();
            //             if (CurrentState is StateSkill skill)
            //             {
            //                 SetActionStatus(UnitActionStatus.Skill);
            //             }
            //             else
            //             {
            //                 SetActionStatus(UnitActionStatus.Idle);
            //             }
        }
    }
}
