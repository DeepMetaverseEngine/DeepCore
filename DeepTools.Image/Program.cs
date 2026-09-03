using DeepCore;
using DeepCore.IO;
using DeepEditor.Common;
using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace DeepTools.Image
{
    class Program
    {
        public static string Usage
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine("premultiply  -i:<InputFile>  -o:<OutputFile>  [-b:<BackColor ARGB>]");
                sb.AppendLine("premultiply  -id:<InputDir>  -od:<OutputDir>  -e:<ImageExtension>  [-b:<BackColor ARGB>]");
                return sb.ToString();
            }
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine(Usage);
                    return;
                }
                else if ("premultiply" == args[0])
                {
                    premultiply(args);
                }
                else
                {
                    Console.WriteLine(Usage);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message + Environment.NewLine + err.StackTrace);
                Console.WriteLine(Usage);
                Environment.ExitCode = -1;
            }
        }

        private static void premultiply(string[] args)
        {
            var prop = Properties.ParseArgs(args, ":");
            var backColor = Color.FromArgb(0, 0, 0, 0);
            if (prop.TryGetValue("-b", out var back))
            {
                var argb = Parser.StringToObject<int>(back);
                backColor = Color.FromArgb(argb);
            }
            if (prop.ContainsKey("-i") && prop.ContainsKey("-o"))
            {
                premultiplyFile(new FileInfo(prop.Get("-i")), new FileInfo(prop.Get("-o")), backColor);
            }
            else if (prop.ContainsKey("-id") && prop.ContainsKey("-od") && prop.ContainsKey("-e"))
            {
                premultiplyDir(new DirectoryInfo(prop.Get("-id")), new DirectoryInfo(prop.Get("-od")), prop.Get("-e"), backColor);
            }
        }

        private static void premultiplyFile(FileInfo input, FileInfo output, Color bc)
        {
            var src = ImageUtils.AsBitmap(System.Drawing.Image.FromFile(input.FullName));
            var dst_stream = new DeepCore.IO.MemoryStream();
            var dst = ImageUtils.PremultiplyAlpha(src, bc);
            dst.Save(dst_stream, ImageUtils.GetImageFormat(output.Extension));
            var dst_data = dst_stream.ToArray();
            if (output.Exists)
            {
                var old_data = File.ReadAllBytes(output.FullName);
                if (CUtils.ArraysEqual(old_data, dst_data))
                {
                    Console.WriteLine(input.FullName + " -> (skip)");
                    return;
                }
            }
            CFiles.CreateFile(output);
            File.WriteAllBytes(output.FullName, dst_data);
            Console.WriteLine(input.FullName + " -> " + output.Name);
        }

        private static void premultiplyDir(DirectoryInfo input, DirectoryInfo output, string extension, Color bc)
        {
            foreach (var file in CFiles.ListAllFiles(input))
            {
                if (extension.Equals(file.Extension, CUtils.StringComparisonIgnoreCase))
                {
                    var suffix = file.FullName.Substring(input.FullName.Length);
                    var outfile = new FileInfo(output.FullName + Path.DirectorySeparatorChar + suffix);
                    premultiplyFile(file, outfile, bc);
                }
            }
        }
    }
}
