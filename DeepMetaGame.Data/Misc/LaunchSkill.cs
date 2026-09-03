using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{

    /// <summary>
    /// 发起法术或者飞行道具
    /// </summary>
    [MessageType(BattleConstants.LaunchSkill)]
    [Desc("释技能")]
    [Expandable]
    public class LaunchSkill : ISNData
    {
        [Desc("SkillID")]
        [TemplateIDAttribute(typeof(SkillTemplate))]
        public int SkillID;

        [Desc("技能等级")]
        public int SkillLevel;

        [Desc("释放技能权值")]
        public int Priority;

        [Desc("AI自动释放技能")]
        public bool AutoLaunch = true;

        [Desc("自动战斗锁敌方式", "战斗 - 警戒")]
        public SeekingExpect AutoSeeking = SeekingExpect.Nearest;
        public enum SeekingExpect : byte
        {
            [Desc("搜索随机单位")]
            Random,
            [Desc("搜索最近单位")]
            Nearest,
            [Desc("搜索最远单位")]
            Farthest,
            [Desc("搜索血量最大单位")]
            HP_Max,
            [Desc("搜索血量最小单位")]
            HP_Min,
            [Desc("自定义搜索")]
            Custom,
            [Desc("搜索血量百分比最小单位")]
            HP_Ratio_Min,
        }

        public LaunchSkill() { }
        public LaunchSkill(int skillID)
        {
            SkillID = skillID;
        }
        public override string ToString()
        {
            return "触发技能:" + SkillID;
        }

    }

}
