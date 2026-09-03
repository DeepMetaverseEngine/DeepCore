using DeepCore.Concurrent;
using DeepCore.EventTrigger;
using DeepCore.Game3D.Slave.Helper;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCore.Threading;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Data;


namespace DeepCore.Game3D.Slave.Layer
{
    partial class LayerZone
    {
        //-------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------
        #region EVENTS

        public delegate void OnInitHandler(LayerZone layer);
        public delegate void OnDisposeHandler(LayerZone layer);
        public delegate void OnActorAddedHandler(LayerZone layer, LayerPlayer actor);
        public delegate void OnObjectEnterHandler(LayerZone layer, LayerZoneObject obj);
        public delegate void OnObjectLeaveHandler(LayerZone layer, LayerZoneObject obj);
        public delegate void OnDecorationChangedHandler(LayerZone layer, LayerEditorDecoration ed);
        public delegate void OnFlagTagChangedHandler(LayerZone layer, LayerFlag flag);
        public delegate void OnFlagEnableChangedHandler(LayerZone layer, LayerFlag flag);

        public delegate void OnMessageReceivedHandler(LayerZone layer, IBattleMessage msg);
        public delegate void OnObjectMessageReceivedHandler(LayerZone layer, IBattleMessage msg, LayerZoneObject obj);
        public delegate void OnGUIMessageReceivedHandler(LayerZone layer, IMessageGUI msg);

        public delegate void OnGameOverHandler(LayerZone layer, int winForce, string msg);
        public delegate void OnScriptCommandHandler(LayerZone layer, string msg);
        public delegate void OnScriptFileHandler(LayerZone layer, string filename);
        public delegate void OnChangeBGMHandler(LayerZone layer, string filename);
        public delegate void OnUnitDeadHandler(LayerZone layer, LayerUnit obj, bool Crushed, uint attackerID, float deadTimeMS);
        public delegate void OnUnitRebirthHandler(LayerZone layer, LayerUnit obj);
        public delegate void OnEnvironmentVarChangedHandler(LayerZone layer, string key, object value);
  
        /* 只要不加 event 就不会报错 */
        private OnInitHandler mLayerInit;
        private OnDisposeHandler mOnDispose;
        private OnActorAddedHandler mActorAdded;
        private OnObjectEnterHandler mObjectEnter;
        private OnObjectLeaveHandler mObjectLeave;
        private OnDecorationChangedHandler mDecorationChanged;
        private OnFlagTagChangedHandler mFlagTagChanged;
        private OnMessageReceivedHandler mMessageReceived;
        private OnObjectMessageReceivedHandler mObjectMessageReceived;

        private OnGameOverHandler mGameOver;
        private OnScriptCommandHandler mOnScriptCommand;
        private OnScriptFileHandler mOnScriptFile;
        private OnChangeBGMHandler mOnChangeBGM;

        [EventTriggerDescAttribute("客户端场景初始化时触发")]
        public event OnInitHandler LayerInit { add { mLayerInit += value; } remove { mLayerInit -= value; } }
        public event OnDisposeHandler LayerDispose { add { mOnDispose += value; } remove { mOnDispose -= value; } }

        [EventTriggerDescAttribute("主角被添加时触发")]
        public event OnActorAddedHandler ActorAdded { add { mActorAdded += value; } remove { mActorAdded -= value; } }
        [EventTriggerDescAttribute("单位进入场景")]
        public event OnObjectEnterHandler ObjectEnter { add { mObjectEnter += value; } remove { mObjectEnter -= value; } }
        [EventTriggerDescAttribute("单位离开场景")]
        public event OnObjectLeaveHandler ObjectLeave { add { mObjectLeave += value; } remove { mObjectLeave -= value; } }
        [EventTriggerDescAttribute("空气墙变化时触发")]
        public event OnDecorationChangedHandler DecorationChanged { add { mDecorationChanged += value; } remove { mDecorationChanged -= value; } }
        [EventTriggerDescAttribute("Flag Tag 变化时触发")]
        public event OnFlagTagChangedHandler FlagTagChanged { add { mFlagTagChanged += value; } remove { mFlagTagChanged -= value; } }
        [EventTriggerDescAttribute("Flag Enable 变化时触发")]
        public event OnFlagEnableChangedHandler FlagEnableChanged;


        [EventTriggerDescAttribute("接收网络消息")]
        public event OnMessageReceivedHandler MessageReceived { add { mMessageReceived += value; } remove { mMessageReceived -= value; } }
        [EventTriggerDescAttribute("接收Object网络消息")]
        public event OnObjectMessageReceivedHandler ObjectMessageReceived { add { mObjectMessageReceived += value; } remove { mObjectMessageReceived -= value; } }

        [EventTriggerDescAttribute("BGM发生变化时触发")]
        public event OnChangeBGMHandler OnChangeBGM { add { mOnChangeBGM += value; } remove { mOnChangeBGM -= value; } }
        [EventTriggerDescAttribute("游戏结束")]
        public event OnGameOverHandler GameOver { add { mGameOver += value; } remove { mGameOver -= value; } }
        [EventTriggerDescAttribute("服务端通知客户端执行指定脚本代码")]
        public event OnScriptCommandHandler OnScriptCommand { add { mOnScriptCommand += value; } remove { mOnScriptCommand -= value; } }
        [EventTriggerDescAttribute("服务端通知客户端执行指定脚本文件")]
        public event OnScriptFileHandler OnScriptFile { add { mOnScriptFile += value; } remove { mOnScriptFile -= value; } }

        public event Action<LayerZone, Exception> OnError;

        public event OnUnitDeadHandler OnUnitDead;
        public event OnUnitRebirthHandler OnUnitRebirth;
        public event OnEnvironmentVarChangedHandler OnEnvironmentVarChanged;
        public event OnGUIMessageReceivedHandler OnGUIMessageReceived;


        private void ClearEvents()
        {
            this.mLayerInit = null;
            this.mOnDispose = null;
            this.mActorAdded = null;
            this.mObjectEnter = null;
            this.mObjectLeave = null;
            this.mDecorationChanged = null;
            this.mFlagTagChanged = null;
            this.mMessageReceived = null;
            this.mObjectMessageReceived = null;

            this.mGameOver = null;
            this.mOnScriptCommand = null;
            this.mOnScriptFile = null;
            this.mOnChangeBGM = null;
            this.OnUnitRebirth = null;

            this.OnError = null;
            this.OnUnitDead = null;
            this.OnEnvironmentVarChanged = null;
        }
        #endregion
    }


}