using DeepCore.Game3D.Host.Instance;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.IO;

namespace DeepCore.Game3D.Host.Helper
{
    public abstract class IUnitStatistic : IZoneUnitStatistic
    {
        public InstanceUnit Owner { get; private set; }

        public IUnitStatistic(InstanceUnit owner)
        {
            this.Owner = owner;
        }

        /// <summary>
        /// 转存可序列化协议对象
        /// </summary>
        /// <returns></returns>
        public virtual UnitStatisticData ToUnitStatisticData()
        {
            var ret = new UnitStatisticData();
            ret.ObjectID = Owner.ID;
            ret.DeadCount = this.DeadCount;
            ret.KillUnitCount = this.KillUnitCount;
            ret.KillPlayerCount = this.KillPlayerCount;
            ret.SelfDamage = this.SelfDamage;
            ret.TotalDamage = this.TotalDamage;
            ret.PlayerDamage = this.PlayerDamage;
            ret.TotalHealing = this.TotalHealing;
            ret.PlayerHealing = this.PlayerHealing;
            return ret;
        }

        public override string ToString()
        {
            var sw = new StringWriter();
            ToString(sw);
            return sw.ToString();
        }

        public virtual void ToString(StringWriter sw)
        {
            sw.WriteLine($"-------------------");
            sw.WriteLine($"死亡次数={DeadCount}");
            sw.WriteLine($"杀死单位数量={KillUnitCount}");
            sw.WriteLine($"承受伤害={SelfDamage}");
            sw.WriteLine($"承受治疗={SelfHealing}");
            sw.WriteLine($"输出伤害={TotalDamage}");
            sw.WriteLine($"输出治疗={TotalHealing}");
            sw.WriteLine($"-------------------");
            sw.WriteLine($"杀死玩家数量={KillPlayerCount}");
            sw.WriteLine($"对玩家输出伤害={PlayerDamage}");
            sw.WriteLine($"对玩家输出治疗={PlayerHealing}");
            sw.WriteLine($"-------------------");
        }

        //------------------------------------------------------------------------------------
        /// <summary>
        /// 死亡次数
        /// </summary>
        public abstract int DeadCount { get; }

        /// <summary>
        /// 总共杀死玩家数量
        /// </summary>
        public abstract int KillPlayerCount { get; }

        /// <summary>
        /// 总共杀死单位数量
        /// </summary>
        public abstract int KillUnitCount { get; }

        /// <summary>
        /// 承受伤害
        /// </summary>
        public abstract long SelfDamage { get; }

        /// <summary>
        /// 对所有单位输出的总伤害
        /// </summary>
        public abstract long TotalDamage { get; }

        /// <summary>
        /// 对玩家输出的总伤害
        /// </summary>
        public abstract long PlayerDamage { get; }

        /// <summary>
        /// 承受治疗量
        /// </summary>
        public abstract long SelfHealing { get; }

        /// <summary>
        /// 对所有单位输出的总治疗量
        /// </summary>
        public abstract long TotalHealing { get; }

        /// <summary>
        /// 对玩家输出的总治疗量
        /// </summary>
        public abstract long PlayerHealing { get; }

        /// <summary>
        /// 总共杀死特定类型单位数量
        /// </summary>
        /// <param name="type"></param>
        public abstract int GetKillUnitCount(UnitType type);

        //------------------------------------------------------------------------------------

        /// <summary>
        /// 当自己死亡
        /// </summary>
        /// <param name="killer"></param>
        public abstract void onDead(InstanceUnit killer);

        /// <summary>
        /// 干掉别人
        /// </summary>
        /// <param name="target"></param>
        public abstract void onKill(InstanceUnit target);

        /// <summary>
        /// 当受到伤害
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="reduceHP"></param>
        public abstract void onDamage(InstanceUnit attacker, long reduceHP);

        /// <summary>
        /// 当造成伤害
        /// </summary>
        /// <param name="target"></param>
        /// <param name="reduceHP"></param>
        public abstract void onAttack(InstanceUnit target, long reduceHP);

        /// <summary>
        /// 当使用道具
        /// </summary>
        /// <param name="item"></param>
        public abstract void onUseItem(ItemTemplate item);

        //------------------------------------------------------------------------------------

        //------------------------------------------------------------------------------------
    }

    //------------------------------------------------------------------------------------

    public class UnitStatistic : IUnitStatistic
    {
        private int m_DeadCount = 0;
        private int m_KillPlayerCount = 0;
        private int m_KillUnitCount = 0;
        private long m_SelfDamage = 0;
        private long m_TotalDamage = 0;
        private long m_PlayerDamage = 0;
        private long m_TotalHealing = 0;
        private long m_PlayerHealing = 0;
        private long m_SelfHealing = 0;

        private Dictionary<UnitType, int> m_KillUnitCountMap = new Dictionary<UnitType, int>();

        public UnitStatistic(InstanceUnit owner) : base(owner)
        {
        }

        public override int DeadCount
        {
            get { return m_DeadCount; }
        }

        public override int KillPlayerCount
        {
            get { return m_KillPlayerCount; }
        }

        public override int KillUnitCount
        {
            get { return m_KillUnitCount; }
        }

        public override long SelfDamage
        {
            get { return m_SelfDamage; }
        }

        public override long TotalDamage
        {
            get { return m_TotalDamage; }
        }

        public override long PlayerDamage
        {
            get { return m_PlayerDamage; }
        }

        public override long SelfHealing
        {
            get { return m_SelfHealing; }
        }

        public override long TotalHealing
        {
            get { return m_TotalHealing; }
        }

        public override long PlayerHealing
        {
            get { return m_PlayerHealing; }
        }

        public void Reset()
        {
            m_DeadCount = 0;
            m_KillPlayerCount = 0;
            m_KillUnitCount = 0;
            m_SelfDamage = 0;
            m_TotalDamage = 0;
            m_PlayerDamage = 0;
            m_TotalHealing = 0;
            m_PlayerHealing = 0;
            m_SelfHealing = 0;
            m_KillUnitCountMap.Clear();
        }

        public override int GetKillUnitCount(UnitType type)
        {
            int ret = 0;
            if (m_KillUnitCountMap.TryGetValue(type, out ret))
                return ret;
            return 0;
        }

        public override void onDead(InstanceUnit killer)
        {
            this.m_DeadCount += 1;
        }

        public override void onKill(InstanceUnit target)
        {
            this.m_KillUnitCount += 1;
            if (target.IsPlayer)
            {
                this.m_KillPlayerCount += 1;
            }

            int count = 0;
            if (this.m_KillUnitCountMap.TryGetValue(target.UType, out count))
            {
                count += 1;
                this.m_KillUnitCountMap[target.UType] = count;
            }
            else
            {
                this.m_KillUnitCountMap.Add(target.UType, 1);
            }
        }

        public override void onDamage(InstanceUnit attacker, long reduceHP)
        {
            if (reduceHP > 0)
            {
                this.m_SelfDamage += reduceHP;
            }
            else if (reduceHP < 0)
            {
                this.m_SelfHealing += (-reduceHP);
            }
        }

        public override void onAttack(InstanceUnit target, long reduceHP)
        {
            if (reduceHP > 0)
            {
                this.m_TotalDamage += reduceHP;
                if (target.IsPlayer)
                {
                    this.m_PlayerDamage += reduceHP;
                }
            }
            else
            {
                this.m_TotalHealing += -reduceHP;
                if (target.IsPlayer)
                {
                    this.m_PlayerHealing += -reduceHP;
                }
            }
        }

        public override void onUseItem(ItemTemplate item)
        {
        }
    }
    //     
    //     public class UnitStatistic
    //     {
    //         public readonly InstanceUnit Owner;
    // 
    //         private int mTotalTakeMoney = 0;
    // 
    //         //自身受到的伤害.
    //         private long mSelfDamage;
    //         //对怪物输出的伤害.
    //         private long mMonsterDamage;
    //         //对玩家输出伤害.
    //         private long mPlayerDamage;
    // 
    //         //治疗回复血量.
    //         private long mSelfHealing;
    //         //怪物造成的治疗量.
    //         private long mMonsterHealing;
    //         //玩家造成的治疗量.
    //         private long mPlayerHealing;
    // 
    // 
    //         private int mDeadCount;
    //         private int mKillPlayerCount;
    //         private int mKillUnitCount;
    // 
    //         private HashMap<UnitType, int> mKillUnitCountMap = new HashMap<UnitType, int>();
    // 
    //         private List<int> mKilledEllitMonsters = new List<int>();
    // 
    //         private List<int> mUseItems = new List<int>();
    // 
    //         public UnitStatistic(InstanceUnit owner)
    //         {
    //             Owner = owner;
    //         }
    //         
    //         protected internal virtual void onDead()
    //         {
    //             mDeadCount += 1;
    //         }
    // 
    //         protected internal virtual void onHitAttack(InstanceUnit self, InstanceUnit sender, int reduceHP)
    //         {
    //             //伤害.
    //             if (reduceHP > 0)
    //             {
    //                 mSelfDamage += reduceHP;
    //                 if (self is InstancePlayer)
    //                 {
    //                     sender.Statistic.mOutPlayerDamage += reduceHP;
    //                 }
    //                 else
    //                 {
    //                     sender.Statistic.mOutMonsterDamage += reduceHP;
    //                 }
    //             }
    //             else if (reduceHP < 0)//治疗量.
    //             {
    //                 mSelfHealingPerSecond += -reduceHP;
    //                 if (self is InstancePlayer)
    //                 {
    //                     sender.Statistic.mOutPlayerHealingPerSecond += -reduceHP;
    //                 }
    //                 else
    //                 {
    //                     sender.Statistic.mOutMonsterHealingPerSecond += -reduceHP;
    //                 }
    //             }
    //             else
    //             {
    //                 return;
    //             }
    // 
    //             if (self.IsDead)
    //             {
    //                 sender.Statistic.addKillUnit(self);
    // 
    //                 sender.Statistic.mTotalTakeMoney += self.Info.DropMoney;
    //             }
    //         }
    // 
    //         protected internal virtual void onUseItem(InstanceUnit self, ItemTemplate item)
    //         {
    //             mUseItems.Add(item.ID);
    //         }
    // 
    // 
    // 
    // 
    //         private void addKillUnit(InstanceUnit unit)
    //         {
    //             mKillUnitCount += 1;
    // 
    //             if (unit is InstancePlayer)
    //             {
    //                 mKillPlayerCount += 1;
    //             }
    //             else if (unit.Info.IsElite)
    //             {
    //                 mKilledEllitMonsters.Add(unit.Info.ID);
    //             }
    // 
    //             UnitType type = unit.Info.UType;
    //             if (!mKillUnitCountMap.ContainsKey(type))
    //             {
    //                 mKillUnitCountMap[type] = 1;
    //             }
    //             else
    //             {
    //                 mKillUnitCountMap[type] = (mKillUnitCountMap[type] + 1);
    //             }
    //         }
    // 
    // 
    // 
    //         /// <summary>
    //         /// 获取的金币数量
    //         /// </summary>
    //         public int TotalTakeMoney
    //         {
    //             get
    //             {
    //                 return mTotalTakeMoney;
    //             }
    //         }
    // 
    // 
    //         /// <summary>
    //         /// 死亡次数
    //         /// </summary>
    //         public int DeadCount
    //         {
    //             get
    //             {
    //                 return mDeadCount;
    //             }
    //         }
    // 
    //         /// <summary>
    //         /// 承受伤害
    //         /// </summary>
    //         public long SelfDamage
    //         {
    //             get
    //             {
    //                 return mSelfDamage;
    //             }
    //         }
    // 
    //         /// <summary>
    //         /// 使用的所有道具 编辑器道具ID[]
    //         /// </summary>
    //         public int[] UseItems
    //         {
    //             get
    //             {
    //                 return mUseItems.ToArray();
    //             }
    //         }
    // 
    //         /// <summary>
    //         /// 总共杀死玩家数量
    //         /// </summary>
    //         public int KillPlayerCount
    //         {
    //             get
    //             {
    //                 return mKillPlayerCount;
    //             }
    //         }
    // 
    //         public int KillUnitCount
    //         {
    //             get
    //             {
    //                 return mKillUnitCount;
    //             }
    //         }
    // 
    //         /// <summary>
    //         /// 杀死精英怪物 编辑器单位ID[]
    //         /// </summary>
    //         public int[] KilledEllitMonsters
    //         {
    //             get
    //             {
    //                 return mKilledEllitMonsters.ToArray();
    //             }
    //         }
    // 
    //         /// <summary>
    //         /// 对怪物造成的总伤害
    //         /// </summary>
    //         public long MonsterDamage
    //         {
    //             get
    //             {
    //                 return mOutMonsterDamage;
    //             }
    //         }
    // 
    //         /// <summary>
    //         /// 对玩家造成的总伤害
    //         /// </summary>
    //         public long PlayerDamage
    //         {
    //             get
    //             {
    //                 return mOutPlayerDamage;
    //             }
    //         }
    //         //治疗回复血量.
    //         public long SelfHealingPerSecond
    //         {
    //             get
    //             {
    //                 return mSelfHealingPerSecond;
    //             }
    //         }
    //         //玩家造成的治疗量.
    //         public long PlayerHealingPerSecond
    //         {
    //             get
    //             {
    //                 return mOutPlayerHealingPerSecond;
    //             }
    //         }
    //         //怪物造成的治疗量.
    //         public long MonsterHealingPerSecond
    //         {
    //             get
    //             {
    //                 return mOutMonsterHealingPerSecond;
    //             }
    //         }
    // 
    //         /// <summary>
    //         /// 总共杀死怪物数量
    //         /// </summary>
    //         public int GetKillUnitCount(UnitType type)
    //         {
    //             if (mKillUnitCountMap.ContainsKey(type))
    //             {
    //                 return mKillUnitCountMap[type];
    //             }
    //             return 0;
    //         }
    // 
    //         public UnitStatisticData ToUnitStatisticData()
    //         {
    //             var rst = new UnitStatisticData();
    //             rst.ObjectID = Owner.ID;
    //             rst.DeadCount = this.DeadCount;
    //             rst.ItemTemplates = this.UseItems;
    //             rst.KillUnitCount = this.KillUnitCount;
    //             rst.KillEnemyCount = this.GetKillUnitCount(UnitType.TYPE_MONSTER);
    //             rst.KillNeutralityCount = this.GetKillUnitCount(UnitType.TYPE_NEUTRALITY);
    //             rst.KillBuildingCount = this.GetKillUnitCount(UnitType.TYPE_BUILDING);
    //             rst.KillPlayerCount = this.KillPlayerCount;
    //             rst.MonsterDamage = this.MonsterDamage;
    //             rst.PlayerDamage = this.PlayerDamage;
    //             rst.SelfDamage = this.SelfDamage;
    //             rst.TotalTakeMoney = this.TotalTakeMoney;
    //             rst.KilledEllitMonsters = this.KilledEllitMonsters;
    //             if (Owner is InstancePlayer)
    //             {
    //                 // 统计杀死玩家  //
    //                 rst.KillPlayers = (Owner as InstancePlayer).KilledPlayersID;
    // 
    //             }
    //             return rst;
    //         }
    // 
    //         public RoomFinishInfoB2R.PlayerResult ToUnitStatisticDataServer()
    //         {
    //             RoomFinishInfoB2R.PlayerResult rst = new RoomFinishInfoB2R.PlayerResult();
    //             rst.ZoneObjectID = Owner.ID;
    //             rst.Force = Owner.Force;
    //             rst.DeadCount = this.DeadCount;
    //             rst.ItemTemplates = this.UseItems;
    //             rst.KillUnitCount = this.KillUnitCount;
    //             rst.KillEnemyCount = this.GetKillUnitCount(UnitType.TYPE_MONSTER);
    //             rst.KillNeutralityCount = this.GetKillUnitCount(UnitType.TYPE_NEUTRALITY);
    //             rst.KillBuildingCount = this.GetKillUnitCount(UnitType.TYPE_BUILDING);
    //             rst.KillPlayerCount = this.KillPlayerCount;
    //             rst.MonsterDamage = this.MonsterDamage;
    //             rst.PlayerDamage = this.PlayerDamage;
    //             rst.SelfDamage = this.SelfDamage;
    //             rst.TotalTakeMoney = this.TotalTakeMoney;
    //             rst.KilledEllitMonsters = this.KilledEllitMonsters;
    //             // 统计杀死玩家  //
    //             if (Owner is InstancePlayer)
    //             {
    //                 var p = Owner as InstancePlayer;
    //                 rst.KillPlayers = p.KilledPlayersID;
    //                 rst.PlayerUUID = p.PlayerUUID;
    //             }
    //             return rst;
    //         }
    //         public void FromUnitStatisticDataServer(RoomFinishInfoB2R.PlayerResult rst)
    //         {
    //             this.mDeadCount = rst.DeadCount;
    //             this.mUseItems = new List<int>(rst.ItemTemplates);
    //             this.mKillUnitCount = rst.KillUnitCount;
    //             this.mKillUnitCountMap.Put(UnitType.TYPE_MONSTER, rst.KillEnemyCount);
    //             this.mKillUnitCountMap.Put(UnitType.TYPE_NEUTRALITY, rst.KillNeutralityCount);
    //             this.mKillUnitCountMap.Put(UnitType.TYPE_BUILDING, rst.KillBuildingCount);
    //             this.mKillPlayerCount = rst.KillPlayerCount;
    //             this.mOutMonsterDamage = rst.MonsterDamage;
    //             this.mOutPlayerDamage = rst.PlayerDamage;
    //             this.mSelfDamage = rst.SelfDamage;
    //             this.mTotalTakeMoney = rst.TotalTakeMoney;
    //             this.mKilledEllitMonsters = new List<int>(rst.KilledEllitMonsters);
    //             // 统计杀死玩家  //
    //             if (Owner is InstancePlayer)
    //             {
    //                 var p = Owner as InstancePlayer;
    //                 foreach (var uuid in rst.KillPlayers)
    //                 {
    //                     p.PutKillPlayer(uuid);
    //                 }
    //             }
    //         }
    //     }
}