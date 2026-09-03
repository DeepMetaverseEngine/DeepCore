using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Reflection;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepMetaGame.Data;
using DeepCore.Game3D.Host.Instance.Components;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("接受任务(常量)", "[游戏]/任务")]
    public class AcceptQuest : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [QuestIDAttribute]
        [Desc("任务ID")]
        public string Quest;
        [Desc("参数")]
        public AbstractValue<string> Args = new StringValue.VALUE("");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})接受任务({1});", Unit, Quest);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstancePlayer p = Unit.GetValueAs(api, args) as InstancePlayer;
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                q.DoAcceptQuest(Quest, Args.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("提交完成任务(常量)", "[游戏]/任务")]
    public class CommitQuest : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [QuestIDAttribute]
        [Desc("任务ID")]
        public string Quest;
        [Desc("参数")]
        public AbstractValue<string> Args = new StringValue.VALUE("");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})完成任务({1});", Unit, Quest);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstancePlayer p = Unit.GetValueAs(api, args) as InstancePlayer;
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                q.DoCommitQuest(Quest, Args.GetValueAs(api, args));
            }
            return null;
        }
    }
    [Desc("放弃任务(常量)", "[游戏]/任务")]
    public class DropQuest : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [QuestIDAttribute]
        [Desc("任务ID")]
        public string Quest;
        [Desc("参数")]
        public AbstractValue<string> Args = new StringValue.VALUE("");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})放弃任务({1});", Unit, Quest);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstancePlayer p = Unit.GetValueAs(api, args) as InstancePlayer;
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                q.DoDropQuest(Quest, Args.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("更新任务子状态(常量)", "[游戏]/任务")]
    public class UpdateQuestStatus : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [QuestIDAttribute]
        [Desc("任务ID")]
        public string Quest;
        [Desc("字段")]
        public AbstractValue<string> Key = new StringValue.VALUE("key");
        [Desc("值")]
        public AbstractValue<string> Value = new StringValue.VALUE("value");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})更新任务({1})状态({2})=({3});", Unit, Quest, Key, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstancePlayer p = Unit.GetValueAs(api, args) as InstancePlayer;
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                q.DoUpdateQuestStatus(Quest, Key.GetValueAs(api, args), Value.GetValueAs(api, args));
            }
            return null;
        }
    }


    [Desc("接受任务(变量)", "[游戏]/任务")]
    public class AcceptQuestVar : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("任务ID")]
        public AbstractValue<string> QuestID = new TriggingQuestIdentify();
        [Desc("参数")]
        public AbstractValue<string> Args = new StringValue.VALUE("");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})接受任务({1});", Unit, QuestID);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstancePlayer p = Unit.GetValueAs(api, args) as InstancePlayer;
            string quest_id = QuestID.GetValueAs(api, args);
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                if (!string.IsNullOrEmpty(quest_id))
                {
                    q.DoAcceptQuest(quest_id, Args.GetValueAs(api, args));
                }
            }
            return null;
        }
    }

    [Desc("提交完成任务(变量)", "[游戏]/任务")]
    public class CommitQuestVar : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("任务ID")]
        public AbstractValue<string> QuestID = new TriggingQuestIdentify();
        [Desc("参数")]
        public AbstractValue<string> Args = new StringValue.VALUE("");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})完成任务({1});", Unit, QuestID);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstancePlayer p = Unit.GetValueAs(api, args) as InstancePlayer;
            string quest_id = QuestID.GetValueAs(api, args);
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                if (!string.IsNullOrEmpty(quest_id))
                {
                    q.DoCommitQuest(quest_id, Args.GetValueAs(api, args));
                }
            }
            return null;
        }
    }
    [Desc("放弃任务(变量)", "[游戏]/任务")]
    public class DropQuestVar : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("任务ID")]
        public AbstractValue<string> QuestID = new TriggingQuestIdentify();
        [Desc("参数")]
        public AbstractValue<string> Args = new StringValue.VALUE("");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})放弃任务({1});", Unit, QuestID);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstancePlayer p = Unit.GetValueAs(api, args) as InstancePlayer;
            string quest_id = QuestID.GetValueAs(api, args);
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                if (!string.IsNullOrEmpty(quest_id))
                {
                    q.DoDropQuest(quest_id, Args.GetValueAs(api, args));
                }
            }
            return null;
        }
    }

    [Desc("更新任务子状态(变量)", "[游戏]/任务")]
    public class UpdateQuestStatusVar : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("任务ID")]
        public AbstractValue<string> QuestID = new TriggingQuestIdentify();
        [Desc("字段")]
        public AbstractValue<string> Key = new StringValue.VALUE("key");
        [Desc("值")]
        public AbstractValue<string> Value = new StringValue.VALUE("value");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})更新任务({1})状态({2})=({3});", Unit, QuestID, Key, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstancePlayer p = Unit.GetValueAs(api, args) as InstancePlayer;
            string quest_id = QuestID.GetValueAs(api, args);
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                if (!string.IsNullOrEmpty(quest_id))
                {
                    q.DoUpdateQuestStatus(quest_id, Key.GetValueAs(api, args), Value.GetValueAs(api, args));
                }
            }
            return null;
        }
    }
}
