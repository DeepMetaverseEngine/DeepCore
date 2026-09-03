using DeepCore.GUI.Cell;
using DeepCore.GUI.Data;
using DeepCore.GUI.Gemo;
using System;
using System.Collections.Generic;

namespace DeepCore.GUI.Display.UI
{
    public class UILabel : UIComponent
    {
        protected UITextField mTextField = null;
        private Anchor mAnchor;
        protected bool mIsImageText = false;
        protected CPJAtlas mAtlas = null;
        public UILabel()
        {
            this.Enable = false;
            this.EnableChildren = false;
            Bounds = new Gemo.Rectangle2D(0, 0, 1, 1);
            mTextField = CreateTextField("", UIEditor.Instance.DefaultFontSize, UIEditor.Instance.DefaultFontStyle);
            this.AddChild(mTextField);
        }

        private UITextField CreateTextField(string text, float fontSize, FontStyle style)
        {
            return new UITextField(text, fontSize, style);
        }

        private void ResetTextField(UITextField o, float w, float h)
        {

            if(o == null)
            {
                return;
            }

            if((mAnchor & Anchor.ANCHOR_HCENTER) != 0)
            {
                o.X = w * 0.5f;
            }
            else if((mAnchor & Anchor.ANCHOR_RIGHT) != 0)
            {
                o.X = w;
            }
            else
            {
                o.X = 0;
            }
            if((mAnchor & Anchor.ANCHOR_VCENTER) != 0)
            {
                o.Y = h * 0.5f;
            }
            else if((mAnchor & Anchor.ANCHOR_BOTTOM) != 0)
            {
                o.Y = h;
            }
            else
            {
                o.Y = 0;
            }

        }

        protected override void Resize(float w, float h, bool flush)
        {
            base.Resize(w, h, flush);

            if(mIsImageText)
            {
                ResetImageFontPosition(w, h);
            }
            else
            {
                ResetTextField(mTextField, w, h);
            }
        }

        public string Text
        {
            get
            {
                return mTextField.Text;
            }
            set
            {
                if(mIsImageText)
                {
                    mTextField.Visible = false;
                    if(value != mTextField.Text)
                    {
                        ParseTextToImage(value);
                    }

                }
                mTextField.Text = value;
            }
        }

        public void SetTextAnchor(Anchor anchor)
        {
            mAnchor = anchor;
            mTextField.SetAnchor(anchor);
            ResetTextField(mTextField, Bounds.width, Bounds.height);
            if(mIsImageText)
            {
                ResetImageFontPosition(Bounds.width, Bounds.height);
            }
        }

        public void SetTextColor(uint rgba)
        {
            mTextField.SetFontColor(rgba);
        }

        public void SetFontSize(int size)
        {
            mTextField.SetFontSize(size);
        }

        public void SetBorderTimes(int times)
        {
            mTextField.SetBorderTimes(times);
        }

        public void SetBorderColor(uint color)
        {
            mTextField.SetBorderColor(color);
        }

        public void SetFontStyle(FontStyle style)
        {
            mTextField.SetFontStyle(style);
        }

        public float GetContentHeight()
        {
            if(mTextField != null)
            {
                return mTextField.GetTextHeight();
            }
            return 0;
        }

        public float GetContentWidth()
        {
            if(mTextField != null)
            {
                return mTextField.GetTextWidth();
            }
            return 0;
        }

        protected override void Disposing()
        {
            if(mTextField != null)
            {
                mTextField.RemoveFromParent(false);
                mTextField.Dispose();
                mTextField = null;
            }

            mAtlas = null;

            if(mRectList != null)
            {
                mRectList.Clear();
                mRectList = null;
            }

            if(mVertex != null)
            {
                mVertex.Dispose();
                mVertex = null;
            }

            base.Disposing();
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);

            if (mIsImageText)
            {
                int l = mRectList.Count;
                Image img = mAtlas.GetTile(0);
                g.BeginImage(img);
                float sx = 0;
                Rectangle2D rect = null;
                for (int i = 0; i < l; i++)
                {
                    rect = mRectList[i];
                    g.DrawRegion(rect.x, rect.y, rect.width, rect.height, Trans.TRANS_NONE, px + sx, py);
                    sx += rect.width;
                }
            }
        }

        private List<Rectangle2D> mRectList = null;
        private float px;
        private float py;
        private float totalW = 0;
        private float totalH = 0;
        private VertexBuffer mVertex;
        public void ParseTextToImage(string content)
        {
            if(content == null)
            {
                return;
            }

            if(mRectList == null)
            {
                mRectList = new List<Rectangle2D>();
            }

            mRectList.Clear();
            totalW = 0;
            totalH = 0;
            char[] chars     = content.ToCharArray();
            int charsLength  = chars.Length;

            int k;
            for(int i = 0; i < charsLength; i++)
            {
                k = mAtlas.GetIndexByKey(chars[i] + "");
                mRectList.Add(mAtlas.GetClipRect(k));
                //VertexBuffer.
            }

            int l = mRectList.Count;

            for(int i = 0; i < l; i++)
            {
                totalW += mRectList[i].width;
                totalH = Math.Max(totalH, mRectList[i].height);
            }

            ResetImageFontPosition(Width, Height);
        }

        private void ResetImageFontPosition(float w, float h)
        {

            if((mAnchor & Anchor.ANCHOR_HCENTER) != 0)
            {
                px = (w - totalW) * 0.5f;
            }
            else if((mAnchor & Anchor.ANCHOR_RIGHT) != 0)
            {
                px = w - totalW;
            }
            else
            {
                px = 0;
            }
            if((mAnchor & Anchor.ANCHOR_VCENTER) != 0)
            {
                py = (h - totalH) * 0.5f;
            }
            else if((mAnchor & Anchor.ANCHOR_BOTTOM) != 0)
            {
                py = h - totalH;
            }
            else
            {
                py = 0;
            }

        }

        private void CalVertexBuffer(string content)
        {
            if(content == null)
            {
                return;
            }

            char[] chars     = content.ToCharArray();

            int charsLength  = chars.Length;
            int uvLength = (charsLength + 1) * 2;

            float [] ax = new float[charsLength + 1];
            float [] ay = new float[2];

            float [] au = new float[charsLength + 1];
            float [] av = new float[2];

            int k;
            Rectangle2D rect = null;
            float sx = 0;
            for(int i = 0; i < charsLength + 1; i++)
            {

                k = mAtlas.GetIndexByKey(chars[i] + "");
                rect = mAtlas.GetClipRect(k);

                ax[i] = sx;
                if(i > 0 && i < ay.Length)
                {
                    ay[i] = rect.height;
                }

                sx += rect.width;
            }

            for(int i = 0; i < charsLength; i++)
            {

                k = mAtlas.GetIndexByKey(chars[i] + "");
                rect = mAtlas.GetClipRect(k);

                au[i] = rect.x;
                av[i] = rect.y;

                if(i + 1 < av.Length)
                {
                    au[i + 1] = rect.x + rect.width;
                    av[i + 1] = rect.y + rect.height;
                }
            }

            if(mVertex != null)
            {
                mVertex.Dispose();
                mVertex = null;
            }

            int length = (charsLength + 1) * 2;
            int[] indices = new int[length];


            int a = 0;
            int b = 1;
            int c = 5;
            int d = 4;

            int j = 0;
            for(int i = 0; i < length; i += 2)
            {
                indices[i] = a + j;
                indices[i + 1] = b + j;
                indices[i + 2] = c + j;
                indices[i + 3] = d + j;

                j++;
            }

            mVertex = GenVertexBuffer(ax, ay, au, av, indices);

            //setPosition.
            int l = mRectList.Count;

            for(int i = 0; i < l; i++)
            {
                totalW += mRectList[i].width;
                totalH = Math.Max(totalH, mRectList[i].height);
            }

            ResetImageFontPosition(Width, Height);

        }

        private VertexBuffer GenVertexBuffer(float[] ax, float[] ay, float[] au, float[] av, int[] indices)
        {
            if(mVertex != null)
            {
                mVertex.Dispose();
            }
            mVertex = Driver.Instance.createVertexBuffer(ax.Length * ay.Length);
            int index = 0;
            for(int iy = 0; iy < ay.Length; ++iy)
            {
                for(int ix = 0; ix < ax.Length; ++ix)
                {
                    mVertex.SetPosition(index, ax[ix], ay[iy]);
                    VertexUtils.SetTexCoords(mVertex, index, mAtlas.GetTile(0), au[ix], av[iy]);
                    index++;
                }
            }

            mVertex.SetIndices(indices, VertexTopology.QUADS);
            mVertex.Optimize();

            return mVertex;
        }



        protected override void DecodeFields(UIEditor editor, Data.UIComponentMeta e)
        {
            base.DecodeFields(editor, e);

            if (e is Data.UETextComponentMeta)
            {
                Data.UETextComponentMeta meta = e as Data.UETextComponentMeta;
                SetTextAnchor(AnchorTool.FromTextAnchor(meta.text_anchor));
                SetFontSize(meta.textFontSize);
                SetTextColor(Color.toRGBA(meta.textColor));
                DecodeImageFont(editor, meta);
                SetBorderColor(Color.toRGBA(meta.textBorderColor));
                if (meta.textBorderAlpha == 0)
                {
                    SetBorderTimes(0);
                }
                this.Text = meta.text;
            }

        }
        
        protected virtual void DecodeImageFont(UIEditor editor, Data.UETextComponentMeta e)
        {
            if (!string.IsNullOrEmpty(e.ImageFont))
            {
                string[] args = e.ImageFont.Split('|');
                string a_name = args[0];
                string a_tg = args[1];
                string path = a_name;
                path = path.Replace("^", "");
                mAtlas = editor.CreateAtlas(path, a_tg);
                mIsImageText = true;
            }
            else
            {
                mIsImageText = false;
            }
        }
    }
}

