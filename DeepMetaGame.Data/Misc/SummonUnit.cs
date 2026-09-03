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
    /// 召唤单位
    /// </summary>
    [MessageType(BattleConstants.SummonUnit)]
    [Desc("召唤单位")]
    [Expandable]
    public class SummonUnit : ISNData
    {
        [Desc("召唤单位模板")]
        [TemplateIDAttribute(typeof(UnitInfo))]
        public int UnitTemplateID;

        [Desc("召唤单位等级")]
        [TemplateLevelAttribute]
        public int UnitLevel = 0;

        [Desc("召唤处产生数量")]
        public int Count = 1;

        [Desc("召唤处产生特效")]
        public LaunchEffect Effect;
        
        [Desc("召唤位置随机")]
        public bool IsRandom = false;

        public SummonUnit() { }
        public SummonUnit(int templateID)
        {
            UnitTemplateID = templateID;
        }

        public override string ToString()
        {
            return "召唤单位:" + UnitTemplateID + " 数量:" + Count;
        }

    }


}
