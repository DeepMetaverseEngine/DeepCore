using DeepCore;
using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DeepCore.GUI.Cell
{
    public abstract partial class CPJAtlas : IDisposable
    {
        protected CPJResource set;
        protected ImagesSet img;
        private HashMap<String, int> imageKeys;
        public string Name { get => img.Name; }
        public abstract IEnumerable<Image> Textures { get; }
        public void ReleaseTexture()
        {
            foreach (var t in Textures)
            {
                t.ReleaseTexture();
            }
        }
        public CPJAtlas(ImagesSet img, CPJResource res)
        {
            this.set = res;
            this.img = img;
            this.imageKeys = new HashMap<String, int>();
            for (int i = 0; i < img.Count; i++)
            {
                if (img.ClipsKey[i] != null && !string.IsNullOrEmpty(img.ClipsKey[i]))
                {
                    imageKeys.Put(img.ClipsKey[i], i);
                }
            }
        }
        public ImagesSet ImagesSet { get { return img; } }

        public int Count { get { return img.Count; } }

        public bool IsNullTile(int index)
        {
            return img.getClipW(index) == 0;
        }

        abstract public Image GetTile(int index);

        public bool ForEachTiles<ST>(ST st, ForEachPredicate<ST, int, string, Image, Rectangle> action)
        {
            for (int i = 0; i < img.Count; i++)
            {
                if (action(st, i, img.getClipKey(i), GetTile(i), GetAtlasRegion(i)))
                {
                    return true;
                }
            }
            return false;
        }

        public int GetIndexByKey(string key)
        {
            int ret = -1;
            if (key != null && imageKeys.TryGetValue(key, out ret))
            {
                return ret;
            }
            return -1;
        }
        public int GetIndexByKey(string key, int defaultIndex)
        {
            if (key != null && imageKeys.TryGetValue(key, out var ret))
            {
                return ret;
            }
            return defaultIndex;
        }


        /// <summary>
        /// 获取渲染图片区域
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        abstract public Rectangle GetAtlasRegion(int index);

        public Rectangle GetClipRect(int index)
        {
            return new Rectangle(
                    img.getClipX(index),
                    img.getClipY(index),
                    img.getClipW(index),
                    img.getClipH(index));
        }


        public int getWidth(int Index)
        {
            return img.getClipW(Index);
        }

        public int getHeight(int Index)
        {
            return img.getClipH(Index);
        }

        public bool render(Graphics g, string key, float x, float y, Trans trans)
        {
            if (key != null && imageKeys.TryGetValue(key, out var index))
            {
                render(g, index, x, y, trans);
                return true;
            }
            return false;
        }


        abstract public void render(Graphics g, int index, float x, float y, Trans trans);

        abstract public void begin(Graphics g);

        abstract public void addVertex(VertexBuffer v, int index, float x, float y, Trans trans, uint color_rgba);

        public virtual void Dispose()
        {
            img = null;
            set = null;
            if (imageKeys != null)
            {
                imageKeys.Clear();
                imageKeys = null;
            }

        }

    }

    //------------------------------------------


    public partial class CPJAtlasGroup : CPJAtlas
    {
        private Image atlas;
        public Image Src { get => atlas; }
        public CPJAtlasGroup(ImagesSet img, CPJResource res)
            : base(img, res)
        {
            atlas = res.Loader.LoadImage(img.Name + "." + img.Extention);
        }
        override public Image GetTile(int index)
        {
            return atlas;
        }
        override public Rectangle GetAtlasRegion(int index)
        {
            var clip = img.Clips[index];
            return new Rectangle(
                clip.ClipX,
                clip.ClipY,
                clip.ClipW,
                clip.ClipH);
        }
        override public void render(Graphics g, int index, float x, float y, Trans trans)
        {
            if (img.Clips[index].ClipW > 0)
            {
                g.BeginImage(atlas);
                g.DrawImageRegion(
                    img.Clips[index].ClipX,
                    img.Clips[index].ClipY,
                    img.Clips[index].ClipW,
                    img.Clips[index].ClipH,
                    trans, x, y
                    );
            }
        }

        override public void begin(Graphics g)
        {
            g.BeginImage(atlas);
        }
        override public void addVertex(VertexBuffer v, int index, float x, float y, Trans trans, uint color_rgba)
        {
            if (img.Clips[index].ClipW > 0)
            {
                VertexUtils.AddImageQuard(v, atlas, color_rgba,
                    img.Clips[index].ClipX,
                    img.Clips[index].ClipY,
                    img.Clips[index].ClipW,
                    img.Clips[index].ClipH,
                    trans, x, y);
            }
        }
        override public void Dispose()
        {
            if (atlas != null)
            {
                atlas.Dispose();
                atlas = null;
            }

            base.Dispose();
        }

        public override IEnumerable<Image> Textures
        {
            get { return atlas != null ? new Image[] { atlas } : new Image[0]; }
        }
    }

    public partial class CPJAtlasSplitGroup : CPJAtlas
    {
        private Image[,] mPartMatrix;
        private int mSplitSize;
        private int mPartCountX;
        private int mPartCountY;

        public CPJAtlasSplitGroup(ImagesSet img, CPJResource res)
            : base(img, res)
        {
            mSplitSize = img.SplitSize;
            mPartCountX = (int)CMath.NextPOT(img.TotalW) / mSplitSize;
            mPartCountY = (int)CMath.NextPOT(img.TotalH) / mSplitSize;
            mPartMatrix = new Image[mPartCountX, mPartCountY];
            for (int x = 0; x < mPartCountX; x++)
            {
                for (int y = 0; y < mPartCountY; y++)
                {
                    string img_path = string.Format("{0}_{1}_{2}.{3}", img.Name, x, y, img.Extention);
                    mPartMatrix[x, y] = res.Loader.LoadImage(img_path);
                }
            }
        }


        override public Image GetTile(int index)
        {
            int bx = ((int)img.Clips[index].ClipX) / mSplitSize;
            int by = ((int)img.Clips[index].ClipY) / mSplitSize;
            Image pTexture = mPartMatrix[bx, by];
            return pTexture;
        }
        override public Rectangle GetAtlasRegion(int index)
        {
            return new Rectangle(
                img.Clips[index].ClipX % mSplitSize,
                img.Clips[index].ClipY % mSplitSize,
                img.Clips[index].ClipW,
                img.Clips[index].ClipH);
        }

        override public void render(Graphics g, int index, float x, float y, Trans trans)
        {
            if (img.Clips[index].ClipW > 0)
            {
                int bx = ((int)img.Clips[index].ClipX) / mSplitSize;
                int by = ((int)img.Clips[index].ClipY) / mSplitSize;
                Image pTexture = mPartMatrix[bx, by];
                if (pTexture != null)
                {
                    g.BeginImage(pTexture);
                    g.DrawImageRegion(
                        img.Clips[index].ClipX % mSplitSize,
                        img.Clips[index].ClipY % mSplitSize,
                        img.Clips[index].ClipW,
                        img.Clips[index].ClipH,
                        trans, x, y);
                }
            }
        }
        override public void begin(Graphics g)
        {
            throw new NotImplementedException("切块图集不支持精灵");
        }
        override public void addVertex(VertexBuffer v, int index, float x, float y, Trans trans, uint color_rgba)
        {
            throw new NotImplementedException("切块图集不支持精灵");
        }

        override public void Dispose()
        {
            foreach (Image img in mPartMatrix)
            {
                img.Dispose();
            }

            base.Dispose();
        }
        public override IEnumerable<Image> Textures
        {
            get
            {
                var list = new List<Image>();
                for (int x = 0; x < mPartCountX; x++)
                {
                    for (int y = 0; y < mPartCountY; y++)
                    {
                        if (mPartMatrix[x, y] != null) list.Add(mPartMatrix[x, y]);
                    }
                }
                return list;
            }
        }
    }

    public class CPJAtlasTiles : CPJAtlas
    {
        private Image[] tiles;

        public CPJAtlasTiles(ImagesSet img, CPJResource res)
            : base(img, res)
        {
            tiles = new Image[img.Count];
            for (int i = 0; i < img.Count; i++)
            {
                if (img.Clips[i].ClipW > 0 && img.Clips[i].ClipH > 0)
                {
                    tiles[i] = res.Loader.LoadImage(img.Name + "/" + i + "." + img.Extention);
                }
            }
        }

        override public Image GetTile(int index)
        {
            return tiles[index];
        }

        override public Rectangle GetAtlasRegion(int index)
        {
            return new Rectangle(0, 0, img.Clips[index].ClipW, img.Clips[index].ClipH);
        }

        override public void render(Graphics g, int index, float x, float y, Trans trans)
        {
            Image tile = tiles[index];
            if (tile != null)
            {
                g.BeginImage(tile);
                g.DrawImageTrans(x, y, trans);
            }
        }
        override public void begin(Graphics g)
        {
            throw new NotImplementedException("切块图集不支持精灵");
        }
        override public void addVertex(VertexBuffer v, int index, float x, float y, Trans trans, uint color_rgba)
        {
            throw new NotImplementedException("切块图集不支持精灵");
        }

        override public void Dispose()
        {
            for (int i = 0; i < img.Count; i++)
            {
                if (tiles[i] != null)
                {
                    tiles[i].Dispose();
                }
            }
            tiles = null;
            base.Dispose();
        }
        public override IEnumerable<Image> Textures
        {
            get
            {
                var list = new List<Image>();
                for (int i = 0; i < img.Count; i++)
                {
                    if (img.Clips[i].ClipW > 0 && img.Clips[i].ClipH > 0)
                    {
                        if (tiles[i] != null)
                        {
                            list.Add(tiles[i]);
                        }
                    }
                }
                return list;
            }
        }
    }

}
