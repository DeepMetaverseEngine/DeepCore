using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static DeepCore.Voxel.Extensions.MagicaVoxel.MagicaVoxelFile;

namespace DeepCore.Voxel.StreamingVoxel.Data
{
    public class Codec : MessageFactoryGenerator
    {
        public const int SCVX_MSGID = 0xFC0000;
        public static Codec Instance { get; } = new Codec();
        public Codec() : base("")
        {
            RegistExternalizable(typeof(StreamingChunk));
            RegistExternalizable(typeof(StreamingCube));
            RegistExternalizable(typeof(StreamingCubeTemplate));
            RegistExternalizable(typeof(StreamingTouchLayer));
            RegistExternalizable(typeof(StreamingMesh));
        }
    }

    /// <summary>
    /// 运行时体素组信息
    /// </summary>
    [MessageType(Codec.SCVX_MSGID + 1)]
    public class StreamingChunk : IExternalizable
    {
        public string UUID = string.Empty;
        public float GridCellSize = 1;
        public Size3D ChunkSize;
        public Vector3 AnchorPoint;
        public StreamingCubeTemplate[] Templates;
        public StreamingCube[] Cubes;
        public StreamingTouchLayer[,][] TouchGrids { get; private set; }
        public readonly Properties Properties = new Properties();
        public StreamingChunk() { }
        public void ReadExternal(IInputStream input)
        {
            UUID = input.GetUTF();
            GridCellSize = input.GetF32();
            ChunkSize = input.GetStruct<Size3D>();
            AnchorPoint = input.GetStruct<Vector3>();
            Templates = input.GetExtArrayNoHead<StreamingCubeTemplate>();
            Cubes = input.GetExtArrayNoHead<StreamingCube>();
            TouchGrids = input.TryGetObj( (input) =>
            {
                var grid = new StreamingTouchLayer[ChunkSize.X, ChunkSize.Y][];
                grid.InitArray2D((x, y) => input.GetExtArrayNoHead<StreamingTouchLayer>());
                return grid;
            });
            Properties.ReadExternal(input);
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(UUID);
            output.PutF32(GridCellSize);
            output.PutStruct(ChunkSize);
            output.PutStruct(AnchorPoint);
            output.PutExtArrayNoHead(Templates);
            output.PutExtArrayNoHead(Cubes);
            output.TryPutObj(TouchGrids,  (output, grid) =>
            {
                grid.ForEachArray2D((layers, x, y) =>
                {
                    output.PutExtArrayNoHead(layers);
                });
            });
            Properties.WriteExternal(output);
        }
        public void InitTouchGrids()
        {
            if (Cubes != null && Cubes.Length > 0)
            {
                if (ChunkSize.X > 256) throw new Exception($"Chunk X size out of 256 '{ChunkSize.X}' ");
                if (ChunkSize.Y > 256) throw new Exception($"Chunk Y size out of 256 '{ChunkSize.Y}' ");
                var grid = TouchGrids = new StreamingTouchLayer[ChunkSize.X, ChunkSize.Y][];
                var matrix = new List<StreamingCube>[ChunkSize.X, ChunkSize.Y];
                foreach (var cube in Cubes)
                {
                    var layers = matrix[cube.X, cube.Y] = matrix[cube.X, cube.Y] ?? new List<StreamingCube>();
                    layers.Add(cube);
                }
                grid.ForEachArray2D((cell, x, y) =>
                {
                    if (matrix[x, y] != null)
                    {
                        matrix[x, y].Sort((a, b) => CMath.GetDirect(a.Z - b.Z));
                        var layers = new List<StreamingTouchLayer>(matrix[x, y].Count);
                        for (int i = 0; i < matrix[x, y].Count; i++)
                        {
                            var cube = matrix[x, y][i];
                            var downward = cube.Z * GridCellSize;
                            var upward = downward + GridCellSize;
                            layers.Add(new StreamingTouchLayer()
                            {
                                Upward = upward,
                                Downward = downward,
                            });
                        }
                        var d = GridCellSize;
                        for (int i = 0; i < layers.Count - 1; i++)
                        {
                            var down = layers[i];
                            var up = layers[i + 1];
                            if (up.Downward - down.Upward <= d)
                            {
                                up.Downward = down.Downward;
                                layers.RemoveAt(i);
                                --i;
                            }
                        }
                        grid[x, y] = layers.ToArray();
                    }
                });
            }
        }
    }

    /// <summary>
    /// 运行时体素块信息
    /// </summary>
    [MessageType(Codec.SCVX_MSGID + 2)]
    public class StreamingCube : IExternalizable
    {
        public byte X;
        public byte Y;
        public byte Z;
        public int CubeTemplateID;
        public StreamingCubeTemplate OverrideCube;
        public override string ToString()
        {
            return $"[{X} {Y} {Z}]";
        }
        public void ReadExternal(IInputStream input)
        {
            X = input.GetU8();
            Y = input.GetU8();
            Z = input.GetU8();
            CubeTemplateID = input.GetS32();
            OverrideCube = input.GetExt<StreamingCubeTemplate>();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutU8(X);
            output.PutU8(Y);
            output.PutU8(Z);
            output.PutS32(CubeTemplateID);
            output.PutExt(OverrideCube);
        }
    }

    /// <summary>
    /// 体素块模板
    /// </summary>
    [MessageType(Codec.SCVX_MSGID + 3)]
    public class StreamingCubeTemplate : IExternalizable
    {
        public uint ColorRGBA;
        public string Material;
        public string AppendData;
        public void ReadExternal(IInputStream input)
        {
            ColorRGBA = input.GetU32();
            Material = input.GetUTF();
            AppendData = input.GetUTF();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutU32(ColorRGBA);
            output.PutUTF(Material);
            output.PutUTF(AppendData);
        }
    }
    /// <summary>
    /// 行走面
    /// </summary>
    [MessageType(Codec.SCVX_MSGID + 4)]
    public class StreamingTouchLayer : IExternalizable
    {
        /// <summary>
        /// 行走上沿
        /// </summary>
        public float Upward;
        /// <summary>
        /// 高度下沿
        /// </summary>
        public float Downward;
        public void ReadExternal(IInputStream input)
        {
            Upward = input.GetF32();
            Downward = input.GetF32();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutF32(Upward);
            output.PutF32(Downward);
        }
    }

    [MessageType(Codec.SCVX_MSGID + 0x80)]
    public class StreamingMesh : IExternalizable
    {
        public string UUID;
        public List<Vector3> vertices;
        public List<Vector4> colors;
        public List<int> colorsID;
        public List<Vector2> uv;
        public List<Vector3> normals;
        public List<int> triangles;
        public void ReadExternal(IInputStream input)
        {
            UUID = input.GetUTF();
            this.vertices = input.GetStructList<Vector3>();
            this.colors = input.GetStructList<Vector4>();
            this.colorsID = input.GetStructList<int>();
            this.uv = input.GetStructList<Vector2>();
            this.normals = input.GetStructList<Vector3>();
            this.triangles = input.GetStructList<int>();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(UUID);
            output.PutStructList(vertices);
            output.PutStructList(colors);
            output.PutStructList(colorsID);
            output.PutStructList(uv);
            output.PutStructList(normals);
            output.PutStructList(triangles);
        }


    }

    public class StreamingVoxChunkFile : ExternalizableFileHeadStruct<StreamingChunk>
    {
        public const string FILE_HEAD = "SCVX";
        public const string FILE_EXT = ".scvx";
        public override string Head { get => FILE_HEAD; }
        public override IExternalizableFactory Codec => Data.Codec.Instance;
        public StreamingVoxChunkFile(StreamingChunk chunk)
        {
            Chunk = chunk;
        }
        public StreamingVoxChunkFile()
        {
        }
    }
    public class StreamingMeshFile : ExternalizableFileHeadStruct<StreamingMesh>
    {
        public const string FILE_HEAD = "SOBJ";
        public const string FILE_EXT = ".sobj";
        public override string Head { get => FILE_HEAD; }
        public override IExternalizableFactory Codec => Data.Codec.Instance;
        public StreamingMeshFile(StreamingMesh chunk)
        {
            Chunk = chunk;
        }
        public StreamingMeshFile()
        {
        }
    }
}
