using System.Collections.Generic;
using Code.HUD;
using Code.System.Pool;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.GameData.Data;
using DeepCore.GameData.Zone;

namespace Code.BattleView
{
    public class SkillSnap
    {
        
    }
    
    public partial class UnityBattleActor : UnityBattleObject
    {
        public LayerPlayer ZonePlayer => ZoneObject as LayerPlayer;
        public AnimationPlayer Anim;
        public List<LayerUnit.SkillState> Skills;
        
        protected override void OnInit()
        {
            Skills = ZonePlayer.GetSkillStatus();
            ZonePlayer.OnActionChanged += OnActorActionChanged;
        }

        private void OnActorActionChanged(LayerUnit unit, UnitActionStatus status, object msg)
        {
            
        }

        protected override void OnUpdate(int deltaMS)
        {
            
        }

        protected override void OnClear()
        {
        }

        protected override void Disposing()
        {
            Skills.Clear();
            Skills = null;
            
            ObjectPool<UnityBattleActor>.Release(this);
        }

        public void LaunchSkill(int id)
        {
            ZonePlayer.SendUnitLaunchSkill(id);
        }

        public void LaunchNormalAttack()
        {
            ZonePlayer.SendUnitLaunchNormalAttack();
        }
        
        
        
    }
}
