using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Voxel.StreamingVoxel.Data;
using System;
using static DeepCore.Voxel.Extensions.MagicaVoxel.MagicaVoxelFile;

namespace DeepCore.VoxelWorld.Message
{

    public class WorldInfo : IExternalizable
    {
        public Size3D TotalSize = new Size3D(128, 128, 128);
        public Size3D ChunkSize = new Size3D(128, 128, 128);
        public float GridCellSize = 1f;
        public readonly Properties Properties = new Properties();
        public void ReadExternal(IInputStream input)
        {
            TotalSize = input.GetStruct<Size3D>();
            ChunkSize = input.GetStruct<Size3D>();
            GridCellSize = input.GetF32();
            Properties.ReadExternal(input);
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutStruct(TotalSize);
            output.PutStruct(ChunkSize);
            output.PutF32(GridCellSize);
            Properties.WriteExternal(output);
        }
    }

    public class PlayerInfo : IExternalizable
    {
        public string PlayerName;
        public string PlayerToken;
        public void ReadExternal(IInputStream input)
        {
            PlayerName = input.GetUTF();
            PlayerToken = input.GetUTF();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(PlayerName);
            output.PutUTF(PlayerToken);
        }
    }
}
