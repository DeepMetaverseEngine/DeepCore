using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{

    [MessageType(BattleConstants.UnitAnimation)]
    [Expandable]
    [Desc("单位动作数据")]
    public class UnitAnimation : IBaseFuncData
    {
        [Desc("动作名称")]
        public string Name;
        [Desc("切换时长")]
        public float CrossTime;
        [Desc("播放速率")]
        public float Speed = 1.0f;
        
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return base.ToString();
            return $"动作名:{Name} 播放速率:{Speed} 切换时长:{CrossTime}";
        }
    }
}
