using Battlehub.RTCommon;
using Battlehub.RTHandles;
using DeepCore.Unity;
using DeepMetaGame.Unity.Preview;
using DeepMetaGame.Unity.Preview.SceneEditor;
using UnityEngine;

namespace Battlehub
{
    public class EditorRTG : SimpleUnityRTG
    {
        private IRTE irte;
        [SerializeField] protected Transform TransformHandler;
        protected override void Awake()
        {
            base.Awake();
        }
        protected override void Start()
        {
            irte = IOC.Resolve<IRTE>();
            irte.Tools.Current = RuntimeTool.Rotate;
            irte.Tools.PivotRotation = RuntimePivotRotation.Global;
            irte.Tools.LockAxes = new()
            {
                PositionX = false,
                PositionY = false,
                PositionZ = false,
                ScaleX = true,
                ScaleY = true,
                ScaleZ = true,
                RotationFree = true,
                RotationX = true,
                RotationZ = true,
                RotationY = false,
                RotationScreen = true,
                RectXY = true,
                RectXZ = true,
                RectYZ = true,
            };
            irte.Selection.SelectionChanged += Selection_SelectionChanged;
            irte.Object.TransformChanged += Object_TransformChanged;
            irte.Undo.StateChanged += Undo_StateChanged;
            irte.Tools.Current = RuntimeTool.Move;
            if (TransformHandler.TryGetComponentInChildren<PositionHandle>(out var handler, true))
            {
                handler.Drop.AddListener(new UnityEngine.Events.UnityAction<BaseHandle>(OnDrop));
            }
        }
        protected GameObject Selected
        {
            get
            {
                if (irte.Selection.objects != null && irte.Selection.objects.Length > 0)
                {
                    return irte.Selection.objects[0] as GameObject;
                }
                return null;
            }
            set
            {
                irte.Selection.objects = new UnityEngine.Object[] { value };
            }
        }

        public override bool IsDebug { get; set; }

        protected virtual void Selection_SelectionChanged(UnityEngine.Object[] unselectedObjects)
        {
            var unselect = (unselectedObjects != null && unselectedObjects.Length > 0) ? unselectedObjects[0] : null;
            if (OnTargetSelectChanged != null)
            {
                OnTargetSelectChanged.Invoke(unselect as GameObject, Selected);
            }
        }

        protected virtual void Object_TransformChanged(ExposeToEditor obj)
        {
            var selected = Selected;
            if (selected == obj.gameObject)
            {
                if (OnTargetTransformChanged != null)
                {
                    OnTargetTransformChanged.Invoke(obj.gameObject);
                }
            }
        }
        protected virtual void OnDrop(BaseHandle handle)
        {
            var selected = Selected;
            if (selected)
            {
                if (OnTargetTransformChanged != null)
                {
                    OnTargetTransformChanged.Invoke(selected);
                }
            }
        }
        protected virtual void Undo_StateChanged()
        {
            var selected = Selected;
            if (selected)
            {
                if (OnTargetPropertyChanged != null)
                {
                    OnTargetPropertyChanged.Invoke(selected);
                }
            }
        }

        protected virtual void Update()
        {
        }
        //     public GameObject PickGameObject(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask)
        //     {
        //         if (Physics.Raycast(ray, out hitInfo, maxDistance, layerMask))
        //             return hitInfo.collider.gameObject;
        //         return null;
        //     }
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        #region IGizmos
        public Matrix4x4 GizmosMatrix { get; set; } = Matrix4x4.identity;
        public Color GizmosColor { get; set; } = Color.white;

        public virtual void GizmosDrawLine(Vector3 p1, Vector3 p2)
        {
            var color = this.GizmosColor;
            var mtx = this.GizmosMatrix;
        }
        public virtual void GizmosDrawCube(Vector3 center, Vector3 size)
        {
            var color = this.GizmosColor;
            var mtx = this.GizmosMatrix;
        }
        public override void SetHeadText(GameObject obj, string text)
        {
            if (obj.TryGetComponent<TMPro.TMP_Text>(out var tmp))
            {
                tmp.text = text;
            }
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        #region ISceneEditorRTG
        public override event TargetSelectChanged OnTargetSelectChanged;
        public override event TargetPropertyChanged OnTargetPropertyChanged;
        public override event TargetTransformChanged OnTargetTransformChanged;

        public override GameObject TargetObject
        {
            get => Selected;
            set
            {
                if (value != Selected)
                {
                    Selected = value;
                }
            }
        }
        public override GameObject DraggingTarget
        {
            get
            {
                if (TransformHandler.TryGetComponentInChildren<PositionHandle>(out var handler, true))
                {
                    return handler.Target.gameObject;
                }
                return null;
            }
        }
        public override bool IsDraggingTarget
        {
            get
            {
                if (TransformHandler.TryGetComponentInChildren<PositionHandle>(out var handler, true))
                {
                    return handler.IsDragging;
                }
                return false;
            }
        }
        public override void SetSnapToGrid(bool snapToGrid, float gridOfSize)
        {
            if (TransformHandler.TryGetComponentInChildren<PositionHandle>(out var handler, true))
            {
                handler.SnapToGrid = snapToGrid;
                handler.SizeOfGrid = gridOfSize;
            }
        }

        private float? lastCamera2DSize = null;
        private Vector3? lastCamera3DPosition = null;
        private Vector3? lastCamera3DLookAt = null;

        public override void SetCameraMode(DeepMetaGame.Unity.Preview.CameraMode mode)
        {

            if (TransformHandler.TryGetComponentInChildren<SceneGizmo>(out var handler, true))
            {
                switch (mode)
                {
                    case CameraMode.Mode2D:
                        lastCamera3DPosition = MainCamera.transform.position;
                        lastCamera3DLookAt = MainCamera.transform.forward;
                        handler.IsOrthographic = true;
                        handler.ChangeOrientation(new Vector3(0, -1f, 0));
                        if (lastCamera2DSize.HasValue)
                        {
                            MainCamera.orthographicSize = lastCamera2DSize.Value;
                        }
                        break;
                    case CameraMode.Mode3D:
                        lastCamera2DSize = MainCamera.orthographicSize;
                        handler.IsOrthographic = false;
                        if (lastCamera3DPosition.HasValue)
                        {
                            MainCamera.transform.position = lastCamera3DPosition.Value;
                            handler.ChangeOrientation(lastCamera3DLookAt.Value);
                            //MainCamera.transform.LookAt = lastCamera3DLookAt.Value;
                            //                         if (handler.gameObject.TryGetComponent<Camera>(out var m_cam))
                            //                         {
                            //                             m_cam.transform.position = lastCamera3DPosition.Value;
                            //                             //m_cam.transform.rotation = lastCamera3DLookAt.Value;
                            //                             //handler.ChangeOrientation(lastCamera3DLookAt.Value);
                            //                         }

                        }
                        break;
                }
            }
        }

        public override void AddEditorVoxel(GameObject obj)
        {

        }

        public override void AddEditorScene(GameObject obj)
        {
        }

        public override IEditorObject AddEditorObject(GameObject obj)
        {
            if (obj)
            {
                var exp = obj.GetOrAddComponent<ExposeToEditor>();
                if (obj.TryGetComponent<UnityEditorObject>(out var editor))
                {
                    var _lock = obj.GetOrAddComponent<LockAxes>();
                    {
                        _lock.PositionX = false;
                        _lock.PositionY = false;
                        _lock.PositionZ = false;
                        _lock.ScaleX = true;
                        _lock.ScaleY = true;
                        _lock.ScaleZ = true;
                        _lock.RotationFree = true;
                        _lock.RotationX = true;
                        _lock.RotationZ = true;
                        _lock.RotationY = !editor.IsDirection;
                        _lock.RotationScreen = true;
                        _lock.RectXY = true;
                        _lock.RectXZ = true;
                        _lock.RectYZ = true;
                    }
                }
                return new ExposeEditorObject(exp, obj);
            }
            return null;
        }
        public override void SetCamera(Vector3 pos, Vector3 target)
        {
            IScenePivot scenePivot = irte.GetWindow(RuntimeWindowType.Scene).IOCContainer.Resolve<IScenePivot>();
            scenePivot.SetCameraPositionAndPivot(pos, target);
        }
        public override void LookAt(Vector3 target, bool focuse , float? bodySize)
        {
            IScenePivot scenePivot = irte.GetWindow(RuntimeWindowType.Scene).IOCContainer.Resolve<IScenePivot>();          
            if (focuse)
            {
                if (bodySize.HasValue)
                {
                    scenePivot.Focus(target, bodySize.Value);
                }
                else
                {
                    scenePivot.Focus(target, 10);
                }
                scenePivot.Focus(FocusMode.Default);
            }
            else
            {
                scenePivot.SetCameraPositionAndPivot(MainCamera.transform.position, target);
            }
        }
        public override void LookAt(Transform target, bool focuse, float? bodySize)
        {
            LookAt(target.position, focuse, bodySize);
        }
        //         public override void LookAt(Vector3 target)
        //         {
        //             IScenePivot scenePivot = irte.GetWindow(RuntimeWindowType.Scene).IOCContainer.Resolve<IScenePivot>();
        //             scenePivot.SetCameraPositionAndPivot(MainCamera.transform.position, target);
        //         }
        //         public override void LookAt(Transform target)
        //         {
        //             IScenePivot scenePivot = irte.GetWindow(RuntimeWindowType.Scene).IOCContainer.Resolve<IScenePivot>();
        //             scenePivot.Focus(FocusMode.Default);
        //         }
        //     public override void LoadResource(int resID, string resName, object sender, LoadResourceCallback cb)
        //     {
        //         base.LoadResource(resID, resName, sender, cb);
        //     }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------


    }

    public class ExposeEditorObject : IEditorObject
    {
        public ExposeToEditor Object { get; }
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public bool Selectable { get => Object.ActiveSelf; set => Object.ActiveSelf = value; }

        public ExposeEditorObject(ExposeToEditor exp, GameObject go)
        {
            this.Object = exp;
            this.gameObject = go;
            this.transform = go.transform;
        }

    }
}