using DeepCore.GUI.Cell;
using DeepCore.GUI.Cell.Game;
using DeepCore.IO;
using DeepCore.Unity;
using DeepCore.Unity3D.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;
using static DeepCore.GUI.Cell.SpriteSet;
using static DeepCore.Unity3D.Cell.CellSpriteObject;

namespace DeepCore.Unity3D.Cell
{
    public interface ICellDisplayObject
    {
        GameObject gameObject { get; }
        Transform transform { get; }
    }
    public class CellSpriteObject : Recyclable, ICellDisplayObject
    {
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public CellSpriteController controller { get; }
        public UniCellSpriteResource CellSprite { get; protected set; }
        public SpriteSet Meta => CellSprite?.Meta;


        private List<SpriteSet.Part> renderParts = new();
        private List<SpriteRenderer> renderStack = new();

        public static CellSpriteObject Create(UniCellSpriteResource sprite)
        {
            var display = new CellSpriteObject();
            display.Init(sprite);
            return display;
        }
        public CellSpriteObject Clone()
        {
            var display = new CellSpriteObject();
            display.Init(this.CellSprite);
            display.controller.PlayMode = this.controller.PlayMode;
            display.controller.Speed = this.controller.Speed;
            display.controller.CurrentAnimate = this.controller.CurrentAnimate;
            display.controller.CurrentFrame = this.controller.CurrentFrame;
            return display;
        }
        protected CellSpriteObject()
        {
            this.gameObject = new GameObject();
            this.transform = gameObject.transform;
            //this.transform.localRotation = Quaternion.Euler(90, 0, 0);
            this.controller = this.gameObject.AddComponent<CellSpriteController>();
            this.controller.Bind(this);
        }
        protected virtual void Init(UniCellSpriteResource sprite)
        {
            this.CellSprite = sprite;
            this.gameObject.name = sprite.Meta.Name;
            this.CleanRenderPart();
        }
        protected override void Disposing()
        {
            this.CellSprite = null;
            this.CleanRenderPart();
        }
        protected override void Destructing()
        {
            if (this.gameObject != null)
            {
                GameObject.Destroy(this.gameObject);
            }
        }

        private void CleanRenderPart()
        {
            renderParts.Clear();
            foreach (var r in renderStack)
            {
                if (r != null)
                {
                    r.sprite = null;
                    r.gameObject.SetActive(false);
                }
            }
        }
        private SpriteRenderer GetOrAddRender(int index)
        {
            if (index < renderStack.Count)
            {
                var renderer = renderStack[index];
                renderer.gameObject.SetActive(true);
                //renderer.transform.localRotation = Quaternion.Euler(90, 0, 0);
                return renderer;
            }
            else
            {
                var part = new GameObject("part" + index);
                part.transform.SetParent(this.transform, false);
                //part.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var renderer = part.AddComponent<SpriteRenderer>();
                renderStack.Add(renderer);
                return renderer;
            }
        }

        internal void Update()
        {
            renderParts.Clear();
            CellSprite.Meta.GetParts(controller.CurrentAnimate, controller.CurrentFrame, renderParts);
            for (int p = 0; p < renderParts.Count; p++)
            {
                var part = renderParts[p];
                var render = GetOrAddRender(p);
                RenderPart(render, in part);
            }
            for (int r = renderParts.Count; r < renderStack.Count; r++)
            {
                renderStack[r].sprite = null;
                renderStack[r].gameObject.SetActive(false);
            }
        }

        private void RenderPart(SpriteRenderer render, in SpriteSet.Part part)
        {
            var sprite = CellSprite.Atlas.GetTileSpriteUnsafe(part.PartTileID);
            if (sprite == null)
            {
                render.sprite = null;
                return;
            }
            render.sprite = sprite;
            render.transform.localPosition = new Vector3(
                part.PartX / CPJEnviroment.GLOBAL_PIXEL_PER_UNIT,
                -part.PartY / CPJEnviroment.GLOBAL_PIXEL_PER_UNIT,
                0);
            render.transform.localScale = new Vector3(part.PartScaleX, part.PartScaleY, 1);
            render.transform.localRotation = Quaternion.Euler(0, 0, part.PartRotate);
            //render.color = new Color32(part.ColorR, part.ColorG, part.ColorB, part.ColorA);
        }

        public enum SpritePlayMode
        {
            Once,
            Loop,
        }
    }
    public class CellSpriteController : MonoBehaviour
    {
        public CellSpriteObject Display { get; private set; }
        public SpritePlayMode PlayMode { get; set; } = SpritePlayMode.Loop;
        public bool IsPause { get; set; } = false;
        public float Speed { get; set; } = 1.0f;
        public int CurrentFrame
        {
            get => currentFrame;
            set => currentFrame = value;
        }
        public string CurrentAnimateName
        {
            get => currentAnimName;
            set
            {
                if (Display != null && Display.CellSprite.Meta.TryGetAnimateIndex(value, out var index))
                {
                    currentAnimName = value;
                    currentAnim = index;
                    currentFrame = 0;
                }
            }
        }
        public int CurrentAnimate
        {
            get => currentAnim;
            set
            {
                currentAnim = value;
                if (Display != null)
                {
                    Display.CellSprite.Meta.TryGetAnimateName(currentAnim, out currentAnimName);
                }
            }
        }
        public float CurrentAnimateTotalTimeMS
        {
            get
            {
                if (Display != null)
                {
                    return Display.CellSprite.Meta.GetFrameCount(CurrentAnimate) * timeInterval.IntervalTimeMS;
                }
                return 1;
            }
        }
        public bool IsEndFrame
        {
            get
            {
                if (Display != null)
                {
                    return CurrentFrame == Display.CellSprite.Meta.GetFrameCount(CurrentAnimate) - 1;
                }
                return false;
            }
        }
        public SpriteSet Meta => Display?.CellSprite?.Meta;

        private int currentAnim;
        private string currentAnimName;
        private int currentFrame;
        private TimeInterval timeInterval;
        internal void Bind(CellSpriteObject owner)
        {
            this.Display = owner;
            this.timeInterval = new TimeInterval(CPJEnviroment.GLOBAL_TICK_INTERVAL_MS);
        }
        void Update()
        {
            if (IsPause) return;
            if (Display != null && this.timeInterval.Update(Time.deltaTime * 1000f * Speed))
            {
                Display.Update();
                UpdateTick();
            }
        }
        protected virtual void UpdateTick()
        {
            var frameCount = Display.CellSprite.Meta.GetFrameCount(CurrentAnimate);
            if (frameCount > 0)
            {
                switch (PlayMode)
                {
                    case SpritePlayMode.Once:
                        if (CurrentFrame < frameCount - 1)
                        {
                            CurrentFrame++;
                        }
                        break;
                    case SpritePlayMode.Loop:
                        CurrentFrame = (CurrentFrame + 1) % frameCount;
                        break;
                }
            }
        }
    }

    public class CellMapObject : Disposable, ICellDisplayObject
    {
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public CellMapController controller { get; }
        public UniCellMapResource CellMap { get; }
        public MapSet Meta => CellMap?.Meta;
        public Grid grid { get; }

        private HashMap<int, LayerInfo> layers = new();

        public CellMapObject(UniCellMapResource map)
        {
            this.gameObject = new GameObject();
            this.gameObject.name = map.Meta.Name;
            this.transform = gameObject.transform;
            this.CellMap = map;
            this.grid = this.gameObject.AddComponent<Grid>();
            this.grid.cellSize = new Vector3(Meta.CellW / CPJEnviroment.GLOBAL_PIXEL_PER_UNIT, Meta.CellH / CPJEnviroment.GLOBAL_PIXEL_PER_UNIT, 0);
            var sw = Stopwatch.StartNew();
            for (var layer = 0; layer < map.Meta.LayerCount; layer++)
            {
                var layerObject = new GameObject();
                layerObject.name = $"Layer_{layer}";
                layerObject.transform.SetParent(this.gameObject.transform, false);
                var tileMap = layerObject.AddComponent<Tilemap>();
                {
                    tileMap.tileAnchor = new Vector3(0, 1, 0);
                }
                CreateTiles(layer, out var positions, out var tileArray);
                tileMap.SetTiles(positions, tileArray);
                var tileRender = layerObject.AddComponent<TilemapRenderer>();
                {
                    //tileRender.sortingOrder = layer;
                }
                layers.Add(layer, new LayerInfo()
                {
                    gameObject = layerObject,
                    tileMap = tileMap,
                    tileRenderer = tileRender,
                });
                layerObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            sw.Stop();
            UnityEngine.Debug.Log($"Gen Cell Tile Map Object {map.Meta.Name} : use {sw.Elapsed} times");
            this.controller = this.gameObject.AddComponent<CellMapController>();
            //this.transform.localRotation = Quaternion.Euler(90, 0, 0);

        }
        protected override void Disposing()
        {
            if (this.gameObject != null)
            {
                GameObject.Destroy(this.gameObject);
            }
        }

        protected virtual void CreateTiles(int layer, out Vector3Int[] positions, out TileBase[] tileArray)
        {
            var map = CellMap;
            var Count = Meta.XCount * Meta.YCount;
            positions = new Vector3Int[Count];
            tileArray = new TileBase[positions.Length];
            for (var iy = 0; iy < map.Meta.YCount; iy++)
            {
                for (var ix = 0; ix < map.Meta.XCount; ix++)
                {
                    var index = iy * map.Meta.XCount + ix;
                    positions[index] = new Vector3Int(ix, map.Meta.YCount - iy - 1, 0);
                    var tile = this.CellMap.Atlas.GetOrAddTile(Meta.Terrain[layer, iy, ix].TerrainTile);
                    tileArray[index] = tile;
                }
            }
        }

        struct LayerInfo
        {
            public GameObject gameObject;
            public Tilemap tileMap;
            public TilemapRenderer tileRenderer;
        }
    }

    public class CellMapController : MonoBehaviour
    {
        public CellMapObject Display { get; private set; }
        internal void Bind(CellMapObject owner)
        {
            this.Display = owner;
        }

    }
}
