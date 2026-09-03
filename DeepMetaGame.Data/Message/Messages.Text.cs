using DeepCore;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Xml;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.IO;

namespace DeepMetaGame.Data.Message
{
    public enum ChatMessageType : byte
    {
        SystemToAll,
        SystemToForce,
        SystemToPlayer,

        PlayerToAll,
        PlayerToForce,
        PlayerToPlayer,
    }
    [MessageType(BattleConstants.TextMessage)]
    public class TextMessage : BattleAction
    {
        public string Message;
        protected override void OnDisposing()
        {
            Message = null;
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutUTF(Message);
        }
        public override void ReadExternal(IInputStream input)
        {
            Message = input.GetUTF();
        }
    }
    [MessageType(BattleConstants.ChatAction)]
    public class ChatAction : ObjectAction
    {
        public string Message;
        public ChatMessageType To = ChatMessageType.SystemToAll;
        public string TargetPlayerUUID;
        protected override void OnDisposing(uint objID)
        {
            Message = default;
            To = default;
            TargetPlayerUUID = default;
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutU8((byte)To);
            output.PutUTF(Message);
            output.PutUTF(TargetPlayerUUID);
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            To = (ChatMessageType)input.GetU8();
            Message = input.GetUTF();
            TargetPlayerUUID = input.GetUTF();
        }
    }

    [MessageType(BattleConstants.ChatNotify)]
    public class ChatNotify : ZoneNotify
    {
        public ChatMessageType To = ChatMessageType.SystemToAll;
        public byte Force;
        public string FromPlayerUUID;
        public string ToPlayerUUID;
        public string Message;
        protected override void OnDisposing()
        {
            To = default;
            Force = default;
            FromPlayerUUID = default;
            ToPlayerUUID = default;
            Message = default;
        }
        public ChatNotify() { }
        public ChatNotify Init(ChatMessageType to)
        {
            To = to;
            return this;
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutU8((byte)To);
            output.PutUTF(Message);
            switch (To)
            {
                case ChatMessageType.PlayerToAll:
                    output.PutUTF(FromPlayerUUID);
                    break;
                case ChatMessageType.PlayerToForce:
                    output.PutUTF(FromPlayerUUID);
                    output.PutU8(Force);
                    break;
                case ChatMessageType.PlayerToPlayer:
                    output.PutUTF(FromPlayerUUID);
                    output.PutUTF(ToPlayerUUID);
                    break;
                case ChatMessageType.SystemToAll:
                    break;
                case ChatMessageType.SystemToForce:
                    output.PutU8(Force);
                    break;
                case ChatMessageType.SystemToPlayer:
                    output.PutUTF(ToPlayerUUID);
                    break;
            }
        }
        public override void ReadExternal(IInputStream input)
        {
            To = (ChatMessageType)input.GetU8();
            Message = input.GetUTF();
            switch (To)
            {
                case ChatMessageType.PlayerToAll:
                    FromPlayerUUID = input.GetUTF();
                    break;
                case ChatMessageType.PlayerToForce:
                    FromPlayerUUID = input.GetUTF();
                    Force = input.GetU8();
                    break;
                case ChatMessageType.PlayerToPlayer:
                    FromPlayerUUID = input.GetUTF();
                    ToPlayerUUID = input.GetUTF();
                    break;
                case ChatMessageType.SystemToAll:
                    break;
                case ChatMessageType.SystemToForce:
                    Force = input.GetU8();
                    break;
                case ChatMessageType.SystemToPlayer:
                    ToPlayerUUID = input.GetUTF();
                    break;
            }
        }
    }

    [MessageType(BattleConstants.BubbleTalkNotify)]
    public class BubbleTalkNotify : ZoneNotify
    {
        public bool PauseBattle;
        public readonly List<TalkInfo> TalkInfos = new List<TalkInfo>();

        protected override void OnDisposing()
        {
            PauseBattle = default;
            TalkInfos.Clear();
        }
        public BubbleTalkNotify() { }

        override public void WriteExternal(IOutputStream output)
        {
            output.PutBool(PauseBattle);
            output.PutExtListNoHead(TalkInfos);
        }
        override public void ReadExternal(IInputStream input)
        {
            PauseBattle = input.GetBool();
            input.GetExtListNoHead<TalkInfo>(TalkInfos);
        }

        public class TalkInfo : IReadExternalizable, IWriteExternalizable
        {
            public uint TalkUnit;
            public string TalkContent;
            public string TalkActionType;
            public int TalkDelayTimeMS;
            public int TalkKeepTimeMS = 1000;

            public TalkInfo() { }

            public TalkInfo(uint TalkUnit, string TalkContent, string TalkActionType, int TalkDelayTimeMS, int TalkKeepTimeMS)
            {
                this.TalkUnit = TalkUnit;
                this.TalkContent = TalkContent;
                this.TalkActionType = TalkActionType;
                this.TalkDelayTimeMS = TalkDelayTimeMS;
                this.TalkKeepTimeMS = TalkKeepTimeMS;
            }

            public void WriteExternal(IOutputStream output)
            {
                output.PutVU32(TalkUnit);
                output.PutUTF(TalkContent);
                output.PutUTF(TalkActionType);
                output.PutVS32(TalkDelayTimeMS);
                output.PutVS32(TalkKeepTimeMS);
            }

            public void ReadExternal(IInputStream input)
            {
                TalkUnit = input.GetVU32();
                TalkContent = input.GetUTF();
                TalkActionType = input.GetUTF();
                TalkDelayTimeMS = input.GetVS32();
                TalkKeepTimeMS = input.GetVS32();
            }
        }
    }

}

