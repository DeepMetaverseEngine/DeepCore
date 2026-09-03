using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepGame3D.Unity.BattleView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace DeepMetaGame.Unity.BattleView.Simple
{

    public class WowActorCamera2D : DeepCore.Unity.Camera.FreeCamera2D
    {
        private UnityZone zone;
        private UnityZoneActor actor;
        public void BindActor(UnityZone zone)
        {
            this.zone = zone;
            if (zone.Actor != null)
            {
                this.actor = zone.Actor;
            }
        }
        public override void Update()
        {
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
            base.Update();
            if (actor != null && actor.layerActor.IsActive)
            {
                var src = this.Camera.transform.position;
                var dst = actor.transform.position;
                Camera.transform.position = new Vector3(dst.x, dst.y, src.z);
                ProcessMotion(actor);
                ProcessInventory(actor);
                ProcessSkill(actor);
                ProcessPickObject(actor);
            }
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
                if (Input.GetKey(KeyCode.W)) { dy += 1; }
                if (Input.GetKey(KeyCode.S)) { dy -= 1; }
                if (Input.GetKey(KeyCode.A)) { dx -= 1; }
                if (Input.GetKey(KeyCode.D)) { dx += 1; }
                angle = CMath.GetDegree(dx, dy);
                distance = CMath.GetDistance(0, 0, dx, dy);
            }
            var moveDistance = distance;
            var moveDir = angle;
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
                            faceTo = moveDir;
                        }
                        if (distance > 0)
                        {
                            faceTo = moveDir;
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