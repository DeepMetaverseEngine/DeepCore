using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Voxel.StreamingVoxel.Data;
using System;

namespace DeepCore.VoxelWorld.Message
{

    public class FetchMapChunkResponse : Response
    {
        public string ChunkUUID;
        public Size3D ChunkSize;
        public float ChunkGridCellSize;
        public int LOD;
        public StreamingChunk Chunk;
    }
    public class MapCubeChanged : Notify
    {
        public Location3D WorldLocation;
        public StreamingCube Cube;
    }
    public class PlayerChangeMapCube : Notify
    {
    }
    public class ObjectEnter : Notify
    {
        public string OID;
        public string ChunkUUID;
        public Size3D ChunkSize;
        public float ChunkGridCellSize;
        public Vector3 Position;
        public Vector3 Anchor;
    }
    public class ObjectLeave : Notify
    {
        public string OID;
    }

    public class PlayerAddObjectToWorld : Notify
    {
        public PlayerInfo Player;
        public Vector3 Position;
        public string ChunkUUID;
        public StreamingChunk Chunk;
    }

    public class ActorEnter : Notify
    {
    }
    public class ActorLeave : Notify
    {
    }
    public class FetchActorInfoResponse : Response
    {
    }
    public class ActorStatusChange : Notify
    {
    }
    public class PlayerUpdateActorStatus : Notify
    {
    }
    public class PlayerEnterWorld : Notify
    {
    }
    public class PlayerLeaveWorld : Notify
    {
    }


    public class FetchTotalChunk : Response
    {
        public int ChunkCount;
    }
    public class FetchChunkList : Response
    {
        public string[] ChunksUUID;
    }

    public class FetchChunkByUUIDResponse : Response
    {
        public StreamingChunk chunk;
    }
    public class PostResponse : Response
    {
        
    }
}
