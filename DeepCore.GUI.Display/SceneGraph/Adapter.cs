using DeepCore.Geometry;
using DeepCore.GUI.Display;
using DeepCore.GUI.Input;
using System;
using System.Threading.Tasks;

namespace DeepCore.GUI.SceneGraph
{
    public delegate void CanvasMouseEvent(IDisplayCanvas canvas, MouseArgs args);
    public delegate void CanvasKeyboardEvent(IDisplayCanvas canvas, KeyboardArgs args);
    public delegate void CanvasPaintEvent(IDisplayCanvas canvas, Graphics g);
    public delegate void CanvasUpdateEvent(IDisplayCanvas canvas);
    public delegate void CanvasResizeEvent(IDisplayCanvas canvas, Vector2 size);


    public interface IDisplayCanvas
    {
        bool Focused { get; }
        Vector2 Size { get; }
        Vector2 MouseLocation { get; }
        MouseButton MouseButtons { get; }
        KeyCode KeyStatus { get; }

        bool IsDisposing { get; }

        event CanvasMouseEvent MouseDown;
        event CanvasMouseEvent MouseUp;
        event CanvasMouseEvent MouseMove;
        event CanvasMouseEvent MouseWheel;

        event CanvasKeyboardEvent KeyDown;
        event CanvasKeyboardEvent KeyPress;
        event CanvasKeyboardEvent KeyUp;

        event CanvasPaintEvent Paint;
        event CanvasUpdateEvent Update;
        event CanvasResizeEvent Resize;

        //void PostToEditor(DisplayNode e, object arg);
        void RequestRepaint();
        void Invoke(Action<IDisplayCanvas> invoke);
        void Invoke(Func<IDisplayCanvas, Task> invoke);
    }
    public struct UpdateArgs
    {
        public DisplayRoot Root { get; }
        public float IntervalMS { get; }
        public UpdateArgs(DisplayRoot root, float intervalMS)
        {
            this.Root = root;
            this.IntervalMS = intervalMS;
        }
    }
    public struct GraphicsArgs
    {
        public Graphics Graphics { get; }
        public DisplayRoot Root { get; }
        public float IntervalMS { get; }
        public GraphicsArgs(Graphics g, DisplayRoot root, float intervalMS)
        {
            this.Root = root;
            this.IntervalMS = intervalMS;
            this.Graphics = g;
        }
    }
    public struct MouseArgs
    {
        public DisplayRoot Root { get; }
        public Vector2 Location;
        public MouseButton Button;
        public int Clicks;
        public float Delta;
        public bool IsCtrlDown;
        public MouseArgs(DisplayRoot root)
        {
            this.Root = root;
        }
        public MouseArgs ToLocalPosition(DisplayNode node)
        {
            return new MouseArgs(node.Root)
            {
                Location = node.LocalMouseLocation.Value,
                Button = this.Button,
                Clicks = this.Clicks,
                Delta = this.Delta,
                IsCtrlDown = this.IsCtrlDown,
            };
        }
    }
    public struct DragArgs
    {
        public DisplayRoot Root { get; }
        public MouseArgs Mouse;
        public BoundingBox2D? VirtualBounds;
        public DragArgs(DisplayRoot root)
        {
            this.Root = root;
        }
        public static DragArgs FromMouse(MouseArgs node, BoundingBox2D? virtualBounds)
        {
            return new DragArgs(node.Root)
            {
                Mouse = node,
                VirtualBounds = virtualBounds,
            };
        }
    }
    public struct KeyboardArgs
    {
        public DisplayRoot Root { get; }
        public bool Alt;
        public bool Control;
        public bool Handled;
        public char KeyChar;
        public KeyCode KeyCode;
        public int KeyValue;
        public KeyCode KeyData;
        public KeyCode Modifiers;
        public bool Shift;
        public bool SuppressKeyPress;
        public KeyboardArgs(DisplayRoot root)
        {
            this.Root = root;
        }
    }
    public struct ChildArgs
    {
        public DisplayNode Parent { get; }
        public DisplayNode Child { get; }
        public ChildArgs(DisplayNode p, DisplayNode c)
        {
            this.Parent = p;
            this.Child = c;
        }
    }

    public class Transform
    {
        private Vector3 translation = new Vector3();
        private float rotation = 0f;
        private Vector2 scale = new Vector2(1f, 1f);

        public Vector3 Translation
        {
            get { return translation; }
            set { translation = value; }
        }
        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }
        public Vector2 Scale
        {
            get { return scale; }
            set { scale = value; }
        }
        internal Vector3 ParentToLocal(in Vector3 parent)
        {
            var mtx = Matrix.Identity;
            this.Apply(ref mtx);
            mtx = Matrix.Invert(in mtx);
            return Vector3.Transform(in parent, in mtx);
        }
        internal Vector3 LocalToParent(in Vector3 local)
        {
            var mtx = Matrix.Identity;
            this.Apply(ref mtx);
            return Vector3.Transform(in local, in mtx);
        }
        internal void ParentToLocal(Vector3[] parent)
        {
            var mtx = Matrix.Identity;
            this.Apply(ref mtx);
            mtx = Matrix.Invert(in mtx);
            for (int i = 0; i < parent.Length; i++)
            {
                parent[i] = Vector3.Transform(in parent[i], in mtx);
            }
        }
        internal void LocalToParent(Vector3[] local)
        {
            var mtx = Matrix.Identity;
            this.Apply(ref mtx);
            for (int i = 0; i < local.Length; i++)
            {
                local[i] = Vector3.Transform(in local[i], in mtx);
            }
        }


        internal void Apply(ref Matrix trans)
        {
            trans = Matrix.Multiply(in trans, Matrix.CreateRotationZ(rotation));
            trans = Matrix.Multiply(in trans, Matrix.CreateScale(new Vector3(scale.X, scale.Y, 1f)));
            trans = Matrix.Multiply(in trans, Matrix.CreateTranslation(translation));
        }
        internal void Apply(Graphics gfx)
        {
            gfx.Translate(translation.X, translation.Y);
            gfx.Scale(scale.X, scale.Y);
            gfx.Rotate(rotation);
        }
    }
    public enum DragDirection
    {
        None = 0,

        Left = 1,
        Right = 2,
        Top = 3,
        Bottom = 4,

        TopLeft = 5,
        TopRight = 6,
        BottomLeft = 7,
        BottomRight = 8,
    }
}
