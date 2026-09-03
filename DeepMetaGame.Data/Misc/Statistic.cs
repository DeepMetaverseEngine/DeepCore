using DeepMetaGame.Data.Template;

namespace DeepMetaGame.Data.Misc
{
    public interface IZoneUnitStatistic
    {
        /// <summary>
        /// 死亡次数
        /// </summary>
        int DeadCount { get; }
        /// <summary>
        /// 总共杀死玩家数量
        /// </summary>
        int KillPlayerCount { get; }
        /// <summary>
        /// 总共杀死单位数量
        /// </summary>
        int KillUnitCount { get; }

        /// <summary>
        /// 承受伤害
        /// </summary>
        long SelfDamage { get; }
        /// <summary>
        /// 对所有单位输出的总伤害
        /// </summary>
        long TotalDamage { get; }
        /// <summary>
        /// 对玩家输出的总伤害
        /// </summary>
        long PlayerDamage { get; }

        /// <summary>
        /// 对所有单位输出的总治疗量
        /// </summary>
        long TotalHealing { get; }
        /// <summary>
        /// 对玩家输出的总治疗量
        /// </summary>
        long PlayerHealing { get; }

        /// <summary>
        /// 总共杀死特定类型单位数量
        /// </summary>
        /// <param name="type"></param>
        int GetKillUnitCount(UnitType type);

    }

}
