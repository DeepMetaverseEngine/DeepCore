using System.Runtime.Serialization;

namespace DeepCore.Event.EventSystem
{
    /// <summary>
    /// 创建一个事件的数据
    /// </summary>
    public class EventMeta : ISerializable
    {
        public string Desc;
        public string To;
        public string Flag;
        public UnionValue Argument;
        public EventMeta[] SubEvents;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new System.NotImplementedException();
        }
    }


}
