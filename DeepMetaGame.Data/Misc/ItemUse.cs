using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{


    [MessageType(BattleConstants.UseItem)]
    [Desc("道具使用")]
    [Expandable]
    public class UseItem : ISNData
    {
        /// <summary>
        /// 掉落道具模板ID
        /// </summary>
        [Desc("道具模板ID")]
        [TemplateIDAttribute(typeof(ItemTemplate))]
        public int ItemTemplateID;

        public UseItem() { }
        public UseItem(int itemID)
        {
            ItemTemplateID = itemID;
        }
        public override string ToString()
        {
            return "使用道具:" + ItemTemplateID;
        }

    }

}
