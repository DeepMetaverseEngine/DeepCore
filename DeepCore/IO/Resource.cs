using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DeepCore.IO
{
    public static class Resource
    {
        //----------------------------------------------------
        //----------------------------------------------------
        #region Singleton
        public static IReadOnlyList<IResourceLoader> Loaders => loaders;
        public static string PathRoot { get; set; }

        private static List<IResourceLoader> loaders = new List<IResourceLoader>();
        static Resource()
        {
            //mCurLoader = new DefaultResourceLoader();
            //mCurLoader.PathRoot = Environment.CurrentDirectory;
            AddLoader(new FileResourceLoader());
        }
        //private static IResourceLoader mCurLoader;
        //private static INative mCurNative;
        public static void AddLoader(IResourceLoader loader)
        {
            if (!loaders.Contains(loader))
            {
                loaders.Add(loader);
            }
            //mCurLoader = loader;
        }
        public static void AddLoaderAt(IResourceLoader loader, int index)
        {
            AddLoader(loader);
            SetLoaderIndex(loader, index);
        }
        public static void SetLoaderIndex(IResourceLoader loader, int newIndex)
        {
            if (newIndex >= 0 && newIndex < loaders.Count)
            {
                var existIndex = loaders.IndexOf(loader);
                if (existIndex >= 0)
                {
                    loaders.Swap(newIndex, existIndex);
                }
            }
        }
        public static bool RemoveLoader(IResourceLoader loader)
        {
            return loaders.Remove(loader);
        }
        public static bool RemoveLoaderWithType(Type loaderType)
        {
            for (int i = 0; i < loaders.Count; i++)
            {
                if (loaderType.IsInstanceOfType(loaders[i].GetType()))
                {
                    loaders.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        public static void Clear()
        {
            loaders.Clear();
        }
        public static bool TryGetLoaderWithPath(string path, out string fullpath, out IResourceLoader loader)
        {
            loader = null;
            fullpath = path;
            if (string.IsNullOrEmpty(path))
                return false;
            foreach (var op in loaders)
            {
                if (op.IsStartWith(path))
                {
                    loader = op;
                    return true;
                }
            }
            if (!string.IsNullOrEmpty(Resource.PathRoot))
            {
                foreach (var op in loaders)
                {
                    if (op.IsStartWith(Resource.PathRoot + path))
                    {
                        fullpath = Resource.PathRoot + path;
                        loader = op;
                        return true;
                    }
                }
            }
            return false;
        }

        //         public static void SetNative(INative native)
        //         {
        //             //mCurNative = native;
        //         }
        //         public static INative CurrentNative
        //         {
        //             get => mCurNative;
        //         }
        //         public static IResourceLoader CurrentLoader
        //         {
        //             get => mCurLoader;
        //         }
        #endregion
        //----------------------------------------------------
        public static bool TryLoadData(string path, out byte[] data)
        {
            data = null;
            if (string.IsNullOrEmpty(path)) return false;
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                data = loader.LoadData(fullpath);
                return data != null;
            }
            // data = mCurLoader.LoadData(path);
            return false;
        }
        public static bool TryOpenStream(string path, out Stream data)
        {
            data = null;
            if (string.IsNullOrEmpty(path)) return false;
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                data = loader.OpenStream(fullpath);
                return data != null;
            }
            return false;
        }
        //----------------------------------------------------

        public static bool ExistData(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                if (loader.ExistData(fullpath))
                {
                    return true;
                }
            }
            return false;
        }
        public static Stream OpenStream(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (TryOpenStream(path, out var stream)) { return stream; }
            return default;
        }
        public static byte[] LoadData(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (TryLoadData(path, out var data)) { return data; }
            return default;
        }
        public static string LoadAllText(string path)
        {
            byte[] data = LoadData(path);
            if (data != null)
            {
                return CUtils.DecodeUTF8(data);
            }
            return null;
        }
        public static string[] LoadAllLines(string path)
        {
            var data = LoadAllText(path);
            if (data != null)
            {
                return CUtils.StringToLines(data);
            }
            return null;
        }
        public static string[] ListFiles(string path, bool fullPath = false)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                var list = loader.ListFiles(fullpath);
                if (list != null)
                {
                    return list;
                }
            }
            return default;
        }
        public static string[] ListDirectories(string path, bool fullPath = false)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                var list = loader.ListDirectories(fullpath);
                if (list != null)
                {
                    return list;
                }
            }
            return default;
        }

        //----------------------------------------------------

        public static async Task<bool> ExistDataAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                if (await loader.ExistDataAsync(fullpath))
                {
                    return true;
                }
            }
            return false;
        }
        public static async Task<Stream> OpenStreamAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                var stream = await loader.OpenStreamAsync(fullpath);
                if (stream != null)
                {
                    return stream;
                }
            }
            return null;
        }
        public static async Task<byte[]> LoadDataAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                var data = await loader.LoadDataAsync(fullpath);
                if (data != null)
                {
                    return data;
                }
            }
            return null;
        }
        public static async Task<string> LoadAllTextAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            byte[] data = await LoadDataAsync(path);
            if (data != null)
            {
                return CUtils.DecodeUTF8(data);
            }
            return null;
        }
        public static async Task<string[]> LoadAllLinesAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var data = await LoadAllTextAsync(path);
            if (data != null)
            {
                return CUtils.StringToLines(data);
            }
            return null;
        }
        //----------------------------------------------------


        //----------------------------------------------------
        public static async Task<string[]> ListFilesAsync(string path, bool fullPath = false)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                var list = await loader.ListFilesAsync(fullpath);
                if (list != null)
                {
                    return list;
                }
            }
            return default;
        }
        public static async Task<string[]> ListDirectoriesAsync(string path, bool fullPath = false)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                var list = await loader.ListDirectoriesAsync(fullpath);
                if (list != null)
                {
                    return list;
                }
            }
            return default;
        }

        //----------------------------------------------------
        public static ArrayList<string> DeepListFiles(string path)
        {
            var ret = new ArrayList<string>();
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                DeepListFiles(loader, ret, fullpath);
                return ret;
            }
            return ret;
            static void DeepListFiles(IResourceLoader mCurLoader, ArrayList<string> list, string path)
            {
                foreach (var sub in mCurLoader.ListFiles(path, true))
                {
                    list.Add(sub);
                }
                foreach (var sub in mCurLoader.ListDirectories(path, true))
                {
                    DeepListFiles(mCurLoader, list, sub);
                }
            }
        }
        public static ArrayList<string> DeepListDirectories(string path)
        {
            var ret = new ArrayList<string>();
            if (TryGetLoaderWithPath(path, out var fullpath, out var loader))
            {
                DeepListDirectories(loader, ret, fullpath);
                return ret;
            }
            return ret;
            static void DeepListDirectories(IResourceLoader mCurLoader, ArrayList<string> list, string path)
            {
                foreach (var sub in mCurLoader.ListDirectories(path, true))
                {
                    list.Add(sub);
                    DeepListDirectories(mCurLoader, list, sub);
                }
            }
        }

        //----------------------------------------------------


        //----------------------------------------------------

        public static string GetFileName(string path)
        {
            if (path.TryLastIndexOf('/', out var left))
            {
                return path.Substring(left + 1);
            }
            if (path.TryLastIndexOf('\\', out left))
            {
                return path.Substring(left + 1);
            }
            return path;
        }
        public static string GetFileNameWithoutExtension(string path)
        {
            if (path.TryLastIndexOf('.', out var right))
            {
                if (path.TryLastIndexOf('/', out var left) && right > left)
                {
                    return path.Substring(left + 1, right - left - 1);
                }
                if (path.TryLastIndexOf('\\', out left) && right > left)
                {
                    return path.Substring(left + 1, right - left - 1);
                }
                return path.Substring(0, right);
            }
            else
            {
                if (path.TryLastIndexOf('/', out var left))
                {
                    return path.Substring(left + 1);
                }
                if (path.TryLastIndexOf('\\', out left))
                {
                    return path.Substring(left + 1);
                }
                return path;
            }
        }
        public static string CombinePath(params string[] paths)
        {
            return CUtils.ArrayToString(paths, DEFAULT_SPLIT1);
        }
        public static string GetParent(string path)
        {
            path = Resource.FormatPath(path);
            if (path.TryLastIndexOf('/', out var indexR))
            {
                return path.Substring(indexR);
            }
            if (path.TryLastIndexOf('\\', out var indexL))
            {
                return path.Substring(indexL);
            }
            return path;
        }
        public static string FormatPath(string path)
        {
            path = path.Replace('\\', DEFAULT_SPLIT);
            if (path.TryIndexOf("://", out var pi, 0))
            {
                var prefix = path.Substring(0, pi + 3);
                var suffix = path.Substring(pi + 3);
                return prefix + formatDD(suffix);
            }
            else
            {
                return formatDD(path);
            }
            string formatDD(string dd)
            {
                dd = dd.ReplaceAll("//", "/");
                dd = dd.ReplaceAll("/./", "/");
                if (dd.StartsWith("./")) dd = dd.Substring(1);
                int d2 = 0;
                while (true)
                {
                    d2 = dd.IndexOf("/../", d2);
                    if (d2 > 0)
                    {
                        if (dd[d2 - 1] == '.')
                        {
                            d2 += 4;
                        }
                        else if (dd.TryLastIndexOf('/', out var ddp, d2 - 1))
                        {
                            var dd1 = dd.Substring(0, ddp);
                            var dd2 = dd.Substring(d2 + 3);
                            dd = dd1 + dd2;
                            d2 = ddp;
                        }
                        else
                        {
                            dd = dd.Substring(d2 + 4);
                            d2 = 0;
                        }
                        continue;
                    }
                    else if (d2 == 0)
                    {
                        d2 += 4;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                return dd;
            }
        }

        public static string ParentPath(string path)
        {
            var index = path.LastIndexOfAny(split);
            if (index >= 0)
            {
                var last = path.Length - 1;
                while (index == last)
                {
                    index = path.LastIndexOfAny(split, index - 1, last);
                    if (index < 0) break;
                    last--;
                }
                return path.Substring(0, index);
            }
            return string.Empty;
        }
        public static bool IsStartWith(string path, string prefix, out string suffix)
        {
            if (path.StringStartWithIgnoreCase(prefix))
            {
                suffix = path.Substring(prefix.Length);
                return true;
            }
            suffix = null;
            return false;
        }
        //----------------------------------------------------

        /// <summary>
        /// 项目命名空间.资源文件所在文件夹名.资源文件名
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static byte[] LoadFromAssembly(Assembly assembly, string resource)
        {
            return IOUtil.LoadFromAssembly(assembly, resource);
        }
        /// <summary>
        /// .资源文件所在文件夹名.资源文件名
        /// </summary>
        /// <param name="type"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static byte[] LoadFromAssembly(Type type, string resource)
        {
            return LoadFromAssembly(type.Assembly, resource);
        }
        public static string LoadTextFromAssembly(Assembly assembly, string resource)
        {
            var data = LoadFromAssembly(assembly, resource);
            return CUtils.DecodeUTF8(data);
        }
        public static string LoadTextFromAssembly(Type type, string resource)
        {
            var data = LoadFromAssembly(type, resource);
            return CUtils.DecodeUTF8(data);
        }

        static private char[] split = new char[] { '\\', DEFAULT_SPLIT };

        public const char DEFAULT_SPLIT = '/';
        public const string DEFAULT_SPLIT2 = "//";
        public const string DEFAULT_SPLIT1 = "/";

        public const string PREFIX_FILE = "file://";
        public const string PREFIX_RES = "res://";
        public const string PREFIX_HTTP = "http://";
        public const string PREFIX_HTTPS = "https://";
        public const string PREFIX_MPQ = "mpq://";
        public const string PREFIX_JAR = "jar://";
        public const string PREFIX_ZIP = "zip://";
    }
    //----------------------------------------------------------------------------------------

    public interface IResourceLoader
    {
        bool IsStartWith(string path);

        Stream OpenStream(string path);
        Task<Stream> OpenStreamAsync(string path);

        byte[] LoadData(string path);
        Task<byte[]> LoadDataAsync(string path);

        bool ExistData(string path);
        Task<bool> ExistDataAsync(string path);


        string[] ListFiles(string path, bool fullPath = false);
        string[] ListDirectories(string path, bool fullPath = false);

        Task<string[]> ListFilesAsync(string path, bool fullPath = false);

        Task<string[]> ListDirectoriesAsync(string path, bool fullPath = false);
    }
    //-----------------------------------------------------------------------------------------------------------------------


    //-----------------------------------------------------------------------------------------------------------------------

    //     public interface INative
    //     {
    //         bool Decompressor(ArraySegment<byte> src, out ArraySegment<byte> dst);
    //         bool Compress(ArraySegment<byte> src, out ArraySegment<byte> dst);
    //     }

}
