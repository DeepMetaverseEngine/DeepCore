using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeepCore.UnityEditor.Expose
{
    public static class DeepEditorGUI
    {
        public delegate T DrawArrayElement<T>(int index, T value);

        public static T[] ArrayField<T>(T[] src, DrawArrayElement<T> draw, string prefix = "Index", int prefixW = 100, Func<T> defaultT=null)
        {
            if (src == null)
            {
                src = [];
            }
            for (int i = 0; i < src.Length; i++)
            {
                var e = src[i];
                GUILayout.BeginHorizontal();
                try
                {
                    EditorGUILayout.LabelField(prefix, new GUIStyle(UnityEngine.GUI.skin.label) { alignment = TextAnchor.MiddleRight }, GUILayout.Width(prefixW));
                    src[i] = draw(i, src[i]);
                    if (GUILayout.Button("删除", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        src = CUtils.ArrayRemove(src, i);
                        break;
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("添加", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                var dt = defaultT != null ? defaultT() : default(T);
                src = CUtils.ArrayAppend(src, dt);
            }
            GUILayout.EndHorizontal();
            return src;
        }
        public static List<T> ListField<T>(List<T> src, DrawArrayElement<T> draw, string prefix = "Index", int prefixW = 100, Func<T> defaultT=null)
        {
            var array = src.ToArray();
            array = ArrayField(array, draw, prefix, prefixW, defaultT);
            src.Clear();
            src.AddRange(array);
            return src;
        }
    }
}
