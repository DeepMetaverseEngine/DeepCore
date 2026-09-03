using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DeepCore.IO;
using DeepCore;

namespace DeepTools.CodeGen
{
    public static class GenMD5
    {
        public static string GetProjectCodeMD5Lines(DirectoryInfo projDir, params string[] exts)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var file in CFiles.ListAllFiles(projDir, fi => Array.IndexOf(exts, fi.Extension.ToLower()) >= 0))
            {
                sb.AppendLine(CMD5.CalculateMD5(file) + " : " + projDir.GetSuffixPath(file));
            }
            return sb.ToString();
        }
        public static string GetProjectCodeMD5(DirectoryInfo projDir, params string[] exts)
        {
            var lines = GetProjectCodeMD5Lines(projDir, exts);
            return CMD5.CalculateMD5(lines);
        }
    }
}
