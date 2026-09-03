using DeepCore.IO;
using DeepCore.Protocol;

namespace DeepMetaGame.ZoneServer.Message
{
    /// <summary>
    /// 客户端进入房间请求
    /// </summary>
    [MessageType(0x000AB00)]
    public class EnterRoomRequestC2B : NetMessage 
    {
        public string PlayerUUID;
        public string Token;
        public string RoomID;
        /// <summary>
        /// 测试数据，模拟战斗管理服报文
        /// </summary>
        public PlayerWillConnectRequestR2B TestToken;
        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutUTF(PlayerUUID);
            output.PutUTF(Token);
            output.PutUTF(RoomID);
            output.PutExt(TestToken);
        }

        public override void ReadExternal(IInputStream input)
        {
            this.MessageID = input.GetS32();
            this.PlayerUUID = input.GetUTF();
            this.Token = input.GetUTF();
            this.RoomID = input.GetUTF();
            this.TestToken = input.GetExt<PlayerWillConnectRequestR2B>();
        }
    }

    /// <summary>
    /// 客户端进入房间回馈
    /// </summary>
    [MessageType(0x000AB01)]
    public class EnterRoomResponseB2C : NetMessage
    {
        public const byte RESULT_OK = 1;
        public const byte RESULT_FAILE = 2;

        public byte Result;

        /// <summary>
        /// 创建房间的扩展属性(用于游戏逻辑)
        /// </summary>
        public CreateRoomInfoR2B RoomData;

        /// <summary>
        /// 角色的扩展属性(用于游戏逻辑)
        /// </summary>
        public CreateUnitInfoR2B PlayerData;

        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(MessageID);
            output.PutU8(Result);
            if (Result == RESULT_OK)
            {
                output.PutExt(RoomData);
                output.PutExt(PlayerData);
            }
        }

        public override void ReadExternal(IInputStream input)
        {
            this.MessageID = input.GetS32();
            this.Result = input.GetU8();
            if (Result == RESULT_OK)
            {
                this.RoomData = (CreateRoomInfoR2B)input.GetExtAny();
                this.PlayerData = (CreateUnitInfoR2B)input.GetExtAny();
            }
        }

    }

    /// <summary>
    /// 客户端离开房间请求
    /// </summary>
    [MessageType(0x000AB02)]
    public class LeaveRoomRequestC2B : NetMessage
    {
        public override void WriteExternal(IOutputStream output)
        {
        }

        public override void ReadExternal(IInputStream input)
        {
        }
    }

    /// <summary>
    /// 被服务端踢掉
    /// </summary>
    [MessageType(0x000AB03)]
    public class KickedByServerNotifyB2C : NetMessage
    {
        public string message;
        public override void WriteExternal(IOutputStream output)
        {
            output.PutUTF(message);
        }

        public override void ReadExternal(IInputStream input)
        {
            this.message = input.GetUTF();
        }
    }






}
