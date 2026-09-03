using DeepCore;
using DeepCore.Game3D.Slave;
using DeepCore.Voxel.Data;
using UnityEngine;

namespace Code.BattleView
{
    public class UnityBattleVoxelTerrain
    {
        public UnityBattle View { get; }
        public UnityBattleVoxelTerrain(UnityBattle view, GameObject parent, VoxelClientTerrain3D voxel)
        {
            this.View = view;
            var gameObject = new GameObject("VoxelTerrain");
            gameObject.transform.SetParent(parent.transform, false);
            var cell_size = voxel.GridCellSize;
            voxel.World.Terrain.ForEachLayers(layer =>
            {
                var cell_body = CreateVoxelLayerObject(layer);
                if (cell_body != null)
                {
                    cell_body.transform.SetParent(gameObject.transform, false);
                }
                return false;
            });
            StaticBatchingUtility.Combine(gameObject);
        }

        public virtual GameObject CreateVoxelLayerObject(VoxelLayer layer)
        {
            var temp = GameObject.Find(View.VoxelTemplateName);
            if (temp != null && temp.activeInHierarchy)
            {
                var totalH = View.Battle.Layer.Terrain3D.TotalHeight;
                var cell_body = GameObject.Instantiate(temp);
                var cell_size = layer.OwnerCell.Terrain.GridCellSize;
                try
                {
                    cell_body.layer = LayerMask.NameToLayer(View.RayCastLayerName);
                }
                catch { }
                cell_body.name = $"VoxelLayer[{layer.X},{layer.Y},{layer.Layer}]";
                {
                    cell_body.transform.position = new Vector3(
                      layer.X * cell_size,
                      layer.Downward + layer.Height / 2,
                      totalH - layer.Y * cell_size);
                    cell_body.transform.localScale = new Vector3(1, layer.Height, 1);
                }
                {
                    var cell_meshr = cell_body.GetComponent<MeshRenderer>();
                    Colors.DecodeARGB(layer.Color, out float r, out var g, out var b, out var a);
                    cell_meshr.material.SetColor("_Color", new Color(r, g, b, a));
                }
                return cell_body;
            }
            else
            {
                var cell_size = layer.OwnerCell.Terrain.GridCellSize;
                var cell_body = new GameObject($"VoxelLayer[{layer.X},{layer.Y},{layer.Layer}]");
                try
                {
                    cell_body.layer = LayerMask.NameToLayer(View.RayCastLayerName);
                }
                catch { }

                var cell_mesh = CreateVoxelMesh(layer);
                var cell_meshf = cell_body.AddComponent<MeshFilter>();
                cell_meshf.mesh = cell_mesh;
                var cell_meshr = cell_body.AddComponent<MeshRenderer>();
                Colors.DecodeARGB(layer.Color, out float r, out var g, out var b, out var a);
                cell_meshr.material.SetColor("_Color", new Color(r, g, b, a));

                cell_body.transform.position = new Vector3(
                  layer.X * cell_size,
                  layer.Downward,
                  layer.Y * cell_size);

                return cell_body;
            }
            return null;
        }
        public virtual Mesh CreateVoxelMesh(VoxelLayer layer)
        {
            var w = layer.OwnerCell.Terrain.GridCellSize;
            var h = layer.Height;

            Vector3[] vertices = {
            new Vector3 (0, 0, 0),
            new Vector3 (w, 0, 0),
            new Vector3 (w, h, 0),
            new Vector3 (0, h, 0),
            new Vector3 (0, h, w),
            new Vector3 (w, h, w),
            new Vector3 (w, 0, w),
            new Vector3 (0, 0, w),
            };

            int[] triangles = {
            0, 2, 1, //face front
			0, 3, 2,
            2, 3, 4, //face top
			2, 4, 5,
            1, 2, 5, //face right
			1, 5, 6,
            0, 7, 4, //face left
			0, 4, 3,
            5, 4, 7, //face back
			5, 7, 6,
            0, 6, 7, //face bottom
			0, 1, 6
            };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.Optimize();
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}

