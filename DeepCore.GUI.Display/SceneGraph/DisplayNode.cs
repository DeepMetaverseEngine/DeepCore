using DeepCore.Components;
using DeepCore.Geometry;
using System;
using System.Collections.Generic;

namespace DeepCore.GUI.SceneGraph
{
    public class DisplayNode : Disposable
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(DisplayNode)) { Verbos = false };
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        public string Name { get; set; }
        public object Tag { get; set; }
        public SingleThreadCollectionPool ObjectPool { get => root?.collection_pool; }
        public DisplayNode()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~DisplayNode()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }

        //-------------------------------------------------------------------------------------------------------------

        #region IDisposable
        protected virtual void OnDispose() { }

        private List<IDisposable> autoRelease;
        sealed protected override void Disposing()
        {
            OnDisposeEvents();
            OnDispose();
            components?.Dispose();
            components = null;
            OnDisposeChildren();
            if (autoRelease != null)
            {
                foreach (var dis in autoRelease) { dis.Dispose(); }
                autoRelease.Clear();
                autoRelease = null;
            }
        }
        public T AutoRelease<T>(T dis) where T : IDisposable
        {
            if (dis != null)
            {
                if (autoRelease == null) autoRelease = new List<IDisposable>();
                autoRelease.Add(dis);
            }
            return dis;
        }
        #endregion

        //-------------------------------------------------------------------------------------------------------------

        //         #region Tint
        // 
        //         public float Alpha { get; set; }
        //         /// <summary>
        //         /// Color RGB
        //         /// </summary>
        //         public uint Color { get; set; }
        // 
        // 
        //         #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Interactive
        /// <summary>
        /// 鼠标相对于当前节点的位置
        /// </summary>
        public virtual Vector2 LocalMouseLocation
        {
            get
            {
                if (root == null) return Vector2.Zero;
                var root_mouse = root.RootMousePoint;
                return TransformRootToLocal(root_mouse);
            }
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Compnents

        private DisplayNodeComponentCollection components;
        public DisplayNodeComponentCollection Components
        {
            get
            {
                if (components == null) { this.components = new(this, static (a, b) => a.Priority - b.Priority); }
                return components;
            }
        }
        private void UpdateComponents()
        {
            components?.ForEach(0, static (st, c) => c.InternalUpdate());
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Transform


        private Transform transform = new Transform();

        public Vector3 Position
        {
            get { return transform.Translation; }
            set { transform.Translation = value; }
        }
        public float Rotation
        {
            get { return transform.Rotation; }
            set { transform.Rotation = value; }
        }
        public Vector2 Scale
        {
            get { return transform.Scale; }
            set { transform.Scale = value; }
        }


        /// <summary>
        /// 本地坐标转换为父节点坐标
        /// </summary>
        public Vector3 TransformLocalToParent(Vector3 pts)
        {
            return transform.LocalToParent(pts);
        }
        /// <summary>
        /// 父节点坐标转换为本地坐标
        /// </summary>
        public Vector3 TransformParentToLocal(Vector3 pts)
        {
            return transform.ParentToLocal(pts);
        }
        /// <summary>
        /// 屏幕坐标转换为本地坐标
        /// </summary>
        public Vector3 TransformRootToLocal(Vector3 pts)
        {
            if (parent != null)
            {
                using (var path = ObjectPool.AllocList<DisplayNode>())
                {
                    SceneGraphTreePath(path);
                    foreach (var node in path)
                    {
                        pts = node.transform.ParentToLocal(pts);
                    }
                }
            }
            return pts;
        }
        /// <summary>
        /// 本地坐标转换为屏幕坐标
        /// </summary>
        public Vector3 TransformLocalToRoot(Vector3 pts)
        {
            if (parent != null)
            {
                var curnode = this;
                do
                {
                    pts = curnode.transform.LocalToParent(pts);
                    curnode = curnode.Parent;
                }
                while (curnode != null);
            }
            return pts;
        }

        /// <summary>
        /// 本地坐标转换为父节点坐标
        /// </summary>
        public void TransformLocalToParent(Vector3[] pts)
        {
            transform.LocalToParent(pts);
        }
        /// <summary>
        /// 父节点坐标转换为本地坐标
        /// </summary>
        public void TransformParentToLocal(Vector3[] pts)
        {
            transform.ParentToLocal(pts);
        }
        /// <summary>
        /// 屏幕坐标转换为本地坐标
        /// </summary>
        public void TransformRootToLocal(Vector3[] pts)
        {
            if (parent != null)
            {
                using (var path = ObjectPool.AllocList<DisplayNode>())
                {
                    SceneGraphTreePath(path);
                    foreach (var node in path)
                    {
                        node.transform.ParentToLocal(pts);
                    }
                }
            }
        }
        /// <summary>
        /// 本地坐标转换为屏幕坐标
        /// </summary>
        public void TransformLocalToRoot(Vector3[] pts)
        {
            if (parent != null)
            {
                var curnode = this;
                do
                {
                    curnode.transform.LocalToParent(pts);
                    curnode = curnode.Parent;
                }
                while (curnode != null);
            }
        }



        /// <summary>
        /// 本地坐标转换为父节点坐标
        /// </summary>
        public RectangleF TransformLocalToParent(RectangleF pts)
        {
            var p1 = pts.Location;
            var p2 = pts.Location + pts.Size;
            p1 = TransformLocalToParent(p1);
            p2 = TransformLocalToParent(p2);
            return new RectangleF(p1, p2 - p1);
        }
        /// <summary>
        /// 父节点坐标转换为本地坐标
        /// </summary>
        public RectangleF TransformParentToLocal(RectangleF pts)
        {
            var p1 = pts.Location;
            var p2 = pts.Location + pts.Size;
            p1 = TransformParentToLocal(p1);
            p2 = TransformParentToLocal(p2);
            return new RectangleF(p1, p2 - p1);
        }
        /// <summary>
        /// 屏幕坐标转换为本地坐标
        /// </summary>
        public RectangleF TransformRootToLocal(RectangleF pts)
        {
            var p1 = pts.Location;
            var p2 = pts.Location + pts.Size;
            p1 = TransformRootToLocal(p1);
            p2 = TransformRootToLocal(p2);
            return new RectangleF(p1, p2 - p1);
        }
        /// <summary>
        /// 本地坐标转换为屏幕坐标
        /// </summary>
        public RectangleF TransformLocalToRoot(RectangleF pts)
        {
            var p1 = pts.Location;
            var p2 = pts.Location + pts.Size;
            p1 = TransformLocalToRoot(p1);
            p2 = TransformLocalToRoot(p2);
            return new RectangleF(p1, p2 - p1);
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Children

        private readonly List<DisplayNode> children = new List<DisplayNode>();
        internal DisplayNode parent;
        internal DisplayRoot root;
        private void OnDisposeChildren()
        {
            RemoveAllChildren(true);
            parent = null;
            root = null;
        }

        public virtual IDisplayCanvas Canvas { get => root?.Canvas; }
        public DisplayNode Parent { get { return parent; } }
        public DisplayRoot Root { get { return root; } }
        public int NumChildren { get { return children.Count; } }
        public void SetParent(DisplayNode parent, bool worldStay = true)
        {
            var screenPos = this.Position;
            if (this.Parent != null)
            {
                if (worldStay)
                {
                    screenPos = Parent.TransformLocalToRoot(screenPos);
                }
                Parent.RemoveChild(this, false);
            }
            parent.AddChild(this);
            if (worldStay)
            {
                var local = parent.TransformRootToLocal(screenPos);
                this.Position = local;
            }
        }
        internal void InternalSetParent(DisplayNode node)
        {
            // check for a recursion
            DisplayNode ancestor = node;
            while (ancestor != this && ancestor != null)
            {
                ancestor = ancestor.parent;
            }
            if (ancestor == this)
            {
                throw new Exception("An object cannot be added as a child to itself or one of its children (or children's children, etc.)");
            }
            else
            {
                this.parent = node;
                this.root = parent?.root;
                ForEachChildren(root, static (root, c) =>
                {
                    c.root = root;
                }, true);
            }
        }
        internal void InternalAddChild(DisplayNode child, int index)
        {
            child.RemoveFromParent();
            child.InternalSetParent(this);
            children.Insert(index, child);
            InvokeAddChild(new ChildArgs(this, child));
        }
        internal void InternalRemoveChild(DisplayNode child, int index, bool dispose)
        {
            children.RemoveAt(index);
            if (dispose)
            {
                child.RemoveAllChildren(true);
                child.Dispose();
            }
            InvokeRemoveChild(new ChildArgs(this, child));
            child.parent = null;
            child.root = null;
        }
        internal void InvokeAddChild(ChildArgs args)
        {
            ChildAdded?.Invoke(this, args);
            if (parent != null && parent != this)
            {
                parent.InvokeRemoveChild(args);
            }
        }
        internal void InvokeRemoveChild(ChildArgs args)
        {
            ChildRemoved?.Invoke(this, args);
            if (parent != null && parent != this)
            {
                parent.InvokeRemoveChild(args);
            }
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
        public DisplayNode AddChildAt(DisplayNode child, int index)
        {
            if (child == null || index < 0)
            {
                //LogError("AddChildAt Error :Child can not be null or index < 0");
                return null;
            }
            if (child.Parent == this)
            {
                SetChildIndex(child, index);
            }
            else
            {
                InternalAddChild(child, index);
            }
            return child;
        }
        public DisplayNode AddChild(DisplayNode child)
        {
            return AddChildAt(child, NumChildren);
        }
        public T AddChild<T>(T child) where T : DisplayNode
        {
            return AddChildAt(child, NumChildren) as T;
        }

        public void RemoveChildByName(string name, bool dispose = true)
        {
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.Name.Equals(name))
                {
                    InternalRemoveChild(child, i, dispose);
                    break;
                }
            }
        }

        public void RemoveChild(DisplayNode child, bool dispose = true)
        {
            int result = children.IndexOf(child);
            if (result != -1)
            {
                RemoveChildAt(result, dispose);
            }
        }

        public void RemoveChildAt(int index, bool dispose = true)
        {
            if (index >= 0 && index < children.Count)
            {
                DisplayNode child = children[index];
                if (child.parent == this)
                {
                    InternalRemoveChild(child, index, dispose);
                }
            }
            else
            {
                throw new Exception("RemoveChild Error :: mChildren Out of Bounds");
            }
        }

        public void RemoveChildren(int beginIndex, int endIndex, bool dispose = true)
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

        public void RemoveFromParent(bool dispose = true)
        {
            if (parent != null)
            {
                parent.RemoveChild(this, dispose);
            }
        }

        public void SetChildIndex(DisplayNode child, int index)
        {
            int oldIndex = GetChildIndex(child);
            if (oldIndex == -1)
            {
                //LogError("SetChildIndex Error: oldIndex = -1");
                return;
            }
            //logic list.
            children.RemoveAt(oldIndex);
            if (index > children.Count)
            {
                index = children.Count;
            }
            children.Insert(index, child);
        }

        public int GetChildIndex(DisplayNode child)
        {
            if (child == null)
            {
                //LogError("UIBase GetChildIndex() child == null");
                return -1;
            }
            return children.IndexOf(child);
        }

        public DisplayNode GetChildAt(int index)
        {
            if (index >= 0 && index < NumChildren)
                return children[index];
            else
                throw new Exception("Invalid child index");
        }

        public void SwapChildren(DisplayNode child1, DisplayNode child2)
        {
            int index1 = GetChildIndex(child1);
            int index2 = GetChildIndex(child2);
            if (index1 == -1 || index2 == -1)
                throw new Exception("Not a child of this container");
            SwapChildrenAt(index1, index2);
        }

        public void SwapChildrenAt(int index1, int index2)
        {
            DisplayNode child1 = GetChildAt(index1);
            DisplayNode child2 = GetChildAt(index2);
            if (child1 != null && child2 != null)
            {
                children[index1] = child2;
                children[index2] = child1;
            }
        }

        public bool BringToFront()
        {
            if (parent != null)
            {
                if (parent.children.Remove(this))
                {
                    parent.children.Add(this);
                    return true;
                }
            }
            return false;
        }
        public bool SendToBack()
        {
            if (parent != null)
            {
                if (parent.children.Remove(this))
                {
                    parent.children.Insert(0, this);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取所有节点
        /// </summary>
        /// <param name="list"></param>
        /// <param name="recursion"></param>
        public void GetChildren(List<DisplayNode> list, bool recursion = false)
        {
            list.AddRange(children);
            if (recursion)
            {
                foreach (var c in children)
                {
                    c.GetChildren(list, recursion);
                }
            }
        }

        /// <summary>
        /// 获取场景数路径，从当前节点一直到根节点
        /// </summary>
        public void SceneGraphTreePath(List<DisplayNode> ret)
        {
            var curnode = this;
            do
            {
                ret.Insert(0, curnode);
                curnode = curnode.Parent;
            }
            while (curnode != null);
        }


        /// <summary>
        /// 遍历所有节点
        /// </summary>
        /// <param name="action">return true for break</param>
        /// <param name="recursion"></param>
        public bool ForEachChildren<ST>(ST st, ForEachPredicate<ST, DisplayNode> action, bool recursion = false)
        {
            if (children.Count > 0)
            {
                using (var list = ObjectPool.AllocList<DisplayNode>())
                {
                    list.AddRange(children);
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (action(st, list[i])) { return true; }
                    }
                    if (recursion)
                    {
                        for (int i = 0; i < list.Count; ++i)
                        {
                            if (list[i].ForEachChildren(st, action, recursion))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public void ForEachChildren<ST>(ST st, ForEachAction<ST, DisplayNode> action, bool recursion = false)
        {
            if (children.Count > 0)
            {
                using (var list = ObjectPool.AllocList<DisplayNode>())
                {
                    list.AddRange(children);
                    for (int i = 0; i < list.Count; ++i)
                    {
                        action(st, list[i]);
                    }
                    if (recursion)
                    {
                        for (int i = 0; i < list.Count; ++i)
                        {
                            list[i].ForEachChildren(st, action, recursion);
                        }
                    }
                }
            }
        }
        public DisplayNode FindNodeByName(string name)
        {
            int i;
            int length = NumChildren;
            // 广度遍历.
            for (i = length - 1; i >= 0; --i)
            {
                var child = this.GetChildAt(i);
                if (child.Name == name)
                {
                    return child;
                }
            }
            // 深度遍历.
            for (i = length - 1; i >= 0; --i)
            {
                var child = this.GetChildAt(i);
                var uicc = child.FindNodeByName(name);
                if (uicc != null)
                {
                    return uicc;
                }
            }
            return null;
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Visit
        public bool IsVisible { get; set; } = true;
        internal void InternalUpdate(UpdateArgs args)
        {
            this.UpdateComponents();
            this.DoUpdate(args);
            this.UpdateChilds(args);
        }
        protected virtual void UpdateChilds(UpdateArgs args)
        {
            if (children.Count > 0)
            {
                using (var list = ObjectPool.AllocList<DisplayNode>(children))
                {
                    foreach (var child in list)
                    {
                        child.InternalUpdate(args);
                    }
                }
            }
        }
        protected virtual void ApplyTransform(GraphicsArgs args)
        {
            var g = args.Graphics;
            this.transform.Apply(g);
        }
        internal void InternalVisit(GraphicsArgs args)
        {
            if (IsVisible)
            {
                var g = args.Graphics;
                g.PushTransform();
                try
                {
                    this.ApplyTransform(args);
                    this.DoDrawBegin(args);
                    try
                    {
                        this.VisitChilds(args);
                    }
                    finally
                    {
                        this.DoDrawAfter(args);
                    }
                }
                finally
                {
                    g.PopTransform();
                }
            }
        }
        protected virtual void VisitChilds(GraphicsArgs args)
        {
            if (children.Count > 0)
            {
                using (var list = ObjectPool.AllocList<DisplayNode>(children))
                {
                    foreach (var child in list)
                    {
                        child.InternalVisit(args);
                    }
                }
            }
        }
        internal void InternalVisitHUD(GraphicsArgs args)
        {
            if (IsVisible)
            {
                var g = args.Graphics;
                g.PushTransform();
                try
                {
                    this.transform.Apply(g);
                    this.DoDrawHUD(args);
                    this.VisitChildsHUD(args);
                }
                finally
                {
                    g.PopTransform();
                }
            }
        }
        protected virtual void VisitChildsHUD(GraphicsArgs args)
        {
            if (children.Count > 0)
            {
                using (var list = ObjectPool.AllocList<DisplayNode>(children))
                {
                    foreach (var child in list)
                    {
                        child.InternalVisitHUD(args);
                    }
                }
            }
        }
        internal DisplayNode InternalHitTest(Vector2 point, Predicate<DisplayNode> select)
        {
            if (children.Count > 0)
            {
                using (var list = ObjectPool.AllocList<DisplayNode>(children))
                {
                    for (int i = list.Count - 1; i >= 0; --i)
                    {
                        var child = list[i];
                        var local = child.TransformParentToLocal(point);
                        var hit = child.InternalHitTest(local, select);
                        if (hit != null)
                        {
                            return hit;
                        }
                    }
                }
            }
            if (select == null || select.Invoke(this))
            {
                if (DoHitTest(in point))
                {
                    return this;
                }
            }
            return null;
        }
        private void DoUpdate(UpdateArgs args)
        {
            OnUpdate(args);
            Update?.Invoke(this, args);
        }

        private void DoDrawHUD(GraphicsArgs args)
        {
            OnDrawHUD(args);
            DrawHUD?.Invoke(this, args);
        }
        private void DoDrawBegin(GraphicsArgs args)
        {
            OnDrawBegin(args);
            DrawBegin?.Invoke(this, args);
        }
        private void DoDrawAfter(GraphicsArgs args)
        {
            OnDrawAfter(args);
            DrawAfter?.Invoke(this, args);
        }
        private bool DoHitTest(in Vector2 localPoint)
        {
            if (OnHitTest(in localPoint))
            {
                return true;
            }
            if (HitTest != null)
            {
                return HitTest.Invoke(this, in localPoint);
            }
            return false;
        }

        protected virtual void OnUpdate(UpdateArgs args) { }
        protected virtual void OnDrawHUD(GraphicsArgs args) { }
        protected virtual void OnDrawBegin(GraphicsArgs args) { }
        protected virtual void OnDrawAfter(GraphicsArgs args) { }
        protected virtual bool OnHitTest(in Vector2 localPoint) { return false; }
        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Events

        public event UpdateHandler Update;
        public event DrawHandler DrawHUD;
        public event DrawHandler DrawBegin;
        public event DrawHandler DrawAfter;
        public event HitTestHandler HitTest;
        public event ChildEventHandler ChildAdded;
        public event ChildEventHandler ChildRemoved;
        private void OnDisposeEvents()
        {
            this.Update = null;
            this.DrawHUD = null;
            this.DrawBegin = null;
            this.DrawAfter = null;
            this.HitTest = null;
            this.ChildAdded = null;
            this.ChildRemoved = null;
        }
        public delegate void UpdateHandler(DisplayNode sender, UpdateArgs intervalSEC);
        public delegate void DrawHandler(DisplayNode sender, GraphicsArgs args);
        public delegate bool HitTestHandler(DisplayNode sender, in Vector2 localPoint);
        public delegate void ChildEventHandler(DisplayNode sender, ChildArgs args);
        #endregion
    }


}
