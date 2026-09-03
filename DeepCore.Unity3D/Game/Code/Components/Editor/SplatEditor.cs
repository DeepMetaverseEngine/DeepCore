using SkillIndicator.Basic;
using UnityEditor;

namespace SkillIndicator.Editor
{
    public class SplatEditor<T> : UnityEditor.Editor where T : Splat
    {
        private T instance => (T)target;

        public override void OnInspectorGUI()
        {
            if (instance == null)
                return;

            EditorGUI.BeginChangeCheck();

            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                instance.OnValueChanged();
            }
        }
    }
    
    
    [CustomEditor(typeof(LineMissile))]
    public class LineMissileEditor : SplatEditor<LineMissile> { }
    
    
    [CustomEditor(typeof(Cone))]
    public class ConeEditor : SplatEditor<Cone> { }

    [CustomEditor(typeof(Point))]
    public class PointEditor : SplatEditor<Point> { }

    [CustomEditor(typeof(AngleMissile))]
    public class AngleMissileEditor : SplatEditor<AngleMissile> { }

    [CustomEditor(typeof(RangeIndicator))]
    public class RangeIndicatorEditor : SplatEditor<RangeIndicator> { }

    
    
}