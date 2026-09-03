using DeepCore.IO;
using DeepCore.Protocol;
using System.Collections.Generic;

namespace DeepMetaGame.ZoneServer.Message
{

    /// <summary>
    /// 创建房间额外的参数
    /// 由游戏服发起(游戏服->战斗服)
    /// </summary>
    [MessageType(0x000AA00)]
    public class CreateRoomInfoR2B : IExternalizable
    {
        public int SceneID;

        public int AverageLevel;

        public string EnvironmentVars;

        virtual public void WriteExternal(IOutputStream output)
        {
            output.PutS32(SceneID);
            output.PutS32(AverageLevel);
            output.PutUTF(EnvironmentVars);
        }
        virtual public void ReadExternal(IInputStream input)
        {
            SceneID = input.GetS32();
            AverageLevel = input.GetS32();
            EnvironmentVars = input.GetUTF();
        }
    }

    /// <summary>
    /// 创建玩家单位额外的参数
    /// 由游戏服发起(游戏服->战斗服)
    /// </summary>
    [MessageType(0x000AA01)]
    public class CreateUnitInfoR2B : IExternalizable
    {
        /// <summary>
        /// 角色模板ID
        /// </summary>
        public int UnitTemplateID;
        /// <summary>
        /// 出生阵营
        /// </summary>
        public byte Force;
        /// <summary>
        /// 玩家出生点
        /// </summary>
        public string StartFlag;
        /// <summary>
        /// 玩家扩展属性
        /// </summary>
        public ISerializable UnitPropData = null;


        virtual public void WriteExternal(IOutputStream output)
        {
            output.PutS32(UnitTemplateID);
            output.PutU8(Force);
            output.PutUTF(StartFlag);
            output.PutObj(UnitPropData);
        }
        virtual public void ReadExternal(IInputStream input)
        {
            UnitTemplateID = input.GetS32();
            Force = input.GetU8();
            StartFlag = input.GetUTF();
            UnitPropData = input.GetObjAny() as ISerializable;
        }
    }


    /// <summary>
    /// 房间战斗结束，结算报文(战斗服->游戏服)
    /// </summary>
    [MessageType(0x000AA02)]
    public class RoomFinishInfoB2R : IExternalizable
    {
        /// <summary>
        /// 游戏编辑器对应的Xml场景ID
        /// </summary>
        public int SceneID;
        /// <summary>
        /// 胜利方
        /// </summary>
        public byte WinnerForce;
        /// <summary>
        /// 自定义消息
        /// </summary>
        public string Message;

        /// <summary>
        /// 游戏总共进行时间
        /// </summary>
        public int TotalTimeSEC;

        /// <summary>
        /// 玩家所有统计信息
        /// </summary>
        public List<PlayerResult> PlayerResults = new List<PlayerResult>();

        /// <summary>
        /// 战斗记录
        /// </summary>
        public byte[] BinLog;

        public void WriteExternal(IOutputStream output)
        {
            output.PutS32(this.SceneID);
            output.PutU8(this.WinnerForce);
            output.PutUTF(this.Message);
            output.PutS32(this.TotalTimeSEC);
            output.PutS32(PlayerResults.Count);
            int count = PlayerResults.Count;
            for (int i = 0; i < count; i++)
            {
                PlayerResult pr = PlayerResults[i];
                pr.WriteExternal(output);
            }
            output.PutBytes(BinLog);
        }

        public void ReadExternal(IInputStream input)
        {
            this.SceneID = input.GetS32();
            this.WinnerForce = input.GetU8();
            this.Message = input.GetUTF();
            this.TotalTimeSEC = input.GetS32();
            int count = input.GetS32();
            for (int i = 0; i < count; i++)
            {
                PlayerResult pr = new PlayerResult();
                pr.ReadExternal(input);
                PlayerResults.Add(pr);
            }
            this.BinLog = input.GetBytes();
        }


        /// <summary>
        /// 玩家战斗结果
        /// </summary>
        public class PlayerResult : IExternalizable
        {
            /// <summary>
            /// 玩家唯一标识
            /// </summary>
            public string PlayerUUID;

            /// <summary>
            /// 场景中的ID
            /// </summary>
            public uint ZoneObjectID;

            /// <summary>
            /// 是否胜利
            /// </summary>
            public bool IsWin;

            /// <summary>
            /// 此次战斗的阵营
            /// </summary>
            public byte Force;

            /// <summary>
            /// 获取的金币数量
            /// </summary>
            public int TotalTakeMoney;

            /// <summary>
            /// 死亡次数
            /// </summary>
            public int DeadCount;

            /// <summary>
            /// 承受伤害
            /// </summary>
            public long SelfDamage;

            /// <summary>
            /// 收获的所有道具 编辑器道具ID[]
            /// </summary>
            public int[] ItemTemplates;

            /// <summary>
            /// 总共杀死单位数量
            /// </summary>
            public int KillUnitCount;
            /// <summary>
            /// 总共杀敌人/小兵数量
            /// </summary>
            public int KillEnemyCount;
            /// <summary>
            /// 总共杀野怪数量
            /// </summary>
            public int KillNeutralityCount;
            /// <summary>
            /// 总共杀死建筑数量
            /// </summary>
            public int KillBuildingCount;

            /// <summary>
            /// 总共杀死玩家数量
            /// </summary>
            public int KillPlayerCount;

            /// <summary>
            /// 杀死精英怪物 编辑器单位ID[]
            /// </summary>
            public int[] KilledEllitMonsters;

            /// <summary>
            /// 杀死的玩家 玩家UUID[]
            /// </summary>
            public string[] KillPlayers;

            /// <summary>
            /// 对怪物造成的总伤害
            /// </summary>
            public long MonsterDamage;

            /// <summary>
            /// 对玩家造成的总伤害
            /// </summary>
            public long PlayerDamage;


            public void WriteExternal(IOutputStream output)
            {
                output.PutUTF(PlayerUUID);
                output.PutU32(ZoneObjectID);
                output.PutBool(IsWin);
                output.PutU8(Force);
                output.PutS32(TotalTakeMoney);
                output.PutS32(DeadCount);
                output.PutS64(SelfDamage);
                output.PutArray(ItemTemplates, static (output, v) => output.PutS32(v));
                output.PutS32(KillUnitCount);
                output.PutS32(KillEnemyCount);
                output.PutS32(KillNeutralityCount);
                output.PutS32(KillBuildingCount);
                output.PutS32(KillPlayerCount);
                output.PutArray(KilledEllitMonsters, static (output, v) => output.PutS32(v));
                output.PutArray(KillPlayers, static (output, v) => output.PutUTF(v));
                output.PutS64(MonsterDamage);
                output.PutS64(PlayerDamage);
            }

            public void ReadExternal(IInputStream input)
            {
                PlayerUUID = input.GetUTF();
                ZoneObjectID = input.GetU32();
                IsWin = input.GetBool();
                Force = input.GetU8();
                TotalTakeMoney = input.GetS32();
                DeadCount = input.GetS32();
                SelfDamage = input.GetS64();
                ItemTemplates = input.GetArray<int>(static input => input.GetS32());
                KillUnitCount = input.GetS32();
                KillEnemyCount = input.GetS32();
                KillNeutralityCount = input.GetS32();
                KillBuildingCount = input.GetS32();
                KillPlayerCount = input.GetS32();
                KilledEllitMonsters = input.GetArray<int>(static input => input.GetS32());
                KillPlayers = input.GetUTFArray();
                MonsterDamage = input.GetS64();
                PlayerDamage = input.GetS64();
            }
        }

    }




    /// <summary>
    /// 通知玩家杀死怪物 (战斗服->游戏服)
    /// </summary>
    [MessageType(0x000AA03)]
    public class UnitKillMonsterB2R : IExternalizable
    {
        public string PlayerUUID;
        public int MonsterId;

        virtual public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(this.PlayerUUID);
            output.PutS32(this.MonsterId);
        }
        virtual public void ReadExternal(IInputStream input)
        {
            this.PlayerUUID = input.GetUTF();
            this.MonsterId = input.GetS32();
        }
    }

    /// <summary>
    /// 通知获得道具  (战斗服->游戏服)
    /// </summary>
    [MessageType(0x000AA04)]
    public class UnitGotItemB2R : IExternalizable
    {
        public string PlayerUUID;
        public int ItemTemplateID;
        public int Count;

        virtual public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(PlayerUUID);
            output.PutS32(ItemTemplateID);
            output.PutS32(Count);
        }
        virtual public void ReadExternal(IInputStream input)
        {
            PlayerUUID = input.GetUTF();
            ItemTemplateID = input.GetS32();
            Count = input.GetS32();
        }
    }

    /// <summary>
    /// 通知获得金币 (战斗服->游戏服)
    /// </summary>
    [MessageType(0x000AA05)]
    public class UnitGotMoneyB2R : IExternalizable
    {
        public string PlayerUUID;
        public int Money;

        virtual public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(this.PlayerUUID);
            output.PutS32(this.Money);
        }
        virtual public void ReadExternal(IInputStream input)
        {
            this.PlayerUUID = input.GetUTF();
            this.Money = input.GetS32();
        }
    }

    /// <summary>
    /// 客户端请求使用游戏币，(战斗服->游戏服)
    /// [Request的MessageID和Response的MessageID必须相等]
    /// </summary>
    [MessageType(0x000AA06)]
    public class ClientCostRequestB2R : NetMessage
    {
        /// <summary>
        /// 玩家标识
        /// </summary>
        public string PlayerUUID;
        /// <summary>
        /// 消耗付费游戏币
        /// </summary>
        public int CostDiamond;
        /// <summary>
        /// 消耗产出金币
        /// </summary>
        public int CostGold;
        /// <summary>
        /// 消耗道具
        /// </summary>
        public int ItemTemplateID;
        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(base.MessageID);
            output.PutUTF(this.PlayerUUID);
            output.PutS32(this.CostDiamond);
            output.PutS32(this.CostGold);
            output.PutS32(this.ItemTemplateID);
        }

        public override void ReadExternal(IInputStream input)
        {
            base.MessageID = input.GetS32();
            this.PlayerUUID = input.GetUTF();
            this.CostDiamond = input.GetS32();
            this.CostGold = input.GetS32();
            this.ItemTemplateID = input.GetS32();
        }
    }

    /// <summary>
    /// 客户端请求使用游戏币回馈，(游戏服->战斗服)
    /// [Request的MessageID和Response的MessageID必须相等]
    /// </summary>
    [MessageType(0x000AA07)]
    public class ClientCostResponseR2B : NetMessage
    {
        public const byte RESULT_OK = 1;
        public const byte RESULT_FAILED = 2;
        public byte Result = 0;
        public string Reason = "";

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(base.MessageID);
            output.PutU8(this.Result);
            output.PutUTF(this.Reason);
        }

        public override void ReadExternal(IInputStream input)
        {
            base.MessageID = input.GetS32();
            this.Result = input.GetU8();
            this.Reason = input.GetUTF();
        }
    }

    /// <summary>
    /// 使用背包道具 (战斗服->游戏服)
    /// </summary>
    [MessageType(0x000AA08)]
    public class UnitUseItemB2R : IExternalizable
    {
        /// <summary>
        /// 玩家标识
        /// </summary>
        public string PlayerUUID;
        public int ItemTemplateID;

        virtual public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(PlayerUUID);
            output.PutS32(ItemTemplateID);
        }
        virtual public void ReadExternal(IInputStream input)
        {
            PlayerUUID = input.GetUTF();
            ItemTemplateID = input.GetS32();
        }
    }

    /// <summary>
    /// 游戏服关闭房间
    /// </summary>
    [MessageType(0x000AA09)]
    public class CloseRoomNotifyR2B : NetMessage
    {
        /// <summary>
        /// 关闭消息类型
        /// </summary>
        public int Result;
        /// <summary>
        /// 关闭消息
        /// </summary>
        public string Message;
        /// <summary>
        /// 关闭倒计时
        /// </summary>
        public int TimeDelayMS;
        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(base.MessageID);
            output.PutS32(this.Result);
            output.PutUTF(this.Message);
            output.PutS32(this.TimeDelayMS);
        }

        public override void ReadExternal(IInputStream input)
        {
            base.MessageID = input.GetS32();
            this.Result = input.GetS32();
            this.Message = input.GetUTF();
            this.TimeDelayMS = input.GetS32();
        }
    }

    /// <summary>
    /// 游戏服向战斗服发送
    /// </summary>
    [MessageType(0x000AA0A)]
    public class SendMessageR2B : NetMessage
    {
        public string Message;
        public override void WriteExternal(IOutputStream output)
        {
            output.PutUTF(this.Message);
        }
        public override void ReadExternal(IInputStream input)
        {
            this.Message = input.GetUTF();
        }
    }

    /// <summary>
    /// 战斗服向游戏服发送
    /// </summary>
    [MessageType(0x000AA0B)]
    public class SendMessageB2R : NetMessage
    {
        public string Message;
        public override void WriteExternal(IOutputStream output)
        {
            output.PutUTF(this.Message);
        }
        public override void ReadExternal(IInputStream input)
        {
            this.Message = input.GetUTF();
        }
    }

    /// <summary>
    /// 战斗服向游戏服发送自定义事件.
    /// </summary>
    [MessageType(0x000AA0C)]
    public class SendEventB2R : NetMessage
    {
        public IExternalizable evt;
        public override void WriteExternal(IOutputStream output)
        {
            output.PutExt(this.evt);
        }
        public override void ReadExternal(IInputStream input)
        {
            evt = input.GetExtAny();
        }
    }



}
