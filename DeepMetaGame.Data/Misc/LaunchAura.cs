using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{

    /// <summary>
    /// 发起BUFF
    /// </summary>
    [MessageType(BattleConstants.LaunchAura)]
    [Desc("开启光环")]
    [Expandable]
    public class LaunchAura : ISNData
    {
        [Desc("光环模板ID")]
        [TemplateIDAttribute(typeof(AuraTemplate))]
        public int AuraID;
        [Desc("光环等级")]
        public int AuraLevel;
        [Desc("触发百分比")]
        public float LaunchPercent = 100f;

        public LaunchAura() { }
        public LaunchAura(int auraID) { AuraID = auraID; }

        public override string ToString()
        {
            return "开启光环:" + AuraID;
        }

    }
}
