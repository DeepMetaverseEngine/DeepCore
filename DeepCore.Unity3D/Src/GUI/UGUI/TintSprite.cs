using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepCore.Unity3D.UGUI
{

    public partial class TintSprite : DisplayNode
    {
        private readonly ImageGraphics mImage;

        public TintSprite(string name = "") : base(name)
        {
            this.mImage = mGameObject.AddComponent<ImageGraphics>();
            this.mImage.color = UnityEngine.Color.white;
            this.Enable = false;
            this.EnableChildren = false;
        }

        public ImageGraphics Graphics { get { return mImage; } }

        public UnityEngine.Color Color
        {
            get { return mImage.color; }
            set { mImage.color = value; }
        }

    }

}
