using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using System;
using System.Threading;

namespace DeepCore.Game3D.Host.ZoneServer.Interface
{
    public delegate void ClientMessageHandler(object message);
    public delegate void GameServerMessageHandler(object message);
    public delegate void GameServerCallHandler(object message, Action<object, Exception> callback);
    public delegate void ZoneInit(IZoneNodeServer battle, InstanceZone z);


    public interface IZoneNodeServer
    {
        public virtual void StartTimer(BaseZoneNode node)
        {
            new ThreadZoneNodeTimer(node);
        }
        /// <summary>
        /// 通知游戏服
        /// </summary>
        /// <param name="msg"></param>
        void PostToGameServer(object msg);
        /// <summary>
        /// 通知游戏服
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="callback"></param>
        void PostToGameServer(object msg, Action<object, Exception> callback);

        /// <summary>
        /// 监听游戏服
        /// </summary>
        /// <param name="handler"></param>
        event GameServerMessageHandler HandleGameServerInvoke;
        /// <summary>
        /// 监听游戏服
        /// </summary>
        /// <param name="handler"></param>
        event GameServerCallHandler HandleGameServerCall;
    }


    public interface IZoneNodeSession
    {
        /// <summary>
        /// 单位全局唯一标识符
        /// </summary>
        string PlayerUUID { get; }
        /// <summary>
        /// 用于显示的名字
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// 绑定数据
        /// </summary>
        ZoneNode.PlayerClient BindingPlayer { get; set; }

        /// <summary>
        /// 和战斗对接时时回调
        /// </summary>
        /// <param name="binding"></param>
        void OnPlayerConnected(ZoneNode.PlayerClient binding);
        /// <summary>
        /// 和战斗断开时回调
        /// </summary>
        void OnPlayerDisconnect(ZoneNode.PlayerClient binding);

        void OnPlayerDisposed();

        /// <summary>
        /// 向此客户端发送战斗服事件
        /// </summary>
        /// <param name="msg"></param>
        void ClientSend(PlayerMessageEntry msg, bool immediately = false);
        /// <summary>
        /// 每帧一次，发送结束
        /// </summary>
        /// <param name="msg"></param>
        void ClientFlush(BattleCodec codec);
        /// <summary>
        /// 监听客户端消息
        /// </summary>
        /// <param name="handler"></param>
        event ClientMessageHandler HandleClientMessage;



        /// <summary>
        /// 通知游戏服
        /// </summary>
        void PostToGameServer(object msg);
        /// <summary>
        /// 通知游戏服
        /// </summary>
        void PostToGameServer(object msg, Action<object, Exception> callback);

        /// <summary>
        /// 监听游戏服
        /// </summary>
        /// <param name="handler"></param>
        event GameServerMessageHandler HandleGameServerMessage;
        /// <summary>
        /// 监听游戏服
        /// </summary>
        /// <param name="handler"></param>
        event GameServerCallHandler HandleGameServerCall;


    }


    public class ThreadZoneNodeTimer
    {

        private Thread mainThread;
        private BaseZoneNode node;
        public ThreadZoneNodeTimer(BaseZoneNode node)
        {
            this.node = node;
            if (mainThread == null)
            {
                mainThread = new Thread(main);
                mainThread.Name = node.Name;
                mainThread.Start();
            }
        }
        //         protected virtual void TimerJoin()
        //         {
        //             try
        //             {
        //                 mainThread?.Join();
        //             }
        //             catch { }
        //         }
        private void main()
        {
            try
            {
                while (node.Update(DeepCore.CUtils.TickTimeMS))
                {
                    var delay = (float)(node.FixedUpdateInterval -node.LastUsedTime);
                    if (delay > 0)
                    {
                        Thread.Sleep((int)delay);
                    }
                    else
                    {
                        Thread.Sleep(0);
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
        }
    }

}
