using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.UnityEditor.Asset
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using System.IO;
    using global::UnityEditor;

    public class AssetSelectPopUpWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> items = null;
        //是否导出脚本
        public static bool exportWithScript = false;
        private bool[] selectionStates;


        #region editor菜单相关


        [MenuItem("Assets/DeepCore/导出Unity资源包")]
        public static void ExportWithoutScript()
        {
            exportWithScript = false;
            ShowWindow();
        }


        [MenuItem("Assets/DeepCore/导出Unity资源包(包含脚本)")]
        public static void ExportWithScript()
        {
            exportWithScript = true;
            ShowWindow();
        }

        public static void ShowWindow()
        {
            AssetSelectPopUpWindow wnd = GetWindow<AssetSelectPopUpWindow>();
            wnd.titleContent = new GUIContent("资源导出");
            wnd.minSize = new Vector2(450, 200);
            wnd.maxSize = new Vector2(1920, 720);
            wnd.Show();
        }
        #endregion




        public void GetAllFiles(bool withScript)
        {
            //获取鼠标选中的所有文件
            Object[] selectedObjects = Selection.GetFiltered<Object>(SelectionMode.Assets);
            List<string> assetPathNames = new List<string>();
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                string directoryPath = AssetDatabase.GetAssetPath(selectedObjects[i]);
                if (directoryPath != null)
                {
                    //如果是文件夹，就遍历文件夹下的所有资源
                    if (Directory.Exists(directoryPath))
                    {
                        string[] folders = Directory.GetFiles(directoryPath);
                        for (int j = 0; j < folders.Length; j++)
                        {
                            //过滤掉.meta文件
                            if (!folders[j].EndsWith(".meta"))
                            {
                                assetPathNames.Add(folders[j]);
                            }
                        }
                    }
                    else
                    {
                        assetPathNames.Add(directoryPath);
                    }
                }
            }

            items = new List<string>();

            for (int i = 0; i < assetPathNames.Count; i++)
            {
                var depends = AssetDatabase.GetDependencies(assetPathNames[i], true);
                for (int j = 0; j < depends.Length; j++)
                {
                    AddFiles(withScript, depends[j]);
                }
            }


            items.Sort();
            selectionStates = new bool[items.Count];
            //默认全选 
            SelectAllItems();

        }


        private void OnEnable()
        {
            GetAllFiles(exportWithScript);
        }

        //打印所有选择的文件
        private void ShowFiles()
        {
            for (int i = 0; i < items.Count; i++)
            {
                Debug.Log($"all Files is {items[i]}");
            }
        }
        private void AddFiles(bool withScript, string filePath)
        {
            //特定的目录不处理
            if (filePath.StartsWith("Packages/"))
            {
                return;
            }
            if (withScript || !filePath.EndsWith(".cs"))
            {
                if (!items.Contains(filePath))
                {
                    items.Add(filePath);
                }
            }
        }
        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Scroll view
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollView.scrollPosition;

                for (int i = 0; i < items.Count; i++)
                {
                    selectionStates[i] = EditorGUILayout.ToggleLeft(items[i], selectionStates[i]);
                }
            }

            GUILayout.Space(10);

            // Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全选"))
            {
                SelectAllItems();
            }
            if (GUILayout.Button("全不选"))
            {
                DeselectAllItems();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("导出"))
            {
                OutputSelectedItems();

            }
        }
        #region 按钮事件
        private void SelectAllItems()
        {
            for (int i = 0; i < selectionStates.Length; i++)
            {
                selectionStates[i] = true;
            }
        }

        private void DeselectAllItems()
        {
            for (int i = 0; i < selectionStates.Length; i++)
            {
                selectionStates[i] = false;
            }
        }

        private void OutputSelectedItems()
        {
            List<string> exportItems = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                if (selectionStates[i])
                {
                    exportItems.Add(items[i]);
                }
            }
            if (exportItems.Count == 0)
            {
                EditorUtility.DisplayDialog("没有任何选中的资源", "请选择想要导出的资源", "OK");
                return;
            }
            var path = EditorUtility.SaveFilePanel("导出资源包", "", "", "unitypackage");
            if (path == "")
                return;

            var flag = ExportPackageOptions.Interactive | ExportPackageOptions.Recurse;
            //如果选择完后还想导出该资源关联的资源可以添加ExportPackageOptions.IncludeDependencies的Flag
            //if (exportWithScript)
            //{
            //    flag = flag | ExportPackageOptions.IncludeDependencies;
            //}
            AssetDatabase.ExportPackage(exportItems.ToArray(), path, flag);
            Close();

        }
        #endregion



    }
}
