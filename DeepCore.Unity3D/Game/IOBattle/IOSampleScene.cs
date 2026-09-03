using Code.System.AB;
using DeepCore;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepGame3D.Unity;
using DeepGame3D.Unity.BattleView;
using Gate.Client;
using Gate.Client.Battle;
using Gate.Client.Modules;
using Gate.Data;
using Gate.Data.Protocol;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace IOGame.Client.Unity.IOBattle
{
    public class IOSampleScene : MonoBehaviour
    {
        //---------------------------------------------------------------------------------------------
        public string DataRootPath;
        public string ServerID = "1";
        public string AccountID = "hzdsb233";
        public string AccountToken = "123456";
        //---------------------------------------------------------------------------------------------
        public GateClient client { get; private set; }
        public UnityBattleZone currentBattle { get; private set; }
        public GameObject currentBattleNode { get; private set; }
        //-------------------------------------------------------------------------------------------------------------------------
        void InitGlobal()
        {
            var args = Environment.GetCommandLineArgs();
            var prop = Properties.ParseArgs(args);
            if (prop.TryGetValue("-dataRoot", out var root) && Directory.Exists(root))
            {
                DataRootPath = Path.GetFullPath(root);
            }
            else if (Directory.Exists(DataRootPath))
            {

            }
            else if (new DirectoryInfo(Application.dataPath).TryFindParentDirectory(Path.Combine("data", "GameEditor"), out var editorRoot))
            {
                DataRootPath = editorRoot.Parent.FullName;
            }
            else
            {
                DataRootPath = $"file://{Application.dataPath}/../../../data/";
            }
            Debug.Log("DataRootPath : " + DataRootPath);
            {
                GateClientConfig.ServerListUrl = Path.Combine(DataRootPath, "serverlist.xml");
                GateClientConfig.LanguageRootDir = Path.Combine(DataRootPath, "templates", "lang");
                GateClientConfig.ClientCodecClass = typeof(Gen.Client.Serializer).FullName;

                GateClientConfig.BattleEditorDir = Path.Combine(DataRootPath, "GameEditor");
                GateClientConfig.BattleCodec = typeof(GenCodec.IOBattleCodec).FullName;
                GateClientConfig.BattleDataFactory = typeof(Core.Battle.Data.IOBattleDataFactory).FullName;
                //GateClientConfig.BattleHostFactory = typeof(Core.Battle.Host.IOBattleHostFactory).FullName;
                GateClientConfig.BattleSlaveFactory = typeof(Core.Battle.Slave.IOBattleSlaveFactory).FullName;
            }
            new IOGameClientManager();
            ABSystemImpl.RootPath = Path.Combine(DataRootPath, "GameEditor");
        }

        void Start()
        {
            try
            {
                InitGlobal();
                client = GateClientManager.Instance.CreateGateClient();
                client.OnGameDisconnected += Client_OnGameDisconnected; ;
                client.OnError += Client_OnError;
                if (client.TryGetModel<AreaModule>(out var area))
                {
                    area.OnZoneEnter += Area_OnZoneEnter;
                }
                this.OnStart();
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
        }
        void Update()
        {
            try
            {
                var ms = (int)(Time.deltaTime * 1000);
                this.OnBeginUpdate();
                client?.Update(ms);
                currentBattle?.Update(ms);
                this.OnEndUpdate();
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
        }
        void OnDestroy()
        {
            try
            {
                client?.Dispose();
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
            this.OnDispose();
        }

        protected virtual void OnStart()
        {
            MockStartConnect(AccountID, AccountToken, AccountID, ServerID).NoWait();
        }
        protected virtual void OnDispose() { }
        protected virtual void OnBeginUpdate() { }
        protected virtual void OnEndUpdate() { }
        protected virtual void OnCreateBattle(UnityBattleZone battle)
        {

        }

        //-------------------------------------------------------------------------------------------------------------------------
        #region GameMockRequest

        protected virtual async Task MockStartConnect(string account, string pswd, string roleName, string serverID)
        {
            var server = GateClientManager.ServerList.GetServer(serverID);
            if (server != null)
            {
                Debug.Log("ServerID : " + server);
                var conn = await client.ConnectGateAndServerAsync(server, account, pswd);
                Debug.Log("ConnectGateAndServer : " + conn);
                var roleList = await client.GameSession.RequestAsync<ClientGetRolesResponse>(new ClientGetRolesRequest() { });
                Debug.Log("ClientGetRoles : " + roleList);
                if (!Response.CheckSuccess(roleList))
                {
                    Debug.LogError(roleList);
                    return;
                }
                if (roleList.s2c_roles == null || roleList.s2c_roles.Count == 0)
                {
                    var create = await client.GameSession.RequestAsync<ClientCreateRoleResponse>(new ClientCreateRoleRequest() { c2s_name = roleName, });
                    Debug.Log("ClientCreateRole : " + create);
                    if (!Response.CheckSuccess(create))
                    {
                        Debug.LogError(create);
                        return;
                    }
                    roleList = await client.GameSession.RequestAsync<ClientGetRolesResponse>(new ClientGetRolesRequest() { });
                    Debug.Log("ClientGetRoles : " + roleList);
                    if (!Response.CheckSuccess(roleList))
                    {
                        Debug.LogError(roleList);
                        return;
                    }
                }
                var role = roleList.s2c_roles[0];
                var enter = await client.GameSession.RequestAsync<ClientEnterGameResponse>(new ClientEnterGameRequest()
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
                Debug.LogError("ServerID not exist : " + ServerID);
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------
        #region GameEvents

        protected virtual void Area_OnZoneEnter(GateBattle obj)
        {
            Debug.Log("Area_OnZoneChanged : " + obj);
            try
            {
                if (currentBattle != null)
                {
                    currentBattle.Dispose();
                    currentBattle = null;
                }
                if (currentBattleNode != null)
                {
                    Destroy(currentBattleNode);
                    currentBattleNode = null;
                }
                currentBattleNode = new GameObject(obj.ToString());
                currentBattleNode.transform.parent = gameObject.transform;
                currentBattle = UnityBattleFactory.Instance.CreateBattle();
                currentBattle.Init(currentBattleNode, obj);
                OnCreateBattle(currentBattle);
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
        }
        private void Client_OnError(Exception err)
        {
            Debug.LogError(err);
        }
        private void Client_OnGameDisconnected(DeepCore.FuckPomeloClient.PomeloClient arg1, DeepCore.FuckPomeloClient.CloseReason arg2)
        {
            Debug.Log("Client_OnGameDisconnected : " + arg2);
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------
    }
}