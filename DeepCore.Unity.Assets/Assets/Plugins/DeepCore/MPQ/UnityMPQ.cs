#if MPQ

using Cysharp.Threading.Tasks;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.MPQ.Updater;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DeepCore.Unity.MPQ
{
    public static class UnityMPQ
    {
        public static Func<string, bool> AcceptFile = new Func<string, bool>(fileName => !fileName.EndsWith(".mpq.z", StringComparison.OrdinalIgnoreCase));
        public static async UniTask CopyToLocalSaveAsync(string streamingAssetsSubPath, DirectoryInfo savePath, IRangeValue p = null)
        {
            var fileNames = new List<string>();
            var old_utc_time = DateTime.MinValue;
            var update_version_file = new FileInfo(Path.Combine(savePath.FullName, "update_version.txt"));
            if (update_version_file.Exists)
            {
                var old_update_version_txt = await Resource.LoadAllTextAsync(update_version_file.FullName).AsUniTask();
                MPQUpdater.TryForEachVersionTextEntrys(
                        old_update_version_txt,
                        out old_utc_time,
                        (key, md5, fsize, userdata) => { },
                        (parent, key, md5, fsize, userdata) => { });
            }
            p?.SetText($"Validating Version");
            var update_version_txt = await LoadTextFromJarAsync($"{streamingAssetsSubPath}/update_version.txt");
            if (update_version_txt != null)
            {
                MPQUpdater.TryForEachVersionTextEntrys(
                    update_version_txt,
                    out var utc_time,
                    (key, md5, fsize, userdata) => { fileNames.Add(key); },
                    (parent, key, md5, fsize, userdata) => { });
                p?.SetRange(0, fileNames.Count, 0);
                if (utc_time > old_utc_time)
                {
                    foreach (var fileName in fileNames)
                    {
                        p?.SetText($"Copying {Path.GetFileName(fileName)}");
                        try
                        {
                            if (AcceptFile(fileName))
                            {
                                var data = await LoadFromJarAsync($"{streamingAssetsSubPath}/{fileName}");
                                if (data != null)
                                {
                                    var saveFile = new FileInfo(Path.Combine(savePath.FullName, fileName));
                                    await CFiles.WriteAllBytesAsync(saveFile, data).AsUniTask();
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Debug.LogError(err);
                        }
                        finally
                        {
                            p?.Add(1);
                        }
                    }
                    CFiles.WriteAllText(update_version_file, update_version_txt);
                }

            }
        }

        public static async UniTask<string> LoadTextFromJarAsync(string name)
        {
            try
            {
                var www = await UnityWebRequestAsync(name);
                return www.downloadHandler.text;
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
            return null;
        }
        public static async UniTask<byte[]> LoadFromJarAsync(string name)
        {
            try
            {
                var www = await UnityWebRequestAsync(name);
                return www.downloadHandler.data;
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
            return null;
        }

        public static async UniTask<UnityWebRequest> UnityWebRequestAsync(string name)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                var www = UnityWebRequest.Get($"jar:file://{Application.dataPath}!/assets/{name.ReplaceAll('\\', '/')}");
                await www.SendWebRequest().ToUniTask();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    return null;
                }
                return www;
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                var www = UnityWebRequest.Get($"file://{Application.dataPath}/StreamingAssets/{name.ReplaceAll('\\', '/')}");
                await www.SendWebRequest().ToUniTask();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    return null;
                }
                return www;
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                var www = UnityWebRequest.Get($"file://{Application.dataPath}/Raw/{name.ReplaceAll('\\', '/')}");
                await www.SendWebRequest().ToUniTask();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    return null;
                }
                return www;
            }
            else
            {
                var www = UnityWebRequest.Get($"file://{Application.streamingAssetsPath}/{name.ReplaceAll('\\', '/')}");
                await www.SendWebRequest().ToUniTask();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    return null;
                }
                return www;
            }
        }

    }
}

#endif 