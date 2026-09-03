using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepCore.Unity3D.Impl
{


    public partial class UnityTextLayer : TextLayer
    {
        internal Texture2D mTexture;
        private UnityImage mBuffer;

        public UnityTextLayer(string text, float size, TextFontStyle style = TextFontStyle.Plain, TextBorderStyle border = TextBorderStyle.None)
            : base(text, (int)size, style, border)
        {
            isDirty = true;
        }

        internal void Refresh()
        {
            if (this.isDirty)
            {
                this.isDirty = false;
                if (mBuffer != null)
                {
                    mBuffer.Dispose();
                    mBuffer = null;
                    mTexture = null;
                }

                if (string.IsNullOrEmpty(mText))
                {
                    return;
                }
            
                int boundW = 0;
                int boundH = 0;
//                 mTexture = UnityDriver.Platform.SysFontTexture(
//                     mText,
//                     false,
//                     mFontStyle,
//                     Math.Max(1.0f, mFontSize),
//                     isEnable ? mFontColorRGBA : DefaultDisableTextColorRGBA,
//                     mBorderTime,
//                     mBorderColorRGBA,
//                     new Vector2(mExpectSize.width, mExpectSize.height),
//                     out boundW,
//                     out boundH);
                if (mTexture != null)
                {
                    mBounds.Width = boundW;
                    mBounds.Height = boundH;
                    mBuffer = new UnityImage(mTexture, boundW, boundH, mText);
                }
            }
        }
        protected override void Disposing()
        {
            if (mBuffer != null)
            {
                mBuffer.Dispose();
            }
            mTexture = null;
        }
        //         public override Image GetBuffer()
        //         {
        //             Refresh();
        //             return mBuffer;
        //         }
//         public override void Render(GUI.Display.Graphics g, Geometry.RectangleF rect, AlignmentStyle alignment)
//         {
//             Refresh();
// 
//         }
     
    }
}
