using DeepCore;
using DeepCore.IO;
using DeepCore.Unity;
using DeepCore.Unity.AB;
using DeepCore.Unity.ResourceSnap;
using DeepCore.Unity.ResourceViewer;
using DeepCore.UnityEditor;
using DeepCore.UnityEditor.Asset;
using DeepCore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class BuildConfig
{
    public const string DefaultBuildConfigPath = "Assets/DefaultABBuildConfig.asset";
    public const string DefaultBuildRulePath = "Assets/DefaultABBuildRule.asset";
    public static string[] ExtensionFilter = new string[1] { ".prefab" };
    public static string BundleExtension = ".ab";
    public static BuildAssetBundleOptions BuildOption = BuildAssetBundleOptions.CompleteAssets;

    public static bool IsValidateAsset(string asset)
    {
        if (!asset.StartsWith("Assets/")) return false;
        //         var ext = Path.GetExtension(asset).ToLower();
        //         return ext != ".dll" && ext != ".cs" && ext != ".meta" && ext != ".js" && ext != ".boo";
        return true;
    }

    //      public static BuildConfig CheckConfig()
    //      {
    //          var config = AssetDatabase.LoadAssetAtPath<BuildConfig>(DefaultBuildConfigPath);
    // //         if (config == null)
    // //         {
    // //             config = CreateInstance<BuildConfig>();
    // //             AssetDatabase.CreateAsset(config, DefaultBuildConfigPath);
    // //         }
    // //         if (config.ActiveRule == null)
    // //         {
    // //             var bCreate = EditorUtility.DisplayDialog("ABBuildConfig", "请先激活一个打包规则！", "使用默认？");
    // //             if (bCreate)
    // //             {
    // //                 var rule = CreateInstance<BuildRule>();
    // //                 AssetDatabase.CreateAsset(rule, DefaultBuildRulePath);
    // //                 config.ActiveRule = rule;
    // //                 EditorUtility.SetDirty(config);
    // //                 AssetDatabase.SaveAssets();
    // //             }
    // //         }
    //          return config;
    //      }

    [MenuItem("Assets/DeepCore/选中资源全量打AB")]
    public static void BuildSelected()
    {
        BuildSelectedAB(ExtensionFilter, BundleExtension, BuildOption);
    }



    public static void BuildSelectedAB(string[] ExtensionFilter, string BundleExtension, BuildAssetBundleOptions BuildOption)
    {
        //var config = CheckConfig();
        {
            var outputDir = EditorUtils.CreateDirectory("_output/assetbundles/" + EditorUtils.GetPlatformName());
            var platform = EditorUserBuildSettings.activeBuildTarget;
            var paths = EditorUtils.GetSelectionAssetPaths("");
            var list = new List<AssetBundleBuild>();
            foreach (var path in paths)
            {
                UnityEngine.Debug.Log($"Build : {path}");
                if (!IsValidateAsset(path))
                    continue;

                var extension = Path.GetExtension(path).ToLower();
                if (!ExtensionFilter.Contains(extension))
                    continue;

                var dir = path.Replace(Path.GetFileName(path), "").Replace("Assets/", "");
                var outputName = (dir + Path.GetFileNameWithoutExtension(path) + BundleExtension).ToLower();
                var outputFile = outputDir + "/" + outputName;
                {
#if false
                    {
                        var deps = new List<string>(AssetDatabase.GetDependencies(path, true));
                        var tmp = new AssetBundleBuild()
                        {
                            assetBundleName = outputName,
                            assetNames = deps.Where(IsValidateAsset).ToArray(),
                        };
                        Debug.Log($"BuildAssetBundles:{path}");
                        BuildPipeline.BuildAssetBundles(outputDir, new[] { tmp }, config.ActiveRule.Options | options, platform);
                    }
#else
                    {
                        var mainAsset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
                        var depends = AssetDatabase.GetDependencies(path, true);
                        //var fuckProps = new DeepCore.Properties();
                        try
                        {
                            if (mainAsset is GameObject go)
                            {
                                if (go.TryGetComponentsInChildren<MeshRenderer>(out var renders, true))
                                {
                                    foreach (var render in renders)
                                    {
                                        try
                                        {
                                            if (render.sharedMaterials != null)
                                            {
                                                foreach (var material in render.sharedMaterials)
                                                {
                                                    if (material)
                                                    {
                                                        //fuckProps.Put(render.gameObject.name, material.name);
                                                    }
                                                }

                                            }
                                        }
                                        catch (Exception err)
                                        {
                                            Debug.LogError(err);
                                        }
                                    }
                                }
                                if (go.TryGetComponentsInChildren<Animation>(out var animations, true))
                                {
                                    foreach (var animation in animations)
                                    {

                                    }
                                }
                                if (go.TryGetComponentsInChildren<Animator>(out var animators, true))
                                {
                                    foreach (var animator in animators)
                                    {

                                    }
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Debug.LogError(err);
                        }
                        Debug.Log($"BuildAssetBundles:{path}");
                        CFiles.CreateFile(outputFile);
                        if (true)
                        {
                            var tmp = new AssetBundleBuild()
                            {
                                assetBundleName = outputName,
                                assetNames = depends.Where(IsValidateAsset).ToArray(),
                            };
                         var manifest =   BuildPipeline.BuildAssetBundles(outputDir, new[] { tmp }, BuildOption, platform);
                            AssetDatabase.ReleaseCachedFileHandles();
                            //CFiles.WriteAllText(outputFile + ".fuck", fuckProps.ToString(), CUtils.UTF8);
                            UnityEngine.Object.DestroyImmediate(manifest);
                        }
                        else
                        {
                            var options = BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle;
                            var assets = new List<UnityEngine.Object>();
                            assets.Add(mainAsset);
                            foreach (var depend in depends)
                            {
                                if (IsValidateAsset(depend))
                                {
                                    var dependAsset = AssetDatabase.LoadAssetAtPath(depend, typeof(UnityEngine.Object));
                                    assets.Add(dependAsset);
                                }
                            }
                            CFiles.CreateFile(outputFile);
                            BuildPipeline.BuildAssetBundle(mainAsset, assets.ToArray(), outputFile, options, platform);
                            AssetDatabase.ReleaseCachedFileHandles();

                        }
                    }
                    {
                        //                         var bundle = AssetBundle.LoadFromFile(outputFile);
                        //                         var assets = bundle.LoadAllAssets();
                        //                         try
                        //                         {
                        //                             foreach (var asset in assets)
                        //                             {
                        //                                 if (asset is GameObject go && go.TryGetComponentsInChildren<MeshRenderer>(out var renders, true))
                        //                                 {
                        //                                     foreach (var render in renders)
                        //                                     {
                        //                                         if (render.gameObject.TryGetComponent<FuckMeshRender>(out var fuck) && fuck.matrialNames != null)
                        //                                         {
                        //                                             var mts = new List<Material>(fuck.matrialNames.Length);
                        //                                             foreach (var mname in fuck.matrialNames)
                        //                                             {
                        //                                                 var m = mname;
                        //                                                 mts.Add(m);
                        //                                             }
                        //                                             render.materials = mts.ToArray();
                        //                                         }
                        //                                     }
                        //                                 }
                        //                             }
                        //                         }
                        //                         catch (Exception err)
                        //                         {
                        //                             Debug.LogError(err);
                        //                         }
                        //                         bundle.Unload(true);
                    }
#endif


                }
                //Debug.Log(dir + Path.GetFileNameWithoutExtension(path).ToLower() + config.ActiveRule.BundleExtension);
                //                 try
                //                 {
                //                     var snap = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                //                     UnityEditor.EditorGUIUtility.PingObject(snap);
                //                     UnityEditor.Selection.activeObject = snap;
                //                     try
                //                     {
                //                         if (snap is GameObject go)
                //                         {
                //                             var text = ObjectResourceInfo.GetObjectInfo(go);
                //                             CFiles.WriteAllText(new FileInfo(Path.Combine(outputDir, outputName + ".snap")), XmlUtil.ObjectToXmlString(text));//.SaveTextureToFile();
                //                         }
                //                     }
                //                     catch (Exception ex)
                //                     {
                //                         Debug.LogError(ex);
                //                     }
                //                     try
                //                     {
                //                         if (snap is GameObject go)
                //                         {
                //                             var text = ResourceInfo.GetAssetPreview(go);
                //                             text.SaveTextureToFile(new FileInfo(Path.Combine(outputDir, outputName + ".snap.png")));
                //                             Texture2D.DestroyImmediate(text);
                //                             //StartCoroutine(SetThumbnail(go, Path.Combine(outputDir, outputName + ".snap.png")));
                //                         }
                //                     }
                //                     catch (Exception ex)
                //                     {
                //                         Debug.LogError(ex);
                //                     }
                //                 }
                //                 catch (Exception ex)
                //                 {
                //                     Debug.LogError(ex);
                //                 }
                GC.Collect();
                UnityEngine.Debug.Log($"Build Complete : {outputName}");
            }
        }
    }

    // Creates a new menu item 'Examples > Create Prefab' in the main menu.
    [MenuItem("Assets/DeepCore/当前场景转Prefab")]
    public static void BuildSelectedScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var ext = Path.GetExtension(scene.path);
        var outputDir = $"{scene.path.Substring(0, scene.path.Length - ext.Length)}";

        // Keep track of the currently selected GameObject(s)
        GameObject[] objectArray = scene.GetRootGameObjects();

        // Loop through every GameObject in the array above
        foreach (var gameObject in objectArray)
        {
            try
            {
                if (!gameObject.TryGetComponentInChildren<IgnoreABBuilder>(out var ignore))
                {
                    // Create folder Prefabs and set the path as within the Prefabs folder,
                    // and name it as the GameObject's name with the .Prefab format

                    string localPath = $"{outputDir}.{gameObject.name}.prefab";

                    // Make sure the file name is unique, in case an existing Prefab has the same name.
                    localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

                    // Create the new Prefab and log whether Prefab was saved successfully.
                    bool prefabSuccess;
                    PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, localPath, InteractionMode.UserAction, out prefabSuccess);
                    if (prefabSuccess == true)
                        Debug.Log("Prefab was saved successfully");
                    else
                        Debug.Log("Prefab failed to save" + prefabSuccess);
                }
            }
            catch (Exception err)
            {
                Debug.Log(err);
            }
        }
    }

    static IEnumerator SetThumbnail(UnityEngine.GameObject go, string output)
    {
        Texture2D thumbnail = null;
        while (thumbnail == null)
        {
            thumbnail = AssetPreview.GetAssetPreview(go);
            yield return new WaitForSeconds(.5f);
        }
        var text = ResourceInfo.GetAssetPreview(go);
        text.SaveTextureToFile(new FileInfo(output));
        Texture2D.DestroyImmediate(text);
    }
}
