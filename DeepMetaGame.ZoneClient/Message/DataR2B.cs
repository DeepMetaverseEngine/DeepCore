using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Reflection;
using System.Collections.Generic;

namespace DeepMetaGame.ZoneServer.Message
{
    /// <summary>
    /// 房间状态信息
    /// </summary>
    [MessageType(0x000AC10)]
    public class RoomInfo : IExternalizable
    {
        /// <summary>
        /// 当前房间是哪个GameServer
        /// </summary>
        public string GameServerID;
        /// <summary>
        /// 房间ID
        /// </summary>
        public string RoomID;
        /// <summary>
        /// 链接套接字
        /// </summary>
        public string ClientConnectString;
        /// <summary>
        /// 
        /// </summary>
        public int Dummy;

        /// <summary>
        /// 房间状态
        /// </summary>
        public int Status;

        /// <summary>
        /// 当前玩家数量
        /// </summary>
        public int PlayerCount;
        /// <summary>
        /// 最大玩家数量
        /// </summary>
        public int PlayerMax;

        /// <summary>
        /// 房间运行了多久
        /// </summary>
        public int TotalTimeSEC;
        /// <summary>
        /// 如果是副本关卡，传出副本关卡进度
        /// </summary>
        public float Progress;

        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(GameServerID);
            output.PutUTF(RoomID);

            output.PutUTF(ClientConnectString);
            output.PutS32(Dummy);

            output.PutS32(Status);

            output.PutS32(PlayerCount);
            output.PutS32(PlayerMax);

            output.PutS32(TotalTimeSEC);
            output.PutF32(Progress);
        }

        public void ReadExternal(IInputStream input)
        {
            GameServerID = input.GetUTF();
            RoomID = input.GetUTF();

            ClientConnectString = input.GetUTF();
            Dummy = input.GetS32();

            Status = input.GetS32();

            PlayerCount = input.GetS32();
            PlayerMax = input.GetS32();

            TotalTimeSEC = input.GetS32();
            Progress = input.GetF32();

        }
    }

    //------------------------------------CreateRoom---------------------------------//

    /// <summary>
    /// 战斗管理器创建房间
    /// </summary>
    [MessageType(0x000AC00)]
    [Desc("创建房间")]
    public class CreateRoomRequestR2B : NetMessage
    {
        public string GameServerID;

        /// <summary>
        /// 由房间管理器分配的房间ID
        /// </summary>
        public string RoomID;

        /// <summary>
        /// 扩展属性(用于游戏逻辑)
        /// </summary>
        public CreateRoomInfoR2B Data;

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(GameServerID);
            output.PutUTF(RoomID);
            output.PutBytes(IOUtil.ObjectToBin(output.Factory, Data));
        }

        public override void ReadExternal(IInputStream input)
        {
            MessageID = input.GetS32();
            GameServerID = input.GetUTF();
            RoomID = input.GetUTF();
            Data = (CreateRoomInfoR2B)IOUtil.BinToObjectAny(input.Factory, input.GetBytes());
        }

    }

    /// <summary>
    /// 战斗管理器创建房间
    /// </summary>
    [MessageType(0x000AC01)]
    public class CreateRoomResponseB2R : NetMessage
    {
        public const byte RESULT_OK = 1;
        public const byte RESULT_ROOM_NORMAL = 2;
        public const byte RESULT_ROOM_EXISTS = 3;
        public const byte RESULT_ERROR = 4;

        public string GameServerID;
        public byte Result;
        public RoomInfo Room;

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(GameServerID);
            output.PutU8(Result);
            if (RESULT_OK == Result)
            {
                output.PutExt(Room);
            }
        }

        public override void ReadExternal(IInputStream input)
        {
            MessageID = input.GetS32();
            GameServerID = input.GetUTF();
            Result = input.GetU8();
            if (RESULT_OK == Result)
            {
                Room = input.GetExt<RoomInfo>();
            }
        }


    }

    //------------------------------------PlayerWillConnect---------------------------------//

    /// <summary>
    /// 玩家将要进入房间服务
    /// 此时，游戏服给予战斗服此玩家对应的Token用于连接上来的玩家的信息验证
    /// </summary>
    [MessageType(0x000AC02)]
    public class PlayerWillConnectRequestR2B : NetMessage
    {
        public string GameServerID;
        /// <summary>
        /// 玩家全局唯一ID
        /// </summary>
        public string PlayerUUID;
        /// <summary>
        /// 玩家显示名字
        /// </summary>
        public string PlayerDisplayName;
        /// <summary>
        /// Token验证串
        /// </summary>
        public string Token;
        /// <summary>
        /// Token有效时间，秒
        /// </summary>
        public int TokenValidTimeSec;
        /// <summary>
        /// 玩家将要进入的房间ID
        /// </summary>
        public string RoomID;

        /// <summary>
        /// 扩展属性(用于游戏逻辑)
        /// </summary>
        public CreateUnitInfoR2B Data;

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(GameServerID);
            output.PutUTF(PlayerUUID);
            output.PutUTF(PlayerDisplayName);
            output.PutUTF(Token);
            output.PutS32(TokenValidTimeSec);
            output.PutUTF(RoomID);
            output.PutBytes(IOUtil.ObjectToBin(output.Factory, Data));
        }

        public override void ReadExternal(IInputStream input)
        {
            MessageID = input.GetS32();
            GameServerID = input.GetUTF();
            PlayerUUID = input.GetUTF();
            PlayerDisplayName = input.GetUTF();
            Token = input.GetUTF();
            TokenValidTimeSec = input.GetS32();
            RoomID = input.GetUTF();
            Data = (CreateUnitInfoR2B)IOUtil.BinToObjectAny(input.Factory, input.GetBytes());
        }
    }

    /// <summary>
    /// 玩家将要进入房间，
    /// 给当前玩家分配的房间ID
    /// </summary>
    [MessageType(0x000AC03)]
    public class PlayerWillConnectResponseB2R : NetMessage
    {
        public const int RESULT_OK = 1;
        public const int RESULT_ROOM_NOT_EXIST = 2;
        public const int RESULT_ROOM_OVER_LOAD = 3;

        public string GameServerID;
        public byte Result;

        /// <summary>
        /// 玩家全局唯一ID
        /// </summary>
        public string PlayerUUID;

        public string Token;
        /// <summary>
        /// 回馈给玩家的房间以及服务器信息
        /// </summary>
        public RoomInfo Room;

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(GameServerID);
            output.PutU8(Result);
            output.PutUTF(PlayerUUID);
            output.PutUTF(Token);
            if (Result == RESULT_OK)
            {
                output.PutExt(Room);
            }
        }

        public override void ReadExternal(IInputStream input)
        {
            MessageID = input.GetS32();
            GameServerID = input.GetUTF();
            Result = input.GetU8();
            PlayerUUID = input.GetUTF();
            Token = input.GetUTF();
            if (Result == RESULT_OK)
            {
                Room = input.GetExt<RoomInfo>();
            }
        }
    }



    //------------------------------------BattleServer至GameServer的协议---------------------------------//

    /// <summary>
    /// 玩家已经离开战斗服务器，实际上此时，Token已经无效
    /// </summary>
    [MessageType(0x000AC04)]
    public class PlayerEnterRoomNotifyB2R : NetMessage
    {
        public string GameServerID;
        public string PlayerUUID;
        public string RoomID;

        public PlayerEnterRoomNotifyB2R() { }
        public PlayerEnterRoomNotifyB2R(string gameServerID, string playerUUID, string roomID)
        {
            this.GameServerID = gameServerID;
            this.PlayerUUID = playerUUID;
            this.RoomID = roomID;
        }

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(GameServerID);
            output.PutUTF(PlayerUUID);
            output.PutUTF(RoomID);
        }

        public override void ReadExternal(IInputStream input)
        {
            MessageID = input.GetS32();
            GameServerID = input.GetUTF();
            PlayerUUID = input.GetUTF();
            RoomID = input.GetUTF();
        }
    }

    /// <summary>
    /// 玩家已经离开战斗服务器，实际上此时，Token已经无效
    /// </summary>
    [MessageType(0x000AC05)]
    public class PlayerLeaveBattleNotifyB2R : NetMessage
    {
        public string GameServerID;
        public string PlayerUUID;
        public string RoomID;

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(GameServerID);
            output.PutUTF(PlayerUUID);
            output.PutUTF(RoomID);
        }

        public override void ReadExternal(IInputStream input)
        {
            MessageID = input.GetS32();
            GameServerID = input.GetUTF();
            PlayerUUID = input.GetUTF();
            RoomID = input.GetUTF();
        }
    }

    //------------------------------------BattleServer至GameServer的协议---------------------------------//

    /// <summary>
    /// 服务器心跳
    /// </summary>
    [MessageType(0x000AC07)]
    public class ServerInfoNotifyB2R : NetMessage
    {
        /// <summary>
        /// 客户端连接套接字
        /// </summary>
        public string ClientConnectString;
        /// <summary>
        /// 
        /// </summary>
        public int Dummy;
        /// <summary>
        /// 当前已创建房间数量
        /// </summary>
        public int CurRoomCount;
        /// <summary>
        /// 服务器最大承载房间数量
        /// </summary>
        public int MaxRoomCount;
        /// <summary>
        /// 所有房间信息
        /// </summary>
        public List<RoomInfo> AllRooms;

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(ClientConnectString);
            output.PutS32(Dummy);
            output.PutS32(CurRoomCount);
            output.PutS32(MaxRoomCount);
            output.PutList(AllRooms, static (output, v) => output.PutExt(v));
        }

        public override void ReadExternal(IInputStream input)
        {
            MessageID = input.GetS32();
            ClientConnectString = input.GetUTF();
            Dummy = input.GetS32();
            CurRoomCount = input.GetS32();
            MaxRoomCount = input.GetS32();
            AllRooms = input.GetList<RoomInfo>(static input => input.GetExt<RoomInfo>());
        }
    }

    /// <summary>
    /// 房间状态同步
    /// </summary>
    [MessageType(0x000AC09)]
    public class RoomInfoNotifyB2R : NetMessage
    {
        public RoomInfo Room;

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutExt(Room);
        }

        public override void ReadExternal(IInputStream input)
        {
            MessageID = input.GetS32();
            Room = input.GetExt<RoomInfo>();
        }
    }

    /// <summary>
    /// 房间销毁通知
    /// </summary>
    [MessageType(0x000AC0b)]
    public class DestroyRoomNotifyB2R : NetMessage
    {
        public string GameServerID;
        public string RoomID;

        override public void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(GameServerID);
            output.PutUTF(RoomID);
        }

        override public void ReadExternal(IInputStream input)
        {
            MessageID = input.GetS32();
            GameServerID = input.GetUTF();
            RoomID = input.GetUTF();
        }
    }

    //------------------------------------BattleServer至GameServer的协议---------------------------------//

    /// <summary>
    /// 一些扩展数据 战斗服->角色服
    /// 比如任务信息同步，场景内触发的事件，等等
    /// </summary>
    [MessageType(0x0008A0d)]
    public class RoomEventNotifyB2R : NetMessage
    {
        public string GameServerID;
        public string RoomID;
        public ISerializable ExtData;

        override public void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(GameServerID);
            output.PutUTF(RoomID);
            byte[] bin = IOUtil.ObjectToBin(output.Factory, ExtData);
            output.PutBytes(bin);
        }

        override public void ReadExternal(IInputStream input)
        {
            this.MessageID = input.GetS32();
            this.GameServerID = input.GetUTF();
            this.RoomID = input.GetUTF();
            byte[] bin = input.GetBytes();
            this.ExtData = IOUtil.BinToObject<ISerializable>(input.Factory, bin);
        }

    }

    /// <summary>
    /// 一些扩展数据 角色服->战斗服
    /// 比如任务信息同步，场景内触发的事件，等等
    /// </summary>
    [MessageType(0x0000AC0e)]
    public class RoomEventNotifyR2B : NetMessage
    {
        public string GameServerID;
        public string RoomID;
        public ISerializable ExtData;

        override public void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(GameServerID);
            output.PutUTF(RoomID);
            byte[] bin = IOUtil.ObjectToBin(output.Factory, ExtData);
            output.PutBytes(bin);
        }

        override public void ReadExternal(IInputStream input)
        {
            this.MessageID = input.GetS32();
            this.GameServerID = input.GetUTF();
            this.RoomID = input.GetUTF();
            byte[] bin = input.GetBytes();
            this.ExtData = IOUtil.BinToObject<ISerializable>(input.Factory, bin);
        }
    }


    [MessageType(0x000AC0f)]
    public class KickPlayerR2B : NetMessage
    {
        public string GameServerID;
        public string PlayerUUID;

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(GameServerID);
            output.PutUTF(PlayerUUID);
        }

        public override void ReadExternal(IInputStream input)
        {
            MessageID = input.GetS32();
            GameServerID = input.GetUTF();
            PlayerUUID = input.GetUTF();
        }

    }
}
