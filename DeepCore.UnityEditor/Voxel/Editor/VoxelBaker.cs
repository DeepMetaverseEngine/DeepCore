using DeepCore.IO;
using DeepCore.SharpZipLib;
using DeepCore.Unity;
using DeepCore.Unity3D.Voxel;
using DeepCore.Voxel.Data;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepCore.UnityEditor.Voxel
{
    public class VoxelBaker
    {
        public VoxelBakerConfig config { get; private set; }

        private GameObject voxelRoot;
        private VoxelAABB aabb;
        private int layerMask;
        private Vector3 startPos;

        private HashMap<GameObject, MeshCollider> addingMeshColliders;
        private HashMap<Texture2D, Texture2D> raytestTextures;

        public VoxelBaker(VoxelBakerConfig config)
        {
            this.config = config;
        }

        //-------------------------------------------------------------------------------------------------------------
        public void RunBakeToXml()
        {
            Physics.queriesHitBackfaces = true;
            var root = SceneManager.GetActiveScene().GetRootGameObjects().FindDeep<VoxelAABB>(t => t.gameObject.GetComponent<VoxelAABB>());
            if (root)
            {
                this.voxelRoot = root.gameObject;
                this.aabb = root;
                this.startPos = aabb.bounds.min;
                this.layerMask = -5;
                if (!string.IsNullOrWhiteSpace(config.ignoreLayers))
                {
                    var layers = config.ignoreLayers.Split(',');
                    this.layerMask = ~LayerMask.GetMask(layers);
                }
                BeginBake(root);
                try
                {
                    BakeToXml();
                }
                finally
                {
                    EndBake();
                }
            }
            else
            {
                Debug.LogError($"Can not find root object : root object must has '{nameof(VoxelAABB)}' Component");
            }
        }
        private void BeginBake(VoxelAABB aabb)
        {
            addingMeshColliders = new HashMap<GameObject, MeshCollider>();
            raytestTextures = new HashMap<Texture2D, Texture2D>();
            if (config.autoBindMeshCollider)
            {
                aabb.transform.ForEachDeep(t =>
                {
                    var go = t.gameObject;
                    if (go.TryGetComponent<MeshRenderer>(out var render))
                    {
                        if (!go.TryGetComponent<MeshCollider>(out var collider))
                        {
                            collider = go.AddComponent<MeshCollider>();
                            addingMeshColliders.Add(go, collider);
                        }
                    }
                });
            }
        }
        private void EndBake()
        {
            if (addingMeshColliders != null)
            {
                foreach (var kv in addingMeshColliders)
                {
                    MeshCollider.DestroyImmediate(kv.Value);
                }
                addingMeshColliders.Clear();
            }
            if (raytestTextures != null)
            {
                foreach (var t in raytestTextures.Values)
                {
                    Texture2D.DestroyImmediate(t);
                }
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







        private void BakeToXml()
        {
            // Create a new progress indicator

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var width = config.width;
            int length_x = (int)(aabb.bounds.size.x / width);
            int length_y = (int)(aabb.bounds.size.z / width);
            var Voxels = new VoxelTerrainData();
            Voxels.GridSize = width;
            Voxels.Grids = new VoxelNodeData[length_x, length_y][];
            Voxels.XLength = length_x;
            Voxels.YLength = length_y;
            Voxels.MinX = aabb.bounds.min.x;
            Voxels.MinY = aabb.bounds.min.z;
            Voxels.MaxX = aabb.bounds.max.x;
            Voxels.MaxY = aabb.bounds.max.z;
            //var vs = new List<VoxelLayer>[length_x, length_y];
            try
            {
                var cancel = false;
                var total = length_x * length_y;
                var count = 0;
                for (int i = 0; i < length_x; i++)
                {
                    for (int j = 0; j < length_y; j++)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar("BakeToXml", $"Ray Test : {i} , {j}", (count / (float)total)))
                        {
                            cancel = true;
                            return;
                        }
                        using (var odc = new RayTestObjectGroup(this, i, j))
                        {
                            var voxels = odc.RayTestObjects();
                            //UpdateObjectDescDictWithBoxCast(odc, i * width, j * width);
                            var layers = Voxels.Grids[i, j] = voxels.ToArray();
                            if (layers == null || layers.Length == 0)
                            {
                                //黑洞需要填平
                                var vl = new VoxelNodeData
                                {
                                    Downward = config.minLevel - config.width,
                                    Upward = config.minLevel,
                                    Color = 0,//VoxelBaker.ColorToUint(Color.black)
                                };
                                var vls = new VoxelNodeData[1] { vl };
                                Voxels.Grids[i, j] = vls;
                            }
                        }
                        count++;
                    }
                }
                System.Threading.Thread.Sleep(1);
                Debug.Log($"ray cast time: {sw.Elapsed}");
                sw.Reset();
                sw.Start();
                try
                {
                    var voxtPath = (
                            Application.dataPath + "/../_output/voxelbaker/" +
                            SceneManager.GetActiveScene().path.Replace(".unity", "/").ToLower() +
                            SceneManager.GetActiveScene().name.ToLower() + ".voxt");
                    var zipPath = voxtPath + ".zip";
                    {
                        System.Threading.Thread.Sleep(1);
                        CFiles.CreateFile(voxtPath);
                        using (var output = File.OpenWrite(voxtPath))
                        {
                            using (var writer = new StreamWriter(output))
                            {
                                cancel = VoxelTerrainData.SaveToText(Voxels, writer, (t, p) =>
                                {
                                    return EditorUtility.DisplayCancelableProgressBar("Encode Voxt", $"Encode Voxt : {t}", p);
                                });
                            }
                        }
                        if (cancel)
                        {
                            CFiles.Delete(voxtPath);
                        }
                        System.Threading.Thread.Sleep(1);
                    }
                    AssetDatabase.Refresh();
                    if (!cancel)
                    {
                        System.Threading.Thread.Sleep(1);
                        using (var zipfile = ZipUtil.CreateZipFile(zipPath))
                        {
                            zipfile.BeginUpdate();
                            zipfile.Add(voxtPath, Path.GetFileName(voxtPath));
                            zipfile.CommitUpdate();
                            zipfile.Close();
                        }
                        System.Threading.Thread.Sleep(1);
                    }
                }
                finally
                {
                    sw.Stop();
                }
                Debug.Log($"object to json cost time: {sw.Elapsed}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            //同时导出navmesh obj
            //var metaPath1 = Application.dataPath.Replace("Assets", "") + SceneManager.GetActiveScene().path.Replace(".unity", "/") + SceneManager.GetActiveScene().name + ".obj";
            //NavMeshExport._Export(metaPath1);
            //var obj_path = NavMeshExport.ExportRoad1();
        }

        public class RayTestObjectGroup : IDisposable
        {
            public class RayTestObjectHits
            {
                public Transform transform;
                public VoxelNodeData layer;
                public List<RaycastHit> hits = new List<RaycastHit>();
            }
            public VoxelBakerConfig config => baker.config;

            private Dictionary<Transform, RayTestObjectHits> dicts = new Dictionary<Transform, RayTestObjectHits>();
            private VoxelBaker baker;
            private int i, j;
            public RayTestObjectGroup(VoxelBaker baker, int i, int j)
            {
                this.baker = baker;
                this.i = i;
                this.j = j;
            }

            public void Dispose()
            {
                dicts.Clear();
                dicts = null;
            }

            public List<VoxelNodeData> RayTestObjects()
            {
                var width = config.width;
                var x0 = baker.startPos.x + (i) * width;
                var y0 = baker.startPos.z + (j) * width;
                var x1 = baker.startPos.x + (i + 1) * width;
                var y1 = baker.startPos.z + (j + 1) * width;
                var xc = baker.startPos.x + (i + 0.5f) * width;
                var yc = baker.startPos.z + (j + 0.5f) * width;
                {
                    UpdateObjectDescDict(xc, yc);
                    UpdateObjectDescDict(x0, y0);
                    UpdateObjectDescDict(x0, y1);
                    UpdateObjectDescDict(x1, y1);
                    UpdateObjectDescDict(x1, y0);
                }
                if (config.useBoxCast)
                {
                    UpdateObjectDescDictBox(xc, yc);
                }
                var voxels = new List<VoxelNodeData>();
                foreach (var hit in dicts.Values)
                {
                    SplitVoxels(xc, yc, hit, voxels);
                }
                voxels.Sort(new System.Comparison<VoxelNodeData>((i1, i2) => i1.Upward.CompareTo(i2.Upward)));
                return voxels;
            }
            private void UpdateObjectDescDictBox(float x, float z)
            {
                var r = config.width * 0.5f;
                var min = Mathf.Min(config.raycastLimit, -config.raycastLimit);
                var max = Mathf.Max(config.raycastLimit, -config.raycastLimit);
                var hitlist = new List<RaycastHit>();
                {
                    var from = new Vector3(x, max, z);
                    var hits = Physics.BoxCastAll(from, Vector3.one * r, Vector3.down, Quaternion.identity, Mathf.Infinity, baker.layerMask);
                    hitlist.AddRange(hits);
                }
                {
                    var from = new Vector3(x, min, z);
                    var hits = Physics.BoxCastAll(from, Vector3.one * r, Vector3.up, Quaternion.identity, Mathf.Infinity, baker.layerMask);
                    hitlist.AddRange(hits);
                }
                hitlist.Sort((a, b) => CMath.GetDirect(b.point.y - a.point.y));
                foreach (var item in hitlist)
                {
                    UpdateObject(item);
                }
            }
            private void UpdateObjectDescDict(float x, float z)
            {
                var min = Mathf.Min(config.raycastLimit, -config.raycastLimit);
                var max = Mathf.Max(config.raycastLimit, -config.raycastLimit);
                var hitlist = new List<RaycastHit>();
                {
                    var from = new Vector3(x, max, z);
                    var hits = Physics.RaycastAll(from, Vector3.down, Mathf.Infinity, baker.layerMask);
                    hitlist.AddRange(hits);
                }
                {
                    var from = new Vector3(x, min, z);
                    var hits = Physics.RaycastAll(from, Vector3.up, Mathf.Infinity, baker.layerMask);
                    hitlist.AddRange(hits);
                }
                hitlist.Sort((a, b) => CMath.GetDirect(b.point.y - a.point.y));
                foreach (var item in hitlist)
                {
                    UpdateObject(item);
                }
            }
            private void UpdateObject(RaycastHit hit)
            {
                var v = hit.point;
                if (dicts.TryGetValue(hit.transform, out var hits))
                {
                    var od = hits.layer;
                    od.Downward = Mathf.Min(od.Downward, v.y);
                    od.Upward = Mathf.Max(od.Upward, v.y);
                }
                else
                {
                    hits = new RayTestObjectHits()
                    {
                        transform = hit.transform,
                        layer = new VoxelNodeData(),
                    };
                    var od = hits.layer;
                    od.Upward = v.y;
                    od.Downward = v.y;
                    od.Color = GetObjectColor(in hit, out var is_baseline);
                    od.BaseLine = is_baseline;
                    dicts.Add(hit.transform, hits);
                }
                hits.hits.Add(hit);
            }
            private void SplitVoxels(float xc, float yc, RayTestObjectHits obj, List<VoxelNodeData> voxels)
            {
                var r = config.width * 0.5f;
                var h = config.minHeight * 0.5f;
                var curLayer = new VoxelNodeData()
                {
                    Downward = obj.layer.Downward,
                    Upward = obj.layer.Upward,
                    Color = obj.layer.Color,
                    BaseLine = obj.layer.BaseLine,
                };
                voxels.Add(curLayer);
                for (float z = obj.layer.Downward; z <= obj.layer.Upward; z += config.minHeight)
                {
                    if (Physics.CheckBox(new Vector3(xc, z, yc), new Vector3(r, h, r), Quaternion.identity, baker.layerMask))
                    {
                        if (curLayer == null)
                        {
                            curLayer = new VoxelNodeData() { Color = obj.layer.Color, };
                            curLayer.Downward = Math.Max(z - h, obj.layer.Downward);
                            curLayer.Upward = Math.Min(z + h, obj.layer.Upward);
                            voxels.Add(curLayer);
                        }
                        else
                        {
                            curLayer.Upward = Math.Min(z + h, obj.layer.Upward);
                        }
                    }
                    else
                    {
                        if (curLayer != null)
                        {
                            curLayer.Upward = Math.Max(z - h, obj.layer.Downward);
                            curLayer = null;
                        }
                    }
                }
                curLayer = voxels[voxels.Count - 1];
                curLayer.Upward = obj.layer.Upward;

            }

            uint GetObjectColor(in RaycastHit hit, out bool is_baseline)
            {
                if (config.useMeshColor && TryGetObjectColorMeshRender(in hit, out var color, out is_baseline))
                {
                    return color;
                }
                else if (config.useNavMesh && TryGetObjectColorNavMesh(in hit, out color, out is_baseline))
                {
                    return color;
                }
                else if (TryObjectColorLayer(in hit, out color, out is_baseline))
                {
                    return color;
                }
                else
                {
                    return 0;
                }
            }
            bool TryGetObjectColorMeshRender(in RaycastHit hit, out uint color, out bool is_baseline)
            {
                var go = hit.transform.gameObject;
                var flag = LayerMask.LayerToName(go.layer);
                is_baseline = flag == config.layerBaseLine;
                if (baker.TryGetRayCastPixel(in hit, out var pcolor))
                {
                    color = ColorToUint(pcolor);
                    return true;
                }
                color = 0;
                return false;
            }
            bool TryGetObjectColorNavMesh(in RaycastHit hit, out uint color, out bool is_baseline)
            {
                var go = hit.transform.gameObject;
                bool is_nav_static = (GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.NavigationStatic) != 0;
                //优先层设置
                string flag = LayerMask.LayerToName(go.layer);
                is_baseline = flag == config.layerBaseLine;
                if (is_nav_static)
                {
                    //目前只区分不可行走
                    if (GameObjectUtility.GetNavMeshArea(go) == UnityEngine.AI.NavMesh.GetAreaFromName(config.layerNotWalkable))
                    {
                        color = ColorToUint(Color.red);
                        return true;
                    }
                    else if (GameObjectUtility.GetNavMeshArea(go) == UnityEngine.AI.NavMesh.GetAreaFromName(config.layerWater))
                    {
                        color = ColorToUint(Color.blue);
                        return true;
                    }
                }
                color = 0;
                return false;
            }
            bool TryObjectColorLayer(in RaycastHit hit, out uint color, out bool is_baseline)
            {
                var go = hit.transform.gameObject;
                var flag = LayerMask.LayerToName(go.layer);
                is_baseline = flag == baker.config.layerBaseLine;
                if (is_baseline)
                {
                    color = ColorToUint(Color.green);
                    return true;
                }
                //目前只区分不可行走
                else if (flag == config.layerNavLayer)
                {
                    color = ColorToUint(Color.green);
                    return true;
                }
                else if (flag == config.layerWater)
                {
                    color = ColorToUint(Color.blue);
                    return true;
                }
                else if (flag == config.layerDummyLayer)
                {
                    color = ColorToUint(Color.black);
                    return true;
                }
                color = 0;
                return false;
            }
        }


        private static void Adjusts(List<VoxelNodeData> voxels, ref int water_index, ref int baseline, int i)
        {
            if (i == baseline)
            {
                baseline = -1;
            }
            if (i == water_index)
            {
                water_index = -1;
            }
            if (i < baseline)
            {
                baseline--;
            }
            if (i < water_index)
            {
                water_index--;
            }
            voxels.RemoveAt(i);
        }
        public static List<VoxelNodeData> MergeVoxels(List<VoxelNodeData> voxels, int x, int y, float minLevel)
        {
            voxels.Sort(new System.Comparison<VoxelNodeData>(
                      (i1, i2) => i1.Upward.CompareTo(i2.Upward)));

            int size = voxels.Count;

            int water_index = -1;
            for (int i = voxels.Count - 1; i > 0; i--)
            {
                if (voxels[i].Color == ColorToUint(Color.blue))
                {
                    water_index = i;
                    break;
                }
            }
            int baseline = -1;
            for (int i = voxels.Count - 1; i > 0; i--)
            {
                if (voxels[i].BaseLine)
                {
                    baseline = i;
                    break;
                }
            }

            foreach (var voxel in voxels)
            {
                if ((voxel.Upward - voxel.Downward) < 0.5)
                {
                    voxel.Downward = voxel.Upward - 0.5f;
                }
            }

            for (int i = voxels.Count - 1; i >= 0; i--)
            {
                //if (i - 1 >= 0)
                {
                    //直接移除最低上沿以下部分
                    if (voxels[i].Upward < minLevel)
                    {
                        Adjusts(voxels, ref water_index, ref baseline, i);
                        continue;
                    }
                    ////直接移除水面以下部分
                    if (water_index > -1 && i < water_index)
                    {
                        Adjusts(voxels, ref water_index, ref baseline, i);
                        continue;
                    }
                    //直接移除baseline以下部分
                    if (baseline > -1 && i < baseline)
                    {
                        //Debug.LogWarningFormat("baseline {0} index {1} Count {2}", baseline, i, size);
                        Adjusts(voxels, ref water_index, ref baseline, i);
                        continue;
                    }
                    if (i > 0)
                    {
                        //var last_downward = voxels[i].downward;
                        //if (last_downward > voxels[i - 1].downward && last_downward < voxels[i - 1].upward)
                        //{
                        //    //走不到
                        //    voxels[i - 1].color = voxels[i].color;
                        //    voxels[i - 1].upward = voxels[i].upward;
                        //    voxels.RemoveAt(i);
                        //    continue;
                        //}

                        //var last_upward = voxels[i].upward;
                        //if (last_upward > voxels[i - 1].downward && last_upward < voxels[i - 1].upward)
                        //{
                        //    //伸展下延，不需要改变颜色
                        //    voxels[i - 1].downward = voxels[i].downward;
                        //    voxels.RemoveAt(i);
                        //    continue;
                        //}

                        //var last_downward2 = voxels[i - 1].downward;
                        //if (last_downward2 > voxels[i].downward && last_downward2 < voxels[i].upward)
                        //{
                        //    //伸展上沿，需要改变颜色
                        //    voxels[i - 1].color = voxels[i].color;
                        //    voxels[i - 1].upward = voxels[i].upward;
                        //    voxels.RemoveAt(i);
                        //    continue;
                        //}

                        if (voxels[i].Downward < voxels[i - 1].Upward)
                        {
                            voxels[i - 1].Upward = voxels[i].Upward;
                            voxels[i - 1].Downward = voxels[i - 1].Downward < voxels[i].Downward ?
                                voxels[i - 1].Downward : voxels[i].Downward;
                            voxels[i - 1].Color = voxels[i].Color;
                            Adjusts(voxels, ref water_index, ref baseline, i);
                            continue;
                        }

                        //var last_upward2 = voxels[i - 1].upward;
                        //if (last_upward2 > voxels[i].downward && last_upward2 < voxels[i].upward)
                        //{
                        //    //走不到
                        //    voxels[i - 1].downward = voxels[i].downward;
                        //    voxels.RemoveAt(i);
                        //    continue;
                        //}

                        //下沿和上沿距離小於2米的時候合并
                        var last_downward3 = voxels[i].Downward;
                        if (last_downward3 - voxels[i - 1].Upward < 2)
                        {
                            voxels[i - 1].Color = voxels[i].Color;
                            voxels[i - 1].Upward = voxels[i].Upward;
                            //Debug.LogWarningFormat("[remove voxel] x: {0}, y: {1}, i: {2}, color: {3}",
                            //    x, y, i, UintToColor(voxels[i].color));
                            Adjusts(voxels, ref water_index, ref baseline, i);
                            continue;
                        }

                    }

                }
            }

            //foreach (var voxel in voxels)
            //{
            //    if ((voxel.upward - voxel.downward) < 1)
            //    {
            //        voxel.downward = voxel.upward - 1f;
            //    }
            //}
            if (voxels.Count > 0)
                voxels[0].Downward = minLevel;

            return voxels;
        }

        public static byte FloatToInt(float f)
        {
            return (byte)Mathf.Max(0, Mathf.Min(255, (int)Mathf.FloorToInt(f * 256.0f)));
        }
        public static uint ColorToUint(Color c)
        {
            uint rtn = 0;
            //rtn = rtn + (uint)FloatToInt(c.a) << 24;
            //rtn = rtn + (uint)FloatToInt(c.r) << 16;
            //rtn = rtn + (uint)FloatToInt(c.g) << 8;
            //rtn = rtn + (uint)FloatToInt(c.b);

            Color32 c32 = (Color32)c;
            rtn = rtn | (uint)c32.a << 24;
            rtn = rtn | (uint)c32.r << 16;
            rtn = rtn | (uint)c32.g << 8;
            rtn = rtn | (uint)c32.b;

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

    }


}