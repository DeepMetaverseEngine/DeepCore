using DeepCore.UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[Serializable]
public class BundleBuild
{
    public string assetBundleName;
    public List<string> assetNames = new List<string>();
    
    public AssetBundleBuild ToBuild()
    {
        return new AssetBundleBuild()
        {
            assetBundleName = assetBundleName,
            assetNames = assetNames.ToArray(),
        };
    }
}

[CreateAssetMenu(fileName = "ABBuildRule", menuName = "DeepCore/ABBuildRule")]
public class BuildRule : ScriptableObject, IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public static readonly MD5 MD5 = MD5.Create();
    public const string ShaderBundleName = "shaders";
    
    [Header("打包设置")]
    [Tooltip("输出根目录")] 
    public string OutputRoot = "AssetBundles";
    [Tooltip("扩展名")] 
    public string BundleExtension = ".ab";
    [Tooltip("打包AB选项")] 
    public BuildAssetBundleOptions Options = BuildAssetBundleOptions.DeterministicAssetBundle;

    [Header("资源目录设置")]
    [Tooltip("按其下文件打AB(Assets相对路径开始)")] 
    public string[] BundleByFile = new string[0];
    [Tooltip("按其下文件夹打AB(Assets相对路径开始)")] 
    public string[] BundleBySubfolder= new string[0];
    
    public void BuildAll()
    {
        var check = new List<string>();
        var bDirty = false;
        foreach (var path in BundleByFile)
        {
            var tmp = path.Replace("\\", "/");
            if (tmp.EndsWith("/"))
            {
                tmp = tmp.Substring(0, tmp.Length - 1);
            }
            check.Add(tmp);
            if (!tmp.Equals(path))
            {
                bDirty = true;
            }
            
            if (!AssetDatabase.IsValidFolder(path))
            {
                EditorUtility.DisplayDialog("ABBuildRule", $"BundleByFile - 无效的文件夹 {tmp}", "Close");
                return;
            }
        }

        if (bDirty)
        {
            BundleByFile = check.ToArray();
            EditorUtility.SetDirty(this);
        }
        
        check.Clear();
        bDirty = false;
        foreach (var path in BundleBySubfolder)
        {
            var tmp = path.Replace("\\", "/");
            if (tmp.EndsWith("/"))
            {
                tmp = tmp.Substring(0, tmp.Length - 1);
            }
            check.Add(tmp);
            if (!tmp.Equals(path))
            {
                bDirty = true;
            }

            if (!AssetDatabase.IsValidFolder(path))
            {
                EditorUtility.DisplayDialog("ABBuildRule", $"BundleBySubfolder - 无效的文件夹 {tmp}", "Close");
                return;
            }
        }
        
        if (bDirty)
        {
            BundleBySubfolder = check.ToArray();
            EditorUtility.SetDirty(this);
        }
        
        AssetDatabase.SaveAssets();
        
        var asset2Bundle = new Dictionary<string, string>();
        foreach (var path in BundleByFile)
        {
            var folderName = Path.GetFileName(path).ToLower();
            var guids = AssetDatabase.FindAssets("", new[] { path });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (BuildConfig.IsValidateAsset(assetPath))
                {
                    var subPath = assetPath.Replace(path, "").Replace(Path.GetExtension(assetPath), "").ToLower();
                    var bundleName = $"{folderName}{subPath}";
                    if (assetPath.EndsWith(".shader") || assetPath.EndsWith(".shadervariants"))
                    {
                        bundleName = ShaderBundleName;
                    }
                    asset2Bundle.Add(assetPath, bundleName);
                }
            }
        }

        foreach (var path in BundleBySubfolder)
        {
            var folder = Path.GetFileName(path).ToLower();
            var subfolders = AssetDatabase.GetSubFolders(path);
            foreach (var subfolder in subfolders)
            {
                var bundleName = $"{folder}/{Path.GetFileName(subfolder).ToLower()}";
                
                var guids = AssetDatabase.FindAssets("", new[] { subfolder });
                foreach (var guid in guids)
                {
                    var asstPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (BuildConfig.IsValidateAsset(asstPath))
                    {
                        if (asstPath.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) 
                            || asstPath.EndsWith(".shadervariants", StringComparison.OrdinalIgnoreCase))
                        {
                            bundleName = ShaderBundleName;
                        }

                        if (!asset2Bundle.ContainsKey(asstPath))
                        {
                            asset2Bundle.Add(asstPath, bundleName);
                        }
                    }
                }
            }
        }
        
        var deps2Bundle = new Dictionary<string, string>();
        foreach (var key in new List<string>(asset2Bundle.Keys))
        {
            var assetPath = key;
            var bundleName = asset2Bundle[key];
            var deps = AssetDatabase.GetDependencies(assetPath, true);
            foreach (var dep in deps)
            {
                var tmpBundleName = bundleName;
                if (!BuildConfig.IsValidateAsset(dep))
                    continue;
                if (asset2Bundle.ContainsKey(dep))
                    continue;
                if (deps2Bundle.ContainsKey(dep))
                {
                    deps2Bundle.Remove(dep);
                    if (dep.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) 
                        || dep.EndsWith(".shadervariants", StringComparison.OrdinalIgnoreCase))
                    {
                        tmpBundleName = ShaderBundleName;
                    }
                    else
                    {
                        tmpBundleName = $"shared/{GetMD5Hash(dep)}";
                    }
                    asset2Bundle.Add(dep, tmpBundleName);
                    continue;
                }

                if (dep.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) 
                    || dep.EndsWith(".shadervariants", StringComparison.OrdinalIgnoreCase))
                {
                    tmpBundleName = ShaderBundleName;
                }
                else
                {
                    tmpBundleName = $"{tmpBundleName}_dep";
                }
                deps2Bundle.Add(dep, tmpBundleName);
            }
        }

        var bundleSnap = new Dictionary<string, BundleBuild>();
        foreach (var pair in asset2Bundle)
        {
            var assetPath = pair.Key;
            var bundleName = $"{pair.Value}{BundleExtension}";
            if (!bundleSnap.TryGetValue(bundleName, out var bb))
            {
                bb = new BundleBuild();
                bb.assetBundleName = bundleName;
                bundleSnap.Add(bundleName, bb);
            }
            bb.assetNames.Add(assetPath);
        }

        foreach (var pair in deps2Bundle)
        {
            var depPath = pair.Key;
            var bundleName = $"{pair.Value}{BundleExtension}";
            if (!bundleSnap.TryGetValue(bundleName, out var bb))
            {
                bb = new BundleBuild();
                bb.assetBundleName = bundleName;
                bundleSnap.Add(bundleName, bb);
            }
            bb.assetNames.Add(depPath);
        }
        
        var outputPath = EditorUtils.CreateDirectory($"{OutputRoot}/{EditorUtils.GetPlatformName()}");
        var bundles = new List<BundleBuild>(bundleSnap.Values).ConvertAll(delegate (BundleBuild input) { return input.ToBuild(); }).ToArray();
        var platform = EditorUserBuildSettings.activeBuildTarget;
        BuildPipeline.BuildAssetBundles(outputPath, bundles, Options, platform);
    }

    public static string GetMD5Hash(string input)
    {
        var data = MD5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return ToHash(data);
    }

    private static string ToHash(byte[] data)
    {
        var sb = new StringBuilder();
        foreach (var t in data)
            sb.Append(t.ToString("x2"));
        return sb.ToString();
    }
    
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
    }

    public void OnPreprocessBuild(BuildReport report)
    {
    }
}