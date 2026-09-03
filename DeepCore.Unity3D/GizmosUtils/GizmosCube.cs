using DeepCore;
using UnityEngine;

namespace DeepGame3D.Unity.GizmosUtils
{
    public class GizmosCube : MonoBehaviour
    {
        
        [Tooltip("高")]
        public Vector3 Expose = Vector3.one;
        [Tooltip("线框颜色")]
        public Color Color = Color.green;

        void OnDrawGizmos()
        {
            if (transform == null) return;
            var defaultMatrix = Gizmos.matrix;
            var defaultColor = Gizmos.color;
            var trans = transform;
            var localScale = trans.localScale;
            var rotation = trans.rotation;
            var pos = trans.position;
            var matrix1 = Matrix4x4.identity;
            matrix1.SetTRS(pos, rotation, localScale);
            try
            {
                Gizmos.DrawCube(Vector3.zero, Expose);
            }
            finally
            {

                // 恢复默认颜色
                Gizmos.color = defaultColor;
                // 恢复默认矩阵
                Gizmos.matrix = defaultMatrix;
            }
        }
    }
}
