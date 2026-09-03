using DeepCore.IO;
using DeepCore.Unity;
using DeepCore.Voxel.Data;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static DeepCore.Unity3D.Voxel.VoxelBaker.RayTestObjectGroup;

namespace DeepCore.Unity3D.Voxel
{
    public class VoxelBaker
    {
        public VoxelAABB config { get; private set; }

        //private GameObject voxelRoot;
        private readonly VoxelAABB aabb;
        private readonly int layerMask;
        private readonly Vector2 startPos;
        private readonly FileInfo voxtPath;
        private readonly HashMap<Texture2D, Texture2D> raytestTextures = new HashMap<Texture2D, Texture2D>();
        private readonly List<RayTestObjectHits> total_hits = new();
        private HashMap<string, Color> layerMap = new();
        public IReadOnlyList<RayTestObjectHits> TotalHits => total_hits;
        public VoxelBaker(VoxelAABB config, FileInfo outputPath)
        {
            this.config = config;
            this.aabb = config;
            this.voxtPath = outputPath;
            this.startPos = this.aabb.StartPoint();
            this.layerMask = -5;
            foreach (var e in config.layerMap)
            {
                layerMap.Put(e.LayerName, e.LayerColor);
            }
            if (config.ignoreLayers != null && config.ignoreLayers.Length > 0)
            {
                var layers = config.ignoreLayers;
                this.layerMask = ~LayerMask.GetMask(layers);
            }
            if (config.includeLayers != null && config.includeLayers.Length > 0)
            {
                var layers = config.includeLayers;
                this.layerMask = LayerMask.GetMask(layers);
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        public VoxelTerrainData RunBakeToXml(BreakPredicate<string, float> progressBar = null)
        {
            VoxelTerrainData ret = null;
            {
                var old_queriesHitBackfaces = Physics.queriesHitBackfaces;
                Physics.queriesHitBackfaces = true;
                var existingColliders = new HashMap<Collider, bool>();
                var addingMeshColliders = new HashMap<GameObject, MeshCollider>();
                {
                    if (config.autoBindMeshCollider)
                    {
                        foreach (var o in aabb.gameObject.scene.GetRootGameObjects())
                        {
                            o.transform.ForEachDeep(t =>
                            {
                                var go = t.gameObject;
                                if (go.TryGetComponents<Collider>(out var colliders))
                                {
                                    // 屏蔽现有的MeshCollider，避免重复碰撞
                                    foreach (var c in colliders)
                                    {
                                        existingColliders.Put(c, c.enabled);
                                        c.enabled = false;
                                    }
                                }
                                if (go.TryGetComponent<MeshRenderer>(out var renderer))
                                {
                                    addingMeshColliders.Add(go, go.AddComponent<MeshCollider>());
                                }
                            });
                        }
                    }
                    else if (config.onlyMeshCollider)
                    {
                        foreach (var o in aabb.gameObject.scene.GetRootGameObjects())
                        {
                            o.transform.ForEachDeep(t =>
                            {
                                var go = t.gameObject;
                                if (go.TryGetComponents<Collider>(out var colliders))
                                {
                                    // 屏蔽现有的MeshCollider，避免重复碰撞
                                    foreach (var c in colliders)
                                    {
                                        existingColliders.Put(c, c.enabled);
                                        c.enabled = false;
                                    }
                                    addingMeshColliders.Add(go, go.AddComponent<MeshCollider>());
                                }
                            });
                        }
                    }
                }
                try
                {
                    ret = BakeToXml(progressBar);
                }
                finally
                {
                    Physics.queriesHitBackfaces = old_queriesHitBackfaces;
                    if (addingMeshColliders.Count > 0)
                    {
                        foreach (var kv in addingMeshColliders)
                        {
                            MeshCollider.DestroyImmediate(kv.Value);
                        }
                        addingMeshColliders.Clear();
                    }
                    if (existingColliders.Count > 0)
                    {
                        foreach (var kv in existingColliders)
                        {
                            kv.Key.enabled = kv.Value;
                        }
                        existingColliders.Clear();
                    }
                    if (raytestTextures != null)
                    {
                        foreach (var t in raytestTextures.Values)
                        {
                            Texture2D.DestroyImmediate(t);
                        }
                        raytestTextures.Clear();
                    }
                }
            }
            return ret;
        }
        private VoxelTerrainData BakeToXml(BreakPredicate<string, float> progressBar)
        {
            var totalsize = this.aabb.boundsWH;
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var width = config.width;
            int length_x = (int)Math.Round(totalsize.x / width);
            int length_y = (int)Math.Round(totalsize.y / width);
            var Voxels = new VoxelTerrainData();
            Voxels.MinHeight = config.minHeight;
            Voxels.GridSize = width;
            Voxels.Grids = new VoxelNodeData[length_x, length_y][];
            Voxels.XLength = length_x;
            Voxels.YLength = length_y;
            Voxels.MinX = startPos.x;
            Voxels.MinY = startPos.y;
            Voxels.MaxX = startPos.x + totalsize.x;
            Voxels.MaxY = startPos.y + totalsize.y;
            {
                var cancel = false;
                var total = length_x * length_y;
                var count = 0;
                for (int ix = 0; ix < length_x; ix++)
                {
                    for (int iy = 0; iy < length_y; iy++)
                    {
                        if (progressBar != null && progressBar($"Ray Test : {ix} , {iy}", (count / (float)total)))
                        {
                            cancel = true;
                            return null;
                        }
                        using (var odc = new RayTestObjectGroup(this, ix, iy))
                        {
                            Voxels.Grids[ix, iy] = odc.RayTestObjects();
                        }
                        count++;
                    }
                }
                System.Threading.Thread.Sleep(1);
                UnityEngine.Debug.Log($"ray cast time: {sw.Elapsed}");
                sw.Reset();
                sw.Start();
                {
                    {
                        CFiles.CreateFile(voxtPath);
                        using (var output = File.OpenWrite(voxtPath.FullName))
                        {
                            using (var writer = new StreamWriter(output))
                            {
                                cancel = VoxelTerrainData.SaveToText(Voxels, writer, (t, p) =>
                                {
                                    return progressBar != null && progressBar($"Encode Voxt : {t}", p);
                                });
                            }
                        }
                        if (cancel)
                        {
                            CFiles.Delete(voxtPath);
                        }
                    }
                    if (!cancel)
                    {
#if false
                        var zipPath = voxtPath.FullName + ".zip";
                         if (progressBar != null && progressBar($"Encode Zip : {zipPath}", 1f))
                         {
                             cancel = true;
                             return Voxels;
                         }
                         using (var zipfile = ZipUtil.CreateZipFile(zipPath))
                         {
                             zipfile.BeginUpdate();
                             zipfile.Add(voxtPath.FullName, Path.GetFileName(voxtPath.FullName));
                             zipfile.CommitUpdate();
                             zipfile.Close();
                        }
                        UnityEngine.Debug.Log($"save time: {sw.Elapsed} : {zipPath}");
#endif
                    }

                }
                return Voxels;
            }
        }
        //-------------------------------------------------------------------------------------------------------------


        public bool TryGetObjectColor(in RaycastHit hit, out uint color, out bool is_baseline)
        {
            if (config.useMeshColor && TryGetObjectColorMeshRender(in hit, out color, out is_baseline))
            {
                return true;
            }
            else if (config.useNavMesh && TryGetObjectColorNavMesh(in hit, out color, out is_baseline))
            {
                return true;
            }
            else if (TryObjectColorLayer(in hit, out color, out is_baseline))
            {
                return true;
            }
            else
            {
                color = ColorToUint(Color.white);
                return false;
            }
        }
        private bool TryGetRayCastPixel(in RaycastHit hit, out Color color)
        {
            if (hit.collider.TryGetComponent<Renderer>(out var render) && render.sharedMaterial && render.sharedMaterial.mainTexture is Texture2D texture)
            {
                var pixelUV = hit.textureCoord;
                pixelUV.x *= texture.width;
                pixelUV.y *= texture.height;
                if (!raytestTextures.TryGetValue(texture, out var readerTexture))
                {
                    readerTexture = new Texture2D(texture.width, texture.height);
                    var tempTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0,
                                           RenderTextureFormat.Default,
                                           RenderTextureReadWrite.Linear);
                    Graphics.Blit(texture, tempTexture);
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = tempTexture;
                    try
                    {
                        readerTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                        readerTexture.Apply();
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        RenderTexture.ReleaseTemporary(tempTexture);
                    }
                    raytestTextures.Add(texture, readerTexture);
                }
                color = readerTexture.GetPixel((int)pixelUV.x, (int)pixelUV.y);
                return true;
            }
            color = Color.white;
            return false;
        }
        private bool TryGetObjectColorMeshRender(in RaycastHit hit, out uint color, out bool is_baseline)
        {
            var go = hit.transform.gameObject;
            var flag = LayerMask.LayerToName(go.layer);
            is_baseline = false;
            if (TryGetRayCastPixel(in hit, out var pcolor))
            {
                color = ColorToUint(pcolor);
                return true;
            }
            color = 0;
            return false;
        }
        private bool TryGetObjectColorNavMesh(in RaycastHit hit, out uint color, out bool is_baseline)
        {
            var go = hit.transform.gameObject;
            //                 bool is_nav_static = (GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.NavigationStatic) != 0;
            //                 //优先层设置
            //                 string flag = LayerMask.LayerToName(go.layer);
            //                 is_baseline = flag == config.layerBaseLine;
            //                 if (is_nav_static)
            //                 {
            //                     //目前只区分不可行走
            //                     if (GameObjectUtility.GetNavMeshArea(go) == UnityEngine.AI.NavMesh.GetAreaFromName(config.layerNotWalkable))
            //                     {
            //                         color = ColorToUint(Color.red);
            //                         return true;
            //                     }
            //                     else if (GameObjectUtility.GetNavMeshArea(go) == UnityEngine.AI.NavMesh.GetAreaFromName(config.layerWater))
            //                     {
            //                         color = ColorToUint(Color.blue);
            //                         return true;
            //                     }
            //                 }
            is_baseline = false;
            color = 0;
            return false;
        }
        private bool TryObjectColorLayer(in RaycastHit hit, out uint color, out bool is_baseline)
        {
            var go = hit.transform.gameObject;
            var flag = LayerMask.LayerToName(go.layer);
            is_baseline = false;
            //目前只区分不可行走
            if (layerMap.TryGetValue(flag, out var ucolor))
            {
                color = ColorToUint(ucolor);
                return true;
            }
            color = 0;
            return false;
        }
        //--------------------------------------------------------------------------------------------------------------

        public static uint ColorToUint(Color c)
        {
            uint rtn = 0;
            //rtn = rtn + (uint)FloatToInt(c.a) << 24;
            //rtn = rtn + (uint)FloatToInt(c.r) << 16;
            //rtn = rtn + (uint)FloatToInt(c.g) << 8;
            //rtn = rtn + (uint)FloatToInt(c.b);

            Color32 c32 = (Color32)c;
            rtn = rtn | ((uint)c32.a) << 24;
            rtn = rtn | ((uint)c32.r) << 16;
            rtn = rtn | ((uint)c32.g) << 8;
            rtn = rtn | ((uint)c32.b);

            return rtn;
        }
        public static Color UintToColor(uint d)
        {
            float a = (byte)(d >> 24) / 255f;
            float r = (byte)(d >> 16) / 255f;
            float g = (byte)(d >> 8) / 255f;
            float b = (byte)(d) / 255f;
            Color c = new Color(r, g, b, a);
            return c;
        }

        //-------------------------------------------------------------------------------------------------------------
        public class RayTestObjectHits
        {
            public Transform transform { get; }
            public Collider collider { get; }
            public int iX { get; }
            public int iY { get; }
            public uint Color { get; private set; }
            public float Upward { get; private set; }
            public float Downward { get; private set; }
            public bool BaseLine { get; private set; }
            public float Height { get => Upward - Downward; }
            public RayTestObjectHits(VoxelBaker baker, int ix, int iy, VoxelCastHit hit)
            {
                this.iX = ix;
                this.iY = iy;
                this.transform = hit.hit.transform;
                this.collider = hit.hit.collider;
                var v = hit.point;
                this.Upward = v.y;
                this.Downward = v.y;
                {
                    if (baker.TryGetObjectColor(in hit.hit, out var baseColor, out var is_baseline))
                    {
                        this.Color = baseColor;
                        this.BaseLine = is_baseline;
                    }
                }
            }
            public void UpdateHit(VoxelBaker baker, VoxelCastHit hit)
            {
                var v = hit.point;
                this.Downward = Mathf.Min(this.Downward, v.y);
                this.Upward = Mathf.Max(this.Upward, v.y);
                if (this.Color == 0)
                {
                    if (baker.TryGetObjectColor(in hit.hit, out var baseColor, out var is_baseline))
                    {
                        this.Color = baseColor;
                        this.BaseLine = is_baseline;
                    }
                }
            }
            public VoxelNodeData ToNodeData()
            {
                return new VoxelNodeData()
                {
                    Downward = this.Downward,
                    Upward = this.Upward,
                    Color = this.Color,
                    BaseLine = this.BaseLine,
                    state = this,
                };
            }
        }
        public class RayTestObjectGroup : IDisposable
        {
            public VoxelAABB config => baker.config;

            private Dictionary<Transform, RayTestObjectHits> dicts = new Dictionary<Transform, RayTestObjectHits>();
            private float totalUpward;
            private float totalDownward;
            private uint totalColor;
            private readonly VoxelBaker baker;
            private readonly int ix, iy;
            public RayTestObjectGroup(VoxelBaker baker, int ix, int iy)
            {
                this.baker = baker;
                this.ix = ix;
                this.iy = iy;
                var minLevel = config.GetMinLevel();
                this.totalDownward = minLevel - config.minHeight;
                this.totalUpward = minLevel;
            }

            public void Dispose()
            {
                dicts.Clear();
                dicts = null;
            }            
            public VoxelNodeData[] RayTestObjects()
            {
                var width = config.width;
                var xc = baker.startPos.x + (ix + 0.5f) * width;
                var yc = baker.startPos.y + (iy + 0.5f) * width;
                {
                    var _min = -config.raycastLimit / 2;
                    var _max = -_min;
                    var min = Mathf.Min(_min, _max);
                    var max = Mathf.Max(_min, _max);
                    {
                        var hitted = VoxelCast(xc, yc, min, max);
                        foreach (var hit in hitted) { AddObject(hit); }
                    }
                }
                var voxels = new List<VoxelNodeData>();
                if (config.singleLayer)
                {
                    var vl = new VoxelNodeData
                    {
                        Downward = totalDownward,
                        Upward = totalUpward,
                        Color = totalColor,
                    };
                    voxels.Add(vl);
                }
                else
                {
                    foreach (var hit in dicts.Values)
                    {
                        CombineVoxels(xc, yc, hit, voxels);
                    }
                }
                voxels.Sort(new System.Comparison<VoxelNodeData>((i1, i2) => i1.Upward.CompareTo(i2.Upward)));
                if (voxels.Count == 0)
                {
                    var minLevel = config.GetMinLevel();
                    //黑洞需要填平
                    var vl = new VoxelNodeData
                    {
                        Downward = minLevel - config.minHeight,
                        Upward = minLevel,
                        Color = ColorToUint(config.color),
                        BaseLine = default,
                        state = this,
                    };
                    voxels.Add(vl);
                }
                else
                {
                    foreach (var vl in voxels)
                    {
                        if (vl.Color == 0)
                        {
                            vl.Color = ColorToUint(config.color);
                        }
                    }
                }
                return voxels.ToArray();
            }
            public class VoxelCastHit
            {
                public RaycastHit hit;
                public Vector3 point;
            }
            private List<VoxelCastHit> VoxelCast(float xc, float yc, float minY, float maxY)
            {
                List<RaycastHit> hitted = new List<RaycastHit>();
                var width = config.width;
                var r = Math.Min(config.rayWidth / 2f, width / 2f);
                {
                    RayCast(xc, yc, minY, maxY, hitted);
                    var x0 = xc - r;
                    var y0 = yc - r;
                    var x1 = xc + r;
                    var y1 = yc + r;
                    RayCast(x0, y0, minY, maxY, hitted);
                    RayCast(x0, y1, minY, maxY, hitted);
                    RayCast(x1, y1, minY, maxY, hitted);
                    RayCast(x1, y0, minY, maxY, hitted);
                }
                if (config.useBoxCast)
                {
                    BoxCast(xc, yc, minY, maxY, hitted);
                }
                hitted.Sort((a, b) => CMath.GetDirect(b.point.y - a.point.y));

                return hitted.ConvertAll(h => new VoxelCastHit() { hit = h, point = h.point });
            }
            private void BoxCast(float x, float z, float minY, float maxY, List<RaycastHit> hitted)
            {
                var d = Mathf.Abs(minY - maxY);
                var r = Math.Min(config.rayWidth / 2f, config.width / 2f);
                {
                    var from = new Vector3(x, maxY, z);
                    var hits = Physics.BoxCastAll(from, Vector3.one * r, Vector3.down, Quaternion.identity, d, baker.layerMask, QueryTriggerInteraction.Collide);
                    hitted.AddRange(hits);
                    //                     Array.Sort(hits, (a, b) => CMath.GetDirect(b.point.y - a.point.y));
                    //                     foreach (var item in hits)
                    //                     {
                    //                         AddObject(item, color);
                    //                     }
                }
                {
                    var from = new Vector3(x, minY, z);
                    var hits = Physics.BoxCastAll(from, Vector3.one * r, Vector3.up, Quaternion.identity, d, baker.layerMask, QueryTriggerInteraction.Collide);
                    hitted.AddRange(hits);
                    //                     Array.Sort(hits, (a, b) => CMath.GetDirect(b.point.y - a.point.y));
                    //                     foreach (var item in hits)
                    //                     {
                    //                         AddObject(item, false);
                    //                     }
                }
            }
            private void RayCast(float x, float z, float minY, float maxY, List<RaycastHit> hitted)
            {
                var d = Mathf.Abs(minY - maxY);
                {
                    var from = new Vector3(x, maxY, z);
                    var hits = Physics.RaycastAll(from, Vector3.down, d, baker.layerMask, QueryTriggerInteraction.Collide);
                    hitted.AddRange(hits);
                    //                   Array.Sort(hits, (a, b) => CMath.GetDirect(b.point.y - a.point.y));
                    //                     foreach (var item in hits)
                    //                     {
                    //                         AddObject(item, color);
                    //                     }
                }
                {
                    var from = new Vector3(x, minY, z);
                    var hits = Physics.RaycastAll(from, Vector3.up, d, baker.layerMask, QueryTriggerInteraction.Collide);
                    hitted.AddRange(hits);
                    //                    Array.Sort(hits, (a, b) => CMath.GetDirect(b.point.y - a.point.y));
                    //                     foreach (var item in hits)
                    //                     {
                    //                         AddObject(item, false);
                    //                     }
                }
            }

            private void AddObject(VoxelCastHit hit)
            {
                if (config.onlyMeshCollider)
                {
                    if (hit.hit.collider is not MeshCollider)
                    {
                        return;
                    }
                }
                var v = hit.point;
                if (dicts.TryGetValue(hit.hit.transform, out var hits))
                {
                    hits.UpdateHit(baker, hit);
                }
                else
                {
                    hits = new RayTestObjectHits(baker, ix, iy, hit);
                    dicts.Add(hit.hit.transform, hits);
                    baker.total_hits.Add(hits);
                }
                if (totalUpward < v.y || this.totalColor == 0)
                {
                    this.totalColor = hits.Color;
                }
                this.totalDownward = Math.Min(totalDownward, v.y);
                this.totalUpward = Math.Max(totalUpward, v.y);
                //hits.hits.Add(hit);
            }
            private void CombineVoxels(float xc, float yc, RayTestObjectHits obj, List<VoxelNodeData> out_voxels)
            {
                var r = Math.Min(config.rayWidth / 2f, config.width / 2f);
                var h = config.minHeight * 0.5f;
                var step = config.minHeight;
                var curLayer = obj.ToNodeData();
                out_voxels.Add(curLayer);
                if (config.splitVoxels && step > 0 && (obj.Upward - obj.Downward) > step)
                {
                    var startZ = obj.Upward - h;
                    var endZ = obj.Downward + h;
                    UnityEngine.Debug.Log($"{ix},{iy}:({startZ},{endZ},{step}):开始掏空体素");
                    for (float z = startZ; z > endZ; z -= step)
                    {
                        var touches = Physics.OverlapBox(new Vector3(xc, z, yc), new Vector3(r, h, r), Quaternion.identity, baker.layerMask, QueryTriggerInteraction.Collide);
                        if (touches != null && touches.Length > 0 && touches.TryIndexOf(obj.collider, out var index))
                        {
                            if (curLayer != null)
                            {
                                UnityEngine.Debug.Log($"{ix},{iy}:({z}):有碰撞，继续延展");
                                //curLayer.Downward = Math.Max(z - h, obj.Downward);
                            }
                            else
                            {
                                UnityEngine.Debug.Log($"{ix},{iy}:({z}):有碰撞，开始新块");
                                curLayer = new VoxelNodeData()
                                {
                                    Color = obj.Color,
                                    Upward = Math.Min(z + h, obj.Upward),
                                    Downward = endZ - h,
                                    state = obj,
                                };
                                out_voxels.Add(curLayer);
                            }
                        }
                        else
                        {
                            // 没有碰撞，结束当前层，并且挖空
                            if (curLayer != null)
                            {
                                UnityEngine.Debug.Log($"{ix},{iy}:({z}):没有碰撞，结束当前层，并且挖空");
                                curLayer.Downward = z + h;
                                curLayer = null;
                            }
                        }
                    }
                    if (curLayer != null)
                    {
                        curLayer.Downward = endZ - h;
                    }
                    UnityEngine.Debug.Log($"{ix},{iy}:({startZ},{endZ},{step}):结束掏空体素");
                }
            }

        }


    }


}