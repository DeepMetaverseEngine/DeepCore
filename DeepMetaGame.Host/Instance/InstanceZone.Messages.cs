using DeepCore.AI.LLM;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Debug;
using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Threading;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using BattleAction = DeepMetaGame.Data.Message.BattleAction;
using ZoneNotify = DeepMetaGame.Data.Message.ZoneNotify;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class InstanceZone
    {
        //-----------------------------------------------------------------------------------------------------------------------------------
        #region RECV
        public virtual void EnqueueAction(BattleAction actions, InstanceUnit sender)
        {
            actions.Retain();
            actions.sender = sender;
            if (event_OnRecvAction != null)
            {
                event_OnRecvAction.Invoke(this, actions);
            }
            mSyncActionQueue.Enqueue(actions);
        }
        private void MainProcessAction(BattleAction act)
        {
            try
            {
                if (act is IMessageGUI gui)
                {
                    ProcessGUIMessage(act);
                }
                if (act is UIInteractiveAction mouse)
                {
                    ProcessHUDMessage(mouse);
                }
                if (act is ObjectAction oa)
                {
                    var unit = act.sender as InstanceUnit;
                    if (unit == null)
                    {
                        unit = mObjects.GetObject<InstanceUnit>(oa.object_id);
                    }
                    if (unit != null)
                    {
                        unit.doAction(oa);
                    }
                    else
                    {
                        log.Warn($"Can Not Found Object : {oa.object_id} : Drop Action : " + oa);
                    }
                    event_OnProcessObjectAction?.Invoke(unit, oa);
                }
                else
                {
                    event_OnProcessZoneAction?.Invoke(this, act);
                }
            }
            finally
            {
                act.Release();
            }
        }


        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------
        #region SEND

        protected internal void PostEventInternal(BattleNotify evt, IPostChannel channel)
        {
            channel?.Post(evt);
            mSendingEvents.Enqueue(evt);
        }

        public void PostEvent(ZoneNotify evt)
        {
            if (evt is PositionMessage pos)
            {
                var space = this.GetSpaceCellNode(pos.Position);
                if (space != null)
                {
                    //evt.sender = space.Channel;
                    this.PostEventInternal(evt, space.Channel);
                }
                else
                {
                    this.PostEventInternal(evt, mZoneChannel);
                }
            }
            else
            {
                //evt.sender = mZoneChannel;
                this.PostEventInternal(evt, mZoneChannel);
            }
        }
        public void PostEvent<ST, T>(ST st, Action<ST, T> init, T defaultT = default) where T : ZoneNotify, new()
        {
            var evt = this.ObjectPool.Alloc<T>();
            init(st, evt);
            this.PostEvent(evt);
        }
        public void PostSystemMessage(BattleNotify evt)
        {
            if (evt is PositionMessage pos)
            {
                var space = this.GetSpaceCellNode(pos.Position);
                if (space != null)
                {
                    //evt.sender = space.Channel;
                    this.PostEventInternal(evt, space.Channel);
                }
            }
            else
            {
                //evt.sender = mZoneChannel;
                this.PostEventInternal(evt, mZoneChannel);
            }
        }
        public void PostSystemMessage<ST, T>(ST st, Action<ST, T> init, T defaultT = default) where T : BattleNotify, new()
        {
            var evt = this.ObjectPool.Alloc<T>();
            init(st, evt);
            this.PostSystemMessage(evt);
        }

        public void PostObjectEvent(InstanceZoneObject obj, ObjectNotify evt)
        {
            if (obj.ID == 0)
            {
                evt.Dispose();
                return;
            }
            if (!obj.IsInZone)
            {
                evt.Dispose();
                return;
            }
            obj.onSendingEvent(ref evt);
            if (evt != null)
            {
                evt.object_id = obj.ID;
                evt.sender = obj;
                this.PostEventInternal(evt, obj.CurrentChannel);
            }
        }
        public void PostObjectEvent<ST, T>(InstanceZoneObject obj, ST st, Action<ST, T> init, T defaultT = default) where T : ObjectNotify, new()
        {
            var evt = this.ObjectPool.Alloc<T>();
            init(st, evt);
            this.PostObjectEvent(obj, evt);
        }

        /// <summary>
        /// 立即发送指令
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="req"></param>
        /// <param name="rsp"></param>
        public void PostActorResponse(InstanceZoneObject obj, ActorRequest req, ActorResponse rsp)
        {
            if (obj.ID == 0)
                return;
            if (!obj.IsInZone)
                return;
            rsp.MessageID = req.MessageID;
            rsp.object_id = obj.ID;
            rsp.sender = obj;
            this.PostEventInternal(rsp, obj.CurrentChannel);
        }

        public virtual void BroadcastMessageBox(string msg, bool showLog = true)
        {
            if (CFG?.ENABLE_SHOW_ERROR_MSG_BOX == true)
            {
                if (showLog)
                {
                    Log?.Error(msg);
                }
                var evt = ObjectPool.Alloc<TestMessageBox>().Init(msg);
                //evt.sender = mZoneChannel;
                this.PostEventInternal(evt, mZoneChannel);
            }
        }

        private void ProcessEvents()
        {
            try
            {
                if (sync_pos_list.Enable)
                {
                    var sync_pos = sync_pos_list.AllocSyncPosEvent(this);
                    if (sync_pos != null)
                    {
                        PostEvent(sync_pos);
                    }
                }
                sync_pos_list.Clear();
                if (mSendingEvents.Count > 0)
                {
                    using (var fuck = ObjectPool.AllocList<BattleNotify>())
                    {
                        //fuck.AddRange(mSendingEvents);//GC 40B
                        while (mSendingEvents.Count > 0)
                        {
                            fuck.Add(mSendingEvents.Dequeue());
                        }
                        if (event_OnPostEvent != null)
                        {
                            event_OnPostEvent.Invoke(this, fuck);
                        }
                        if (mListener != null)
                        {
                            mListener.OnEventHandler(fuck);
                        }
                        if (event_OnGameOver != null)
                        {
                            foreach (var evt in fuck)
                            {
                                if (evt is GameOverEvent)
                                {
                                    event_OnGameOver.Invoke(this, evt as GameOverEvent);
                                }
                            }
                        }
                        for (int i = fuck.Count - 1; i >= 0; --i)
                        {
                            fuck[i].Release();
                        }
                    }
                }
            }
            finally
            {
                mSendingEvents.Clear();
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------
    }



}
