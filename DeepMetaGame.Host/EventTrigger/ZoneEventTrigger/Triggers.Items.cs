using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Reflection;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepMetaGame.Data.Template;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("某个道具添加到场景", "[游戏]/物品")]
    public class AddedItem : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个道具添加到场景");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            // // args = args.Clone();
            InstanceZone.ItemAddedHandler handler = new InstanceZone.ItemAddedHandler((z, i, u) =>
            {
                args.TriggingItem = i;
                args.TriggingItemTemplate = i.Info;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnItemAdded += handler,
                static (zone, handler) => zone.OnItemAdded -= handler);
        }

        [TriggingArg("触发的物品")] public InstanceItem TriggingItem(EventArguments args) => args.TriggingItem;
        [TriggingArg("触发的物品模板")] public ItemTemplate TriggingItemTemplate(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("某个单位开始检取特定道具", "[游戏]/物品")]
    public class TryUnitPickItem : ZoneAbstractTrigger
    {
        [Desc("道具")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();

        [Desc("是否可以检取")]
        public AbstractValue<bool> Condition = new BooleanValue.BooleanComparison();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当单位开始检取{0}", Item);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.TryPickItemHandler((z, u, i) =>
            {
                args.TriggingUnit = u;
                args.TriggingItem = i;
                args.TriggingItemTemplate = i.Info;
                api.TestAndDoAction(args);
                var ditem = Item.GetValueAs(api, args);
                if (ditem != null && ditem == i)
                {
                    bool ret = Condition.GetValueAs(api, args);
                    return ret;
                }
                return true;
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnTryPickItem += handler,
                static (zone, handler) => zone.OnTryPickItem -= handler);
        }

        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的物品")] public InstanceItem TriggingItem(EventArguments args) => args.TriggingItem;
        [TriggingArg("触发的物品模板")] public ItemTemplate TriggingItemTemplate(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("某个单位开始检取某个道具", "[游戏]/物品")]
    public class TryUnitPickAnyItem : ZoneAbstractTrigger
    {
        [Desc("是否可以检取")]
        public AbstractValue<bool> Condition = new BooleanValue.BooleanComparison();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当单位开始检取道具");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.TryPickItemHandler((z, u, i) =>
            {
                args.TriggingUnit = u;
                args.TriggingItem = i;
                args.TriggingItemTemplate = i.Info;
                api.TestAndDoAction(args);
                bool ret = Condition.GetValueAs(api, args);
                return ret;
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnTryPickItem += handler,
                static (zone, handler) => zone.OnTryPickItem -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的物品")] public InstanceItem TriggingItem(EventArguments args) => args.TriggingItem;
        [TriggingArg("触发的物品模板")] public ItemTemplate TriggingItemTemplate(EventArguments args) => args.TriggingItemTemplate;
    }
}
