using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{
    [MessageType( BattleConstants. FocusTarget)]
    [Desc("自动锁定")]
    [Expandable]
    public class FocusTarget : ISNData
    {
        [Desc("自动锁定范围内目标（范围）", "自动锁定")]
        public float SeekingTargetRange = 10f;
        //         [Desc("自动锁定范围内目标（方式）", "自动锁定")]
        //         public SpellTemplate.SeekingExpect SeekingTargetExpect = SpellTemplate.SeekingExpect.Nearest;
        [Desc("自动锁定范围内目标（方式）", "自动锁定")]
        public LaunchSkill.SeekingExpect SeekingTargetExpect = LaunchSkill.SeekingExpect.Random;
        [Desc("搜索单位(忽略链中)", "自动锁定")]
        public bool SeekingIgnoreInChain = false;


        [Desc("锁定目标部位", "自动锁定")]
        public SeekingTargetAnchor TargetAnchor = SeekingTargetAnchor.Waist;
        [Desc("锁定目标第几个", "自动锁定")]
        public int SeekingTargetIndex = 0;

        [Desc("改变法术初始坐标", "自动锁定")]
        public bool ChangeStartPos = true;
        [Desc("改变朝向", "自动锁定")]
        public bool ChangeDirection = true;
        [Desc("改变目标坐标", "自动锁定")]
        public bool ChangeTargetPos = true;


    }

    [Desc("锁定目标部位")]
    public enum SeekingTargetAnchor : byte
    {
        [Desc("脚下")]
        Foot = 0,
        [Desc("腰部")]
        Waist = 1,
        [Desc("头部")]
        Head = 2,
    }
}
