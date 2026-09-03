using DeepCore.Voxel.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static DeepCore.Unity3D.Voxel.VoxelBaker;

namespace DeepCore.Unity3D.Voxel
{
    public static class VoxelToMesh
    {
        public static int CHUNK_SLICE = 32;
        public const int CUBE_VERTICES_COUNT = 24;
        public class VoxelCubeTemplate
        {
            readonly public Vector3[] vertices;
            readonly public Vector3[] normals;
            readonly public int[] triangles;
            public VoxelCubeTemplate(float grid)
            {
                var cube = CreateVoxelCube(grid);
                this.vertices = cube.vertices;
                this.normals = cube.normals;
                this.triangles = cube.triangles;
            }
        }
        private static Mesh voxelTemp;
        public static Mesh CreateVoxelCube(float grid = 1f)
        {
            if (voxelTemp == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                voxelTemp = GameObject.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);
                GameObject.DestroyImmediate(temp);
            }
            var cube = GameObject.Instantiate(voxelTemp);
            {
                var vertices = cube.vertices;
                for (int i = 0; i < cube.vertexCount; i++)
                {
                    var v = vertices[i];
                    if (v.x < 0) v.x = 0; else v.x = grid;
                    if (v.y < 0) v.y = 0; else v.y = grid;
                    if (v.z < 0) v.z = 0; else v.z = grid;
                    vertices[i] = v;
                }
                cube.vertices = vertices;
            }
            return cube;
        }

        public class VoxelChunk
        {
            public Vector3[] vertices;
            public Color32[] colors32;
            public Vector3[] normals;
            public int[] triangles;
            public Vector3 position;
            public Geometry.Location2D sliceLocation;
            public Mesh GetMesh()
            {
                var mesh = new Mesh();
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.colors32 = colors32;
                mesh.triangles = triangles;
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.Optimize();
                return mesh;
            }
        }
        class VoxelChunkBaker
        {
            private readonly VoxelTerrain3D terrain;
            private readonly VoxelLayer[] layers;
            private readonly Geometry.Location2D sliceLocation;
            private readonly float grid;
            private readonly float totalW;
            private readonly float totalH;
            private readonly VoxelCubeTemplate cube;
            private readonly int vcount;
            private readonly int tcount;

            private Vector3[] vertices;
            private Vector3[] normals;
            private Color32[] colors32;
            private int[] triangles;
            private int vindex = 0;
            private int tindex = 0;

            internal VoxelChunkBaker(VoxelCubeTemplate cube, VoxelTerrain3D terrain, VoxelLayer[] layers, Geometry.Location2D sliceLocation)
            {
                this.terrain = terrain;
                this.layers = layers;
                this.sliceLocation = sliceLocation;
                this.cube = cube;
                this.grid = terrain.GridCellSize;
                this.totalW = terrain.TotalSizeY;
                this.totalH = terrain.TotalSizeY;
                this.vcount = layers.Length * cube.vertices.Length;
                this.tcount = layers.Length * cube.triangles.Length;
            }
            private void AddLayer(VoxelLayer layer, in int vi, in int ti)
            {
                var c = layer.OwnerCell;
                var cx = (c.X - sliceLocation.X);
                var cy = (c.Y - sliceLocation.Y);
                var sliceH = CHUNK_SLICE * grid;
                var offset = new Vector3(
                    cx * grid,
                    layer.Downward,
                    sliceH - cy * grid - grid);
                var color = new Color32();
                DeepCore.Colors.DecodeARGB(layer.Color.ARGB, out color.r, out color.g, out color.b, out color.a);
                color.a = 255;
                for (int i = 0; i < cube.vertices.Length; i++)
                {
                    var v = cube.vertices[i];
                    v.x = v.x + offset.x;
                    v.z = v.z + offset.z;
                    if (v.y == 0) v.y = layer.Downward;
                    else v.y = layer.Upward;
                    vertices[vi + i] = v;
                    colors32[vi + i] = color;
                    normals[vi + i] = cube.normals[i];
                }
                for (int i = 0; i < cube.triangles.Length; i++)
                {
                    triangles[ti + i] = vi + cube.triangles[i];
                }
            }
            public VoxelChunk GetChunk()
            {
                var mesh = new VoxelChunk();
                var sliceH = CHUNK_SLICE * grid;
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.colors32 = colors32;
                mesh.triangles = triangles;
                mesh.sliceLocation = sliceLocation;
                mesh.position = new Vector3(sliceLocation.X * grid, 0, totalH - sliceLocation.Y * grid - sliceH);
                return mesh;
            }

            public void Run()
            {
                this.vertices = new Vector3[vcount];
                this.normals = new Vector3[vcount];
                this.colors32 = new Color32[vcount];
                this.triangles = new int[tcount];
                this.vindex = 0;
                this.tindex = 0;
                foreach (var layer in layers)
                {
                    var vi = vindex;
                    var ti = tindex;
                    this.vindex += cube.vertices.Length;
                    this.tindex += cube.triangles.Length;
                    AddLayer(layer, vi, ti);
                }
            }

        }
        //----------------------------------------------------------------------------------------------------------------------------------------
        public static void BakeVoxelChunkLayers(VoxelCubeTemplate cube, VoxelTerrain3D terrain, Geometry.Location2D sliceLocation, List<VoxelLayer> layers, Action<VoxelChunk> cb)
        {
            var max_vcount = 2400;
            while (layers.Count > 0)
            {
                var count = Math.Min(max_vcount, layers.Count);
                var layersArray = new VoxelLayer[count];
                layers.CopyTo(0, layersArray, 0, count);
                layers.RemoveRange(0, count);
                var bake = new VoxelChunkBaker(cube, terrain, layersArray, sliceLocation);
                bake.Run();
                cb(bake.GetChunk());
            }
        }
        public static Stack<VoxelChunk> BakeVoxelChunkMeshs(VoxelTerrain3D terrain)
        {
            var grid = terrain.GridCellSize;
            var cube = new VoxelCubeTemplate(grid);
            var meshs = new Stack<VoxelChunk>();
            var tasks = new List<Task>();
            terrain.ForEachChunkLayers(0, (layers, loc, st) =>
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        BakeVoxelChunkLayers(cube, terrain, loc, layers, chunk =>
                        {
                            lock (meshs) { meshs.Push(chunk); }
                        });
                    }
                    catch (Exception err)
                    {
                        Debug.LogError(err);
                    }
                }));
            }, CHUNK_SLICE);
            Task.WaitAll(tasks.ToArray());
            return meshs;
        }

        public static async Task<Stack<VoxelChunk>> BakeVoxelChunkMeshsAsync(VoxelTerrain3D terrain, Action<VoxelChunk> onload = null)
        {
            var grid = terrain.GridCellSize;
            var cube = new VoxelCubeTemplate(grid);
            var meshs = new Stack<VoxelChunk>();
            var tasks = new List<Task>();
            terrain.ForEachChunkLayers(0, (layers, loc, st) =>
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        BakeVoxelChunkLayers(cube, terrain, loc, layers, chunk =>
                        {
                            onload(chunk);
                            lock (meshs) { meshs.Push(chunk); }
                        });
                    }
                    catch (Exception err)
                    {
                        Debug.LogError(err);
                    }
                }));
            }, CHUNK_SLICE);
            await Task.WhenAll(tasks.ToArray());
            return meshs;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------
        public static GameObject InstancingVoxelLayer(VoxelLayer layer, Mesh sharedMesh, GameObject voxelTemp)
        {
            var totalH = layer.OwnerCell.Terrain.TotalSizeY;
            var cell_size = layer.OwnerCell.Terrain.GridCellSize;
            var cell_body = UnityEngine.Object.Instantiate(voxelTemp);
            {
                cell_body.transform.localPosition = new Vector3(
                  layer.X * cell_size,
                  layer.Downward,
                  totalH - layer.Y * cell_size);
                cell_body.transform.localScale = new Vector3(cell_size, layer.Height, cell_size);
            }
            if (cell_body.TryGetComponent<MeshFilter>(out var cell_meshf))
            {
                cell_meshf.sharedMesh = sharedMesh;
                cell_meshf.mesh = sharedMesh;
            }
            if (cell_body.TryGetComponent<MeshRenderer>(out var cell_meshr))
            {
                Colors.DecodeARGB(layer.Color, out float r, out var g, out var b, out var a);
                cell_meshr.material = GameObject.Instantiate(cell_meshr.material);
                cell_meshr.material.SetColor("_Color", new Color(r, g, b, 1f));
                cell_meshr.material.color = new Color(r, g, b, a);
                cell_meshr.material.enableInstancing = true;
            }
            cell_body.isStatic = true;
            cell_body.SetActive(true);
            return cell_body;
        }
        public static GameObject InstancingVoxelTerrain(VoxelTerrain3D terrain, GameObject voxelTemp)
        {
            var tvoxel = new GameObject("VoxelTerrain");
            tvoxel.AddComponent<VoxelTest>();
            var cube = CreateVoxelCube(1f);
            tvoxel.SetLayer(voxelTemp.layer);
            terrain.ForEachLayers(0, (layer, st) =>
            {
                var cell_body = InstancingVoxelLayer(layer, cube, voxelTemp);
                if (cell_body != null)
                {
                    cell_body.transform.SetParent(tvoxel.transform, false);
                    cell_body.SetLayer(voxelTemp.layer);
                }
                return false;
            });
            tvoxel.isStatic = true;
            StaticBatchingUtility.Combine(tvoxel);
            tvoxel.transform.localPosition = new Vector3(
                                          terrain.ResourceStartX,
                                          0,
                                          terrain.ResourceStartY);
            return tvoxel;
        }
        public static GameObject InstancingVoxelTerrainChunks(VoxelTerrain3D vox, GameObject cube = null)
        {
            var tvoxel = new GameObject("VoxelTerrain");
            tvoxel.AddComponent<VoxelTest>();
            var chunks = VoxelToMesh.BakeVoxelChunkMeshs(vox);
            while (chunks.TryPop(out var chunk))
            {
                var body = cube != null ? GameObject.Instantiate(cube.gameObject) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.GetComponent<MeshFilter>().mesh = chunk.GetMesh();
                body.transform.localPosition = chunk.position;
                body.transform.SetParent(tvoxel.transform);
                body.SetActive(true);
            }
            tvoxel.transform.localPosition = new Vector3(
                                          vox.ResourceStartX,
                                          0,
                                          vox.ResourceStartY);
            return tvoxel;
        }
        public static GameObject InstancingVoxelTerrainChunks(VoxelTerrainData data, VoxelBuildConfig cfg, GameObject cube = null)
        {
            var vox = new VoxelTerrain3D(data, cfg);
            return InstancingVoxelTerrainChunks(vox, cube);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------
        public static GameObject InstancingHitLayer(VoxelTerrainData data, RayTestObjectHits node, Mesh sharedMesh, GameObject voxelTemp)
        {
            var cell_size = data.GridSize;
            var cell_body = UnityEngine.Object.Instantiate(voxelTemp);
            {
                cell_body.transform.localPosition = new Vector3(
                  node.iX * cell_size,
                  node.Downward,
                  node.iY * cell_size);
                cell_body.transform.localScale = new Vector3(cell_size, node.Height, cell_size);
            }
            if (cell_body.TryGetComponent<MeshFilter>(out var cell_meshf))
            {
                cell_meshf.sharedMesh = sharedMesh;
                cell_meshf.mesh = sharedMesh;
            }
            Colors.DecodeARGB(node.Color, out float r, out var g, out var b, out var a);
            if (cell_body.TryGetComponent<MeshRenderer>(out var cell_meshr))
            {
                cell_meshr.material = GameObject.Instantiate(cell_meshr.material);
                cell_meshr.material.SetColor("_Color", new Color(r, g, b, 1f));
                cell_meshr.material.color = new Color(r, g, b, a);
                cell_meshr.material.enableInstancing = true;
            }
            var tracer = cell_body.AddComponent<VoxelTracer>();
            {
                tracer.hitTarget = node.transform;
                tracer.Color = new Color(r, g, b, a);
                tracer.Upward = node.Upward;
                tracer.Downward = node.Downward;
                tracer.BaseLine = node.BaseLine;
                tracer.iX = node.iX;
                tracer.iY = node.iY;
            }
            cell_body.name = $"{tracer.iX},{tracer.iY}";
            cell_body.SetActive(true);
            return cell_body;
        }
        public static GameObject InstancingHitNodes(VoxelTerrainData data, IEnumerable<RayTestObjectHits> nodes, GameObject voxelTemp)
        {
            var tvoxel = new GameObject("VoxelTerrain");
            tvoxel.AddComponent<VoxelTest>();
            var cube = CreateVoxelCube(1f);
            foreach (var layer in nodes)
            {
                var cell_body = InstancingHitLayer(data, layer, cube, voxelTemp);
                if (cell_body != null)
                {
                    cell_body.transform.SetParent(tvoxel.transform, true);
                }
            }
            tvoxel.transform.localPosition = new Vector3(
                                          data.MinX,
                                          0,
                                          data.MinY);
            return tvoxel;
        }
        public static GameObject InstancingTerrainDataLayer(VoxelTerrainData data, VoxelNodeData node, Mesh sharedMesh, GameObject voxelTemp)
        {
            if (node.state is RayTestObjectHits hit)
            {
                var cell_size = data.GridSize;
                var cell_body = UnityEngine.Object.Instantiate(voxelTemp);
                {
                    cell_body.transform.localPosition = new Vector3(
                      hit.iX * cell_size,
                      node.Downward,
                      hit.iY * cell_size);
                    cell_body.transform.localScale = new Vector3(cell_size, node.Height, cell_size);
                }
                if (cell_body.TryGetComponent<MeshFilter>(out var cell_meshf))
                {
                    cell_meshf.sharedMesh = sharedMesh;
                    cell_meshf.mesh = sharedMesh;
                }
                Colors.DecodeARGB(node.Color, out float r, out var g, out var b, out var a);
                if (cell_body.TryGetComponent<MeshRenderer>(out var cell_meshr))
                {
                    cell_meshr.material = GameObject.Instantiate(cell_meshr.material);
                    cell_meshr.material.SetColor("_Color", new Color(r, g, b, 1f));
                    cell_meshr.material.color = new Color(r, g, b, a);
                    cell_meshr.material.enableInstancing = true;
                }
                var tracer = cell_body.AddComponent<VoxelTracer>();
                {
                    tracer.hitTarget = hit.transform;
                    tracer.Color = new Color(r, g, b, a);
                    tracer.Upward = node.Upward;
                    tracer.Downward = node.Downward;
                    tracer.BaseLine = node.BaseLine;
                    tracer.iX = hit.iX;
                    tracer.iY = hit.iY;
                }
                cell_body.name = $"{tracer.iX},{tracer.iY}";
                cell_body.SetActive(true);
                return cell_body;
            }
            return null;
        }
        public static GameObject InstancingTerrainData(VoxelTerrainData data, GameObject voxelTemp)
        {
            var tvoxel = new GameObject("VoxelTerrain");
            tvoxel.AddComponent<VoxelTest>();
            var cube = CreateVoxelCube(1f);
            data.ForEachVoxelNodes((ix, iy, layerIndex, node) =>
            {
                var cell_body = InstancingTerrainDataLayer(data, node, cube, voxelTemp);
                if (cell_body != null)
                {
                    cell_body.transform.SetParent(tvoxel.transform, true);
                }
            });
            tvoxel.transform.localPosition = new Vector3(
                                          data.MinX,
                                          0,
                                          data.MinY);
            return tvoxel;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------
#if false
        public static Mesh CreateVoxelLayer8(VoxelLayer layer)
        {
            var w = layer.OwnerCell.Terrain.GridCellSize;
            var h = layer.Height;
            var color = new Color32();
            DeepCore.Colors.DecodeARGB(layer.Color, out color.r, out color.g, out color.b, out color.a);
            var mesh = new Mesh();
            mesh.vertices = new Vector3[] {
                new Vector3(0, 0, 0),
                new Vector3(w, 0, 0),
                new Vector3(w, h, 0),
                new Vector3(0, h, 0),
                new Vector3(0, h, w),
                new Vector3(w, h, w),
                new Vector3(w, 0, w),
                new Vector3(0, 0, w),
                };
            mesh.normals = new Vector3[] {
                new Vector3(0, 0, 0),
                new Vector3(w, 0, 0),
                new Vector3(w, h, 0),
                new Vector3(0, h, 0),
                new Vector3(0, h, w),
                new Vector3(w, h, w),
                new Vector3(w, 0, w),
                new Vector3(0, 0, w),
                };
            mesh.colors32 = new Color32[] {
                color,
                color,
                color,
                color,
                color,
                color,
                color,
                color,
                };
            mesh.triangles = new int[] {
                0, 2, 1, //face front
  			    0, 3, 2, //
                2, 3, 4, //face top
  			    2, 4, 5, //
                1, 2, 5, //face right
  			    1, 5, 6, //
                0, 7, 4, //face left
  			    0, 4, 3, //
                5, 4, 7, //face back
  			    5, 7, 6, //
                0, 6, 7, //face bottom
  			    0, 1, 6, //
                };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
            return mesh;
        }
        public static Mesh CreateVoxelLayer(VoxelLayer layer, Mesh cube, out Vector3 pos)
        {
            var w = layer.OwnerCell.Terrain.GridCellSize;
            var h = layer.Height;
            var c = layer.OwnerCell;
            var totalH = layer.OwnerCell.Terrain.TotalSizeY;
            var color = new Color32();
            DeepCore.Colors.DecodeARGB(layer.Color, out color.r, out color.g, out color.b, out color.a);
            var colors32 = new Color32[cube.vertexCount];
            var vertices = new Vector3[cube.vertexCount];
            var cube_vertices = cube.vertices;
            for (int i = 0; i < cube.vertexCount; i++)
            {
                var v = cube_vertices[i];
                if (v.y < 0) v.y = 0; else if (v.y > 0) v.y = h;
                vertices[i] = v;
                colors32[i] = color;
            }
            var mesh = Mesh.Instantiate(cube);
            mesh.vertices = vertices;
            mesh.colors32 = colors32;
            pos = new Vector3(c.X * w, layer.Downward, totalH - c.Y * w - w);
            return mesh;
        }
        public static Mesh CreateVoxelMesh(VoxelTerrain3D terrain)
        {
            var gird = terrain.GridCellSize;
            var totalH = terrain.TotalSizeY;
            var cube = CreateVoxelCube(gird);
            var combines = new List<CombineInstance>();
            terrain.ForEachLayers(layer =>
            {
                var combine = new CombineInstance();
                combine.mesh = CreateVoxelLayer(layer, cube, out var pos);
                combine.transform = Matrix4x4.Translate(pos);
                combines.Add(combine);
                return false;
            });
            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combines.ToArray());
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
            return mesh;
        }
        public static Stack<VoxelMesh> CreateVoxelChunkMeshs(VoxelTerrain3D terrain)
        {
            var gird = terrain.GridCellSize;
            var totalH = terrain.TotalSizeY;
            var cube = CreateVoxelCube(gird);
            var meshs = new Stack<VoxelMesh>();
            terrain.ForEachChunkLayers((layers, loc) =>
            {
                var combines = new List<CombineInstance>();
                foreach (var layer in layers)
                {
                    var combine = new CombineInstance();
                    combine.mesh = CreateVoxelLayer(layer, cube, out var pos);
                    combine.transform = Matrix4x4.Translate(pos);
                    combines.Add(combine);
                }
                var mesh = new Mesh();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.CombineMeshes(combines.ToArray());
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.Optimize();
                meshs.Push(new VoxelMesh()
                {
                    position = new Vector3(loc.X * gird, 0, totalH - loc.Y * gird),
                    mesh = mesh,
                });
            }, CHUNK_SLICE);
            return meshs;
        }

        public class VoxelMesh
        {
            public Vector3 position;
            public Mesh mesh;
        }

#endif

    }
}
