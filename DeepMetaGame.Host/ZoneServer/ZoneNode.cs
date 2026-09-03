using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Host.ZoneServer.Interface;
using DeepCore.GameData;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepCore.Protocol;
using DeepCore.Reflection;

namespace DeepCore.Game3D.Host.ZoneServer
{
    public partial class ZoneNode : BaseZoneNode
    {
        //------------------------------------------------------------------------------------------------------------
        public ZoneNode(IZoneNodeServer server, ZoneHostFactory hostFactory, EditorTemplates data_root) : base(server, hostFactory, data_root)
        {
            this.Codec = new BattleCodec(data_root.Templates, true);
        }
        protected BattleCodec Codec { get; }
        public bool EnableSyncPos { get; set; } = false;
        public ulong ZoneTick { get { return base.Zone != null ? base.Zone.Tick : 0; } }
        public TimeSpan ZonePassTime { get { return base.Zone != null ? TimeSpan.FromMilliseconds(base.Zone.PassTimeMS) : TimeSpan.Zero; } }
        public int PlayerCount { get { return mPlayerObjectMap.Count; } }
        public bool EnableAOI
        {
            get { return mEnableAOI; }
            set
            {
                lock (this)
                {
                    if (mEnableAOI != value)
                    {
                        mEnableAOI = value;
                        QueueTask(() => OnAoiChanged(value));
                    }
                }
            }
        }

        /// <summary>
        /// 根据UUID获取单位
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public InstancePlayer GetPlayer(string uuid)
        {
            return mPlayerObjectMap.GetPlayer(uuid);
        }
        public PlayerClient GetPlayerClient(string uuid)
        {
            return mPlayerObjectMap.GetClient(uuid);
        }
        public bool GetPlayerAndClient(string uuid, out InstancePlayer player, out PlayerClient client)
        {
            return mPlayerObjectMap.TryGet(uuid, out player, out client);
        }
        /// <summary>
        /// 遍历所有客户端
        /// </summary>
        /// <param name="action"></param>
        public void ForEachPlayers(Action<PlayerClient> action)
        {
            mPlayerObjectMap.ForEachPlayers(action);
        }
        /// <summary>
        /// 遍历所有客户端(战斗线程)
        /// </summary>
        /// <param name="action"></param>
        public void ForEachPlayersInZone(Action<PlayerClient> action)
        {
            using (var players = Zone.ObjectPool.AllocList<PlayerClient>())
            {
                mPlayerObjectMap.GetPlayers(players);
                foreach (var c in players)
                {
                    action(c);
                }
            }
        }

        //         /// <summary>
        //         /// 单位进入场景
        //         /// </summary>
        //         /// <param name="player"></param>
        //         /// <param name="temp"></param>
        //         /// <param name="force"></param>
        //         /// <param name="level"></param>
        //         /// <param name="enterPos"></param>
        //         /// <param name="callback"></param>
        //         /// <param name="callerror"></param>
        //         public void PlayerEnter(IZoneNodeSession player, UnitInfo temp, byte force, int level, Vector3 enterPos, Action<PlayerClient, Exception> callback)
        //         {
        //             AddUnit add = new AddUnit()
        //             {
        //                 info = temp,
        //                 editor_name = "",
        //                 player_uuid = player.PlayerUUID,
        //                 force = force,
        //                 level = level,
        //                 pos = enterPos,
        //                 direction = 0
        //             };
        //             PlayerEnter(player, add, callback);
        //         }
        //         public System.Threading.Tasks.Task<PlayerClient> PlayerEnterAsync(IZoneNodeSession player, UnitInfo temp, byte force, int level, Vector3 enterPos)
        //         {
        //             AddUnit add = new AddUnit()
        //             {
        //                 info = temp,
        //                 editor_name = "",
        //                 player_uuid = player.PlayerUUID,
        //                 force = force,
        //                 level = level,
        //                 pos = enterPos,
        //                 direction = 0
        //             };
        //             return PlayerEnterAsync(player, add);
        //         }
        public void PlayerEnter(IZoneNodeSession player, TAddUnit add, Action<PlayerClient, Exception> callback, bool overridePlayer)
        {
            if (player != null)
            {
                try
                {
                    // 客户端连接到战斗服 //
                    var post = QueueTask(() =>
                    {
                        log.Debug($"PlayerEnter : {player.PlayerUUID} : enter");
                        try
                        {
                            if (!overridePlayer)
                            {
                                if (mPlayerObjectMap.TryGet(player.PlayerUUID, out var oldSession, out var oldPlayer))
                                {
                                    callback(null, new PlayerAlreadyExistException(player.PlayerUUID, this.SceneID.ToString(), "PlayerEnter"));
                                    return;
                                }
                            }
                            if (OnPlayerBeginEnter(player, Zone, ref add))
                            {
                                log.Debug($"PlayerEnter : {player.PlayerUUID} : OnPlayerBeginEnter");
                                var actor = Zone.GetPlayerByUUID(player.PlayerUUID);
                                bool reconnected = false;
                                if (actor == null)
                                {
                                    actor = base.Zone.AddUnit(add) as InstancePlayer;
                                }
                                else
                                {
                                    // 场景内已有玩家 //
                                    reconnected = true;
                                }
                                if (actor != null)
                                {
                                    actor.SetSyncMode(SyncMode.MoveByClient_PreSkillByClient);
                                    // 绑定客户端ID和游戏角色 //
                                    //zc.Actor.ClientID = zc.ID;
                                    //绑定关系//
                                    PlayerClient zc = mPlayerObjectMap.GetClient(player.PlayerUUID);
                                    if (reconnected)
                                    {
                                        ReconnectPlayerClient(player, actor);
                                        actor.OnReconnected(add);
                                    }
                                    else
                                    {
                                        if (zc != null)
                                        {
                                            zc.Dispose();
                                        }
                                        // 初始化 InstanceUnit 各个字段 //
                                        actor.Alias = player.DisplayName;
                                        actor.OnConnected(add);
                                        zc = CreatePlayerClient(player, actor);
                                        // 准备发送当前场景信息 //
                                        mPlayerObjectMap.PutPlayer(zc);
                                    }
                                    zc.Send(new PlayerMessageEntry()
                                    {
                                        message = Zone.ObjectPool.Alloc<ClientEnterScene>().Init(
                                            Zone.UUID,
                                            SceneData.ID,
                                            Zone.SpaceDivSizeW,
                                            Zone.Gravity,
                                            Zone.Terrain3D.StepIntercept,
                                            Templates.ResourceVersion,
                                            Zone.GetLayerInitData())
                                    }, true);
                                    zc.Send(new PlayerMessageEntry()
                                    {
                                        message = actor.AllocLockActorEvent(player.DisplayName, zc.SyncObjectRange, zc.SyncObjectOutRange, base.ClientUpdateIntervalMS)
                                    }, true);
                                    zc.Send(new PlayerMessageEntry()
                                    {
                                        message = actor.AllocSyncSkillActives()
                                    }, true);
                                    zc.Send(new PlayerMessageEntry()
                                    {
                                        message = Zone.AllocSyncFlagsEvent()
                                    }, true);
                                    zc.Start();
                                    zc.Send(new PlayerMessageEntry()
                                    {
                                        message = zc.AllocSyncObjectsEvent()
                                    }, true);
                                    OnPlayerEntered(zc);
                                    zc.Session.OnPlayerConnected(zc);
                                    callback(zc, null);
                                }
                                else
                                {
                                    callback(null, new Exception("Can not add player : " + add.info));
                                }
                            }
                            else
                            {
                                callback(null, new Exception("Can not add player : " + add.info));
                            }
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                            OnError(err);
                            callback(null, err);
                        }
                    });
                    if (!post)
                    {
                        callback(null, new ArgumentNullException());
                    }
                }
                catch (Exception err)
                {
                    callback(null, err);
                }
            }
            else
            {
                callback(null, new ArgumentNullException());
            }
        }

        public System.Threading.Tasks.Task<PlayerClient> PlayerEnterAsync(IZoneNodeSession player, TAddUnit add, bool overridePlayer)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<PlayerClient>();
            try
            {
                this.PlayerEnter(player, add, (c, e) =>
                {
                    if (c != null)
                    {
                        tcs.TrySetResult(c);
                    }
                    else if (e != null)
                    {
                        tcs.TrySetException(e);
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }
                }, overridePlayer);
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            return tcs.Task;
        }
        /// <summary>
        /// 单位离开场景
        /// </summary>
        /// <param name="player"></param>
        /// <param name="callback"></param>
        /// <param name="callerror"></param>
        public void PlayerReconnect(IZoneNodeSession player, Action<PlayerClient, Exception> callback)
        {
            if (player != null)
            {
                try
                {
                    var post = QueueTask(() =>
                    {
                        try
                        {
                            var z = Zone;
                            PlayerClient zc;
                            InstancePlayer actor;
                            if (mPlayerObjectMap.TryGet(player.PlayerUUID, out actor, out zc))
                            {
                                actor.OnReconnected(null);
                                zc.Send(new PlayerMessageEntry() { message = z.ObjectPool.Alloc<ClientEnterScene>().Init(Zone.UUID, SceneData.ID, Zone.SpaceDivSizeW, Zone.Gravity, Zone.Terrain3D.StepIntercept, Templates.ResourceVersion, Zone.GetLayerInitData()) }, true);
                                zc.Send(new PlayerMessageEntry() { message = actor.AllocLockActorEvent(player.DisplayName, zc.SyncObjectRange, zc.SyncObjectOutRange, base.ClientUpdateIntervalMS) }, true);
                                zc.Send(new PlayerMessageEntry() { message = actor.AllocSyncSkillActives() }, true);
                                zc.Send(new PlayerMessageEntry() { message = Zone.AllocSyncFlagsEvent() }, true);
                                zc.Send(new PlayerMessageEntry() { message = zc.AllocSyncObjectsEvent() }, true);
                                callback(zc, null);
                            }
                            else
                            {
                                callback(null, new PlayerNotExistException(player.PlayerUUID, Zone.UUID, "PlayerReconnect"));
                            }
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                            OnError(err);
                            callback(null, err);
                        }
                    });
                    if (!post)
                    {
                        callback(null, new ArgumentNullException("PlayerReconnect: scene disposed"));
                    }
                }
                catch (Exception err)
                {
                    callback(null, err);
                }
            }
            else
            {
                callback(null, new ArgumentNullException("PlayerReconnect: player is null"));
            }
        }
        public void PlayerDisconnect(IZoneNodeSession player, Action<PlayerClient, Exception> callback, bool keep_object = false)
        {
            if (player != null && player.PlayerUUID != null)
            {
                try
                {
                    var post = QueueTask(() =>
                    {
                        try
                        {
                            InstancePlayer out_player;
                            PlayerClient out_client = (player.BindingPlayer);
                            if (mPlayerObjectMap.TryGet(player.PlayerUUID, out out_player, out out_client))
                            {
                                if (player.BindingPlayer != null)
                                {
                                    out_client.Actor.OnDisconnected();
                                    OnPlayerDisconnect(out_client);
                                }
                                callback(out_client, null);
                            }
                            else
                            {
                                callback(out_client, new PlayerNotExistException(player.PlayerUUID, Zone.UUID.ToString(), "PlayerLeave"));
                            }
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                            OnError(err);
                            callback(null, err);
                        }
                    });
                    if (!post)
                    {
                        callback(null, new ArgumentNullException("PlayerDisconnect: scene disposed"));
                    }
                }
                catch (Exception err)
                {
                    callback(null, err);
                }
            }
            else
            {
                callback(null, new ArgumentNullException("PlayerDisconnect player is null"));
            }
        }
        /// <summary>
        /// 单位离开场景
        /// </summary>
        /// <param name="player"></param>
        /// <param name="callback"></param>
        /// <param name="callerror"></param>
        /// <param name="keep_object">保留单位</param>
        public void PlayerLeave(IZoneNodeSession player, Action<PlayerClient, Exception> callback, bool keep_object = false)
        {
            if (player != null && player.PlayerUUID != null)
            {
                try
                {
                    var post = QueueTask(() =>
                    {
                        try
                        {
                            InstancePlayer out_player;
                            PlayerClient out_client = (player.BindingPlayer);
                            if (mPlayerObjectMap.RemoveByKey(player.PlayerUUID, out out_player, out out_client))
                            {
                                try
                                {
                                    if (player.BindingPlayer != null)
                                    {
                                        out_client.LastZoneSaveData = out_client.Actor.GetLastZoneSaveData();
                                        if (!keep_object)
                                        {
                                            out_client.Actor.RemoveFromParent();
                                        }
                                        else
                                        {
                                            out_client.Actor.OnDisconnected();
                                        }
                                        // 通知客户端清理BattleClient //
                                        out_client.Send(new PlayerMessageEntry() { message = Zone.ObjectPool.Alloc<PlayerLeaveScene>().Init(out_player.ID) }, true);
                                        OnPlayerLeft(out_client);
                                    }
                                }
                                finally
                                {
                                    out_client.Dispose();
                                }
                                callback(out_client, null);
                            }
                            //正常leave会走，断线时也会走 //M1.5BUG 修复：先报错，至少要call error 以免上层一直await
                            else
                            {
                                callback(null, new PlayerNotExistException(player.PlayerUUID, Zone.UUID.ToString(), "PlayerLeave"));
                            }
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                            OnError(err);
                            callback(null, err);
                        }
                    });
                    if (!post)
                    {
                        callback(null, new ArgumentNullException("PlayerLeave:Scene disposed"));
                    }
                }
                catch (Exception err)
                {
                    callback(null, err);
                }
            }
            else
            {

                callback(null, new ArgumentNullException($"PlayerLeave:Player is null,IZoneNodeSession exist = [{player != null}], uuid = [{player?.PlayerUUID}]"));
            }
        }

        public System.Threading.Tasks.Task<PlayerClient> PlayerLeaveAsync(IZoneNodeSession player)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<PlayerClient>();
            try
            {
                this.PlayerLeave(player, (c, e) =>
                {
                    if (c != null)
                    {
                        tcs.TrySetResult(c);
                    }
                    else if (e != null)
                    {
                        tcs.TrySetException(e);
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }
                });
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            return tcs.Task;
        }

        //------------------------------------------------------------------------------------------------------------

        public System.Threading.Tasks.Task<TResult> QueueSceneTaskAsync<TResult>(System.Func<EditorScene, TResult> action)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<TResult>();
            try
            {
                this.QueueSceneTask((z) =>
                {
                    try
                    {
                        var result = action(z);
                        tcs.TrySetResult(result);
                    }
                    catch (Exception err)
                    {
                        tcs.TrySetException(err);
                    }
                });
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            return tcs.Task;
        }
        public System.Threading.Tasks.Task<TResult> QueuePlayerTaskAsync<TResult>(string playerUUID, System.Func<InstancePlayer, TResult> action)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<TResult>();
            try
            {
                this.QueuePlayerTask(playerUUID, (p) =>
                {
                    try
                    {
                        var result = action(p);
                        tcs.TrySetResult(result);
                    }
                    catch (Exception err)
                    {
                        tcs.TrySetException(err);
                    }
                });
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            return tcs.Task;
        }
        public void QueueSceneTask(System.Action<EditorScene, Exception> action)
        {
            try
            {
                var post = base.QueueTask(() =>
                {
                    action(base.Zone, null);
                });
                if (!post)
                {
                    action(null, new ArgumentNullException());
                }
            }
            catch (Exception err) { action(null, err); }
        }
        public void QueuePlayerTask(string playerUUID, System.Action<InstancePlayer, Exception> action)
        {
            try
            {
                var post = base.QueueTask(() =>
                {
                    var p = Zone.GetPlayerByUUID(playerUUID);
                    action(p, null);
                });
                if (!post)
                {
                    action(null, new ArgumentNullException());
                }
            }
            catch (Exception err) { action(null, err); }
        }
        protected void QueueSceneTask(System.Action<EditorScene> action)
        {
            var post = base.QueueTask(() =>
            {
                action(base.Zone);
            });
            if (!post)
            {
                action(null);
            }
        }
        protected void QueuePlayerTask(string playerUUID, System.Action<InstancePlayer> action)
        {
            var post = base.QueueTask(() =>
            {
                var p = Zone.GetPlayerByUUID(playerUUID);
                action(p);
            });
            if (!post)
            {
                action(null);
            }
        }

        //------------------------------------------------------------------------------------------------------------

        protected override void OnZoneCreated(EditorScene zone)
        {
            base.OnZoneCreated(zone);
            this.mEnableAOI = base.SceneData.EnableServerAOI;
            this.Zone.SyncPos = EnableSyncPos || !mEnableAOI;
        }
        protected override void OnZoneUpdate()
        {
            using (var players = Zone.ObjectPool.AllocList<PlayerClient>())
            using (var entries = Zone.ObjectPool.AllocList<PlayerMessageEntry>())
            {
                mPlayerObjectMap.GetPlayers(players);

                //客户端更新//
                foreach (var c in players)
                {
                    c.BeginUpdate();
                }
                //预编码//
                foreach (var evt in PostEvents)
                {
                    entries.Add(AllocPlayerEntry(evt));
                }
                //向客户端推送实时场景信息//
                foreach (var c in players)
                {
                    foreach (var msg in entries)
                    {
                        c.Send(msg);
                    }
                }
                //客户端更新//
                foreach (var c in players)
                {
                    c.EndUpdate();
                }
            }
        }
        protected override void OnEndUpdate()
        {
            base.OnEndUpdate();
            using (var players = Zone.ObjectPool.AllocList<PlayerClient>())
            {
                mPlayerObjectMap.GetPlayers(players);
                foreach (var c in players)
                {
                    var zone_client = c.Session;
                    zone_client.ClientFlush(Codec);
                }
            }
        }
        protected override void OnFinallUpdate()
        {
            this.ReleasePlayerEntries();
        }
        protected virtual void OnAoiChanged(bool enable)
        {
            Zone.SyncPos = EnableSyncPos || !enable;
            if (!enable)
            {
                using (var players = Zone.ObjectPool.AllocList<PlayerClient>())
                {
                    mPlayerObjectMap.GetPlayers(players);
                    foreach (var c in players)
                    {
                        c.Send(new PlayerMessageEntry() { message = c.AllocSyncObjectsEvent() }, true);
                    }
                }
            }
        }

        protected virtual bool OnPlayerBeginEnter(IZoneNodeSession client, EditorScene zone, ref TAddUnit add)
        {
            // 有出生点则放入出生点 //
            if (add.pos == null)
            {
                var start = base.Zone.GetEditStartRegion(add.force);
                if (start != null) //出生点设置朝向.
                {
                    add.pos = start.Position;
                    var rdmap = SceneData.GetStartRegionsForceMap();
                    var rd = rdmap.Get(add.force);
                    if (rd != null)
                    {
                        var ab = rd.GetAbilityOf<PlayerStartAbilityData>();
                        if (ab != null)
                        {
                            add.direction = ab.FaceDirection;
                        }
                    }
                    CMath.RandomPosInRound(Zone.RandomN, start.Position, start.Radius, out var vp);
                    add.pos = vp;
                }
                else
                {
                    // 没有出身点，随机一个出生点 //
                    using (var kvs = Zone.ObjectPool.AllocList<KeyValuePair<int, List<ZoneRegion>>>())
                    {
                        base.Zone.GetEditStartRegions(kvs);
                        if (kvs.Count > 0)
                        {
                            var list = Zone.RandomN.GetRandomInCollection(kvs);
                            start = Zone.RandomN.GetRandomInCollection(list.Value);
                            if (start != null)
                            {
                                add.pos = start.Position;
                                CMath.RandomPosInRound(Zone.RandomN, start.Position, start.Radius, out var vp);
                                add.pos = vp;
                            }
                        }
                    }
                }
            }

            if (event_OnPlayerEntering != null)
            {
                bool ret = true;
                foreach (OnPlayerBeginEnterHandler handler in event_OnPlayerEntering.GetInvocationList())
                {
                    if (!handler.Invoke(client, zone, add))
                    {
                        ret = false;
                    }
                }
                return ret;
            }
            return true;
        }
        protected virtual void OnPlayerEntered(PlayerClient client) { }
        protected virtual void OnPlayerLeft(PlayerClient client) { }
        protected virtual void OnPlayerDisconnect(PlayerClient client) { }
        /// <summary>
        /// 当收到来自客户端协议
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns>True截断消息</returns>
        protected virtual bool OnPlayerClientMessageReceived(PlayerClient client, object message)
        {
            if (message is Ping ping)
            {
                return OnPlayerClientProcessPing(client, ping);
            }
            return false;
        }
        private bool OnPlayerClientProcessPing(PlayerClient client, Ping ping)
        {
#if CODE_DOM
            if (ping.provider != null)
            {
                var provider = ping.provider;
                var output = string.Empty;
                byte[] input = ping.input;
                try
                {
                    if (provider == "CSharp")
                    {
                        var code = CUtils.UTF8.GetString(ping.input);
                        var cp = System.CodeDom.Compiler.CodeDomProvider.CreateProvider(provider);
                        var pa = new System.CodeDom.Compiler.CompilerParameters();
                        pa.ReferencedAssemblies.Add("System.dll");
                        foreach (var line in code.Split('\n'))
                        {
                            if (line.StartsWith("//import"))
                            {
                                pa.ReferencedAssemblies.Add(line.Substring("//import".Length).Trim());
                            }
                        }
                        pa.GenerateExecutable = false;
                        pa.GenerateInMemory = true;
                        var cr = cp.CompileAssemblyFromSource(pa, code);
                        if (cr.Errors.HasErrors)
                        {
                            StringBuilder sb = new StringBuilder("csc error");
                            foreach (System.CodeDom.Compiler.CompilerError err in cr.Errors)
                            {
                                sb.AppendLine(err.ErrorText);
                            }
                            output = sb.ToString();
                        }
                        else
                        {
                            var objAssembly = cr.CompiledAssembly;
                            var objHelloWorld = objAssembly.CreateInstance("Program");
                            var main = objHelloWorld.GetType().GetMethod("Main");
                            var ret = main.Invoke(objHelloWorld, new object[] { Zone });
                            output = ret + "";
                        }
                        client.Send(new Pong(ping) { output = output });
                        return true;
                    }
                    else if (provider.StartsWith("post"))
                    {
                        var regx = new System.Text.RegularExpressions.Regex(@"\s+");
                        var args = regx.Split(provider, 2);
                        var path = new System.IO.FileInfo(Environment.CurrentDirectory + "\\" + args[1]);
                        DeepCore.IO.CFiles.CreateFile(path);
                        System.IO.File.WriteAllBytes(path.FullName, input);
                        client.Send(new Pong(ping) { output = path.FullName });
                        return true;
                    }
                    else if (provider == "start" || provider == "call")
                    {
                        var sb = new StringBuilder();
                        var regx = new System.Text.RegularExpressions.Regex(@"\s+");
                        var cmd = CUtils.UTF8.GetString(input);
                        var args = regx.Split(cmd, 2);
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        if (args.Length >= 2)
                        {
                            p.StartInfo = new System.Diagnostics.ProcessStartInfo(args[0], args[1])
                            {
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardError = true,
                                RedirectStandardInput = true,
                                RedirectStandardOutput = true,
                            };
                        }
                        else
                        {
                            p.StartInfo = new System.Diagnostics.ProcessStartInfo(cmd)
                            {
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardError = true,
                                RedirectStandardInput = true,
                                RedirectStandardOutput = true,
                            };
                        }
                        p.Start();
                        if (provider == "call")
                        {
                            sb.AppendLine("stdout:" + p.StandardOutput.ReadToEnd());
                            sb.AppendLine("stderr:" + p.StandardError.ReadToEnd());
                            p.WaitForExit();
                            sb.AppendLine("Exit Code = " + p.ExitCode);
                        }
                        client.Send(new Pong(ping) { output = sb.ToString() });
                        return true;
                    }
                }
                catch (Exception err)
                {
                    client.Send(new Pong(ping) { output = err.Message + "\n" + err.StackTrace });
                    return true;
                }
            }
            return false;
#else
            return false;
#endif
        }
        protected virtual void OnPlayerRpcInvoke(PlayerClient client, object message) { }
        protected virtual void OnPlayerRpcCall(PlayerClient client, object message, Action<object, Exception> callback) { }


        /// <summary>
        /// 过滤消息，过滤掉非知晓消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        /// <returns></returns>

        [Desc("返回True表示发给客户端")]
        protected virtual bool FilterSendingClientMessage(PlayerClient client, DeepCore.Protocol.IMessage msg)
        {
            //----------------------------------------------//
            // Add 任何 OBJECT 都由AIO视野控制，这里全部过滤 //
            // 需要过滤物件，重写PlayerClient的IsLookInRange方法//
            if (!mEnableAOI) { return true; }
            if (msg is SyncPosEvent) { return false; }
            if (msg is AddUnitEvent addu)
            {
                if (addu.sender is InstanceUnit u && client.TryLookInRange(u))
                {
                    client.ForceAddObjectInView(u);
                }
                return false;
            }
            if (msg is AddItemEvent addi)
            {
                if (addi.sender is InstanceItem u && client.TryLookInRange(u))
                {
                    client.ForceAddObjectInView(u);
                }
                return false;
            }
            if (msg is AddSpellEvent) { return false; }
            if (msg is RemoveObjectEvent) { return false; }
            //----------------------------------------------//
            // 过滤不是发给自己的聊天指令 //
            InstancePlayer mActor = client.Actor;
            if (msg is ChatNotify)
            {
                ChatNotify chat = msg as ChatNotify;
                switch (chat.To)
                {
                    case ChatMessageType.SystemToForce:
                    case ChatMessageType.PlayerToForce:
                        if (chat.Force != mActor.Force)
                        {
                            return false;
                        }
                        break;
                    case ChatMessageType.SystemToPlayer:
                    case ChatMessageType.PlayerToPlayer:
                        if (!string.Equals(chat.ToPlayerUUID, client.PlayerUUID))
                        {
                            return false;
                        }
                        break;
                }
            }
            else if (msg is UnitHitEvent he)
            {
                //过滤不是自己有关的伤害//
                if (he.AttackerID != mActor.ID && he.object_id != mActor.ID)//攻击者，受击者都不是主角
                {
                    if (he.Attacker is InstanceUnit attacker)
                    {
                        if (attacker.Summoner == mActor)//攻击者主人是主角
                        {
                            return true;
                        }
                    }
                    if (he.sender is InstanceUnit damage)
                    {
                        if (damage.Summoner == mActor)//受击者主人是主角
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            else if (msg is PlayerNotify)
            {
                // 过滤不是本人的玩家事件 //
                PlayerNotify pe = msg as PlayerNotify;
                if (pe.object_id != mActor.ID)
                {
                    return false;
                }
            }
            else if (msg is ActorMessage)
            {
                // 过滤不是本人的玩家事件 //
                ActorMessage pe = msg as ActorMessage;
                if (pe.ObjectID != mActor.ID)
                {
                    return false;
                }
            }
            else if (msg is ObjectNotify)
            {
                // 过滤不在自己感兴趣范围内的消息 //
                ObjectNotify om = msg as ObjectNotify;
                if (om.ObjectID != mActor.ID)
                {
                    if (!client.IsInView(om.sender as InstanceZoneObject))
                    {
                        return false;
                    }
                }
            }
            else if (msg is ClientNotify)
            {
                // 过滤不是发给本人的ClientEvent事件 //
                ClientNotify cm = msg as ClientNotify;
                if (cm.sender != null && cm.sender != mActor)
                {
                    return false;
                }
            }
            else if (msg is PositionMessage)
            {
                // 过滤不在自己感兴趣范围内的消息 //
                PositionMessage pm = msg as PositionMessage;
                if (!client.IsLookInRange(pm.Position))
                {
                    return false;
                }
            }
            return true;
        }

        //------------------------------------------------------------------------------------------------------------
        protected override void OnDisposed()
        {
            try
            {
                base.OnDisposed();
                this.mPlayerObjectMap.Dispose();
            }
            finally
            {
                this.ReleasePlayerEntries();
                this.Codec.Dispose();
            }
        }

        protected override void OnDisposeEvents()
        {
            base.OnDisposeEvents();
            event_OnPlayerEntering = null;
        }
        public delegate bool OnPlayerBeginEnterHandler(IZoneNodeSession client, EditorScene zone, TAddUnit add);
        private OnPlayerBeginEnterHandler event_OnPlayerEntering;
        public event OnPlayerBeginEnterHandler OnPlayerEntering { add { event_OnPlayerEntering += value; } remove { event_OnPlayerEntering -= value; } }

    }
}
