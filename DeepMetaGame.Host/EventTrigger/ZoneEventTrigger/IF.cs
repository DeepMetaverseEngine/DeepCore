
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Formula;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using static DeepCore.Game3D.Host.Instance.InstanceZone;

namespace DeepCore.Game3D.Host.EventTrigger.ZoneEventTrigger
{


    [Desc("IF Position 比较", "[游戏]")]
    public class IFPositionAction : IFAction<Vector3?>
    {
        [Desc("Value1")]
        public AbstractValue<Vector3?> A = new PositionValue.VALUE();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("Value2")]
        public AbstractValue<Vector3?> B = new PositionValue.VALUE();
        protected override void GetCompareText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0} {1} {2}", A, Op, B);
        }
        protected override bool Compare(EventExecutor api, IEventArguments args)
        {
            var a = A.GetValueAs(api, args);
            var b = B.GetValueAs(api, args);
            return FormulaHelper.Compare(a, Op, b);
        }
    }


    [Desc("IF Unit 比较", "[游戏]")]
    public class IFUnitAction : IFAction<InstanceUnit>
    {
        [Desc("Value1")]
        public AbstractValue<InstanceUnit> A = new UnitValue.Trigging();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("Value2")]
        public AbstractValue<InstanceUnit> B = new UnitValue.TriggingTarget();
        protected override void GetCompareText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0} {1} {2}", A, Op, B);
        }
        protected override bool Compare(EventExecutor api, IEventArguments args)
        {
            var a = A.GetValueAs(api, args);
            var b = B.GetValueAs(api, args);
            return FormulaHelper.Compare(a, Op, b);
        }
    }

    [Desc("IF Item 比较", "[游戏]")]
    public class IFItemAction : IFAction<InstanceItem>
    {
        [Desc("Value1")]
        public AbstractValue<InstanceItem> A = new ItemValue.Trigging();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("Value2")]
        public AbstractValue<InstanceItem> B = new ItemValue.LastCreated();
        protected override void GetCompareText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0} {1} {2}", A, Op, B);
        }
        protected override bool Compare(EventExecutor api, IEventArguments args)
        {
            var a = A.GetValueAs(api, args);
            var b = B.GetValueAs(api, args);
            return FormulaHelper.Compare(a, Op, b);
        }
    }


    [Desc("IF Flag 比较", "[游戏]")]
    public class IFFlagAction : IFAction<InstanceFlag>
    {
        [Desc("Value1")]
        public AbstractValue<InstanceFlag> A = new FlagValue.EditorRegion();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("Value2")]
        public AbstractValue<InstanceFlag> B = new FlagValue.EditorRegion();
        protected override void GetCompareText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0} {1} {2}", A, Op, B);
        }
        protected override bool Compare(EventExecutor api, IEventArguments args)
        {
            var a = A.GetValueAs(api, args);
            var b = B.GetValueAs(api, args);
            return FormulaHelper.Compare(a, Op, b);
        }
    }


}
