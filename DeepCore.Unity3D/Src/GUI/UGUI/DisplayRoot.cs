using System;
using UnityEngine;

namespace DeepCore.Unity3D.UGUI
{
    public partial class DisplayRoot : DisplayNode
    {
        private Canvas mCanvas;
        private RectTransform mCanvasTransform;

        public Canvas RootCanvas { get { return mCanvas; } }
        private CavaseMonoBehaviour canvasBehaviour;

        public DisplayRoot(Canvas canvas, string name = "") : base(name)
        {
            this.mCanvas = canvas;
            this.mCanvasTransform = canvas.GetComponent<RectTransform>();
            this.mTransform.SetParent(canvas.gameObject.transform);
            this.mGameObject.SetActive(true);
            this.EnableChildren = true;
            this.mTransform.localScale = Vector3.one;
            this.mTransform.localRotation = Quaternion.identity;
            this.UpdateSize();

            canvasBehaviour = canvas.gameObject.AddComponent<CavaseMonoBehaviour>();
            canvasBehaviour.displayRoot = this;
        }

        public DisplayRoot(Canvas canvas, GameObject obj) : base(obj)
        {
            this.mCanvas = canvas;
            this.mCanvasTransform = canvas.GetComponent<RectTransform>();
            this.mTransform.SetParent(canvas.gameObject.transform);
            this.mGameObject.SetActive(true);
            this.EnableChildren = true;
            this.mTransform.localScale = Vector3.one;
            this.mTransform.localRotation = Quaternion.identity;
            this.UpdateSize();

            canvasBehaviour = canvas.gameObject.AddComponent<CavaseMonoBehaviour>();
            canvasBehaviour.displayRoot = this;
        }

        public void OnRectTransformDimensionsChange()
        {
            //Debug.Log("RootCanvase OnRectTransformDimensionsChange " + Time.frameCount);
            UpdateSize();
        }

        protected override DisplayNodeBehaviour GenNodeBehavior()
        {
            return this.mGameObject.AddComponent<DisplayRootBehaviour>();
        }
        internal override void InternalUpdate()
        {
            // do not update from parent
        }
        internal void ForceUpdate()
        {
            //Debug.Log("ForceUpdate " + Time.frameCount);
            //this.UpdateSize();
            base.InternalUpdate();
            DisplayNode.FuckFuckList();
        }
        protected void UpdateSize()
        {
            try
            {
                mTransform.sizeDelta = mCanvasTransform.sizeDelta;
                mTransform.localPosition = Vector3.zero;
                //mTransform.position = new Vector3(0, mCanvasTransform.sizeDelta.y, 0);
                mTransform.localScale = Vector3.one;
                mTransform.localRotation = Quaternion.identity;
                mTransform.anchoredPosition = Vector2.zero;
            }
            catch (Exception err)
            {
                Debug.LogError(err.Message);
            }
        }

        public UnityEngine.Canvas Canvas { get { return mCanvas; } }

    }
    public class DisplayRootBehaviour : DisplayNodeBehaviour
    {
        void Update()
        {
            (mBinding as DisplayRoot).ForceUpdate();
        }
    }

    public class CavaseMonoBehaviour : MonoBehaviour
    {
        public DisplayRoot displayRoot;

        void OnRectTransformDimensionsChange()
        {
            displayRoot.OnRectTransformDimensionsChange();
        }

    }
}
