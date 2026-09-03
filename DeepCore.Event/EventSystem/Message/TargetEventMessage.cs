using DeepCore.IO;

namespace DeepCore.Event.EventSystem.Message
{
    public abstract class TargetEventMessage : EventMessage
    {
        public int ToEvent;
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(ToEvent);
        }

        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            ToEvent = input.GetS32();
        }
    }

}
