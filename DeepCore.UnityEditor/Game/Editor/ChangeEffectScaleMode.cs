using DeepCore.UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IOGame.Client.UnityEditor.Editor
{
    public class ChangeEffectScaleMode
    {
        [MenuItem("Assets/DeepCore/ChangeEffectScaleMode")]
        public static void Effect()
        {
            var objects = Selection.objects;
            foreach (var obj in objects)
            {
                var o = PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj);
                Debug.Log("prefab name: " + o.name);
                string path = AssetDatabase.GetAssetPath(o);
                
                Debug.Log("Path: " + path);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null)
                {
                    Debug.Log("=============  check folder ============");
                    CheckChild();
                }
                else
                {
                    ConvertSingle(go, obj);
                }
            }
            AssetDatabase.ReleaseCachedFileHandles();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void ConvertSingle(GameObject go, Object obj)
        {
            var particles = go.GetComponentsInChildren<ParticleSystem>();
            foreach (var p in particles)
            {
                p.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            Debug.Log(" single --------- " + go.name);
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        private static void CheckChild()
        {
            string[] paths = EditorUtils.GetSelectionAssetPaths("");
            Debug.Log(string.Concat(paths, "/n"));
            foreach (string path in paths)
            {
                if (!BuildConfig.IsValidateAsset(path))
                    continue;
                
                Debug.Log("path: " + path);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var o = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(go, path);
                if (o == null)
                {
                    Debug.LogError("error: GetCorrespondingObjectFromSource   ");
                }
                ConvertSingle(go, o);
                // Debug.Log(" single --------- " + go.name);
                
            }
        }
    }
}