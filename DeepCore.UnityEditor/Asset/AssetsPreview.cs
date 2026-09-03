using DeepCore.Unity.ResourceViewer;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DeepCore.UnityEditor.Asset
{
    public class AssetsPreview
    {
        //private static HashMap<UnityEngine.Object, Texture2D> _itemsPreviews = new HashMap<UnityEngine.Object, Texture2D>();
        //         public static Texture2D GetPreviewForced(UnityEngine.Object obj)
        //         {
        //             if (obj == null)
        //                 return null;
        // 
        //             int instanceId = obj.GetInstanceID();
        // 
        //             if (_itemsPreviews.ContainsKey(obj))
        //             {
        //                 return _itemsPreviews[obj];
        //             }
        //             else
        //             {
        //                 if (Application.isPlaying)
        //                     EditorUtility.SetDirty(obj);
        // 
        //                 Texture2D result = AssetPreview.GetAssetPreview(obj);
        //                 int tries = 1000;
        //                 while (AssetPreview.IsLoadingAssetPreview(instanceId) && tries > 0)
        //                 {
        //                     tries--;
        //                 }
        // 
        //                 if (tries != 0)
        //                     _itemsPreviews.Add(obj, result);
        //                 return result;
        //             }
        //         }


        [MenuItem("Assets/DeepCore/Bind Resource Info")]
        private static void BindResourceInfo()
        {
            var paths = EditorUtils.GetSelectionAssetPaths("t:prefab");
            var i = 0;
            foreach (var path in paths)
            {
                i++;
                EditorUtility.DisplayProgressBar("Bind Resource Info", $"...{i}/{paths.Length}", 1f * i / paths.Length);
                if (!path.EndsWith(".prefab")) continue;
                try
                {
                    BindResourceInfo(path);
                }
                catch (Exception err)
                {
                    Debug.LogError(err);
                }
            }
            EditorUtility.ClearProgressBar();
        }
        public static void BindResourceInfo(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            BindResourceInfo(go);
            EditorUtility.SetDirty(go);
            AssetDatabase.SaveAssets();
        }

        public static void BindResourceInfo(GameObject go)
        {
            if (!go.TryGetComponent<ResourceInfo>(out var info))
            {
                info = go.AddComponent<ResourceInfo>();
            }
            info.Refresh();
        }



        /// <summary>
        /// 获取预览图象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        //         private Texture GetAssetPreview(GameObject obj)
        //         {
        //             GameObject clone = GameObject.Instantiate(obj);
        //             Transform cloneTransform = clone.transform;
        //             cloneTransform.position = new Vector3(-1000, -1000, -1000);
        //             //cloneTransform.localRotation = new Quaternion(0, 0, 0, 1);
        // 
        //             Transform[] all = clone.GetComponentsInChildren<Transform>();
        //             foreach (Transform trans in all)
        //             {
        //                 trans.gameObject.layer = 21;
        //             }
        // 
        //             Bounds bounds = GetBounds(clone);
        //             Vector3 Min = bounds.min;
        //             Vector3 Max = bounds.max;
        //             GameObject cameraObj = new GameObject("render camera");
        //             cameraObj.transform.position = new Vector3(cloneTransform.position.x, (Max.y + Min.y) / 2f, Max.z + (Max.z - Min.z));
        // 
        //             Vector3 center = new Vector3(cloneTransform.position.x, (Max.y + Min.y) / 2f, cloneTransform.position.z);
        // 
        //             cameraObj.transform.LookAt(center);
        // 
        //             Camera renderCamera = cameraObj.AddComponent<Camera>();
        //             renderCamera.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        //             renderCamera.clearFlags = CameraClearFlags.Color;
        //             renderCamera.cameraType = CameraType.Preview;
        //             renderCamera.cullingMask = 1 << 21;
        //             int angle = (int)(Mathf.Atan2((Max.y - Min.y) / 2, (Max.z - Min.z)) * 180 / 3.1415f * 2);
        //             renderCamera.fieldOfView = angle;
        // 
        //             RenderTexture texture = new RenderTexture(64, 64, 0, RenderTextureFormat.Default);
        //             renderCamera.targetTexture = texture;
        // 
        //             renderCamera.RenderDontRestore();
        // 
        //             RenderTexture tex = new RenderTexture(64, 64, 0, RenderTextureFormat.Default);
        //             Graphics.Blit(texture, tex);
        // 
        //             Object.DestroyImmediate(clone);
        //             Object.DestroyImmediate(cameraObj);
        // 
        //             return tex;
        //         }
        //         /// <summary>
        //         /// 获得某物体的bounds
        //         /// </summary>
        //         /// <param name="obj"></param>
        //         private Bounds GetBounds(GameObject obj)
        //         {
        //             Vector3 Min = new Vector3(99999, 99999, 99999);
        //             Vector3 Max = new Vector3(-99999, -99999, -99999);
        //             MeshRenderer[] renders = obj.GetComponentsInChildren<MeshRenderer>();
        //             for (int i = 0; i < renders.Length; i++)
        //             {
        //                 if (renders[i].bounds.min.x < Min.x)
        //                     Min.x = renders[i].bounds.min.x;
        //                 if (renders[i].bounds.min.y < Min.y)
        //                     Min.y = renders[i].bounds.min.y;
        //                 if (renders[i].bounds.min.z < Min.z)
        //                     Min.z = renders[i].bounds.min.z;
        // 
        //                 if (renders[i].bounds.max.x > Max.x)
        //                     Max.x = renders[i].bounds.max.x;
        //                 if (renders[i].bounds.max.y > Max.y)
        //                     Max.y = renders[i].bounds.max.y;
        //                 if (renders[i].bounds.max.z > Max.z)
        //                     Max.z = renders[i].bounds.max.z;
        //             }
        // 
        //             Vector3 center = (Min + Max) / 2;
        //             Vector3 size = new Vector3(Max.x - Min.x, Max.y - Min.y, Max.z - Min.z);
        //             return new Bounds(center, size);
        //         }
    }
}
