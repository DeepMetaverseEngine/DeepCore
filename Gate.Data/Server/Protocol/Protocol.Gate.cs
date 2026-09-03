using DeepCore;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Reflection;
using Gate.Data;
using System;
using System.Collections.Generic;
using System.Text;
namespace Gate.Server.Protocol
{
    //---------------------------------------------------------------------------------
    //-------------------------------------------------------------------

    /// <summary>
    /// 连接已断开通知（只是网络断开，服务状态不变）
    /// </summary>
    public class SessionDisconnectNotify : Notify
    {
        public string sessionName;
        public string socketID;
        public string roleID;
    }

    //-------------------------------------------------------------------

    /// <summary>
    /// 用户重新连接通知（只是网络重连，服务状态不变）
    /// </summary>
    public class SessionReconnectNotify : Notify
    {
        public string sessionName;
        public string roleID;
        public HashMap<string, string> config;
    }
    //-------------------------------------------------------------------
    public class SessionBeginLeaveRequest : Request
    {
        public string sessionName;
        public string roleID;
    }
    public class SessionBeginLeaveResponse : Response
    {
    }

    //-------------------------------------------------------------------
    /// <summary>
    ///用于广播的协议，发送给所有SessionService
    /// </summary>
    [ProtocolRoute("Connector", "* -> Connector")]
    public class ConnectorBroadcastNotify : Notify
    {
        public string serverGroupID
        {
            set
            {
                if (value == null)
                {
                    serverGroups = null;
                    return;
                }

                if (serverGroups == null)
                {
                    serverGroups = new ArrayList<string>();
                }
                serverGroups.Add(value);
            }
        }
        public string sessionID
        {
            set
            {
                if (value == null)
                {
                    sessions = null;
                    return;
                }

                if (sessions == null)
                {
                    sessions = new ArrayList<string>();
                }
                sessions.Add(value);
            }
        }
        /// <summary>
        /// 可接受广播的ServerGroupID。
        /// 如果为空，则广播到所有ServerGroup。
        /// </summary>
        public List<string> serverGroups;
        /// <summary>
        /// 可接受广播的所有客户端，一般指一个频道里的人。
        /// 如果为空，则广播到所有Session。
        /// </summary>
        public List<string> sessions;
        /// <summary>
        /// 真正广播出去的协议。
        /// </summary>
        public Notify notify;
    }

    [ProtocolRoute("*", "Session")]
    public class KickPlayerNotify : Notify
    {
        public string reason;
    }
    /// <summary>
    /// 链接服通知Gate服当前状态
    /// </summary>
    [ProtocolRoute("Connect", "Gate")]
    public class SyncConnectToGateNotify : Notify
    {
        /// <summary>
        /// 服务地址
        /// </summary>
        public string connectServiceAddress;
        /// <summary>
        /// 服务IP
        /// </summary>
        public string connectAddress;
        /// <summary>
        /// 给客户端的Token
        /// </summary>
        public string connectToken;

        /// <summary>
        /// 已连接客户端数量
        /// </summary>
        public int clientNumber;

        /// <summary>
        /// 每个Group玩家数
        /// </summary>
        public HashMap<string, int> groupClientNumbers;
    }


    /// <summary>
    /// 通知Gate服务器开启.
    /// </summary>
    [ProtocolRoute("AdminServer", "Gate")]
    public class SyncGateServerOpen : Notify
    {
        public bool status;
    }

    /// <summary>
    /// 通知Gate服务器某个Group人数限制.
    /// </summary>
    [ProtocolRoute("AdminServer", "Gate")]
    public class SyncGateClientNumberLimit : Notify
    {
        public string serverGroupID;
        public int clientLimit;

    }


    public class SystemGateReloadServerList : ISerializable
    {
    }
    /// <summary>
    /// 由系统发出允许正常玩家登陆。
    /// </summary>
    public class SystemGMServerOpenNotify : ISerializable
    {

    }

    //---------------------------------------------------------------------------------
}
