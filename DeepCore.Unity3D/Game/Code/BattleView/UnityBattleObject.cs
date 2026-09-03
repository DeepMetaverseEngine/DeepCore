using Code.BattleView.GizmosUtils;
using Code.System.Pool;
using Code.System.Tick;
using Code.Utility;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.GameData.Zone;
using DeepGame3D.Unity.BattleView;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.BattleView
{
    public abstract partial class UnityBattleObject : ICleanable, IPoolable
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
        public LayerZoneObject ZoneObject { get; private set; }
        public bool IsTurnable { get; protected set; }
        public GizmosCylinder GizmosCylinder { get; private set; }

        public UnityBattleObject Init(UnityBattle battle, LayerZoneObject obj, GameObject parent)
        {
            ZoneObject = obj;
            Battle = battle;
            this.GameObject = new GameObject(obj.Name);
            this.GameObject.transform.SetParent(parent.transform, false);
            this.GameObject.transform.localPosition = obj.ToUnityPosition();
            this.GameObject.transform.localRotation = obj.ToUnityRotation();
            
            RegistAllObjectEvent();
            ZoneObject.OnDoEvent += ZoneObject_OnDoEvent;
            if (Application.isEditor)
            {
                GizmosCylinder = GameObject.AddComponent<GizmosCylinder>();
                GizmosCylinder.Radians = ZoneObject.Direction;
                GizmosCylinder.Height = ZoneObject.BodyHeight;
                GizmosCylinder.Radius = ZoneObject.BodyBlockSize;
            }
            OnInit();
            return this;
        }

        private void ZoneObject_OnDoEvent(LayerZoneObject obj, ObjectEvent e)
        {
            DoObjectEvent(e);
        }

        protected abstract void OnInit();

        public void Update(int deltaMS)
        {
            Transform.position = ZoneObject.ToUnityPosition();
            Transform.rotation = ZoneObject.ToUnityRotation();
            
            OnUpdate(deltaMS);
            UpdateMaterialActions(deltaMS);
            
            if (GizmosCylinder)
            {
                GizmosCylinder.Radians = ZoneObject.Direction;
                GizmosCylinder.Height = ZoneObject.BodyHeight;
                GizmosCylinder.Radius = ZoneObject.BodyBlockSize;
            }
        }

        protected abstract void OnUpdate(int deltaMS);

        public void Clear()
        {
            OnClear();
            ClearMaterialActions();

            ZoneObject.OnDoEvent -= ZoneObject_OnDoEvent;

            if (_bindEffectSerials.Count > 0)
            {
                foreach (var serial in _bindEffectSerials)
                {
                    TickSystem.TickCancel(serial);
                }
                _bindEffectSerials.Clear();
            }

            if (GameObject)
            {
                Object.Destroy(GameObject);
                GameObject = null;
            }

            Battle = null;
            if (ZoneObject != null)
            {
                ZoneObject.Dispose();
                ZoneObject = null;
            }
        }

        protected abstract void OnClear();

        public void Dispose()
        {
            Clear();
            Disposing();
        }

        protected abstract void Disposing();
    }
}