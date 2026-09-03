using DeepCore.IO;
using DeepCore.Voxel.Data;
using DeepCore.Voxel.Extensions.MagicaVoxel;
using DeepCore.Voxel.StreamingVoxel.Data;
using DeepCore.Voxel.StreamingVoxel;
using DeepCore.Voxel;
using System;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System.Text;
using static System.Net.WebRequestMethods;
using DeepCore.SharpZipLib;
using DeepCore.Xml;

namespace DeepTools.Voxel
{
    public class Program
    {
        public static string Usage
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine($"vox2scvx");
                sb.AppendLine($"  转换 {nameof(MagicaVoxelFile)}({MagicaVoxelFile.FILE_EXT}) 文件到 {nameof(StreamingVoxChunkFile)}({StreamingVoxChunkFile.FILE_EXT})");
                sb.AppendLine($"   -input=fileName   {nameof(MagicaVoxelFile)}({MagicaVoxelFile.FILE_EXT}) 文件");
                sb.AppendLine($"  -output=fileName   {nameof(StreamingVoxChunkFile)}({StreamingVoxChunkFile.FILE_EXT}) 文件");
                sb.AppendLine($"        -slice=128   （可选）切割尺寸");
                sb.AppendLine($"           -grid=1   （可选）每格尺寸");
                sb.AppendLine();
                sb.AppendLine($"vox2voxw");
                sb.AppendLine($"  转换 {nameof(MagicaVoxelFile)}({MagicaVoxelFile.FILE_EXT}) 文件到 {nameof(VoxelWorld)}({VoxelWorld.FILE_EXT})");
                sb.AppendLine($"  -input=fileName    {nameof(MagicaVoxelFile)}({MagicaVoxelFile.FILE_EXT}) 文件");
                sb.AppendLine($"  -output=fileName   {nameof(VoxelWorld)}({VoxelWorld.FILE_EXT}) 文件");
                sb.AppendLine();
                sb.AppendLine($"scvx2sobj");
                sb.AppendLine($"  转换 {nameof(StreamingVoxChunkFile)}({StreamingVoxChunkFile.FILE_EXT}) 文件到 {nameof(StreamingMeshFile)}({StreamingMeshFile.FILE_EXT})");
                sb.AppendLine($"  -input=fileName    {nameof(StreamingVoxChunkFile)}({StreamingVoxChunkFile.FILE_EXT}) 文件");
                sb.AppendLine($"  -output=fileName   {nameof(StreamingMeshFile)}({StreamingMeshFile.FILE_EXT}) 文件");
                sb.AppendLine();
                sb.AppendLine($"zip2scvx");
                sb.AppendLine($"  转换 {nameof(VoxelTerrainData)}(.xml|.zip) 文件到 {nameof(StreamingVoxChunkFile)}({StreamingVoxChunkFile.FILE_EXT})");
                sb.AppendLine($"  -input=fileName    {nameof(VoxelTerrainData)}(.xml|.zip) 文件");
                sb.AppendLine($"  -output=dirName    输出目录");
                sb.AppendLine($"  -slice=128        （可选）切割尺寸");
                sb.AppendLine();
                sb.AppendLine($"scvx2lod");
                sb.AppendLine($"  转换 {nameof(StreamingVoxChunkFile)}({StreamingVoxChunkFile.FILE_EXT}) 文件到LOD减面");
                sb.AppendLine($"  -input=fileName    {nameof(StreamingVoxChunkFile)}({StreamingVoxChunkFile.FILE_EXT}) 文件");
                sb.AppendLine($"  -output=fileName   {nameof(StreamingVoxChunkFile)}({StreamingVoxChunkFile.FILE_EXT}) LOD文件");
                sb.AppendLine($"  -lod=1            （可选）缩减尺寸，默认1");
                sb.AppendLine();
                sb.AppendLine($"公共参数");
                sb.AppendLine($"    -wd=<工作目录>   （可选）");
                sb.AppendLine();
                sb.AppendLine($"样例");
                sb.AppendLine($"  vox2scvx -wd=E:\\MagicaVoxel\\ -input=streaming_block.vox -output=streaming_block.scvx");
                return sb.ToString();
            }
        }
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                try
                {
                    var prop = DeepCore.Properties.ParseArgs(args);
                    if (prop.TryGetValue("-wd", out var _workdir))
                    {
                        Environment.CurrentDirectory = _workdir;
                    }
                    var cmd = args[0];
                    if (cmd == nameof(zip2scvx))
                    {
                        zip2scvx(prop);
                    }
                    else if (cmd == nameof(vox2voxw))
                    {
                         vox2voxw(prop);
                    }
                    else if (cmd == nameof(vox2scvx))
                    {
                        vox2scvx(prop);
                    }
                    else if (cmd == nameof(scvx2sobj))
                    {
                        scvx2sobj(prop);
                    }
                    else if (cmd == nameof(scvx2lod))
                    {
                        scvx2lod(prop);
                    }
                    else
                    {
                        Console.WriteLine(Usage);
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                    Console.WriteLine(Usage);
                    Environment.Exit(1);
                }
            }
        }

        public static void scvx2lod(DeepCore.Properties prop)
        {
            var _input = prop.Get("-input");
            var _output = prop.Get("-output");
            var _lod = 1;
            if (prop.TryGetAsInt("-lod", out var value))
            {
                _lod = value;
            }
            var scvx = StreamingVoxChunkFile.Load<StreamingVoxChunkFile>(new FileInfo(_input));
            var scvxlod = StreamingConverter.BakeChunkLOD(scvx.Chunk, _lod);
            StreamingVoxChunkFile.Save(new StreamingVoxChunkFile() { Chunk = scvxlod }, new FileInfo(_output));
        }
        public static void scvx2sobj(DeepCore.Properties prop) 
        {
            var _input = prop.Get("-input");
            var _output = prop.Get("-output");
            var scvx = StreamingVoxChunkFile.Load<StreamingVoxChunkFile>(new FileInfo(_input));
            var mesh = StreamingConverter.ConvertToMesh(scvx.Chunk);
            StreamingMeshFile.Save(new StreamingMeshFile(mesh), new FileInfo(_output));
        }
        public static void zip2scvx(DeepCore.Properties prop)
        {
            var _input = prop.Get("-input");
            var _output = prop.Get("-output");
            var _slice = 128;
            if (prop.TryGetAsInt("-slice", out var value))
            {
                _slice = value;
            }
            var data = LoadTerrainDataFromZipFile(_input);
            VoxelConverter.ConvertTerrainDataToStreamingVoxChunks(data, _slice, new DirectoryInfo(_output));
        }
        public static void vox2scvx(DeepCore.Properties prop)
        {
            var _input = prop.Get("-input");
            var _output = prop.Get("-output");
            var _grid = 1f;
            if (prop.TryGetAsInt("-grid", out var value))
            {
                _grid = value;
            }
            if (prop.TryGetAsInt("-slice", out var _slice))
            {
                var scvx = VoxelConverter.ConvertMagicaVoxelToStreamingVoxChunksFile(new FileInfo(_input), new DirectoryInfo(_output), _grid, _slice);
            }
            else
            {
                var scvx = VoxelConverter.ConvertMagicaVoxelToStreamingVoxChunkFile(new FileInfo(_input), new FileInfo(_output), _grid, out var vox);
            }
        }
        public static void vox2voxw(DeepCore.Properties prop)
        {
            var _input = prop.Get("-input");
            var _output = prop.Get("-output");
            var world = VoxelConverter.ConvertMagicaVoxelFileToVoxelWorld(new FileInfo(_input), new FileInfo(_output), out var vox);
        }

        public static VoxelTerrainData LoadTerrainDataFromZipFile(string path)
        {
            using (var zstream = ZipUtil.LoadZipEntry(path, e => e.Name.ToLower().EndsWith(".voxt")))
            {
                //var doc = XmlUtil.LoadXML(zstream);
                using(var reader = new StreamReader(zstream))
                {
                    return VoxelTerrainData.LoadFromText(reader);
                }
            }
        }

    }
}
