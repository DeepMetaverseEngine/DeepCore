using DeepCore.GUI.Display;
using DeepCore.GUI.Display.Text;
using UnityEngine;

namespace DeepCore.Unity3D.UGUI
{
    //----------------------------------------------------------------------------------------
    /// <summary>
    /// 可滚动富文本
    /// </summary>
    public partial class RichTextBox : ScrollablePanel, ITextComponent
    {
        private readonly UGUIRichTextLayer mLayer;
        private bool mIsNeedScroll;
        private bool mScrollable = true;
        protected readonly bool mUseBitmapFont;

        public DisplayNode Binding { get { return mContent; } }

        public UGUIRichTextLayer RichTextLayer
        {
            get { return mLayer; }
        }
        public AttributedString AText
        {
            get { return mLayer.GetText(); }
            set
            {
                if (IsDispose) return;
                mLayer.SetString(value);
            }
        }
        public string XmlText
        {
            set
            {
                if (IsDispose) return;
                mLayer.XmlText = value;
            }
        }
        public string UnityRichText
        {
            set
            {
                if (IsDispose) return;
                this.XmlText = UIUtils.UnityRichTextToXmlText(value);
            }
        }
        public bool IsNeedScroll
        {
            get { return mIsNeedScroll; }
        }
        public bool Scrollable
        {
            get { return mScrollable; }
            set { this.mScrollable = value; }
        }



        public RichTextBox(string name = "", bool use_bitmap = false) : base(name)
        {
            this.mUseBitmapFont = use_bitmap;
            this.mLayer = UIFactory.Instance.CreateRichTextLayer(mContent, mUseBitmapFont);
            this.mMaskScrollRect.enabled = false;
            this.mMaskScrollRect.horizontal = false;
            this.mMaskScrollRect.vertical = true;
            this.mMaskScrollRect.AutoUpdateContentSize = false;
            this.mRectMask.enabled = true;
            this.mMaskGraphics.enabled = false;
            //             this.mMask.enabled = false;
            this.mContent.EnableChildren = false;
        }
        public bool TestClick(Vector2 point, out RichTextClickInfo info)
        {
            Rect scroll_rect = this.ScrollRect2D;
            return RichTextLayer.Click(point.x + scroll_rect.x, point.y + scroll_rect.y, out info);
        }

        protected override void OnDispose()
        {
            mLayer.Dispose();
            base.OnDispose();
        }
        protected override void OnUpdateContentSize()
        {
            Vector2 bsize = this.Size2D;
            this.mLayer.SetWidth(bsize.x);
            Rect scroll_rect = this.ScrollRect2D;
            this.mContent.Size2D = new Vector2(bsize.x, mLayer.ContentHeight);
            this.mIsNeedScroll = mScrollable && (mLayer.ContentHeight > bsize.y);
            this.mMaskScrollRect.enabled = mIsNeedScroll;
            //this.mRectMask.enabled = mIsNeedScroll;
            this.mMaskGraphics.enabled = mIsNeedScroll;
            this.EnableChildren = mIsNeedScroll;
            if (!mIsNeedScroll)
            {
                switch (this.Anchor)
                {
                    case GUI.Data.TextAnchor.L_T:
                    case GUI.Data.TextAnchor.C_T:
                    case GUI.Data.TextAnchor.R_T:
                        break;
                    case GUI.Data.TextAnchor.L_C:
                    case GUI.Data.TextAnchor.C_C:
                    case GUI.Data.TextAnchor.R_C:
                        scroll_rect.y = -(bsize.y - this.mLayer.ContentHeight) * 0.5f;
                        this.Container.Position2D = -scroll_rect.position;
                        break;
                    case GUI.Data.TextAnchor.L_B:
                    case GUI.Data.TextAnchor.C_B:
                    case GUI.Data.TextAnchor.R_B:
                        scroll_rect.y = -(bsize.y - this.mLayer.ContentHeight);
                        this.Container.Position2D = -scroll_rect.position;
                        break;
                }
            }
            this.mLayer.Render(NullGraphics.NULL, 0, 0, bsize.x, bsize.y, scroll_rect.x, scroll_rect.y, Mathf.FloorToInt(Time.deltaTime * 1000));
        }
        protected override void OnSizeChanged(Vector2 size)
        {
            base.OnSizeChanged(size);
            this.mLayer.SetWidth(size.x);
        }

        #region ITextComponent

        public string Text
        {
            get { return mLayer.Text; }
            set
            {
                if (IsDispose) return;
                mLayer.Text = value;
            }
        }
        public int FontSize
        {
            get { return mLayer.FontSize; }
            set { mLayer.FontSize = value; }
        }
        public UnityEngine.Color FontColor
        {
            get { return mLayer.FontColor; }
            set { mLayer.FontColor = value; }
        }
        public GUI.Data.FontStyle Style
        {
            get { return mLayer.Style; }
            set { mLayer.Style = value; }
        }
        public bool IsUnderline
        {
            get { return mLayer.IsUnderline; }
            set { mLayer.IsUnderline = value; }
        }
        public Vector2 TextOffset
        {
            get { return mLayer.TextOffset; }
            set { mLayer.TextOffset = value; }
        }
        public GUI.Data.TextAnchor Anchor
        {
            get { return mLayer.Anchor; }
            set { mLayer.Anchor = value; }
        }
        public Vector2 PreferredSize
        {
            get { return mLayer.PreferredSize; }
        }
        public Rect LastCaretPosition
        {
            get
            {
                var bounds = mLayer.LastCaretPosition;
                bounds.position += mContent.Position2D;
                return bounds;
            }
        }
        public void SetBorder(UnityEngine.Color bc, Vector2 distance)
        {
            mLayer.SetBorder(bc, distance);
        }
        public void SetShadow(UnityEngine.Color bc, Vector2 distance)
        {
            mLayer.SetShadow(bc, distance);
        }
        public void SetFont(Font font)
        {
            mLayer.SetFont(font);
        }

        #endregion
    }

    //----------------------------------------------------------------------------------------
    /// <summary>
    /// 不可滚动富文本
    /// </summary>
    public class RichTextPan : DisplayText
    {
        private readonly UGUIRichTextLayer mLayer;
        private bool mScrollToCaret = false;
        private Rect mScrollRect = new Rect();
        protected readonly bool mUseBitmapFont;

        public UGUIRichTextLayer RichTextLayer
        {
            get { return mLayer; }
        }
        public AttributedString AText
        {
            get { return mLayer.GetText(); }
            set { mLayer.SetString(value); }
        }
        public string XmlText
        {
            set { mLayer.XmlText = value; }
        }
        public string UnityRichText
        {
            set { this.XmlText = UIUtils.UnityRichTextToXmlText(value); }
        }
        public bool AutoScrollToCaret
        {
            get { return mScrollToCaret; }
            set { mScrollToCaret = value; }
        }

        public Vector2 ContentSize
        {
            get
            {
                return new Vector2(mLayer.ContentWidth, mLayer.ContentHeight);
            }
        }

        public RichTextPan(bool use_bitmap, string name = "") : base(name)
        {
            this.mUseBitmapFont = use_bitmap;
            this.mLayer = UIFactory.Instance.CreateRichTextLayer(this, use_bitmap);
            this.mLayer.SetWidth(this.Width);
        }

        public RichTextPan(string name = "") : this(false, name)
        {
        }
        public bool TestClick(Vector2 point, out RichTextClickInfo info)
        {
            return RichTextLayer.Click(point.x, point.y, out info);
        }
        protected override void OnDispose()
        {
            mLayer.Dispose();
            base.OnDispose();
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();

            this.mScrollRect.size = this.Size2D;
            this.mScrollRect.position = Vector2.zero;
            this.mLayer.SetWidth(mScrollRect.width);
            if (mScrollToCaret)
            {
                // todo
                float dh = mLayer.ContentHeight - Binding.Height;
                if (dh > 0)
                {
                    mScrollRect.y += dh;
                }
            }
            else
            {
                switch (this.Anchor)
                {
                    case GUI.Data.TextAnchor.L_T:
                    case GUI.Data.TextAnchor.C_T:
                    case GUI.Data.TextAnchor.R_T:
                        break;
                    case GUI.Data.TextAnchor.L_C:
                    case GUI.Data.TextAnchor.C_C:
                    case GUI.Data.TextAnchor.R_C:
                        mScrollRect.y = -(mScrollRect.height - this.mLayer.ContentHeight) * 0.5f;
                        break;
                    case GUI.Data.TextAnchor.L_B:
                    case GUI.Data.TextAnchor.C_B:
                    case GUI.Data.TextAnchor.R_B:
                        mScrollRect.y = -(mScrollRect.height - this.mLayer.ContentHeight);
                        break;
                }
            }
            this.mLayer.Render(NullGraphics.NULL, -mScrollRect.x, -mScrollRect.y, mScrollRect.width, mScrollRect.height, mScrollRect.x, mScrollRect.y, Mathf.FloorToInt(Time.deltaTime * 1000));
        }
        #region ITextComponent

        public override string Text
        {
            get { return mLayer.Text; }
            set { mLayer.Text = value; }
        }
        public override int FontSize
        {
            get { return mLayer.FontSize; }
            set { mLayer.FontSize = value; }
        }
        public override UnityEngine.Color FontColor
        {
            get { return mLayer.FontColor; }
            set { mLayer.FontColor = value; }
        }
        public override GUI.Data.FontStyle Style
        {
            get { return mLayer.Style; }
            set { mLayer.Style = value; }
        }
        public override bool IsUnderline
        {
            get { return mLayer.IsUnderline; }
            set { mLayer.IsUnderline = value; }
        }
        public override Vector2 TextOffset
        {
            get { return mLayer.TextOffset; }
            set { mLayer.TextOffset = value; }
        }
        public override GUI.Data.TextAnchor Anchor
        {
            get { return mLayer.Anchor; }
            set { mLayer.Anchor = value; }
        }
        public override Vector2 PreferredSize
        {
            get { return mLayer.PreferredSize; }
        }
        public override Rect LastCaretPosition
        {
            get
            {
                var bounds = mLayer.LastCaretPosition;
                bounds.position -= mScrollRect.position;
                return bounds;
            }
        }
        public override void SetBorder(UnityEngine.Color bc, Vector2 distance)
        {
            mLayer.SetBorder(bc, distance);
        }
        public override void SetShadow(UnityEngine.Color bc, Vector2 distance)
        {
            mLayer.SetShadow(bc, distance);
        }
        public override void SetFont(Font font)
        {
            mLayer.SetFont(font);
        }

        #endregion
    }

    //----------------------------------------------------------------------------------------
    class NullGraphics : DeepCore.GUI.Display.Graphics
    {
        public static NullGraphics NULL = new NullGraphics();

        public override void BeginImage(Image image)
        {
            throw new System.NotImplementedException();
        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public override void DrawArc(in Geometry.RectangleF rect, float startAngle, float arcAngle)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawImage(in Geometry.Vector2 pos)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawImage(in Geometry.Vector2 pos, Anchor anchor)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawImageEllipse(in Geometry.RectangleF rect, float startAngle, float endAngle)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawImageRegion(in Geometry.RectangleF srcRect, Trans tx, in Geometry.Vector2 pos)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawImageRegion(in Geometry.RectangleF srcRect, in Geometry.RectangleF dstRect)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawImageTrans(in Geometry.Vector2 pos, Trans trans)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawImageZoom(in Geometry.RectangleF zoom)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawLine(in Geometry.Line2 line)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawPolygon(Geometry.Vector2[] points)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawRect(in Geometry.RectangleF rect)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawRoundRect(in Geometry.RectangleF rect, float rx, float ry)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawString(string text, in Geometry.Vector2 pos, Anchor anchor)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawString(string text, in Geometry.RectangleF region, ImageAnchor anchor)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawTextLayer(TextLayer text, in Geometry.Vector2 pos, Anchor anchor)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawTextLayer(TextLayer text, in Geometry.RectangleF rect, ImageAnchor anchor)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawVertex(VertexBuffer vertex)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawVertex(VertexBuffer vertex, int[] indices, VertexTopology mode)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawVertexSequence(VertexBuffer vertex, VertexTopology mode)
        {
            throw new System.NotImplementedException();
        }

        public override void FillArc(in Geometry.RectangleF rect, float startAngle, float arcAngle)
        {
            throw new System.NotImplementedException();
        }

        public override void FillPolygon(Geometry.Vector2[] points)
        {
            throw new System.NotImplementedException();
        }

        public override void FillRect(in Geometry.RectangleF rect)
        {
            throw new System.NotImplementedException();
        }

        public override void FillRect4Color(in Geometry.RectangleF rect, uint[] rgba)
        {
            throw new System.NotImplementedException();
        }

        public override void FillRoundRect(in Geometry.RectangleF rect, float rx, float ry)
        {
            throw new System.NotImplementedException();
        }

        public override Geometry.Vector2 MeasureString(string text)
        {
            throw new System.NotImplementedException();
        }

        public override Geometry.Vector2 MeasureString(string text, int width)
        {
            throw new System.NotImplementedException();
        }

        public override void MultiplyTransform(in Geometry.Matrix tras)
        {
            throw new System.NotImplementedException();
        }

        public override void PopTransform()
        {
            throw new System.NotImplementedException();
        }

        public override void PushTransform()
        {
            throw new System.NotImplementedException();
        }

        public override void Rotate(float angle)
        {
            throw new System.NotImplementedException();
        }

        public override void Scale(in Geometry.Vector2 scale)
        {
            throw new System.NotImplementedException();
        }

        public override void Translate(in Geometry.Vector2 pos)
        {
            throw new System.NotImplementedException();
        }

        protected override void InternalSetBlend(Blend blen)
        {
            throw new System.NotImplementedException();
        }

        protected override void InternalSetClip(in Geometry.RectangleF rect)
        {
            throw new System.NotImplementedException();
        }

        protected override void InternalSetColor(in GUI.Display.Color color, float alpha)
        {
            throw new System.NotImplementedException();
        }

        protected override void InternalSetFont(float size, GUI.Display.FontStyle style)
        {
            throw new System.NotImplementedException();
        }

        protected override void InternalSetPen(float size, DashStyle style)
        {
            throw new System.NotImplementedException();
        }
    }
}
