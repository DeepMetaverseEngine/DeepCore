using DeepCore.Unity.ResourceViewer;
using System;
using UnityEditor;
using UnityEngine;
namespace DeepCore.UnityEditor
{
    public static class MissingScriptHelper
    {
        [MenuItem("Assets/DeepCore/移除Missing脚本")]
        private static void RemoveMissingScript()
        {
            var paths = EditorUtils.GetSelectionAssetPaths("t:prefab");
            var i = 0;
            foreach (var path in paths)
            {
                i++;
                EditorUtility.DisplayProgressBar("删除丢失脚本", $"...{i}/{paths.Length}", 1f * i / paths.Length);
                if (!path.EndsWith(".prefab")) continue;
                RemoveMissingScript(path);
            }

            EditorUtility.ClearProgressBar();
        }

        public static void RemoveMissingScript(string path)
        {
            var deps = AssetDatabase.GetDependencies(new[] { path });
            if (deps.Length > 0)
            {
                foreach (var dep in deps)
                {
                    if (!dep.EndsWith(".prefab") || dep.Equals(path)) continue;
                    RemoveMissingScript(dep);
                }
            }

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (RemoveMissingScript(go) <= 0) return;
            EditorUtility.SetDirty(go);
            AssetDatabase.SaveAssets();
        }

        public static int RemoveMissingScript(GameObject go)
        {
            int count = 0;
            foreach (Transform child in go.transform)
            {
                count += RemoveMissingScript(child.gameObject);
            }

            return GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go) + count;
        }





    }
}