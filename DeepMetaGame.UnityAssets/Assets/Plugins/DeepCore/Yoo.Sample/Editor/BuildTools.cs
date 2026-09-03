

#if YOOASSET_EDITOR_SAMPLE

using DeepCore;
using DeepCore.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

public class BuildTools
{
    //     //构建Android平台热更代码
    //     [MenuItem("Build/HotFixCode/Android")]
    //     public static void BuildAndroidHotFixCode()
    //     {
    //         EditorTools.ClearUnityConsole();
    // 
    //         HybridCLR.Editor.SettingsUtil.Enable = true;
    // 
    //         CheckPlatform("Android");
    // 
    //         BuildHotFixCode();
    //     }
    // 
    //     //构建Windows平台热更代码
    //     [MenuItem("Build/HotFixCode/Windows")]
    //     public static void BuildWindowsHotFixCode()
    //     {
    //         EditorTools.ClearUnityConsole();
    // 
    //         HybridCLR.Editor.SettingsUtil.Enable = true;
    // 
    //         CheckPlatform("Windows");
    // 
    //         BuildHotFixCode();
    //     }

    // 
    //     //构建Android平台YooAssetBundle
    //     [MenuItem("Build/Bundle/Android")]
    //     public static void BuildAndroidBundle()
    //     {
    //         EditorTools.ClearUnityConsole();
    // 
    //         CheckPlatform("Android");
    // 
    //         BuildAssetBundle();
    //     }
    // 
    //     //构建Windows平台YooAssetBundle
    //     [MenuItem("Build/Bundle/Windows")]
    //     public static void BuildWindowsBundle()
    //     {
    //         EditorTools.ClearUnityConsole();
    // 
    //         CheckPlatform("Windows");
    // 
    //         //设置playersetting
    //         PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Standard);
    //         PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 1);
    //         PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
    // 
    //         BuildAssetBundle();
    //     }
    // 
    // 


    //     //构建热更代码
    //     public static void BuildHotFixCode()
    //     {
    //         BuildLog("HybridCLR Generate All Start !");
    //         HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll();
    //         BuildLog("HybridCLR Generate All End !");
    // 
    //         BuildLog("HybridCLR Build Hotfix Code Start !");
    //         HotfixGeneratorMenu.GeneratorHotfix();
    //         BuildLog("HybridCLR Build Hotfix Code  End !");
    //         AssetDatabase.Refresh();
    //     }
    // 
    // 
    //     //构建Yooasset 资源Bundle
    //     public static void BuildAssetBundle()
    //     {
    //         BuildLog("YooAsset Build AssetBundle Start !");
    // 
    //         // 打开窗口
    //         AssetBundleBuilderWindow window = AssetBundleBuilderWindow.GetWindow<AssetBundleBuilderWindow>("AssetBundle Builder", true, WindowsDefine.DockedWindowTypes);
    // // 
    // //         window.viewer._clearBuildCacheToggle.value = true;
    // //         window.viewer._useAssetDependencyDBToggle.value = true;
    // //         window.viewer._outputNameStyleField.value = EFileNameStyle.BundleName;
    // //         window.viewer._copyBuildinFileOptionField.value = EBuildinFileCopyOption.ClearAndCopyAll;
    // // 
    // //         window.BuildByJenkins();
    // 
    //         AssetDatabase.Refresh();
    // 
    //         window.Close();
    // 
    //         BuildLog("YooAsset Build AssetBundle  End !");
    //     }
    // 
    // 


    public static void CheckPlatform(string param)
    {
        BuildLog($"检查平台 : 目标平台{param}  Start !");

        BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        switch (param)
        {
            case "Android":
                buildTarget = BuildTarget.Android;
                break;
            case "Windows":
                buildTarget = BuildTarget.StandaloneWindows64;
                break;
            default:
                break;
        }

        // 检查当前平台
        if (EditorUserBuildSettings.activeBuildTarget != buildTarget)
        {
            bool success = EditorUserBuildSettings.SwitchActiveBuildTarget(buildTarget == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.Standalone, buildTarget);
            if (success)
            {
                BuildLog($"转换至目标平台 {buildTarget.ToString()} 成功 ！");
            }
            else
            {
                BuildLog($"转换至目标平台 {buildTarget.ToString()} 失败 ！");
            }
        }

        BuildLog($"检查平台 : 目标平台{param}  End !");
    }
    // 
    //     [MenuItem("Build/Project/GameEdiroRuntime")]
    //     //编辑器runtime打包
    //     public static void BuildForGameEdiroRuntime()
    //     {
    //         //检查平台
    //         CheckPlatform("Windows");
    //         //关闭hybridclr
    //         HybridCLR.Editor.SettingsUtil.Enable = false;
    //         //ScriptingBackend to mono
    //         UnityEditor.PlayerSettings.stripEngineCode = false;
    //         var buildTargetGroup = BuildTargetGroup.Standalone;
    //         PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.Mono2x);
    //         PlayerSettings.SetApiCompatibilityLevel(buildTargetGroup, ApiCompatibilityLevel.NET_4_6);
    //         PlayerSettings.SetManagedStrippingLevel(buildTargetGroup, ManagedStrippingLevel.Disabled);
    //         AssetDatabase.SaveAssets();
    //         AssetDatabase.Refresh();
    // 
    //         BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
    //         buildPlayerOptions.scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
    // 
    //         var dataPath = Path.GetFullPath($"{Application.dataPath}/../../Data/GameEditor/UnityRun").Replace("\\", "/");
    //         string locationPathName = $"{dataPath}/KOI.exe";
    //         buildPlayerOptions.locationPathName = locationPathName;
    //         buildPlayerOptions.target = BuildTarget.StandaloneWindows;
    //         buildPlayerOptions.options = BuildOptions.None;
    //         var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
    //         if (buildReport.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
    //         {
    //             UnityEngine.Debug.LogError($"BuildForGameEdiroRuntime failed:{buildReport.ToString()}");
    //         }
    //         else
    //         {
    //             UnityEngine.Debug.Log($"BuildForGameEdiroRuntime successded path:{locationPathName}");
    //         }
    //     }
    // 
    //     [MenuItem("Build/Project/Windows")]
    //     public static void BuildForWindow()
    //     {
    //         EditorTools.ClearUnityConsole();
    // 
    //         CheckPlatform("Windows");
    // 
    //         BuildHotFixCode();
    // 
    //         BuildAssetBundle();
    // 
    //         BuildLog("构建 Windows Start !");
    // 
    //         BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
    //         buildPlayerOptions.scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
    // 
    //         string path = Application.dataPath.Replace("Assets", "Build_Package/Windows_Package");
    //         if (!Directory.Exists(path))
    //         {
    //             Directory.CreateDirectory(path);
    //         }
    // 
    //         buildPlayerOptions.locationPathName = path + "/GameMain.exe";
    //         buildPlayerOptions.target = BuildTarget.StandaloneWindows;
    //         buildPlayerOptions.options = BuildOptions.None;
    //         BuildPipeline.BuildPlayer(buildPlayerOptions);
    // 
    //         BuildLog("构建 Windows End !");
    //     }
    // 
    // 
    // 
    //     [MenuItem("Build/Project/Android")]
    //     public static void BuildForAndroid()
    //     {
    //         EditorTools.ClearUnityConsole();
    // 
    //         CheckPlatform("Android");
    // 
    //         BuildHotFixCode();
    // 
    //         BuildAssetBundle();
    // 
    //         BuildLog("构建 Android Start !");
    // 
    //         BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
    //         buildPlayerOptions.scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
    // 
    //         string path = Application.dataPath.Replace("Assets", "Build_Package/Android_Package");
    //         if (!Directory.Exists(path))
    //         {
    //             Directory.CreateDirectory(path);
    //         }
    // 
    //         buildPlayerOptions.locationPathName = path + "/GameMain.apk";
    //         buildPlayerOptions.target = BuildTarget.Android;
    //         buildPlayerOptions.options = BuildOptions.None;
    //         BuildPipeline.BuildPlayer(buildPlayerOptions);
    // 
    //         BuildLog("构建 Android End !");
    //     }
    // 

    static void BuildLog(string message)
    {
        Debug.Log($"BuildLog: {message}");
    }

    // 
    // 
    // 
    //     [MenuItem("Tools/Clear PlayerPrefs")]
    //     public static void ClearPlayerPrefs()
    //     {
    //         PlayerPrefs.DeleteAll();
    //         PlayerPrefs.Save();
    //     }


    //构建Windows平台YooAssetBundle
    [MenuItem("Build/Bundle/Windows")]
    public static void BuildWindowsBundle()
    {
        EditorTools.ClearUnityConsole();

        CheckPlatform("Windows");

        //设置playersetting
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 1);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

        BuildInternal(BuildTarget.StandaloneWindows64);
    }

    private static void BuildInternal(BuildTarget buildTarget)
    {
        Debug.Log($"开始构建 : {buildTarget}");

        var buildoutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        var streamingAssetsRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();

        // 构建参数
        BuiltinBuildParameters buildParameters = new BuiltinBuildParameters();
        buildParameters.BuildOutputRoot = buildoutputRoot;
        buildParameters.BuildinFileRoot = streamingAssetsRoot;
        buildParameters.BuildPipeline = EBuildPipeline.BuiltinBuildPipeline.ToString();
        buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle; //必须指定资源包类型
        buildParameters.BuildTarget = buildTarget;
        buildParameters.PackageName = "DefaultPackage";
        buildParameters.PackageVersion = CUtils.FormatTime(DateTime.Now);
        buildParameters.VerifyBuildingResult = true;
        buildParameters.EnableSharePackRule = true; //启用共享资源构建模式，兼容1.5x版本
        buildParameters.FileNameStyle = EFileNameStyle.HashName;
        buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.None;
        buildParameters.BuildinFileCopyParams = string.Empty;
        buildParameters.EncryptionServices = new EncryNone();
        buildParameters.CompressOption = ECompressOption.LZ4;
        buildParameters.ClearBuildCacheFiles = false; //不清理构建缓存，启用增量构建，可以提高打包速度！
        buildParameters.UseAssetDependencyDB = true; //使用资源依赖关系数据库，可以提高打包速度！

        // 执行构建
        BuiltinBuildPipeline pipeline = new BuiltinBuildPipeline();
        var buildResult = pipeline.Run(buildParameters, true);
        if (buildResult.Success)
        {
            Debug.Log($"构建成功 : {buildResult.OutputPackageDirectory}");
            var src = $"{buildResult.OutputPackageDirectory}";
            if (CFiles.TryFindParentDirectory(src, Path.Combine("Data", "GameEditor", "res"), out var res_yoo))
            {
                Debug.Log($"自动复制到编辑器目录 : {res_yoo}");
                CFiles.DeleteAll(Path.Combine(res_yoo, "yoo", "DefaultPackage"));
                CFiles.DirectoryCopy(src, Path.Combine(res_yoo, "yoo", "DefaultPackage"));
            }
        }
        else
        {
            Debug.LogError($"构建失败 : {buildResult.ErrorInfo}");
        }
    }
    class EncryNone : IEncryptionServices
    {
        public EncryptResult Encrypt(EncryptFileInfo fileInfo)
        {
            return new EncryptResult() { Encrypted = false };
        }
    }
}
#endif