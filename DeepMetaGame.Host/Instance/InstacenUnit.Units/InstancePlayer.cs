using DeepCore.EventTrigger;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.IO;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{

    /// <summary>
    /// 可自动战斗的可操作单位
    /// </summary>
    public partial class InstancePlayer : InstanceUnit
    {
        //-------------------------------------------------------------------------------------------------------------------------

        protected SyncMode mCurrentSyncMode = SyncMode.MoveByClient_PreSkillByClient;
        protected bool mIsSkillControlByServer = true;
        //protected Vector.Vector3 mLastForceSyncPos;

        public PlayerQuestComponent QuestComponent { get; protected set; }

        public string ClientID { get; set; }
        public bool IsReady { get; protected set; }

        public override bool IsPlayer { get { return true; } }
        public override bool IntersectObj { get { return (CFG.PLAYER_NONE_TOUCH) ? false : base.IntersectObj; } }
        public override bool IsSkillControllableByServer { get { return mIsSkillControlByServer; } }
        public SyncMode ClientSyncMode { get { return mCurrentSyncMode; } }

        public override bool EnableSyncSkill => true;

        public InstancePlayer(InstanceZone zone, TAddUnit add)
            : base(zone, add)
        {
            this.PlayerEnvironmentVarMap = new EnvironmentVarMap<InstancePlayer>(this);
            this.PlayerEnvironmentVarMap.OnEnvironmentVarChangeHandler += PlayerEnvironmentVarMap_OnEnvironmentVarChangeHandler;
            this.mControlMove = new StatePlayerControlMove(this);
            this.mControlUpdateMove = new StatePlayerUpdateMove(this);
            this.mCustomAction = new StatePlayerCustomAction(this);
            this.mCustomControlMove = new StatePlayerCustomControlMove(this);
            this.mClimb = new StatePlayerClimb(this);
            this.mPos.MoveKeepInColor = false;
        }

        protected override void Disposing()
        {
            mPlayerTransportScene = null;
            this.mControlMove?.Dispose();
            this.mControlUpdateMove?.Dispose();
            this.mCustomAction?.Dispose();
            this.mCustomControlMove?.Dispose();
            this.mClimb?.Dispose();
            base.Disposing();
        }

        protected override void updatePosEnd(UnitSyncPos cache)
        {
            base.updatePosEnd(cache);
        }
        //         protected override void updatePhysical()
        //         {
        //             if (IsReady)
        //             {
        //                 base.updatePhysical();
        //             }
        //         }
        protected virtual UnitComponent CreateGuard()
        {
            return new PlayerAIComponent();
        }

        public override void ResetAI()
        {
            this.mIsSkillControlByServer = IsGuard || (mCurrentSyncMode != SyncMode.MoveByClient_PreSkillByClient);
            base.ResetAI();
        }

        /// <summary>
        /// 获得化玩家上下线或者切换场景，需要带到逻辑服务器或者需要存储的数据。
        /// 该数据最初值由 AddUnit.last_zone_save_data 初始化。
        /// </summary>
        /// <returns></returns>
        public virtual ISerializable GetLastZoneSaveData()
        {
            return null;
        }


        /// <summary>
        /// 联网模式，断开连接。
        /// </summary>
        public virtual void OnDisconnected()
        {
            IsReady = false;
        }
        /// <summary>
        /// 联网模式，重新连接。
        /// </summary>
        public virtual void OnReconnected(TAddUnit? add)
        {
            this.mCustomAction.OnReconnected();
            this.mControlMove.OnReconnected();
            this.mCustomControlMove.OnReconnected();
            this.mControlUpdateMove.OnReconnected();

            //this.mControlJump.OnReconnected();
            //this.mClimb.OnReconnected();

            // TODO add.last_zone_save_data
        }

        /// <summary>
        /// 联网，连接成功.
        /// </summary>
        /// <param name="add"></param>
        public virtual void OnConnected(TAddUnit? add)
        {
            // TODO add.last_zone_save_data
        }

        public override void SendForceSync()
        {
            base.SendForceSync();
            //mLastForceSyncPos = new Vector.Vector3(X, Y, Z);
        }
        public void SetSyncMode(SyncMode act)
        {
            this.mCurrentSyncMode = act;
            this.mIsSkillControlByServer = IsGuard || (mCurrentSyncMode != SyncMode.MoveByClient_PreSkillByClient);
        }
        public virtual LockActorEvent AllocLockActorEvent(string displayName, float inRange, float outRange, float updateInterval)
        {
            // 准备发送当前场景信息 //
            var loc = ObjectPool.Alloc<LockActorEvent>();
            loc.sender = this;
            loc.ServerUpdateInterval = updateInterval;
            loc.ClientSyncObjectRange = inRange;
            loc.ClientSyncObjectOutRange = outRange;
            loc.UnitData = IOUtil.CloneObject<SyncUnitInfo>(DataFactory.PersistCodec, this.GenSyncUnitInfo(true));
            loc.UnitData.Name = displayName;
            loc.UnitData.UType = this.UType;
            loc.UnitData.pos.X = this.X;
            loc.UnitData.pos.Y = this.Y;
            loc.UnitData.pos.Z = this.Z;
            loc.GameServerProp = Parent.CloneData(this.Info.Properties);
            loc.Skills = this.AllocSkillEvent();
            loc.ClientSyncMode = this.ClientSyncMode;
            this.GetCurrentBuffStatus(loc.UnitData.CurrentBuffStatus);
            this.GetCurrentAuraStatus(loc.UnitData.CurrentAuraStatus);
            this.Cartridge.GetCurrentCardStatus(loc.UnitData.CurrentCardStatus);
            this.GetCurrentSkillStatus(loc.CurrentSkillStatus);
            this.GetCurrentItemStatus(loc.CurrentItemStatus);
            this.GetCurrentUnitVars(loc.CurrentUnitVars);
            this.GetCurrentPlayerVars(loc.CurrentPlayerVars);
            Parent.GetCurrentZoneVars(loc.CurrentZoneVars);
            return loc;
        }

        protected override void onAdded()
        {
            var startRegion = Zone.GetRegionWithObject(this);
            if (startRegion != null)
            {
                startRegion.addInRegionViewed(this);
            }
            base.onAdded();
            this.DoSomething();
        }

        override protected void onUpdateAI()
        {
            base.onUpdateAI();
            this.updateCurrentSkill();
            this.updateCustomMove();
        }

        private void updateCustomMove()
        {
            mCustomControlMove?.update();
        }

        protected override void DoDefaultBehavior()
        {
            if (mWaitingSkill != null && CurrentState is StateSkill)
            {
                if (doLaunchSkill(mWaitingSkill).IsLaunched)
                {
                    return;
                }
            }
            if (mCurrentSyncMode != SyncMode.MoveByClient_PreSkillByClient)
            {
                if (mControlMove.IsMove)
                    ChangeState(mControlMove);
                else
                    base.DoDefaultBehavior();
            }
            else
            {
                if (mControlUpdateMove.IsMove)
                    ChangeState(mControlUpdateMove);
                else
                    base.DoDefaultBehavior();
            }
        }
        protected virtual void updateCurrentSkill()
        {
            if (CurrentState is StateSkill)
            {
                var current = CurrentState as StateSkill;
                if (mWaitingSkill != null && current.IsCancelableBySkill)
                {
                    doLaunchSkill(mWaitingSkill);
                }
            }
        }
        public virtual bool IsQuestAccepted(string quest)
        {
            if (QuestComponent != null)
            {
                return QuestComponent.IsQuestAccepted(quest);
            }
            return false;
        }
        // --------------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------------
        #region Events


        public delegate void TransportSceneHandler(InstancePlayer player, InstanceFlag flag, int nextSceneID, string nextScenePosition);
        [EventTriggerDesc("玩家跨场景传送")]
        public event TransportSceneHandler OnTransportScene { add { mPlayerTransportScene += value; } remove { mPlayerTransportScene -= value; } }

        private TransportSceneHandler mPlayerTransportScene;

        internal void callback_onTransportScene(InstanceFlag flag, int nextSceneID, string nextScenePosition)
        {
            if (mPlayerTransportScene != null)
            {
                mPlayerTransportScene.Invoke(this, flag, nextSceneID, nextScenePosition);
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------------//

        #region Environment

        //-----------------------------------------------------------------------------------------------------//

        public EnvironmentVarMap<InstancePlayer> PlayerEnvironmentVarMap { get; }
        private void PlayerEnvironmentVarMap_OnEnvironmentVarChangeHandler(InstancePlayer st, string key, EnvironmentVar var, object value, bool syncToClient)
        {
            if (EnvironmentVar.ALWAYS_SYNC_ENVIRONMENT_VAR || var.SyncToClient || syncToClient)
            {
                PostEvent(ObjectPool.Alloc<PlayerSyncEnvironmentVarEvent>().Init(ID, new ClientStruct.ZoneEnvironmentVar()
                {
                    Key = key,
                    Value = HostFactory.EncodeZoneVar(value),
                    SyncToClient = syncToClient,
                }));
                base.cb_EnvironmentVarMapChanged(st, key, var, value, syncToClient);
            }
        }

        public void SetPlayerEnvironmentVar(string key, object value, bool syncToClient = true)
        {
            PlayerEnvironmentVarMap.SetEnvironmentVar(key, value, syncToClient);
        }
        public T GetPlayerEnvironmentVarAs<T>(string key)
        {
            return PlayerEnvironmentVarMap.GetEnvironmentVarAs<T>(key);
        }
        public bool TryGetPlayerEnvironmentVar(string key, out object value)
        {
            return PlayerEnvironmentVarMap.TryGetEnvironmentVar(key, out value);
        }
        public bool TryGetPlayerEnvironmentVarAs<T>(string key, out T value)
        {
            return PlayerEnvironmentVarMap.TryGetEnvironmentVarAs(key, out value);
        }
        public int ListPlayerEnvironmentVars(List<EnvironmentVar> list)
        {
            return PlayerEnvironmentVarMap.ListEnvironmentVars(list);
        }
        public List<EnvironmentVar> ListPlayerEnvironmentVars()
        {
            return PlayerEnvironmentVarMap.ListEnvironmentVars();
        }

        public void GetCurrentPlayerVars(IList<ClientStruct.ZoneEnvironmentVar> ret)
        {
            int i = 0;
            foreach (EnvironmentVar var in PlayerEnvironmentVarMap.Values)
            {
                var o = new ClientStruct.ZoneEnvironmentVar();
                {
                    o.Key = var.Key;
                    o.SyncToClient = var.SyncToClient;
                    if (var.SyncToClient)
                    {
                        o.Value = var.Value;
                    }
                }
                ret.Add(o);
                i++;
            }
        }

        #endregion

        //-----------------------------------------------------------------------------------------------------//


    }

}
