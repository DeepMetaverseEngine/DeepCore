using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("触发中的玩家添加AOI道具", "[游戏]/位面")]
    public class AddItemActionAOI : ZoneAbstractAction
    {
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        [Desc("道具模板ID")]
        [TemplateIDAttribute(typeof(ItemTemplate))]
        public int ItemTemplateID = 0;
        [Desc("道具阵营")]
        public AbstractValue<double> Force = new IntegerValue.VALUE(0);
        [Desc("朝向")]
        public float Direction;
        [Desc("位置")]
        public AbstractValue<Vector3?> Position = new PositionValue.VALUE();
        [Desc("用户定义名字")]
        public string ItemName;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("玩家({0})添加AOI道具({1})到({2});", Player, ItemTemplateID, Position);
        }

         protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var player = Player.GetValueAs(api, args) as InstancePlayer;
            var pos = Position.GetValueAs(api, args);
            var temp = api.ZoneAPI.Templates.GetItem(ItemTemplateID);
            if (temp != null && pos.HasValue && player != null && player.AoiStatus != null)
            {
                var item = api.ZoneAPI.AddItem(new Data.AddItemParam()
                {
                    template = temp,
                    name = ItemName,
                    force = (byte)Force.GetValueAs(api, args),
                    pos = pos.Value,
                    direction = Direction,
                });
                item.AoiStatus = (player.AoiStatus);
            }
            return null;
        }
    }

    [Desc("触发中的玩家添加AOI单位", "[游戏]/位面")]
    public class AddUnitActionAOI : ZoneAbstractAction
    {
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();

        [Desc("单位模板ID")]
        [TemplateIDAttribute(typeof(UnitInfo))]
        public int UnitTemplateID = 0;

        [Desc("单位等级")]
        [TemplateLevelAttribute]
        public int UnitLevel = 0;

        [Desc("单位阵营")]
        public AbstractValue<double> Force = new IntegerValue.VALUE(0);

        [Desc("用户定义名字(编辑器名字)")]
        public string UnitName;

        [Desc("位置")]
        public AbstractValue<Vector3?> Position = new PositionValue.VALUE();

        [Desc("朝向")]
        public float Direction;

        [Desc("开始寻路")]
        public AbstractValue<InstanceFlag> StartPoint;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("玩家({0})添加单位({1})到({2});", Player, UnitTemplateID, Position);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var player = Player.GetValueAs(api, args) as InstancePlayer;
            var pos = Position.GetValueAs(api, args);
            var temp = api.ZoneAPI.Templates.GetUnit(UnitTemplateID);
            if (temp != null && pos.HasValue && player != null && player.AoiStatus != null)
            {
                var unit = api.ZoneAPI.AddUnit(new Data.AddUnitParam()
                {
                    template = temp,
                    name = UnitName,
                    force = (byte)Force.GetValueAs(api, args),
                    level = UnitLevel,
                    pos = pos.Value,
                    direction = Direction
                });
                unit.AoiStatus = (player.AoiStatus);
                if (StartPoint != null)
                {
                    var flag = StartPoint.GetValueAs(api, args);
                    if (flag != null)
                    {
                        unit.StartAttackTo(flag as ZoneWayPoint);
                    }
                }
            }
            return null;
        }
    }


    [Desc("玩家进入位面", "[游戏]/位面")]
    public class PlayerEnterAOI : ZoneAbstractAction
    {
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})进入位面;", Player);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var player = Player.GetValueAs(api, args) as InstancePlayer;
            if (player != null)
            {
                player.SetAoiStatus(api.ZoneAPI.CreateAOI(player));
            }
            return null;
        }
    }

    [Desc("玩家离开位面", "[游戏]/位面")]
    public class PlayerLeaveAOI : ZoneAbstractAction
    {
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})离开位面;", Player);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var player = Player.GetValueAs(api, args) as InstancePlayer;
            if (player != null && player.AoiStatus != null)
            {
                player.AoiStatus = (null);
            }
            return null;
        }
    }


    [Desc("单位是否在位面", "[游戏]/位面")]
    public class UnitInAOIStatus : ZoneBooleanValue
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({0})是否在位面", Player);
        }

        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var player = Player.GetValueAs(api, args);
            if (player != null)
            {
                return player.AoiStatus != null;
            }
            return false;
        }
    }

    [Desc("单位位面是否一致", "[游戏]/位面")]
    public class UnitEqualAOIStatus : ZoneBooleanValue
    {
        [Desc("单位")]
        [ListDescAttribute(typeof(AbstractValue<InstanceUnit>))]
        public List<AbstractValue<InstanceUnit>> Units = new List<AbstractValue<InstanceUnit>>();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位位面是否一致");
        }

        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ObjectAoiStatus aoi = null;
            for (int index = 0; index < Units.Count; index++)
            {
                var unitValue = Units[index];
                var u = unitValue.GetValueAs(api, args);
                if (index != 0 && u.AoiStatus != aoi)
                {
                    return false;
                }
                aoi = u.AoiStatus;
            }
            return true;
        }
    }

    [Desc("单位位面宿主", "[游戏]/位面")]
    public class UnitAOIOwnerUnit : UnitValue
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})位面宿主", Unit);
        }

        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null && unit.AoiStatus != null)
            {
                return (unit.AoiStatus).Owner;
            }
            return null;
        }
    }

    [Desc("位面内单位数", "[游戏]/位面")]
    public class GetAOIUnitCount : ZoneIntegerValue
    {
        [Desc("宿主")]
        public AbstractValue<InstanceUnit> Owner = new UnitValue.Trigging();

        [Desc("指定Force")]
        public AbstractValue<double> Force = new ZoneIntegerValue.UnitForce();

        [Desc("指定TemplateID")]
        public AbstractValue<double> TemplateID = new ZoneIntegerValue.UnitTemplateID();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})位面内单位数", Owner);
        }

        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Owner.GetValueAs(api, args);
            if (unit != null && unit.AoiStatus != null)
            {
                return (unit.AoiStatus).GetUnitCountByForceTemplateID(
                    (int)Force.GetValueAs(api, args),
                    (int)TemplateID.GetValueAs(api, args));
            }
            return 0;
        }
    }

    [Desc("位面内单位数ByForce", "[游戏]/位面")]
    public class GetAOIUnitCountByForce : ZoneIntegerValue
    {
        [Desc("宿主")]
        public AbstractValue<InstanceUnit> Owner = new UnitValue.Trigging();

        [Desc("指定Force")]
        public AbstractValue<double> Force = new ZoneIntegerValue.UnitForce();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})位面内(Force={1})单位数", Owner, Force);
        }

        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Owner.GetValueAs(api, args);
            if (unit != null && unit.AoiStatus != null)
            {
                return (unit.AoiStatus).GetUnitCountByForce(
                    (int)Force.GetValueAs(api, args));
            }
            return 0;
        }
    }

    [Desc("位面内单位数ByTemplateID", "[游戏]/位面")]
    public class GetAOIUnitCountByTemplateID : ZoneIntegerValue
    {
        [Desc("宿主")]
        public AbstractValue<InstanceUnit> Owner = new UnitValue.Trigging();

        [Desc("指定TemplateID")]
        public AbstractValue<double> TemplateID = new ZoneIntegerValue.UnitTemplateID();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})位面内(模板={1})单位数", Owner, TemplateID);
        }

        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Owner.GetValueAs(api, args);
            if (unit != null && unit.AoiStatus != null)
            {
                return (unit.AoiStatus).GetUnitCountByTemplateID(
                    (int)TemplateID.GetValueAs(api, args));
            }
            return 0;
        }
    }

    [Desc("找到单位所属位面的单位ByTemplateID", "[游戏]/位面")]
    public class GetAOIUnitByTemplateID : UnitValue
    {
        [Desc("宿主")]
        public AbstractValue<InstanceUnit> Owner = new UnitValue.Trigging();

        [Desc("指定TemplateID")]
        public AbstractValue<double> TemplateID = new ZoneIntegerValue.UnitTemplateID();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})位面内(TemplateID={1})的单位", Owner, TemplateID);
        }

        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Owner.GetValueAs(api, args);
            if (unit != null && unit.AoiStatus != null)
            {
                return (unit.AoiStatus).FindUnitByTemplateID((int)TemplateID.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("找到单位所属位面的单位ByName", "[游戏]/位面")]
    public class GetAOIUnitByName : UnitValue
    {
        [Desc("宿主")]
        public AbstractValue<InstanceUnit> Owner = new UnitValue.Trigging();

        [Desc("指定Name")]
        public AbstractValue<string> Name = new StringValue.VALUE("");

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})位面内(Name={1})的单位", Owner, Name);
        }

        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Owner.GetValueAs(api, args);
            if (unit != null && unit.AoiStatus != null)
            {
                return (unit.AoiStatus).FindUnitByName(Name.GetValueAs(api, args));
            }
            return null;
        }
    }


}
