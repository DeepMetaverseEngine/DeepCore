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
    [MessageType(BattleConstants.LaunchBuff)]
    [Desc("发起BUFF")]
    [Expandable]
    public class LaunchBuff : ISNData
    {
        /// <summary>
        /// Buff模板ID
        /// </summary>
        [Desc("Buff模板ID")]
        [TemplateIDAttribute(typeof(BuffTemplate))]
        public int BuffID;
        [Desc("Buff等级")]
        public int BuffLevel;
        [Desc("触发百分比")]
        public float LaunchPercent = 100f;

        public LaunchBuff() { }
        public LaunchBuff(int buffID) { BuffID = buffID; }

        public override string ToString()
        {
            return "触发BUFF:" + BuffID;
        }

    }


}
