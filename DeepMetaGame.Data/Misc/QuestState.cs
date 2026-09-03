using System;
using System.Collections.Generic;
using System.Text;
using DeepCore;

namespace DeepMetaGame.Data.Misc
{

    public enum QuestState : byte
    {
        Uncharted = 0,
        Accepted = 1,
        Update = 2,
        Commited = 3,
    }
    public class QuestData
    {
        public readonly string QuestID;
        public QuestState State = QuestState.Uncharted;
        public HashMap<string, string> Attributes = new HashMap<string, string>();
        public QuestData(string id)
        {
            QuestID = id;
        }
    }

}
