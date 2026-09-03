using DeepCore.GUI.Cell;
using DeepCore.GUI.Cell.Game;
using DeepCore.GUI.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Display.SceneGraph.Cell
{
    public class DisplayCellSpriteNode : DisplayNode
    {
        public RectInteracviteComponent Rect { get; }
        public CSpriteController Sprite { get => sprite; }

        private CPJResource cpj;
        private CSpriteController sprite;

        public DisplayCellSpriteNode(CPJResource cpj, string spriteName)
        {
            this.Rect = base.Components.AddComponent<RectInteracviteComponent>();
            this.cpj = cpj;
            var spr = cpj.GetSprite(spriteName);
            if (spr != null)
            {
                this.sprite = new CSpriteController(spr);
                this.Rect.Bounds = spr.getVisibleBounds().ToRect();
            }
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            if (sprite != null)
            {
                sprite.Render(args.Graphics);
            }
        }
    }
}
