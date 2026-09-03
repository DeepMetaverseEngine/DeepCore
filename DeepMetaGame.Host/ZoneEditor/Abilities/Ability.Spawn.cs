using DeepCore.Game3D.Host.Helper;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Xml.Linq;
using static DeepCore.Game3D.Host.Instance.Abilities.AbstractSpawnAbility;
using static DeepCore.Game3D.Host.Instance.Abilities.SpawnUnitAbility;

namespace DeepCore.Game3D.Host.Instance.Abilities
{
    public interface ISpawnContainer
    {
        string Name { get; }
        InstanceZone Zone { get; }
        bool Enable { get; }
        float Direction { get; }
        Vector3 Position { get; }
        SpawnCollection SpawnCollection { get; }

        Vector3 GetSpawnPos(AbstractSpawnAbility spawn);
        void KeepInSpawnRegion(AbstractSpawnAbility spawn, ref Vector3 pos);
        void BeginSpawnOnce(AbstractSpawnAbility spawn);

        event Action<ISpawnContainer> OnSpawnEnabled;
        event Action<ISpawnContainer> OnSpawnDisabled;
    }
    public delegate void SpawnAction<ST>(ST st, AbstractSpawnAbility spawn, InstanceZoneObject spawned);
    //-------------------------------------------------------------------------------------------

    /// <summary>
    /// 生成单位的触发器
    /// </summary>
    public abstract class AbstractSpawnAbility : Ability
    {
        public ISpawnContainer Container => mAdded;
        protected ISpawnContainer mAdded = null;

        private float mSpawnTimeIntervalMS;
        private float mDelayTimeMS = 0;
        private int mSpawnOnceCount;

        protected LaunchEffect mSpawnEffect;
        protected LaunchEffect mSpawnObjectEffect;

        protected bool mSpawnWithoutAlive = true;
        protected bool mResetOnWithoutAlive = false;

        protected int mTotalSpawnCount = 0;

        private int mLimitedSpawnCount = 0;
        private int mLimitedAliveCount = 0;

        protected HashMap<InstanceZoneObject, InstanceZoneObject> mSpawnedUnits =
            new HashMap<InstanceZoneObject, InstanceZoneObject>();

        private string mUnitTag;

        protected TimeTaskMS mTimeTask;

        private float mStartDirection = float.NaN;
        protected TeamFormation mTeam;
        protected TeamFormationGroup mTeamHelper;

        //--------------------------------------------------------------------------------------------------
        public AbstractSpawnAbility(InstanceZone zone, EditorAbilityData data)
            : base(zone, data)
        {
            Zone.OnObjectRemoved += removeSpawned;
        }

        protected override void OnStart(InstanceAttributes obj)
        {
            if (obj is ISpawnContainer region)
            {
                if (mAdded == null)
                {
                    InstanceZone zone = region.Zone;
                    mAdded = region;
                    if (SpawnTemplateCount > 0)
                    {
                        mTimeTask = zone.AddTimeTask(mSpawnTimeIntervalMS, mDelayTimeMS, 0, this, static (st, t) => st.onTimeSpawn(t));
                    }
                    region.OnSpawnDisabled += this.onRegionStop;
                    region.OnSpawnEnabled += this.onRegionStart;
                    region.SpawnCollection.AddSpawn(this);
                    if (!region.Enable)
                    {
                        mTimeTask?.Pause();
                    }
                }
                else
                {
                    throw new Exception("This Trigger already bind a region !!!");
                }
            }
        }

        //--------------------------------------------------------------------------------------------------
        protected override void Disposing()
        {
            Zone.OnObjectRemoved -= removeSpawned;
            base.Disposing();
            mOnSpawnOver = null;
            mOnSpawnObject = null;
            mOnLastObjectRemoved = null;
            mTimeTask?.Dispose();
        }

        public InstanceZone BindingZone
        {
            get { return mAdded.Zone; }
        }

        public ISpawnContainer BindingRegion
        {
            get { return mAdded; }
        }

        public int OnceSpawnCount
        {
            get { return mSpawnOnceCount; }
        }

        public int LimitedSpawnCount
        {
            get { return mLimitedSpawnCount; }
        }

        public int LimitedAliveCount
        {
            get { return mLimitedAliveCount; }
        }

        /// <summary>
        /// 当前存活数量
        /// </summary>
        public virtual int AliveCount
        {
            get { return mSpawnedUnits.Count; }
        }


        public int TotalSpawnCount
        {
            get { return mTotalSpawnCount; }
        }

        public float StartDirection
        {
            get
            {
                return (!float.IsNaN(mStartDirection))
                    ? mStartDirection
                    : (float)(Zone.RandomN.NextDouble() * CMath.PI_MUL_2);
            }
        }

        /// <summary>
        /// 是否已完成刷新
        /// </summary>
        public virtual bool IsSpawnOver
        {
            get
            {
                if (mLimitedSpawnCount == 0)
                    return false;
                return (mTotalSpawnCount >= mLimitedSpawnCount);
            }
        }

        public bool IsWaitAlive
        {
            get
            {
                if (mLimitedAliveCount == 0)
                    return false;
                return (mSpawnedUnits.Count >= mLimitedAliveCount);
            }
        }

        /// <summary>
        /// 生产单位间隔时间
        /// </summary>
        /// <param name="interval"></param>
        public void setSpawnInterval(float interval)
        {
            this.mSpawnTimeIntervalMS = interval;
        }
        public void setTotalSpawnCount(int count)
        {
            this.mTotalSpawnCount = count;
        }



        public void ResetSpawnInterval(float? interval = null)
        {
            mTimeTask?.Dispose();
            if (!interval.HasValue)
            {
                interval = mSpawnTimeIntervalMS;
            }
            else
            {
                mSpawnTimeIntervalMS = interval.Value;
            }
            if (SpawnTemplateCount > 0)
            {
                mTimeTask = Zone.AddTimeTask(interval.Value, mDelayTimeMS, 0, this, static (st, t) => st.onTimeSpawn(t));
            }
            if (BindingRegion != null && !BindingRegion.Enable)
            {
                mTimeTask?.Pause();
            }
        }

        /// <summary>
        /// 设置每次产生个数
        /// </summary>
        /// <param name="count"></param>
        public void setSpawnCount(int count)
        {
            this.mSpawnOnceCount = count;
        }

        /// <summary>
        /// 设置出生特效
        /// </summary>
        /// <param name="effect"></param>
        public void setSpawnEffect(LaunchEffect effect, LaunchEffect objEffect)
        {
            this.mSpawnEffect = effect;
            this.mSpawnObjectEffect = objEffect;
        }

        /// <summary>
        /// 单位产生绑定的标志
        /// </summary>
        /// <param name="tag"></param>
        public void setUnitTag(string tag)
        {
            this.mUnitTag = tag;
        }

        /// <summary>
        /// 只有当前怪物没有存活时才刷新
        /// </summary>
        /// <param name="without_alive"></param>
        public void setSpawnWithoutAlive(bool without_alive)
        {
            this.mSpawnWithoutAlive = without_alive;
        }

        /// <summary>
        /// 设置怪物存活最大上限
        /// </summary>
        /// <param name="count"></param>
        public void setLimitedAliveCount(int count)
        {
            this.mLimitedAliveCount = count;
        }

        /// <summary>
        /// 设置怪物最大产生数量
        /// </summary>
        /// <param name="count"></param>
        public void setLimitedSpawnCount(int count)
        {
            this.mLimitedSpawnCount = count;
        }

        /// <summary>
        /// 设置当前触发器禁止时间，就是多长时间后才起效。
        /// </summary>
        /// <param name="time"></param>
        public void setDelayTime(float time)
        {
            this.mDelayTimeMS = time;
        }

        public void setStartDirection(float direction)
        {
            if (direction >= 0)
            {
                this.mStartDirection = direction;
            }
        }

        public void setTeamFormation(TeamFormation team)
        {
            this.mTeam = team;
        }


        public void setResetOnWithoutAlive(bool flag)
        {
            this.mResetOnWithoutAlive = flag;
        }

        public void getSpawnedObjects<T>(List<T> list) where T : InstanceZoneObject
        {
            foreach (var o in mSpawnedUnits.Values)
            {
                if (o is T)
                {
                    list.Add(o as T);
                }
            }
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        public void Reset()
        {
            mTotalSpawnCount = 0;
            mSpawnedUnits.Clear();
            if (mTimeTask == null)
            {
                if (SpawnTemplateCount > 0)
                {
                    mTimeTask = Zone.AddTimeTask(mSpawnTimeIntervalMS, mDelayTimeMS, 0, this, static (st, t) => st.onTimeSpawn(t));
                }
            }
            else
            {
                mTimeTask?.Reset();
            }
            onReset();
        }

        protected virtual void onReset() { }

        private void onRegionStart(ISpawnContainer region)
        {
            mTimeTask?.Resume();
        }

        private void onRegionStop(ISpawnContainer region)
        {
            mTimeTask?.Pause();
        }
        protected virtual bool onBeginSpawn(ISpawnContainer region) { return true; }

        protected virtual void onTimeSpawn(TimeTaskMS task)
        {
            spawnOnce();
        }
        protected int spawnOnce()
        {
            ISpawnContainer region = mAdded;
            InstanceZone zone = region.Zone;
            if (!region.Enable)
            {
                return 0;
            }
            if (SpawnTemplateCount <= 0)
            {
                return 0;
            }
            if (mSpawnWithoutAlive && mSpawnedUnits.Count > 0)
            {
                return 0;
            }
            if (IsSpawnOver)
            {
                return 0;
            }
            if (IsWaitAlive)
            {
                return 0;
            }
            var count = 0;
            if (mTeam != null)
            {
                if (mTeamHelper == null || mTeamHelper.Data != mTeam)
                {
                    mTeamHelper = new TeamFormationGroup(mTeam, region);
                }
                for (int i = mSpawnOnceCount - 1; i >= 0; --i)
                {
                    var pos = region.GetSpawnPos(this);
                    mTeamHelper.AddPos(pos.X, pos.Y, pos.Z, StartDirection);
                }
                mTeamHelper.ResetPos(this);
            }
            else
            {
                mTeamHelper = null;
            }
            if (onBeginSpawn(region))
            {
                try
                {
                    region.BeginSpawnOnce(this);
                    for (int i = mSpawnOnceCount - 1; i >= 0; --i)
                    {
                        if (IsSpawnOver)
                        {
                            break;
                        }
                        if (IsWaitAlive)
                        {
                            break;
                        }
                        var pos = (mTeamHelper != null) ? mTeamHelper.PopPos() : null;
                        var obj = SpawnObject(pos);
                        if (obj != null)
                        {
                            count++;
                            this.mTotalSpawnCount++;
                            this.mSpawnedUnits.Put(obj, obj);
                            obj.UnitTag = mUnitTag;
                            OnObjectSpawned(obj);
                            if (mOnSpawnObject != null)
                            {
                                mOnSpawnObject.Invoke(region, this, obj);
                            }
                            if (mSpawnEffect != null)
                            {
                                zone.PostEvent(zone.ObjectPool.Alloc<AddEffectEvent>().Init(obj.ID, region.Position, region.Direction, mSpawnEffect));
                            }
                            if (mSpawnObjectEffect != null)
                            {
                                zone.PostObjectEvent(obj, zone.ObjectPool.Alloc<UnitEffectEvent>().Init(obj.ID, mSpawnObjectEffect));
                            }
                            if (IsSpawnOver)
                            {
                                if (mOnSpawnOver != null)
                                {
                                    mOnSpawnOver.Invoke(mAdded, this);
                                }
                                return count;
                            }
                        }
                    }
                }
                finally
                {
                    if (mTeamHelper != null)
                    {
                        mTeamHelper.Clear();
                    }
                    mOnSpawnOnce?.Invoke(mAdded, this, count);
                }
            }
            return count;
        }

        public delegate InstanceZoneObject SpawnObjectDelegate<ST>(AbstractSpawnAbility ab, ST st, TeamFormationGroup.TeamMember pos);
        [Desc("手动刷一次")]
        public int SpawnOnce<ST>(ST st, SpawnObjectDelegate<ST> spawnObject, int? spawnCountLimit = null)
        {
            var count = 0;
            ISpawnContainer region = mAdded;
            InstanceZone zone = region.Zone;
            if (mTeam != null)
            {
                if (mTeamHelper == null || mTeamHelper.Data != mTeam)
                {
                    mTeamHelper = new TeamFormationGroup(mTeam, region);
                }
                for (int i = mSpawnOnceCount - 1; i >= 0; --i)
                {
                    var pos = region.GetSpawnPos(this);
                    mTeamHelper.AddPos(pos.X, pos.Y, pos.Z, StartDirection);
                }
                mTeamHelper.ResetPos(this);
            }
            else
            {
                mTeamHelper = null;
            }
            try
            {
                {
                    for (int i = mSpawnOnceCount - 1; i >= 0; --i)
                    {
                        if (spawnCountLimit.HasValue && count >= spawnCountLimit.Value)
                        {
                            break;
                        }
                        var pos = (mTeamHelper != null) ? mTeamHelper.PopPos() : null;
                        var obj = spawnObject(this, st, pos);
                        if (obj != null)
                        {
                            count++;
                            this.mSpawnedUnits.Put(obj, obj);
                            obj.UnitTag = mUnitTag;
                            OnObjectSpawned(obj);
                            if (mOnSpawnObject != null)
                            {
                                mOnSpawnObject.Invoke(region, this, obj);
                            }
                            if (mSpawnEffect != null)
                            {
                                zone.PostEvent(zone.ObjectPool.Alloc<AddEffectEvent>().Init(obj.ID, region.Position, region.Direction, mSpawnEffect));
                            }
                            if (mSpawnObjectEffect != null)
                            {
                                zone.PostObjectEvent(obj, zone.ObjectPool.Alloc<UnitEffectEvent>().Init(obj.ID, mSpawnObjectEffect));
                            }
                        }
                    }
                }
            }
            finally
            {
                if (mTeamHelper != null)
                {
                    mTeamHelper.Clear();
                }
            }
            return count;
        }

        [Desc("手动刷一次")]
        public int ManualSpawn<ST>(ST st, int? spawnCountLimit = null, SpawnAction<ST> onSpawn = null)
        {
            var count = 0;
            ISpawnContainer region = mAdded;
            InstanceZone zone = region.Zone;
            if (mTeam != null)
            {
                if (mTeamHelper == null || mTeamHelper.Data != mTeam)
                {
                    mTeamHelper = new TeamFormationGroup(mTeam, region);
                }
                for (int i = mSpawnOnceCount - 1; i >= 0; --i)
                {
                    var pos = region.GetSpawnPos(this);
                    mTeamHelper.AddPos(pos.X, pos.Y, pos.Z, StartDirection);
                }
                mTeamHelper.ResetPos(this);
            }
            else
            {
                mTeamHelper = null;
            }
            try
            {
                if (onBeginSpawn(region))
                {
                    region.BeginSpawnOnce(this);
                    for (int i = mSpawnOnceCount - 1; i >= 0; --i)
                    {
                        if (spawnCountLimit.HasValue && count >= spawnCountLimit.Value)
                        {
                            break;
                        }
                        var pos = (mTeamHelper != null) ? mTeamHelper.PopPos() : null;
                        var obj = SpawnObject(pos);
                        if (obj != null)
                        {
                            count++;
                            this.mSpawnedUnits.Put(obj, obj);
                            obj.UnitTag = mUnitTag;
                            OnObjectSpawned(obj);
                            if (mOnSpawnObject != null)
                            {
                                mOnSpawnObject.Invoke(region, this, obj);
                            }
                            if (mSpawnEffect != null)
                            {
                                zone.PostEvent(zone.ObjectPool.Alloc<AddEffectEvent>().Init(obj.ID, region.Position, region.Direction, mSpawnEffect));
                            }
                            if (mSpawnObjectEffect != null)
                            {
                                zone.PostObjectEvent(obj, zone.ObjectPool.Alloc<UnitEffectEvent>().Init(obj.ID, mSpawnObjectEffect));
                            }
                            onSpawn?.Invoke(st, this, obj);
                        }
                    }
                }
            }
            finally
            {
                if (mTeamHelper != null)
                {
                    mTeamHelper.Clear();
                }
            }
            return count;
        }
        protected virtual void removeSpawned(InstanceZone zone, InstanceZoneObject obj)
        {
            obj = mSpawnedUnits.RemoveByKey(obj);
            if (obj != null)
            {
                OnSpawnedObjectRemoved(obj);
                if (IsSpawnOver && mSpawnedUnits.Count == 0)
                {
                    mTimeTask?.Pause();
                    if (mOnLastObjectRemoved != null)
                    {
                        mOnLastObjectRemoved.Invoke(mAdded, this, obj);
                    }
                }

                if (IsSpawnOver == false && mSpawnedUnits.Count == 0 && mResetOnWithoutAlive)
                {
                    this.mTimeTask?.Reset();
                }
            }
        }

        //--------------------------------------------------------------------------------------------------

        #region Delegate

        public delegate void SpawnOnceHandler(ISpawnContainer region, AbstractSpawnAbility trigger, int count);
        public delegate void SpawnObjectHandler(ISpawnContainer region, AbstractSpawnAbility trigger, InstanceZoneObject unit);
        public delegate void SpawnOverHandler(ISpawnContainer region, AbstractSpawnAbility trigger);
        public delegate void SpawnLastObjectRemovedHandler(ISpawnContainer region, AbstractSpawnAbility trigger, InstanceZoneObject unit);

        protected SpawnOnceHandler mOnSpawnOnce;
        protected SpawnOverHandler mOnSpawnOver;
        protected SpawnObjectHandler mOnSpawnObject;
        protected SpawnLastObjectRemovedHandler mOnLastObjectRemoved;

        public event SpawnOnceHandler OnSpawnOnce
        {
            add { mOnSpawnOnce += value; }
            remove { mOnSpawnOnce -= value; }
        }
        public event SpawnOverHandler OnSpawnOver
        {
            add { mOnSpawnOver += value; }
            remove { mOnSpawnOver -= value; }
        }
        public event SpawnObjectHandler OnSpawnUnit
        {
            add { mOnSpawnObject += value; }
            remove { mOnSpawnObject -= value; }
        }
        public event SpawnLastObjectRemovedHandler OnLastObjectRemoved
        {
            add { mOnLastObjectRemoved += value; }
            remove { mOnLastObjectRemoved -= value; }
        }

        #endregion

        //--------------------------------------------------------------------------------------------------

        #region ABSTRACT

        abstract public int SpawnTemplateCount { get; }

        /// <summary>
        /// 创建并生成一个单位
        /// </summary>
        /// <returns></returns>
        abstract protected InstanceZoneObject SpawnObject(TeamFormationGroup.TeamMember pos);

        /// <summary>
        /// 当一个单位被生成
        /// </summary>
        /// <param name="obj"></param>
        abstract protected void OnObjectSpawned(InstanceZoneObject obj);

        /// <summary>
        /// 当被生成的单位被移除场景
        /// </summary>
        /// <param name="obj"></param>
        abstract protected void OnSpawnedObjectRemoved(InstanceZoneObject obj);

        #endregion
    }

    //-------------------------------------------------------------------------------------------
    public class SpawnCollection : Disposable
    {
        public ISpawnContainer Owner { get; }
        private List<AbstractSpawnAbility> mSpawnTriggers = new List<AbstractSpawnAbility>();
        private bool mAllTriggerOver = false;
        private bool mAllTriggerNoneAlive = false;
        public event Action<ISpawnContainer, AbstractSpawnAbility, InstanceZoneObject> OnSpawnOver;
        public event Action<ISpawnContainer, AbstractSpawnAbility, InstanceZoneObject> OnObjectSpawned;

        public SpawnCollection(ISpawnContainer owner)
        {
            Owner = owner;
        }

        protected override void Disposing()
        {
            OnSpawnOver = null;
            foreach (var e in mSpawnTriggers)
            {
                e.Dispose();
            }

            mSpawnTriggers.Clear();
        }
        public void ResetSpawn()
        {
            foreach (var spawn in mSpawnTriggers)
            {
                spawn.Reset();
            }
        }
        public void ManualSpawn<ST>(ST st, int? spawnCountLimit = null, SpawnAction<ST> onSpawn = null)
        {
            foreach (var spawn in mSpawnTriggers)
            {
                spawn.ManualSpawn(st, spawnCountLimit, onSpawn);
            }
        }
        public int SpawnOnce<ST>(ST st, SpawnObjectDelegate<ST> spawnObject, int? spawnCountLimit = null)
        {
            var ret = 0;
            foreach (var spawn in mSpawnTriggers)
            {
                ret += spawn.SpawnOnce(st, spawnObject, spawnCountLimit);
            }
            return ret;
        }
        public void AddSpawn(AbstractSpawnAbility trigger)
        {
            trigger.OnLastObjectRemoved += Trigger_LastObjectRemoved;
            trigger.OnSpawnUnit += Trigger_OnSpawnUnit;
            mSpawnTriggers.Add(trigger);
            mAllTriggerOver = false;
        }
        private void Trigger_LastObjectRemoved(ISpawnContainer region, AbstractSpawnAbility trigger, InstanceZoneObject unit)
        {
            if (IsSpawnOver)
            {
                OnSpawnOver?.Invoke(region, trigger, unit);
                Owner.Zone.cb_OnFlagSpawnOver(region, trigger, unit);
            }
        }
        private void Trigger_OnSpawnUnit(ISpawnContainer region, AbstractSpawnAbility trigger, InstanceZoneObject unit)
        {
            OnObjectSpawned?.Invoke(region, trigger, unit);
            Owner.Zone.cb_OnFlagSpawnObject(region, trigger, unit);
        }


        /// <summary>
        /// 获取此区域绑定的怪物刷新点
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AbstractSpawnAbility> GetSpawnTriggers()
        {
            return mSpawnTriggers;
        }

        /// <summary>   
        /// 获取此区域绑定的怪物刷新点
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public AbstractSpawnAbility GetSpawnTrigger(string name)
        {
            for (int i = mSpawnTriggers.Count - 1; i >= 0; --i)
            {
                AbstractSpawnAbility tg = mSpawnTriggers[i];
                if (string.Equals(tg.Name, name))
                {
                    return tg;
                }
            }

            return null;
        }

        /// <summary>
        /// 获得所有由此刷新点产生的单位
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public void getSpawnedObjectsInRegion<T>(List<T> list) where T : InstanceZoneEntity
        {
            foreach (var spawn in mSpawnTriggers)
            {
                spawn.getSpawnedObjects<T>(list);
            }
        }

        /// <summary>
        /// 检测是否所有刷怪点都结束
        /// </summary>
        public bool IsSpawnOver
        {
            get
            {
                if (!mAllTriggerOver)
                {
                    for (int i = mSpawnTriggers.Count - 1; i >= 0; --i)
                    {
                        AbstractSpawnAbility tg = mSpawnTriggers[i];
                        if (!tg.IsSpawnOver)
                        {
                            return false;
                        }
                    }

                    mAllTriggerOver = true;
                }

                return true;
            }
        }

        /// <summary>
        /// 检测是否所有刷怪点都无存活
        /// </summary>
        public bool IsSpawnNoneAlive
        {
            get
            {
                if (!mAllTriggerNoneAlive)
                {
                    for (int i = mSpawnTriggers.Count - 1; i >= 0; --i)
                    {
                        AbstractSpawnAbility tg = mSpawnTriggers[i];
                        if (!tg.IsSpawnOver)
                        {
                            return false;
                        }

                        if (tg.AliveCount > 0)
                        {
                            return false;
                        }
                    }

                    mAllTriggerNoneAlive = true;
                }

                return true;
            }
        }


        public int SpawnAliveCount
        {
            get
            {
                int ret = 0;
                using (var list = Owner.Zone.ObjectPool.AllocList<InstanceUnit>())
                {
                    this.getSpawnedObjectsInRegion<InstanceUnit>(list);
                    foreach (var o in list)
                    {
                        if (!o.IsDead) ret++;
                    }
                }

                return ret;
            }
        }

        public int TotalSpawnCount
        {
            get
            {
                int ret = 0;
                foreach (var spawn in GetSpawnTriggers())
                {
                    ret += spawn.TotalSpawnCount;
                }

                return ret;
            }
        }

        public int SpawnOnceCount
        {
            get
            {
                int ret = 0;
                foreach (var spawn in GetSpawnTriggers())
                {
                    ret += spawn.OnceSpawnCount;
                }

                return ret;
            }
        }

        public int SpawnLimitedCount
        {
            get
            {
                int ret = 0;
                foreach (var spawn in GetSpawnTriggers())
                {
                    ret += spawn.LimitedSpawnCount;
                }

                return ret;
            }
        }

        public int SpawnAliveLimitedCount
        {
            get
            {
                int ret = 0;
                foreach (var spawn in GetSpawnTriggers())
                {
                    ret += spawn.LimitedAliveCount;
                }

                return ret;
            }
        }

        public T ForEachSpawnedObjectsInRegion<T>(BreakPredicate<T> it)
        {
            using (var list = Owner.Zone.ObjectPool.AllocList<InstanceZoneEntity>())
            {
                this.getSpawnedObjectsInRegion(list);
                foreach (var o in list)
                {
                    if (o is T t && it(t)) return t;
                }
            }

            return default(T);
        }
    }


    //-------------------------------------------------------------------------------------------


    /// <summary>
    /// 生成单位的触发器
    /// </summary>
    public class SpawnUnitAbility : AbstractSpawnAbility
    {
        public struct SpawnUnitInfo
        {
            public UnitInfo Unit;
            public int Level;
            public float Percent = 100f;
            public UnitType? OverrideType;
            public object arg;
            public SpawnUnitInfo(UnitInfo unit, int level, float percent = 100f, UnitType? overrideType = null)
            {
                this.Unit = unit;
                this.OverrideType = overrideType;
                this.Level = level;
                this.Percent = percent;
            }
        }
        public class SpawnGroupInfo
        {
            private int SpawnIndexer = 0;
            private List<SpawnUnitInfo> mSpawnUnitTemplates = new List<SpawnUnitInfo>(1);
            public int Count => mSpawnUnitTemplates.Count;
            public void Add(SpawnUnitInfo spawn)
            {
                mSpawnUnitTemplates.Add(spawn);
            }
            public void RandomList(Random rand)
            {
                mSpawnUnitTemplates.TrimExcess();
                rand.RandomList(mSpawnUnitTemplates);
            }
            public bool Next(out SpawnUnitInfo spawn)
            {
                if (mSpawnUnitTemplates.Count > 0)
                {
                    spawn = mSpawnUnitTemplates[SpawnIndexer % mSpawnUnitTemplates.Count];
                    SpawnIndexer++;
                    return true;
                }
                else
                {
                    spawn = default;
                    return false;
                }
            }
        }

        private DropList<SpawnGroupInfo> mSpawnGroups = new DropList<SpawnGroupInfo>();
        protected HashMap<uint, InstanceUnit> mAlivedUnits = new HashMap<uint, InstanceUnit>();
        private InstanceFlag mStartPath = null;
        protected byte mUnitForce;
        protected string mUnitName;
        private SpawnGroupInfo currentGroup;
        protected SpawnUnitAbilityData metaData;
        public int GroupCount => mSpawnGroups.Count;
        public override int SpawnTemplateCount { get { return mSpawnGroups.Count; } }
        public override int AliveCount
        {
            get
            {
                if (mAlivedUnits.Count > 0)
                {
                    int count = 0;
                    foreach (var u in mAlivedUnits.Values)
                    {
                        if (!u.IsDead)
                        {
                            count++;
                        }
                    }
                    return count;
                }
                return 0;
            }
        }

        public SpawnUnitAbility(InstanceZone zone, SpawnUnitAbilityData data)
            : base(zone, data)
        {
            this.metaData = data;
            this.setDelayTime(data.StartTimeDelayMS);
            this.setSpawnInterval(data.IntervalMS);
            this.setSpawnCount(data.OnceCount);
            this.setSpawnEffect(data.SpawnEffect, data.SpawnObjectEffect);
            this.setLimitedAliveCount(data.AliveLimit);
            this.setLimitedSpawnCount(data.TotalLimit);
            this.setSpawnWithoutAlive(data.WithoutAlive);
            this.setUnitTag(data.UnitTag);
            this.setUnitName(data.UnitName);
            this.setUnitForce(data.Force);
            if (data.UnitTemplates != null)
            {
                foreach (var spawn in data.UnitTemplates)
                {
                    this.addUnitInfo(spawn.UnitTemplateID, spawn.UnitLevel, spawn.Percent, spawn.OverrideType);
                }
            }
            if (data.UnitGroup?.UnitGroupPath != null)
            {
                using (var array = zone.ObjectPool.AllocList<UnitInfo>())
                {
                    zone.Templates.GetAllUnitsByPath(data.UnitGroup.UnitGroupPath, array);
                    foreach (var unit in array)
                    {
                        this.addUnitInfo(unit.ID, data.UnitGroup.UnitLevel, 100f, data.OverrideType);
                    }
                }
            }
            this.setTeamFormation(data.TFormation);
            this.setStartPath(zone, data.StartPointName /*, data.StartPathHoldMinTimeMS, data.StartPathHoldMaxTimeMS*/);
            this.setStartDirection(data.StartDirection);
            this.setResetOnWithoutAlive(data.ResetOnWithoutAlive);
            if (data.RandomSpawn)
            {
                randomTemplates();
            }
        }


        public SpawnGroupInfo addGroup(int weight)
        {
            var g = new SpawnGroupInfo();
            this.mSpawnGroups.Add(g, weight);
            return g;
        }
        public bool tryGetGroup(int index, out SpawnGroupInfo group)
        {
            if (index >= 0 && index < mSpawnGroups.Count)
            {
                group = mSpawnGroups[index];
                return true;
            }
            group = null;
            return false;
        }
        //----------------------------------------------------------------------------------------------------------------------
        public virtual void clearTemplates()
        {
            //tempCaches.Clear();
            mSpawnGroups.Clear();
        }
        public virtual void randomTemplates()
        {
            for (int i = 0; i < mSpawnGroups.Count; i++)
            {
                var g = mSpawnGroups[i];
                g.RandomList(Zone.RandomN);
            }
            mSpawnGroups.TrimExcess();
        }
        //----------------------------------------------------------------------------------------------------------------------
        public virtual void addUnitInfo(SpawnGroupInfo group, SpawnUnitInfo spawn)
        {
            group.Add(spawn);
            //tempCaches.Add(spawn);
        }
        public void addUnitInfo(SpawnGroupInfo group, UnitInfo info, int level, float percent, UnitType? overrideType = null)
        {
            group.Add(new SpawnUnitInfo(info, level, percent, overrideType));
        }
        public void addUnitInfo(SpawnGroupInfo group, int templateID, int level = 0, float percent = 100f, UnitType? overrideType = null)
        {
            UnitInfo info = Zone.Templates.GetUnit(templateID);
            if (info != null)
            {
                this.addUnitInfo(group, info, level, percent, overrideType);
            }
        }
        public void addUnits(SpawnGroupInfo group, int[] unitsTemplateID, int level, UnitType? overrideType = null)
        {
            if (unitsTemplateID != null)
            {
                foreach (int id in unitsTemplateID)
                {
                    UnitInfo info = Zone.Templates.GetUnit(id);
                    if (info != null)
                    {
                        this.addUnitInfo(group, info, level, 100f, overrideType);
                    }
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------------------

        public void addUnitInfo(SpawnUnitInfo spawn)
        {
            SpawnGroupInfo group;
            if (mSpawnGroups.Count == 0)
            {
                group = addGroup(100);
            }
            else
            {
                group = mSpawnGroups[0];
            }
            addUnitInfo(group, spawn);
        }
        public void addUnitInfo(UnitInfo info, int level, float percent, UnitType? overrideType = null)
        {
            addUnitInfo(new SpawnUnitInfo(info, level, percent, overrideType));
        }
        public void addUnitInfo(int templateID, int level = 0, float percent = 100f, UnitType? overrideType = null)
        {
            UnitInfo info = Zone.Templates.GetUnit(templateID);
            if (info != null)
            {
                this.addUnitInfo(info, level, percent, overrideType);
            }
        }
        public void addUnits(int[] unitsTemplateID, int level, UnitType? overrideType = null)
        {
            if (unitsTemplateID != null)
            {
                foreach (int id in unitsTemplateID)
                {
                    UnitInfo info = Zone.Templates.GetUnit(id);
                    if (info != null)
                    {
                        this.addUnitInfo(info, level, 100f, overrideType);
                    }
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 设置产生单位的Force
        /// </summary>
        /// <param name="force"></param>
        public void setUnitForce(byte force)
        {
            this.mUnitForce = force;
        }

        /// <summary>
        /// 设置出生单位名字
        /// </summary>
        /// <param name="name"></param>
        public void setUnitName(string name)
        {
            this.mUnitName = name;
        }

        /// <summary>
        /// 设置移动路线
        /// </summary>
        public InstanceFlag setStartPath(InstanceZone zone, string flagName /*, int minHoldTimeMS, int maxHoldTimeMS*/)
        {
            this.mStartPath = null;
            if (!string.IsNullOrEmpty(flagName))
            {
                InstanceFlag flag = zone.GetFlag(flagName);
                if (flag != null)
                {
                    mStartPath = flag;
                }
            }
            return mStartPath;
        }
        protected override bool onBeginSpawn(ISpawnContainer region)
        {
            return mSpawnGroups.TryDropOnce(region.Zone.RandomN, out currentGroup);
        }
        protected override InstanceZoneObject SpawnObject(TeamFormationGroup.TeamMember pos)
        {
            InstanceZone zone = this.BindingZone;
            if (currentGroup != null)
            {
                var group = currentGroup;
                if (group.Next(out var spawn) && CUtils.RandomPercent(zone.RandomN, spawn.Percent))
                {
                    var mpos = pos != null ? pos.Position : BindingRegion.GetSpawnPos(this);
                    if (BindingRegion.Zone.Terrain3D.TryGetVoxelLayerByPos(mpos, out var layer))
                    {
                        if (metaData.OnTheGround)
                        {
                            mpos.Z = layer.Upward;
                        }
                        var direction = pos != null ? pos.Direction : this.StartDirection;
                        var overrideType = this.metaData.OverrideType;
                        if (spawn.OverrideType.HasValue)
                        {
                            overrideType = spawn.OverrideType.Value;
                        }
                        var info = spawn.Unit;
                        var level = spawn.Level;
                        var unit = zone.AddUnit(new TAddUnit()
                        {
                            info = info,
                            editor_name = mUnitName,
                            player_uuid = mUnitName,
                            force = mUnitForce,
                            level = level,
                            pos = mpos,
                            direction = direction,
                            overrideType = overrideType,
                            arg = spawn.arg,
                        });
                        if (metaData.CopyDecorationShape != null && zone.GetFlag(metaData.CopyDecorationShape) is ZoneDecoration flag)
                        {
                            unit.FaceTo(flag.Direction);
                            unit.Transport(flag.Position, false);
                            unit.ZoneShape = flag.ZoneShape;
                        }
                        return unit;
                    }
                }
            }
            return null;
        }

        protected override void OnObjectSpawned(InstanceZoneObject obj)
        {
            if (obj is InstanceUnit unit)
            {
                mAlivedUnits.Put(unit.ID, unit);
                unit.OnDead += unit_OnDead;
                if (mStartPath != null && unit.Moveable)
                {
                    unit.OnFirstActivated += unit_OnActivated;
                }
            }
        }

        protected override void OnSpawnedObjectRemoved(InstanceZoneObject obj)
        {
            if (obj is InstanceUnit unit)
            {
                mAlivedUnits.RemoveByKey(unit.ID);
                unit.OnDead -= unit_OnDead;
                unit.OnFirstActivated -= unit_OnActivated;
            }
        }

        private void unit_OnDead(InstanceUnit unit, InstanceUnit attacker)
        {
            mAlivedUnits.RemoveByKey(unit.ID);
        }

        private void unit_OnActivated(InstanceUnit unit)
        {
            unit.OnFirstActivated -= unit_OnActivated;
            UnitStartAttackTo(unit, mStartPath);
        }

        protected virtual void UnitStartAttackTo(InstanceUnit unit, InstanceFlag path)
        {
            if (unit.Moveable && path != null)
            {
                unit.StartAttackTo(path);
            }
        }
    }

    //-------------------------------------------------------------------------------------------

    /// <summary>
    /// 生成物品的触发器
    /// </summary>
    public class SpawnItemAbility : AbstractSpawnAbility
    {
        public struct SpawnItemInfo
        {
            public ItemTemplate Item;
            public float Percent;

            public SpawnItemInfo(ItemTemplate item, float percent)
            {
                this.Item = item;
                this.Percent = percent;
            }
        }

        private SpawnItemAbilityData metaData;
        private List<SpawnItemInfo> mSpawnUnitTemplates = new List<SpawnItemInfo>();
        private byte mUnitForce;
        private string mUnitName;
        private int mSpawnIndexer = 0;
        public int SpawnIndexer
        {
            get { return mSpawnIndexer; }
        }
        public override int SpawnTemplateCount
        {
            get { return mSpawnUnitTemplates.Count; }
        }

        public SpawnItemAbility(InstanceZone zone, SpawnItemAbilityData data)
            : base(zone, data)
        {
            this.metaData = data;
            this.setDelayTime(data.StartTimeDelayMS);
            this.setSpawnInterval(data.IntervalMS);
            this.setSpawnCount(data.OnceCount);
            this.setSpawnEffect(data.SpawnEffect, data.SpawnObjectEffect);
            this.setLimitedAliveCount(data.AliveLimit);
            this.setLimitedSpawnCount(data.TotalLimit);
            this.setSpawnWithoutAlive(data.WithoutAlive);
            this.setUnitTag(data.UnitTag);
            this.setUnitName(data.UnitName);
            this.setUnitForce(data.Force);
            this.setStartDirection(data.StartDirection);
            if (data.ItemTemplates != null)
            {
                foreach (var spawn in data.ItemTemplates)
                {
                    this.addItemInfo(spawn.ItemTemplateID, spawn.Percent);
                }
            }
            this.setTeamFormation(data.TFormation);
            this.setResetOnWithoutAlive(data.ResetOnWithoutAlive);
            if (data.RandomSpawn)
            {
                Zone.RandomN.RandomList(mSpawnUnitTemplates);
            }
        }

        /// <summary>
        /// 设置产生单位的Force
        /// </summary>
        /// <param name="force"></param>
        public void setUnitForce(byte force)
        {
            this.mUnitForce = force;
        }

        /// <summary>
        /// 设置出生单位名字
        /// </summary>
        /// <param name="name"></param>
        public void setUnitName(string name)
        {
            this.mUnitName = name;
        }

        /// <summary>
        /// 添加单位模板
        /// </summary>
        /// <param name="info">将要产生的单位模板</param>
        public void addItemInfo(int templateID, float percent = 100f)
        {
            ItemTemplate info = Zone.Templates.GetItem(templateID);
            if (info != null)
            {
                mSpawnUnitTemplates.Add(new SpawnItemInfo(info, percent));
            }
        }

        /// <summary>
        /// 清除所有怪物模板
        /// </summary>
        public void clearTemplates()
        {
            mSpawnUnitTemplates.Clear();
        }


        protected override InstanceZoneObject SpawnObject(TeamFormationGroup.TeamMember pos)
        {
            InstanceZone zone = this.BindingZone;
            SpawnItemInfo spawn = mSpawnUnitTemplates[SpawnIndexer % mSpawnUnitTemplates.Count];
            try
            {
                if (CUtils.RandomPercent(zone.RandomN, spawn.Percent))
                {
                    var mpos = pos != null ? pos.Position : BindingRegion.GetSpawnPos(this);
                    if (BindingRegion.Zone.Terrain3D.TryGetVoxelLayerByPos(mpos, out var layer))
                    {
                        if (metaData.OnTheGround)
                        {
                            mpos.Z = layer.Upward;
                        }

                        float direction = pos != null ? pos.Direction : base.StartDirection;
                        ItemTemplate info = spawn.Item;
                        var evt = new TAddItem();
                        {
                            evt.template = info;
                            evt.name = mUnitName;
                            evt.pos = mpos;
                            evt.direction = direction;
                            evt.force = mUnitForce;
                            evt.creater = null;
                        }
                        InstanceItem item = zone.AddItem(evt);
                        //InstanceItem item = zone.AddItem(info, mUnitName, in mpos, direction, mUnitForce, null);
                        return item;
                    }
                }
            }
            finally
            {
                this.mSpawnIndexer++;
            }
            return null;
        }

        protected override void OnObjectSpawned(InstanceZoneObject obj)
        {
        }

        protected override void OnSpawnedObjectRemoved(InstanceZoneObject obj)
        {
        }
    }

    //-------------------------------------------------------------------------------------------
}