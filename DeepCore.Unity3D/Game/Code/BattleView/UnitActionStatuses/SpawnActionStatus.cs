using Code.BattleView.MaterialActions;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.GameData.Zone;
using UnityEngine;

namespace Code.BattleView.UnitActionStatuses
{
    public class SpawnActionStatus : ActionStatus
    {
        public SpawnActionStatus(DeepCore.GameData.Data.UnitActionStatus status, string key) : base(status, key)
        {
        }

        protected override void OnStart(UnityBattleUnit owner)
        {
            base.OnStart(owner);
            
            var action = System.Pool.ObjectPool<TeleportAction>.Get();
            action.Init(owner.GameObject, 1f, 0f, owner.ZoneUnit.Info.SpawnTimeMS);
            owner.DoMaterialAction(action, false);
        }
    }
}
