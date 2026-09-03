using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{
    [MessageType( BattleConstants. CardSlot)]
    [Desc("装备词缀")]
    [Expandable]
    public class CardSlot : ISNData
    {
        [Desc("词缀ID")]
        [TemplateID(typeof(CardTemplate))]
        public int CardTemplateID;
        [Desc("词缀操作")]
        public CardSlotOperation Op = CardSlotOperation.Upgrade;
        [DependOnProperty(nameof(Op))]public bool IsSetLevel { get => Op == CardSlotOperation.SetLevel; }
        [DependOnProperty(nameof(IsSetLevel))]
        [Desc("词缀等级")]
        public int Level;
        [Desc("词缀操作")]
        public enum CardSlotOperation
        {
            [Desc("设置等级")] SetLevel = 0,
            [Desc("升级")] Upgrade = 1,
            [Desc("降级")] Degrade = 2,
            [Desc("清除")] Clear = 3,
        }
    }


}
