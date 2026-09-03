using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("收到场景聊天", "[游戏]/文本消息")]
    public class ZoneTextMessageTrigger : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("收到场景聊天");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.OnProcessZoneAction += (z, e) =>
            {
                if (e is TextMessage chat)
                {
                    args.TriggingStringValue = chat.Message?.Trim();
                    api.TestAndDoAction(args);
                }
            };
        }
        [TriggingArg("场景聊天文本")] public string Message(EventArguments args) => args.TriggingStringValue;
    }

    [Desc("场景聊天文本", "[游戏]/文本消息")]
    public class ZoneTextMessageValue : ZoneStringValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("(场景聊天文本)");
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.TriggingStringValue;
        }
    }

    [Desc("发送系统聊天消息", "[游戏]/文本消息")]
    public class SendChatMessage : ZoneAbstractAction
    {
        [Desc("消息")]
        public AbstractValue<string> Message = new StringValue.VALUE("你好世界!");
        [Desc("消息类型")]
        public ChatMessageType SendTo = ChatMessageType.SystemToAll;

        [Desc("预计发送的阵营", "可选")]
        public AbstractValue<double> ToForce = new IntegerValue.VALUE(0);
        [Desc("预计发送的单位", "可选")]
        public AbstractValue<InstanceUnit> ToPlayer = new UnitValue.NA();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("发送聊天消息: \"{0}\";", Message);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            string msg = Message.GetValueAs(api, args);
            if (!string.IsNullOrEmpty(msg))
            {
                ChatNotify chat = api.ZoneAPI.ObjectPool.Alloc<ChatNotify>().Init(SendTo);
                chat.Message = msg;
                chat.To = SendTo;
                switch (SendTo)
                {
                    case ChatMessageType.SystemToPlayer:
                        InstancePlayer target = ToPlayer.GetValueAs(api, args) as InstancePlayer;
                        if (target != null)
                        {
                            chat.ToPlayerUUID = target.PlayerUUID;
                        }
                        break;
                    case ChatMessageType.SystemToForce:
                        chat.Force = (byte)ToForce.GetValueAs(api, args);
                        break;
                    case ChatMessageType.SystemToAll:
                        break;
                }
                api.ZoneAPI.SendEvent(chat);
            }
            return null;
        }
    }

}
