using DeepCore;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Slave.Runtime;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Unity.BattleView.UI;
using UnityEngine;

namespace DeepMetaGame.Unity.BattleView
{
    public interface IUnityBattleObject
    {
        GameObject gameObject { get; }
        Transform transform { get; }
    }

    public abstract class UnityPoolingObject : Recyclable, IPoolingObject, IUnityBattleObject
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(UnityPoolingObject));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        protected sealed override void RecordDisposing() { Alloc.RecordDispose(GetType()); }
        protected sealed override void RecordReuse() { Alloc.RecordReuse(GetType()); }

        public UnityZone parent { get; }
        public UnityZone zone { get => parent; }
        public UnityZoneObjectPool objectPool { get => parent.objectPool; }
        public UnityBattleConfig config { get => parent.config; }
        public UnityZoneSpaceTransverter Space { get => zone.Space; }
        public GameObject gameObject { get; private set; }
        public Transform transform { get; private set; }
        public UnityPoolingObject(UnityZone zone)
        {
            Alloc.RecordConstructor(GetType());
            this.parent = zone;
            this.gameObject = new GameObject();
            this.transform = gameObject.transform;
        }
        ~UnityPoolingObject()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        protected override void Destructing()
        {
            if (this.gameObject)
            {
                if (CanDispose)
                {
                    Debug.LogWarning($"{GetType()} : {this} On OnDestory Error! Someone holding this object!");
                }
                OnDestory();
                GameObject.Destroy(this.gameObject);
                this.transform = null;
                this.gameObject = null;
            }
        }
        protected abstract void OnDestory();
    }

    public abstract class UnityLayerObject : UnityPoolingObject, IPoolingObject
    {
        public bool IsActive { get => gameObject != null && gameObject.activeSelf; }
        public LayerObject layerObject { get; private set; }
        public HUDUnitHPBar HPBar { get; private set; }
        public AbstractBattle battle { get => parent?.battle; }
        public LayerZone layer { get => battle.Layer; }
        public TemplateManager templates => layer?.Templates;
        public UnityLayerObjectBeharvior mono { get; private set; }
        //public SingleThreadCollectionPool objectPool { get => parent.objectPool; }
        public string displayName { get => layerObject.DisplayName; }
        public UnityLayerObject(UnityZone zone) : base(zone)
        {
            mono = gameObject.AddComponent<UnityLayerObjectBeharvior>();
            mono.zoneObject = this;
        }
        public override string ToString()
        {
            return gameObject.name;
        }
        internal void Init(LayerObject zobj, GameObject parent)
        {
            layerObject = zobj;
            layerObject.Retain();
            gameObject.name = zobj.Name;
            //gameObject.transform.SetParent(parent.transform, false);
            zone.SetParentNode(this, parent.transform);
            gameObject.SetActive(true);
            UpdatePosition();
            InitHPBar();
            OnInit();
            CreateGizmos();
        }
        internal void PauseChanged(bool pause)
        {
            OnPauseChanged(pause);
        }
        protected override void Disposing()
        {
            InvokeUpdate = null;
            ClearGizmos();
            CleanHPBar();
            OnDisposing();
            if (gameObject) gameObject.SetActive(false);
            layerObject?.Release();
            layerObject = null;
        }
        internal void Update(float deltaMS)
        {
            if (IsDisposing) return;
            UpdatePosition();
            OnUpdate(deltaMS);
            InvokeUpdate?.Invoke(this);
            UpdateHPBar();
        }
        internal void UpdateResource() => OnUpdateResource();
        protected abstract void OnUpdateResource();
        protected virtual void OnPauseChanged(bool pause) { }
        protected abstract void OnDisposing();
        protected abstract void OnInit();
        protected abstract void OnUpdate(float deltaMS);
        protected virtual void UpdatePosition()
        {
            Space.UpdatePosition(this, transform);
        }
        public event OnUpdateHandler InvokeUpdate;
        public delegate void OnUpdateHandler(UnityLayerObject obj);

        //-----------------------------------------------------------------------------------
        #region Gizmos

        private GameObject gizmos;
        //protected virtual void OnUpdateGizmos(GameObject gizmos) { }
        internal void CreateGizmos()
        {
            this.gizmos = UnityBattleFactory.Voxel.CreateGizmos(this);            
        }
        internal void ClearGizmos()
        {
            if (gizmos)
            {
                GameObject.Destroy(gizmos);
            }
        }


        #endregion

        //-----------------------------------------------------------------------------------
        #region HPBar

        protected virtual void InitHPBar()
        {
            HPBar = UnityBattleFactory.Instance.CreateHUDUnitHPBar(this);
        }
        protected virtual void UpdateHPBar()
        {
            HPBar?.Update();
        }
        protected virtual void CleanHPBar()
        {
            HPBar?.Dispose();
            HPBar = null;
        }


        #endregion
        //-----------------------------------------------------------------------------------


    }
}
