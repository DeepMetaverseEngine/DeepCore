using DeepCore.Components;
using DeepCore.Geometry;
using DeepCore.GUI.Display;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.Input;
using System;

namespace DeepCore.GUI.SceneGraph
{

    public class InteractiveComponent : DisplayNodeComponent
    {
        public static Color COLOR_SELECT { get; set; } = Color.Yellow;
        public static Color COLOR_PICK { get; set; } = Color.White;

        public bool IsPickable { get; set; } = true;
        public bool IsSelectable { get; set; } = true;
        public bool IsDragMoveable { get; set; } = true;

        public InteractiveComponent()
        {
        }
        protected override void OnDispose(DisplayNode owner)
        {
            this.CleanEvents();
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            this.Owner.DrawHUD += Owner_DrawHUD;
            this.Owner.HitTest += Owner_HitTest;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            this.Owner.DrawHUD -= Owner_DrawHUD;
            this.Owner.HitTest -= Owner_HitTest;
        }


        private bool is_selected = false;
        private bool is_picked = false;
        private bool is_dragMoving = false;

        public bool IsSelected
        {
            get => is_selected;
            internal set
            {
                if (value != is_selected)
                {
                    is_selected = value;
                    SelectedChanged?.Invoke(this, value);
                }
            }
        }
        public bool IsPicked
        {
            get => is_picked;
            internal set
            {
                if (value != is_picked)
                {
                    is_picked = value;
                    PickChanged?.Invoke(this, value);
                }
            }
        }
        public bool IsDragMoving
        {
            get => is_dragMoving;
            internal set
            {
                if (value != is_dragMoving)
                {
                    is_dragMoving = value;
                    DragMoveChanged?.Invoke(this, value);
                }
            }
        }
        public bool IsMouseDown
        {
            get
            {
                if (Owner?.root?.LastPressedNode == this)
                {
                    if (Owner.root.Canvas.MouseButtons == MouseButton.Left)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        //----------------------------------------------------------------------------------------------------------

        public delegate void SelectionChangedHandler(InteractiveComponent sender, bool select);
        public delegate void MouseHandler(InteractiveComponent sender, MouseArgs args);
        public delegate void DragHandler(InteractiveComponent sender, DragArgs args);

        public event SelectionChangedHandler SelectedChanged;
        public event SelectionChangedHandler PickChanged;
        public event SelectionChangedHandler DragMoveChanged;

        public event MouseHandler MouseDown;
        public event MouseHandler MouseUp;
        public event MouseHandler MouseMove;
        public event MouseHandler MouseClick;
        public event MouseHandler MouseWheel;
        public event DragHandler MouseStartDrag;
        public event DragHandler MouseDragging;
        public event DragHandler MouseEndDrag;
        protected virtual void CleanEvents()
        {
            SelectedChanged = null;
            PickChanged = null;
            DragMoveChanged = null;
            MouseDown = null;
            MouseUp = null;
            MouseMove = null;
            MouseStartDrag = null;
            MouseClick = null;
            MouseWheel = null;
        }
        //----------------------------------------------------------------------------------------------------------
        private bool Owner_HitTest(DisplayNode sender, in Vector2 localPoint)
        {
            if (Enable)
            {
                if (IsPickable)
                {
                    return HitTest(in localPoint);
                }
            }
            return false;
        }
        private void Owner_DrawHUD(DisplayNode sender, GraphicsArgs args)
        {
            if (Enable)
            {
                if (IsPicked)
                {
                    DrawPickedHUD(args);
                }
                if (IsSelected)
                {
                    DrawSelectedHUD(args);
                }
            }
        }
        internal void Canvas_MouseDown(MouseArgs obj)
        {
            if (Enable) MouseDown?.Invoke(this, obj);
        }
        internal void Canvas_MouseMove(MouseArgs obj)
        {
            if (Enable) MouseMove?.Invoke(this, obj);
        }
        internal void Canvas_MouseUp(MouseArgs obj)
        {
            if (Enable) MouseUp?.Invoke(this, obj);
        }
        internal void Canvas_MouseClick(MouseArgs obj)
        {
            if (Enable) MouseClick?.Invoke(this, obj);
        }
        internal void Canvas_MouseWheel(MouseArgs obj)
        {
            if (Enable) MouseWheel?.Invoke(this, obj);
        }
        internal void Canvas_StartDragging(DragArgs obj)
        {
            if (Enable) MouseStartDrag?.Invoke(this, obj);
        }
        internal void Canvas_EndDragging(DragArgs obj)
        {
            if (Enable) MouseEndDrag?.Invoke(this, obj);
        }
        internal void Canvas_Dragging(DragArgs obj)
        {
            if (Enable) MouseDragging?.Invoke(this, obj);
        }
        internal void Canvas_ResizeFromRoot(Vector2 pos, RectangleF? bounds)
        {
            ResizeFromRoot(pos, bounds);
        }
        //----------------------------------------------------------------------------------------------------------

        protected virtual bool HitTest(in Vector2 localPoint)
        {
            return CMath.IncludeRectPointW(-4, -4, 8, 8, localPoint.X, localPoint.Y);
        }
        protected virtual void DrawPickedHUD(GraphicsArgs args)
        {
            args.Graphics.SetColor(COLOR_PICK);
            args.Graphics.DrawArc(-4, -4, 8, 8, 0, 360);
        }
        protected virtual void DrawSelectedHUD(GraphicsArgs args)
        {
            //pen_select.Width = 2;
            //pen_select.StartCap = LineCap.Round;
            //pen_select.DashStyle = System.Drawing.Drawing2D.DashStyle.
            args.Graphics.SetColor(COLOR_SELECT);
            args.Graphics.DrawArc(-4, -4, 8, 8, 0, 360);
        }
        protected virtual void ResizeFromRoot(Vector2 pos, RectangleF? bounds)
        {


        }

    }

    public class RectInteracviteComponent : InteractiveComponent
    {
        public bool IsDragResizeable { get; set; } = true;
        /// <summary>
        /// 本地坐标系
        /// </summary>
        public DeepCore.Geometry.RectangleF Bounds { get; set; }
        /// <summary>
        /// 相对根节点坐标系
        /// </summary>
        public DeepCore.Geometry.RectangleF RootBounds
        {
            get
            {
                var bounds = this.Bounds;
                var a = new Vector2(bounds.X, bounds.Y);
                var b = new Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height);
                a = Owner.TransformLocalToRoot(a);
                b = Owner.TransformLocalToRoot(b);
                return new DeepCore.Geometry.RectangleF(a, b - a);
            }
        }
        /// <summary>
        /// 相对父节点的坐标系
        /// </summary>
        public RectangleF ParentBounds
        {
            get
            {
                if (Owner.parent is UEContainerNode parentUI)
                {
                    var size = parentUI.Rect.Bounds;
                    return new RectangleF(0, 0, size.Width, size.Height);
                }
                if (Owner.root != null)
                {
                    var size = Owner.Canvas.Size;
                    return new RectangleF(0, 0, size.X, size.Y);
                }
                return this.Bounds;
            }
        }
        public RectInteracviteComponent(float x, float y, float w, float h)
        {
            this.Bounds = new DeepCore.Geometry.RectangleF(x, y, w, h);
        }
        public RectInteracviteComponent()
        {
            this.Bounds = new DeepCore.Geometry.RectangleF(0, 0, 100, 100);
        }
        public RectInteracviteComponent(RectangleF bounds)
        {
            this.Bounds = bounds;
        }
        protected override void ResizeFromRoot(Vector2 pos, RectangleF? bounds)
        {
            Owner.Position = pos;
            var size = this.Bounds.Size;
            if (bounds != null)
            {
                size = bounds.Value.Size;
                this.Bounds = new RectangleF(Vector2.Zero, size);
            }
            var pbounds = ParentBounds;
            //this.Margin = new Padding(pos.X, pos.Y, pbounds.Width - size.X, pbounds.Height - size.Y);
        }
        protected override bool HitTest(in Vector2 localPoint)
        {
            var bounds = this.Bounds;
            return CMath.IncludeRectPointW(bounds.X, bounds.Y, bounds.Width, bounds.Height, localPoint.X, localPoint.Y);
        }
        protected override void DrawPickedHUD(GraphicsArgs args)
        {
            var bounds = this.Bounds;
            args.Graphics.SetColor(COLOR_PICK);
            args.Graphics.DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
        protected override void DrawSelectedHUD(GraphicsArgs args)
        {
            var bounds = this.Bounds;
            if (IsDragResizeable)
            {
                var rw = DisplayRoot.DRAG_RESIZE_W;
                var dw = rw / 2;
                args.Graphics.SetColor(COLOR_SELECT);
                if (IsDragMoving && Owner.Root.DraggingVirtualBounds.HasValue)
                {
                    bounds = Owner.TransformRootToLocal(Owner.Root.DraggingVirtualBounds.Value.Bounds);
                }
                args.Graphics.DrawRect(bounds.X + dw, bounds.Y + dw, bounds.Width - rw, bounds.Height - rw);
                args.Graphics.FillRect(bounds.X, bounds.Y, rw, rw);
                args.Graphics.FillRect(bounds.X + bounds.Width - rw, bounds.Y, rw, rw);
                args.Graphics.FillRect(bounds.X, bounds.Y + bounds.Height - rw, rw, rw);
                args.Graphics.FillRect(bounds.X + bounds.Width - rw, bounds.Y + bounds.Height - rw, rw, rw);
            }
            else
            {
                args.Graphics.SetColor(COLOR_SELECT);
                args.Graphics.DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }
    }

}
