using DeepCore;
using DeepCore.IO;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DeepTools.NuGetIncrement
{
    public class Program
    {
        public static string Usage
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine("nuinc [InputFile] or [InputDirectory]");
                sb.AppendLine("    -ext      Project file extension with [InputDirectory].");
                sb.AppendLine("    -upgrade  Force upgrade version number.");
                sb.AppendLine("    -force    Force set version number.");
                sb.AppendLine("Example:");
                sb.AppendLine("nuinc test.csproj -force=1.2.1");
                sb.AppendLine("nuinc solutionDirectory -ext=.nuspec");
                return sb.ToString();
            }
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            try
            {
                var pargs = DeepCore.Properties.ParseArgs(args);
                if (args.Length == 0)
                {
                    Console.WriteLine(Usage);
                    return;
                }
                else
                {
                    var ext = pargs.Get("-ext");
                    var nugrade = new NuGetVersionUpgrader(pargs)
                    {
                    };
                    if (File.Exists(args[0]))
                    {
                        var file = new FileInfo(args[0]);
                        nugrade.Increment(file);
                    }
                    else if (Directory.Exists(args[0]))
                    {
                        var dir = new DirectoryInfo(args[0]);
                        nugrade.IncrementAll(dir, ext);
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message + Environment.NewLine + err.StackTrace);
                Console.WriteLine(Usage);
                Environment.ExitCode = -1;
            }
        }



    }

    public class NuGetVersionUpgrader
    {
        public const string CSPROJ = ".csproj";
        public const string NUSPEC = ".nuspec";
        public const string MD5_IGNORE = ".md5ignore";

        public string ForceVersion = null;
        public bool ForceUpgrade = false;

        public NuGetVersionUpgrader(Properties pargs)
        {
            var upgrade = pargs.GetAsBool("-upgrade");
            var force = pargs.Get("-force");
            this.ForceVersion = force;
            this.ForceUpgrade = upgrade;
        }

        public int IncrementAll(string root, string extension = CSPROJ)
        {
            return IncrementAll(new DirectoryInfo(root), extension);
        }
        public bool Increment(string file)
        {
            return Increment(new FileInfo(file));
        }

        public int IncrementAll(DirectoryInfo root, string extension = CSPROJ)
        {
            extension = extension ?? CSPROJ;
            int count = 0;
            var projects = CFiles.ListAllFiles(root, (f) => f.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
            foreach (var project in projects)
            {
                if (Increment(project))
                {
                    count++;
                }
            }
            return count;
        }
        public virtual bool Increment(FileInfo file)
        {
            if (File.Exists(file.FullName + NUSPEC))
            {
                Increment(new FileInfo(file.FullName + NUSPEC));
            }
            try
            {
                var groupName = "PropertyGroup";
                var versionName = "Version";
                if (file.Extension.Equals(NUSPEC, StringComparison.OrdinalIgnoreCase))
                {
                    groupName = "metadata";
                    versionName = "version";
                }
                else if (file.Extension.Equals(CSPROJ, StringComparison.OrdinalIgnoreCase))
                {
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
                var input = Resource.LoadAllText(file.FullName);
                var doc = XmlUtil.FromString(input);
                if (IsPackageProject(doc))
                {
                    var ep = doc.DocumentElement.FindChild<XmlElement>(groupName, true);
                    var ev = doc.DocumentElement.FindChild<XmlElement>(versionName, true);
                    if (ev != null && ev.ParentNode == ep)
                    {
                        if (!string.IsNullOrEmpty(ForceVersion))
                        {
                            ev.InnerText = ForceVersion;
                            Console.WriteLine(file.FullName + " -> Force Set : " + ev.InnerText);
                        }
                        else if (ForceUpgrade)
                        {
                            ev.InnerText = IncrementVersion(ev.InnerText);
                            Console.WriteLine(file.FullName + " -> Force Upgrade : " + ev.InnerText);
                        }
                        else if (IsSameMD5(file.Directory))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(file.FullName + " -> Keep : " + ev.InnerText);
                            return true;
                        }
                        else
                        {
                            ev.InnerText = IncrementVersion(ev.InnerText);
                            Console.WriteLine(file.FullName + " -> Upgrade : " + ev.InnerText);
                        }
                    }
                    else if (ep != null)
                    {
                        ev = doc.CreateElement(versionName);
                        ev.InnerText = ForceVersion ?? "1.0.1";
                        ep.AppendChild(ev);
                        Console.WriteLine(file.FullName + " -> Default : " + ev.InnerText);
                    }
                    var set = new XmlWriterSettings();
                    set.Indent = true;
                    set.Encoding = CUtils.UTF8_BOM;
                    set.OmitXmlDeclaration = true;
                    var output = doc.ToXmlString(set);
                    File.WriteAllText(file.FullName, output);
                    File.WriteAllBytes(file.Directory.FullName + @"\.md5", GetProjectCodeMD5(file.Directory));
                }
                return true;
            }
            catch (Exception err)
            {
                err.PrintStackTrace(file.FullName);
                return false;
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static string IncrementVersion(string input)
        {
            var version = input.Split('.');
            if (Parser.TryParseInt(version[2], out var patch))
            {
                patch += 1;
                version[2] = patch.ToString();
            }
            else
            {
                version[2] = version[2] + "1";
            }
            return CUtils.ArrayToString(version, ".");
        }

        public static bool IsPackageProject(XmlDocument doc)
        {
            //   <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
            bool isPackageProject = false;
            doc.DocumentElement.ForEachChilds(e =>
            {
                if (e is XmlElement element)
                {
                    if (element.Name == "GeneratePackageOnBuild" && bool.TryParse(element.InnerText, out var gen) && gen)
                    {
                        isPackageProject = true;
                        return true;
                    }
                }
                return false;
            }, true);
            return isPackageProject;
        }

        public static bool IsSameMD5(DirectoryInfo projDir)
        {
            var newBin = GetProjectCodeMD5(projDir);
            var md5_file = projDir.FullName + @"\.md5";
            if (File.Exists(md5_file))
            {
                var oldBin = File.ReadAllBytes(md5_file);
                if (CUtils.ArraysEqual(newBin, oldBin))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static byte[] GetProjectCodeMD5(DirectoryInfo projDir)
        {
            var sb = new StringBuilder();
            var list = new List<FileInfo>();
            if (File.Exists(Path.Combine(projDir.FullName, MD5_IGNORE)))
            {
                var ignore = File.ReadAllText(Path.Combine(projDir.FullName, MD5_IGNORE));
                var filter = new FileFilters(ignore);
                CFiles.ListAllFiles(list, projDir, filter);
            }
            else
            {
                CFiles.ListAllFiles(list, projDir);
            }
            foreach (var file in list)
            {
                switch (file.Extension.ToLower())
                {
                    //case ".nuspec":
                    //case ".csproj":
                    case ".user":
                    case ".md5":
                    case ".dll":
                    case ".exe":
                    case ".pdb":
                    case ".nupkg":
                    case ".deps.json":
                        break;
                    default:
                        {
                            var sub = file.FullName.Substring(projDir.FullName.Length);
                            if (!sub.StartsWith(@"\bin\") && !sub.StartsWith(@"\obj\"))
                            {
                                sb.AppendLine(CMD5.CalculateMD5(file) + " : " + sub);
                            }
                        }
                        break;
                }
            }
            return CUtils.UTF8_BOM.GetBytes(sb.ToString());
        }


        public static int MoveAll(string root, string target, string extension = ".nupkg")
        {
            return MoveAll(new DirectoryInfo(root), new DirectoryInfo(target), extension);
        }
        public static int MoveAll(DirectoryInfo root, DirectoryInfo target, string extension = ".nupkg")
        {
            int count = 0;
            var projects = CFiles.ListAllFiles(root, (f) => f.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
            foreach (var project in projects)
            {
                if (Move(project, target))
                {
                    count++;
                }
            }
            return count;
        }
        public static bool Move(FileInfo nupkg, DirectoryInfo target)
        {
            try
            {
                File.Copy(nupkg.FullName, Path.Combine(target.FullName, nupkg.Name), true);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(nupkg.FullName + " -> " + target.Name);
                File.Delete(nupkg.FullName);
                return true;
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
                return false;
            }
            finally
            {
                Console.ResetColor();
            }

        }

    }
}

