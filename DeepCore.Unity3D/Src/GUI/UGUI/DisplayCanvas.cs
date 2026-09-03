using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{
    public partial class DisplayCanvas : DisplayNode
    {
        private readonly Canvas mCanvas;
        //private readonly CanvasScaler mCanvasScaler;
        private readonly GraphicRaycaster mRaycaster;

        public Canvas BindingCanvas { get { return mCanvas; } }

        public DisplayCanvas(string name = "") : base(name)
        {
            this.EnableChildren = true;
            this.mCanvas = mGameObject.AddComponent<Canvas>();
            //this.mCanvasScaler = mGameObject.AddComponent<CanvasScaler>();
            this.mRaycaster = mGameObject.AddComponent<GraphicRaycaster>();
        }

        protected override CanvasRenderer GenCanvasRenderer()
        {
            return null;
        }

        protected override void OnStart()
        {
            base.OnStart();
            this.mTransform.anchorMin = new Vector2(.5f, .5f);
            this.mTransform.anchorMax = new Vector2(.5f, .5f);
            this.mTransform.pivot = new Vector2(.5f, .5f);
        }
    }
}
