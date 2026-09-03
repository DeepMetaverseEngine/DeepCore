using DeepCore;
using DeepCore.Geometry.Terrain;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepCore.Unity3D;
using DeepCore.Unity3D.Voxel;
using DeepCore.Voxel.Data;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneEditor.EditorToScene;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace DeepMetaGame.Unity.Preview.SceneEditor
{
    public class UnityEditorWorld : SceneEditorBehavior
    {
        //-----------------------------------------------------------------------------------------------------------------
        void Awake()
        {
            World = this;
        }
        void Start()
        {
        }
        void Update()
        {
            try
            {
                if (Proxy.nodeVoxel)
                {
                    Proxy.nodeVoxel.SetActive(VS.ShowSceneVoxel);
                }
                if (Proxy.nodeScene)
                {
                    Proxy.nodeScene.SetActive(VS.ShowSceneRes);
                }
                if (Proxy.nodeNavMesh)
                {
                    Proxy.nodeNavMesh.SetActive(VS.ShowSceneNav);
                }
                foreach (var o in objects.Values.ToArray())
                {
                    o.EditorUpdate();
                }
                if (SceneResource != null)
                {
                    SceneResource.IsVisible = VS.ShowSceneRes;
                    SceneResource.UpdateResource(this.gameObject);
                }
            }
            catch (Exception ex)
            {
                PLog(ex);
            }
        }
        void OnDestroy()
        {
            try
            {
                foreach (var o in objects.Values.ToArray())
                {
                    Destroy(o.gameObject);
                }
                objects.Clear();
            }
            catch (Exception ex)
            {
                PLog(ex);
            }
        }
        //-----------------------------------------------------------------------------------------------------------------

        #region Terrain      

        internal readonly TerrainInfo terrain = new TerrainInfo();
        public float TerrainH { get => terrain.TerrainH; private set => terrain.TerrainH = value; }
        public float TerrainW { get => terrain.TerrainW; private set => terrain.TerrainW = value; }
        public float GridSize { get; private set; } = 0.5f;
        public MsgSetScene Data { get; private set; }
        public Light DefaultLight { get; private set; }
        public SceneData SceneData { get; private set; }
        public IViewResource SceneResource { get; private set; }
        public ITerrainWorld VoxelWorld { get; private set; }
        public ITerrain VoxelTerrain { get; private set; }

        internal void InitVoxelWorld(ITerrainWorld vox, MsgSetScene scene, SceneData sd)
        {
            this.SceneData = sd;
            try
            {
                if (vox != null)
                {
                    Debug.Log($"Load Voxel : " + vox);
                    this.TerrainH = vox.Terrain.TotalSizeY;
                    this.TerrainW = vox.Terrain.TotalSizeX;
                    this.terrain.ResX = vox.Terrain.ResourceStartX;
                    this.terrain.ResY = vox.Terrain.ResourceStartY;
                    this.GridSize = vox.Terrain.GridCellSize;
                    this.VoxelWorld = vox;
                    this.VoxelTerrain = vox.Terrain;
                    if (Proxy.nodeVoxel && RTG.TempVoxel != null && RTG.TempVoxel.TryGetComponentInChildren<MeshFilter>(out var cube))
                    {
                        if (vox is VoxelWorld vox3D)
                        {
                            var chunks = VoxelToMesh.BakeVoxelChunkMeshs(vox3D.Terrain);
                            while (chunks.TryPop(out var chunk))
                            {
                                var body = Instantiate(cube.gameObject);
                                body.GetComponent<MeshFilter>().mesh = chunk.GetMesh();
                                if (!body.TryGetComponent<MeshCollider>(out var collider))
                                {
                                    collider = body.AddComponent<MeshCollider>();
                                }
                                body.transform.localPosition = chunk.position;
                                body.transform.SetParent(Proxy.nodeVoxel.transform);
                                if (LayerMask.NameToLayer(RTG.RayCastTerrainLayerName) >= 0)
                                {
                                    body.SetLayer(RTG.RayCastTerrainLayerName);
                                }
                                body.SetActive(true);
                            }
                        }
                        RTG.AddEditorVoxel(Proxy.nodeVoxel);
                        Proxy.nodeVoxel.transform.localPosition = new Vector3(
                                                      vox.Terrain.ResourceStartX,
                                                      0,
                                                      vox.Terrain.ResourceStartY);
                    }
                }
                else if (scene.Data is ZoneInfo zoneInfo)
                {
                    Debug.Log($"Zone Info : ");
                    this.TerrainH = zoneInfo.TotalWidth;
                    this.TerrainW = zoneInfo.TotalHeight;
                    this.terrain.ResX = scene.ResourceStartX;
                    this.terrain.ResY = scene.ResourceStartY;
                    this.GridSize = zoneInfo.GridCellW;
                }
            }
            finally
            {
                if (RTG.TempVoxel != null)
                {
                    RTG.TempVoxel.gameObject.SetActive(false);
                }
                if (RTG.TempGizmoz)
                {
                    RTG.TempGizmoz.gameObject.SetActive(false);
                }
                if (RTG.TempHeadText)
                {
                    RTG.TempHeadText.gameObject.SetActive(false);
                }
            }
        }
        internal void InitLayerResource(MsgSetScene layer)
        {
            Data = layer;
            OnInitResource();
        }


        protected virtual void OnInitResource()
        {
            try
            {
                this.DefaultLight = GameObject.FindObjectOfType<Light>();
                this.SceneResource = Proxy.LoadResource(this, (sender, wrap) =>
                {
                    try
                    {
                        var res = wrap?.AsGameObject();
                        if (res != null)
                        {
                            if (res.transform)
                            {
                                res.transform.SetParent(Proxy.nodeScene.transform, false);
                            }
                            if (LayerMask.NameToLayer(RTG.RayCastTerrainLayerName) >= 0)
                            {
                                res.SetLayer(RTG.RayCastTerrainLayerName);
                            }
                            RTG.AddEditorScene(res);
                            VS.ShowSceneVoxel = false;
                            if (res.TryGetComponentInChildren<Light>(out var sceneLight))
                            {
                                if (DefaultLight != null)
                                {
                                    DefaultLight.enabled = false;
                                }
                                DefaultLight = sceneLight;
                            }
                        }
                        OnInitNavMeshLayer(wrap);
                    }
                    catch (Exception err)
                    {
                        PLog($"Load Scene Resource Error : {Data.FileName} : {err.Message}");
                    }
                    Proxy.rsp_VS();
                    Proxy.RefreshHWND();
                });
            }
            catch (Exception err)
            {
                PLog($"Load Scene Resource Error : {Data.FileName} : {err.Message}");
            }
        }

        protected virtual void OnInitNavMeshLayer(object scene)
        {
            if (Proxy.nodeNavMesh && RTG.TempNavMesh != null && RTG.TempNavMesh.TryGetComponentInChildren<MeshFilter>(out var cube))
            {
                var body = Instantiate(cube.gameObject);
                // NavMesh.CalculateTriangulation returns a NavMeshTriangulation object.
                NavMeshTriangulation meshData = NavMesh.CalculateTriangulation();

                // Create a new mesh and chuck in the NavMesh's vertex and triangle data to form the mesh.
                Mesh mesh = new Mesh();
                mesh.SetVertices(meshData.vertices);
                mesh.SetIndices(meshData.indices, MeshTopology.Triangles, 0);

                // Assigns the newly-created mesh to the MeshFilter on the same GameObject.
                body.GetComponent<MeshFilter>().mesh = mesh;
                if (!body.TryGetComponent<MeshCollider>(out var collider))
                {
                    collider = body.AddComponent<MeshCollider>();
                }
                body.transform.SetParent(Proxy.nodeNavMesh.transform);
                if (LayerMask.NameToLayer(RTG.RayCastTerrainLayerName) >= 0)
                {
                    body.SetLayer(RTG.RayCastTerrainLayerName);
                }
                body.SetActive(true);
                RTG.TempNavMesh.gameObject.SetActive(false);
            }

        }



        public bool RayCastTerrain(Ray ray, out RaycastHit hitInfo)
        {
            var hitMask = LayerMask.GetMask(RTG.RayCastTerrainLayerName);
            if (Physics.Raycast(ray, out hitInfo, RTG.RayCastMaxDistance, hitMask)) // Voxel Mask 6
            {
                return true;
            }
            return false;
        }
        public bool RayCastVoxelTerrainLayer(Ray ray, out DeepCore.Geometry.Vector3? hitPoint, out ITerrainLayer hitLayer)
        {
            if (VoxelTerrain != null) // Voxel Mask 6
            {
                if (VoxelTerrain.RayCast(new DeepCore.Geometry.Ray()
                {
                    Position = UnityWorldToBattlePosition(ray.origin),
                    Direction = TransHelper.UnityToBattleOffset(ray.direction),
                    //distance = VoxelTerrain.TotalSizeX * VoxelTerrain.TotalSizeY
                }, out var _hitPoint, out hitLayer))
                {
                    hitPoint = _hitPoint;
                    return true;
                }
            }
            hitPoint = null;
            hitLayer = null;
            return false;
        }

        public bool RayCastBasePlane(Ray ray, out float distance, out Vector3 hitpoint)
        {
            var plane = new Plane(
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 1));
            if (plane.Raycast(ray, out distance))
            {
                hitpoint = ray.GetPoint(distance);
                return true;
            }
            hitpoint = Vector3.zero;
            return false;
        }


        #endregion
        //-----------------------------------------------------------------------------------------------------------------
        #region Objects

        //---------------------------------------------------

        private HashMap<string, UnityEditorObject> objects = new HashMap<string, UnityEditorObject>();
        public string SelectedObjectName { get; private set; }
        public UnityEditorObject SelectedObject
        {
            get
            {
                if (!string.IsNullOrEmpty(SelectedObjectName) && objects.TryGetValue(SelectedObjectName, out var obj))
                {
                    return obj.GetComponent<UnityEditorObject>();
                }
                return null;
            }
        }
        public IEnumerable<UnityEditorObject> Objects => objects.Values;

        //---------------------------------------------------
        public void AddUnit(MsgPutUnit msg)
        {
            AddObject<UnityEditorUnit>(msg);
        }
        public void AddItem(MsgPutItem msg)
        {
            AddObject<UnityEditorItem>(msg);
        }
        public void AddRegion(MsgPutRegion msg)
        {
            AddObject<UnityEditorRegion>(msg);
        }
        public void AddDecoration(MsgPutDecoration msg)
        {
            AddObject<UnityEditorDecoration>(msg);
        }
        public void AddPoint(MsgPutPoint msg)
        {
            AddObject<UnityEditorWayPoint>(msg);
        }
        public void AddArea(MsgPutArea msg)
        {
            AddObject<UnityEditorArea>(msg);
        }
        private void AddObject<T>(MsgPutObject put) where T : UnityEditorObject
        {
            try
            {
                var selectedName = SelectedObjectName;

                var name = put.ObjData.Name;
                var obj = Proxy.OnCreateObject<T>(Proxy.nodeObjects.transform, put.ObjData.Name);
                obj.Init(put);

                RemoveObject(name);

                objects.Add(obj.Name, obj);
                if (LayerMask.NameToLayer(RTG.RayCastObjectLayerName) >= 0)
                {
                    obj.gameObject.SetLayer(RTG.RayCastObjectLayerName);
                }
                //if (Proxy.NodeObjects) obj.transform.SetParent(Proxy.NodeObjects);
                obj.ResetFromData();
                //RTG.AddEditorObject(obj.gameObject);
                if (selectedName == obj.name)
                {
                    Select(obj, false);
                }
            }
            catch (Exception ex)
            {
                PLog($"Add Object Error : {put.ObjData.Name}");
                Debug.LogException(ex);
            }
        }

        //---------------------------------------------------

        public UnityEditorObject GetObject(string name)
        {
            if (name == null) return null;
            return objects.Get(name);
        }

        public void RemoveObject(string name)
        {
            if (SelectedObjectName == name)
            {
                SelectedObjectName = null;
            }
            var removed = objects.RemoveByKey(name);
            if (removed != null)
            {
                removed.End();
                Destroy(removed.gameObject);
            }
        }

        public void RenameObject(string srcName, string dstName)
        {
            var removed = objects.RemoveByKey(srcName);
            if (removed != null)
            {
                removed.SetName(dstName);
                objects.Add(dstName, removed);
            }
        }


        public void Deselect(bool fireToEditor)
        {
            if (SelectedObject is UnityEditorObject edit)
            {
                SelectedObjectName = null;
                RTG.TargetObject = null;
                if (fireToEditor)
                {
                    Proxy.rsp_ObjectSelected(edit, false);
                }
            }
        }
        public bool Select(UnityEditorObject u, bool fireToEditor)
        {
            if (u != null)
            {
                if (u != SelectedObject)
                {
                    SelectedObjectName = u.Name;
                    RTG.TargetObject = u.gameObject;
                    if (fireToEditor)
                    {
                        Proxy.rsp_ObjectSelected(u, true);
                    }
                }
                return true;
            }
            else
            {
                Deselect(fireToEditor);
                return false;
            }
        }



        #endregion
        //-----------------------------------------------------------------------------------------------------------------

        #region _HUD_  

        private bool initGUI = false;
        private Texture2D txt_back;

        private Texture2D tex_show_unit;
        private Texture2D tex_show_item;
        private Texture2D tex_show_region;
        private Texture2D tex_show_deco;
        private Texture2D tex_show_point;
        private Texture2D tex_show_area;

        private Texture2D tex_show_map_voxel;
        private Texture2D tex_show_map_res;
        private Texture2D tex_show_map_nav;

        private Texture2D tex_show_unit_body;
        private Texture2D tex_show_unit_res;
        private Texture2D tex_show_unit_name;

        private Texture2D tex_show_in_black_hole;
        private Texture2D tex_show_line_render;

        private Texture2D tex_dock_grid;

        public Texture2D TexWayPointLink { get; private set; }


        void OnInitGUI()
        {
            var asm = typeof(UnityEditorWorld).Assembly;
            txt_back = Proxy.Textures.MakeTexture(GetType(), "txt_back", 64, 64, Color.black.SetAlpha(0.3f));
            tex_show_unit = Proxy.Textures.MakeAssemblyTexture(asm, "icon_app_28.png");
            tex_show_item = Proxy.Textures.MakeAssemblyTexture(asm, "icon_game_61.png");
            tex_show_region = Proxy.Textures.MakeAssemblyTexture(asm, "icon_common_15.png");
            tex_show_deco = Proxy.Textures.MakeAssemblyTexture(asm, "icon_nature_27.png");
            tex_show_point = Proxy.Textures.MakeAssemblyTexture(asm, "icon_app_181.png");
            tex_show_area = Proxy.Textures.MakeAssemblyTexture(asm, "icon_architecture_18.png");

            tex_show_map_voxel = Proxy.Textures.MakeAssemblyTexture(asm, "icon_common_98.png");
            tex_show_map_res = Proxy.Textures.MakeAssemblyTexture(asm, "icon_nature_29.png");
            tex_show_map_nav = Proxy.Textures.MakeAssemblyTexture(asm, "icon_app_184.png");

            tex_show_unit_body = Proxy.Textures.MakeAssemblyTexture(asm, "icon_sport_15.png");
            tex_show_unit_res = Proxy.Textures.MakeAssemblyTexture(asm, "icon_game_94.png");
            tex_show_unit_name = Proxy.Textures.MakeAssemblyTexture(asm, "icon_app_31.png");

            tex_show_in_black_hole = Proxy.Textures.MakeAssemblyTexture(asm, "icon_common_37.png");
            tex_show_line_render = Proxy.Textures.MakeAssemblyTexture(asm, "icon_arrow_67.png");

            tex_dock_grid = Proxy.Textures.MakeAssemblyTexture(asm, "icon_common_18.png");
            {
                TexWayPointLink = Proxy.Textures.MakeAssemblyTexture(asm, "icon_simpleshape_23.png");
                TexWayPointLink.wrapMode = TextureWrapMode.Repeat;
            }
        }


        void OnGUI()
        {
            if (initGUI == false)
            {
                initGUI = true;
                try
                {
                    OnInitGUI();
                }
                catch (Exception e)
                {
                    PLog(e);
                }
            }
            // draw tooltip
            try
            {
                DrawHelperText();
                DrawTools();
                GUIUtils.AutoTooltips(new Vector2(0, Screen.height - 32));
            }
            catch (Exception e)
            {
                PLog(e);
            }
        }
        private void DrawTools()
        {
#if FALSE
            GUIUtils.DrawGrid(new Vector2(0, Screen.height), new Vector2(40, -32), 40, 1,
            (rect) => { GUIUtils.Toggle(rect, ref IsShowUnit/*           */, new GUIContent() { image = tex_show_unit, tooltip = $"显示单位" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowItem/*           */, new GUIContent() { image = tex_show_item, tooltip = $"显示物品" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowRegion/*         */, new GUIContent() { image = tex_show_region, tooltip = $"显示区域" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowPoint/*          */, new GUIContent() { image = tex_show_point, tooltip = $"显示路点" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowDecoration/*     */, new GUIContent() { image = tex_show_deco, tooltip = $"显示装饰物（空气墙）" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowArea/*           */, new GUIContent() { image = tex_show_area, tooltip = $"显示首都" }); },
            (rect) => { },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowObjectResource/* */, new GUIContent() { image = tex_show_unit_res, tooltip = $"显示【单位资源】开关" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowObjectGizmos/*   */, new GUIContent() { image = tex_show_unit_body, tooltip = $"显示【单位体积】开关" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowObjectName/*     */, new GUIContent() { image = tex_show_unit_name, tooltip = $"显示【单位名字】开关" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowTerrainResource/**/, new GUIContent() { image = tex_show_map_res, tooltip = $"显示【地图资源】开关" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowTerrainVoxel/*   */, new GUIContent() { image = tex_show_map_voxel, tooltip = $"显示【地图体素】开关" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowNavMesh/*        */, new GUIContent() { image = tex_show_map_nav, tooltip = $"显示【地图寻路】开关" }); },
            (rect) => { },
            (rect) => { GUIUtils.Toggle(rect, ref OnlyShowNoneVoxel/*    */, new GUIContent() { image = tex_show_in_black_hole, tooltip = $"检查无体素物件" }); },
            (rect) => { GUIUtils.Toggle(rect, ref IsShowDocker/*         */, new GUIContent() { image = tex_show_line_render, tooltip = $"显示【射线投射】在地表\n绿色为体素\n红色为黑洞" }); },
            (rect) => { },
            (rect) => { GUIUtils.Toggle(rect, ref IsGridToSize/*         */, new GUIContent() { image = tex_dock_grid, tooltip = $"编辑时对齐到网格" }); },
            (rect) => { },
            (rect) =>
            {
                if (GUIUtils.Button(rect, new GUIContent() { text = "齐", tooltip = "所有物件对齐到体素", }))
                {
                    foreach (var obj in Objects)
                    {
                        if (obj.DockVoxel())
                        {
                            Proxy.rsp_ObjectPositionChanged(obj);
                        }
                    }
                }
            },
            (rect) =>
            {
                if (GUIUtils.Button(rect, new GUIContent() { text = "立", tooltip = "当前对齐到体素", }))
                {
                    if (SelectedObject is UnityEditorObject edit && edit.DockVoxel())
                    {
                        Proxy.rsp_ObjectPositionChanged(edit);
                    }
                }
            },
            (rect) => { });
#endif
        }
        private void DrawHelperText()
        {
            var ray = RTG.MainCamera.ScreenPointToRay(Input.mousePosition);
            var sb = new StringBuilder();
            sb.Append($"  ");
            if (SelectedObject is UnityEditorObject obj)
            {
                var wp = obj.transform.position;
                var zp = obj.RuntimePosition;
                sb.Append($" | Object:{obj.Name} ({zp.X.ToString("#0.0")},{zp.Y.ToString("#0.0")},{zp.Z.ToString("#0.0")}) World:({wp.x.ToString("#0.0")}, {wp.y.ToString("#0.0")}, {wp.z.ToString("#0.0")}) |  ");
            }
            if (RayCastVoxelTerrainLayer(ray, out var touchPos, out var layer))
            {
                var zp = layer.UpwardCenterPos;
                zp.X -= GridSize / 2f;
                zp.Y -= GridSize / 2f;
                sb.Append($" | Mouse:({zp.X.ToString("#0.0")}, {zp.Y.ToString("#0.0")}, {zp.Z.ToString("#0.0")}, ARGB={layer.Color.ARGB.ToString("X8")})");
            }
            var style = new GUIStyle(GUI.skin.label);
            {
                style.alignment = TextAnchor.LowerLeft;
                style.normal.textColor = Color.white;
                style.normal.background = Proxy.Textures.MakeTexture(GetType(), "txt_status", 64, 64, Color.black.SetAlpha(0.8f));
            }
            GUI.Label(new Rect(0, Screen.height - 32, Screen.width, 32), sb.ToString(), style);
        }

        #endregion

        //-----------------------------------------------------------------------------------------------------------------
    }
}
