using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Voxel.Data.PathFinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore.Voxel.Data
{
    //----------------------------------------------------------------------------------------------------------------------------------------

    public abstract class VoxelWorldManager : TerrainFactory
    {
        static VoxelWorldManager()
        {
            new SharedVoxelWorldManager();
        }
        new public static VoxelWorldManager Instance => TerrainFactory.Instance as VoxelWorldManager;
        public VoxelWorldManager()
        {
            //Instance = this;
        }
        public virtual IVoxelAstarMap LoadVoxelAstar(VoxelTerrain3D terrain, InputStream inputP)
        {
            if (inputP.TryPickFileHead(VoxelAstar.FILE_HEAD))
            {
                return new VoxelAstar(terrain, inputP);
            }
            else if (inputP.TryPickFileHead(SpaceAstar.FILE_HEAD))
            {
                return new SpaceAstar(terrain, inputP);
            }
            return null;
        }
        public virtual IVoxelAstarMap CreateVoxelAstar(VoxelTerrain3D terrain, IRangeValue progress = null)
        {
            switch (terrain.BuildConfig.AstarType)
            {
                case AstarType.Voxel:
                    return new VoxelAstar(terrain, progress);
                case AstarType.Space:
                    return new SpaceAstar(terrain, progress);
            }
            return new VoxelAstar(terrain);
        }
        public abstract void Clear();
    }
    public class SharedVoxelWorldManager : VoxelWorldManager
    {
        protected SemaphoreSlim request_lock = new SemaphoreSlim(1);
        protected HashMap<string, VoxelWorld> cache = new HashMap<string, VoxelWorld>();
        public SharedVoxelWorldManager()
        {
        }
        public virtual void CacheAll(IDictionary<string, VoxelWorld> caches)
        {
            request_lock.Wait();
            try
            {
                cache.AddAll(caches);
            }
            finally
            {
                request_lock.Release();
            }
        }
        public override void Clear()
        {
            request_lock.Wait();
            try
            {
                foreach (var v in cache)
                {
                    v.Value.Dispose();
                }
                cache.Clear();
            }
            finally
            {
                request_lock.Release();
            }
        }
        public override ITerrainWorld GetOrCreateVoxelWorld(string path, object data)
        {
            request_lock.Wait();
            try
            {
                var src = cache.GetOrAdd(path, (p) =>
                {
                    try
                    {
                        return VoxelWorld.LoadFromFile(path);
                    }
                    catch (Exception err)
                    {
                        throw new Exception("Error Load Voxel File : " + path + " : " + err.Message, err);
                    }
                });
                if (src != null)
                {
                    return new VoxelWorld(src);
                }
                return src;
            }
            finally
            {
                request_lock.Release();
            }
        }
        public override async Task<ITerrainWorld> GetOrCreateVoxelWorldAsync(string path, object data)
        {
            await request_lock.WaitAsync();
            try
            {
                VoxelWorld src = cache.Get(path);
                if (src != null)
                {
                    return new VoxelWorld(src);
                }
                {
                    src = await VoxelWorld.LoadFromFileAsync(path);
                    cache.Put(path, src);
                }
                if (src != null)
                {
                    return new VoxelWorld(src);
                }
                return src;
            }
            catch (Exception err)
            {
                throw new Exception("Error Load Voxel File : " + path + " : " + err.Message);
            }
            finally
            {
                request_lock.Release();
            }
        }
    }
    public class SingleVoxelWorldManager : VoxelWorldManager
    {
        protected SemaphoreSlim request_lock = new SemaphoreSlim(1);
        protected string lastCachePath = null;
        protected VoxelWorld cache;
        public SingleVoxelWorldManager()
        {
        }
        public override void Clear()
        {
            request_lock.Wait();
            try
            {
                cache?.Dispose();
                cache = null;
            }
            finally
            {
                request_lock.Release();
            }
        }
        public override ITerrainWorld GetOrCreateVoxelWorld(string path, object data)
        {
            request_lock.Wait();
            try
            {
                if (lastCachePath == path && cache != null)
                {
                    return new VoxelWorld(cache);
                }
                cache?.Dispose();
                try
                {
                    cache = VoxelWorld.LoadFromFile(path);
                    if (cache != null)
                    {
                        lastCachePath = path;
                        return new VoxelWorld(cache);
                    }
                }
                catch (Exception err)
                {
                    throw new Exception("Error Load Voxel File : " + path + " : " + err.Message, err);
                }
                return null;
            }
            finally
            {
                request_lock.Release();
            }
        }
        public override async Task<ITerrainWorld> GetOrCreateVoxelWorldAsync(string path, object data)
        {
            await request_lock.WaitAsync();
            try
            {
                if (lastCachePath == path && cache != null)
                {
                    return new VoxelWorld(cache);
                }
                cache?.Dispose();
                try
                {
                    cache = await VoxelWorld.LoadFromFileAsync(path);
                    if (cache != null)
                    {
                        lastCachePath = path;
                        return new VoxelWorld(cache);
                    }
                }
                catch (Exception err)
                {
                    throw new Exception("Error Load Voxel File : " + path + " : " + err.Message, err);
                }
                return null;
            }
            catch (Exception err)
            {
                throw new Exception("Error Load Voxel File : " + path + " : " + err.Message);
            }
            finally
            {
                request_lock.Release();
            }
        }
    }

}
