using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Voxel.Data.PathFinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore.Voxel.Data
{
    public class VoxelWorld : Disposable, ITerrainWorld
    {
        public static Logger log = new LazyLogger("VoxelWorld");
        public static string FILE_EXT = ".voxw";
        public static readonly byte[] FILE_HEAD = System.Text.Encoding.ASCII.GetBytes("VOXW");
        public string FileName { get; set; }
        public VoxelTerrain3D Terrain { get; private set; }
        public IVoxelAstarMap PathMap { get; private set; }
        public IVoxelAstar PathFinder { get; private set; }
        public HashMap<string, object> Attributes { get; private set; } = new HashMap<string, object>();
        public bool IsGZip { get => VoxelStream.IsGZip(ref Flags); private set => VoxelStream.SetGZip(ref Flags, value); }
        private BitSet32 Flags;

        public VoxelWorld(string fileName, VoxelTerrain3D terrain, IVoxelAstarMap pathMap)
        {
            this.FileName = System.IO.Path.GetFileName(fileName);
            this.Terrain = terrain;
            this.PathMap = pathMap;
            this.PathFinder = PathMap.CreatePathFinder();
            this.IsGZip = terrain.IsGZip;
        }
        public VoxelWorld(VoxelWorld src)
        {
            this.FileName = src.FileName;
            this.Terrain = src.Terrain;
            this.PathMap = src.PathMap;
            this.PathFinder = PathMap.CreatePathFinder();
            this.IsGZip = src.IsGZip;
        }
        protected override void Disposing()
        {
            Terrain?.Dispose();
            PathFinder?.Dispose();
        }
        public VoxelWorld(InputStream input)
        {
            var watch = Stopwatch.StartNew();
            if (!input.TryLoadFileHead(FILE_HEAD))
            {
                throw new Exception("Bad File Head");
            }
            this.FileName = input.GetUTF();
            this.Flags = input.GetStruct<BitSet32>();
            this.Attributes = input.GetXmlObject<HashMap<string, object>>();
            this.Terrain = new VoxelTerrain3D(input);
            log.Info($"Load Voxel Terrain Use : {FileName} : {watch.Elapsed}");
            watch.Restart();
            this.PathMap = VoxelWorldManager.Instance.LoadVoxelAstar(this.Terrain, input);
            watch.Stop();
            log.Info($"Load Voxel PathFinder Use : {FileName} : {watch.Elapsed}");
            this.PathFinder = PathMap.CreatePathFinder();
        }
        public void Save(OutputStream output)
        {
            var watch = Stopwatch.StartNew();
            output.SaveFileHead(FILE_HEAD);
            output.PutUTF(FileName);
            output.PutStruct(Flags);
            output.PutXmlObject(this.Attributes);
            this.Terrain.Save(output);
            this.PathMap.Save(output);
            watch.Stop();
            Console.WriteLine($"Save Voxel World Use : {FileName} : {watch.Elapsed}");
        }
        public bool TryGetAttributeAs<T>(string key, out T value)
        {
            if (Attributes.TryGetValue(key, out var val))
            {
                value = (T)val;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        public static VoxelWorld LoadFromBin(byte[] bin)
        {
            using (var mem = new DeepCore.IO.MemoryStream(bin))
            {
                return LoadFromStream(mem);
            }
        }
        public static VoxelWorld LoadFromStream(Stream stream)
        {
            var input = new InputStream(stream, null);
            return new VoxelWorld(input);
        }
        public static VoxelWorld LoadFromStream(InputStream stream)
        {
            return new VoxelWorld(stream);
        }
        public static VoxelWorld LoadFromFile(string voxFile)
        {
            var bin = Resource.LoadData(voxFile);
            return VoxelWorld.LoadFromBin(bin);
        }
        public static void SaveToBin(VoxelWorld world, out byte[] bin)
        {
            using (var mem = new DeepCore.IO.MemoryStream())
            {
                world.Save(new OutputStream(mem, null));
                bin = mem.ToArray();
            }
        }
        public static void SaveToFile(VoxelWorld world, string voxFilePath)
        {
            SaveToBin(world, out var bin);
            File.WriteAllBytes(voxFilePath, bin);
        }

        public static async Task<VoxelWorld> LoadFromFileAsync(string voxFile)
        {
            var bin = await Resource.LoadDataAsync(voxFile);
            return VoxelWorld.LoadFromBin(bin);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------
       
        //----------------------------------------------------------------------------------------------------------------------------------------
        #region ITerrainWorld
        ITerrain ITerrainWorld.Terrain => this.Terrain;
        ITerrainAstar ITerrainWorld.PathFinder => this.PathFinder;
        ITerrainAgent ITerrainWorld.CreateAgent()
        {
            return new VoxelObject(); 
        }
        ITerrainAgent ITerrainWorld.CreateAgent(Vector3 pos)
        {
            return new VoxelObject(pos);
        }
        ITerrainAgent ITerrainWorld.CreateAgent(ITerrainLayer pos)
        {
            return new VoxelObject(pos as VoxelLayer);
        }
//         ITerrainBlock ITerrainWorld.FindNearRandomMoveableNode(Random random, ITerrainAgent src, float radius)
//         {
//             return this.FindNearRandomMoveableNode(random,src as VoxelObject, radius);
//         }
//         ITerrainBlock ITerrainWorld.FindNearRandomMoveableNodeRect(Random random, ITerrainAgent src, float width, float height)
//         {
//             return this.FindNearRandomMoveableNodeRect(random, src as VoxelObject, width, height);
//         }
//         ITerrainBlock ITerrainWorld.FindNearRandomMoveableNode(Random random, ITerrainBlock src, float radius)
//         {
//             return this.FindNearRandomMoveableNode(random, src as VoxelLayer, radius);
//         }
//         ITerrainBlock ITerrainWorld.FindNearRandomMoveableNodeRect(Random random, ITerrainBlock src, float width, float height)
//         {
//             return this.FindNearRandomMoveableNodeRect(random, src as VoxelLayer, width, height);
//         }
//         ITerrainBlock ITerrainWorld.FindNearRandomMoveableNode(Random random, ref Vector3 src, float radius)
//         {
//             return this.FindNearRandomMoveableNode(random, ref src, radius);
//         }
//         ITerrainBlock ITerrainWorld.FindNearRandomMoveableNodeRect(Random random, ref Vector3 src, float width, float height)
//         {
//             return this.FindNearRandomMoveableNodeRect(random, ref src, width, height);
//         }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------
    }

}
