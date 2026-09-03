using DeepCore.GUI.Cell;
using DeepCore.GUI.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Display.SceneGraph.Cell
{
    public class DisplayCellAtlasNode : DisplayNode
    {
        public RectInteracviteComponent Rect { get; }
        public int TileIndex { get=> tileIndex; }
        public string TileKey { get => tileKey; }
        private CPJResource cpj;
        private CPJAtlas atlas;
        private int tileIndex;
        private string tileKey;
        public DisplayCellAtlasNode(CPJResource cpj, string atlasName, string tileKey = null, int tileIndex = 0)
        {
            this.Rect = base.Components.AddComponent<RectInteracviteComponent>();
            this.cpj = cpj;
            this.atlas = cpj.GetAtlas(atlasName);
            this.tileKey = tileKey;
            this.tileIndex = tileIndex;
            if(atlas != null)
            {
                this.tileIndex = atlas.GetIndexByKey(tileKey, tileIndex);
                var clip = atlas.GetAtlasRegion(tileIndex);
                this.Rect.Bounds = new Geometry.RectangleF(0, 0, clip.Width, clip.Height);
            }
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            if (atlas != null)
            {
                atlas.render(args.Graphics, tileIndex, 0, 0, Data.Trans.TRANS_NONE);
            }
        }
    }
}
