using DeepCore;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace Gate.Data.Protocol
{
    //---------------------------------------------------------------------------------------

    public interface IConnectProtocol { }
    /// <summary>
    /// 链接Connect
    /// </summary>
    [MessageType(Constants.MSG_START + 0x101)]
    public class ClientEnterServerRequest : Request, IConnectProtocol, INetProtocolBotIgnore
    {
        public string c2s_account;
        public string c2s_gate_token;
        public string c2s_login_token;
        public string c2s_session_token;
        public DateTime c2s_time;
    }
    [MessageType(Constants.MSG_START + 0x102)]
    public class ClientEnterServerResponse : Response, IConnectProtocol, INetProtocolBotIgnore
    {
        [DependOnProperty(nameof(IsSuccess))]
        public string s2c_session_token;
    }

    //--------------------------------------------------------------------------------
    /// <summary>
    /// 重新连接Connect
    /// </summary>
    [MessageType(Constants.MSG_START + 0x103)]
    public class ClientReconnectServerRequest : Request, IConnectProtocol
    {
        public string c2s_account;
        public string c2s_token;
        public int c2s_logicServerID;
    }
    [MessageType(Constants.MSG_START + 0x104)]
    public class ClientReconnectServerResponse : Response, IConnectProtocol
    {
    }

    //---------------------------------------------------------------------------------------

}
