using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{
    public partial class ImageSprite : DisplayNode
    {
        private readonly ImageGraphics mImage;

        public ImageSprite(string name = "") : base(name)
        {
            this.mImage = mGameObject.AddComponent<ImageGraphics>();
            this.Enable = false;
            this.EnableChildren = false;
        }

        public UnityEngine.UI.Image Graphics { get { return mImage; } }
        
        public void SetImage(DeepCore.Unity3D.Impl.UnityImage src)
        {
            this.mImage.SetImage(src); 
        }
        public void SetImage(DeepCore.Unity3D.Impl.UnityImage src, Rect clip, Vector2 pivot)
        {
            this.mImage.SetImage(src, clip, pivot);
        }
    }

}
