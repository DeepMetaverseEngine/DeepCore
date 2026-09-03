using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{


    /// <summary>
    /// 道具掉落
    /// </summary>
    [MessageType(BattleConstants.DropItem)]
    [Desc("道具掉落")]
    [Expandable]
    public class DropItem : ISNData
    {
        /// <summary>
        /// 掉落道具模板ID
        /// </summary>
        [Desc("掉落道具模板ID")]
        [TemplateIDAttribute(typeof(ItemTemplate))]
        public int ItemTemplateID;
        /// <summary>
        /// 掉落数量
        /// </summary>
        [Desc("掉落数量")]
        public int DropCount = 1;
        /// <summary>
        /// 掉落道具百分比
        /// </summary>
        [Desc("掉落道具百分比")]
        public float DropPercent;

        [Desc("掉落位置随机范围点")]
        public float DropPosRange = 0;

        public DropItem() { }
        public DropItem(int itemID, float percent)
        {
            ItemTemplateID = itemID;
            DropPercent = percent;
        }
        public override string ToString()
        {
            return "道具掉落:" + ItemTemplateID + " 掉率:" + DropPercent + "%";
        }
    }


    /// <summary>
    /// 道具掉落
    /// </summary>
    [MessageType(BattleConstants.DropItemList)]
    [Desc("道具掉落列表")]
    [Expandable]
    public class DropItemList : ISNData
    {
        /// <summary>
        /// 掉落道具模板ID
        /// </summary>
        [Desc("掉落道具模板列表")]
        public ArrayList<DropItem> DropItems = new ArrayList<DropItem>();

        public DropItemList() { }

        public override string ToString()
        {
            return "道具掉落:" + DropItems.Count + "个物品中的一个";
        }

    }

}
