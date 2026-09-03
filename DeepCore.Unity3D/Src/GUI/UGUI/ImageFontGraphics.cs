using DeepCore.GUI.Cell;
using DeepCore.GUI.Gemo;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{    //---------------------------------------------------------------------------------------------------

    public partial class ImageFontGraphics : UnityEngine.UI.MaskableGraphic, ITextComponent
    {
        private DisplayNode mBinding;

        [SerializeField]
        private string m_Text;
        [SerializeField]
        private GUI.Data.TextAnchor m_Anchor = GUI.Data.TextAnchor.L_T;
        [SerializeField]
        private Color m_FontColor = Color.white;
        [SerializeField]
        private Vector2 m_TextOffset = Vector3.zero;
        private DeepCore.Unity3D.Impl.UnityImage mSrc;
        private CPJAtlas mAtlas;
        private Texture2D mTexture;
        private Vector2 mPreferredSize = Vector2.zero;
        private Rect mLastCaretPosition = new Rect(0, 0, 0, 0);


        public override Texture mainTexture { get { return mTexture; } }

        public DisplayNode Binding { get { return mBinding; } }

        public CPJAtlas Atlas
        {
            get { return mAtlas; }
            set
            {
                mAtlas = value;
                mSrc = mAtlas.GetTile(0) as DeepCore.Unity3D.Impl.UnityImage;
                if (mSrc != null)
                {
                    this.mTexture = mSrc.Texture2D;
                    this.material = mSrc.TextureMaterial;
					base.SetAllDirty();
                }
                
            }
        }

        public string Text
        {
            get { return m_Text; }
            set
            {
                if (value == null) value = "";
                if (value != m_Text)
                {
                    this.m_Text = value;
                    base.SetVerticesDirty();
                    if (mAtlas != null)
                    {
                        mPreferredSize = GetImageFontPreferredSize(mAtlas, m_Text);
                    }
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
        public int FontSize
        {
            get { return 1; }
            set { }
        }
        public GUI.Data.FontStyle Style
        {
            get { return GUI.Data.FontStyle.Normal; }
            set { }
        }
        public Color FontColor
        {
            get { return m_FontColor; }
            set
            {
                if (m_FontColor != value)
                {
                    m_FontColor = value;
                    value.a = base.color.a * value.a;
                    base.color = value;
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
        public bool IsUnderline
        {
            get { return false; }
            set { }
        }
        public void SetBorder(Color bc, Vector2 distance)
        {
            //Do nothing.
        }
        public void SetShadow(Color bc, Vector2 distance)
        {

        }
        public void SetFont(UnityEngine.Font font)
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


        protected override void Start()
        {
            this.mBinding = DisplayNode.AsDisplayNode(gameObject);
            base.Start();
        }

        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (mAtlas != null)
            {
                //mPreferredSize = GetImageFontPreferredSize(mAtlas, m_Text);
                mLastCaretPosition.size = UIFactory.Instance.DefaultCaretSize;
                mLastCaretPosition.y = 0;
                mLastCaretPosition.x = 0;
                if (mSrc != null)
                {
                    float sw = 0, sh = 0;
                    Rect bounds = new Rect(0, 0, mPreferredSize.x, mPreferredSize.y);
                    UIUtils.AdjustAnchor(m_Anchor, rectTransform.sizeDelta, ref bounds);
                    Vector2 offset = bounds.position;
                    offset.x += m_TextOffset.x;
                    offset.y += m_TextOffset.y;
                    for (int i = 0; i < m_Text.Length; i++)
                    {
                        char ch = m_Text[i];
                        int index = mAtlas.GetIndexByKey(ch.ToString());
                        if (index >= 0)
                        {
                            Rectangle2D rect = mAtlas.GetAtlasRegion(index);
                            if (rect != null)
                            {
                                Rect offsetrect = new Rect(0, 0, rect.width, rect.height);
                                UIUtils.AdjustAnchor(m_Anchor, rectTransform.sizeDelta, ref offsetrect);
                                UIUtils.CreateVertexQuard(mSrc, color, rect.x , rect.y, sw + offset.x, offset.y + offsetrect.y, rect.width, rect.height, vh);
                                sw += rect.width;
                                sh = Math.Max(rect.height, sh);
                            }
                        }
                    }
                    mLastCaretPosition.x = sw;
                    mLastCaretPosition.height = sh;
                }
            }
            else
            {
                mPreferredSize = Vector3.zero;
                mLastCaretPosition.size = UIFactory.Instance.DefaultCaretSize;
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

        public static Vector2 GetImageFontPreferredSize(CPJAtlas atlas, string text)
        {
            float sw = 0, sh = 0;
            DeepCore.Unity3D.Impl.UnityImage src = atlas.GetTile(0) as DeepCore.Unity3D.Impl.UnityImage;
            if (src != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char ch = text[i];
                    int index = atlas.GetIndexByKey(ch.ToString());
                    if (index >= 0)
                    {
                        Vector2 size = new Vector2(atlas.getWidth(index), atlas.getHeight(index));
                        if (size.x > 0)
                        {
                            sw += size.x;
                            sh = Math.Max(size.y, sh);
                        }
                    }
                }
            }
            return new Vector2(sw, sh);
        }

        private void ResetTexture()
        {
            if (mSrc != null && mSrc.Texture2D != mTexture)
            {
                this.mTexture = mSrc.Texture2D;
                this.material = mSrc.TextureMaterial;
                base.SetAllDirty();
            }
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            ResetTexture();
        }

        void Update()
        {
            ResetTexture();
        }

    }



}
