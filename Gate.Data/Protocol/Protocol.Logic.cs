using DeepCore.IO;
using DeepCore.Protocol;
using System;
using System.Collections.Generic;

namespace Gate.Data.Protocol
{

    public interface ILogicProtocol { }

    [MessageType(Constants.MSG_START + 0x301)]
    public class ClientPing : Request, ILogicProtocol
    {
        public DateTime time = DateTime.Now;
        public byte[] rawdata;
    }
    [MessageType(Constants.MSG_START + 0x302)]
    public class ClientPong : Response, ILogicProtocol
    {
        public DateTime time;
        public byte[] rawdata;
    }
    [MessageType(Constants.MSG_START + 0x303)]
    public class LogicTimeNotify : Response, ILogicProtocol
    {
        public int index;
        public DateTime time;
    }

    //--------------------------------------------------------------------------------

    /// <summary>
    /// 通知客户端角色信息变更
    /// </summary>
    [MessageType(Constants.MSG_START + 0x304)]
    public class PlayerDynamicNotify : Notify, ILogicProtocol, INetProtocolS2C
    {
        public List<PropertyStruct> s2c_data;
    }


}
