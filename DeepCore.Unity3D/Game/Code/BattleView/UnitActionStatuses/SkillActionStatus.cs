using DeepCore.Game3D.Slave.Layer;
using DeepCore.GameData.Zone;
using UnityEngine;

namespace Code.BattleView.UnitActionStatuses
{
    public class SkillActionStatus : ActionStatus
    {
        private LayerUnit.ISkillAction skillAction;

        public SkillActionStatus(DeepCore.GameData.Data.UnitActionStatus status, string key) : base(status, key)
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

        protected override void OnUpdate(UnityBattleUnit owner, float deltaTime)
        {
        }

        private void UpdateCardPlay(UnityBattleUnit owner, float deltaTime)
        {
            if (Vector3.Distance(owner.Transform.localPosition, MoveDistance) <= 0.1f)
            {
                //start play back
                // play move action
                owner.Transform.localPosition = Vector3.Lerp(MoveDistance, PositionOriginal, deltaTime);
                // play scale action
                owner.Transform.localScale = Vector3.Lerp(ScaleTarget, ScaleOriginal, deltaTime);
            }
            else
            {
                //start play forward
                owner.Transform.localPosition = Vector3.Lerp(PositionOriginal, MoveDistance, deltaTime);
                owner.Transform.localScale = Vector3.Lerp(ScaleOriginal, ScaleTarget, deltaTime);
            }
            
        }

        public virtual void ZUnit_OnSkillActionStart(UnityBattleUnit owner, LayerUnit.ISkillAction skillAction)
        {
            this.skillAction = skillAction;
            this.ActionName = skillAction.CurrentActionName;
        }

        private readonly Vector3 Move = new(4, 4, 4);
        private readonly Vector3 ScaleCoefficient = new(0.7f, 1.3f, 1f);
        private Vector3 MoveDistance;
        private Vector3 ScaleTarget;
        private Vector3 PositionOriginal;
        private Vector3 ScaleOriginal;
        
        
        
    }
}
