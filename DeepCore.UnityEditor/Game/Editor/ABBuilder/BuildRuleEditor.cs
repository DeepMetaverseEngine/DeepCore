using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildRule))]
public class BuildRulesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var script = target as BuildRule;
        var config = AssetDatabase.LoadAssetAtPath<BuildConfig>(BuildConfig.DefaultBuildConfigPath);
        if (config != null && config.ActiveRule == script)
        {
            GUILayout.TextField("规则已激活");
        }
        else
        {
            GUILayout.Space(5f);
            if (GUILayout.Button("激活规则", GUILayout.Height(40)))
            {
                if (config == null)
                {
                    config = CreateInstance<BuildConfig>();
                    AssetDatabase.CreateAsset(config, BuildConfig.DefaultBuildConfigPath);
                }

                config.ActiveRule = script;
                EditorUtility.SetDirty(config);
                
                AssetDatabase.SaveAssets();
            }
        }
    }
}
