using DeepCore.Game3D.Host.FuncData;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Abilities;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Host.ZoneServer.Interface;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Threading.Tasks;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceZone
    {
        public override InstanceZone Zone => this;
        //-------------------------------------------------------------------------------------------------------//
        //-------------------------------------------------------------------------------------------------------//
        public virtual InstanceFlag CreateFlag(SceneObjectData data) 
            => HostFactory.CreateFlag(this, data);
        public virtual InstanceSpell CreateSpell(TAddSpell add)
            => HostFactory.CreateSpell(this, add);
        public virtual InstanceItem CreateItem(TAddItem add) 
            => HostFactory.CreateItem(this, add);
        public virtual InstanceUnit CreateUnit(TAddUnit add)
            => HostFactory.CreateUnit(this, add);
        //---------------------------------------------------------------------------------------------
        public virtual InstanceUnit.EquipSkill CreateUnitSkillState(InstanceUnit owner, SkillTemplate data, LaunchSkill skill) 
            => HostFactory.CreateUnitSkillState(owner, data, skill);
        public virtual InstanceUnit.EquipBuff CreateUnitBuffState(InstanceUnit owner, TAddBuff addbuff) 
            => HostFactory.CreateUnitBuffState(owner,  addbuff);
        public virtual InstanceUnit.EquipAura CreateUnitAuraState(InstanceUnit owner, AuraTemplate aura, int level, InstanceUnit.EquipSkill fromSkillID) 
            => HostFactory.CreateUnitAuraState(owner, aura, level, fromSkillID);
        public virtual InstanceUnit.InventorySlot CreateUnitInventorySlot(int index, InstanceUnit owner) 
            => HostFactory.CreateUnitInventorySlot(index, owner);
        //---------------------------------------------------------------------------------------------
        public virtual HateSystem CreateHateSystem(InstanceUnit owner) 
            => HostFactory.CreateHateSystem(owner);
        public virtual ObjectAoiStatus CreateAOI(InstancePlayer player)
            => HostFactory.CreateAOI(player);
        public virtual UnitCartridge CreateCartridge(in TAddUnit add, InstanceUnit owner) 
            => HostFactory.CreateCartridge(in add, owner);
        //---------------------------------------------------------------------------------------------
        public virtual InstanceZoneFormula CreateFormula() 
            => HostFactory.CreateFormula(this);
        public virtual InstanceUnitFormula CreateFormula(InstanceUnit unit)
            => HostFactory.CreateFormula(unit);
        //---------------------------------------------------------------------------------------------
        public virtual Ability CreateAbility(EditorAbilityData data, InstanceAttributes obj) 
            => HostFactory.CreateAbility(data, obj);

        public virtual IQuestAdapter CreateQuestAdapter()
            => HostFactory.CreateQuestAdapter(this);










        //---------------------------------------------------------------------------------------------
        #region QUEST
        /// <summary>
        /// 【第三方系统通知】任务已接受
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        internal void gs_OnQuestAccepted(string playerUUID, string quest)
        {
            QueueTask((InstanceZone zone) =>
            {
                var p = GetPlayerByUUID(playerUUID);
                if (p?.QuestComponent is PlayerQuestComponent q)
                {
                    q.doQuestAccepted(quest);
                    if (event_OnQuestAccepted != null)
                        event_OnQuestAccepted.Invoke(p, quest);
                }
            });
        }
        /// <summary>
        /// 【第三方系统通知】任务已完成
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        internal void gs_OnQuestCommitted(string playerUUID, string quest)
        {
            QueueTask((InstanceZone zone) =>
            {
                var p = GetPlayerByUUID(playerUUID);
                if (p?.QuestComponent is PlayerQuestComponent q)
                {
                    q.doQuestCommitted(quest);
                    if (event_OnQuestCommitted != null)
                        event_OnQuestCommitted.Invoke(p, quest);
                }
            });
        }
        /// <summary>
        /// 【第三方系统通知】任务已放弃
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        internal void gs_OnQuestDropped(string playerUUID, string quest)
        {
            QueueTask((InstanceZone zone) =>
            {
                var p = GetPlayerByUUID(playerUUID);
                if (p?.QuestComponent is PlayerQuestComponent q)
                {
                    q.doQuestDropped(quest);
                    if (event_OnQuestDropped != null)
                        event_OnQuestDropped.Invoke(p, quest);
                }
            });
        }
        /// <summary>
        /// 【第三方系统通知】任务状态已改变
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal void gs_OnQuestStatusChanged(string playerUUID, string quest, string key, string value)
        {
            QueueTask((InstanceZone zone) =>
            {
                var p = GetPlayerByUUID(playerUUID);
                if (p?.QuestComponent is PlayerQuestComponent q)
                {
                    q.doQuestStatusChanged(quest, key, value);
                    if (event_OnQuestStatusChanged != null)
                        event_OnQuestStatusChanged.Invoke(p, quest, key, value);
                }
            });
        }

        /// <summary>
        /// 本地通知游戏服
        /// </summary>
        /// <param name="player"></param>
        /// <param name="quest"></param>
        /// <param name="args"></param>
        internal void doAcceptQuest(InstancePlayer player, string quest, string args)
        {
            mQuestAdapter.DoAcceptQuest(player, quest, args);
        }
        /// <summary>
        /// 本地通知游戏服
        /// </summary>
        /// <param name="player"></param>
        /// <param name="quest"></param>
        /// <param name="args"></param>
        internal void doCommitQuest(InstancePlayer player, string quest, string args)
        {
            mQuestAdapter.DoCommitQuest(player, quest, args);
        }
        /// <summary>
        /// 本地通知游戏服
        /// </summary>
        /// <param name="player"></param>
        /// <param name="quest"></param>
        /// <param name="args"></param>
        internal void doDropQuest(InstancePlayer player, string quest, string args)
        {
            mQuestAdapter.DoDropQuest(player, quest, args);
        }
        /// <summary>
        /// 本地通知游戏服
        /// </summary>
        /// <param name="player"></param>
        /// <param name="quest"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal void doUpdateQuestStatus(InstancePlayer player, string quest, string key, string value)
        {
            mQuestAdapter.DoUpdateQuestStatus(player, quest, key, value);
        }


        #endregion
    }



}
