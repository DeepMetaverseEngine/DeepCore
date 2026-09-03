using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.Geometry.Terrain
{
    [Reflectible]
    public abstract class TerrainFactory
    {
        public static TerrainFactory Instance { get; private set; }
        public TerrainFactory()
        {
            Instance = this;
        }
        public abstract ITerrainWorld GetOrCreateVoxelWorld(string path, object data);
        public abstract Task<ITerrainWorld> GetOrCreateVoxelWorldAsync(string path, object data);
    }
}
