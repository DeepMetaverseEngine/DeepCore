using DeepCore.IO;

namespace DeepCore.Event.EventSystem.Message
{
    public class PingPongMessage : TargetEventMessage
    {
        public override string ToString()
        {
            return base.ToString() + " " + "PingPong";
        }
    }
}
