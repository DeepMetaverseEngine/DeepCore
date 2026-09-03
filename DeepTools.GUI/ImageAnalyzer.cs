// DeepTools.GUI.ImageAnalyzer
using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepTools.GUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DeepTools.GUI
{
    public class ImageAnalyzer
    {
        private static readonly Logger log = LoggerFactory.GetLogger(nameof(ImageAnalyzer));
        private readonly string[] extensions = new string[2] { ".png", ".jpg" };
        private readonly List<TextureInfo> infos = new List<TextureInfo>();
        private readonly DirectoryInfo root;
        public ImageAnalyzer(DirectoryInfo dir, string exts)
        {
            this.root = dir;
            if (exts != null)
            {
                string[] ext = exts.Split(',');
                this.extensions = Array.ConvertAll(ext, (string e) => e.ToLower());
            }
        }

        public void Run()
        {
            this.RunDir(this.root);
        }

        protected void RunDir(DirectoryInfo dir)
        {
            foreach (var png in dir.GetFiles())
            {
                if (this.extensions.Contains(png.Extension.ToLower()))
                {
                    this.RunSingle(png);
                }
            }
            foreach (var sub in dir.GetDirectories())
            {
                this.RunDir(sub);
            }
        }

        protected void RunSingle(FileInfo pngFile)
        {
            try
            {
                var info = new TextureInfo(pngFile);
                this.infos.Add(info);
            }
            catch (Exception err)
            {
                ImageAnalyzer.log.Error(err);
            }
        }

        public void WriteInfo(FileInfo output, Comparison<TextureInfo> comparer = null)
        {
            using (var sb = StringBuilderObjectPool.AllocAutoRelease())
            {
                sb.WriteLine(CUtils.ArrayToString(new string[9]
                {
                    "文件", 
                    "ASTC使用率%", 
                    "PVR使用率%",
                    "原尺寸",
                    "ASTC尺寸", 
                    "PVR尺寸",
                    "原内存(bytes)",
                    "ASTC内存(bytes)", 
                    "PVR内存(bytes)"
                }));
                var list = new List<TextureInfo>(infos);
                if (comparer != null) { list.Sort(comparer); }
                else { list.Sort((a, b) => CMath.GetDirect(a.UsagePow2 - b.UsagePow2)); }
                foreach (var info in list)
                {
                    sb.WriteLine(CUtils.ArrayToString(new object[9]
                    {
                        this.root.GetSuffixPath(info.File),
                        info.UsagePow2.ToString("F0"),
                        info.UsageSquare.ToString("F0"),
                        info.Size.ToString("{0}x{1}"),
                        info.SizePow2.ToString("{0}x{1}"),
                        info.SizeSquare.ToString("{0}x{1}"),
                        (info.Bytes),
                        (info.BytesPow2),
                        (info.BytesSquare)
                    }));
                }
                Console.WriteLine(sb.ToString());
                File.WriteAllText(output.FullName, sb.ToString(), CUtils.UTF8_BOM);
            }
        }
        public class TextureInfo
        {
            public FileInfo File { get; }
            public Size Size { get; }
            public Size SizePow2 { get; }
            public Size SizeSquare { get; }
            public long Bytes { get; }
            public long BytesPow2 { get; }
            public long BytesSquare { get; }
            public float UsagePow2 { get; }
            public float UsageSquare { get; }
            public TextureInfo(FileInfo file)
            {
                Image t = Image.FromFile(file.FullName);
                this.File = file;
                this.Size = t.Size;
                this.SizePow2 = this.Size.CalcTextureSize(true, false);
                this.SizeSquare = this.Size.CalcTextureSize(true, true);
                this.Bytes = this.Size.CalcTextureBytes(false, false);
                this.BytesPow2 = this.Size.CalcTextureBytes(true, false);
                this.BytesSquare = this.Size.CalcTextureBytes(true, true);
                this.UsagePow2 = 100f * (float)this.Bytes / (float)this.BytesPow2;
                this.UsageSquare = 100f * (float)this.Bytes / (float)this.BytesSquare;
            }
        }

    }
}
