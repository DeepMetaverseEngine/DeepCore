using DeepCore;
using DeepCore.Reflection;
using DeepCore.Unity3D.UGUIAction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{    //-----------------------------------------------------------------------------------------------------------------------

    public partial class ScrollRectInteractive : ScrollRect
    {
        private bool mStartDrag = false;

        private DisplayNode mBinding;
        private DisplayNode mContainer;
        public DisplayNode Binding { get { return mBinding; } }
        public DisplayNode Container { get { return mContainer; } }

        private bool mShowSlider = false;
        private bool mAutoUpdateContentSize = true;
        private DisplayNode mScrollH;
        private DisplayNode mScrollV;
        private float mScrollFadeTimeMaxMS = 600;
        private float mScrollFadeTimeMS = 0;

        public bool IsDragging
        {
            get { return mStartDrag; }
        }

        public bool ShowSlider
        {
            get { return mShowSlider; }
            set { mShowSlider = value; }
        }
        public float ScrollFadeTimeMaxMS
        {
            get { return mScrollFadeTimeMaxMS; }
            set { mScrollFadeTimeMaxMS = value; }
        }
        public Rect ScrollRect2D
        {
            get
            {
                Rect scroll_rect = mBinding.Bounds2D;
                Vector2 cpos = mContainer.Position2D;
                scroll_rect.position = new Vector2(-cpos.x, -cpos.y);
                return scroll_rect;
            }
        }
        public bool AutoUpdateContentSize
        {
            get { return mAutoUpdateContentSize; }
            set { mAutoUpdateContentSize = value; }
        }


        //------------------------------------------------------------------------

        public ScrollRectInteractive()
        {
            this.onValueChanged.AddListener(doScrolled);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scrollh">水平</param>
        /// <param name="scrollv">垂直</param>
        public void SetScrollBarPair(DisplayNode scrollh, DisplayNode scrollv)
        {
            if (mScrollH != scrollh && mScrollH != null)
            {
                mScrollH.RemoveFromParent(true);
            }
            if (mScrollV != scrollv && mScrollV != null)
            {
                mScrollV.RemoveFromParent(true);
            }
            this.mScrollH = scrollh;
            this.mScrollV = scrollv;
            if (mBinding != null && mScrollH != null)
            {
                mBinding.AddChild(mScrollH);
            }
            if (mBinding != null && mScrollV != null)
            {
                mBinding.AddChild(mScrollV);
            }
        }
        public bool IsInViewRect(ref Rect src, ref Rect dst)
        {
            if (src.xMax <= dst.xMin) return false;
            if (src.xMin >= dst.xMax) return false;
            if (src.yMax <= dst.yMin) return false;
            if (src.yMin >= dst.yMax) return false;
            return true;
        }
        public void LookAt(Vector2 pos, bool scroll = false)
        {
            this.StopMovement();
            Vector2 contentsize = mContainer.Size2D;
            Vector2 camerasize = mBinding.Size2D;
            pos.x = CMath.Clamp(pos.x, 0, contentsize.x - camerasize.x);
            pos.y = CMath.Clamp(pos.y, 0, contentsize.y - camerasize.y);

            if (scroll)
            {
                MoveAction ma = new MoveAction();
                ma.Duration = 1;
                ma.TargetX = -pos.x;
                ma.TargetY = -pos.y;
                mContainer.AddAction(ma);
            }
            else
            {
                mContainer.Position2D = -pos;
            }
        }

        //------------------------------------------------------------------------
        protected override void Awake()
        {
            this.mBinding = DisplayNode.AsDisplayNode(gameObject);
            this.mContainer = DisplayNode.AsDisplayNode(base.content.gameObject);
            if (mScrollH != null)
            {
                mBinding.AddChild(mScrollH);
            }
            if (mScrollV != null)
            {
                mBinding.AddChild(mScrollV);
            }
            base.Awake();
        }

        protected override void OnDestroy()
        {
            this.onValueChanged.RemoveAllListeners();
            this.event_OnEndDrag = null;
            this.event_Scrolled = null;
            this.event_OnBeginDrag = null;
            base.OnDestroy();
        }
        protected virtual void Update()
        {
            if (this.mAutoUpdateContentSize)
            {
                this.OnUpdateContentSize();
            }
            this.OnUpdateSlider();
        }
        protected virtual void OnUpdateContentSize()
        {
            Rect scrollRect = this.ScrollRect2D;
            Vector2 size = Vector2.zero;
            for (int i = mContainer.NumChildren - 1 ; i >= 0 ; --i)
            {
                var child = mContainer.GetChildAt(i);
                if (child != null)
                {
                    Rect cb = child.Bounds2D;
                    size.x = Math.Max(size.x, cb.x + cb.width);
                    size.y = Math.Max(size.y, cb.y + cb.height);
                    if (IsInViewRect(ref scrollRect, ref cb))
                    {
                        child.VisibleInParent = true;
                    }
                    else
                    {
                        child.VisibleInParent = false;
                    }
                }
            }
            mContainer.Size2D = size;
        }
        protected virtual void OnUpdateSlider()
        {
            if (mShowSlider)
            {
                float alpha = 1 - Math.Min(1, mScrollFadeTimeMS / mScrollFadeTimeMaxMS);
                mScrollFadeTimeMS += UnityEngine.Time.deltaTime * 1000f;
                Vector2 vsize = mBinding.Size2D;
                Rect container_bounds = this.Container.Bounds2D;
                Rect scroll_rect = this.ScrollRect2D;
                if (mScrollH != null)
                {
                    if (this.horizontal)
                    {
                        Vector2 psize = mScrollH.Size2D;
                        float th = psize.y;
                        float tw = vsize.x;
                        float pt1 = CMath.GetRate(scroll_rect.x, 0, container_bounds.width);
                        float pt2 = CMath.GetRate(scroll_rect.x + scroll_rect.width, 0, container_bounds.width);
                        pt1 = CMath.Clamp(pt1, 0, 1);
                        pt2 = CMath.Clamp(pt2, 0, 1);
                        float ptw = pt2 - pt1;
                        if (ptw > 0)
                        {
                            this.mScrollH.Alpha = alpha;
                            this.mScrollH.Visible = true;
                            this.mScrollH.Bounds2D = new Rect(
                                    (pt1 * tw),
                                    (vsize.y - th),
                                    (ptw * tw),
                                    (th));
                        }
                        else
                        {
                            this.mScrollH.Visible = false;
                        }
                    }
                    else
                    {
                        this.mScrollH.Visible = false;
                    }
                }
                if (mScrollV != null)
                {
                    if (this.vertical)
                    {
                        Vector2 psize = mScrollV.Size2D;
                        float tw = psize.x;
                        float th = vsize.y;
                        float pt1 = CMath.GetRate(scroll_rect.y, 0, container_bounds.height);
                        float pt2 = CMath.GetRate(scroll_rect.y + scroll_rect.height, 0, container_bounds.height);
                        pt1 = CMath.Clamp(pt1, 0, 1);
                        pt2 = CMath.Clamp(pt2, 0, 1);
                        float pth = pt2 - pt1;
                        if (pth > 0)
                        {
                            this.mScrollV.Alpha = alpha;
                            this.mScrollV.Visible = true;
                            this.mScrollV.Bounds2D = new Rect(
                                (vsize.x - tw),
                                (pt1 * th),
                                (tw),
                                (pth * th));
                        }
                        else
                        {
                            this.mScrollV.Visible = false;
                        }
                    }
                    else
                    {
                        this.mScrollV.Visible = false;
                    }
                }
            }
            else
            {
                if (this.mScrollV != null) this.mScrollV.Visible = false;
                if (this.mScrollH != null) this.mScrollH.Visible = false;
            }
        }

        public float StartDragDistance { get; set; }
        public float mStartMove { get; set; }
        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!mBinding.EnableTouchInParents)
            {
                mStartDrag = false;
                return;
            }

            if (StartDragDistance > 0)
            {
                if (eventData.pointerPress != gameObject)
                {
                    if ((vertical && Mathf.Abs(eventData.delta.y) < StartDragDistance) ||
                        (horizontal && Mathf.Abs(eventData.delta.x) < StartDragDistance))
                    {
                        mStartDrag = false;
                        mStartMove = vertical ?eventData.delta.y : eventData.delta.x;
//                        eventData.pointerDrag = null;
                        return;
                    }
                }
            }


            mStartDrag = true;
            base.OnBeginDrag(eventData);

            if (event_OnBeginDrag != null)
            {
                event_OnBeginDrag.Invoke(mBinding, eventData);
            }
        }
        public override void OnEndDrag(PointerEventData eventData)
        {
            mStartMove = 0;
            mStartDrag = false;
            if (!mBinding.EnableTouchInParents) return;
            base.OnEndDrag(eventData);
            if (event_OnEndDrag != null)
            {
                event_OnEndDrag.Invoke(mBinding, eventData);
            }
        }


        public override void OnDrag(PointerEventData eventData)
        {
            if (event_OnDrag != null)
            {
                event_OnDrag.Invoke(mBinding, eventData);
            }
            if (eventData.pointerPress != gameObject)
            {
                if (!mStartDrag)
                {
                    mStartMove += vertical ? eventData.delta.y : eventData.delta.x;
                    if ((vertical && Math.Abs(mStartMove) > StartDragDistance) ||
                        (horizontal && Math.Abs(mStartMove) > StartDragDistance))
                    {
                        if (vertical)
                        {
                           eventData.delta = new Vector2(eventData.delta.x,mStartMove);
                           eventData.position = new Vector2(eventData.position.x ,eventData.position.y- mStartMove);
                        }
                        else
                        {
                            eventData.delta = new Vector2(mStartMove,eventData.delta.y);
                            eventData.position = new Vector2(eventData.position.x - mStartMove,eventData.position.y);
                        }
                        
                        OnBeginDrag(eventData);
                        mStartMove = 0;
                    }
                }
            }

            if (mStartDrag)
            {
                base.OnDrag(eventData);
            }
        }

        //------------------------------------------------------------------------

        private void doScrolled(Vector2 vector)
        {
            mScrollFadeTimeMS = 0;
            if (event_Scrolled != null) { event_Scrolled.Invoke(vector); }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (event_OnPointerClickTarget != null)
                event_OnPointerClickTarget(eventData);
        }


        public delegate void OnPointClickHandler(PointerEventData eventData);

        public UnityEngine.Events.UnityAction<Vector2> event_Scrolled;
        public DisplayNode.PointerEventHandler event_OnEndDrag;
        public DisplayNode.PointerEventHandler event_OnBeginDrag;
        public DisplayNode.PointerEventHandler event_OnDrag;
        public OnPointClickHandler event_OnPointerClickTarget;

        public event UnityEngine.Events.UnityAction<Vector2> OnScrolled { add { event_Scrolled += value; } remove { event_Scrolled -= value; } }
        public event DisplayNode.PointerEventHandler OnEndDragEvent { add { event_OnEndDrag += value; } remove { event_OnEndDrag -= value; } }
        public event DisplayNode.PointerEventHandler OnBeginDragEvent { add { event_OnBeginDrag += value; } remove { event_OnBeginDrag -= value; } }
        public event DisplayNode.PointerEventHandler OnDragEvent { add { event_OnDrag += value; } remove { event_OnDrag -= value; } }


    }



}
