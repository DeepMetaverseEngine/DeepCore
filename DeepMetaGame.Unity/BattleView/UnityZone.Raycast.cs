using DeepCore;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.Protocol;
using DeepCore.Unity;
using DeepCore.Unity3D;
using DeepCore.Voxel.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Unity;
using DeepMetaGame.Unity.BattleView;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeepGame3D.Unity.BattleView
{
    partial class UnityZone
    {
        //----------------------------------------------------------------------------------------------------------------------------


        public UnityZoneObject SelectedObject { get => GetObject(SelectedObjectID); set { SelectedObjectID = value != null ? value.objectID : 0; } }
        public uint SelectedObjectID { get; set; }
        public DeepCore.Geometry.Vector3? SelectedVoxel { get; set; }


        //----------------------------------------------------------------------------------------------------------------------------
        public bool RayCastObject<T>(out RaycastHit hit, out DeepCore.Geometry.Vector3? target, out T obj) where T : UnityLayerObject
        {
            return RayCastObject(config.GameCamera, out hit, out target, out obj);
        }
        public bool RayCastObject<T>(Camera camera, out RaycastHit hit, out DeepCore.Geometry.Vector3? target, out T obj) where T : UnityLayerObject
        {
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            return InputComponent.Instance.RayCastObject(this, ray, out hit, out target, out obj);
        }
    
        public bool RayCastTerrain(out RaycastHit hitInfo, out DeepCore.Geometry.Vector3? target)
        {
            return RayCastTerrain(config.GameCamera, out hitInfo, out target);
        }
        public bool RayCastTerrain(Camera camera, out RaycastHit hitInfo, out DeepCore.Geometry.Vector3? target)
        {
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            return RayCastTerrain(ray, out hitInfo, out target);
        }
        public bool RayCastTerrain(Ray ray, out RaycastHit hitInfo, out DeepCore.Geometry.Vector3? target)
        {
            return InputComponent.Instance.RayCastTerrain(this, ray, out hitInfo, out target);
        }
        public bool RayCastVoxelTerrainLayer(Ray ray, out DeepCore.Geometry.Vector3? hitPoint)
        {
            if (this.layer?.Terrain3D is VoxelClientTerrain3D voxel)
            {
                var gray = new DeepCore.Geometry.Ray()
                {
                    Position = this.UnityWorldToBattlePosition(ray.origin),
                    Direction = Space.UnityToBattleOffset(ray.direction),
                };
                if (voxel.World.Terrain.RayCast(gray, out hitPoint, out var layer))
                {
                    return true;
                }
            }
            hitPoint = null;
            return false;
        }
        public bool RayCastTerrainFromCamera(Ray ray, out DeepCore.Geometry.Vector3? hitPoint)
        {
            if (this.RayCastVoxelTerrainLayer(ray, out hitPoint))
            {
                return true;
            }
            else if (this.RayCastTerrain(ray, out var hit, out hitPoint))
            {
                return true;
            }
            return false;
        }
        //         public void ActorLaunchSkill(KeyCode e, Glu.Ray ray)
        //         {
        //             var actor = Layer.Actor;
        //             if (actor != null)
        //             {
        //                 var status = actor.GetSkillStatus();
        //                 int i = ToSkillIndex(e.KeyCode);
        //                 if (i < status.Count)
        //                 {
        //                     var ss = status[i];
        //                     var target = base.RayCastVoxel(ray, out var touch);
        //                     if (target != null)
        //                     {
        //                         actor.SendUnitLaunchSkill(ss.Data.ID, touch.WorldToObject().ToGeometry());
        //                     }
        //                     else
        //                     {
        //                         actor.SendUnitLaunchSkill(ss.Data.ID);
        //                     }
        //                 }
        //             }
        //         }



        //         public virtual LayerItem CheckPickItem(Glu.Ray ray)
        //         {
        //             if (Actor != null)
        //             {
        //                 //this.Cursor = Cursors.Default;
        //                 var item = Layer.GetNearPickableItem(Actor, Actor.AGuard ? Actor.AGuard.GuardRange : Actor.BodyBlockSize);
        //                 if (item != null)
        //                 {
        //                     var item3D = GetObject(item.ObjectID);
        //                     //if (CMath.IncludeRoundPoint(item.X, item.Y, item.Info.BodySize, mouseX, mouseY))
        //                     if (item3D != null && item3D.TryRayCast(ray, out var wdpos))
        //                     {
        //                         //this.Cursor = Cursors.Hand;
        //                         return item;
        //                     }
        //                 }
        //             }
        //             return null;
        //         }
        // 
        //         public LayerZoneObject3D PickObject3D(Glu.Ray ray, out Vector3 wd_pos)
        //         {
        //             foreach (var u in objects.Values)
        //             {
        //                 if (u.IsPickable)
        //                 {
        //                     if (u.TryRayCast(ray, out wd_pos))
        //                     {
        //                         SelectedObject = (u);
        //                         return u;
        //                     }
        //                 }
        //             }
        //             wd_pos = Vector3.Zero;
        //             SelectedObject = null;
        //             return null;
        //         }


    }
}