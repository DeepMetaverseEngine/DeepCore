using DeepCore.GUI.Cell;
using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeepCore.Unity3D.Cell
{
    public delegate void LoadImageCallback<ST>(ST st, string path, Texture2D texture);

    public class UniCPJLoader
    {
        public static bool TryGetOutputFile(string path, out string binPath, out string setName)
        {
            if (path.TryLastIndexOf(':', out int idx))
            {
                var cpjFile = path.Substring(0, idx).Trim();
                if (cpjFile.EndsWith(".xcpj", StringComparison.OrdinalIgnoreCase))
                {
                    var fname = Path.GetFileNameWithoutExtension(cpjFile);
                    binPath = Path.GetDirectoryName(cpjFile) + $"/output/{fname}.bin";
                    if (Resource.ExistData(binPath))
                    {
                        setName = path.Substring(idx + 1).Trim();
                        return true;
                    }
                }
            }
            binPath = null;
            setName = null;
            return false;
        }

        public virtual UniCPJFileResource LoadFile(string path)
        {
            path = Resource.FormatPath(path);
            var cpjfile = CPJFileLoader.LoadBin(path);
            if (cpjfile != null)
            {
                var ufile = new UniCPJFileResource(this, cpjfile, path);
                return ufile;
            }
            return null;
        }
        public virtual async Task<UniCPJFileResource> LoadFileAsync(string path)
        {
            path = Resource.FormatPath(path);
            var cpjfile = await Task.Run(() => CPJFileLoader.LoadBin(path));
            if (cpjfile != null)
            {
                var ufile = new UniCPJFileResource(this, cpjfile, path);
                return (ufile);
            }
            return null;
        }
        protected virtual Texture2D LoadTexture(byte[] data)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            tex.LoadImage(data, true);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.anisoLevel = 2;
            tex.mipMapBias = 0;
            return tex;
        }
        internal protected virtual async Task<Texture2D> LoadImageAsync<ST>(UniCellAtlasResource atlas, ST st, LoadImageCallback<ST> cb)
        {
            try
            {
                string imgpath = atlas.File.Path.Substring(0, atlas.File.Path.LastIndexOf('/')) + "/" + atlas.Meta.Name + ".png";
                var data = await Resource.LoadDataAsync(imgpath);
                var tcs = new TaskCompletionSource<Texture2D>();
                UnityHelper.MainThreadInvoke(() =>
                {
                    var tex = LoadTexture(data);
                    tex.name = atlas.Meta.Name;
                    cb(st, imgpath, tex);
                    tcs.SetResult(tex);
                });
                return await tcs.Task;
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
                throw (err);
            }
        }
        internal protected virtual Texture2D LoadImage(UniCellAtlasResource atlas, out string imgpath)
        {
            try
            {
                imgpath = atlas.File.Path.Substring(0, atlas.File.Path.LastIndexOf('/')) + "/" + atlas.Meta.Name + ".png";
                var data = Resource.LoadData(imgpath);
                var tex = LoadTexture(data);
                tex.name = atlas.Meta.Name;
                return tex;
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
                throw (err);
            }
        }
    }



    public abstract class UniCPJResource : Recyclable
    {
        public UniCPJLoader Loader { get; }
        public UniCPJResource(UniCPJLoader loader)
        {
            Loader = loader;
        }
        protected override void Destructing()
        {

        }
    }

    public abstract class UniCPJSetResource<SET> : UniCPJResource where SET : SetObject
    {
        public UniCPJFileResource File { get; }
        public SET Meta { get; }
        public UniCPJSetResource(UniCPJFileResource file, SET meta) : base(file.Loader)
        {
            this.File = file;
            this.Meta = meta;
        }
    }



    public class UniCPJFileResource : UniCPJResource
    {
        public CPJFileSet File { get; }
        public string Path { get; }

        private HashMap<string, UniCellAtlasResource> images = new HashMap<string, UniCellAtlasResource>();
        private HashMap<string, UniCellSpriteResource> sprites = new HashMap<string, UniCellSpriteResource>();
        private HashMap<string, UniCellMapResource> maps = new HashMap<string, UniCellMapResource>();


        public UniCPJFileResource(UniCPJLoader loader, CPJFileSet set, string path) : base(loader)
        {
            this.File = set;
            this.Path = path;
            foreach (var img in set.ImgTable)
            {
                images.Add(img.Key, new UniCellAtlasResource(this, img.Value));
            }
            foreach (var spr in set.SprTable)
            {
                sprites.Add(spr.Key, new UniCellSpriteResource(this, spr.Value, images[spr.Value.ImagesName]));
            }
            foreach (var map in set.MapTable)
            {
                maps.Add(map.Key, new UniCellMapResource(this, map.Value, images[map.Value.ImagesName]));
            }
        }
        protected override void Disposing()
        {
            foreach (var spr in sprites.Values)
            {
                spr.Dispose();
            }
            sprites.Clear();
            foreach (var atlas in images.Values)
            {
                atlas.Dispose();
            }
            images.Clear();
        }
        public void Load()
        {
            foreach (var atlas in images.Values)
            {
                atlas._Load();
            }
        }
        public async Task _LoadAsync()
        {
            foreach (var atlas in images.Values)
            {
                await atlas._LoadAsync();
            }
        }
        public IEnumerable<UniCellAtlasResource> AllAtlases => images.Values;
        public IEnumerable<UniCellSpriteResource> AllSprites => sprites.Values;

        public UniCellAtlasResource GetAtlas(string name)
        {
            if (images.TryGetValue(name, out var atlas))
            {
                return atlas;
            }
            return null;
        }
        public UniCellSpriteResource GetSprite(string name)
        {
            if (sprites.TryGetValue(name, out var sprite))
            {
                return sprite;
            }
            return null;
        }
        public UniCellMapResource GetMap(string name)
        {
            if (maps.TryGetValue(name, out var map))
            {
                return map;
            }
            return null;
        }
    }



    public class UniCellAtlasResource : UniCPJSetResource<ImagesSet>
    {
        public int Count { get { return Meta.Count; } }
        private Sprite[] sprites;
        private HashMap<String, int> imageKeys;
        private Texture2D texture;
        private HashMap<int, Tile> tiles = new HashMap<int, Tile>();

        public UniCellAtlasResource(UniCPJFileResource file, ImagesSet set) : base(file, set)
        {
            this.imageKeys = new HashMap<String, int>();
            this.sprites = new Sprite[set.Count];
            for (int i = 0; i < set.Count; i++)
            {
                if (set.ClipsKey[i] != null && !string.IsNullOrEmpty(set.ClipsKey[i]))
                {
                    imageKeys.Put(set.ClipsKey[i], i);
                }
            }
        }

        public Sprite GetTileSprite(string key)
        {
            if (imageKeys.TryGetValue(key, out int index))
            {
                return sprites[index];
            }
            return null;
        }
        public Sprite GetTileSprite(int index)
        {
            if (index >= 0 && index < sprites.Length)
            {
                return sprites[index];
            }
            return null;
        }


        internal Sprite GetTileSpriteUnsafe(int index)
        {
            return sprites[index];
        }

        public Tile GetOrAddTile(int index)
        {
            if (tiles.TryGetValue(index, out Tile tile))
            {
                return tile;
            }
            if (index >= 0 && index < sprites.Length)
            {
                var spr = sprites[index];
                tile = ScriptableObject.CreateInstance<Tile>();
                {
                    tile.sprite = spr;
                    //tile.transform = Matrix4x4.Scale(new Vector3(1, -1, 1));
                }
                return tile;
            }
            return null;
        }




        protected override void Disposing()
        {
            foreach (var spr in sprites)
            {
                if (spr != null) Sprite.Destroy(spr);
            }
            if (texture) Texture2D.Destroy(texture);
            this.texture = null;
        }

        protected virtual Sprite[] CreateTiles(Texture2D texture)
        {
            var set = Meta;
            for (int i = 0; i < set.Count; i++)
            {
                var clip = set.Clips[i];
                sprites[i] = Sprite.Create(
                    texture: texture,
                    rect: new Rect(clip.ClipX, texture.height - clip.ClipY - clip.ClipH, clip.ClipW, clip.ClipH),
                    pivot: new Vector2(0, 1f),
                    pixelsPerUnit: CPJEnviroment.GLOBAL_PIXEL_PER_UNIT,
                    extrude: 0,
                    meshType: SpriteMeshType.Tight);
                sprites[i].name = $"{set.Name}[{set.ClipsKey[i]}]";
            }
            return sprites;
        }

        internal void _Load()
        {
            this.texture = Loader.LoadImage(this, out var path);
            if (this.texture != null)
            {
                try
                {
                    if (texture != null)
                    {
                        CreateTiles(texture);
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace("UniCellAtlas LoadImage Error: " + path);
                    throw;
                }
            }
        }
        internal Task _LoadAsync()
        {
            return Loader.LoadImageAsync(this, (this), static (st, path, texture) =>
            {
                var owner = st;
                try
                {
                    if (owner.IsDisposing)
                    {
                        Texture2D.Destroy(texture);
                    }
                    else
                    {
                        owner.texture = texture;
                        if (texture != null)
                        {
                            owner.CreateTiles(texture);
                        }
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace("UniCellAtlas LoadImage Error: " + path);
                    throw;
                }
            });
        }
    }

    public class UniCellSpriteResource : UniCPJSetResource<SpriteSet>
    {
        public UniCellAtlasResource Atlas { get; }
        public UniCellSpriteResource(UniCPJFileResource file, SpriteSet set, UniCellAtlasResource atlas) : base(file, set)
        {
            this.Atlas = atlas;
        }
        protected override void Disposing()
        {

        }

    }

    public class UniCellMapResource : UniCPJSetResource<MapSet>
    {
        public UniCellAtlasResource Atlas { get; }
        public UniCellMapResource(UniCPJFileResource file, MapSet set, UniCellAtlasResource atlas) : base(file, set)
        {
            this.Atlas = atlas;
        }
        protected override void Disposing()
        {

        }

    }

}
