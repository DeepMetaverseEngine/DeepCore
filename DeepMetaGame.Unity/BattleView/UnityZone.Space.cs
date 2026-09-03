using DeepCore.Game3D.Slave.Layer;
using DeepCore.GUI.Gemo;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Unity.BattleView;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepMetaGame.Unity.BattleView
{
    //----------------------------------------------------------------------------------------------------------------------------------
    public abstract class UnityZoneSpaceTransverter
    {

        public virtual Vector3 ToUnityWorldPosition(UnityLayerObject value)
        {
            return BattleToUnityWorldPosition(value.parent.terrain, value.layerObject.Position);
        }
        public virtual Vector3 ToUnityWorldPosition(UnityLayerObject value, DeepCore.Geometry.Vector3 p)
        {
            return BattleToUnityWorldPosition(value.parent.terrain, p);
        }

        public virtual Quaternion ToUnityRotation(UnityLayerObject self)
        {
            return BattleToUnityRotation(self.layerObject.Direction);
        }
        public virtual Quaternion ToUnityRotation(UnityLayerObject self, float radians)
        {
            return BattleToUnityRotation(radians);
        }
        public virtual void UpdatePosition(UnityLayerObject self, Transform transform)
        {
            transform.SetPositionAndRotation(
                ToUnityWorldPosition(self),
                ToUnityRotation(self));
        }



        public abstract float LookAt(Transform transform, in Vector3 target);


        /// <summary>
        /// 战斗坐标转换为Unity世界坐标。
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public abstract Vector3 BattleToUnityWorldPosition(TerrainInfo terrain, in DeepCore.Geometry.Vector3 p);
        /// <summary>
        /// Unity世界坐标转换为战斗坐标。
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="Pos"></param>
        /// <returns></returns>
        public abstract DeepCore.Geometry.Vector3 UnityWorldToBattlePosition(TerrainInfo terrain, in Vector3 Pos);

        /// <summary>
        /// 战斗坐标转换为Unity坐标（不考虑场景因素）
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public abstract Vector3 BattleToUnityOffset(DeepCore.Geometry.Vector3 p);
        /// <summary>
        /// Unity坐标转换为战斗坐标（不考虑场景因素）
        /// </summary>
        /// <param name="Pos"></param>
        /// <returns></returns>
        public abstract DeepCore.Geometry.Vector3 UnityToBattleOffset(Vector3 Pos);

        public abstract Quaternion BattleToUnityRotation(float direction);
        public abstract float UnityToBattleRotation(Quaternion q);
        public abstract Vector3 BattleToUnityVoxelAnchorOffset(DeepCore.Geometry.Vector3 p, float height, VoxelAnchor anchor);
        public Vector3 BattleToUnityVoxelAnchorOffset(float height, VoxelAnchor anchor)
        {
            return BattleToUnityVoxelAnchorOffset(DeepCore.Geometry.Vector3.Zero, height, anchor);
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------------
    public class UnityZoneSpace3D : UnityZoneSpaceTransverter
    {
        public override float LookAt(Transform transform, in Vector3 target)
        {
            var src = transform.position;
            var rotate = MathVector.getDegree(target.x - src.x, target.z - src.z);
            transform.LookAt(target);
            return rotate;
        }
        public override Vector3 BattleToUnityVoxelAnchorOffset(DeepCore.Geometry.Vector3 pos, float height, VoxelAnchor anchor)
        {
            var p = pos.ToUnity();
            switch (anchor)
            {
                case VoxelAnchor.Floating:
                    p.y += height / 2;
                    break;
                case VoxelAnchor.Ceiling:
                    p.y += height;
                    break;
                case VoxelAnchor.Flooring:
                default:
                    break;
            }
            return p;
        }

        public override Vector3 BattleToUnityWorldPosition(TerrainInfo terrain, in DeepCore.Geometry.Vector3 p)
        {
            return new Vector3(
                p.X + terrain.ResX,
                p.Z,
                (terrain.TerrainH - p.Y) + terrain.ResY);
        }
        public override DeepCore.Geometry.Vector3 UnityWorldToBattlePosition(TerrainInfo terrain, in Vector3 Pos)
        {
            return new DeepCore.Geometry.Vector3(
                Pos.x - terrain.ResX,
                terrain.TerrainH - (Pos.z - terrain.ResY),
                Pos.y);
        }

        public override Vector3 BattleToUnityOffset(DeepCore.Geometry.Vector3 p)
        {
            return new Vector3(p.X, p.Z, -p.Y);
        }
        public override DeepCore.Geometry.Vector3 UnityToBattleOffset(Vector3 Pos)
        {
            return new DeepCore.Geometry.Vector3(Pos.x, -Pos.z, Pos.y);
        }

        public override Quaternion BattleToUnityRotation(float direction)
        {
            var radians = direction;
            return Quaternion.AngleAxis(radians * Mathf.Rad2Deg + 90f, Vector3.up);
        }
        public override float UnityToBattleRotation(Quaternion q)
        {
            var angles = q.eulerAngles;// (out var angle, out var axis);
            return (angles.y - 90f) / Mathf.Rad2Deg;
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------------
    public class UnityZoneSpace2D : UnityZoneSpaceTransverter
    {
        public override float LookAt(Transform transform, in Vector3 target)
        {
            var src = transform.position;
            var rotate = MathVector.getDegree(target.x - src.x, target.y - src.y);
            transform.rotation = BattleToUnityRotation(rotate);
            return rotate;
        }
        public override Vector3 BattleToUnityVoxelAnchorOffset(DeepCore.Geometry.Vector3 pos, float height, VoxelAnchor anchor)
        {
            var p = pos.ToUnity();
            switch (anchor)
            {
                case VoxelAnchor.Floating:
                    p.y -= height / 2;
                    break;
                case VoxelAnchor.Ceiling:
                    p.y -= height;
                    break;
                case VoxelAnchor.Flooring:
                default:
                    break;
            }
            return p;
        }

        public override Vector3 BattleToUnityWorldPosition(TerrainInfo terrain, in DeepCore.Geometry.Vector3 p)
        {
            return new Vector3(
                p.X + terrain.ResX,
                p.Y + terrain.ResY,
                -p.Z);
        }
        public override DeepCore.Geometry.Vector3 UnityWorldToBattlePosition(TerrainInfo terrain, in Vector3 Pos)
        {
            return new DeepCore.Geometry.Vector3(
                Pos.x - terrain.ResX,
                Pos.y - terrain.ResY,
                -Pos.z);
        }

        public override Vector3 BattleToUnityOffset(DeepCore.Geometry.Vector3 p)
        {
            return new Vector3(p.X, p.Y, -p.Z);
        }
        public override DeepCore.Geometry.Vector3 UnityToBattleOffset(Vector3 Pos)
        {
            return new DeepCore.Geometry.Vector3(Pos.x, Pos.y, -Pos.z);
        }

        public override Quaternion BattleToUnityRotation(float direction)
        {
            var radians = direction;
            return Quaternion.Euler(0, 0, radians * Mathf.Rad2Deg);
        }
        public override float UnityToBattleRotation(Quaternion q)
        {
            return (q.eulerAngles.z) / Mathf.Rad2Deg;
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------------
}
