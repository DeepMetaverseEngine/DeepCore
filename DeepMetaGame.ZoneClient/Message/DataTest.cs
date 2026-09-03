using DeepCore.IO;
using DeepCore.Protocol;
using System.Collections.Generic;

namespace DeepMetaGame.ZoneServer.Message
{
    [MessageType(0x000AD00)]
    public class RoomDesc : NetMessage
    {
        public RoomInfo Info;
        public CreateRoomRequestR2B Data;
        public override void WriteExternal(DeepCore.IO.IOutputStream output)
        {
            output.PutS32(this.MessageID);
            output.PutExt(this.Info);
            output.PutExt(this.Data);
        }
        public override void ReadExternal(DeepCore.IO.IInputStream input)
        {
            this.MessageID = input.GetS32();
            this.Info = input.GetExt<RoomInfo>();
            this.Data = input.GetExt<CreateRoomRequestR2B>();
        }
    }

    [MessageType(0x000AD01)]
    public class QueryAllRoomDesc : NetMessage
    {
        public override void WriteExternal(DeepCore.IO.IOutputStream output)
        {
            output.PutS32(this.MessageID);
        }
        public override void ReadExternal(DeepCore.IO.IInputStream input)
        {
            this.MessageID = input.GetS32();
        }
    }

    [MessageType(0x000AD02)]
    public class AllRoomDesc : NetMessage
    {
        public List<RoomDesc> Rooms = new List<RoomDesc>();
        public override void WriteExternal(DeepCore.IO.IOutputStream output)
        {
            output.PutS32(this.MessageID);
            output.PutList(this.Rooms, static (output, v) => output.PutExt(v));
        }
        public override void ReadExternal(DeepCore.IO.IInputStream input)
        {
            this.MessageID = input.GetS32();
            this.Rooms = input.GetList<RoomDesc>(static input=>input.GetExt<RoomDesc>());
        }
    }

    [MessageType(0x000AD03)]
    public class EchoTextCommand : NetMessage
    {
        public string Command;
        public EchoTextCommand() { }
        public EchoTextCommand(string command)
        {
            this.Command = command;
        }
        public override void WriteExternal(DeepCore.IO.IOutputStream output)
        {
            output.PutS32(this.MessageID);
            output.PutUTF(this.Command);
        }
        public override void ReadExternal(DeepCore.IO.IInputStream input)
        {
            this.MessageID = input.GetS32();
            this.Command = input.GetUTF();
        }
        public override string ToString()
        {
            return Command + "";
        }
    }
}
