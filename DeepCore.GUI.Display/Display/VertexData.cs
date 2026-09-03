using DeepCore.GUI.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Display
{
    public struct VertexPoint
    {
        public float X, Y;
        public uint Color;
        public float U, V;

        public VertexPoint(float x, float y, uint color, float u, float v)
        {
            this.X = x;
            this.Y = y;
            this.Color = color;
            this.U = u;
            this.V = v;
        }
    }

    /// <summary>
    /// 顶点绘制方式
    /// </summary>
    public enum VertexTopology : byte
    {
        /// <summary>
        /// 绘制三角形。
        /// </summary>
        TRIANGLES = 4,
        /// <summary>
        /// 绘制三角形带。
        /// </summary>
        TRIANGLE_STRIP = 5,
        /// <summary>
        /// 绘制四边形。
        /// </summary>
        QUADS = 7,
        /// <summary>
        /// 绘制线。
        /// </summary>
        LINES = 1,
    }


    /// <summary>
    /// 顶点缓冲区,
    /// 一个完整定点信息包括，Position 2F, Color uint, TexCoords 2F
    /// </summary>
    public interface VertexBuffer : IDisposable
    {
        /// <summary>
        /// 顶点数量
        /// </summary>
        int Count { get; }
        int IndicesCount { get; }

        /// <summary>
        /// Appends the vertices from another VertexData object.
        /// </summary>
        /// <param name="data"></param>
        void Append(VertexBuffer data);
        void Append(float x, float y, uint color, float u, float v);
        void Append(VertexPoint vertex);

        /// <summary>
        /// Updates the vertex point of a vertex.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="vertex"></param>
        void SetVertex(int index, VertexPoint vertex);
        
        /// <summary>
        /// Updates the position values of a vertex.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void SetPosition(int index, float x, float y);

        /// <summary>
        /// Updates the RGB color values of a vertex (alpha is not changed). 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="color"></param>
        void SetColor(int index, uint color);

        /// <summary>
        /// Updates the texture coordinates of a vertex (range 0-1).
        /// </summary>
        /// <param name="index"></param>
        /// <param name="u"></param>
        /// <param name="v"></param>
        void SetTexCoords(int index, float u, float v);

        /// <summary>
        /// Sets the index buffer for the submesh.
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="topology"></param>
        void SetIndices(int[] indices, VertexTopology topology);
        void SetIndices(int index, int[] indices);
        void AddIndicesQuard(int a, int b, int c, int d);

        void Optimize();

        void Clear();

        #region TRANSFORM

        void translate(float x, float y);
        void rotate(float angle);
        void scale(float sx, float sy);
        void pushTransform();
        void popTransform();

        #endregion
    }

    public static class VertexUtils
    {
        // -----------------------------------------------------------------------------------------
        // utility functions
        // -----------------------------------------------------------------------------------------

        public static void SetTexCoords(VertexBuffer buffer, int index, Image src, float sx, float sy)
        {
            buffer.SetTexCoords(index, src.MaxU * sx / src.Width, src.MaxV * sy / src.Height);
        }

        static public void AddImageQuard(VertexBuffer buffer, Image src, uint rgba, float sx1, float sy1, float sw, float sh, Trans trans, float dx1, float dy1)
        {
            uint color = rgba;
            float dx2 = dx1 + sw;
            float dy2 = dy1 + sh;
            float sx2 = sx1 + sw;
            float sy2 = sy1 + sh;
            TransformTrans(ref sx1, ref sy1, ref sx2, ref sy2, trans, ref dx1, ref dy1, ref dx2, ref dy2);
            float u1 = src.MaxU * sx1 / src.Width;
            float v1 = src.MaxV * sy1 / src.Height;
            float u2 = src.MaxU * sx2 / src.Width;
            float v2 = src.MaxV * sy2 / src.Height;
            int i = buffer.Count;
            buffer.Append(dx1, dy1, color, u1, v1);
            buffer.Append(dx2, dy1, color, u2, v1);
            buffer.Append(dx2, dy2, color, u2, v2);
            buffer.Append(dx1, dy2, color, u1, v2);
            buffer.AddIndicesQuard(i + 0, i + 1, i + 2, i + 3);
        }

        public static void SetImageQuard(VertexBuffer buffer, int index, Image src, uint rgba, float sx1, float sy1, float sw, float sh, Trans trans, float dx1, float dy1)
        {
            float dx2 = dx1 + sw;
            float dy2 = dy1 + sh;
            float sx2 = sx1 + sw;
            float sy2 = sy1 + sh;
            TransformTrans(ref sx1, ref sy1, ref sx2, ref sy2, trans, ref dx1, ref dy1, ref dx2, ref dy2);
            float u1 = src.MaxU * sx1 / src.Width;
            float v1 = src.MaxV * sy1 / src.Height;
            float u2 = src.MaxU * sx2 / src.Width;
            float v2 = src.MaxV * sy2 / src.Height;
            buffer.SetVertex(index + 0, new VertexPoint(dx1, dy1, rgba, u1, v1));
            buffer.SetVertex(index + 1, new VertexPoint(dx2, dy1, rgba, u2, v1));
            buffer.SetVertex(index + 2, new VertexPoint(dx2, dy2, rgba, u2, v2));
            buffer.SetVertex(index + 3, new VertexPoint(dx1, dy2, rgba, u1, v2));
        }

        public static void TransformTrans(ref float sx1, ref float sy1, ref float sx2, ref float sy2, Trans trans, ref float dx1, ref float dy1, ref float dx2, ref float dy2)
        {
            switch (trans)
            {
                case Trans.TRANS_NONE:
                    break;
                case Trans.TRANS_MIRROR:
                    break;
            }


        }
    }

}
