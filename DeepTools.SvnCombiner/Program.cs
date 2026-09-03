using DeepCore;
using DeepCore.Xml;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DeepTools.SvnCombiner
{
    public class Program
    {
        public static string Usage
        {
            get { return "[-i inputdir] [-o outputfile] [-f filter]"; }
        }
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine(Usage);
                    return;
                }
                else
                {
                    DirectoryInfo inputdir;
                    FileInfo outputfile;
                    FileInfo filter;
                    DoArgs(args, out inputdir, out outputfile, out filter);
                    if (inputdir != null && outputfile != null)
                    {
                        var exec = new SvnAuthzCombine(inputdir, outputfile);
                        if (filter != null) exec.SetFilter(File.ReadAllText(filter.FullName));
                        exec.Combine();
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                Console.WriteLine(Usage);
                Environment.ExitCode = -1;
            }
        }

        private static void DoArgs(string[] args, out DirectoryInfo inputdir, out FileInfo outputfile, out FileInfo filter)
        {
            inputdir = null;
            outputfile = null;
            filter = null;
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLower();
                if (arg.StartsWith("-") && i < args.Length - 1)
                {
                    switch (arg)
                    {
                        case "-i":
                            inputdir = new DirectoryInfo(args[i + 1]);
                            break;
                        case "-o":
                            outputfile = new FileInfo(args[i + 1]);
                            break;
                        case "-f":
                            filter = new FileInfo(args[i + 1]);
                            break;
                    }
                }
            }
        }

    }

}
