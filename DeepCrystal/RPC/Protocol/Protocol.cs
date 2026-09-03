using DeepCore.IO;
using DeepCore.Protocol;
using System;
namespace DeepCrystal.RPC.Protocol
{

    [ProtocolRoute("*", "*")]
    public class Ping : Request
    {
        public DateTime time = DateTime.Now;
        public int index;
    }
    [ProtocolRoute("*", "*")]
    public class Pong : Response
    {
        public DateTime time = DateTime.Now;
        public int index;
    }

    /// <summary>
    /// 由系统发出关闭服务器协议，收到此协议后，Connector和Gate不在处理新的链接，并且将现有所有链接下线。
    /// </summary>
    public class SystemShutdownNotify : ISerializable
    {
        public string reason;
    }
    /// <summary>
    /// 由系统发出所有静态服务已启动完毕。
    /// </summary>
    public class SystemStaticServicesStartedNotify : ISerializable
    {

    }

    //---------------------------------------------------------------------------------
}
