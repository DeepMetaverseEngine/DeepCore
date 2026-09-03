using DeepCore.Voxel.StreamingVoxel.Data;
using System;
using DeepCore.Threading;
using System.Threading.Tasks;
using DeepCore.Geometry;
using DeepCore.VoxelWorld.Message;

namespace DeepCore.VoxelWorld.VoxelClient
{
    public partial class SCVoxelWorld : Disposable
    {
        private UpdateActionTaskQueue executor;
        private WorldInfo worldInfo;
        private ISCVoxelAdapter adapter;
        public WorldInfo WorldInfo
        {
            get => worldInfo;
        }
        public SingleThreadCollectionPool ObjectPool
        {
            get => executor.ObjectPool;
        }
        public ISCVoxelAdapter Adapter
        {
            get=>adapter;
        }
        public int LastUpdateIntervalMS { get; private set; }
        public SCVoxelWorld(ISCVoxelAdapter adapter)
        {
            this.adapter = adapter;
            this.executor = new UpdateActionTaskQueue();
            this.worldInfo = adapter.WorldInfo;
            this.GridCellSize = worldInfo.GridCellSize;
            this.GridCellRadius = worldInfo.GridCellSize / 2f;
            this.ChunkSize = worldInfo.ChunkSize;
            this.InitListenObjects();
        }
        protected override void Disposing()
        {
            this.executor.Dispose();
            this.adapter.Dispose();
        }

        public void Update(int intervalMS)
        {
            LastUpdateIntervalMS = intervalMS;
            executor.Update();
            UpdateObjects();
        }
        //------------------------------------------------------------------------------------------------
        public Action<T> InvokeWrapAction<T>(Action<T> value)
        {
            return new Action<T>((r) => Invoke(r, value));
        }
        public void Invoke(Action action)
        {
            executor.QueueTask(action);
        }
        public void Invoke<T>(T st, Action<T> action)
        {
            executor.QueueTask(() => { action(st); });
        }
        //------------------------------------------------------------------------------------------------

        public float GridCellSize { get; private set; } = 1;
        public float GridCellRadius { get; private set; } = 0.5f;
        public Size3D ChunkSize { get; private set; } = new Size3D(256, 256, 256);


        /// <summary>
        /// 地图坐标转换格子坐标
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="location"></param>
        public void WorldPosToVoxel(in Vector3 pos, out Location3D loc)
        {
            loc.X = CMath.CycDiv(pos.X, GridCellSize);
            loc.Y = CMath.CycDiv(pos.Y, GridCellSize);
            loc.Z = CMath.CycDiv(pos.Z, GridCellSize);
        }
        public void WorldPosToChunkLocation(in Vector3 pos, out Location3D loc)
        {
            WorldPosToVoxel(in pos, out loc);
            loc = ChunkSize.AligningChunkLocation(loc);
        }
        public void WorldSizeToVoxel(float radius, out int size)
        {
            size = CMath.CycDiv(radius, GridCellSize);
        }
        /// <summary>
        /// 检测盒子是否包含坐标
        /// </summary>
        /// <param name="box"></param>
        /// <param name="chunkLocation"></param>
        /// <returns></returns>
        public bool TestBoxContainsChunkLocation(in BoundingBox box, in Location3D chunkLocation)
        {
            Vector3 obj_pos = chunkLocation;
            Vector3 obj_size = ChunkSize;
            var obx2 = new BoundingBox(obj_pos, obj_pos + obj_size);
            if (box.Intersects(in obx2))
            {
                return true;
            }
            return false;
        }
        
        //------------------------------------------------------------------------------------------------
    }
}
