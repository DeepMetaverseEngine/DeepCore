using DeepCore.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Voxel.Data
{
    public static class VoxelRayCast
    {
        public static VoxelLayer RayCastVoxel(this VoxelTerrain3D terrain, RayCast ray, out Vector3 touch)
        {
            if (terrain != null)
            {
                Vector2 pos = ray.center;
                var dir = CMath.GetDegree(ray.normal.X, ray.normal.Y);
                var sqrt = (float)Math.Sqrt(terrain.TotalSizeX * terrain.TotalSizeX + terrain.TotalSizeY * terrain.TotalSizeY);
                var len = CMath.GetDistance(0, 0, ray.normal.X, ray.normal.Y) * sqrt;
                VoxelLayer ret = null;
                Vector3 ray_touch = Vector3.Zero;
                var tuple = (terrain, ray_touch);
                terrain.ForEachCellsRayStepPloar(ref tuple, ref pos, dir, len, (st, cell, cx, cy, current) =>
                {
                    if (cell != null)
                    {
                        var pp = new Vector3(current.X, current.Y, 0);
                        for (int i = cell.LayerCount - 1; i >= 0; --i)
                        {
                            var layer = cell.GetLayer(i);
                            pp.Z = layer.Upward;
                            ray_touch = RayCast.RayPlaneIntersection(ray.center, ray.normal, pp, Vector3.UnitZ);
                            if (CMath.IncludeRectPointW(
                                cell.X * terrain.GridCellSize,
                                cell.Y * terrain.GridCellSize,
                                terrain.GridCellSize,
                                terrain.GridCellSize,
                                ray_touch.X,
                                ray_touch.Y))
                            {
                                ret = layer;
                                return true;
                            }
                        }
                    }
                    return false;
                }, false);
                touch = ray_touch;
                return ret;
            }
            touch = Vector3.Zero;
            return null;
        }
        public static VoxelLayer RayCastVoxelBounding(this VoxelTerrain3D terrain, RayCast ray, out Vector3 touch)
        {
            if (terrain != null)
            {
                Vector2 pos = ray.center;
                var dir = CMath.GetDegree(ray.normal.X, ray.normal.Y);
                var sqrt = (float)Math.Sqrt(terrain.TotalSizeX * terrain.TotalSizeX + terrain.TotalSizeY * terrain.TotalSizeY);
                var len = CMath.GetDistance(0, 0, ray.normal.X, ray.normal.Y) * sqrt;
                VoxelLayer ret = null;
                Vector3? ray_touch = null;
                terrain.ForEachCellsRayStepPloar(ref terrain, ref pos, dir, len, (st, cell, cx, cy, current) =>
                {
                    if (cell != null)
                    {
                        for (int i = cell.LayerCount - 1; i >= 0; --i)
                        {
                            var layer = cell.GetLayer(i);
                            var geoBox = layer.GetBlockBoundingBox();
                            ray_touch = RayCast.RayBoundingBoxIntersection(ray, geoBox);
                            if (ray_touch != null)
                            {
                                ret = layer;
                                return true;
                            }
                        }
                    }
                    return false;
                }, false);
                if (ray_touch != null)
                {
                    touch = ray_touch.Value;
                    return ret;
                }
            }
            touch = Vector3.Zero;
            return null;
        }
    }
}
