using DeepCore;
using DeepCore.IO;
using DeepCore.Xml;
using DeepTools.CodeGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static DeepCore.Colors;

namespace DeepTools.LanguageXLS
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            try
            {
                XLSLang.Gen(args, OnError);
            }
            catch
            {
                Environment.ExitCode = -1;
            }
        }
        static private void OnError(Exception err)
        {
            XLSLang.log.Error(err.Message, err);
            XLSLang.log.Error("Press Any Key To Exit !!!");
            Console.In.Read();
            Environment.ExitCode = -1;
        }

    }

    public class XLSLang
    {
        static public DeepCore.Log.Logger log = new DeepCore.Log.LazyLogger("XLS Lang");
        public static void Gen(params string[] args)
        {
            Gen(args, err => err.PrintStackTrace());
        }
        public static void Gen(string[] args, Action<Exception> OnError)
        {
            if (args.Length < 3)
            {
                log.Info("\n" + Usage);
                return;
            }
            else
            {
                var prop = DeepCore.Properties.ParseArgs(args, ":");
                Gen(args[0], prop, OnError);
            }
        }
        public static void Gen(string cmd, DeepCore.Properties prop, Action<Exception> OnError)
        {
            try
            {
                var gen = new XLSGenerator(prop);
                gen.onerror += OnError;
                if (cmd == "lang")
                {
                    gen.LangProcessXLS();
                }
                else if (cmd == "lua")
                {
                    gen.LuaProcessXLS();
                }
                else if (cmd == "json")
                {
                    prop["-oext"] = ".json";
                    prop["-templ"] = "xls-json-code.xml";
                    //                     prop["-array_L"] = "[";
                    //                     prop["-array_R"] = "]";
                    //                     prop["-array_SP"] = ",";
                    //                     prop["-combine_L"] = "{";
                    //                     prop["-combine_R"] = "}";
                    //                     prop["-combine_EQ"] = ":";
                    //                     prop["-combine_SP"] = ",";
                    gen.LuaProcessXLS();
                }
                else if (cmd == "local")
                {
                    gen.LocalProcessXLS();
                }
                else if (cmd == "md5")
                {
                    gen.ProcessMD5();
                }
                else
                {
                    gen.LuaProcessXLS();
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                log.Error("\n" + Usage);
                throw;
            }
        }

        /// <summary>
        /// xlslang lua -id:.\xls -od:.\lua "-filter_text:-~"
        /// </summary>
        /// <returns></returns>
        public static void BuildSimple_XLS2LUA(DirectoryInfo inputDir, DirectoryInfo outputDir, string filterText = "")
        {
            {
                var gen = new XLSGenerator(DeepCore.Properties.ParseArgs(new string[] {
                    $"-id:{inputDir.FullName}",
                    $"-od:{outputDir.FullName}",
                    $"-filter_text:-~;{filterText}",
                }, ":"));
                gen.onerror += err => log.Error(err);
                gen.LuaProcessXLS();
            }
            {
                var gen = new XLSGenerator(DeepCore.Properties.ParseArgs(new string[] {
                    $"-id:{inputDir.FullName}",
                    $"-od:{outputDir.FullName}",
                    $"-filter_text:-~;{filterText}",
                }, ":"));
                gen.onerror += err => log.Error(err);
                gen.LuaProcessXLS();
            }
        }

        public static string Usage
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine("xlslang lang  -id:InputDirectory -of:OutputFile [可选参数...]");
                sb.AppendLine("xlslang lua   -id:InputDirectory -od:OutputDirectory [可选参数...]");
                sb.AppendLine("xlslang local -if:InputFile      -od:OutputDirectory [可选参数...]");
                sb.AppendLine("xlslang md5   -id:InputDirectory -of:OutputFile [可选参数...]");
                sb.AppendLine();
                sb.AppendLine(XLSLoader.Usage);
                return sb.ToString();
            }

        }
    }
}
