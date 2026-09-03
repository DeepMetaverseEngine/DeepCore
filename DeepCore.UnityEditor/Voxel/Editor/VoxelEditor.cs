using DeepCore.Voxel.Data;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace DeepCore.UnityEditor.Voxel
{
    public class VoxelEditor : MonoBehaviour
    {
        public Material mat_red;
        public Material mat_blue;
        public Material mat_green;
        public Material mat_safe;
        public Material mat_waterWalkable;


        public Material selected_mat;
        public Material updated_green;
        public Material updated_red;
        public Material updated_blue;
        public Material updated_safe;
        public Material updated_waterWalkable;
        public Material voxel_unknown;

        private static VoxelData data = new VoxelData();

        private bool mouseDownState = false;
        private static bool isLoadData = false;
        private int brushIndex = 0;
        private int brushType = 0;
        private int brushApplyType = 0;
        private float brush_RectWidth = 1.0f;
        private readonly int brush_RectWidth_Max = 50;
        private float brush_CircleRadius = 1.0f;
        private readonly int brush_CircleRadius_Max = 50;

        private readonly Color color_White = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        private readonly Color color_Red = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        private readonly Color color_Green = new Color(0.0f, 1.0f, 0.0f, 1.0f);
        private readonly Color color_Blue = new Color(0.0f, 0.0f, 1.0f, 1.0f);
        private readonly Color color_Yellow = new Color(1.0f, 1.0f, 0.0f, 1.0f);
        private readonly Color color_LightBlue = new Color(0.0f, 220.0f / 255.0f, 1.0f, 1.0f);

        public static VoxelEditor Instance { get; private set; }

        public Transform Root
        {
            get
            {
                Transform rootTrans = null;
                var rootObj = gameObject;
                if (rootObj != null)
                {
                    rootTrans = rootObj.transform;
                }
                else
                {
                    var prefab = AssetDatabase.LoadAssetAtPath("Assets/voxel/Resources/VoxelEditor.prefab", typeof(GameObject)) as GameObject;
                    if (prefab != null)
                    {
                        rootObj = Instantiate(prefab);
                        rootObj.name = "VoxelEditor";
                        rootTrans = rootObj.transform;
                    }
                }

                rootTrans.GetComponent<VoxelEditor>().BindInstance();
                return rootTrans;
            }
            set { Root = value; }
        }

        private static List<Mesh> meshs;

        private TempVoxel selectedVoxelData = new TempVoxel();

        private List<GameObject> selectedVoxelObjList = new List<GameObject>();
        private List<TempVoxel> selectedVoxelDataList = new List<TempVoxel>();
        private List<GameObject> updateVoxelObjList = new List<GameObject>();
        private List<TempVoxel> updateVoxelDataList = new List<TempVoxel>();

        private static VoxelDataMeta voxelMeta = new VoxelDataMeta();

        private Stopwatch sw;

        public static readonly VoxelMetaExternalizableFactory externalizableFactory = new VoxelMetaExternalizableFactory();

        public GameObject voxelObject;

        private string metaPath;
        private string xmlPath;

        private readonly string[] voxelTypes = new string[] { "路面[可行走]", "路面[不可行走]", "水面[可行走]", "水面[不可行走]", "安全区" };
        private readonly string[] brushSizes = new string[] { "矩形", "圆形" };
        private readonly string[] brushApplyTypes = new string[] { "仅编辑", "应用类型", "应用所有" };

        private float minLevel = 0f;

        // Start is called before the first frame update
        void Start()
        {
            Instance = this;
            sw = Stopwatch.StartNew();
        }

        // Update is called once per frame
        void Update()
        {
            if (selectedVoxelDataList != null && selectedVoxelDataList.Count > 0)
            {
                if (Input.GetKeyDown(KeyCode.J))
                {
                    foreach (var voxelData in selectedVoxelDataList)
                    {
                        voxelData.x -= 1;
                        if (voxelData.x < 0)
                        {
                            voxelData.x = 0;
                        }
                    }
                    UpdateSelectedVoxelByKeyboard();
                }
                else if (Input.GetKeyDown(KeyCode.L))
                {
                    foreach (var voxelData in selectedVoxelDataList)
                    {
                        voxelData.x += 1;
                        if (voxelData.x > data.voxels.GetLength(0) - 1)
                        {
                            voxelData.x = data.voxels.GetLength(0) - 1;
                        }
                    }
                    UpdateSelectedVoxelByKeyboard();
                }
                else if (Input.GetKeyDown(KeyCode.I))
                {
                    foreach (var voxelData in selectedVoxelDataList)
                    {
                        voxelData.y += 1;
                        if (voxelData.y > data.voxels.GetLength(1) - 1)
                        {
                            voxelData.y = data.voxels.GetLength(1) - 1;
                        }
                    }
                    UpdateSelectedVoxelByKeyboard();
                }
                else if (Input.GetKeyDown(KeyCode.K))
                {
                    foreach (var voxelData in selectedVoxelDataList)
                    {
                        voxelData.y -= 1;
                        if (voxelData.y < 0)
                        {
                            voxelData.y = 0;
                        }
                    }
                    UpdateSelectedVoxelByKeyboard();
                }
                else if (Input.GetKeyDown(KeyCode.R))
                {
                    UpdateSelectedVoxelColorKeyboard(color_Red);
                }
                else if (Input.GetKeyDown(KeyCode.G))
                {
                    UpdateSelectedVoxelColorKeyboard(color_Green);
                }
                else if (Input.GetKeyDown(KeyCode.B))
                {
                    UpdateSelectedVoxelColorKeyboard(color_Blue);
                }
                else if (Input.GetKeyDown(KeyCode.Q))
                {
                    UpdateSelectedVoxelColorKeyboard(color_Yellow);
                }
                else if (Input.GetKeyDown(KeyCode.E))
                {
                    UpdateSelectedVoxelColorKeyboard(color_LightBlue);
                }
            }

            if (brushApplyType > 0 && mouseDownState)
            {
                BrushVoxel();
            }
        }

        private void UpdateSelectedVoxelByKeyboard()
        {
            if (selectedVoxelObjList.Count > 0)
            {
                foreach (var obj in selectedVoxelObjList)
                {
                    if (obj != null)
                    {
                        DestroyImmediate(obj);
                    }
                }
                selectedVoxelObjList.Clear();
            }

            foreach (var voxelData in selectedVoxelDataList)
            {
                var vs = data.voxels[voxelData.x, voxelData.y];
                VoxelLayer tmpVoxel = new VoxelLayer();
                for (int i = 0; i < vs.Length; i++)
                {
                    if (tmpVoxel.upward < vs[i].upward)
                    {
                        tmpVoxel = vs[i];
                    }
                }

                voxelData.color = tmpVoxel.color;
                voxelData.upward = tmpVoxel.upward;
                voxelData.downward = tmpVoxel.downward;

                GenerateSelectVoxelGameObjectByType();
            }
            SaveChangedVoxel();
        }

        private void UpdateSelectedVoxelColorKeyboard(Color color)
        {
            var objList = GenerateTempVoxelGameObject(selectedVoxelDataList, color, true);
            selectedVoxelObjList.AddRange(objList);
        }

        private void BindInstance()
        {
            Instance = this;
        }

        private void GenerateVoxel(float size, float upward, float downward, float x, float y,
            ref List<Vector3> verts, ref List<int> tris)
        {
            Vector3 v1 = new Vector3(x * size, upward, y * size);
            Vector3 v2 = new Vector3(x * size + size, upward, y * size);
            Vector3 v3 = new Vector3(x * size, upward, y * size + size);
            Vector3 v4 = new Vector3(x * size + size, upward, y * size + size);

            Vector3 v5 = new Vector3(x * size, downward, y * size);
            Vector3 v6 = new Vector3(x * size + size, downward, y * size);
            Vector3 v7 = new Vector3(x * size, downward, y * size + size);
            Vector3 v8 = new Vector3(x * size + size, downward, y * size + size);

            Vector3[] vertices = {
            v5,//new Vector3 (0, 0, 0),
            v6,//new Vector3 (1, 0, 0),
            v2,//new Vector3 (1, 1, 0),
            v1,//new Vector3 (0, 1, 0),
            v3,//new Vector3 (0, 1, 1),
            v4,//new Vector3 (1, 1, 1),
            v8,//new Vector3 (1, 0, 1),
            v7,//new Vector3 (0, 0, 1),
        };
            verts.AddRange<Vector3>(vertices);// new Vector3[] { v1, v2, v3, v4 });
            var len = verts.Count - 8;

            int[] triangles = {
            0 + len, 2 + len, 1 + len, //face front
	        0 + len, 3 + len, 2 + len,
            2 + len, 3 + len, 4 + len, //face top
	        2 + len, 4 + len, 5 + len,
            1 + len, 2 + len, 5 + len, //face right
	        1 + len, 5 + len, 6 + len,
            0 + len, 7 + len, 4 + len, //face left
	        0 + len, 4 + len, 3 + len,
            5 + len, 4 + len, 7 + len, //face back
	        5 + len, 7 + len, 6 + len,
            0 + len, 6 + len, 7 + len, //face bottom
	        0 + len, 1 + len, 6 + len
        };
            tris.AddRange<int>(triangles);// new int[] { 0 + len, 3 + len, 2 + len, 0 + len, 1 + len, 3 + len });
        }

        private void GenerateMesh(Mesh mesh, ref List<Vector3> verts, ref List<int> tris)
        {
            mesh.Clear();
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        private VoxelProxy CreateObject(Mesh mesh, Color c, string name = "")
        {
            GameObject o = null;
            if (voxelObject != null)
            {
                o = Instantiate(voxelObject) as GameObject;
            }
            o.name = name;

            o.transform.SetParent(Root);
            var mf = o.GetComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mc = o.GetComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            var r = o.GetComponent<MeshRenderer>();
            if (c.Equals(color_Red))
            {
                r.sharedMaterial = Instantiate(mat_red);
                r.sharedMaterial.SetColor("_WireColor", c);
            }
            else if (c.Equals(color_Blue))
            {
                r.sharedMaterial = Instantiate(mat_blue);
                r.sharedMaterial.SetColor("_WireColor", c);
            }
            else if (c.Equals(color_Green))
            {
                r.sharedMaterial = Instantiate(mat_green);
                r.sharedMaterial.SetColor("_WireColor", c);
            }
            else if (c.Equals(color_Yellow))
            {
                r.sharedMaterial = Instantiate(mat_safe);
                r.sharedMaterial.SetColor("_WireColor", c);
            }
            else if (c.Equals(color_LightBlue))
            {
                r.sharedMaterial = Instantiate(mat_waterWalkable);
                r.sharedMaterial.SetColor("_WireColor", c);
            }
            else
            {
                r.sharedMaterial = Instantiate(voxel_unknown);
                r.sharedMaterial.SetColor("_Color", c);
            }

            var v = o.GetComponent<VoxelProxy>();
            return v;
        }

        private void ClearMeshs()
        {
            if (meshs != null)
            {
                foreach (var item in meshs)
                {
                    DestroyImmediate(item, true);
                }
                meshs.Clear();
                meshs = null;
            }
        }

        private void DestroyResource()
        {
            ClearMeshs();

            if (data != null) data = null;
            if (Instance != null) Instance = null;
            if (voxelMeta != null) voxelMeta = null;

            ResetSelectVoxelList();
            ResetUpdateVoxelList();
            selectedVoxelObjList = null;
            selectedVoxelDataList = null;
            updateVoxelObjList = null;
            updateVoxelDataList = null;
        }

        private void OnDestroy()
        {
            DestroyResource();
        }

        public Color UintToColor(uint d)
        {
            float a = (byte)(d >> 24) / 255f;
            float r = (byte)(d >> 16) / 255f;
            float g = (byte)(d >> 8) / 255f;
            float b = (byte)(d) / 255f;
            Color c = new Color(r, g, b, a);
            return c;
        }

        private bool VoxelDirty(List<TempVoxel> voxels)
        {
            var retValue = false;
            if (voxels != null && voxels.Count > 0)
            {
                foreach (var voxel in voxels)
                {
                    if (voxel.isDirty)
                    {
                        retValue = true;
                        break;
                    }
                }
            }

            return retValue;
        }

        //private GameObject FindVoxelObject(List<TempVoxel> voxels)
        //{
        //    var voxelProxys = gameObject.GetComponentsInChildren<VoxelProxy>();
        //    foreach (var voxel in voxels)
        //    {
        //        foreach (var voxelProxy in voxelProxys)
        //        {
        //            foreach (var voxelObj in voxelProxy.voxels)
        //            {
        //                if (voxel.x == voxelObj.x && voxel.y == voxelObj.y)
        //                {
        //                    return voxelProxy.gameObject;
        //                }
        //            }
        //        }
        //    }

        //    return null;
        //}

        private GameObject FindVoxelObjectByVoxel(TempVoxel voxel)
        {
            var voxelProxys = gameObject.GetComponentsInChildren<VoxelProxy>();

            foreach (var voxelProxy in voxelProxys)
            {
                foreach (var voxelObj in voxelProxy.voxels)
                {
                    if (voxel.x == voxelObj.x && voxel.y == voxelObj.y && voxel.layer == voxelObj.layer)
                    {
                        return voxelProxy.gameObject;
                    }
                }
            }

            return null;
        }

        private void GenerateVoxels(bool isChange = false)
        {
            if (meshs == null)
            {
                meshs = new List<Mesh>();
            }

            var xw = data.voxels.GetLength(0);
            var yw = data.voxels.GetLength(1);

            int len = 65535 / 8;

            Dictionary<uint, List<TempVoxel>> color_voxel_pair = new Dictionary<uint, List<TempVoxel>>();
            for (int x = 0; x < xw; x++)
            {
                for (int y = 0; y < yw; y++)
                {
                    var vs = data.voxels[x, y];
                    for (int i = 0; i < vs.Length; i++)
                    {
                        var v = vs[i];
                        if (!color_voxel_pair.TryGetValue(v.color, out List<TempVoxel> tvstmp))
                        {
                            tvstmp = new List<TempVoxel>();
                            color_voxel_pair[v.color] = tvstmp;
                        }
                        var tc = new TempVoxel
                        {
                            x = x,
                            y = y,
                            upward = v.upward,
                            downward = v.downward,
                            color = v.color,
                            layer = i,
                            isDirty = v.isDirty
                        };
                        tvstmp.Add(tc);
                    }
                }
            }

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<TempVoxel> tvs = new List<TempVoxel>();

            foreach (var item in color_voxel_pair)
            {
                Color c = UintToColor(item.Key);
                int count = 0;


                for (int i = 0; i < item.Value.Count; i++)
                {
                    var tc = item.Value[i];
                    tvs.Add(tc);
                    GenerateVoxel(data.size, tc.upward, tc.downward, tc.x, tc.y, ref verts, ref tris);
                    count++;

                    VoxelProxy v;
                    var name = string.Format("{0}_{1}", tc.x, tc.y);

                    if (count >= len)
                    {
                        if (isChange)
                        {
                            var changeState = VoxelDirty(tvs);
                            if (changeState)
                            {
                                var m = new Mesh();
                                meshs.Add(m);
                                GenerateMesh(m, ref verts, ref tris);

                                var proxyTrans = Root.Find(name);
                                if (proxyTrans != null && proxyTrans.gameObject != null)
                                {
                                    proxyTrans.parent = null;
                                    DestroyImmediate(proxyTrans.gameObject);
                                }

                                v = CreateObject(m, c, name);
                            }
                            else
                            {
                                var obj = FindVoxelObjectByVoxel(tvs[0]);
                                if (obj != null)
                                {
                                    if (obj.name.Contains("delete"))
                                    {
                                        var m = new Mesh();
                                        meshs.Add(m);
                                        GenerateMesh(m, ref verts, ref tris);

                                        obj.Parent(null);
                                        DestroyImmediate(obj);
                                        v = CreateObject(m, c, name);
                                    }
                                    else
                                    {
                                        v = obj.GetComponent<VoxelProxy>();
                                    }
                                }
                                else
                                {
                                    var m = new Mesh();
                                    meshs.Add(m);
                                    GenerateMesh(m, ref verts, ref tris);

                                    v = CreateObject(m, c, name);
                                }
                            }
                        }
                        else
                        {
                            var m = new Mesh();
                            meshs.Add(m);
                            GenerateMesh(m, ref verts, ref tris);

                            v = CreateObject(m, c, name);
                        }

                        v.voxels = tvs.ToArray();
                        count = 0;
                        verts.Clear();
                        tris.Clear();
                        tvs.Clear();
                    }
                }

                if (verts.Count > 0)
                {
                    var maxtc = item.Value[item.Value.Count - 1];
                    var name = string.Format("{0}_{1}", maxtc.x, maxtc.y);
                    VoxelProxy v;
                    if (isChange)
                    {
                        var changeState = VoxelDirty(tvs);
                        if (changeState)
                        {
                            var proxyTrans = Root.Find(name);
                            if (proxyTrans != null && proxyTrans.gameObject != null)
                            {
                                proxyTrans.parent = null;
                                DestroyImmediate(proxyTrans.gameObject);
                            }

                            var m = new Mesh();
                            meshs.Add(m);
                            GenerateMesh(m, ref verts, ref tris);
                            v = CreateObject(m, c, name);
                        }
                        else
                        {
                            var obj = FindVoxelObjectByVoxel(tvs[0]);
                            if (obj != null)
                            {
                                if (obj.name.Contains("delete"))
                                {
                                    var m = new Mesh();
                                    meshs.Add(m);
                                    GenerateMesh(m, ref verts, ref tris);

                                    obj.Parent(null);
                                    DestroyImmediate(obj);
                                    v = CreateObject(m, c, name);
                                }
                                else
                                {
                                    v = obj.GetComponent<VoxelProxy>();
                                }
                            }
                            else
                            {
                                var m = new Mesh();
                                meshs.Add(m);
                                GenerateMesh(m, ref verts, ref tris);

                                v = CreateObject(m, c, name);
                            }

                        }
                    }
                    else
                    {
                        var m = new Mesh();
                        meshs.Add(m);
                        GenerateMesh(m, ref verts, ref tris);
                        v = CreateObject(m, c, name);
                    }

                    v.voxels = tvs.ToArray();
                    count = 0;
                    verts.Clear();
                    tris.Clear();
                    tvs.Clear();
                }
            }
            verts.Clear();
            tris.Clear();
            tvs.Clear();
            verts = null;
            tris = null;
            tvs = null;

            color_voxel_pair.Clear();
            color_voxel_pair = null;
        }

        private void GenerateVoxelsFromVox(VoxelTerrain3D vt3d)
        {
            foreach (Transform item in Root)
            {
                item.SetParent(null);
                GameObject.DestroyImmediate(item.gameObject);

            }

            meshs = new List<Mesh>();

            var xw = vt3d.XCount;
            var yw = vt3d.YCount;


            int len = 65535 / 8;
            meshs.Clear();

            Dictionary<uint, List<TempVoxel>> color_voxel_pair = new Dictionary<uint, List<TempVoxel>>();
            for (int x = 0; x < xw; x++)
            {
                for (int y = 0; y < yw; y++)
                {
                    var vs = vt3d.GetVoxelCell(x, y);
                    if (vs != null)
                    {
                        for (int i = 0; i < vs.LayerCount; i++)
                        {
                            var v = vs.GetLayer(i);
                            List<TempVoxel> tvs;
                            if (!color_voxel_pair.TryGetValue(v.Color, out tvs))
                            {
                                tvs = new List<TempVoxel>();
                                color_voxel_pair[v.Color] = tvs;
                            }
                            var tc = new TempVoxel();
                            tc.x = x;
                            tc.y = yw - y;
                            tc.upward = v.Upward;
                            tc.downward = v.Downward;
                            tc.color = v.Color;
                            tc.layer = i;
                            tvs.Add(tc);
                        }
                    }
                }
            }

            foreach (var item in color_voxel_pair)
            {
                Color c = UintToColor(item.Key);
                int count = 0;
                List<Vector3> verts = new List<Vector3>();
                List<int> tris = new List<int>();

                for (int i = 0; i < item.Value.Count; i++)
                {
                    var tc = item.Value[i];
                    GenerateVoxel(vt3d.GridCellSize, tc.upward, tc.downward, tc.x, tc.y, ref verts, ref tris);
                    count++;

                    if (count >= len)
                    {
                        var m = new Mesh();
                        meshs.Add(m);
                        GenerateMesh(m, ref verts, ref tris);

                        CreateObject(m, c);
                        count = 0;
                        verts.Clear();
                        tris.Clear();
                    }
                }
                if (verts.Count > 0)
                {
                    var m = new Mesh();
                    meshs.Add(m);
                    GenerateMesh(m, ref verts, ref tris);
                    CreateObject(m, c);
                    count = 0;
                    verts.Clear();
                    tris.Clear();
                }
            }
        }

        public void OnMouseDown()
        {
            mouseDownState = true;
        }

        public void OnMouseUp()
        {
            mouseDownState = false;
            if (brushApplyType == 0)
            {
                SelectVoxel();
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("导入Meta"))
            {
                metaPath = EditorUtility.OpenFilePanel("Select File", "", "bin");
                ImportMetaConfig();
            }

            if (GUILayout.Button("导入XML"))
            {
                xmlPath = EditorUtility.OpenFilePanel("Select File", "", "xml");
                ImportXmlConfig();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("保存"))
            {
                SaveVoxelData();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("导出"))
            {
                ExportConfig();
            }
            GUILayout.EndHorizontal();

            if (isLoadData)
            {
                if (selectedVoxelDataList != null && selectedVoxelDataList.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("体素块(U):");
                    selectedVoxelDataList[0].upward = float.Parse(GUILayout.TextField(selectedVoxelDataList[0].upward.ToString(), GUILayout.Width(50)));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("体素块(D):");
                    selectedVoxelDataList[0].downward = float.Parse(GUILayout.TextField(selectedVoxelDataList[0].downward.ToString(), GUILayout.Width(50)));
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("笔刷类型:");
                brushIndex = GUILayout.SelectionGrid(brushIndex, voxelTypes, 5);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("笔刷大小:");
                brushType = GUILayout.SelectionGrid(brushType, brushSizes, 2);
                if (brushType == 0)
                {
                    GUILayout.BeginHorizontal();
                    brush_RectWidth = Mathf.Floor(GUILayout.HorizontalSlider(brush_RectWidth, 1, brush_RectWidth_Max, GUILayout.Width(200)));
                    brush_RectWidth = Mathf.Floor(float.Parse(GUILayout.TextField(brush_RectWidth.ToString(), brush_RectWidth_Max, GUILayout.Width(50))));
                    GUILayout.EndHorizontal();
                }
                else if (brushType == 1)
                {
                    GUILayout.BeginHorizontal();
                    brush_CircleRadius = Mathf.Floor(GUILayout.HorizontalSlider(brush_CircleRadius, 1, brush_CircleRadius_Max, GUILayout.Width(200)));
                    brush_CircleRadius = Mathf.Floor(float.Parse(GUILayout.TextField(brush_CircleRadius.ToString(), brush_CircleRadius_Max, GUILayout.Width(50))));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("笔刷应用");
                brushApplyType = GUILayout.SelectionGrid(brushApplyType, brushApplyTypes, 3);
                GUILayout.EndHorizontal();
            }
        }

        private void BrushVoxel()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, 1 << LayerMask.NameToLayer("VOX_DATA")))
            {
                VoxelProxy vp = hitInfo.collider.GetComponent<VoxelProxy>();
                if (vp != null && data != null)
                {
                    ResetSelectVoxelList();

                    int x = (int)(Mathf.Floor(hitInfo.point.x / data.size));
                    int y = (int)(Mathf.Floor(hitInfo.point.z / data.size));
                    var updateVoxelDataListTmp = new List<TempVoxel>();
                    if (brushType == 0)
                    {
                        //矩形范围
                        for (int i = 0; i < brush_RectWidth; i++)
                        {
                            for (int j = 0; j < brush_RectWidth; j++)
                            {
                                foreach (var item in vp.voxels)
                                {
                                    if (item.x == x + i && item.y == y - j)
                                    {
                                        item.hIndex = i;
                                        item.vIndex = j;
                                        var objName = string.Format("{0}_{1}_Update", item.x, item.y);
                                        var brushTrans = Root.Find(objName);
                                        if (brushTrans != null)
                                        {
                                            var brushObj = brushTrans.gameObject;
                                            if (brushObj != null)
                                            {
                                                if (updateVoxelObjList.Count > 0)
                                                {
                                                    if (updateVoxelObjList.Contains(brushObj))
                                                    {
                                                        updateVoxelObjList.Remove(brushObj);
                                                    }
                                                }
                                                brushObj.Parent(null);
                                                DestroyImmediate(brushObj);
                                            }
                                        }

                                        if (updateVoxelDataList.Count > 0)
                                        {
                                            if (updateVoxelDataList.Contains(item))
                                            {
                                                updateVoxelDataList.Remove(item);
                                            }
                                        }

                                        if (updateVoxelDataListTmp.Count > 0)
                                        {
                                            if (updateVoxelDataListTmp.Contains(item))
                                            {
                                                updateVoxelDataListTmp.Remove(item);
                                            }
                                        }

                                        updateVoxelDataListTmp.Add(item);
                                        break;
                                    }
                                }
                            }
                        }

                        if (updateVoxelDataListTmp.Count > 0)
                        {
                            UpdateVoxel(updateVoxelDataListTmp);
                        }
                    }
                    else if (brushType == 1)
                    {
                        //矩形范围
                        for (int i = 0; i < brush_CircleRadius; i++)
                        {
                            for (int j = 0; j < brush_CircleRadius; j++)
                            {
                                foreach (var item in vp.voxels)
                                {
                                    if (item.x == x + i && item.y == y - j)
                                    {
                                        var objName = string.Format("{0}_{1}_Update", item.x, item.y);
                                        var brushTrans = Root.Find(objName);
                                        if (brushTrans != null)
                                        {
                                            var brushObj = brushTrans.gameObject;
                                            if (brushObj != null)
                                            {
                                                if (updateVoxelObjList != null && updateVoxelObjList.Count > 0)
                                                {
                                                    if (updateVoxelObjList.Contains(brushObj))
                                                    {
                                                        updateVoxelObjList.Remove(brushObj);
                                                    }
                                                }
                                                brushObj.Parent(null);
                                                DestroyImmediate(brushObj);
                                            }
                                        }


                                        if (updateVoxelDataList != null && updateVoxelDataList.Count > 0)
                                        {
                                            if (updateVoxelDataList.Contains(item))
                                            {
                                                updateVoxelDataList.Remove(item);
                                            }
                                        }

                                        if (updateVoxelDataListTmp.Count > 0)
                                        {
                                            if (updateVoxelDataListTmp.Contains(item))
                                            {
                                                updateVoxelDataListTmp.Remove(item);
                                            }
                                        }
                                        updateVoxelDataListTmp.Add(item);

                                        break;
                                    }
                                }
                            }
                        }

                        if (updateVoxelDataListTmp.Count > 0)
                        {
                            UpdateVoxel(updateVoxelDataListTmp);
                        }
                    }
                    updateVoxelDataListTmp.Clear();
                    updateVoxelDataListTmp = null;
                }
            }
        }

        private void LoadXmlData(string path)
        {
            var xmlDoc = XmlUtil.LoadXML(path);
            data = XmlUtil.XmlToObject<VoxelData>(xmlDoc);
        }

        private TempVoxel GetVoxelByPos(int hIndex, int vIndex)
        {
            TempVoxel voxel = new TempVoxel();
            if (selectedVoxelDataList.Count > 0)
            {
                foreach (var data in selectedVoxelDataList)
                {
                    if (data.hIndex == hIndex && data.vIndex == vIndex)
                    {
                        voxel = data;
                        break;
                    }
                }
            }

            return voxel;
        }

        private List<GameObject> GenerateTempVoxelGameObject(List<TempVoxel> itemList, Color color, bool isUpdateSelected = false)
        {
            List<GameObject> voxelList = new List<GameObject>();
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            foreach (var item in itemList)
            {
                if (brushApplyType == 0)
                {
                    var selectName = string.Format("{0}_{1}_Select", item.x, item.y);
                    var selectVoxelTrans = Root.Find(selectName);
                    if (selectVoxelTrans != null)
                    {
                        var selectVoxelObj = selectVoxelTrans.gameObject;
                        if (selectVoxelObj != null)
                        {
                            DestroyImmediate(selectVoxelObj);
                        }
                    }

                    GenerateVoxel(data.size, item.upward, item.downward, item.x, item.y, ref verts, ref tris);
                    var m = new Mesh();
                    GenerateMesh(m, ref verts, ref tris);
                    var o = new GameObject();
                    o.transform.SetParent(Root);
                    var mf = o.AddComponent<MeshFilter>();
                    mf.sharedMesh = m;
                    var render = o.AddComponent<MeshRenderer>();

                    o.name = string.Format("{0}_{1}_Select", item.x, item.y);
                    render.sharedMaterial = Instantiate(selected_mat);

                    render.material.SetColor("_Color", color);
                    if (isUpdateSelected)
                    {
                        foreach (var voxelData in selectedVoxelDataList)
                        {
                            voxelData.color = VoxelBaker.ColorToUint(color);
                        }
                        SaveChangedVoxel();
                    }
                    voxelList.Add(o);
                }
                else
                {
                    //应用笔刷
                    if (brushApplyType == 1)
                    {
                        item.color = VoxelBaker.ColorToUint(color);
                    }
                    else if (brushApplyType == 2)
                    {
                        item.color = VoxelBaker.ColorToUint(color);
                        var voxel = GetVoxelByPos(item.hIndex, item.vIndex);
                        item.upward = voxel.upward;
                        item.downward = voxel.downward;
                    }

                    GenerateVoxel(data.size, item.upward, item.downward, item.x, item.y, ref verts, ref tris);
                    var m = new Mesh();
                    GenerateMesh(m, ref verts, ref tris);
                    var o = new GameObject();
                    o.transform.SetParent(Root);
                    var mf = o.AddComponent<MeshFilter>();
                    mf.sharedMesh = m;
                    var render = o.AddComponent<MeshRenderer>();
                    o.name = string.Format("{0}_{1}_Update", item.x, item.y);

                    if (color.Equals(color_Green))
                    {
                        render.sharedMaterial = Instantiate(updated_green);
                    }
                    else if (color.Equals(color_Red))
                    {
                        render.sharedMaterial = Instantiate(updated_red);
                    }
                    else if (color.Equals(color_Blue))
                    {
                        render.sharedMaterial = Instantiate(updated_blue);
                    }
                    else if (color.Equals(color_Yellow))
                    {
                        render.sharedMaterial = Instantiate(updated_safe);
                    }
                    else if (color.Equals(color_LightBlue))
                    {
                        render.sharedMaterial = Instantiate(updated_waterWalkable);
                    }

                    voxelList.Add(o);
                    SaveUpdateVoxel(item);
                }
            }
            verts.Clear();
            tris.Clear();
            verts = null;
            tris = null;
            return voxelList;
        }

        private void ResetSelectVoxelList()
        {
            if (selectedVoxelObjList != null && selectedVoxelObjList.Count > 0)
            {
                foreach (var obj in selectedVoxelObjList)
                {
                    if (obj != null)
                    {
                        obj.Parent(null);
                        DestroyImmediate(obj);
                    }
                }
                selectedVoxelObjList.Clear();
            }

            if (selectedVoxelDataList != null && selectedVoxelDataList.Count > 0)
            {
                selectedVoxelDataList.Clear();
            }
        }

        private void ResetUpdateVoxelList()
        {
            if (updateVoxelObjList != null && updateVoxelObjList.Count > 0)
            {
                foreach (var obj in updateVoxelObjList)
                {
                    if (obj != null)
                    {
                        obj.Parent(null);
                        DestroyImmediate(obj);
                    }
                }
                updateVoxelObjList.Clear();
            }

            if (updateVoxelDataList != null && updateVoxelDataList.Count > 0)
            {
                updateVoxelDataList.Clear();
            }
        }

        public void UpdateVoxel(List<TempVoxel> voxel)
        {
            List<GameObject> updateObjListTmp = null;
            switch (brushIndex)
            {
                case 0:
                    updateObjListTmp = GenerateTempVoxelGameObject(voxel, color_Green);
                    break;
                case 1:
                    updateObjListTmp = GenerateTempVoxelGameObject(voxel, color_Red);
                    break;
                case 2:
                    updateObjListTmp = GenerateTempVoxelGameObject(voxel, color_LightBlue);
                    break;
                case 3:
                    updateObjListTmp = GenerateTempVoxelGameObject(voxel, color_Blue);
                    break;
                case 4:
                    updateObjListTmp = GenerateTempVoxelGameObject(voxel, color_Yellow);
                    break;
            }
            updateVoxelObjList.AddRange(updateObjListTmp);
        }

        private void GenerateSelectVoxelGameObjectByType()
        {
            var objList = GenerateTempVoxelGameObject(selectedVoxelDataList, color_White);
            selectedVoxelObjList.AddRange(objList);
        }

        public void SelectVoxel()
        {
            if (brushApplyType > 0) return;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, 1 << LayerMask.NameToLayer("VOX_DATA")))
            {
                VoxelProxy vp = hitInfo.collider.GetComponent<VoxelProxy>();
                if (vp != null && data != null)
                {
                    ResetSelectVoxelList();

                    int x = (int)(Mathf.Floor(hitInfo.point.x / data.size));
                    int y = (int)(Mathf.Floor(hitInfo.point.z / data.size));
                    if (brushType == 0)
                    {
                        //矩形范围
                        for (int i = 0; i < brush_RectWidth; i++)
                        {
                            for (int j = 0; j < brush_RectWidth; j++)
                            {
                                foreach (var item in vp.voxels)
                                {
                                    if (item.x == x + i && item.y == y - j)
                                    {
                                        item.hIndex = i;
                                        item.vIndex = j;
                                        selectedVoxelDataList.Add(item);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (brushType == 1)
                    {
                        //圆形范围
                        for (int i = 0; i < brush_RectWidth; i++)
                        {
                            for (int j = 0; j < brush_RectWidth; j++)
                            {
                                foreach (var item in vp.voxels)
                                {
                                    if (item.x == x + i && item.y == y - j)
                                    {
                                        item.hIndex = i;
                                        item.vIndex = j;
                                        selectedVoxelDataList.Add(item);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    GenerateSelectVoxelGameObjectByType();
                }
            }
        }

        public void SaveUpdateVoxel(TempVoxel updateVoxel)
        {
            //UnityEngine.Debug.Log(string.Format("UpdateVoxel:({3},{4})_{0}_{1}_{2}",UnitToColor(updateVoxel.color),updateVoxel.upward,updateVoxel.downward,updateVoxel.x,updateVoxel.y));

            updateVoxelDataList.Add(updateVoxel);
            var vs = data.voxels[updateVoxel.x, updateVoxel.y];
            var v = vs[updateVoxel.layer];

            if (v.color.Equals(updateVoxel.color))
            {
                v.upward = updateVoxel.upward > updateVoxel.downward ? updateVoxel.upward :
                updateVoxel.downward;
                v.downward = updateVoxel.upward > updateVoxel.downward ? updateVoxel.downward :
                    updateVoxel.upward;
                v.isDirty = true;
                List<VoxelLayer> l = new List<VoxelLayer>(vs);
                data.voxels[updateVoxel.x, updateVoxel.y] =
                    VoxelBaker.MergeVoxels(l, updateVoxel.x, updateVoxel.y,minLevel).ToArray();
                l.Clear();
                l = null;
            }
            else
            {
                //索引位置原有体素块需要被更新
                DeleteSelectedVoxel(updateVoxel, ref vs);

                var obj = FindVoxelObjectByVoxel(updateVoxel);
                if (obj != null && !obj.name.Contains("delete"))
                {
                    obj.name = string.Format("{0}_{1}", obj.name, "delete");
                }

                //索引位置新增体素块
                var newVoxelLayer = new VoxelLayer();
                newVoxelLayer.upward = updateVoxel.upward > updateVoxel.downward ? updateVoxel.upward :
                updateVoxel.downward;
                newVoxelLayer.downward = updateVoxel.upward > updateVoxel.downward ? updateVoxel.downward :
                    updateVoxel.upward;

                newVoxelLayer.isDirty = true;
                newVoxelLayer.color = updateVoxel.color;

                List<VoxelLayer> l = new List<VoxelLayer>(vs);
                l.Add(newVoxelLayer);
                data.voxels[updateVoxel.x, updateVoxel.y] =
                    VoxelBaker.MergeVoxels(l, updateVoxel.x, updateVoxel.y, minLevel).ToArray();
                l.Clear();
                l = null;
            }
        }

        public void SaveChangedVoxel()
        {
            foreach (var voxelData in selectedVoxelDataList)
            {
                var vs = data.voxels[voxelData.x, voxelData.y];
                var v = vs[voxelData.layer];
                v.color = voxelData.color;
                v.upward = voxelData.upward > voxelData.downward ? voxelData.upward :
                    voxelData.downward;
                v.downward = voxelData.upward > voxelData.downward ? voxelData.downward :
                    voxelData.upward;
                v.isDirty = true;
                List<VoxelLayer> l = new List<VoxelLayer>(vs);
                data.voxels[voxelData.x, voxelData.y] =
                    VoxelBaker.MergeVoxels(l, voxelData.x, voxelData.y, minLevel).ToArray();
                l.Clear();
                l = null;
            }
        }

        public void DeleteSelectedVoxel(TempVoxel voxel, ref VoxelLayer[] vs)
        {
            List<VoxelLayer> l = new List<VoxelLayer>(vs);
            l.RemoveAt(voxel.layer);
            vs = VoxelBaker.MergeVoxels(l, voxel.x, voxel.y, minLevel).ToArray();
            l.Clear();
            l = null;

            data.voxels[voxel.x, voxel.y] = vs;
            if (data.voxels[voxel.x, voxel.y] == null
                || data.voxels[voxel.x, voxel.y].Length == 0)
            {
                //黑洞需要填平
                VoxelLayer vl = new VoxelLayer();
                vl.downward = -1;
                vl.upward = 0;
                vl.color = VoxelBaker.ColorToUint(Color.black);
                vl.isDirty = true;
                VoxelLayer[] vls = new VoxelLayer[1] { vl };
                data.voxels[voxel.x, voxel.y] = vls;
            }
        }

        private void InitRoot()
        {
            var childs = Root.GetComponentsInChildren<VoxelProxy>();
            if (childs.Length > 0)
            {
                foreach (var child in childs)
                {
                    if (child != null && child.gameObject != null)
                    {
                        child.transform.parent = null;
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        private void ImportMetaConfig()
        {
            if (!string.IsNullOrEmpty(metaPath))
            {
                InitRoot();
                var bytes = System.IO.File.ReadAllBytes(metaPath);
                using (var mstream = new DeepCore.IO.MemoryStream(bytes))
                {
                    DeepCore.IO.InputStream input = new DeepCore.IO.InputStream(mstream, externalizableFactory);
                    voxelMeta = input.GetExtAny() as VoxelDataMeta;
                }
                MetaToVoxelData();
                sw.Reset();
                sw.Start();
                GenerateVoxels();
                sw.Stop();
                UnityEngine.Debug.Log(string.Format("GenerateVoxels cost time:{0:00}:{1:00}:{2:00}", sw.Elapsed.Minutes, sw.Elapsed.Seconds, sw.Elapsed.Milliseconds / 10));
                isLoadData = true;
            }
        }

        private void ImportXmlConfig()
        {
            if (!string.IsNullOrEmpty(xmlPath))
            {
                InitRoot();
                LoadXmlData(xmlPath);
                sw.Reset();
                sw.Start();
                GenerateVoxels();
                sw.Stop();
                UnityEngine.Debug.Log(string.Format("GenerateVoxels cost time:{0:00}:{1:00}:{2:00}", sw.Elapsed.Minutes, sw.Elapsed.Seconds, sw.Elapsed.Milliseconds / 10));
                isLoadData = true;
            }
        }

        private void ExportConfig()
        {
            if (string.IsNullOrEmpty(xmlPath))
            {
                if (!string.IsNullOrEmpty(metaPath))
                {
                    xmlPath = metaPath.Replace("bin", "xml");
                }
                else
                    return;
            }

            FileUtil.DeleteFileOrDirectory(xmlPath + ".bak");
            FileUtil.CopyFileOrDirectory(xmlPath, xmlPath + ".bak");

            sw.Reset();
            sw.Start();
            XmlSerializer xs = new XmlSerializer();
            var xmlDoc = xs.ObjectToXml(data);
            xmlDoc.Save(xmlPath);
            sw.Stop();
            UnityEngine.Debug.Log(string.Format("xml export time:{0:00}:{1:00}.{2:00}", sw.Elapsed.Minutes, sw.Elapsed.Seconds, sw.Elapsed.Milliseconds / 10));
        }

        private void SaveVoxelData()
        {
            if (string.IsNullOrEmpty(metaPath))
            {
                if (!string.IsNullOrEmpty(xmlPath))
                {
                    metaPath = xmlPath.Replace("xml", "bin");
                }
                else
                    return;
            }

            if (selectedVoxelDataList != null)
            {
                SaveChangedVoxel();
            }

            VoxelDataToMeta();

            SaveMeta(metaPath, voxelMeta);
            sw.Reset();
            sw.Start();
            GenerateVoxels(true);
            ResetSelectVoxelList();
            ResetUpdateVoxelList();
            sw.Stop();
            UnityEngine.Debug.Log(string.Format("GenerateVoxels cost time:{0:00}:{1:00}:{2:00}", sw.Elapsed.Minutes, sw.Elapsed.Seconds, sw.Elapsed.Milliseconds / 10));
        }

        private void SaveMeta(string path, VoxelDataMeta meta)
        {
            using (var mstream = new DeepCore.IO.MemoryStream())
            {
                var output = new DeepCore.IO.OutputStream(mstream, externalizableFactory);
                output.PutExt(meta);
                mstream.Flush();
                System.IO.File.WriteAllBytes(path, mstream.ToArray());
            }
        }

        private void VoxelDataToMeta()
        {
            voxelMeta.size = data.size;
            voxelMeta.xLength = data.xLength;
            voxelMeta.yLength = data.yLength;
            if (voxelMeta.voxels == null)
            {
                voxelMeta.voxels = new VoxelLayerMeta[voxelMeta.xLength, voxelMeta.yLength][];
            }

            for (int i = 0; i < voxelMeta.xLength; i++)
            {
                for (int j = 0; j < voxelMeta.yLength; j++)
                {
                    if (voxelMeta.voxels[i, j] != null)
                    {
                        voxelMeta.voxels[i, j] = null;
                    }

                    voxelMeta.voxels[i, j] = new VoxelLayerMeta[data.voxels[i, j].Length];
                    for (int k = 0; k < data.voxels[i, j].Length; k++)
                    {
                        if (voxelMeta.voxels[i, j][k] != null)
                        {
                            voxelMeta.voxels[i, j][k] = null;
                        }

                        voxelMeta.voxels[i, j][k] = new VoxelLayerMeta
                        {
                            upward = data.voxels[i, j][k].upward,
                            downward = data.voxels[i, j][k].downward,
                            color = data.voxels[i, j][k].color,
                            length = data.voxels[i, j].Length
                        };
                    }
                }
            }
        }

        private void MetaToVoxelData()
        {
            data.size = voxelMeta.size;
            data.xLength = voxelMeta.xLength;
            data.yLength = voxelMeta.yLength;
            if (data.voxels == null)
            {
                data.voxels = new VoxelLayer[data.xLength, data.yLength][];
            }

            for (int i = 0; i < data.xLength; i++)
            {
                for (int j = 0; j < data.yLength; j++)
                {
                    if (data.voxels[i, j] != null)
                    {
                        data.voxels[i, j] = null;
                    }
                    data.voxels[i, j] = new VoxelLayer[voxelMeta.voxels[i, j].Length];

                    for (int k = 0; k < voxelMeta.voxels[i, j].Length; k++)
                    {
                        if (data.voxels[i, j][k] != null)
                        {
                            data.voxels[i, j][k] = null;
                        }

                        data.voxels[i, j][k] = new VoxelLayer
                        {
                            upward = voxelMeta.voxels[i, j][k].upward,
                            downward = voxelMeta.voxels[i, j][k].downward,
                            color = voxelMeta.voxels[i, j][k].color,
                        };
                    }
                }
            }
        }



    }

    static class ParentExt
    {
        public static void Parent(this GameObject obj, Transform transform)
        {
            obj.transform.SetParent(transform);
        }
    }
}
