using DeepCore.Game3D.Slave.Layer;
using DeepCore.GameData.Zone;
using UnityEngine;

namespace Code.BattleView.UnitActionStatuses
{
    public class ChaosActionStatus : ActionStatus
    {
        private LayerUnit.ISkillAction skillAction;

        public ChaosActionStatus(DeepCore.GameData.Data.UnitActionStatus status, string key) : base(status, key)
        {
        }

        public LayerUnit.ISkillAction SkillAction
        {
            get { return skillAction; }
        }

        public SkillTemplate Data
        {
            get { return skillAction.SkillData; }
        }

        protected override void OnStart(UnityBattleUnit owner)
        {
        }

        protected override void OnStop(UnityBattleUnit owner)
        {
            base.OnStop(owner);
            skillAction = null;
        }

        protected override void OnUpdate(UnityBattleUnit owner, float delteTime)
        {
        }

        public virtual void ZUnit_OnSkillActionStart(UnityBattleUnit owner, LayerUnit.ISkillAction skillAction)
        {
            this.skillAction = skillAction;
            this.ActionName = skillAction.CurrentActionName;
            if (owner.Anim)
            {
                if (CrossFade)
                {
                    owner.Anim.CrossFade(this.ActionName, 0.15f, -1, 0f);
                }
                else
                {
                    owner.Anim.Play(this.ActionName, -1, 0f);
                }
            }
        }
    }
}
