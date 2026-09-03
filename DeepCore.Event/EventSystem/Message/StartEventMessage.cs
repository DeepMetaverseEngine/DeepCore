using System;
using DeepCore.IO;

namespace DeepCore.Event.EventSystem.Message
{
    public class StartEventMessage : EventMessage
    {
        public string EventDesc;
        public bool IsStartEvent;
        public string MessageID;
        public UnionValue Argument;

        public StartEventMessage()
        {
            MessageID = Guid.NewGuid().ToString();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(MessageID);
            output.PutUTF(EventDesc);
            output.PutBool(IsStartEvent);
            UnionValueSerializer.WriteToStream(output, Argument);
        }

        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            MessageID = input.GetUTF();
            EventDesc = input.GetUTF();
            IsStartEvent = input.GetBool();
            Argument = UnionValueSerializer.ReadFromStream(input);
        }

        public override string ToString()
        {
            return $"{base.ToString()} {EventDesc} {Argument}";
        }
    }


    public class ExceptionStopEventMessage : EventMessage
    {
        public string MessageID;
        public string ResultReason;

        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(MessageID);
            output.PutUTF(ResultReason);
        }

        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            MessageID = input.GetUTF();
            ResultReason = input.GetUTF();
        }
    }
}