using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace Gate.Data.Protocol
{
    public interface ISessionProtocol { }
    //--------------------------------------------------------------------------------
    /// <summary>
    /// 选择角色
    /// </summary>
    [MessageType(Constants.MSG_START + 0x201)]
    public class ClientSelectRoleRequest : Request, ISessionProtocol
    {
        public string c2s_roleUUID;
    }
    [MessageType(Constants.MSG_START + 0x202)]
    public class ClientSelectRoleResponse : Response, ISessionProtocol
    {
    }

    //--------------------------------------------------------------------------------
    /// <summary>
    /// 创建角色
    /// </summary>
    [MessageType(Constants.MSG_START + 0x203)]
    public class ClientCreateRoleRequest : Request, ISessionProtocol
    {
        public string c2s_name;
        public int c2s_template_id;
        /// <summary>
        /// 自定义扩展数据.
        /// </summary>
        public ISerializable c2s_extension_data;
    }
    [MessageType(Constants.MSG_START + 0x204)]
    public class ClientCreateRoleResponse : Response, ISessionProtocol
    {
        [MessageCode("角色创建已达上限！")]
        public const int CODE_CREATE_ROLE_LIMIT = CODE_ERROR + 1;
        [MessageCode("无效的创建信息！")]
        public const int CODE_CREATE_ROLE_INVAILD = CODE_ERROR + 2;
        [MessageCode("角色模板信息不存在！")]
        public const int CODE_TEMPLATE_NOT_EXIST = CODE_ERROR + 3;
        [MessageCode("角色名已存在！")]
        public const int CODE_NAME_ALREADY_EXIST = CODE_ERROR + 4;
        [MessageCode("名字中含有敏感字符")]
        public const int CODE_BLACK_NAME = CODE_ERROR + 5;

        public RoleSnap s2c_role;

        public override string ToString()
        {
            return $"Role: {s2c_role?.name}\t born on {s2c_role?.create_time}, last login on{s2c_role?.last_login_time}";
        }
    }

    //     [MessageType(Constants.SESSION_START + 5)]
    //     public class ClientGetRandomNameRequest : Request, ISessionProtocol
    //     {
    //         //0男1女.
    //         public byte c2s_role_gender;
    //         public int c2s_role_template_id;
    //     }
    //     [MessageType(Constants.SESSION_START + 6)]
    //     public class ClientGetRandomNameResponse : Response, ISessionProtocol
    //     {
    //         [DependOnProperty(nameof(IsSuccess))]
    //         public string s2c_name;
    //     }
    //--------------------------------------------------------------------------------

    /// <summary>
    /// 获取角色列表
    /// </summary>
    [MessageType(Constants.MSG_START + 0x205)]
    public class ClientGetRolesRequest : Request, ISessionProtocol
    {
        public bool c2s_need_role_data; // 是否需要角色数据，默认只返回角色快照
    }
    [MessageType(Constants.MSG_START + 0x206)]
    public class ClientGetRolesResponse : Response, ISessionProtocol
    {
        [DependOnProperty(nameof(IsSuccess))]
        public List<RoleSnap> s2c_snaps;
        [DependOnProperty(nameof(IsSuccess))]
        public List<ServerRoleData> s2c_roles;
    }

    //--------------------------------------------------------------------------------


    //--------------------------------------------------------------------------------
    /// <summary>
    /// 删除角色请求.
    /// </summary>
    [MessageType(Constants.MSG_START + 0x207)]
    public class ClientDeleteRoleRequest : Request, ISessionProtocol
    {
        public string c2s_role_uuid = null;

    }
    /// <summary>
    /// 删除角色结果.
    /// </summary>
    [MessageType(Constants.MSG_START + 0x208)]
    public class ClientDeleteRoleResponse : Response, ISessionProtocol
    {
        [MessageCode("无效的角色ID！")]
        public const int CODE_ROLEID_INVAILD = 501;
    }

    //--------------------------------------------------------------------------------

    /// <summary>
    /// 进入游戏
    /// </summary>
    [MessageType(Constants.MSG_START + 0x209)]
    public class ClientEnterGameRequest : Request, ISessionProtocol, INetProtocolBotIgnore
    {
        public string c2s_roleUUID;
        public string c2s_sdkToken;
    }
    [MessageType(Constants.MSG_START + 0x20A)]
    public class ClientEnterGameResponse : Response, ISessionProtocol, INetProtocolBotIgnore
    {
        [MessageCode("无效的角色ID！")]
        public const int CODE_ROLEID_INVAILD = 501;
        [MessageCode("角色逻辑不存在！")]
        public const int CODE_LOGIC_NOT_FOUND = 502;
        [MessageCode("角色已登录！")]
        public const int CODE_LOGIC_ALREADY_LOGIN = 503;
        [MessageCode("角色已封停")]
        public const int CODE_ROLE_SUSPEND = 504;
        [DependOnProperty(nameof(IsSuccess))]
        public ServerRoleData s2c_role;
        public DateTime s2c_suspendTime;
        public bool s2c_reconnected; // 是否是重连
    }
    //--------------------------------------------------------------------------------

    /// <summary>
    /// 退出游戏
    /// </summary>
    [MessageType(Constants.MSG_START + 0x20B)]
    public class ClientExitGameRequest : Request, ISessionProtocol, INetProtocolBotIgnore
    {
        public string c2s_roleUUID;
    }
    [MessageType(Constants.MSG_START + 0x20C)]
    public class ClientExitGameResponse : Response, ISessionProtocol, INetProtocolBotIgnore
    {
    }

}
