using DeepCore.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using static DeepCore.Voxel.Extensions.MagicaVoxel.MagicaVoxelFile;

namespace DeepCore.Voxel.StreamingVoxel.Data
{
    public static class StreamingConverter
    {
        public struct AddMeshVertexQuard
        {
            public Vector3[] vertex;
            public Vector3 normal;
            public Vector4 color;
            public int colorTemplate;
            public byte[] triangles;
            public static AddMeshVertexQuard Quard
            {
                get
                {
                    return new AddMeshVertexQuard()
                    {
                        vertex = new Vector3[4],
                        normal = Vector3.Zero,
                        color = Vector4.Zero,
                        colorTemplate = 0,
                        triangles = new byte[6],
                    };
                }
            }
        }
        private static byte[] triangle_clock = new byte[] { 0, 1, 2, 0, 2, 3 };
        private static byte[] triangle_rclock = new byte[] { 3, 2, 0, 2, 1, 0 };

        public delegate void AddMeshVertex(in AddMeshVertexQuard add);
        public static void ConvertToMesh(this StreamingChunk chunk, AddMeshVertex add_plane)
        {
            var grid = new StreamingCube[chunk.ChunkSize.X + 1, chunk.ChunkSize.Y + 1, chunk.ChunkSize.Z + 1];
            foreach (var c in chunk.Cubes)
            {
                grid[c.X, c.Y, c.Z] = c;
            }
            var quard = AddMeshVertexQuard.Quard;
            foreach (var c in chunk.Cubes)
            {
                int x = c.X;
                int y = c.Y;
                int z = c.Z;
                var dcolor = chunk.Templates[c.CubeTemplateID];
                DeepCore.Colors.DecodeRGBA(dcolor.ColorRGBA, out float r, out float g, out float b, out float a);
                var color = new Vector4(r, g, b, a);
                if (z == 255 || grid[x, y, z + 1] == null)
                {
                    // we need up face
                    quard.vertex[0] = new Vector3(x + 0, z + 1, y + 0);
                    quard.vertex[1] = new Vector3(x + 0, z + 1, y + 1);
                    quard.vertex[2] = new Vector3(x + 1, z + 1, y + 1);
                    quard.vertex[3] = new Vector3(x + 1, z + 1, y + 0);
                    quard.normal = Vector3.Up;
                    quard.color = color;
                    quard.colorTemplate = c.CubeTemplateID;
                    quard.triangles = triangle_clock;
                    add_plane(in quard);
                }
                if (z == 0 || grid[x, y, z - 1] == null)
                {
                    // we need down face
                    quard.vertex[0] = new Vector3(x + 0, z + 0, y + 0);
                    quard.vertex[1] = new Vector3(x + 0, z + 0, y + 1);
                    quard.vertex[2] = new Vector3(x + 1, z + 0, y + 1);
                    quard.vertex[3] = new Vector3(x + 1, z + 0, y + 0);
                    quard.normal = Vector3.Down;
                    quard.color = color;
                    quard.colorTemplate = c.CubeTemplateID;
                    quard.triangles = triangle_rclock;
                    add_plane(in quard);
                }
                if (x == 0 || grid[x - 1, y, z] == null)
                {
                    // we need left face
                    quard.vertex[0] = new Vector3(x + 0, z + 0, y + 0);
                    quard.vertex[1] = new Vector3(x + 0, z + 0, y + 1);
                    quard.vertex[2] = new Vector3(x + 0, z + 1, y + 1);
                    quard.vertex[3] = new Vector3(x + 0, z + 1, y + 0);
                    quard.normal = Vector3.Left;
                    quard.color = color;
                    quard.colorTemplate = c.CubeTemplateID;
                    quard.triangles = triangle_clock;
                    add_plane(in quard);
                }
                if (x == 255 || grid[x + 1, y, z] == null)
                {
                    // we need right face
                    quard.vertex[0] = new Vector3(x + 1, z + 0, y + 0);
                    quard.vertex[1] = new Vector3(x + 1, z + 0, y + 1);
                    quard.vertex[2] = new Vector3(x + 1, z + 1, y + 1);
                    quard.vertex[3] = new Vector3(x + 1, z + 1, y + 0);
                    quard.normal = Vector3.Right;
                    quard.color = color;
                    quard.colorTemplate = c.CubeTemplateID;
                    quard.triangles = triangle_rclock;
                    add_plane(in quard);
                }
                if (y == 0 || grid[x, y - 1, z] == null)
                {
                    // we need back face
                    quard.vertex[0] = new Vector3(x + 0, z + 0, y + 0);
                    quard.vertex[1] = new Vector3(x + 0, z + 1, y + 0);
                    quard.vertex[2] = new Vector3(x + 1, z + 1, y + 0);
                    quard.vertex[3] = new Vector3(x + 1, z + 0, y + 0);
                    quard.normal = Vector3.Forward;
                    quard.color = color;
                    quard.colorTemplate = c.CubeTemplateID;
                    quard.triangles = triangle_clock;
                    add_plane(in quard);
                }
                if (y == 255 || grid[x, y + 1, z] == null)
                {
                    // we need forward face
                    quard.vertex[0] = new Vector3(x + 0, z + 0, y + 1);
                    quard.vertex[1] = new Vector3(x + 0, z + 1, y + 1);
                    quard.vertex[2] = new Vector3(x + 1, z + 1, y + 1);
                    quard.vertex[3] = new Vector3(x + 1, z + 0, y + 1);
                    quard.normal = Vector3.Backward;
                    quard.color = color;
                    quard.colorTemplate = c.CubeTemplateID;
                    quard.triangles = triangle_rclock;
                    add_plane(in quard);
                }
            }
        }
        public static StreamingMesh ConvertToMesh(this StreamingChunk chunk)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector4> colors = new List<Vector4>();
            List<int> colorsID = new List<int>();
            List<Vector2> uv = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();
            var d = 0;
            ConvertToMesh(chunk, add_plane);
            void add_plane(in AddMeshVertexQuard quard)
            {
                d = vertices.Count;
                vertices.Add(quard.vertex[0] * chunk.GridCellSize);
                vertices.Add(quard.vertex[1] * chunk.GridCellSize);
                vertices.Add(quard.vertex[2] * chunk.GridCellSize);
                vertices.Add(quard.vertex[3] * chunk.GridCellSize);
                normals.Add(quard.normal);
                normals.Add(quard.normal);
                normals.Add(quard.normal);
                normals.Add(quard.normal);
                uv.Add(new Vector2(0.0f, 0.0f));
                uv.Add(new Vector2(1.0f, 0.0f));
                uv.Add(new Vector2(1.0f, 1.0f));
                uv.Add(new Vector2(0.0f, 1.0f));
                colors.Add(quard.color);
                colors.Add(quard.color);
                colors.Add(quard.color);
                colors.Add(quard.color);
                colorsID.Add(quard.colorTemplate);
                colorsID.Add(quard.colorTemplate);
                colorsID.Add(quard.colorTemplate);
                colorsID.Add(quard.colorTemplate);
                for (int i = 0; i < quard.triangles.Length; i++)
                {
                    triangles.Add(d + quard.triangles[i]);
                }
            }
            return new StreamingMesh()
            {
                UUID = chunk.UUID,
                vertices = vertices,
                colors = colors,
                uv = uv,
                normals = normals,
                triangles = triangles,
                colorsID = colorsID,
            };
        }

        //------------------------------------------------------------------------------------------------------------------------
        public static StreamingChunk BakeChunkLOD(this StreamingChunk chunk, int lod_num)
        {
            if (lod_num == 0) return chunk;
            var lod = 1;
            for (int i = 0; i < lod_num; i++) { lod *= 2; }
            var srcGrid = new StreamingCube[
                chunk.ChunkSize.X,
                chunk.ChunkSize.Y,
                chunk.ChunkSize.Z];
            foreach (var c in chunk.Cubes)
            {
                srcGrid[c.X, c.Y, c.Z] = c;
            }
            var newChunk = new StreamingChunk();
            newChunk.UUID = $"{chunk.UUID}_LOD{lod_num}";
            newChunk.GridCellSize = chunk.GridCellSize * lod;
            newChunk.ChunkSize = new Size3D(
                CMath.RoundMod(chunk.ChunkSize.X, lod),
                CMath.RoundMod(chunk.ChunkSize.Y, lod),
                CMath.RoundMod(chunk.ChunkSize.Z, lod));
            newChunk.AnchorPoint = chunk.AnchorPoint;
            newChunk.Templates = chunk.Templates;
            var newCubes = new List<StreamingCube>();
            {
                srcGrid.ForEachArray3D(0, 0, 0,
                    chunk.ChunkSize.X,
                    chunk.ChunkSize.Y,
                    chunk.ChunkSize.Z,
                    lod, lod, lod, (src, sx, sy, sz) =>
                    {
                        var cube = CombineCubeLOD(srcGrid, sx, sy, sz, lod);
                        if (cube != null)
                        {
                            newCubes.Add(cube);
                        }
                    });
            }
            newChunk.Cubes = newCubes.ToArray();
            // LOD has no touches
            //newChunk.InitTouchGrids();
            return newChunk;
        }
        public static StreamingCube CombineCubeLOD(StreamingCube[,,] srcGrid, int sx, int sy, int sz, int lod)
        {
            var scount = 0;
            var scolor = 0;
            srcGrid.ForEachArray3D(
                sx, sy, sz,
                Math.Min(sx + lod, srcGrid.GetLength(0)),
                Math.Min(sy + lod, srcGrid.GetLength(1)),
                Math.Min(sz + lod, srcGrid.GetLength(2)),
                1, 1, 1, (c, x, y, z) =>
                {
                    if (c != null)
                    {
                        scolor = c.CubeTemplateID;
                        scount++;
                    }
                });
            if (sz == 0 || scount >= (lod * lod * lod) / 2)
            {
                return new StreamingCube()
                {
                    X = (byte)(sx / lod),
                    Y = (byte)(sy / lod),
                    Z = (byte)(sz / lod),
                    CubeTemplateID = scolor,
                };
            }
            return null;
        }

        //------------------------------------------------------------------------------------------------------------------------
        struct CombineChunk
        {
            public Location3D StartLocation;
            public StreamingChunk Chunk;
        }
        public static StreamingChunk CombineChunkLOD(StreamingChunk[,,] chunks, Size3D newChunkSize, string newUUID)
        {
            var xlen = chunks.GetLength(0);
            var ylen = chunks.GetLength(1);
            var zlen = chunks.GetLength(2);
            var newXLen = newChunkSize.X;
            var newYLen = newChunkSize.Y;
            var newZLen = newChunkSize.Z;
            if (newXLen > 256 || newYLen > 256 || newZLen > 256)
            {
                throw new Exception($"New Chunk out of 256 size {newXLen} {newYLen} {newZLen}");
            }
            var grids = new CombineChunk[xlen, ylen, zlen];
            grids.InitArray3D((x, y, z) =>
            {
                var cell = new CombineChunk() { Chunk = chunks[x, y, z] };
                if (x == 0) cell.StartLocation.X = 0;
                else cell.StartLocation.X = grids[x - 1, y, z].StartLocation.X + grids[x - 1, y, z].Chunk.ChunkSize.X;
                if (y == 0) cell.StartLocation.Y = 0;
                else cell.StartLocation.Y = grids[x, y - 1, z].StartLocation.Y + grids[x, y - 1, z].Chunk.ChunkSize.Y;
                if (z == 0) cell.StartLocation.Z = 0;
                else cell.StartLocation.Z = grids[x, y, z - 1].StartLocation.Z + grids[x, y, z - 1].Chunk.ChunkSize.Z;
                return cell;
            });
            var cbase = chunks[0, 0, 0];
            var newChunk = new StreamingChunk();
            newChunk.UUID = newUUID;
            newChunk.Templates = cbase.Templates;
            newChunk.GridCellSize = cbase.GridCellSize;
            newChunk.ChunkSize = new Size3D(newXLen, newYLen, newZLen);
            newChunk.AnchorPoint = Vector3.Zero;
            var newCubes = new List<StreamingCube>();
            grids.ForEachArray3D((c, x, y, z) =>
            {
                int sx = c.StartLocation.X;
                int sy = c.StartLocation.Y;
                int sz = c.StartLocation.Z;
                foreach (var cube in c.Chunk.Cubes)
                {
                    newCubes.Add(new StreamingCube()
                    {
                        CubeTemplateID = cube.CubeTemplateID,
                        X = (byte)(sx + cube.X),
                        Y = (byte)(sy + cube.Y),
                        Z = (byte)(sz + cube.Z),
                    });
                }
            });
            newChunk.Cubes = newCubes.ToArray();
            // LOD has no touches
            //newChunk.InitTouchGrids();
            return newChunk;
        }
    }
}
