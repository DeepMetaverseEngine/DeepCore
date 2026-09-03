using DeepCore.Geometry;
using DeepCore.GUI.Cell;
using DeepCore.GUI.Cell.Game;
using DeepCore.GUI.Data;
using DeepCore.IO;
using System;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    public class UILayout : UIDisplayable
    {
        public UILayoutMeta Meta { get; }
        public Rectangle? ImageRegion { get; }

        protected UIResourceImage loadImage;
        protected UIResourceCPJ loadCPJ;
        protected CSpriteController sprite;
        protected Image imageBuffer;
        protected Rectangle imageRegion;

        public UILayout(UIFactory editor, UILayoutMeta meta) : base(editor)
        {
            this.Meta = meta;
            if (editor.ExistData(Meta.ImageAtlas?.CPJFile))
            {
                this.loadCPJ = editor.AddCPJ(Meta.ImageAtlas?.CPJFile);
                if (loadCPJ != null)
                {
                    if (!string.IsNullOrEmpty(Meta.ImageAtlas.SpriteName))
                    {
                        var spr = loadCPJ.CPJ.GetSprite(Meta.ImageAtlas.SpriteName);
                        if (spr != null)
                        {
                            this.sprite = new CSpriteController(spr);
                        }
                    }
                    else if (!string.IsNullOrEmpty(Meta.ImageAtlas?.ImagesName))
                    {
                        var images = loadCPJ.CPJ.GetAtlas(Meta.ImageAtlas.ImagesName);
                        if (images != null)
                        {
                            var index = images.GetIndexByKey(Meta.ImageAtlas.ImageKey, Meta.ImageAtlas.ImageIndex);
                            this.imageBuffer = images.GetTile(index);
                            this.imageRegion = images.GetAtlasRegion(index);
                            this.ImageRegion = imageRegion;
                        }
                    }
                }
            }
            else if (editor.ExistData(Meta.ImageAtlas?.ImagePath))
            {
                this.loadImage = editor.AddImage(Meta.ImageAtlas?.ImagePath);
                if (loadImage?.Image != null)
                {
                    this.imageBuffer = loadImage.Image;
                    this.imageRegion = new Rectangle(0, 0, loadImage.Image.Width, loadImage.Image.Height);
                    this.ImageRegion = imageRegion;
                }
            }
        }
        protected override void Disposing()
        {
            loadImage?.Release();
            loadCPJ?.Release();
        }
        //----------------------------------------------------------------------------------------------
        public virtual void RenderGauge(Graphics g, RectangleF bounds, GaugeOrientation gauge, float grate)
        {
            var gbounds = bounds;
            switch (gauge)
            {
                case GaugeOrientation.LEFT_2_RIGHT:
                    gbounds.width = gbounds.Width * grate;
                    this.Render(g, gbounds);
                    break;
                case GaugeOrientation.RIGTH_2_LEFT:
                    gbounds.width = gbounds.Width * grate;
                    gbounds.x = bounds.Right - gbounds.Width;
                    this.Render(g, gbounds);
                    break;
                case GaugeOrientation.TOP_2_BOTTOM:
                    gbounds.height = gbounds.Height * grate;
                    this.Render(g, gbounds);
                    break;
                case GaugeOrientation.BOTTOM_2_TOP:
                    gbounds.height = gbounds.Height * grate;
                    gbounds.y = bounds.Bottom - gbounds.Height;
                    this.Render(g, gbounds);
                    break;
                case GaugeOrientation.FAN:
                    if (Meta.Style == UILayoutStyle.COLOR)
                    {
                        g.SetColor(Meta.BackColor);
                        g.FillRectEllipse(bounds, 0, 360 - 360 * grate);
                    }
                    else if (Meta.Style == UILayoutStyle.IMAGE_STYLE_BACK_4)
                    {
                        g.BeginImage(imageBuffer);
                        g.DrawImageEllipse(bounds, 0, 360 - 360 * grate);
                    }
                    break;
            }
        }
        //----------------------------------------------------------------------------------------------

        public override void Render(Graphics g, RectangleF bounds)
        {
            var w = bounds.width;
            var h = bounds.height;
            g.PushTransform();
            try
            {
                g.Translate(bounds.Location);
                var style = Meta.Style;
                if (style == UILayoutStyle.NULL)
                {
                    return;
                }
                else if (style == UILayoutStyle.COLOR)
                {
                    g.SetColor(Meta.BackColor);
                    g.FillRect(0, 0, w, h);
                    g.SetColor(Meta.BorderColor);
                    g.DrawRect(0, 0, w, h);
                }
                else if (sprite != null)
                {
                    switch (style)
                    {
                        case UILayoutStyle.SPRITE:
                            sprite.Render(g, w / 2, h / 2);
                            break;
                    }
                }
                else if (imageBuffer != null)
                {
                    g.BeginImage(imageBuffer);
                    switch (style)
                    {
                        case UILayoutStyle.IMAGE_STYLE_BACK_4:
                            RenderBack4(g, w, h);
                            break;
                        case UILayoutStyle.IMAGE_STYLE_BACK_4_CENTER:
                            RenderBack4Center(g, w, h);
                            break;

                        case UILayoutStyle.IMAGE_STYLE_ALL_9:
                            RenderAll9(g, w, h);
                            break;
                        case UILayoutStyle.IMAGE_STYLE_H_012:
                            RenderH012(g, w, h);
                            break;
                        case UILayoutStyle.IMAGE_STYLE_V_036:
                            RenderV036(g, w, h);
                            break;
                        case UILayoutStyle.IMAGE_STYLE_H_012_345:
                            Render_012_345(g, w, h);
                            break;
                        case UILayoutStyle.IMAGE_STYLE_V_036_147:
                            Render_036_147(g, w, h);
                            break;
                        case UILayoutStyle.IMAGE_STYLE_H_MIRROR:
                            RenderHMirror(g, w, h);
                            break;
                        case UILayoutStyle.IMAGE_STYLE_V_MIRROR:
                            RenderVMirror(g, w, h);
                            break;
                        default:
                            break;
                    }
                }
            }
            finally
            {
                g.PopTransform();
            }
        }

        private void RenderBack4(Graphics g, float w, float h)
        {
            var region = imageRegion;
            g.DrawImageRegion(new RectangleF(region.x, region.y, region.width, region.height), new RectangleF(0, 0, w, h));
        }

        private void RenderBack4Center(Graphics g, float w, float h)
        {
            var region = imageRegion;
            float iw = region.width;
            float ih = region.height;
            float tx = (w - iw) * 0.5f;
            float ty = (h - ih) * 0.5f;
            g.DrawImageRegion(new RectangleF(region.x, region.y, region.width, region.height), new RectangleF(tx, ty, iw, ih));
        }

        private void RenderAll9(Graphics g, float w, float h)
        {
            //             var region = imageRegion;
            //             var clip = this.Meta.ImageClip;
            //             g.DrawImageRegion(
            //                 region.x + clip.Left,
            //                 region.y + clip.Top,
            //                 region.width - clip.Left - clip.Right,
            //                 region.height - clip.Top - clip.Bottom,
            //                 Trans.TRANS_NONE,
            //                 clip.Left,
            //                 clip.Top,
            //                 w - clip.Left - clip.Right,
            //                 h - clip.Top - clip.Bottom);
            Render_012_345_678(g, w, h);
        }

        private void RenderH012(Graphics g, float w, float h)
        {
            var region = imageRegion;
            var clip = this.Meta.ImageClip;
            var c = clip;
            if (c.Left + c.Right > w)
            {
                c.Left = w * (c.Left / (c.Left + c.Right));
                c.Right = w - c.Left;
            }
            g.DrawImageRegion(
                region.x, region.y, clip.Left, region.height,
                0, 0, clip.Left, h);
            g.DrawImageRegion(
                region.x + clip.Left, region.y, region.width - clip.Left - clip.Right, region.height,
                clip.Left, 0, w - clip.Left - clip.Right, h);
            g.DrawImageRegion(
                region.x + region.width - clip.Right, region.y, clip.Right, region.height,
                w - clip.Right, 0, clip.Right, h);
        }
        private void RenderV036(Graphics g, float w, float h)
        {
            var region = imageRegion;
            var clip = this.Meta.ImageClip;
            var c = clip;
            if (c.Top + c.Bottom > h)
            {
                c.Top = h * (c.Top / (c.Top + c.Bottom));
                c.Bottom = h - c.Top;
            }
            g.DrawImageRegion(region.x, region.y, region.width, clip.Top,
                0, 0, w, c.Top);
            g.DrawImageRegion(region.x, region.y + clip.Top, region.width, region.height - clip.Top - clip.Bottom,
                0, c.Top, w, h - c.Top - c.Bottom);
            g.DrawImageRegion(region.x, region.y + region.height - clip.Bottom, region.width, clip.Bottom,
                0, h - c.Bottom, w, c.Bottom);
        }

        private void Render_012_345_678(Graphics g, float w, float h)
        {
            var region = imageRegion;
            var clip = this.Meta.ImageClip;
            var c = clip;
            if (c.Left + c.Right > w)
            {
                c.Left = w * (c.Left / (c.Left + c.Right));
                c.Right = w - c.Left;
            }
            if (c.Top + c.Bottom > h)
            {
                c.Top = h * (c.Top / (c.Top + c.Bottom));
                c.Bottom = h - c.Top;
            }
            {
                // top left
                g.DrawImageRegion(region.x, region.y, clip.Left, clip.Top,
                    0, 0, c.Left, c.Top);
                // top middle
                g.DrawImageRegion(region.x + clip.Left, region.y, region.width - clip.Left - clip.Right, clip.Top,
                    c.Left, 0, w - c.Left - c.Right, c.Top);
                // top right
                g.DrawImageRegion(region.x + region.width - clip.Right, region.y, clip.Right, clip.Top,
                    w - c.Right, 0, c.Right, c.Top);
            }
            {
                // center left
                g.DrawImageRegion(region.x, region.y + clip.Top, clip.Left, region.height - clip.Top - clip.Bottom,
                    0, c.Top, c.Left, h - c.Top - c.Bottom);
                // center middle
                g.DrawImageRegion(region.x + clip.Left, region.y + clip.Top, region.width - clip.Left - clip.Right, region.height - clip.Top - clip.Bottom,
                    c.Left, c.Top, w - c.Left - c.Right, h - c.Top - c.Bottom);
                // center right
                g.DrawImageRegion(region.x + region.width - clip.Right, region.y + clip.Top, clip.Right, region.height - clip.Top - clip.Bottom,
                    w - c.Right, c.Top, c.Right, h - c.Top - c.Bottom);
            }
            {
                // bottom left
                g.DrawImageRegion(region.x, region.y + region.height - clip.Bottom, clip.Left, clip.Bottom,
                    0, h - c.Bottom, c.Left, c.Bottom);
                // bottom middle
                g.DrawImageRegion(region.x + clip.Left, region.y + region.height - clip.Bottom, region.width - clip.Left - clip.Right, clip.Bottom,
                    c.Left, h - c.Bottom, w - c.Left - c.Right, c.Bottom);
                // bottom right
                g.DrawImageRegion(region.x + region.width - clip.Right, region.y + region.height - clip.Bottom, clip.Right, clip.Bottom,
                    w - c.Right, h - c.Bottom, c.Right, c.Bottom);
            }
        }
        private void Render_012_345(Graphics g, float w, float h)
        {
            var region = imageRegion;
            var clip = this.Meta.ImageClip;
            var c = clip;
            if (c.L + c.L > w)
            {
                c.L = w / 3f;
            }
            if (c.T + c.B > h)
            {
                c.T = h * (c.T / (c.T + c.B));
                c.B = h - c.T;
            }
            {
                // top left
                g.DrawImageRegion(region.x, region.y, clip.Left, clip.Top,
                    0, 0, c.Left, c.Top);
                // top middle
                g.DrawImageRegion(region.x + clip.Left, region.y, region.width - clip.Left - clip.Right, clip.Top,
                    c.Left, 0, w - c.Left - c.Right, c.Top);
                // top right
                g.DrawImageRegion(region.x + region.width - clip.Right, region.y, clip.Right, clip.Top,
                    w - c.Right, 0, c.Right, c.Top);
            }
            {
                // center left
                g.DrawImageRegion(region.x, region.y + clip.Top, clip.Left, region.height - clip.Top - clip.Bottom,
                    0, c.Top, c.Left, h - c.Top - c.Bottom);
                // center middle
                g.DrawImageRegion(region.x + clip.Left, region.y + clip.Top, region.width - clip.Left - clip.Right, region.height - clip.Top - clip.Bottom,
                    c.Left, c.Top, w - c.Left - c.Right, h - c.Top - c.Bottom);
                // center right
                g.DrawImageRegion(region.x + region.width - clip.Right, region.y + clip.Top, clip.Right, region.height - clip.Top - clip.Bottom,
                    w - c.Right, c.Top, c.Right, h - c.Top - c.Bottom);
            }
            {
                // bottom left
                g.DrawImageRegion(region.x, region.y, clip.Left, clip.Top, Trans.TRANS_MIRROR_Y,
                    0, h - c.Bottom, c.Left, c.Bottom);
                // bottom middle
                g.DrawImageRegion(region.x + clip.Left, region.y, region.width - clip.Left - clip.Right, clip.Top, Trans.TRANS_MIRROR_Y,
                    c.Left, h - c.Bottom, w - c.Left - c.Right, c.Bottom);
                // bottom right
                g.DrawImageRegion(region.x + region.width - clip.Right, region.y, clip.Right, clip.Top, Trans.TRANS_MIRROR_Y,
                    w - c.Right, h - c.Bottom, c.Right, c.Bottom);
            }
        }
        private void Render_036_147(Graphics g, float w, float h)
        {
            var r = imageRegion;
            var clip = this.Meta.ImageClip;
            var c = clip;
            if (c.L + c.R > w)
            {
                c.L = w * (c.L / (c.L + c.R));
                c.R = w - c.L;
            }
            if (c.T + c.T > h)
            {
                c.T = h / 3f;
            }
            {
                // top left
                g.DrawImageRegion(r.x, r.y, clip.L, clip.T,
                    0, 0, c.L, c.T);
                // top middle
                g.DrawImageRegion(r.x + clip.L, r.y, r.W - clip.L - clip.R, clip.T,
                    c.L, 0, w - c.L - c.R, c.T);
                // top right
                g.DrawImageRegion(r.x, r.y, clip.L, clip.T, Trans.TRANS_MIRROR_X,
                    w - c.R, 0, c.R, c.T);
            }
            {
                // center left
                g.DrawImageRegion(r.x, r.y + clip.T, clip.L, r.H - clip.T - clip.B,
                    0, c.T, c.L, h - c.T - c.B);
                // center middle
                g.DrawImageRegion(r.x + clip.L, r.y + clip.T, r.width - clip.L - clip.R, r.H - clip.T - clip.B,
                    c.L, c.T, w - c.L - c.R, h - c.T - c.B);
                // center right
                g.DrawImageRegion(r.x, r.y + clip.T, clip.L, r.H - clip.T - clip.B, Trans.TRANS_MIRROR_X,
                    w - c.R, c.T, c.R, h - c.T - c.B);
            }
            {
                // bottom left
                g.DrawImageRegion(r.x, r.y + r.height - clip.Bottom, clip.Left, clip.Bottom,
                    0, h - c.Bottom, c.Left, c.Bottom);
                // bottom middle
                g.DrawImageRegion(r.x + clip.Left, r.y + r.height - clip.Bottom, r.width - clip.Left - clip.Right, clip.Bottom,
                    c.Left, h - c.Bottom, w - c.Left - c.Right, c.Bottom);
                // bottom right
                g.DrawImageRegion(r.x, r.y + r.height - clip.Bottom, clip.Left, clip.Bottom, Trans.TRANS_MIRROR_X,
                    w - c.Right, h - c.Bottom, c.Right, c.Bottom);
            }
        }
        private void RenderHMirror(Graphics g, float w, float h)
        {
            var r = imageRegion;
            g.DrawImageRegion(r.X, r.Y, r.W, r.H, 0, 0, w / 2, h);
            g.DrawImageRegion(r.X, r.Y, r.W, r.H, Trans.TRANS_MIRROR_X, w / 2, 0, w / 2, h);
        }
        private void RenderVMirror(Graphics g, float w, float h)
        {
            var r = imageRegion;
            g.DrawImageRegion(r.X, r.Y, r.W, r.H, 0, 0, w, h / 2);
            g.DrawImageRegion(r.X, r.Y, r.W, r.H, Trans.TRANS_MIRROR_Y, 0, h / 2, w, h / 2);

        }



    }
}
