using DeepCore.Geometry;
using System;
using System.Collections.Generic;

namespace DeepCore.GUI.Display.Node
{
    public class DisplayNode : IDisposable
    {
        public static TypeAllocRecorder Alloc { get; private set; } = new TypeAllocRecorder(typeof(DisplayNode));

        private Matrix mTransform;

        private float mLongTouchTime = -1f;
        private float mAlpha = 1.0f;
        private bool mDisable = false;
        private bool mVisible = true;
        private bool mTouchStopHere = false;
        private bool mTouchPassHere = false;
        private bool mEnable = true;
        private bool mEnableChildren = true;
        private bool mIsDispose = false;
        private DisplayNode mParent = null;
        private DisplayStage mBase = null;
        private string mName = null;
        private RectangleF? mScrollRect;
        private RectangleF? mBounds;
        private bool mPopUp = false;

        private readonly List<DisplayNode> mRenderList = new List<DisplayNode>();
        private readonly List<DisplayNode> mChildrenList = new List<DisplayNode>();
        private readonly Queue<NodeEvent> mEvents = new Queue<NodeEvent>();
        /// <summary>
        /// mChildren正在递归
        /// </summary>
        private bool mOnChildrenRecursion = false;
        private bool mInRemoveProcess = false;
        private Vector2? mTouchPoint = null;
        //-------------------------------------------------------------------------------------------------------
        public virtual float X
        {
            get { return mTransform.Translation.X; }
            set { mTransform.Translation = new Vector3(value, Y, 0); }
        }
        public virtual float Y
        {
            get { return mTransform.Translation.Y; }
            set { mTransform.Translation = new Vector3(X, value, 0); }
        }
        public float ScaleX
        {
            get { return mTransform.Scale.X; }
            set { mTransform.Scale = new Vector3(value, ScaleY, 1); }
        }
        public float ScaleY
        {
            get { return mTransform.Scale.Y; }
            set { mTransform.Scale = new Vector3(ScaleX, value, 1); }
        }
        public Vector3 Position
        {
            get { return mTransform.Translation; }
            set { mTransform.Translation = value; }
        }
        public Vector3 Scale
        {
            get { return mTransform.Scale; }
            set { mTransform.Scale = value; }
        }
        //-------------------------------------------------------------------------------------------------------
        public bool Visible
        {
            get { return mVisible; }
            set { mVisible = value; }
        }
        public virtual bool Disable
        {
            get { return mDisable; }
            set { mDisable = value; }
        }
        public bool TouchStopHere
        {
            get { return mTouchStopHere; }
            set { mTouchStopHere = value; }
        }
        public bool TouchPassHere
        {
            get { return mTouchPassHere; }
            set { mTouchPassHere = value; }
        }
        public bool Enable
        {
            get { return mEnable; }
            set { mEnable = value; }
        }
        public bool EnableChildren
        {
            get { return mEnableChildren; }
            set { mEnableChildren = value; }
        }
        public bool IsDispose
        {
            get { return mIsDispose; }
        }
        public string Name
        {
            set { mName = value; }
            get { return mName; }
        }
        public int NumChildren
        {
            get { return mChildrenList.Count; }
        }
        public virtual RectangleF? ScrollRect
        {
            set { mScrollRect = value; }
            get { return mScrollRect; }
        }
        public virtual RectangleF? Bounds
        {
            get { return mBounds; }
            set { mBounds = value; }
        }
        public float LongTouchTime
        {
            get { return mLongTouchTime; }
            set { mLongTouchTime = value; }
        }
        public bool PopUp
        {
            get { return mPopUp; }
            set { mPopUp = value; }
        }
        public float DeltaTimeSec
        {
            get; private set;
        }
        //-------------------------------------------------------------------------------------------------------

        public DisplayNode(string name = "")
        {
            Alloc.RecordConstructor(this.GetType());
            this.Name = string.IsNullOrEmpty(name) ? this.ToString() : name;
            this.mTransform = Matrix.Identity;
        }
#if DEBUG
        ~DisplayNode()
        {
            Alloc.RecordDestructor(this.GetType());
        }
#endif
        public void Dispose()
        {
            if (mIsDispose)
            {
                return;
            }
            this.Disposing();
            mIsDispose = true;
            Alloc.RecordDispose(this.GetType());
        }
        public virtual DisplayNode Clone()
        {
            return null;
        }

        protected virtual void Disposing()
        {
            event_OnTouchClick = null;
            event_OnTouchBegin = null;
            event_OnTouchEnd = null;
            event_OnTouchMove = null;
            event_OnTouchOut = null;
            event_OnLongTouch = null;
            mParent = null;
            mBase = null;
            mBounds = null;
            mScrollRect = null;
            mTouchPoint = null;
            if (mRenderList != null)
            {
                for (int i = mRenderList.Count - 1; i >= 0; --i)
                {
                    mRenderList[i].Dispose();
                }
                mRenderList.Clear();
            }
            if (mChildrenList != null)
            {
                mChildrenList.Clear();
            }
            if (mEvents != null)
            {
                mEvents.Clear();
            }
        }

        public float Alpha
        {
            get { return mAlpha; }
            set { mAlpha = value < 0.0f ? 0.0f : (value > 1.0f ? 1.0f : value); }
        }

        #region Container

        struct NodeEvent
        {
            public const byte TYPE_ADD = 1;
            public const byte TYPE_REMOVE = 2;
            public const byte TYPE_MOVE = 3;
            public const byte TYPE_SWAP = 4;
            public byte Type;
            public int Index;
            public int Index2;
            public DisplayNode Node;
            public bool Dispose;

            public static NodeEvent AddChild(DisplayNode child)
            {
                NodeEvent ret = new NodeEvent();
                ret.Node = child;
                ret.Type = TYPE_ADD;
                return ret;
            }
            public static NodeEvent MoveChild(DisplayNode child, int index)
            {
                NodeEvent ret = new NodeEvent();
                ret.Node = child;
                ret.Index = index;
                ret.Type = TYPE_MOVE;
                return ret;
            }
            public static NodeEvent SwapChild(int index, int index2)
            {
                NodeEvent ret = new NodeEvent();
                ret.Index = index;
                ret.Index2 = index2;
                ret.Type = TYPE_SWAP;
                return ret;
            }
            public static NodeEvent RemoveChild(DisplayNode child, bool dispose)
            {
                NodeEvent ret = new NodeEvent();
                ret.Node = child;
                ret.Type = TYPE_REMOVE;
                ret.Dispose = dispose;
                child.mInRemoveProcess = true;
                return ret;
            }
        }

        internal void ProcessEvents()
        {
            while (mEvents.Count > 0)
            {
                NodeEvent ne = mEvents.Dequeue();
                switch (ne.Type)
                {
                    case NodeEvent.TYPE_ADD:
                        {
                            mRenderList.Add(ne.Node);
                        }
                        break;
                    case NodeEvent.TYPE_MOVE:
                        {
                            mRenderList.Remove(ne.Node);
                            mRenderList.Insert(Math.Min(ne.Index, mRenderList.Count), ne.Node);
                        }
                        break;
                    case NodeEvent.TYPE_SWAP:
                        {
                            DisplayNode t = mRenderList[ne.Index];
                            mRenderList[ne.Index] = mRenderList[ne.Index2];
                            mRenderList[ne.Index2] = t;
                        }
                        break;
                    case NodeEvent.TYPE_REMOVE:
                        {
                            mRenderList.Remove(ne.Node);
                            ne.Node.mInRemoveProcess = false;
                            if (ne.Dispose)
                            {
                                ne.Node.Dispose();
                            }
                        }
                        break;
                }
            }
        }
        internal void QueueAddChild(DisplayNode child)
        {
            mEvents.Enqueue(NodeEvent.AddChild(child));
            if (!mOnChildrenRecursion)
                ProcessEvents();

        }
        internal void QueueMoveChild(DisplayNode child, int index)
        {
            mEvents.Enqueue(NodeEvent.MoveChild(child, index));
            if (!mOnChildrenRecursion)
                ProcessEvents();
        }
        internal void QueueSwapChild(int index, int index2)
        {
            mEvents.Enqueue(NodeEvent.SwapChild(index, index2));
            if (!mOnChildrenRecursion)
                ProcessEvents();
        }
        internal void QueueRemoveChild(DisplayNode child, bool dispose)
        {
            mEvents.Enqueue(NodeEvent.RemoveChild(child, dispose));
            if (!mOnChildrenRecursion)
                ProcessEvents();
        }

        public DisplayNode Parent
        {
            get { return mParent; }
        }
        internal virtual DisplayStage Root
        {
            get { return mBase; }
            set { mBase = value; }
        }

        internal void SetParent(DisplayNode node)
        {
            // check for a recursion
            DisplayNode ancestor = node;
            while (ancestor != this && ancestor != null)
                ancestor = ancestor.mParent;

            if (ancestor == this)
                throw new Exception("An object cannot be added as a child to itself or one " +
                                        "of its children (or children's children, etc.)");
            else
                mParent = node;
            mBase = node.Root;
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

        public virtual void AddChild(DisplayNode child)
        {
            AddChildAt(child, NumChildren);
        }

        public virtual void AddChildAt(DisplayNode child, int index)
        {
            if (child == null || index < 0)
            {
                //LogError("AddChildAt Error :Child can not be null or index < 0");
                return;
            }

            if (child.Parent == this)
            {
                SetChildIndex(child, index);
            }

            else
            {
                child.RemoveFromParent();
                child.SetParent(this);
                mChildrenList.Insert(index, child);
                QueueAddChild(child);
                QueueMoveChild(child, index);
            }
        }

        public virtual void SetChildIndex(DisplayNode child, int index)
        {
            int oldIndex = GetChildIndex(child);
            if (oldIndex == -1)
            {
                //LogError("SetChildIndex Error: oldIndex = -1");
                return;
            }

            //logic list.
            mChildrenList.RemoveAt(oldIndex);
            if (index > mChildrenList.Count)
            {
                index = mChildrenList.Count;
            }
            mChildrenList.Insert(index, child);
            //render list.
            QueueMoveChild(child, index);
        }

        public virtual int GetChildIndex(DisplayNode child)
        {
            if (child == null)
            {
                //LogError("UIBase GetChildIndex() child == null");
                return -1;
            }

            return mChildrenList.IndexOf(child);
        }

        public void RemoveChild(string name, bool dispose = false)
        {
            foreach (DisplayNode child in mChildrenList)
            {
                if (child.Name.Equals(name))
                {
                    mChildrenList.Remove(child);
                    child.mParent = null;
                    child.Root = null;
                    QueueRemoveChild(child, dispose);
                    break;
                }
            }
        }

        public void RemoveChild(DisplayNode child, bool dispose)
        {
            int result = mChildrenList.IndexOf(child);
            if (result != -1)
            {
                RemoveChildAt(result, dispose);
            }
        }

        public void RemoveChildAt(int index, bool dispose)
        {
            if (index >= 0 && index < mChildrenList.Count)
            {
                DisplayNode child = mChildrenList[index];
                if (child.mParent == this)
                {
                    child.mParent = null;
                    child.Root = null;
                    mChildrenList.RemoveAt(index);
                    QueueRemoveChild(child, dispose);
                }
            }
            else
            {
                throw new Exception("RemoveChild Error :: mChildren Out of Bounds");
            }
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
            }
        }


        /** Returns a child object at a certain index. */
        public DisplayNode GetChildAt(int index)
        {
            if (index >= 0 && index < NumChildren)
                return mChildrenList[index];
            else
                throw new Exception("Invalid child index");
        }

        public IEnumerable<DisplayNode> GetChildren()
        {
            return new List<DisplayNode>(mChildrenList);
        }


        /// <summary>
        /// Swaps the indexes of two children.
        /// </summary>
        /// <param name="child1"></param>
        /// <param name="child2"></param>
        public void SwapChildren(DisplayNode child1, DisplayNode child2)
        {
            int index1 = GetChildIndex(child1);
            int index2 = GetChildIndex(child2);
            if (index1 == -1 || index2 == -1)
                throw new Exception("Not a child of this container");
            SwapChildrenAt(index1, index2);
        }

        /// <summary>
        /// Swaps the indexes of two children.
        /// </summary>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        public void SwapChildrenAt(int index1, int index2)
        {
            DisplayNode child1 = GetChildAt(index1);
            DisplayNode child2 = GetChildAt(index2);

            if (child1 != null && child2 != null)
            {
                mChildrenList[index1] = child2;
                mChildrenList[index2] = child1;
                QueueSwapChild(index1, index2);
            }
        }

        /// <summary>
        /// The topmost object in the display tree the object is part of.
        /// </summary>
        /// <returns></returns>
        public DisplayNode FindBase()
        {
            DisplayNode currentObject = this;
            while (currentObject.mParent != null)
                currentObject = currentObject.mParent;
            return currentObject;
        }
        public virtual void Update(float delatTime)
        {
            DeltaTimeSec = delatTime;
            mOnChildrenRecursion = true;
            int length = mRenderList.Count;
            for (int i = 0; i < length; i++)
            {
                mRenderList[i].Update(delatTime);
            }
            mOnChildrenRecursion = false;
            ProcessEvents();
        }


        public DisplayNode FindChildByName(string name)
        {
            DisplayNode child = null;
            DisplayNode uicc = null;
            int i;

            int length = NumChildren;

            for (i = length - 1; i >= 0; --i)
            {
                child = this.GetChildAt(i);

                if (child.Name == name)
                {
                    return child;
                }
            }

            for (i = length - 1; i >= 0; --i)
            {
                child = this.GetChildAt(i);
                uicc = child.FindChildByName(name);
                if (uicc != null)
                {
                    return uicc;
                }
            }

            return null;
        }

        #endregion

        #region Render

        private float curAlpha = 0;
        private Blend currentBlend;
        private DisplayNode child = null;
        private int mRenderListLen = 0;
        private int mRenderListIndex = 0;
        /// <summary>
        /// Indicates if an object occupies any visible area. (Which is the case when its 'alpha', 
        /// 'scaleX' and 'scaleY' values are not zero, and its 'visible' property is enabled.).
        /// </summary>
        /// <returns></returns>
        public bool HasVisibleArea()
        {
            return mAlpha != 0.0f && mVisible && this.ScaleX != 0.0f && this.ScaleY != 0.0f;
        }
        /// <summary>
        /// render
        /// </summary>
        /// <param name="g"></param>
        public virtual void Visit(Graphics g)
        {
            if (mInRemoveProcess)
            {
                return;
            }
            //alpha.
            curAlpha = g.CurrentAlpha;
            if (mAlpha != 1f)
            {
                g.SetAlpha(mAlpha * curAlpha);
            }
            //gray.
            currentBlend = g.CurrentBlend;
            if (mDisable)
            {
                RenderDisable(g);
            }
            {
                Draw(g);
                mOnChildrenRecursion = true;
                child = null;
                mRenderListLen = mRenderList.Count;
                for (mRenderListIndex = 0; mRenderListIndex < mRenderListLen; mRenderListIndex++)
                {
                    child = mRenderList[mRenderListIndex];
                    if (child.HasVisibleArea() == false)
                    {
                        continue;
                    }
                    g.PushTransform();
                    g.MultiplyTransform(child.mTransform);
                    child.Visit(g);
                    g.PopTransform();
                }
                mOnChildrenRecursion = false;
                DrawAfter(g);
            }
            g.SetBlend(currentBlend);
            g.SetAlpha(curAlpha);
        }
        /// <summary>
        /// 子类继承此方法用于渲染节点.
        /// </summary>
        /// <param name="g"></param>
        virtual public void Draw(Graphics g) { }
        virtual public void DrawAfter(Graphics g) { }
        virtual public void RenderDisable(Graphics g) { g.SetBlend(Blend.BLEND_MODE_GRAY); }

        #endregion

        #region Event

        public delegate void DrawDebugHandler(Graphics g);

        public delegate void TouchClickEventHandler(DisplayNode sender);
        public delegate void TouchBeginEventHandler(DisplayNode sender);
        public delegate void TouchEndEventHandler(DisplayNode sender);
        public delegate void TouchMoveEventHandler(DisplayNode sender);
        public delegate void TouchOutEventHandler(DisplayNode sender);
        public delegate void LongTouchEventHandler(DisplayNode sender);

        internal TouchClickEventHandler event_OnTouchClick;
        internal TouchBeginEventHandler event_OnTouchBegin;
        internal TouchEndEventHandler event_OnTouchEnd;
        internal TouchMoveEventHandler event_OnTouchMove;
        internal TouchOutEventHandler event_OnTouchOut;
        internal LongTouchEventHandler event_OnLongTouch;

        public event TouchClickEventHandler OnTouchClick { add { event_OnTouchClick += value; } remove { event_OnTouchClick -= value; } }
        public event TouchBeginEventHandler OnTouchBegin { add { event_OnTouchBegin += value; } remove { event_OnTouchBegin -= value; } }
        public event TouchEndEventHandler OnTouchEnd { add { event_OnTouchEnd += value; } remove { event_OnTouchEnd -= value; } }
        public event TouchMoveEventHandler OnTouchMove { add { event_OnTouchMove += value; } remove { event_OnTouchMove -= value; } }
        public event TouchOutEventHandler OnTouchOut { add { event_OnTouchOut += value; } remove { event_OnTouchOut -= value; } }
        public event LongTouchEventHandler OnLongTouch { add { event_OnLongTouch += value; } remove { event_OnLongTouch -= value; } }


        public virtual void TouchBegin(NodeTouch touch)
        {
            if (event_OnTouchBegin != null)
            {
                event_OnTouchBegin(this);
            }
            else
            {
                if (mParent != null && PopUp)
                {
                    mParent.TouchBegin(touch);
                }
            }
        }

        public virtual void TouchEnd(NodeTouch touch)
        {
            if (event_OnTouchEnd != null)
            {
                event_OnTouchEnd(this);
            }
            else
            {

                if (mParent != null && PopUp)
                {
                    mParent.TouchEnd(touch);
                }
            }
        }

        public virtual void TouchMove(NodeTouch touch)
        {
            if (event_OnTouchMove != null)
            {
                event_OnTouchMove(this);
            }
            else
            {
                if (mParent != null && PopUp)
                {
                    mParent.TouchMove(touch);
                }
            }
        }

        public virtual void TouchOut(NodeTouch touch)
        {
            if (event_OnTouchOut != null)
            {
                event_OnTouchOut(this);
            }
            else
            {
                if (mParent != null && PopUp)
                {
                    mParent.TouchOut(touch);
                }
            }
        }

        public virtual void TouchClick(NodeTouch touch)
        {
            if (event_OnLongTouch != null)
            {
                event_OnLongTouch(this);
            }
            else
            {
                if (mParent != null && PopUp)
                {
                    mParent.TouchClick(touch);
                }
            }
        }

        public virtual void LongTouch(NodeTouch touch)
        {
            if (event_OnLongTouch != null)
            {
                event_OnLongTouch(this);
            }
        }

        protected virtual DisplayNode PushEvent(TouchEvent touchData, bool forTouch = true)
        {
            if (mEnable == false)
            {
                return null;
            }
            if (forTouch && !HasVisibleArea())
            {
                return null;
            }
            if (mEnableChildren && NumChildren > 0)
            {
                DisplayNode node = null;
                for (int i = NumChildren - 1; i >= 0; --i)
                {
                    node = mRenderList[i];


                    DisplayNode target = node.PushEvent(touchData, forTouch);

                    if (target != null)
                    {
                        return target;
                    }
                }
            }
            return HitTest(touchData.GlobalPos);
        }

        protected virtual DisplayNode HitTest(Vector2 globalPoint)
        {
            if (mTouchPassHere == true)
            {
                mTouchPoint = null;
                return null;
            }

            DisplayNode temp = null;

            if (mEnable == false || mBounds == null)
            {
                temp = null;
            }
            else
            {
                mTouchPoint = this.GlobalToLocal(globalPoint);
                if (mBounds.Value.Contains(mTouchPoint.Value) == true)
                {
                    temp = this;
                }
            }

            if (mTouchStopHere == true && temp == null)
            {
                mTouchPoint = null;
                temp = this;
            }

            if (temp == null) { mTouchPoint = null; }

            return temp;
        }

        public Vector2? GetTouchPoint()
        {
            return mTouchPoint;
        }

        #endregion

        #region Debug

        //         /// <summary>
        //         /// 获取全局引用计数.
        //         /// </summary>
        //         /// <returns></returns>
        //         public static int GetDisplayNodeReferenceCount()
        //         {
        //             return mReference;
        //         }
        // 
        //         public static string DumpAllDisplayNodeList()
        //         {
        //             if (!UseReferenceCheck)
        //                 return "UseReferenceCheck == false";
        // 
        //             string sout = string.Empty;
        // 
        //             for (int i = mReferenceList.Count - 1; i >= 0; --i)
        //             {
        //                 sout += ((DisplayNode)mReferenceList[i]).Name;
        //                 sout += "\r\n";
        //             }
        // 
        //             return sout;
        //         }
        // 
        //         public static List<DisplayNode> GetReferenceList()
        //         {
        //             if (UseReferenceCheck)
        //             {
        //                 return mReferenceList;
        //             }
        //             return null;
        // 
        //         }
        #endregion

        //-------------------------------------------------------------------------------------------

        #region TREE_AND_COORDINATE

        /// <summary>
        /// 获取场景数路径，从当前节点一直到根节点
        /// </summary>
        /// <returns>返回结果，最后一个是当前节点，首个是根节点</returns>
        public List<DisplayNode> SceneGraphTreePath()
        {
            List<DisplayNode> ret = new List<DisplayNode>();
            DisplayNode curnode = this;
            do
            {
                ret.Insert(0, curnode);
                curnode = curnode.Parent;
            }
            while (curnode != Root);
            return ret;
        }

        /// <summary>
        /// 屏幕坐标转换为本地坐标
        /// </summary>
        /// <param name="globalPoint"></param>
        /// <returns></returns>
        public Vector2? GlobalToLocal(Vector2 globalPoint)
        {
            if (this == Root)
            {
                return globalPoint;
            }
            else if (Parent != null)
            {
                List<DisplayNode> path = SceneGraphTreePath();
                foreach (DisplayNode node in path)
                {
                    globalPoint = node.ParentToLocal(globalPoint);
                }
                return globalPoint;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 本地坐标转换为屏幕坐标
        /// </summary>
        /// <param name="localPoint"></param>
        /// <returns></returns>
        public Vector2? LocalToGlobal(Vector2 localPoint)
        {
            if (this == Root)
            {
                return new Vector2(localPoint.X, localPoint.Y);
            }
            else if (Parent != null)
            {
                DisplayNode curnode = this;
                do
                {
                    localPoint = curnode.LocalToParent(localPoint);
                    curnode = curnode.Parent;
                }
                while (curnode != null);
                return localPoint;
            }
            else
            {
                return null;
            }
        }

        DisplayNode curnode = null;
        protected bool LocalToGlobal_S(ref Vector2 localPoint)
        {
            if (this == Root)
            {
                return true;
            }
            else if (Parent != null)
            {
                curnode = this;
                do
                {
                    curnode.LocalToParent_S(ref localPoint);
                    curnode = curnode.Parent;
                }
                while (curnode != null);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 本地坐标转换为父节点坐标
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector2 LocalToParent(Vector2 point)
        {
            return new Vector2(
                (point.X * this.ScaleX) + this.X,
                (point.Y * this.ScaleY) + this.Y);
        }

        protected void LocalToParent_S(ref Vector2 point)
        {
            point.X = (point.X * this.ScaleX) + this.X;
            point.Y = (point.Y * this.ScaleY) + this.Y;
        }

        /// <summary>
        /// 父节点坐标转换为本地坐标
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector2 ParentToLocal(Vector2 point)
        {
            return new Vector2(
                (point.X - this.X) / this.ScaleX,
                (point.Y - this.Y) / this.ScaleY);
        }

        /// <summary>
        /// 屏幕坐标转换为本地坐标
        /// </summary>
        /// <param name="globalPoint"></param>
        /// <returns></returns>
        public RectangleF? GlobalToLocal(Rectangle globalRect)
        {
            var p1 = GlobalToLocal(new Vector2(
                globalRect.x, globalRect.y));
            var p2 = GlobalToLocal(new Vector2(
                globalRect.x + globalRect.width,
                globalRect.y + globalRect.height));
            if (p1 != null)
            {
                return new RectangleF(p1.Value.X, p1.Value.Y, p2.Value.X - p1.Value.X, p2.Value.Y - p1.Value.Y);
            }
            return null;
        }

        /// <summary>
        /// 本地坐标转换为屏幕坐标
        /// </summary>
        /// <param name="localPoint"></param>
        /// <returns></returns>
        public RectangleF? LocalToGlobal(RectangleF localRect)
        {
            var p1 = LocalToGlobal(new Vector2(
                localRect.x, localRect.y));
            var p2 = LocalToGlobal(new Vector2(
                localRect.x + localRect.width,
                localRect.y + localRect.height));
            if (p1 != null)
            {
                return new RectangleF(p1.Value.X, p1.Value.Y, p2.Value.X - p1.Value.X, p2.Value.Y - p1.Value.Y);
            }
            return null;
        }

        protected RectangleF LocalToGlobal_S(RectangleF localRect)
        {
            var p1 = new Vector2(localRect.x, localRect.y);
            LocalToGlobal_S(ref p1);

            var p2 = new Vector2(localRect.x + localRect.width, localRect.y + localRect.height);
            LocalToGlobal_S(ref p2);

            return new RectangleF(p1.X, p1.Y, p2.X - p1.X, p2.Y - p1.Y);
        }

        /// <summary>
        /// 本地坐标转换为父节点坐标
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public RectangleF? LocalToParent(RectangleF rect)
        {
            var p1 = LocalToParent(new Vector2(
                 rect.x, rect.y));
            var p2 = LocalToParent(new Vector2(
                rect.x + rect.width,
                rect.y + rect.height));
            if (p1 != null)
            {
                return new RectangleF(p1.Value, p2.Value - p1.Value);
            }
            return null;
        }

        /// <summary>
        /// 父节点坐标转换为本地坐标
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public RectangleF? ParentToLocal(RectangleF rect)
        {
            var p1 = ParentToLocal(new Vector2(
                rect.x, rect.y));
            var p2 = ParentToLocal(new Vector2(
                rect.x + rect.width,
                rect.y + rect.height));
            if (p1 != null)
            {
                return new RectangleF(p1.Value, p2.Value - p1.Value);
            }
            return null;
        }

        /// <summary>
        /// 获取当前节点的世界坐标.
        /// </summary>
        /// <returns></returns>
        public virtual Vector2? GetGlobalPoint()
        {
            if (Parent != null)
            {
                return Parent.LocalToGlobal(new Vector2(this.X, this.Y));
            }

            return null;
        }

        #endregion

        //-------------------------------------------------------------------------------------------

    }

}
