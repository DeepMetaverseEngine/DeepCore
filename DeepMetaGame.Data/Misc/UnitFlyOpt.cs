using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{


    [MessageType(BattleConstants.UnitFlyOpt)]
    [Desc("单位无重力飞行配置")]
    [Expandable]
    public class UnitFlyOpt : IBaseFuncData
    {
        [Desc("最小Z坐标")] public float MinPosZ;
        [Desc("最大Z坐标")] public float MaxPosZ = 500;
        [Desc("设置起始Z坐标-为0表示维持单位自身值")] public float StartPosZ;
        [Desc("是否使用单位当前相对位置")] public bool IsRelativeMinMax;

        public override string ToString()
        {
            var s = IsRelativeMinMax ? "(相对位置)" : string.Empty;
            return $"{MinPosZ}-{MaxPosZ}{s}";
        }
    }
}
