using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{

    /// <summary>
    /// 只用于透明并且需要点击的地方
    /// </summary>
    public partial class InteractiveDummyGraphics : Image
    {
        private DisplayNode mBinding;
        public DisplayNode Binding { get { return mBinding; } }

        public InteractiveDummyGraphics()
        {
        }

        protected override void Start()
        {
            this.mBinding = DisplayNode.AsDisplayNode(gameObject);
            this.raycastTarget = true;
            base.Start();
        }

        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            if (mBinding.EnableChildren)
            {
                return base.Raycast(sp, eventCamera);
            }
            return false;
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            var size = rectTransform.sizeDelta;
            Color color = new Color(0, 0, 0, 0);
            vh.Clear();
            vh.AddVert(new Vector3(0, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(size.x, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(size.x, -size.y), color, Vector2.zero);
            vh.AddVert(new Vector3(0, -size.y), color, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
        
    }
}
