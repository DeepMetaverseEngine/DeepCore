using DeepCore.Geometry;
using DeepCore.GUI.Display;
using DeepCore.GUI.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static DeepCore.GUI.SceneGraph.InteractiveComponent;

namespace DeepCore.GUI.SceneGraph
{

    public class DisplayRoot : DisplayNode
    {
        private Stopwatch passTime = new Stopwatch();
        private IDisplayCanvas canvas;
        private double lastTimeMS;
        private float intervalMS;
        internal SingleThreadCollectionPool collection_pool = new SingleThreadCollectionPool();
        public int Tick { get; private set; }
        public override IDisplayCanvas Canvas { get => canvas; }
        public TimeSpan ElapsedTime { get => passTime.Elapsed; }
        public double PassTimeMS { get => passTime.Elapsed.TotalMilliseconds; }
        public float IntervalMS { get => intervalMS; }


        public DisplayRoot(IDisplayCanvas canvas)
        {
            this.canvas = canvas;
            this.parent = null;
            this.root = this;

            this.Canvas.MouseDown += this.camera_MouseDown;
            this.Canvas.MouseUp += this.camera_MouseUp;
            this.Canvas.MouseMove += this.camera_MouseMove;
            this.Canvas.MouseWheel += camera_MouseWheel;

            this.Canvas.MouseDown += this.pick_MouseDown;
            this.Canvas.MouseUp += this.pick_MouseUp;
            this.Canvas.MouseMove += this.pick_MouseMove;
            this.Canvas.MouseWheel += this.pick_MouseWheel;

            this.Canvas.KeyPress += input_KeyPress;
            this.Canvas.KeyDown += input_KeyDown;
            this.Canvas.KeyUp += input_KeyUp;

            this.Canvas.Update += CanvasUpdate;
            this.Canvas.Paint += CanvasPaint;

            this.passTime.Start();
        }
        protected override void OnDispose()
        {
            this.CanvasStartDrag = null;
            this.CanvasDragging = null;
            this.CanvasEndDrag = null;

            base.OnDispose();

            this.pick_last_mouse_down_click = null;
            this.dragging_rect = null;
            this.end_dragging_rect = null;
            this.end_dragging_list = null;
            this.dragging_list.Clear();
            this.selected_list.Clear();

            this.collection_pool?.Dispose();
            this.collection_pool = null;
            this.canvas = null;
            this.focused_input = null;
        }
        protected virtual void CanvasUpdate(IDisplayCanvas canvas)
        {
            var passTimeMS = ElapsedTime.TotalMilliseconds;
            this.intervalMS = (float)(passTimeMS - lastTimeMS);
            this.lastTimeMS = passTimeMS;
            try
            {
                root.InternalUpdate(new UpdateArgs(this, intervalMS));
                UpdatePickNode();
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
        }

        protected virtual void CanvasPaint(IDisplayCanvas canvas, Graphics g)
        {
            g.PushTransform();
            try
            {
                UpdateCamera(g);
                var args = new GraphicsArgs(g, this, intervalMS);
                root.InternalVisit(args);
                if (EnableDrawHUD)
                {
                    root.InternalVisitHUD(args);
                }
            }
            finally
            {
                g.PopTransform();
            }
            Tick++;
        }



        //------------------------------------------------------------------------------------------------------
        #region Input
        public InputComponent FocusedInput { get => focused_input; }
        private InputComponent focused_input;
        public void SetFocuseInput(InputComponent focused_input)
        {
            this.focused_input = focused_input;
        }
        private void input_KeyPress(IDisplayCanvas canvas, KeyboardArgs args)
        {
            if (this.focused_input != null)
            {
                this.focused_input.Canvas_KeyPress(args);
            }
        }
        private void input_KeyDown(IDisplayCanvas canvas, KeyboardArgs args)
        {
            if (this.focused_input != null)
            {
                this.focused_input.Canvas_KeyDown(args);
            }
        }
        private void input_KeyUp(IDisplayCanvas canvas, KeyboardArgs args)
        {
            if (this.focused_input != null)
            {
                this.focused_input.Canvas_KeyUp(args);
            }
        }

        #endregion
        //------------------------------------------------------------------------------------------------------
        #region Selection

        public delegate void CanvasDragHandler(IDisplayCanvas canvas, InteractiveComponent sender, DragArgs args);
        public event CanvasDragHandler CanvasStartDrag;
        public event CanvasDragHandler CanvasDragging;
        public event CanvasDragHandler CanvasEndDrag;

        /// <summary>
        /// 开始拖拽时距离
        /// </summary>
        public static float DRAG_MOVE_START_DISTANCE { get; set; } = 8.0f;
        public static float DRAG_RESIZE_W { get; set; } = 8;

        public bool EnableDragMoveNode { get; set; } = true;
        public bool EnableDragResizeNode { get; set; } = true;
        public bool EnableDrawHUD { get; set; } = true;

        public float GridSize { get; set; } = 8;
        public bool SnapToGrid { get; set; } = false;
        private class DraggingInfo
        {
            public readonly DisplayNode node;
            public readonly InteractiveComponent comp;
            public Vector2 start_drag_pos;
            public Vector2 start_drag_parent_pos;
            public DraggingInfo(InteractiveComponent comp)
            {
                this.comp = comp;
                this.node = comp.Owner;
            }
        }


        private bool multi_select = true;
        private bool pick_drag_started = false;
        private Vector2 pick_last_mouse_down_pos;
        private BoundingBox2D? virtual_resize_bounds;
        private InteractiveComponent pick_last_mouse_down_click;
        private RectInteracviteComponent dragging_rect;
        private RectInteracviteComponent end_dragging_rect;
        private HashMap<InteractiveComponent, DraggingInfo> end_dragging_list;
        private readonly HashMap<InteractiveComponent, DraggingInfo> dragging_list = new HashMap<InteractiveComponent, DraggingInfo>();
        private readonly List<InteractiveComponent> selected_list = new List<InteractiveComponent>();

        public bool IsMultiSelect
        {
            get { return multi_select; }
            set
            {
                if (value != multi_select)
                {
                    multi_select = value;
                    if (!value)
                    {
                        if (selected_list.Count > 0)
                        {
                            SelectSingleNode(selected_list[0].Owner);
                        }
                    }
                }
            }
        }
        public InteractiveComponent[] SelectedNodes
        {
            get { return selected_list.ToArray(); }
        }
        public InteractiveComponent SelectedNode
        {
            get { return selected_list.Count > 0 ? selected_list[0] : null; }
        }
        public InteractiveComponent LastPressedNode
        {
            get { return pick_last_mouse_down_click; }
        }
        public int SelectedCount
        {
            get { return selected_list.Count; }
        }
        public DragDirection DraggingDirection
        {
            get; private set;
        }
        public BoundingBox2D? DraggingVirtualBounds
        {
            get => virtual_resize_bounds;
        }
        public bool IsMouseInteractive { get; private set; }

        private DragDirection SetDragDirection(InteractiveComponent hitted, Vector2 rootLocation)
        {
            DraggingDirection = DragDirection.None;
            if (EnableDragResizeNode)
            {
                if (hitted is RectInteracviteComponent rect && rect.IsDragResizeable)
                {
                    var p = rootLocation;
                    var rb = rect.RootBounds;
                    var rw = DRAG_RESIZE_W;
                    if (CMath.IncludeRectPointW(rb.X, rb.Y, rw, rw, p.X, p.Y))
                    {
                        DraggingDirection = DragDirection.TopLeft;
                    }
                    else if (CMath.IncludeRectPointW(rb.X + rb.Width - rw, rb.Y, rw, rw, p.X, p.Y))
                    {
                        DraggingDirection = DragDirection.TopRight;
                    }
                    else if (CMath.IncludeRectPointW(rb.X, rb.Y + rb.Height - rw, rw, rw, p.X, p.Y))
                    {
                        DraggingDirection = DragDirection.BottomLeft;
                    }
                    else if (CMath.IncludeRectPointW(rb.X + rb.Width - rw, rb.Y + rb.Height - rw, rw, rw, p.X, p.Y))
                    {
                        DraggingDirection = DragDirection.BottomRight;
                    }
                    else if (CMath.IncludeRectPointW(rb.X, rb.Y + rw, rw, rb.Height - rw - rw, p.X, p.Y))
                    {
                        DraggingDirection = DragDirection.Left;
                    }
                    else if (CMath.IncludeRectPointW(rb.X + rb.Width - rw, rb.Y + rw, rw, rb.Height - rw - rw, p.X, p.Y))
                    {
                        DraggingDirection = DragDirection.Right;
                    }
                    else if (CMath.IncludeRectPointW(rb.X + rw, rb.Y, rb.Width - rw - rw, rw, p.X, p.Y))
                    {
                        DraggingDirection = DragDirection.Top;
                    }
                    else if (CMath.IncludeRectPointW(rb.X + rw, rb.Y + rb.Height - rw, rb.Width - rw - rw, rw, p.X, p.Y))
                    {
                        DraggingDirection = DragDirection.Bottom;
                    }
                }
            }
            return DraggingDirection;
        }
        private BoundingBox2D? SetDragBounds(InteractiveComponent hitted, Vector2 rootLocation)
        {
            var p = rootLocation;
            var b = virtual_resize_bounds.Value;
            var d = DRAG_RESIZE_W * 2;
            p = GridToSize(p);
            switch (DraggingDirection)
            {
                case DragDirection.Left:
                    if (p.X < b.Max.X - d)
                    {
                        b.Min.X = p.X;
                    }
                    break;
                case DragDirection.Right:
                    if (p.X > b.Min.X + d)
                    {
                        b.Max.X = p.X;
                    }
                    break;
                case DragDirection.Top:
                    if (p.Y < b.Max.Y - d)
                    {
                        b.Min.Y = p.Y;
                    }
                    break;
                case DragDirection.Bottom:
                    if (p.Y > b.Min.Y + d)
                    {
                        b.Max.Y = p.Y;
                    }
                    break;

                case DragDirection.TopLeft:
                    if ((p.X < b.Max.X - d) && (p.Y < b.Max.Y - d))
                    {
                        b.Min = p;
                    }
                    break;
                case DragDirection.BottomRight:
                    if ((p.X > b.Min.X + d) && (p.Y > b.Min.Y + d))
                    {
                        b.Max = p;
                    }
                    break;
                case DragDirection.TopRight:
                    if ((p.Y < b.Max.Y - d) && (p.X > b.Min.X + d))
                    {
                        b.Max.X = p.X;
                        b.Min.Y = p.Y;
                    }
                    break;
                case DragDirection.BottomLeft:
                    if ((p.Y > b.Min.Y + d) && (p.X < b.Max.X - d))
                    {
                        b.Min.X = p.X;
                        b.Max.Y = p.Y;
                    }
                    break;
                default:
                    break;
            }
            virtual_resize_bounds = b;
            return virtual_resize_bounds;
        }


        private bool MousePickRayCastInteractive(Vector2 rootLocation, out DisplayNode hitted, out InteractiveComponent hitted_comp)
        {
            hitted = null;
            InteractiveComponent comp = null;
            //if (selected_list.Count > 0)
            //             {
            //                 //优先点选已选择单位
            //                 hitted = root.InternalHitTest(rootLocation, node =>
            //                 {
            //                     if (node.Components.TryGetComponentAs<InteractiveComponent>(out var inter, true) && inter.IsSelected)
            //                     {
            //                         comp = inter;
            //                         return true;
            //                     }
            //                     return false;
            //                 });
            //             }
            if (hitted == null)
            {
                hitted = root.InternalHitTest(rootLocation, node =>
                {
                    if (node.Components.TryGetComponentAs<InteractiveComponent>(out var inter, true))
                    {
                        comp = inter;
                        return true;
                    }
                    return false;
                });
            }
            hitted_comp = comp;
            return hitted != null;
        }

        private void pick_MouseDown(IDisplayCanvas canvas, MouseArgs e)
        {
            this.SetFocuseInput(null);
            IsMouseInteractive = false;
            var rpos = CanvasLocationToRoot(e.Location);
            var hitted_comp = pick_last_mouse_down_click = null;
            if (MousePickRayCastInteractive(rpos, out var hitted, out hitted_comp))
            {
                if (hitted.Components.TryGetComponentAs<InputComponent>(out var input))
                {
                    this.SetFocuseInput(input);
                }
                IsMouseInteractive = true;
                SetDragDirection(hitted_comp, rpos);
                pick_last_mouse_down_click = hitted_comp;
                hitted_comp.Canvas_MouseDown(e.ToLocalPosition(hitted));
            }
            else
            {
                DraggingDirection = DragDirection.None;
            }
            this.end_dragging_rect = null;
            this.end_dragging_list = null;
            this.virtual_resize_bounds = null;
            this.dragging_rect = null;
            this.dragging_list.Clear();
            this.pick_drag_started = false;
            this.pick_last_mouse_down_pos = e.Location;
            //处理拖拽和点选
            if (e.Button == MouseButton.Left)
            {
                foreach (var ht in selected_list)
                {
                    ht.IsDragMoving = false;
                }
                if (hitted_comp != null)
                {
                    if (IsMultiSelect && e.IsCtrlDown)
                    {
                        AddMultiSelectNode(hitted);
                    }
                    else
                    {
                        SelectSingleNode(hitted);
                    }
                    if (DraggingDirection == DragDirection.None)
                    {
                        if (EnableDragMoveNode)
                        {
                            foreach (var ht in selected_list)
                            {
                                if (ht.IsDragMoveable)
                                {
                                    this.dragging_list.Add(ht, new DraggingInfo(ht)
                                    {
                                        start_drag_pos = ht.Owner.Position,
                                        start_drag_parent_pos = ht.Owner.Parent.LocalMouseLocation.Value,
                                    });
                                }
                            }
                        }

                    }
                    else
                    {
                        if (EnableDragResizeNode)
                        {
                            if (hitted_comp.IsDragMoveable)
                            {
                                this.dragging_rect = hitted_comp as RectInteracviteComponent;
                                this.virtual_resize_bounds = new BoundingBox2D(dragging_rect.RootBounds);
                                this.dragging_list.Add(hitted_comp, new DraggingInfo(hitted_comp)
                                {
                                    start_drag_pos = hitted_comp.Owner.Position,
                                    start_drag_parent_pos = hitted_comp.Owner.Parent.LocalMouseLocation.Value,
                                });
                            }
                        }
                    }
                }
                else
                {
                    if (!IsMultiSelect)
                    {
                        SelectSingleNode(null);
                    }
                }
            }
        }

        private void pick_MouseMove(IDisplayCanvas canvas, MouseArgs e)
        {
            IsMouseInteractive = false;
            var rpos = CanvasLocationToRoot(e.Location);
            if (MousePickRayCastInteractive(rpos, out var hitted, out var hitted_comp))
            {
                IsMouseInteractive = true;
                if (e.Button == MouseButton.None)
                    DraggingDirection = SetDragDirection(hitted_comp, rpos);
                hitted_comp.Canvas_MouseMove(e.ToLocalPosition(hitted));
            }
            else
            {
                if (e.Button == MouseButton.None)
                    DraggingDirection = DragDirection.None;
            }
            //处理拖拽
            if (e.Button == MouseButton.Left)
            {
                if (EnableDragMoveNode || EnableDragResizeNode)
                {
                    if (dragging_list.Count > 0)
                    {
                        if (!pick_drag_started)
                        {
                            var mouse_distance = Vector2.Distance(pick_last_mouse_down_pos, e.Location);
                            if (mouse_distance >= DRAG_MOVE_START_DISTANCE)
                            {
                                //超出误操作区域后开始移动
                                if (DraggingDirection == DragDirection.None)
                                {
                                    if (EnableDragMoveNode)
                                    {
                                        foreach (var ht in dragging_list.Keys)
                                        {
                                            ht.IsDragMoving = true;
                                            {
                                                var ee = DragArgs.FromMouse(e, null);
                                                ht.Canvas_StartDragging(ee);
                                                CanvasStartDrag?.Invoke(this.Canvas, ht, ee);
                                            }
                                            if (this.pick_last_mouse_down_click == ht)
                                            {
                                                this.pick_last_mouse_down_click = null;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        this.pick_last_mouse_down_click = null;
                                    }
                                }
                                else if (dragging_rect is RectInteracviteComponent ht)
                                {
                                    this.pick_last_mouse_down_click = null;
                                    if (EnableDragResizeNode)
                                    {
                                        ht.IsDragMoving = true;
                                        {
                                            var ee = DragArgs.FromMouse(e, null);
                                            ht.Canvas_StartDragging(ee);
                                            CanvasStartDrag?.Invoke(this.Canvas, ht, ee);
                                        }
                                        this.virtual_resize_bounds = new BoundingBox2D(ht.RootBounds);
                                    }
                                }
                                this.pick_drag_started = true;
                            }
                        }
                        if (pick_drag_started)
                        {
                            if (DraggingDirection == DragDirection.None)
                            {
                                if (EnableDragMoveNode)
                                {
                                    foreach (var ht in dragging_list)
                                    {
                                        var new_parent_pos = ht.Key.Owner.Parent.LocalMouseLocation.Value;
                                        var offset = ht.Value.start_drag_parent_pos - new_parent_pos;
                                        var pos = ht.Value.start_drag_pos - offset;
                                        pos = GridToSize(ht.Key.Owner, pos);
                                        ht.Key.Owner.Position = pos;
                                        {
                                            var ee = DragArgs.FromMouse(e, null);
                                            ht.Key.Canvas_Dragging(ee);
                                            CanvasDragging?.Invoke(canvas, ht.Key, ee);
                                        }
                                    }
                                    end_dragging_list = (dragging_list);
                                }
                            }
                            else if (dragging_rect is RectInteracviteComponent ht)
                            {
                                if (EnableDragResizeNode)
                                {
                                    var aabb = SetDragBounds(ht, rpos);
                                    {
                                        var ee = DragArgs.FromMouse(e, aabb);
                                        ht.Canvas_Dragging(ee);
                                        CanvasDragging?.Invoke(canvas, ht, ee);
                                    }
                                    end_dragging_rect = ht;
                                }
                            }
                        }
                    }
                }
            }

        }
        private void pick_MouseUp(IDisplayCanvas canvas, MouseArgs e)
        {
            var rpos = CanvasLocationToRoot(e.Location);
            //清理拖拽
            {
                this.pick_drag_started = false;
                if (EnableDragMoveNode)
                {
                    if (DraggingDirection == DragDirection.None)
                    {
                        if (EnableDragMoveNode)
                        {
                            if (end_dragging_list != null)
                            {
                                foreach (var ht in end_dragging_list)
                                {
                                    //                             ht.IsDragMoving = false;
                                    //                             ht.Canvas_EndDragging(DragArgs.FromMouse(e, null));
                                    var new_parent_pos = ht.Key.Owner.Parent.LocalMouseLocation.Value;
                                    var offset = ht.Value.start_drag_parent_pos - new_parent_pos;
                                    var pos = ht.Value.start_drag_pos - offset;
                                    pos = GridToSize(ht.Key.Owner, pos);
                                    ht.Key.IsDragMoving = false;
                                    ht.Key.Owner.Position = pos;
                                    ht.Key.Canvas_ResizeFromRoot(ht.Key.Owner.Position, null);
                                    {
                                        var ee = DragArgs.FromMouse(e, null);
                                        ht.Key.Canvas_EndDragging(ee);
                                        CanvasEndDrag?.Invoke(canvas, ht.Key, ee);
                                    }
                                }
                            }
                        }
                    }
                    else if (end_dragging_rect is RectInteracviteComponent ht)
                    {
                        if (EnableDragResizeNode)
                        {
                            var vb = SetDragBounds(ht, rpos);
                            ht.IsDragMoving = false;
                            if (DraggingVirtualBounds.HasValue)
                            {
                                var aabb = DraggingVirtualBounds.Value;
                                var min = ht.Parent.TransformRootToLocal(aabb.Min);
                                var max = ht.Parent.TransformRootToLocal(aabb.Max);
                                min = GridToSize(ht.Owner, min);
                                max = GridToSize(ht.Owner, max);
                                ht.Owner.Position = min;
                                ht.Canvas_ResizeFromRoot(min, new RectangleF(min, max - min));
                            }
                            {
                                var ee = DragArgs.FromMouse(e, vb);
                                ht.Canvas_EndDragging(ee);
                                CanvasEndDrag?.Invoke(canvas, ht, ee);
                            }
                        }
                    }
                }
                this.end_dragging_rect = null;
                this.end_dragging_list = null;
                this.dragging_rect = null;
                this.virtual_resize_bounds = null;
                this.dragging_list.Clear();
            }

            IsMouseInteractive = false;
            if (MousePickRayCastInteractive(rpos, out var hitted, out var hitted_comp))
            {
                IsMouseInteractive = true;
                SetDragDirection(hitted_comp, rpos);
                hitted_comp.Canvas_MouseUp(e.ToLocalPosition(hitted));
                if (pick_last_mouse_down_click == hitted_comp)
                {
                    this.pick_last_mouse_down_click.Canvas_MouseClick(e.ToLocalPosition(hitted));
                    this.pick_last_mouse_down_click = null;
                }
            }
            else
            {
                DraggingDirection = DragDirection.None;
            }
        }
        private void pick_MouseWheel(IDisplayCanvas canvas, MouseArgs e)
        {
            var rpos = CanvasLocationToRoot(e.Location);
            if (MousePickRayCastInteractive(rpos, out var hitted, out var hitted_comp))
            {
                hitted_comp.Canvas_MouseWheel(e.ToLocalPosition(hitted));
            }
        }

        public DisplayNode RayCast(Vector2 rootLocation, Predicate<DisplayNode> select = null)
        {
            return root.InternalHitTest(rootLocation, select);
        }
        public DisplayNode RayCastWithMouse(Predicate<DisplayNode> select = null)
        {
            var mpos = Canvas.MouseLocation;
            var rpos = CanvasLocationToRoot(mpos);
            return RayCast(rpos, select);
        }

        protected virtual bool RayCastInteractive(Vector2 rootLocation, out DisplayNode hitted, out InteractiveComponent hitted_comp)
        {
            InteractiveComponent comp = null;
            hitted = root.InternalHitTest(rootLocation, node =>
            {
                if (node.Components.TryGetComponentAs<InteractiveComponent>(out var inter, true))
                {
                    comp = inter;
                    return true;
                }
                return false;
            });
            hitted_comp = comp;
            return hitted != null;
        }
        private void UpdatePickNode()
        {
            if (!pick_drag_started)
            {
                var hitted = RayCastWithMouse();
                SetPickNode(hitted);
            }
            else
            {
                SetPickNode(null);
            }
        }
        private void SetPickNode(DisplayNode hitted)
        {
            root.ForEachChildren(hitted, static (hitted, c) =>
            {
                if (c.Components.TryGetComponentAs<InteractiveComponent>(out var pick, true))
                {
                    pick.IsPicked = (pick.IsPickable) && (c == hitted);
                }
                return false;
            }, true);
        }

        private void AddMultiSelectNode(DisplayNode hitted)
        {
            if (selected_list.Count > 0 && !IsMultiSelect)
            {
                return;
            }
            root.ForEachChildren(hitted, (hitted, c) =>
            {
                if (c.Components.TryGetComponentAs<InteractiveComponent>(out var pick, true))
                {
                    if (c == hitted && pick.IsSelectable)
                    {
                        pick.IsSelected = true;
                        if (!selected_list.Contains(pick))
                        {
                            selected_list.Add(pick);
                        }
                        return true;
                    }
                }
                return false;
            }, true);
        }

        private void SelectSingleNode(DisplayNode hitted)
        {
            ClearSelected();
            if (hitted == null) return;
            root.ForEachChildren(hitted, (hitted, c) =>
            {
                if (c.Components.TryGetComponentAs<InteractiveComponent>(out var pick, true))
                {
                    if (c == hitted && pick.IsSelectable)
                    {
                        pick.IsSelected = true;
                        selected_list.Add(pick);
                        return true;
                    }
                    else
                    {
                        pick.IsSelected = false;
                    }
                }
                return false;
            }, true);
        }

        public void SetSelected(DisplayNode display)
        {
            SelectSingleNode(display);
        }
        public void ClearSelected()
        {
            selected_list.Clear();
            root.ForEachChildren(this, (root, c) =>
            {
                if (c.Components.TryGetComponentAs<InteractiveComponent>(out var pick, true))
                {
                    pick.IsSelected = false;
                }
                return false;
            }, true);
        }

        public Vector2 GridToSize(DisplayNode node, Vector2 pos)
        {
            if (SnapToGrid)
            {
                return new Vector2(
                    (int)((pos.X) / GridSize) * GridSize,
                    (int)((pos.Y) / GridSize) * GridSize);
            }
            return pos;
        }
        public Vector2 GridToSize(Vector2 pos)
        {
            if (SnapToGrid)
            {
                return new Vector2(
                (int)((pos.X) / GridSize) * GridSize,
                (int)((pos.Y) / GridSize) * GridSize);
            }
            return pos;
        }
        public float GridToSize(float x)
        {
            if (SnapToGrid)
            {
                return (int)((x) / GridSize) * GridSize;
            }
            return x;
        }
        #endregion
        //------------------------------------------------------------------------------------------------------
        #region Camera
        public static float DRAG_CAMERA_DISTANCE { get; set; } = 8.0f;
        public static MouseButton DRAG_CAMERA_BUTTON { get; set; } = MouseButton.Middle;
        public static Color COLOR_CROSSHAIR { get; set; } = Color.LightGray;

        private bool canvas_mouse_last_down = false;
        private Vector2 canvas_mouse_last_pos = Vector2.NaN;
        private Vector2 camera_pos = Vector2.Zero;
        private Vector2 camera_last_pos = Vector2.Zero;
        private float camera_zoom = 1f;

        public Vector2 CameraLeftTop
        {
            set
            {
                var size = this.Canvas.Size;
                this.CameraPos = new DeepCore.Geometry.Vector2(
                   value.X - size.X / 2,
                   value.Y - size.Y / 2);
            }
            get
            {
                var pos = this.CameraPos;
                var size = this.Canvas.Size;
                return new DeepCore.Geometry.Vector2(
                    pos.X + size.X / 2,
                    pos.Y + size.Y / 2);
            }
        }

        public Vector2 CameraPos
        {
            get => camera_pos;
            set { camera_pos = value; }
        }
        public float CameraZoom
        {
            get => camera_zoom;
            set { camera_zoom = value; }
        }
        public RectangleF CameraLocalBounds
        {
            get
            {
                var p1 = CanvasLocationToRootLocal(Vector2.Zero);
                var p2 = CanvasLocationToRootLocal(Canvas.Size);
                return new RectangleF(p1, p2 - p1);
            }
        }
        public RectangleF CameraToLocalBounds(DisplayNode local)
        {
            var bounds = this.CameraLocalBounds;
            var b1 = local.TransformRootToLocal(bounds.Location);
            var b2 = local.TransformRootToLocal(bounds.Location + bounds.Size);
            return new RectangleF(b1, b2 - b1);
        }
        public bool IsShowCameraCross { get; set; } = true;
        public bool EnableCamera { get; set; } = true;
        public bool EnableMouseRightMoveCamera { get; set; } = true;
        public bool EnableMouseWheelZoomCamera { get; set; } = true;
        public Vector3 RootMousePoint { get => CanvasLocationToRootLocal(Canvas.MouseLocation); }
        public override Vector2 LocalMouseLocation => this.RootMousePoint;
        protected virtual void UpdateCamera(Graphics g)
        {
            //g.SetColor(Color.White);
            //g.DrawString($"CameraPos[{(int)camera_pos.X}, {(int)camera_pos.Y}]", Vector2.Zero);
            //g.DrawString($"MousePos[{(int)RootMousePoint.X}, {(int)RootMousePoint.Y}]", new Vector2(0, 20));
            if (EnableCamera)
            {
                var winSize = Canvas.Size;
                g.Translate(winSize.X / 2, winSize.Y / 2);
                if (IsShowCameraCross)
                {
                    DrawCross(g, new Vector2(camera_pos.X * camera_zoom, camera_pos.Y * camera_zoom));
                }
                {
                    g.Scale(camera_zoom, camera_zoom);
                    g.Translate(camera_pos.X, camera_pos.Y);
                }
            }
        }
        protected virtual void DrawCross(Graphics g, in Vector2 offset)
        {
            var winSize = Canvas.Size;
            g.SetColor(COLOR_CROSSHAIR);
            g.DrawLine(winSize.X, offset.Y, -winSize.X, offset.Y);
            g.DrawLine(offset.X, winSize.Y, offset.X, -winSize.Y);
        }
        private Vector3 CanvasLocationToRootLocal(in Vector2 winPos)
        {
            if (EnableCamera)
            {
                var winSize = Canvas.Size;
                var mpos = winPos - new Vector2(winSize.X / 2, winSize.Y / 2);
                mpos.X /= camera_zoom;
                mpos.Y /= camera_zoom;
                mpos -= camera_pos;
                return mpos;
            }
            return winPos;
        }
        public Vector3 CanvasLocationToRoot(Vector2 mpos)
        {
            mpos = CanvasLocationToRootLocal(mpos);
            var rpos = root.TransformParentToLocal(mpos);
            return rpos;
        }
        public Vector3 CanvasLocationToNode(Vector2 mpos, DisplayNode node)
        {
            mpos = CanvasLocationToRootLocal(mpos);
            var rpos = root.TransformParentToLocal(mpos);
            rpos = node.TransformRootToLocal(rpos);
            return rpos;
        }
        private void camera_MouseDown(IDisplayCanvas canvas, MouseArgs e)
        {
            if (EnableCamera && EnableMouseRightMoveCamera)
            {
                pick_drag_started = false;
                if (e.Button == DRAG_CAMERA_BUTTON)
                {
                    canvas_mouse_last_down = true;
                    canvas_mouse_last_pos = e.Location;
                    camera_last_pos = camera_pos;
                }
            }
        }
        private void camera_MouseMove(IDisplayCanvas canvas, MouseArgs e)
        {
            if (EnableCamera && EnableMouseRightMoveCamera)
            {
                if (canvas_mouse_last_down)
                {
                    var offset = new Vector2(
                        (canvas_mouse_last_pos.X - e.Location.X),
                        (canvas_mouse_last_pos.Y - e.Location.Y));

                    if (!pick_drag_started)
                    {
                        if (offset.Length() > DRAG_CAMERA_DISTANCE)
                        {
                            pick_drag_started = true;
                        }
                    }
                    else
                    {
                        this.camera_pos = camera_last_pos - offset / camera_zoom;
                        Canvas.RequestRepaint();
                    }
                }
            }
        }
        private void camera_MouseUp(IDisplayCanvas canvas, MouseArgs e)
        {
            if (EnableCamera && EnableMouseRightMoveCamera)
            {
                pick_drag_started = false;
                if (e.Button == DRAG_CAMERA_BUTTON)
                {
                    canvas_mouse_last_down = false;
                    Canvas.RequestRepaint();
                }
            }
        }
        private void camera_MouseWheel(IDisplayCanvas canvas, MouseArgs e)
        {
            if (EnableCamera && EnableMouseWheelZoomCamera)
            {
                this.camera_zoom += CMath.GetDirect(e.Delta) * 0.1f * camera_zoom;
                this.camera_zoom = Math.Max(0.01f, camera_zoom);
                Canvas.RequestRepaint();
            }
        }


        #endregion
        //------------------------------------------------------------------------------------------------------
    }


}
