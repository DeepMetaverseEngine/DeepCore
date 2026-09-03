using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Debug;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Abilities;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.Game3D.Host.Instance
{
    public delegate void ZoneEventHandler<T>(InstanceZone zone, T msg);
    public delegate void ZoneEventHandler<T1, T2>(InstanceZone zone, T1 arg1, T2 arg2);
    public delegate void ZoneEventHandler<T1, T2, T3>(InstanceZone zone, T1 arg1, T2 arg2, T3 arg3);

    partial class InstanceZone
    {
        protected virtual void ClearEvents()
        {
            this.OnFirstUpdate = null;
            this.event_OnUpdate = null;
            this.event_OnInit = null;
            this.event_OnObjectPosChanged = null;
            this.event_OnObjectSpaceChanged = null;
            //this.event_OnObjectAreaChanged = null;

            this.event_OnObjectAdded = null;
            this.event_OnObjectRemoved = null;

            this.event_OnPostEvent = null;
            //             this.event_OnSendToGS = null;
            //             this.event_OnRecvFromGS = null;
            //             this.event_OnSendMessageB2REvent = null;
            this.event_OnProcessZoneAction = null;

            this.OnUnitActivated = null;
            this.OnUnitFirstActivated = null;


            this.event_OnUnitAdded = null;
            this.event_OnUnitRemoved = null;
            this.event_OnUnitDead = null;
            this.event_OnUnitDamage = null;
            this.event_OnUnitRebirth = null;
            this.event_OnUnitGotInstanceItem = null;
            this.event_OnUnitGotInventoryItem = null;
            this.event_OnUnitLostInventoryItem = null;
            this.event_OnUnitUseItem = null;
            this.event_OnUnitGotBuff = null;
            this.event_OnUnitGotMoney = null;
            this.event_OnUnitPickUnit = null;
            this.event_OnUnitTransport = null;

            this.event_OnGameOver = null;
            this.event_OnProcessObjectAction = null;
            this.event_OnRecvAction = null;

            this.event_UnitLaunchAura = null;
            this.event_UnitEnterAura = null;
            this.event_UnitLeaveAura = null;

            this.event_TryPickItem = null;
            this.event_FinishPickItem = null;
            this.event_ItemAdded = null;
            this.event_PlayerReady = null;
            this.event_PlayerTransportScene = null;

            this.event_OnQuestAccepted = null;
            this.event_OnQuestCommitted = null;
            this.event_OnQuestDropped = null;
            this.event_OnQuestStatusChanged = null;

            this.event_UnitEnterAOI = null;
            this.event_UnitLeaveAOI = null;

            this.event_OnEnvironmentVarChangeHandler = null;
            this.OnUnitSwapZoneInfoFlag = null;
            this.OnObjectError = null;

            ClearTriggerEvents();
            ClearGUIEvents();
            ClearFlagEvents();

        }
        //----------------------------------------------------------------------------------------------------
        #region InstanceZone
        /// <summary>
        /// 场景更新触发
        /// </summary>
        /// <param name="zone"></param>
        public delegate void UpdateHandler(InstanceZone zone);
        public delegate void InitHandler(InstanceZone zone);
        public delegate void PostEventHandler(InstanceZone zone, IEnumerable<BattleNotify> events);
        public delegate void RecvActionHandler(InstanceZone zone, BattleAction act);
        //         public delegate void SendMessageB2RHandler(InstanceZone zone, SendMessageB2R e);
        //         public delegate void RecvMessageR2BHandler(InstanceZone zone, SendMessageR2B e);
        //         public delegate void SendEventB2RHandler(InstanceZone zone, SendEventB2R e);
        public delegate void GameOverHandler(InstanceZone zone, GameOverEvent evt);
        public delegate void ProcessZoneActionHandler(InstanceZone zone, BattleAction act);
        public delegate void ProcessObjectActionHandler(InstanceZoneObject obj, ObjectAction act);

        public event UpdateHandler OnFirstUpdate;
        public event UpdateHandler OnUpdate { add { event_OnUpdate += value; } remove { event_OnUpdate -= value; } }
        [EventTriggerDescAttribute("场景初始化时触发")]
        public event InitHandler OnInit { add { event_OnInit += value; } remove { event_OnInit -= value; } }

        [EventTriggerDescAttribute("场景推送消息时触发")]
        public event PostEventHandler OnPostEvent { add { event_OnPostEvent += value; } remove { event_OnPostEvent -= value; } }
        [EventTriggerDescAttribute("收到输入动作（非主线程）")]
        public event RecvActionHandler OnRecvAction { add { event_OnRecvAction += value; } remove { event_OnRecvAction -= value; } }

        //         [EventTriggerDescAttribute("【战斗服->游戏服】已发送字符串消息")]
        //         public event SendMessageB2RHandler OnSendMessageToGS { add { event_OnSendToGS += value; } remove { event_OnSendToGS -= value; } }
        //         [EventTriggerDescAttribute("【游戏服->战斗服】已接收字符串消息")]
        //         public event RecvMessageR2BHandler OnRecvMessageFromGS { add { event_OnRecvFromGS += value; } remove { event_OnRecvFromGS -= value; } }
        //         [EventTriggerDescAttribute("【战斗服->游戏服】发送自定义事件")]
        //         public event SendEventB2RHandler OnSendEventB2R { add { event_OnSendMessageB2REvent += value; } remove { event_OnSendMessageB2REvent -= value; } }
        [EventTriggerDescAttribute("收到游戏结束事件")]
        public event GameOverHandler OnGameOver { add { event_OnGameOver += value; } remove { event_OnGameOver -= value; } }
        [EventTriggerDescAttribute("处理收到客户端输入的动作")]
        public event ProcessZoneActionHandler OnProcessZoneAction { add { event_OnProcessZoneAction += value; } remove { event_OnProcessZoneAction -= value; } }
        [EventTriggerDescAttribute("处理收到客户端输入的动作")]
        public event ProcessObjectActionHandler OnProcessObjectAction { add { event_OnProcessObjectAction += value; } remove { event_OnProcessObjectAction -= value; } }

        private UpdateHandler event_OnUpdate;
        private InitHandler event_OnInit;
        private PostEventHandler event_OnPostEvent;
        private RecvActionHandler event_OnRecvAction;
        //         private SendMessageB2RHandler event_OnSendToGS;
        //         private RecvMessageR2BHandler event_OnRecvFromGS;
        //         private SendEventB2RHandler event_OnSendMessageB2REvent;
        private GameOverHandler event_OnGameOver;
        private ProcessZoneActionHandler event_OnProcessZoneAction;
        private ProcessObjectActionHandler event_OnProcessObjectAction;

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        #region InstanceItem

        /// <summary>
        /// 单位尝试检取道具监听，
        /// 返回False禁止检取
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="unit"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public delegate bool TryPickItemHandler(InstanceZone zone, InstanceUnit unit, InstanceItem item);

        /// <summary>
        /// 单位完成采集道具
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="unit"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public delegate bool FinishPickItemHandler(InstanceZone zone, InstanceUnit unit, InstanceItem item);

        /// <summary>
        /// 添加物品到场景
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="item"></param>
        /// <param name="item_creater"></param>
        public delegate void ItemAddedHandler(InstanceZone zone, InstanceItem item, InstanceUnit item_creater);

        [EventTriggerDescAttribute("单位尝试检取采集道具，返回False禁止检取")]
        public event TryPickItemHandler OnTryPickItem { add { event_TryPickItem += value; } remove { event_TryPickItem -= value; } }
        [EventTriggerDescAttribute("单位完成采集道具")]
        public event FinishPickItemHandler OnFinishPickItem { add { event_FinishPickItem += value; } remove { event_FinishPickItem -= value; } }
        [EventTriggerDescAttribute("添加物品单位到场景")]
        public event ItemAddedHandler OnItemAdded { add { event_ItemAdded += value; } remove { event_ItemAdded -= value; } }


        private TryPickItemHandler event_TryPickItem;
        private FinishPickItemHandler event_FinishPickItem;
        private ItemAddedHandler event_ItemAdded;

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        #region InstancePlayer

        /// <summary>
        /// 玩家准备完毕
        /// </summary>
        public delegate void PlayerReadyHandler(InstancePlayer player, string info);
        /// <summary>
        /// 玩家跨场景传送请求
        /// </summary>
        public delegate void PlayerTransportSceneHandler(InstancePlayer player, InstanceFlag flag, int nextSceneID, string nextScenePosition);

        [EventTriggerDescAttribute("玩家准备完毕")]
        public event PlayerReadyHandler OnPlayerReady { add { event_PlayerReady += value; } remove { event_PlayerReady -= value; } }
        [EventTriggerDescAttribute("玩家跨场景传送")]
        public event PlayerTransportSceneHandler OnPlayerTransportScene { add { event_PlayerTransportScene += value; } remove { event_PlayerTransportScene -= value; } }

        private PlayerReadyHandler event_PlayerReady;
        private PlayerTransportSceneHandler event_PlayerTransportScene;

        internal void cb_playerReady(InstancePlayer player, string info)
        {
            if (event_PlayerReady != null)
            {
                event_PlayerReady.Invoke(player, info);
            }
        }
        internal void cb_playerTransportScene(InstancePlayer player, InstanceFlag flag, int nextSceneID, string nextScenePosition)
        {
            player.callback_onTransportScene(flag, nextSceneID, nextScenePosition);
            if (event_PlayerTransportScene != null)
            {
                event_PlayerTransportScene.Invoke(player, flag, nextSceneID, nextScenePosition);
            }
        }


        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        #region InstanceUnit
        /// <summary>
        /// 当单位可攻击时触发，如果单位有出生动画，则动画完结后触发。
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="obj"></param>
        public delegate void UnitActivatedHandler(InstanceZone zone, InstanceUnit obj);

        /// <summary>
        /// 单位进入触发
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="obj"></param>
        public delegate void UnitAddedHandler(InstanceZone zone, InstanceUnit obj);

        /// <summary>
        /// 单位移除触发
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="obj"></param>
        public delegate void UnitRemovedHandler(InstanceZone zone, InstanceUnit obj);

        /// <summary>
        /// 单位死亡触发
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="obj"></param>
        /// <param name="attacker"></param>
        public delegate void UnitDeadHandler(InstanceZone zone, InstanceUnit obj, InstanceUnit attacker);

        /// <summary>
        /// 单位受到攻击触发
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="obj"></param>
        /// <param name="attacker"></param>
        public delegate void UnitDamageHandler(InstanceZone zone, InstanceUnit obj, InstanceUnit attacker, long reduceHP, in TAttackSource source, in TAttackResult result);

        /// <summary>
        /// 单位复活触发
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="obj"></param>
        public delegate void UnitRebirthHandler(InstanceZone zone, InstanceUnit obj);

        /// <summary>
        /// 单位获取场景物品
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="unit"></param>
        /// <param name="item"></param>
        public delegate bool UnitGotInstanceItemHandler(InstanceZone zone, InstanceUnit unit, InstanceItem item);

        /// <summary>
        /// 单位获取物品进背包
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="unit"></param>
        /// <param name="item"></param>
        /// <param name="count"></param>
        public delegate void UnitGotInventoryItemHandler(InstanceZone zone, InstanceUnit unit, ItemTemplate item, int count);
        /// <summary>
        /// 单位丢失物品
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="unit"></param>
        /// <param name="item"></param>
        /// <param name="count"></param>
        public delegate void UnitLostInventoryItemHandler(InstanceZone zone, InstanceUnit unit, ItemTemplate item, int count);
        /// <summary>
        /// 单位获取物品
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="unit"></param>
        /// <param name="item"></param>
        /// <param name="item_creater"></param>
        public delegate void UnitUseItemHandler(InstanceZone zone, InstanceUnit unit, ItemTemplate item, InstanceUnit item_creater);

        /// <summary>
        /// 单位获取金币
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="add_money"></param>
        public delegate void UnitGotMoneyHandler(InstanceUnit obj, long add_money);
        /// <summary>
        /// 单位和其他单位产生交互
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="obj"></param>
        /// <param name="pickable"></param>
        public delegate void UnitPickUnitHandler(InstanceZone zone, InstanceUnit obj, InstanceUnit pickable);

        public delegate void UnitTransportHandler(InstanceZone zone, InstanceUnit obj, Geometry.Vector3 oldpos);

        //结束剧情播放
        public delegate void UnitFinishStoryHandler(InstanceZone zone, InstanceUnit obj, string storyFileName);

        public delegate void OnObjectErrorHandler(Exception err, InstanceZoneObject obj);

        private UnitAddedHandler event_OnUnitAdded;
        private UnitRemovedHandler event_OnUnitRemoved;
        private UnitDeadHandler event_OnUnitDead;
        private UnitDamageHandler event_OnUnitDamage;
        private UnitRebirthHandler event_OnUnitRebirth;
        private UnitGotInstanceItemHandler event_OnUnitGotInstanceItem;
        private UnitGotInventoryItemHandler event_OnUnitGotInventoryItem;
        private UnitLostInventoryItemHandler event_OnUnitLostInventoryItem;
        private UnitUseItemHandler event_OnUnitUseItem;
        private UnitGotMoneyHandler event_OnUnitGotMoney;
        private UnitPickUnitHandler event_OnUnitPickUnit;
        private UnitTransportHandler event_OnUnitTransport;
        private UnitFinishStoryHandler event_OnUnitFinishStory;


        [EventTriggerDescAttribute("当单位可攻击时触发，如果单位有出生动画，则动画完结后触发。")]
        public event UnitActivatedHandler OnUnitActivated;
        [EventTriggerDescAttribute("当单位可攻击时触发，如果单位有出生动画，则动画完结后触发。")]
        public event UnitActivatedHandler OnUnitFirstActivated;

        [EventTriggerDescAttribute("单位进入触发")]
        public event UnitAddedHandler OnUnitAdded { add { event_OnUnitAdded += value; } remove { event_OnUnitAdded -= value; } }
        [EventTriggerDescAttribute("单位移除触发")]
        public event UnitRemovedHandler OnUnitRemoved { add { event_OnUnitRemoved += value; } remove { event_OnUnitRemoved -= value; } }
        [EventTriggerDescAttribute("单位死亡触发")]
        public event UnitDeadHandler OnUnitDead { add { event_OnUnitDead += value; } remove { event_OnUnitDead -= value; } }
        [EventTriggerDescAttribute("单位受到攻击触发")]
        public event UnitDamageHandler OnUnitDamage { add { event_OnUnitDamage += value; } remove { event_OnUnitDamage -= value; } }
        [EventTriggerDescAttribute("单位复活触发")]
        public event UnitRebirthHandler OnUnitRebirth { add { event_OnUnitRebirth += value; } remove { event_OnUnitRebirth -= value; } }
        [EventTriggerDescAttribute("单位获取物品")]
        public event UnitGotInstanceItemHandler OnUnitGotInstanceItem { add { event_OnUnitGotInstanceItem += value; } remove { event_OnUnitGotInstanceItem -= value; } }
        [EventTriggerDescAttribute("单位获取背包物品")]
        public event UnitGotInventoryItemHandler OnUnitGotInventoryItem { add { event_OnUnitGotInventoryItem += value; } remove { event_OnUnitGotInventoryItem -= value; } }
        [EventTriggerDescAttribute("单位丢失背包物品")]
        public event UnitLostInventoryItemHandler OnUnitLostInventoryItem { add { event_OnUnitLostInventoryItem += value; } remove { event_OnUnitLostInventoryItem -= value; } }
        [EventTriggerDescAttribute("单位使用道具")]
        public event UnitUseItemHandler OnUnitUseItem { add { event_OnUnitUseItem += value; } remove { event_OnUnitUseItem -= value; } }
        [EventTriggerDescAttribute("单位中BUFF")]
        public event UnitGotBuffHandler OnUnitGotBuff { add { event_OnUnitGotBuff += value; } remove { event_OnUnitGotBuff -= value; } }

        [EventTriggerDescAttribute("单位移除BUFF")]
        public event UnitRemoveBuffHandler OnUnitRemoveBuff { add { event_OnUnitRemoveBuff += value; } remove { event_OnUnitRemoveBuff -= value; } }
        [EventTriggerDescAttribute("单位获得金币")]
        public event UnitGotMoneyHandler OnUnitGotMoney { add { event_OnUnitGotMoney += value; } remove { event_OnUnitGotMoney -= value; } }
        [EventTriggerDescAttribute("单位和其他单位产生交互")]
        public event UnitPickUnitHandler OnUnitPickUnit { add { event_OnUnitPickUnit += value; } remove { event_OnUnitPickUnit -= value; } }
        [EventTriggerDescAttribute("单位场景内传送")]
        public event UnitTransportHandler OnUnitTransport { add { event_OnUnitTransport += value; } remove { event_OnUnitTransport -= value; } }
        [EventTriggerDescAttribute("单位结束剧情播放")]
        public event UnitFinishStoryHandler OnUnitFinishStory { add { event_OnUnitFinishStory += value; } remove { event_OnUnitFinishStory -= value; } }


        public event OnObjectErrorHandler OnObjectError;

        internal void cb_unitTransportCallBack(InstanceUnit target, Geometry.Vector3 oldpos)
        {
            target.callback_onTransport(oldpos);
            event_OnUnitTransport?.Invoke(this, target, oldpos);
        }
        internal void cb_unitDamageCallBack(InstanceUnit target, InstanceUnit attacker, long reduceHP, in TAttackSource source, in TAttackResult result)
        {
            LastHittedUnit = target;
            LastAttackUnit = attacker;

            if (attacker != null)
                attacker.callback_onAttack(target, reduceHP, in source, in result);
            if (target != null)
                target.callback_onDamage(attacker, reduceHP, in source, in result);
            if (event_OnUnitDamage != null)
                event_OnUnitDamage.Invoke(this, target, attacker, reduceHP, in source, in result);


        }
        internal void cb_unitDeadCallBack(InstanceUnit obj, InstanceUnit attacker)
        {
            StatisticForceDead(obj);

            LastHittedUnit = obj;
            LastKilledUnit = obj;

            if (attacker == null)
                attacker = obj;
            LastAttackUnit = attacker;

            attacker.callback_onKill(obj);
            obj.doDead(attacker);
            obj.deadDropItems(attacker.Force);
            if (obj.ADropItem)
            {
                attacker.killDropItem(obj, obj.ADropItem);
            }
            obj.callback_onDead(attacker);
            if (obj.IsDead && event_OnUnitDead != null)
                event_OnUnitDead.Invoke(this, obj, attacker);

        }


        internal void cb_unitActivatedCallBack(InstanceUnit obj)
        {
            nearChange(obj);
            LastActivatedUnit = obj;
            obj.callback_onActivated();
            if (obj.IsFirstActive)
            {
                if (OnUnitFirstActivated != null)
                    OnUnitFirstActivated.Invoke(this, obj);
            }
            if (OnUnitActivated != null)
                OnUnitActivated.Invoke(this, obj);
        }
        internal void cb_unitRebirthCallBack(InstanceUnit obj)
        {
            LastRebirthUnit = obj;
            obj.callback_onRebirth();
            if (event_OnUnitRebirth != null)
                event_OnUnitRebirth.Invoke(this, obj);

        }
        internal void cb_unitGotInventoryItemCallBack(InstanceUnit obj, ItemTemplate item, int count)
        {
            LastUnitGotInventoryItem = item;
            obj.callback_onGotInventoryItem(item);
            if (event_OnUnitGotInventoryItem != null)
                event_OnUnitGotInventoryItem.Invoke(this, obj, item, count);
        }
        internal void cb_unitLostInventoryItemCallBack(InstanceUnit obj, ItemTemplate item, int count)
        {
            LastUnitLostInventoryItem = item;
            obj.callback_onLostInventoryItem(item);
            if (event_OnUnitLostInventoryItem != null)
                event_OnUnitLostInventoryItem.Invoke(this, obj, item, count);
        }
        internal bool cb_unitGotInstanceItemCallBack(InstanceUnit obj, InstanceItem item)
        {
            LastUnitGotInstanceItem = item;
            var ret = obj.callback_onGotInstanceItem(item);
            if (event_OnUnitGotInstanceItem != null)
            {
                ret &= event_OnUnitGotInstanceItem.Invoke(this, obj, item);
            }
            return ret;
        }
        internal void cb_unitUseItemCallBack(InstanceUnit obj, ItemTemplate item, InstanceUnit item_creater)
        {
            LastUnitUseItem = item;
            obj.callback_onUseItem(item, item_creater);
            if (event_OnUnitUseItem != null)
                event_OnUnitUseItem.Invoke(this, obj, item, item_creater);
        }
        internal void cb_unitGotMoneyCallBack(InstanceUnit obj, long add_money)
        {
            if (event_OnUnitGotMoney != null)
            {
                event_OnUnitGotMoney.Invoke(obj, add_money);
            }
        }
        internal void cb_unitPickUnitCallBack(InstanceUnit src, InstanceUnit pickable)
        {
            LastPickableUnit = pickable;
            if (event_OnUnitPickUnit != null)
            {
                event_OnUnitPickUnit.Invoke(this, src, pickable);
            }
        }
        internal bool cb_unitTryPickItem(InstanceUnit unit, InstanceItem item)
        {
            LastPickingItem = item;
            LastPickingItemUnit = unit;
            bool ret = true;
            if (event_TryPickItem != null)
            {
                foreach (TryPickItemHandler trypick in event_TryPickItem.GetInvocationList())
                {
                    if (!trypick.Invoke(this, unit, item))
                    {
                        ret = false;
                    }
                }
            }
            return ret;
        }
        public void cb_unitFinishPickItem(InstanceUnit unit, InstanceItem item)
        {
            if (event_FinishPickItem != null)
            {
                event_FinishPickItem.Invoke(this, unit, item);
            }
        }
        internal void cb_unitFinishStory(InstanceUnit unit, string storyFileName)
        {
            if (event_OnUnitFinishStory != null)
            {
                event_OnUnitFinishStory.Invoke(this, unit, storyFileName);
            }
        }


        //----------------------------------------------------------------------------------------------------
        public delegate void UnitSwapZoneInfoFlagHandler(InstanceZone zone, InstanceUnit unit, int oldFLag, int newFlag);
        public event UnitSwapZoneInfoFlagHandler OnUnitSwapZoneInfoFlag;
        internal void cb_OnUnitSwapZoneInfoFlag(InstanceZoneObject obj, int oldFLag, int newFlag)
        {
            if (obj is InstanceUnit unit)
            {
                OnUnitSwapZoneInfoFlag?.Invoke(this, unit, oldFLag, newFlag);
            }
        }
        //----------------------------------------------------------------------------------------------------
        public delegate void UnitLevelUpHandler(InstanceZone zone, InstanceUnit unit);
        public event UnitLevelUpHandler OnUnitLevelUp;
        internal void cb_OnUnitLevelUp(InstanceUnit unit)
        {
            OnUnitLevelUp?.Invoke(this, unit);
        }
        //----------------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------------------------


        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Skill Spell Buff


        private UnitGotBuffHandler event_OnUnitGotBuff;
        private UnitRemoveBuffHandler event_OnUnitRemoveBuff;

        /// <summary>
        /// 单位中BUFF
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="unit"></param>
        /// <param name="buff"></param>
        public delegate void UnitGotBuffHandler(InstanceZone zone, InstanceUnit unit, InstanceUnit.EquipBuff buff);

        /// <summary>
        /// 单位移除BUFF
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="unit"></param>
        /// <param name="buff"></param>
        public delegate void UnitRemoveBuffHandler(InstanceZone zone, InstanceUnit unit, InstanceUnit.EquipBuff buff);
        internal void cb_unitGotBuffCallBack(InstanceUnit obj, InstanceUnit.EquipBuff buff)
        {
            LastUnitGotBuff = buff.Data;
            obj.callback_onGotBuff(buff);
            if (event_OnUnitGotBuff != null)
                event_OnUnitGotBuff.Invoke(this, obj, buff);
        }


        internal void cb_unitRemoveBuffCallBack(InstanceUnit obj, InstanceUnit.EquipBuff buff)
        {
            obj.callback_onRemoveBuff(buff);
            if (event_OnUnitRemoveBuff != null)
                event_OnUnitRemoveBuff.Invoke(this, obj, buff);
        }


        /// <summary>
        /// 单位开始放技能
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="u"></param>
        /// <param name="skill"></param>
        public delegate void UnitLaunchSkillHandler(InstanceZone zone, InstanceUnit u, InstanceUnit.EquipSkill skill, StateSkill st);
        [EventTriggerDescAttribute("单位释放技能")]
        public event UnitLaunchSkillHandler OnUnitLaunchSkill { add { event_OnUnitLaunchSkill += value; } remove { event_OnUnitLaunchSkill -= value; } }
        private UnitLaunchSkillHandler event_OnUnitLaunchSkill;
        internal void cb_unitLaunchSkill(InstanceUnit unit, InstanceUnit.EquipSkill ss, StateSkill st)
        {
            LastLaunchSkill = ss.Data;
            LastLaunchSkillUnit = unit;
            unit.callback_OnLaunchSkill(ss, st);
            if (event_OnUnitLaunchSkill != null)
            {
                event_OnUnitLaunchSkill.Invoke(this, unit, ss, st);
            }
        }


        public delegate void UnitOverSkillHandler(InstanceZone zone, InstanceUnit u, InstanceUnit.EquipSkill skill, StateSkill st);
        [EventTriggerDesc("单位结束技能")]
        public event UnitOverSkillHandler OnUnitOverSkill { add { event_OnUnitOverSkill += value; } remove { event_OnUnitOverSkill -= value; } }
        private UnitOverSkillHandler event_OnUnitOverSkill;
        internal void cb_unitOverSkill(InstanceUnit unit, InstanceUnit.EquipSkill ss, StateSkill st)
        {
            unit.callback_OnOverSkill(ss, st);
            if (event_OnUnitOverSkill != null)
            {
                event_OnUnitOverSkill.Invoke(this, unit, ss, st);
            }
        }


        public delegate void LaunchSpellHandler(InstanceZone zone, InstanceSpell spell, TAddSpell add);
        [EventTriggerDesc("LaunchSpell触发")]
        public event LaunchSpellHandler OnLaunchSpell { add { event_OnLaunchSpell += value; } remove { event_OnLaunchSpell -= value; } }
        private LaunchSpellHandler event_OnLaunchSpell;
        internal void cb_launchSpell(InstanceSpell spell, TAddSpell add)
        {
            add.launcher?.cb_launchSpell(spell, add);
            if (event_OnLaunchSpell != null)
            {
                event_OnLaunchSpell.Invoke(this, spell, add);
            }
        }

        public delegate void RemoveSpellHandler(InstanceZone zone, InstanceSpell spell);
        [EventTriggerDesc("RemoveSpell触发")]
        public event RemoveSpellHandler OnRemoveSpell;
        internal void cb_removeSpell(InstanceSpell spell)
        {
            spell.LauncherOwner.cb_removeSpell(spell);
            OnRemoveSpell?.Invoke(this, spell);
        }

        public delegate void UnitSpellHittedHandler(InstanceUnit launcher, InstanceSpell spell, InstanceUnit target, TAttackSource attackSource);
        public event UnitSpellHittedHandler UnitSpellHitted;
        internal void cb_unitSpellHitted(InstanceUnit launcher, InstanceSpell spell, InstanceUnit target, TAttackSource attackSource)
        {
            UnitSpellHitted?.Invoke(launcher, spell, target, attackSource);
        }

        public delegate void UnitSpellFirstHittedHandler(InstanceUnit launcher, InstanceSpell spell, InstanceUnit target, TAttackSource attackSource);
        public event UnitSpellFirstHittedHandler UnitSpellFirstHitted;
        internal void cb_unitSpellFirstHitted(InstanceUnit launcher, InstanceSpell spell, InstanceUnit target, TAttackSource attackSource)
        {
            UnitSpellFirstHitted?.Invoke(launcher, spell, target, attackSource);
        }


        public delegate void UnitSpellLaunchSpellHandler(InstanceUnit launcher, InstanceSpell sender, TAddSpell add);
        public event UnitSpellLaunchSpellHandler UnitSpellLaunchSpell;
        internal void cb_unitSpellLaunchSpell(InstanceUnit launcher, InstanceSpell sender, TAddSpell add)
        {
            UnitSpellLaunchSpell?.Invoke(launcher, sender, add);
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        #region InstanceZoneObject

        public delegate void ObjectPosChangedHandler(InstanceZone zone, InstanceZoneObject o);
        public delegate void ObjectSpaceChangedHandler(InstanceZone zone, IEntityObject o, ZoneSpaceDivision.ZoneSpaceCellNode old_node, ZoneSpaceDivision.ZoneSpaceCellNode new_node);
        //public delegate void ObjectAreaChangedHandler(InstanceZone zone, InstanceZoneObject o, ZoneArea old_node, ZoneArea new_node);
        public delegate void ObjectAddedHandler(InstanceZone zone, InstanceZoneObject obj);
        public delegate void ObjectRemovedHandler(InstanceZone zone, InstanceZoneObject obj);


        private ObjectPosChangedHandler event_OnObjectPosChanged;
        private ObjectSpaceChangedHandler event_OnObjectSpaceChanged;
        //private ObjectAreaChangedHandler event_OnObjectAreaChanged;
        private ObjectAddedHandler event_OnObjectAdded;
        private ObjectRemovedHandler event_OnObjectRemoved;
        [EventTriggerDescAttribute("单位位置发生变化时触发")]
        public event ObjectPosChangedHandler OnObjectPosChanged { add { event_OnObjectPosChanged += value; } remove { event_OnObjectPosChanged -= value; } }
        [EventTriggerDescAttribute("单位空间分割发生变化时触发")]
        public event ObjectSpaceChangedHandler OnObjectSpaceChanged { add { event_OnObjectSpaceChanged += value; } remove { event_OnObjectSpaceChanged -= value; } }
        //         [EventTriggerDescAttribute("单位Area发生变化时触发")]
        //         public event ObjectAreaChangedHandler OnObjectAreaChanged { add { event_OnObjectAreaChanged += value; } remove { event_OnObjectAreaChanged -= value; } }
        [EventTriggerDescAttribute("Object进入触发")]
        public event ObjectAddedHandler OnObjectAdded { add { event_OnObjectAdded += value; } remove { event_OnObjectAdded -= value; } }
        [EventTriggerDescAttribute("Object移除触发")]
        public event ObjectRemovedHandler OnObjectRemoved { add { event_OnObjectRemoved += value; } remove { event_OnObjectRemoved -= value; } }



        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Quest

        /// <summary>
        /// 【服务端】任务已接受
        /// </summary>
        /// <param name="player"></param>
        /// <param name="quest"></param>
        public delegate void QuestAcceptedHandler(InstancePlayer player, string quest);
        /// <summary>
        /// 【服务端】任务已提交
        /// </summary>
        /// <param name="player"></param>
        /// <param name="quest"></param>
        public delegate void QuestCommittedHandler(InstancePlayer player, string quest);
        /// <summary>
        /// 【服务端】任务已放弃
        /// </summary>
        /// <param name="player"></param>
        /// <param name="quest"></param>
        public delegate void QuestDroppedHandler(InstancePlayer player, string quest);
        /// <summary>
        /// 【服务端】任务状态已更新
        /// </summary>
        /// <param name="player"></param>
        /// <param name="quest"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public delegate void QuestStatusChangedHandler(InstancePlayer player, string quest, string key, string value);


        /// <summary>
        /// cg 完成事件
        /// </summary>
        public delegate void UnitCGCompletedEventHandler(InstanceUnit obj, string cgid);

        /// <summary>
        /// Npc对话 完成事件
        /// </summary>
        public delegate void NpcTalkCompletedEventHandler(InstanceUnit obj, InstanceUnit target, int npcid, string tag);
        [EventTriggerDescAttribute("【游戏服->战斗服】任务已接受")]
        public event QuestAcceptedHandler OnQuestAccepted { add { event_OnQuestAccepted += value; } remove { event_OnQuestAccepted -= value; } }
        [EventTriggerDescAttribute("【游戏服->战斗服】任务已提交")]
        public event QuestCommittedHandler OnQuestCommitted { add { event_OnQuestCommitted += value; } remove { event_OnQuestCommitted -= value; } }
        [EventTriggerDescAttribute("【游戏服->战斗服】任务已放弃")]
        public event QuestDroppedHandler OnQuestDropped { add { event_OnQuestDropped += value; } remove { event_OnQuestDropped -= value; } }
        [EventTriggerDescAttribute("【游戏服->战斗服】任务状态已更新")]
        public event QuestStatusChangedHandler OnQuestStatusChanged { add { event_OnQuestStatusChanged += value; } remove { event_OnQuestStatusChanged -= value; } }


        private QuestAcceptedHandler event_OnQuestAccepted;
        private QuestCommittedHandler event_OnQuestCommitted;
        private QuestDroppedHandler event_OnQuestDropped;
        private QuestStatusChangedHandler event_OnQuestStatusChanged;

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Aura

        [EventTriggerDescAttribute("单位释放光环")]
        public event UnitLaunchAuraHandler OnUnitLaunchAura { add { event_UnitLaunchAura += value; } remove { event_UnitLaunchAura -= value; } }
        [EventTriggerDescAttribute("单位进入光环")]
        public event UnitEnterAuraHandler OnUnitEnterAura { add { event_UnitEnterAura += value; } remove { event_UnitEnterAura -= value; } }
        [EventTriggerDescAttribute("单位离开光环")]
        public event UnitLeaveAuraHandler OnUnitLeaveAura { add { event_UnitLeaveAura += value; } remove { event_UnitLeaveAura -= value; } }

        private UnitLaunchAuraHandler event_UnitLaunchAura;
        private UnitEnterAuraHandler event_UnitEnterAura;
        private UnitLeaveAuraHandler event_UnitLeaveAura;

        public delegate void UnitLaunchAuraHandler(InstanceZone zone, InstanceUnit u, InstanceUnit.EquipAura aura);
        public delegate void UnitEnterAuraHandler(InstanceZone zone, InstanceUnit u, InstanceUnit.EquipAura aura);
        public delegate void UnitLeaveAuraHandler(InstanceZone zone, InstanceUnit u, InstanceUnit.EquipAura aura);

        internal void cb_OnUnitLaunchAura(InstanceUnit u, InstanceUnit.EquipAura aura)
        {
            LastUnitLaunchAura = aura.Data;
            event_UnitLaunchAura?.Invoke(this, u, aura);
            u.cb_OnUnitLaunchAura(aura);
        }
        internal void cb_OnUnitEnterAura(InstanceUnit u, InstanceUnit.EquipAura aura)
        {
            LastUnitEnterAura = aura.Data;
            event_UnitEnterAura?.Invoke(this, u, aura);
            u.cb_OnUnitEnterAura(aura);
        }
        internal void cb_OnUnitLeaveAura(InstanceUnit u, InstanceUnit.EquipAura aura)
        {
            LastUnitLeaveAura = aura.Data;
            event_UnitLeaveAura?.Invoke(this, u, aura);
            u.cb_OnUnitLeaveAura(aura);
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        #region AOI

        public delegate void UnitEnterAOIStatus(InstanceUnit u, ObjectAoiStatus aoi);
        public delegate void UnitLeaveAOIStatus(InstanceUnit u, ObjectAoiStatus aoi);

        private UnitEnterAOIStatus event_UnitEnterAOI;
        private UnitLeaveAOIStatus event_UnitLeaveAOI;

        [EventTriggerDescAttribute("单位进入AOI")]
        public event UnitEnterAOIStatus OnUnitEnterAOI { add { event_UnitEnterAOI += value; } remove { event_UnitEnterAOI -= value; } }
        [EventTriggerDescAttribute("单位离开AOI")]
        public event UnitLeaveAOIStatus OnUnitLeaveAOI { add { event_UnitLeaveAOI += value; } remove { event_UnitLeaveAOI -= value; } }

        internal void cb_unitEnterAOI(InstanceUnit u, ObjectAoiStatus aoi)
        {
            event_UnitEnterAOI?.Invoke(u, aoi);
        }

        internal void cb_unitLeaveAOI(InstanceUnit u, ObjectAoiStatus aoi)
        {
            event_UnitLeaveAOI?.Invoke(u, aoi);
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        #region FLAG
        protected virtual void ClearFlagEvents()
        {
            OnFlagOn = null;
            OnFlagOff = null;
            OnFlagSpawnOver = null;
            OnFlagSpawnObject = null;
            OnUnitPassPoint = null;
            OnUnitHoldPoint = null;
            OnUnitEnterRegion = null;
            OnUnitLeaveRegion = null;
            OnUnitEnterArea = null;
            OnUnitLeaveArea = null;
            //             OnUnitEnterRegionOnce = null;
            //             OnUnitLeaveRegionOnce = null;
            //             OnUnitEnterAreaOnce = null;
            //             OnUnitLeaveAreaOnce = null;
        }
        public delegate void OnFlagEnableDelegate(InstanceZone zone, InstanceFlag flag);
        public delegate void OnFlagDisableDelegate(InstanceZone zone, InstanceFlag flag);
        public delegate void OnFlagSpawnOverDelegate(InstanceZone zone, ISpawnContainer flag, AbstractSpawnAbility ab, InstanceZoneObject obj);
        public delegate void OnFlagSpawnObjectDelegate(InstanceZone zone, ISpawnContainer flag, AbstractSpawnAbility ab, InstanceZoneObject obj);
        public delegate void OnUnitPassPointDelegate(InstanceZone zone, InstanceUnit obj, InstanceFlag point, InstanceFlag next);
        public delegate void OnUnitHoldPointDelegate(InstanceZone zone, InstanceUnit obj, InstanceFlag point, PointHoldAbility hold);
        public delegate void OnUnitEnterRegionDelegate(InstanceZone zone, InstanceUnit obj, ZoneRegion region);
        public delegate void OnUnitLeaveRegionDelegate(InstanceZone zone, InstanceUnit obj, ZoneRegion region);
        public delegate void OnUnitEnterAreaDelegate(InstanceZone zone, InstanceUnit obj, ZoneArea area);
        public delegate void OnUnitLeaveAreaDelegate(InstanceZone zone, InstanceUnit obj, ZoneArea area);

        public event OnFlagEnableDelegate      /**/OnFlagOn;
        public event OnFlagDisableDelegate     /**/OnFlagOff;
        public event OnFlagSpawnOverDelegate   /**/OnFlagSpawnOver;
        public event OnFlagSpawnObjectDelegate /**/OnFlagSpawnObject;
        public event OnUnitPassPointDelegate   /**/OnUnitPassPoint;
        public event OnUnitHoldPointDelegate   /**/OnUnitHoldPoint;
        public event OnUnitEnterRegionDelegate /**/OnUnitEnterRegion;
        public event OnUnitLeaveRegionDelegate /**/OnUnitLeaveRegion;
        public event OnUnitEnterAreaDelegate   /**/OnUnitEnterArea;
        public event OnUnitLeaveAreaDelegate   /**/OnUnitLeaveArea;
        //         public event OnUnitEnterRegionDelegate /**/OnUnitEnterRegionOnce;
        //         public event OnUnitLeaveRegionDelegate /**/OnUnitLeaveRegionOnce;
        //         public event OnUnitEnterAreaDelegate   /**/OnUnitEnterAreaOnce;
        //         public event OnUnitLeaveAreaDelegate   /**/OnUnitLeaveAreaOnce;
        internal void cb_OnFlagOn(InstanceFlag flag) => OnFlagOn?.Invoke(this, flag);
        internal void cb_OnFlagOff(InstanceFlag flag) => OnFlagOff?.Invoke(this, flag);
        internal void cb_OnFlagSpawnOver(ISpawnContainer flag, AbstractSpawnAbility ab, InstanceZoneObject obj) => OnFlagSpawnOver?.Invoke(this, flag, ab, obj);
        internal void cb_OnFlagSpawnObject(ISpawnContainer flag, AbstractSpawnAbility ab, InstanceZoneObject obj) => OnFlagSpawnObject?.Invoke(this, flag, ab, obj);
        internal void cb_OnUnitPassPoint(InstanceUnit obj, InstanceFlag point, InstanceFlag next) => OnUnitPassPoint?.Invoke(this, obj, point, next);
        internal void cb_OnUnitHoldPoint(InstanceUnit obj, InstanceFlag point, PointHoldAbility hold) => OnUnitHoldPoint?.Invoke(this, obj, point, hold);
        internal void cb_OnUnitEnterRegion(InstanceUnit obj, ZoneRegion region) => OnUnitEnterRegion?.Invoke(this, obj, region);
        internal void cb_OnUnitLeaveRegion(InstanceUnit obj, ZoneRegion region) => OnUnitLeaveRegion?.Invoke(this, obj, region);
        internal void cb_OnUnitEnterArea(InstanceUnit obj, ZoneArea area) => OnUnitEnterArea?.Invoke(this, obj, area);
        internal void cb_OnUnitLeaveArea(InstanceUnit obj, ZoneArea area) => OnUnitLeaveArea?.Invoke(this, obj, area);
        //         internal void cb_OnUnitEnterRegionOnce(InstanceUnit obj, ZoneRegion region) => OnUnitEnterRegionOnce?.Invoke(this, obj, region);
        //         internal void cb_OnUnitLeaveRegionOnce(InstanceUnit obj, ZoneRegion region) => OnUnitLeaveRegionOnce?.Invoke(this, obj, region);
        //         internal void cb_OnUnitEnterAreaOnce(InstanceUnit obj, ZoneArea area) => OnUnitEnterAreaOnce?.Invoke(this, obj, area);
        //         internal void cb_OnUnitLeaveAreaOnce(InstanceUnit obj, ZoneArea area) => OnUnitLeaveAreaOnce?.Invoke(this, obj, area);
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
