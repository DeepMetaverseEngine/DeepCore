using DeepCore.GUI.Display;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{
    public partial class UIVertexBuffer : VertexBuffer
    {
        private Matrix4x4 cur_matrix;
        private Stack<Matrix4x4> stack_matrix = new Stack<Matrix4x4>();
        private List<UIVertex> mVBOs = null;
        private UnityEngine.Color mBlendColor = UnityEngine.Color.white;

        public List<UIVertex> VBO { get { return mVBOs; } }
        public int Count
        {
            get
            {
                return mVBOs.Count;
            }
            set
            {
                int d = value - mVBOs.Count;
                if (d < 0)
                {
                    mVBOs.RemoveRange(value, -d);
                }
                else if (d > 0)
                {
                    for (int i = 0; i < d; i++)
                    {
                        mVBOs.Add(UIVertex.simpleVert);
                    }
                }
            }
        }
        public int IndicesCount { get { return mVBOs.Count; } }
        public UnityEngine.Color BlendColor
        {
            get { return mBlendColor; }
            set { mBlendColor = value; }
        }

        private UIVertexBuffer()
        {
        }
        public void Dispose()
        {
            this.mVBOs = null;
            this.mBlendColor = UnityEngine.Color.white;
            this.stack_matrix.Clear();
            this.cur_matrix = Matrix4x4.identity;
        }

        public void Append(VertexBuffer data)
        {
            mVBOs.AddRange(((UIVertexBuffer)data).mVBOs);
        }
        public void Append(float x, float y, uint color, float u, float v)
        {
            var position = cur_matrix.MultiplyPoint3x4(new Vector3(x, y));
            position.y = -position.y;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = UIUtils.UInt32_RGBA_To_Color(color) * mBlendColor;
            vertex.uv0 = new Vector2(u, 1f - v);
            mVBOs.Add(vertex);
        }
        public void Append(VertexPoint v)
        {
            var position = cur_matrix.MultiplyPoint3x4(new Vector3(v.X, v.Y));
            position.y = -position.y;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = UIUtils.UInt32_RGBA_To_Color(v.Color) * mBlendColor;
            vertex.uv0 = new Vector2(v.U, 1f - v.V);
            mVBOs.Add(vertex);
        }

        public void SetColor(int index, uint color)
        {
            UIVertex vertex = this.mVBOs[index];
            vertex.color = UIUtils.UInt32_RGBA_To_Color(color) * mBlendColor;
            this.mVBOs[index] = vertex;
        }
        public void SetPosition(int index, float x, float y)
        {
            var position = cur_matrix.MultiplyPoint3x4(new Vector3(x, y));
            position.y = -position.y;
            UIVertex vertex = this.mVBOs[index];
            vertex.position = position;
            this.mVBOs[index] = vertex;
        }
        public void SetTexCoords(int index, float u, float v)
        {
            UIVertex vertex = this.mVBOs[index];
            vertex.uv0 = new Vector2(u, 1f - v);
            this.mVBOs[index] = vertex;
        }
        public void SetVertex(int index, VertexPoint v)
        {
            var position = cur_matrix.MultiplyPoint3x4(new Vector3(v.X, v.Y));
            position.y = -position.y;
            UIVertex vertex = this.mVBOs[index];
            vertex.position = position;
            vertex.uv0 = new Vector2(v.U, 1f - v.V);
            vertex.color = UIUtils.UInt32_RGBA_To_Color(v.Color) * mBlendColor;
            this.mVBOs[index] = vertex;
        }

        public void GetVertex(int index, out VertexPoint v)
        {
            UIVertex vertex = this.mVBOs[index];
            v = new VertexPoint();
            v.X = vertex.position.x;
            v.Y = -vertex.position.y;
            v.U = vertex.uv0.x;
            v.V = 1f - vertex.uv0.y;
            v.Color = UIUtils.Color_To_UInt32_RGBA(vertex.color);
        }

        public void Clear()
        {
            mVBOs.Clear();
        }
        public void Optimize()
        {
        }
        public void SetIndices(int[] indices, VertexTopology topology)
        {
        }
        public void SetIndices(int index, int[] indices)
        {
        }
        public void AddIndicesQuard(int a, int b, int c, int d)
        {
        }
        public void AddIndicesTrangle(int a, int b, int c)
        {
        }


        public void translate(float x, float y)
        {
            if (x != 0 || y != 0)
            {
                Matrix4x4 m = Matrix4x4.TRS(
                    new Vector3(x, y, 0),
                    Quaternion.identity,
                    Vector3.one);
                cur_matrix = cur_matrix * m;
            }
        }
        public void rotate(float angle)
        {
            if (angle != 0)
            {
                Matrix4x4 m = Matrix4x4.TRS(
                    Vector3.zero,
                    Quaternion.Euler(0f, 0f, angle),
                    Vector3.one);
                cur_matrix = cur_matrix * m;
            }
        }
        public void scale(float sx, float sy)
        {
            if (sx != 1 || sy != 1)
            {
                Matrix4x4 m = Matrix4x4.TRS(
                      Vector3.zero,
                      Quaternion.identity,
                      new Vector3(sx, sy, 1.0f));
                cur_matrix = cur_matrix * m;
            }
        }
        public void pushTransform()
        {
            stack_matrix.Push(cur_matrix);
        }
        public void popTransform()
        {
            cur_matrix = stack_matrix.Pop();
        }

    }
    

    public class VertexHelperBuffer : VertexBuffer
    {
        //---------------------------------------------------------------------------------------
        #region Pool
        public static VertexHelperBuffer AllocAutoRelease(VertexHelper helper)
        {
            var ret = new VertexHelperBuffer();
            ret.BindMesh(helper);
            return ret;
        }
        private static readonly Vector4 s_DefaultTangent = new Vector4(1f, 0f, 0f, -1f);
        private static readonly Vector3 s_DefaultNormal = Vector3.back;
        #endregion
        //---------------------------------------------------------------------------------------
        private VertexHelper m_Helper;
        private UnityEngine.Color m_BlendColor = UnityEngine.Color.white;
        private Matrix4x4 cur_matrix;
        private Stack<Matrix4x4> stack_matrix = new Stack<Matrix4x4>();

        public int Count
        {
            get { return this.m_Helper.currentVertCount; }
        }
        public int IndicesCount
        {
            get { return m_Helper.currentIndexCount; }
        }
        public UnityEngine.Color BlendColor
        {
            get { return m_BlendColor; }
            set { m_BlendColor = value; }
        }

        private VertexHelperBuffer() { }
        private void BindMesh(VertexHelper m)
        {
            this.m_Helper = m;
        }
        public void Dispose()
        {
            this.m_Helper = null;
            this.m_BlendColor = UnityEngine.Color.white;
            this.stack_matrix.Clear();
            this.cur_matrix = Matrix4x4.identity;
        }

        //---------------------------------------------------------------------------------------
        #region VertexBuffer

        void VertexBuffer.Append(VertexBuffer data)
        {
            throw new NotImplementedException();
        }
        void VertexBuffer.Append(float x, float y, uint color, float u, float v)
        {
            var position = cur_matrix.MultiplyPoint3x4(new Vector3(x, y));
            position.y = -position.y;
            m_Helper.AddVert(
                position,
                UIUtils.UInt32_RGBA_To_Color(color) * m_BlendColor,
                new Vector2(u, 1f - v));
        }
        void VertexBuffer.Append(VertexPoint vertex)
        {
            var position = cur_matrix.MultiplyPoint3x4(new Vector3(vertex.X, vertex.Y));
            position.y = -position.y;
            m_Helper.AddVert(
                position,
                UIUtils.UInt32_RGBA_To_Color(vertex.Color) * m_BlendColor,
                new Vector2(vertex.U, 1f - vertex.V));
        }
        void VertexBuffer.SetVertex(int index, VertexPoint vertex)
        {
            var v = UIVertex.simpleVert;
            v.position = (cur_matrix.MultiplyPoint3x4(new Vector3(vertex.X, vertex.Y)));
            v.position.y = -v.position.y;
            v.color = (UIUtils.UInt32_RGBA_To_Color(vertex.Color)) * m_BlendColor;
            v.uv0 = (new Vector2(vertex.U, 1f - vertex.V));
            v.uv1 = (Vector2.zero);
            v.normal = (s_DefaultNormal);
            v.tangent = (s_DefaultTangent);
            m_Helper.SetUIVertex(v, index);
        }
        void VertexBuffer.SetPosition(int index, float x, float y)
        {
            var v = UIVertex.simpleVert;
            m_Helper.PopulateUIVertex(ref v, index);
            v.position = (cur_matrix.MultiplyPoint3x4(new Vector3(x, y)));
            v.position.y = -v.position.y;
            m_Helper.SetUIVertex(v, index);
        }
        void VertexBuffer.SetColor(int index, uint color)
        {
            var v = UIVertex.simpleVert;
            m_Helper.PopulateUIVertex(ref v, index);
            v.color = (UIUtils.UInt32_RGBA_To_Color(color)) * m_BlendColor;
            m_Helper.SetUIVertex(v, index);
        }
        void VertexBuffer.SetTexCoords(int index, float u, float v)
        {
            var vtx = UIVertex.simpleVert;
            m_Helper.PopulateUIVertex(ref vtx, index);
            vtx.uv0 = (new Vector2(u, 1f - v));
            m_Helper.SetUIVertex(vtx, index);
        }
        void VertexBuffer.AddIndicesQuard(int a, int b, int c, int d)
        {
            m_Helper.AddTriangle(a, b, c);
            m_Helper.AddTriangle(c, d, a);
        }
        void VertexBuffer.SetIndices(int[] indices, VertexTopology topology)
        {
            throw new NotImplementedException();
        }
        void VertexBuffer.SetIndices(int index, int[] indices)
        {
            throw new NotImplementedException();
        }
        void VertexBuffer.Optimize()
        {
        }
        void VertexBuffer.Clear()
        {
            m_Helper.Clear();
        }

        #endregion
        //---------------------------------------------------------------------------------------
        #region Transform

        public void translate(float x, float y)
        {
            if (x != 0 || y != 0)
            {
                Matrix4x4 m = Matrix4x4.TRS(
                    new Vector3(x, y, 0),
                    Quaternion.identity,
                    Vector3.one);
                cur_matrix = cur_matrix * m;
            }
        }
        public void rotate(float angle)
        {
            if (angle != 0)
            {
                Matrix4x4 m = Matrix4x4.TRS(
                    Vector3.zero,
                    Quaternion.Euler(0f, 0f, angle),
                    Vector3.one);
                cur_matrix = cur_matrix * m;
            }
        }
        public void scale(float sx, float sy)
        {
            if (sx != 1 || sy != 1)
            {
                Matrix4x4 m = Matrix4x4.TRS(
                      Vector3.zero,
                      Quaternion.identity,
                      new Vector3(sx, sy, 1.0f));
                cur_matrix = cur_matrix * m;
            }
        }
        public void pushTransform()
        {
            stack_matrix.Push(cur_matrix);
        }
        public void popTransform()
        {
            cur_matrix = stack_matrix.Pop();
        }

        #endregion
        //---------------------------------------------------------------------------------------
    }
    
}
