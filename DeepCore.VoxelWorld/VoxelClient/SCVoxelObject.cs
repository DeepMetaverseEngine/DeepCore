using DeepCore.Voxel.StreamingVoxel.Data;
using DeepCore.VoxelWorld.Message;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.VoxelWorld.VoxelClient
{
    public class SCVoxelObject : Disposable
    {
        public SCVoxelWorld World { get; }
        public ObjectEnter Enter { get; }
        public string OID { get => Enter.OID; }
        public StreamingChunk Chunk { get; private set; }
        public SCVoxelObject(SCVoxelWorld world, ObjectEnter enter)
        {
            this.World = world;
            this.Enter = enter;
            this.World.Adapter.FetchChunkByUUIDAsync(enter.ChunkUUID).ContinueWith(t => OnChunkFetched(t.Result.chunk));
        }
        protected void OnChunkFetched(StreamingChunk chunk)
        {
            this.Chunk = chunk;
            event_OnChunkLoaded?.Invoke(this, chunk);
        }
        protected override void Disposing()
        {
            event_OnChunkLoaded = null;
        }
        public void Update() { }

        public delegate void ChunkLoaded(SCVoxelObject obj, StreamingChunk chunk);
        public event ChunkLoaded OnChunkLoaded { add { event_OnChunkLoaded += value; } remove { event_OnChunkLoaded -= value; } }
        private ChunkLoaded event_OnChunkLoaded;
    }
}
