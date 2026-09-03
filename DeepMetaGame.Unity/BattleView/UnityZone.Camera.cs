using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.GUI.Cell;
using DeepCore.GUI.Input;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Unity;
using DeepMetaGame.Unity.BattleView;
using UnityEngine;

namespace DeepGame3D.Unity.BattleView
{
    partial class UnityZone
    {
        public InputComponent InputHelper { get => InputComponent.Instance; }
        public IBattleCamera MainCamera { get; private set; }
        public UnityEffectPlay CameraEffect;
        protected virtual void InitCamera()
        {
            this.ListenZoneEvent<CameraFocusUnitEvent>(OnCameraFocusUnitEvent);
            this.ListenZoneEvent<CameraControlEvent>(OnCameraControlEvent);
            if (this.config.GameCamera)
            {
                this.MainCamera = UnityBattleFactory.Instance.CreateBattleCamera(this, this.config.GameCamera);
                if (MainCamera != null)
                {
                    var camera = this.MainCamera;
                    var camPos = layer.FindFlagWithAbility<LayerEditorRegion>(typeof(CameraPositionAbilityData));
                    if (camPos != null && GetFlag(camPos.Name) is UnityLayerRegion rg)
                    {
                        camera.MoveTo(rg.transform);
                    }
                    var camTgt = layer.FindFlagWithAbility<LayerEditorRegion>(typeof(CameraTargetAbilityData));
                    if (camTgt != null && GetFlag(camTgt.Name) is UnityLayerRegion trg)
                    {
                        camera.LookAt(trg.transform);
                    }
                    var camFocus = layer.FindFlagWithAbility<LayerEditorRegion>(typeof(CameraFocusAbilityData));
                    if (camFocus != null && GetFlag(camFocus.Name) is UnityLayerRegion tfc)
                    {
                        camera.Focus(tfc);
                    }
                    camera.ResetFromTransform();
                }
            }
            if (this.layer.Data.Abilities != null)
            {
                if (this.layer.Data.Abilities.TryGetComponentAs<SceneUIAbility>(out var ui))
                {
                    if (ui.MouseRayCastEffect != null)
                    {
                        CameraEffect = LoadEffect(ui.MouseRayCastEffect);
                    }
                }
            }
        }

        protected virtual void OnCameraFocusUnitEvent(CameraFocusUnitEvent focus)
        {
            if (MainCamera != null)
            {
                var obj = GetObject(focus.ObjectID);
                if (obj != null)
                {
                    MainCamera.Focus(obj);
                }
            }
        }
        protected virtual void OnCameraControlEvent(CameraControlEvent focus)
        {
            if (MainCamera != null)
            {
                MainCamera.Control(focus.Name);
            }
        }
        protected virtual void OnInitActorCamera(UnityZoneActor actor)
        {
            layer.TaskQueue.Enqueue(actor, (z, actor) =>
            {
                actor.layerActor.SendReady();
                if (MainCamera != null)
                {
                    MainCamera.BindActor(actor);
                }
            });
        }

        protected virtual void CleanCamera()
        {
            CameraEffect?.Dispose();
            MainCamera?.Cleanup();
        }

        private Vector3? last_mouse_down;
        protected virtual void UpdateCamera()
        {
            if (MainCamera != null)
            {
                MainCamera.UpdateCamera();
                var camera = this.MainCamera;
                if (InputHelper.TryScreenPointToRay(camera, out var ray))
                {
                    var rdata = GetRaycastData(ray, out var _map, out var _obj, out var _flag);
                    if (InputHelper.IsMouseDown(out var mouse))
                    {
                        last_mouse_down = InputHelper.MousePosition;
                        if (mouse == MouseButton.Left)
                        {
                            if (_obj is UnityZoneObject obj)
                            {
                                this.SelectedObject = obj;
                                layer.SendAction(layer.ObjectPool.AllocInit(obj, static (obj, t) =>
                                {
                                    t.HitObjectID = obj.objectID;
                                }, default(MouseSelectObjectAction)));
                            }
                            else
                            {
                                this.SelectedObject = null;
                            }
                        }
                        this.layer.SendAction(layer.ObjectPool.AllocInit((layer, mouse, rdata), static (st, t) =>
                        {
                            t.SenderObjectID = st.layer.Actor != null ? st.layer.Actor.ObjectID : 0;
                            t.Button = st.mouse;
                            t.raycast = st.rdata;
                        }, default(MouseDownAction)));
                    }
                    if (InputHelper.IsMouseUp(out mouse))
                    {
                        this.layer.SendAction(layer.ObjectPool.AllocInit((layer, mouse, rdata), static (st, t) =>
                        {
                            t.SenderObjectID = st.layer.Actor != null ? st.layer.Actor.ObjectID : 0;
                            t.Button = st.mouse;
                            t.raycast = st.rdata;
                        }, default(MouseUpAction)));
                        if (last_mouse_down.HasValue)
                        {
                            var dis = Vector3.Distance(last_mouse_down.Value, InputHelper.MousePosition);
                            if (dis <= config.MouseClickDistance)
                            {
                                this.layer.SendAction(layer.ObjectPool.AllocInit((layer, mouse, rdata), static (st, t) =>
                                {
                                    t.SenderObjectID = st.layer.Actor != null ? st.layer.Actor.ObjectID : 0;
                                    t.Button = st.mouse;
                                    t.raycast = st.rdata;
                                }, default(MouseClickAction)));
                            }
                        }
                        last_mouse_down = null;
                    }
                    if (InputHelper.IsMouseHold(out mouse))
                    {
                        this.layer?.SendAction(layer.ObjectPool.AllocInit((layer, mouse, rdata), static (st, t) =>
                        {
                            t.SenderObjectID = st.layer.Actor != null ? st.layer.Actor.ObjectID : 0;
                            t.Button = st.mouse;
                            t.raycast = st.rdata;
                        }, default(MouseMoveAction)));
                    }
                    else if (InputHelper.IsMouseMove())
                    {
                        this.layer?.SendAction(layer.ObjectPool.AllocInit((layer, mouse, rdata), static (st, t) =>
                        {
                            t.SenderObjectID = st.layer.Actor != null ? st.layer.Actor.ObjectID : 0;
                            t.Button = st.mouse;
                            t.raycast = st.rdata;
                        }, default(MouseMoveAction)));
                    }
                    if (CameraEffect != null)
                    {
                        if (rdata.IsHitTerrain)
                        {
                            CameraEffect.gameObject.SetActive(true);
                            CameraEffect.transform.position = BattleToUnityWorldPosition(rdata.HitTerrainPosition);
                        }
                        else
                        {
                            CameraEffect.gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                }
            }
            if (InputHelper.TryKeyboard(MainCamera))
            {
                if (InputHelper.IsKeyDown(out var key))
                {
                    layer.SendAction(layer.ObjectPool.AllocInit((layer, key), static (st, t) =>
                    {
                        t.SenderObjectID = st.layer.Actor != null ? st.layer.Actor.ObjectID : 0;
                        t.Key = st.key;
                        t.Modifiers = st.key;
                    }, default(KeyDownAction)));
                }
                else if (InputHelper.IsKeyUp(out key))
                {
                    layer.SendAction(layer.ObjectPool.AllocInit((layer, key), static (st, t) =>
                    {
                        t.SenderObjectID = st.layer.Actor != null ? st.layer.Actor.ObjectID : 0;
                        t.Key = st.key;
                        t.Modifiers = st.key;
                    }, default(KeyUpAction)));
                }
            }
        }
        public virtual Raycast GetRaycastData(Ray ray, out DeepCore.Geometry.Vector3? hitTerrain, out UnityZoneUnit hitUnit, out UnityZoneFlag hitFlag)
        {
            var ret = objectPool.Alloc<Raycast>();
            ret.screen = Input.mousePosition.ToGeometry();
            ret.origin = ray.direction.UnityToVoxel().ToGeometry();
            ret.normal = ray.origin.UnityToVoxel().ToGeometry();
            //射到地图
            if (this.RayCastTerrainFromCamera(ray, out hitTerrain))
            {
                ret.IsHitTerrain = true;
                ret.HitTerrainPosition = hitTerrain.Value;
                // CameraEffect.transform.position = BattleToUnityPosition(hitTerrain.Value);
            }
            if (this.RayCastObject<UnityZoneUnit>(out var _hit, out var _target, out hitUnit))
            {
                //                 ret.HitObjectPlanePosition = DeepCore.Geometry.RayCast.RayPlaneIntersection(
                //                     ret.origin,
                //                     ret.normal,
                //                     hitObject.layerObject.Position,
                //                     DeepCore.Geometry.Vector3.UnitZ);
                ret.HitObjectPosition = _target.Value;
                ret.HitObjectID = hitUnit.layerUnit.ObjectID;
                //ret.HitFlagName = (hitObject is UnityZoneFlag flag) ? flag.layerFlag.Name : null;
                //   CameraEffect.transform.position = BattleToUnityPosition(_target.Value);
            }
            if (this.RayCastObject<UnityZoneFlag>(out _hit, out _target, out hitFlag))
            {
                ret.HitFlagName = hitFlag.layerFlag.Name;
                //   CameraEffect.transform.position = BattleToUnityPosition(_target.Value);
            }
            return ret;
        }
    }

    public interface IBattleCamera
    {
        Camera camera { get; }
        Transform transform { get; }
        void MoveTo(Transform target);
        void LookAt(Transform target);
        void BindActor(UnityZoneActor actor);
        void Focus(UnityLayerObject unit);
        void ResetFromTransform();
        void Control(string name);
        void UpdateCamera();
        void Cleanup();
    }
}