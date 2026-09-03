using DeepCore.Game3D.Host.Instance;
using DeepCore.Geometry;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using Vector3 = DeepCore.Geometry.Vector3;

namespace DeepCore.Game3D.Host.Helper
{
    public struct AttackRangeHelper
    {
        private readonly InstanceUnit mLauncherUnit;
        private readonly InstanceZone mParent;
        private float mCircleInR;
        private AttackReason mAttackReason;
        private TemplateData mWeapon;

        public AttackShape Shape;
        public SkillTemplate.CastTarget ExpectTarget;
        public float AttackRange;
        public float FanAngle;
        public float Direction;
        public float Distance;
        public float StripWide;
        public float Height;

        public AttackRangeHelper(InstanceUnit launcher)
        {
            this.mLauncherUnit = launcher;
            this.mParent = launcher.Parent;
            this.mAttackReason = AttackReason.Attack;
        }

        public void GetShapeAttackable(List<InstanceUnit> list, AttackReason reason, TemplateData weapon, in Vector3 pos, InstanceUnit target = null)
        {
            this.GetShapeAttackable(list, reason, weapon, pos, pos, target);
        }
        public void GetShapeAttackable(List<InstanceUnit> list, AttackReason reason, TemplateData weapon, Vector3 pos, Vector3 prevPos, InstanceUnit target = null)
        {
            mAttackReason = reason;
            mWeapon = weapon;
            switch (Shape)
            {
                case AttackShape.Round:
                    if (pos != prevPos)
                    {
                        mParent.GetObjectsInStripe(this, 
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelStripe shape) => touch_Stripe(sender, o, in shape),
                            VoxelStripe.InitFromPoint(prevPos, pos, AttackRange, Height), 
                            list);
                    }
                    else
                    {
                        mParent.GetObjectsInCylinder(this,
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelCylinder shape) => touch_Cylinder(sender, o, in shape), 
                            new VoxelCylinder(pos, AttackRange, Height), list);
                    }
                    break;
                case AttackShape.Circle:
                    if (AttackRange > StripWide)
                    {
                        mCircleInR = AttackRange - StripWide;
                        mParent.GetObjectsInCylinder(this,
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelCylinder shape) => touch_Circle(sender, o, in shape),
                            new VoxelCylinder(pos, AttackRange, Height), list);
                    }
                    else
                    {
                        mParent.GetObjectsInCylinder(this, 
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelCylinder shape) => touch_Cylinder(sender, o, in shape), 
                            new VoxelCylinder(pos, AttackRange, Height), list);
                    }
                    break;
                case AttackShape.Fan:
                    {
                        float dfan = FanAngle / 2f;
                        mParent.GetObjectsInFan(this, 
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelFan shape) => touch_Fan(sender, o, in shape), 
                            new VoxelFan(pos, AttackRange, Height, Direction - dfan, Direction + dfan), list);
                    }
                    break;
                case AttackShape.Strip:
                    {
                        mParent.GetObjectsInStripe(this, 
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelStripe shape) => touch_Stripe(sender, o, in shape), 
                            VoxelStripe.InitFromCenter(pos, Direction, StripWide, Distance, Height), list);
                    }
                    break;
                case AttackShape.StripRay:
                    {
                        mParent.GetObjectsInStripe(this, 
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelStripe shape) => touch_Stripe(sender, o, in shape), 
                            VoxelStripe.InitFromRay(pos, Direction, StripWide, Distance, Height), list);
                    }
                    break;
                case AttackShape.StripRayTouchEnd:
                    {
                        mParent.GetObjectsInStripe(this,
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelStripe shape) => touch_Stripe(sender, o, in shape), 
                            VoxelStripe.InitFromRay(pos, Direction, StripWide, Distance, Height), list);
                        if (list.Count > 1)
                        {
                            list.Sort(new ObjectBodySorterNearest<InstanceUnit>(pos, StripWide / 2f));
                            list.RemoveRange(1, list.Count - 1);
                        }
                    }
                    break;
                case AttackShape.RectStrip:
                    {
                        mParent.GetObjectsInRectStripe(this, 
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelRectStripe shape) => touch_RectStripe(sender, o, in shape),
                            VoxelRectStripe.InitFromCenter(pos, Direction, StripWide, Distance, Height), list);
                    }
                    break;
                case AttackShape.WideStrip:
                    {
                        mParent.GetObjectsInRectStripe(this, 
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelRectStripe shape) => touch_RectStripe(sender, o, in shape),
                            VoxelRectStripe.InitFromCenter(pos, Direction, Distance, StripWide, Height), list);
                    }
                    break;
                case AttackShape.RectStripRay:
                    {
                        mParent.GetObjectsInRectStripe(this, 
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelRectStripe shape) => touch_RectStripe(sender, o, in shape),
                            VoxelRectStripe.InitFromRay(pos, Direction, StripWide, Distance, Height), list);
                    }
                    break;
                case AttackShape.LineToTargetPos:
                    {
                        mParent.GetObjectsInStripe(this,
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelStripe shape) => touch_Stripe(sender, o, in shape),
                            VoxelStripe.InitFromRay(pos, Direction, StripWide, Distance, Height), list);
                    }
                    break;
                case AttackShape.Single:
                case AttackShape.LineToTarget:
                case AttackShape.LineToStart:
                case AttackShape.LineToSender:
                    //此类型作为单独命中指定目标//
                    if (target != null) list.Add(target);
                    break;
                default:
                    {
                        mParent.GetObjectsInCylinder(this, 
                            static (AttackRangeHelper sender, InstanceZoneObject o, in VoxelCylinder shape) => touch_Cylinder(sender, o, in shape),
                            new VoxelCylinder(pos, AttackRange, Height), list);
                    }
                    break;
            }
        }
        private static bool touch_Cylinder(AttackRangeHelper sender, InstanceZoneObject o, in VoxelCylinder shape)
        {
            if (Collider.Cylinder_Touch_HitBody(sender, o, in shape))
            {
                return sender.mParent.Formula.IsAttackable(sender.mLauncherUnit, o as InstanceUnit, sender.ExpectTarget, sender.mAttackReason, sender.mWeapon);
            }
            return false;
        }
        private static bool touch_Fan(AttackRangeHelper sender, InstanceZoneObject o, in VoxelFan shape)
        {
            if (Collider.Fan_Touch_HitBody(sender, o, in shape))
            {
                return sender.mParent.Formula.IsAttackable(sender.mLauncherUnit, o as InstanceUnit, sender.ExpectTarget, sender.mAttackReason, sender.mWeapon);
            }
            return false;
        }
        private static bool touch_Stripe(AttackRangeHelper sender, InstanceZoneObject o, in VoxelStripe shape)
        {
            if (Collider.Stripe_Touch_HitBody(sender, o, in shape))
            {
                return sender.mParent.Formula.IsAttackable(sender.mLauncherUnit, o as InstanceUnit, sender.ExpectTarget, sender.mAttackReason, sender.mWeapon);
            }
            return false;
        }
        private static bool touch_RectStripe(AttackRangeHelper sender, InstanceZoneObject o, in Geometry.VoxelRectStripe shape)
        {
            if (Collider.RectStripe_Touch_HitBody(sender, o, in shape))
            {
                return sender.mParent.Formula.IsAttackable(sender.mLauncherUnit, o as InstanceUnit, sender.ExpectTarget, sender.mAttackReason, sender.mWeapon);
            }
            return false;
        }
        private static bool touch_Circle(AttackRangeHelper sender, InstanceZoneObject o, in VoxelCylinder shape)
        {
            if (Collider.Cylinder_Touch_HitBody(sender, o, in shape))
            {
                if (sender.mParent.Formula.IsAttackable(sender.mLauncherUnit, o as InstanceUnit, sender.ExpectTarget, sender.mAttackReason, sender.mWeapon))
                {
                    if (sender.mCircleInR < o.BodyHitSize)
                    {
                        return true;
                    }
                    return (!CMath.IncludeRoundRound(shape.Center.X, shape.Center.Y, sender.mCircleInR, o.X, o.Y, o.BodyHitSize));
                }
            }
            return false;
        }


        /// <summary>
        /// 从给定的List中找到能够攻击的目标
        /// </summary>
        /// <param name="rangeHelper"></param>
        /// <param name="srclist">输入</param>
        /// <param name="reason"></param>
        /// <param name="weapon"></param>
        /// <param name="pos"></param>
        /// <param name="resultlist">输出</param>
        public void GetShapeAttackable(List<InstanceUnit> srclist, AttackReason reason, TemplateData weapon, in Vector3 pos, List<InstanceUnit> resultlist)
        {
            var Shape = this.Shape;
            var BodySize = this.AttackRange;
            var Height = this.Height;
            var StripWide = this.StripWide;
            var FanAngle = this.FanAngle;
            var Direction = this.Direction;
            var Distance = this.Distance;
            mAttackReason = reason;
            mWeapon = weapon;
            switch (Shape)
            {
                case AttackShape.Round:
                    {
                        var voxelShape = new VoxelCylinder(pos, BodySize, Height);
                        GetShapeAttackable_VoxelCylinder(srclist, this, in voxelShape, resultlist);
                    }
                    break;
                case AttackShape.Circle:
                    {
                        var circleShape = new VoxelCylinder(pos, BodySize, Height);
                        if (BodySize > StripWide)
                        {
                            mCircleInR = BodySize - StripWide;
                            GetShapeAttackable_VoxelCircle(srclist, this, in circleShape, resultlist);
                        }
                        else
                        {
                            GetShapeAttackable_VoxelCylinder(srclist, this, in circleShape, resultlist);
                        }
                    }
                    break;
                case AttackShape.Fan:
                    {
                        float dfan = FanAngle * 0.5f;
                        var fanShape = new VoxelFan(pos, BodySize, Height, Direction - dfan, Direction + dfan);
                        GetShapeAttackable_VoxelFan(srclist, this, in fanShape, resultlist);
                    }
                    break;
                case AttackShape.Strip:
                    {
                        var strip = VoxelStripe.InitFromCenter(pos, Direction, StripWide, Distance, Height);
                        GetShapeAttackable_VoxelStripe(srclist, this, in strip, resultlist);
                    }
                    break;
                case AttackShape.StripRay:
                    {
                        var stripRay = VoxelStripe.InitFromRay(pos, Direction, StripWide, Distance, Height);
                        GetShapeAttackable_VoxelStripe(srclist, this, in stripRay, resultlist);
                    }
                    break;
                case AttackShape.StripRayTouchEnd:
                    {
                        var stripRayTouchEnd = VoxelStripe.InitFromRay(pos, Direction, StripWide, Distance, Height);
                        GetShapeAttackable_VoxelStripe(srclist, this, in stripRayTouchEnd, resultlist);
                    }
                    break;
                case AttackShape.RectStrip:
                    {
                        var RectStrip = VoxelRectStripe.InitFromCenter(pos, Direction, StripWide, Distance, Height);
                        GetShapeAttackable_VoxelRectStripe(srclist, this, in RectStrip, resultlist);
                    }
                    break;
                case AttackShape.RectStripRay:
                    {
                        var RectStripRay = VoxelRectStripe.InitFromRay(pos, Direction, Distance, StripWide, Height);
                        GetShapeAttackable_VoxelRectStripe(srclist, this, in RectStripRay, resultlist);
                    }
                    break;
                case AttackShape.WideStrip:
                    {
                        var WideStrip = VoxelRectStripe.InitFromCenter(pos, Direction, Distance, StripWide, Height);
                        GetShapeAttackable_VoxelRectStripe(srclist, this, in WideStrip, resultlist);
                    }
                    break;
                case AttackShape.LineToTargetPos:
                    {
                        var stripRay = VoxelStripe.InitFromRay(pos, Direction, StripWide, Distance, Height);
                        GetShapeAttackable_VoxelStripe(srclist, this, in stripRay, resultlist);
                    }
                    break;
                case AttackShape.Single:
                case AttackShape.LineToTarget:
                case AttackShape.LineToStart:
                case AttackShape.LineToSender:
                    {
                        var c = srclist.Count;
                        for (int i = 0; i < c; i++)
                        {
                            resultlist.Add(srclist[i]);
                        }
                    }
                    break;
                default:
                    {
                        var defaultShape = new VoxelCylinder(pos, BodySize, Height);
                        GetShapeAttackable_VoxelCylinder(srclist, this, in defaultShape, resultlist);
                    }
                    break;
            }

        }

        private void GetShapeAttackable_VoxelCylinder(List<InstanceUnit> srclist, AttackRangeHelper sender, in VoxelCylinder shape, List<InstanceUnit> resultlist)
        {
            var c = srclist.Count;
            InstanceUnit temp = null;
            for (int i = 0; i < c; i++)
            {
                temp = srclist[i];
                if (AttackRangeHelper.touch_Cylinder(sender, temp, in shape))
                {
                    resultlist.Add(temp);
                }
            }
        }

        private void GetShapeAttackable_VoxelFan(List<InstanceUnit> srclist, AttackRangeHelper sender, in VoxelFan shape, List<InstanceUnit> resultlist)
        {
            var c = srclist.Count;
            InstanceUnit temp = null;
            for (int i = 0; i < c; i++)
            {
                temp = srclist[i];
                if (AttackRangeHelper.touch_Fan(sender, temp, in shape))
                {
                    resultlist.Add(temp);
                }
            }
        }

        private void GetShapeAttackable_VoxelStripe(List<InstanceUnit> srclist, AttackRangeHelper sender, in VoxelStripe shape, List<InstanceUnit> resultlist)
        {
            var c = srclist.Count;
            InstanceUnit temp = null;
            for (int i = 0; i < c; i++)
            {
                temp = srclist[i];
                if (AttackRangeHelper.touch_Stripe(sender, temp, in shape))
                {
                    resultlist.Add(temp);
                }
            }
        }

        private void GetShapeAttackable_VoxelRectStripe(List<InstanceUnit> srclist, AttackRangeHelper sender, in VoxelRectStripe shape, List<InstanceUnit> resultlist)
        {
            var c = srclist.Count;
            InstanceUnit temp = null;
            for (int i = 0; i < c; i++)
            {
                temp = srclist[i];
                if (AttackRangeHelper.touch_RectStripe(sender, temp, in shape))
                {
                    resultlist.Add(temp);
                }
            }
        }

        private void GetShapeAttackable_VoxelCircle(List<InstanceUnit> srclist, AttackRangeHelper sender, in VoxelCylinder shape, List<InstanceUnit> resultlist)
        {
            var c = srclist.Count;
            InstanceUnit temp = null;
            for (int i = 0; i < c; i++)
            {
                temp = srclist[i];
                if (AttackRangeHelper.touch_Circle(sender, temp, in shape))
                {
                    resultlist.Add(temp);
                }
            }
        }


    }
}