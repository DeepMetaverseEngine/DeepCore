using DeepCore.Unity3D.UGUI;
using UnityEditor;
using UnityEngine;

namespace DeepCore.Unity3D.Editor.UGUIEditor
{
    //---------------------------------------------------------------------------------------------------------------

    [CanEditMultipleObjects, CustomEditor(typeof(ImageFontGraphics), true)]
    public class ImageFontGraphicsEditor : UnityEditor.UI.GraphicEditor
    {
        private SerializedProperty m_Text;
        private SerializedProperty m_Anchor;
        private SerializedProperty m_TextOffset;

        protected override void OnEnable()
        {
            this.m_Text = base.serializedObject.FindProperty("m_Text");
            this.m_Anchor = base.serializedObject.FindProperty("m_Anchor");
            this.m_TextOffset = base.serializedObject.FindProperty("m_TextOffset");

            base.OnEnable();
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            base.serializedObject.Update();
            EditorGUILayout.PropertyField(this.m_Text, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.m_Anchor, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.m_TextOffset, new GUILayoutOption[0]);
            base.serializedObject.ApplyModifiedProperties();

        }
    }
}
