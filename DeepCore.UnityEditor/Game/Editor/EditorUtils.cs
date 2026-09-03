using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace DeepCore.UnityEditor
{
    public static class EditorUtils
    {
        public static string GetPlatformName()
        {
            return GetPlatformForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
        }

        private static string GetPlatformForAssetBundles(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
#if UNITY_2017_3_OR_NEWER
            case BuildTarget.StandaloneOSX:
                return "OSX";
#else
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
                case BuildTarget.StandaloneOSX:
                    return "OSX";
#endif
                default:
                    return null;
            }
        }

        public static string CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public static string[] GetSelectionAssetPaths(string filter)
        {
            var dirs = new List<string>();
            if (Selection.objects.Length > 0)
            {
                foreach (var obj in Selection.objects)
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    //Debug.Log(path);
                    dirs.Add(path);
                }
            }

            return GetAssetPaths(filter, dirs.ToArray());
        }

        public static string[] GetAssetPaths(string filter, string[] searchInFolders)
        {
            var folders = new List<string>(searchInFolders);
            var paths = new HashSet<string>();
            for (var i = folders.Count - 1; i >= 0; i--)
            {
                var path = folders[i];
                if (!AssetDatabase.IsValidFolder(path))
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    var ext = Path.GetExtension(path).Replace('.', ':');
                    if (!string.IsNullOrEmpty(filter) && !filter.Contains(name) && !filter.Contains($"t{ext}"))
                    {
                        folders.RemoveAt(i);
                        UnityEngine.Debug.Log($"Exclude : {path}");
                        continue;
                    }
                    paths.Add(path);
                }
                else
                {
                    UnityEngine.Debug.Log($"Ignore : {path}");
                }
            }

            if (folders.Count > 0)
            {
                var guids = AssetDatabase.FindAssets(filter, folders.ToArray());
                if (guids.Length <= 0) return paths.ToArray();
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!Directory.Exists(path))
                    {
                        paths.Add(path);
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"Ignore : {path}");
                    }
                }
            }

            return paths.ToArray();
        }



    }
}