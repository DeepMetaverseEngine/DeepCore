using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepCore.Voxel.Extensions.MagicaVoxel
{
    /// <summary>
    /// MagicaVoxel.vox File Format [10/18/2016]
    /// </summary>
    public partial class MagicaVoxelFile
    {
        private static LazyLogger log = new LazyLogger(nameof(MagicaVoxelFile));

        //------------------------------------------------------------------------------------------------------------
        public string Head { get; private set; }
        public int Version { get; private set; }
        public ChunkMain Main { get; private set; }
        public bool HasExtension { get => Main?.Extensions.Length > 0; }
        public int TotalVoxelCount
        {
            get; private set;
        }
        public MagicaVoxelFile()
        {
            TotalVoxelCount = 0;
        }
        public Geometry.BoundingBox GetAABB()
        {
            GetAABB(out int minX, out int minY, out int minZ, out int maxX, out int maxY, out int maxZ);
            return new Geometry.BoundingBox(new Geometry.Vector3(minX, minY, minZ), new Geometry.Vector3(maxX, maxY, maxZ));
        }
        public void GetAABB(out int minX, out int minY, out int minZ, out int maxX, out int maxY, out int maxZ)
        {
            var _minX = int.MaxValue;
            var _minY = int.MaxValue;
            var _minZ = int.MaxValue;
            var _maxX = int.MinValue;
            var _maxY = int.MinValue;
            var _maxZ = int.MinValue;
            ForEachModels(v =>
            {
                _minX = Math.Min(_minX, Math.Min(v.X1, v.X2));
                _minY = Math.Min(_minY, Math.Min(v.Y1, v.Y2));
                _minZ = Math.Min(_minZ, Math.Min(v.Z1, v.Z2));
                _maxX = Math.Max(_maxX, Math.Max(v.X1, v.X2));
                _maxY = Math.Max(_maxY, Math.Max(v.Y1, v.Y2));
                _maxZ = Math.Max(_maxZ, Math.Max(v.Z1, v.Z2));
            });
            minX = _minX;
            minY = _minY;
            minZ = _minZ;
            maxX = _maxX;
            maxY = _maxY;
            maxZ = _maxZ;
        }
        public Geometry.BoundingBox GetTrimAABB()
        {
            GetTrimAABB(out int minX, out int minY, out int minZ, out int maxX, out int maxY, out int maxZ);
            return new Geometry.BoundingBox(new Geometry.Vector3(minX, minY, minZ), new Geometry.Vector3(maxX, maxY, maxZ));
        }
        public void GetTrimAABB(out int minX, out int minY, out int minZ, out int maxX, out int maxY, out int maxZ)
        {
            var _minX = int.MaxValue;
            var _minY = int.MaxValue;
            var _minZ = int.MaxValue;
            var _maxX = int.MinValue;
            var _maxY = int.MinValue;
            var _maxZ = int.MinValue;
            ForEachVoxels(v =>
            {
                _minX = Math.Min(_minX, v.X);
                _minY = Math.Min(_minY, v.Y);
                _minZ = Math.Min(_minZ, v.Z);
                _maxX = Math.Max(_maxX, v.X);
                _maxY = Math.Max(_maxY, v.Y);
                _maxZ = Math.Max(_maxZ, v.Z);
            });
            minX = _minX;
            minY = _minY;
            minZ = _minZ;
            maxX = _maxX;
            maxY = _maxY;
            maxZ = _maxZ;
        }
        public void ForEachVoxels(Action<VisitCube> action)
        {
            if (Main != null)
            {
                if (Main.SceneGraph != null)
                {
                    Main.SceneGraph.ForEachVoxels(action);
                }
                else
                {
                    var zero = Translation.Zero;
                    foreach (var m in this.Main.Models)
                    {
                        var sx = m.Size.SizeX / 2;
                        var sy = m.Size.SizeY / 2;
                        var sz = m.Size.SizeZ / 2;
                        foreach (var c in m.XYZI.Voxels)
                        {
                            action(new VisitCube()
                            {
                                X = c.X - sx,
                                Y = c.Y - sy,
                                Z = c.Z,
                                ColorIndex = c.ColorIndex
                            });
                        }
                    }
                }
            }
        }
        public void ForEachModels(Action<VisitModel> action)
        {
            if (Main != null)
            {
                if (Main.SceneGraph != null)
                {
                    Main.SceneGraph.ForEachModels(action);
                }
                else
                {
                    var zero = Translation.Zero;
                    foreach (var m in this.Main.Models)
                    {
                        action(new VisitModel()
                        {
                            X1 = 0,
                            Y1 = 0,
                            Z1 = 0,
                            X2 = m.Size.SizeX - 1,
                            Y2 = m.Size.SizeY - 1,
                            Z2 = m.Size.SizeZ - 1,
                        });
                    }
                }
            }
        }



        //------------------------------------------------------------------------------------------------------------
        #region Loader
        public static MagicaVoxelFile Load(IInputStream input)
        {
            var ret = new MagicaVoxelFile();
            ret.Read(input);
            return ret;
        }
        public static MagicaVoxelFile Load(Stream stream)
        {
            var input = new InputStream(stream, null);
            return Load(input);
        }
        public static MagicaVoxelFile Load(byte[] data)
        {
            using (var stream = new IO.MemoryStream(data))
            {
                return MagicaVoxelFile.Load(stream);
            }
        }
        public static MagicaVoxelFile Load(FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                return MagicaVoxelFile.Load(stream);
            }
        }


        // 1.File Structure: RIFF style
        // -------------------------------------------------------------------------------
        // # Bytes  | Type       | Value
        // -------------------------------------------------------------------------------
        // 1x4 | char | id 'VOX ' : 'V' 'O' 'X' 'space', 'V' is first
        // 4   | int  | version number: 150
        // 
        // Chunk 'MAIN'
        void Read(IInputStream input)
        {
            if (input.TryValidateFileHeadASCII(FILE_HEAD, out var head))
            {
                this.Head = head;
                this.Version = input.GetS32();
                if (Chunk.TryLoadChunkAs<ChunkMain>(input, this, out var main))
                {
                    this.Main = main;
                }
            }
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------
        #region Chunks

        // 2.Chunk Structure
        // -------------------------------------------------------------------------------
        // # Bytes  | Type       | Value
        // -------------------------------------------------------------------------------
        // 1x4 | char | chunk id
        // 4 | int | num bytes of chunk content (N)
        // 4 | int | num bytes of children chunks (M)
        // 
        // N |            | chunk content
        // 
        // M |            | children chunks
        // -------------------------------------------------------------------------------
        public abstract class Chunk
        {
            public MagicaVoxelFile Owner { get; private set; }
            public string ChunkID { get; private set; }
            public int ContentBytes { get; private set; }
            public int ChildrenBytes { get; private set; }
            public Chunk[] Children { get; private set; }
            public ExtensionChunk[] Extensions { get; private set; }
            public override string ToString()
            {
                return ChunkID;
            }
            private void Read(MagicaVoxelFile meta, IInputStream input)
            {
                this.Owner = meta;
                this.ContentBytes = input.GetS32();
                this.ChildrenBytes = input.GetS32();
                if (ContentBytes > 0)
                {
                    try
                    {
                        using (input.BeginValidateBodySize(ContentBytes))
                        {
                            this.ReadContent(input);
                        }
                    }
                    catch (Exception err)
                    {
                        throw new Exception($"LoadChunkContentError:{ChunkID} Inner:{err.Message}", err);
                    }
                }
                if (ChildrenBytes > 0)
                {
                    try
                    {
                        using (input.BeginValidateBodySize(ChildrenBytes))
                        {
                            var childs = new List<Chunk>();
                            var exts = new List<ExtensionChunk>();
                            while (TryPickChunk(input, out var c))
                            {
                                c.Read(meta, input);
                                childs.Add(c);
                                if (c is ExtensionChunk ext)
                                {
                                    exts.Add(ext);
                                }
                            }
                            this.Children = childs.ToArray();
                            this.Extensions = exts.ToArray();
                            this.InitChildren();
                        }
                    }
                    catch (Exception err)
                    {
                        throw new Exception($"LoadChunkChildrenError:{ChunkID} Inner:{err.Message}", err);
                    }
                }
            }
            protected virtual void ReadContent(IInputStream input) { }
            protected virtual void InitChildren() { }
            public static bool TryPickChunk(IInputStream input, out Chunk chunk)
            {
                if (input.Position <= input.Length - 4)
                {
                    var head = input.GetFileHeadASCII(4);
                    if (CHUNK_TYPES.TryGetValue(head, out var ctype))
                    {
                        chunk = (Chunk)DeepActivator.CreateInstance(ctype);
                        chunk.ChunkID = head;
                    }
                    else
                    {
                        chunk = new DummyChunk();
                        chunk.ChunkID = head;
                        //log.Error($"Find DummyChunk '{head}'");
                    }
                }
                else
                {
                    chunk = null;
                }
                return chunk != null;
            }
            public static bool TryLoadChunkAs<T>(IInputStream input, MagicaVoxelFile file, out T chunkT) where T : Chunk
            {
                var pos = input.Position;
                var ret = TryPickChunk(input, out var chunk);
                if (chunk is T c)
                {
                    c.Read(file, input);
                    chunkT = c;
                    return true;
                }
                else
                {
                    input.Position = pos;
                    chunkT = null;
                    return false;
                }
            }

            public bool TryGetChildAs<D>(out D value) where D : Chunk
            {
                return Children.TryFindAs(d => true, out value);
            }
            public bool TryGetChildTypes<D>(out D[] value) where D : Chunk
            {
                return Children.TryFindTypes(d => true, out value);
            }

        }
        public class DummyChunk : Chunk
        {
            protected override void ReadContent(IInputStream input)
            {
                input.Position += ContentBytes;
            }
        }

        // 3. Chunk id 'MAIN' : the root chunk and parent chunk of all the other chunks
        // Chunk 'MAIN'
        // {
        //     // pack of models
        //     Chunk 'PACK'    : optional
        // 
        //     // models
        //     Chunk 'SIZE'
        //     Chunk 'XYZI'
        // 
        //     Chunk 'SIZE'
        //     Chunk 'XYZI'...
        // 
        //     Chunk 'SIZE'
        //     Chunk 'XYZI'
        // 
        //     // palette
        //     Chunk 'RGBA'    : optional
        // 
        //     // materials
        //     Chunk 'MATT'    : optional
        //     Chunk 'MATT'...
        //     Chunk 'MATT'
        // }
        [Desc("MAIN")]
        public class ChunkMain : Chunk
        {
            public ChunkPack Pack { get; private set; }
            public Model[] Models { get; private set; }
            public ChunkRGBA Palette { get; private set; }
            public ChunkMATT[] Materials { get; private set; }
            public SceneGraph SceneGraph { get; private set; }
            protected override void InitChildren()
            {
                if (TryGetChildAs(out ChunkPack pack))
                {
                    this.Pack = pack;
                    this.Models = new Model[Pack.NumModels];
                }
                if (TryGetChildTypes(out ChunkSize[] models_size) &&
                    TryGetChildTypes(out ChunkXYZI[] models_xyzi))
                {
                    if (this.Models == null)
                    {
                        this.Models = new Model[models_size.Length];
                    }
                    for (var i = 0; i < Models.Length; ++i)
                    {
                        Models[i] = new Model()
                        {
                            Size = models_size[i],
                            XYZI = models_xyzi[i],
                        };
                        this.Owner.TotalVoxelCount += models_xyzi[i].NumVoxels;
                    }
                }
                if (TryGetChildAs(out ChunkRGBA rgba))
                {
                    this.Palette = rgba;
                }
                else
                {
                    this.Palette = ChunkRGBA.Default;
                }
                if (TryGetChildTypes(out ChunkMATT[] matts))
                {
                    this.Materials = matts;
                }
                this.SceneGraph = LoadSceneGraph(this);
            }
        }

        // 4. Chunk id 'PACK' : if it is absent, only one model in the file
        // -------------------------------------------------------------------------------
        // # Bytes  | Type       | Value
        // -------------------------------------------------------------------------------
        // 4        | int        | numModels : num of SIZE and XYZI chunks
        // -------------------------------------------------------------------------------
        [Desc("PACK")]
        public class ChunkPack : Chunk
        {
            public int NumModels { get; private set; }
            protected override void ReadContent(IInputStream input)
            {
                this.NumModels = input.GetS32();
            }
        }

        // 5. Chunk id 'SIZE' : model size
        // -------------------------------------------------------------------------------
        // # Bytes  | Type       | Value
        // -------------------------------------------------------------------------------
        // 4 | int | size x
        // 4 | int | size y
        // 4 | int | size z: gravity direction
        // -------------------------------------------------------------------------------
        [Desc("SIZE")]
        public class ChunkSize : Chunk
        {
            public int SizeX { get; private set; }
            public int SizeY { get; private set; }
            public int SizeZ { get; private set; }
            public override string ToString()
            {
                return $"{base.ToString()}:{SizeX} {SizeY} {SizeZ}";
            }
            protected override void ReadContent(IInputStream input)
            {
                this.SizeX = input.GetS32();
                this.SizeY = input.GetS32();
                this.SizeZ = input.GetS32();
            }
        }

        // 6. Chunk id 'XYZI' : model voxels
        // -------------------------------------------------------------------------------
        // # Bytes  | Type       | Value
        // -------------------------------------------------------------------------------
        // 4        | int        | numVoxels(N)
        // 4 x N    | int        | (x, y, z, colorIndex) : 1 byte for each component
        // -------------------------------------------------------------------------------
        [Desc("XYZI")]
        public class ChunkXYZI : Chunk
        {
            public int NumVoxels { get; private set; }
            public Cube[] Voxels { get; private set; }
            protected override void ReadContent(IInputStream input)
            {
                NumVoxels = input.GetS32();
                Voxels = new Cube[NumVoxels];
                for (int i = 0; i < NumVoxels; i++)
                {
                    Voxels[i].X = input.GetU8();
                    Voxels[i].Y = input.GetU8();
                    Voxels[i].Z = input.GetU8();
                    Voxels[i].ColorIndex = input.GetU8();
                }
            }
        }

        // 7.Chunk id 'RGBA' : palette
        // -------------------------------------------------------------------------------
        // # Bytes  | Type       | Value
        // -------------------------------------------------------------------------------
        // 4 x 256 | int | (R, G, B, A) : 1 byte for each component
        //               | * < NOTICE >
        //               | *color[0 - 254] are mapped to palette index[1 - 255], e.g : 
        //               |
        //               | for (int i = 0; i <= 254; i++)
        //               | {
        //               | palette[i + 1] = ReadRGBA();
        //               | }
        // -------------------------------------------------------------------------------
        // 8.Default Palette: if chunk 'RGBA' is absent
        // -------------------------------------------------------------------------------
        // unsigned int default_palette[256] = {
        //     0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff, 0xffccccff, 0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff,
        //     0xff6699ff, 0xff3399ff, 0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff, 0xff0066ff, 0xffff33ff, 0xffcc33ff, 0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff,
        //     0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff, 0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc, 0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc, 0xff99cccc, 0xff66cccc, 0xff33cccc,
        //     0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc, 0xff0099cc, 0xffff66cc, 0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc, 0xff9933cc,
        //     0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc, 0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99,
        //     0xffcccc99, 0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999, 0xff669999, 0xff339999, 0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699,
        //     0xff006699, 0xffff3399, 0xffcc3399, 0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099, 0xffcc0099, 0xff990099, 0xff660099, 0xff330099, 0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66,
        //     0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66, 0xff99cc66, 0xff66cc66, 0xff33cc66, 0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966, 0xff009966, 0xffff6666,
        //     0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366, 0xff993366, 0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
        //     0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33, 0xffcccc33, 0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933,
        //     0xff669933, 0xff339933, 0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633, 0xff006633, 0xffff3333, 0xffcc3333, 0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033,
        //     0xffcc0033, 0xff990033, 0xff660033, 0xff330033, 0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00, 0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00, 0xff99cc00, 0xff66cc00, 0xff33cc00,
        //     0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900, 0xff009900, 0xffff6600, 0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300, 0xff993300,
        //     0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000, 0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044,
        //     0xff000022, 0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700, 0xff005500, 0xff004400, 0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000,
        //     0xff880000, 0xff770000, 0xff550000, 0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd, 0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777, 0xff555555, 0xff444444, 0xff222222, 0xff111111
        // };
        // -------------------------------------------------------------------------------
        [Desc("RGBA")]
        public class ChunkRGBA : Chunk
        {
            private static uint[] DefaultPalette = new uint[256]
            {
                0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff, 0xffccccff, 0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff,
                0xff6699ff, 0xff3399ff, 0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff, 0xff0066ff, 0xffff33ff, 0xffcc33ff, 0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff,
                0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff, 0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc, 0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc, 0xff99cccc, 0xff66cccc, 0xff33cccc,
                0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc, 0xff0099cc, 0xffff66cc, 0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc, 0xff9933cc,
                0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc, 0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99,
                0xffcccc99, 0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999, 0xff669999, 0xff339999, 0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699,
                0xff006699, 0xffff3399, 0xffcc3399, 0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099, 0xffcc0099, 0xff990099, 0xff660099, 0xff330099, 0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66,
                0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66, 0xff99cc66, 0xff66cc66, 0xff33cc66, 0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966, 0xff009966, 0xffff6666,
                0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366, 0xff993366, 0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
                0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33, 0xffcccc33, 0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933,
                0xff669933, 0xff339933, 0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633, 0xff006633, 0xffff3333, 0xffcc3333, 0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033,
                0xffcc0033, 0xff990033, 0xff660033, 0xff330033, 0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00, 0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00, 0xff99cc00, 0xff66cc00, 0xff33cc00,
                0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900, 0xff009900, 0xffff6600, 0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300, 0xff993300,
                0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000, 0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044,
                0xff000022, 0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700, 0xff005500, 0xff004400, 0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000,
                0xff880000, 0xff770000, 0xff550000, 0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd, 0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777, 0xff555555, 0xff444444, 0xff222222, 0xff111111
            };
            public Color[] Palette { get; private set; }
            protected override void ReadContent(IInputStream input)
            {
                Palette = new Color[256];
                for (int i = 1; i < Palette.Length; i++)
                {
                    Palette[i].R = input.GetU8();
                    Palette[i].G = input.GetU8();
                    Palette[i].B = input.GetU8();
                    Palette[i].A = input.GetU8();
                }
                Palette[0].RGBA = input.GetU32();
            }
            public Color GetColor(int index)
            {
                return Palette[index];
            }
            public static ChunkRGBA Default
            {
                get
                {
                    var ret = new ChunkRGBA();
                    ret.Palette = new Color[256];
                    for (int i = 0; i < ret.Palette.Length; i++)
                    {
                        Colors.DecodeABGR(DefaultPalette[i],
                            out ret.Palette[i].R,
                            out ret.Palette[i].G,
                            out ret.Palette[i].B,
                            out ret.Palette[i].A);
                    }
                    return ret;
                }
            }
        }

        // 9.Chunk id 'MATT' : material, if it is absent, it is diffuse material
        // -------------------------------------------------------------------------------
        // # Bytes  | Type       | Value
        // -------------------------------------------------------------------------------
        //        4 | int        | id[1 - 255]
        //                       
        //        4 | int        | material type
        //                       | 0 : diffuse
        //                       | 1 : metal
        //                       | 2 : glass
        //                       | 3 : emissive
        //        
        //        4 | float      | material weight
        //                       | diffuse  : 1.0
        //                       | metal    : (0.0 - 1.0] : blend between metal and diffuse material
        //                       | glass    : (0.0 - 1.0] : blend between glass and diffuse material
        //                       | emissive : (0.0 - 1.0] : self - illuminated material
        //        
        //        4 | int        | property bits: set if value is saved in next section
        //                       | bit(0) : Plastic
        //                       | bit(1) : Roughness
        //                       | bit(2) : Specular
        //                       | bit(3) : IOR
        //                       | bit(4) : Attenuation
        //                       | bit(5) : Power
        //                       | bit(6) : Glow
        //                       | bit(7) : isTotalPower(*no value)
        //        
        //    4 * N | float      | normalized property value : (0.0 - 1.0]
        //                       | *need to map to real range
        //                       | * Plastic material only accepts {0.0, 1.0} for this version
        // -------------------------------------------------------------------------------
        [Desc("MATT")]
        public class ChunkMATT : Chunk
        {
            public int ID { get; private set; }
            public MaterialType MattType { get; private set; }
            public float MaterialWeight { get; private set; }
            public bool IsPlastic { get; private set; }
            public bool IsRoughness { get; private set; }
            public bool IsSpecular { get; private set; }
            public bool IsIOR { get; private set; }
            public bool IsAttenuation { get; private set; }
            public bool IsPower { get; private set; }
            public bool IsGlow { get; private set; }
            public bool IsisTotalPower { get; private set; }
            public float[] NormalizedPropertyValue { get; private set; }
            protected override void ReadContent(IInputStream input)
            {
                ID = input.GetS32();
                MattType = input.GetEnum32<MaterialType>();
                MaterialWeight = input.GetF32();
                var bitset = new BitSet32(input.GetS32());
                {
                    IsPlastic = bitset.Get(0);
                    IsRoughness = bitset.Get(1);
                    IsSpecular = bitset.Get(2);
                    IsIOR = bitset.Get(3);
                    IsAttenuation = bitset.Get(4);
                    IsPower = bitset.Get(5);
                    IsGlow = bitset.Get(6);
                    IsisTotalPower = bitset.Get(7);
                }
                var n = (base.ContentBytes - 4 - 4 - 4 - 4) / 4;
                this.NormalizedPropertyValue = new float[n];
                for (int i = 0; i < n; i++)
                {
                    this.NormalizedPropertyValue[i] = input.GetF32();
                }
            }
            public override string ToString()
            {
                return $"{base.ToString()}:{ID}";
            }
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------
        #region Extension

        // * there can be multiple SIZE and XYZI chunks for multiple models; model id is their index in the stored order
        // * the palette chunk is always stored into the file, so default palette is not needed any more
        // * the MATT chunk is deprecated, replaced by the MATL chunk, see (4)
        // * (a), (b), (c) are special data types; (d) is the scene graph in the world editor
        public abstract class ExtensionChunk : Chunk
        {

        }
        // =================================
        // (a) STRING type
        // 
        // int32   : buffer size (in bytes)
        // int8xN	: buffer (without the ending "\0")
        private static string ReadString(IInputStream input)
        {
            var size = input.GetS32();
            unsafe
            {
                var buffer = stackalloc byte[size];
                input.GetRawBytes(buffer, 0, size);
                return CUtils.UTF8.GetString(buffer, size);
            }
        }

        // =================================
        // (b) DICT type
        // 
        // int32	: num of key-value pairs
        // 
        // // for each key-value pair
        // {
        // STRING	: key
        // STRING	: value
        // }xN
        private static Properties ReadDict(IInputStream input)
        {
            var size = input.GetS32();
            var map = new Properties(size);
            for (var i = 0; i < size; i++)
            {
                var key = ReadString(input);
                var value = ReadString(input);
                map.Add(key, value);
            }
            return map;
        }

        // =================================
        // (c) ROTATION type
        // 
        // store a row-major rotation in the bits of a byte
        // 
        // for example :
        // R =
        //  0  1  0
        //  0  0 -1
        // -1  0  0 
        // ==>
        // unsigned char _r = (1 << 0) | (2 << 2) | (0 << 4) | (1 << 5) | (1 << 6)
        // 
        // bit | value
        // 0-1 : 1 : index of the non-zero entry in the first row
        // 2-3 : 2 : index of the non-zero entry in the second row
        // 4   : 0 : the sign in the first row (0 : positive; 1 : negative)
        // 5   : 1 : the sign in the second row (0 : positive; 1 : negative)
        // 6   : 1 : the sign in the third row (0 : positive; 1 : negative)
        private static Rotation ReadRotation(byte rotation)
        {
            var r = new Rotation();
            int firstIndex = (rotation & 0b0011);
            int secondIndex = (rotation & 0b1100) >> 2;
            unsafe
            {
                var array = stackalloc int[3] { -1, -1, -1 };
                int index = 0;
                array[firstIndex] = 0;
                array[secondIndex] = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (array[i] == -1)
                    {
                        index = i;
                        break;
                    }
                }
                int thirdIndex = index;
                var negativeFirst = (((rotation & 0b0010000) >> 4) == 1) ? -1 : 1;
                var negativeSecond = (((rotation & 0b0100000) >> 5) == 1) ? -1 : 1;
                var negativeThird = (((rotation & 0b1000000) >> 6) == 1) ? -1 : 1;
                /*
                matrix.setElement(0, firstIndex, negativeFirst ? -1 : 1);
                matrix.setElement(1, secondIndex, negativeSecond ? -1 : 1);
                matrix.setElement(2, thirdIndex, negativeThird ? -1 : 1);
                */
                switch (firstIndex)
                {
                    case 0: r.M11 = negativeFirst; break;
                    case 1: r.M12 = negativeFirst; break;
                    case 2: r.M13 = negativeFirst; break;
                }
                switch (secondIndex)
                {
                    case 0: r.M21 = negativeSecond; break;
                    case 1: r.M22 = negativeSecond; break;
                    case 2: r.M23 = negativeSecond; break;
                }
                switch (thirdIndex)
                {
                    case 0: r.M31 = negativeThird; break;
                    case 1: r.M32 = negativeThird; break;
                    case 2: r.M33 = negativeThird; break;
                }
            }
            return r;
        }


        // =================================
        // (d) Scene Graph
        // 
        // T : Transform Node
        // G : Group Node
        // S : Shape Node
        // 
        //      T
        //      |
        //      G
        //     / \
        //    T   T
        //    |   |
        //    G   S
        //   / \
        //  T   T
        //  |   |
        //  S   S
        // =================================

        static SceneGraph LoadSceneGraph(ChunkMain main)
        {
            return SceneGraph.InitSceneGraph(main);
        }


        // =================================
        // (1) Transform Node Chunk : "nTRN"
        // 
        // int32	: node id
        // DICT	: node attributes
        // 	  (_name : string)
        // 	  (_hidden : 0/1)
        // int32 	: child node id
        // int32 	: reserved id (must be -1)
        // int32	: layer id
        // int32	: num of frames (must be 1)
        // 
        // // for each frame
        // {
        // DICT	: frame attributes
        // 	  (_r : int8) ROTATION, see (c)
        // 	  (_t : int32x3) translation
        // }xN
        [Desc("nTRN")]
        public class ChunkTransformNode : ExtensionChunk, ISceneGraphNode
        {
            public int NodeID { get; private set; }
            public Properties NodeAttributes { get; private set; }
            public int ChildNodeID { get; private set; }
            public int ReservedID { get; private set; }
            public int LayerID { get; private set; }
            public int NumOfFrames { get; private set; }
            public Frame[] Frames { get; private set; }
            public Translation Translation { get => Frames[0].Translation; }
            public Rotation Rotation { get => Frames[0].Rotation; }
            protected override void ReadContent(IInputStream input)
            {
                NodeID = input.GetS32();
                NodeAttributes = ReadDict(input);
                ChildNodeID = input.GetS32();
                ReservedID = input.GetS32();
                LayerID = input.GetS32();
                NumOfFrames = input.GetS32();
                Frames = new Frame[NumOfFrames];
                for (int f = 0; f < NumOfFrames; f++)
                {
                    Frames[f] = new Frame();
                    Frames[f].FrameAttributes = ReadDict(input);
                    if (Frames[f].FrameAttributes.TryGetAsByte("_r", out var _r))
                    {
                        Frames[f].Rotation = ReadRotation(_r);
                    }
                    else
                    {
                        Frames[f].Rotation = Rotation.Identy;
                    }
                    if (Frames[f].FrameAttributes.TryGetValue("_t", out var _t))
                    {
                        var xyz = _t.Split(' ').Convert1D((i, v) => Parser.ParseInt(v));
                        Frames[f].Translation.X = xyz[0];
                        Frames[f].Translation.Y = xyz[1];
                        Frames[f].Translation.Z = xyz[2];
                    }
                }
            }
            public override string ToString()
            {
                return $"{base.ToString()}:{NodeID}";
            }
        }
        // =================================
        // (2) Group Node Chunk : "nGRP" 
        // 
        // int32	: node id
        // DICT	: node attributes
        // int32 	: num of children nodes
        // 
        // // for each child
        // {
        // int32	: child node id
        // }xN
        [Desc("nGRP")]
        public class ChunkGroupNode : ExtensionChunk, ISceneGraphNode
        {
            public int NodeID { get; private set; }
            public Properties NodeAttributes { get; private set; }
            public int NumOfChildren { get; private set; }
            public int[] ChildrenID { get; private set; }
            protected override void ReadContent(IInputStream input)
            {
                NodeID = input.GetS32();
                NodeAttributes = ReadDict(input);
                NumOfChildren = input.GetS32();
                ChildrenID = new int[NumOfChildren];
                for (int f = 0; f < NumOfChildren; f++)
                {
                    ChildrenID[f] = input.GetS32();
                }
            }
            public override string ToString()
            {
                return $"{base.ToString()}:{NodeID}";
            }
        }

        // =================================
        // (3) Shape Node Chunk : "nSHP" 
        // 
        // int32	: node id
        // DICT	: node attributes
        // int32 	: num of models (must be 1)
        // 
        // // for each model
        // {
        // int32	: model id
        // DICT	: model attributes : reserved
        // }xN
        [Desc("nSHP")]
        public class ChunkShapeNode : ExtensionChunk, ISceneGraphNode
        {
            public int NodeID { get; private set; }
            public Properties NodeAttributes { get; private set; }
            public int NumOfModels { get; private set; }
            public ShapeModel[] Models { get; private set; }
            protected override void ReadContent(IInputStream input)
            {
                NodeID = input.GetS32();
                NodeAttributes = ReadDict(input);
                NumOfModels = input.GetS32();
                Models = new ShapeModel[NumOfModels];
                for (int f = 0; f < NumOfModels; f++)
                {
                    Models[f].ModelID = input.GetS32();
                    Models[f].NodeAttributes = ReadDict(input);
                }
            }
            public override string ToString()
            {
                return $"{base.ToString()}:{NodeID}";
            }
        }
        // =================================
        // (4) Material Chunk : "MATL"
        // 
        // int32	: material id
        // DICT	: material properties
        // 	  (_type : str) _diffuse, _metal, _glass, _emit
        // 	  (_weight : float) range 0 ~ 1
        // 	  (_rough : float)
        // 	  (_spec : float)
        // 	  (_ior : float)
        // 	  (_att : float)
        // 	  (_flux : float)
        // 	  (_plastic)
        [Desc("MATL")]
        public class ChunkMaterial : ExtensionChunk
        {
            public int MaterialID { get; private set; }
            public Properties MaterialAttributes { get; private set; }
            public MaterialType _type { get; private set; }
            public float _weight { get; private set; }
            public float _rough { get; private set; }
            public float _spec { get; private set; }
            public float _ior { get; private set; }
            public float _att { get; private set; }
            public float _flux { get; private set; }
            public float _plastic { get; private set; }
            protected override void ReadContent(IInputStream input)
            {
                MaterialID = input.GetS32();
                MaterialAttributes = ReadDict(input);
                _type = MaterialAttributes.GetAsEnum<MaterialType>(nameof(_type));
                _weight = MaterialAttributes.GetAsFloat(nameof(_weight));
                _rough = MaterialAttributes.GetAsFloat(nameof(_rough));
                _spec = MaterialAttributes.GetAsFloat(nameof(_spec));
                _ior = MaterialAttributes.GetAsFloat(nameof(_ior));
                _att = MaterialAttributes.GetAsFloat(nameof(_att));
                _flux = MaterialAttributes.GetAsFloat(nameof(_flux));
                _plastic = MaterialAttributes.GetAsFloat(nameof(_plastic));
            }
            public override string ToString()
            {
                return $"{base.ToString()}:{MaterialID}";
            }
        }

        // =================================
        // (5) Layer Chunk : "LAYR"
        // int32	: layer id
        // DICT	: layer atrribute
        // 	  (_name : string)
        // 	  (_hidden : 0/1)
        // int32	: reserved id, must be -1
        [Desc("LAYR")]
        public class ChunkLayer : ExtensionChunk
        {
            public int LayerID { get; private set; }
            public Properties LayerAtrribute { get; private set; }
            public int ReservedID { get; private set; }
            public string _name { get; private set; }
            public bool _hidden { get; private set; }
            protected override void ReadContent(IInputStream input)
            {
                LayerID = input.GetS32();
                LayerAtrribute = ReadDict(input);
                ReservedID = input.GetS32();
                _name = LayerAtrribute.Get(nameof(_name));
                _hidden = LayerAtrribute.GetAsInt(nameof(_hidden)) == 1;
            }
            public override string ToString()
            {
                return $"{base.ToString()}:{LayerID}";
            }
        }

        #endregion

        //------------------------------------------------------------------------------------------------------------
    }
}
