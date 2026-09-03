using DeepCore.EventTrigger;
using DeepCore.Game3D.Host.FuncData;
using DeepCore.Game3D.Host.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceUnit
    {
        protected override void ClearEvents()
        {
            base.ClearEvents();
            this.OnEnvironmentVarChangeHandler = null;
            this.OnFieldChange = null;
            this.OnUpdate = null;
            this.OnHandleAction = null;
            this.OnRemoved = null;
            this.OnActivated = null;
            this.OnFirstActivated = null;
            this.OnDead = null;
            this.OnDamage = null;
            this.OnAttack = null;
            this.OnRebirth = null;
            this.OnHandleResetAI = null;
            this.OnGotInstanceItem = null;
            this.OnGotInventoryItem = null;
            this.OnLostInventoryItem = null;
            this.OnUseItem = null;
            this.OnGotBuff = null;
            this.OnTransport = null;
            this.OnStateChanged = null;
            this.OnSkillChanged = null;
            this.OnLaunchSkill = null;
            this.OnTryPickUnit = null;
            this.OnPickUnit = null;
            this.OnTryAddSkill = null;
            this.OnSkillAdded = null;
            this.OnSkillRemoved = null;
            this.OnLaunchAura = null;
            this.OnEnterAura = null;
            this.OnLeaveAura = null;
            this.OnUnitLevelUp = null;
            this.OnStateStart = null;
            this.OnStateStop = null;
            this.TryBlockState = null;
            this.OnUpdateAI = null;
            this.event_OnDoSomething?.Dispose();
            this.event_OnDoSomething = null;
            this.OnBindUnitEvt = null;
            this.OnUnBindUnitEvt = null;
//             this.__mCurrentHP?.Dispose();
//             this.__mCurrentMP?.Dispose();
//             this.__mCurrentSP?.Dispose();
        }

        //----------------------------------------------------------------------------------------------------
        #region Unit

        //----------------------------------------------------------------------------------------------------
        public event Action<InstanceUnit, string, object> OnEnvironmentVarChangeHandler;
        //----------------------------------------------------------------------------------------------------
        public delegate void FieldChangeHandler(InstanceUnit sender, UnitFieldMask mask, object value);
        public event FieldChangeHandler OnFieldChange;
        //----------------------------------------------------------------------------------------------------
        public delegate void UpdateHandler(InstanceUnit unit);
        public event UpdateHandler OnUpdate;
        public event UpdateHandler OnUpdateAI;
        //----------------------------------------------------------------------------------------------------
        public delegate void HandleActionHandler(InstanceUnit sender, ObjectAction act);
        public event HandleActionHandler OnHandleAction;
        //----------------------------------------------------------------------------------------------------
        internal void callback_onResetAI() { OnHandleResetAI?.Invoke(this); }
        public delegate void ResetAIHandler(InstanceUnit sender);
        public event ResetAIHandler OnHandleResetAI;
        public delegate bool DoSomethingHandler(InstanceUnit sender, bool handed);
        public event DoSomethingHandler OnDoSomething
        {
            add { event_OnDoSomething += value; }
            remove { event_OnDoSomething -= (value); }
        }
        private MultiCastInvoker<DoSomethingHandler> event_OnDoSomething;
        //----------------------------------------------------------------------------------------------------
        public delegate void RemovedHandler(InstanceUnit sender);
        [EventTriggerDescAttribute("单位被移除时触发")] public event RemovedHandler OnRemoved;
        //----------------------------------------------------------------------------------------------------

        public delegate void ActiveChangedHandler(InstanceUnit sender, bool active);
        public delegate void ActivatedHandler(InstanceUnit sender);


        [EventTriggerDesc("当单位可攻击时触发，如果单位有出生动画(SpawnTimeMS>0)，则动画完结后触发")] public event ActivatedHandler OnActivated;
        [EventTriggerDesc("当单位可攻击时触发，如果单位有出生动画(SpawnTimeMS>0)，则动画完结后触发")] public event ActivatedHandler OnFirstActivated;

        [EventTriggerDesc("当单位可攻击时触发，如果单位有出生动画(SpawnTimeMS>0)，则动画完结后触发")] public event ActiveChangedHandler OnActiveChanged;
        protected internal virtual void callback_onActivated()
        {
            if (first_active)
            {
                OnFirstActivated?.Invoke(this);
                OnFirstActivated = null;
            }
            this.OnActivated?.Invoke(this);
        }
        protected internal virtual void cb_ActiveChanged(bool active) { OnActiveChanged?.Invoke(this, active); }

        //----------------------------------------------------------------------------------------------------
        internal void callback_onDead(InstanceUnit attacker) { this.OnDead?.Invoke(this, attacker); }
        public delegate void DeadHandler(InstanceUnit sender, InstanceUnit attacker);
        [EventTriggerDescAttribute("单位死亡时触发")] public event DeadHandler OnDead;
        internal void callback_onKill(InstanceUnit dead) { this.OnKill?.Invoke(this, dead); }
        public delegate void KillHandler(InstanceUnit sender, InstanceUnit dead);
        [EventTriggerDescAttribute("单位杀人时触发")] public event KillHandler OnKill;
        //----------------------------------------------------------------------------------------------------
        internal void callback_onDamage(InstanceUnit attacker, long reduceHP, in TAttackSource source, in TAttackResult result) { this.OnDamage?.Invoke(this, attacker, reduceHP, in source, in result); }
        public delegate void DamageHandler(InstanceUnit sender, InstanceUnit attacker, long hp, in TAttackSource source, in TAttackResult result);
        [EventTriggerDescAttribute("单位受到伤害时触发")] public event DamageHandler OnDamage;
        //----------------------------------------------------------------------------------------------------
        internal void callback_onAttack(InstanceUnit target, long reduceHP, in TAttackSource source, in TAttackResult result) { this.OnAttack?.Invoke(this, target, reduceHP, in source, in result); }
        public delegate void AttackHandler(InstanceUnit sender, InstanceUnit target, long hp, in TAttackSource source, in TAttackResult result);
        [EventTriggerDescAttribute("单位攻击别人时触发")] public event AttackHandler OnAttack;
        //----------------------------------------------------------------------------------------------------
        internal void callback_onRebirth() { this.OnRebirth?.Invoke(this); }
        public delegate void RebirthHandler(InstanceUnit sender);
        [EventTriggerDescAttribute("单位复活时触发")] public event RebirthHandler OnRebirth;
        //----------------------------------------------------------------------------------------------------
        internal void callback_onGotBuff(InstanceUnit.EquipBuff buff) { this.OnGotBuff?.Invoke(this, buff); }
        public delegate void GotBuffHandler(InstanceUnit sender, InstanceUnit.EquipBuff buff);
        [EventTriggerDescAttribute("单位获得BUFF时触发")] public event GotBuffHandler OnGotBuff;


        internal void callback_onRemoveBuff(InstanceUnit.EquipBuff buff) { this.OnRemoveBuff?.Invoke(this, buff); }
        public delegate void RemoveBuffHandler(InstanceUnit sender, InstanceUnit.EquipBuff buff);
        [EventTriggerDescAttribute("单位移除BUFF时触发")] public event RemoveBuffHandler OnRemoveBuff;
        //----------------------------------------------------------------------------------------------------
        public delegate void StateChangedHandler(InstanceUnit sender, State old_state, State new_state);
        [EventTriggerDescAttribute("单位状态机改变时触发")] public event StateChangedHandler OnStateChanged;
        //----------------------------------------------------------------------------------------------------
        public delegate bool OnTryPickUnitHandler(InstanceUnit sender, InstanceUnit picking);
        [EventTriggerDescAttribute("尝试Pick单位")] public event OnTryPickUnitHandler OnTryPickUnit;
        //----------------------------------------------------------------------------------------------------
        public delegate void PickUnitHandler(InstanceUnit sender, InstanceUnit pickable);
        [EventTriggerDescAttribute("单位和其他单位产生交互时触发")] public event PickUnitHandler OnPickUnit;
        //----------------------------------------------------------------------------------------------------
        internal void callback_onTransport(Geometry.Vector3 oldpos) { this.OnTransport?.Invoke(this, oldpos); }
        public delegate void TransportHandler(InstanceUnit sender, Geometry.Vector3 oldpos);
        [EventTriggerDescAttribute("单位场景内传送")] public event TransportHandler OnTransport;
        //----------------------------------------------------------------------------------------------------
        internal void callback_onBindEvent(UnitEventTemplate template) { OnBindUnitEvt?.Invoke(this, template); }
        public delegate void BindUnitEventHandler(InstanceUnit sender, UnitEventTemplate uet);
        [EventTriggerDescAttribute("单位绑定事件")] public event BindUnitEventHandler OnBindUnitEvt;
        //----------------------------------------------------------------------------------------------------
        internal void callback_onUnBindEvent(int id) { OnUnBindUnitEvt?.Invoke(this, id); }
        public delegate void UnBindUnitEventHandler(InstanceUnit sender, int id);
        [EventTriggerDescAttribute("单位解除绑定事件")] public event UnBindUnitEventHandler OnUnBindUnitEvt;
        //----------------------------------------------------------------------------------------------------
        internal void cb_launchSpell(InstanceSpell spell, TAddSpell add) { OnLaunchSpell?.Invoke(this, spell, add); }
        public delegate void LaunchSpellHandler(InstanceUnit sender, InstanceSpell spell, TAddSpell add);
        [EventTriggerDesc("LaunchSpell触发")]
        public event LaunchSpellHandler OnLaunchSpell;
        //----------------------------------------------------------------------------------------------------
        internal void cb_removeSpell(InstanceSpell spell) { OnRemoveSpell?.Invoke(this, spell); }
        public delegate void RemoveSpellHandler(InstanceUnit sender, InstanceSpell spell);
        [EventTriggerDesc("LaunchSpell触发")] public event RemoveSpellHandler OnRemoveSpell;
        //----------------------------------------------------------------------------------------------------
        internal void cb_OnUnitLevelUp() { OnUnitLevelUp?.Invoke(this); }
        public delegate void UnitLevelUpHandler(InstanceUnit sender);
        [EventTriggerDesc("单位升级")] public event UnitLevelUpHandler OnUnitLevelUp;
        //----------------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------------------------

        public delegate bool TryBlockStateAction(InstanceUnit sender, State newState, State oldState);
        [EventTriggerDesc("测试打断状态机")] public event TryBlockStateAction TryBlockState;

        public delegate void RefreshDataHandler(InstanceUnit sender, UnitInfo data);
        [EventTriggerDesc("刷新数据")] public event RefreshDataHandler OnRefreshData;
        internal void cb_OnRefreshData(UnitInfo data) { OnRefreshData?.Invoke(this, data); }

        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Item

        internal bool callback_onGotInstanceItem(InstanceItem item)
        {
            if (this.OnGotInstanceItem != null)
                return this.OnGotInstanceItem.Invoke(this, item);
            return true;
        }
        public delegate bool GotInstanceItemHandler(InstanceUnit obj, InstanceItem item);
        [EventTriggerDescAttribute("单位获得物品时触发")]
        public event GotInstanceItemHandler OnGotInstanceItem;

        internal void callback_onGotInventoryItem(ItemTemplate item) { this.OnGotInventoryItem?.Invoke(this, item); }
        public delegate void GotInventoryItemHandler(InstanceUnit obj, ItemTemplate item);
        [EventTriggerDescAttribute("单位获得物品进入背包时触发")]
        public event GotInventoryItemHandler OnGotInventoryItem;

        internal void callback_onLostInventoryItem(ItemTemplate item) { this.OnLostInventoryItem?.Invoke(this, item); }
        public delegate void LostInventoryItemHandler(InstanceUnit obj, ItemTemplate item);
        [EventTriggerDescAttribute("单位丢掉背包中的物品时触发")]
        public event LostInventoryItemHandler OnLostInventoryItem;
        internal void callback_onUseItem(ItemTemplate item, InstanceUnit item_creater) { this.OnUseItem?.Invoke(this, item, item_creater); }
        public delegate void UseItemHandler(InstanceUnit obj, ItemTemplate item, InstanceUnit item_creater);
        [EventTriggerDescAttribute("单位使用物品时触发(包括捡到物品立即使用)")]
        public event UseItemHandler OnUseItem;

        #endregion
        //----------------------------------------------------------------------------------------------------
        #region State
        public delegate void StateStartHandler(InstanceUnit sender, State state);
        public delegate void StateStopHandler(InstanceUnit sender, State state);
        [EventTriggerDesc("状态机每次开始时触发")] public event StateStartHandler OnStateStart;
        [EventTriggerDesc("状态机每次结束时触发")] public event StateStopHandler OnStateStop;
        internal void cb_StateStart(State state)
        {
            OnStateStart?.Invoke(this, state);
        }
        internal void cb_StateStop(State state)
        {
            OnStateStop?.Invoke(this, state);
        }

        public delegate bool BlockOtherGetawayHandler(InstanceUnit sender, InstanceUnit other);
        [EventTriggerDesc("阻挡到其他单位移动")] public event BlockOtherGetawayHandler OnBlockOtherGetaway;
        /// <returns >通知其他单位自己可以让开</returns>
        internal bool cb_OnBlockOtherGetaway(InstanceUnit other)
        {
            if (OnBlockOtherGetaway != null)
            {
                var invokes = OnBlockOtherGetaway.GetInvocationList();
                var ret = false;
                foreach (BlockOtherGetawayHandler invoke in invokes)
                {
                    ret |= invoke(this, other);
                }
                return ret;
            }
            return false;
        }
        public delegate void MoveBlockWithObjectHandler(InstanceUnit sender, IEntityObject other);
        [EventTriggerDesc("在移动时被某个单位阻挡")] public event MoveBlockWithObjectHandler OnMoveBlockWithObject;
        internal void cb_onMoveBlockWithObject(IEntityObject obj)
        {
            OnMoveBlockWithObject?.Invoke(this, obj);
        }
        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Skill


        public delegate void SkillChangedHandler(InstanceUnit obj, EquipSkill baseSkill, IReadOnlyDictionary<int, EquipSkill> skills);
        [EventTriggerDescAttribute("单位技能发生变化时触发")]
        public event SkillChangedHandler OnSkillChanged;

        public delegate bool TryAddSkill(InstanceUnit unit, ref SkillTemplate sk);
        [EventTriggerDescAttribute("单位尝试添加技能，可重置技能属性")] public event TryAddSkill OnTryAddSkill;

        public delegate bool TryLaunchSkill(InstanceUnit unit, InstanceUnit.EquipSkill skill, ref InstanceUnit.TLaunchSkillParam param);
        [EventTriggerDescAttribute("单位尝试释放技能")] public event TryLaunchSkill OnTryLaunchSkill;
        internal bool cb_TryLaunchSkill(InstanceUnit.EquipSkill skill, ref InstanceUnit.TLaunchSkillParam param)
        {
            if (OnTryLaunchSkill != null)
            {
                foreach (TryLaunchSkill invoke in OnTryLaunchSkill.GetInvocationList())
                {
                    if (invoke(this, skill, ref param) == false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public delegate void SkillAdded(InstanceUnit unit, EquipSkill sk);
        [EventTriggerDescAttribute("单位获得技能")]
        public event SkillAdded OnSkillAdded;

        public delegate void SkillRemoved(InstanceUnit unit, EquipSkill sk);
        [EventTriggerDescAttribute("单位移除技能")]
        public event SkillRemoved OnSkillRemoved;

        internal void callback_OnLaunchSkill(EquipSkill st, StateSkill state) => OnLaunchSkill?.Invoke(this, st, state);
        public delegate void OnLaunchSkillHandler(InstanceUnit obj, EquipSkill skill, StateSkill st);
        [EventTriggerDescAttribute("单位释放技能")]
        public event OnLaunchSkillHandler OnLaunchSkill;

        internal void callback_OnOverSkill(EquipSkill st, StateSkill state) { OnOverSkill?.Invoke(this, st, state); }
        public delegate void OnOverSkillHandler(InstanceUnit obj, EquipSkill skill, StateSkill st);
        [EventTriggerDescAttribute("单位结束技能")]
        public event OnOverSkillHandler OnOverSkill;

        //----------------------------------------------------------------------------------------------------
        public delegate void UnitSpellHittedHandler(InstanceUnit unit, InstanceSpell spell, InstanceUnit target, TAttackSource attackSource);
        public event UnitSpellHittedHandler UnitSpellHitted;
        internal void cb_unitSpellHitted(InstanceSpell spell, InstanceUnit target, TAttackSource attackSource)
        {
            UnitSpellHitted?.Invoke(this, spell, target, attackSource);
            Parent.cb_unitSpellHitted(this, spell, target, attackSource);
        }
        public delegate void UnitSpellFirstHittedHandler(InstanceUnit unit, InstanceSpell spell, InstanceUnit target, TAttackSource attackSource);
        public event UnitSpellFirstHittedHandler UnitSpellFirstHitted;
        internal void cb_unitSpellFirstHitted(InstanceSpell spell, InstanceUnit target, TAttackSource attackSource)
        {
            UnitSpellFirstHitted?.Invoke(this, spell, target, attackSource);
            Parent.cb_unitSpellFirstHitted(this, spell, target, attackSource);
        }
        //----------------------------------------------------------------------------------------------------
        public delegate void UnitSpellLaunchSpellHandler(InstanceUnit unit, InstanceSpell sender, TAddSpell add);
        public event UnitSpellLaunchSpellHandler UnitSpellLaunchSpell;
        internal void cb_unitSpellLaunchSpell(InstanceSpell sender, TAddSpell add)
        {
            UnitSpellLaunchSpell?.Invoke(this, sender, add);
            Parent.cb_unitSpellLaunchSpell(this, sender, add);
        }
        //----------------------------------------------------------------------------------------------------
        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Aura


        internal void cb_OnUnitLaunchAura(InstanceUnit.EquipAura aura) { OnLaunchAura?.Invoke(this, aura); }
        public delegate void LaunchAuraHandler(InstanceUnit u, InstanceUnit.EquipAura aura);
        [EventTriggerDescAttribute("单位释放光环")]
        public event LaunchAuraHandler OnLaunchAura;

        internal void cb_OnUnitEnterAura(InstanceUnit.EquipAura aura) { OnEnterAura?.Invoke(this, aura); }
        public delegate void EnterAuraHandler(InstanceUnit u, InstanceUnit.EquipAura aura);
        [EventTriggerDescAttribute("单位进入光环")]
        public event EnterAuraHandler OnEnterAura;

        internal void cb_OnUnitLeaveAura(InstanceUnit.EquipAura aura) { OnLeaveAura?.Invoke(this, aura); }
        public delegate void LeaveAuraHandler(InstanceUnit u, InstanceUnit.EquipAura aura);
        [EventTriggerDescAttribute("单位离开光环")]
        public event LeaveAuraHandler OnLeaveAura;


        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Card
        //public delegate bool TryPutCardHandler(InstanceUnit sender, UnitCartridge cartridge, CardSlot slot, CardTemplate card);
        public delegate void CardsChangedHandler(InstanceUnit sender, UnitCartridge cartridge);
        public event CardsChangedHandler OnCardsChanged;
        //public event TryPutCardHandler TryPutCard;
        protected internal virtual void cb_OnCardsChanged(UnitCartridge cartridge)
        {
            this.PostEvent(ObjectPool.Alloc<PlayerSyncCardsEvent>().Init (this.ObjectID, cartridge.OwnerFuncs));
            OnCardsChanged?.Invoke(this, cartridge);
        }
        //         protected internal virtual bool cb_TryPutCard(UnitCartridge cartridge, CardSlot slot, CardTemplate card)
        //         {
        //             if (TryPutCard != null)
        //                 return TryPutCard.Invoke(this, cartridge, slot, card);
        //             return true;
        //         }

        public delegate void CardAddHandler(InstanceUnit sender, UnitCartridge cartridge, UnitCartridge.UnitCardSlot card);
        public delegate void CardRemoveHandler(InstanceUnit sender, UnitCartridge cartridge, UnitCartridge.UnitCardSlot card);
        public delegate void CardLevelChangeHandler(InstanceUnit sender, UnitCartridge cartridge, UnitCartridge.UnitCardSlot card, int oldLevel);

        public event CardAddHandler OnCardAdd;
        public event CardRemoveHandler OnCardRemove;
        public event CardLevelChangeHandler OnCardLevelChange;
        protected internal virtual void cb_OnCardsAdded(UnitCartridge cartridge, UnitCartridge.UnitCardSlot slot)
        {
            OnCardAdd?.Invoke(this, cartridge, slot);
        }
        protected internal virtual void cb_OnCardsRemove(UnitCartridge cartridge, UnitCartridge.UnitCardSlot slot)
        {
            OnCardRemove?.Invoke(this, cartridge, slot);
        }
        protected internal virtual void cb_OnCardsLevelChange(UnitCartridge cartridge, UnitCartridge.UnitCardSlot slot, int oldLevel)
        {
            OnCardLevelChange?.Invoke(this, cartridge, slot, oldLevel);
        }

        #endregion
        //----------------------------------------------------------------------------------------------------
    }
}
