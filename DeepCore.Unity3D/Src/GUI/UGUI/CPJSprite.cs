using DeepCore.GUI.Cell.Game;
using UnityEngine;

namespace DeepCore.Unity3D.UGUI
{
    public partial class CPJSprite : DisplayNode
    {
        private readonly CPJSpriteGraphics mGraphics;
        private CSpriteController mSprite;
        private CCD mVisibleBounds;

        public CPJSprite(string name) : base(name)
        {
            this.Enable = false;
            this.EnableChildren = false;
            this.mGraphics = mGameObject.AddComponent<CPJSpriteGraphics>();
        }
        public CPJSprite(CSpriteMeta meta) : this(meta.Data.Name)
        {
            this.SpriteMeta = meta;
        }
        protected override void OnDispose()
        {
            if (mSprite != null) mSprite.Dispose();
            base.OnDispose();
        }
        public void SetPreferredBounds()
        {
            if (mSprite != null)
            {
                this.Size2D = new Vector2(mVisibleBounds.Width, mVisibleBounds.Height);
                Vector2 pivot = new Vector2(
                    CMath.GetRate(0, mVisibleBounds.X1, mVisibleBounds.X2),
                    1f - CMath.GetRate(0, mVisibleBounds.Y1, mVisibleBounds.Y2));
                this.mTransform.pivot = pivot;
            }
        }

        public CSpriteMeta SpriteMeta
        {
            get { return mSprite.Meta; }
            set
            {
                if (mSprite != null) mSprite.Dispose();
                this.mSprite = new CSpriteController(value);
                this.mGraphics.SetSpriteMeta(this);
                this.mVisibleBounds = mSprite.Meta.getVisibleBounds();
            }
        }
        public CPJSpriteGraphics Graphics
        {
            get { return mGraphics; }
        }
        public CSpriteController Controller
        {
            get { return mSprite; }
        }
        public CCD VisibleBounds
        {
            get { return mVisibleBounds; }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (this.mSprite != null)
            {
                this.mGraphics.SetFrame(mSprite.CurrentAnimate, mSprite.CurrentFrame);
                this.mSprite.Update(Mathf.FloorToInt(Time.deltaTime * 1000));
            }
        }

    }
}
