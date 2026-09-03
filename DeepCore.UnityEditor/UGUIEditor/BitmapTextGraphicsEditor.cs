using DeepCore.Unity3D.UGUI;
using UnityEditor;
using UnityEngine;

namespace DeepCore.Unity3D.Editor.UGUIEditor
{
    //---------------------------------------------------------------------------------------------------------------

    [CanEditMultipleObjects, CustomEditor(typeof(BitmapTextGraphics), true)]
    public class BitmapTextGraphicsEditor : UnityEditor.UI.GraphicEditor
    {
        private SerializedProperty m_Text;
        private SerializedProperty m_FontStyle;
        private SerializedProperty m_FontSize;

        private SerializedProperty m_BorderColor;
        private SerializedProperty m_BorderTimes;
        private SerializedProperty m_Anchor;
        private SerializedProperty m_TextOffset;
        private SerializedProperty m_IsUnderline;

        protected override void OnEnable()
        {
            this.m_Text = base.serializedObject.FindProperty("m_Text");
            this.m_FontStyle = base.serializedObject.FindProperty("m_FontStyle");
            this.m_FontSize = base.serializedObject.FindProperty("m_FontSize");
            this.m_BorderColor = base.serializedObject.FindProperty("m_BorderColor");
            this.m_BorderTimes = base.serializedObject.FindProperty("m_BorderTimes");
            this.m_Anchor = base.serializedObject.FindProperty("m_Anchor");
            this.m_TextOffset = base.serializedObject.FindProperty("m_TextOffset");
            this.m_IsUnderline = base.serializedObject.FindProperty("m_IsUnderline");
            base.OnEnable();
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            base.serializedObject.Update();
            EditorGUILayout.PropertyField(this.m_Text, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.m_FontStyle, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.m_FontSize, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.m_BorderColor, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.m_BorderTimes, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.m_Anchor, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.m_TextOffset, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.m_IsUnderline, new GUILayoutOption[0]);
            if (base.serializedObject.ApplyModifiedProperties())
            {
                (base.target as BitmapTextGraphics).Apply();
            }
        }
    }
}
