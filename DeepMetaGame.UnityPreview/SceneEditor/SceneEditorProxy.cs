using DeepCore.Game3D.Host;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Unity;
using DeepCore.Unity3D.AB;
using DeepCore.Unity3D.Impl;
using DeepCore.Voxel.Data;
using DeepCore.Xml;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneEditor.EditorToScene;
using DeepMetaGame.Data.ZoneEditor.SceneRequest;
using DeepMetaGame.Data.ZoneEditor.SceneToEditor;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview.SceneEditor
{
    public class SceneEditorProxy : UnityIPC
    {
        public static SceneEditorProxy Proxy { get; private set; }
        //---------------------------------------------------------------------------

        private UnityEditorWorld world;
        private Vector3 lastCameraPos;
        private Quaternion lastCameraLookAt;
        private SceneEditorStatus lastVS;

        public Config CFG { get; private set; }
        public TerrainDefinitionMap TerrainDefinitionMap { get; private set; }
        public UnitActionDefinitionMap UnitActionDefinitionMap { get; private set; }

        public GameObject worldRoot { get; private set; }
        public GameObject nodeVoxel { get; private set; }
        public GameObject nodeNavMesh { get; private set; }
        public GameObject nodeScene { get; private set; }
        public GameObject nodeObjects { get; private set; }
        //---------------------------------------------------------------------------
        protected override void Awake()
        {
            Proxy = this;
            base.Awake();
        }
        protected override void Start()
        {
            this.worldRoot = new GameObject("WorldRoot");
            this.worldRoot.transform.SetParent(this.transform, false);
            {
                this.nodeVoxel = new GameObject("nodeVoxel");
                this.nodeNavMesh = new GameObject("nodeNavMesh");
                this.nodeObjects = new GameObject("nodeObjects");
                this.nodeVoxel.transform.SetParent(worldRoot.transform, false);
                this.nodeNavMesh.transform.SetParent(worldRoot.transform, false);
                this.nodeObjects.transform.SetParent(worldRoot.transform, false);
            }
            this.nodeScene = new GameObject("nodeScene");
            this.nodeScene.transform.SetParent(this.transform, false);

            HandleFromSession += OnMsgReceived;
            //
            SceneEditorBehavior.RTG.OnTargetSelectChanged += RTG_OnTargetEditorObjectChanged;
            SceneEditorBehavior.RTG.OnTargetPropertyChanged += RTG_OnTargetEditorPropertyChanged;

            world = OnCrateWorld();

            // 
            base.Start();

        }
        protected override void Update()
        {
            base.Update();
            if (Session != null)
            {
                try
                {
                    if (world && world.TerrainW > 0 && world.TerrainH > 0)
                    {
                        if (lastCameraLookAt != RTG.MainCamera.transform.rotation || lastCameraPos != RTG.MainCamera.transform.position)
                        {
                            rsp_CameraChanged(true);
                            lastCameraLookAt = RTG.MainCamera.transform.rotation;
                            lastCameraPos = RTG.MainCamera.transform.position;
                        }
                    }
                    if (Input.GetMouseButtonDown(0))
                    {
                        rsp_RspMouseDown();
                    }
                }
                catch { }
            }
        }

        //---------------------------------------------------------------------------

        protected internal virtual UnityEditorWorld OnCrateWorld()
        {
            return gameObject.AddComponent<UnityEditorWorld>();
        }
        protected internal virtual T OnCreateObject<T>(Transform parent, string name) where T : UnityEditorObject
        {
            var go = new GameObject(name);
            var obj = go.AddComponent<T>();
            if (parent) go.transform.SetParent(parent);
            else go.transform.SetParent(transform);
            return obj;
        }
        //         protected internal virtual void SetHeadText(GameObject obj, string text)
        //         {
        //             //             if (obj.TryGetComponent<TMPro.TMP_Text>(out var tmp))
        //             //             {
        //             //                 tmp.text = text;
        //             //             }
        //         }
        //---------------------------------------------------------------------------

        public delegate void LoadSceneCallback(object sender, object go);
        protected internal virtual IViewResource LoadResource(UnityEditorWorld scene, LoadSceneCallback cb)
        {
            var o = RTG.LoadResource(scene.Data.FileName, Data.ResourceType.Scene, scene);
            if (o?.gameObject != null)
            {
                o.transform.SetParent(nodeScene.transform);
            }
            cb(scene, o?.gameObject);
            return o;
        }
        protected internal virtual IViewResource LoadResource(UnityEditorObject obj)
        {
            if (obj is UnityEditorUnit unit)
            {
                if (unit.Info.Abilities.TryGetComponentAs<UnitResourceAbility>(out var res))
                {
                    return RTG.LoadResource(res.FileName, Data.ResourceType.Object, obj);
                }
            }
            else if (obj is UnityEditorItem item)
            {
                if (item.Info.Abilities.TryGetComponentAs<ItemResource>(out var res))
                {
                    return RTG.LoadResource(res.FileName, Data.ResourceType.Object, obj);
                }
            }
            else if (obj is UnityEditorRegion region)
            {
                return RTG.LoadResource(region.Data.ResourceName, Data.ResourceType.Object, obj);
            }
            else if (obj is UnityEditorWayPoint point)
            {
                return RTG.LoadResource(point.Data.ResourceName, Data.ResourceType.Object, obj);
            }
            else if (obj is UnityEditorDecoration deco)
            {
                return RTG.LoadResource(deco.Data.ResourceName, Data.ResourceType.Object, obj);
            }
            else if (obj is UnityEditorArea area)
            {
            }
            return null;
        }


        //---------------------------------------------------------------------------
        #region _From_Editor_

        protected override void Session_Connected(Exception err, object message)
        {
            base.Session_Connected(err, message);
            RTG.IsDebug = false;
        }
        virtual public void OnMsgReceived(ISerializable data)
        {
            //PLog($"> {data}");
            try
            {
                switch (data)
                {
                    case MsgInitPlugin _MsgInitPlugin:
                        e2p_PluginInit(_MsgInitPlugin);
                        break;
                    case SceneEditorStatus _VS:
                        e2p_VS(_VS);
                        break;
                    case MsgSetScene _MsgSetScene:
                        e2p_SetSceneData(_MsgSetScene);
                        break;
                    case MsgPutUnit _MsgPutUnit:
                        e2p_AddUnit(_MsgPutUnit);
                        break;
                    case MsgPutItem _MsgPutItem:
                        e2p_AddItem(_MsgPutItem);
                        break;
                    case MsgPutPoint _MsgPutPoint:
                        e2p_AddPoint(_MsgPutPoint);
                        break;
                    case MsgPutRegion _MsgPutRegion:
                        e2p_AddRegion(_MsgPutRegion);
                        break;
                    case MsgPutDecoration _MsgPutDecoration:
                        e2p_AddDecoration(_MsgPutDecoration);
                        break;
                    case MsgPutArea _MsgPutArea:
                        e2p_AddArea(_MsgPutArea);
                        break;
                    case MsgRemoveObject _MsgRemoveObject:
                        e2p_RemoveObject(_MsgRemoveObject);
                        break;
                    case MsgRenameObject _MsgRenameObject:
                        e2p_RenameObject(_MsgRenameObject);
                        break;
                    case MsgSelectObject _MsgSelectObject:
                        e2p_SelectObject(_MsgSelectObject);
                        break;
                    case MsgShowTerrain _MsgShowTerrain:
                        e2p_MsgShowTerrain(_MsgShowTerrain);
                        break;
                    case MsgLocateCamera _MsgLocateCamera:
                        e2p_SetCamera(_MsgLocateCamera);
                        rsp_CameraChanged(false);
                        break;
                    case MsgSetTerrainBrush _MsgSetTerrainBrush:
                        e2p_MsgSetTerrainBrush(_MsgSetTerrainBrush);
                        break;
                    case MsgSetEditorMode _MsgSetEditorMode:
                        e2p_MsgSetEditorMode(_MsgSetEditorMode);
                        break;
                    case MsgSceneResArgsChanged _MsgSceneResArgsChanged:
                        e2p_MsgSceneResArgsChanged(_MsgSceneResArgsChanged);
                        break;
                    case MsgAdjustAllObjectsPos _MsgAdjustAllObjectsPos:
                        e2p_MsgAdjustAllObjectsPos(_MsgAdjustAllObjectsPos);
                        break;
                    case MsgObjectVisible _MsgObjectVisible:
                        e2p_MsgObjectVisible(_MsgObjectVisible);
                        break;
                    case MsgDockObject _MsgDockObject:
                        e2p_MsgDockObject(_MsgDockObject);
                        break;

                    //                     case RspObjectFieldChanged _RspObjectFieldChanged:
                    //                         e2p_RspObjectFieldChanged(_RspObjectFieldChanged);
                    //                         break;

                    // undo //
                    case RspZoneFlagBathChanged _RspZoneFlagBathChanged:
                        e2p_undo_SetFlagsData(_RspZoneFlagBathChanged);
                        break;
                    case RspZoneFlagChanged _RspZoneFlagChanged:
                        e2p_undo_SetFlagData(_RspZoneFlagChanged);
                        break;
                    case ReqUpdateObject _ReqUpdateObject:
                        e2p_undo_ReqUpdateObject(_ReqUpdateObject);
                        break;
                    case ReqUpdateObjects _ReqUpdateObjects:
                        e2p_undo_ReqUpdateObjects(_ReqUpdateObjects);
                        break;
                    case RspObjectTransformChanged _RspObjectPositionChanged:
                        e2p_undo_RspObjectPositionChanged(_RspObjectPositionChanged);
                        break;
                    case RspObjectFieldChanged _RspObjectFieldChanged:
                        e2p_undo_RspObjectFieldChanged(_RspObjectFieldChanged);
                        break;
                }
            }
            catch (Exception e)
            {
                PLog(e);
            }
        }
        private void e2p_PluginInit(MsgInitPlugin init)
        {
            this.CFG = init.CFG;
            this.TerrainDefinitionMap = init.TerrainMap;
            this.UnitActionDefinitionMap = init.UnitActionMap;
            Proxy.RefreshHWND();
            System.Threading.Tasks.Task.Run(new Action(() =>
            {
                try
                {
                    var SceneData = Templates.LoadScene(init.Scene.SceneID, false, true, false);
                    var voxelBinFile = GetResourceFullPath(init.Scene.VoxelFileName);
                    var vox = ZoneDataFactory.Factory.CreateVoxelWorld(
                        this,
                        Templates,
                        init.Scene.VoxelFileName,
                        SceneData,
                        init.Scene.Data);
                    //!string.IsNullOrEmpty(init.Scene.VoxelFileName) ? VoxelWorld.LoadFromFile(voxelBinFile) : null;
                    MainInvoke(new Action(() =>
                    {
                        world.InitVoxelWorld(vox, init.Scene, SceneData);
                        world.InitLayerResource(init.Scene);
                        SendToSession(new RspEditorState());
                        MainInvoke(new Action(() =>
                        {
                            SceneEditorBehavior.RTG.SetCamera(
                              RTG.MainCamera.transform.position,
                                 new Vector3(world.TerrainW / 2f, 0, world.TerrainH / 2f));
                            rsp_CameraChanged(true);
                        }));
                        if (RTG.NodeTemplates)
                        {
                            RTG.NodeTemplates.SetActive(false);
                        }
                        PLog($"Unity Runtime Started : -parentHWND {ParentHWND}");
                    }));
                }
                catch (Exception err)
                {
                    PLog(err);
                    MainInvoke(new Action(() =>
                    {
                        SendToSession(new RspEditorState());
                    }));
                }
            }));
        }
        private void e2p_VS(SceneEditorStatus vs)
        {
            PropertyUtil.CopyFieldsTo(vs, SceneEditorBehavior.VS);
            if (vs.GridSize > 0)
            {
                RTG.SetSnapToGrid(vs.GridToSize, vs.GridSize);
            }
            else
            {
                RTG.SetSnapToGrid(false, 1f);
            }
            if (lastVS == null || lastVS.Camera2D != vs.Camera2D)
            {
                if (vs.Camera2D)
                {
                    RTG.SetCameraMode(CameraMode.Mode2D);
                }
                else
                {
                    RTG.SetCameraMode(CameraMode.Mode3D);
                }
            }
            lastVS = XmlUtil.CloneObject(vs);
        }
        public void e2p_MsgSetTerrainBrush(MsgSetTerrainBrush msg)
        {
            //             this.LastBrush = msg;
            //             this.LastBrush.Size = Math.Max(1, LastBrush.Size);
        }

        public void e2p_MsgSetEditorMode(MsgSetEditorMode msg)
        {
            //this.LastMode = msg;
        }

        public void e2p_SetCamera(MsgLocateCamera msg)
        {
            var ray = RTG.MainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0), Camera.MonoOrStereoscopicEye.Mono);
            var campos = RTG.MainCamera.transform.position;
            var locpos = SceneEditorBehavior.BattleToUnityWorldPosition(msg.pos);
            var newpos = new Vector3(locpos.x, campos.y, locpos.z);
            if (world.RayCastBasePlane(ray, out var dist, out var hit))
            {
                ray.origin = newpos;
                SceneEditorBehavior.RTG?.SetCamera(newpos, ray.GetPoint(dist));
            }
            else
            {
                SceneEditorBehavior.RTG?.SetCamera(newpos, locpos);
            }
        }

        public void e2p_SetSceneData(MsgSetScene data)
        {
            //             this.terrainZone = data.Data;
            //             this.SpaceDivW = data.SpaceDivSizeW;
            //             if (VoxelTerrain == null)
            //             {
            //                 this.ResetCameraPos(new SizeF(terrainZone.TotalWidth, terrainZone.TotalHeight));
            //             }
        }
        public void e2p_MsgSceneResArgsChanged(MsgSceneResArgsChanged init)
        {

        }
        public void e2p_MsgShowTerrain(MsgShowTerrain msg)
        {
            // this.ShowTerrainGrids2D = msg.Show;
        }

        public void e2p_undo_SetFlagsData(RspZoneFlagBathChanged data)
        {
            //             foreach (RspZoneFlagChanged dd in data.Flags)
            //             {
            //                 TerrainZone[dd.SceneX, dd.SceneY] = dd.Flag;
            //             }
        }
        public void e2p_undo_SetFlagData(RspZoneFlagChanged data)
        {
            //TerrainZone[data.SceneX, data.SceneY] = data.Flag;
        }

        public void e2p_undo_ReqUpdateObject(ReqUpdateObject req)
        {
            var obj = world.GetObject(req.Data.Name);
            if (obj != null)
            {
                if (req.FieldName != null)
                {
                    PropertyUtil.CopyFieldTo(req.FieldName, req.Data, obj.Data);
                }
                else
                {
                    PropertyUtil.CopyFieldsTo(req.Data, obj.Data);
                }
                obj.ResetFromData();
            }
        }
        public void e2p_undo_ReqUpdateObjects(ReqUpdateObjects req)
        {
            if (req.Datas != null)
            {
                foreach (var data in req.Datas)
                {
                    var obj = world.GetObject(data.Name);
                    if (obj != null)
                    {
                        PropertyUtil.CopyFieldsTo(data, obj.Data);
                        obj.ResetFromData();
                    }
                }
            }
        }
        public void e2p_undo_RspObjectPositionChanged(RspObjectTransformChanged dt)
        {
            var wo = world.GetObject(dt.Name);
            if (wo != null)
            {
                wo.Data.Position = new DeepCore.Geometry.Vector3(dt.x, dt.y, dt.z);
                wo.ResetFromData();
            }
        }
        public void e2p_undo_RspObjectFieldChanged(RspObjectFieldChanged dt)
        {
            var wo = world.GetObject(dt.Name);
            if (wo != null)
            {
                //PLog($"e2p_undo_RspObjectFieldChanged : set field '{dt.field}'={dt.value}");
                if (!wo.ObjectData.SetObjectField(dt.field, dt.value))
                {
                    PLog($"e2p_undo_RspObjectFieldChanged : can not set field '{dt.field}'={dt.value}");
                }
                wo.ResetFromData();
            }
            else
            {
                PLog($"e2p_undo_RspObjectFieldChanged : can not find '{dt.Name}'");
            }
        }
        public void e2p_AddUnit(MsgPutUnit msg)
        {
            world.AddUnit(msg);
        }
        public void e2p_AddItem(MsgPutItem msg)
        {
            world.AddItem(msg);
        }
        public void e2p_AddRegion(MsgPutRegion msg)
        {
            world.AddRegion(msg);
        }
        public void e2p_AddPoint(MsgPutPoint msg)
        {
            world.AddPoint(msg);
        }
        public void e2p_AddDecoration(MsgPutDecoration msg)
        {
            world.AddDecoration(msg);
        }
        public void e2p_AddArea(MsgPutArea msg)
        {
            world.AddArea(msg);
        }
        public void e2p_RenameObject(MsgRenameObject msg)
        {
            world.RenameObject(msg.SrcName, msg.DstName);
        }
        public void e2p_RemoveObject(MsgRemoveObject msg)
        {
            world.RemoveObject(msg.Name);
        }
        public void e2p_SelectObject(MsgSelectObject msg)
        {
            var obj = world.GetObject(msg.Name);
            if (world.Select(obj, false))
            {
                if (msg.IsLocateCamera)
                {
                    SceneEditorBehavior.RTG?.LookAt(obj.transform);
                }
            }
        }

        public void e2p_MsgAdjustAllObjectsPos(MsgAdjustAllObjectsPos msg)
        {
            foreach (var wo in world.Objects)
            {
                var wp = wo.Position;
                wp.X += msg.OffsetX;
                wp.Y += msg.OffsetY;
                wp.Z += msg.OffsetZ;
                wo.Data.Position = wp;
                wo.ResetFromData();
            }
        }
        public void e2p_MsgObjectVisible(MsgObjectVisible visible)
        {
            if (visible.state != null)
            {
                foreach (var e in visible.state)
                {
                    var obj = world.GetObject(e.Key);
                    if (obj != null)
                    {
                        obj.IsEditVisible = e.Value;
                    }
                }
            }
        }
        public void e2p_MsgDockObject(MsgDockObject dock)
        {
            if (dock.docking == MsgDockObject.Docking.All)
            {
                foreach (var obj in world.Objects)
                {
                    if (obj.DockVoxel())
                    {
                        Proxy.rsp_ObjectPositionChanged(obj);
                    }
                }
            }
            else if (dock.docking == MsgDockObject.Docking.Specify)
            {
                if (!string.IsNullOrEmpty(dock.objectName))
                {
                    var edit = world.GetObject(dock.objectName);
                    if (edit != null && edit.DockVoxel())
                    {
                        Proxy.rsp_ObjectPositionChanged(edit);
                    }
                }
            }
            else //if (dock.docking == MsgDockObject.Docking.Selected)
            {
                if (world.SelectedObject is UnityEditorObject edit && edit.DockVoxel())
                {
                    Proxy.rsp_ObjectPositionChanged(edit);
                }
            }
        }

        //         public void e2p_RspObjectFieldChanged(RspObjectFieldChanged rsp)
        //         {
        //             var edit = world.GetObject(rsp.Name);
        //             if (edit != null)
        //             {
        //                 edit.Data.SetObjectField(rsp.field, rsp.value);
        //                 edit.ResetFromData();
        //             }
        //         }
        #endregion
        //---------------------------------------------------------------------------
        #region _To_Editor_
        //         void OnDrawGizmos()
        //         {
        //             try
        //             {
        //                 Gizmos.color = Color.yellow;
        //                 var plane = new Plane(
        //                 new Vector3(0, 0, 0),
        //                 new Vector3(1, 0, 0),
        //                 new Vector3(1, 0, 1));
        //                 var frustumCorners = new Vector3[4];
        //                 MainCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), MainCamera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        //                 for (var i = 0; i < 4; i++)
        //                 {
        //                     var p = MainCamera.transform.TransformVector(frustumCorners[i]);
        //                     var tray = new Ray(MainCamera.transform.position, (p - MainCamera.transform.position).normalized);
        //                     if (plane.Raycast(tray, out var dist))
        //                     {
        //                         var hitpoint = tray.GetPoint(dist);
        //                         Gizmos.DrawCube(hitpoint, Vector3.one * 5f);
        //                     }
        //                 }
        //             }
        //             catch { }
        //         }
        public void rsp_VS()
        {
            SendToSession(SceneEditorBehavior.VS);
        }
        public void rsp_CameraChanged(bool refreshMiniMap)
        {
            var rsp = new RspCameraChanged();
            {
                var ray = RTG.MainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0), Camera.MonoOrStereoscopicEye.Mono);
                if (world.RayCastTerrain(ray, out var hit))
                {
                    var hitp = SceneEditorBehavior.UnityWorldToBattlePosition(hit.point);
                    rsp.X = hitp.X;
                    rsp.Y = hitp.Y;
                    rsp.Z = hitp.Z;
                }
            }
            {
                var campos = RTG.MainCamera.transform.position;
                var plane1 = new Plane(
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 1));
                var plane2 = new Plane(
                new Vector3(0, campos.y + 1000, 0),
                new Vector3(1, campos.y + 1000, 0),
                new Vector3(1, campos.y + 1000, 1));
                var frustumCorners = new Vector3[4];
                var hitPoints = new DeepCore.Geometry.Vector3[4];
                RTG.MainCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), RTG.MainCamera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
                for (var i = 0; i < 4; i++)
                {
                    var p = RTG.MainCamera.transform.TransformVector(frustumCorners[i]);
                    var tray = new Ray(campos, (p - campos).normalized);
                    if (plane1.Raycast(tray, out var dist))
                    {
                        var hitpoint = tray.GetPoint(dist);
                        hitPoints[i] = SceneEditorBehavior.UnityWorldToBattlePosition(hitpoint);
                    }
                    else if (plane2.Raycast(tray, out dist))
                    {
                        var hitpoint = tray.GetPoint(dist);
                        hitPoints[i] = SceneEditorBehavior.UnityWorldToBattlePosition(hitpoint);
                    }
                }
                rsp.X1 = hitPoints[0].X;
                rsp.Y1 = hitPoints[0].Y;
                rsp.X2 = hitPoints[1].X;
                rsp.Y2 = hitPoints[1].Y;
                rsp.X3 = hitPoints[2].X;
                rsp.Y3 = hitPoints[2].Y;
                rsp.X4 = hitPoints[3].X;
                rsp.Y4 = hitPoints[3].Y;
            }
            rsp.RefreshMiniMap = refreshMiniMap;
            SendToSession(rsp);
        }

        public void rsp_VoxelFileChanged(string file)
        {
            //             var rsp = new RspVoxelFileChanged();
            //             rsp.VoxelFileName = file;
            //             SendToEditor(rsp);
        }
        public void rsp_FillTerrain()
        {
            //             var changed = world.PopFillTerrainStack();
            //             if (changed.Count > 0)
            //             {
            //                 RspZoneFlagBathChanged bath = new RspZoneFlagBathChanged();
            //                 bath.Flags = changed;
            //                 SendToEditor(bath);
            //             }
        }
        public void rsp_RspTerrainBrushChanged()
        {
            //             RspTerrainBrushChanged changed = new RspTerrainBrushChanged();
            //             changed.Size = LastBrush.Size;
            //             SendToEditor(changed);
            //             scene.RefreshHelper();
        }

        public void rsp_ObjectSelected(UnityEditorObject u, bool selected)
        {
            if (u.IsEditable)
            {
                var rsp = new RspOnObjectSelected();
                rsp.Name = u.Name;
                rsp.Selected = selected;
                SendToSession(rsp);
            }
        }
        public void rsp_ObjectPositionChanged(UnityEditorObject u)
        {
            if (u.IsEditable)
            {
                var rsp = new RspObjectTransformChanged();
                var pos = u.Position;
                rsp.Name = u.Name;
                rsp.x = pos.X;
                rsp.y = pos.Y;
                rsp.z = pos.Z;
                rsp.dir = u.Direction;
                SendToSession(rsp);
            }
        }
        public void rsp_ObjectFieldChanged(UnityEditorObject u, string fieldName, object value, object oldValue)
        {
            var rsp = new RspObjectFieldChanged();
            rsp.Name = u.Name;
            rsp.field = fieldName;
            rsp.value = value;
            rsp.old_value = oldValue;
            SendToSession(rsp);
        }

        public void rsp_RspMouseDown()
        {
            var rsp = new RspMouseDown();
            var ray = RTG.MainCamera.ScreenPointToRay(Input.mousePosition);
            if (world)
            {
                if (world.RayCastVoxelTerrainLayer(ray, out var touch, out var layer))
                {
                    rsp.rayTouchVoxel = layer.UpwardCenterPos;
                }
            }
            if (world)
            {
                if (world.RayCastBasePlane(ray, out var distance, out var touchPlane))
                {
                    rsp.rayTouchPlane = SceneEditorBehavior.UnityWorldToBattlePosition(touchPlane);
                }
            }
            SendToSession(rsp);
        }

        #endregion

        //---------------------------------------------------------------------------
        #region _From_RTG_
        private void RTG_OnTargetEditorPropertyChanged(GameObject obj)
        {
            if (obj && obj.TryGetComponent<UnityEditorObject>(out var edit))
            {
                if (edit.ResetFromTransform())
                {
                    rsp_ObjectPositionChanged(edit);
                }
            }
        }
        private void RTG_OnTargetEditorObjectChanged(GameObject _old, GameObject _new)
        {
            if (_new != null && _new.TryGetComponent<UnityEditorObject>(out var nedit))
            {
                SceneEditorBehavior.World.Select(nedit, true);
            }
            else if (_old && _old.activeInHierarchy)
            {
                SceneEditorBehavior.World.Deselect(true);
            }
            else
            {
                SceneEditorBehavior.World.Deselect(false);
            }
        }
        #endregion
        //---------------------------------------------------------------------------

    }

}
