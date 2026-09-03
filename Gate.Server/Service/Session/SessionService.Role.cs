using DeepCore;
using DeepCrystal.RPC;
using Gate.Data;
using Gate.Data.Protocol;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gate.Server.Service.Session
{
    //---------------------------------------------------------------------------------------
    //处理角色相关逻辑.
    //---------------------------------------------------------------------------------------
    public partial class SessionService
    {
        [RpcHandler(typeof(ClientCreateRoleRequest), typeof(ClientCreateRoleResponse))]
        public virtual async Task<ClientCreateRoleResponse> client_rpc_Handle(ClientCreateRoleRequest req)
        {
            try
            {
                var serverID = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginServerID));
                var privilege = await accountSave.LoadFieldAsync<RolePrivilege>(nameof(AccountData.privilege));
                var roleIDMap = await accountSave.LoadFieldAsync<HashMap<string, RoleIDSnap>>(nameof(AccountData.roleList));
                if (roleIDMap == null)
                {
                    roleIDMap = new HashMap<string, RoleIDSnap>();
                }
                int roleCount = 0;
                foreach (var item in roleIDMap)
                {
                    if (item.Value.name == req.c2s_name)
                    {
                        return new ClientCreateRoleResponse()
                        {
                            s2c_code = ClientCreateRoleResponse.CODE_NAME_ALREADY_EXIST,
                            s2c_role = await queryRoleSnap.LoadDataAsync(item.Value.roleUUID),
                        };
                    }
                    if (item.Value.serverID == serverID)
                    {
                        roleCount++;
                    }
                }

                if (roleCount >= GateServerManager.Mapping.GetRoleMaxCount())//该服下账号是否达到创建上限.
                {
                    return new ClientCreateRoleResponse() { s2c_code = ClientCreateRoleResponse.CODE_CREATE_ROLE_LIMIT };
                }
                else if (GateServerManager.Mapping.IsRoleNameBlackWord(req.c2s_name))
                {
                    return new ClientCreateRoleResponse() { s2c_code = ClientCreateRoleResponse.CODE_BLACK_NAME };
                }
                else if (!GateServerManager.Mapping.CheckRoleName(req.c2s_name))
                {
                    //名字异常，长度不符合规范
                    return new ClientCreateRoleResponse() { s2c_code = ClientCreateRoleResponse.CODE_CREATE_ROLE_INVAILD };
                }
                // 创建纯数据
                var roleData = GateServerManager.Mapping.CreateRoleData(req, accountID, serverID);
                //用户权限.
                roleData.privilege = privilege;

                if (roleData == null)
                {
                    return new ClientCreateRoleResponse() { s2c_code = ClientCreateRoleResponse.CODE_TEMPLATE_NOT_EXIST };
                }
                var digitID = await GateServerManager.Mapping.TryRegistRoleNameMappingAsync(serverID, roleData.uuid, roleData.name, this);
                if (digitID == null)
                {
                    return new ClientCreateRoleResponse() { s2c_code = ClientCreateRoleResponse.CODE_NAME_ALREADY_EXIST };
                }
                roleData.digitID = digitID;

                // Role数据映射
                var snapData = await GateServerManager.Mapping.CreateRoleAndSnapAsync(roleData, this);
                var roleIDSnap = GateServerManager.Mapping.CreateRoleIDSnapData(roleData);
                roleIDMap.Add(roleIDSnap.roleUUID, roleIDSnap);

                accountSave.SetField(nameof(AccountData.roleList), roleIDMap);
                await accountSave.FlushAsync();

                //单区内角色记录
                var serverRoleIDSet = GateServerManager.Mapping.GetServerRoleIDMappingSet(this, roleData.server_id);
                await serverRoleIDSet.AddRoleIDAsync(roleData.uuid);
                var ret = new ClientCreateRoleResponse()
                {
                    s2c_role = snapData
                };
                //网络协议接口日志//
                //log.Log(ret);
                //BI创角记录.
                return ret;
            }
            catch (Exception err)
            {
                log.Error(string.Format("ClientCreateRoleRequest Handle Error:account = {0}  msg = {1} ", accountID, err.Message), err);
                return (new ClientCreateRoleResponse()
                {
                    s2c_code = ClientCreateRoleResponse.CODE_ERROR,
                    s2c_msg = err.Message
                });
            }
        }

        //         [RpcHandler(typeof(ClientGetRandomNameRequest), typeof(ClientGetRandomNameResponse), ServerNames.ConnectServerType)]
        //         public virtual Task<ClientGetRandomNameResponse> client_rpc_Handle(ClientGetRandomNameRequest req)
        //         {
        //             try
        //             {
        //                 var rd = RPGServerTemplateManager.Instance.GetRoleTemplate(req.c2s_role_template_id, req.c2s_role_gender);
        //                 if (rd != null)
        //                 {
        //                     //获取随机名字方法.
        //                     return Task.FromResult(new ClientGetRandomNameResponse()
        //                     {
        //                         s2c_name = RPGServerTemplateManager.Instance.RandomName(rd)
        //                     });
        //                 }
        //                 else
        //                 {
        //                     return Task.FromResult(new ClientGetRandomNameResponse() { s2c_code = ClientCreateRoleResponse.CODE_ERROR, });
        //                 }
        //             }
        //             catch (Exception err)
        //             {
        //                 log.ErrorFormat("ClientGetRandomNameRequest Handle Error:account = {0} msg = {1} ", accountID, err.Message);
        //                 return Task.FromResult(new ClientGetRandomNameResponse() { s2c_code = ClientCreateRoleResponse.CODE_ERROR, s2c_msg = err.Message });
        //             }
        //         }

        [RpcHandler(typeof(ClientGetRolesRequest), typeof(ClientGetRolesResponse))]
        public virtual async Task<ClientGetRolesResponse> client_rpc_Handle(ClientGetRolesRequest req)
        {
            try
            {
                var serverID = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginServerID));
                var roleIDMap = await accountSave.LoadFieldAsync<HashMap<string, RoleIDSnap>>(nameof(AccountData.roleList));
                // using (var saveAcc = PersistenceFactory.Instance.Get<AccountData>(null, accountID))
                if (roleIDMap != null && roleIDMap.Count > 0)
                {
                    var snaps = new List<RoleSnap>();
                    var roles = new List<ServerRoleData>();
                    foreach (var item in roleIDMap)
                    {
                        if (item.Value.serverID == serverID)
                        {
                            var snap = await queryRoleSnap.LoadDataAsync(item.Value.roleUUID);
                            if (snap != null && snap.server_id == serverID)
                            {
                                snaps.Add(snap);
                            }
                        }
                    }
                    if (req.c2s_need_role_data)
                    {
                        foreach (var item in snaps)
                        {
                            using (var roleMapping = GateServerManager.Mapping.CreateRoleDataMapping(item.uuid, this))
                            {
                                var roleData = await roleMapping.LoadDataAsync();
                                if (roleData != null)
                                {
                                    roles.Add(roleData);
                                }
                            }
                        }
                    }
                    return (new ClientGetRolesResponse()
                    {
                        s2c_code = ClientGetRolesResponse.CODE_OK,
                        s2c_snaps = snaps,
                        s2c_roles = roles,
                    });
                }
                else
                {
                    return (new ClientGetRolesResponse() { s2c_code = ClientGetRolesResponse.CODE_OK });
                }
            }
            catch (Exception err)
            {
                log.ErrorFormat("ClientGetRolesRequest Handle Error:account = {0} msg = {1} ", accountID, err.Message);
                return (new ClientGetRolesResponse() { s2c_code = ClientGetRolesResponse.CODE_ERROR, s2c_msg = err.Message });
            }
        }

        [RpcHandler(typeof(ClientDeleteRoleRequest), typeof(ClientDeleteRoleResponse))]
        public virtual async Task<ClientDeleteRoleResponse> client_rpc_Handle(ClientDeleteRoleRequest req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.c2s_role_uuid))
                {
                    return (new ClientDeleteRoleResponse() { s2c_code = ClientDeleteRoleResponse.CODE_ROLEID_INVAILD });
                }
                else
                {
                    var roleIDMap = await accountSave.LoadFieldAsync<HashMap<string, RoleIDSnap>>(nameof(AccountData.roleList));
                    if (roleIDMap.Remove(req.c2s_role_uuid))
                    {
                        accountSave.SetField(nameof(AccountData.roleList), roleIDMap);
                        await accountSave.FlushAsync();

                        await GateServerManager.Mapping.DeleteRoleDataAsync(req.c2s_role_uuid, this);
                        return (new ClientDeleteRoleResponse());
                    }
                    else
                    {
                        return (new ClientDeleteRoleResponse() { s2c_code = ClientDeleteRoleResponse.CODE_ROLEID_INVAILD });
                    }
                }

            }
            catch (Exception err)
            {
                log.ErrorFormat("ClientDeleteRoleRequest Handle Error:account = {0}  msg = {1} ", accountID, err.Message);
                return (new ClientDeleteRoleResponse() { s2c_code = ClientDeleteRoleResponse.CODE_ERROR, s2c_msg = err.Message });
            }
        }
    }

}
