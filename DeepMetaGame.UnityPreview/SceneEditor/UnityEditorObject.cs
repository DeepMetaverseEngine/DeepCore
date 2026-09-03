using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity3D.Voxel;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneEditor.EditorToScene;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Unity.Preview.Preview;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using static DeepCore.GameData.Zone.ZoneEditor.EventTrigger.FlagValue;
using static System.Net.Mime.MediaTypeNames;


namespace DeepMetaGame.Unity.Preview.SceneEditor
{
    public abstract class UnityEditorObject : SceneEditorBehavior
    {
        public SceneObjectData Data { get; private set; }
        public SceneObjectData ObjectData { get { return Data; } }
        public string Name { get { return Data.Name; } }
        public bool IsLocked { get => Data.IsLocked; }
        public virtual float Radius { get => Data.Radius; }
        public virtual float Height { get => Data.Height; }
        public DeepCore.Geometry.Vector3 RuntimePosition
        {
            get => SceneEditorBehavior.UnityWorldToBattlePosition(transform.localPosition);
            set => transform.localPosition = SceneEditorBehavior.BattleToUnityWorldPosition(value);
        }

        public DeepCore.Geometry.Vector3 Position { get => Data.Position; }
        public float Direction { get => Data.Direction; }
        public bool IsSelected { get { return World.SelectedObjectName == Name; } }
        public abstract bool IsVisible { get; }
        public abstract bool IsDirection { get; }

        public bool IsEditable { get; private set; } = true;

        public bool IsEditVisible { get; set; } = true;

        public bool IsSelfVisible { get; private set; }

        protected Transform childGizmos { get; private set; }
        protected Transform childText { get; private set; }
        protected IViewResource modelWrap { get; private set; }



        protected Color color;
        protected Color selectedColor = Color.white;


        internal void Init(MsgPutObject put)
        {
            var data = put.ObjData;
            this.name = data.Name;
            Data = put.ObjData;
            gameObject.name = data.Name;
            Data.ToColorARGB(out color.a, out color.r, out color.g, out color.b);
            color.a *= 0.75f;
            selectedColor = Color.white;

            OnInit(put);

            OnInitResource();

            childGizmos = OnInitGizmos();
            if (childGizmos)
            {
                childGizmos.SetParent(transform, false);
                InitGizmosMatrial();
            }
            childText = OnInitHeadText();
            if (childText)
            {
                if (RTG.NodeHUD) childText.SetParent(RTG.NodeHUD, false);
                RTG.SetHeadText(childText.gameObject, ToHeadString());
            }
        }
        protected virtual string ToHeadString()
        {
            return this.name;
        }
        internal void End()
        {
            this.IsEditable = false;

        }

        protected virtual void OnDestroy()
        {
            try
            {
                OnDestoryResource();
                OnDestoryHeadText();
                if (lineObject) GameObject.Destroy(lineObject);
            }
            catch (Exception e)
            {
                PLog(e);
            }
        }
        protected virtual void OnRenderObject()
        {
            try
            {
                OnDrawAltitude();
            }
            catch (Exception e)
            {
                PLog(e);
            }
        }

        public void SetName(string name)
        {
            if (Data.Name != name)
            {
                this.name = name;
                Data.Name = name;
                gameObject.name = name;
                if (childText)
                {
                    childText.gameObject.name = name;
                    RTG.SetHeadText(childText.gameObject, ToHeadString());
                }
            }
        }
        internal void EditorUpdate()
        {
            try
            {
                var visible = this.IsSelfVisible = IsEditVisible && IsVisible;
                if (VS.OnlyBlackHole)
                {
                    if (!IsSelected)
                    {
                        var cp = transform.localPosition;
                        var locap = UnityWorldToBattlePosition(cp);
                        if (World.VoxelTerrain != null && World.VoxelTerrain.TryGetVoxelLayerByPos(locap, out var cell, out var layer))
                        {
                            visible = false;
                        }
                    }
                }
                gameObject.SetActive(visible);
                if (VS.GridToSize)
                {
                    if (IsSelected)
                    {
                        if (RTG.IsDraggingTarget && RTG.TargetObject == this.gameObject)
                        {
                            GridToSize();
                        }
                    }
                }
                OnUpdateResource(visible);
                if (this.lineObject != null)
                {
                    this.lineObject.ActiveSelf(IsSelfVisible);
                }
                if (childGizmos && childGizmos.TryGetComponent<MeshRenderer>(out var grender))
                {
                    grender.enabled = visible && VS.ShowObjectsBody;
                }
                if (childText)
                {
                    if (IsSelected)
                    {
                        childText.SetActive(true);
                        var pos = transform.position;
                        pos.y += Height * 1.2f;
                        childText.transform.position = RTG.MainCamera.WorldToScreenPoint(pos, Camera.MonoOrStereoscopicEye.Mono);
                    }
                    else if (!VS.ShowObjectsName || !visible)
                    {
                        childText.SetActive(false);
                    }
                    else if (!IsSelected && Vector3.Distance(RTG.MainCamera.transform.position, transform.position) > RTG.HeadVisibleDistance)
                    {
                        childText.SetActive(false);
                    }
                    else if (RTG.MainCamera.IsInCamera(transform))
                    {
                        childText.SetActive(true);
                        var pos = transform.position;
                        pos.y += Height * 1.2f;
                        childText.transform.position = RTG.MainCamera.WorldToScreenPoint(pos, Camera.MonoOrStereoscopicEye.Mono);
                    }
                    else
                    {
                        childText.SetActive(false);
                    }
                }
            }
            catch (Exception e)
            {
                PLog(e);
            }
        }
        public IEditorObject RuntimeTransform { get; private set; }
        public bool ResetFromData()
        {
            var lp = transform.localPosition;
            var ld = transform.localRotation;
            transform.localPosition = BattleToUnityWorldPosition(Data.Position);
            if (IsDirection)
            {
                transform.localRotation = BattleToUnityRotation(Data.Direction);
            }
            else
            {
                transform.localRotation = Quaternion.identity;
            }
            this.RuntimeTransform = RTG.AddEditorObject(gameObject);
            //this.RuntimeTransform.Selectable = !IsLocked;
            if (IsLocked)
            {
                var colls = this.gameObject.GetComponentsInChildren<Collider>();
                foreach (Collider coll in colls)
                {
                    coll.enabled = false;
                }
            }
            else
            {
                var colls = this.gameObject.GetComponentsInChildren<Collider>();
                foreach (Collider coll in colls)
                {
                    coll.enabled = true;
                }
            }
            return lp != transform.localPosition || ld != transform.localRotation;
        }

        public void GridToSize()
        {
            if (IsLocked)
            {
                return;
            }
            var pos = Position;
            if (VS.GridSize > 0)
            {
                pos.X = CMath.AlignTo(pos.X, VS.GridSize);
                pos.Y = CMath.AlignTo(pos.Y, VS.GridSize);
            }
            transform.localPosition = BattleToUnityWorldPosition(pos);
            Data.Position = pos;
        }
        public bool DockVoxel()
        {
            if (IsLocked)
            {
                return false;
            }
            var old = Position;
            var pos = Position;
            if (World.VoxelTerrain != null)
            {
                if (World.VoxelTerrain.TryGetVoxelLayerByPos(pos, out var upward, out var top))
                {
                    pos.Z = upward;
                }
            }
            transform.localPosition = BattleToUnityWorldPosition(pos);
            Data.Position = pos;
            return old != Position;
        }

        public bool ResetFromTransform()
        {
            //if (Data.IsLocked)
            //{
            //    return false;
            //}
            var lp = Position;
            var ld = Direction;
            Data.Position = UnityWorldToBattlePosition(transform.localPosition);
            if (IsDirection)
            {
                Data.Direction = UnityToBattleRotation(transform.localRotation);
            }
            else
            {
                Data.Direction = 0;
            }
            return lp != Position || ld != Direction;
        }

        protected virtual void OnInit(MsgPutObject data)
        {
        }

        protected virtual Transform OnInitHeadText()
        {
            if (RTG.TempHeadText)
            {
                var head = Instantiate(RTG.TempHeadText);
                head.SetActive(true);
                return head;
            }
            return null;
        }
        protected virtual void OnDestoryHeadText()
        {
            if (childText)
            {
                Destroy(childText.gameObject);
            }
        }

        protected virtual void OnInitResource()
        {

        }
        protected virtual void OnUpdateResource(bool visible)
        {
            if (this.modelWrap != null)
            {
                this.modelWrap.IsVisible = (visible && VS.ShowObjectsRes);
                this.modelWrap?.UpdateResource(this.gameObject);
            }
        }
        protected virtual void OnDestoryResource()
        {
            if (modelWrap != null)
            {
                modelWrap.Dispose();
            }
        }
        protected virtual Transform OnInitGizmos()
        {
            var cylinder = VoxelGizmos.CreateVoxelCylinder(Radius, Height);
            return cylinder.transform;
        }
        protected IViewResource InitResource()
        {
            return InitResource(DeepCore.Geometry.Vector3.Zero, 1f);
        }
        protected IViewResource InitResource(float scale = 1f)
        {
            return InitResource(DeepCore.Geometry.Vector3.Zero, scale);
        }
        protected virtual IViewResource InitResource(DeepCore.Geometry.Vector3 offset, float scale = 1f)
        {
            try
            {
                var res = Proxy.LoadResource(this);
                if (res != null)
                {
                    //res.transform.SetParent(transform);
                    res.transform.localPosition = Vector3.zero + offset.ToUnity().VoxelToUnity();
                    res.transform.localScale = Vector3.one * scale;
                    if (res.gameObject.TryGetComponent<Animator>(out var animator))
                    {
                        animator.applyRootMotion = false;
                    }
                    if (res.gameObject.TryGetComponent<Animation>(out var animation))
                    {
                        animation.wrapMode = WrapMode.Loop;
                    }
                    this.modelWrap = res;
                    return res;
                }
            }
            catch (Exception e)
            {
                PLog($"Init Flag Resource Error :  {Data} : {e.Message}");
            }
            return null;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        #region LineRender
        private GameObject lineObject => lineRenderer?.gameObject;
        private LineRenderer lineRenderer;
        public LineRenderer InitGizmosMatrial(string name, Color color)
        {
            var lineObject = new GameObject(this.name);
            lineObject.transform.parent = this.gameObject.transform.parent;
            lineObject.transform.position = this.transform.position;

            var lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 3;
            if (childGizmos.TryGetComponent<MeshRenderer>(out var srender) && RTG.TempGizmoz && RTG.TempGizmoz.TryGetComponent<MeshRenderer>(out var drender))
            {
                lineRenderer.material = Instantiate(drender.material);
                lineRenderer.material.color = color;
                srender.material = Instantiate(drender.material);
                srender.material.color = color;
            }
            return lineRenderer;
        }
        protected virtual LineRenderer InitGizmosMatrial()
        {
            lineRenderer = InitGizmosMatrial(this.name + ":line", color);
            return lineRenderer;
        }
        protected virtual void OnDrawAltitude()
        {
            if (lineObject != null)
            {
                if ((World.SelectedObject == this || VS.ShowObjectsAltitude) && World.VoxelTerrain != null)
                {
                    var cp = transform.position;
                    var aabb = World.VoxelTerrain.AABB;
                    var locap = UnityWorldToBattlePosition(transform.localPosition);
                    var w = World.VoxelTerrain.GridCellSize;
                    var vc = Color.magenta;
                    var top = aabb.Max.Z + 100;
                    lineObject.transform.position = this.transform.position;
                    lineRenderer.SetPosition(0, new Vector3(cp.x, cp.y + top, cp.z));
                    lineRenderer.SetPosition(1, cp);
                    if (World.VoxelTerrain.TryGetVoxelLayerByPos(locap, out var upward, out var t))
                    {
                        if (upward <= locap.Z)
                        {
                            lineRenderer.enabled = World.SelectedObject == this;
                            vc = Color.green;
                            lineRenderer.SetPosition(2, new Vector3(cp.x, upward, cp.z));
                        }
                        else
                        {
                            vc = Color.magenta;
                            lineRenderer.enabled = true;
                            lineRenderer.SetPosition(2, cp);
                        }
                    }
                    else
                    {
                        vc = Color.black;
                        lineRenderer.enabled = true;
                        lineRenderer.SetPosition(2, new Vector3(cp.x, cp.y + aabb.Min.Z, cp.z));
                    }
                    vc.a = 1f + Mathf.Sin(Time.time * 2f) * 0.5f;
                    vc = new Color(vc.r * 2f, vc.g * 2f, vc.b * 2f, vc.a);
                    lineRenderer.startColor = vc;
                    lineRenderer.endColor = vc;
                    lineRenderer.material.color = vc;
                    lineRenderer.startWidth = w / 2f;
                    lineRenderer.endWidth = w / 2f;
                }
                else
                {
                    lineRenderer.enabled = false;
                }
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
    }
    public abstract class UnityEditorObject<T> : UnityEditorObject where T : SceneObjectData
    {
        new public T Data { get => base.Data as T; }
    }
    //----------------------------------------------------------------------------------------------------------------------------------------
    #region Instance

    public class UnityEditorUnit : UnityEditorObject<UnitData>
    {
        public override bool IsVisible => VS.OnlyShowUnit || VS.ShowObjectsAll;
        public UnitInfo Info { get; private set; }
        public override float Radius => Info != null ? Info.BodySize : Data.Radius;
        public override float Height => Info != null ? Info.BodyHeight : base.Height;
        public override bool IsDirection { get { return true; } }

        private readonly UnitActionMap actionMap = new UnitActionMap();
        private IViewResource actionCustomWrap;
        private bool actionCustomOverride;

        protected override void OnInit(MsgPutObject put)
        {
            Info = (put as MsgPutUnit).UnitData;
            base.OnInit(put);
            actionMap.Append(Proxy.UnitActionDefinitionMap);
            if (Info.Abilities.TryGetComponentAs<UnitResourceAbility>(out var resAB) && resAB.OverrideActionMap != null)
            {
                actionMap.Append(resAB.OverrideActionMap);
            }
        }
        protected override string ToHeadString()
        {
            return $"{Info}\n{base.ToHeadString()}";
        }
        protected override void OnInitResource()
        {
            if (Info != null && Info.Abilities.TryGetComponentAs<UnitResourceAbility>(out var res))
            {
                base.InitResource(res.BodyScale);
            }
            if (this.actionMap != null && this.actionMap.TryGetAction(Data.MainStatus, Data.SubStatus, out var action))
            {
                if (action.CustomResource != null)
                {
                    var wrap = RTG.LoadResource(action.CustomResource, DeepMetaGame.Data.ResourceType.Object_Effect, this);
                    if (wrap != null)
                    {
                        this.actionCustomWrap = wrap;
                        this.actionCustomOverride = action.CustomResourceOverride;
                        wrap.transform.SetParent(transform, false);
                        wrap.transform.localPosition = Vector3.zero;
                        wrap.transform.localScale = Vector3.one;
                        if (action.CustomResourceOverride)
                        {
                            base.modelWrap.IsVisible = false;
                        }
                    }
                }
            }
        }
        protected override void OnUpdateResource(bool visible)
        {
            if (this.modelWrap != null)
            {
                if (this.actionCustomWrap != null)
                {
                    this.actionCustomWrap.IsVisible = (visible && VS.ShowObjectsRes);
                    if (this.actionCustomOverride)
                    {
                        this.modelWrap.IsVisible = false;
                    }
                    else
                    {
                        this.modelWrap.IsVisible = (visible && VS.ShowObjectsRes);
                    }
                }
                else
                {
                    this.modelWrap.IsVisible = (visible && VS.ShowObjectsRes);
                }
            }
            this.actionCustomWrap?.UpdateResource(this.gameObject);
            this.modelWrap?.UpdateResource(this.gameObject);
        }
        protected override void OnDestoryResource()
        {
            if (actionCustomWrap != null)
            {
                actionCustomWrap.Dispose();
                actionCustomWrap = null;
            }
            base.OnDestoryResource();
        }
        protected override Transform OnInitGizmos()
        {
            if (Info != null)
            {
                switch (Info.FillZoneShape)
                {
                    case UnitInfo.Shape.RECTANGLE:
                        var rect = VoxelGizmos.CreateVoxelRect(Info.BodySize, Info.BodySize, Height);
                        return rect.transform;
                    case UnitInfo.Shape.ROUND:
                    default:
                        return base.OnInitGizmos();
                }
            }
            return base.OnInitGizmos();
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------------------
    public class UnityEditorItem : UnityEditorObject<ItemData>
    {
        public override bool IsVisible => VS.OnlyShowItem || VS.ShowObjectsAll;
        public ItemTemplate Info { get; private set; }
        public override float Radius => Info != null ? Info.BodySize : Data.Radius;
        public override float Height => Info != null ? Info.BodyHeight : base.Height;
        public override bool IsDirection => true;
        protected override void OnInit(MsgPutObject put)
        {
            Info = (put as MsgPutItem).Item;
            base.OnInit(put);
        }
        protected override string ToHeadString()
        {
            return $"{Info}\n{base.ToHeadString()}";
        }
        protected override void OnInitResource()
        {
            if (Info != null && Info.Abilities.TryGetComponentAs<ItemResource>(out var res))
            {
                base.InitResource(res.BodyScale);
            }
        }
    }

    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------
    #region Flag

    public abstract class UnityEditorFlag<T> : UnityEditorObject<T> where T : SceneObjectData
    {
    }
    public abstract class UnityEditorVirtualFlag<T> : UnityEditorFlag<T> where T : SceneVirtualObjectData
    {
        private GameObject lineNext => lineNextRender?.gameObject;
        private LineRenderer lineNextRender;
        protected override void OnInitResource()
        {
            base.OnInitResource();
            if (Data.BindingEffect != null)
            {
                DisplayEffect.LoadEffect(this.gameObject, Data.BindingEffect, modelWrap, this.Height);
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (lineNext) GameObject.Destroy(lineNext);
        }
        protected override void OnDrawAltitude()
        {
            DrawNext(GetNextPoints());
            base.OnDrawAltitude();
        }
        protected override LineRenderer InitGizmosMatrial()
        {
            lineNextRender = base.InitGizmosMatrial(this.name + ":nexts", color);
            return base.InitGizmosMatrial();
        }
        protected override void OnUpdateResource(bool visible)
        {
            if (VS.ShowObjectsBody && IsSelfVisible && IsVisible)
            {
                lineNext.ActiveSelf(true);
            }
            else
            {
                lineNext.ActiveSelf(false);
            }
            base.OnUpdateResource(visible);
        }
        protected virtual void DrawNext(List<UnityEditorObject> nexts)
        {
            var width = this.Radius;
            foreach (var o in nexts)
            {
                width = Math.Min(width, o.Radius);
            }
            if (nexts != null && nexts.Count > 0)
            {
                lineNext.transform.position = this.transform.position;
                lineNextRender.positionCount = nexts.Count * 2;

                var vc = this.IsSelected ? Color.white : this.color;
                var cp = transform.position;

                for (int i = 0; i < nexts.Count; i++)
                {
                    var next = nexts[i];
                    var np = next.transform.position;
                    lineNextRender.SetPosition(i * 2 + 0, cp + new Vector3(0, this.Height / 2f, 0));
                    lineNextRender.SetPosition(i * 2 + 1, np + new Vector3(0, next.Height / 2f, 0));
                }

                vc.a = 1f + Mathf.Sin(Time.time * 2f) * 0.5f;
                vc = new Color(vc.r * 2f, vc.g * 2f, vc.b * 2f, vc.a);
                lineNextRender.startColor = vc;
                lineNextRender.endColor = vc;
                lineNextRender.material.color = vc;
                lineNextRender.startWidth = width / 2f;
                lineNextRender.endWidth = width / 2f;
                lineNextRender.enabled = true;
            }
            else
            {
                lineNextRender.enabled = false;
            }
        }
        public virtual List<UnityEditorObject> GetNextPoints()
        {
            var list = new List<UnityEditorObject>();
            foreach (string next in Data.NextNames)
            {
                var nextobb = World.GetObject(next);
                if (nextobb is UnityEditorObject nextWP)
                {
                    list.Add(nextWP);
                }
            }
            return list;
        }
        public bool ForEachNextPoints(BreakPredicate<UnityEditorObject> action)
        {
            foreach (string next in Data.NextNames)
            {
                var nextobb = World.GetObject(next);
                if (nextobb is UnityEditorObject nextWP)
                {
                    if (action(nextWP)) { return true; }
                }
            }
            return false;
        }
    }

    public class UnityEditorRegion : UnityEditorVirtualFlag<RegionData>
    {
        public override bool IsVisible => VS.OnlyShowRegion || VS.ShowObjectsAll;
        //public override float Radius => Data.RegionType == RegionData.Shape.RECTANGLE ? Math.Max(Data.W, Data.H) : Data.R;
        //         public override bool IsDirection
        //         {
        //             get
        //             {
        //                 switch (Data.RegionType)
        //                 {
        //                     case RegionData.Shape.ROUND:
        //                         return true;
        //                     case RegionData.Shape.RECTANGLE:
        //                     default:
        //                         return false;
        //                 }
        //             }
        //         }
        public override float Radius
        {
            get
            {
                switch (Data.RegionType)
                {
                    case RegionData.Shape.STRIP:
                        return Data.Radius;
                    case RegionData.Shape.ROUND:
                        return Data.R;
                    case RegionData.Shape.RECTANGLE:
                    default:
                        return Math.Max(Data.W, Data.H);
                }
            }
        }
        public override bool IsDirection
        {
            get
            {
                switch (Data.RegionType)
                {
                    case RegionData.Shape.STRIP:
                    case RegionData.Shape.ROUND:
                        return true;
                    case RegionData.Shape.RECTANGLE:
                    default:
                        return false;
                }
            }
        }
        protected override Transform OnInitGizmos()
        {
            if (Data.RegionType == RegionData.Shape.RECTANGLE)
            {
                var rect = VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Height);
                return rect.transform;
            }
            else if (Data.RegionType == RegionData.Shape.STRIP)
            {
                var strip = VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Height);
                return strip.transform;
            }
            else
            {
                var cylinder = VoxelGizmos.CreateVoxelCylinder(Radius, Height);
                return cylinder.transform;
            }
        }
        //         protected override Transform OnInitGizmos()
        //         {
        //             if (Data.RegionType == RegionData.Shape.RECTANGLE)
        //             {
        //                 var rect = VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Height);
        //                 return rect.transform;
        //             }
        //             else
        //             {
        //                 var cylinder = VoxelGizmos.CreateVoxelCylinder(Radius, Height);
        //                 return cylinder.transform;
        //             }
        //         }
        protected override void OnInitResource()
        {
            base.InitResource(Data.ResourceOffset, Data.Scale);
            base.OnInitResource();
        }
        //         protected override void OnDrawAltitude()
        //         {
        //             var nexts = GetNextSpawnPoints();
        //             DrawNext(nexts);
        //             base.OnDrawAltitude();
        //         }
        public override List<UnityEditorObject> GetNextPoints()
        {
            var list = base.GetNextPoints();
            {
                foreach (var ab in Data.GetAbilities())
                {
                    if (ab is SpawnUnitAbilityData spawn)
                    {
                        var nextobb = World.GetObject(spawn.StartPointName);
                        if (nextobb is UnityEditorObject nextWP)
                        {
                            list.Add(nextWP);
                        }
                    }
                }
            }
            return list;
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------------------

    public class UnityEditorDecoration : UnityEditorVirtualFlag<DecorationData>
    {
        public override bool IsVisible => VS.OnlyShowDecoration || VS.ShowObjectsAll;
        public override float Radius
        {
            get
            {
                switch (Data.RegionType)
                {
                    case DecorationData.Shape.STRIP:
                        return Data.Radius;
                    case DecorationData.Shape.ROUND:
                        return Data.R;
                    case DecorationData.Shape.RECTANGLE:
                    default:
                        return Math.Max(Data.W, Data.H);
                }
            }
        }
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
        protected override Transform OnInitGizmos()
        {
            if (Data.RegionType == DecorationData.Shape.RECTANGLE)
            {
                var rect = VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Height);
                return rect.transform;
            }
            else if (Data.RegionType == DecorationData.Shape.STRIP)
            {
                var strip = VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Height);
                return strip.transform;
            }
            else
            {
                var cylinder = VoxelGizmos.CreateVoxelCylinder(Radius, Height);
                return cylinder.transform;
            }
        }
        protected override void OnInitResource()
        {
            base.InitResource(Data.ResourceOffset, Data.Scale);
            base.OnInitResource();
        }

    }

    //----------------------------------------------------------------------------------------------------------------------------------------

    public class UnityEditorWayPoint : UnityEditorVirtualFlag<PointData>
    {
        public override bool IsVisible => VS.OnlyShowPoint || VS.ShowObjectsAll;
        public override bool IsDirection => true;
        protected override void OnInitResource()
        {
            base.InitResource(Data.ResourceOffset, Data.Scale);
            base.OnInitResource();
        }
        protected GameObject lineBezierCurve => lineBezierCurveRender.gameObject;
        protected LineRenderer lineBezierCurveRender;
        protected override LineRenderer InitGizmosMatrial()
        {
            lineBezierCurveRender = InitGizmosMatrial(this.name + ":BezierCurve", color);
            return base.InitGizmosMatrial();
        }
        protected override void OnUpdateResource(bool visible)
        {
            if (VS.ShowObjectsBody && IsSelfVisible && IsVisible)
            {
                lineBezierCurve.ActiveSelf(true);
            }
            else
            {
                lineBezierCurve.ActiveSelf(false);
            }
            base.OnUpdateResource(visible);
        }
        protected override void DrawNext(List<UnityEditorObject> nexts)
        {
            DrawBezier(nexts);
            base.DrawNext(nexts);
        }
        protected virtual void DrawBezier(List<UnityEditorObject> nexts)
        {
            if (nexts != null && nexts.Count > 0 && Data.TangentSize != 0)
            {
                var width = Data.Size / 5;
                foreach (var next in nexts)
                {
                    if (next is UnityEditorWayPoint wp)
                    {
                        //lineBezierCurveRender.useWorldSpace = false;
                        lineBezierCurveRender.transform.position = this.transform.position;
                        var vc = this.IsSelected ? Color.white : this.color;
                        {
                            var p0 = this.Position;
                            var p1 = DeepCore.Geometry.VectorDrawing.VectorOffset(p0, this.Data.TangentSize, this.Direction + CMath.RADIANS_90);
                            var nextTS = wp.Data.TangentSize;
                            var p3 = wp.Position;
                            var p2 = DeepCore.Geometry.VectorDrawing.VectorOffset(p3, nextTS, next.Direction - CMath.RADIANS_90);
                            // 三次贝塞尔示例
                            var bezier = new DeepCore.Geometry.CubicBezier(p0, p1, p2, p3);
                            var points = bezier.Sample(100);
                            lineBezierCurveRender.positionCount = points.Count * 2;
                            // 绘制曲线
                            {
                                lineBezierCurveRender.SetPosition(0, BattleToUnityWorldPosition(p0) + new Vector3(0, this.Height / 2f, 0));
                                lineBezierCurveRender.SetPosition(1, BattleToUnityWorldPosition(p1) + new Vector3(0, next.Height / 2f, 0));
                            }
                            for (int ip = 0; ip < points.Count - 1; ip++)
                            {
                                var src = points[ip];
                                var dst = points[ip + 1];
                                lineBezierCurveRender.SetPosition(ip * 2 + 2, BattleToUnityWorldPosition(src) + new Vector3(0, this.Height / 2f, 0));
                                lineBezierCurveRender.SetPosition(ip * 2 + 3, BattleToUnityWorldPosition(dst) + new Vector3(0, next.Height / 2f, 0));
                            }
                        }
                        vc.a = 1f + Mathf.Sin(Time.time * 2f) * 0.5f;
                        vc = new Color(vc.r * 2f, vc.g * 2f, vc.b * 2f, vc.a);
                        lineBezierCurveRender.startColor = vc;
                        lineBezierCurveRender.endColor = vc;
                        lineBezierCurveRender.material.color = vc;
                        lineBezierCurveRender.startWidth = width / 2f;
                        lineBezierCurveRender.endWidth = width / 2f;
                        lineBezierCurveRender.enabled = true;
                        return;
                    }
                }
            }
            lineBezierCurveRender.enabled = false;
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------------------

    public class UnityEditorArea : UnityEditorVirtualFlag<AreaData>
    {
        public override bool IsVisible => VS.OnlyShowArea || VS.ShowObjectsAll;
        public override bool IsDirection => false;
        protected override Transform OnInitGizmos()
        {
            var rect = VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Height);
            return rect.transform;
        }
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------
}

