using DeepCore;
using DeepCore.IO;
using DeepCore.Protocol;
using System.Collections.Generic;

namespace Gate.Data.Protocol
{
    //---------------------------------------------------------------------------------------

    public interface IAreaProtocol { }

    /// <summary>
    /// (客户端)进入场景请求
    /// </summary>
    [MessageType(Constants.MSG_START + 0x401)]
    public class ClientEnterZoneRequest : Request
    {
        public string c2s_action;
    }
    /// <summary>
    /// (客户端)进入场景回馈
    /// </summary>
    [MessageType(Constants.MSG_START + 0x402)]
    public class ClientEnterZoneResponse : Response
    {
        [MessageCode("已排入队列")]
        public const int CODE_ENQUEUE = 202;
    }
    /// <summary>
    /// (客户端)进入场景通知
    /// </summary>
    [MessageType(Constants.MSG_START + 0x403)]
    public class ClientEnterZoneNotify : Notify, INetProtocolS2C
    {
        public string s2c_ZoneUUID;
        public int s2c_ZoneTemplateID;
        public int s2c_RoleUnitTemplateID;
        public string s2c_RoleDisplayName;
        public ISerializable s2c_RoleData;
        public int s2c_SceneLineIndex;
        public float s2c_ZoneUpdateIntervalMS;
        public HashMap<string, string> s2c_Ext;
    }
    /// <summary>
    /// (客户端)离开场景通知
    /// </summary>
    [MessageType(Constants.MSG_START + 0x404)]
    public class ClientLeaveZoneNotify : Notify, INetProtocolS2C
    {
        public string s2c_ZoneUUID;
    }
    /// <summary>
    /// (客户端)进入场景排队
    /// </summary>
    [MessageType(Constants.MSG_START + 0x405)]
    public class ClientEnterZoneQueueUpdateNotify : Notify, INetProtocolS2C
    {
        public int s2c_queueSize;
        public int s2c_expectTimeSec;
    }

    //---------------------------------------------------------------------------------------
    /// <summary>
    /// (客户端)战斗Action
    /// </summary>
    [MessageType(Constants.MSG_START + 0x406)]
    sealed public class ClientBattleAction : Notify, IAreaProtocol, IRpcNoneSerializable, INetProtocolC2S
    {
        public byte[] c2s_battleAction;
    }
    /// <summary>
    /// 战斗Action
    /// </summary>
    sealed public class SessionBattleAction : Notify
    {
        public string roleID;
        /// <summary>
        /// ClientBattleAction
        /// </summary>
        public byte[] clientBattleAction;
    }
    /// <summary>
    /// (客户端)战斗Event
    /// </summary>
    [MessageType(Constants.MSG_START + 0x407)]
    public class ClientBattleEvent : Notify, IAreaProtocol, IRpcNoneSerializable, INetProtocolS2C
    {
        public byte[] s2c_battleEvent;
    }
    //---------------------------------------------------------------------------------------

    /// <summary>
    // 客户端获取分线信息.
    /// </summary>
    [MessageType(Constants.MSG_START + 0x408)]
    public class ClientGetZoneInfoSnapRequest : Request, ILogicProtocol
    {
    }

    /// <summary>
    /// 客户端获取分线信息.
    /// </summary>
    [MessageType(Constants.MSG_START + 0x409)]
    public class ClientGetZoneInfoSnapResponse : Response, ILogicProtocol
    {
        public string s2c_curZoneUUID;
        public List<ZoneInfoSnap> s2c_snaps;
    }

    /// <summary>
    /// 场景换线.
    /// </summary>
    [MessageType(Constants.MSG_START + 0x40A)]
    public class ClientChangeZoneLineRequest : Request, ILogicProtocol
    {
        public string c2s_zoneuuid;
    }

    /// <summary>
    /// 场景换线
    /// </summary>
    [MessageType(Constants.MSG_START + 0x40B)]
    public class ClientChangeZoneLineResponse : Response, ILogicProtocol
    {
        [MessageCode("目标场景繁忙")]
        public const int CODE_LINE_BUSY = 501;

        [MessageCode("目标场景不存在")]
        public const int CODE_NOT_EXIST = 502;

        [MessageCode("正在战斗中，无法切线")]
        public const int CODE_IN_BATTLE = 503;
    }

    //---------------------------------------------------------------------------------------

}
