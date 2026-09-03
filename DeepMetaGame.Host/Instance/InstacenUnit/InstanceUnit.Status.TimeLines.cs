using DeepCore.Components;
using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.FuncData;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.Instance.Triggers;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.Game3D.Host.Instance
{

    /// <summary>
    /// 所有常态状态（Buff，技能，被动系）
    /// </summary>
    partial class InstanceUnit
    {
        //-----------------------------------------------------------------------------------------------------//

        //--------------------------------------------------------------------------------------------------------------
        #region _时效性效果_
        //--------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 是否可被发现
        /// </summary>
        virtual public bool IsVisible { get { return !mInvisibleTimeMS.Enable; } }
        /// <summary>
        /// 是否无敌
        /// </summary>
        virtual public bool IsInvincible { get { return mInvincibleTimeMS.Enable; } }
        /// <summary>
        /// 是否无敌
        /// </summary>
        virtual public bool IsNoDamage { get { return mNoDamageTimeMS.Enable; } }
        /// <summary>
        /// 此单位是否霸体
        /// </summary>
        virtual public bool IsNoneBlock { get { return mNoneBlockTimeMS.Enable; } }
        /// <summary>
        /// 是否为眩晕
        /// </summary>
        virtual public bool IsStun { get { return mStunTimeMS.Enable; } }
        /// <summary>
        /// 是否沉默
        /// </summary>
        virtual public bool IsSilent { get { return mSilentTimeMS.Enable; } }
        /// <summary>
        /// 是否锁住移动
        /// </summary>
        virtual public bool IsLock { get { return mLockTimeMS.Enable; } }

        //--------------------------------------------------------------------------------------------------------------
        private readonly List<MultiTimeLine> mMultiTimeLineGroup = new List<MultiTimeLine>();
        private UnitSyncMultiTimeLine mMultiTimeLineSync;
        private bool mTimelineDirty = true;
        private void cleanMultiTimeLines()
        {
            mTimelineDirty = true;
            foreach (var line in mMultiTimeLineGroup)
            {
                line.Clear();
            }
            mMultiTimeLineGroup.Clear();
        }
        /// <summary>
        /// 注册自定义MultiTimeLine
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MultiTimeLine RegistMultiTimeLine(out int index)
        {
            mTimelineDirty = true;
            index = mMultiTimeLineGroup.Count;
            var timeline = new MultiTimeLine(ObjectPool);
            mMultiTimeLineGroup.Add(timeline);
            return timeline;
        }
        /// <summary>
        /// 获取指定ID TimeLine
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MultiTimeLine GetTimeLine(int index)
        {
            if (mMultiTimeLineGroup.Count > index)
            {
                return mMultiTimeLineGroup[index];
            }
            return null;
        }
        /// <summary>
        /// 指定TimeLine是否还有任务
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsTimeLineEnable(int index)
        {
            return mMultiTimeLineGroup[index].Enable;
        }
        /// <summary>
        /// 获取指定 TimeLine 的 ID
        /// </summary>
        /// <param name="timeline"></param>
        /// <returns></returns>
        public int GetTimeLineIndex(MultiTimeLine timeline)
        {
            return mMultiTimeLineGroup.IndexOf(timeline);
        }
        /// <summary>
        /// 添加TimeLine任务
        /// </summary>
        /// <param name="index"></param>
        /// <param name="timeMS"></param>
        /// <returns></returns>
        public TimeExpire AddTimeLineTask(int index, int timeMS)
        {
            mTimelineDirty = true;
            return mMultiTimeLineGroup[index].Add(timeMS);
        }
        /// <summary>
        /// 强制移除TimeLine任务
        /// </summary>
        /// <param name="index"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool RemoveTimeLineTask(int index, TimeExpire task)
        {
            mTimelineDirty = true;
            return mMultiTimeLineGroup[index].Remove(task);
        }

        public void RemoveTimeLineTask(int index)
        {
            mTimelineDirty = true;
            var ret = mMultiTimeLineGroup[index];
            ret?.Clear();
            mMultiTimeLineGroup.RemoveAt(index);
        }
        //--------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 被击中后不会被中断（霸体），是一个叠加值
        /// </summary>
        private MultiTimeLine mNoneBlockTimeMS;
        /// <summary>
        /// 眩晕，移动限制状态，是一个叠加值
        /// </summary>
        private MultiTimeLine mStunTimeMS;
        /// <summary>
        /// 隐形，是一个叠加值
        /// </summary>
        private MultiTimeLine mInvisibleTimeMS;
        /// <summary>
        /// 无敌时间，是个叠加值
        /// </summary>
        private MultiTimeLine mInvincibleTimeMS;
        /// <summary>
        /// 无伤时间，是个叠加值
        /// </summary>
        private MultiTimeLine mNoDamageTimeMS;
        /// <summary>
        /// 沉默时间，是个叠加值
        /// </summary>
        private MultiTimeLine mSilentTimeMS;
        /// <summary>
        /// 锁住时间，是个叠加值
        /// </summary>
        private MultiTimeLine mLockTimeMS;

        private void InitTimeLines()
        {
            int index;
            mTimelineDirty = true;
            mNoneBlockTimeMS = RegistMultiTimeLine(out index);//0
            mStunTimeMS = RegistMultiTimeLine(out index);//1
            mInvisibleTimeMS = RegistMultiTimeLine(out index);//2
            mInvincibleTimeMS = RegistMultiTimeLine(out index);//3
            mNoDamageTimeMS = RegistMultiTimeLine(out index);//4
            mSilentTimeMS = RegistMultiTimeLine(out index);//5
            mLockTimeMS = RegistMultiTimeLine(out index);//6
            mMultiTimeLineSync = ObjectPool.Alloc<UnitSyncMultiTimeLine>().Init(this.ID);
        }
        private void UpdateTimeLines(float intervalMS)
        {
            var changed = false;
            for (int i = mMultiTimeLineGroup.Count - 1; i >= 0; --i)
            {
                if (mMultiTimeLineGroup[i].Update(intervalMS))
                {
                    changed = true;
                }
            }
            if (changed || mTimelineDirty)
            {
                mTimelineDirty = false;
                if (mMultiTimeLineSync.Update(mMultiTimeLineGroup))
                {
                    var evt = ObjectPool.Alloc<UnitSyncMultiTimeLine>().Init(this.ID);
                    evt.Update(mMultiTimeLineGroup);
                    PostEvent(evt);
                }
            }
        }

        /// <summary>
        /// 设置霸体时间，如果当前已霸体，则取最大值
        /// </summary>
        /// <param name="timeMS"></param>
        public TimeExpire SetNoneBlockTimeMS(float timeMS)
        {
            mTimelineDirty = true;
            return mNoneBlockTimeMS.Add(timeMS);
        }
        /// <summary>
        /// 设置眩晕时间，如果当前已眩晕，则取最大值
        /// </summary>
        /// <param name="timeMS"></param>
        public TimeExpire SetStunTimeMS(float timeMS)
        {
            mTimelineDirty = true;
            var ret = mStunTimeMS.Add(timeMS);
            if (!IsStateDead)
            {
                ChangeState(StateStun.Alloc(this));
            }
            return ret;
        }
        /// <summary>
        /// 设置隐身时间，如果当前已隐身，则取最大值
        /// </summary>
        /// <param name="timeMS"></param>
        public TimeExpire SetInvisibleTimeMS(float timeMS)
        {
            mTimelineDirty = true;
            return mInvisibleTimeMS.Add(timeMS);
        }
        /// <summary>
        /// 设置无敌时间，如果当前已无敌，则取最大值
        /// </summary>
        /// <param name="timeMS"></param>
        public TimeExpire SetInvincibleTimeMS(float timeMS)
        {
            mTimelineDirty = true;
            return mInvincibleTimeMS.Add(timeMS);
        }
        public TimeExpire SetNoDamageTimeMS(float timeMS)
        {
            mTimelineDirty = true;
            return mNoDamageTimeMS.Add(timeMS);
        }
        /// <summary>
        /// 设置沉默时间
        /// </summary>
        /// <param name="timeMS"></param>
        public TimeExpire SetSilentTimeMS(float timeMS)
        {
            mTimelineDirty = true;
            return mSilentTimeMS.Add(timeMS);
        }
        /// <summary>
        /// 设置锁住时间
        /// </summary>
        /// <param name="timeMS"></param>
        public TimeExpire SetLockTimeMS(float timeMS)
        {
            mTimelineDirty = true;
            return mLockTimeMS.Add(timeMS);
        }


        public bool RemoveNoneBlock(TimeExpire task)
        {
            mTimelineDirty = true;
            return mNoneBlockTimeMS.Remove(task);
        }
        public bool RemoveStun(TimeExpire task)
        {
            mTimelineDirty = true;
            return mStunTimeMS.Remove(task);
        }
        public bool RemoveInvisible(TimeExpire task)
        {
            mTimelineDirty = true;
            return mInvisibleTimeMS.Remove(task);
        }
        public bool RemoveInvincible(TimeExpire task)
        {
            mTimelineDirty = true;
            return mInvincibleTimeMS.Remove(task);
        }
        public bool RemoveNoDamage(TimeExpire task)
        {
            mTimelineDirty = true;
            return mNoDamageTimeMS.Remove(task);
        }
        public bool RemoveSilent(TimeExpire task)
        {
            mTimelineDirty = true;
            return mSilentTimeMS.Remove(task);
        }
        public bool RemoveLock(TimeExpire task)
        {
            mTimelineDirty = true;
            return mLockTimeMS.Remove(task);
        }

        public void ClearNoneBlock()
        {
            mTimelineDirty = true;
            mNoneBlockTimeMS.Clear();
        }
        public void ClearStun()
        {
            mTimelineDirty = true;
            mStunTimeMS.Clear();
        }
        public void ClearInvisible()
        {
            mTimelineDirty = true;
            mInvisibleTimeMS.Clear();
        }
        public void ClearInvincible()
        {
            mTimelineDirty = true;
            mInvincibleTimeMS.Clear();
        }
        public void ClearNoDamage()
        {
            mTimelineDirty = true;
            mNoDamageTimeMS.Clear();
        }
        public void ClearSilent()
        {
            mTimelineDirty = true;
            mSilentTimeMS.Clear();
        }
        public void ClearLock()
        {
            mTimelineDirty = true;
            mLockTimeMS.Clear();
        }
        #endregion

        //---------------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------------

    }
}
