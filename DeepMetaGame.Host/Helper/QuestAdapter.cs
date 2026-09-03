using DeepCore.Game3D.Host.Instance;
using System;

namespace DeepCore.Game3D.Host.Helper
{

    //     public enum QuestState : byte
    //     {
    //         Uncharted = 0,
    //         Accepted = 1,
    //         Commited = 2,
    //     }
    //     public class QuestData
    //     {
    //         public readonly string QuestID;
    //         public QuestState State = QuestState.Uncharted;
    //         public HashMap<string, string> Attributes = new HashMap<string, string>();
    //         public QuestData(string id)
    //         {
    //             this.QuestID = id;
    //         }
    //     }

    public  class IQuestAdapter : IDisposable
    {
        private InstanceZone mZone;
        public IQuestAdapter(InstanceZone zone)
        {
            mZone = zone;
        }

        public InstanceZone Zone { get { return mZone; } }
        
        /// <summary>
        /// 【第三方系统通知】任务已接受
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        public virtual void OnQuestAcceptedHandler(string playerUUID, string quest)
        {
            mZone.gs_OnQuestAccepted(playerUUID, quest);
        }
        /// <summary>
        /// 【第三方系统通知】任务已完成
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        public virtual void OnQuestCommittedHandler(string playerUUID, string quest)
        {
            mZone.gs_OnQuestCommitted(playerUUID, quest);
        }
        /// <summary>
        /// 【第三方系统通知】任务已放弃
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        public virtual void OnQuestDroppedHandler(string playerUUID, string quest)
        {
            mZone.gs_OnQuestDropped(playerUUID, quest);
        }
        /// <summary>
        /// 【第三方系统通知】任务状态已改变
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public virtual void OnQuestStatusChangedHandler(string playerUUID, string quest, string key, string value)
        {
            mZone.gs_OnQuestStatusChanged(playerUUID, quest, key, value);
        }


        /// <summary>
        /// 【战斗模块请求】接受任务
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        /// <param name="args"></param>
        public virtual void DoAcceptQuest(InstancePlayer player, string quest, string args) { }
        /// <summary>
        /// 【战斗模块请求】
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        /// <param name="args"></param>
        public virtual void DoCommitQuest(InstancePlayer player, string quest, string args) { }
        /// <summary>
        /// 【战斗模块请求】
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        /// <param name="args"></param>
        public virtual void DoDropQuest(InstancePlayer player, string quest, string args) { }
        /// <summary>
        /// 【战斗模块请求】
        /// </summary>
        /// <param name="playerUUID"></param>
        /// <param name="quest"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public virtual void DoUpdateQuestStatus(InstancePlayer player, string quest, string key, string value) { }

        public virtual void Dispose()
        {

        }
    }
}
