using DeepCore.Geometry;
using DeepCore.GUI.Cell.Game;
using DeepCore.GUI.Data;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    public class UIImageLayer : UIDisplayable
    {
        //public CPJAtlasMeta Meta { get; }
        public UIImageStyleMeta Style { get; }

        protected UIResourceImage loadImage;
        protected UIResourceCPJ loadCPJ;

        protected CSpriteController sprite;
        protected Image imageBuffer;
        protected Rectangle imageRegion;

        public UIImageLayer(UIFactory editor, CPJAtlasMeta atlas, UIImageStyleMeta style) : base(editor)
        {
            //this.Meta = atlas;
            this.Style = style;
            if (editor.ExistData(atlas?.CPJFile))
            {
                this.loadCPJ = editor.AddCPJ(atlas.CPJFile);
                if (loadCPJ != null)
                {
                    if (!string.IsNullOrEmpty(atlas.SpriteName))
                    {
                        var spr = loadCPJ.CPJ.GetSprite(atlas.SpriteName);
                        if (spr != null)
                        {
                            this.sprite = new CSpriteController(spr);
                        }
                    }
                    else if (!string.IsNullOrEmpty(atlas.ImagesName))
                    {
                        var images = loadCPJ.CPJ.GetAtlas(atlas.ImagesName);
                        if (images != null)
                        {
                            var index = images.GetIndexByKey(atlas.ImageKey, atlas.ImageIndex);
                            this.imageBuffer = images.GetTile(index);
                            this.imageRegion = images.GetAtlasRegion(index);
                        }
                    }
                }
            }
            else if (editor.ExistData(atlas?.ImagePath))
            {
                this.loadImage = editor.AddImage(atlas.ImagePath);
                if (loadImage != null)
                {
                    this.imageBuffer = loadImage.Image;
                    this.imageRegion = new Rectangle(0, 0, loadImage.Image.Width, loadImage.Image.Height);
                }
            }
        }
        protected override void Disposing()
        {
            loadImage?.Release();
            loadCPJ?.Release();
        }
        public void SetImage(UIResourceImage image, Rectangle? imageRegion = null)
        {
            if (image?.Image != null)
            {
                this.loadImage = image;
                this.imageBuffer = loadImage.Image;
                this.imageRegion = imageRegion ?? new Rectangle(0, 0, loadImage.Image.Width, loadImage.Image.Height);
            }
        }
        public override void Render(Graphics g, RectangleF bounds)
        {
            if (Style != null)
            {
                bounds = Style.Padding.Cut(bounds);
            }
            if (sprite != null)
            {
                var p = bounds.Center;
                sprite.Render(g, p.X, p.Y);
            }
            else if (imageBuffer != null)
            {
                g.BeginImage(imageBuffer);
                if (Style != null)
                {
                    var p = bounds.Center;
                    g.DrawImageBounds(imageRegion, Style.Align, bounds);
//                     g.SetColor(Color.Red);
//                     g.DrawRect(bounds);
                }
                else
                {
                    g.DrawImageRegion(imageRegion, bounds);
                }
            }
        }
    }
}
