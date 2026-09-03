using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildConfig))]
public class BuildConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var config = target as BuildConfig;
        GUILayout.Space(5f);
        if (GUILayout.Button("Build Rule", GUILayout.Height(40)))
        {
            if (config.ActiveRule == null)
            {
                EditorUtility.DisplayDialog("ABBuildConfig", "请先激活一个打包规则！", "Close");
            }
            else
            {
                config.ActiveRule.BuildAll();
            }
        }
    }
}
