using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepCore.Unity.OnGUI
{
    public class MyGUI : UnityEngine.GUI
    {
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            return DoBeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
        }
    }

    public enum DockStyle
    {
        None,
        Top,
        Bottom,
        Left,
        Right,
        Fill,
    }
    public enum AnchorStyles
    {
        Top = 0x01,
        Bottom = 0x02,
        TopBottom = 0x03,
        Left = 0x10,
        Right = 0x20,
        LeftRight = 0x30,
        None = 0,
    }
    public struct Padding
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;
        public Padding(float left, float top, float right, float bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }

        public static Padding Zero = new Padding() { Left = 0, Right = 0, Top = 0, Bottom = 0, };
    }

    public abstract class GUIObject : Disposable
    {
        public Rect Bounds { get; set; } = new Rect(0, 0, 100, 20);
        public Vector2 Position { get => Bounds.position; set => Bounds = new Rect(value, Size); }
        public Vector2 Size { get => Bounds.size; set => Bounds = new Rect(Position, value); }
        public float X { get => Bounds.x; }
        public float Y { get => Bounds.y; }
        public float Width { get => Bounds.width; }
        public float Height { get => Bounds.height; }

        public GUIContent Content { get; set; } = new GUIContent();
        public string Text { get => Content.text; set => Content.text = value; }
        public string Tooltip { get => Content.tooltip; set => Content.tooltip = value; }
        public Texture Image { get => Content.image; set => Content.image = value; }
        public GUIStyle Style { get; set; }

        public DockStyle Dock { get; set; } = DockStyle.None;
        public AnchorStyles Anchor { get; set; } = AnchorStyles.None;
        public Padding AnchorPadding { get; set; } = Padding.Zero;
        public Padding LocalPadding { get; set; } = Padding.Zero;
        public bool AutoSize { get; set; } = false;

        private GUIContainer parent;
        private bool firstShown = false;


        public GUIContainer Parent
        {
            get => parent;
            internal set
            {
                parent = value;
            }
        }

        public object UserTag { get; set; }

        public GUIObject(GUIStyle style)
        {
            this.Style = style;
        }
        internal void InternalVisit(GUIGraphics g)
        {
            if (!firstShown)
            {
                firstShown = true;
                OnShown();
            }
            OnVisit(g);
        }
        protected virtual void OnShown()
        {

        }
        protected abstract void OnVisit(GUIGraphics g);

        public void Draw()
        {
            using (var g = new GUIGraphics())
            {
                g.ParentSize = new Vector2(Screen.width, Screen.height);
                g.LocalBounds = this.Bounds;
                InternalVisit(g);
                GUIUtils.AutoTooltips();
            }
        }
    }


    public abstract class GUIContainer : GUIObject
    {
        private readonly List<GUIObject> Childs = new List<GUIObject>();
        public Rect ChildTotalBounds
        {
            get
            {
                var rect = new Rect(0, 0, 0, 0);
                foreach (var child in Childs)
                {
                    var cb = child.Bounds;
                    rect.xMin = Math.Min(rect.xMin, cb.xMin);
                    rect.yMin = Math.Min(rect.yMin, cb.yMin);
                    rect.xMax = Math.Max(rect.xMax, cb.xMax);
                    rect.yMax = Math.Max(rect.yMax, cb.yMax);
                }
                return rect;
            }
        }
        public GUIContainer(GUIStyle style) : base(style)
        {
            this.LocalPadding = new Padding(4, 4, 4, 4);
            this.Style = new GUIStyle(UnityEngine.GUI.skin.box);
        }
        //         public Rect ChildViewBounds
        //         {
        //             get
        //             {
        //                 var bounds = Bounds;
        //                 return new Rect(
        //                          LocalPadding.Left,
        //                          LocalPadding.Top,
        //                          bounds.width - LocalPadding.Left - LocalPadding.Right,
        //                          bounds.height - LocalPadding.Top - LocalPadding.Bottom);
        //             }
        //         }
        public bool AddChild(GUIObject obj)
        {
            if (!Childs.Contains(obj))
            {
                Childs.Add(obj);
                obj.Parent = this;
                return true;
            }
            return false;
        }
        public bool RemoveChild(GUIObject obj)
        {
            if (Childs.Remove(obj))
            {
                obj.Parent = null;
                return true;
            }
            return false;
        }
        protected override void Disposing()
        {
            foreach (var child in Childs.ToArray())
            {
                child.Dispose();
            }
            Childs.Clear();
        }
        protected virtual void OnVisitChildren(GUIGraphics g)
        {
            g.ParentSize = g.GetChildBounds(g.LocalBounds, this.LocalPadding).size;
            foreach (var child in Childs.ToArray())
            {
                OnVisitChild(g, child);
            }
        }
        protected virtual void OnVisitChild(GUIGraphics g, GUIObject child)
        {
            var childBounds = child.Bounds;
            g.Layout(this, child, ref childBounds);
            child.Bounds = childBounds;
            g.LocalBounds = childBounds;
            child.InternalVisit(g);
        }
        protected override void OnVisit(GUIGraphics g)
        {
            OnVisitChildren(g);
        }

    }


    public class GUICanvas : GUIContainer
    {
        public int FontSize { get; set; } = 9;
        public GUICanvas() : base(new GUIStyle()) { }
        public void Visit()
        {
            using (var g = new GUIGraphics())
            {
                this.Bounds = new Rect(0, 0, Screen.width, Screen.height);
                g.CurrentStype.fontSize = FontSize;
                g.ParentSize = new Vector2(Screen.width, Screen.height);
                g.LocalBounds = this.Bounds;
                InternalVisit(g);
            }
            if (GUI.changed)
            {
                Input.ResetInputAxes();
            }
            GUIUtils.AutoTooltips();
        }
    }



    public class GUITextureManager : Disposable
    {
        public static Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        public static Texture2D LoadTextureFromAssembly(Assembly asm, string assemblyName)
        {
            var data = DeepCore.IO.Resource.LoadFromAssembly(asm, assemblyName);
            if (data != null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
                tex.name = assemblyName;
                tex.LoadImage(data, true);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.anisoLevel = 2;
                tex.mipMapBias = 0;
                Debug.Log($"LoadTextureFromAssembly : {assemblyName}");
                return tex;
            }
            return null;
        }

        private HashMap<object, HashMap<string, Texture2D>> m_Map = new HashMap<object, HashMap<string, Texture2D>>();
        public Texture2D MakeTexture(string name, int width, int height, Color col)
        {
            return MakeTexture(this, name, width, height, col);
        }
        public Texture2D MakeTexture(object handler, string name, int width, int height, Color col)
        {
            return m_Map.GetOrAdd(handler, static a => new HashMap<string, Texture2D>()).GetOrAdd(name, n => MakeTexture(width, height, col));
        }
        public Texture2D MakeAssemblyTexture(Assembly asm, string assemblyName)
        {
            return m_Map.GetOrAdd(asm, static a => new HashMap<string, Texture2D>()).GetOrAdd(assemblyName, n => LoadTextureFromAssembly(asm, n));
        }
        protected override void Disposing()
        {
            foreach (var e in m_Map.Values)
            {
                foreach (var t in e.Values)
                {
                    try
                    {
                        Texture2D.Destroy(t);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                    }
                }
            }
            m_Map.Clear();
        }
    }

    public class GUIGraphics : DeepCore.Disposable
    {
        private Stack<ValueTuple<Color, Color, Color>> _colorStack = new();
        private Stack<Rect> _clipStack = new();
        private GUIStyle currentStyle = null;

        public GUIGraphics()
        {
            currentStyle = new GUIStyle(UnityEngine.GUI.skin.box);
            ParentSize = new Vector2(Screen.width, Screen.height);
        }
        protected override void Disposing()
        {

        }

        public GUIStyle CurrentStype { get { return currentStyle; } }
        public bool ContainsMouse(Rect frect)
        {
            return frect.Contains(Event.current.mousePosition);
        }
        public void PushColor()
        {
            _colorStack.Push(new(UnityEngine.GUI.backgroundColor, UnityEngine.GUI.color, UnityEngine.GUI.contentColor));
        }
        public void PopColor()
        {
            var tuple = _colorStack.Pop();
            UnityEngine.GUI.backgroundColor = tuple.Item1;
            UnityEngine.GUI.color = tuple.Item2;
            UnityEngine.GUI.contentColor = tuple.Item3;
        }
        //-------------------------------------------------------------------------------------------------------------------
        internal void Layout(GUIContainer parent, GUIObject child, ref Rect bounds)
        {
            if (OnChildLayoutDock(parent, child, ref bounds))
            {

            }
            else if (OnChildLayoutAnchor(parent, child, ref bounds))
            {

            }
        }
        public Rect GetChildBounds(Rect localBounds, Padding localPadding)
        {
            return new Rect(
                     localPadding.Left,
                     localPadding.Top,
                      localBounds.width - localPadding.Left - localPadding.Right,
                      localBounds.height - localPadding.Top - localPadding.Bottom);
        }
        private bool OnChildLayoutDock(GUIContainer parent, GUIObject child, ref Rect bounds)
        {
            switch (child.Dock)
            {
                case DockStyle.Fill:
                    var childBounds = GetChildBounds(this.LocalBounds, parent.LocalPadding);
                    bounds = childBounds;
                    return true;
            }
            return false;
        }
        private bool OnChildLayoutAnchor(GUIContainer parent, GUIObject child, ref Rect bounds)
        {
            if (child.Anchor != AnchorStyles.None)
            {
                var parentSize = this.ParentSize;
                var origin = child.AnchorPadding;
                {
                    var maskX = child.Anchor & (AnchorStyles.LeftRight);
                    if (maskX == AnchorStyles.LeftRight)
                    {
                        bounds.width = parentSize.x - origin.Left - origin.Right;
                        bounds.x = origin.Left;
                    }
                    else if (maskX == AnchorStyles.Left)
                    {
                        bounds.x = origin.Left;
                    }
                    else if (maskX == AnchorStyles.Right)
                    {
                        bounds.x = parentSize.x - bounds.width - origin.Right;
                    }
                }
                {
                    var maskY = child.Anchor & (AnchorStyles.TopBottom);
                    if (maskY == AnchorStyles.TopBottom)
                    {
                        bounds.height = parentSize.y - origin.Top - origin.Bottom;
                        bounds.y = origin.Top;
                    }
                    else if (maskY == AnchorStyles.Top)
                    {
                        bounds.y = origin.Top;
                    }
                    else if (maskY == AnchorStyles.Bottom)
                    {
                        bounds.y = parentSize.y - bounds.height - origin.Bottom;
                    }
                }
                return true;
            }
            return false;
        }
        //-------------------------------------------------------------------------------------------------------------------

        public void DrawBox(Rect rect, GUIContent content, GUIStyle style)
        {
            GUI.Box(rect, content, style);
        }
        public bool DrawButton(Rect rect, GUIContent content, GUIStyle style)
        {
            return GUI.Button(rect, content, style);
        }
        public void DrawBeginGroup(Rect rect, GUIContent content, GUIStyle style)
        {
            GUI.BeginGroup(rect, content, style);
        }
        public void DrawEndGroup()
        {
            UnityEngine.GUI.EndGroup();
        }
        public void DrawLabel(Rect rect, GUIContent content, GUIStyle style)
        {
            GUI.Label(rect, content, style);
        }
        public void DrawBeginScrollView(Rect rect, GUIContent content, GUIStyle style)
        {
            GUI.BeginGroup(rect, content, style);
        }
        public void DrawEndScrollView()
        {
            UnityEngine.GUI.EndGroup();
        }
        public bool DrawRepeatButton(Rect rect, GUIContent content, GUIStyle style)
        {
            return GUI.RepeatButton(rect, content, style);
        }
        //         public string DrawTextField(Rect rect, GUIContent content, GUIStyle style)
        //         {
        //             return GUI.TextField(rect, content, style);
        //         }
        //-------------------------------------------------------------------------------------------------------------------
        public class BeginGroupRect : Disposable
        {
            private Rect _rect;
            public Rect rect => _rect;
            public BeginGroupRect(Rect rect)
            {
                _rect = rect;
                UnityEngine.GUI.BeginGroup(rect);
            }
            protected override void Disposing()
            {
                UnityEngine.GUI.EndGroup();
            }
        }
        public BeginGroupRect BeginGroup(Rect rect)
        {
            return new BeginGroupRect(rect);
        }
        //-------------------------------------------------------------------------------------------------------------------


        //-------------------------------------------------------------------------------------------------------------------

        public Rect LocalBounds { get; internal set; }
        public Vector2 ParentSize { get; internal set; }

        //-----------------------------------------------------------------------------------------------
        /*
        public void DrawLine(float x1, float y1, float x2, float y2)
        {
            GL.Begin(GL.LINES);
            GL.Vertex3(x1, y1, 0);
            GL.Vertex3(x2, y2, 0);
            GL.End();
        }

        public void FillRect4Color(float x, float y, float w, float h, UnityEngine.Color[] rgba)
        {
            GL.Begin(GL.QUADS);
            GL.Color(rgba[0]);
            GL.Vertex3(x, y, 0);
            GL.Color(rgba[1]);
            GL.Vertex3(x, y + h, 0);
            GL.Color(rgba[2]);
            GL.Vertex3(x + w, y + h, 0);
            GL.Color(rgba[3]);
            GL.Vertex3(x + w, y, 0);
            GL.End();
        }

        public void DrawRect(float x, float y, float w, float h)
        {
            float x2 = x + w;
            float y2 = y + h;
            GL.Begin(GL.LINES);

            GL.Color(mUnityColor);

            GL.Vertex3(x, y, 0);
            GL.Vertex3(x2, y, 0);

            GL.Vertex3(x2, y, 0);
            GL.Vertex3(x2, y2, 0);

            GL.Vertex3(x2, y2, 0);
            GL.Vertex3(x, y2, 0);

            GL.Vertex3(x, y2, 0);
            GL.Vertex3(x, y, 0);

            GL.End();
        }
        public void FillRect(float x, float y, float w, float h)
        {
            GL.Begin(GL.QUADS);
            GL.Color(mUnityColor);
            GL.Vertex3(x, y, 0);
            GL.Color(mUnityColor);
            GL.Vertex3(x, y + h, 0);
            GL.Color(mUnityColor);
            GL.Vertex3(x + w, y + h, 0);
            GL.Color(mUnityColor);
            GL.Vertex3(x + w, y, 0);
            GL.End();
        }

        public void DrawArc(float x, float y, float w, float h, float startAngle, float arcAngle)
        {
            int point_count = 32;
            float sw = w / 2;
            float sh = h / 2;
            float sx = x + sw;
            float sy = y + sh;

            float degree_start = CMath.AngleToRadian(startAngle);
            float degree_delta = Mathf.PI * 2 / point_count;
            point_count++;
            GL.Begin(GL.LINES);
            GL.Color(mUnityColor);
            for (int i = 0; i < point_count; i++)
            {
                float idegree = degree_start + i * degree_delta;
                GL.Vertex3(sx + Mathf.Cos(idegree) * sw, sy + Mathf.Sin(idegree) * sh, 0);
                GL.Vertex3(sx, sy, 0);
            }
            GL.End();
        }

        public void FillArc(float x, float y, float w, float h, float startAngle, float arcAngle)
        {
            int point_count = 32;
            float sw = w / 2;
            float sh = h / 2;
            float sx = x + sw;
            float sy = y + sh;

            float degree_start = CMath.AngleToRadian(startAngle);
            float degree_delta = Mathf.PI * 2 / point_count;
            point_count++;
            GL.Begin(GL.TRIANGLE_STRIP);
            GL.Color(mUnityColor);
            for (int i = 0; i < point_count; i++)
            {
                float idegree = degree_start + i * degree_delta;
                GL.Vertex3(sx + Mathf.Cos(idegree) * sw, sy + Mathf.Sin(idegree) * sh, 0);
                GL.Vertex3(sx, sy, 0);
            }
            GL.End();
        }

        //-----------------------------------------------------------------------------------------------

    */



    }
}
