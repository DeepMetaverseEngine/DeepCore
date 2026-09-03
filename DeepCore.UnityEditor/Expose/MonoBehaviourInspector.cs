using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using DeepCore.Unity.Expose;

namespace DeepCore.UnityEditor.Expose
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class MonoBehaviourInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Type type = target.GetType();

            var exposedProperties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).
                Where(item => item.IsDefined(typeof(ExposePropertyAttribute), true)).ToArray();
            if (exposedProperties.Length > 0)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
                foreach (PropertyInfo propertyInfo in exposedProperties)
                {
                    DrawProperty(propertyInfo);
                }
            }

            if (UnityEngine.GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        private void DrawProperty(PropertyInfo propertyInfo)
        {
            try
            {
                bool hasGetMethod = propertyInfo.GetGetMethod(true) != null;
                if (!hasGetMethod)
                {
                    return;
                }

                bool hasSetMethod = propertyInfo.GetSetMethod(true) != null;
                UnityEngine.GUI.enabled = hasSetMethod;

                object value = TypeDrawer.Draw(propertyInfo.PropertyType, propertyInfo.Name, propertyInfo.GetValue(target, null));
                if (hasSetMethod)
                {
                    propertyInfo.SetValue(target, value, null);
                }
            }
            catch (Exception exception)
            {
                EditorGUILayout.LabelField(exception.ToString());
            }
        }
    }

    public static class TypeDrawer
    {
        public static object Draw(Type type, string name, object getValue)
        {
            object value = null;
            if (typeof(int) == type)
            {
                value = EditorGUILayout.IntField(name, (int)getValue);
            }
            else if (typeof(long) == type)
            {
                value = EditorGUILayout.LongField(name, (long)getValue);
            }
            else if (typeof(float) == type)
            {
                value = EditorGUILayout.FloatField(name, (float)getValue);
            }
            else if (typeof(string) == type)
            {
                value = EditorGUILayout.TextField(name, (string)getValue);
            }
            else if (typeof(bool) == type)
            {
                value = EditorGUILayout.Toggle(name, (bool)getValue);
            }
            else if (typeof(Vector2) == type)
            {
                value = EditorGUILayout.Vector2Field(name, (Vector2)getValue);
            }
            else if (typeof(Vector3) == type)
            {
                value = EditorGUILayout.Vector3Field(name, (Vector3)getValue);
            }
            else if (typeof(Vector4) == type)
            {
                value = EditorGUILayout.Vector4Field(name, (Vector4)getValue);
            }
            else if (type.IsEnum)
            {
                if (type.IsDefined(typeof(FlagsAttribute), true))
                    value = EditorGUILayout.EnumFlagsField(name, (Enum)getValue);
                else
                    value = EditorGUILayout.EnumPopup(name, (Enum)getValue);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                value = EditorGUILayout.ObjectField(name, (UnityEngine.Object)getValue, type, true);
            }
            return value;
        }
    }

}
