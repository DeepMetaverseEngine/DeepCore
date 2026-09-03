using DeepCore.Geometry;
using DeepCore.GUI.Data;
using System;

namespace DeepCore.GUI.Display
{

    public abstract partial class TextLayer : IDisposable
    {
        public static Color DefaultDisableTextColorRGBA = 0x808080FF;

        public static TypeAllocRecorder Alloc { get; private set; } = new TypeAllocRecorder(typeof(TextLayer)) { Verbos = false };
        public static long RefCount { get => Alloc.AllocCount; }
        public static long AliveCount { get => Alloc.ActiveCount; }
        private bool m_disposed = false;

        protected string mText = "";

        protected Color mFontColorRGBA = 0xffffffff;
        protected Color mBorderColorRGBA = 0x000000ff;
        protected TextBorderStyle mBorderTime;
        protected RectangleF mExpectSize;
        protected float mFontSize;
        protected TextFontStyle mFontStyle;
        protected RectangleF mBounds;
        protected bool isDirty = true;

        protected bool isEnable = true;

        public TextLayer(string t, int size, TextFontStyle style = TextFontStyle.Plain, TextBorderStyle border = TextBorderStyle.None)
        {
            Alloc.RecordConstructor(GetType());
            this.mText = t;
            this.mFontSize = Math.Max(1, size);
            this.mFontStyle = style;
            this.mBorderTime = border;
        }
        ~TextLayer()
        {
            if (!m_disposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        public void Dispose()
        {
            if (m_disposed) { return; }
            Alloc.RecordDispose(GetType());
            Disposing();
            m_disposed = true;
        }
        protected abstract void Disposing();


        public float Width
        {
            get { return mBounds.Width; }
        }
        public float Height
        {
            get { return mBounds.Height; }
        }
        public Vector2 Size { get => mBounds.Size; }

        public bool IsEnable
        {
            get { return isEnable; }
            set
            {
                if (this.isEnable != value)
                {
                    this.isEnable = value;
                    this.isDirty = true;
                }
            }
        }

        public Vector2 ExpectSize
        {
            get { return mExpectSize.Size; }
            set
            {
                if (!value.Equals(mExpectSize.Size))
                {
                    this.mExpectSize.Size = value;
                    this.isDirty = true;
                }
            }
        }

        public string Text
        {
            get { return mText; }
            set
            {
                if (!value.Equals(this.mText))
                {
                    this.mText = value;
                    this.isDirty = true;
                }
            }
        }

        public float FontSize
        {
            set
            {
                value = Math.Max(1, value);
                if (mFontSize != value)
                {
                    mFontSize = value;
                    this.isDirty = true;
                }
            }
            get { return mFontSize; }
        }

        public TextFontStyle TextFontStyle
        {
            set
            {
                if (mFontStyle != value)
                {
                    mFontStyle = value;
                    this.isDirty = true;
                }
            }
            get { return mFontStyle; }
        }

        public TextBorderStyle BorderTime
        {
            get { return mBorderTime; }
            set
            {
                if (this.mBorderTime != value)
                {
                    this.mBorderTime = value;
                    this.isDirty = true;
                }
            }
        }

        public Color FontColor
        {
            get { return mFontColorRGBA; }
            set
            {
                if (this.mFontColorRGBA != value)
                {
                    this.mFontColorRGBA = value;
                    this.isDirty = true;
                }
            }
        }

        public Color BorderColor
        {
            get
            {
                return mBorderColorRGBA;
            }
            set
            {
                if (this.mBorderColorRGBA != value)
                {
                    this.mBorderColorRGBA = value;
                    this.isDirty = true;
                }
            }
        }


        public void SetFontColor(int rgb, float alpha = 1.0f)
        {
            uint uc = (uint)((rgb & 0xffffff) | (int)Math.Min(alpha * 255, 255));
            FontColor = (uc);
        }

        public void SetFontColor(uint rgba)
        {
            FontColor = rgba;
        }

        public void SetBorderColor(int rgb, int alpha = 0xff)
        {
            uint uc = (uint)((rgb & 0xffffff) | (int)Math.Min(alpha * 255, 255));
            BorderColor = (uc);
        }

        //public abstract void Render(Graphics g, RectangleF rect, AlignmentStyle alignment);
    }
}
