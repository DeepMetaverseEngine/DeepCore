using DeepCore.GUI.Data;
using DeepCore.Unity3D.Impl;
using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{
    //---------------------------------------------------------------------------------------------------

    public partial class BitmapTextGraphics : UnityEngine.UI.MaskableGraphic, ITextComponent
    {
        private readonly UnityTextLayer mTextLayer;
        private DisplayNode mBinding;

        private UnityImage mSrc;
        private Texture2D mTexture;
        private bool m_IsRefresh = true;
        private Vector2 mPreferredSize = Vector2.zero;
        private Rect mLastCaretPosition = new Rect(0, 0, 0, 0);
        private bool mScrollToCaret = false;

        [SerializeField]
        private string m_Text;

        [SerializeField]
        private GUI.Data.TextAnchor m_Anchor = GUI.Data.TextAnchor.L_T;
        [SerializeField]
        private Vector2 m_TextOffset = Vector3.zero;

        [SerializeField]
        private int m_FontSize;
        [SerializeField]
        private GUI.Data.FontStyle m_FontStyle = GUI.Data.FontStyle.Normal;
        [SerializeField]
        private Color m_BorderColor = Color.black;
        [SerializeField]
        private Color m_FontColor = Color.white;
        [SerializeField]
        private TextBorderCount m_BorderTimes = TextBorderCount.Null;
        [SerializeField]
        private bool m_IsUnderline = false;

        public override Texture mainTexture { get { return mTexture; } }

        public DisplayNode Binding { get { return mBinding; } }

        public string Text
        {
            get { return m_Text; }
            set
            {
                if (value == null) value = "";
                if (value != m_Text)
                {
                    m_Text = value;
                    m_IsRefresh = true;
                }
            }
        }
        public int FontSize
        {
            get { return m_FontSize; }
            set
            {
                if (value != m_FontSize)
                {
                    m_FontSize = value;
                    m_IsRefresh = true;
                }
            }
        }
        public UnityEngine.Color FontColor
        {
            get { return m_FontColor; }
            set
            {
                if (value != m_FontColor)
                {
                    m_FontColor = value;
                    m_IsRefresh = true;
                }
            }
        }
        public GUI.Data.FontStyle Style
        {
            get { return m_FontStyle; }
            set
            {
                if (value != m_FontStyle)
                {
                    m_FontStyle = value;
                    m_IsRefresh = true;
                }
            }
        }
        public bool IsUnderline
        {
            get { return m_IsUnderline; }
            set
            {
                if (value != m_IsUnderline)
                {
                    m_IsUnderline = value;
                    m_IsRefresh = true;
                }
            }
        }
        public GUI.Display.FontStyle LayerFontStyle
        {
            get { return mTextLayer.TextFontStyle; }
            set
            {
                if (value != mTextLayer.TextFontStyle)
                {
                    m_FontStyle = UIUtils.ToFontStyle(value, out m_IsUnderline);
                    m_IsRefresh = true;
                }
            }
        }
        public GUI.Data.TextBorderCount BorderTime
        {
            get { return m_BorderTimes; }
            set
            {
                if (value != m_BorderTimes)
                {
                    m_BorderTimes = value;
                    m_IsRefresh = true;
                }
            }
        }
        public Color BorderColor
        {
            get { return m_BorderColor; }
            set
            {
                if (value != m_BorderColor)
                {
                    m_BorderColor = value;
                    m_IsRefresh = true;
                }
            }
        }
        public GUI.Data.TextAnchor Anchor
        {
            get { return m_Anchor; }
            set
            {
                if (value != m_Anchor)
                {
                    this.m_Anchor = value;
                    base.SetVerticesDirty();
                }
            }
        }
        public Vector2 TextOffset
        {
            get { return m_TextOffset; }
            set
            {
                if (value != m_TextOffset)
                {
                    this.m_TextOffset = value;
                    base.SetVerticesDirty();
                }
            }
        }
        public void SetBorder(Color bc, Vector2 distance)
        {
            if (m_BorderTimes != TextBorderCount.Border || bc != m_BorderColor)
            {
                m_BorderColor = bc;
                m_BorderTimes = TextBorderCount.Border;
                m_IsRefresh = true;
            }
        }
        public void SetShadow(Color bc, Vector2 distance)
        {
            var value = UIUtils.ToTextShadowCount(distance);
            if (m_BorderTimes != value || bc != m_BorderColor)
            {
                m_BorderColor = bc;
                m_BorderTimes = value;
                m_IsRefresh = true;
            }
        }
        public void SetFont(Font font)
        {

        }

        public virtual Vector2 PreferredSize
        {
            get { return mPreferredSize; }
        }
        public virtual Rect LastCaretPosition
        {
            get { return mLastCaretPosition; }
        }
        public virtual bool AutoScrollToCaret
        {
            get { return mScrollToCaret; }
            set
            {
                if (mScrollToCaret != value)
                {
                    mScrollToCaret = value;
                    this.SetVerticesDirty();
                }
            }
        }


        public BitmapTextGraphics()
        {
            this.m_Text = "";
            this.m_FontSize = 16;

            this.mTextLayer = new UnityTextLayer("", GUI.Display.FontStyle.STYLE_PLAIN, m_FontSize);
            this.mTextLayer.FontColor = GUI.Display.Color.COLOR_WHITE;
            this.mTextLayer.BorderColor = GUI.Display.Color.COLOR_BLACK;
            this.mTextLayer.BorderTime = 0;

            this.material = null;
        }

        protected override void OnDestroy()
        {
            mTextLayer.Dispose();
            base.OnDestroy();
        }
        public void Apply()
        {
            m_IsRefresh = true;
        }
        private void Refresh()
        {
            mTextLayer.Text = m_Text;
            mTextLayer.FontSize = m_FontSize;
            mTextLayer.TextFontStyle = UIUtils.ToTextLayerFontStyle(m_FontStyle, m_IsUnderline);
            mTextLayer.FontColor = UIUtils.Color_To_UInt32_RGBA(m_FontColor);
            mTextLayer.BorderColor = UIUtils.Color_To_UInt32_RGBA(m_BorderColor);
            mTextLayer.BorderTime = (int)m_BorderTimes;

            this.mSrc = this.mTextLayer.GetBuffer() as UnityImage;
            if (mSrc != null)
            {
                this.mPreferredSize = new Vector2(this.mSrc.Width, this.mSrc.Height);
                this.mTexture = this.mSrc.Texture2D;
                this.material = this.mSrc.TextureMaterial;
            }
            else
            {
                this.mPreferredSize = Vector2.zero;
                this.mTexture = null;
                this.material = null;
            }
            SetAllDirty();
        }
        protected override void Start()
        {
            this.mBinding = DisplayNode.AsDisplayNode(gameObject);
            base.Start();
            if (m_IsRefresh)
            {
                m_IsRefresh = false;
                Refresh();
            }
        }

        protected virtual void Update()
        {
            if (mSrc != null && mSrc.Texture2D != mTexture)
            {
                m_IsRefresh = true;
            }
            if (m_IsRefresh)
            {
                m_IsRefresh = false;
                Refresh();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect bounds = new Rect(0, 0, mPreferredSize.x, mPreferredSize.y);
            UIUtils.AdjustAnchor(m_Anchor, rectTransform.sizeDelta, ref bounds);
            bounds.position += m_TextOffset;
            if (mSrc != null)
            {
                Vector2 src_pos = new Vector2(0, 0);
                if (mScrollToCaret)
                {
                    float dw = bounds.width - Binding.Width;
                    if (dw > 0)
                    {
                        src_pos.x += dw;
                        bounds.width -= dw;
                    }
                }
                UIUtils.CreateVertexQuard(this.mSrc, color, src_pos.x, src_pos.y, bounds.x, bounds.y, bounds.width, bounds.height, vh);
                this.mLastCaretPosition.height = bounds.height;
                this.mLastCaretPosition.width = UIFactory.Instance.DefaultCaretSize.x;
                this.mLastCaretPosition.y = 0;
                this.mLastCaretPosition.x = bounds.x + bounds.width;
            }
            else
            {
                this.mLastCaretPosition.y = 0;
                this.mLastCaretPosition.x = bounds.x;
                this.mLastCaretPosition.size = UIFactory.Instance.DefaultCaretSize;
            }
        }


        public virtual void CalculateLayoutInputHorizontal()
        {
        }
        public virtual void CalculateLayoutInputVertical()
        {
        }
        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            if (mBinding.Enable && mBinding.EnableTouchInParents)
            {
                return base.Raycast(sp, eventCamera);
            }
            return false;
        }
	

    }


}
