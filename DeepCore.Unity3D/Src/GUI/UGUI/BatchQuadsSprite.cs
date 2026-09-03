using DeepCore.GUI.Cell;
using DeepCore.GUI.Data;
using UnityEngine;

namespace DeepCore.Unity3D.UGUI
{
    public partial class BatchQuadsSprite : DisplayNode
    {
        private readonly BatchQuadsGraphics mGFX;

        public BatchQuadsSprite(string name = "") : base(name)
        {
            this.mGFX = mGameObject.AddComponent<BatchQuadsGraphics>();
            this.Enable = false;
            this.EnableChildren = false;
        }

        public BatchQuadsGraphics Graphics { get { return mGFX; } }

        public CPJAtlas Atlas
        {
            get { return mGFX.Atlas; }
            set { mGFX.Atlas = value; }
        }
        public BatchQuadsGraphics.BatchImageQuad[] Quads
        {
            get { return mGFX.Quads; }
        }
        public int QuadsCount
        {
            get { return mGFX.QuadsCount; }
        }
        public BatchQuadsGraphics.BatchImageQuad GetQuad(int i)
        {
            return mGFX.GetQuad(i);
        }
        public void SetQuadVisible(int i, bool visible)
        {
            mGFX.SetQuadVisible(i, visible);
        }
        public void SetQuad(int i, BatchQuadsGraphics.BatchImageQuad quad)
        {
            mGFX.SetQuad(i, quad);
        }
        public int AddQuad(BatchQuadsGraphics.BatchImageQuad qd)
        {
            return mGFX.AddQuad(qd);
        }
        public int AddQuad(int index)
        {
            return mGFX.AddQuad(index);
        }
        public int AddQuad(int index, ImageAnchor anchor, Vector2 pivot, Vector2 offset, Vector2 scale, float rotate)
        {
            return mGFX.AddQuad(index, anchor, pivot, offset, scale, rotate);
        }
        public int AddQuadImageFont(string text)
        {
            return mGFX.AddQuadImageFont(text);
        }
        public int AddQuadImageFont(string text, ImageAnchor anchor, Vector2 pivot, Vector2 offset, Vector2 scale, float rotate)
        {
            return mGFX.AddQuadImageFont(text, anchor, pivot, offset, scale, rotate);
        }
        public int AddQuadByKey(string key, ImageAnchor anchor, Vector2 pivot, Vector2 offset, Vector2 scale, float rotate)
        {
            return mGFX.AddQuadByKey(key, anchor, pivot, offset, scale, rotate);
        }
        public void ClearQuads()
        {
            mGFX.ClearQuads();
        }
    }
}
