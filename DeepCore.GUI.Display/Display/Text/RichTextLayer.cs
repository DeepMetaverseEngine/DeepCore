using DeepCore.Geometry;
using DeepCore.GUI.Cell.Game;
using DeepCore.GUI.Data;
using System;
using System.Collections.Generic;

namespace DeepCore.GUI.Display.Text
{
    public abstract partial class BaseRichTextLayer : IDisposable
    {
        private List<Region> mBlocks = new List<Region>();
        private List<Region> mOldBlocks = new List<Region>();
        private List<Line> mLines = new List<Line>();

        private AttributedString mText = new AttributedString();

        private float mWidth;
        private TextBorderStyle mBorderCount;
        private uint mBorderColor;

        private RichTextAlignment mAlignment = RichTextAlignment.taLEFT;
        private float mLineSpace;
        private float mFixedLineSpace;
        private float mIgnoreSpace;
        private bool mEnableLineBreak;

        private float mContentWidth;
        private float mContentHeigh;
        private bool mIsEnable = true;
        private bool mIsColorDirty = true;

        private HashMap<string, TImageRegion> mImageCache = new HashMap<string, TImageRegion>();
        private HashMap<string, TSpriteMetaAnimateFrame> mSpriteCache = new HashMap<string, TSpriteMetaAnimateFrame>();

        public RichTextAlignment Alignment { get { return mAlignment; } }
        public bool IsMultiline { get { return mEnableLineBreak; } }
        public int LineCount { get { return mLines.Count; } }
        public float LineSpace { get { return mLineSpace; } }
        public float FixedLineSpace { get { return mFixedLineSpace; } }
        public int TextLength { get { return mText.Length; } }
        public float Width { get { return mWidth; } }
        /// <summary>
        /// 获取全部宽
        /// </summary>
        public float ContentWidth { get { return mContentWidth; } }
        /// <summary>
        /// 获取全部高
        /// </summary>
        public float ContentHeight { get { return mContentHeigh; } }

        public TextBorderStyle BorderCount { get { return mBorderCount; } }
        public uint BorderColor { get { return mBorderColor; } }

        public bool IsEnable
        {
            get { return mIsEnable; }
            set
            {
                if (mIsEnable != value)
                {
                    mIsEnable = value;
                    mIsColorDirty = true;
                }
            }
        }

        protected List<Region> AllRegions
        {
            get { return mBlocks; }
        }
        protected List<Line> AllLines
        {
            get { return mLines; }
        }

        protected BaseRichTextLayer(float width = 100, RichTextAlignment anchor = RichTextAlignment.taLEFT)
        {
            mWidth = width;
            mAlignment = anchor;
            mEnableLineBreak = true;
            mLineSpace = 0;
            mFixedLineSpace = 0;
            mIgnoreSpace = 0;
            mContentHeigh = 0;
            mContentWidth = 0;
            mBorderCount = 0;
        }

        /// <summary>
        /// 设置锚点
        /// </summary>
        /// <param name="anchor"></param>
        /// <returns></returns>
        public BaseRichTextLayer SetAnchor(RichTextAlignment anchor)
        {
            if (mAlignment != anchor)
            {
                mAlignment = anchor;
                ResetLines();
            }
            return this;
        }

        /// <summary>
        /// 设置行距
        /// </summary>
        /// <param name="space"></param>
        /// <returns></returns>
        public BaseRichTextLayer SetLineSpace(float space)
        {
            if (mLineSpace != space)
            {
                mLineSpace = space;
                ResetLines();
            }
            return this;
        }
        /// <summary>
        /// 设置固定行间距，忽略文字大小
        /// </summary>
        /// <param name="space"></param>
        /// <returns></returns>
        public BaseRichTextLayer SetFixedLineSpace(float space)
        {
            if (mFixedLineSpace != space)
            {
                mFixedLineSpace = space;
                ResetLines();
            }
            return this;
        }

        public void SetIgnoreSpace(float space)
        {
            if (mIgnoreSpace != space)
            {
                mIgnoreSpace = space;
            }
        }

        public BaseRichTextLayer SetEnableMultiline(bool multiline)
        {
            if (mEnableLineBreak != multiline)
            {
                mEnableLineBreak = multiline;
                ResetChars();
            }
            return this;
        }

        public BaseRichTextLayer SetWidth(float width)
        {
            if (mWidth != width)
            {
                mWidth = width;
                ResetChars();
            }
            return this;
        }


        public BaseRichTextLayer SetBorder(TextBorderStyle count, uint color)
        {
            if (mBorderCount != count || mBorderColor != color)
            {
                mBorderCount = count;
                mBorderColor = color;
                ResetChars();
            }
            return this;
        }

        public BaseRichTextLayer SetString(AttributedString text)
        {
            try
            {
                if (!mText.Equals(text))
                {
                    this.mText = text;
                    ResetChars();
                }
            }
            catch { }
            return this;
        }

        public BaseRichTextLayer.Line GetLine(int index)
        {
            if (index < mLines.Count)
            {
                return mLines[index];
            }
            return null;
        }

        public AttributedString GetText()
        {
            return mText;
        }

        /// <summary>
        /// 获取文本在文本区域的位置
        /// </summary>
        /// <param name="charIndex">文本位置</param>
        /// <returns></returns>
        public BaseRichTextLayer.Region GetRegion(int charIndex)
        {
            if (mLines.Count == 0)
            {
                return null;
            }
            for (int i = 0; i < mBlocks.Count; i++)
            {
                BaseRichTextLayer.Region rg = mBlocks[i];
                if (charIndex <= rg.mStartIndex)
                {
                    return rg;
                }
            }
            return null;
        }
        public BaseRichTextLayer.Region GetNowRegion(int charIndex)
        {
            if (charIndex > mBlocks.Count || charIndex < 0)
            {
                return null;
            }
            else
            {
                return mBlocks[charIndex];
            }
        }
        public int getRegionLen()
        {
            return mBlocks.Count;
        }
        public void resetChatGegion(int height)
        {
            for (int i = 0; i < mLines.Count; i++)
            {
                Line line = mLines[i];

                for (int c = 0; c < line.mBlocks.Count; c++)
                {
                    Region rg = line.mBlocks[c];
                    float dh = height - rg.mBounds.Height;
                    string str = rg.Text.Substring(0, 1);
                    if (i == 0)
                    {
                        if (str == "T")
                            rg.mBounds.Y = dh + (rg.mBounds.Height - line.mBlocks[0].mBounds.Height) / 2;
                        else
                            rg.mBounds.Y = dh;
                    }
                    else
                    {
                        rg.mBounds.Y = rg.mBounds.Y + dh * 2;
                        if (rg.mBounds.Y > height + dh)
                            rg.mBounds.Y = height + dh;
                    }
                }
            }
        }

        protected void BeginRender(Graphics g)
        {
            if (mOldBlocks.Count > 0)
            {
                foreach (BaseRichTextLayer.Region it in mOldBlocks)
                {
                    it.Dispose();
                }
                mOldBlocks.Clear();
            }
            if (mIsColorDirty)
            {
                mIsColorDirty = false;
                this.CreateRegionBuffs();
            }
        }

        protected void InternalRender(Graphics g, float px, float py, float pw, float ph, float cx, float cy, int deltaMS)
        {
            Line tempL = null;
            for (int i = 0; i < mLines.Count; i++)
            {
                tempL = mLines[i];
                if (tempL.mBlocks.Count > 0)
                {
                    if (CMath.IntersectRectW(cx, cy, pw, ph,
                        tempL.mBounds.X,
                        tempL.mBounds.Y,
                        tempL.mBounds.Width,
                        tempL.mBounds.Height))
                    {
                        tempL.RenderLine(g, px, py, deltaMS);
                    }
                    else
                    {
                        tempL.HideLine(px, py);
                    }
                }
            }
        }

        protected void AfterRender(Graphics g) { }

        protected virtual void OnDrawRegion(Graphics g, Region rg, float px, float py) { }
        protected virtual void OnDrawLine(Graphics g, Line rg, float px, float py) { }

        /// <summary>
        /// 渲染文本
        /// </summary>
        /// <param name="g">绘图指针</param>
        /// <param name="dx">绘制到目标x</param>
        /// <param name="dy">绘制到目标y</param>
        /// <param name="dw">绘制范围宽</param>
        /// <param name="dh">绘制范围高</param>
        /// <param name="sx">自身坐标X</param>
        /// <param name="sy">自身坐标Y</param>
        /// <param name="deltaMS">与上一帧的间隔</param>
        public virtual void Render(Graphics g, float dx, float dy, float dw, float dh, float sx, float sy, int deltaMS = 0)
        {
            this.BeginRender(g);
            this.InternalRender(g, dx, dy, dw, dh, sx, sy, deltaMS);
            this.AfterRender(g);
        }

        /// <summary>
        /// 绘制全部富文本
        /// </summary>
        /// <param name="g"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public void Render(Graphics g, float dx, float dy)
        {
            Render(g, dx, dy, Width, mContentHeigh, 0, 0);
        }
        public void Render(Graphics g, RectangleF bounds, Vector2 scroll, int deltaMS = 0)
        {
            Render(g, bounds.X, bounds.Y, bounds.Width, bounds.Height, scroll.X, scroll.Y, deltaMS);
        }
        public void Render(Graphics g, RectangleF bounds, int deltaMS = 0)
        {
            Render(g, bounds.X, bounds.Y, bounds.Width, bounds.Height, 0, 0, deltaMS);
        }

        /// <summary>
        /// 点选文本
        /// </summary>
        /// <param name="cx">自身坐标X</param>
        /// <param name="cy">自身坐标Y</param>
        /// <param name="info">输出点选信息</param>
        /// <returns></returns>
        public virtual bool Click(float cx, float cy, out RichTextClickInfo info)
        {
            info = new RichTextClickInfo();

            for (int i = 0; i < mLines.Count; i++)
            {
                Line line = mLines[i];
                for (int c = 0; c < line.mBlocks.Count; c++)
                {
                    Region rg = line.mBlocks[c];
                    if (rg.mBounds.Contains(cx, cy))
                    {
                        info.mRegion = rg;
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual RichTextClickInfo Click(float cx, float cy)
        {
            RichTextClickInfo info = new RichTextClickInfo();

            for (int i = 0; i < mLines.Count; i++)
            {
                Line line = mLines[i];
                for (int c = 0; c < line.mBlocks.Count; c++)
                {
                    Region rg = line.mBlocks[c];
                    if (rg.mBounds.Contains(cx, cy))
                    {
                        info.mRegion = rg;
                        return info;
                    }
                }
            }
            return info;
        }

        public virtual void Dispose()
        {
            foreach (BaseRichTextLayer.Region it in mBlocks)
            {
                it.Dispose();
            }
            foreach (BaseRichTextLayer.Region it in mOldBlocks)
            {
                it.Dispose();
            }
            mBlocks.Clear();
            mOldBlocks.Clear();
            mLines.Clear();
        }


        //----------------------------------------------------------------------------------------

        protected virtual void OnBeginResetChars() { }
        protected virtual void OnEndResetLines() { }

        private Action<BaseRichTextLayer> event_EndResetLines;
        public event Action<BaseRichTextLayer> EndResetLines
        {
            add { event_EndResetLines += value; }
            remove { event_EndResetLines -= value; }
        }

        private void ResetChars()
        {
            OnBeginResetChars();

            if (mOldBlocks.Count > 0)
            {
                // 不要删除已经在显示的内容，防止显示空白，BeginRender时会更新到最新
                foreach (BaseRichTextLayer.Region it in mBlocks)
                {
                    it.Dispose();
                }
                mBlocks.Clear();
            }
            else
            {
                // 临时缓存正在显示的内容，防止显示空白, BeginRender时删除
                var tmp = mOldBlocks;
                mOldBlocks = mBlocks;
                mBlocks = tmp;
            }

            this.mBlocks.Capacity = (mText.Length);
            // 按照属性划分区段
            if (mText.Length > 0)
            {
                Region buildRG = null;
                for (int i = 0; i < mText.Length; i++)
                {
                    TextAttribute curTA = mText.GetAttribute(i);
                    if (!string.IsNullOrEmpty(curTA.resImage))
                    {
                        AddImage(curTA);
                    }
                    else if (!string.IsNullOrEmpty(curTA.resSprite))
                    {
                        AddSprite(curTA);
                    }
                    char mCH = mText.GetChar(i);
                    //出现换行，立即分段，保持每个换行符占一个Region//
                    if (mEnableLineBreak && (mCH == '\n'))
                    {
                        if (buildRG != null)
                        {
                            buildRG.mEndIndex = i;
                            buildRG.mText = mText.ToString().Substring(buildRG.mStartIndex, buildRG.CharCount);
                            mBlocks.Add(buildRG);
                        }
                        Region br = new Region(this, curTA);
                        br.mText = "\n";
                        br.mStartIndex = i;
                        br.mEndIndex = i + 1;
                        mBlocks.Add(br);
                        buildRG = null;
                    }
                    else
                    {
                        if (buildRG != null)
                        {
                            //属性不同，则截断//
                            if (!TestSplitByAttribute(buildRG.mAttribute, curTA))
                            {
                                buildRG.mEndIndex = i;
                                buildRG.mText = mText.ToString().Substring(buildRG.mStartIndex, buildRG.CharCount);
                                mBlocks.Add(buildRG);
                                buildRG = new Region(this, curTA);
                                buildRG.mStartIndex = i;
                            }
                        }
                        else
                        {
                            //首次解析//
                            buildRG = new Region(this, curTA);
                            buildRG.mStartIndex = i;
                        }
                    }

                }
                //防止最后一段漏掉//
                if (buildRG != null)
                {
                    buildRG.mEndIndex = mText.Length;
                    buildRG.mText = mText.ToString().Substring(buildRG.mStartIndex, buildRG.CharCount);
                    mBlocks.Add(buildRG);
                    buildRG = null;
                }
            }

            // 计算换行
            this.mLines.Clear();
            if (mBlocks.Count > 0)
            {
                if (mEnableLineBreak)
                {
                    float curX = 0;
                    Line curLine = new Line(this);
                    for (int i = 0; i < mBlocks.Count; i++)
                    {
                        Region rg = mBlocks[i];
                        Region split_cur;
                        Region split_next;
                        int testW = (int)(mWidth - curX);
                        //测试边界已经到达//
                        if (testW <= 0)
                        {
                            //当前行入栈//
                            mLines.Add(curLine);
                            curLine = new Line(this);
                            curX = 0;
                            testW = (int)mWidth;
                        }

                        if (rg.mText.EndsWith("\n"))
                        {
                            //直接换行////当前行入栈// 
                            this.TestLineBreak2(rg.mText.Substring(0, rg.mText.Length - 1), rg.mAttribute, float.MaxValue, out rg.mBounds.Width, out rg.mBounds.Height);
                            curLine.AddRegion(rg);
                            mLines.Add(curLine);
                            curLine = new Line(this);
                            curX = 0;
                        }
                        else if (TestLineBreak(rg, testW, out split_cur, out split_next))
                        {
                            //测试截断成功//
                            if (split_next == rg)
                            {
                                //当前段直接到下一行//
                                if (curLine.RegionCount == 0)
                                {
                                    //当前行为空，表示最小尺寸已经大于宽//添加到当前行//
                                    curLine.AddRegion(rg);
                                    mLines.Add(curLine);
                                    curLine = new Line(this);
                                    curX = 0;
                                }
                                else
                                {
                                    //添加到下一行//
                                    mLines.Add(curLine);
                                    curLine = new Line(this);
                                    curX = 0;
                                    //重新从下一行开始检索//
                                    --i;
                                }
                            }
                            else
                            {
                                // 换行并且被切成2断
                                mBlocks.RemoveAt(i);
                                mBlocks.Insert(i, split_cur);
                                mBlocks.Insert(i + 1, split_next);
                                rg.Dispose();
                                // next line
                                curLine.AddRegion(split_cur);
                                mLines.Add(curLine);
                                curLine = new Line(this);
                                curX = 0;
                            }
                        }
                        else
                        {
                            //本行增加段//
                            curLine.AddRegion(rg);
                            curX += rg.mBounds.Width;
                            //最后一行//
                            if (i == (mBlocks.Count - 1))
                            {
                                mLines.Add(curLine);
                            }
                        }
                    }
                }
                else
                {
                    Line curLine = new Line(this);
                    for (int i = 0; i < mBlocks.Count; i++)
                    {
                        Region rg = mBlocks[i];
                        Region split_cur;
                        Region split_next;
                        TestLineBreak(rg, int.MaxValue, out split_cur, out split_next);
                        curLine.AddRegion(rg);
                    }
                    mLines.Add(curLine);
                }
            }

            ResetLines();

            mIsColorDirty = true;
        }

        private void ResetLines()
        {
            mContentWidth = 1;
            mContentHeigh = 0;

            float c_left = float.MaxValue;
            float c_right = 0;

            for (int i = 0; i < mLines.Count; i++)
            {
                Line line = mLines[i];
                line.mIndex = i;
                float dw = 0;
                float dh = 0;
                //计算宽高//
                {
                    line.mAnchor = RichTextAlignment.taNA;
                    for (int c = 0; c < line.mBlocks.Count; c++)
                    {
                        Region rg = line.mBlocks[c];
                        rg.mBounds.X = dw;
                        rg.mBounds.Y = mContentHeigh;
                        dw += rg.mBounds.Width;

                        if (mIgnoreSpace > 0)
                            dh = mIgnoreSpace;
                        else
                            dh = Math.Max(dh, rg.mBounds.Height);

                        if (rg.Attribute.anchor != RichTextAlignment.taNA)
                        {
                            line.mAnchor = rg.Attribute.anchor;
                        }
                    }
                    line.mBounds.Width = dw;
                    line.mBounds.Height = dh;
                    line.mBounds.X = 0;
                    line.mBounds.Y = mContentHeigh;
                    if (line.mAnchor == RichTextAlignment.taNA)
                    {
                        line.mAnchor = mAlignment;
                    }
                }
                //计算位置//
                {
                    float dx = 0;
                    switch (line.mAnchor)
                    {
                        case RichTextAlignment.taLEFT:
                            break;
                        case RichTextAlignment.taCENTER:
                            dx += (mWidth - dw) / 2;
                            break;
                        case RichTextAlignment.taRIGHT:
                            dx += (mWidth - dw);
                            break;
                    }
                    for (int c = 0; c < line.mBlocks.Count; c++)
                    {
                        Region rg = line.mBlocks[c];
                        rg.mBounds.X += dx;
                        rg.mBounds.Y = rg.mBounds.Y + dh - rg.mBounds.Height;
                    }
                    line.mBounds.X += dx;
                }
                c_left = Math.Min(line.mBounds.X, c_left);
                c_right = Math.Max(line.mBounds.X + line.mBounds.Width, c_right);
                if (mFixedLineSpace > 0)
                {
                    mContentHeigh += mFixedLineSpace;
                }
                else
                {
                    mContentHeigh += dh + mLineSpace;
                }
            }
            if (c_left < c_right)
            {
                mContentWidth = c_right - c_left;
            }
            else
            {
                mContentWidth = 1;
            }
            OnEndResetLines();
            if (event_EndResetLines != null) event_EndResetLines.Invoke(this);
        }



        /// <summary>
        /// 测试截断
        /// </summary>
        /// <param name="rg"></param>
        /// <param name="testW"></param>
        /// <param name="split_cur"></param>
        /// <param name="split_next"></param>
        /// <returns></returns>
        private bool TestLineBreak(Region rg, float testW, out Region split_cur, out Region split_next)
        {
            rg.mBounds.Width = 0;
            rg.mBounds.Height = 0;
            split_cur = null;
            split_next = null;
            TextAttribute ta = rg.mAttribute;

            float last_tw = 0;
            float last_th = 0;
            float tw = 0;
            float th = 0;

            int textLength = rg.mText.Length;

            for (int ci = 0; ci < textLength; ++ci)
            {
                int cci = ci;
                int emoji_extra_len = 0; // emoji 额外长度 //
                bool isemoji = GraphicsDriver.Instance.IsEmoji(rg.mText, ci, out emoji_extra_len);
                if (isemoji)
                {
                    //keep self index//
                    emoji_extra_len -= 1;
                    ci += emoji_extra_len;
                }
                string text = rg.mText.Substring(0, ci + 1);
                if (TestLineBreak2(text, ta, testW, out tw, out th))
                {
                    if (cci == 0)
                    {
                        // 直接需要换行 //
                        split_next = rg;
                        split_cur = rg;
                        rg.mBounds.Width = tw;
                        rg.mBounds.Height = th;
                    }
                    else if (isemoji)
                    {
                        //需要切2段//
                        Region[] splits = rg.Split(ci - emoji_extra_len);
                        rg.Dispose();
                        split_cur = splits[0];
                        //切开后，第一段宽高保持上一次检测值//
                        split_cur.mBounds.Width = last_tw;
                        split_cur.mBounds.Height = last_th;
                        split_next = splits[1];
                    }
                    else
                    {
                        if (TestSpellBreak(rg.mText, ci, out var best_ci))
                        {
                            if (best_ci == 0)
                            {
                                // 直接需要换行 //
                                split_next = rg;
                                split_cur = rg;
                                rg.mBounds.Width = tw;
                                rg.mBounds.Height = th;
                                return true;
                            }
                            ci = best_ci;
                        }
                        //需要切2段//
                        Region[] splits = rg.Split(ci);
                        rg.Dispose();
                        split_cur = splits[0];
                        //切开后，第一段宽高保持上一次检测值//
                        split_cur.mBounds.Width = last_tw;
                        split_cur.mBounds.Height = last_th;
                        split_next = splits[1];
                    }
                    return true;
                }
                else
                {
                    rg.mBounds.Width = tw;
                    rg.mBounds.Height = th;
                }
                last_tw = tw;
                last_th = th;
            }
            return false;
        }


        /// <summary>
        /// 检测文本范围，构造Region尺寸。
        /// </summary>
        /// <param name="text"></param>
        /// <param name="ta"></param>
        /// <param name="testW"></param>
        /// <param name="tw"></param>
        /// <param name="th"></param>
        /// <returns></returns>
        private bool TestLineBreak2(String text, TextAttribute ta, float testW, out float tw, out float th)
        {
            tw = 0;
            th = 0;
            if (text.Equals("\n"))
            {
                TestTextLineBreak(text, ta, testW, out tw, out th);
                tw = 0;
                return true;
            }
            else if (ta.drawable != null)
            {
                tw = ta.drawable.CharWidth * text.Length;
                th = ta.drawable.CharHeight;
                if (tw > testW)
                {
                    tw = (float)Math.Ceiling(testW - ta.drawable.CharWidth);
                    return true;
                }
            }
            else if (!string.IsNullOrEmpty(ta.resImage))
            {
                if (ta.resImageZoom != null)
                {
                    tw = ta.resImageZoom.Width * text.Length;
                    th = ta.resImageZoom.Height;
                    if (tw > testW)
                    {
                        tw = (float)Math.Ceiling(testW - ta.resImageZoom.Width);
                        return true;
                    }
                }
                else
                {
                    TImageRegion img = AddImage(ta);
                    if (img.image != null)
                    {
                        tw = img.sw * text.Length;
                        th = img.sh;
                        if (tw > testW)
                        {
                            tw = (float)Math.Ceiling(testW - img.sw);
                            return true;
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(ta.resSprite))
            {
                TSpriteMetaAnimateFrame spr = AddSprite(ta);
                if (spr.sprite != null)
                {
                    CCD bounds = spr.sprite.getVisibleBounds(spr.anim);
                    tw = bounds.Width * text.Length;
                    th = bounds.Height;
                    if (tw > testW)
                    {
                        tw = (float)Math.Ceiling(testW - bounds.Width);
                        return true;
                    }
                }
            }
            else if (this.TestTextLineBreak(text, ta, testW, out tw, out th))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检测属性上是否要分段
        /// </summary>
        /// <param name="rg"></param>
        /// <param name="ta"></param>
        /// <returns></returns>
        protected virtual bool TestSplitByAttribute(TextAttribute rg, TextAttribute ta)
        {
            return rg.Equals(ta);
        }
        /// <summary>
        /// 检测文本是否超过指定范围，并返回文本实际尺寸。
        /// </summary>
        /// <param name="text"></param>
        /// <param name="ta"></param>
        /// <param name="testW"></param>
        /// <param name="tw"></param>
        /// <param name="th"></param>
        /// <returns></returns>
        protected virtual bool TestTextLineBreak(string text, TextAttribute ta, float testW, out float tw, out float th)
        {
            return GraphicsDriver.Instance.TestTextLineBreak(text, ta.fontName, ta.fontSize, ta.fontStyle, mBorderCount, testW, out tw, out th);
        }
        /// <summary>
        /// 防止英文单词被切断
        /// </summary>
        /// <returns></returns>
        protected virtual bool TestSpellBreak(string regionText, int splitIndex, out int bestSplitIndex)
        {
            return GraphicsDriver.Instance.TestTextSpellBreak(regionText, splitIndex, out bestSplitIndex);
        }
        //------------------------------------------------------------------------------------

        private void CreateRegionBuffs()
        {
            mIsColorDirty = false;
            // 生成缓冲区
            for (int i = 0; i < mBlocks.Count; i++)
            {
                Region rg = mBlocks[i];
                rg.mIndex = i;
                CreateRegionBuffer(rg);
            }
        }

        private Drawable CreateRegionBuffer(Region rg)
        {
            if (rg.IsBreak)
            {
                rg.Buffer = null;
            }
            else if (rg.mAttribute.drawable != null)
            {
                rg.Buffer = this.CreateDrawable(rg, rg.mAttribute.drawable);
            }
            else if (!string.IsNullOrEmpty(rg.mAttribute.resImage))
            {
                TImageRegion img = AddImage(rg.mAttribute);
                if (img.image != null)
                {
                    rg.Buffer = this.CreateDrawable(rg, img);
                }
            }
            else if (!string.IsNullOrEmpty(rg.mAttribute.resSprite))
            {
                TSpriteMetaAnimateFrame spr = AddSprite(rg.mAttribute);
                if (spr.sprite != null)
                {
                    rg.Buffer = this.CreateDrawable(rg, spr);
                }
            }
            else
            {
                rg.Buffer = this.CreateDrawable(rg, rg.Text);
            }
            return rg.Buffer;
        }

        private TImageRegion AddImage(TextAttribute ta)
        {
            TImageRegion ret = new TImageRegion();
            if (mImageCache.TryGetValue(ta.resImage, out ret))
            {
                return ret;
            }
            if (ta.resImage.StartsWith("#"))
            {
                string[] kvs = ta.resImage.Split(',');
                if (kvs.Length >= 3)
                {
                    string file = kvs[0].Substring(1);
                    Cell.CPJResource res = this.AddCPJResource(file);
                    if (res != null)
                    {
                        Cell.CPJAtlas atlas = res.GetAtlas(kvs[1]);
                        int tid;
                        if (atlas != null && Parser.TryParseInt(kvs[2], out tid))
                        {
                            Image image = atlas.GetTile(tid);
                            if (image != null)
                            {
                                var rect = atlas.GetAtlasRegion(tid);
                                ret.sx = rect.X;
                                ret.sy = rect.Y;
                                ret.sw = rect.Width;
                                ret.sh = rect.Height;
                                ret.image = image;
                                mImageCache.Add(ta.resImage, ret);
                                return ret;
                            }
                        }
                    }
                }
            }
            else
            {
                string file = ta.resImage;
                Image image = AddImage(file);
                if (image != null)
                {
                    ret.sw = image.Width;
                    ret.sh = image.Height;
                    ret.image = image;
                    mImageCache.Add(ta.resImage, ret);
                    return ret;
                }
            }
            return ret;
        }

        private TSpriteMetaAnimateFrame AddSprite(TextAttribute ta)
        {
            TSpriteMetaAnimateFrame ret;
            if (mSpriteCache.TryGetValue(ta.resSprite, out ret))
            {
                return ret;
            }
            string[] resSpr = ta.resSprite.Split(',');
            string file = resSpr[0];
            Cell.CPJResource res = this.AddCPJResource(file);
            if (res != null)
            {
                int anim;
                string spr_name = null;
                if (resSpr.Length >= 2)
                {
                    spr_name = resSpr[1];
                }
                else
                {
                    spr_name = res.Loader.DefaultSpriteName;
                }
                if (resSpr.Length >= 3)
                {
                    Parser.TryParseInt(resSpr[2], out anim);
                }
                else
                {
                    anim = 0;
                }
                if (spr_name != null)
                {
                    CSprite spr = res.GetSprite(spr_name);
                    if (spr != null)
                    {
                        ret.sprite = spr;
                        ret.anim = anim;
                        mSpriteCache.Add(ta.resSprite, ret);
                        return ret;
                    }
                }
            }
            return ret;
        }

        //----------------------------------------------------------------------------------------
        protected abstract Image AddImage(string path);

        protected abstract Cell.CPJResource AddCPJResource(string path);

        public abstract Drawable CreateDrawable(Region rg, object content);

        public interface Drawable : IDisposable
        {
            float CharWidth { get; }
            float CharHeight { get; }
            void Render(Graphics g, Region self, float x, float y, int deltaMS);
            void Hide(Region self, float x, float y);
        }

        //----------------------------------------------------------------------------------------

        #region _LineAndRegion_

        /// <summary>
        /// 一块渲染区域
        /// </summary>
        public class Region
        {
            private readonly BaseRichTextLayer mLayer;
            public bool IsBreak { get { return mText.EndsWith("\n"); } }
            public int CharStartIndex { get { return mStartIndex; } }
            public int CharEndIndex { get { return mEndIndex; } }
            public int CharCount { get { return mEndIndex - mStartIndex; } }
            public RectangleF Bounds { get { return mBounds; } }
            public TextAttribute Attribute { get { return mAttribute; } }
            public string Text { get { return mText; } }
            public int Index { get { return mIndex; } }

            internal Drawable Buffer
            {
                get { return m_buffer; }
                set
                {
                    if (m_buffer != null) m_buffer.Dispose();
                    m_buffer = value;
                }
            }

            private Drawable m_buffer;

            internal readonly TextAttribute mAttribute;
            internal int mStartIndex;
            internal int mEndIndex;
            internal string mText;
            internal RectangleF mBounds = new RectangleF();

            /// <summary>
            /// 位于mBlocks的位置
            /// </summary>
            internal int mIndex;

            internal Region(BaseRichTextLayer layer, TextAttribute ta)
            {
                this.mLayer = layer;
                this.mAttribute = ta;
            }

            internal Region[] Split(int charIndex)
            {
                if (charIndex >= 0 && charIndex < mText.Length)
                {
                    Region a = new Region(mLayer, mAttribute);
                    {
                        a.mStartIndex = this.mStartIndex;
                        a.mEndIndex = this.mStartIndex + charIndex;
                        a.mText = this.mText.Substring(0, charIndex);
                    }
                    Region b = new Region(mLayer, mAttribute);
                    {
                        b.mStartIndex = this.mStartIndex + charIndex;
                        b.mEndIndex = this.mEndIndex;
                        b.mText = this.mText.Substring(charIndex);
                    }
                    return new Region[] { a, b };
                }
                return null;
            }

            public void Render(Graphics g, float px, float py, int deltaMS)
            {
                if (m_buffer != null)
                {
                    m_buffer.Render(g, this, mBounds.X + px, mBounds.Y + py, deltaMS);
                }
                this.mLayer.OnDrawRegion(g, this, px, py);
            }

            public void Hide(float px, float py)
            {
                if (m_buffer != null)
                {
                    m_buffer.Hide(this, mBounds.X + px, mBounds.Y + py);
                }
            }

            internal void Dispose()
            {
                if (m_buffer != null)
                {
                    m_buffer.Dispose();
                    m_buffer = null;
                }
            }

            public override string ToString()
            {
                return string.Format("Region({0})", mText);
            }
        }

        public class Line
        {
            private readonly BaseRichTextLayer mLayer;
            internal int mIndex = 0;
            internal RectangleF mBounds = new RectangleF();
            internal readonly List<Region> mBlocks = new List<Region>();
            internal RichTextAlignment mAnchor = RichTextAlignment.taNA;
            public List<Region> AllRegions => mBlocks;
            public int Index
            {
                get
                {
                    return mIndex;
                }
            }
            public RectangleF Bounds
            {
                get
                {
                    return mBounds;
                }
            }
            public int CharStartIndex
            {
                get
                {
                    if (mBlocks.Count > 0)
                    {
                        return mBlocks[0].mStartIndex;
                    }
                    return -1;
                }
            }
            public int RegionCount
            {
                get
                {
                    return mBlocks.Count;
                }
            }
            public int CharEndIndex
            {
                get
                {
                    if (mBlocks.Count > 0)
                    {
                        return mBlocks[mBlocks.Count - 1].mEndIndex;
                    }
                    return -1;
                }
            }
            public RichTextAlignment Anchor
            {
                get { return mAnchor; }
            }
            internal Line(BaseRichTextLayer layer)
            {
                this.mLayer = layer;
            }

            internal void AddRegion(Region rg)
            {
                mBlocks.Add(rg);
                if (rg.Attribute.anchor != RichTextAlignment.taNA)
                {
                    this.mAnchor = rg.Attribute.anchor;
                }
            }

            public void RenderLine(Graphics g, float px, float py, int deltaMS)
            {
                for (int i = 0; i < mBlocks.Count; i++)
                {
                    mBlocks[i].Render(g, px, py, deltaMS);
                }
                mLayer.OnDrawLine(g, this, px, py);
            }
            public void HideLine(float px, float py)
            {
                for (int i = 0; i < mBlocks.Count; i++)
                {
                    mBlocks[i].Hide(px, py);
                }
            }

        }

        #endregion

        //----------------------------------------------------------------------------------------
    }

    //----------------------------------------------------------------------------------------

    public struct RichTextClickInfo
    {
        public BaseRichTextLayer.Region mRegion;
        public BaseRichTextLayer.Line mLine;
    }

    //----------------------------------------------------------------------------------------


    public class RichTextLayer : BaseRichTextLayer
    {
        public static bool TestDraw = false;
        public static uint TestFillContentSize = 0x80808080;
        public static uint TestDrawRegionSize = 0x000000ff;
        public static string DefaultImageRoot = "";
        public string ImageRoot = DefaultImageRoot;

        protected HashMap<string, Image> mImageMap = new HashMap<string, Image>();
        protected HashMap<string, Cell.CPJResource> mResMap = new HashMap<string, Cell.CPJResource>();

        /// <summary>
        /// 自己加载的图片
        /// </summary>
        protected HashMap<string, Image> mSelfImageMap = new HashMap<string, Image>();
        /// <summary>
        /// 自己加载的精灵
        /// </summary>
        protected HashMap<string, Cell.CPJResource> mSelfResMap = new HashMap<string, Cell.CPJResource>();


        public RichTextLayer(float width = 100, RichTextAlignment anchor = RichTextAlignment.taLEFT)
            : base(width, anchor)
        {
        }

        protected override Image AddImage(string file)
        {
            Image img = mImageMap.Get(file);
            if (img == null)
            {
                img = GraphicsDriver.Instance.CreateImage(ImageRoot + file);
                if (img != null)
                {
                    mSelfImageMap.Put(file, img);
                    mImageMap.Put(file, img);
                }
            }
            return img;
        }

        protected override Cell.CPJResource AddCPJResource(string file)
        {
            Cell.CPJResource res = mResMap.Get(file);
            if (res == null)
            {
                res = Cell.CPJResource.CreateResource(ImageRoot + file);
                if (res != null)
                {
                    mSelfResMap.Put(file, res);
                    mResMap.Put(file, res);
                }
            }
            return res;
        }


        /// <summary>
        /// 添加图片映射表
        /// </summary>
        /// <param name="file"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public virtual void AddImage(string file, Image img)
        {
            if (img != null)
            {
                mImageMap.Put(file, img);
            }
        }
        /// <summary>
        /// 添加精灵映射表
        /// </summary>
        /// <param name="file"></param>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual void AddResource(string file, Cell.CPJResource res)
        {
            if (res != null)
            {
                mResMap.Put(file, res);
            }
        }

        public override void Render(Graphics g, float px, float py, float pw, float ph, float cx, float cy, int deltaMS)
        {
            base.BeginRender(g);
            Blend cur_blend = g.CurrentBlend;
            try
            {
                if (!IsEnable)
                {
                    g.SetBlend(Blend.BLEND_MODE_GRAY);
                }
                //测试绘制区域.//
                if (TestDraw && ContentWidth > 0 && ContentHeight > 0)
                {
                    g.SetColor(TestFillContentSize);
                    switch (Alignment)
                    {
                        case RichTextAlignment.taLEFT:
                            g.FillRect(0, 0, ContentWidth, ContentHeight);
                            break;
                        case RichTextAlignment.taRIGHT:
                            g.FillRect(Width - ContentWidth, 0, ContentWidth, ContentHeight);
                            break;
                        case RichTextAlignment.taCENTER:
                            g.FillRect((Width - ContentWidth) / 2, 0, ContentWidth, ContentHeight);
                            break;
                    }
                }
                base.InternalRender(g, px, py, pw, ph, cx, cy, deltaMS);
            }
            finally
            {
                g.SetBlend(cur_blend);
            }
            base.AfterRender(g);
        }

        protected override void OnDrawRegion(Graphics g, Region rg, float px, float py)
        {
            if (TestDraw)
            {
                g.SetColor(TestDrawRegionSize);
                g.DrawRect(rg.Bounds.X + px, rg.Bounds.Y + py, rg.Bounds.Width, rg.Bounds.Height);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (mSelfImageMap != null)
            {
                foreach (KeyValuePair<string, Image> kvp in mSelfImageMap)
                {
                    kvp.Value.Dispose();
                }
                mSelfImageMap.Clear();
            }
            if (mSelfResMap != null)
            {
                foreach (KeyValuePair<string, Cell.CPJResource> kvp in mSelfResMap)
                {
                    kvp.Value.Dispose();
                }
                mSelfResMap.Clear();
            }
        }

        public override Drawable CreateDrawable(Region rg, object content)
        {
            if (content is TextDrawable)
            {
                return content as TextDrawable;
            }
            if (content is TImageRegion)
            {
                return new DrawImage(this, rg, (TImageRegion)content);
            }
            if (content is TSpriteMetaAnimateFrame)
            {
                return new DrawSprite(this, rg, (TSpriteMetaAnimateFrame)content);
            }
            return new DrawText(this, rg, content.ToString());
        }

        public class DrawText : Drawable
        {
            private TextLayer text;
            public DrawText(RichTextLayer layer, Region rg, string content)
            {
                TextAttribute ta = rg.Attribute;
                this.text = GraphicsDriver.Instance.CreateTextLayer(content, ta.fontName, ta.fontSize, ta.fontStyle, ta.borderCount);
                text.IsEnable = layer.IsEnable;
                text.FontColor = ta.fontColor;
                if (ta.borderCount > 0)
                {
                    text.BorderColor = ta.borderColor;
                    text.BorderTime = ta.borderCount;
                }
                else
                {
                    text.BorderColor = layer.BorderColor;
                    text.BorderTime = layer.BorderCount;
                }
            }
            public float CharWidth { get { return text.Width; } }
            public float CharHeight { get { return text.Height; } }
            public void Render(Graphics g, Region rg, float x, float y, int deltaMS)
            {
                g.DrawTextLayer(text, new RectangleF(x, y, text.Width, text.Height), AlignmentStyle.BottomLeft);
            }
            public void Hide(Region self, float x, float y) { }
            public void Dispose()
            {
                text.Dispose();
            }
        }
        public class DrawImage : Drawable
        {
            private TImageRegion image;
            private TextAttribute ta;
            public DrawImage(RichTextLayer layer, Region rg, TImageRegion img)
            {
                this.image = img;
                this.ta = rg.Attribute;
            }
            public float CharWidth
            {
                get
                {
                    if (ta.resImageZoom != null) { return ta.resImageZoom.Width; }
                    return image.sw;
                }
            }
            public float CharHeight
            {
                get
                {
                    if (ta.resImageZoom != null) { return ta.resImageZoom.Height; }
                    return image.sh;
                }
            }
            public void Render(Graphics g, Region rg, float x, float y, int deltaMS)
            {
                string text = rg.mText;
                int len = text.Length;
                g.BeginImage(image.image);
                if (ta.resImageZoom != null)
                {
                    float tw = ta.resImageZoom.Width;
                    float th = ta.resImageZoom.Height;
                    for (int i = 0; i < len; i++)
                    {
                        g.DrawImageZoom(x + i * tw, y, tw, th);
                    }
                }
                else
                {
                    for (int i = 0; i < len; i++)
                    {
                        g.DrawImage(x + i * image.sw, y);
                    }
                }
            }
            public void Hide(Region self, float x, float y) { }
            public void Dispose() { }
        }
        public class DrawSprite : Drawable
        {
            private CSprite spr;
            private int frame_count;
            private int curframe;
            private int curanim;
            private int mFramPassMS = 0;
            private int mFrameMS = 0;
            private CCD bounds;

            public DrawSprite(RichTextLayer layer, Region rg, TSpriteMetaAnimateFrame spr)
            {
                this.spr = spr.sprite;
                if (this.spr.Data.FPS > 0)
                {
                    mFrameMS = 1000 / this.spr.Data.FPS;
                }
                this.curanim = spr.anim;
                this.frame_count = spr.sprite.getFrameCount(spr.anim);
                this.bounds = spr.sprite.getVisibleBounds(spr.anim);
            }
            public float CharWidth { get { return bounds.Width; } }
            public float CharHeight { get { return bounds.Height; } }

            public void Render(Graphics g, Region rg, float x, float y, int deltaMS)
            {
                if (frame_count > 0)
                {
                    string text = rg.mText;
                    int len = text.Length;
                    float px;
                    for (int i = 0; i < len; i++)
                    {
                        px = x + i * bounds.Width;
                        spr.render(g, curanim, curframe, px - bounds.X1, y - bounds.Y1);
                    }

                    if (mFrameMS > 0)
                    {
                        mFramPassMS += deltaMS;
                        if (mFramPassMS > mFrameMS)
                        {
                            curframe++;
                            mFramPassMS = 0;
                        }
                    }
                    else
                    {
                        curframe++;
                    }

                    curframe = curframe % frame_count;
                }
            }

            public void Hide(Region self, float x, float y) { }
            public void Dispose() { }
        }

    }

    //----------------------------------------------------------------------------------------
}
