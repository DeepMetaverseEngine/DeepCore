using UnityEngine;

namespace Code.BattleView.GizmosUtils
{
    public class GizmosCylinder : MonoBehaviour
    {
        [Tooltip("朝向")]
        public float Radians;
        [Tooltip("高")]
        public float Height = 1;
        [Tooltip("圆环的半径")]
        public float Radius = 1; 
        [Tooltip("圆平滑度，值越低圆环越平滑")]
        public float Theta = 0.1f;
        [Tooltip("线框颜色")]
        public Color Color = Color.green; 
       
        void OnDrawGizmos()
        {
            if (transform == null) return;
            if (Theta < 0.0001f) Theta = 0.0001f;
 
            var defaultMatrix = Gizmos.matrix;
            var defaultColor = Gizmos.color;
            
            var trans = transform;
            var localScale = trans.localScale;
            var rotation = trans.rotation;
            var pos = trans.position;
            
            var matrix1 = Matrix4x4.identity;
            matrix1.SetTRS(pos, rotation, localScale);
            var matrix2 = Matrix4x4.identity;
            matrix2.SetTRS(new Vector3(pos.x, pos.y + Height, pos.z), rotation, localScale);
            // 设置颜色
            Gizmos.color = Color;
 
            // 绘制圆环
            Vector3 beginPoint = Vector3.zero;
            Vector3 firstPoint = Vector3.zero;
            for (float theta = 0; theta < 2 * Mathf.PI; theta += this.Theta)
            {
                float x = Radius * Mathf.Cos(theta);
                float z = Radius * Mathf.Sin(theta);
                Vector3 endPoint = new Vector3(x, 0, z);
                if (theta == 0)
                {
                    firstPoint = endPoint;
                }
                else
                {
                    Gizmos.matrix = matrix1;
                    Gizmos.DrawLine(beginPoint, endPoint);
                    Gizmos.matrix = matrix2;
                    Gizmos.DrawLine(beginPoint, endPoint);
                    Gizmos.matrix = defaultMatrix;
                    Gizmos.DrawLine(matrix1.MultiplyPoint3x4(beginPoint), matrix2.MultiplyPoint3x4(beginPoint));
                }
                beginPoint = endPoint;
            }
 
            // 绘制最后一条线段
            Gizmos.matrix = matrix1;
            Gizmos.DrawLine(firstPoint, beginPoint);
            Gizmos.matrix = matrix2;
            Gizmos.DrawLine(firstPoint, beginPoint);
            Gizmos.matrix = defaultMatrix;
            Gizmos.DrawLine(matrix1.MultiplyPoint3x4(beginPoint), matrix2.MultiplyPoint3x4(beginPoint));
            
            // 朝向
            var rot = Quaternion.AngleAxis(-Radians * Mathf.Rad2Deg, Vector3.up);
            Gizmos.DrawLine(pos, pos + rot * Vector3.right * 100);
            
            // 恢复默认颜色
            Gizmos.color = defaultColor;
 
            // 恢复默认矩阵
            Gizmos.matrix = defaultMatrix;
        }
    }
}
