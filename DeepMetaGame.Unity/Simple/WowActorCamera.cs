using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepGame3D.Unity.BattleView;
using UnityEngine;

namespace DeepMetaGame.Unity.BattleView.Simple
{

    public class WowActorCamera : DeepCore.Unity.Camera.WowCamera
    {
        private UnityZone zone;
        private UnityZoneActor actor;
        private DeepCore.Geometry.Vector3 targetTo;
        private DeepCore.Geometry.Vector3 camPos;
        private float camDir;

        public void BindActor(UnityZone zone)
        {
            this.zone = zone;
            if (zone.Actor != null)
            {
                this.actor = zone.Actor;
                this.Target = actor.gameObject.transform;
                this.TargetOffset = new Vector3(0, actor.layerActor.BodyHeight, 0);
                this.HorizontalAngle = actor.transform.rotation.eulerAngles.y;
            }
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (zone == null)
            {
                var mono = GameObject.FindObjectOfType<UnityZoneBeharvior>();
                if (mono != null)
                {
                    zone = mono.zone;
                    BindActor(zone);
                }
            }
            if (actor == null && zone != null && zone.Actor != null)
            {
                BindActor(zone);
            }
        }
        protected override void InternalUpdate(Transform target)
        {
            if (actor != null)
            {
                this.TargetOffset = new Vector3(0, actor.layerActor.BodyHeight, 0);
                if (actor?.layer?.CameraOffset != null)
                {
                    var offset = actor.layer.CameraOffset;
                    var pos = new DeepCore.Geometry.Vector3();
                    DeepCore.Geometry.VectorHelper.MovePolar(ref pos, offset.Angle, offset.Radius);
                    pos.Z = offset.OffsetZ;
                    this.TargetOffset += zone.Space.BattleToUnityOffset(pos);
                    this.LockHorizontal = offset.LockYaw;
                    this.LockVertical = offset.LockPitch;
                }
            }

            base.InternalUpdate(target);

            if (actor != null)
            {
                this.targetTo = actor.parent.UnityWorldToBattlePosition(actor.transform.position);
                this.camPos = actor.parent.UnityWorldToBattlePosition(transform.position);
                this.camDir = CMath.GetDegree(targetTo.X - camPos.X, targetTo.Y - camPos.Y);
                {
                    ProcessMotion(actor);
                    ProcessInventory(actor);
                    ProcessSkill(actor);
                    ProcessPickObject(actor);
                }
            }

        }

        protected override void OnTargetRotation(Transform target, Quaternion rotation)
        {
            //base.OnTargetRotation(target, rotation);
        }
        protected virtual void ProcessMotion(UnityZoneActor actor)
        {
            var zactor = actor.layerActor;
            var faceTo = zactor.Direction;
            int dx = 0, dy = 0, dz = 0;
            float angle = 0;
            float distance = 0;
            if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
            {
                angle = 0;
                distance = 1;
            }
            else
            {
                if (Input.GetKey(KeyCode.W)) { dx += 1; }
                if (Input.GetKey(KeyCode.S)) { dx -= 1; }
                if (Input.GetKey(KeyCode.A)) { dy -= 1; }
                if (Input.GetKey(KeyCode.D)) { dy += 1; }
                //                 if (Input.GetKey(KeyCode.Q)) { dz += 1; }
                //                 if (Input.GetKey(KeyCode.E)) { dz -= 1; }
                angle = CMath.GetDegree(dx, dy);
                distance = CMath.GetDistance(0, 0, dx, dy);
            }
            var moveDistance = distance;
            var moveDir = camDir + angle;
            if (actor.layerActor.AUnitMotion)
            {
                switch (actor.layerActor.AUnitMotion.Control)
                {
                    case Data.Template.UnitMotionAbility.ControlType.FaceToMouseTarget:
                        var camera = this.Camera;
                        var ray = camera.ScreenPointToRay(Input.mousePosition);
                        var rdata = actor.parent.GetRaycastData(ray, out var _map, out var _obj, out var _flag);
                        if (_map != null)
                        {
                            faceTo = DeepCore.Geometry.VectorHelper.GetDegree(actor.layerActor.Position, _map.Value);
                        }
                        break;
                    case Data.Template.UnitMotionAbility.ControlType.FaceToCameraFront:
                        if (Input.GetMouseButton(1))
                        {
                            faceTo = camDir;
                        }
                        if (distance > 0)
                        {
                            faceTo = camDir;
                        }
                        break;
                    case Data.Template.UnitMotionAbility.ControlType.FaceToMoveDirection:
                        if (moveDistance > 0)
                        {
                            faceTo = moveDir;
                        }
                        break;
                }
            }
            if (dz != 0)
            {
                zactor.SendUnit3DAxisAngle(moveDir, distance, faceTo, CMath.GetDirect(dz) * zactor.MoveSpeedSEC);
            }
            else if (distance > 0)
            {
                zactor.SendUnitAxisAngle(moveDir, distance, faceTo);
            }
            else
            {
                zactor.SendUnitAxisAngle(0, 0, faceTo);
            }
            if (zactor.Gravity != 0)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    zactor.SendJump(moveDir, moveDistance);
                }
                if (Input.GetKeyDown(KeyCode.E))
                {
                    zactor.SendFall(moveDir, moveDistance);
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    zactor.SendJump(moveDir, moveDistance);
                }
                if (Input.GetKey(KeyCode.E))
                {
                    zactor.SendFall(moveDir, moveDistance);
                }
            }
        }
        protected virtual void ProcessSkill(UnityZoneActor actor)
        {
            var selected = actor.parent.SelectedObject as UnityZoneUnit;
            for (int i = (int)KeyCode.Alpha1; i <= (int)KeyCode.Alpha9; i++)
            {
                int index = (int)(i - KeyCode.Alpha1);
                if (Input.GetKeyDown((KeyCode)i))
                {
                    if (selected != null)
                    {
                        actor.layerActor.SendUnitLaunchSkillByIndex(index, selected.layerZoneObject);
                    }
                    else
                    {
                        actor.parent.RayCastTerrain(out var hit, out var tgt);
                        actor.layerActor.SendUnitLaunchSkillByIndex(index, tgt);
                    }
                }
            }
        }
        protected virtual void ProcessInventory(UnityZoneActor actor)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                for (int i = (int)KeyCode.Alpha1; i <= (int)KeyCode.Alpha9; i++)
                {
                    int index = (int)(i - KeyCode.Alpha1);
                    if (Input.GetKeyDown((KeyCode)i))
                    {
                        actor.layerActor.SendUnitUseItemByIndex(index);
                    }
                }
            }
        }
        protected virtual void ProcessPickObject(UnityZoneActor actor)
        {
        }
    }


}