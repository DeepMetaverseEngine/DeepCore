using DeepCore;
using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.GUI.Cell;
using DeepCore.IO;
using DeepCore.Voxel.Data;
using DeepCore.Voxel.Data.PathFinder;
using DeepCore.Voxel.Extensions.MagicaVoxel;
using DeepCore.Voxel.StreamingVoxel.Data;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DeepTools.Voxel
{
    public static class VoxelConverter
    {
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Terrain XML

        public class WorldInfo
        {
            public Size3D TotalSize = new Size3D(128, 128, 128);
            public Size3D ChunkSize = new Size3D(128, 128, 128);
            public float GridCellSize = 1f;
            public Properties Properties = new Properties();
        }
        public static WorldInfo ConvertTerrainDataToStreamingVoxChunks(VoxelTerrainData xml, int slice, DirectoryInfo outDir, IRangeValue progress = null)
        {
            if (slice > 256) throw new Exception("Slice can not great than 256 : " + slice);
            CFiles.CreateDir(outDir);
            var chunkSize = new Size3D(slice, slice, slice);
            var pallet = new List<uint>();
            var gridCell = xml.GridSize;
            var minZ = 0;
            var maxZ = 0;
            xml.Grids.ForEachArray2D(chunkSize, (st, cell, x, y) =>
            {
                if (cell != null && cell.Length > 0)
                {
                    foreach (var layer in cell)
                    {
                        minZ = Math.Min(minZ, (int)(layer.Downward / gridCell));
                        maxZ = Math.Max(maxZ, (int)(layer.Upward / gridCell));
                        if (!pallet.Contains(layer.Color))
                        {
                            pallet.Add(layer.Color);
                        }
                    }
                }
            });
            var templates = pallet.ConvertAll(argb =>
            {
                var ret = new StreamingCubeTemplate();
                ret.ColorRGBA = Colors.ARGB2RGBA(argb);
                return ret;
            });

            // save world
            var totalSize = new Size3D(xml.XCount, xml.YCount, maxZ - minZ + 1);
            var world = new WorldInfo()
            {
                ChunkSize = chunkSize,
                TotalSize = totalSize,
                GridCellSize = gridCell,
            };
            File.WriteAllText(outDir.FullName + "/world.xml", XmlUtil.ObjectToXmlString(world));

            totalSize.FoeEachSlice(slice, sloc =>
            {
                Console.WriteLine("Bake " + sloc);
                var targetMap = new HashMap<Location3D, List<StreamingCube>>();
                for (int x = sloc.X; x < sloc.X + slice && x < xml.XCount; x++)
                {
                    for (int y = sloc.Y; y < sloc.Y + slice && y < xml.YCount; y++)
                    {
                        var cell = xml.Grids[x, y];
                        if (cell != null && cell.Length > 0)
                        {
                            var cx = x / slice;
                            var cy = y / slice;
                            Array.Sort(cell, (a, b) => CMath.GetDirect(a.Downward - b.Downward));
                            foreach (var layer in cell)
                            {
                                for (float fz = layer.Downward; fz < layer.Upward; fz += gridCell)
                                {
                                    var z = (int)(fz / gridCell) - minZ;
                                    var cz = z / slice;
                                    var tloc = new Location3D(cx, cy, cz);
                                    var cubes = targetMap.GetOrAdd(tloc, loc => new List<StreamingCube>());
                                    var cube = new StreamingCube()
                                    {
                                        X = (byte)(x % slice),
                                        Y = (byte)(y % slice),
                                        Z = (byte)(z % slice),
                                        CubeTemplateID = pallet.IndexOf(layer.Color),
                                    };
                                    cubes.Add(cube);
                                }
                            }
                        }
                    }
                }
                foreach (var ck in targetMap)
                {
                    var loc = ck.Key;
                    var cubes = ck.Value;
                    var chunk = new StreamingChunk()
                    {
                        ChunkSize = chunkSize,
                        Cubes = cubes.ToArray(),
                        GridCellSize = gridCell,
                        Templates = templates.ToArray(),
                        UUID = loc.ToString(),
                    };
                    chunk.InitTouchGrids();
                    var file = new StreamingVoxChunkFile()
                    {
                        Chunk = chunk,
                    };
                    StreamingVoxChunkFile.Save(file, new FileInfo($"{outDir.FullName}/{loc.X * slice}_{loc.Y * slice}_{loc.Z * slice}.{StreamingVoxChunkFile.FILE_EXT}"));
                }
            });
            return world;
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region CPJ

        public static VoxelTerrainData ConvertCPJMapToTerrainData(MapSet map, VoxelBuildConfig prop = null, IRangeValue progress = null)
        {
            var gridSize = 1f;
            progress?.SetMax(map.XCount * map.YCount);
            progress?.SetMin(0);
            progress?.SetValue(0);
            var grids = new List<VoxelNodeData>[map.XCount, map.YCount];
            for (int x = 0; x < map.XCount; ++x)
            {
                for (int y = 0; y < map.YCount; ++y)
                {
                    var grid = grids[x, y] = new List<VoxelNodeData>(1);
                    var cell = map.GetMapCell(0, x, y);
                    if (cell.TerrainFlag == 0)
                    {
                        grid.Add(new VoxelNodeData()
                        {
                            Color = 0xFF00FF00,
                            Upward = 0,
                            Downward = -1,
                        });
                    }
                }
            }
            var ret = new VoxelTerrainData();
            ret.GridSize = gridSize;
            ret.Grids = grids.Convert2D((i, j, v) => v.ToArray());
            return ret;
        }

        public static VoxelWorld ConvertCPJMapToVoxelWorld(MapSet map, VoxelBuildConfig prop = null, IRangeValue progress = null)
        {
            prop = prop ?? new VoxelBuildConfig()
            {
                FlipY = false,
            };
            var data = ConvertCPJMapToTerrainData(map, prop, progress);
            var terrain = new VoxelTerrain3D(data, prop, progress);
            var astar = VoxelWorldManager.Instance.CreateVoxelAstar(terrain, progress);
            var retWorld = new VoxelWorld(map.Name, terrain, astar);
            //             var voxelFileName = Path.GetFullPath(vf.Directory.FullName + Path.DirectorySeparatorChar + vf.Name + VoxelWorld.FILE_EXT);
            //             VoxelWorld.SaveToFile(retWorld, voxelFileName);
            // 
            //             return Instance.GetSubVoxelFileName(voxelFileName);
            return retWorld;
        }

        public static void SaveCPJMapToVoxel(CPJFileSet cpj, DirectoryInfo dir, VoxelBuildConfig prop = null)
        {
            foreach (var map in cpj.MapTable)
            {
                var wd = ConvertCPJMapToVoxelWorld(map.Value, prop);
                var voxelFileName = Path.GetFullPath(dir.FullName + Path.DirectorySeparatorChar + map.Value.ImagesName + "_" + map.Key + VoxelWorld.FILE_EXT);
                VoxelWorld.SaveToFile(wd, voxelFileName);
                Console.Write(string.Format("Save Map To Vox: {0} -> {1}", map.Key, voxelFileName));
            }
        }

        #endregion


        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region MagicaVoxelFile
        public static VoxelTerrainData ConvertMagicaVoxelFileToVoxelTerrainData(this MagicaVoxelFile data, IRangeValue progress = null)
        {
            var gridSize = 1f;
            progress?.SetMax(data.TotalVoxelCount);
            progress?.SetMin(0);
            progress?.SetValue(0);
            data.GetTrimAABB(out int minX, out int minY, out int minZ, out int maxX, out int maxY, out int maxZ);
            var xlen = maxX - minX + 1;
            var ylen = maxY - minY + 1;
            var zlen = maxZ - minZ + 1;
            var grids = new List<VoxelNodeData>[xlen, ylen];
            for (int x = 0; x < xlen; ++x)
            {
                for (int y = 0; y < ylen; ++y)
                {
                    grids[x, y] = new List<VoxelNodeData>(1);
                }
            }
            data.ForEachVoxels(v =>
            {
                var c = data.Main.Palette.GetColor(v.ColorIndex);
                var vX = v.X - minX;
                var vY = v.Y - minY;
                var vZ = v.Z - minZ;
                grids[vX, vY].Add(new VoxelNodeData()
                {
                    Upward = vZ * gridSize + gridSize,
                    Downward = vZ * gridSize,
                    Color = c.ARGB,
                });
                progress?.Add(1);
            });
            var ret = new VoxelTerrainData();
            ret.GridSize = gridSize;
            ret.Grids = grids.Convert2D((i, j, v) => v.ToArray());
            return ret;
        }
        public static VoxelWorld ConvertMagicaVoxelFileToVoxelWorld(this MagicaVoxelFile vox, VoxelBuildConfig prop = null, IRangeValue progress = null)
        {
            var data = vox.ConvertMagicaVoxelFileToVoxelTerrainData(progress);
            if (prop == null)
            {
                prop = new VoxelBuildConfig()
                {
                    ClipHorizon = VoxelClipHorizon.NA,
                    FlipY = true,
                    IsGZip = true,
                };
            }
            var terrain = new VoxelTerrain3D(data, prop);
            var astar = VoxelWorldManager.Instance.CreateVoxelAstar(terrain);
            var world = new VoxelWorld("VOX ", terrain, astar);
            return world;
        }
        public static VoxelWorld ConvertMagicaVoxelFileToVoxelWorld(FileInfo input, FileInfo output, out MagicaVoxelFile vox, VoxelBuildConfig prop = null, IRangeValue progress = null)
        {
            vox = MagicaVoxelFile.Load(input);
            var world = ConvertMagicaVoxelFileToVoxelWorld(vox);
            world.FileName = input.FullName;
            using (var os = output.OpenOutputStream())
            {
                world.Save(os);
            }
            return world;
        }

        public static StreamingChunk[,,] ConvertMagicaVoxelToStreamingVoxChunks(MagicaVoxelFile vox, float gridSize, int slice, IRangeValue progress = null)
        {
            if (slice > 256) throw new Exception("Slice can not great than 256 : " + slice);
            vox.GetTrimAABB(out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
            var xlen = maxX - minX + 1;
            var ylen = maxY - minY + 1;
            var zlen = maxZ - minZ + 1;
            var xsize = CMath.RoundMod(xlen, slice);
            var ysize = CMath.RoundMod(ylen, slice);
            var zsize = CMath.RoundMod(zlen, slice);
            var templates = new List<StreamingCubeTemplate>();
            progress?.SetRange(0, vox.TotalVoxelCount, 0);
            foreach (var color in vox.Main.Palette.Palette)
            {
                templates.Add(new StreamingCubeTemplate()
                {
                    ColorRGBA = color.RGBA,
                });
            }
            var cubes = new List<StreamingCube>[xsize, ysize, zsize];
            var matrix3D = new StreamingChunk[xsize, ysize, zsize];
            matrix3D.ForEachArray3D(vox, (st, v, x, y, z) =>
            {
                var chunk = matrix3D[x, y, z] = new StreamingChunk();
                chunk.GridCellSize = gridSize;
                chunk.ChunkSize = new Size3D(slice, slice, slice);
                chunk.Templates = templates.ToArray();
                cubes[x, y, z] = new List<StreamingCube>();
            });
            vox.ForEachVoxels((vc) =>
            {
                var sx = vc.X - minX;
                var sy = vc.Y - minY;
                var sz = vc.Z - minZ;
                var cx = sx / slice;
                var cy = sy / slice;
                var cz = sz / slice;
                var vx = CMath.CycNum(sx, slice);
                var vy = CMath.CycNum(sy, slice);
                var vz = CMath.CycNum(sz, slice);
                var chunk = matrix3D[cx, cy, cz];
                var cube = new StreamingCube();
                {
                    cube.X = (byte)(vx);
                    cube.Y = (byte)(ylen - 1 - vy);
                    cube.Z = (byte)(vz);
                    cube.CubeTemplateID = vc.ColorIndex;
                }
                cubes[cx, cy, cz].Add(cube);
                progress?.Add(1);
            });
            matrix3D.ForEachArray3D(vox, (st, v, x, y, z) =>
            {
                matrix3D[x, y, z].Cubes = cubes[x, y, z].ToArray();
                matrix3D[x, y, z].InitTouchGrids();
            });
            return matrix3D;
        }
        public static StreamingVoxChunkFile[,,] ConvertMagicaVoxelToStreamingVoxChunksFile(FileInfo voxFile, DirectoryInfo outputDir, float gridSize, int slice, IRangeValue progress = null)
        {
            var vox = MagicaVoxelFile.Load(voxFile);
            if (vox != null)
            {
                CFiles.CreateDir(outputDir);
                var matrix3D = ConvertMagicaVoxelToStreamingVoxChunks(vox, gridSize, slice, progress);
                var ret = new StreamingVoxChunkFile[
                    matrix3D.GetLength(0),
                    matrix3D.GetLength(1),
                    matrix3D.GetLength(2)];
                var smd5 = CMD5.CalculateMD5(voxFile);
                matrix3D.ForEachArray3D(vox, (st, v, x, y, z) =>
                {
                    var scvx = matrix3D[x, y, z];
                    scvx.UUID = CMD5.CalculateMD5($"{smd5}_{x}_{y}_{z}");
                    var sfile = ret[x, y, z] = new StreamingVoxChunkFile(scvx);
                    var outFile = new FileInfo(outputDir.FullName + Path.DirectorySeparatorChar + voxFile.Name + $"_{x}_{y}_{z}" + StreamingVoxChunkFile.FILE_EXT);
                    StreamingVoxChunkFile.Save(sfile, outFile);
                });
                return ret;
            }
            return null;
        }

        public static StreamingChunk ConvertMagicaVoxelToStreamingChunk(MagicaVoxelFile vox, float gridSize, IRangeValue progress = null)
        {
            vox.GetTrimAABB(out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
            var xlen = maxX - minX + 1;
            var ylen = maxY - minY + 1;
            var zlen = maxZ - minZ + 1;
            var chunk = new StreamingChunk();
            var cubes = new List<StreamingCube>();
            var templates = new List<StreamingCubeTemplate>();
            progress?.SetRange(0, vox.TotalVoxelCount, 0);
            foreach (var color in vox.Main.Palette.Palette)
            {
                templates.Add(new StreamingCubeTemplate()
                {
                    ColorRGBA = color.RGBA,
                });
            }
            vox.ForEachVoxels((vc) =>
            {
                var sx = vc.X - minX;
                var sy = vc.Y - minY;
                var sz = vc.Z - minZ;
                var cube = new StreamingCube();
                cube.X = (byte)(sx);
                cube.Y = (byte)(ylen - 1 - sy);
                cube.Z = (byte)(sz);
                cube.CubeTemplateID = vc.ColorIndex;
                cubes.Add(cube);
                progress?.Add(1);
            });
            chunk.GridCellSize = gridSize;
            chunk.ChunkSize = new Size3D(xlen, ylen, zlen);
            chunk.AnchorPoint = new Vector3(-minX * gridSize, -minY * gridSize, -minZ * gridSize);
            chunk.Cubes = cubes.ToArray();
            chunk.Templates = templates.ToArray();
            chunk.InitTouchGrids();
            return chunk;
        }
        public static StreamingVoxChunkFile ConvertMagicaVoxelToStreamingVoxChunkFile(FileInfo voxFile, FileInfo outFile, float gridSize, out MagicaVoxelFile vox, IRangeValue progress = null)
        {
            vox = MagicaVoxelFile.Load(voxFile);
            if (vox != null)
            {
                var scvx = ConvertMagicaVoxelToStreamingChunk(vox, gridSize, progress);
                scvx.UUID = CMD5.CalculateMD5(voxFile);
                var sfile = new StreamingVoxChunkFile(scvx);
                StreamingVoxChunkFile.Save(sfile, outFile);
                return sfile;
            }
            return null;
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region SCVX

        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
