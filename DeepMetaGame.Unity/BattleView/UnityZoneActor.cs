using DeepCore.Game3D.Slave.Layer;
using DeepGame3D.Unity.BattleView;

namespace DeepMetaGame.Unity.BattleView
{
    public partial class UnityZoneActor : UnityZoneUnit
    {
        public LayerPlayer zActor => layerActor;
        public LayerPlayer layerActor => layerZoneObject as LayerPlayer;

        public UnityZoneActor(UnityZone zone) : base(zone) { }
    }
}
