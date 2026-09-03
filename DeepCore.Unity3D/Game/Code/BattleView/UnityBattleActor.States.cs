using Code.BattleView.UnitActionStatuses;
using DeepCore.GameData.Data;

namespace Code.BattleView;

public partial class UnityBattleActor
{
    
    public class State : ActionStatus
    {
        public State(UnitActionStatus status, string key, string animName = "", bool crossFade = false, float speed = 1) 
            : base(status, key, animName, crossFade, speed)
        {
            
        }
    }
    
    
    
    
}