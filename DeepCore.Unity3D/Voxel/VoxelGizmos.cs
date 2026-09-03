using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepCore.Unity3D.Voxel
{
    public static class VoxelGizmos
    {
        public static GameObject CreateVoxelCylinder(float radius, float height, bool isCollider = true)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var filter = temp.GetComponent<MeshFilter>();
            var mesh = filter.mesh;
            {
                var vertices = mesh.vertices;
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    var v = vertices[i] * 2f * radius;
                    if (v.y < 0) v.y = 0; else v.y = height;
                    vertices[i] = v;
                }
                mesh.vertices = vertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                filter.mesh = mesh;
            }
            if (isCollider)
            {
                var collider = temp.GetComponent<CapsuleCollider>();
                if (collider)
                {
                    collider.radius = radius;
                    collider.height = height;
                    collider.center = new Vector3(0, height / 2f, 0);
                }
            }
            else if (temp.TryGetComponent<Collider>(out var c))
            {
                GameObject.Destroy(c);
            }
            return temp;
        }

        public static GameObject CreateVoxelRect(float w, float h, float height, bool isCollider = true)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var filter = temp.GetComponent<MeshFilter>();
            var mesh = filter.mesh;
            {
                var wx = w / 2f;
                var wy = h / 2f;
                var vertices = mesh.vertices;
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    var v = vertices[i];
                    if (v.x < 0) v.x = -wx; else v.x = wx;
                    if (v.z < 0) v.z = -wy; else v.z = wy;
                    if (v.y < 0) v.y = 0; else v.y = height;
                    vertices[i] = v;
                }
                mesh.vertices = vertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                filter.mesh = mesh;
            }
            if (isCollider)
            {
                var collider = temp.GetComponent<BoxCollider>();
                {
                    collider.size = new Vector3(w, height, h);
                    collider.center = new Vector3(0, height / 2f, 0);
                }
            }
            else if (temp.TryGetComponent<Collider>(out var c))
            {
                GameObject.Destroy(c);
            }
            return temp;
        }

        public static GameObject CreateVoxelFan(float radius, float degrees, float height, bool isCollider = true)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            temp.name = "Fan";
            var filter = temp.GetComponent<MeshFilter>();
            {
                var baker = new MeshBaker();
                //baker.NormalCenter = new Vector3(radius / 2f, height / 2f, 0);
                InitFanMesh(baker, Vector3.zero, radius, 0, degrees, height, true);
                filter.mesh = baker.BakeMesh();
            }
            if (isCollider)
            {
                var meshCollider = temp.AddComponent<MeshCollider>();
                {

                }
            }
            else if (temp.TryGetComponent<Collider>(out var c))
            {
                GameObject.Destroy(c);
            }
            return temp;

        }


        public static GameObject CreateVoxelStrip(float wide, float distance, float height, bool isCollider = true)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            temp.name = "Strip";
            var filter = temp.GetComponent<MeshFilter>();
            {
                var baker = new MeshBaker();
                //baker.NormalCenter = new Vector3(0, height / 2f, 0);
                InitStripMesh(baker, Vector3.zero, wide, -distance / 2, distance / 2, height);
                filter.mesh = baker.BakeMesh();
            }
            if (isCollider)
            {
                var meshCollider = temp.AddComponent<MeshCollider>();
                {

                }
            }
            else if (temp.TryGetComponent<Collider>(out var c))
            {
                GameObject.Destroy(c);
            }
            return temp;
        }
        public static GameObject CreateVoxelStripRay(float wide, float distance, float height, bool isCollider = true)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            temp.name = "StripRay";
            var filter = temp.GetComponent<MeshFilter>();
            {
                var baker = new MeshBaker();
                //baker.NormalCenter = new Vector3(distance / 2f, height / 2f, 0);
                InitStripMesh(baker, Vector3.zero, wide, 0, distance, height);
                filter.mesh = baker.BakeMesh();
            }
            if (isCollider)
            {
                var meshCollider = temp.AddComponent<MeshCollider>();
                {

                }
            }
            else if (temp.TryGetComponent<Collider>(out var c))
            {
                GameObject.Destroy(c);
            }
            return temp;
        }
        public static GameObject CreateVoxelRectStrip(float wide, float distance, float height, bool isCollider = true)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            temp.name = "RectStrip";
            var filter = temp.GetComponent<MeshFilter>();
            {
                var baker = new MeshBaker();
                //baker.NormalCenter = new Vector3(0, height / 2f, 0);
                InitRectStripMesh(baker, Vector3.zero, wide, -distance / 2, distance / 2, height, true);
                filter.mesh = baker.BakeMesh();
            }
            if (isCollider)
            {
                var meshCollider = temp.AddComponent<MeshCollider>();
                {

                }
            }
            else if (temp.TryGetComponent<Collider>(out var c))
            {
                GameObject.Destroy(c);
            }
            return temp;
        }
        public static GameObject CreateVoxelRectStripRay(float wide, float distance, float height, bool isCollider = true)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            temp.name = "RectStripRay";
            var filter = temp.GetComponent<MeshFilter>();
            {
                var baker = new MeshBaker();
                //baker.NormalCenter = new Vector3(distance / 2f, height / 2f, 0);
                InitRectStripMesh(baker, Vector3.zero, wide, 0, distance, height, true);
                filter.mesh = baker.BakeMesh();
            }
            if (isCollider)
            {
                var meshCollider = temp.AddComponent<MeshCollider>();
                {

                }
            }
            else if (temp.TryGetComponent<Collider>(out var c))
            {
                GameObject.Destroy(c);
            }
            return temp;
        }

        public static GameObject CreateVoxelSingle(float distance, float height, bool isCollider = true)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Plane);
            temp.name = "Single";
            var filter = temp.GetComponent<MeshFilter>();
            {
                var baker = new MeshBaker();
                //baker.NormalCenter = new Vector3(distance / 2f, height / 2f, 0);
                InitSingleMesh(baker, Vector3.zero, distance, height);
                filter.mesh = baker.BakeMesh();
            }
            if (isCollider)
            {
                var meshCollider = temp.AddComponent<MeshCollider>();
                {

                }
            }
            else if (temp.TryGetComponent<Collider>(out var c))
            {
                GameObject.Destroy(c);
            }
            return temp;
        }


        public static GameObject CreateVoxelLineTarget(Transform target, float wide, float height, bool isCollider = true)
        {
            var temp = new GameObject();
            temp.name = "Line";

            var lineRenderer = temp.AddComponent<LineRenderer>();
            lineRenderer.endWidth = wide;
            lineRenderer.startWidth = wide;
            //lineRenderer.widthMultiplier = 0.1f;

            var line = temp.AddComponent<LineToTarget>();
            if (target != null)
            {
                line.target = target;
            }
            line.wide = wide;
            line.height = height;
            line.lineRenderer = lineRenderer;
            return temp;
        }

        public static void InitSingleMesh(MeshBaker baker, Vector3 center, float size, float height)
        {
            {
                var vStart = baker.VertexCount;

                baker.AddVertex(center);
                baker.AddVertex(center + new Vector3(size, 0, 0));
                baker.AddVertex(center + new Vector3(size, height, 0));
                baker.AddVertex(center + new Vector3(0, height, 0));
                baker.AddNormal(Vector3.back);
                baker.AddNormal(Vector3.back);
                baker.AddNormal(Vector3.back);
                baker.AddNormal(Vector3.back);

                baker.AddTriangle(vStart + 0);
                baker.AddTriangle(vStart + 3);
                baker.AddTriangle(vStart + 1);

                baker.AddTriangle(vStart + 3);
                baker.AddTriangle(vStart + 2);
                baker.AddTriangle(vStart + 1);
            }
            {
                var vStart = baker.VertexCount;

                baker.AddVertex(center);
                baker.AddVertex(center + new Vector3(size, 0, 0));
                baker.AddVertex(center + new Vector3(size, height, 0));
                baker.AddVertex(center + new Vector3(0, height, 0));
                baker.AddNormal(Vector3.forward);
                baker.AddNormal(Vector3.forward);
                baker.AddNormal(Vector3.forward);
                baker.AddNormal(Vector3.forward);

                baker.AddTriangle(vStart + 0);
                baker.AddTriangle(vStart + 1);
                baker.AddTriangle(vStart + 2);

                baker.AddTriangle(vStart + 2);
                baker.AddTriangle(vStart + 3);
                baker.AddTriangle(vStart + 0);
            }
        }

        public static void InitFanMesh(MeshBaker baker, Vector3 center, float radius, float startDegree, float degrees, float height, bool close)
        {
            var tStart = baker.TriangleCount;
            var vStart = baker.VertexCount;
            {
                var count = (int)(16 * Math.Max(radius, 1));
                var rstep = degrees / count;
                var degStart = startDegree - degrees / 2;

                // 创建所有点，上下两层
                baker.AddVertex(center);
                baker.AddVertex(center + new Vector3(0, height, 0));
                baker.AddNormal(Vector3.down);
                baker.AddNormal(Vector3.up);
                //                 baker.AutoVertex(center); 
                //                 baker.AutoVertex(center + new Vector3(0, height, 0));

                for (int i = 0; i < count + 1; i++)
                {
                    float px = (float)(Math.Cos(degStart) * radius);
                    float py = (float)(Math.Sin(degStart) * radius);
                    //                     baker.AutoVertex(
                    //                         center + new Vector3(px, 0, py),
                    //                         center + new Vector3(px, height, py));
                    baker.AddVertex(
                        center + new Vector3(px, 0, py),
                        center + new Vector3(px, height, py));
                    baker.AddNormal(
                        Vector3.down,
                        Vector3.up);
                    degStart += rstep;
                }
                if (close)
                {
                    baker.AddTriangle(vStart + 0, vStart + 1, vStart + 2);
                    baker.AddTriangle(vStart + 3, vStart + 2, vStart + 1);
                }
                // 缝合所有点，上下两层
                for (int i = 0; i < count; i++)
                {
                    var pi = 2 + (i * 2);
                    baker.AddTriangle(vStart + 0);
                    baker.AddTriangle(vStart + pi);
                    baker.AddTriangle(vStart + pi + 2);

                    baker.AddTriangle(vStart + pi);
                    baker.AddTriangle(vStart + pi + 1);
                    baker.AddTriangle(vStart + pi + 2);

                    baker.AddTriangle(vStart + pi + 3);
                    baker.AddTriangle(vStart + pi + 2);
                    baker.AddTriangle(vStart + pi + 1);

                    baker.AddTriangle(vStart + pi + 3);
                    baker.AddTriangle(vStart + pi + 1);
                    baker.AddTriangle(vStart + 1);
                }
                if (close)
                {
                    var li = baker.VertexCount - 2;
                    baker.AddTriangle(vStart + li + 0);
                    baker.AddTriangle(vStart + 1);
                    baker.AddTriangle(vStart + 0);

                    baker.AddTriangle(vStart + 1);
                    baker.AddTriangle(vStart + li);
                    baker.AddTriangle(vStart + li + 1);
                }
            }
        }

        public static void InitRectStripMesh(MeshBaker baker, Vector3 center, float wide, float left, float right, float height, bool close)
        {
            var cr = wide / 2f;
            var w1 = left;
            var w2 = right;
            {
                var cv = new Vector3[] {
                    center+   new Vector3(w1, 0,      -cr),
                    center+   new Vector3(w2, 0,      -cr),
                    center+   new Vector3(w2, height, -cr),
                    center+   new Vector3(w1, height, -cr),
                    center+   new Vector3(w1, height, +cr),
                    center+   new Vector3(w2, height, +cr),
                    center+   new Vector3(w2, 0,      +cr),
                    center+   new Vector3(w1, 0,      +cr),
                };
                var cn = new Vector3[] {
                            Vector3.down,
                            Vector3.down,
                            Vector3.up,
                            Vector3.up,
                            Vector3.up,
                            Vector3.up,
                            Vector3.down,
                            Vector3.down,
                            };
                var vi = baker.VertexCount;
                var ct = new int[] {
                    vi+0, vi+2, vi+1, //face front
  			        vi+0, vi+3, vi+2, //
                    vi+2, vi+3, vi+4, //face top
  			        vi+2, vi+4, vi+5, //
                    vi+5, vi+4, vi+7, //face back
  			        vi+5, vi+7, vi+6, //
                    vi+0, vi+6, vi+7, //face bottom
  			        vi+0, vi+1, vi+6, //
                    };
                baker.AddVertex(cv);
                baker.AddNormal(cn);
                baker.AddTriangle(ct);
                if (close)
                {
                    var clr = new int[] {
                    vi+1, vi+2, vi+5, //face right
  			        vi+1, vi+5, vi+6, //
                    vi+0, vi+7, vi+4, //face left
  			        vi+0, vi+4, vi+3, //
                    };
                    baker.AddTriangle(clr);
                }
            }
        }

        public static void InitStripMesh(MeshBaker baker, Vector3 center, float wide, float left, float right, float height)
        {
            var cr = wide / 2f;
            var c1 = center + new Vector3(left, 0, 0);
            var c2 = center + new Vector3(right, 0, 0);
            InitRectStripMesh(baker, center, wide, left, right, height, false);
            InitFanMesh(baker, c1, cr, Mathf.PI, Mathf.PI, height, false);
            InitFanMesh(baker, c2, cr, 0, Mathf.PI, height, false);
        }


    }
    public class MeshBaker
    {
        private List<UnityEngine.Vector3> vertices = new();
        private List<UnityEngine.Vector3> normals = new();
        private List<int> triangles = new();
        public int VertexCount { get => vertices.Count; }
        public int TriangleCount { get => triangles.Count; }

        //public Vector3 NormalCenter { get; set; } = Vector3.zero;

        public void AddNormal(params Vector3[] v) { normals.AddRange(v); }
        public void AddVertex(params Vector3[] v) { vertices.AddRange(v); }
        public void AddTriangle(params int[] v) { triangles.AddRange(v); }

//         /// <summary>
//         ///  Add Vertex and auto caculate normal
//         /// </summary>
//         /// <param name="v"></param>
//         public void AutoVertex(params Vector3[] vs)
//         {
//             vertices.AddRange(vs);
//             foreach (var v in vs)
//             {
//                 normals.Add(Vector3.Normalize(Vector3.Lerp(NormalCenter, v, 1f)));
//             }
//         }

        public Mesh BakeMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            //mesh.RecalculateNormals();
            mesh.Optimize();
            return mesh;
        }
    }

    public class LineToTarget : MonoBehaviour
    {
        public LineRenderer lineRenderer { get; set; }

        public Transform target { get; set; }
        public Vector3? targetPos { get; set; }

        public float wide { get; set; } = 0.01f;
        public float height { get; set; } = 1f;
        public Color color { get; set; } = Color.red;

        void Start()
        {
        }

        void Update()
        {
            if (lineRenderer != null)
            {
                //设置颜色
                lineRenderer.endColor = color;
                lineRenderer.startColor = color;
                //设置宽度
                lineRenderer.endWidth = wide;
                lineRenderer.startWidth = wide;

                if (target)
                {
                    lineRenderer.SetPosition(0, this.transform.position + new Vector3(0, height / 2f, 0));
                    lineRenderer.SetPosition(1, target.position + new Vector3(0, height / 2f, 0));
                }
                else if (targetPos != null)
                {
                    var pos = targetPos.Value;
                    pos.y = this.transform.position.y;
                    lineRenderer.SetPosition(0, this.transform.position + new Vector3(0, height / 2f, 0));
                    lineRenderer.SetPosition(1, pos + new Vector3(0, height / 2f, 0));
                }
            }
        }

    }
}