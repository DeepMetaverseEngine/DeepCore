using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Reflection;
using System.Collections.Generic;
using System.Text;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("字符串")]
    public abstract class ZoneStringValue : StringValue
    {
        sealed protected override string GetValue(DeepCore.EventTrigger.EventExecutor api, DeepCore.EventTrigger.IEventArguments args)
        {
            return this.GetValue(api as IEventTriggerAdapter, (EventArguments)args);
        }
        protected abstract string GetValue(IEventTriggerAdapter api, EventArguments args);

        //---------------------------------------------------------------------------------------------------------------------
        #region __游戏__

        [Desc("场景用户自定义属性", "[游戏]/场景")]
        public class ZoneTextAttribute : ZoneStringValue
        {
            [Desc("键值")]
            public string Key;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景键值[{0}]", Key);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetAttribute(Key) as string;
            }
        }

        //         [Desc("最后接收到的消息(从服务器)", "场景")]
        //         public class ZoneLastRecvMessage : ZoneStringValue
        //         {
        //             public override void ToFunctionText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("最后从服务器接收到的消息");
        //             }
        //             protected override string GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 if (api.ZoneAPI.LastRecvMessageR2B != null)
        //                 {
        //                     return api.ZoneAPI.LastRecvMessageR2BMessage;
        //                 }
        //                 return null;
        //             }
        //         }

        //         [Desc("最后发送到的消息(到服务器)", "场景")]
        //         public class ZoneLastSentMessage : ZoneStringValue
        //         {
        //             public override void ToFunctionText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("最后发送到服务器的消息");
        //             }
        //             protected override string GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 if (api.ZoneAPI.LastSentMessageB2R != null)
        //                 {
        //                     return api.ZoneAPI.LastSentMessageB2RMessage;
        //                 }
        //                 return null;
        //             }
        //         }

        [Desc("Flag名字", "[游戏]/Flag")]
        public class FlagName : ZoneStringValue
        {
            [Desc("Flag")]
            public AbstractValue<InstanceFlag> Flag = new FlagValue.TriggingRegion();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("Flag({0})名字", Flag);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Flag.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.Name;
                }
                return null;
            }
        }


        [Desc("单位用户自定义属性", "[游戏]/单位")]
        public class UnitTextAttribute : ZoneStringValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("键值")]
            public string Key;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})键值[{1}]", Unit, Key);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.GetAttribute(Key) as string;
                }
                return null;
            }
        }

        [Desc("单位名字", "[游戏]/单位")]
        public class UnitName : ZoneStringValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})名字", Unit);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.Name;
                }
                return null;
            }
        }
        [Desc("物品名字", "[游戏]/物品")]
        public class ItemName : ZoneStringValue
        {
            [Desc("物品")]
            public AbstractValue<InstanceItem> Item = new ItemValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("物品({0})名字", Item);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceItem item = Item.GetValueAs(api, args);
                if (item != null)
                {
                    return item.Name;
                }
                return null;
            }
        }

        [Desc("玩家UUID", "[游戏]/单位")]
        public class PlayerUUID : ZoneStringValue
        {
            [Desc("玩家单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("玩家单位({0})UUID", Unit);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args) as InstancePlayer;
                if (unit != null)
                {
                    return unit.PlayerUUID;
                }
                return null;
            }
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------------------
    }


}
