using DeepCore;
using DeepCore.GUI.Cell.Game;
using DeepCore.IO;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.GUI.Cell
{

    public partial class CPJResource : IDisposable
    {
        protected CPJLoader loader;

        protected HashMap<string, CPJAtlas> images = new HashMap<string, CPJAtlas>();
        protected HashMap<string, CSprite> sprites = new HashMap<string, CSprite>();

        public IReadOnlyCollection<CPJAtlas> AllAtlas { get => images.Values; }
        public IReadOnlyCollection<CSprite> AllCSprite { get => sprites.Values; }
        public CPJLoader Loader
        {
            get { return loader; }
        }


        //	-------------------------------------------------------------------------------------

        public CPJResource(CPJLoader loader)
        {
            this.loader = loader;

            foreach (ImagesSet img in loader.File.ImgTable.Values)
            {
                CPJAtlas atlas = null;
                if (img.IsTiles)
                {
                    atlas = new CPJAtlasTiles(img, this);
                }
                else if (img.SplitSize != 0)
                {
                    atlas = new CPJAtlasSplitGroup(img, this);
                }
                else
                {
                    atlas = new CPJAtlasGroup(img, this);
                }

                images.Add(img.Name, atlas);
            }
        }

        public void Dispose()
        {
            if (images != null)
            {
                foreach (CPJAtlas img in images.Values)
                {
                    img.Dispose();
                }

                images = null;
            }

            if (sprites != null)
            {
                sprites.Clear();
                sprites = null;
            }

            //             if (loader != null)
            //             {
            //                 loader.Dispose();
            //                 loader = null;
            //             }
        }

        public CPJAtlas GetAtlas(String key)
        {
            return images.Get(key);
        }

        public ImagesSet GetSetImages(String key)
        {
            return loader.File.ImgTable.Get(key);
        }

        public CSprite GetSprite(String key)
        {
            if (sprites.ContainsKey(key))
            {
                return sprites.Get(key);
            }
            else
            {
                SpriteSet ss = loader.File.SprTable.Get(key);
                if (ss != null)
                {
                    CPJAtlas img = images.Get(ss.ImagesName);
                    if (img != null)
                    {
                        CSprite ret = new CSprite(ss, img);
                        sprites.Put(key, ret);
                        return ret;
                    }
                }
                return null;
            }
        }

        public void ReleaseTexture()
        {
            if (images != null)
            {
                foreach (CPJAtlas img in images.Values)
                {
                    img.ReleaseTexture();
                }
            }
        }
        //-------------------------------------------------------------------------------------

        public static async Task<CPJResource> CreateResourceAsync(string path)
        {
            try
            {
                if (path.EndsWith(".cpj", StringComparison.OrdinalIgnoreCase))
                {
                    path = Path.Combine(Path.GetDirectoryName(path), "output", Path.GetFileNameWithoutExtension(path) + ".xml");
                }
                var path_bin = path.Substring(0, path.LastIndexOf('.')) + ".bin";
                if (await DeepCore.IO.Resource.ExistDataAsync(path_bin))
                {
                    var bytes = await Resource.LoadDataAsync(path_bin);
                    var file = CPJFileLoader.LoadBin(bytes);
                    return new CPJResource(new CPJLoader(path, file));

                }
                else if (await DeepCore.IO.Resource.ExistDataAsync(path))
                {
                    var bytes = await Resource.LoadDataAsync(path_bin);
                    var xml = XmlUtil.LoadXML(bytes);
                    var file = CPJFileLoader.LoadXML(xml);
                    return new CPJResource(new CPJLoader(path, file));
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"LoadCPJ Error : {path} : ");
            }
            return null;
        }
        public static CPJResource CreateResource(string path)
        {
            try
            {
                if (path.EndsWith(".cpj", StringComparison.OrdinalIgnoreCase))
                {
                    path = Path.Combine(Path.GetDirectoryName(path), "output", Path.GetFileNameWithoutExtension(path) + ".xml");
                }
                var path_bin = path.Substring(0, path.LastIndexOf('.')) + ".bin";
                if (DeepCore.IO.Resource.TryOpenStream(path_bin, out var stream))
                {
                    using (stream)
                    {
                        var file = CPJFileLoader.LoadBin(stream);
                        return new CPJResource(new CPJLoader(path, file));
                    }
                }
                else if (DeepCore.IO.Resource.TryOpenStream(path, out stream))
                {
                    using (stream)
                    {
                        var file = CPJFileLoader.LoadXML(stream);
                        return new CPJResource(new CPJLoader(path, file));
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"LoadCPJ Error : {path} : ");
            }
            return null;
        }

        //-------------------------------------------------------------------------------------
    }

}
