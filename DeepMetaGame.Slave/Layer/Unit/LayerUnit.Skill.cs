using DeepCore.Game3D.Slave.Data;
using DeepCore.Geometry;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using static DeepCore.Game3D.Slave.Layer.LayerPlayer;
using DeepMetaGame.Data.Helper;

namespace DeepCore.Game3D.Slave.Layer
{
    public partial class LayerUnit
    {
        //---------------------------------------------------------------------------
        #region Skill

        public float GetSkillAttackRange(SkillTemplate skill)
        {
            return (BodyBlockSize + skill.AttackRange * BodyScale);
        }
        public float GetSkillAttackRange(float range)
        {
            return (BodyBlockSize + range * BodyScale);
        }
        internal void SyncSkillStatus(IEnumerable<ClientStruct.UnitSkillStatus> skills)
        {
            if (skills != null)
            {
                foreach (ClientStruct.UnitSkillStatus st in skills)
                {
                    SkillState status = this.GetSkillState(st.SkillTemplateID);
                    if (status != null)
                    {
                        status.Sync(st);
                    }
                }
            }
        }

        protected virtual void ResetSkills()
        {
            if (ASkill)
            {
                SkillTemplate basicSkill = null;
                if (ASkill.BaseSkillID != null)
                {
                    basicSkill = Templates.GetSkill(ASkill.BaseSkillID.SkillID);
                }
                using (var skills = ObjectPool.AllocList<SkillInit>(ASkill.Skills.Count))
                {
                    foreach (LaunchSkill skid in ASkill.Skills)
                    {
                        SkillTemplate skt = Templates.GetSkill(skid.SkillID);
                        if (skt != null)
                        {
                            skills.Add(new SkillInit()
                            {
                                Skill = skt,
                                Launch = skid,
                            });
                        }
                    }
                    InitSkills(new SkillInit() { Skill = basicSkill, Launch = ASkill.BaseSkillID }, skills.ToArray());
                }
            }
        }

        public void InitSkills(SkillInit baseSkill, SkillInit[] skills)
        {
            BaseSkillID = 0;
            mSkillStatus.Clear();
            if (baseSkill?.Skill != null)
            {
                mSkillStatus.Put(SkillState.Alloc(this, baseSkill));
                BaseSkillID = baseSkill.Skill.ID;
            }
            foreach (var skt in skills)
            {
                if (BaseSkillID != skt.Skill.ID)
                {
                    mSkillStatus.Put(SkillState.Alloc(this, skt));
                }
            }
            if (mOnSkillChanged != null)
            {
                mOnSkillChanged.Invoke(this, BaseSkillID, mSkillStatus.Keys.ToArray());
            }
        }
        protected void AddSkill(SkillInit skill, bool isDefault)
        {
            mSkillStatus.Put(SkillState.Alloc(this, skill));
            if (isDefault)
            {
                BaseSkillID = skill.Skill.ID;
            }
            if (mOnSkillChanged != null)
            {
                mOnSkillChanged.Invoke(this, BaseSkillID, mSkillStatus.Keys.ToArray());
            }
        }
        protected void RemoveSkill(int skillTemplateID)
        {
            if (mSkillStatus.RemoveByKey(skillTemplateID, out var st))
            {
                try
                {
                    if (BaseSkillID == skillTemplateID)
                    {
                        BaseSkillID = 0;
                    }
                    if (mOnSkillChanged != null)
                    {
                        mOnSkillChanged.Invoke(this, BaseSkillID, mSkillStatus.Keys.ToArray());
                    }
                }
                finally
                {
                    st.Dispose();
                }
            }
        }

        protected void UpdateSkills(float intervalMS)
        {
            for (int i = 0; i < mSkillStatus.Count; i++)
            {
                SkillState ss = mSkillStatus.GetAt(i);
                ss.Update(intervalMS);
            }
            if (mChantingSkill != null && mChantingSkill.Update(intervalMS))
            {
                mChantingSkill = null;
            }
        }

        protected virtual void DoSkillChanged(PlayerSkillChangedEvent e)
        {
            this.InitSkills(e.baseSkill, e.skills.ToArray());
        }
        protected virtual void DoPlayerSkillActiveChangedEvent(PlayerSkillActiveChangedEvent e)
        {
            for (int i = e.Skills.Count - 1; i >= 0; --i)
            {
                PlayerSkillActiveChangedEvent.State sat = e.Skills[i];
                SkillState ss = GetSkillState(sat.SkillTemplateID);
                if (ss != null)
                {
                    ss.SetActive(sat.ST);
                }
            }
            if (mOnSkillChanged != null)
            {
                mOnSkillChanged.Invoke(this, BaseSkillID, mSkillStatus.Keys.ToArray());
            }
        }

        protected virtual void DoSkillAdded(PlayerSkillAddedEvent e)
        {
            this.AddSkill(e.Skill, e.IsDefault);
        }
        protected virtual void DoSkillRefresh(PlayerSkillRefreshEvent e)
        {
            if (e.Skill != null)
            {
                SkillState ss = GetSkillState(e.Skill.ID);
                if (ss != null)
                {
                    ss.Sync(e);
                }
            }
        }
        protected virtual void DoSkillRemoved(PlayerSkillRemovedEvent e)
        {
            this.RemoveSkill(e.SkillID);
        }

        protected virtual void DoLaunchSkill(UnitLaunchSkillEvent me)
        {
            var ss = mSkillStatus.GetOrCreate(me.skill_id, (me, Templates, this), static (st, i) =>
            {
                var temp = st.Templates.GetSkill(st.me.skill_id);
                if (temp == null) throw new Exception("Can Not Found Skill : " + st.me.skill_id);
                return SkillState.Alloc(st.Item3, new SkillInit()
                {
                    Skill = temp,
                    Launch = new LaunchSkill() { SkillID = temp.ID, SkillLevel = st.me.skill_level, }
                });
            });
            if (ss.Data.ChantTimeMS <= 0)
            {

            }
            this.mLastLaunchSkill = ss;
            ss.Launch(me);
            doLaunchSkillAction(ss, me);
        }
        protected virtual void DoSkillCDChanged(PlayerCDEvent evt)
        {
            if (evt.is_all)
            {
                for (int i = 0; i < mSkillStatus.Count; i++)
                {
                    SkillState ss = mSkillStatus.GetAt(i);
                    if (evt.is_clear)
                        ss.ClearCD();
                    else if (evt.is_decrease_time)
                        ss.DecreaseCD(evt.decrease_timeMS);
                    else if (evt.is_decrease_pct)
                        ss.DecreaseCD_Pct(evt.decrease_pct);
                }
            }
            else
            {
                SkillState ss = mSkillStatus.Get(evt.skill_template_id);
                if (ss != null)
                {
                    if (evt.is_clear)
                        ss.ClearCD();
                    else if (evt.is_decrease_time)
                        ss.DecreaseCD(evt.decrease_timeMS);
                    else if (evt.is_decrease_pct)
                        ss.DecreaseCD_Pct(evt.decrease_pct);
                }
            }
        }
        protected virtual void DoChangeAction(UnitSkillActionChangeEvent e)
        {
            if (mLastLaunchSkill != null)
            {
                mLastLaunchSkill.ChangeAction(e.ActionIndex);
            }
            if (mCurrentSkillAction is ISkillAction skillAction)
            {
                skillAction.onUnitSkillActionChangeEvent(e);
            }
            if (mLastLaunchSkill != null && mOnSkillActionChanged != null)
            {
                mOnSkillActionChanged.Invoke(this, mLastLaunchSkill, e.ActionIndex);
            }
        }
        protected virtual void DoPlayerSkillStopEvent(PlayerSkillStopEvent e)
        {
            SkillState ss = mSkillStatus.Get(e.SkillID);
            if (ss != null)
            {
                ss.PlayerStop(e);
            }
            mChantingSkill = null;
            clearSkillAction();
        }
        protected virtual void DoPlayerSkillTimeChangedEvent(PlayerSkillTimeChangedEvent e)
        {
            SkillState ss = mSkillStatus.Get(e.SkillTemplateID);
            if (ss != null)
            {
                ss.TimeChange(e);
            }
        }

        protected virtual void DoObjectSkillTimeChangedEvent(ObjectSkillTimeChangedEvent e)
        {
            SkillState ss = mSkillStatus.Get(e.SkillTemplateID);
            if (ss != null)
            {
                ss.TimeChange(e);
            }
        }
        public int BaseSkillID { get; private set; }
        public int SkillCount { get => mSkillStatus.Count; }

        internal SkillMap mSkillStatus = new SkillMap();
        private SkillState mLastLaunchSkill;
        public class SkillMap
        {
            private HashMap<int, SkillState> Map = new HashMap<int, SkillState>();
            private List<SkillState> For = new List<SkillState>();

            public int Count { get { return For.Count; } }
            public ICollection<int> Keys { get { return Map.Keys; } }
            public IEnumerable<SkillState> Skills { get { return For; } }
            public SkillState[] SkillsArray { get { return For.ToArray(); } }

            public SkillState GetAt(int i)
            {
                return For[i];
            }
            public SkillState GetOrCreate<ST>(int id, ST st, Func<ST, int, SkillState> create)
            {
                var state = Map.Get(id);
                if (state == null)
                {
                    state = create(st, id);
                    For.Add(state);
                }
                return state;
            }
            public SkillState Get(int id)
            {
                return Map.Get(id);
            }
            public bool ContainsKey(int id)
            {
                return Map.ContainsKey(id);
            }
            public void Put(SkillState state)
            {
                SkillState old = Map.Get(state.Data.ID);
                if (old != null)
                {
                    For.Remove(old);
                    old.Dispose();
                }
                Map.Put(state.Data.ID, state);
                For.Add(state);
            }
            public bool RemoveByKey(int id, out SkillState ret)
            {
                ret = Map.RemoveByKey(id);
                if (ret != null)
                {
                    For.Remove(ret);
                    return true;
                }
                return false;
            }
            public void Clear()
            {
                foreach (var e in For)
                {
                    e.Dispose();
                }
                Map.Clear();
                For.Clear();
            }
        }
        public class SkillState : LayerStatus
        {
            private SkillTemplate data;
            private LayerUnit owner;
            private SkillActiveState current_state;
            private float all_action_time_ms;
            private float pass_time_ms;
            private float stop_time_ms;
            private float percent = 1f;
            private float action_speed = 1f;
            private float total_cd_time_ms;
            private float skillCastRate = 1f;
            public int Level { get; private set; } = 0;
            protected SkillState() { }
            public static SkillState Alloc(LayerUnit owner, SkillInit sk)
            {
                return owner.ObjectPool.AllocOrCreateAutoRelease<SkillState>(static s => new SkillState()).Init(owner, sk);
            }
            protected SkillState Init(LayerUnit owner, SkillInit sk)
            {
                this.data = sk.Skill;
                this.owner = owner;
                this.all_action_time_ms = Data.TotalActionQueueTimeMS;
                this.total_cd_time_ms = data.CoolDownMS;
                this.pass_time_ms = stop_time_ms = FullCDTimeMS;
                this.pass_time_ms = data.CoolDownMS;
                this.Level = sk.Launch.SkillLevel;
                return this;
            }
            protected override void Disposing()
            {
                this.data = null;
                this.owner = null;
                this.current_state = default;
                this.all_action_time_ms = 0;
                this.pass_time_ms = default;
                this.stop_time_ms = default;
                this.percent = 1f;
                this.action_speed = 1f;
                this.total_cd_time_ms = default;
                this.skillCastRate = 1f;
                this.Level = 0;
            }


            public SkillTemplate Data => data;
            public LayerUnit Owner => owner;
            internal bool TryLaunch(UnitLaunchSkillRequest act)
            {
                if (IsActive &&
                    Owner.MP >= Data.CostMP &&
                    Owner.HP >= Data.CostHP &&
                    (!IsCD))
                {
                    return true;
                }
                return false;
            }

            internal void Launch(UnitLaunchSkillEvent evt)
            {
                this.percent = 0;
                this.pass_time_ms = 0;
                this.CurrentActionID = evt.action_index;
                if (evt.IsCastSpeedUP)
                {
                    this.skillCastRate = evt.SkillCastRate;
                }
                else
                {
                    this.skillCastRate = 1f;
                }
                if (evt.IsActionSpeedUP)
                {
                    this.action_speed = evt.fast_action_rate;
                }
                else
                {
                    this.action_speed = 1f;
                }
                if (evt.IsChangeTotalCDTime)
                {
                    this.total_cd_time_ms = evt.TotalCDTimeMS;
                }
                else
                {
                    this.total_cd_time_ms = ToFullTimeCD();
                }
            }
            public byte NextAction()
            {
                if (Data.IsSingleAction)
                {
                    int action_step = this.CurrentActionID;
                    var action_time = this.FullCDTimeMS;
                    if (Data.IsCoolDownWithAction)
                    {
                        action_time = Data.ActionQueue[action_step % Data.ActionQueue.Count].TotalTimeMS;
                    }
                    // 是放技能时，处于多段攻击连击冷却时间范围
                    if (pass_time_ms - stop_time_ms < Data.SingleActionCoolDownMS || pass_time_ms < (action_time + Data.SingleActionCoolDownMS))
                    {
                        action_step += 1;
                        action_step = (byte)(action_step % Data.ActionQueue.Count);
                        return (byte)action_step;
                    }
                }
                return 0;
            }

            private float ToFullTimeCD()
            {
                if (Data.IsCoolDownWithAction)
                {
                    if (Data.IsSingleAction)
                    {
                        return Data.ActionQueue[CurrentActionID].TotalTimeMS;
                    }
                    else
                    {
                        return all_action_time_ms;
                    }
                }
                return Data.CoolDownMS;
            }
            internal void SetActive(SkillActiveState state)
            {
                this.current_state = state;
            }
            internal void ChangeAction(int step)
            {
                this.CurrentActionID = step;
            }
            internal void ClearCD()
            {
                this.pass_time_ms = FullCDTimeMS;
            }
            internal void DecreaseCD(float ms)
            {
                this.pass_time_ms += ms;
            }
            internal void DecreaseCD_Pct(float pct)
            {
                this.pass_time_ms += (float)(FullCDTimeMS * pct);
            }
            internal void PlayerStop(PlayerSkillStopEvent stop)
            {
                this.stop_time_ms = pass_time_ms;
            }
            internal void Sync(ClientStruct.UnitSkillStatus syn)
            {
                this.Level = syn.SkillLevel;
                this.pass_time_ms = syn.PassTime;
                internal_update();
            }
            internal void Sync(PlayerSkillRefreshEvent syn)
            {
                this.Level = syn.SkillLevel;
                this.pass_time_ms = syn.PassTime;
                this.total_cd_time_ms = syn.Skill.CoolDownMS;
                internal_update();
            }
            internal void TimeChange(ObjectSkillTimeChangedEvent e)
            {
                this.pass_time_ms = e.SkillPassTimeMS;
                this.total_cd_time_ms = e.SkillTotalTimeMS;
                this.skillCastRate = e.SkillCastRate;
                internal_update();
            }
            internal void TimeChange(PlayerSkillTimeChangedEvent e)
            {
                this.pass_time_ms = e.SkillPassTimeMS;
                this.total_cd_time_ms = e.SkillTotalTimeMS;
                this.skillCastRate = e.SkillCastRate;
                internal_update();
            }
            internal void Update(float intervalMS)
            {
                if (!IsActive && IsPauseOnDeactive) { return; }
                this.pass_time_ms += (float)(intervalMS * (skillCastRate * Owner.FastCastRate));
                internal_update();
            }
            internal void SetPassTime(float passTime)
            {
                this.pass_time_ms = passTime;
                internal_update();
            }
            private void internal_update()
            {
                this.percent = Math.Min(pass_time_ms / (float)FullCDTimeMS, 1f);
                if (pass_time_ms >= FullCDTimeMS)
                {
                    this.percent = 1f;
                    this.stop_time_ms = pass_time_ms;
                }
            }
            public float ExpireTimeMS
            {
                get => Math.Max(0, total_cd_time_ms - pass_time_ms);
            }
            public float FullCDTimeMS
            {
                get
                {
                    return total_cd_time_ms;
                }
            }
            public bool IsCD
            {
                get
                {
                    if (Data.IsCoolDownWithAction)
                    {
                        return false;
                    }
                    return (percent < 1f);
                }
            }
            public float CDAmount
            {
                get
                {
                    if (Data.IsCoolDownWithAction)
                    {
                        return 1f;
                    }
                    return percent;
                }
            }
            public float ActionSpeed { get { return action_speed; } }
            public float PassTimeMS { get { return pass_time_ms; } }
            public float StopTimeMS { get { return stop_time_ms; } }
            public int CurrentActionID { get; private set; }
            public SkillActiveState ActiveState { get { return current_state; } }
            public bool IsActive { get { return current_state == SkillActiveState.Active || current_state == SkillActiveState.ActiveAndHide; } }
            public bool IsPauseOnDeactive { get { return current_state == SkillActiveState.DeactiveAndPause; } }
        }

        public List<SkillState> GetSkillStatus()
        {
            return new List<SkillState>(mSkillStatus.Skills);
        }
        public void GetSkillStatus(IList<SkillState> ret)
        {
            ret.AddRange(mSkillStatus.Skills);
        }

        public SkillState GetSkillState(int templateID)
        {
            return mSkillStatus.Get(templateID);
        }

        public SkillState GetSkillStateByIndex(int skillIndex)
        {
            if (skillIndex >= 0 && skillIndex < SkillCount)
            {
                return mSkillStatus.GetAt(skillIndex);
            }
            return null;
        }


        #endregion
        //---------------------------------------------------------------------------
        // skill action
        //---------------------------------------------------------------------------
        #region SkillAction

        /// <summary>
        /// 是否正在吟唱
        /// </summary>
        public bool IsChanttingSkill
        {
            get { return mChantingSkill != null; }
        }
        public float ChantingSkillPassMS
        {
            get { return (mChantingSkill != null) ? (float)mChantingSkill.PassTimeMS : 0; }
        }
        public float ChantingSkillTotalMS
        {
            get { return (mChantingSkill != null) ? (float)mChantingSkill.TotalTimeMS : 0; }
        }
        public float ChantingSkillAmount
        {
            get { return (mChantingSkill != null) ? (ChantingSkillPassMS / (float)ChantingSkillTotalMS) : 0f; }
        }

        public SkillTemplate ChantingSkillData
        {
            get { return (mChantingSkill != null) ? mChantingSkill.Tag : null; }
        }
        public SkillState ChantingSkill
        {
            get { if (mChantingSkill != null) { return GetSkillState(mChantingSkill.Tag.ID); } return null; }
        }
        private TimeExpire<SkillTemplate> mChantingSkill;

        protected virtual void DoUnitChantSkillEvent(UnitChantSkillEvent e)
        {
            var temp = Templates.GetSkill(e.skill_id);
            mChantingSkill = new TimeExpire<SkillTemplate>().Init(e.chant_ms, temp);
            SkillState ss = GetSkillState(e.skill_id);
            if (ss != null)
            {
                if (mOnChantSkill != null)
                {
                    mOnChantSkill.Invoke(this, ss, e.chant_ms);
                }
            }
        }
        protected virtual void doLaunchSkillAction(SkillState ss, UnitLaunchSkillEvent me)
        {
            clearSkillAction();
            PreSetCurrentMainState(UnitActionStatus.Skill, null, me);
            PreSetPos(me.start_pos);
            PreFaceTo(me.start_dir);
            //             mCurrentSkillAction = this.Parent.ActorSyncMode == SyncMode.MoveByClient_PreSkillByClient ?
            //                 UnitPreSkillAction.Alloc(this, ss) :
            //                 UnitForceSkillAction.Alloc(this, ss);
            mCurrentSkillAction?.Dispose();
            var skillAction = mCurrentSkillAction = SlaveFactory.AllocSkillAction(this, ss);
            skillAction.onLaunch(me);
            invokeLaunchSkill(ss, skillAction);
            invokeSkillActionStart(skillAction);
        }
        protected virtual void invokeLaunchSkill(SkillState ss, ISkillAction me)
        {
            if (mOnLaunchSkill != null)
            {
                mOnLaunchSkill.Invoke(this, ss, me);
            }
        }
        protected virtual void invokeSkillActionStart(ISkillAction act)
        {
            if (mOnSkillActionStart != null)
            {
                mOnSkillActionStart.Invoke(this, act);
            }
        }
        public ISkillAction CurrentSkillAction
        {
            get { return mCurrentSkillAction; }
        }
        public UnitActionData CurrentSkillActionData
        {
            get
            {
                if (CurrentSkillAction != null)
                {
                    return CurrentSkillAction.CurrentAction;
                }
                return null;
            }
        }
        public UnitActionData.AttackShape CurrentAttackShape
        {
            get
            {
                if (mCurrentSkillAction is ISkillAction skillAction)
                {
                    var shape = skillAction?.SkillData?.AttackShape;
                    if (skillAction?.CurrentAction?.OverrideAttackShape != null)
                    {
                        shape = skillAction.CurrentAction.OverrideAttackShape;
                    }
                    return shape;
                }
                return default;
            }
        }

        //-------------------------------------------------------------------------------------------
        // unit impl
        protected ISkillAction mCurrentSkillAction;
        protected virtual void updateSkillAction(float intervalMS)
        {
            if (mCurrentSkillAction is ISkillAction skillAction)
            {
                skillAction.onUpdate(intervalMS);
                if (skillAction.IsDone)
                {
                    clearSkillAction();
                }
            }
        }
        protected virtual void clearSkillAction()
        {
            if (mCurrentSkillAction is ISkillAction skillAction)
            {
                skillAction.onStop();
                skillAction.Dispose();
                mCurrentSkillAction = null;
            }
        }

        public abstract class ISkillAction : LayerStatus
        {
            protected BitSet8 current_action_status = new BitSet8();
            public bool IsControlMoveable { get { return current_action_status.Get(0); } protected set { current_action_status.Set(0, value); } }
            public bool IsControlFaceable { get { return current_action_status.Get(1); } protected set { current_action_status.Set(1, value); } }
            public bool IsCancelableBySkill { get { return current_action_status.Get(2); } protected set { current_action_status.Set(2, value); } }
            public bool IsCancelableByMove { get { return current_action_status.Get(3); } protected set { current_action_status.Set(3, value); } }
            public bool IsNoneBlock { get { return current_action_status.Get(4); } protected set { current_action_status.Set(4, value); } }
            public bool IsNoneTouch { get { return current_action_status.Get(5); } protected set { current_action_status.Set(5, value); } }
            public bool IsFaceToTarget { get { return current_action_status.Get(6); } protected set { current_action_status.Set(6, value); } }
            public bool IsInvisible { get { return current_action_status.Get(7); } protected set { current_action_status.Set(7, value); } }

            public abstract SkillTemplate SkillData { get; }
            public abstract UnitLaunchSkillEvent LaunchEvent { get; }
            public abstract bool IsDone { get; }
            public abstract float FastActionRate { get; }
            public abstract byte ActionStepIndex { get; }
            public abstract IReadOnlyList<float> ActionTimeArray { get; }
            public abstract float TotalTimeMS { get; }
            public abstract int CurrentActionIndex { get; }
            public abstract string CurrentActionName { get; }
            public abstract UnitActionData CurrentAction { get; }
            public abstract float ExpirePercent { get; }

            protected ISkillAction() { }
            public abstract void onLaunch(UnitLaunchSkillEvent e);
            public abstract void onUnitSkillActionChangeEvent(UnitSkillActionChangeEvent evt);
            public abstract void onUpdate(float intervalMS);
            public abstract void onStop();

        }
        public class UnitForceSkillAction : ISkillAction
        {
            private LayerUnit ownerUnit;
            private SkillState state;
            private SkillTemplate skill;
            private UnitLaunchSkillEvent launch_event;

            private float current_pass_time = 0;
            private float total_pass_time = 0;
            private float total_time_ms = 0;
            private bool is_done = false;
            private int current_action_index = 0;
            private UnitActionData current_action = null;
            private float current_action_total_time = 0;
            private readonly List<float> action_time_array = new();
            private readonly Queue<UnitActionData> action_queue = new();
            private readonly PopupKeyFrames<UnitActionData.KeyFrame> current_frames = new();

            public LayerZone Parent { get { return ownerUnit.Parent; } }
            public override IReadOnlyList<float> ActionTimeArray { get { return action_time_array; } }
            public override float TotalTimeMS { get { return total_time_ms; } }
            public override float FastActionRate { get { return (launch_event != null) ? launch_event.fast_action_rate : 1f; } }
            public override byte ActionStepIndex { get { return (launch_event != null) ? launch_event.action_index : (byte)0; } }
            public override int CurrentActionIndex { get { return current_action_index; } }
            public override string CurrentActionName { get { return (current_action != null) ? current_action.ActionName : null; } }
            public override UnitActionData CurrentAction { get { return current_action; } }
            public override float ExpirePercent { get { return total_pass_time / TotalTimeMS; } }
            public override SkillTemplate SkillData { get { return skill; } }
            public override UnitLaunchSkillEvent LaunchEvent { get { return launch_event; } }
            public override bool IsDone { get { return is_done; } }

            protected UnitForceSkillAction() { }
            public static UnitForceSkillAction Alloc(LayerUnit actor, SkillState skill)
            {
                var ret = actor.ObjectPool.AllocOrCreateAutoRelease<UnitForceSkillAction>(static s => new UnitForceSkillAction());
                ret.Init(actor, skill);
                return ret;
            }
            protected void Init(LayerUnit actor, SkillState skill)
            {
                this.state = skill;
                this.skill = skill.Data;
                this.ownerUnit = actor;
                this.skill.ActionQueueTimeArray(this.action_time_array);
            }
            protected override void Disposing()
            {
                base.current_action_status.Clear();
                this.ownerUnit = null;
                this.skill = null;
                this.launch_event?.Release();
                this.launch_event = null;

                this.current_pass_time = 0;
                this.total_pass_time = 0;
                this.total_time_ms = 0;
                this.is_done = false;
                this.current_action_index = 0;
                this.current_action = null;
                this.current_action_total_time = 0;
                this.action_time_array.Clear();
                this.action_queue.Clear();
                this.current_frames.Clear();
            }

            public override void onLaunch(UnitLaunchSkillEvent e)
            {
                this.launch_event = e;
                this.launch_event.Retain();
                this.total_pass_time = 0;
                this.current_pass_time = 0;
                this.action_time_array.Clear();
                if (e.action_time_array != null && e.action_time_array.Count > 0)
                {
                    this.total_time_ms = e.TotalActionTimeMS;
                    this.action_time_array.AddRange(e.action_time_array);
                }
                else if (skill.IsSingleAction)
                {
                    this.total_time_ms = skill.ActionQueue[launch_event.action_index].TotalTimeMS;
                    this.action_time_array.Add(TotalTimeMS);
                }
                else
                {
                    this.total_time_ms = skill.TotalActionQueueTimeMS;
                    skill.ActionQueueTimeArray(this.action_time_array);
                }
                if (skill.IsSingleAction)
                {
                    var sa = skill.ActionQueue[launch_event.action_index];
                    this.action_queue.Clear();
                    this.action_queue.Enqueue(sa);
                    this.current_action_index = launch_event.action_index;
                }
                else
                {
                    this.action_queue.Clear();
                    this.action_queue.EnqueueRange(skill.ActionQueue);
                    this.current_action_index = launch_event.action_index;
                }
                this.current_action = null;
                this.nextAction(this.current_action_index);
            }
            public override void onUnitSkillActionChangeEvent(UnitSkillActionChangeEvent e)
            {
                while (this.CurrentActionIndex < e.ActionIndex)
                {
                    if (!nextAction(current_action_index + 1))
                    {
                        this.total_pass_time = TotalTimeMS;
                        this.is_done = true;
                        break;
                    }
                }
                if (!is_done)
                {
                    ownerUnit.invokeSkillActionStart(this);
                }
            }
            public override void onStop()
            {
                this.is_done = true;
                this.total_pass_time = TotalTimeMS;
            }
            public override void onUpdate(float intervalMS)
            {
                {
                    var time_pass = (float)(intervalMS * (this.FastActionRate * ownerUnit.FastActionRate));
                    this.total_pass_time += time_pass;
                    this.total_pass_time = Math.Min(total_pass_time, TotalTimeMS);
                    this.current_pass_time += time_pass;
                }

                if (current_action == null)
                {
                    if (nextAction(current_action_index + 1))
                    {
                        ownerUnit.invokeSkillActionStart(this);
                    }
                }
                if (current_action == null)
                {
                    this.is_done = true;
                    this.total_pass_time = TotalTimeMS;
                }
                else
                {
                    // 关键帧 //
                    using (var kfs = ownerUnit.ObjectPool.AllocList<UnitActionData.KeyFrame>())
                    {
                        if (current_frames.PopKeyFrames(current_pass_time, kfs) > 0)
                        {
                            for (int i = 0; i < kfs.Count; i++)
                            {
                                doKeyFrame(kfs[i]);
                            }
                        }
                    }
                    // 下段动作 //
                    if ((current_action != null) && (current_pass_time >= current_action_total_time))
                    {
                        current_action = null;
                        if (action_queue.Count == 0)
                        {
                            this.is_done = true;
                            this.total_pass_time = TotalTimeMS;
                        }
                    }
                }
            }

            protected bool nextAction(int index)
            {
                if (action_queue != null && action_queue.Count > 0)
                {
                    this.current_action_index = index;
                    this.current_action = action_queue.Dequeue();
                    if (current_action != null)
                    {
                        this.current_frames.AddRange(current_action.KeyFrames);
                        this.current_pass_time = 0;
                        //NewAction//
                        if (launch_event.action_time_array != null)
                        {
                            if (skill.IsSingleAction)
                            {
                                this.current_action_total_time = launch_event.TotalActionTimeMS;
                            }
                            else if (current_action_index < launch_event.action_time_array.Count)
                            {
                                this.current_action_total_time = launch_event.action_time_array[current_action_index];
                            }
                        }
                        else
                        {
                            this.current_action_total_time = current_action.TotalTimeMS;
                        }
                        this.IsCancelableBySkill = current_action.IsCancelableBySkill;
                        this.IsNoneBlock = current_action.IsNoneBlock;
                        this.IsNoneTouch = current_action.IsNoneTouch;
                        this.IsFaceToTarget = current_action.IsFaceToTarget;
                        this.IsCancelableByMove = current_action.IsCancelable;
                        this.IsControlMoveable = current_action.IsControlMoveable;
                        this.IsControlFaceable = current_action.IsControlFaceable;
                        this.IsInvisible = current_action.IsInvisible;
                        return true;
                    }
                }
                current_action = null;
                return false;
            }

            protected void doKeyFrame(UnitActionData.KeyFrame kf)
            {
                // 关键帧改变状态
                if (kf.ChangeStatus != null)
                {
                    this.IsNoneBlock = kf.ChangeStatus.IsNoneBlock;
                    this.IsNoneTouch = kf.ChangeStatus.IsNoneTouch;
                    this.IsFaceToTarget = kf.ChangeStatus.IsFaceToTarget;
                    this.IsCancelableByMove = kf.ChangeStatus.IsCancelable;
                    this.IsCancelableBySkill = kf.ChangeStatus.IsCancelableBySkill;
                    this.IsControlMoveable = kf.ChangeStatus.IsControlMoveable;
                    this.IsControlFaceable = kf.ChangeStatus.IsControlFaceable;
                    this.IsInvisible = kf.ChangeStatus.IsInvisible;
                }
                if (kf.CustomAction != null)
                {
                    ownerUnit.DoKeyFrameCustomAction(kf.CustomAction);
                }
            }

        }


        //---------------------------------------------------------------------------
        public class UnitPreSkillAction : ISkillAction
        {
            //--------------------------------------------------------
            protected LayerUnit ownerUnit;
            protected SkillTemplate skill;
            protected SkillState state;
            protected UnitLaunchSkillEvent launch_event;

            protected float total_pass_time = 0;
            protected float total_time_ms = 0;
            protected float fast_action_rate = 1f;
            protected int current_action_index = 0;
            protected float current_pass_time = 0;
            protected float current_action_total_time = 0;
            protected bool is_done = false;
            protected LayerUnit targetUnit;
            protected Nullable<Vector3> targetPos;
            protected PreSkillStartMove action_move_time;
            protected UnitActionData current_action = null;

            protected readonly List<float> action_time_array = new();
            protected readonly Queue<UnitActionData> action_queue = new Queue<UnitActionData>();
            protected readonly PopupKeyFrames<UnitActionData.KeyFrame> current_frames = new PopupKeyFrames<UnitActionData.KeyFrame>();

            protected Nullable<Vector3> move_to;
            protected double startTime;
            //--------------------------------------------------------

            public static UnitPreSkillAction Alloc(LayerUnit actor, SkillState skill)
            {
                var ret = actor.ObjectPool.AllocAutoRelease<UnitPreSkillAction>();
                ret.Init(actor, skill);
                return ret;
            }
            protected virtual void Init(LayerUnit actor, SkillState state)
            {
                this.state = state;
                this.skill = state.Data;
                this.ownerUnit = actor;
            }
            protected override void Disposing()
            {
                base.current_action_status.Clear();
                this.ownerUnit = null;
                this.skill = null;
                this.state = null;
                this.launch_event?.Release();
                this.launch_event = null;
                this.StopFaceTo?.Release();
                this.StopFaceTo = null;
                this.total_pass_time = 0;
                this.total_time_ms = 0;
                this.fast_action_rate = 1f;
                this.current_action_index = 0;
                this.current_pass_time = 0;
                this.current_action_total_time = 0;
                this.is_done = false;
                this.targetUnit = null;
                this.targetPos = null;
                this.action_move_time?.Release();
                this.action_move_time = null;
                this.current_action = null;

                this.action_time_array.Clear();
                this.action_queue.Clear();
                this.current_frames.Clear();

                this.move_to = null;
                this.startTime = 0;
            }

            //--------------------------------------------------------

            public LayerZone Parent { get { return ownerUnit.Parent; } }
            public UnitAxisAction StopFaceTo { get; protected set; }
            public SkillState State { get { return state; } }

            public override bool IsDone { get { return is_done; } }
            public override float FastActionRate { get { return fast_action_rate; } }
            public override byte ActionStepIndex { get { return (launch_event != null) ? launch_event.action_index : (byte)0; } }
            public override IReadOnlyList<float> ActionTimeArray { get { return action_time_array; } }
            public override float TotalTimeMS { get { return total_time_ms; } }
            public override int CurrentActionIndex { get { return current_action_index; } }
            public override string CurrentActionName { get { if (current_action != null) return current_action.ActionName; return null; } }
            public override UnitActionData CurrentAction { get { return current_action; } }
            public override float ExpirePercent { get { return total_pass_time / (float)TotalTimeMS; } }
            public override SkillTemplate SkillData { get { return skill; } }
            public override UnitLaunchSkillEvent LaunchEvent { get { return launch_event; } }


            public override void onLaunch(UnitLaunchSkillEvent e)
            {
                this.startTime = CUtils.TickTimeMS;
                this.launch_event = e;
                this.launch_event.Retain();

                this.current_pass_time = 0;
                this.fast_action_rate = e.fast_action_rate;
                this.targetUnit = Parent.GetUnit(e.target_object_id);
                if (e.IsSpellTargetPos)
                {
                    this.targetPos = e.spell_target_pos;
                    this.ownerUnit.PreFaceTo(targetPos.Value.X, targetPos.Value.Y);
                }

                if (skill.IsSingleAction)
                {
                    var sa = skill.ActionQueue[e.action_index];
                    this.action_queue.Clear();
                    this.action_queue.Enqueue(sa);
                    this.current_action_index = e.action_index;
                }
                else
                {
                    this.action_queue.Clear();
                    foreach (var action in skill.ActionQueue)
                    {
                        this.action_queue.Enqueue(action);
                    }
                    this.current_action_index = 0;
                }
                this.action_time_array.Clear();
                if (e.action_time_array != null && e.action_time_array.Count > 0)
                {
                    this.total_time_ms = e.TotalActionTimeMS;
                    this.action_time_array.AddRange(e.action_time_array);
                }
                else if (skill.IsSingleAction)
                {
                    this.total_time_ms = skill.ActionQueue[launch_event.action_index].TotalTimeMS;
                    this.action_time_array.Add(TotalTimeMS);
                }
                else
                {
                    this.total_time_ms = skill.TotalActionQueueTimeMS;
                    skill.ActionQueueTimeArray(this.action_time_array);
                }
                nextAction(current_action_index);

            }
            public override void onUnitSkillActionChangeEvent(UnitSkillActionChangeEvent e)
            {
                while (this.CurrentActionIndex < e.ActionIndex)
                {
                    if (!nextAction(current_action_index + 1))
                    {
                        is_done = true;
                        total_pass_time = TotalTimeMS;
                        break;
                    }
                }
                if (!is_done)
                {
                    ownerUnit.invokeSkillActionStart(this);
                }
            }
            public override void onStop()
            {
                var elapsed = CUtils.TickTimeMS - startTime;
                if (action_move_time != null)
                {
                    action_move_time.Stop();
                    action_move_time.Release();
                    action_move_time = null;
                }
                is_done = true;
                total_pass_time = TotalTimeMS;
                if (StopFaceTo != null)
                {
                    ownerUnit.PreFaceTo(StopFaceTo.faceto);
                }
            }
            public override void onUpdate(float intervalMS)
            {
                {
                    var pass_time = (float)(intervalMS * fast_action_rate * ownerUnit.FastActionRate);
                    this.total_pass_time += pass_time;
                    this.total_pass_time = Math.Min(total_pass_time, TotalTimeMS);
                    this.current_pass_time += pass_time;
                }

                if (current_action == null)
                {
                    if (nextAction(current_action_index + 1))
                    {
                        ownerUnit.invokeSkillActionStart(this);
                    }
                }
                if (current_action == null)
                {
                    is_done = true;
                    total_pass_time = TotalTimeMS;
                }
                else
                {
                    // 关键帧 //
                    using (var kfs = ownerUnit.ObjectPool.AllocList<UnitActionData.KeyFrame>())
                    {
                        if (current_frames.PopKeyFrames(current_pass_time, kfs) > 0)
                        {
                            for (int i = 0; i < kfs.Count; i++)
                            {
                                doKeyFrame(kfs[i]);
                            }
                        }
                    }

                    // 移动到目标时，不切换动作 //
                    if (current_action.IsMoveToTarget || current_action.IsJumpToTarget)
                    {
                        // 冲到目标，立即下段 //
                        doMoveToTarget();
                    }
                    else
                    {
                        // 技能位移 //
                        if (action_move_time != null)
                        {
                            doMove(intervalMS);
                        }
                        if (move_to.HasValue)
                        {
                            if (IsControlMoveable)
                            {
                                ownerUnit.PreMoveToTarget(
                                    move_to.Value.X, 
                                    move_to.Value.Y,
                                    move_to.Value.Z * ownerUnit.MoveSpeedSEC,
                                    intervalMS);
                            }
                            else
                            {
                                move_to = null;
                            }
                        }
                        if (current_action.BodyBlockOnAttackRange)
                        {
                            doBodyBlock();
                        }
                        if (IsFaceToTarget)
                        {
                            float tx = 0;
                            float ty = 0;
                            if (getTargetPos(null, out tx, out ty))
                            {
                                if (float.IsNaN(current_action.TurnSpeedSEC) || current_action.TurnSpeedSEC == 0)
                                {
                                    ownerUnit.PreFaceTo(tx, ty);
                                }
                                else
                                {
                                    ownerUnit.PreTurnFaceTo(new Geometry.Vector2(tx, ty), current_action.TurnSpeedSEC, intervalMS);
                                }
                            }
                        }
                        //                         else if (!float.IsNaN(current_action.TurnSpeedSEC))
                        //                         {
                        //                             ownerUnit.PreTurn(MoveHelper.GetTurnSpeed(current_action.TurnSpeedSEC, intervalMS));
                        //                         }
                    }
                    // 下段动作 //
                    if ((current_action != null) && (current_pass_time >= current_action_total_time))
                    {
                        current_action = null;
                        if (action_queue.Count == 0)
                        {
                            is_done = true;
                            total_pass_time = TotalTimeMS;
                        }
                    }
                }
            }
            protected bool nextAction(int index)
            {
                if (action_move_time != null)
                {
                    action_move_time.Stop();
                    action_move_time.Release();
                    action_move_time = null;
                }

                if (action_queue.Count > 0)
                {
                    this.current_action_index = index;
                    //NewAction//
                    this.current_action = action_queue.Dequeue();
                    this.current_frames.AddRange(current_action.KeyFrames);
                    this.current_pass_time = 0;
                    //NewAction//
                    if (launch_event.action_time_array.Count > 0)
                    {
                        if (skill.IsSingleAction)
                        {
                            this.current_action_total_time = launch_event.TotalActionTimeMS;
                        }
                        else if (current_action_index < launch_event.action_time_array.Count)
                        {
                            this.current_action_total_time = launch_event.action_time_array[current_action_index];
                        }
                        else
                        {
                            this.current_action_total_time = current_action.TotalTimeMS;
                        }
                    }
                    else
                    {
                        this.current_action_total_time = current_action.TotalTimeMS;
                    }

                    this.IsCancelableBySkill = current_action.IsCancelableBySkill;
                    this.IsNoneBlock = current_action.IsNoneBlock;
                    this.IsNoneTouch = current_action.IsNoneTouch;
                    this.IsFaceToTarget = current_action.IsFaceToTarget;
                    this.IsCancelableByMove = current_action.IsCancelable;
                    this.IsControlMoveable = current_action.IsControlMoveable;
                    this.IsControlFaceable = current_action.IsControlFaceable;
                    this.IsInvisible = current_action.IsInvisible;

                    if (current_action.IsMoveToTarget)
                    {
                        doMoveToTarget();
                    }
                    else if (current_action.IsJumpToTarget)
                    {
                        doJumpToTarget();
                    }
                    if (this.IsFaceToTarget || launch_event.IsAutoFocusNearTarget)
                    {
                        if (targetUnit != null && targetUnit != ownerUnit)
                        {
                            if (float.IsNaN(current_action.TurnSpeedSEC) || current_action.TurnSpeedSEC == 0)
                            {
                                ownerUnit.PreFaceTo(
                                    targetUnit.X,
                                    targetUnit.Y);
                            }
                        }
                    }
                    return true;
                }
                current_action = null;
                return false;
            }
            protected void doKeyFrame(UnitActionData.KeyFrame kf)
            {
                // 关键帧改变状态
                if (kf.ChangeStatus != null)
                {
                    this.IsNoneBlock = kf.ChangeStatus.IsNoneBlock;
                    this.IsNoneTouch = kf.ChangeStatus.IsNoneTouch;
                    this.IsFaceToTarget = kf.ChangeStatus.IsFaceToTarget;
                    this.IsCancelableByMove = kf.ChangeStatus.IsCancelable;
                    this.IsCancelableBySkill = kf.ChangeStatus.IsCancelableBySkill;
                    this.IsControlMoveable = kf.ChangeStatus.IsControlMoveable;
                    this.IsControlFaceable = kf.ChangeStatus.IsControlFaceable;
                    this.IsInvisible = kf.ChangeStatus.IsInvisible;
                }
                // 如果关键帧绑定单位位移
                if (kf.Move != null)
                {
                    StartMove action_move = kf.Move;
                    this.action_move_time = ownerUnit.PreSkillMove(
                        this.ownerUnit.Direction + action_move.Direction,
                        action_move.RotateSpeedSEC,
                        action_move.KeepTimeMS,
                        action_move.SpeedSEC,
                        action_move.SpeedAdd,
                        action_move.SpeedAcc,
                        action_move.ZSpeedSEC,
                        action_move.OverrideGravity,
                        action_move.IsNoneTouch);
                    //this.action_move_time.SetMoveTarget(this.targetUnit, true, SkillData.AttackBodyTouchRange);
                    ownerUnit.PreFaceTo(action_move_time.StartDirection);
                    this.action_move_time.Retain();
                    if (this.targetUnit != null && this.ownerUnit != targetUnit && !action_move.IsNoneTouch)
                    {
                        this.action_move_time.SetBlockTarget(this.targetUnit);
                    }
                }
                if (kf.CustomAction != null)
                {
                    ownerUnit.DoKeyFrameCustomAction(kf.CustomAction);
                }
            }
            protected void doMove(float intervalMS)
            {
                action_move_time.IsNoneTouch = this.IsNoneTouch;
                if (action_move_time.IsEnd)
                {
                    this.action_move_time.Release();
                    this.action_move_time = null;
                }
            }

            //             public void setFaceTo(Vector2 face_to)
            //             {
            //                 if (IsControlFaceable)
            //                 {
            //                     ownerUnit.PreFaceTo(face_to.x, face_to.y);
            //                 }
            //                 this.StopFaceTo = new UnitAxisAction() {  faceto = face_to};
            //             }

            /// <summary>
            /// 防止技能位移导致单位重合
            /// </summary>
            private void doBodyBlock()
            {
                ownerUnit.PreElasticOtherObjects();
            }

            /// <summary>
            /// 移动到目标面前
            /// </summary>
            private bool doMoveToTarget()
            {
                //Force Sync from server//
                return false;
            }
            private bool doJumpToTarget()
            {
                //ownerUnit.Z = MoveHelper.CalulateParabolicHeight(current_action.JumpToTargetHeightZ, current_action.TotalTimeMS, this.current_pass_time);
                return false;
            }

            public bool getTargetPos(UnitActionData action, out float x, out float y)
            {
                x = ownerUnit.X;
                y = ownerUnit.Y;

                UnitActionData.TargetPosEnum pos = UnitActionData.TargetPosEnum.Body;
                float offset = 0;
                if (action != null)
                {
                    pos = action.TargetPos;
                    offset = action.TargetOffset;
                }

                if (targetPos != null)
                {
                    x = targetPos.Value.X;
                    y = targetPos.Value.Y;
                    return true;
                }
                else if (targetUnit != null && targetUnit != ownerUnit)
                {
                    switch (pos)
                    {
                        case UnitActionData.TargetPosEnum.Body:
                            x = targetUnit.X;
                            y = targetUnit.Y;
                            break;
                        case UnitActionData.TargetPosEnum.Face:
                            var angle = MathVector.getDegree(ownerUnit.X, ownerUnit.Y, targetUnit.X, targetUnit.Y);
                            var len = -(ownerUnit.BodyBlockSize + targetUnit.BodyBlockSize + offset);
                            MathVector.movePolar(ref x, ref y, angle, len);
                            break;
                    }
                    return true;
                }
                return false;
            }

        }
        #endregion
        //---------------------------------------------------------------------------

        //---------------------------------------------------------------------------
    }
}
