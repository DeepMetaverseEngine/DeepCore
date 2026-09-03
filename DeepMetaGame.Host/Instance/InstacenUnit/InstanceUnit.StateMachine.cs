using DeepCore.Log;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceUnit
    {
        //-----------------------------------------------------------------------------------------------------//
        #region STATE-MACHINE-----------------------------------------------------------------------------------------------------

        #region Internal 

        private State current_state = null;
        private State next_state;
        private Queue<State> next_state_queue = new Queue<State>();

        /// <summary>
        /// 当前状态机
        /// </summary>
        public State CurrentState => current_state;
        /// <summary>
        /// 下一个状态
        /// </summary>
        public State NextState => next_state;
        public int NextStateQueueCount => next_state_queue.Count;

        /// <summary>
        /// 死亡状态可能的状态机，一般配合<see cref="IsDead"/>使用
        /// </summary>
        public virtual bool IsStateDead
        {
            get
            {
                if ((current_state is StateDead) ||
                    (current_state is StateDamage) ||
                    (current_state is StateRebirth) ||
                    (next_state is StateDead) ||
                    (next_state is StateDamage) ||
                    (next_state is StateRebirth))
                {
                    return true;
                }

                return false;
            }
        }
        public bool IsInQueue(State s)
        {
            return next_state_queue.Contains(s);
        }
        /// <summary>
        /// 在队列里插一个状态
        /// </summary>
        /// <param name="s"></param>
        public void QueueState(State s)
        {
            next_state_queue.Enqueue(s);
        }
        /// <summary>
        /// 在当前状态执行完成后执行
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public bool QueueCurrentState(State s)
        {
            if (current_state != null)
            {
                // 失败后，Alloc的单位会被Release一次
                s.Retain();
                if (ChangeState(s))
                {
                    s.Release();
                }
                else
                {
                    current_state.OnStopOnce += ((obj, st) => { obj.ChangeState(s); });
                    return true;
                }
                return true;
            }
            else
            {
                return ChangeState(s);
            }
        }
        public bool ChangeOrQueueState(State s)
        {
            // 失败后，Alloc的单位会被Release一次
            s.Retain();
            if (ChangeState(s))
            {
                s.Release();
                return true;
            }
            else
            {
                QueueState(s);
                return false;
            }
        }

        public bool ChangeState(State newState)
        {
            if (this.IsDisposing)
            {
                newState.failed();
                return false;
            }
            if (this.Enable == false)
            {
                newState.failed();
                return false;
            }
            if (newState == current_state)
            {
                return true;
            }
            if (newState == next_state)
            {
                return true;
            }
            if (newState.unit != this)
            {
                newState.failed();
                throw new Exception("State is not Owner unit : " + newState);
            }
            if (tryBlockState(newState, current_state))
            {
                if (tryBlockState(newState, next_state))
                {
                    if (next_state != null)
                    {
                        next_state.stop();
                    }
                    next_state = newState;
                    return true;
                }
            }
            newState.failed();
            return false;
        }

        protected virtual bool tryBlockState(State newState, State oldState)
        {
            if (oldState == null || oldState.IsDisposing)
            {
                return true;
            }
            if (newState is StateDead)
            {
                if (oldState is StateDead)
                {
                    return false;
                }
                if (oldState is StateRebirth)
                {
                    return false;
                }
                return true;
            }
            if (TryBlockState != null && TryBlockState.Invoke(this, newState, oldState))
            {
                return true;
            }
            if (oldState.OnBlock(newState))
            {
                return true;
            }
            return false;
        }
        private void updateState()
        {
            if (IsDead && (!IsStateDead))
            {
                mDeadTime = Parent.PassTimeMS;
                onDead(this);
                Parent.cb_unitDeadCallBack(this, this);
                PostEvent(ObjectPool.Alloc<UnitDeadEvent>().Init(ID, this.ID, false, DeadTimeMS));
                ChangeState(StateDead.Alloc(this, this, false));
            }
            else if (next_state == null && next_state_queue.Count > 0)
            {
                // 尝试从队列中取一个状态机，有机会就执行 //
                while (next_state_queue.Count > 0)
                {
                    var queued_state = next_state_queue.Peek();
                    if (queued_state.IsPooling) queued_state.Retain();
                    if (ChangeState(queued_state))
                    {
                        if (queued_state.IsPooling) queued_state.Release();
                        next_state_queue.Dequeue();
                    }
                    else
                    {
                        // 失败后，Alloc的单位会被Release一次
                        break;
                    }
                }
            }

            if (next_state != null && next_state != current_state)
            {
                var old_state = current_state;
                if (old_state != null)
                {
                    old_state.stop();
                    // 停止后，Alloc的单位会被自动Release
                    // New 的单位则不会，可以反复使用
                }
                this.current_state = next_state;
                this.next_state = null;
                this.current_state.start();
                this.onStateChanged(old_state, current_state);
                this.OnStateChanged?.Invoke(this, old_state, current_state);
            }
            else if (current_state != null)
            {
                current_state.update();
            }

            UpdateTimeLines(Parent.UpdateIntervalMS);
        }

        private void cleanState()
        {
            current_state?.Dispose();
            next_state?.Dispose();
            foreach (var n in next_state_queue) { n.Dispose(); }
            next_state_queue.Clear();
        }
        #endregion

        //-----------------------------------------------------------------------------------------------------//
        /// <summary>
        /// 由AI决定接下来做什么，行为树入口
        /// </summary>
        public void DoSomething()
        {
            if (event_OnDoSomething.TryGetInvocationList(out var invokes))
            {
                var ret = false;
                foreach (DoSomethingHandler invoke in invokes)
                {
                    if (invoke(this, ret))
                    {
                        ret |= true;
                        //return;
                    }
                }
                if (ret && NextState != null)
                {
                    return;
                }
            }
            DoDefaultBehavior();
        }

        protected virtual void DoDefaultBehavior()
        {
            if (NextState == null)
                StartIdle();
        }

        //-----------------------------------------------------------------------------------------------------//
        /// <summary>
        /// 单位待机
        /// </summary>
        public virtual bool StartIdle()
        {
            return ChangeState(AllocState<StateIdle>());
        }

        public virtual bool StartSpawn(float timeMS)
        {
            return ChangeState(StateSpawn.Alloc(this, timeMS));
        }

        /// <summary>
        /// 直接复活
        /// </summary>
        public virtual bool StartRebirth(int max_hp = 0, int max_mp = 0, float? resettime = null)
        {
            if (IsDead)
            {
                return ChangeState(StateRebirth.Alloc(this, max_hp, max_mp, resettime));
            }

            return false;
        }

        /// <summary>
        /// 单位移动
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public virtual bool StartMoveTo(DeepCore.Geometry.Vector3 pos)
        {
            return ChangeState(StateMove.Alloc(this, pos));
        }
        /// <summary>
        /// 单位移动带寻路
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public virtual bool StartMoveAI(DeepCore.Geometry.Vector3 pos)
        {
            return ChangeState(StateMoveAI.Alloc(this, pos));
        }

        /// <summary>
        /// 调整射击位置
        /// </summary>
        /// <param name="expect_skill"></param>
        /// <param name="target"></param>
        /// <param name="onEndAction"></param>
        public virtual bool StartAdjustLaunchSkill(SkillTemplate expect_skill, InstanceUnit target)
        {
            if (expect_skill.AttackKeepRange > 0 && expect_skill.AttackRange > expect_skill.AttackKeepRange)
            {
                float keep_range = GetSkillAttackRange(expect_skill.AttackKeepRange) + target.BodyBlockSize;
                float distance = MathVector.getDistance(this.X, this.Y, target.X, target.Y);
                if (distance < keep_range)
                {
                    //                     var half = (expect_skill.AttackRange - expect_skill.AttackKeepRange) / 2;
                    //                     var md = (keep_range - distance) + (float)(RandomN.NextDouble() * half);
                    //                     var target_pos = new Geometry.Vector3(this.X, this.Y, this.Z);
                    //                     var rd = (CMath.PI_DIV_2 / 4);
                    //                     var target_direction = this.Direction + CMath.PI_F - (rd / 2 + (float)(RandomN.NextDouble() * rd));
                    //                     Geometry.VectorHelper.MovePolar(ref target_pos, target_direction, md);
                    //                     if (!Parent.TryTouchMap(this, target_pos))
                    //                     {
                    //                         var back_pos = new Geometry.Vector3(this.X, this.Y, this.Z);
                    //                         Geometry.VectorHelper.MovePolar(ref back_pos, target_direction, this.BodyBlockSize);
                    //                         if (!Parent.TryTouchMap(this, back_pos))
                    {
                        //var move = StateMove.Alloc(this, target_pos); 
                        //                             move.MinStepCheckCount = 0;
                        //                             move.StopOnTouchMap = true;
                        var angle = RandomN.RandomRadians(CFG.AI_FOLLOW_AND_ATTACK_ADJUST_ESCAPE_ANGLE);
                        var move = StateMoveAway.Alloc(this, target, keep_range, angle);
                        return this.ChangeState(move);
                    }
                    //}
                }
            }

            return false;
        }

        /// <summary>
        /// 单位移动
        /// </summary>
        /// <param name="obj"></param>
        public virtual bool StartFollowTo(InstanceZoneObject obj)
        {
            return ChangeState(StateFollowObject.Alloc(this, obj));
        }

        /// <summary>
        /// 单位逃跑
        /// </summary>
        /// <param name="timeMS"></param>
        /// <param name="distance"></param>
        public virtual bool StartEscape(float timeMS, float distance = 0)
        {
            return ChangeState(StateEscape.Alloc(this, timeMS, distance));
        }
        public virtual bool StartChaos(float timeMS)
        {
            return ChangeState(StateChaos.Alloc(this, timeMS));
        }

        /// <summary>
        /// 和自身交互（搓炉石）
        /// </summary>
        /// <param name="timeMS"></param>
        /// <param name="done"></param>
        /// <param name="status"></param>
        public virtual bool StartPickProgressSelf(float timeMS, StatePickObject.OnPickDone done, object status = null)
        {
            var picking = StatePickObject.Alloc(this, this, timeMS, status, done);
            return ChangeState(picking);
        }

        /// <summary>
        /// 和目标交互
        /// </summary>
        /// <param name="item"></param>
        /// <param name="timeMS"></param>
        /// <param name="done"></param>
        /// <param name="status"></param>
        /// <returns></returns>  
        public virtual bool StartPickProgressObject(InstanceZoneObject item, float timeMS, StatePickObject.OnPickDone done, object status = null)
        {
            var picking = StatePickObject.Alloc(this, item, timeMS, status, done);
            return ChangeState(picking);
        }
        public bool StartPickProgress<ST, T>(T item, float timeMS, StatePickObject.OnPickDone<ST, T> done, ST status = default) where T : InstanceZoneObject
        {
            return StartPickProgressObject(item, timeMS, (a, cancel, b, c) => done(a, cancel, (T)b, (ST)c), status);
        }

        public virtual bool StartAttackTo(InstanceFlag path)
        {
            var StateRunningPath = StateAttackToZoneWayPoint.Alloc(this, path);
            return ChangeState(StateRunningPath);
        }
        public virtual bool StartFollowAndAttack(InstanceUnit target, AttackReason reason, SkillTemplate.CastTarget castTarget = SkillTemplate.CastTarget.Enemy, EquipSkill equipSkill = null)
        {
            if (this.AGuard)
            {
                if (target != null && target.IsActive)
                {
                    var state = StateFollowAndAttack.FollowAndAttack(this, target, castTarget, reason, equipSkill);
                    if (this.ChangeState(state))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public virtual bool StartFollowAndPickObject(InstanceUnit target, float pickTimeMS, StatePickObject.OnPickDone pickDone, object status = null)
        {
            var state = StateFollowAndPickObject.Alloc(this, target, pickTimeMS, pickDone, pickDone);
            if (this.ChangeState(state))
            {
                return true;
            }
            return false;
        }
        public virtual bool StartFollowAndPickItem(InstanceItem target)
        {
            if (target != null)
            {
                var state = StateFollowAndPickItem.Alloc(this, target);
                if (this.ChangeState(state))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool StartGuardUnit(InstanceUnit vip)
        {
            return ChangeState(StateFollowAndGuard.Alloc(this, vip, this.BodyBlockSize * 2 + vip.BodyBlockSize, AGuard.GuardRange));
        }
        public virtual bool StartGuardInPosition(Geometry.Vector3? pos)
        {
            if (pos.HasValue)
            {
                return ChangeState(StateGuardInPosition.Alloc(this, pos.Value));
            }
            return false;
        }
        /// <summary>
        /// 立刻开始返回原点
        /// </summary>
        public virtual bool StartBackToOrgin(Geometry.Vector3? mOrginPosition)
        {
            if (mOrginPosition.HasValue)
            {
                return ChangeState(StateBackToPosition.Alloc(this, mOrginPosition.Value));
            }
            return false;
        }

        /// <summary>
        /// 在一定范围内浪
        /// </summary>
        /// <param name="timeMS">浪多久</param>
        /// <param name="range">浪多远</param>
        public virtual bool StartIdleMove(Geometry.Vector3 pos, float timeMS, float range)
        {
            return ChangeState(StateIdleMove.Alloc(this, pos, timeMS, range));
        }

        /// <summary>
        /// 防止单位攻击叠在一起
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual bool StartMoveScatterTarget(InstanceUnit target)
        {
            //只有单位为非碰撞时，才有这个需求//
            if (StateMove.TryMoveScatterTarget(this, target, out var state))
            {
                return this.ChangeState(state);
            }
            return false;
        }


        //-----------------------------------------------------------------------------------------------------//
        //private UnitForceSyncPosEvent mForceSync = new UnitForceSyncPosEvent();

        private UnitActionStatus mCurrentActionMainState = UnitActionStatus.Idle;
        private UnitActionStatus mPrevActionStatus = UnitActionStatus.NA;
        private string mCurrentActionSubState = null;
        private string mPrevActionSubstate = null;
        private float mPrevLayerUpward = 0;

        /// <summary>
        /// 当前动做主状态
        /// </summary>
        public UnitActionStatus CurrentActionStatus
        {
            get { return mCurrentActionMainState; }
        }
        public int CurrentActionStatusInt32
        {
            get { return (int)mCurrentActionMainState; }
        }
        /// <summary>
        /// 当前动做子状态
        /// </summary>
        public string CurrentActionSubstate
        {
            get { return mCurrentActionSubState; }
        }

        #endregion STATE-MACHINE-----------------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------------//
        #region SEND_EVENT

        public void SetActionStatus(UnitActionStatus st)
        {
            if (st.IsMoving() && !this.Moveable)
            {
                this.mCurrentActionMainState = UnitActionStatus.Idle;
            }
            else
            {
                this.mCurrentActionMainState = st;
            }
        }
        public void SetActionSubState(string substate, bool force)
        {
            if (force || substate != null)
            {
                this.mCurrentActionSubState = substate;
            }
        }
        public void SetActionStatus(UnitActionStatus st, string substate, bool force = false)
        {
            if (st.IsMoving() && !this.Moveable)
            {
                this.mCurrentActionMainState = UnitActionStatus.Idle;
            }
            else
            {
                this.mCurrentActionMainState = st;
            }
            if (force || substate != null)
            {
                this.mCurrentActionSubState = substate;
            }
        }
        public void PostEvent(ObjectNotify evt)
        {
            Parent.PostObjectEvent(this, evt);
        }
        public void PostEvent<ST, T>(ST st, Action<ST, T> init, T defaultT = default) where T : ObjectNotify, new()
        {
            Parent.PostObjectEvent(this, st, init, defaultT);
        }

        //         public void PostEvent(ZoneEvent evt)
        //         {
        //             if (evt is AddItemEvent)
        //             {
        //                 var e = evt as AddItemEvent;
        //                 e.creater_id = this.ID;
        //             }
        // 
        //             Parent.PostEvent(evt);
        //         }

        public override void SendForceSync()
        {
            this.PostEvent(this, static (st, mForceSync) =>
            {
                mForceSync.object_id = st.ID;
                mForceSync.Position.X = st.X;
                mForceSync.Position.Y = st.Y;
                mForceSync.Position.Z = st.Z;
                mForceSync.Direction = st.Direction;
                mForceSync.BodyDirection = st.BodyDirection;
                mForceSync.UnitMainState = (byte)st.mCurrentActionMainState;
                mForceSync.UnitSubState = st.mCurrentActionSubState;
                mForceSync.LayerUpward = st.CurrentLayer?.Upward ?? 0;
            }, default(UnitForceSyncPosEvent));
        }
        public void SendForceSyncState()
        {
            this.PostEvent(this, static (st, mForceSync) =>
            {
                mForceSync.object_id = st.ID;
                mForceSync.UnitMainState = (byte)st.mCurrentActionMainState;
                mForceSync.UnitSubState = st.mCurrentActionSubState;
            }, default(UnitForceSyncStateEvent));
        }

        //-------------------------------------------------------------------------------------
        #endregion
        //-----------------------------------------------------------------------------------------------------//

        public T AllocState<T, ST>(ST st, OnCreateInPool<T, ST> create) where T : State
        {
            var state = ObjectPool.AllocOrCreateAutoRelease<T, ST>(st, create, this);
            return state;
        }
        public T AllocState<T>(OnCreateInPool<T> create) where T : State
        {
            var state = ObjectPool.AllocOrCreateAutoRelease<T>(create, this);
            return state;
        }
        public T AllocState<T>() where T : State, new()
        {
            var state = ObjectPool.AllocAutoRelease<T>(this);
            return state;
        }

        public abstract class State : IAutoRecycle
        {
            protected static Logger log = new LazyLogger(typeof(State));
            private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(State));
            public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
            public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
            public static int ActiveObjectCount { get { return Alloc.ActiveCount; } }
            public static int AllocObjectCount { get { return Alloc.AllocCount; } }
            //---------------------------------------------------------------------------------------
            private int retainCount = 0;
            private ObjectPool pool;
            private bool m_disposed = false;
            private bool m_disposing = false;
            private bool m_started = false;
            private InstanceUnit m_unit;
            public bool IsPooling { get { return pool != null; } }
            //---------------------------------------------------------------------------------------
            /// <summary>
            /// Alloc 出来的可回收
            /// </summary>
            protected State()
            {
                Alloc.RecordConstructor(GetType());
            }
            /// <summary>
            /// New 出来的不可回收
            /// </summary>
            protected State(InstanceUnit unit)
            {
                Alloc.RecordConstructor(GetType());
                m_unit = unit;
            }
            ~State()
            {
                if (!m_disposed)
                {
                    Alloc.RecordDispose(GetType());
                }
                Alloc.RecordDestructor(GetType());
            }
            void IPoolingObject.OnAlloc(ObjectPool pool, bool create, params object[] args)
            {
                if (this.m_unit != null)
                {
                    throw new Exception("池对象已经在使用！");
                }
                this.pool = pool;
                this.m_disposing = false;
                this.m_disposed = false;
                this.retainCount = 0;
                if (!create)
                {
                    Alloc.RecordReuse(GetType());
                }
                this.m_unit = args[0] as InstanceUnit;
                this.m_started = false;
                this.OnStarted = null;
                this.OnStopped = null;
                this.OnStopOnce = null;
            }
            void IPoolingObject.OnDestory(ObjectPool pool)
            {
                DoDispose();
            }
            void IAutoRecycle.OnRecycle()
            {
                DoDispose();
                if (this.pool != null)
                {
                    //回池
                    this.pool.ReleaseObject(this);
                }
            }
            private void DoDispose()
            {
                if (!m_disposed)
                {
                    m_disposed = true;
                    Alloc.RecordDispose(this.GetType());
                    OnStopOnce = null;
                    OnStarted = null;
                    OnStopped = null;
                    m_started = false;
                    m_unit = null;
                    Disposing();
                }
            }
            public void Retain(int count = 1)
            {
                if (count < 1) throw new ArgumentException("Retain count must be great than 0");
                retainCount += count;
            }
            public bool Release()
            {
                if (CanDispose)
                {
                    this.Dispose();
                    return true;
                }
                else
                {
                    retainCount--;
                    return false;
                }
            }
            public void Dispose()
            {
                if (CanDispose)
                {
                    if (this.m_disposing == false)
                    {
                        this.m_disposing = true;
                        this.OnPostDisposing();
                        if (IsPooling && this.pool.Collection is BattleObjectPool c1)
                        {
                            c1.PostRecycle(this);
                        }
                        else
                        {
                            // New 出来的对象交给GC回收，Alloc出来的对象交给池回收
                            // DoDispose();
                        }
                    }
                }
                else
                {
                    retainCount--;
                }
            }
            //---------------------------------------------------------------------------------------
            public InstanceUnit unit { get => m_unit; }
            public SingleThreadCollectionPool ObjectPool => unit.ObjectPool;
            public InstanceZone zone { get { return unit.Parent; } }
            public bool IsStarted { get { return m_started; } }
            public bool IsDisposing => m_disposing;
            internal void start()
            {
                if (!m_started)
                {
                    m_started = true;
                    OnStart();
                    OnStarted?.Invoke(unit, this);
                    unit.cb_StateStart(this);
                }
            }
            internal void failed()
            {
                if (IsPooling)
                {
                    //失败回池
                    Dispose();
                }
            }
            internal void stop()
            {
                try
                {
                    if (m_started)
                    {
                        m_started = false;
                        OnStop();
                        if (this.OnStopOnce != null)
                        {
                            var invoke = OnStopOnce;
                            this.OnStopOnce = null;
                            invoke.Invoke(unit, this);
                        }
                        OnStopped?.Invoke(unit, this);
                        unit.cb_StateStop(this);
                    }
                }
                finally
                {
                    if (IsPooling)
                    {
                        //停止后回池
                        Dispose();
                    }
                }
            }
            internal void update()
            {
                OnUpdate();
            }
            protected virtual void OnPostDisposing() { }
            //---------------------------------------------------------------------------------------
            public bool CanRecycle => retainCount <= 0 && m_disposing;
            public bool CanDispose => retainCount <= 0;
            protected abstract void Disposing();
            /// 当前状态是否可以被新状态打断
            public abstract bool OnBlock(State new_state);
            protected abstract void OnStart();
            protected abstract void OnUpdate();
            protected abstract void OnStop();
            public virtual void ContinueWith(State newState) { }

            //---------------------------------------------------------------------------------------
            [Desc("状态机结束时触发，停止后自动清理所有监听")]
            public event StateStopHandler OnStopOnce;
            public event StateStopHandler OnStopped;
            public event StateStartHandler OnStarted;
            //---------------------------------------------------------------------------------------
        }

        public abstract class UnitState<UT> : State where UT : InstanceUnit
        {
            new public UT unit => base.unit as UT;
        }
        //-----------------------------------------------------------------------------------------------------//

        public class StateSpawn : State
        {
            private float timeMS;
            private TimeExpire timer;
            public static StateSpawn Alloc(InstanceUnit unit, float timeMS)
            {
                return unit.AllocState<StateSpawn>().Init(timeMS);
            }
            protected StateSpawn Init(float timeMS)
            {
                this.timeMS = timeMS;
                this.timer = unit.ObjectPool.AllocAutoRelease<TimeExpire>().Init(timeMS);
                return this;
            }
            protected override void Disposing()
            {
                this.timeMS = 0;
                this.timer.Dispose();
                this.timer = null;
            }


            override public bool OnBlock(State new_state)
            {
                return timer.IsEnd;
            }
            override protected void OnStart()
            {
                unit.SetInvincibleTimeMS(timeMS);
                unit.SetActionStatus(UnitActionStatus.Spawn);
                if (unit.AResource?.SpawnEffect != null)
                {
                    zone.PostEvent(zone.ObjectPool.Alloc<AddEffectEvent>().Init(unit.ID, unit.Position, unit.Direction, unit.AResource?.SpawnEffect));
                }
            }
            override protected void OnUpdate()
            {
                if (timer.Update(zone.UpdateIntervalMS))
                {
                    unit.DoSomething();
                }
            }
            override protected void OnStop()
            {
                unit.doActivated();
            }
        }

        //-----------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 待机状态
        /// </summary>
        public class StateIdle : State
        {
            protected override void Disposing()
            {
            }
            public static StateIdle Alloc(InstanceUnit unit)
            {
                return unit.AllocState<StateIdle>();
            }
            override public bool OnBlock(State new_state)
            {
                return true;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(UnitActionStatus.Idle);
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }
            override protected void OnUpdate()
            {
                unit.SetActionStatus(UnitActionStatus.Idle);
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }
            override protected void OnStop()
            {
            }
        }
        public class StateIdleTime : State
        {
            private bool Force;
            private UnitActionStatus ActionStatus = UnitActionStatus.Idle;
            private string SubState;
            public static StateIdleTime Alloc(InstanceUnit unit, float timeSEC, bool force, UnitActionStatus main = UnitActionStatus.Idle, string sub = null)
            {
                return unit.AllocState<StateIdleTime>().Init(unit, timeSEC, force, main, sub);
            }

            protected StateIdleTime Init(InstanceUnit unit, float timeSEC, bool force, UnitActionStatus main = UnitActionStatus.Idle, string sub = null)
            {
                this.mIdleTime = unit.ObjectPool.AllocAutoRelease<TimeExpire>().Init((timeSEC * 1000));
                this.ActionStatus = main;
                this.SubState = sub;
                this.Force = force;
                return this;
            }
            protected override void Disposing()
            {
                this.Force = false;
                this.mIdleTime?.Dispose();
                this.mIdleTime = null;
                this.ActionStatus = default;
                this.SubState = default;
            }
            private TimeExpire mIdleTime;


            override public bool OnBlock(State new_state)
            {
                if (Force)
                {
                    if (new_state is IStateNoneControllable) { return true; }
                    return mIdleTime.IsEnd;
                }
                return true;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(ActionStatus, SubState);
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }
            override protected void OnUpdate()
            {
                unit.SetActionStatus(ActionStatus, SubState);
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
                if (mIdleTime.Update(zone.UpdateIntervalMS))
                {
                    unit.DoSomething();
                }
            }
            override protected void OnStop()
            {
            }
        }

        public class StateClientAction : State
        {
            private bool Force;
            private UnitActionStatus ActionStatus;
            private string Sub;
            private string ActionName;
            private TimeExpire mIdleTime;
            public static StateClientAction Alloc(InstanceUnit unit, UnitActionStatus state, string sub, string actionName, float timeSEC, bool force = false)
            {
                var s = unit.AllocState<StateClientAction>();
                s.Force = force;
                s.ActionName = actionName;
                s.ActionStatus = state;
                s.Sub = sub;
                s.mIdleTime = unit.ObjectPool.AllocTimeExpire((timeSEC * 1000));
                return s;
            }
            protected override void Disposing()
            {
                this.Force = false;
                this.ActionStatus = default;
                this.Sub = default;
                this.ActionName = default;
                this.mIdleTime?.Dispose();
                this.mIdleTime = null;
            }

            override public bool OnBlock(State new_state)
            {
                if (Force)
                {
                    if (new_state is IStateNoneControllable) { return true; }
                    return mIdleTime.IsEnd;
                }
                return true;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(ActionStatus);
                unit.SetActionSubState(null, true);
                unit.PostEvent(unit.ObjectPool.Alloc<UnitDoActionEvent>().Init(unit.ID, ActionStatus, Sub, ActionName));
            }
            override protected void OnUpdate()
            {
                if (mIdleTime.Update(zone.UpdateIntervalMS))
                {
                    unit.DoSomething();
                }
            }
            override protected void OnStop()
            {
            }
        }
        public class StateDefinedAction : State
        {
            private UnitActionStatus ActionStatus;
            private string SubState;
            public static StateDefinedAction Alloc(InstanceUnit unit, UnitActionStatus state, string substate)
            {
                var s = unit.AllocState<StateDefinedAction>();
                s.SubState = substate;
                s.ActionStatus = state;
                return s;
            }
            protected override void Disposing()
            {
                this.ActionStatus = default;
                this.SubState = default;
            }

            override public bool OnBlock(State new_state)
            {
                return true;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(ActionStatus);
                unit.SetActionSubState(SubState, true);
                unit.PostEvent(unit.ObjectPool.Alloc<UnitDoActionEvent>().Init(unit.ID, ActionStatus, SubState, null));
            }
            override protected void OnUpdate()
            {

            }
            override protected void OnStop()
            {
            }
        }

        /*
        public class StateJump : State
        {
            private readonly float direction;
            private readonly float moveSpeed;
            private readonly float speedz;
            private readonly float gravity;
            private FallingDown falldown;

            public StateJump(InstanceUnit unit, float direction, float moveSpeed, float speedz, float gravity)
                : base(unit)
            {
                this.direction = direction;
                this.moveSpeed = moveSpeed;
                this.speedz = speedz;
                this.gravity = gravity;
            }
            override public bool onBlock(State new_state)
            {
                if (new_state is IStateControllable) { return false; }
                if (new_state is IStateNoneControllable) { return true; }
                if (falldown == null || falldown.IsEnd) return true;
                return false;
            }
            override protected void onStart()
            {
                unit.SetActionStatus(UnitActionStatus.Jump);
                this.falldown = unit.StartJump(speedz, gravity);
            }
            override protected void onUpdate()
            {
                //unit.SetActionStatus(UnitActionStatus.Jump);
                if (moveSpeed != 0)
                {
                    unit.MoveAirTo(direction, moveSpeed, zone.UpdateIntervalMS);
                }
                if (falldown.IsEnd)
                {
                    unit.DoSomething();
                }
            }
            override protected void onStop()
            {
            }
        }
        

        /// <summary>
        /// 骑乘状态
        /// </summary>
        public class StateRide : State
        {
            private bool isCancel = false;

            public StateRide(InstanceUnit unit)
                : base(unit)
            {
            }

            override public bool onBlock(State new_state)
            {
                return isCancel;
            }
            override protected void onStart()
            {
                unit.SetActionStatus(UnitActionStatus.Ride);
            }
            override protected void onUpdate()
            {
                unit.SetActionStatus(UnitActionStatus.Ride);
            }
            override protected void onStop()
            {
            }

            public void Cancel()
            {
                isCancel = true;
                unit.DoSomething();
            }

        }

        */
        //-----------------------------------------------------------------------------------------------------//

        //--------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------

        public class StatePickObject : State
        {
            private TimeExpire mTimer;
            private float mTotalTimeMS;
            private InstanceZoneObject mPickable;

            protected bool mIsDone = false;
            private string mStopReason;
            private object mStatus;

            private OnPickDone mOnDone;
            private OnPickBlock mOnBlock;
            private OnCheckPickable mCheckTargetActive;
            public bool Force { set; get; }


            public static StatePickObject Alloc(InstanceUnit unit, InstanceZoneObject pickable, float timeMS, object status, OnPickDone done, OnPickBlock block = null)
            {
                var ret = unit.AllocState<StatePickObject>();
                ret.Init(unit, pickable, timeMS, status, done, block);
                return ret;
            }
            protected virtual StatePickObject Init(InstanceUnit unit, InstanceZoneObject pickable, float timeMS, object status, OnPickDone done, OnPickBlock block = null)
            {
                this.mStatus = status;
                this.mTimer = unit.ObjectPool.AllocTimeExpire(timeMS);
                this.mPickable = pickable;
                this.mTotalTimeMS = timeMS;
                this.mOnDone = done;
                this.mOnBlock = block;
                return this;
            }
            protected override void Disposing()
            {
                this.mTimer?.Dispose();
                this.mTimer = null;

                this.mTotalTimeMS = default;
                this.mPickable = default;

                this.mIsDone = false;
                this.mStopReason = default;
                this.mStatus = default;

                this.mOnDone = default;
                this.mOnBlock = default;
                this.mCheckTargetActive = default;
                this.Force = default;

            }


            public string StopReason { get { return mStopReason; } }
            public bool IsDone => mIsDone;

            public object Status { get { return mStatus; } }
            public InstanceZoneObject Target { get { return mPickable; } }
            /// <summary>
            /// 设置目标检测状态方法
            /// </summary>
            /// <param name="check">check返回True，终止状态机。</param>
            public void SetCheckTargetAcvite(OnCheckPickable check)
            {
                this.mCheckTargetActive = check;
            }


            /// <summary>
            /// 手动停止
            /// </summary>
            /// <param name="reason"></param>
            public void Stop(string reason)
            {
                mStopReason = reason;
                mIsDone = true;
            }
            public override bool OnBlock(State new_state)
            {
                //强制模式不允许打断.
                if (Force)
                    return false;
                if (new_state is StateIdle)
                {
                    if (mIsDone)
                    {
                        mOnBlock?.Invoke(unit, mPickable);
                    }
                    return mIsDone;
                }
                mStopReason = mStopReason ?? new_state.GetType().Name;
                mOnBlock?.Invoke(unit, mPickable);
                //block by anything//
                return true;
            }


            protected override void OnStart()
            {
                unit.SetActionStatus(UnitActionStatus.Pick);
                unit.PostEvent(unit.ObjectPool.Alloc<UnitStartPickObjectEvent>().Init(unit.ID, mTotalTimeMS, mPickable.ID, $"{mStatus}"));
            }
            protected override void OnStop()
            {
                unit.PostEvent(unit.ObjectPool.Alloc<UnitStopPickObjectEvent>().Init(unit.ID, mStopReason));
                this.mOnDone?.Invoke(unit, true, mPickable, mStatus);
                this.mOnDone = null;
                mCheckTargetActive = null;
            }

            protected override void OnUpdate()
            {
                if (!mPickable.Enable)
                {
                    mIsDone = true;
                    unit.SetActionStatus(UnitActionStatus.Idle);
                    unit.DoSomething();
                    return;
                }
                if (mCheckTargetActive != null && mCheckTargetActive(unit, mPickable, ref mStopReason))
                {
                    mIsDone = true;
                    unit.SetActionStatus(UnitActionStatus.Idle);
                    unit.DoSomething();
                    return;
                }
                if (mTimer.Update(zone.UpdateIntervalMS))
                {
                    mIsDone = true;

                    if (mOnDone != null)
                    {
                        bool done = mOnDone.Invoke(unit, false, mPickable, mStatus);
                        if (!done)
                        {
                            mIsDone = false;
                            mTimer.Reset();
                            unit.PostEvent(unit.ObjectPool.Alloc<UnitStartPickObjectEvent>().Init(unit.ID, mTotalTimeMS, mPickable.ID, $"{mStatus}"));
                        }
                        else
                        {
                            mOnDone = null;
                            unit.SetActionStatus(UnitActionStatus.Idle);
                            unit.DoSomething();
                        }
                    }
                    else
                    {
                        unit.SetActionStatus(UnitActionStatus.Idle);
                        unit.DoSomething();
                    }
                }
            }

            public delegate bool OnPickDone<ST, T>(InstanceUnit unit, bool cancel, T pickable, ST state) where T : InstanceZoneObject;
            public delegate bool OnPickDone(InstanceUnit unit, bool cancel, InstanceZoneObject pickable, object state);
            public delegate void OnPickBlock(InstanceUnit unit, InstanceZoneObject pickable);
            public delegate bool OnCheckPickable(InstanceUnit unit, InstanceZoneObject pickable, ref string reason);
        }

        /*
        public class StateJump : State
        {
            private readonly float direction;
            private readonly float moveSpeed;
            private readonly float speedz;
            private readonly float gravity;
            private FallingDown falldown;

            public StateJump(InstanceUnit unit, float direction, float moveSpeed, float speedz, float gravity)
                : base(unit)
            {
                this.direction = direction;
                this.moveSpeed = moveSpeed;
                this.speedz = speedz;
                this.gravity = gravity;
            }
            override public bool onBlock(State new_state)
            {
                if (new_state is IStateControllable) { return false; }
                if (new_state is IStateNoneControllable) { return true; }
                if (falldown == null || falldown.IsEnd) return true;
                return false;
            }
            override protected void onStart()
            {
                unit.SetActionStatus(UnitActionStatus.Jump);
                this.falldown = unit.StartJump(speedz, gravity);
            }
            override protected void onUpdate()
            {
                //unit.SetActionStatus(UnitActionStatus.Jump);
                if (moveSpeed != 0)
                {
                    unit.MoveAirTo(direction, moveSpeed, zone.UpdateIntervalMS);
                }
                if (falldown.IsEnd)
                {
                    unit.DoSomething();
                }
            }
            override protected void onStop()
            {
            }
        }
        

        /// <summary>
        /// 骑乘状态
        /// </summary>
        public class StateRide : State
        {
            private bool isCancel = false;

            public StateRide(InstanceUnit unit)
                : base(unit)
            {
            }

            override public bool onBlock(State new_state)
            {
                return isCancel;
            }
            override protected void onStart()
            {
                unit.SetActionStatus(UnitActionStatus.Ride);
            }
            override protected void onUpdate()
            {
                unit.SetActionStatus(UnitActionStatus.Ride);
            }
            override protected void onStop()
            {
            }

            public void Cancel()
            {
                isCancel = true;
                unit.DoSomething();
            }

        }

        */
        //-----------------------------------------------------------------------------------------------------//

    }
}
