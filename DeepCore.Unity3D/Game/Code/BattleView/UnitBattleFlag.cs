using Code.BattleView.GizmosUtils;
using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepGame3D.Unity.BattleView;
using UnityEngine;

namespace Code.BattleView
{
    public abstract class UnityBattleFlag : Disposable
    {
        public GameObject GameObject { get; private set; }

        #region public Transform Transform { get; }
        private Transform _transform;
        public Transform Transform
        {
            get
            {
                if (!_transform)
                {
                    _transform = GameObject.transform;
                }
                return _transform;
            }
        }
        #endregion

        public UnityBattle Battle { get; private set; }
        public LayerFlag ZoneObject { get; private set; }
        public bool IsTurnable { get; protected set; }
        public GizmosCylinder GizmosCylinder { get; private set; }


        public virtual UnityBattleFlag Init(UnityBattle battle, LayerFlag obj, GameObject parent)
        {
            ZoneObject = obj;
            Battle = battle;
            this.GameObject = new GameObject(obj.Name);
            this.GameObject.transform.SetParent(parent.transform, false);
            this.GameObject.transform.localPosition = obj.ToUnityPosition();
            this.GameObject.transform.localRotation = obj.ToUnityRotation();
            if (Application.isEditor)
            {
                GizmosCylinder = GameObject.AddComponent<GizmosCylinder>();
                GizmosCylinder.Radians = ZoneObject.Direction;
                GizmosCylinder.Height = ZoneObject.BodyHeight;
                GizmosCylinder.Radius = ZoneObject.BodyBlockSize;
            }
            return this;
        }
        public virtual void Update(int deltaMS)
        {
            Transform.position = ZoneObject.ToUnityPosition();
            Transform.rotation = ZoneObject.ToUnityRotation();
            if (GizmosCylinder)
            {
                GizmosCylinder.Radians = ZoneObject.Direction;
                GizmosCylinder.Height = ZoneObject.BodyHeight;
                GizmosCylinder.Radius = ZoneObject.BodyBlockSize;
            }
        }
        protected override void Disposing()
        {
            if (GameObject)
            {
                UnityEngine.Object.Destroy(GameObject);
                GameObject = null;
            }
            Battle = null;
        }


    }

    public class UnityBattleDecoration : UnityBattleFlag
    {

    }
    public class UnityBattleRegion : UnityBattleFlag
    {

    }
    public class UnityBattlePoint : UnityBattleFlag
    {

    }
}
