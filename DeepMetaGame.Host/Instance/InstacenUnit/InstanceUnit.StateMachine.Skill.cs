using DeepCore.Game3D.Host.Helper;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.Reflection;
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
        /// <summary>
        /// 释放技能状态
        /// </summary>
        public class StateSkill : State
        {
            private InstanceUnit.EquipSkill skill;
            private AttackRangeHelper attack_range;
            private readonly Queue<UnitActionData> action_queue = new Queue<UnitActionData>();
            private TLaunchSkillParam param;
            private InstanceUnit _targetUnit;
            private bool is_done = false;
            private float fastActionRate = 1f;
            private UnitActionData current_action = null;
            private readonly PopupKeyFrames<UnitActionData.KeyFrame> current_frames = new PopupKeyFrames<UnitActionData.KeyFrame>();
            private BitSet8 current_action_status = new BitSet8();
            private float current_pass_time = 0;
            private Geometry.Vector3? move_to;
            private HitMoveSpeed start_move;
            private Geometry.Vector3 body_hited_last_pos;
            private readonly HashMap<uint, InstanceUnit> body_hited = new HashMap<uint, InstanceUnit>();
            private TimeExpire chantExpire;
            private ITerrainWayPoint move_to_target_path;
            private Geometry.Vector3 jump_to_target_pos;
            private UnitAxisAction stopFaceTo;
            private float actionTotalTimeMS;
            private SkillLaunched launched;
            private SkillLaunched mSkillLaunched;

            public static StateSkill Alloc(InstanceUnit unit, EquipSkill skill, TLaunchSkillParam param, SkillLaunched skillLaunched)
            {
                return unit.AllocState<StateSkill>().Init(unit, skill, param, skillLaunched);
            }
            protected virtual StateSkill Init(InstanceUnit unit, EquipSkill skill, TLaunchSkillParam param, SkillLaunched skillLaunched)
            {
                this.skill = skill;
                this.skill.Retain();
                this.param = param;
                this.actionTotalTimeMS = skill.Data.TotalActionQueueTimeMS;
                this.launched = skillLaunched;
                this.TargetUnit = zone.GetUnit(param.TargetUnitID);
                this.attack_range = new AttackRangeHelper(unit);
                if (SkillData.ChantTimeMS > 0)
                {
                    this.chantExpire = unit.AllocTimeExpire(SkillData.ChantTimeMS);
                    this.IsCancelableByMove = true;
                }
                return this;
            }
            protected override void Disposing()
            {
                this.start_move?.Release();
                this.start_move = default;
                this.skill.Release();
                this.skill = default;
                this.attack_range = default;
                this.action_queue.Clear();
                this.param = default;
                this.TargetUnit = null;
                this.is_done = false;
                this.fastActionRate = 1f;
                this.current_action = null;
                this.current_frames.Clear();
                this.current_action_status = new BitSet8();
                this.current_pass_time = 0;
                this.move_to = default;
                //this.start_move?.Dispose();// do not dispose it will auto dispose

                this.body_hited_last_pos = default;
                this.body_hited.Clear();
                this.chantExpire?.Dispose();
                this.chantExpire = default;
                this.move_to_target_path = default;
                this.jump_to_target_pos = default;
                this.stopFaceTo = default;
                this.actionTotalTimeMS = default;
                this.launched = default;
                this.mSkillLaunched = default;
            }

            private TimeExpire m_noneblock;
            private TimeExpire m_invisible;

            public InstanceUnit.EquipSkill Skill { get { return skill; } }
            public SkillTemplate SkillData { get { return skill.Data; } }
            public float CurrentPassTimeMS { get { return current_pass_time; } }
            public float CurrentExpireTimeMS
            {
                get
                {
                    if (current_action != null)
                    {
                        return Math.Max(current_action.TotalTimeMS - current_pass_time, 0);
                    }
                    return 0;
                }
            }
            public float CurrentActionTimeMS
            {
                get
                {
                    if (current_action != null)
                    {
                        return current_action.TotalTimeMS;
                    }
                    return 0;
                }
            }
            public float CurrentActionTimeProgressRate
            {
                get
                {
                    if (current_action != null && current_action.TotalTimeMS > 0)
                    {
                        return current_pass_time / current_action.TotalTimeMS;
                    }
                    return 0;
                }
            }

            public int CurrentActionIndex
            {
                get
                {
                    if (current_action != null)
                    {
                        return SkillData.ActionQueue.IndexOf(current_action);
                    }
                    return 0;
                }
            }
            public UnitActionData CurrentAction => current_action;
            public float ActionTotalTimeMS { get => actionTotalTimeMS; }

            public InstanceUnit TargetUnit
            {
                get { return _targetUnit; }
                set
                {
                    _targetUnit?.Release();
                    _targetUnit = value;
                    _targetUnit?.Retain();
                }
            }
            public bool IsControlMoveable { get { return current_action_status.Get(0); } private set { current_action_status.Set(0, value); } }
            public bool IsControlFaceable { get { return current_action_status.Get(1); } private set { current_action_status.Set(1, value); } }
            public bool IsCancelableBySkill { get { return current_action_status.Get(2); } private set { current_action_status.Set(2, value); } }
            public bool IsCancelableByMove { get { return current_action_status.Get(3); } private set { current_action_status.Set(3, value); } }
            public bool IsNoneTouch { get { return current_action_status.Get(5); } private set { current_action_status.Set(5, value); } }
            public bool IsFaceToTarget { get { return current_action_status.Get(6); } private set { current_action_status.Set(6, value); } }
            public bool IsNoneBlock { get { return current_action_status.Get(4); } }
            private void SetNoneBlock(bool value, float timeMS)
            {
                m_noneblock?.Dispose();
                if (value)
                {
                    m_noneblock = unit.SetNoneBlockTimeMS(timeMS);
                }
                else
                {
                    m_noneblock = null;
                }
                current_action_status.Set(4, value);
            }
            public bool IsInvisible { get { return current_action_status.Get(7); } }
            private void SetIsInvisible(bool value, float timeMS)
            {
                m_invisible?.Dispose();
                if (value)
                {
                    m_invisible = unit.SetInvisibleTimeMS(timeMS);
                }
                else
                {
                    m_invisible = null;
                }
                current_action_status.Set(7, value);
            }
            public uint TargetUnitID { get => TargetUnit != null ? TargetUnit.ID : 0; }
            public Geometry.Vector3? SpellTargetPos { get => param.SpellTargetPos; }
            public TLaunchSkillParam StartParam { get { return param; } }

            public bool IsStartMove { get { return start_move != null && start_move.IsEnd == false && start_move.IsDisposing == false; } }

            /// <summary>
            /// 技能是否完结
            /// </summary>
            public bool IsDone { get { return is_done; } }
            /// <summary>
            /// 是否在吟唱中
            /// </summary>
            public bool IsChanting { get { return chantExpire != null; } }
            /// <summary>
            /// 技能释放结束后的朝向
            /// </summary>
            public UnitAxisAction StopFaceTo { get => stopFaceTo; set => stopFaceTo = value; }
            /// <summary>
            /// 动作总时间
            /// </summary>
            /// <summary>
            /// 释放前判断
            /// </summary>
            /// <returns></returns>
            public bool tryLaunch()
            {
                if (SkillData.ActionQueue.Count > 0)
                {
                    if (!CheckTargetRange())
                    {
                        return false;
                    }
                    if (!startMoveToTarget(SkillData.ActionQueue[0]))
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            }
            public bool CanBlockByNewSkill(SkillTemplate sk)
            {
                // 反击
                if (sk.IsCounter)
                {
                    return true;
                }
                // 高优先级技能打断老技能
                if (sk.ActionPriority > this.SkillData.ActionPriority)
                {
                    return true;
                }
                // 当前动作是否可被打断
                if (this.IsCancelableBySkill)
                {
                    return true;
                }
                return false;
            }
            override public bool OnBlock(State new_state)
            {
                // 吟唱中 //
                if (IsChanting)
                {
                    return true;
                }
                // 技能完结 //
                if (is_done)
                {
                    return true;
                }
                if (unit.IsDead)
                {
                    return true;
                }
                // 被死亡或控制类技能中断 //
                if (new_state is StateDead || new_state is StateStun || new_state is ForceState)
                {
                    skill.ClearActionIndex();
                    return true;
                }
                // 如果死亡则受击 //
                else if (new_state is StateDamage)
                {
                    if (IsNoneBlock)
                    {
                        return false;
                    }
                    skill.ClearActionIndex();
                    return true;
                }
                else if (new_state is StateSkill)
                {
                    StateSkill ns = new_state as StateSkill;
                    if (CanBlockByNewSkill(ns.SkillData))
                    {
                        return true;
                    }
                }
                else
                {
                    // 当前动作是否可被打断 //
                    if (this.IsCancelableByMove)
                    {
                        return true;
                    }
                }
                return false;
            }
            //private long startTime;
            override protected void OnStart()
            {
                //startTime = CUtils.TickTimeMS;
                unit.SetActionStatus(UnitActionStatus.Skill);
                if (IsChanting)
                {
                    unit.PostEvent(ObjectPool.Alloc<UnitChantSkillEvent>().Init(unit.ID, SkillData));
                }
                else
                {
                    beginLaunch();
                }
                // 如果关键帧绑定特效
                if (skill.Data.CastEffect != null)
                {
                    unit.PostEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(unit.ID, skill.Data.CastEffect));
                }
            }

            override protected void OnStop()
            {
                move_to_target_path = null;
                if (start_move != null)
                {
                    start_move.Stop();
                    start_move.Release();
                    start_move = null;
                }
                //unit.Z = 0;
                is_done = true;
                if (!IsChanting)
                {
                    if (skill != null)
                    {
                        skill.Stop(this);
                    }
                    if (unit.IsSkillControllableByServer)
                    {
                        if (StopFaceTo != null)
                        {
                            //unit.Direction = MathVector.getDegree(StopFaceTo);
                            unit.FaceTo(StopFaceTo.faceto);
                        }
                    }
                }
                zone.cb_unitOverSkill(unit, this.skill, this);
                //var elapsed = CUtils.TickTimeMS - startTime;
                PlayerSkillStopEvent evt = unit.ObjectPool.Alloc<PlayerSkillStopEvent>().Init(unit.ID, skill.ID);
                unit.PostEvent(evt);
            }
            override protected void OnUpdate()
            {
                if (chantExpire != null)
                {
                    if (!CheckTargetRange())
                    {
                        is_done = true;
                        unit.DoSomething();
                        return;
                    }
                    if (chantExpire.Update(zone.UpdateIntervalMS))
                    {
                        beginLaunch();
                        chantExpire = null;
                    }
                }
                if (IsChanting) return;
                if (is_done)
                {
                    unit.DoSomething();
                    return;
                }
                {
                    var passMS = (zone.UpdateIntervalMS * fastActionRate * unit.FastActionRate);
                    if (passMS <= 0)
                    {
                        zone.BroadcastMessageBox($"{unit} : skill is freeze : {fastActionRate} * {unit.FastActionRate}", true);
                        is_done = true;
                        unit.DoSomething();
                        return;
                    }
                    this.current_pass_time += passMS;
                }
                if (current_action == null)
                {
                    if (action_queue == null) return;
                    nextAction();
                }
                if (current_action == null)
                {
                    is_done = true;
                    unit.DoSomething();
                }
                else
                {
                    body_hited_last_pos.X = unit.X;
                    body_hited_last_pos.Y = unit.Y;

                    //                     if (IsFaceToTarget)
                    //                     {
                    //                         if (getTargetPos(null, out var tpos))
                    //                         {
                    //                             unit.FaceTo(tpos.X, tpos.Y);
                    //                         }
                    //                     }


                    // 关键帧 //
                    using (var kfs = unit.ObjectPool.AllocList<UnitActionData.KeyFrame>())
                    {
                        if (current_frames.PopKeyFrames(current_pass_time, kfs) > 0)
                        {
                            for (int i = 0; i < kfs.Count; i++)
                            {
                                doKeyFrame(kfs[i]);
                            }
                        }
                    }
                    if (current_action != null)
                    {
                        var action = current_action;
                        if (IsFaceToTarget)
                        {
                            if (getTargetPos(null, out var tpos))
                            {
                                if (float.IsNaN(action.TurnSpeedSEC) || action.TurnSpeedSEC == 0)
                                {
                                    unit.FaceTo(tpos.X, tpos.Y);
                                }
                                else
                                {
                                    unit.TurnTo(tpos, action.TurnSpeedSEC, zone.UpdateIntervalMS);
                                }
                            }
                        }
                        //                         else if (!float.IsNaN(action.TurnSpeedSEC))
                        //                         {
                        //                             unit.Turn(MoveHelper.GetTurnSpeed(current_action.TurnSpeedSEC, zone.UpdateIntervalMS));
                        //                         }
                        // 技能位移 //
                        // 移动到目标时，不切换动作 //
                        if (action.IsMoveToTarget)
                        {
                            // 冲到目标，立即下段 //
                            doMoveToTarget(action);
                        }
                        else if (action.IsJumpToTarget)
                        {
                            doJumpToTarget(action);
                        }
                        else
                        {
                            if (unit.IsSkillControllableByServer)
                            {
                                if (start_move != null)
                                {
                                    doMove();
                                }
                                if (move_to != null)
                                {
                                    if (IsControlMoveable)
                                    {
                                        unit.MoveImpactTo(
                                            move_to.Value.X,
                                            move_to.Value.Y,
                                            unit.MoveSpeedSEC * move_to.Value.Z,
                                            zone.UpdateIntervalMS, false);
                                    }
                                    else
                                    {
                                        move_to = null;
                                    }
                                }
                            }
                            // 身体攻击 //
                            if (action.BodyHit != null)
                            {
                                doBodyHit(action);
                            }
                            // 防止技能位移导致单位重合 //
                            if (action.BodyBlockOnAttackRange)
                            {
                                doBodyBlock(action);
                            }
                        }
                    }
                    if ((current_action == null) || (current_pass_time >= current_action.TotalTimeMS))
                    {
                        // 下段动作 //
                        if (!nextAction())
                        {
                            is_done = true;
                            unit.DoSomething();
                        }
                    }

                }
            }

            private void beginLaunch()
            {
                skill.Launch(this, param);
                this.fastActionRate = skill.FastActionRate;
                if (skill.Data.ActionSpeedRate > 0)
                {
                    this.fastActionRate *= skill.Data.ActionSpeedRate;
                }
                {
                    byte actionIndex = skill.ActionIndex;
                    this.action_queue.Clear();
                    if (skill.Data.IsSingleAction)
                    {
                        this.action_queue.Enqueue(skill.Data.ActionQueue[actionIndex]);
                    }
                    else
                    {
                        this.action_queue.EnqueueRange(skill.Data.ActionQueue);
                    }
                    launched.Invoke(this);
                    if (mSkillLaunched != null)
                    {
                        mSkillLaunched.Invoke(this);
                        mSkillLaunched = null;
                    }
                }
                if (unit.IsSkillControllableByServer)
                {
                    if (param.AutoFocusNearTarget && (TargetUnit == null || !TargetUnit.IsActive))
                    {
                        //自动锁定目标//
                        bool directionChange = false;
                        this.TargetUnit = unit.getSkillAttackableFirstTarget(this.SkillData, AttackReason.Look, ref directionChange);
                        if (TargetUnit != null && TargetUnit != unit && directionChange)
                        {
                            unit.FaceTo(TargetUnit.X, TargetUnit.Y);
                        }
                    }
                }
                else if (param.SpellTargetPos != null && !param.SpellTargetPos.Value.IsNaN)
                {
                    if (IsControlFaceable == false)
                    {
                        unit.FaceTo(param.SpellTargetPos.Value.X, param.SpellTargetPos.Value.Y);
                        unit.SendForceFaceSync();
                    }
                }
                {
                    UnitLaunchSkillEvent evt = ObjectPool.Alloc<UnitLaunchSkillEvent>().Init(
                        unit.ID,
                        skill.Data,
                        skill.Level,
                        skill.ActionIndex,
                        this.fastActionRate,
                        skill.FastCastRate,
                        skill.TotalCDTime,
                        param.AutoFocusNearTarget,
                        TargetUnit != unit ? param.SpellTargetPos : null,
                        TargetUnit != null ? TargetUnit.ID : 0);
                    {
                        evt.start_pos = unit.Position;
                        evt.start_dir = unit.Direction;
                    }
                    ;
                    unit.PostEvent(evt);
                }
                nextAction();
                beginMoveToTarget();

            }

            private bool beginMoveToTarget()
            {
                if (current_action != null && current_action.IsMoveToTarget)
                {
                    if (TargetUnit != null && zone.TouchObject2(unit, TargetUnit))
                    {
                        if (current_action.IsMoveToTargetStopAction && nextAction())
                        {
                            unit.PostEvent(ObjectPool.Alloc<UnitSkillActionChangeEvent>().Init(unit.ID, (byte)CurrentActionIndex));
                        }
                        return true;
                    }
                }
                return false;
            }
            private bool nextAction()
            {
                move_to_target_path = null;
                if (start_move != null)
                {
                    start_move.Stop();
                    start_move.Release();
                    start_move = null;
                }

                if (action_queue.Count > 0)
                {
                    //NewAction//
                    this.current_action = action_queue.Dequeue();
                    //this.current_frames = new PopupKeyFrames<UnitActionData.KeyFrame>(current_action.KeyFrames);
                    this.current_frames.AddRange(current_action.KeyFrames);
                    this.current_pass_time = 0;

                    this.IsCancelableByMove = current_action.IsCancelable;
                    this.IsCancelableBySkill = current_action.IsCancelableBySkill;
                    //this.IsNoneBlock = current_action.IsNoneBlock;
                    this.IsNoneTouch = current_action.IsNoneTouch;
                    //this.IsInvisible = current_action.IsInvisible;
                    this.IsFaceToTarget = current_action.IsFaceToTarget;
                    this.IsControlFaceable = current_action.IsControlFaceable;
                    this.IsControlMoveable = current_action.IsControlMoveable;

                    this.SetNoneBlock(current_action.IsNoneBlock, current_action.TotalTimeMS);
                    this.SetIsInvisible(current_action.IsInvisible, current_action.TotalTimeMS);
                    //                     if (IsNoneBlock)
                    //                     {
                    //                         unit.SetNoneBlockTimeMS(current_action.TotalTimeMS);// = true;
                    //                     }
                    //                     if (IsInvisible)
                    //                     {
                    //                         unit.SetInvisibleTimeMS(current_action.TotalTimeMS);// = true;
                    //                     }
                    if (current_action.IsMoveToTarget)
                    {
                        //先寻路//
                        startMoveToTarget(current_action);
                    }
                    else if (current_action.IsJumpToTarget)
                    {
                        //计算跳跃距离//
                        startJumpToTarget(current_action);
                    }
                    else
                    {
                        if (unit.IsSkillControllableByServer)
                        {
                            if (param.AutoFocusNearTarget && (TargetUnit == null || !TargetUnit.IsActive))
                            {
                                //自动锁定目标//
                                bool directionChange = false;
                                TargetUnit = unit.getSkillAttackableFirstTarget(this.SkillData, AttackReason.Look, ref directionChange);
                                if (TargetUnit != null && TargetUnit != unit && directionChange)
                                {
                                    if (float.IsNaN(current_action.TurnSpeedSEC) || current_action.TurnSpeedSEC == 0)
                                    {
                                        unit.FaceTo(TargetUnit.X, TargetUnit.Y);
                                    }
                                }
                            }
                        }
                    }
                    return true;
                }
                current_action = null;
                return false;
            }

            private void doKeyFrame(UnitActionData.KeyFrame kf)
            {
                // 关键帧改变状态
                if (kf.ChangeStatus != null)
                {
                    //this.IsNoneBlock = kf.ChangeStatus.IsNoneBlock;
                    this.IsNoneTouch = kf.ChangeStatus.IsNoneTouch;
                    this.IsFaceToTarget = kf.ChangeStatus.IsFaceToTarget;
                    this.IsCancelableByMove = kf.ChangeStatus.IsCancelable;
                    this.IsCancelableBySkill = kf.ChangeStatus.IsCancelableBySkill;
                    this.IsControlFaceable = kf.ChangeStatus.IsControlFaceable;
                    this.IsControlMoveable = kf.ChangeStatus.IsControlMoveable;
                    //this.IsInvisible = kf.ChangeStatus.IsInvisible;
                    this.SetNoneBlock(current_action.IsNoneBlock, this.CurrentExpireTimeMS);
                    this.SetIsInvisible(current_action.IsInvisible, this.CurrentExpireTimeMS);
                }
                if (kf.ChangeTarget != null)
                {
                    this.TargetUnit = zone.SeekSkillAttackableUnit(unit,
                        skill,
                        kf.ChangeTarget,
                        AttackReason.Look,
                        out var targetPos);
                    if (TargetUnit != null)
                    {
                        this.param.TargetUnitID = TargetUnit.ID;
                        if (kf.ChangeTarget.ChangeDirection)
                        {
                            unit.FaceTo(TargetUnit.X, TargetUnit.Y);
                        }
                        if (kf.ChangeTarget.ChangeTargetPos)
                        {
                            this.param.SpellTargetPos = targetPos;
                        }
                    }
                    else
                    {
                        this.param.TargetUnitID = 0;
                    }
                }
                // 如果关键帧绑定特效
                if (kf.Effect != null)
                {
                    unit.PostEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(unit.ID, kf.Effect));
                }
                // 如果关键帧绑定近战攻击
                if (kf.Attack != null)
                {
                    doHitAttack(kf.Attack);
                }
                // 如果关键帧绑定释放法术
                if (kf.Spell != null)
                {
                    zone.SkillLaunchSpell(this, kf.Spell);
                }
                // 如果关键帧绑定召唤
                if (kf.Summon != null)
                {
                    float tx = unit.X;
                    float ty = unit.Y;
                    float tz = unit.Z;
                    if (param.SpellTargetPos != null && !param.SpellTargetPos.Value.IsNaN)
                    {
                        var pos = param.SpellTargetPos.Value;
                        float td = MathVector.getDistance(unit.X, unit.Y, pos.X, pos.Y);
                        var skillRange = unit.GetSkillAttackRange(SkillData.AttackRange);
                        // TargetPos超出技能范围 //
                        if (td > skillRange)
                        {
                            // 把TargetPos拉回 //
                            VectorHelper.MovePolar(ref pos, MathVector.getDegree(unit.X, unit.Y, pos.X, pos.Y), skillRange - td);
                            param.SpellTargetPos = pos;
                        }
                        // 设置法术出生点 (非自身坐标发射，比如Cannon) //
                        tx = pos.X;
                        ty = pos.Y;
                        tz = pos.Z;
                    }
                    zone.UnitSummonUnit(unit, kf.Summon, new Geometry.Vector3(tx, ty, tz), unit.Direction, param.SummonID);
                }
                // 如果关键帧绑定自己释放BUFF
                if (kf.SelfBuff != null)
                {
                    unit.AddBuff(kf.SelfBuff, unit, skill);
                }
                // 如果关键帧绑定自己释放Aura
                if (kf.SelfAura != null)
                {
                    unit.LaunchAura(kf.SelfAura, skill);
                }
                // 如果关键帧绑定单位位移
                if (kf.Move != null)
                {
                    startMove(kf.Move);
                }
                if (kf.Blink != null)
                {
                    startBlink(kf.Blink);
                }
            }

            private void startBlink(BlinkMove blink)
            {
                if (blink.BeginEffect != null)
                {
                    unit.PostEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(unit.ID, blink.BeginEffect));
                }
                unit.MoveBlink(blink, param.SpellTargetPos, TargetUnit);
                unit.SendForceSync();
                if (blink.TargetEffect != null)
                {
                    unit.PostEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(unit.ID, blink.TargetEffect));
                }
            }

            private void startMove(StartMove action_move)
            {
                this.start_move?.Release();
                if (unit.IsSkillControllableByServer)
                {
                    this.start_move = unit.StartHitMove(this, action_move);
                }
                else
                {
                    this.start_move = unit.StartHitMove(this, unit.Direction, 0, action_move.KeepTimeMS, 0, 0, 0, false);
                }
                this.start_move.Retain();
                if (this.TargetUnit != null && unit != TargetUnit && !action_move.IsNoneTouch)
                {
                    this.start_move.SetBlockTarget(this.TargetUnit);
                }
            }
            /// <summary>
            /// 检测冲锋距离
            /// </summary>
            /// <returns></returns>
            private bool startMoveToTarget(UnitActionData action)
            {
                if (action.IsMoveToTarget)
                {
                    if (getTargetPos(action, out var tpos))
                    {
                        this.move_to_target_path = zone.FindPathSrcLayer(unit, tpos);
                        if (this.move_to_target_path != null)
                        {
                            float max = MoveHelper.GetDistance(action.TotalTimeMS, action.MoveToTargetSpeedSEC);
                            float total = this.move_to_target_path.TotalDistance;
                            if (total > max)
                            {
                                move_to_target_path = null;
                                return false;
                            }
                            return true;
                        }
                    }
                    return false;
                }
                return true;
            }
            /// <summary>
            /// 检测跳跃速度
            /// </summary>
            /// <param name="action"></param>
            /// <returns></returns>
            private bool startJumpToTarget(UnitActionData action)
            {
                if (action.IsJumpToTarget)
                {
                    if (getTargetPos(action, out var tpos))
                    {
                        if (action.IsJumpLockTarget)
                        {
                            //自动冲向目标
                            if (TargetUnit == null)
                            {
                                return false;
                            }

                            float rg = unit.GetSkillAttackRange(SkillData);
                            if (Collider.Intersects(unit.Position, TargetUnit.Position, rg))
                            {
                                return false;
                            }

                            if (!Collider.Intersects(unit.Position, TargetUnit.Position, action.JumpLockMaxRange))
                            {
                                return false;
                            }

                            float distance = CMath.GetDistance(unit.X, unit.Y, tpos.X, tpos.Y);
                            jump_to_target_pos = tpos;

                            if (action.JumpLockTimeMS == 0)
                            {
                                action.JumpLockTimeMS = 1;
                            }

                            var fall = unit.StartJump(action.JumpToTargetSpeedZ);
                            if (fall != null)
                            {
                                fall.OnFallDown += (f) =>
                                {
                                    if (action.JumpFallenDownKeyFrame != null)
                                    {
                                        doKeyFrame(action.JumpFallenDownKeyFrame);
                                        //doHitAttack(action.JumpFallenDownAttack);
                                    }
                                    if (action.IsMoveToTargetStopAction && nextAction())
                                    {
                                        unit.PostEvent(ObjectPool.Alloc<UnitSkillActionChangeEvent>().Init(unit.ID, (byte)CurrentActionIndex));
                                    }
                                };
                            }
                        }
                        else
                        {
                            float distance = CMath.GetDistance(unit.X, unit.Y, tpos.X, tpos.Y);
                            jump_to_target_pos = tpos;
                            var fall = unit.StartJump(action.JumpToTargetSpeedZ);
                            if (fall != null)
                            {
                                fall.OnFallDown += (f) =>
                                {
                                    if (action.JumpFallenDownKeyFrame != null)
                                    {
                                        doKeyFrame(action.JumpFallenDownKeyFrame);
                                        //doHitAttack(action.JumpFallenDownAttack);
                                    }
                                    if (action.IsMoveToTargetStopAction && nextAction())
                                    {
                                        unit.PostEvent(ObjectPool.Alloc<UnitSkillActionChangeEvent>().Init(unit.ID, (byte)CurrentActionIndex));
                                    }
                                };
                            }
                        }
                    }
                }
                return true;
            }
            /// <summary>
            /// 近战攻击
            /// </summary>
            /// <param name="attack"></param>
            private void doHitAttack(AttackProp attack)
            {
                var shape = skill.Data.AttackShape;
                if (current_action?.OverrideAttackShape != null)
                {
                    shape = current_action.OverrideAttackShape;
                }
                if (shape.IsSingle)
                {
                    if (TargetUnit != null)
                    {
                        if (CheckTargetRange(true))
                        {
                            using (var attack2 = TAttackSource.AllocWithSkill(skill, attack))
                            {
                                zone.UnitAttackSingle(unit, attack2, TargetUnit, SkillData.ExpectTarget);
                            }
                        }
                    }
                }
                else
                {
                    attack_range.Shape = (AttackShape)shape.AShape;
                    attack_range.Direction = unit.Direction;
                    attack_range.ExpectTarget = SkillData.ExpectTarget;
                    attack_range.FanAngle = shape.AttackAngle;
                    attack_range.AttackRange = unit.GetSkillAttackRange(shape.AttackRange);
                    attack_range.Distance = unit.GetSkillAttackRange(shape.AttackRange);
                    attack_range.StripWide = shape.StripWide * unit.BodyScale;
                    attack_range.Height = unit.BodyHeight;
                    var dpos = unit.Position;
                    if (shape.OffsetRadius != 0)
                    {
                        Geometry.VectorHelper.MovePolar(ref dpos, unit.Direction, shape.OffsetRadius);
                    }
                    using (var list = unit.ObjectPool.AllocList<InstanceUnit>())
                    {
                        attack_range.GetShapeAttackable(list, AttackReason.Attack, SkillData, dpos, TargetUnit);
                        if (list.Count > 0)
                        {
                            using (var attackSrc = TAttackSource.AllocWithSkill(skill, attack))
                            {
                                zone.UnitAttackDirect(unit, attackSrc, list);
                            }
                        }
                    }
                }
                //                 {
                //                     float rg = unit.GetSkillAttackRange(skill.Data);
                //                     int hitcount = zone.UnitAttackFan(
                //                         unit,
                //                         new AttackSource(skill, attack),
                //                         unit.Direction,
                //                         rg,
                //                         skill.Data.AttackAngle,
                //                         SkillData.ExpectTarget);
                //                 }
            }

            /// <summary>
            /// 身体攻击
            /// </summary>
            private void doBodyHit(UnitActionData current_action)
            {
                using (var list = unit.ObjectPool.AllocList<InstanceUnit>())
                {
                    float line_r = (current_action.BodyHitSize > 0) ? current_action.BodyHitSize : unit.BodyHitSize;
                    var stripe = Geometry.VoxelStripe.InitFromPoint(body_hited_last_pos,
                        unit.Position, line_r, unit.BodyHeight);
                    zone.GetObjectsInStripe(this, Collider.Stripe_Touch_HitBody, stripe, list);
                    if (list.Count > 0)
                    {
                        CUtils.RemoveAll(list, body_hited.Values);
                        using (var atkSrc = TAttackSource.AllocWithSkill(skill, current_action.BodyHit))
                        {
                            zone.UnitAttack(unit, atkSrc, list, SkillData.ExpectTarget);
                            if (list.Count > 0)
                            {
                                for (int i = 0; i < list.Count; i++)
                                {
                                    InstanceUnit o = list[i];
                                    body_hited.Put(o.ID, o);
                                }
                                if (current_action.BodyHitNextAction)
                                {
                                    if (nextAction())
                                    {
                                        unit.PostEvent(ObjectPool.Alloc<UnitSkillActionChangeEvent>().Init(unit.ID, (byte)CurrentActionIndex));
                                    }
                                    unit.SendForceSync();
                                }
                            }
                        }
                    }
                }
            }
            private void doMove()
            {
                start_move.IsNoneTouch = IsNoneTouch;
                if (start_move.IsEnd || start_move.IsDisposing)
                {
                    start_move.Release();
                    start_move = null;
                }
            }

            /// <summary>
            /// 防止技能位移导致单位重合
            /// </summary>
            private void doBodyBlock(UnitActionData current_action)
            {
                if (start_move != null || move_to != null)
                {
                    if (unit.ElasticOtherObjects())
                    {
                        unit.SendForceSync();
                    }
                }
            }

            /// <summary>
            /// 移动到目标面前
            /// </summary>
            private bool doMoveToTarget(UnitActionData current_action)
            {
                if (this.move_to_target_path == null)
                {
                    if (current_action.IsMoveToTargetStopAction && nextAction())
                    {
                        unit.PostEvent(ObjectPool.Alloc<UnitSkillActionChangeEvent>().Init(unit.ID, (byte)CurrentActionIndex));
                    }
                    return true;
                }
                else if (TargetUnit != null && zone.TouchObject2(unit, TargetUnit))
                {
                    this.move_to_target_path = null;
                    if (unit.ElasticOtherObjects()) { unit.SendForceSync(); }
                    if (current_action.IsMoveToTargetStopAction && nextAction())
                    {
                        unit.PostEvent(ObjectPool.Alloc<UnitSkillActionChangeEvent>().Init(unit.ID, (byte)CurrentActionIndex));
                    }
                    return true;
                }
                else
                {
                    var tpos = move_to_target_path.Position;
                    unit.FaceTo(tpos.X, tpos.Y);// = MathVector.getDegree(tx - unit.X, ty - unit.Y);
                    if (unit.MoveAirToTarget(tpos.X, tpos.Y, current_action.MoveToTargetSpeedSEC, zone.UpdateIntervalMS))
                    {
                        move_to_target_path = move_to_target_path.Next;
                    }
                    unit.SendForceSync();
                    return false;
                }
            }
            private bool doJumpToTarget(UnitActionData current_action)
            {
                //unit.Z = MoveHelper.CalulateParabolicHeight(current_action.JumpToTargetHeightZ, current_action.TotalTimeMS, this.current_pass_time);
                unit.MoveAirToTarget(jump_to_target_pos.X, jump_to_target_pos.Y, current_action.MoveToTargetSpeedSEC, zone.UpdateIntervalMS);
                unit.SendForceSync();
                // fall down 已经处理 nextAction
                //                 if (TargetUnit != null && zone.TouchObject2(unit, TargetUnit))
                //                 {
                //                     if (nextAction())
                //                     {
                //                         unit.PostEvent(new UnitSkillActionChangeEvent(unit.ID, (byte)CurrentActionIndex));
                //                     }
                //                     return true;
                //                 }
                return false;
            }

            public void controlMoveTo(float x, float y, float distanceRate)
            {
                if (unit.IsSkillControllableByServer)
                {
                    if (IsControlMoveable)
                    {
                        move_to = new Geometry.Vector3(x, y, distanceRate);
                    }
                    if (IsControlFaceable)
                    {
                        unit.FaceTo(x, y);
                    }
                    //this.StopFaceTo = new Vector2(x - unit.X, y - unit.Y);
                }
            }

            public void controlFaceTo(float x, float y)
            {
                if (unit.IsSkillControllableByServer && IsControlFaceable)
                {
                    unit.FaceTo(x, y);
                }
            }

            public void controlMoveTo(UnitAxisAction axis)
            {
                if (unit.IsSkillControllableByServer)
                {
                    if (axis == null)
                    {
                        this.move_to = null;
                        return;
                    }
                    if (axis.distanceRate != 0)
                    {
                        if (IsControlMoveable)
                        {
                            var pos = unit.Position;
                            pos.Z = axis.distanceRate;
                            VectorHelper.MovePolar(ref pos, axis.angle, axis.distanceRate);
                            move_to = pos;
                        }
                    }
                    else
                    {
                        move_to = null;
                    }
                    if (IsControlFaceable)
                    {
                        unit.FaceTo(axis.faceto);
                    }
                    this.StopFaceTo = axis;// new Vector2(pos.x - unit.X, pos.y - unit.Y);
                }
            }

            public void block(State newState = null)
            {
                bool old_done = is_done;
                is_done = true;
                if (newState != null)
                {
                    unit.ChangeState(newState);
                }
                else if (!old_done)
                {
                    unit.DoSomething();
                }
            }

            /// <summary>
            /// 检测目标距离
            /// </summary>
            /// <returns></returns>
            private bool CheckTargetRange(bool isSingle = false)
            {
                return skill.CheckTargetRange(TargetUnit, isSingle);
            }
            private bool getTargetPos(UnitActionData action, out Geometry.Vector3 targetPos)
            {
                targetPos = unit.Position;
                UnitActionData.TargetPosEnum pos = UnitActionData.TargetPosEnum.Body;
                float offset = 0;
                if (action != null)
                {
                    pos = action.TargetPos;
                    offset = action.TargetOffset;
                }
                if (TargetUnit != null && TargetUnit != unit)
                {
                    targetPos = TargetUnit.Position;
                    switch (pos)
                    {
                        case UnitActionData.TargetPosEnum.Body:
                            break;
                        case UnitActionData.TargetPosEnum.Face:
                            var angle = MathVector.getDegree(unit.X, unit.Y, TargetUnit.X, TargetUnit.Y);
                            var len = -(unit.BodyBlockSize + TargetUnit.BodyBlockSize + offset);
                            Geometry.VectorHelper.MovePolar(ref targetPos, angle, len);
                            break;
                    }
                    return true;
                }
                else if (param.SpellTargetPos != null && !param.SpellTargetPos.Value.IsNaN)
                {
                    targetPos = param.SpellTargetPos.Value;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void doUpdatePosByClient(UnitUpdatePosAction act)
            {
                if (!unit.IsSkillControllableByServer)
                {
                    //if (this.IsStartMove)
                    {
                        if (act.pos.HasValue) unit.ControlSetPos(act.pos.Value);
                        if (act.direction.HasValue) unit.FaceTo(act.direction.Value);
                    }
                    /*
                    else
                    {
                        if (this.IsControlMoveable)
                        {
                            unit.SetPos(act.pos, unit.IntersectMap);
                        }
                        if (this.IsControlFaceable)
                        {
                            unit.FaceTo(act.d);
                            //unit.Direction = act.d;
                        }
                    }*/
                }
            }

            public delegate void SkillLaunched(StateSkill state);
            public event SkillLaunched OnSkillLaunched { add { mSkillLaunched += value; } remove { mSkillLaunched -= value; } }

        }

        //--------------------------------------------------------------------------------------------------------

    }
}
