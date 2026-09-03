using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.SceneGraph;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    //-----------------------------------------------------------------------------------------------------------

    [Reflectible]
    public abstract class UEComponentNode : DisplayNode
    {
        public bool IsEditor => Editor.IsEditor;
        public RectInteracviteComponent Rect { get; }
        public UIFactory Editor { get; }
        public UEComponentMeta Meta { get; }
        public UEContainerNode ParentUI { get => parent as UEContainerNode; }
        public string EditName { get => Meta.EditorName; set { Meta.EditorName = value; Name = value; } }
        public RectangleF LocalBounds { get => Rect.Bounds; set => Rect.Bounds = value; }
        public UILayout Layout { get; protected set; }
        public UILayout LayoutDisable { get; protected set; }
        public Padding Margin { get; set; } = Padding.Zero;

        public int UserTag { set; get; }
        public Object UserData { set; get; }
        public UEComponentNode(UIFactory editor, UEComponentMeta e)
        {
            this.Rect = this.Components.AddComponent<RectInteracviteComponent>();
            this.Editor = editor;
            this.Name = e.EditorName;
            this.Meta = e;
            //             if (IsEditor)
            //             {
            //                 Rect.MouseEndDrag += (sender, e) =>
            //                 {
            //                     var p = this.Position;
            //                     this.Canvas.PostToEditor(this, Meta);
            //                 };
            //             }
        }
        public override string ToString()
        {
            return EditName;
        }
        #region Init--------------------------------------------------------------------------------------------
        public void Decode()
        {
            this.DoDecodeFields();
            if (Meta is UEContainerMeta containerMeta)
            {
                this.DoDecodeChilds();
            }
        }
        protected virtual void DoDecodeFields()
        {
            var editor = this.Editor;
            var e = this.Meta;
            this.Rect.Bounds = new RectangleF(0, 0, e.Width, e.Height);
            this.Margin = e.Margin;
            this.Position = new Geometry.Vector3(e.X, e.Y, e.Z);
            this.IsVisible = e.Visible;
            this.Name = e.EditorName;
            this.UserData = e.UserData;
            this.UserTag = e.UserTag;
            this.Layout = editor.CreateLayout(e.Layout);
            this.LayoutDisable = editor.CreateLayout(e.DisableLayout);
            this.AutoRelease(this.Layout);
            this.AutoRelease(this.LayoutDisable);
            this.Rect.IsPickable = e.Enable;
        }
        protected virtual void DoDecodeChilds() { }

        public UEComponentMeta Encode()
        {
            DoEncodeFields();
            if (Meta is UEContainerMeta containerMeta)
            {
                this.DoEncodeChilds();
            }
            return Meta;
        }
        protected virtual void DoEncodeFields()
        {
            var pos = this.Position;
            Meta.X = pos.X;
            Meta.Y = pos.Y;
            Meta.Z = pos.Z;
            Meta.Width = Rect.Bounds.Width;
            Meta.Height = Rect.Bounds.Height;
            Meta.Margin = this.Margin;
        }
        protected virtual void DoEncodeChilds()
        {

        }

        #endregion Init---------------------------------------------------------------------------------------------

        #region Editor----------------------------------------------------------------------------------------------
        public void RefreshFromEditor()
        {
            if (Editor.IsEditor)
            {
                OnRefreshFromEditor();
            }
        }
        protected virtual void OnRefreshFromEditor()
        {
            this.DoDecodeFields();
        }
        #endregion Editor-------------------------------------------------------------------------------------------

        #region Visit-----------------------------------------------------------------------------------------------
        protected override void OnUpdate(UpdateArgs args)
        {
            this.OnUpdateLayout(args);
            base.OnUpdate(args);
        }
        protected void OnUpdateLayout(UpdateArgs args)
        {
            var bounds = this.LocalBounds;
            var psize = Rect.ParentBounds;
            var margin = this.Margin;
            if (this.Meta.Anchor != AlignmentStyle.None)
            {
                var pos = this.Position;
                if (EnumMask.GetMask(this.Meta.Anchor, AlignmentStyle.MASK_LEFT))
                {
                    pos.X = margin.Left;
                }
                else if (EnumMask.GetMask(this.Meta.Anchor, AlignmentStyle.MASK_CENTER))
                {
                    pos.X = (psize.Width - bounds.Width) / 2f + (margin.Left - margin.Right);
                }
                else if (EnumMask.GetMask(this.Meta.Anchor, AlignmentStyle.MASK_RIGHT))
                {
                    pos.X = (psize.Width - bounds.Width) - margin.Right;
                }
                if (EnumMask.GetMask(this.Meta.Anchor, AlignmentStyle.MASK_TOP))
                {
                    pos.Y = margin.Top;
                }
                else if (EnumMask.GetMask(this.Meta.Anchor, AlignmentStyle.MASK_MIDDLE))
                {
                    pos.Y = (psize.Height - bounds.Height) / 2f + (margin.Top - margin.Bottom);
                }
                else if (EnumMask.GetMask(this.Meta.Anchor, AlignmentStyle.MASK_BOTTOM))
                {
                    pos.Y = (psize.Height - bounds.Height) - margin.Bottom;
                }
                this.Position = pos;
                //this.Meta.Position = this.Position;
            }
            else if (this.Meta.Dock != DockStyle.None)
            {
                switch (this.Meta.Dock)
                {
                    case DockStyle.Left:
                        this.Position = new Vector3(
                            margin.Left,
                            margin.Top, 0);
                        this.Rect.Bounds = new RectangleF(0, 0,
                            bounds.Width,
                            psize.Height - margin.CutHeight);
                        break;
                    case DockStyle.Top:
                        this.Position = new Vector3(
                            margin.Left,
                            margin.Top, 0);
                        this.Rect.Bounds = new RectangleF(0, 0,
                            psize.Width - margin.CutWidth,
                            bounds.Height);
                        break;
                    case DockStyle.Right:
                        this.Position = new Vector3(
                            psize.Width - bounds.Width - margin.Right,
                            margin.Top, 0);
                        this.Rect.Bounds = new RectangleF(0, 0,
                            bounds.Width,
                            psize.Height - margin.CutHeight);
                        break;
                    case DockStyle.Bottom:
                        this.Position = new Vector3(
                            margin.Left,
                            psize.Height - bounds.Height - margin.Bottom, 0);
                        this.Rect.Bounds = new RectangleF(0, 0,
                            psize.Width - margin.CutWidth,
                            bounds.Height);
                        break;
                    case DockStyle.Fill:
                        this.Position = new Vector3(
                            margin.Left,
                            margin.Top, 0);
                        this.Rect.Bounds = new RectangleF(0, 0,
                            psize.Width - margin.CutWidth,
                            psize.Height - margin.CutHeight);
                        break;

                    case DockStyle.TopLeft:
                        this.Position = new Vector3(
                            margin.Left,
                            margin.Top, 0);
                        break;
                    case DockStyle.TopRight:
                        this.Position = new Vector3(
                            psize.Width - bounds.Width - margin.Right,
                            margin.Top, 0);
                        break;
                    case DockStyle.BottomLeft:
                        this.Position = new Vector3(
                            margin.Left,
                            psize.Height - bounds.Height - margin.Bottom, 0);
                        break;
                    case DockStyle.BottomRight:
                        this.Position = new Vector3(
                            psize.Width - bounds.Width - margin.Right,
                            psize.Height - bounds.Height - margin.Bottom, 0);
                        break;
                    case DockStyle.None:
                    default:
                        break;
                }

            }

        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            DrawLayout(args);
        }
        protected override void OnDrawHUD(GraphicsArgs args)
        {
            //             if (IS_EDITOR)
            //             {
            //                 args.Graphics.SetColor(new Color(Color.RoyalBlue, 0.5f));
            //                 args.Graphics.DrawRect(LocalBounds);
            //             }
            base.OnDrawHUD(args);
        }
        protected virtual void DrawLayout(GraphicsArgs args)
        {
            var bounds = Rect.Bounds;
            if (Rect.Enable)
            {
                Layout?.Render(args.Graphics, bounds);
            }
            else
            {
                LayoutDisable?.Render(args.Graphics, bounds);
            }
        }

        #endregion Visit--------------------------------------------------------------------------------------------

        #region DataBinding-----------------------------------------------------------------------------------------

        private HashMap<string, object> bindingData = new HashMap<string, object>();
        public void BindData(string key, object data, bool deep)
        {
            this.bindingData.Put(key, data);
            DoBindData(key, data);
            if (deep)
            {
                if (this is UEContainerNode container)
                {
                    container.ForEachUEChilds((key, data, deep), static (st, node) =>
                    {
                        node.BindData(st.key, st.data, st.deep);
                        return false;
                    });
                }
            }
        }
        public bool TryGetBindData(string key, out object value)
        {
            return bindingData.TryGetValue(key, out value);
        }
        protected virtual void DoBindData(string key, object value) { }
        public virtual string GetTextValue() { return Meta.GetStringValue(); }

        #endregion DataBinding--------------------------------------------------------------------------------------

        #region Interaction-----------------------------------------------------------------------------------------

        public delegate void OnTextChangedHandler(UEComponentNode input, string newText, string oldText);
        public delegate void OnCheckedChangedHandler(UEComponentNode input, bool isChecked);
        public event OnTextChangedHandler OnTextChanged;
        public event OnCheckedChangedHandler OnCheckedChanged;
        protected void Invoke_TextChanged(string newText, string oldText)
        {
            OnTextChanged?.Invoke(this, newText, oldText);
        }
        protected void Invoke_CheckedChanged(bool isChecked)
        {
            OnCheckedChanged?.Invoke(this, isChecked);
        }
        #endregion Interaction--------------------------------------------------------------------------------------
    }

    //--------------------------------------------------------------------------------------------------------------
    public abstract class UEDisplayNode : UEComponentNode
    {
        public UEDisplayNode(UIFactory editor, UEComponentMeta e) : base(editor, e)
        {
        }
    }
    public abstract class UEDisplayNode<T> : UEDisplayNode where T : UEComponentMeta
    {
        new public T Meta { get => base.Meta as T; }
        public UEDisplayNode(UIFactory editor, T e) : base(editor, e)
        {
        }
    }

    //-----------------------------------------------------------------------------------------------------------

    public abstract class UEContainerNode : UEComponentNode
    {
        new public UEContainerMeta Meta { get => base.Meta as UEContainerMeta; }
        public UEContainerNode(UIFactory editor, UEContainerMeta e) : base(editor, e)
        {
        }
        protected override void DoDecodeChilds()
        {
            var editor = this.Editor;
            if (this.Meta is UEContainerMeta e && e.Childs != null)
            {
                int len = e.Childs.Count;
                for (int i = 0; i < len; ++i)
                {
                    var child = e.Childs[i];
                    var cui = editor.CreateUI(child);
                    if (cui != null)
                    {
                        cui.Decode();
                        this.AddChild(cui);
                    }
                }
            }
        }
        protected override void DoEncodeChilds()
        {
            if (this.Meta is UEContainerMeta e)
            {
                this.Meta.Childs = new System.Collections.Generic.List<UEComponentMeta>(NumChildren);
                ForEachUEChilds(this, (p, c) =>
                {
                    this.Meta.Childs.Add(c.Encode());
                    return false;
                });
            }
        }

        public async Task<bool> ForEachUEChildsAsync<ST>(ST st, ForEachPredicateAsync<ST, UEComponentNode> action, bool deep = false)
        {
            using (var list = ObjectPool.AllocList<UEComponentNode>())
            {
                int i;
                int length = NumChildren;
                // 广度遍历.
                for (i = 0; i < length; ++i)
                {
                    var child = this.GetChildAt(i);
                    if (child is UEComponentNode childui)
                    {
                        list.Add(childui);
                    }
                }
                foreach (var child in list)
                {
                    if (await action(st, child))
                    {
                        return true;
                    }
                }
                if (deep)
                {
                    foreach (var child in list)
                    {
                        if (child is UEContainerNode subContainer)
                        {
                            if (await subContainer.ForEachUEChildsAsync(st, action, deep))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public bool ForEachUEChilds<ST>(ST st, ForEachPredicate<ST, UEComponentNode> action, bool deep = false)
        {
            using (var list = ObjectPool.AllocList<UEComponentNode>())
            {
                int i;
                int length = NumChildren;
                // 广度遍历.
                for (i = 0; i < length; ++i)
                {
                    var child = this.GetChildAt(i);
                    if (child is UEComponentNode childui)
                    {
                        list.Add(childui);
                    }
                }
                foreach (var child in list)
                {
                    if (action(st, child))
                    {
                        return true;
                    }
                }
                if (deep)
                {
                    foreach (var child in list)
                    {
                        if (child is UEContainerNode subContainer)
                        {
                            if (subContainer.ForEachUEChilds(st, action, deep))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public UEComponentNode FindUINode(string edit_name)
        {
            int i;
            int length = NumChildren;
            // 广度遍历.
            for (i = length - 1; i >= 0; --i)
            {
                var child = this.GetChildAt(i);
                if (child is UEComponentNode childui)
                {
                    if (childui.EditName == edit_name)
                    {
                        return childui;
                    }
                }
            }
            // 深度遍历.
            for (i = length - 1; i >= 0; --i)
            {
                var child = this.GetChildAt(i);
                if (child is UEContainerNode childContainer)
                {
                    var uicc = childContainer.FindUINode(edit_name);
                    if (uicc != null)
                    {
                        return uicc;
                    }
                }
            }
            return null;
        }

        protected override void OnRefreshFromEditor()
        {
            base.OnRefreshFromEditor();
            ForEachUEChilds(this, static (st, child) =>
            {
                child.RefreshFromEditor();
                return false;
            });
        }

    }

    public abstract class UEContainerNode<T> : UEContainerNode where T : UEContainerMeta
    {
        new public T Meta { get => base.Meta as T; }
        public UEContainerNode(UIFactory editor, T e) : base(editor, e)
        {
        }
    }
    //-----------------------------------------------------------------------------------------------------------

}

