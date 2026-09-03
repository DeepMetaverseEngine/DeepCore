using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Space;
using System;

namespace DeepMetaGame.Data.Misc
{
    public interface IZoneShape : IShape, ISerializable { }

    [MessageType(BattleConstants.ZoneShapePoint)]
    public class ZoneShapePoint : ShapePoint, IZoneShape { }

    [MessageType(BattleConstants.ZoneShapeRect)]
    public class ZoneShapeRect : ShapeRect, IZoneShape { }

    [MessageType(BattleConstants.ZoneShapeRound)]
    public class ZoneShapeRound : ShapeRound, IZoneShape { }

    [MessageType(BattleConstants.ZoneShapeEllipse)]
    public class ZoneShapeEllipse : ShapeEllipse, IZoneShape { }

    [MessageType(BattleConstants.ZoneShapeLine)]
    public class ZoneShapeLine : ShapeLine, IZoneShape { }

    [MessageType(BattleConstants.ZoneShapeStripWidth)]
    public class ZoneShapeStripWidth : ShapeStripWidth, IZoneShape { }

    [MessageType(BattleConstants.DockingOffset)]
    public class DockingOffset : ISerializable
    {
        public float Angle;
        public float Radius;
        public float Z;
        [Desc("是绑定身体，否则绑定脸")] public bool BindBodyRotation = false;
        [Desc("固定朝向(弧度)")]  public float? SolidFaceAngle;
        [Desc("固定拖尾长度")] public int? TailsCount;

        public override string ToString()
        {
            return $"A:{Angle} R:{Radius} Z:{Z}";
        }

        public static DockingOffset FromVectorOffset(DeepCore.Geometry.Vector3 v3)
        {
            var ret = new DockingOffset();
            ret.Z = v3.Z;
            ret.Angle = VectorHelper.GetDegree(v3.X,v3.Y);
            ret.Radius = VectorHelper.GetDistance(v3.X, v3.Y);
            return ret;
        }
    }

}

