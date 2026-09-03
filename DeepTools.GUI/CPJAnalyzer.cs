// DeepTools.GUI.ImageAnalyzer
using DeepCore;
using DeepCore.GUI.Cell;
using DeepCore.IO;
using DeepCore.Log;
using DeepTools.GUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace DeepTools.GUI
{
    public class CPJAnalyzer
    {
        private static readonly Logger log = LoggerFactory.GetLogger(nameof(CPJAnalyzer));
        private const string cpj_ext = ".cpj";
        private const string xml_ext = ".xml";
        private const string cpj_out = "output";

        private readonly List<AtlasInfo> infos = new List<AtlasInfo>();
        private readonly DirectoryInfo root;
        public CPJAnalyzer(DirectoryInfo dir)
        {
            this.root = dir;
        }
        public void Run()
        {
            this.RunDir(this.root);
        }
        protected void RunDir(DirectoryInfo dir)
        {
            foreach (var cpj in dir.GetFiles())
            {
                if (cpj.Extension.StringEqualsIgnoreCase(cpj_ext))
                {
                    var outxml = new FileInfo(cpj.Directory.FullName +
                        Path.DirectorySeparatorChar + cpj_out + Path.DirectorySeparatorChar +
                        Path.GetFileNameWithoutExtension(cpj.FullName) + xml_ext);
                    if (outxml.Exists)
                    {
                        this.RunSingle(outxml);
                    }
                }
            }
            foreach (var sub in dir.GetDirectories())
            {
                this.RunDir(sub);
            }
        }

        protected void RunSingle(FileInfo outxml)
        {
            try
            {
                log.Info(outxml.FullName);
                var cpjres = CPJResource.CreateResource(outxml.FullName);
                foreach (var img in cpjres.Loader.ImgTable)
                {
                    var atlas = cpjres.GetAtlas(img.Key);
                    if (atlas != null)
                    {
                        var info = new AtlasInfo(cpjres, atlas);
                        infos.Add(info);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
        }
        public void WriteInfo(FileInfo output, Comparison<AtlasInfo> comparer = null)
        {
            using (var sb = new StringWriter())
            {
                const int columns = 10;
                sb.WriteLine(CUtils.ArrayToString(new string[columns]
                {
                    "CPJ文件",
                    "图集名",
                    "有效像素尺寸",
                    "有效像素内存(bytes)",
                    "ASTC使用率%",
                    "ASTC尺寸",
                    "ASTC内存(bytes)",
                    "PVR使用率%",
                    "PVR尺寸",
                    "PVR内存(bytes)"
                }));
                var list = new List<AtlasInfo>(infos);
                if (comparer != null) { list.Sort(comparer); }
                else { list.Sort((a, b) => CMath.GetDirect(a.UsagePow2 - b.UsagePow2)); }
                foreach (var info in list)
                {
                    sb.WriteLine(CUtils.ArrayToString(new object[columns]
                    {
                        this.root.GetSuffixPath(info.CPJFile),
                        info.Meta.Name,
                        info.Size.ToString("{0}x{1}"),
                        info.BytesPixels,
                        info.UsagePow2.ToString("F0"),
                        info.SizePow2.ToString("{0}x{1}"),
                        info.BytesPow2,
                        info.UsageSquare.ToString("F0"),
                        info.SizeSquare.ToString("{0}x{1}"),
                        info.BytesSquare
                    }));
                }
                File.WriteAllText(output.FullName, sb.ToString(), CUtils.UTF8_BOM);
            }
        }

        public class AtlasInfo
        {
            public FileInfo CPJFile { get; }
            public ImagesSet Meta { get; }
            public int TileCount { get; }
            public long Pixels { get; }
            public Size Size { get; }
            public Size SizePow2 { get; }
            public Size SizeSquare { get; }
            public long BytesPixels { get; }
            public long BytesPow2 { get; }
            public long BytesSquare { get; }
            public float UsagePow2 { get; }
            public float UsageSquare { get; }

            public AtlasInfo(CPJResource res, CPJAtlas atlas)
            {
                this.CPJFile = new FileInfo(res.Loader.FileName);
                this.Meta = atlas.ImagesSet;
                this.TileCount = Meta.Count;
                this.Pixels = 0;
                for (var tid = 0; tid < Meta.Count; tid++)
                {
                    if (Meta.TryGetClip(tid, out var tx, out var ty, out var tw, out var th, out var tk))
                    {
                        this.Pixels += tw * th;
                    }
                }
                this.BytesPixels = this.Pixels * 4; ;
                if (atlas is CPJAtlasGroup group)
                {
                    this.Size = new Size(group.Src.Width, group.Src.Height);
                    this.SizePow2 = this.Size.CalcTextureSize(true, false);
                    this.SizeSquare = this.Size.CalcTextureSize(true, true);
                    this.BytesPow2 = this.Size.CalcTextureBytes(true, false);
                    this.BytesSquare = this.Size.CalcTextureBytes(true, true);
                }
                else if (atlas is CPJAtlasTiles tiles)
                {
                    this.BytesPow2 = 0;
                    this.BytesSquare = 0;
                    for (var tid = 0; tid < Meta.Count; tid++)
                    {
                        this.Size = Size.Empty;
                        this.SizePow2 = Size.Empty;
                        this.SizeSquare = Size.Empty;
                        if (Meta.TryGetClip(tid, out var tx, out var ty, out var tw, out var th, out var tk))
                        {
                            this.BytesPow2 += new Size(tw, th).CalcTextureBytes(true, false);
                            this.BytesSquare += new Size(tw, th).CalcTextureBytes(true, false);
                        }
                    }
                }
                else
                {
                    throw new Exception("Not support atlas type : " + atlas.GetType().FullName);
                }
                this.UsagePow2 = 100f * (float)this.BytesPixels / (float)this.BytesPow2;
                this.UsageSquare = 100f * (float)this.BytesPixels / (float)this.BytesSquare;
            }
        }


    }
}
