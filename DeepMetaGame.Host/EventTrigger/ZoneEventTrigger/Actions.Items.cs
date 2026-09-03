using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("添加物品", "[游戏]/物品")]
    public class AddItemAction : ZoneAbstractAction<InstanceItem>
    {
        [Desc("物品模板ID")]
        [TemplateID(typeof(ItemTemplate))]
        public int ItemTemplateID = 0;
        [Desc("物品模板ID组（随机一个）")]
        [TemplateGroup(typeof(ItemTemplate))]
        public string ItemGroupPath;

        [Desc("名字")]
        public string Name = "";
        [Desc("阵营")]
        public AbstractValue<double> TargetForce = new IntegerValue.VALUE(0);
        [Desc("位置")]
        public AbstractValue<Vector3?> Position = new PositionValue.VALUE();
        [Desc("方向")]
        public AbstractValue<double> Direction = new RealValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("添加物品({0})到({1});", ItemTemplateID, Position);
        }
        override protected InstanceItem Run(IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Position.GetValueAs(api, args);
            var temp = api.Templates.GetItem(ItemTemplateID);
            if (!string.IsNullOrEmpty(ItemGroupPath))
            {
                using (var array = api.ZoneAPI.ObjectPool.AllocList<ItemTemplate>())
                {
                    api.ZoneAPI.Templates.GetAllItemsByPath(ItemGroupPath, array);
                    temp = api.ZoneAPI.RandomN.GetRandomInCollection(array);
                }
            }
            if (pos != null && temp != null)
            {
                byte force = (byte)TargetForce.GetValueAs(api, args);
                float direction = (float)Direction.GetValueAs(api, args);
                //temp, Name, pos.x, pos.y, direction, force, null
                return api.ZoneAPI.AddItem(new Data.AddItemParam()
                {
                    template = temp,
                    pos = pos.Value,
                    direction = direction,
                    name = Name,
                    force = force,
                });
            }
            return null;
        }
    }


    [Desc("直接移除物品", "[游戏]/物品")]
    public class RemoveItemAction : ZoneAbstractAction
    {
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NamedItem();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("直接移除({0});", Item);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var item = Item.GetValueAs(api, args);
            if (item != null)
            {
                api.ZoneAPI.RemoveObject(item);
            }
            return null;
        }
    }

    [Desc("单位直接捡取物品", "[游戏]/物品")]
    public class DirectPickItemAction : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NamedItem();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})直接捡取({1});", Unit, Item);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            var item = Item.GetValueAs(api, args);
            if (unit != null && item != null)
            {
                item.DirectPickItem(unit);
            }
            return null;
        }
    }

    #region __物品组__

    [Desc("遍历坐标半径范围内所有物品", "[游戏]/遍历物品组")]
    public class EveryRangedItemDoAction : ZoneAbstractAction
    {
        [Desc("坐标")]
        public AbstractValue<Vector3?> Position = new PositionValue.VALUE();
        [Desc("半径")]
        public float Range = 10f;
        [Desc("高度")]
        public float Height = 1f;
        [Desc("动作")]
        public AbstractAction Action = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendForEach(
             sw1 => sw.AppendFormat("坐标({0})半径({1})范围内的物品;", Position, Range),
             sw2 => sw.AppendLine(Action));
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Position.GetValueAs(api, args);
            if (pos != null)
            {
                api.ZoneAPI.ForEachObjectsInCylinder<InstanceItem>(new Geometry.VoxelCylinder(pos.Value, Range, Height), (u) =>
                {
                    args.IteratingObject = (u);
                    Action.Invoke(api, args);
                    args.IteratingObject = (null);
                    return false;
                });
            }
            return null;
        }
        [TriggingArg("迭代中的物品")] public InstanceItem Iterating(EventArguments args) => args.IteratingObject as InstanceItem;
    }

    [Desc("遍历区域内所有物品", "[游戏]/遍历物品组")]
    public class EveryItemInRegionDoAction : ZoneAbstractAction
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

        [Desc("动作")]
        public AbstractAction Action = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendForEach(
             sw1 => sw.AppendFormat("区域({0})内的物品;", Region),
             sw2 => sw.AppendLine(Action));
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var region = Region.GetValueAs<ZoneRegion>(api, args);
            if (region != null)
            {
                region.ForEachObjectsInRegion<InstanceItem>((u) =>
                {
                    args.IteratingObject = (u);
                    Action.Invoke(api, args);
                    args.IteratingObject = (null);
                    return false;
                });
            }

            return null;
        }
        [TriggingArg("迭代中的物品")] public InstanceItem Iterating(EventArguments args) => args.IteratingObject as InstanceItem;
    }



    #endregion
}
