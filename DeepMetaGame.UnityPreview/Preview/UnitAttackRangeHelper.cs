using DeepCore.Geometry;
using DeepMetaGame.Data.Misc;

namespace DeepMetaGame.Unity.Preview.Preview
{
    public class UnitAttackRangeHelper
    {
        public PreviewObject Launcher { get; }
        public AttackShape Shape { get; set; }
        public float AttackRange { get; set; }
        public float FanAngle { get; set; }
        public float Direction { get; set; }
        public float Distance { get; set; }
        public float StripWide { get; set; }
        public float Height { get; set; }
        public float OffsetRadius { get; set; }
        public Vector3 Position { get; set; }
        public UnitAttackRangeHelper(PreviewObject launcher)
        {
            Launcher = launcher;
        }

        public bool Touch(PreviewObject target)
        {
            var pos = Position;
            if (OffsetRadius != 0)
            {
                VectorHelper.MovePolar(ref pos, Direction, OffsetRadius);
            }
            switch (Shape)
            {
                case AttackShape.Round:
                    return target.Body.Intersects(new VoxelCylinder(pos, AttackRange, Height));
                case AttackShape.Circle:
                    if (AttackRange > StripWide)
                    {
                        return target.Body.Intersects(new VoxelCylinder(pos, AttackRange, Height)) && !target.Body.Intersects(new VoxelCylinder(pos, AttackRange - StripWide, Height));
                    }
                    else
                    {
                        return target.Body.Intersects(new VoxelCylinder(pos, AttackRange, Height));
                    }
                case AttackShape.Fan:
                    {
                        float dfan = FanAngle / 2f;
                        return target.Body.Intersects(new VoxelFan(pos, AttackRange, Height, Direction - dfan, Direction + dfan));
                    }
                case AttackShape.Strip:
                    {
                        return target.Body.Intersects(VoxelStripe.InitFromCenter(pos, Direction, StripWide, Distance, Height));
                    }
                case AttackShape.StripRay:
                    {
                        return target.Body.Intersects(VoxelStripe.InitFromRay(pos, Direction, StripWide, Distance, Height));
                    }
                case AttackShape.StripRayTouchEnd:
                    {
                        return target.Body.Intersects(VoxelStripe.InitFromRay(pos, Direction, StripWide, Distance, Height));
                    }
                case AttackShape.RectStrip:
                    {
                        return target.Body.Intersects(VoxelRectStripe.InitFromCenter(pos, Direction, StripWide, Distance, Height));
                    }
                case AttackShape.WideStrip:
                    {
                        return target.Body.Intersects(VoxelRectStripe.InitFromCenter(pos, Direction, Distance, StripWide, Height));
                    }
                case AttackShape.RectStripRay:
                    {
                        return target.Body.Intersects(VoxelRectStripe.InitFromRay(pos, Direction, StripWide, Distance, Height));
                    }
                case AttackShape.LineToTargetPos:
                    {
                        var targetPos = target.Position;
                        return target.Body.Intersects(VoxelStripe.InitFromRay(pos, Direction, StripWide, Vector3.Distance(targetPos, pos), Height));
                    }
                case AttackShape.Single:
                case AttackShape.LineToTarget:
                case AttackShape.LineToStart:
                case AttackShape.LineToSender:
                default:
                    //此类型作为单独命中指定目标//
                    if (target != null)
                    {
                        var dis = Vector3.Distance(Position, target.Position);
                        return dis - target.BodySize <= AttackRange;
                    }
                    break;
            }
            return PreviewObject.TouchBodyRange(Launcher, target, AttackRange);
        }

    }
}
