using DeepCore.Concurrent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeepCore.IO
{
    public static class CFiles
    {
        public static Encoding UTF8 => CUtils.UTF8;
        public static Encoding UTF8_BOM => CUtils.UTF8_BOM;
        //---------------------------------------------------------------------------------------------------------------------
        //         #region Obsolete
        // 
        //         [Obsolete]
        //         public static List<FileInfo> listAllFiles(DirectoryInfo dir, Filter filter = null)
        //         {
        //             return ListAllFiles(dir, filter);
        //         }
        // 
        //         [Obsolete]
        //         public static void listAllFiles(List<FileInfo> list, DirectoryInfo dir, Filter filter = null)
        //         {
        //             ListAllFiles(list, dir, filter);
        //         }
        //         [Obsolete]
        //         public static void createFile(FileInfo file)
        //         {
        //             CreateFile(file);
        //         }
        //         [Obsolete]
        //         public static void createDir(DirectoryInfo dir)
        //         {
        //             CreateDir(dir);
        //         }
        //         [Obsolete]
        //         public static bool fileEquals(FileInfo a, FileInfo b)
        //         {
        //             return FileEquals(a, b);
        //         }
        // 
        //         #endregion
        //---------------------------------------------------------------------------------------------------------------------

        public static string ReplaceSpecialChars(string name, char @override = '-')
        {
            name = name.Replace('/', @override);
            name = name.Replace('\\', @override);
            name = name.Replace(':', @override);
            name = name.Replace('*', @override);
            name = name.Replace('?', @override);
            name = name.Replace('"', @override);
            name = name.Replace('|', @override);
            return name;
        }
        public delegate bool FileFilter(FileInfo src);
        public delegate bool DirectoryFilter(DirectoryInfo src);

        public static List<FileInfo> ListAllFiles(DirectoryInfo dir, string[] fileNames, bool hierarchy = true)
        {
            List<FileInfo> list = new List<FileInfo>();
            ListAllFiles(list, dir, f => Array.Exists(fileNames, n => n.ToLower() == f.Name.ToLower()), hierarchy);
            return list;
        }
        public static List<FileInfo> ListAllFiles(string dir, string[] fileNames, bool hierarchy = true)
        {
            List<FileInfo> list = new List<FileInfo>();
            ListAllFiles(list, new DirectoryInfo(dir), f => Array.Exists(fileNames, n => n.ToLower() == f.Name.ToLower()), hierarchy);
            return list;
        }
        public static List<FileInfo> ListAllFiles(DirectoryInfo dir, bool hierarchy = true)
        {
            List<FileInfo> list = new List<FileInfo>();
            ListAllFiles(list, dir, hierarchy);
            return list;
        }
        public static List<FileInfo> ListAllFiles(string dir, bool hierarchy = true)
        {
            List<FileInfo> list = new List<FileInfo>();
            ListAllFiles(list, new DirectoryInfo(dir), hierarchy);
            return list;
        }
        public static void ListAllFiles(List<FileInfo> list, DirectoryInfo dir, bool hierarchy = true)
        {
            if (dir.Exists)
            {
                foreach (FileInfo sub in dir.GetFiles())
                {
                    list.Add(sub);
                }
                if (hierarchy)
                {
                    foreach (DirectoryInfo sub in dir.GetDirectories())
                    {
                        ListAllFiles(list, sub, hierarchy);
                    }
                }
            }
        }
        public static List<FileInfo> ListAllFiles(DirectoryInfo dir, FileFilter filter, bool hierarchy = true)
        {
            List<FileInfo> list = new List<FileInfo>();
            ListAllFiles(list, dir, filter, hierarchy);
            return list;
        }
        public static List<FileInfo> ListAllFiles(string dir, FileFilter filter, bool hierarchy = true)
        {
            List<FileInfo> list = new List<FileInfo>();
            ListAllFiles(list, new DirectoryInfo(dir), filter, hierarchy);
            return list;
        }
        public static void ListAllFiles(List<FileInfo> list, DirectoryInfo dir, FileFilter filter, bool hierarchy = true)
        {
            if (dir.Exists)
            {
                foreach (FileInfo sub in dir.GetFiles())
                {
                    if (filter == null || filter.Invoke(sub))
                    {
                        list.Add(sub);
                    }
                }
                if (hierarchy)
                {
                    foreach (DirectoryInfo sub in dir.GetDirectories())
                    {
                        ListAllFiles(list, sub, filter, hierarchy);
                    }
                }
            }
        }
        public static void ListAllFiles(List<FileInfo> list, string dir, FileFilter filter, bool hierarchy = true)
        {
            ListAllFiles(list, new DirectoryInfo(dir), filter, hierarchy);
        }
        public static List<FileInfo> ListAllFiles(DirectoryInfo dir, FileFilters filter, bool hierarchy = true)
        {
            List<FileInfo> list = new List<FileInfo>();
            ListAllFiles(list, dir, filter, hierarchy);
            return list;
        }
        public static List<FileInfo> ListAllFiles(string dir, FileFilters filter, bool hierarchy = true)
        {
            List<FileInfo> list = new List<FileInfo>();
            ListAllFiles(list, new DirectoryInfo(dir), filter, hierarchy);
            return list;
        }
        public static void ListAllFiles(List<FileInfo> list, DirectoryInfo dir, FileFilters filter, bool hierarchy = true)
        {
            if (dir.Exists)
            {
                foreach (FileInfo sub in dir.GetFiles())
                {
                    if (filter == null || filter.AcceptFile(sub))
                    {
                        list.Add(sub);
                    }
                }
                if (hierarchy)
                {
                    foreach (DirectoryInfo sub in dir.GetDirectories())
                    {
                        ListAllFiles(list, sub, filter, hierarchy);
                    }
                }
            }
        }
        public static void ListAllFiles(List<FileInfo> list, string dir, FileFilters filter, bool hierarchy = true)
        {
            ListAllFiles(list, new DirectoryInfo(dir), filter, hierarchy);
        }

        //----------------------------------------------------------------------------------------------------------------------------------

        public static FileInfo FindFile(this DirectoryInfo dir, FileFilter find, bool hierarchy = true)
        {
            if (dir.Exists)
            {
                {
                    foreach (var sub in dir.GetFiles())
                    {
                        if (find(sub))
                        {
                            return sub;
                        }
                    }
                }
                if (hierarchy)
                {
                    foreach (var sub in dir.GetDirectories())
                    {
                        if (FindFile(sub, find, hierarchy) is FileInfo ret)
                        {
                            return ret;
                        }
                    }
                }
            }
            return null;
        }
        public static DirectoryInfo FindDirectory(this DirectoryInfo dir, DirectoryFilter find, bool hierarchy = true)
        {
            if (dir.Exists)
            {
                {
                    foreach (var sub in dir.GetDirectories())
                    {
                        if (find(sub))
                        {
                            return sub;
                        }
                    }
                }
                if (hierarchy)
                {
                    foreach (var sub in dir.GetDirectories())
                    {
                        if (FindDirectory(sub, find, hierarchy) is DirectoryInfo ret)
                        {
                            return ret;
                        }
                    }
                }
            }
            return null;
        }
        //----------------------------------------------------------------------------------------------------------------------------------
        //         public static void WriteAllBytes(this IExternalizableFactory factory, string path, object data)
        //         {
        //             CreateFile(path);
        //             using (var ms = new MemoryStream())
        //             using (var os = new OutputStream(ms, factory))
        //             {
        //                 os.PutObj(data);
        //                 var bytes = ms.ToArray();
        //                 File.WriteAllBytes(path, bytes);
        //             }
        //         }
        public static void WriteAllBytes(string path, byte[] bytes) { CreateFile(path); File.WriteAllBytes(path, bytes); }
        public static void WriteAllLines(string path, IEnumerable<string> contents) { CreateFile(path); File.WriteAllLines(path, contents); }
        public static void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding) { CreateFile(path); File.WriteAllLines(path, contents, encoding); }
        public static void WriteAllLines(string path, string[] contents) { CreateFile(path); File.WriteAllLines(path, contents); }
        public static void WriteAllLines(string path, string[] contents, Encoding encoding) { CreateFile(path); File.WriteAllLines(path, contents, encoding); }
        public static void WriteAllText(string path, string contents) { CreateFile(path); File.WriteAllText(path, contents); }
        public static void WriteAllText(string path, string contents, Encoding encoding) { CreateFile(path); File.WriteAllText(path, contents, encoding); }
        public static void WriteAllBytes(FileInfo path, byte[] bytes) { CreateFile(path); File.WriteAllBytes(path.FullName, bytes); }
        public static void WriteAllLines(FileInfo path, IEnumerable<string> contents) { CreateFile(path); File.WriteAllLines(path.FullName, contents); }
        public static void WriteAllLines(FileInfo path, IEnumerable<string> contents, Encoding encoding) { CreateFile(path); File.WriteAllLines(path.FullName, contents, encoding); }
        public static void WriteAllLines(FileInfo path, string[] contents) { CreateFile(path); File.WriteAllLines(path.FullName, contents); }
        public static void WriteAllLines(FileInfo path, string[] contents, Encoding encoding) { CreateFile(path); File.WriteAllLines(path.FullName, contents, encoding); }
        public static void WriteAllText(FileInfo path, string contents) { CreateFile(path); File.WriteAllText(path.FullName, contents); }
        public static void WriteAllText(FileInfo path, string contents, Encoding encoding) { CreateFile(path); File.WriteAllText(path.FullName, contents, encoding); }

        //----------------------------------------------------------------------------------------------------------------------------------
        public static async Task WriteAllBytesAsync(string path, byte[] bytes) { CreateFile(path); await File.WriteAllBytesAsync(path, bytes); }
        public static async Task WriteAllLinesAsync(string path, IEnumerable<string> contents) { CreateFile(path); await File.WriteAllLinesAsync(path, contents); }
        public static async Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding) { CreateFile(path); await File.WriteAllLinesAsync(path, contents, encoding); }
        public static async Task WriteAllLinesAsync(string path, string[] contents) { CreateFile(path); await File.WriteAllLinesAsync(path, contents); }
        public static async Task WriteAllLinesAsync(string path, string[] contents, Encoding encoding) { CreateFile(path); await File.WriteAllLinesAsync(path, contents, encoding); }
        public static async Task WriteAllTextAsync(string path, string contents) { CreateFile(path); await File.WriteAllTextAsync(path, contents); }
        public static async Task WriteAllTextAsync(string path, string contents, Encoding encoding) { CreateFile(path); await File.WriteAllTextAsync(path, contents, encoding); }
        public static async Task WriteAllBytesAsync(FileInfo path, byte[] bytes) { CreateFile(path); await File.WriteAllBytesAsync(path.FullName, bytes); }
        public static async Task WriteAllLinesAsync(FileInfo path, IEnumerable<string> contents) { CreateFile(path); await File.WriteAllLinesAsync(path.FullName, contents); }
        public static async Task WriteAllLinesAsync(FileInfo path, IEnumerable<string> contents, Encoding encoding) { CreateFile(path); await File.WriteAllLinesAsync(path.FullName, contents, encoding); }
        public static async Task WriteAllLinesAsync(FileInfo path, string[] contents) { CreateFile(path); await File.WriteAllLinesAsync(path.FullName, contents); }
        public static async Task WriteAllLinesAsync(FileInfo path, string[] contents, Encoding encoding) { CreateFile(path); await File.WriteAllLinesAsync(path.FullName, contents, encoding); }
        public static async Task WriteAllTextAsync(FileInfo path, string contents) { CreateFile(path); await File.WriteAllTextAsync(path.FullName, contents); }
        public static async Task WriteAllTextAsync(FileInfo path, string contents, Encoding encoding) { CreateFile(path); await File.WriteAllTextAsync(path.FullName, contents, encoding); }

        //----------------------------------------------------------------------------------------------------------------------------------
        public static FileInfo CurrentSubFile(string suffix)
        {
            return new FileInfo(string.Format("{0}{1}{2}", Environment.CurrentDirectory, Path.DirectorySeparatorChar, suffix));
        }
        public static DirectoryInfo CurrentSubDir(string suffix)
        {
            return new DirectoryInfo(string.Format("{0}{1}{2}", Environment.CurrentDirectory, Path.DirectorySeparatorChar, suffix));
        }

        public static string GetSuffixPath(this DirectoryInfo root, FileInfo file, bool keepDirectorySeparatorChar = true)
        {
            var fullRoot = root.FullName;
            var fullPath = file.FullName;
            var ret = fullPath.Substring(fullRoot.Length);
            if (keepDirectorySeparatorChar == false && (ret[0] == Path.DirectorySeparatorChar))
            {
                return ret.Substring(1);
            }
            return ret;
        }
        public static string GetSuffixPath(this DirectoryInfo root, DirectoryInfo dir, bool keepDirectorySeparatorChar = true)
        {
            var fullRoot = root.FullName;
            var fullPath = dir.FullName;
            var ret = fullPath.Substring(fullRoot.Length);
            if (keepDirectorySeparatorChar == false && (ret[0] == Path.DirectorySeparatorChar))
            {
                return ret.Substring(1);
            }
            return ret;
        }
        public static FileInfo CreateFile(string file)
        {
            var _file = new FileInfo(file);
            CreateFile(_file);
            return _file;
        }
        public static DirectoryInfo CreateDir(string dir)
        {
            var _dir = new DirectoryInfo(dir);
            CreateDir(_dir);
            return _dir;
        }
        public static FileInfo CreateFile(FileInfo file)
        {
            CreateDir(file.Directory);
            return file;
        }
        public static DirectoryInfo CreateDir(DirectoryInfo dir)
        {
            var stack = new Stack<DirectoryInfo>(1);
            var _dir = dir;
            while (!_dir.Exists)
            {
                stack.Push(_dir);
                _dir = _dir.Parent;
            }
            while (stack.Count > 0)
            {
                var d = stack.Pop();
                d.Create();
            }
            return dir;
        }
        public static bool FileEquals(FileInfo a, FileInfo b)
        {
            var va = Path.GetFullPath(a.FullName);
            var vb = Path.GetFullPath(b.FullName);
            return va.Equals(vb);
        }
        public static bool FileEquals(string a, string b)
        {
            var va = Path.GetFullPath(a);
            var vb = Path.GetFullPath(b);
            return va.Equals(vb);
        }
        public static bool DirectoryEquals(DirectoryInfo a, DirectoryInfo b)
        {
            var va = Path.GetFullPath(a.FullName);
            var vb = Path.GetFullPath(b.FullName);
            return va.Equals(vb);
        }
        public static bool DirectoryEquals(string a, string b)
        {
            var va = Path.GetFullPath(a);
            var vb = Path.GetFullPath(b);
            return va.Equals(vb);
        }
        public static void FileCopy(string srcFile, string dstFile, bool overwrite)
        {
            FileInfo df = new FileInfo(dstFile);
            CreateDir(df.Directory);
            System.IO.File.Copy(srcFile, dstFile, overwrite);
        }
        public static void FileCopyTo(string srcFile, string dstDir, bool overwrite)
        {
            var df = new FileInfo(Path.Combine(dstDir, Path.GetFileName(srcFile)));
            CreateDir(df.Directory);
            System.IO.File.Copy(srcFile, df.FullName, overwrite);
        }
        public static void DirectoryCopy(string sourceDirName, string destDirName,
            FileFilter filter = null,
            bool copySubDirs = true,
            bool _override = true,
            AtomicInteger progress = null)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if (filter != null)
                {
                    if (filter(file) == false)
                    {
                        progress?.IncrementAndGet();
                        continue;
                    }
                }
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, _override);
                progress?.IncrementAndGet();
            }
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, filter, copySubDirs, _override, progress);
                }
            }
        }

        public static bool Delete(string path)
        {
            if (System.IO.Directory.Exists(path))
            {
                System.IO.Directory.Delete(path, true);
                return true;
            }
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return true;
            }
            return false;
        }
        public static bool Delete(FileInfo path)
        {
            if (path.Exists)
            {
                path.Delete();
                return true;
            }
            return false;
        }
        public static bool Delete(DirectoryInfo path)
        {
            if (path.Exists)
            {
                path.Delete(true);
                return true;
            }
            return false;
        }
        public static bool DeleteAll(string path)
        {
            if (System.IO.Directory.Exists(path))
            {
                foreach (var f in Directory.GetFiles(path))
                {
                    Delete(f);
                }
                foreach (var d in Directory.GetDirectories(path))
                {
                    Delete(d);
                }
                return true;
            }
            return false;
        }
        public static bool DeleteAll(DirectoryInfo path)
        {
            if (path.Exists)
            {
                foreach (var f in path.GetFiles())
                {
                    Delete(f);
                }
                foreach (var d in path.GetDirectories())
                {
                    Delete(d);
                }
                return true;
            }
            return false;
        }

        public static InputStream OpenInputStream(this FileInfo file, IExternalizableFactory codec = null)
        {
            return new InputStream(file.OpenRead(), codec);
        }
        public static OutputStream OpenOutputStream(this FileInfo file, IExternalizableFactory codec = null)
        {
            return new OutputStream(file.OpenWrite(), codec);
        }
        //--------------------------------------------------------------------------------------------------------------
        public static void WriteSerializable(FileInfo path, ISerializable contents, IExternalizableFactory codec)
        {
            CFiles.CreateFile(path);
            using (var output = OpenOutputStream(path, codec))
            {
                output.PutObj(contents);
            }
        }
        public static ISerializable ReadSerializable(FileInfo path, IExternalizableFactory codec)
        {
            if (path.Exists)
            {
                using (var input = OpenInputStream(path, codec))
                {
                    return input.GetSer();
                }
            }
            return null;
        }
        public static T ReadSerializable<T>(FileInfo path, IExternalizableFactory codec)
        {
            if (path.Exists)
            {
                using (var input = OpenInputStream(path, codec))
                {
                    return input.GetObj<T>();
                }
            }
            return default;
        }
        public static void WriteSerializable(string path, ISerializable contents, IExternalizableFactory codec)
            => WriteSerializable(new FileInfo(path), contents, codec);
        public static ISerializable ReadSerializable(string path, IExternalizableFactory codec)
            => ReadSerializable(new FileInfo(path), codec);
        public static T ReadSerializable<T>(string path, IExternalizableFactory codec)
            => ReadSerializable<T>(new FileInfo(path), codec);

        //--------------------------------------------------------------------------------------------------------------
        public static string GetFileNameWithoutExtensions(string path)
        {
            while (Path.HasExtension(path))
            {
                path = Path.GetFileNameWithoutExtension(path);
            }
            return path;
        }
        public static FileSystemInfo[] GetPathList(DirectoryInfo root, FileSystemInfo sub)
        {
            var list = new List<FileSystemInfo>();
            if (sub is DirectoryInfo dir)
            {
                while (dir != root && dir != null)
                {
                    list.Add(dir);
                    dir = dir.Parent;
                }
                list.Reverse();
            }
            else if (sub is FileInfo file)
            {
                dir = file.Directory;
                while (dir != root && dir != null)
                {
                    list.Add(dir);
                    dir = dir.Parent;
                }
                list.Reverse();
                list.Add(sub);
            }
            return list.ToArray();
        }




        public static bool FindParent(this DirectoryInfo root, Predicate<DirectoryInfo> find)
        {
            if (root.Exists)
            {
                var dir = root;
                while (dir != null && dir != dir.Root)
                {
                    if (find(dir))
                    {
                        return true;
                    }
                    dir = dir.Parent;
                }
            }
            return false;
        }
        public static T FindParent<T>(this DirectoryInfo root, Func<DirectoryInfo, T> find) where T : class
        {
            if (root.Exists)
            {
                var dir = root;
                while (dir != null && dir != dir.Root)
                {
                    var value = find(dir);
                    if (value != null)
                    {
                        return value;
                    }
                    dir = dir.Parent;
                }
            }
            return null;
        }
        public static FileInfo FindParentFile(this DirectoryInfo root, string expect_path)
        {
            if (root.Exists)
            {
                var dir = root;
                while (dir != null && dir != dir.Root)
                {
                    var sub_path = Path.Combine(dir.FullName, expect_path);
                    if (File.Exists(sub_path))
                    {
                        return new FileInfo(sub_path);
                    }
                    dir = dir.Parent;
                }
            }
            return null;
        }
        public static DirectoryInfo FindParentDirectory(this DirectoryInfo root, string expect_path)
        {
            if (root.Exists)
            {
                var dir = root;
                while (dir != null && dir != dir.Root)
                {
                    var sub_path = Path.Combine(dir.FullName, expect_path);
                    if (Directory.Exists(sub_path))
                    {
                        return new DirectoryInfo(sub_path);
                    }
                    dir = dir.Parent;
                }
            }
            return null;
        }

        public static bool TryFindParentFile(this DirectoryInfo root, string expect_path, out FileInfo ret)
        {
            if (root.Exists)
            {
                var dir = root;
                while (dir != null && dir != dir.Root)
                {
                    var sub_path = Path.Combine(dir.FullName, expect_path);
                    if (File.Exists(sub_path))
                    {
                        ret = new FileInfo(sub_path);
                        return true;
                    }
                    dir = dir.Parent;
                }
            }
            ret = null;
            return false;
        }
        public static bool TryFindParentFile(string root, string expect_path, out string ret)
        {
            if (TryFindParentFile(new DirectoryInfo(root), expect_path, out var ret_dir))
            {
                ret = ret_dir.FullName;
                return true;
            }
            ret = null;
            return false;
        }
        public static bool TryFindParentDirectory(this DirectoryInfo root, string expect_path, out DirectoryInfo ret)
        {
            if (root.Exists)
            {
                var dir = root;
                while (dir != null && dir != dir.Root)
                {
                    var sub_path = Path.Combine(dir.FullName, expect_path);
                    if (Directory.Exists(sub_path))
                    {
                        ret = new DirectoryInfo(sub_path);
                        return true;
                    }
                    dir = dir.Parent;
                }
            }
            ret = null;
            return false;
        }
        public static bool TryFindParentDirectory(string root, string expect_path, out string ret)
        {
            if (TryFindParentDirectory(new DirectoryInfo(root), expect_path, out var ret_dir))
            {
                ret = ret_dir.FullName;
                return true;
            }
            ret = null;
            return false;
        }

        //--------------------------------------------------------------------------------------------------------------

        public static FileInfo GetChildFile(this DirectoryInfo parent, string name)
        {
            var d = new FileInfo(parent.FullName + Path.DirectorySeparatorChar + name);
            if (d.Exists)
            {
                return d;
            }
            return null;
        }
        public static DirectoryInfo GetChildDirectory(this DirectoryInfo parent, string name)
        {
            var d = new DirectoryInfo(parent.FullName + Path.DirectorySeparatorChar + name);
            if (d.Exists)
            {
                return d;
            }
            return null;
        }
        //------------------------------------------------------------------------------------------------------------------------------------------
        public static void ShellXCopy(DirectoryInfo workDir, string srcName, string dstName, params string[] types)
        {
            var src = new DirectoryInfo(workDir.FullName + Path.DirectorySeparatorChar + srcName);
            var dst = new DirectoryInfo(workDir.FullName + Path.DirectorySeparatorChar + dstName);
            CFiles.CreateDir(dst);
            if (types.Length == 0)
            {
                var start = new ProcessStartInfo();
                start.WorkingDirectory = workDir.FullName;
                start.FileName = "xcopy";
                start.Arguments = $"/S/Y  \"{src.FullName}\\*.*\"  \"{dst.FullName}\"";
                start.UseShellExecute = false;
                //start.RedirectStandardOutput = true;
                var p = Process.Start(start);
                p.WaitForExit();
            }
            else
            {
                foreach (var type in types)
                {
                    var start = new ProcessStartInfo();
                    start.WorkingDirectory = workDir.FullName;
                    start.FileName = "xcopy";
                    start.Arguments = $"/S/Y  \"{src.FullName}\\{type}\"  \"{dst.FullName}\"";
                    //Console.WriteLine($"{start.FileName} {start.Arguments}");
                    start.UseShellExecute = false;
                    //start.RedirectStandardOutput = true;
                    var p = Process.Start(start);
                    p.WaitForExit();
                }
            }
        }
        public static void ShellCopy(DirectoryInfo workDir, string src, string dst)
        {
            CFiles.FileCopyTo(
                Path.Combine(workDir.FullName, src),
                Path.Combine(workDir.FullName, dst), true);
        }
        public static void ShellRename(DirectoryInfo workDir, string srcName, string dstName)
        {
            var src = new FileInfo(workDir.FullName + Path.DirectorySeparatorChar + srcName);
            var dst = new FileInfo(workDir.FullName + Path.DirectorySeparatorChar + dstName);
            src.CopyTo(dst.FullName);
            src.Delete();
        }
        public static void ShellRename(string srcName, string dstName)
        {
            var src = new FileInfo(srcName);
            var dst = new FileInfo(dstName);
            src.CopyTo(dst.FullName);
            src.Delete();
        }
        //------------------------------------------------------------------------------------------------------------------------------------------
    }


    //------------------------------------------------------------------------------------------------------------------------------------------

    public class FileFilters
    {
        protected StringFilters filter_dir;
        protected StringFilters filter_file;

        public FileFilters(string regex)
        {
            var dir_regex = new StringBuilder();
            var file_regex = new StringBuilder();
            var fts = regex.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < fts.Length; i++)
            {
                if (fts[i].EndsWith("/"))
                {
                    dir_regex.Append(fts[i].Substring(0, fts[i].Length - 1));
                    if (i < fts.Length - 1)
                    {
                        dir_regex.Append(";");
                    }
                }
                else
                {
                    file_regex.Append(fts[i]);
                    if (i < fts.Length - 1)
                    {
                        file_regex.Append(";");
                    }
                }
            }
            this.filter_dir = new StringFilters(dir_regex.ToString());
            this.filter_file = new StringFilters(file_regex.ToString());
        }

        /// <summary>
        /// true 符合， false 排除
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool AcceptFile(FileInfo file)
        {
            if (!filter_dir.Accept(file.Directory.FullName))
            {
                return false;
            }
            if (!filter_file.Accept(file.FullName))
            {
                return false;
            }
            return true;
        }
        public bool AcceptDir(DirectoryInfo dir)
        {
            if (!filter_dir.Accept(dir.FullName))
            {
                return false;
            }
            return true;
        }
        public bool AcceptResource(string file)
        {
            if (!filter_dir.Accept(Resource.GetParent(file)))
            {
                return false;
            }
            if (!filter_file.Accept(file))
            {
                return false;
            }
            return true;
        }
        public bool AcceptResourceDir(string dir)
        {
            if (!filter_dir.Accept(dir))
            {
                return false;
            }
            return true;
        }
    }


}
