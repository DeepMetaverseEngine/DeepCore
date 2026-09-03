using System;
using System.Collections.Generic;
using System.Text;
using DeepCore.GUI.Display;
using DeepCore.GUI.Loader;

namespace DeepCore.GUI.Editor
{
    public partial class ImageLoader : AbstractLoader
    {
        private Image mImage;

    	public override void ReleaseTexture()
        {
            if(mImage != null)
            {
                mImage.ReleaseTexture();
            }
        }

        public ImageLoader(string name) : base(name) { }

        public override Image GetImage(string filePath)
        {
            if (FileName == null) { return null; }
            if (mImage == null) { mImage = GraphicsDriver.Instance.CreateImage(filePath); }
            return mImage;
        }

        public override bool IsLoaded()
        {
            return mImage != null;
        }

        public override void Dispose()
        {
            if (mImage != null)
            {
                mImage.Dispose();
                mImage = null;
            }
            base.Dispose();
        }

        public override Cell.CPJResource GetAtlasResource(string filePath)
        {
            return null;
        }
    }
}
