
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Action;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.UI
{
    public class UIComponent : ActionNode
    {
        protected UIEditor mEditor = null;
        protected UIComponentMeta mMeta = null;
        protected UILayout mUILayout = null;

        public UIComponent(UILayout layout = null)
        {
            if (layout != null)
                mUILayout = layout;
            else
                mUILayout = new UILayout();
        }

        public int UserTag { set; get; }
        public Object UserData { set; get; }
        public string EditName { set; get; }
        public UIComponentMeta MetaData { get => mMeta; }
        public virtual UILayout Layout
        {
            set { mUILayout = value; }
            get { return mUILayout; }
        }
        public virtual float Width
        {
            get
            {
                if (Bounds == null) { return 0; }
                else { return Bounds.width; }
            }
        }
        public virtual float Height
        {
            get
            {
                if (Bounds == null) { return 0; }
                else { return Bounds.height; }
            }
        }
        protected virtual void Resize(float w, float h, bool flush)
        {
            if (Bounds == null) { Bounds = new Gemo.Rectangle2D(0, 0, 1, 1); }
            if (flush || w != Bounds.width || h != Bounds.height)
            {
                this.Bounds.width = w;
                this.Bounds.height = h;
            }
        }
        public virtual void SetSize(float w, float h)
        {
            this.Resize(w, h, false);
        }

        public void SetLocation(float x, float y)
        {
            this.Position = new Geometry.Vector3(x, y, 0);
        }

        public void SetImage(Image image, Rectangle2D bounds = null)
        {
            if (mUILayout != null)
            {
                mUILayout.SetImage(image, mUILayout.Style, 0);
                if (bounds != null)
                {
                    SetSize(bounds.width, bounds.height);
                }
            }
        }

        public DisplayNode GetUI(string edit_name)
        {
            DisplayNode child = null;
            UIComponent childui = null;
            UIComponent uicc = null;
            int i;

            // 广度遍历.
            int length = NumChildren;
            for (i = length - 1; i >= 0; --i)
            {
                child = this.GetChildAt(i);
                if (child is UIComponent)
                {
                    childui = child as UIComponent;
                    if (childui.EditName == edit_name)
                    {
                        return childui;
                    }
                }
            }

            for (i = length - 1; i >= 0; --i)
            {
                child = this.GetChildAt(i);
                if (child is UIComponent)
                {
                    childui = child as UIComponent;
                    uicc = (UIComponent)childui.GetUI(edit_name);
                    if (uicc != null)
                    {
                        return uicc;
                    }
                }
            }

            return null;
        }

        public override void Draw(Graphics g)
        {
            if (mUILayout != null) { mUILayout.Render(g, Width, Height); }
        }
        public override void Update(float delatTime)
        {
            base.Update(delatTime);
            if (mUILayout != null)
            {
                mUILayout.Update();
            }
        }
        protected override void Disposing()
        {
            this.UserData = null;
            this.EditName = null;
            this.mEditor = null;
            if (mUILayout != null)
            {
                mUILayout.Dispose();
                mUILayout = null;
            }
            base.Disposing();
        }


        public virtual void DecodeFromMeta(UIEditor editor, UIComponentMeta e)
        {
            this.mEditor = editor;
            this.mMeta = e;

            this.DecodeBegin(editor, e);
            this.DecodeFields(editor, e);
            this.DecodeChilds(editor, e);
            this.DecodeEnd(editor, e);

        }

        protected virtual void AddEditorComopnent(UIComponent c)
        {
            this.AddChild(c);
        }

        protected virtual void DecodeFields(UIEditor editor, UIComponentMeta e)
        {
            this.Bounds = new Rectangle2D(0, 0, e.Width, e.Height);
            this.Position = new Geometry.Vector3(e.X, e.Y, 0);
            this.Visible = e.Visible;
            this.EditName = e.EditorName;
            this.Name = string.Format("{0} - {1}", this.EditName, e.ClassName);

            this.UserData = e.UserData;
            this.UserTag = e.UserTag;

            this.Layout = editor.CreateLayout(e.Layout);

            //this.Enable = e.Enable;
            //this.EnableChildren = e.EnableChilds;

        }

        protected virtual void DecodeChilds(UIEditor editor, UIComponentMeta e)
        {
            if (e.Childs != null)
            {
                int len = e.Childs.Count;
                for (int i = 0; i < len; ++i)
                {
                    var child = e.Childs[i];
                    var cui = editor.CreateComponent(child);
                    if (cui != null)
                    {
                        AddEditorComopnent(cui);
                    }
                }
            }
        }

        protected virtual void DecodeBegin(UIEditor editor, UIComponentMeta e)
        {

        }

        protected virtual void DecodeEnd(UIEditor editor, UIComponentMeta e)
        {

        }

        public override DisplayNode Clone()
        {
            if (mMeta != null)
            {
                return mEditor.CreateComponent(mMeta);
            }
            return null;
        }
        
    }
}

