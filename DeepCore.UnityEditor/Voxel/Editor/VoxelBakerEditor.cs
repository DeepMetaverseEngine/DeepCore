using DeepCore.IO;
using DeepCore.Unity;
using DeepCore.Unity3D.Impl;
using DeepCore.Unity3D.Voxel;
using DeepCore.UnityEditor.Expose;
using DeepCore.Voxel.Data;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepCore.UnityEditor.Voxel
{
    [CustomEditor(typeof(VoxelAABB))]
    class VoxelAABBHelperEditor : Editor
    {
        private bool rayCastFoldout = false;
        private bool layerIgnoreFoldout = false;
        private bool layerIncludeFoldout = false;
        private bool layerMapFoldout = false;
        public override void OnInspectorGUI()
        {
            if (this.target is VoxelAABB config)
            {
                EditorGUILayout.Separator();
                {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.Label("烘培体素", EditorStyles.boldLabel);
                    config.boundsWH = EditorGUILayout.Vector2Field("AABB 长宽尺寸", config.boundsWH);
                    config.boundsTop = EditorGUILayout.FloatField("AABB 顶部", config.boundsTop);
                    config.boundsBottom = EditorGUILayout.FloatField("AABB 底部", config.boundsBottom);
                    config.width = EditorGUILayout.FloatField("体素体积", config.width);
                    config.rayWidth = EditorGUILayout.FloatField("射线宽度", config.rayWidth);
                    config.minHeight = EditorGUILayout.FloatField("最小高度", config.minHeight);
                    config.m_DebugLineColor = EditorGUILayout.ColorField("默认颜色", config.m_DebugLineColor);
                    config.m_DebugCube = (Transform)EditorGUILayout.ObjectField("Gizmos Cube模板", config.m_DebugCube, typeof(Transform), default(Transform));
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(config);
                    }
                }
                EditorGUILayout.Separator();
                {
                    this.rayCastFoldout = EditorGUILayout.Foldout(rayCastFoldout, "射线设置");
                    if (this.rayCastFoldout)
                    {
                        EditorGUI.BeginChangeCheck();
                        config.useMeshColor = EditorGUILayout.Toggle("使用MeshColor", config.useMeshColor);
                        config.useNavMesh = EditorGUILayout.Toggle("使用NavMesh", config.useNavMesh);
                        config.useBoxCast = EditorGUILayout.Toggle("使用BoxCast", config.useBoxCast);
                        config.singleLayer = EditorGUILayout.Toggle("只保留一根体素", config.singleLayer);
                        config.splitVoxels = EditorGUILayout.Toggle("掏空体素", config.splitVoxels);
                        config.onlyMeshCollider = EditorGUILayout.Toggle("仅测试MeshCollider", config.onlyMeshCollider);
                        config.autoBindMeshCollider = EditorGUILayout.Toggle("自动绑定MeshCollider", config.autoBindMeshCollider);
                        config.raycastLimit = EditorGUILayout.FloatField("射线限制", config.raycastLimit);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(config);
                        }
                    }
                }
                EditorGUILayout.Separator();
                {
                    this.layerIgnoreFoldout = EditorGUILayout.Foldout(layerIgnoreFoldout, "忽略的层级");
                    if (this.layerIgnoreFoldout)
                    {
                        EditorGUI.BeginChangeCheck();
                        config.ignoreLayers = DeepEditorGUI.ArrayField(config.ignoreLayers, (i, e) =>
                        {
                            var layer = LayerMask.NameToLayer(e);
                            layer = EditorGUILayout.LayerField(layer);
                            return LayerMask.LayerToName(layer);
                        }, "忽略：", 100, () => "Default");
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(config);
                        }
                    }
                    this.layerIncludeFoldout = EditorGUILayout.Foldout(layerIncludeFoldout, "仅包含的层级，优先于忽略");
                    if (this.layerIncludeFoldout)
                    {
                        EditorGUI.BeginChangeCheck();
                        config.includeLayers = DeepEditorGUI.ArrayField(config.includeLayers, (i, e) =>
                        {
                            var layer = LayerMask.NameToLayer(e);
                            layer = EditorGUILayout.LayerField(layer);
                            return LayerMask.LayerToName(layer);
                        }, "包含：", 100, () => "Default");
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(config);
                        }
                    }
                }
                EditorGUILayout.Separator();
                {
                    this.layerMapFoldout = EditorGUILayout.Foldout(layerMapFoldout, "颜色设置");
                    if (this.layerMapFoldout)
                    {
                        EditorGUI.BeginChangeCheck();
                        config.layerMap = DeepEditorGUI.ArrayField(config.layerMap, (i, e) =>
                        {
                            var layer = LayerMask.NameToLayer(e.LayerName);
                            var color = e.LayerColor;
                            layer = EditorGUILayout.LayerField(layer);
                            color = EditorGUILayout.ColorField(color);
                            return new(LayerMask.LayerToName(layer), color);
                        }, "层级颜色：", 100, () => new VoxelAABB.LayerTuple("Default", Color.green));
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(config);
                        }
                    }
                }
                EditorGUI.BeginChangeCheck();
                /*
                try
                {
                    var layer = LayerMask.NameToLayer(config.layerBaseLine);
                    layer = EditorGUILayout.LayerField("BaseLine Layer", layer);
                    config.layerBaseLine = LayerMask.LayerToName(layer);
                }
                finally
                {
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(config);
                    }
                }
                */
                EditorGUILayout.Separator();
                {
                    var style = new GUIStyle(UnityEngine.GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
                    if (GUILayout.Button("Bake Voxel", style))
                    {
                        Bake(config, DebugType.None);
                    }
                    if (GUILayout.Button("Bake Voxel (显示烘焙的Mesh)", style))
                    {
                        Bake(config, DebugType.BakeMesh);
                    }
                    if (GUILayout.Button("Bake Voxel (显示SingleMesh，非常耗时建议小范围使用)", style))
                    {
                        Bake(config, DebugType.SingleVoxel);
                    }
                    if (GUILayout.Button("Bind MeshCollider", style))
                    {
                        BindColliders(config);
                    }
                }
                EditorGUILayout.Separator();

            }
        }
        public enum DebugType
        {
            None,
            BakeMesh,
            SingleVoxel,
        }
        private FileInfo Bake(VoxelAABB config, DebugType trace = DebugType.None)
        {
            var voxtPath = new FileInfo(
                                Application.dataPath + "/../_output/voxelbaker/" +
                                SceneManager.GetActiveScene().path.Replace(".unity", "/").ToLower() +
                                SceneManager.GetActiveScene().name.ToLower() + ".voxt");
            try
            {
                if (new DirectoryInfo(Application.dataPath).TryFindParentDirectory(Path.Combine("GameEditor"), out var editorRoot))
                {
                    voxtPath = new FileInfo(editorRoot.FullName + "/vox/" +SceneManager.GetActiveScene().name.ToLower() + ".voxt");
                }
                else if (new DirectoryInfo(Application.dataPath).TryFindParentDirectory(Path.Combine("Data", "GameEditor"), out editorRoot))
                {
                    voxtPath = new FileInfo(editorRoot.FullName + "/vox/" + SceneManager.GetActiveScene().name.ToLower() + ".voxt");
                }
                if (config.transform.TryGetComponentInChildren<VoxelTest>(out var test))
                {
                    GameObject.DestroyImmediate(test.gameObject);
                }
                var baker = new VoxelBaker(config, voxtPath);
                var voxdata = baker.RunBakeToXml((text, rate) =>
                {
                    return EditorUtility.DisplayCancelableProgressBar("Bake Voxel Data", text, rate);
                });
                UnityDriver.ShowInFolder(voxtPath.Directory);
                var cube = config.m_DebugCube != null ? config.m_DebugCube.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
                try
                {
                    switch (trace)
                    {
                        case DebugType.SingleVoxel:
                            {
                                var testobj = VoxelToMesh.InstancingTerrainData(voxdata, cube);
                                testobj.transform.SetParent(config.transform, true);
                            }
                            break;
                        case DebugType.BakeMesh:
                            {
                                var vox = new VoxelTerrain3D(voxdata, VoxelTerrainData.CreateVoxelBuildConfig(voxdata));
                                var testobj = VoxelToMesh.InstancingVoxelTerrainChunks(vox, cube);
                                testobj.transform.SetParent(config.transform, true);
                            }
                            break;
                    }
                }
                finally
                {
                    if (config.m_DebugCube == null)
                    {
                        GameObject.DestroyImmediate(cube);
                    }
                }
            }
            finally
            {
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }
            return voxtPath;
        }
        private void BindColliders(VoxelAABB config)
        {
            foreach (var o in config.gameObject.scene.GetRootGameObjects())
            {
                o.transform.ForEachDeep(t =>
                {
                    var go = t.gameObject;
                    if (go.TryGetComponent<MeshRenderer>(out var render))
                    {
                        if (!go.TryGetComponent<MeshCollider>(out var collider))
                        {
                            collider = go.AddComponent<MeshCollider>();
                        }
                    }
                });
            }
        }
    }

    // 下面的代码是为了兼容旧版本的VoxelBakerEditor
#if false

    public class VoxelBakerEditor : EditorWindow
    {
        [MenuItem("DeepCore/VoxelBaker")]
        public static void Open()
        {
            VoxelBakerEditor win = (VoxelBakerEditor)EditorWindow.GetWindow(typeof(VoxelBakerEditor));
            win.Show();
        }
        //private static VoxelBakerConfig config = new VoxelBakerConfig();
        // Start is called before the first frame update
        void OnGUI()
        {
             GUILayout.Label("---------------------", EditorStyles.boldLabel);
             {
                 {
                     GUILayout.Label("烘培体素", EditorStyles.boldLabel);
                     config.width = EditorGUILayout.FloatField("体素体积", config.width);
                     config.minHeight = EditorGUILayout.FloatField("最小高度", config.minHeight);
                     config.minLevel = EditorGUILayout.FloatField("最低上沿", config.minLevel);
                     config.boxExtend = EditorGUILayout.FloatField("射线宽度", config.boxExtend);
                     config.useMeshColor = EditorGUILayout.Toggle("使用MeshColor", config.useMeshColor);
                     config.useNavMesh = EditorGUILayout.Toggle("使用NavMesh", config.useNavMesh);
                     config.useBoxCast = EditorGUILayout.Toggle("使用BoxCast", config.useBoxCast);
                     config.singleLayer = EditorGUILayout.Toggle("只保留一根体素", config.singleLayer);
                     config.splitVoxels = EditorGUILayout.Toggle("掏空体素", config.splitVoxels);
                     config.onlyMeshCollider = EditorGUILayout.Toggle("仅测试MeshCollider", config.onlyMeshCollider);
                     config.autoBindMeshCollider = EditorGUILayout.Toggle("自动绑定MeshCollider", config.autoBindMeshCollider);
                     config.raycastLimit = EditorGUILayout.FloatField("射线限制", config.raycastLimit);
                 }
                 GUILayout.Label("---------------------", EditorStyles.boldLabel);
                 {
                     config.ignoreLayers = EditorGUILayout.TextField("Ignore Layers，逗号','分隔", config.ignoreLayers);
                     config.includeLayers = EditorGUILayout.TextField("Include Layers，逗号','分隔", config.includeLayers);
                     config.layerBaseLine = EditorGUILayout.TextField("BaseLine Layer", config.layerBaseLine);
                     config.layerNavLayer = EditorGUILayout.TextField("Nav Layer", config.layerNavLayer);
                     config.layerWater = EditorGUILayout.TextField("Water Layer", config.layerWater);
                     config.layerDummyLayer = EditorGUILayout.TextField("Dummy Layer", config.layerDummyLayer);
                     config.layerNotWalkable = EditorGUILayout.TextField("NotWalkable Layer", config.layerNotWalkable);
                 }
                 GUILayout.Label("---------------------", EditorStyles.boldLabel);
                 GUILayout.BeginHorizontal();
                 if (GUILayout.Button("bake to xml", GUILayout.Width(128)))
                 {
                     var root = SceneManager.GetActiveScene().GetRootGameObjects().FindDeep<VoxelAABB>(t => t.gameObject.GetComponent<VoxelAABB>());
                     var voxtPath = (
                             Application.dataPath + "/../_output/voxelbaker/" +
                             SceneManager.GetActiveScene().path.Replace(".unity", "/").ToLower() +
                             SceneManager.GetActiveScene().name.ToLower() + ".voxt");
                     try
                     {
                         new VoxelBaker(config).RunBakeToXml(root, voxtPath, (text, rate) => EditorUtility.DisplayCancelableProgressBar("Bake Voxel Data", text, rate));
                     }
                     finally
                     {
                         AssetDatabase.Refresh();
                         EditorUtility.ClearProgressBar();
                     }
                 }
                 GUILayout.EndHorizontal();
                 GUILayout.Label("---------------------", EditorStyles.boldLabel);
          }
        }
    }
#endif

}