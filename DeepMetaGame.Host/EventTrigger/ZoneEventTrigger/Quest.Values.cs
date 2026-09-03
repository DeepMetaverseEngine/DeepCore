using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using System;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("触发的任务ID", "[游戏]/任务")]
    public class TriggingQuestIdentify : ZoneStringValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("触发的任务");
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.TriggingQuestID;
        }
    }

    [Desc("单位任务子状态查询", "[游戏]/任务")]
    public class GetQuestField : ZoneStringValue
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("任务ID")]
        public AbstractValue<string> QuestID = new TriggingQuestIdentify();
        [Desc("任务子状态字段")]
        public AbstractValue<string> Key = new ZoneStringValue.VALUE("key");

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({0})任务({1})子状态({2})", Unit, QuestID, Key);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var p = Unit.GetValueAs(api, args) as InstancePlayer;
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                string quest_id = QuestID.GetValueAs(api, args);
                string key = Key.GetValueAs(api, args);
                if (!string.IsNullOrEmpty(quest_id) && !string.IsNullOrEmpty(key))
                {
                    QuestData qd = q.GetQuest(quest_id);
                    if (qd != null)
                    {
                        return qd.Attributes.Get(key);
                    }
                }
            }
            return null;
        }
    }


    [Desc("单位任务主状态判断", "[游戏]/任务")]
    public class GetQuestState : ZoneBooleanValue
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("任务ID")]
        public AbstractValue<string> QuestID = new TriggingQuestIdentify();
        [Desc("预期任务状态")]
        public QuestState ExpectState = QuestState.Accepted;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({0})任务({1})状态是否为({2})", Unit, QuestID, ExpectState);
        }
        protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var p = Unit.GetValueAs(api, args) as InstancePlayer;
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                string quest_id = QuestID.GetValueAs(api, args);
                if (!string.IsNullOrEmpty(quest_id))
                {
                    QuestData qd = q.GetQuest(quest_id);
                    if (qd != null)
                    {
                        return qd.State == ExpectState;
                    }
                }
            }
            return false;
        }
    }



    [Desc("任务是否已接受", "[游戏]/任务")]
    public class QuestAccepted : ZoneBooleanValue
    {
        [Desc("单位 - 某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [QuestIDAttribute]
        [Desc("任务ID")]
        public string Quest;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})是否已接受任务({1})", Unit, Quest);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var p = Unit.GetValueAs(api, args) as InstancePlayer;
            if (p?.QuestComponent is PlayerQuestComponent pq)
            {
                return pq.IsQuestAccepted(Quest);
            }
            return false;
        }
    }

    [Desc("任务是否已接受(变量)", "[游戏]/任务")]
    public class QuestAcceptedSV : ZoneBooleanValue
    {
        [Desc("单位 - 某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("任务ID")]
        public AbstractValue<string> QuestID = new TriggingQuestIdentify();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})是否已接受任务({1})", Unit, QuestID);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var p = Unit.GetValueAs(api, args) as InstancePlayer;
            string questID = QuestID.GetValueAs(api, args);
            if (p?.QuestComponent is PlayerQuestComponent q)
            {
                return q.IsQuestAccepted(questID);
            }
            return false;
        }
    }

}
