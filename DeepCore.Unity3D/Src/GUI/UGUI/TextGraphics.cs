using DeepCore.Unity3D.Impl;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BaseVertexEffect = UnityEngine.UI.BaseMeshEffect;


namespace DeepCore.Unity3D.UGUI
{    //---------------------------------------------------------------------------------------------------

    public partial class TextGraphics : Text, ITextComponent
    {
        private static Material s_DefaultImageMaterial;
        public static Material DefaultImageMaterial
        {
            get
            {
                if (s_DefaultImageMaterial == null)
                {
                    s_DefaultImageMaterial = new Material(UnityShaders.MFUGUI_TextGray_Shader);
                    s_DefaultImageMaterial.mainTextureScale = new Vector2(1, 1);
                    s_DefaultImageMaterial.color = UnityEngine.Color.white;
                    //ret.SetFloat("_Gray", 1);
                    s_DefaultImageMaterial.SetPass(0);
                }
                return s_DefaultImageMaterial;
            }
        }
        public static string DefaultUnderlineChar = "_";
        public static UnderlineStyle DefaultUnderlineType = UnderlineStyle.Fill;

        public enum UnderlineStyle
        {
            Sequence = 0,
            Fill = 1,
            Sliced = 2,
        }

        //--------------------------------------------------------------------------------------------------
        private DisplayNode mBinding;
        private BaseVertexEffect mTextEffect;
        private GUI.Data.TextAnchor mTextAnchor = GUI.Data.TextAnchor.L_T;
        private static readonly UIVertex[] m_TempVerts = new UIVertex[4];

        [SerializeField]
        private Vector2 m_TextOffset = new Vector2();
        [SerializeField]
        private Color m_FontColor = Color.white;
        [SerializeField]
        private bool m_IsUnderline = false;
        [SerializeField]
        private string m_UnderlineChar = DefaultUnderlineChar;
        [SerializeField]
        private UnderlineStyle m_UnderlineType = DefaultUnderlineType;


        public DisplayNode Binding { get { return mBinding; } }

        public BaseVertexEffect TextEffect
        {
            get { return mTextEffect; }
        }

        public string Text
        {
            get { return this.text; }
            set
            {
                if (value == null) value = "";
                if (value != this.text)
                {
                    this.text = value;
                }
            }
        }
        public int FontSize
        {
            get { return this.fontSize; }
            set { this.fontSize = value; }
        }
        public UnityEngine.Color FontColor
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
                    m_TextOffset = value;
                    base.SetVerticesDirty();
                }
            }
        }
        public GUI.Data.TextAnchor Anchor
        {
            get { return mTextAnchor; }
            set
            {
                this.mTextAnchor = value;
                this.alignment = UIUtils.ToUnityAnchor(value);
            }
        }
        public GUI.Data.FontStyle Style
        {
            get { return (GUI.Data.FontStyle)base.fontStyle; }
            set { base.fontStyle = (UnityEngine.FontStyle)value; }
        }
        public bool IsUnderline
        {
            get { return m_IsUnderline; }
            set
            {
                if (value != m_IsUnderline)
                {
                    m_IsUnderline = value;
                    base.SetVerticesDirty();
                }
            }
        }
        public string UnderlineChar
        {
            get { return m_UnderlineChar; }
            set
            {
                if (value != m_UnderlineChar)
                {
                    m_UnderlineChar = value;
                    base.SetVerticesDirty();
                }
            }
        }
        public UnderlineStyle UnderlineType
        {
            get { return m_UnderlineType; }
            set
            {
                if (value != m_UnderlineType)
                {
                    m_UnderlineType = value;
                    base.SetVerticesDirty();
                }
            }
        }
        public Vector2 PreferredSize
        {
            get { return new Vector2(base.preferredWidth, base.preferredHeight); }
        }
        public Rect LastCaretPosition
        {
            get { return new Rect(mBinding.Width, 0, UIFactory.Instance.DefaultCaretSize.x, mBinding.Height); }
        }

        public TextGraphics()
        {
            //this.RegisterDirtyVerticesCallback(()=> { OnRefreshUnderline(); });
        }

        public TextGraphics DefaultInit()
        {
            this.font = UIFactory.Instance.DefaultFont;
            this.fontSize = 24;
            this.fontStyle = UnityEngine.FontStyle.Normal;
            this.color = Color.white;
            this.resizeTextForBestFit = true;
            this.resizeTextMinSize = UIFactory.Instance.DefaultFontBestFitMin;
            this.resizeTextMaxSize = UIFactory.Instance.DefaultFontBestFitMax;
            this.horizontalOverflow = HorizontalWrapMode.Overflow;
            this.verticalOverflow = VerticalWrapMode.Overflow;
            this.material = DefaultImageMaterial;
            return this;
        }

        protected override void Start()
        {
            this.mBinding = DisplayNode.AsDisplayNode(gameObject);
            base.Start();
        }

        public UnityEngine.UI.Outline AddBorder(UnityEngine.Color bc, Vector2 distance)
        {
            if (mTextEffect != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(mTextEffect);
            }
            if (distance != Vector2.zero)
            {
                UnityEngine.UI.Outline border = base.gameObject.AddComponent<UnityEngine.UI.Outline>();
                border.effectColor = bc;
                border.effectDistance = new Vector2(distance.x, -distance.y);
                this.mTextEffect = border;
                return border;
            }
            return null;
        }

        public UnityEngine.UI.Shadow AddShadow(UnityEngine.Color bc, Vector2 distance)
        {
            if (mTextEffect != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(mTextEffect);
            }
            if (distance != Vector2.zero)
            {
                UnityEngine.UI.Shadow shadow = base.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectColor = bc;
                shadow.effectDistance = new Vector2(distance.x, -distance.y);
                this.mTextEffect = shadow;
                return shadow;
            }
            return null;
        }

        public void SetBorder(Color bc, Vector2 distance)
        {
            AddBorder(bc, distance);
        }
        public void SetShadow(Color bc, Vector2 distance)
        {
            AddShadow(bc, distance);
        }
        public void SetFont(UnityEngine.Font font)
        {
            this.font = font;
        }
        public void SetTextFont(UnityEngine.Font font, int size, UnityEngine.FontStyle style)
        {
            this.font = font;
            this.fontSize = size;
            this.fontStyle = style;
        }

        public void SetAutoSizeMode()
        {
            this.resizeTextForBestFit = false;
            this.horizontalOverflow = UnityEngine.HorizontalWrapMode.Wrap;
            this.verticalOverflow = UnityEngine.VerticalWrapMode.Overflow;
            ContentSizeFitter Fit = gameObject.AddComponent<ContentSizeFitter>();
            Fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            if (m_IsUnderline)
            {
                this.OnPopulateUnderlineMesh(vh);
            }
            if (m_TextOffset != Vector2.zero)
            {
                this.OnPopulateOffset(vh);
            }
        }
        protected virtual void OnPopulateOffset(VertexHelper vh)
        {
            if (font == null) { return; }
            Vector3 offset = new Vector3(m_TextOffset.x, -m_TextOffset.y);
            var vertex = UIVertex.simpleVert;
            for (int i = vh.currentVertCount - 1; i >= 0; --i)
            {
                vh.PopulateUIVertex(ref vertex, i);
                vertex.position += offset;
                vh.SetUIVertex(vertex, i);
            }
        }
        protected virtual void OnPopulateUnderlineMesh(VertexHelper vh)
        {
            if (font == null) { return; }
            if (vh.currentVertCount >= 4)
            {
                this.m_DisableFontTextureRebuiltCallback = true;
                Vector2 size = base.rectTransform.rect.size;
                TextGenerationSettings generationSettings = this.GetGenerationSettings(size);
                this.cachedTextGenerator.Populate(m_UnderlineChar, generationSettings);
                IList<UIVertex> verts = this.cachedTextGenerator.verts;
                int num = verts.Count - 4;
                if (num >= 4)
                {
                    UICharInfo dchar = this.cachedTextGenerator.characters[0];
                    if (dchar.charWidth > 0)
                    {
                        UIVertex begin_vertex = UIVertex.simpleVert;
                        UIVertex end_vertex = UIVertex.simpleVert;
                        vh.PopulateUIVertex(ref begin_vertex, 0);
                        vh.PopulateUIVertex(ref end_vertex, vh.currentVertCount - 2);
                        float d = 1f / this.pixelsPerUnit;
                        switch (m_UnderlineType)
                        {
                            case UnderlineStyle.Fill:
                                {
                                    for (int j = 0; j < 4; j++)
                                    {
                                        m_TempVerts[j] = verts[j];
                                        m_TempVerts[j].position = m_TempVerts[j].position * d;
                                    }
                                    m_TempVerts[0].position.x = m_TempVerts[3].position.x = begin_vertex.position.x;
                                    m_TempVerts[1].position.x = m_TempVerts[2].position.x = end_vertex.position.x;
                                    vh.AddUIVertexQuad(m_TempVerts);
                                }
                                break;

                            case UnderlineStyle.Sequence:
                                for (float x = begin_vertex.position.x; x < end_vertex.position.x; x += dchar.charWidth)
                                {
                                    for (int j = 0; j < 4; j++)
                                    {
                                        m_TempVerts[j] = verts[j];
                                        m_TempVerts[j].position = m_TempVerts[j].position * d;
                                        m_TempVerts[j].position.x += x;
                                    }
                                    vh.AddUIVertexQuad(m_TempVerts);
                                }
                                break;

                            case UnderlineStyle.Sliced:
                                {
                                    float u_clip = Math.Abs(verts[1].uv0.x - verts[0].uv0.x) / 3;//将u切成3份// 
                                    float x_clip = (dchar.charWidth) / 3;//将x切成3份//

                                    float u_0 = verts[0].uv0.x;
                                    float u_1 = u_0 + u_clip;
                                    float u_3 = verts[1].uv0.x;
                                    float u_2 = u_3 - u_clip;

                                    float x_0 = begin_vertex.position.x;
                                    float x_1 = x_0 + x_clip;
                                    float x_3 = end_vertex.position.x;
                                    float x_2 = x_3 - x_clip;

                                    for (int i = m_TempVerts.Length - 1; i >= 0; --i)
                                    {
                                        m_TempVerts[i] = verts[i];
                                        m_TempVerts[i].position = m_TempVerts[i].position * d;
                                    }
                                    // left
                                    {
                                        m_TempVerts[3].position.x = m_TempVerts[0].position.x = x_0;
                                        m_TempVerts[2].position.x = m_TempVerts[1].position.x = x_1;
                                        m_TempVerts[3].uv0.x = m_TempVerts[0].uv0.x = u_0;
                                        m_TempVerts[2].uv0.x = m_TempVerts[1].uv0.x = u_1;
                                        vh.AddUIVertexQuad(m_TempVerts);
                                    }
                                    // center
                                    {
                                        m_TempVerts[3].position.x = m_TempVerts[0].position.x = x_1;
                                        m_TempVerts[2].position.x = m_TempVerts[1].position.x = x_2;
                                        m_TempVerts[3].uv0.x = m_TempVerts[0].uv0.x = u_1;
                                        m_TempVerts[2].uv0.x = m_TempVerts[1].uv0.x = u_2;
                                        vh.AddUIVertexQuad(m_TempVerts);
                                    }
                                    // right
                                    {
                                        m_TempVerts[3].position.x = m_TempVerts[0].position.x = x_2;
                                        m_TempVerts[2].position.x = m_TempVerts[1].position.x = x_3;
                                        m_TempVerts[3].uv0.x = m_TempVerts[0].uv0.x = u_2;
                                        m_TempVerts[2].uv0.x = m_TempVerts[1].uv0.x = u_3;
                                        vh.AddUIVertexQuad(m_TempVerts);
                                    }
                                }
                                break;
                        }

                    }
                }
                this.m_DisableFontTextureRebuiltCallback = false;
            }
        }


        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            if (mBinding.Enable && mBinding.EnableTouchInParents)
            {
                return base.Raycast(sp, eventCamera);
            }
            return false;
        }

        readonly Vector3[] m_Corners = new Vector3[4];
        private Rect rootCanvasRect
        {
            get
            {
                rectTransform.GetWorldCorners(m_Corners);

                if (canvas)
                {
                    Canvas rootCanvas = canvas.rootCanvas;
                    for (int i = 0; i < 4; ++i)
                        m_Corners[i] = rootCanvas.transform.InverseTransformPoint(m_Corners[i]);
                }

                return new Rect(m_Corners[0].x, m_Corners[0].y, m_Corners[2].x - m_Corners[0].x, m_Corners[2].y - m_Corners[0].y);
            }
        }

        public override void Cull(Rect clipRect, bool validRect)
        {

            var cull = !validRect || !clipRect.Overlaps(rootCanvasRect, true);
            UpdateCull(cull);
        }

        private void UpdateCull(bool cull)
        {
            var cullingChanged = canvasRenderer.cull != cull;
            canvasRenderer.cull = cull;

            if (cullingChanged)
            {
                onCullStateChanged.Invoke(cull);
                SetVerticesDirty();
            }
        }
    }

}
