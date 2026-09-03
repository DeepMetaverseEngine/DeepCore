using DeepCore;
using DeepCore.Components;
using DeepCore.Concurrent;
using DeepCore.Reflection;
using DeepCore.Unity3D.UGUIAction;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{
    public partial class DisplayNode : IDisposable, IActionCompment
    {
        private static List<GameObject> s_fuck_list = new List<GameObject>();
        private static AtomicLong s_RefCount = new AtomicLong(0);
        private static AtomicLong s_AliveCount = new AtomicLong(0);
        private static RectTransform mDisabledNode = null;
        public static long RefCount { get { return s_RefCount.Value; } }
        public static long AliveCount { get { return s_AliveCount.Value; } }
        protected internal static void FuckFuckList()
        {
            lock (s_fuck_list)
            {
                if (s_fuck_list.Count > 0)
                {
                    foreach (var o in s_fuck_list)
                    {
                        DeepCore.Unity3D.UnityHelper.Destroy(o, 0.3f);
                    }
                    s_fuck_list.Clear();
                }
            }
        }

        private readonly DisplayNodeBehaviour mBinding;
        internal protected readonly GameObject mGameObject;
        internal protected readonly RectTransform mTransform;
        internal protected readonly CanvasRenderer mCanvasRender;

        private DisplayNode mParent;
        private DisplayRoot mRoot;
        // 一般用于上层临时隐藏这个单位 //
        private bool mVisibleInParent = true;

        private bool mVisible = true;
        private bool mIsDispose = false;
        private bool mIsEnable = false;
        private bool mIsEnableChildren = false;
        private bool mIsPointerDown = false;
        private IInteractiveComponent mSelectable;
        private bool mNeedRefreshInteractive = true;
        private ComponentCollection<DisplayNodeComponent> mUIComponents;

        //-------------------------------------------------------------------------------------------------------

        public DisplayNode(string name = "")
        {
            s_RefCount++;
            s_AliveCount++;

            mGameObject = new GameObject(name);
            mGameObject.SetActive(false);

            mTransform = mGameObject.AddComponent<RectTransform>();
            mTransform.anchorMin = new Vector2(0, 1);
            mTransform.anchorMax = new Vector2(0, 1);
            mTransform.pivot = new Vector2(0, 1);
            mTransform.position = Vector3.zero;
            mTransform.anchoredPosition = Vector2.zero;
            mTransform.localScale = Vector3.one;
            mTransform.sizeDelta = new Vector2(100, 100);

            mCanvasRender = GenCanvasRenderer();

            mBinding = GenNodeBehavior();
            mBinding.mBinding = this;

            mUIComponents = new ComponentCollection<DisplayNodeComponent>((a, b) => a.Priority - b.Priority);
            mUIComponents.OnAdded += (obj) => { obj.InternalAdded(this); };
            mUIComponents.OnRemoved += (obj) => { obj.InternalRemoved(this); };

            if (mDisabledNode == null)
            {
                GameObject o = new GameObject("DisabledNode");
                mDisabledNode = o.AddComponent<RectTransform>();
                UnityEngine.Object.DontDestroyOnLoad(o);
                o.SetActive(false);
            }

        }

        public DisplayNode(GameObject obj)
        {
            s_RefCount++;
            s_AliveCount++;

            mGameObject = obj;
            mGameObject.SetActive(false);

            mTransform = mGameObject.transform as RectTransform;
            if (!mTransform)
            {
                mTransform = mGameObject.AddComponent<RectTransform>();
            }
            mTransform.anchorMin = new Vector2(0, 1);
            mTransform.anchorMax = new Vector2(0, 1);
            mTransform.pivot = new Vector2(0, 1);
            mTransform.position = Vector3.zero;
            mTransform.anchoredPosition = Vector2.zero;
            mTransform.localScale = Vector3.one;
            mTransform.sizeDelta = new Vector2(100, 100);

            mCanvasRender = GenCanvasRenderer();

            mBinding = GenNodeBehavior();
            mBinding.mBinding = this;

            mUIComponents = new ComponentCollection<DisplayNodeComponent>((a, b) => a.Priority - b.Priority);
            mUIComponents.OnAdded += (c) => { c.InternalAdded(this); };
            mUIComponents.OnRemoved += (c) => { c.InternalRemoved(this); };

            if (mDisabledNode == null)
            {
                GameObject o = new GameObject("DisabledNode");
                mDisabledNode = o.AddComponent<RectTransform>();
                UnityEngine.Object.DontDestroyOnLoad(o);
                o.SetActive(false);
            }
        }
        ~DisplayNode()
        {
            if (!mIsDispose)
            {
                lock (s_fuck_list)
                {
                    s_fuck_list.Add(mGameObject);
                }
            }
            s_RefCount--;
        }
        public void Dispose()
        {
            //避免2次dispose，造成崩溃.
            if (mIsDispose)
            {
                // throw new Exception("DisplayNode:\"{0}\" already disposed !");
                return;
            }
            this.mUIComponents.ForEach(this, static (e, c) => c.Dispose());
            this.mUpdateNodes.Clear();
            if (this.event_disposed != null)
            {
                this.event_disposed.Invoke(this);
            }
            this.RemoveAllAction();
            this.OnDisposeEvents();
            this.OnDispose();
            this.mAttributes.Clear();
            var children = new List<DisplayNode>();
            {
                GetAllChild(children);
                foreach (var c in children)
                {
                    c.Dispose();
                }
            }
            if (mParent != null)
            {
                mParent.IsChildrenDirty = true;
            }
            this.mParent = null;
            this.mTransform.SetParent(mDisabledNode, false);
            DeepCore.Unity3D.UnityHelper.Destroy(mGameObject, 0.3f);
            this.mIsDispose = true;
            s_AliveCount--;
        }
        public virtual DisplayNode Clone()
        {
            return null;
        }

        //-------------------------------------------------------------------------------------------------------
        public ComponentCollection<DisplayNodeComponent> UIComponents { get => mUIComponents; }
        public GameObject UnityObject { get { return mGameObject; } }
        public DisplayNode Parent { get { return mParent; } }
        public RectTransform Transform { get { return mTransform; } }
        public DisplayRoot Root
        {
            get
            {
                if (this is DisplayRoot)
                {
                    mRoot = this as DisplayRoot;
                    return mRoot;
                }
                else if (mRoot != null)
                {
                    return mRoot;
                }
                else
                {
                    DisplayNode parent = mParent;
                    while (parent != null)
                    {
                        if (parent is DisplayRoot)
                        {
                            mRoot = parent as DisplayRoot;
                            return mRoot;
                        }
                        parent = parent.mParent;
                    }
                }
                return mRoot;
            }
        }

        public bool IsDispose { get { return mIsDispose; } }
        public int NumChildren { get { return mTransform.childCount; } }

        public string UserData { get; set; }
        public int UserTag { get; set; }
        public object Tag { get; set; }

        public string Name
        {
            get { return mGameObject.name; }
            set { mGameObject.name = value; }
        }
        /// <summary>
        /// 可显示
        /// </summary>
        public bool Visible
        {
            get { return mVisible; }
            set
            {
                if (mVisible != value)
                {
                    mVisible = value;
                    if (mParent != null)
                    {
                        mGameObject.SetActive(mVisibleInParent && mVisible);
                    }
                    else
                    {
                        mGameObject.SetActive(false);
                    }
                    OnVisibleChanged(value);
                }
            }
        }
        /// <summary>
        /// 是否在父节点显示范围内，用于滚动控件交互以及临时需要隐藏节点操作
        /// </summary>
        public bool VisibleInParent
        {
            get { return mVisibleInParent; }
            set
            {
                if (mVisibleInParent != value)
                {
                    mVisibleInParent = value;
                    if (mParent != null)
                    {
                        mGameObject.SetActive(mVisibleInParent && mVisible);
                    }
                    else
                    {
                        mGameObject.SetActive(false);
                    }
                    OnVisibleChanged(value);
                }
            }
        }

        /// <summary>
        /// 目标外也能触发move事件
        /// </summary>
        public bool EnableOutMove { get; set; }

        /// <summary>
        /// 可点击自身
        /// </summary>
        public bool Enable
        {
            get { return mIsEnable; }
            set
            {
                mBinding.IsEnable = value;
                if (mIsEnable != value)
                {
                    mIsEnable = value;
                    mNeedRefreshInteractive = true;
                }
            }
        }
        /// <summary>
        /// 可点击子节点
        /// </summary>
        public bool EnableChildren
        {
            get { return mIsEnableChildren; }
            set
            {
                mBinding.IsEnableChildren = value;
                mIsEnableChildren = value;
            }
        }
        /// <summary>
        /// 是否允许点击事件
        /// </summary>
        public bool IsInteractive
        {
            get { return mSelectable != null && mSelectable.AsSelectable.enabled; }
            set
            {
                mBinding.IsInteractive = value;
                if (IsInteractive != value)
                {
                    if (value)
                    {
                        if (mSelectable == null)
                        {
                            mSelectable = GenInteractive();
                            mSelectable.event_PointerDown = this.DoPointerDown;
                            mSelectable.event_PointerUp = this.DoPointerUp;
                            mSelectable.event_PointerClick = this.DoPointerClick;
                            mSelectable.AsSelectable.enabled = true;
                            mSelectable.AsSelectable.interactable = true;
                        }
                        else
                        {
                            mSelectable.AsSelectable.enabled = true;
                            mSelectable.AsSelectable.interactable = true;
                        }
                    }
                    else
                    {
                        if (mSelectable != null)
                        {
                            mSelectable.AsSelectable.enabled = false;
                            mSelectable.AsSelectable.interactable = false;
                        }
                    }
                    mNeedRefreshInteractive = true;
                }
            }
        }

        /// <summary>
        /// 父节点允许子节点点击
        /// </summary>
        public bool EnableTouchInParents
        {
            get
            {
                DisplayNode parent = mParent;
                while (parent != null)
                {
                    if (!parent.EnableChildren)
                    {
                        return false;
                    }
                    parent = parent.mParent;
                }
                return true;
            }
        }


        //-------------------------------------------------------------------------------------------------------


        public T AddComponent<T>() where T : Component
        {
            return mGameObject.AddComponent<T>();
        }
        public Component AddComponent(Type componentType)
        {
            return mGameObject.AddComponent(componentType);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        #region __Events__

        internal void DoStart()
        {
            this.OnStart();
        }
        internal void DoUpdate()
        {
            if (mNeedRefreshInteractive)
            {
                mNeedRefreshInteractive = false;
                OnInteractiveChanged();
            }
            if (mSelectable != null && (EnableOutMove ? mIsPointerDown : mSelectable.IsPressDown))
            {
                DoPointerMove(mSelectable.LastPointerDown);
            }
            this.UpdateAction(Time.deltaTime);
            this.OnUpdate();
            this.mUIComponents.ForEach(this, static (c, com) => com.DoUpdate());
        }
        internal void DoEndUpdate()
        {
            this.OnEndUpdate();
        }
        private void DoPointerDown(PointerEventData e)
        {
            this.OnPointerDown(e);
            mIsPointerDown = true;
            if (this.event_PointerDown != null)
            {
                this.event_PointerDown.Invoke(this, e);
            }
            this.mUIComponents.ForEach(e, static (e, c) => c.DoPointerDown(e));
        }
        private void DoPointerUp(PointerEventData e)
        {
            this.OnPointerUp(e);
            mIsPointerDown = false;
            if (this.event_PointerUp != null)
            {
                this.event_PointerUp.Invoke(this, e);
            }
            this.mUIComponents.ForEach(e, static (e, c) => c.DoPointerUp(e));
        }
        private void DoPointerClick(PointerEventData e)
        {
            this.OnPointerClick(e);
            if (this.event_PointerClick != null)
            {
                this.event_PointerClick.Invoke(this, e);
            }
            this.mUIComponents.ForEach(e, static (e, c) => c.DoPointerClick(e));
        }
        private void DoPointerMove(PointerEventData e)
        {
            this.OnPointerMove(e);
            if (this.event_PointerMove != null)
            {
                this.event_PointerMove.Invoke(this, e);
            }
        }

        protected virtual void OnDispose() { }
        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnEndUpdate() { }
        protected virtual void OnChildAdded(DisplayNode child) { }
        protected virtual void OnChildRemoved(DisplayNode child) { }
        protected virtual void OnSizeChanged(Vector2 size) { }
        protected virtual void OnVisibleChanged(bool visible) { }
        protected virtual void OnPointerDown(PointerEventData e) { }
        protected virtual void OnPointerUp(PointerEventData e) { }
        protected virtual void OnPointerClick(PointerEventData e) { }
        protected virtual void OnPointerMove(PointerEventData e) { }

        public delegate void PointerEventHandler(DisplayNode sender, PointerEventData e);
        public delegate void ChildEventHandler(DisplayNode sender, DisplayNode e);
        public delegate void DiposeEventHandle(DisplayNode sender);

        public ChildEventHandler event_ChildAdded;
        public ChildEventHandler event_ChildRemoved;
        public PointerEventHandler event_PointerDown;
        public PointerEventHandler event_PointerUp;
        public PointerEventHandler event_PointerMove;
        public PointerEventHandler event_PointerClick;
        public DiposeEventHandle event_disposed;

        [Desc("节点已销毁")]
        public event DiposeEventHandle Disposed { add { event_disposed += value; } remove { event_disposed -= value; } }
        [Desc("子节点已添加")]
        public event ChildEventHandler ChildAdded { add { event_ChildAdded += value; } remove { event_ChildAdded -= value; } }
        [Desc("子节点已移除")]
        public event ChildEventHandler ChildRemoved { add { event_ChildRemoved += value; } remove { event_ChildRemoved -= value; } }
        [Desc("鼠标(触摸)已按下")]
        public event PointerEventHandler PointerDown { add { event_PointerDown += value; } remove { event_PointerDown -= value; } }
        [Desc("鼠标(触摸)已松开")]
        public event PointerEventHandler PointerUp { add { event_PointerUp += value; } remove { event_PointerUp -= value; } }
        [Desc("鼠标(触摸)已移动")]
        public event PointerEventHandler PointerMove { add { event_PointerMove += value; } remove { event_PointerMove -= value; } }
        [Desc("Click")]
        public event PointerEventHandler PointerClick { add { event_PointerClick += value; } remove { event_PointerClick -= value; } }

        protected virtual void OnDisposeEvents()
        {
            this.event_ChildAdded = null;
            this.event_ChildRemoved = null;
            this.event_PointerDown = null;
            this.event_PointerUp = null;
            this.event_PointerMove = null;
            this.event_PointerClick = null;
            this.event_disposed = null;
        }

        #endregion
        //-----------------------------------------------------------------------------------
        #region __Transform2D__

        public float X
        {
            get { return Position2D.x; }
            set { Position2D = new Vector2(value, Position2D.y); }
        }
        public float Y
        {
            get { return Position2D.y; }
            set { Position2D = new Vector2(Position2D.x, value); }
        }
        public float Width
        {
            get { return Size2D.x; }
            set { Size2D = new Vector2(value, Size2D.y); }
        }
        public float Height
        {
            get { return Size2D.y; }
            set { Size2D = new Vector2(Size2D.x, value); }
        }

        /// <summary>
        /// 缩放系数.
        /// </summary>
        public Vector2 Scale
        {
            get
            {
                Vector3 temp = mTransform.localScale;
                return new Vector2(temp.x, temp.y);
            }
            set
            {
                mTransform.localScale = new Vector3(value.x, value.y, 1);
            }
        }

        public Rect Bounds2D
        {
            get
            {
                Rect rect = new Rect();
                rect.position = mTransform.localPosition;
                rect.size = mTransform.sizeDelta;
                rect.y = -rect.y;
                return rect;
            }
            set
            {
                this.Position2D = value.position;
                this.Size2D = value.size;
            }
        }
        public Vector2 Position2D
        {
            get
            {
                Vector2 pos = mTransform.localPosition;
                pos.y = -pos.y;
                return pos;
            }
            set
            {
                value.y = -value.y;
                this.mTransform.localPosition = new Vector3(value.x, value.y, this.mTransform.localPosition.z);
            }
        }
        public Vector2 Size2D
        {
            get { return mTransform.sizeDelta; }
            set
            {
                if (this.mTransform.sizeDelta != value)
                {
                    this.mTransform.sizeDelta = value;
                    OnSizeChanged(value);
                }
            }
        }

        public void SetAnchor(Vector2 v)
        {
            if (this.Parent == null || (mTransform.anchorMin == v && mTransform.anchorMax == v))
            {
                return;
            }
            Vector2 last = mTransform.anchoredPosition;
            Vector2 s = mTransform.sizeDelta;
            Vector2 p = this.Position2D;

            mTransform.anchorMin = v;
            mTransform.anchorMax = v;
            mTransform.offsetMin = Vector2.zero;
            mTransform.offsetMax = Vector2.zero;

            Vector2 half = new Vector2(this.Parent.Size2D.x * v.x, this.Parent.Size2D.y * v.y);
            Vector2 shalf = new Vector2(s.x * v.x, s.y * v.y);

            Vector2 offet = new Vector2(half.x - last.x - shalf.x, half.y + last.y - shalf.y);
            mTransform.anchoredPosition = offet;
        }

        public void SetPivot(float x, float y)
        {
            mTransform.pivot = new Vector2(x, y);
        }

        public void SetAnchoredPosition(float x, float y)
        {
            mTransform.anchoredPosition = new Vector2(x, y);
        }

        public Vector2 ScreenToLocalPoint2D(PointerEventData e)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(mTransform, e.position, e.pressEventCamera, out local);
            local.y = -local.y;
            return local;
        }

        /// <summary>
        /// 本地坐标转世界坐标系.
        /// </summary>
        /// <returns></returns>
        public Vector2 LocalToGlobal()
        {
            return this.UnityObject.transform.parent.TransformPoint(mTransform.localPosition);
        }

        /// <summary>
        /// 世界坐标转本地坐标系.是否为MFUI坐标系，若为false则会自动转换.
        /// </summary>
        /// <param name="wordpos"></param>
        /// <param name="isMFUGUIPos"></param>
        /// <returns></returns>
        public Vector2 GlobalToLocal(Vector2 wordpos, bool isMFUGUIPos = true)
        {
            Vector2 ret = this.UnityObject.transform.InverseTransformPoint(wordpos);
            if (isMFUGUIPos)
            {
                ret.y = -ret.y;
            }
            return ret;
        }


        #endregion

        //---------------------------------------------------------------------------------------------------------------------------------------------
        #region _Graphics_

        private Graphic mGraphic;
        private GrayMaterialModifier mGrayModifier;
        private BitSet8 mDirtyMask = new BitSet8(0);
        private float mAlpha = 1f;
        private float mParentAlpha = 1f;
        private bool mIsGray = false;
        private bool mParentIsGray = false;

        /// <summary>
        /// Alpha度
        /// </summary>
        public float Alpha
        {
            get { return mAlpha; }
            set
            {
                if (mAlpha != value)
                {
                    this.SetAlphaInternal(value, true);
                    this.mAlpha = value;
                    this.mBinding.Alpha = value;
                }
            }
        }

        public float RealAlpha
        {
            get { return mAlpha * mParentAlpha; }
        }

        public bool IsGray
        {
            get { return mIsGray; }
            set
            {
                if (mIsGray != value)
                {
                    this.SetGrayInternal(value, true);
                    this.mIsGray = value;
                    this.mBinding.IsGray = value;
                }
            }
        }

        public bool IsAlphaDirty
        {
            get { return mDirtyMask.Get(0); }
            set { mDirtyMask.Set(0, value); }
        }
        public bool IsGrayDirty
        {
            get { return mDirtyMask.Get(1); }
            set { mDirtyMask.Set(1, value); }
        }
        private bool IsChildrenDirty
        {
            get { return mDirtyMask.Get(2); }
            set { mDirtyMask.Set(2, value); }
        }


        protected Graphic GetGraphic()
        {
            if (mGraphic == null)
            {
                mGraphic = mGameObject.GetComponent<Graphic>();
            }
            return mGraphic;
        }

        private void SetAlphaInternal(float _alpha, bool recursive = true)
        {
            float realAlpha = _alpha * mParentAlpha;
            var grap = GetGraphic();
            if (grap != null)
            {
                var c = grap.color;
                c.a = realAlpha;
                grap.color = c;
            }
            if (recursive)
            {
                RefreshUpdateNodes();
                int count = mUpdateNodes.Count;
                for (int i = 0; i < count; i++)
                {
                    var node = mUpdateNodes[i];
                    if (node != null)
                    {
                        node.mParentAlpha = realAlpha;
                        node.SetAlphaInternal(node.mAlpha, recursive);
                    }
                }
            }
        }

        protected void SetGrayInternal(bool isGray, bool recursive = true)
        {
            bool trueGray = mParentIsGray || isGray;
            var grap = GetGraphic();
            if (grap != null)
            {
                if (trueGray)
                {
                    if (this.mGrayModifier == null)
                    {
                        this.mGrayModifier = GenGrayMaterialModifier(grap);
                    }
                }
                else
                {
                    if (this.mGrayModifier != null)
                    {
                        DeepCore.Unity3D.UnityHelper.DestroyImmediate(mGrayModifier);
                        this.mGrayModifier = null;
                    }
                }
                grap.SetMaterialDirty();
            }
            if (recursive)
            {
                RefreshUpdateNodes();
                int count = mUpdateNodes.Count;
                for (int i = 0; i < count; i++)
                {
                    var node = mUpdateNodes[i];
                    if (node != null)
                    {
                        node.mParentIsGray = trueGray;
                        node.SetGrayInternal(node.mIsGray, recursive);
                    }
                }
            }
        }

        private void RefreshDirty()
        {
            if (IsChildrenDirty)
            {
                RefreshUpdateNodes();
                IsChildrenDirty = false;
            }
            if (IsAlphaDirty)
            {
                SetAlphaInternal(this.mAlpha);
                IsAlphaDirty = false;
            }
            if (IsGrayDirty)
            {
                SetGrayInternal(mIsGray);
                IsGrayDirty = false;
            }
        }

        protected void CheckDirty()
        {
            if (this.mParentAlpha != Parent.RealAlpha)
            {
                this.mParentAlpha = Parent.RealAlpha;
                this.IsAlphaDirty = true;
            }
            if (this.mParentIsGray != (Parent.mParentIsGray || Parent.mIsGray))
            {
                this.mParentIsGray = (Parent.mParentIsGray || Parent.mIsGray);
                this.IsGrayDirty = true;
            }
        }
        #endregion

        //-----------------------------------------------------------------------------------

        #region __BindingMonoBehaviour__

        public static DisplayNode AsDisplayNode(GameObject obj)
        {
            DisplayNodeBehaviour binding = obj.GetComponent<DisplayNodeBehaviour>();
            if (binding != null)
            {
                return binding.Binding;
            }
            return null;
        }
        public static DisplayNode AsDisplayNode(Component obj)
        {
            DisplayNodeBehaviour binding = obj.GetComponent<DisplayNodeBehaviour>();
            if (binding != null)
            {
                return binding.Binding;
            }
            return null;
        }

        private bool mIsInit = false;
        private List<DisplayNode> mUpdateNodes = new List<DisplayNode>();

        private List<DisplayNode> RefreshUpdateNodes()
        {
            if (!IsChildrenDirty)
                return mUpdateNodes;

            IsChildrenDirty = false;

            int num_children = NumChildren;
            if (mUpdateNodes.Count != num_children)
            {
                CUtils.SetListSize(mUpdateNodes, num_children);
            }
            for (int i = num_children - 1; i >= 0; --i)
            {
                Transform child = mTransform.GetChild(i);
                if (mUpdateNodes[i] != null && mUpdateNodes[i].mTransform == child)
                {
                    continue;
                }
                else
                {
                    mUpdateNodes[i] = AsDisplayNode(child);
                }
            }
            return mUpdateNodes;
        }

        internal virtual void InternalUpdate()
        {
            if (!mIsInit)
            {
                this.mIsInit = true;
                this.DoStart();
                mBinding.IsEnable = this.Enable;
                mBinding.IsEnableChildren = this.EnableChildren;
                mBinding.IsInteractive = this.IsInteractive;
                mBinding.Alpha = this.Alpha;
                mBinding.IsGray = this.IsGray;
            }
            else //if (UnityEngine.Application.isEditor)
            {
                if (this.Enable != mBinding.IsEnable)
                {
                    this.Enable = mBinding.IsEnable;
                }
                if (this.EnableChildren != mBinding.IsEnableChildren)
                {
                    this.EnableChildren = mBinding.IsEnableChildren;
                }
                if (this.IsInteractive != mBinding.IsInteractive)
                {
                    this.IsInteractive = mBinding.IsInteractive;
                }
                if (this.Alpha != mBinding.Alpha)
                {
                    this.Alpha = mBinding.Alpha;
                }
                if (this.IsGray != mBinding.IsGray)
                {
                    this.IsGray = mBinding.IsGray;
                }
            }

            if (mIsDispose) return;
            if (mGameObject.activeSelf)
            {
                this.DoUpdate();
                this.RefreshDirty();
                DisplayNode child;
                for (int i = mUpdateNodes.Count - 1; i >= 0 && i < mUpdateNodes.Count; --i)
                {
                    if ((child = mUpdateNodes[i]) != null)
                    {
                        child.InternalUpdate();
                    }
                }
                this.DoEndUpdate();
            }
            else
            {
                this.RefreshDirty();
            }
        }

        protected virtual IInteractiveComponent GenInteractive()
        {
            return mGameObject.AddComponent<DisplayNodeInteractive>();
        }
        protected virtual DisplayNodeBehaviour GenNodeBehavior()
        {
            return mGameObject.AddComponent<DisplayNodeBehaviour>();
        }
        protected virtual CanvasRenderer GenCanvasRenderer()
        {
            return mGameObject.AddComponent<CanvasRenderer>();
        }
        protected virtual GrayMaterialModifier GenGrayMaterialModifier(UnityEngine.UI.Graphic g)
        {
            if (g is Text)
            {
                return mGameObject.AddComponent<TextGrayMaterialModifier>();
            }
            else
            {
                return mGameObject.AddComponent<GrayMaterialModifier>();
            }
        }
        protected virtual void OnInteractiveChanged()
        {
            var g = mGameObject.GetComponent<UnityEngine.UI.Graphic>();
            if (g != null)
            {
                g.raycastTarget = IsInteractive && Enable;
            }
            //ForEachChilds<DisplayNode>((c) => { c.OnInteractiveChanged(); }, true);
        }
        /// <summary>
        /// 交互能力
        /// </summary>
        public IInteractiveComponent Selectable
        {
            get { return mSelectable; }
        }

        /// <summary>
        /// 是否被按下
        /// </summary>
        public bool IsPressed
        {
            get { return (mSelectable != null) ? mSelectable.IsPressDown : false; }
        }

        #endregion


        //-----------------------------------------------------------------------------------

        #region __Container__

        public bool AddChildAt(DisplayNode child, int index)
        {
            if (child == null || index < 0)
            {
                return false;
            }
            if (child.Parent == this)
            {
                SetChildIndex(child, index);
            }
            else
            {
                child.RemoveFromParent();
                child.mParent = this;
                child.mTransform.SetParent(this.mTransform, false);
                child.mGameObject.SetActive(child.mVisible);
                child.CheckDirty();
                this.IsChildrenDirty = true;
                this.SetChildIndex(child, index);
                this.OnChildAdded(child);
                if (event_ChildAdded != null) event_ChildAdded.Invoke(this, child);
                return true;
            }
            return false;
        }
        public bool RemoveChild(DisplayNode child, bool dispose = false)
        {
            if (child.mParent == this)
            {
                child.mParent = null;
                child.mTransform.SetParent(mDisabledNode, false);
                IsChildrenDirty = true;
                //child.mTransform.SetParent(this.mTransform.root.transform, false);
                //child.mGameObject.SetActive(false);
                this.OnChildRemoved(child);
                if (event_ChildRemoved != null) event_ChildRemoved.Invoke(this, child);
                if (dispose)
                {
                    child.Dispose();
                }
                return true;
            }
            return false;
        }


        public bool ContainsChild(DisplayNode child)
        {
            while (child != null)
            {
                if (child == this)
                    return true;
                else
                    child = child.Parent;
            }
            return false;
        }
        public void AddChild(DisplayNode child)
        {
            this.AddChildAt(child, NumChildren);
        }
        public DisplayNode RemoveChildByName(string name, bool dispose = false)
        {
            Transform child = mTransform.Find(name);

            if (child == null)
            {
                return null;
            }

            DisplayNode ret = AsDisplayNode(child.gameObject);
            if (ret != null)
            {
                ret.RemoveFromParent(dispose);
            }
            return ret;
        }
        public DisplayNode RemoveChildAt(int index, bool dispose = false)
        {
            DisplayNode ret = GetChildAt(index);
            if (ret != null)
            {
                ret.RemoveFromParent(dispose);
            }
            return ret;
        }
        public void RemoveChildren(int beginIndex = 0, int endIndex = -1, bool dispose = false)
        {
            if (endIndex < 0 || endIndex >= NumChildren)
                endIndex = NumChildren - 1;
            for (int i = beginIndex; i <= endIndex; ++i)
                RemoveChildAt(beginIndex, dispose);
        }
        public void RemoveAllChildren(bool dispose = true)
        {
            RemoveChildren(0, -1, dispose);
        }
        public void RemoveFromParent(bool dispose = false)
        {
            if (mParent != null)
            {
                mParent.RemoveChild(this, dispose);
                mParent = null;
            }
        }

        /** Returns a child object at a certain index. */
        public DisplayNode GetChildAt(int index)
        {
            Transform child = mTransform.GetChild(index);
            DisplayNode ret = AsDisplayNode(child.gameObject);
            return ret;
        }
//         [Obsolete]
//         public IEnumerable<DisplayNode> AllChildren
//         {
//             get { return GetAllChild(); }
//         }
//         [Obsolete]
//         public IEnumerable<DisplayNode> GetAllChild()
//         {
//             List<DisplayNode> ret = new List<DisplayNode>(mTransform.childCount);
//             GetAllChild(ret);
//             return ret;
//         }
        public void GetAllChild(List<DisplayNode> ret)
        {
            int count = mTransform.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = mTransform.GetChild(i);
                DisplayNode node = AsDisplayNode(child.gameObject);
                if (node != null)
                {
                    ret.Add(node);
                }
            }
        }

        public void SetParentIndex(int index)
        {
            mTransform.SetSiblingIndex(index);
        }
        public void SetChildIndex(DisplayNode child, int index)
        {
            child.mTransform.SetSiblingIndex(index);
        }
        public int GetChildIndex(DisplayNode child)
        {
            return child.mTransform.GetSiblingIndex();
        }

        #endregion

        //-----------------------------------------------------------------------------------

        #region __ChildsUtils__

        /// <summary>
        /// 根据名字搜寻子节点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public T FindChildByName<T>(string name, bool recursive = true) where T : DisplayNode
        {
            return FindChildAs<T>((child) =>
            {
                return (name == child.Name);
            },
            recursive);
        }

        public DisplayNode FindChildByName(string name, bool recursive = true)
        {
            return FindChildByName<DisplayNode>(name, recursive);
        }

        public DisplayNode FindChild(Predicate<DisplayNode> select, bool recursive = true)
        {
            return FindChildAs<DisplayNode>(select, recursive);
        }

        /// <summary>
        /// 搜寻子节点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="select">选择器，返回True表示搜索到</param>
        /// <param name="recursive">是否递归</param>
        /// <returns></returns>
        public T FindChildAs<T>(Predicate<T> select, bool recursive = true) where T : DisplayNode
        {
            T child = null;
            T uicc = null;
            int i;
            int length = NumChildren;
            for (i = length - 1; i >= 0; --i)
            {
                child = this.GetChildAt(i) as T;
                if (child != null && select(child))
                {
                    return child;
                }
            }
            if (recursive)
            {
                length = NumChildren;
                for (i = length - 1; i >= 0; --i)
                {
                    DisplayNode sc = this.GetChildAt(i);
                    if (sc != null)
                    {
                        uicc = sc.FindChildAs<T>(select, recursive);
                        if (uicc != null)
                        {
                            return uicc;
                        }
                    }
                }
            }
            return null;
        }

        public void ForEachChilds<T>(Action<T> action, bool recursive = true) where T : DisplayNode
        {
            T child = null;
            int i;
            int length = NumChildren;
            for (i = length - 1; i >= 0; --i)
            {
                child = this.GetChildAt(i) as T;
                if (child != null)
                {
                    action(child);
                }
            }
            if (recursive)
            {
                length = NumChildren;
                for (i = length - 1; i >= 0; --i)
                {
                    DisplayNode sc = this.GetChildAt(i);
                    if (sc != null)
                    {
                        sc.ForEachChilds<T>(action, recursive);
                    }
                }
            }
        }

        /// <summary>
        /// 获得所有子节点总共占用尺寸
        /// </summary>
        /// <returns></returns>
        public Vector2 GetChildsContentSize()
        {
            Vector2 ret = new Vector2(0, 0);
            int count = mTransform.childCount;
            for (int i = 0; i < mTransform.childCount; i++)
            {
                var child = mTransform.GetChild(i);
                DisplayNode node = AsDisplayNode(child.gameObject);
                if (node != null)
                {
                    Vector2 size = node.Size2D;
                    ret.x += Math.Max(ret.x, size.x);
                    ret.y += Math.Max(ret.y, size.y);
                }
            }
            return ret;
        }

        public void SetContentSizeFitter(ContentSizeFitter.FitMode h, ContentSizeFitter.FitMode v)
        {
            var fitter = UnityObject.GetComponent<ContentSizeFitter>();
            if (!fitter)
                fitter = UnityObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = h;
            fitter.verticalFit = v;
        }

        #endregion

        //-----------------------------------------------------------------------------------
        #region __Attribute__

        private HashMap<string, object> mAttributes = new HashMap<string, object>();

        public bool IsAttribute(string key)
        {
            return mAttributes.ContainsKey(key);
        }
        public void SetAttribute(string key, object value)
        {
            mAttributes.Put(key, value);
        }
        public object GetAttribute(string key)
        {
            return mAttributes.Get(key);
        }

        public string[] GetAttributeKeys()
        {
            return mAttributes.Select(entry => entry.Key).ToArray();
        }

        public T GetAttributeAs<T>(string key)
        {
            object obj = mAttributes.Get(key);
            if (obj != null)
            {
                return (T)obj;
            }
            return default(T);
        }

        public int GetAttributeCount()
        {
            return mAttributes.Count;
        }
        #endregion
        //-----------------------------------------------------------------------------------

        #region MFAction.

        private readonly List<CellAction> mActionList = new List<CellAction>();
        private List<IAction> mWaitList = new List<IAction>();


        protected class CellAction
        {
            public string mActionType = null;
            public IAction mAction = null;

            public CellAction(IAction action)
            {
                mAction = action;
                mActionType = action.GetActionType();
            }

            public void Dispose()
            {
                mActionType = null;
                mAction = null;
            }
        }


        protected virtual string ParseActionType(IAction action)
        {
            return action.GetActionType();
        }

        public virtual void AddAction(IAction action)
        {
            if (action == null)
            {
                throw new Exception("action can not be null");
            }

            PushWaitList(action);
        }

        private void PushWaitList(IAction action)
        {
            mWaitList.Add(action);
        }

        private void CheckAction(IAction action)
        {
            string actionType = ParseActionType(action);

            IAction oldAction = null;
            if (HasAction(actionType))
            {
                //mMapAction.TryGetValue(actionType, out oldAction);
                oldAction = GetAction(actionType);

                if (oldAction != null)
                {
                    RemoveAction(oldAction, false);
                }
            }

            StartAction(actionType, action);
        }

        private void UpdateWaitList()
        {
            for (int i = 0; i < mWaitList.Count; i++)
            {
                CheckAction(mWaitList[i]);
            }

            mWaitList.Clear();
        }


        protected virtual void StartAction(string actionType, IAction action)
        {

            CellAction ca = new CellAction(action);

            //先放到队列中，下一帧执行.
            mActionList.Add(ca);

            action.onStart(this);
        }

        public virtual void RemoveAction(IAction action, bool sendCallBack)
        {

            RemoveAction(ParseActionType(action), sendCallBack);
        }

        public virtual void RemoveAction(string actionType, bool sendCallBack)
        {
            for (int i = 0; i < mActionList.Count; i++)
            {
                if (mActionList[i].mActionType == actionType)
                {
                    mActionList[i].mAction.onStop(this, sendCallBack);
                    mActionList[i].Dispose();
                    mActionList.RemoveAt(i);
                    i--;
                    return;
                }
            }
        }

        public virtual bool HasAction(IAction action)
        {
            string name = ParseActionType(action);
            return HasAction(name);
        }

        public virtual bool HasAction(string ActionType)
        {

            for (int i = 0; i < mActionList.Count; i++)
            {
                if (mActionList[i].mActionType == ActionType)
                {
                    return true;
                }
            }

            return false;

        }

        public virtual IAction GetAction(string actionType)
        {

            for (int i = 0; i < mActionList.Count; i++)
            {
                if (mActionList[i].mActionType == actionType)
                {
                    return mActionList[i].mAction;
                }
            }

            return null;
        }

        public virtual void RemoveAllAction(bool sendCallBack = false)
        {

            for (int i = mActionList.Count - 1; i >= 0; i--)
            {
                mActionList[i].mAction.onStop(this, sendCallBack);
                mActionList[i].Dispose();
            }

            mActionList.Clear();

            mWaitList.Clear();
        }

        public virtual void UpdateAction(float deltaTime)
        {
            if (mActionList != null && mActionList.Count > 0)
            {
                IAction act = null;

                for (int i = 0; i < mActionList.Count; i++)
                {
                    CellAction ca = mActionList[i];
                    act = ca.mAction;

                    act.onUpdate(this, deltaTime);

                    if (act.IsEnd())
                    {
                        act.onStop(this, true);
                        ca.Dispose();
                        mActionList.Remove(ca);
                        i--;
                    }
                }
            }

            UpdateWaitList();
        }



        #endregion

        //-----------------------------------------------------------------------------------
    }


}
