using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Reflection;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Geometry;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("场景-物品")]
    public abstract class ItemValue : ZoneAbstractValue<InstanceItem>
    {
        [Desc("值 - 没有场景物品", "[游戏]/值")]
        public class NA : ItemValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("没有物品");
            }
            protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return null;
            }
        }
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : ItemValue
        {
            protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                try
                {
                    if (args.ReturnValue is InstanceItem v3) { return v3; }
                }
                catch { }
                return null;
            }
        }

        [Desc("功能 - 触发的场景物品", "[游戏]/功能")]
        public class Trigging : ItemValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的场景物品");
            }
            protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingItem;
            }
        }

        [Desc("功能 - 最后产生的场景物品", "[游戏]/功能")]
        public class LastCreated : ItemValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后产生的物品");
            }
            protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastCreatedInstanceItem;
            }
        }

        [Desc("单位最后从场景检取的场景物品", "[游戏]/功能")]
        public class LastGotInstance : ItemValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位最后从场景检取物品");
            }
            protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastCreatedInstanceItem;
            }
        }


        [Desc("检取中的场景物品", "[游戏]/功能")]
        public class LastPickingInstance : ItemValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("检取中的场景物品");
            }
            protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastPickingItem;
            }
        }

        [Desc("指定名字的场景物品", "[游戏]/功能")]
        public class NamedItem : ItemValue
        {
            [Desc("名字")]
            public AbstractValue<string> Name = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("名字为\"{0}\"的场景物品", Name);
            }
            protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetItemByName(Name.GetValueAs(api, args));
            }
        }


        [Desc("遍历迭代中的场景物品", "[游戏]/循环迭代")]
        public class PickingIteratingItem : ItemValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("遍历迭代中的场景物品");
            }
            protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.IteratingObject as InstanceItem;
            }
        }


        [Desc("场景 - 随机物品", "[游戏]/场景")]
        public class RandomItem : ItemValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("随机物品");
            }
            protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.SelectRandomItem<InstanceItem>((unit) =>
                {
                    return true;
                });
            }
        }
        [Desc("场景 - 最近的物品", "[游戏]/场景")]
        public class NearItem : ItemValue
        {
            [Desc("参照位置")]
            public AbstractValue<Vector3?> SrcPosition = new PositionValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("离{0}最近的物品", SrcPosition);
            }
            protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var src = SrcPosition.GetValueAs(api, args);
                if (src == null) return null;
                return api.ZoneAPI.SelectNearItem<InstanceItem>(src.Value, static (unit) =>
                {
                    return true;
                });
            }
        }

    }
// 
//     [Desc("基础-单位物品")]
//     public abstract class ItemArrayValue : ZoneAbstractArrayValue<InstanceItem>
//     {
//         [Desc("物品数组", "值")] public class VALUE : ArrayValue<AbstractValue<InstanceItem>, InstanceItem> { }
//         [Desc("物品数组索引", "数组")] public class INDEX : ArrayIndexValue<InstanceItem> { }
//         [Desc("物品数组随机", "数组")] public class RANDOM : ArrayRandomValue<InstanceItem> { }
//         [Desc("迭代中的物品", "数组")] public class ITERATOR : ArrayIteratingValue<InstanceItem> { }
//     }
}
