using DeepCore.Unity3D.UGUIEditor;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace DeepCore.Unity3D.Editor.UGUIEditor
{
    //---------------------------------------------------------------------------------------------------------------

    [CanEditMultipleObjects, CustomEditor(typeof(UILayoutGraphics), true)]
    public class UILayoutGraphicsEditor : ImageEditor
    {
        private SerializedProperty m_IsShowUILayout;

        protected override void OnEnable()
        {
            this.m_IsShowUILayout = base.serializedObject.FindProperty("m_IsShowUILayout");
            base.OnEnable();
        }
        public override void OnInspectorGUI()
        {
            base.serializedObject.Update();
            EditorGUILayout.PropertyField(this.m_IsShowUILayout, new GUILayoutOption[0]);
            base.serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}