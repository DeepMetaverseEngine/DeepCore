using DeepCore.Game3D.Slave.Layer;
using DeepCore.NetClient;
using Gate.Client.Battle;
using Gate.Data.Protocol;
using System;

namespace Gate.Client.Modules
{
    public class AreaModule : MMOClientModule<MMOClient>
    {
        protected GateBattle current_battle;
        protected GateBattle next_battle;

        public GateBattle CurrentBattle
        {
            get => current_battle;
        }
        public GateBattle NextBattle
        {
            get => next_battle;
        }
        public int CurrentBattlePing
        {
            get { return CurrentBattle != null ? CurrentBattle.CurrentPing : 0; }
        }
        public LayerZone CurrentZoneLayer
        {
            get { return CurrentBattle != null ? CurrentBattle.Layer : null; }
        }
        public LayerPlayer CurrentZoneActor
        {
            get { return CurrentBattle != null ? CurrentBattle.Actor : null; }
        }
        public bool IsDelayReleaseBattleClient { get; set; }

        public AreaModule(MMOClient client) : base(client)
        {
            this.Client.GameSession.Listen<ClientEnterZoneNotify>(Area_OnClientEnterZoneNotify);
            this.Client.GameSession.Listen<ClientLeaveZoneNotify>(Area_OnClientLeaveZoneNotify);
            this.Client.GameSession.Listen<ClientBattleEvent>(Area_OnClientBattleEvent);
        }
        protected override void OnDisposing()
        {
            event_OnZoneChanged = null;
            event_OnZoneLeaved = null;
            event_OnZoneActorEntered = null;
        }
        protected override void OnEnterGame(ClientEnterGameResponse enter)
        {
        }
        protected override void OnGameClientDisconnected(CloseReason close)
        {
        }
        protected override void BeginUpdate(float intervalMS)
        {
            if (current_battle != null)
            {
                current_battle.BeginUpdate(intervalMS);
            }
            base.BeginUpdate(intervalMS);
        }
        protected override void Update(float intervalMS)
        {
            base.Update(intervalMS);
            if (current_battle != null)
            {
                current_battle.Update();
            }
        }

        protected virtual void Area_OnClientBattleEvent(ClientBattleEvent notify)
        {
            if (NextBattle != null)
            {
                NextBattle.OnReceived(notify);
            }
            else if (CurrentBattle != null)
            {
                CurrentBattle.OnReceived(notify);
            }
            else
            {
                log.Error("Battle Not Init !!!");
            }
        }
        protected virtual void Area_OnClientEnterZoneNotify(ClientEnterZoneNotify notify)
        {
            if (!IsDelayReleaseBattleClient && current_battle != null)
            {
                current_battle.Dispose();
                current_battle = null;
            }
            log.Info("ClientEnterZoneNotify : " + notify);
            var battle = MMOClientManager.Instance.Battle.CreateBattle(Client, notify);
            battle.Layer.ActorAdded += Layer_ActorAdded;

            if (current_battle == null || !IsDelayReleaseBattleClient)
            {
                current_battle = battle;
            }
            else
            {
                next_battle = battle;
            }
            if (event_OnZoneChanged != null) event_OnZoneChanged(battle);
        }
        protected virtual void Area_OnClientLeaveZoneNotify(ClientLeaveZoneNotify notify)
        {
            log.Info("ClientLeaveZoneNotify : " + notify);
            if (event_OnZoneLeaved != null) event_OnZoneLeaved(current_battle);
            if (current_battle != null) { current_battle.Dispose(); current_battle = null; }
        }
        protected virtual void Layer_ActorAdded(LayerZone layer, LayerPlayer actor)
        {
            if (next_battle != null)
            {
                if (current_battle != null)
                {
                    current_battle.Dispose();
                }
                current_battle = next_battle;
                next_battle = null;
            }
            if (event_OnZoneActorEntered != null)
                event_OnZoneActorEntered(actor);
        }



        private Action<GateBattle> event_OnZoneChanged;
        private Action<GateBattle> event_OnZoneLeaved;
        private Action<LayerPlayer> event_OnZoneActorEntered;

        public event Action<GateBattle> OnZoneEnter { add { event_OnZoneChanged += value; } remove { event_OnZoneChanged -= value; } }
        public event Action<GateBattle> OnZoneLeave { add { event_OnZoneLeaved += value; } remove { event_OnZoneLeaved -= value; } }
        public event Action<LayerPlayer> OnZoneActorEntered { add { event_OnZoneActorEntered += value; } remove { event_OnZoneActorEntered -= value; } }

    }
}
