using Code.System.Pool;
using DeepCore.Game3D.Slave.Layer;

namespace Code.BattleView
{
    public class UnityBattleItem : UnityBattleObject
    {
        public LayerItem ZoneItem => ZoneObject as LayerItem;
        
        protected override void OnInit()
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
            ObjectPool<UnityBattleItem>.Release(this);
        }
    }
}
