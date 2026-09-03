using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepCore.Unity3D.Impl
{
    public struct UnityVertexBuffer : VertexBuffer
    {
        private int mTopology;
        private List<int> mIndices;
        private List<Vector3> mPos;
        private List<Vector2> mUVs;

        public UnityVertexBuffer(int capacity)
        {
            this.mTopology = GL.QUADS;
            this.mIndices = new List<int>(capacity);
            this.mPos = new List<Vector3>(capacity);
            this.mUVs = new List<Vector2>(capacity);
        }

        public void Dispose()
        {
            mIndices = null;
            mPos = null;
            mUVs = null;
        }

        /// <summary>
        /// 顶点数量
        /// </summary>
        public int Count
        {
            get { return mPos.Count; }
            set
            {
                CUtils.SetListSize<Vector3>(mPos, value);
                CUtils.SetListSize<Vector2>(mUVs, value);
            }
        }

        public int IndicesCount
        {
            get
            {
                return mIndices.Count;
            }
        }

        /// <summary>
        /// Appends the vertices from another VertexData object.
        /// </summary>
        /// <param name="data"></param>
        public void Append(VertexBuffer data)
        {
            mPos.AddRange(((UnityVertexBuffer)data).mPos);
            mUVs.AddRange(((UnityVertexBuffer)data).mUVs);
        }
        public void Append(float x, float y, uint color, float u, float v)
        {
            mPos.Add(new Vector3(x, y, 0));
            mUVs.Add(new Vector2(u, 1f - v));
        }
        public void Append(VertexPoint vertex)
        {
            mPos.Add(new Vector3(vertex.X, vertex.Y, 0));
            mUVs.Add(new Vector2(vertex.U, 1f - vertex.V));
        }


        /// <summary>
        /// Updates the vertex point of a vertex.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="vertex"></param>
        public void SetVertex(int index, VertexPoint vertex)
        {
            this.mPos[index] = new Vector3(vertex.X, vertex.Y, 0);
            this.mUVs[index] = new Vector2(vertex.U, 1f - vertex.V);
        }

        /// <summary>
        /// Returns the vertex point of a vertex.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="vertex"></param>
        public void GetVertex(int index, out VertexPoint vertex)
        {
            VertexPoint v = new VertexPoint();
            v.X = mPos[index].x;
            v.Y = mPos[index].y;
            v.U = mUVs[index].x;
            v.V = mUVs[index].y;
            vertex = v;
        }

        /// <summary>
        /// Updates the position values of a vertex.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetPosition(int index, float x, float y)
        {
            this.mPos[index] = new Vector3(x, y, 0);
        }

        /// <summary>
        /// Updates the RGB color values of a vertex (alpha is not changed). 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="color"></param>
        public void SetColor(int index, uint color)
        {

        }

        /// <summary>
        /// Updates the texture coordinates of a vertex (range 0-1).
        /// </summary>
        /// <param name="index"></param>
        /// <param name="u"></param>
        /// <param name="v"></param>
        public void SetTexCoords(int index, float u, float v)
        {
            this.mUVs[index] = new Vector2(u, 1f - v);
        }


        public void SetIndices(int[] indices, VertexTopology topology)
        {
            if (indices == null)
            {
                throw new Exception("Indices Can not be set to NULL !");
            }
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] < 0 && indices[i] >= mPos.Count)
                {
                    throw new Exception("Indices Value Must In Vertex Length range !");
                }
            }
            mIndices.Clear();
            mIndices.AddRange(indices);
            switch (topology)
            {
                case VertexTopology.QUADS:
                    mTopology = GL.QUADS;
                    break;
                case VertexTopology.LINES:
                    mTopology = GL.LINES;
                    break;
                case VertexTopology.TRIANGLE_STRIP:
                    mTopology = GL.TRIANGLE_STRIP;
                    break;
                case VertexTopology.TRIANGLES:
                    mTopology = GL.TRIANGLES;
                    break;
            }
        }
        public void SetIndices(int index, int[] indices)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                mIndices[i + index] = indices[i];
            }
        }
        public void AddIndicesQuard(int a, int b, int c, int d)
        {
            mIndices.Add(a);
            mIndices.Add(b);
            mIndices.Add(c);
            mIndices.Add(d);
        }

        public void Optimize()
        {
        }

        public object Clone()
        {
            UnityVertexBuffer ret = new UnityVertexBuffer(this.Count);
            ret.Append(this);
            return ret;
        }
        public void Clear()
        {
            this.mUVs.Clear();
            this.mPos.Clear();
        }

        internal void Draw()
        {
            GL.Begin(mTopology);
            for (int i = 0; i < mIndices.Count; i++)
            {
                Vector2 uv = mUVs[mIndices[i]];
                GL.TexCoord2(uv.x, uv.y);
                GL.Vertex(mPos[mIndices[i]]);
            }
            GL.End();
        }
        internal void DrawVertex(int[] indices, int mode)
        {
            GL.Begin(mode);
            for (int i = 0; i < indices.Length; i++)
            {
                Vector2 uv = mUVs[indices[i]];
                GL.TexCoord2(uv.x, uv.y);
                GL.Vertex(mPos[indices[i]]);
            }
            GL.End();
        }
        internal void DrawSequence(int mode)
        {
            GL.Begin(mode);
            for (int i = 0; i < mPos.Count; i++)
            {
                Vector2 uv = mUVs[i];
                GL.TexCoord2(uv.x, uv.y);
                GL.Vertex(mPos[i]);
            }
            GL.End();
        }

        public void translate(float x, float y)
        {
            throw new NotImplementedException();
        }

        public void rotate(float angle)
        {
            throw new NotImplementedException();
        }

        public void scale(float sx, float sy)
        {
            throw new NotImplementedException();
        }

        public void pushTransform()
        {
            throw new NotImplementedException();
        }

        public void popTransform()
        {
            throw new NotImplementedException();
        }
    }

    public struct UnityQuards2D
    {
        public IUnityImageInterface src;
        public float u0, v0, u1, v1;
        public float x0, y0, x1, y1, x2, y2, x3, y3;

        public static UnityQuards2D DrawImage(IUnityImageInterface src, float x, float y)
        {
            UnityQuards2D q = new UnityQuards2D();
            q.src = src;
            q.u0 = 0;
            q.v0 = 1f;
            q.u1 = src.MaxU;
            q.v1 = 1f - src.MaxV;

            q.x0 = x;
            q.y0 = y;
            q.x1 = x + src.Width;
            q.y1 = y;
            q.x2 = x + src.Width;
            q.y2 = y + src.Height;
            q.x3 = x;
            q.y3 = y + src.Height;

            return q;
        }
        public static UnityQuards2D DrawImageZoom(IUnityImageInterface src, float x, float y, float w, float h)
        {
            UnityQuards2D q = new UnityQuards2D();
            q.src = src;
            q.u0 = 0;
            q.v0 = 1f;
            q.u1 = src.MaxU;
            q.v1 = 1f - src.MaxV;

            q.x0 = x;
            q.y0 = y;
            q.x1 = x + w;
            q.y1 = y;
            q.x2 = x + w;
            q.y2 = y + h;
            q.x3 = x;
            q.y3 = y + h;

            return q;
        }

        public static UnityQuards2D DrawImageTrans(IUnityImageInterface src, float x, float y, Trans trans)
        {
            return DrawImageRegion(src, 0, 0, src.Width, src.Height, x, y, src.Width, src.Height, trans);
        }

        public static UnityQuards2D DrawImageRegion(
            IUnityImageInterface src,
            float sx, float sy, float sw, float sh,
            float dx, float dy, float dw, float dh,
            Trans trans)
        {
            float sx2 = sx + sw;
            float sy2 = sy + sh;
            float dx2 = dx + dw;
            float dy2 = dy + dh;
            VertexUtils.TransformTrans(ref sx, ref sy, ref sx2, ref sy2, trans, ref dx, ref dy, ref dx2, ref dy2);

            UnityQuards2D q = new UnityQuards2D();
            q.src = src;
            q.u0 = (sx) * src.MaxU / src.Width;
            q.v0 = 1f - (sy) * src.MaxV / src.Height;
            q.u1 = (sx2) * src.MaxU / src.Width;
            q.v1 = 1f - (sy2) * src.MaxV / src.Height;

            q.x0 = dx;
            q.y0 = dy;
            q.x1 = dx2;
            q.y1 = dy;
            q.x2 = dx2;
            q.y2 = dy2;
            q.x3 = dx;
            q.y3 = dy2;

            return q;
        }

        internal void Draw()
        {
            GL.TexCoord2(u0, v0);
            GL.Vertex3(x0, y0, 0);

            GL.TexCoord2(u1, v0);
            GL.Vertex3(x1, y1, 0);

            GL.TexCoord2(u1, v1);
            GL.Vertex3(x2, y2, 0);

            GL.TexCoord2(u0, v1);
            GL.Vertex3(x3, y3, 0);
        }
    }
}
