using DeepCore.Event.EventSystem.Events;
using DeepCore.IO;

namespace DeepCore.Event.EventSystem.Message
{
    public class SyncEventStateMessage : TargetEventMessage
    {
        public EventState State;
        public bool IsTrigger;
        public string ResultReason;
        public UnionValue Content;
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutEnum8(State);
            output.PutBool(IsTrigger);
            output.PutUTF(ResultReason);
            UnionValueSerializer.WriteToStream(output, Content);
        }

        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            State = input.GetEnum8<EventState>();
            IsTrigger = input.GetBool();
            ResultReason = input.GetUTF();
            Content = UnionValueSerializer.ReadFromStream(input);
        }

        public override string ToString()
        {
            return $"{base.ToString()} IsTrigger:{IsTrigger} {State}-{ResultReason} {Content}";
        }
    }
}
