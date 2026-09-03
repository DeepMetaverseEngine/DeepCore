using DeepCore.Unity;
using DeepCore.Unity3D.Voxel;
using DeepMetaGame.Data.Misc;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview.Preview
{
    public class AttackShapeGizmos : MonoBehaviour
    {
        private AttackShape shape;
        private float bodySize;
        private float bodyHeight;
        private float distance;
        private float fanAngle;
        private float stripWide;
        private Transform target;

        private GameObject gizmos;
        public GameObject GizmosObject { get => gizmos; }

        public AttackShapeGizmos InitGizmos(
              AttackShape shape,
              float bodySize,
              float bodyHeight,
              float distance,
              float fan_angle,
              float strip_wide,
              Transform target,
              float offset_radius)
        {
            this.shape = shape;
            this.bodySize = bodySize;
            this.bodyHeight = bodyHeight;
            this.distance = distance;
            fanAngle = fan_angle;
            stripWide = strip_wide;
            this.target = target;

            gizmos = CreateAttackShape(shape, bodySize, bodyHeight, distance, fanAngle, stripWide, target,
                transform,
                transform);
            if (gizmos != null)
            {
                gizmos.transform.SetParent(transform, false);
            }
            if (offset_radius != 0)
            {
                gizmos.transform.localPosition =
                    new Vector3(offset_radius, 0, 0);
            }
            return this;
        }
        public void LookAt(Transform targetPos)
        {
            var tp = targetPos.position;
            tp.y = transform.position.y;
            transform.LookAt(tp);
        }

        public void SetMaterial(Material material, Color color)
        {
            SetMaterial(gizmos, material, color);
        }

        void Update()
        {

        }

        public static void SetMaterial(GameObject gizmos, Material material, Color color)
        {
            if (gizmos != null)
            {
                if (gizmos.TryGetComponentInChildren<MeshRenderer>(out var srender))
                {
                    srender.material = material;
                    srender.material.color = color;
                }
                if (gizmos.TryGetComponentInChildren<LineToTarget>(out var lineTo))
                {
                    lineTo.color = color;
                }
                if (gizmos.TryGetComponentInChildren<LineRenderer>(out var line))
                {
                    line.startColor = color;
                    line.endColor = color;
                    line.material = material;
                }
            }
        }
        public static GameObject CreateAttackShape(
                  AttackShape shape,
                  float bodySize,
                  float bodyHeight,
                  float distance,
                  float fanAngle,
                  float stripWide,
                  Transform target,
                  Transform sender,
                  Transform start)
        {
            switch (shape)
            {
                case AttackShape.Single:
                    //return VoxelGizmos.CreateVoxelLineTarget(target);
                    return VoxelGizmos.CreateVoxelSingle(bodySize, bodyHeight, false);
                case AttackShape.Round:
                case AttackShape.Circle:
                    return VoxelGizmos.CreateVoxelCylinder(bodySize, bodyHeight, false);
                case AttackShape.Fan:
                    return VoxelGizmos.CreateVoxelFan(bodySize, fanAngle, bodyHeight, false);
                case AttackShape.Strip:
                    return VoxelGizmos.CreateVoxelStrip(stripWide, distance, bodyHeight, false);
                case AttackShape.StripRay:
                case AttackShape.StripRayTouchEnd:
                    return VoxelGizmos.CreateVoxelStripRay(stripWide, distance, bodyHeight, false);
                case AttackShape.RectStrip:
                    return VoxelGizmos.CreateVoxelRectStrip(stripWide, distance, bodyHeight, false);
                case AttackShape.RectStripRay:
                    return VoxelGizmos.CreateVoxelRectStripRay(stripWide, distance, bodyHeight, false);
                case AttackShape.WideStrip:
                    return VoxelGizmos.CreateVoxelRectStrip(distance, stripWide, bodyHeight, false);
                case AttackShape.LineToTarget:
                case AttackShape.LineToTargetPos:
                    return VoxelGizmos.CreateVoxelLineTarget(target, Mathf.Max(stripWide, 0.01f), bodyHeight, false);
                case AttackShape.LineToStart:
                    if (start)
                        return VoxelGizmos.CreateVoxelLineTarget(start, Mathf.Max(stripWide, 0.01f), bodyHeight, false);
                    return null;
                case AttackShape.LineToSender:
                    if (sender)
                        return VoxelGizmos.CreateVoxelLineTarget(sender, Mathf.Max(stripWide, 0.01f), bodyHeight, false);
                    return null;
            }
            return null;
        }

        // public static GameObject CreateWarningShape(LaunchEffect effect)
        // {
        //     if (effect != null && effect.Warning != null)
        //     {
        //         var warning = effect.Warning;
        //         switch (warning.WarnType)
        //         {
        //             case LaunchEffect.WarningType.WARNING_TYPE_CIRCLE:
        //                 return VoxelGizmos.CreateVoxelCylinder(warning.WarnScaleX, 0.1f, false);
        //             case LaunchEffect.WarningType.WARNING_TYPE_SECTOR:
        //                 return VoxelGizmos.CreateVoxelFan(warning.WarnScaleX, warning.WarnDegree * Mathf.Deg2Rad, 0.1f, false);
        //             case LaunchEffect.WarningType.WARNING_TYPE_SQUARE:
        //                 return VoxelGizmos.CreateVoxelRectStrip(warning.WarnScaleZ, warning.WarnScaleX, 0.1f, false);
        //             case LaunchEffect.WarningType.WARNING_TYPE_NONE:
        //                 return null;
        //         }
        //     }
        //     return null;
        // }
    }
}
