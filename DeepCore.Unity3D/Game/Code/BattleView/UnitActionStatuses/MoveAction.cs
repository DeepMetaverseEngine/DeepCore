using DeepCore.Game3D.Slave.Layer;
using DeepCore.GameData.Data;
using UnityEngine;

namespace Code.BattleView.UnitActionStatuses
{
    public class MoveAction : ActionStatus
    {
        private readonly Vector3 Scale = new (1.2f, 1.2f, 1.2f);
        
        private Vector3 OriginalScale;
        
        public MoveAction(UnitActionStatus status, string key) : base(status, key)
        {
            
        }

        protected override void OnStart(UnityBattleUnit owner)
        {
            OriginalScale = owner.Transform.localScale;
        }

        protected override void OnStop(UnityBattleUnit owner)
        {
            base.OnStop(owner);
            owner.Transform.localScale = OriginalScale;
        }

        protected override void OnUpdate(UnityBattleUnit owner, float deltaTime)
        {
            base.OnUpdate(owner, deltaTime);
            
            owner.Transform.localScale = Vector3.Lerp(Vector3.one, Scale, deltaTime);
        }
    }
}