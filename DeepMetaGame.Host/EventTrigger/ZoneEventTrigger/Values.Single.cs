using DeepCore.Formula;
using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Reflection;
using System;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Geometry;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("小数点型")]
    public abstract class ZoneRealValue : RealValue
    {
        protected override double GetValue(DeepCore.EventTrigger.EventExecutor api, DeepCore.EventTrigger.IEventArguments args)
        {
            return this.GetValue(api as IEventTriggerAdapter, (EventArguments)args);
        }
        protected abstract double GetValue(IEventTriggerAdapter api, EventArguments args);
        //------------------------------------------------------------------------------------------------------------
        #region __Attributes__

        [Desc("场景用户自定义属性", "[游戏]/场景")]
        public class ZoneRealAttribute : ZoneRealValue
        {
            [Desc("键值")]
            public string Key;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景键值[{0}]", Key);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                try
                {
                    return Parser.ParseFloat(api.ZoneAPI.GetAttribute(Key) as string);
                }
                catch 
                {
                }
                return 0;
            }
        }

        [Desc("单位用户自定义属性", "[游戏]/单位")]
        public class UnitRealAttribute : ZoneRealValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("键值")]
            public string Key;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})键值[{1}]", Unit, Key);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    try
                    {
                        return Parser.ParseFloat(unit.GetAttribute(Key) as string);
                    }
                    catch { }
                    return 0;
                }
                return 0;
            }
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------

        #region __TowUnit__

        [Desc("获得两个单位的夹角(弧度)", "[游戏]/2单位")]
        public class GetTowUnitAngle2D : ZoneRealValue
        {
            [Desc("单位A")]
            public AbstractValue<InstanceUnit> UnitA = new UnitValue.Trigging();
            [Desc("单位B")]
            public AbstractValue<InstanceUnit> UnitB = new UnitValue.Trigging();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("GetDirect({0}, {1})", UnitA, UnitB);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit a = UnitA.GetValueAs(api, args);
                InstanceUnit b = UnitB.GetValueAs(api, args);
                if (a != null && b != null)
                {
                    var p1 = a.Position;
                    var p2 = b.Position;
                    return CMath.GetDegree(p1.X, p1.Y, p2.X, p2.Y);
                }
                return 0;
            }
        }

        [Desc("获得两个单位的夹角(角度)", "[游戏]/2单位")]
        public class GetTowUnitAngle2DDegree : ZoneRealValue
        {
            [Desc("单位A")]
            public AbstractValue<InstanceUnit> UnitA = new UnitValue.Trigging();
            [Desc("单位B")]
            public AbstractValue<InstanceUnit> UnitB = new UnitValue.Trigging();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("GetDirectDegree({0}, {1})", UnitA, UnitB);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit a = UnitA.GetValueAs(api, args);
                InstanceUnit b = UnitB.GetValueAs(api, args);
                if (a != null && b != null)
                {
                    var p1 = a.Position;
                    var p2 = b.Position;
                    return CMath.GetDegree(p1.X, p1.Y, p2.X, p2.Y) * 180.0 / Math.PI;
                }
                return 0;
            }
        }

        [Desc("获得两个单位的距离", "[游戏]/2单位")]
        public class GetTowUnitDistance2D : ZoneRealValue
        {
            [Desc("单位A")]
            public AbstractValue<InstanceUnit> UnitA = new UnitValue.Trigging();
            [Desc("单位B")]
            public AbstractValue<InstanceUnit> UnitB = new UnitValue.Trigging();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("GetDistance({0}, {1})", UnitA, UnitB);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit a = UnitA.GetValueAs(api, args);
                InstanceUnit b = UnitB.GetValueAs(api, args);
                if (a != null && b != null)
                {
                    var p1 = a.Position;
                    var p2 = b.Position;
                    return CMath.GetDistance(p1.X, p1.Y, p2.X, p2.Y);
                }
                return 0;
            }
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------

        #region __Position__

        [Desc("获得两个点的夹角(弧度)", "[游戏]/2点")]
        public class GetTowPosAngle : ZoneRealValue
        {
            [Desc("点1")]
            public AbstractValue<Vector3?> P1 = new PositionValue.VALUE();
            [Desc("点2")]
            public AbstractValue<Vector3?> P2 = new PositionValue.VALUE();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("GetDirect({0}, {1})", P1, P2);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var a = P1.GetValueAs(api, args);
                var b = P2.GetValueAs(api, args);
                if (a != null && b != null)
                {
                    return CMath.GetDegree(a.Value.X, a.Value.Y, b.Value.X, b.Value.Y);
                }
                return 0;
            }
        }

        [Desc("获得两个点的夹角(角度)", "[游戏]/2点")]
        public class GetTowPosAngleDegree : ZoneRealValue
        {
            [Desc("点1")]
            public AbstractValue<Vector3?> P1 = new PositionValue.VALUE();
            [Desc("点2")]
            public AbstractValue<Vector3?> P2 = new PositionValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("GetDirectDegree({0}, {1})", P1, P2);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var a = P1.GetValueAs(api, args);
                var b = P2.GetValueAs(api, args);
                if (a != null && b != null)
                {
                    return CMath.GetDegree(a.Value.X, a.Value.Y, b.Value.X, b.Value.Y) * 180.0 / Math.PI;
                }
                return 0;
            }
        }

        [Desc("获得两个点的距离", "[游戏]/2点")]
        public class GetTowPosDistance : ZoneRealValue
        {
            [Desc("点1")]
            public AbstractValue<Vector3?> P1 = new PositionValue.VALUE();
            [Desc("点2")]
            public AbstractValue<Vector3?> P2 = new PositionValue.VALUE();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("GetDistance({0}, {1})", P1, P2);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var a = P1.GetValueAs(api, args);
                var b = P2.GetValueAs(api, args);
                if (a != null && b != null)
                {
                    return CMath.GetDistance(a.Value.X, a.Value.Y, b.Value.X, b.Value.Y);
                }
                return 0;
            }
        }

        #endregion

        //------------------------------------------------------------------------------------------------------------

// 
//         [Desc("游戏时间加速", "[游戏]")]
//         public class GetGameTimeScaleValue : ZoneRealValue
//         {
//             protected override double GetValue(IEditorValueAdapter api, EventArguments args)
//             {
//                 return api.ZoneAPI.TimeScale;
//             }
//         }

        //------------------------------------------------------------------------------------------------------------
    }


}
