
using DeepCore;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCrystal.RPC;
using System;
using System.Reflection;

namespace Gate.Server
{
    public class GateServerConfig
    {
        //--------------------------------------------------
        public string RealmID;
        public string GMTUrl;
        public string ServerListUrl;
        //public string LanguageUrl;
        public string MySQLConnectorString;
        //--------------------------------------------------
        public string BattleCodec;
        public string BattleDataFactory;
//         public string BattleHostFactory;
//         public string BattleSlaveFactory;
        public string BattleEditorDir;
        //--------------------------------------------------
        public string ReplaceNetHost;
        public string ClientHostFactoryClass;
        public string ClientCodecClass;
        public string ServerCodecClass;
        //--------------------------------------------------
        public bool EnableServerTest;
        //--------------------------------------------------
    }


    public static class GateTimerConfig
    {
        /// <summary>
        /// Connect同步状态到Gate
        /// </summary>
        public static int timer_sec_ConnectSyncToGateNotify = 3;
        /// <summary>
        /// Gate处理等待队列
        /// </summary>
        public static int timer_sec_GateUpdateQueue = 10;

        /// <summary>
        /// 玩家断线后，Session保持时间(这个变量的意思变成超过多久没收到任何消息后Kill连接)
        /// </summary>
        public static int timer_sec_SessionKeepTimeout = 300;

        /// <summary>
        /// 心跳超时检测定时器间隔（秒），独立于超时阈值，建议30秒
        /// </summary>
        public static int timer_sec_SessionHeartbeatCheckInterval = 30;

        /// <summary>
        /// 定期储存数据10分钟.
        /// </summary>
        public static int timer_minute_LogicSaveDataTimer = 5;




        /// <summary>
        /// Area同步状态到AreaManager
        /// </summary>
        public static int timer_sec_AreaStateNotify = 10;

        /// <summary>
        /// 场景单位公共数据刷新.
        /// </summary>
        public static int timer_sec_AreaOnPollingAreaZoneNode = 5;

        /// <summary>
        /// 副本无人后多久清理副本
        /// </summary>
        public static int timer_sec_ZoneKeepPlayerTimeout = 10;

        /// <summary>
        /// GameOver后场景销毁延迟时间.
        /// </summary>
        public static int timer_sec_ZoneDelayDestoryTime = 30;


    }

}
