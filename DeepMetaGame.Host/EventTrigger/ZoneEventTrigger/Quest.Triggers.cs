using DeepCore.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;


namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("某个单位任务已接受", "[游戏]/任务")]
    public class OnQuestAccepted : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("某个单位任务已接受");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.QuestAcceptedHandler handler = new InstanceZone.QuestAcceptedHandler((u, q) =>
            {
                args.TriggingUnit = u;
                args.TriggingQuestID = q;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnQuestAccepted += handler,
                static (zone, handler) => zone.OnQuestAccepted -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("QuestID")] public string Card(EventArguments args) => args.TriggingQuestID;
    }
    [Desc("某个单位任务已完成", "[游戏]/任务")]
    public class OnQuestCommitted : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("某个单位任务已完成");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.QuestCommittedHandler handler = new InstanceZone.QuestCommittedHandler((u, q) =>
            {
                args.TriggingUnit = u;
                args.TriggingQuestID = q;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnQuestCommitted += handler,
                static (zone, handler) => zone.OnQuestCommitted -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("QuestID")] public string Card(EventArguments args) => args.TriggingQuestID;
    }
    [Desc("某个单位任务已放弃", "[游戏]/任务")]
    public class OnQuestDropped : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("某个单位任务已放弃");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.QuestDroppedHandler handler = new InstanceZone.QuestDroppedHandler((u, q) =>
            {
                args.TriggingUnit = u;
                args.TriggingQuestID = q;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnQuestDropped += handler,
                static (zone, handler) => zone.OnQuestDropped -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("QuestID")] public string Card(EventArguments args) => args.TriggingQuestID;
    }

    [Desc("某个单位任务状态已更新", "[游戏]/任务")]
    public class OnQuestStateUpdated : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("某个单位任务状态已更新");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.QuestStatusChangedHandler handler = new InstanceZone.QuestStatusChangedHandler((u, q, k, v) =>
            {
                args.TriggingUnit = u;
                args.TriggingQuestID = q;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnQuestStatusChanged += handler,
                static (zone, handler) => zone.OnQuestStatusChanged -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("QuestID")] public string Card(EventArguments args) => args.TriggingQuestID;
    }

}
