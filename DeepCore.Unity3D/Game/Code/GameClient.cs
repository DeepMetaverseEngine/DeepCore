using Code.System.AB;
using DeepCore.FuckPomeloClient;
using DeepCore.Protocol;
using Gate.Client;
using Gate.Data;
using IOGame.Client;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Code
{
    public class GameClient
    {
        public static GameClient Instance;
        public GateClient Client;
        public IClientData DataPath;

        public GameClient(IClientData dataPath)
        {
            Instance = this;
            DataPath = dataPath;
            var DataRootPath = DataPath.DataRootPath;
            Debug.Log($"========= ~ Data Root Path: {DataRootPath} ");
            
            GateClientConfig.ServerListUrl = Path.Combine(DataRootPath, "serverlist.xml");
            GateClientConfig.LanguageRootDir = Path.Combine(DataRootPath, "templates", "lang");
            GateClientConfig.ClientCodecClass = typeof(IOGame.Client.GenCodec.Serializer).FullName;

            GateClientConfig.BattleEditorDir = Path.Combine(DataRootPath, "GameEditor");
            GateClientConfig.BattleCodec = typeof(IOGame.Client.GenCodec.IOBattleCodec).FullName;
            // GateClientConfig.BattleDataFactory = typeof(IOGame.Core.Battle.Data.IOBattleDataFactory).FullName;
            // GateClientConfig.BattleHostFactory = typeof(IOGame.Core.Battle.Host.IOBattleHostFactory).FullName;
            // GateClientConfig.BattleSlaveFactory = typeof(IOGame.Core.Battle.Slave.IOBattleSlaveFactory).FullName;

            new IOGameClientManager();

            ABSystemImpl.RootPath = DataPath.ResRoot;
            
            Debug.Log($"========= ~ Res Root:{ABSystemImpl.RootPath} =========");
            
            Client = IOGameClientManager.Instance.CreateGateClient();
            BattleRuntimeManager.Instance.Init();
            Client.OnGameDisconnected += OnGameDisconnected;
            Client.OnError += OnError;
        }

        public virtual async Task StartConnect()
        {
            var server = GateClientManager.ServerList.GetServer(DataPath.ServerID);
            if (server != null)
            {
                Debug.Log("ServerID : " + server);
                var conn = await Client.ConnectGateAndServerAsync(server, DataPath.AccountID, DataPath.AccountToken);
                Debug.Log("ConnectGateAndServer : " + conn);
                var roleList = await Client.GameSession.RequestAsync<ClientGetRolesResponse>(new ClientGetRolesRequest() { });
                Debug.Log("ClientGetRoles : " + roleList);
                if (!Response.CheckSuccess(roleList))
                {
                    Debug.LogError(roleList);
                    return;
                }
                if (roleList.s2c_roles == null || roleList.s2c_roles.Count == 0)
                {
                    var create = await Client.GameSession.RequestAsync<ClientCreateRoleResponse>(new ClientCreateRoleRequest { c2s_name = DataPath.AccountID, });
                    Debug.Log("ClientCreateRole : " + create);
                    if (!Response.CheckSuccess(create))
                    {
                        Debug.LogError(create);
                        return;
                    }
                    roleList = await Client.GameSession.RequestAsync<ClientGetRolesResponse>(new ClientGetRolesRequest() { });
                    Debug.Log("ClientGetRoles : " + roleList);
                    if (!Response.CheckSuccess(roleList))
                    {
                        Debug.LogError(roleList);
                        return;
                    }
                }
                var role = roleList.s2c_roles[0];
                var enter = await Client.GameSession.RequestAsync<ClientEnterGameResponse>(new ClientEnterGameRequest()
                {
                    c2s_roleUUID = role.uuid
                });
                Debug.Log("ClientEnterGame : " + enter);
                if (!Response.CheckSuccess(enter))
                {
                    Debug.LogError(enter);
                    return;
                }
            }
            else
            {
                Debug.LogError("ServerID not exist : " + DataPath.ServerID);
            }
        }


        private void OnError(Exception obj)
        {
            
            
        }

        private void OnGameDisconnected(PomeloClient arg1, CloseReason arg2)
        {
            
        }

    }
}