using DeepCore.Unity3D.UGUI;
using UnityEditor;
using UnityEngine;

namespace DeepCore.Unity3D.Editor.UGUIEditor
{
    //---------------------------------------------------------------------------------------------------------------

    [CanEditMultipleObjects, CustomEditor(typeof(TextGraphics), true)]
    public class TextGraphicsEditor : UnityEditor.UI.TextEditor
    {
        private SerializedProperty mTextOffset;
        private SerializedProperty mIsUnderline;
        private SerializedProperty mUnderlineChar;
        private SerializedProperty mUnderlineType;

        protected override void OnEnable()
        {
            this.mTextOffset = base.serializedObject.FindProperty("m_TextOffset");
            this.mIsUnderline = base.serializedObject.FindProperty("m_IsUnderline");
            this.mUnderlineChar = base.serializedObject.FindProperty("m_UnderlineChar");
            this.mUnderlineType = base.serializedObject.FindProperty("m_UnderlineType");
            base.OnEnable();
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            base.serializedObject.Update();
            EditorGUILayout.PropertyField(this.mTextOffset, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.mIsUnderline, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.mUnderlineChar, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(this.mUnderlineType, new GUILayoutOption[0]);
            base.serializedObject.ApplyModifiedProperties();

        }
    }
}
