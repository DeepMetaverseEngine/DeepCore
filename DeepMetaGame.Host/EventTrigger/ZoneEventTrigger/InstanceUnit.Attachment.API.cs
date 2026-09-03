using DeepCore.EventTrigger.Data;
using DeepCore.EventTrigger;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.Reflection;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Net.Mail;

namespace DeepCore.Game3D.Host.EventTrigger.ZoneEventTrigger
{


    [Desc("单位挂载", "[游戏]/单位")]
    public class UnitDockingAction : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("挂载到爸爸")]
        public AbstractValue<InstanceUnit> Docking = new UnitValue.NA();

        [Desc("角度")]
        public AbstractValue<double> Angle360 = new RealValue.VALUE(90);
        [Desc("距离")]
        public AbstractValue<double> Distance = new RealValue.VALUE(1);
        [Desc("高度")]
        public AbstractValue<double> OffsetZ = new RealValue.VALUE(0);
        [Desc("是绑定身体，否则绑定脸")]
        public AbstractValue<bool> BindBodyRotation = new BooleanValue.VALUE(false);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({0})挂载到({1});", Unit, Docking);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var newPos = Docking.GetValueAs(api, args);
                if (newPos != null)
                {
                    newPos.AddAttachment(unit, new DeepMetaGame.Data.Misc.DockingOffset()
                    {
                        Angle = CMath.AngleToRadian((float)Angle360.GetValueAs(api, args)),
                        Radius = (float)Distance.GetValueAs(api, args),
                        Z = (float)OffsetZ.GetValueAs(api, args),
                        BindBodyRotation = BindBodyRotation.GetValueAs(api, args),
                        SolidFaceAngle = null
                    });
                }
            }
            return unit;
        }
    }

    [Desc("单位固定角度挂载", "[游戏]/单位")]
    public class UnitSolidDockingAction : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("挂载到爸爸")]
        public AbstractValue<InstanceUnit> Docking = new UnitValue.NA();

        [Desc("角度")]
        public AbstractValue<double> Angle360 = new RealValue.VALUE(90);
        [Desc("距离")]
        public AbstractValue<double> Distance = new RealValue.VALUE(1);
        [Desc("高度")]
        public AbstractValue<double> OffsetZ = new RealValue.VALUE(0);
        [Desc("是绑定身体，否则绑定脸")]
        public AbstractValue<bool> BindBodyRotation = new BooleanValue.VALUE(false);
        [Desc("固定朝向(弧度)")]
        public AbstractValue<double> SolidFaceAngle = new RealValue.VALUE(0);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({0})挂载到({1});", Unit, Docking);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var newPos = Docking.GetValueAs(api, args);
                if (newPos != null)
                {
                    newPos.AddAttachment(unit, new DeepMetaGame.Data.Misc.DockingOffset()
                    {
                        Angle = CMath.AngleToRadian((float)Angle360.GetValueAs(api, args)),
                        Radius = (float)Distance.GetValueAs(api, args),
                        Z = (float)OffsetZ.GetValueAs(api, args),
                        BindBodyRotation = BindBodyRotation.GetValueAs(api, args),
                        SolidFaceAngle =SolidFaceAngle.GetValueAs<float>(api, args),
                    });
                }
            }
            return unit;
        }
    }

    [Desc("单位当前挂载到单位", "[游戏]/功能")]
    public class UnitDockingParent : UnitValue
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.挂载单位", Unit);
        }
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return Unit.GetValueAs(api, args)?.CurrentDockingParent as InstanceUnit;
        }
    }

    [Desc("单位当前挂载位置", "[游戏]/功能")]
    public class UnitDockingPosition : PositionValue
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.挂载位置", Unit);
        }
        protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var dock = unit.CurrentDockingParent;
                if (dock != null)
                {
                    return dock.Position;
                }
                return unit.Position;
            }
            return Vector3.Zero;
        }
    }

    [Desc("单位当前正在挂载", "[游戏]/功能")]
    public class IsUnitDocking : ZoneBooleanValue
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.正在挂载", Unit);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var dock = unit.CurrentDockingParent;
                return (dock != null);
            }
            return false;
        }
    }
}
