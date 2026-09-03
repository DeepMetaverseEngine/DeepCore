using DeepCore.GUI.Data;
using DeepCore.Unity3D.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public partial class UEScrollPan : UIComponent
    {
        protected ScrollablePanel mMaskPanel;
        protected float mBorderSize;

        public UEScrollPan()
        {

        }

        protected virtual ScrollablePanel CreateScrollablePanel(string name)
        {
            return new ScrollablePanel(name);
        }

        public DisplayNode ContainerPanel { get { return mMaskPanel.Container; } }
        public ScrollablePanel Scrollable { get { return mMaskPanel; } }

        public Rect ScrollRect2D { get { return mMaskPanel.ScrollRect2D; } }
        public bool ShowSlider
        {
            get
            {
                if (mMaskPanel is ScrollablePanel)
                {
                    return (mMaskPanel as ScrollablePanel).ShowSlider;
                }
                return false;
            }
            set
            {
                if (mMaskPanel is ScrollablePanel)
                {
                    (mMaskPanel as ScrollablePanel).ShowSlider = value;
                }
            }
        }
        public Rect ViewRect2D
        {
            get
            {
                Rect rect = this.Bounds2D;
                rect.x = mBorderSize;
                rect.y = mBorderSize;
                rect.width -= mBorderSize * 2;
                rect.height -= mBorderSize * 2;
                return rect;
            }
        }

        public void SetScrollBar(UILayout layout_scroll_h, UILayout layout_scroll_v)
        {
            UIComponent scroll_v = null;
            UIComponent scroll_h = null;
            if (layout_scroll_v != null)
            {
                scroll_v = new UIComponent("scroll_v");
                scroll_v.Layout = layout_scroll_v;
                scroll_v.Size2D = layout_scroll_v.PreferredSize;
            }
            if (layout_scroll_h != null)
            {
                scroll_h = new UIComponent("scroll_h");
                scroll_h.Layout = layout_scroll_h;
                scroll_h.Size2D = layout_scroll_h.PreferredSize;
            }
            if (mMaskPanel is ScrollablePanel)
            {
                (mMaskPanel as ScrollablePanel).SetScrollBarPair(scroll_h, scroll_v);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Rect view_rect = this.ViewRect2D;
            this.mMaskPanel.Bounds2D = view_rect;
        }

        protected override void AddEditorComopnent(UIComponent c)
        {
            mMaskPanel.Container.AddChild(c);
        }

        protected override void DecodeBegin(UIEditor.Decoder editor, UIComponentMeta e)
        {
            this.mMaskPanel = CreateScrollablePanel("scrollable");
            if (this.mMaskPanel != null)
            {
                this.AddChild(mMaskPanel);
            }

            base.DecodeBegin(editor, e);
            this.Decode_ScrollPan(editor, e as UEScrollPanMeta);
        }

        private void Decode_ScrollPan(UIEditor.Decoder editor, UEScrollPanMeta e)
        {
            this.mMaskPanel.Scroll.movementType = e.EnableElasticity ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
            this.mMaskPanel.Scroll.horizontal = e.EnableScrollH;
            this.mMaskPanel.Scroll.vertical = e.EnableScrollV;

            this.mBorderSize = e.BorderSize;
            this.ShowSlider = e.ShowSlider;

            if (mMaskPanel is ScrollablePanel)
            {
                (mMaskPanel as ScrollablePanel).ScrollFadeTimeMaxMS = e.scroll_fade_time_max * 30;
            }

            this.SetScrollBar(
                editor.CreateLayout(e.layout_scroll_h),
                editor.CreateLayout(e.layout_scroll_v));
        }

        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            this.EnableChildren = true;

            Rect view_rect = this.ViewRect2D;
            this.mMaskPanel.Bounds2D = view_rect;

            if (UIEditorMeta.UIEditorRunTime)
            {
                this.Enable = e.Enable;
                this.EnableChildren = e.EnableChilds;
                this.IsInteractive = true;
            }
        }

    }
}
