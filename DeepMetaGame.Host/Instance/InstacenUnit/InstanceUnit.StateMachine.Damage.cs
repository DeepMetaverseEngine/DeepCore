using DeepCore.Game3D.Host.Helper;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceUnit
    {
        public interface IStateNoneControllable
        {
            public State AsState() => this as State;
        }
        public interface IStateControllable
        {
            public State AsState() => this as State;
            bool OnControlAction(ObjectAction ma);
            void OnReconnected();
        }

        /// <summary>
        /// 被击中受击过程
        /// </summary>
        public class StateDamage : State, IStateNoneControllable
        {
            //--------------------------------------------------------------------------------------------------------
            private TAttackSource source;
            private TAttackResult result;
            private InstanceUnit attacker;
            private float startDirection;
            private float rotateSpeedSEC;
            private bool isEnd = false;
            private TimeExpire damageExpire;
            private HitMoveSpeed hitMoveSpeed;
            private IStateNoneControllable nextState;
            private readonly HashMap<uint, InstanceUnit> body_hited = new HashMap<uint, InstanceUnit>();

            public static StateDamage Alloc(InstanceUnit unit, in TAttackSource source, in TAttackResult result, InstanceUnit attacker)
            {
                return unit.AllocState<StateDamage>().Init(unit, in source, in result, attacker);
            }
            protected virtual StateDamage Init(InstanceUnit unit, in TAttackSource source, in TAttackResult result, InstanceUnit attacker)
            {
                this.result = result;
                this.source = source;
                this.source.Retain();
                this.attacker = attacker;
                this.body_hited.Put(unit.ID, unit);

                AttackProp.HitMoveType mtype = source.Attack.HitMoveMType;
                var moveSource = HitMoveSource;
                //                 if (source.FromSpell != null)
                //                 {
                //                     this.startDirection = MoveAI.CalculateHitMoveDirection(unit, source.FromSpellUnit, mtype);
                //                 }
                //                 else if (source.FromSkill != null)
                //                 {
                //                     this.startDirection = MoveAI.CalculateHitMoveDirection(unit, attacker, mtype);
                //                 }
                if (moveSource != null)
                {
                    this.startDirection = MoveAI.CalculateHitMoveDirection(unit, moveSource, mtype);
                }
                else
                {
                    this.startDirection = unit.Direction + CMath.PI_F;
                }
                if (result.OutHitMove != null)
                {
                    this.startDirection += result.OutHitMove.Direction;
                    this.rotateSpeedSEC = result.OutHitMove.RotateSpeedSEC * ((unit.RandomN.Next() % 2) == 0 ? -1 : 1);
                }
                // 计算受击时间 //
                float damageTime = unit.Info.DamageTimeMS;
                if (result.OutKnockDownTimeMS > 0)
                {
                    damageTime = result.OutKnockDownTimeMS;
                }
                else if (unit.mInfo.DamageTimeMS > 0)
                {
                    damageTime = unit.mInfo.DamageTimeMS;
                }
                else
                {
                    damageTime = zone.CFG.OBJECT_DAMAGE_TIME_MS;
                }
                this.damageExpire = unit.AllocTimeExpire(damageTime);

                return this;
            }
            protected override void Disposing()
            {
                this.hitMoveSpeed?.Release();
                this.source.Release();
                this.source = default;
                this.result = default;
                this.attacker = default;
                this.startDirection = default;
                this.rotateSpeedSEC = default;
                this.isEnd = false;
                this.damageExpire?.Dispose();
                this.damageExpire = default;
                this.hitMoveSpeed = default;
                this.nextState = default;
                this.body_hited.Clear();
            }


            //--------------------------------------------------------------------------------------------------------
            public bool IsFallingDown
            {
                get { return (hitMoveSpeed != null && hitMoveSpeed.IsFly && unit.Z > 0); }
            }
            public bool IsKnockDown
            {
                get { return result.OutHasKnockDown; }
            }
            public bool IsDamageProtect
            {
                get { return source.Attack.IsDamageProtect; }
            }
            public float DamageTimeMS
            {
                get { return (float)damageExpire.TotalTimeMS; }
            }
            public bool IsHitMove
            {
                get { return hitMoveSpeed != null; }
            }
            public InstanceZoneObject HitMoveSource
            {
                get
                {
                    if (source.FromSpell != null)
                    {
                        if (source.Attack.HitMoveBySpellLauncher)
                        {
                            return attacker;
                        }
                        else
                        {
                            return source.FromSpellUnit;
                        }
                    }
                    return attacker;
                }
            }
            public void SetNextNoneControllable(IStateNoneControllable state)
            {
                nextState = state;
            }

            override public bool OnBlock(State new_state)
            {
                if (isEnd)
                {
                    return true;
                }
                if (new_state is StateDead)
                {
                    return true;
                }
                if (new_state is StateDamage)
                {
                    return onBlockNewDamage(new_state as StateDamage);
                }
                if (new_state is IStateNoneControllable)
                {
                    this.SetNextNoneControllable(new_state as IStateNoneControllable);
                }
                if (unit.IsDead)
                {
                    return false;
                }
                if (new_state is StateSkill)
                {
                    StateSkill ss = new_state as StateSkill;
                    // 反击或者状态解除//
                    if (ss.SkillData.IsCounter)
                    {
                        return true;
                    }
                }
                return isEnd;
            }
            private bool onBlockNewDamage(StateDamage new_damage)
            {
                //击飞只能被击飞中断//
                if (this.IsFallingDown)
                {
                    if (!new_damage.result.OutHasFly)
                    {
                        return false;
                    }
                }
                //击倒只能被击飞或击倒中断//
                if (this.IsKnockDown)
                {
                    if (!new_damage.result.OutHasFly)
                    {
                        return false;
                    }
                    if (!new_damage.result.OutHasKnockDown)
                    {
                        return false;
                    }
                }
                //如果正在位移，需要更高优先级//
                if (hitMoveSpeed != null)
                {
                    if (new_damage.result.OutHasFly || new_damage.result.OutHasKnockDown || new_damage.result.OutHitMove != null)
                    {
                        //带位移的受击，相等优先级也可以打断当前位移//
                        if (new_damage.result.OutWeight >= this.result.OutWeight)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        //普通受击，更高优先级才能打断当前位移//
                        if (new_damage.result.OutWeight > this.result.OutWeight)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                return true;
            }
            override protected void OnStart()
            {
                if (result.OutHitMove != null)
                {
                    this.hitMoveSpeed = unit.StartHitMove(this,
                        startDirection,
                        result.OutHitMove.RotateSpeedSEC,
                        result.OutHitMove.KeepTimeMS,
                        result.OutHitMove.SpeedSEC,
                        result.OutHitMove.SpeedAdd,
                        result.OutHitMove.SpeedAcc,
                        result.OutHitMove.IsNoneTouch);

                    if (result.OutHasFly)
                    {
                        this.hitMoveSpeed.SetFly(
                            result.OutHitMove.ZSpeedSEC,
                            result.OutHitMove.OverrideGravity);
                    }
                }
                else if (result.OutHasFly)
                {
                    this.hitMoveSpeed = unit.StartHitMove(this,
                        startDirection,
                        0,
                        0, // 如果击飞，则计算按落地时间 //
                        zone.CFG.OBJECT_DAMAGE_FLY_SPEED_SEC,
                        zone.CFG.OBJECT_DAMAGE_FLY_SPEED_ADD,
                        zone.CFG.OBJECT_DAMAGE_FLY_SPEED_ACC,
                        false);
                    this.hitMoveSpeed.SetFly(
                        zone.CFG.OBJECT_DAMAGE_FLY_ZSPEED_SEC,
                        zone.Gravity);
                }
                if (hitMoveSpeed != null)
                {
                    var moveSource = HitMoveSource;
                    if (source.Attack.HitMoveMType == AttackProp.HitMoveType.ToSenderCenter)
                    {
                        this.hitMoveSpeed.SetMoveTarget(moveSource, false, 0);
                    }
                    else if (source.Attack.HitMoveMType == AttackProp.HitMoveType.ToSenderBodySize)
                    {
                        this.hitMoveSpeed.SetMoveTarget(moveSource, true, 0);
                    }
                    this.hitMoveSpeed.Retain();
                }
                unit.PostEvent(ObjectPool.Alloc<UnitDamageEvent>().Init (
                    unit.ID,
                    DamageTimeMS,
                    result.OutHasKnockDown,
                    source.Attack,
                    hitMoveSpeed?.GetEvent()));
                unit.SetActionStatus(UnitActionStatus.Damage);
            }

            override protected void OnUpdate()
            {
                if (hitMoveSpeed != null)
                {
                    if (hitMoveSpeed.IsEnd || hitMoveSpeed.IsDisposing)
                    {
                        if (source.Attack.FlyFallenDownAttack != null)
                        {
                            doFlyDownAttack();
                        }
                        if (hitMoveSpeed.IsFly && damageExpire.TotalTimeMS == 0)
                        {
                            end();
                        }
                        hitMoveSpeed.Release();
                        hitMoveSpeed = null;
                    }
                    else if (source.Attack.HitMoveBodyAttack != null)
                    {
                        doBodyAttack();
                    }
                }
                else if (damageExpire != null)
                {
                    if (damageExpire.Update(zone.UpdateIntervalMS))
                    {
                        end();
                    }
                }
                else
                {
                    end();
                }
            }

            override protected void OnStop()
            {
                if (unit.IsStun)
                {
                    unit.ChangeState(StateStun.Alloc(unit));
                }
                else if (nextState != null)
                {
                    unit.ChangeState(nextState.AsState());
                }
            }

            private void end()
            {
                if (unit.IsDead)
                {
                    isEnd = true;
                    unit.ChangeState(StateDead.Alloc(unit, attacker));
                }
                else if (unit.IsStun)
                {
                    unit.SetActionStatus(UnitActionStatus.Stun);
                }
                else
                {
                    isEnd = true;
                    unit.DoSomething();
                }
            }

            /// <summary>
            /// 自己落地摔一下
            /// </summary>
            private void doFlyDownAttack()
            {
                using (var attack = TAttackSource.AllocWithAttack(source, source.Attack.FlyFallenDownAttack))
                {
                    unit.ProcessHitAttack(attacker, attack);
                }
            }

            /// <summary>
            /// 飞行过程中打别人
            /// </summary>
            private void doBodyAttack()
            {
                using (var list = unit.Parent.ObjectPool.AllocList<InstanceUnit>())
                {
                    var stripe = Geometry.VoxelStripe.InitFromPoint(unit.Position, hitMoveSpeed.PrevPos, unit.BodyBlockSize + source.Attack.HitMoveBodyAttackSize, unit.BodyHeight);
                    zone.GetObjectsInStripe(this, Collider.Stripe_Touch_HitBody, stripe, list);
                    if (list.Count > 0)
                    {
                        CUtils.RemoveAll(list, body_hited.Values);
                        using (var attack = TAttackSource.AllocWithAttack(source, source.Attack.HitMoveBodyAttack))
                        {
                            zone.UnitAttack(attacker, attack, list, source.FromExpectTarget);
                            if (list.Count > 0)
                            {
                                for (int i = 0; i < list.Count; i++)
                                {
                                    InstanceUnit o = list[i];
                                    body_hited.Put(o.ID, o);
                                }
                            }
                        }
                    }
                }

            }
        }

        /// <summary>
        /// 死亡状态
        /// </summary>
        public class StateDead : State, IStateNoneControllable
        {
            private TimeExpire dead_time;
            private InstanceZoneObject attacker;
            private bool crushed;

            public static StateDead Alloc(InstanceUnit unit, InstanceZoneObject attacker, bool crush = false)
            {
                return unit.AllocState<StateDead>().Init(unit, attacker, crush);
            }
            protected virtual StateDead Init(InstanceUnit unit, InstanceZoneObject attacker, bool crush = false)
            {
                this.attacker = attacker;
                this.crushed = crush;
                this.dead_time = unit.AllocTimeExpire(unit.DeadTimeMS);
                return this;
            }
            protected override void Disposing()
            {
                this.attacker = default;
                this.crushed = default;
                this.dead_time?.Dispose();
                this.dead_time = null;
            }

            override public bool OnBlock(State new_state)
            {
                if (new_state is StateRebirth)
                {
                    return true;
                }
                if (new_state is StateDead)
                {
                    return false;
                }
                return !unit.IsDead;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(UnitActionStatus.Dead);
                if (unit.ASkill?.DeadLaunchSpell != null)
                {
                    zone.UnitLaunchSpell(unit, unit, unit.ASkill?.DeadLaunchSpell, this, unit.Position);
                }
            }

            override protected void OnUpdate()
            {
                if (dead_time.Update(zone.UpdateIntervalMS))
                {
                    if (unit.RebirthTimeMS <= 0)
                    {
                        zone.RemoveObject(unit);
                    }
                    else
                    {
                        unit.StartRebirth();
                    }
                }
            }

            override protected void OnStop()
            {
                //unit.SetActionStatus(UnitActionStatus.Idle);
            }
        }

        /// <summary>
        /// 复活
        /// </summary>
        public class StateRebirth : State, IStateNoneControllable
        {
            protected TimeExpire timer;
            protected int max_hp = 0;
            protected int max_mp = 0;

            public static StateRebirth Alloc(InstanceUnit unit, int max_hp, int max_mp)
            {
                var ret = unit.AllocState<StateRebirth>();
                ret.timer = unit.AllocTimeExpire(unit.RebirthTimeMS);
                ret.max_hp = max_hp;
                ret.max_mp = max_mp;
                return ret;
            }
            public static StateRebirth Alloc(InstanceUnit unit, int max_hp, int max_mp, float? resettime = null)
            {
                var ret = Alloc(unit, max_hp, max_mp);
                if (resettime.HasValue && resettime.Value > 0)
                {
                    ret.timer.Reset(resettime.Value);
                }
                return ret;
            }
            protected override void Disposing()
            {
                this.timer.Dispose();
                this.timer = null;
                this.max_hp = 0;
                this.max_mp = 0;
            }

            override public bool OnBlock(State new_state)
            {
                if (new_state is StateDead)
                {
                    return false;
                }
                if (unit.IsDead == false)
                {
                    return true;
                }
                return timer.IsEnd;
            }
            override protected void OnStart()
            {
                unit.SetInvincibleTimeMS((float)timer.ExpireTimeMS);
                unit.SetActionStatus(UnitActionStatus.Rebirth);
            }
            override protected void OnUpdate()
            {
                if (timer.Update(zone.UpdateIntervalMS))
                {
                    unit.doRebirth(max_hp, max_mp);
                    if (unit.SpawnTimeMS > 0)
                    {
                        //unit.SetInvincibleTimeMS(unit.SpawnTimeMS());
                        //unit.changeState(new StateSpawn(unit, unit.mInfo.SpawnTimeMS));
                        unit.StartSpawn(unit.SpawnTimeMS);
                    }
                    else
                    {
                        unit.DoSomething();
                    }
                }
            }
            override protected void OnStop()
            {
            }
        }

        //---------------------------------------------------------------------------------------

        /// <summary>
        /// 眩晕状态
        /// </summary>
        public class StateStun : State, IStateNoneControllable
        {
            public static StateStun Alloc(InstanceUnit unit)
            {
                return unit.AllocState<StateStun>();
            }

            protected override void Disposing()
            {
            }
            override public bool OnBlock(State new_state)
            {
                if (new_state is StateDamage)
                {
                    return true;
                }
                if (unit.IsDead && new_state is StateDead)
                {
                    return true;
                }
                if (new_state is StateSkill)
                {
                    StateSkill ss = new_state as StateSkill;
                    // 反击或者状态解除//
                    if (ss.SkillData.IsCounter)
                    {
                        return true;
                    }
                }
                return !unit.IsStun;
            }

            override protected void OnStart()
            {
                unit.SetActionStatus(UnitActionStatus.Stun);
            }
            override protected void OnUpdate()
            {
                if (!unit.IsStun)
                {
                    unit.DoSomething();
                }
            }
            override protected void OnStop()
            {
                //unit.SetActionStatus(UnitActionStatus.Idle);
            }

        }

        //---------------------------------------------------------------------------------------
        /// <summary>
        /// 状态限制移动
        /// </summary>
        abstract public class StateMoveNoneControllable : State, IStateNoneControllable
        {
            protected TimeExpire mExpire;
            private MoveAI mMoveAI;
            private MoveBlockResult mLastResult;
            protected virtual StateMoveNoneControllable Init(InstanceUnit unit, float timeMS)
            {
                this.mExpire = unit.AllocTimeExpire(timeMS);
                this.mMoveAI = unit.CreateMoveAI(false);
                return this;
            }
            protected override void Disposing()
            {
                this.mExpire?.Dispose();
                this.mExpire = null;
                this.mMoveAI?.Dispose();
                this.mMoveAI = null;
                this.mLastResult = default;
            }

            public MoveBlockResult LastMoveResult { get { return mLastResult; } }

            /// <summary>
            /// 搜索要去的地方
            /// </summary>
            /// <returns></returns>
            protected abstract ITerrainLayer FindTargetPos();

            public override bool OnBlock(State new_state)
            {
                if (new_state is StateDead)
                {
                    return true;
                }
                if (new_state is StateDamage)
                {
                    (new_state as StateDamage).SetNextNoneControllable(this);
                    return true;
                }
                if (new_state is StateSkill)
                {
                    StateSkill ss = new_state as StateSkill;
                    // 反击或者状态解除//
                    if (ss.SkillData.IsCounter)
                    {
                        return true;
                    }
                }
                return mExpire.IsEnd;
            }

            protected override void OnStart()
            {
                var target = this.FindTargetPos();
                this.mMoveAI.FindPath(target);
            }

            protected override void OnUpdate()
            {
                if (mExpire.Update(zone.UpdateIntervalMS))
                {
                    unit.DoSomething();
                }
                else
                {
                    mLastResult = mMoveAI.Update();
                    if ((mLastResult.result & MoveResult.RESULTS_MOVE_END) != 0)
                    {
                        var target = this.FindTargetPos();
                        this.mMoveAI.FindPath(target);
                    }
                }
            }

            protected override void OnStop()
            {

            }
        }

        //---------------------------------------------------------------------------------------
        /// <summary>
        /// 逃跑状态
        /// </summary>
        public class StateEscape : StateMoveNoneControllable
        {
            private float mDistance;
            public static StateEscape Alloc(InstanceUnit unit, float timeMS, float distance = 0)
            {
                var ret = unit.AllocState<StateEscape>();
                ret.Init(unit, timeMS, distance);
                return ret;
            }
            protected virtual StateEscape Init(InstanceUnit unit, float timeMS, float distance = 0)
            {
                base.Init(unit, timeMS);
                if (distance > 0)
                {
                    this.mDistance = distance;
                }
                else if (unit.AGuard)
                {
                    this.mDistance = unit.AGuard.GuardRange;
                }
                return this;
            }
            protected override void OnStart()
            {
                base.OnStart();
                unit.SetActionStatus(UnitActionStatus.Escape);
            }
            protected override ITerrainLayer FindTargetPos()
            {
                var node = unit.FindNearRandomMoveableNode(mDistance);
                //return new Vector2(node.PosX, node.PosY);
                return node;
            }
        }

        //---------------------------------------------------------------------------------------
        /// <summary>
        /// 混乱状态
        /// </summary>
        public class StateChaos : StateMoveNoneControllable
        {
            public static StateChaos Alloc(InstanceUnit unit, float timeMS)
            {
                var ret = unit.AllocState<StateChaos>();
                ret.Init(unit, timeMS);
                return ret;
            }

            protected override void OnStart()
            {
                base.OnStart();
                unit.SetActionStatus(UnitActionStatus.Chaos);
            }
            protected override void OnStop()
            {
                base.OnStop();
            }
            protected override ITerrainLayer FindTargetPos()
            {
                if (unit.AGuard)
                {
                    var node = unit.FindNearRandomMoveableNode(unit.AGuard.GuardRange);
                    //   return new Vector2(node.PosX, node.PosY);
                    return node;
                }
                return null;
            }

        }

    }
}
