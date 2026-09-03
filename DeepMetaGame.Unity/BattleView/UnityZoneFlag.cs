using DeepCore.Game3D.Slave.Layer;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data.ZoneEditor;
using System;
using UnityEngine;

namespace DeepMetaGame.Unity.BattleView
{
    public abstract class UnityZoneFlag : UnityLayerObject
    {
        private Collider collider;
        public LayerFlag zFlag { get => layerFlag; }
        public LayerFlag layerFlag { get => layerObject as LayerFlag; }
        public abstract float Direction { get; }
        public abstract bool IsDirection { get; }
        public UnityZoneFlag(UnityZone zone) : base(zone) { }
        protected override void OnInit()
        {
            this.collider = UnityBattleFactory.Instance.CreateFlagCollider(this);
            this.transform.localPosition = Space.ToUnityWorldPosition(this);
            if (IsDirection)
            {
                this.transform.localRotation = zone.BattleToUnityRotation(Direction);
            }
            else
            {
                this.transform.localRotation = Quaternion.identity;
            }
            if (zFlag.EditorData is SceneVirtualObjectData v && v.BindingEffect != null)
            {
                parent.BindObjectEffect(this, v.BindingEffect);
            }
        }
        protected override void OnDisposing()
        {
            CleanResource();
        }
        protected override void OnUpdate(float deltaMS)
        {
        }
        protected override void UpdatePosition()
        {
        }
        protected override void OnDestory()
        {
        }

        #region Resource
        public IResourceObject ModelWrap { get; private set; }
        public string ResourceName { get; private set; }
        public Vector3 ResourceOffset { get; private set; }
        private IAssetLoadingTask assetLoading;
        protected virtual void InitResource(string resourceID, DeepCore.Geometry.Vector3 offset)
        {
            this.ResourceName = resourceID;
            //this.ResourceOffset = offset.ToUnity().VoxelToUnity();
            // if (!string.IsNullOrEmpty(resourceID))
            {
                this.Retain();
                this.assetLoading = UnityBattleFactory.Resource.LoadFlagResource(this, (flag, res, err) =>
                {
                    try
                    {
                        if (res != null)
                        {
                            if (flag.IsDisposing) { res.Dispose(); return; }
                            flag.ModelWrap = res;
                            var space3D = flag.Space;
                            flag.ResourceOffset = space3D.BattleToUnityOffset(offset);
                            //                             if (res.transform.TryGetComponent<Animator>(out var animator))
                            //                             {
                            //                                 animator.applyRootMotion = false;
                            //                             }
                            //                             if (res.transform.TryGetComponent<Animation>(out var animation))
                            //                             {
                            //                                 animation.wrapMode = WrapMode.Loop;
                            //                             }
                            //                             res.transform.localPosition += flag.ResourceOffset;
                        }
                        if (err != null)
                        {
                            Debug.LogError($"Init Unit Resource Error : {flag.layerFlag.EditorData} : {flag.ResourceName} : {err.Message}");
                        }
                    }
                    finally
                    {
                        flag.Release();
                    }
                });
            }
        }
        protected override void OnUpdateResource()
        {
            if (ModelWrap != null)
            {
                ModelWrap.UpdateResource(this);
                if (ModelWrap.transform.gameObject.activeSelf != this.layerFlag.Enable)
                {
                    ModelWrap.transform.gameObject.SetActive(this.layerFlag.Enable);
                }
            }
        }
        protected virtual void CleanResource()
        {
            this.assetLoading?.Dispose();
            this.assetLoading = null;
            if (ModelWrap != null)
            {
                ModelWrap.Dispose();
                ModelWrap = null;
            }
        }
        #endregion
    }

    public class UnityLayerDecoration : UnityZoneFlag
    {
        private IDisposable SoundAmbient;
        public LayerEditorDecoration decorationObject { get => layerObject as LayerEditorDecoration; }
        public DecorationData Data => decorationObject.Data;
        public override float Direction => Data.Direction;
        public override bool IsDirection
        {
            get
            {
                switch (Data.RegionType)
                {
                    case DecorationData.Shape.STRIP:
                    case DecorationData.Shape.ROUND:
                        return true;
                    case DecorationData.Shape.RECTANGLE:
                    default:
                        return false;
                }
            }
        }
        public UnityLayerDecoration(UnityZone zone) : base(zone) { }
        protected override void OnInit()
        {
            base.OnInit();
            base.InitResource(decorationObject.Data.ResourceName, decorationObject.Data.ResourceOffset);
            if (!string.IsNullOrEmpty(decorationObject.Data.SoundAmbient))
            {
                this.SoundAmbient = AudioComponent.Instance.PlayAmbient(
                    this.transform,
                    DeepMetaGame.Data.ResourceType.Sound_Ambient,
                    decorationObject.Data.SoundAmbient);
            }
            //             if (Application.isEditor)
            //             {
            //                var gizmosCylinder = gameObject.AddComponent<GizmosCylinder>();
            //                 {
            //                     gizmosCylinder.Direction = layerObject.Direction;
            //                     gizmosCylinder.Height = layerObject.BodyHeight;
            //                     gizmosCylinder.Radius = layerObject.BodyBlockSize;
            //                 }
            //             }
        }
        protected override void OnDisposing()
        {
            this.SoundAmbient?.Dispose();
            base.OnDisposing();
        }
    }
    public class UnityLayerRegion : UnityZoneFlag
    {
        public LayerEditorRegion regionObject { get => layerObject as LayerEditorRegion; }
        public RegionData Data => regionObject.Data;
        public override float Direction => Data.Direction;
        public override bool IsDirection
        {
            get
            {
                switch (Data.RegionType)
                {
                    case RegionData.Shape.ROUND:
                        return true;
                    case RegionData.Shape.RECTANGLE:
                    default:
                        return false;
                }
            }
        }
        public UnityLayerRegion(UnityZone zone) : base(zone) { }
        protected override void OnInit()
        {
            base.OnInit();
            base.InitResource(regionObject.Data.ResourceName, regionObject.Data.ResourceOffset);
            //             if (Application.isEditor)
            //             {
            //                 if (Data.RegionType == RegionData.Shape.RECTANGLE)
            //                 {
            //                     var rect = VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Data.Height);
            //                     rect.transform.SetParent(transform, false);
            //                 }
            //                 else
            //                 {
            //                     var cylinder = VoxelGizmos.CreateVoxelCylinder(Data.Radius, Data.Height);
            //                     cylinder.transform.SetParent(transform, false);
            //                 }
            //             }
        }
    }
    public class UnityLayerPoint : UnityZoneFlag
    {
        public LayerEditorPoint pointObject { get => layerObject as LayerEditorPoint; }
        public PointData Data => pointObject.Data;
        public override float Direction => Data.Direction;
        public override bool IsDirection => true;
        public UnityLayerPoint(UnityZone zone) : base(zone) { }
        protected override void OnInit()
        {
            base.OnInit();
            base.InitResource(pointObject.Data.ResourceName, pointObject.Data.ResourceOffset);
            //             if (Application.isEditor)
            //             {
            //                 var gizmosCylinder = gameObject.AddComponent<GizmosCylinder>();
            //                 {
            //                     gizmosCylinder.Direction = layerObject.Direction;
            //                     gizmosCylinder.Height = layerObject.BodyHeight;
            //                     gizmosCylinder.Radius = layerObject.BodyBlockSize;
            //                 }
            //             }
        }
    }
}
