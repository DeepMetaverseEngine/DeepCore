using DeepCore.EventTrigger;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.Instance.Triggers;
using DeepCore.Geometry;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;

namespace DeepCore.Game3D.Host.Instance
{

    //--------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 所有自动电脑AI
    /// </summary>
    public partial class InstanceBehaviorUnit : InstanceUnit
    {
        protected NpcAIComponent mAI;
        public NpcAIComponent AI => mAI;
        public InstanceBehaviorUnit(InstanceZone zone, TAddUnit add)
            : base(zone, add)
        {
        }
        protected override void OnInitFormula(InstanceUnitFormula Formula, TAddUnit add)
        {
            this.mAI = InitAI();
            base.OnInitFormula(Formula, add);
        }
        protected virtual NpcAIComponent InitAI()
        {
            return Components.AddComponent<NpcAIComponent>();
        }
        public override bool StartAttackTo(InstanceFlag path)
        {
            if (mAI != null)
            {
                return mAI.StartAttackTo(path);
            }
            return base.StartAttackTo(path);
        }
        public override bool StartFollowAndAttack(InstanceUnit target, AttackReason reason, SkillTemplate.CastTarget castTarget = SkillTemplate.CastTarget.Enemy, EquipSkill equipSkill = null)
        {
            if (mAI != null)
            {
                return mAI.StartFollowAndAttack(target, reason, equipSkill);
            }
            return base.StartFollowAndAttack(target, reason, castTarget, equipSkill);
        }
        public override bool StartBackToOrgin(Vector3? mOrginPosition)
        {
            if (mAI != null)
            {
                return mAI.StartBackToOrgin(mOrginPosition);
            }
            return base.StartBackToOrgin(mOrginPosition);
        }
        public override bool StartGuardUnit(InstanceUnit vip)
        {
            if (mAI != null)
            {
                return mAI.StartGuardUnit(vip);
            }
            return base.StartGuardUnit(vip);
        }
        public override bool StartGuardInPosition(Vector3? pos)
        {
            if (mAI != null)
            {
                return mAI.StartGuardInPosition(pos);
            }
            return base.StartGuardInPosition(pos);
        }
        public override bool StartMoveScatterTarget(InstanceUnit target)
        {
            if (mAI != null)
            {
                return mAI.StartMoveScatterTarget(target);
            }
            return base.StartMoveScatterTarget(target);
        }
        public override bool StartIdleMove(Vector3 pos, float timeMS, float range)
        {
            if (mAI != null)
            {
                return mAI.StartIdleMove(timeMS, range);
            }
            return base.StartIdleMove(pos, timeMS, range);
        }
    }

    //--------------------------------------------------------------------------------------------------------


}
