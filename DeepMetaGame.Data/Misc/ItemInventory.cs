using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{

    /// <summary>
    /// 携带道具
    /// </summary>
    [MessageType(BattleConstants.InventoryItem)]
    [Desc("单位携带道具")]
    [Expandable]
    public class InventoryItem : IBaseFuncData
    {

        [TemplateIDAttribute(typeof(ItemTemplate))]
        [Desc("道具模板ID")]
        public int ItemTemplateID;

        [Desc("数量ID")]
        public int Count;

        public override string ToString()
        {
            return "携带道具:" + ItemTemplateID + "x" + Count;
        }


    }
}
