using DeepCore.GUI.Data;
using System;
using System.IO;
using System.Text;

namespace DeepTools.GUI
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            //new Win32Driver();
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "xml2bin":
                    case "gui2bin":
                        ConvertXml2Bin(args);
                        return;
                    case "cpj2bin":
                        ConvertCpj2Bin(args);
                        return;
                    case "cpjtest":
                        TestCPJ(args);
                        return;
//                     case "img_usage":
//                         UsageImg(args);
//                         return;
//                     case "cpj_usage":
//                         UsageCPJ(args);
//                         return;
                }
            }
            Console.WriteLine(Usage);
        }
        public static string Usage
        {
            get
            {
                StringBuilder sb = new StringBuilder("UI工具集");
                sb.AppendLine("1. UI编辑器Xml转换Bin");
                sb.AppendLine("   gui  xml2bin  <输入文件夹>");
                sb.AppendLine("   gui  gui2bin  <输入文件夹>");
                sb.AppendLine("2. CPJ编辑器Xml转换Bin");
                sb.AppendLine("   gui  cpj2bin  <输入文件夹>");
                sb.AppendLine("3. 测试CPJ");
                sb.AppendLine("   gui  cpjtest  <输入文件夹>");
                sb.AppendLine("4. 分析图片");
                sb.AppendLine("   gui  img_usage  -in:<输入文件夹> -out:<输出文件.csv> [-ext:图片后缀(.png,.jpg)]");
                sb.AppendLine("4. 分析CPJ");
                sb.AppendLine("   gui  cpj_usage  -in:<输入文件夹> -out:<输出文件.csv>");
                return sb.ToString();
            }
        }

        private static void ConvertXml2Bin(string[] args)
        {
            string root = ".";
            if (args.Length > 1 && Directory.Exists(args[1]))
            {
                root = args[1];
            }
            new UIEditorConverter().ConvertXML(new DirectoryInfo(root));
        }
        private static void ConvertCpj2Bin(string[] args)
        {
            string root = ".";
            if (args.Length > 1 && Directory.Exists(args[1]))
            {
                root = args[1];
            }
            new UIEditorConverter().ConvertCPJ(new DirectoryInfo(root));
        }
        private static void TestCPJ(string[] args)
        {
            string root = ".";
            if (args.Length > 1 && Directory.Exists(args[1]))
            {
                root = args[1];
            }
            new UIEditorConverter().TestCPJ(new DirectoryInfo(root));
        }
//         private static void UsageImg(string[] args)
//         {
//             var prop = DeepCore.Properties.ParseArgs(args, ":");
//             var root = prop.Get("-in") ?? (Environment.CurrentDirectory);
//             var outf = prop.Get("-out") ?? (Environment.CurrentDirectory + Path.DirectorySeparatorChar + "usage.csv");
//             var extf = prop.Get("-ext") ?? (".png,.jpg");
//             var ana = new ImageAnalyzer(new DirectoryInfo(root), extf);
//             ana.Run();
//             ana.WriteInfo(new FileInfo(outf));
//         }
//         private static void UsageCPJ(string[] args)
//         {
//             var prop = DeepCore.Properties.ParseArgs(args, ":");
//             var root = prop.Get("-in") ?? (Environment.CurrentDirectory);
//             var outf = prop.Get("-out") ?? (Environment.CurrentDirectory + Path.DirectorySeparatorChar + "usage.csv");
//             var ana = new CPJAnalyzer(new DirectoryInfo(root));
//             ana.Run();
//             ana.WriteInfo(new FileInfo(outf));
//         }
    }
}
