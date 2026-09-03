using DeepCore.AI.LLM;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Debug;
using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.GameData.EventTrigger;
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
using System;
using System.Collections.Generic;
using BattleAction = DeepMetaGame.Data.Message.BattleAction;
using ZoneNotify = DeepMetaGame.Data.Message.ZoneNotify;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceZone
    {
        //         private void MainProcessTask(Action<InstanceZone> task)
        //         {
        //             task(this);
        //         }
        /// <summary>
        /// 【线程安全】向主线程排一个任务
        /// </summary>
        /// <param name="task"></param>
        public void QueueTask(System.Action<InstanceZone> task)
        {
            mTasks.Enqueue(task);
        }
        public void QueueTask<ST>(ST st, System.Action<InstanceZone, ST> task)
        {
            mTasks.Enqueue(st, task);
        }
        public override ZoneTimeExpire AllocTimeExpire(float delayMS)
        {
            return ZoneTimeExpire.Alloc(this, delayMS);
        }
        public override ZoneTimeInterval AllocTimeInterval(float intervalMS)
        {
            return ZoneTimeInterval.Alloc(this, intervalMS);
        }

        /// <summary>
        /// 【线程安全】增加时间任务
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="delayMS"></param>
        /// <param name="repeat"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimeTask(float intervalMS, float delayMS, int repeat, TickHandler handler)
        {
            return mTimeTasks.AddTimeTask(intervalMS, delayMS, repeat, handler);
        }
        /// <summary>
        /// 【线程安全】增加延时回调方法
        /// </summary>
        /// <param name="delayMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimeDelayMS(float delayMS, TickHandler handler)
        {
            return mTimeTasks.AddTimeDelayMS(delayMS, handler);
        }
        /// <summary>
        /// 【线程安全】增加定时回调方法
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimePeriodicMS(float intervalMS, TickHandler handler)
        {
            return mTimeTasks.AddTimePeriodicMS(intervalMS, handler);
        }
        /// <summary>
        /// 【线程安全】增加时间任务
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="delayMS"></param>
        /// <param name="repeat"></param>
        /// <param name="handler"></param>
        public TimeTaskMS<ST> AddTimeTask<ST>(float intervalMS, float delayMS, int repeat, ST st, TickHandler<ST> handler)
        {
            return mTimeTasks.AddTimeTask<ST>(intervalMS, delayMS, repeat, st, handler);
        }
        /// <summary>
        /// 【线程安全】增加延时回调方法
        /// </summary>
        /// <param name="delayMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS<ST> AddTimeDelayMS<ST>(float delayMS, ST st, TickHandler<ST> handler)
        {
            return mTimeTasks.AddTimeDelayMS<ST>(delayMS, st, handler);
        }
        /// <summary>
        /// 【线程安全】增加定时回调方法
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS<ST> AddTimePeriodicMS<ST>(float intervalMS, ST st, TickHandler<ST> handler)
        {
            return mTimeTasks.AddTimePeriodicMS<ST>(intervalMS, st, handler);
        }


    }



}
