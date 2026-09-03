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
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="action"></param>
        public void SendAction(BattleAction action)
        {
            LayerClient.SendAction(action);
            action.Release();
        }
        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="handler"></param>
        /// <param name="timeout"></param>
        /// <param name="timeOutMS"></param>
        /// <returns></returns>
        internal void SendRequest(ActorRequest msg, OnResponseHandler handler, int timeOutMS = 15000)
        {
            if (handler != null)
            {
                msg.Retain();
                Request req = new Request(this, timeOutMS, msg, handler);
                lock (mListenRequests)
                {
                    mListenRequests.Add(req.MessageID, req);
                }
            }
            LayerClient.SendAction(msg);
        }

        /// <summary>
        /// 接收新消息
        /// </summary>
        /// <param name="msg"></param>
        public void QueueMessage(IBattleMessage msg)
        {
            if (IsDisposing) return;
            var queue = mSyncMessageQueue;
            if (queue != null)
            {
                if (msg is NetPong pong)
                {
                    this.NetPing = (int)pong.CurrentPing;
                }
                else if (msg is PackNotify pack)
                {
                    for (int i = 0; i < pack.events.Count; i++)
                    {
                        QueueMessage(pack.events[i] as IBattleMessage);
                    }
                }
                else
                {
                    queue.Enqueue(msg);
                    msg.Retain();
                }
            }
        }
        public void QueueMessages(IEnumerable<IBattleMessage> msg)
        {
            if (IsDisposing) return;
            foreach (var m in msg)
            {
                QueueMessage(m);
            }
        }

        /// <summary>
        /// 客户端模拟服务端的包
        /// </summary>
        /// <param name="msg"></param>
        internal void PreQueueEvent(IBattleMessage msg)
        {
            if (msg is PackNotify pack)
            {
                for (int i = 0; i < pack.events.Count; i++)
                {
                    MainProcessMessage(pack.events[i] as IBattleMessage);
                }
            }
            else
            {
                MainProcessMessage(msg);
            }
        }

        public GameOverEvent LastGameOver => lastGameOver;
        private GameOverEvent lastGameOver;

        //-------------------------------------------------------------------------------------------
        #region MESSAGES

        protected virtual void MainProcessMessage(IBattleMessage msg)
        {
            try
            {
                if (msg is Pong pong)
                {
                    this.CurrentPing = pong.CurrentPing;
                }
                else if (msg is ActorResponse)
                {
                    on_received_response(msg as ActorResponse);
                }
                if (msg is IMessageGUI guimsg)
                {
                    ProcessGUIEvents(guimsg);
                    OnGUIMessageReceived?.Invoke(this, guimsg);
                }
                //
                if (msg is ObjectNotify)
                {
                    var oe = msg as ObjectNotify;
                    var obj = GetObject(oe.object_id);
                    if (obj != null)
                    {
                        //doZoneEvent(ZoneEvent.OBJECT_EVENT, obj, msg);
                        obj.DoEvent(msg as ObjectNotify);
                        obj.cb_OnDoEvent(msg as ObjectNotify);
                        {
                            if (obj is LayerUnit unit)
                            {
                                if (msg is UnitDeadEvent msg_dead)
                                {
                                    OnUnitDead?.Invoke(this, unit, msg_dead.Crushed, msg_dead.attacker_id, msg_dead.DeadTimeMS);
                                }
                                else if (msg is UnitRebirthEvent msg_rebirth)
                                {
                                    OnUnitRebirth?.Invoke(this, unit);
                                }

                            }

                        }
                        if (mMessageReceived != null)
                        {
                            mMessageReceived.Invoke(this, msg);
                        }
                        if (mObjectMessageReceived != null)
                        {
                            mObjectMessageReceived.Invoke(this, msg, obj);
                        }
                        return;
                    }
                    else
                    {
                        log.WarnFormat("Can not find object : {0} {1}", oe.object_id, oe);
                    }
                }
                else if (msg is PackNotify)
                {
                    throw new Exception("PackNotify");
                }
                else if (msg is ServerStatusB2C)
                {
                    mLastServerStatus = msg as ServerStatusB2C;
                }
                else
                {
                    if (msg is ClientEnterScene)
                    {
                        doClientEnterScene(msg as ClientEnterScene);
                    }
                    else if (msg is LockActorEvent)
                    {
                        doLockActorEvent(msg as LockActorEvent);
                    }
                    else if (msg is SyncObjectsEvent)
                    {
                        doSyncUnitsEvent(msg as SyncObjectsEvent);
                    }
                    else if (msg is SyncFlagsEvent)
                    {
                        doSyncFlagsEvent(msg as SyncFlagsEvent);
                    }
                    else if (msg is AddUnitEvent)
                    {
                        doAddUnitEvent(msg as AddUnitEvent);
                    }
                    else if (msg is AddSpellEvent)
                    {
                        doAddSpellEvent(msg as AddSpellEvent);
                    }
                    else if (msg is AddItemEvent)
                    {
                        doAddItemEvent(msg as AddItemEvent);
                    }
                    else if (msg is RemoveObjectEvent)
                    {
                        doRemoveObjectEvent(msg as RemoveObjectEvent);
                    }
                    else if (msg is SyncPosEvent)
                    {
                        doSyncPosEvent(msg as SyncPosEvent);
                    }
                    //                     else if (msg is DecorationChangedEvent)
                    //                     {
                    //                         doDecorationChanged(msg as DecorationChangedEvent);
                    //                     }
                    else if (msg is FlagTagChangedEvent)
                    {
                        doFlagTagChanged(msg as FlagTagChangedEvent);
                    }
                    else if (msg is FlagEnableChangedEvent)
                    {
                        doFlagEnableChanged(msg as FlagEnableChangedEvent);
                    }
                    else if (msg is SyncEnvironmentVarEvent)
                    {
                        doSyncEnvironmentVarEvent(msg as SyncEnvironmentVarEvent);
                    }
                    else if (msg is ChangeBGMEvent)
                    {
                        doChangeBGMEvent(msg as ChangeBGMEvent);
                    }
                    else if (msg is CameraOffset offset)
                    {
                        doCameraOffset(offset);
                    }
                    else if (msg is DoScriptEvent)
                    {
                        doDoScriptEvent(msg as DoScriptEvent);
                    }
                    else if (msg is ScriptCommandEvent)
                    {
                        doScriptCommandEvent(msg as ScriptCommandEvent);
                    }
                    else if (msg is GameOverEvent)
                    {
                        //doZoneEvent(ZoneEvent.GAME_OVER, null, msg);
                        doGameOver(msg as GameOverEvent);
                    }
                    //doZoneEvent(ZoneEvent.MESSAGE, null, msg);
                    if (mMessageReceived != null)
                    {
                        mMessageReceived.Invoke(this, msg);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err);
                this.doError(err);
            }
            finally
            {
                if (msg is BattleNotify be)
                {
                    be.sender = null;
                }
                msg?.Release();
            }
        }

        private void doClientEnterScene(ClientEnterScene msg)
        {
            this.InitSceneData(msg);
        }

        private void doSyncUnitsEvent(SyncObjectsEvent msg)
        {
            foreach (var syn in msg.Objects)
            {
                try
                {
                    doSyncObjectInfo(syn);
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                }
            }
        }
        private LayerZoneObject doSyncObjectInfo(SyncObjectInfo syn)
        {
            var ret = GetObject(syn.ObjectID);
            if (ret == null)
            {
                if (syn is SyncUnitInfo sunit)
                {
                    var unit = Templates.GetUnit(syn.TemplateID);
                    if (unit != null)
                    {
                        ret = SlaveFactory.CreateClientUnit(this, unit, sunit, null);
                    }
                }
                else if (syn is SyncItemInfo sitem)
                {
                    var item = Templates.GetItem(syn.TemplateID);
                    if (item != null)
                    {
                        ret = SlaveFactory.CreateClientItem(this, item, sitem, null);
                    }
                }
                else if (syn is SyncSpellInfo sspell)
                {
                    var spell = Templates.GetSpell(syn.TemplateID);
                    if (spell != null)
                    {
                        ret = SlaveFactory.CreateClientSpell(this, spell, sspell, null);
                    }
                }
                if (ret != null)
                {
                    addObj(ret);
                }
                else
                {
                    log.Error(string.Format("SyncObject : Can Not Create Object : {0} as {1}", syn.TemplateID, syn));
                    return null;
                }
            }
            else
            {
                if (syn is SyncUnitInfo sunit && ret is LayerUnit aunit)
                {
                    aunit.DoSyncInfo(sunit);
                }
                else if (syn is SyncItemInfo sitem && ret is LayerItem aitem)
                {

                }
                else if (syn is SyncSpellInfo sspell && ret is LayerItem aspell)
                {

                }
                else
                {
                    log.Error($"SyncObject : Unknow Unit : {syn} as {ret}");
                }
            }
            ret.InternalSyncObject(syn);
            return ret;
        }

        private void doLockActorEvent(LockActorEvent msg)
        {
            if (msg.CurrentZoneVars != null)
            {
                foreach (ClientStruct.ZoneEnvironmentVar var in msg.CurrentZoneVars)
                {
                    if (EnvironmentVarMap.TrySet(var, out var k, out var v))
                    {
                        this.OnEnvironmentVarChanged?.Invoke(this, k, v);
                    }
                }
            }
            this.ActorSyncMode = msg.ClientSyncMode;
            if (msg.UnitData != null)
            {
                var syn = msg.UnitData;
                var ret = GetUnit(syn.ObjectID);
                if (ret != null)
                {
                    removeObj(syn.ObjectID);
                }
                UnitInfo unit = Templates.GetUnit(syn.TemplateID);
                if (unit != null)
                {
                    LayerPlayer act = SlaveFactory.CreateClientActor(this, unit, msg);
                    addObj(act);
                }
            }
        }

        private void doAddUnitEvent(AddUnitEvent e)
        {
            UnitInfo info = Templates.GetUnit(e.Sync.TemplateID);
            if (info != null)
            {
                LayerUnit ret = SlaveFactory.CreateClientUnit(this, info, e.Sync, e);
                addObj(ret);
            }
        }

        private void doAddSpellEvent(AddSpellEvent e)
        {
            SpellTemplate sp = Templates.GetSpell(e.spell_template_id);
            if (sp != null)
            {
                var syn = new SyncSpellInfo();
                syn.ObjectID = e.spell_id;
                syn.pos = e.spell_pos;
                syn.direction = e.direction;
                var ret = SlaveFactory.CreateClientSpell(this, sp, syn, e);
                ret.InternalSyncObject(syn);               
                addObj(ret);
            }
        }

        private void doAddItemEvent(AddItemEvent e)
        {
            ItemTemplate item = Templates.GetItem(e.Sync.TemplateID);
            if (item != null)
            {
                LayerItem ret = SlaveFactory.CreateClientItem(this, item, e.Sync, e);
                addObj(ret);
            }
        }

        private void doRemoveObjectEvent(RemoveObjectEvent e)
        {
            removeObj(e.object_id);
        }

        private void doSyncPosEvent(SyncPosEvent e)
        {
            this.mRemotePassTimeMS = e.PassTimeMS;
            this.mLastRemotePassClientTimeMS = mLocalPassTimeMS;
            if (e.ReadUnitPosList != null)
            {
                //原版有GC
                /*        
                foreach (var pos in e.ReadUnitPosList)
                {
                  var gu = GetObject<LayerZoneObject>(pos.ID);
                   if(gu != null)
                   {
                     gu.SyncPos(pos);
                   }
                }
                */

                var tempLt = e.ReadUnitPosList;
                UnitSyncPos usp = null;
                for (int i = 0; i < tempLt.Count; i++)
                {
                    usp = tempLt[i];
                    var gu = GetObject(usp.ID);
                    if (gu != null)
                    {
                        gu.SyncPos(usp);
                    }
                }

            }
        }

        //         private void doDecorationChanged(DecorationChangedEvent e)
        //         {
        //             var ed = mObjects.GetFlag<LayerEditorDecoration>(e.Name);
        //             if (ed != null && ed.Enable != e.Enable)
        //             {
        //                 ed.Enable = e.Enable;
        //               
        //                 //doZoneEvent(ZoneEvent.DECORATION_CHANGED, ed, e);
        //             }
        //         }

        private void doSyncFlagsEvent(SyncFlagsEvent msg)
        {
            foreach (var kv in msg.Stats)
            {
                var flag = mObjects.GetFlag(kv.Key);
                if (flag != null)
                {
                    var e = kv.Value;
                    if (flag.Enable != e.enable)
                    {
                        flag.Enable = e.enable;
                        if (flag is LayerEditorDecoration ed)
                        {
                            ed.DecorationChanged();
                            if (mDecorationChanged != null)
                            {
                                mDecorationChanged.Invoke(this, ed);
                            }
                        }
                        if (FlagEnableChanged != null)
                        {
                            FlagEnableChanged.Invoke(this, flag);
                        }
                    }
                    if (flag.Tag != e.tag)
                    {
                        flag.Tag = e.tag;
                        if (mFlagTagChanged != null)
                        {
                            mFlagTagChanged.Invoke(this, flag);
                        }
                    }
                }
            }
            //             mObjects.ForEachFlagsPredicate<LayerZone, LayerEditorDecoration>(this, (z, ed) =>
            //             {
            //                 bool enable = !msg.ClosedDecorations.Contains(ed.Name);
            //                 if (ed.Enable != enable)
            //                 {
            //                     ed.Enable = enable;
            //                     ed.DecorationChanged();
            //                     //doZoneEvent(ZoneEvent.DECORATION_CHANGED, ed);
            //                     if (mDecorationChanged != null)
            //                     {
            //                         mDecorationChanged.Invoke(this, ed);
            //                     }
            //                 }
            //                 return false;
            //             });
        }

        private void doFlagTagChanged(FlagTagChangedEvent e)
        {
            var flag = mObjects.GetFlag(e.Name);
            if (flag != null && flag.Tag != e.Tag)
            {
                flag.Tag = e.Tag;
                if (mFlagTagChanged != null)
                {
                    mFlagTagChanged.Invoke(this, flag);
                }
                //doZoneEvent(ZoneEvent.DECORATION_CHANGED, ed, e);
            }
        }
        private void doFlagEnableChanged(FlagEnableChangedEvent e)
        {
            var flag = mObjects.GetFlag(e.Name);
            if (flag != null && flag.Enable != e.Enable)
            {
                flag.Enable = e.Enable;
                if (flag is LayerEditorDecoration ed)
                {
                    ed.DecorationChanged();
                    if (mDecorationChanged != null)
                    {
                        mDecorationChanged.Invoke(this, ed);
                    }
                }
                if (FlagEnableChanged != null)
                {
                    FlagEnableChanged.Invoke(this, flag);
                }
                //doZoneEvent(ZoneEvent.DECORATION_CHANGED, ed, e);
            }
        }
        private void doSyncEnvironmentVarEvent(SyncEnvironmentVarEvent e)
        {
            if (EnvironmentVarMap.TrySet(e.Var, out var k, out var v))
            {
                OnEnvironmentVarChanged?.Invoke(this, k, v);
            }
        }

        private void doChangeBGMEvent(ChangeBGMEvent e)
        {
            if (mOnChangeBGM != null)
            {
                mOnChangeBGM.Invoke(this, e.FileName);
            }
        }

        private void doDoScriptEvent(DoScriptEvent e)
        {
            if (mOnScriptFile != null)
            {
                mOnScriptFile.Invoke(this, e.ScriptFileName);
            }
        }

        private void doScriptCommandEvent(ScriptCommandEvent e)
        {
            if (mOnScriptCommand != null)
            {
                mOnScriptCommand.Invoke(this, e.message);
            }
        }

        protected virtual void doCameraOffset(CameraOffset cameraOffset)
        {
            this.CameraOffset = cameraOffset;
        }
        private void doGameOver(GameOverEvent evt)
        {
            lastGameOver = evt ?? lastGameOver;
            if (mGameOver != null)
            {
                mGameOver.Invoke(this, evt.WinForce, evt.message);
            }
        }

        protected virtual void doError(Exception obj)
        {
            log.Error(obj.Message, obj);
            OnError?.Invoke(this, obj);
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region REQUEST_RESPONSE

        private void check_request_timeout()
        {
            var curTime = CUtils.TickTimeMS;
            lock (mListenRequests)
            {
                if (mListenRequests.Count > 0)
                {
                    using (var removing = ObjectPool.AllocList<Request>())
                    {
                        foreach (Request req in mListenRequests.Values)
                        {
                            if (req.EndTime < curTime)
                            {
                                removing.Add(req);
                            }
                        }
                        if (removing.Count > 0)
                        {
                            foreach (Request remove in removing)
                            {
                                mListenRequests.RemoveByKey(remove.MessageID);
                                remove.onTimeout();
                            }
                        }
                    }
                }
            }
        }

        private bool on_received_response(ActorResponse rsp)
        {
            Request request = null;
            lock (mListenRequests)
            {
                request = mListenRequests.RemoveByKey(rsp.MessageID) as Request;
            }
            if (request != null)
            {
                request.onRecivedMessage(rsp);
                return true;
            }
            return false;
        }

        private AtomicInteger MessageIDGen = new AtomicInteger(1);

        public class Request
        {
            internal static Logger log = LoggerFactory.GetLogger("ZoneLayer.Request");

            private OnResponseHandler mHandler;
            public LayerZone Layer { get; private set; }
            public int TimeOutMS { get; private set; }
            public double EndTime { get; private set; }
            public double SendTime { get; private set; }
            public int MessageID { get; private set; }
            public bool IsTimeOut { get; private set; }
            public ActorRequest RequestMessage { get; protected set; }
            public ActorResponse ResponseMessage { get; protected set; }

            public Request(LayerZone client, int timeOutMS, ActorRequest request, OnResponseHandler handler = null)
            {
                this.Layer = client;
                this.MessageID = request.MessageID = client.MessageIDGen.GetAndIncrement();
                this.RequestMessage = request;
                this.TimeOutMS = timeOutMS;
                this.SendTime = CUtils.TickTimeMS;
                this.EndTime = SendTime + timeOutMS;
                this.mHandler = handler;
            }
            virtual internal void onRecivedMessage(ActorResponse msg)
            {
                ResponseMessage = msg;
                if (mHandler != null)
                {
                    try
                    {
                        mHandler.Invoke(this);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
                mHandler = null;
            }
            virtual internal void onTimeout()
            {
                IsTimeOut = true;
                if (mHandler != null)
                {
                    try
                    {
                        mHandler.Invoke(this);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
                mHandler = null;
            }
        }
        public delegate void OnResponseHandler(Request req);
        public delegate void OnResponseHandler<RSP>(Request req, RSP response) where RSP : ActorResponse;


        #endregion

    }


}