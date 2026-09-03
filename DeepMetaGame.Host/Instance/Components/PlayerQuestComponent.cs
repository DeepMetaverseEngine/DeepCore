using DeepMetaGame.Data.Template;using DeepMetaGame.Data.Message;
using System;
using System.Collections.Generic;
using System.Text;
using DeepMetaGame.Data.Misc;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class PlayerQuestComponent : PlayerComponent
    {
        protected HashMap<string, QuestData> mQuests = new HashMap<string, QuestData>();

        public QuestData ForEachQuest<ST>(ST st, ForEachPredicate<ST,QuestData> action)
        {
            foreach(var q in mQuests.Values)
            {
                if (action(st, q))
                {
                    return q;
                }
            }
            return null;
        }


        public void DoAcceptQuest(string questID, string args)
        {
            Owner.Parent.doAcceptQuest(Owner, questID, args);
        }
        public void DoCommitQuest(string questID, string args)
        {
            Owner.Parent.doCommitQuest(Owner, questID, args);
        }
        public void DoDropQuest(string questID, string args)
        {
            Owner.Parent.doDropQuest(Owner, questID, args);
        }
        public void DoUpdateQuestStatus(string questID, string key, string value)
        {
            Owner.Parent.doUpdateQuestStatus(Owner, questID, key, value);
        }

        public virtual void InitQuestData(ICollection<QuestData> datas)
        {
            foreach (QuestData q in datas)
            {
                if (q.QuestID != null)
                {
                    mQuests.Put(q.QuestID, q);
                }
            }
        }

        public virtual QuestData doQuestAccepted(string quest)
        {
            if (!string.IsNullOrEmpty(quest))
            {
                QuestData qd = new QuestData(quest);
                qd.State = QuestState.Accepted;
                mQuests.Put(quest, qd);
                return qd;
            }
            return null;
        }
        public virtual QuestData doQuestCommitted(string quest)
        {
            QuestData qd = mQuests.RemoveByKey(quest);
            if (qd != null)
            {
                qd.State = QuestState.Commited;
            }
            return qd;
        }
        public virtual QuestData doQuestDropped(string quest)
        {
            QuestData qd = mQuests.RemoveByKey(quest);
            if (qd != null)
            {
                qd.State = QuestState.Uncharted;
            }
            return qd;
        }
        public virtual QuestData doQuestStatusChanged(string quest, string key, string value)
        {
            QuestData qd = mQuests.Get(quest);
            if (qd != null)
            {
                qd.Attributes.Put(key, value);
            }
            return qd;
        }




        public virtual bool IsQuestAccepted(string quest)
        {
            QuestData qd = mQuests.Get(quest);
            if (qd != null)
            {
                return qd.State == QuestState.Accepted;
            }
            return false;
        }
        public virtual string GetQuestStatus(string quest, string key)
        {
            QuestData qd = mQuests.Get(quest);
            if (qd != null)
            {
                string value;
                if (qd.Attributes.TryGetValue(key, out value))
                {
                    return value;
                }
            }
            return null;
        }
        public virtual QuestData GetQuest(string quest)
        {
            return mQuests.Get(quest);
        }

    }
}
