using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{
    [MessageType(BattleConstants.UnitAttachment)]
    [Desc("单位挂载附件")]
    [Expandable]
    public class UnitAttachment : IBaseFuncData
    {
        [Desc("召唤单位模板")]
        [TemplateIDAttribute(typeof(UnitInfo))]
        public int UnitTemplateID;
        [Desc("召唤单位等级")]
        [TemplateLevelAttribute]
        public int UnitLevel = 0;

        [Desc("绑定身体还是脸")] public bool BindBodyRotation = false;
        [Desc("相对位置角度(弧度)")] public float Angle;
        [Desc("相对位置角度(角度)")] public float Angle360 { get => CMath.RadianToAngle(Angle); set => Angle = CMath.AngleToRadian(value); }
        [Desc("相对位置半径")] public float Radius;
        [Desc("相对位置高度")] public float Z;

        [Desc("固定朝向(弧度)")] public float? SolidFaceAngle;
        [Desc("固定朝向(角度)")]
        public float? SolidFaceAngle360
        {
            get => SolidFaceAngle.HasValue ? CMath.RadianToAngle(SolidFaceAngle.Value) : null;
            set
            {
                if (value == null) SolidFaceAngle = null;
                else CMath.AngleToRadian(value.Value);
            }
        }

        public override string ToString()
        {
            return $"A:{Angle} R:{Radius} Z:{Z}";
        }

        public DockingOffset ToDockingOffset()
        {
            var ret = new DockingOffset();
            ret.BindBodyRotation = this.BindBodyRotation;
            ret.Z = this.Z;
            ret.Angle = this.Angle;
            ret.Radius = this.Radius;
            ret.SolidFaceAngle = this.SolidFaceAngle;
            return ret;
        }
    }
}
