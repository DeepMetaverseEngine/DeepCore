using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.FuncData;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{

    /// <summary>
    /// 所有常态状态（Buff，技能，被动系）
    /// </summary>
    partial class InstanceUnit
    {
        public virtual bool EnableSyncSkill { get => false; }
        //-----------------------------------------------------------------------------------------------------//

        //-----------------------------------------------------------------------------------------------
        #region _技能状态类_

        public PlayerSkillChangedEvent AllocSkillEvent()
        {
            var evt = ObjectPool.Alloc<PlayerSkillChangedEvent>().Init(ID);
            if (mDefaultSkill != null)
            {
                evt.baseSkill = new SkillInit()
                {
                    Skill = mDefaultSkill.Data,
                    Launch = mDefaultSkill.LaunchSkill
                };
            }
            using (var skills = AllocSkillsList())
            {
                foreach (EquipSkill sk in skills)
                {
                    if (mDefaultSkill == null || sk.Data.ID != mDefaultSkill.ID)
                    {
                        evt.skills.Add(new SkillInit()
                        {
                            Skill = sk.Data,
                            Launch = sk.LaunchSkill
                        });
                    }
                }
            }
            return evt;
        }
        public void GetCurrentSkillStatus(IList<ClientStruct.UnitSkillStatus> ret)
        {
            int i = 0;
            using (var skills = AllocSkillsList())
            {
                foreach (EquipSkill sk in skills)
                {
                    ret.Add(new ClientStruct.UnitSkillStatus()
                    {
                        SkillTemplateID = (sk.Data.ID),
                        SkillLevel = sk.Level,
                        PassTime = (sk.PassTime),
                    });
                    i++;
                }
            }
        }
        /// <summary>
        /// 初始化技能可用性
        /// </summary>
        public PlayerSkillActiveChangedEvent AllocSyncSkillActives()
        {
            var mSyncSkillActives = ObjectPool.Alloc<PlayerSkillActiveChangedEvent>().Init(this.ID);
            using (var skills = AllocSkillsList())
            {
                foreach (EquipSkill st in skills)
                {
                    PlayerSkillActiveChangedEvent.State sat = new PlayerSkillActiveChangedEvent.State();
                    sat.SkillTemplateID = st.ID;
                    sat.ST = st.ActiveState;
                    mSyncSkillActives.Skills.Add(sat);
                }
            }
            return mSyncSkillActives;
        }

        private SkillMap mSkillStatus = new SkillMap();
        private EquipSkill mDefaultSkill;
        private List<SkillTemplate> mAllSkills = new List<SkillTemplate>();

        public int SkillCount => mAllSkills.Count;

        private bool mSyncSkillActivesChanged = false;

        private void updateSyncSkillActives()
        {
            if (mSyncSkillActivesChanged)
            {
                mSyncSkillActivesChanged = false;
                if (EnableSyncSkill)
                {
                    PostEvent(AllocSyncSkillActives());
                }
            }
        }

        private bool DoTryAddSkill(ref SkillTemplate sk)
        {
            bool ret = true;
            if (OnTryAddSkill != null)
            {
                foreach (TryAddSkill tryadd in OnTryAddSkill.GetInvocationList())
                {
                    if (!tryadd.Invoke(this, ref sk))
                    {
                        ret = false;
                    }
                }
            }
            return ret;
        }

        protected virtual void InitSkills()
        {
            if (ASkill)
            {
                this.InitSkills(ASkill.BaseSkillID, ASkill.Skills);
            }
        }
        /// <summary>
        /// 重置当前单位技能，当前单位技能重置为单位模板指定技能
        /// </summary>
        public void ResetSkills()
        {
            if (ASkill && Parent.Formula.TryResetSkill(this))
            {
                InitSkills(ASkill.BaseSkillID, ASkill.Skills);
            }
        }
        public void ClearSkills()
        {
            InitSkills(default(LaunchSkill), null);
        }
        /// <summary>
        /// 设置当前单位技能
        /// </summary>
        /// <param name="baseSkill"></param>
        /// <param name="skills"></param>
        public virtual void InitSkills(LaunchSkill baseSkill, IEnumerable<LaunchSkill> skills = null)
        {
            using (var exist = AllocSkillsList())
            {
                mSyncSkillActivesChanged = true;
                mDefaultSkill = null;
                mSkillStatus.Clear();
                mAllSkills.Clear();
                foreach (EquipSkill st in exist)
                {
                    OnSkillRemoved?.Invoke(this, st);
                }

                if (baseSkill != null)
                {
                    SkillTemplate st = Cartridge.GetSkill(baseSkill.SkillID);
                    if (DoTryAddSkill(ref st))
                    {
                        if (st != null && !mSkillStatus.ContainsKey(baseSkill.SkillID))
                        {
                            mDefaultSkill = Zone.CreateUnitSkillState(this, st, baseSkill);//new SkillState(st, baseSkill, this);
                            mAllSkills.Add(st);
                            mSkillStatus.Add(mDefaultSkill);
                            OnSkillAdded?.Invoke(this, mDefaultSkill);
                        }
                    }
                }
                if (skills != null)
                {
                    foreach (LaunchSkill lsk in skills)
                    {
                        SkillTemplate stt = Cartridge.GetSkill(lsk.SkillID);
                        if (DoTryAddSkill(ref stt))
                        {
                            if (stt != null && !mSkillStatus.ContainsKey(lsk.SkillID))
                            {
                                EquipSkill sk = Zone.CreateUnitSkillState(this, stt, lsk);
                                mAllSkills.Add(stt);
                                mSkillStatus.Add(sk);
                                OnSkillAdded?.Invoke(this, sk);
                            }
                        }
                    }
                }
                if (IsInZone && EnableSyncSkill)
                {
                    Parent.PostObjectEvent(this, AllocSkillEvent());
                }
                OnSkillChanged?.Invoke(this, mDefaultSkill, mSkillStatus.SkillsMap);
            }
        }
        /// <summary>
        /// 设置当前单位技能
        /// </summary>
        /// <param name="baseSkill"></param>
        /// <param name="skills"></param>
        public void InitSkills(SkillTemplate baseSkill, IEnumerable<SkillTemplate> skills = null)
        {
            //List<EquipSkill> exist = new List<EquipSkill>(mSkillStatus.Skills);
            using (var exist = AllocSkillsList())
            {
                mSyncSkillActivesChanged = true;
                mDefaultSkill = null;
                foreach (EquipSkill st in exist)
                {
                    OnSkillRemoved?.Invoke(this, st);
                }
                mSkillStatus.Clear();
                mAllSkills.Clear();

                if (baseSkill != null)
                {
                    if (DoTryAddSkill(ref baseSkill))
                    {
                        if (baseSkill != null && !mSkillStatus.ContainsKey(baseSkill.ID))
                        {
                            mDefaultSkill = Zone.CreateUnitSkillState(this, baseSkill, new LaunchSkill(baseSkill.ID));
                            mAllSkills.Add(baseSkill);
                            mSkillStatus.Add(mDefaultSkill);
                            OnSkillAdded?.Invoke(this, mDefaultSkill);
                        }
                    }
                }
                if (skills != null)
                {
                    foreach (SkillTemplate ssk in skills)
                    {
                        SkillTemplate stt = ssk;
                        if (DoTryAddSkill(ref stt))
                        {
                            if (stt != null && !mSkillStatus.ContainsKey(stt.ID))
                            {
                                EquipSkill state = Zone.CreateUnitSkillState(this, stt, new LaunchSkill(stt.ID));
                                mAllSkills.Add(stt);
                                mSkillStatus.Add(state);
                                OnSkillAdded?.Invoke(this, state);
                            }
                        }
                    }
                }
                if (IsInZone && EnableSyncSkill)
                {
                    Parent.PostObjectEvent(this, AllocSkillEvent());
                }
                OnSkillChanged?.Invoke(this, mDefaultSkill, mSkillStatus.SkillsMap);
            }
        }
        public virtual EquipSkill LearnSkill(UnitCartridge cartridge, CardTemplate card, SkillTemplate st)
        {
            if (mSkillStatus.ContainsKey(st.ID))
            {
                return null;
            }
            return AddSkill(st, 0, false);
        }
        public EquipSkill AddSkill(SkillTemplate st, bool is_default = false)
        {
            return AddSkill(st, 0, is_default);
        }
        //扩一下
        public EquipSkill AddSkill(SkillTemplate st, int level, bool is_default = false)
        {
            if (DoTryAddSkill(ref st))
            {
                if (st != null && !mSkillStatus.ContainsKey(st.ID))
                {
                    mSyncSkillActivesChanged = true;
                    LaunchSkill ls = new LaunchSkill(st.ID)
                    {
                        SkillLevel = level
                    };
                    EquipSkill state = Zone.CreateUnitSkillState(this, st, ls);
                    mAllSkills.Add(st);
                    mSkillStatus.Add(state);
                    if (is_default)
                    {
                        mDefaultSkill = state;
                    }
                    if (IsInZone) { Parent.PostObjectEvent(this, ObjectPool.Alloc<PlayerSkillAddedEvent>().Init(ID, new SkillInit() { Skill = st, Launch = ls }, is_default)); }
                    OnSkillAdded?.Invoke(this, state);
                    OnSkillChanged?.Invoke(this, mDefaultSkill, mSkillStatus.SkillsMap);
                    return state;
                }
            }
            return null;
        }
        public bool RemoveSkill(int skillTemplateID)
        {
            EquipSkill state = mSkillStatus.RemoveByKey(skillTemplateID);
            if (state != null)
            {
                try
                {
                    mSyncSkillActivesChanged = true;
                    mAllSkills.RemoveAll((t) => { return (t.ID == skillTemplateID); });
                    if (state == mDefaultSkill)
                    {
                        mDefaultSkill = null;
                    }
                    if (IsInZone) { Parent.PostObjectEvent(this, ObjectPool.Alloc<PlayerSkillRemovedEvent>().Init(ID, skillTemplateID)); }
                    OnSkillRemoved?.Invoke(this, state);
                    OnSkillChanged?.Invoke(this, mDefaultSkill, mSkillStatus.SkillsMap);
                }
                finally
                {
                    state.Dispose();
                }
            }
            return state != null;
        }

        private void BuffActiveSkill(bool start, List<LaunchSkill> lt, List<int> keepSkill)
        {
            List<int> list = new List<int>();

            if (lt != null && lt.Count > 0)
            {
                for (int i = 0; i < lt.Count; i++)
                {
                    list.Add(lt[i].SkillID);
                }
            }

            if (keepSkill == null)
                keepSkill = new List<int>();

            int tempID = 0;

            for (int i = 0; i < mAllSkills.Count; i++)
            {
                tempID = mAllSkills[i].ID;

                if (list.Contains(tempID))
                    SetSkillActive(tempID, start);
                else if (keepSkill.Contains(tempID))
                    continue;
                else
                    SetSkillActive(tempID, !start);
            }
        }

        public SkillTemplate DefaultSkill
        {
            get
            {
                if (mDefaultSkill != null)
                {
                    return mDefaultSkill.Data;
                }
                return null;
            }
        }
        public EquipSkill DefaultSkillState
        {
            get
            {
                return mDefaultSkill;
            }
        }
        public IReadOnlyDictionary<int, EquipSkill> SkillStatus
        {
            get
            {
                return mSkillStatus.SkillsMap;
            }
        }

        public SkillTemplate GetSkill(int skillID)
        {
            EquipSkill st = mSkillStatus.Get(skillID);
            if (st != null)
            {
                return st.Data;
            }
            return null;
        }

        public EquipSkill GetSkillState(int skillID)
        {
            EquipSkill st = mSkillStatus.Get(skillID);
            if (st != null)
            {
                return st;
            }
            return null;
        }

        /// <summary>
        /// 获得当前随机可释放的技能
        /// </summary>
        /// <param name="expectTarget"></param>
        /// <returns></returns>
        public virtual EquipSkill GetRandomLaunchableExpectSkill(SkillTemplate.CastTarget expectTarget, bool checkAutoLaunch = true)
        {
            int rand = RandomN.Next(0, mAllSkills.Count);
            for (int si = mAllSkills.Count - 1; si >= 0; --si)
            {
                SkillTemplate st = mAllSkills[CMath.CycNum(rand, si, mAllSkills.Count)];
                if (st.ExpectTarget == expectTarget)
                {
                    EquipSkill sst = mSkillStatus.Get(st.ID);
                    if (sst != null && sst.CheckAutoLaunch(checkAutoLaunch) && sst.TryLaunch())
                    {
                        return sst;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获得当前随机可释放的技能
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expectTarget"></param>
        /// <param name="reason"></param>
        /// <param name="checkRange"></param>
        /// <returns></returns>
        public virtual EquipSkill GetRandomLaunchableExpectSkill(InstanceUnit target, SkillTemplate.CastTarget expectTarget, AttackReason reason = AttackReason.Tracing, bool checkRange = false, bool checkAutoLaunch = true)
        {
            int rand = RandomN.Next(0, mAllSkills.Count);
            for (int si = mAllSkills.Count - 1; si >= 0; --si)
            {
                SkillTemplate st = mAllSkills[CMath.CycNum(rand, si, mAllSkills.Count)];
                if (st.ExpectTarget == expectTarget)
                {
                    EquipSkill sst = mSkillStatus.Get(st.ID);
                    if (sst != null && sst.CheckAutoLaunch(checkAutoLaunch) && sst.TryLaunch() && Parent.Formula.IsAttackableBySkill(this, target, sst, reason))
                    {
                        if (!checkRange || sst.CheckTargetRange(target))
                        {
                            return sst;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获得当前随机可释放的技能
        /// </summary>
        /// <returns></returns>
        public virtual EquipSkill GetRandomLaunchableSkill()
        {
            int rand = RandomN.Next(0, mAllSkills.Count);
            for (int si = mAllSkills.Count - 1; si >= 0; --si)
            {
                SkillTemplate st = mAllSkills[CMath.CycNum(rand, si, mAllSkills.Count)];
                EquipSkill sst = mSkillStatus.Get(st.ID);
                if (sst != null && sst.TryLaunch())
                {
                    return sst;
                }
            }
            return null;
        }
        public void GetKeepSkills(List<int> keeps, List<LaunchSkill> ret)
        {
            using (var skills = AllocSkillsList())
            {
                foreach (EquipSkill st in skills)
                {
                    if (keeps.Contains(st.ID))
                    {
                        ret.Add(st.LaunchSkill);
                    }
                }
            }
        }
        private void updateSkills()
        {
            var intervalMS = Parent.UpdateIntervalMS;
            for (int i = 0; i < mSkillStatus.Count; i++)
            {
                EquipSkill st = mSkillStatus.GetAt(i);
                st.Update(intervalMS);
            }
        }
        protected void cleanSkills()
        {
            this.mSkillStatus.Clear();
        }

        public AbstractCollectionPool.AutoReleaseList<EquipSkill> AllocSkillsList()
        {
            var ret = this.ObjectPool.AllocList<EquipSkill>();
            mSkillStatus.ForEachSkills(ret, static (ret, sk) => { ret.Add(sk); return false; });
            return ret;
        }
        public EquipSkill ForEachSkills<ST>(ST st, ForEachPredicate<ST, EquipSkill> action)
        {
            return mSkillStatus.ForEachSkills<ST>(st, action);
        }
        public void ForEachSkills<ST>(ST st, ForEachAction<ST, EquipSkill> action)
        {
            mSkillStatus.ForEachSkills<ST>(st, action);
        }
        // Skills
        class SkillMap
        {
            private HashMap<int, EquipSkill> Map = new HashMap<int, EquipSkill>();
            private List<EquipSkill> For = new List<EquipSkill>();
            public int Count { get { return For.Count; } }
            public IReadOnlyDictionary<int, EquipSkill> SkillsMap { get { return Map; } }
            public EquipSkill ForEachSkills<ST>(ST st, ForEachPredicate<ST, EquipSkill> action)
            {
                for (int i = 0; i < For.Count; i++)
                {
                    var sk = For[i];
                    if (sk != null && action(st, sk))
                    {
                        return sk;
                    }
                }
                return default;
            }
            public void ForEachSkills<ST>(ST st, ForEachAction<ST, EquipSkill> action)
            {
                for (int i = 0; i < For.Count; i++)
                {
                    var sk = For[i];
                    if (sk != null)
                    {
                        action(st, sk);
                    }
                }
            }
            public EquipSkill GetAt(int i)
            {
                return For[i];
            }
            public EquipSkill Get(int id)
            {
                return Map.Get(id);
            }
            public bool Add(EquipSkill state)
            {
                if (!Map.ContainsKey(state.ID))
                {
                    Map.Put(state.ID, state);
                    For.Add(state);
                    return true;
                }
                return false;
            }
            public EquipSkill RemoveByKey(int id)
            {
                EquipSkill ret = Map.RemoveByKey(id);
                if (ret != null)
                {
                    For.Remove(ret);
                }
                return ret;
            }
            public bool ContainsKey(int id)
            {
                return Map.ContainsKey(id);
            }
            public void Clear()
            {
                for (int i = 0; i < For.Count; i++)
                {
                    For[i].Dispose();
                }
                Map.Clear();
                For.Clear();
            }
        }

        public class EquipSkill : InstanceStatus
        {
            private SkillActiveState current_state = SkillActiveState.Active;
            private float total_cd_time;
            private float fastCastRate = 1f;
            private float fastActionRate = 1f;
            /// <summary>
            /// 从技能开始时的逝去时间
            /// </summary>
            private float pass_time;
            /// <summary>
            /// 最后CD完成时间
            /// </summary>
            private float stop_time;
            /// <summary>
            /// 如果是多段攻击，记录段数
            /// </summary>
            private byte action_step;
            /// <summary>
            /// 多段攻击自动下段
            /// </summary>
            private bool auto_increase_action_step = true;
            /// <summary>
            /// 当前是否在冷却
            /// </summary>
            private bool is_cd = false;
            /// <summary>
            /// 当前释放技能时的状态机
            /// </summary>
            private StateSkill state;

            private bool increase_action_step_without_cool_down = false;
            private InstanceUnit owner;
            private SkillTemplate data;
            private LaunchSkill launchSkill;
            private CustomUnitEventTriggerCollection _bindEvent;
            private object tag;
            public int Level { get; set; }
            protected EquipSkill() { }
            public static EquipSkill Alloc(InstanceUnit owner, SkillTemplate data, LaunchSkill skill)
            {
                return owner.ObjectPool.AllocOrCreateAutoRelease<EquipSkill>(static s => new EquipSkill()).Init(owner, data, skill);
                //如果单位死亡，则可能分配给其他单位
                //发射一发子弹后自己死了，被销毁，则子弹还携带数据在继续跑，可能导致命中后，该对象被其他单位复用。
                //return new EquipSkill().Init(owner, data, skill);
            }
            protected virtual EquipSkill Init(InstanceUnit owner, SkillTemplate data, LaunchSkill skill)
            {
                this.owner = owner;
                this.data = data;
                this.launchSkill = skill;
                this.pass_time = int.MaxValue / 2;
                this.Level = skill.SkillLevel;
                this.RefreshData(data);
                this._bindEvent = owner.BindCustomUnitEvent(this.data);
                return this;
            }
            public void RefreshData(SkillTemplate _data)
            {
                var old_is_original = this.data.IsOriginal;
                var post = (this.data != _data);
                this.data = _data;
                this.total_cd_time = Math.Max(0, data.CoolDownMS);
                if (this._bindEvent != null)
                {
//                     if (post || old_is_original)
//                     {
//                         this.owner.RemoveCustomEvent(_bindEvent);
//                         this._bindEvent = owner.BindCustomUnitEvent(this.data);
//                     }
//                     else
                    {
                        this._bindEvent.RefreshData(_data);
                    }
                }
                if (post && owner.EnableSyncSkill)
                {
                    owner.Parent.PostObjectEvent(owner, Owner.ObjectPool.Alloc<PlayerSkillRefreshEvent>().Init(owner.ID, this.data, this.Level, this.pass_time));
                }
            }
            protected override void Disposing()
            {
                this.owner.RemoveCustomEvent(_bindEvent);
                this._bindEvent = null;
                this.current_state = SkillActiveState.Active;
                this.total_cd_time = default;
                this.fastCastRate = 1f;
                this.fastActionRate = 1f;
                this.pass_time = default;
                this.stop_time = default;
                this.action_step = default;
                this.auto_increase_action_step = true;
                this.is_cd = false;
                this.state = default;
                this.increase_action_step_without_cool_down = false;
                this.owner = default;
                this.data = default;
                this.launchSkill = default;
                this.tag = default;
            }

            public SkillTemplate Data { get => data; }
            public LaunchSkill LaunchSkill { get => launchSkill; internal set => launchSkill = value; }
            public InstanceUnit Owner { get => owner; }
            public InstanceZone Zone { get { return owner.Parent; } }
            public int ID { get { return Data.ID; } }
            /// <summary>
            /// 当前技能是否在转CD
            /// </summary>
            public bool IsCD { get { return is_cd; } }
            public SkillActiveState ActiveState { get { return current_state; } }
            public bool IsActive { get { return current_state == SkillActiveState.Active || current_state == SkillActiveState.ActiveAndHide; } }
            public bool IsPauseOnDeactive { get { return current_state == SkillActiveState.DeactiveAndPause; } }
            public bool IsAvaliable { get => (!IsCD && IsActive && IsDone); }
            public StateSkill State { get { return state; } }
            /// <summary>
            /// 当前技能是否放完
            /// </summary>
            public bool IsDone
            {
                get
                {
                    if (Data.IsCoolDownWithAction)
                    {
                        if (state == null)
                            return true;
                        if (state.IsDone)
                            return true;
                        if (state.IsCancelableBySkill)
                            return true;
                        return false;
                    }
                    else if (is_cd)
                    {
                        return false;
                    }
                    return true;
                }
            }
            /// <summary>
            /// CD 需要的总时间
            /// </summary>
            public float TotalCDTime { get { return total_cd_time; } }
            public float PassTime { get { return pass_time; } }
            public byte ActionIndex { get { return action_step; } }
            public float CDAmount
            {
                get
                {
                    if (Data.IsCoolDownWithAction)
                    {
                        return 1f;
                    }
                    if (IsCD)
                    {
                        if (total_cd_time > 0)
                        {
                            return pass_time / total_cd_time;
                        }
                    }
                    return 0;
                }
            }
            public object Tag { get => tag; set => tag = value; }
            /// <summary>
            /// CD 加速
            /// </summary>
            /// <summary>
            /// CD 加速
            /// </summary>
            public float FastCastRate
            {
                get => fastCastRate;
                set
                {
                    if (fastCastRate != value)
                    {
                        fastCastRate = value;
                        Owner.PostEvent(Owner.ObjectPool.Alloc<PlayerSkillTimeChangedEvent>().Init(Owner.ID, ID, this.pass_time, this.total_cd_time, this.FastCastRate));
                    }
                }
            }

            /// <summary>
            /// 动作加速
            /// </summary>
            public float FastActionRate { get => fastActionRate; }


            public void SetTotalCD(float timeMS)
            {
                this.total_cd_time = Math.Max(0, timeMS);
            }
            /// <summary>
            /// 检测目标距离
            /// </summary>
            /// <returns></returns>
            public bool CheckTargetRange(InstanceUnit targetUnit, bool isSingle = false)
            {
                if (Data.AttackMustBeInRange)
                {
                    if (Data.AttackShape.IsSingle)//点对点
                    {
                        if (targetUnit != null)
                        {
                            //点对点按距离计算
                            if (Owner.IsInAttackRange(this.Data, targetUnit))
                            {
                                return true;
                            }
                        }
                        return false;
                    }
                }
                else if (isSingle)
                {
                    if (targetUnit != null)
                    {
                        var rg = Owner.GetSkillAttackRange(this.Data);
                        var cy = new Geometry.VoxelCylinder(Owner.Position, rg, Owner.BodyHeight);
                        if (Collider.Cylinder_Touch_HitBody(this, targetUnit, in cy))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                return true;
            }


            public virtual bool CheckAutoLaunch(bool checkAuto = true)
            {
                return !checkAuto || launchSkill.AutoLaunch;
            }
            /// <summary>
            /// 指定技能动作
            /// </summary>
            /// <param name="step"></param>
            public void SetActionIndex(byte step)
            {
                this.action_step = step;
            }

            /// <summary>
            /// 指定技能动作，在动作自动控制多段攻击模式下，只生效一次.
            /// </summary>
            /// <param name="step"></param>
            /// <param name="without_single_action_cooldownMS"></param>
            public void SetActionIndex(byte step, bool without_single_action_cooldownMS)
            {
                this.action_step = step;
                this.increase_action_step_without_cool_down = without_single_action_cooldownMS;
            }

            /// <summary>
            /// 设置是否自动控制多段攻击（如果要手动指定播放技能，则设置为False）
            /// </summary>
            /// <param name="at"></param>
            public void SetAutoIncreaseActionIndex(bool at)
            {
                this.auto_increase_action_step = at;
            }

            internal void ClearActionIndex()
            {
                this.action_step = 0;
            }
            internal void Launch(StateSkill s, TLaunchSkillParam param)
            {
                this.NextAction();
                this.pass_time = 0;
                this.state = s;
                if (!Data.IsCoolDownWithAction)
                {
                    this.is_cd = true;
                }
                //                 this.fastCastRate = Owner.FastCastRate;
                //                 this.fastActionRate = Owner.FastActionRate;
                if (param.OverrideFastCastRate.HasValue)
                {
                    this.fastCastRate = param.OverrideFastCastRate.Value;
                    if (this.fastCastRate <= 0) { throw new Exception("OverrideFastCastRate can not be zero"); }
                }
                if (param.OverrideFastActionRate.HasValue)
                {
                    this.fastActionRate = param.OverrideFastActionRate.Value;
                    if (this.fastActionRate <= 0) { throw new Exception("OverrideFastActionRate can not be zero"); }
                }
            }
            internal void StartCD()
            {
                this.pass_time = 0;
                if (!Data.IsCoolDownWithAction)
                {
                    this.is_cd = true;
                }
                Owner.PostEvent(Owner.ObjectPool.Alloc<PlayerSkillTimeChangedEvent>().Init(Owner.ID, ID, this.pass_time, this.total_cd_time, this.fastCastRate));
            }
            internal void Stop(StateSkill s)
            {
                this.state = null;
                // CD 依靠动作结束
                if (Data.IsCoolDownWithAction)
                {
                    this.is_cd = false;
                    this.stop_time = this.pass_time;
                }
            }
            internal void Update(float intervalMS)
            {
                if (!IsActive && IsPauseOnDeactive)
                {
                    return;
                }
                this.pass_time += (float)(intervalMS * (this.fastCastRate * this.Owner.FastCastRate));
                CheckCDPasstime();
            }
            public virtual bool TryLaunch()
            {
                if (!IsActive)
                {
                    return false;
                }
                if (Owner.__mCurrentMP.Value >= Data.CostMP && Owner.__mCurrentHP.Value >= Data.CostHP)
                {
                    return IsDone;
                }
                return false;
            }
            private byte NextAction()
            {
                if (Data.IsSingleAction && auto_increase_action_step)
                {
                    var action_time = this.TotalCDTime;
                    if (Data.IsCoolDownWithAction)
                    {
                        action_time = Data.ActionQueue[action_step % Data.ActionQueue.Count].TotalTimeMS;
                    }
                    if (increase_action_step_without_cool_down)
                    {
                        this.action_step += 1;
                        this.action_step = (byte)(action_step % Data.ActionQueue.Count);
                    }
                    // 是放技能时，处于多段攻击连击冷却时间范围
                    else if (pass_time - stop_time < Data.SingleActionCoolDownMS || pass_time < (action_time + Data.SingleActionCoolDownMS))
                    {
                        this.action_step += 1;
                        this.action_step = (byte)(action_step % Data.ActionQueue.Count);
                    }
                    else
                    {
                        this.action_step = 0;
                    }
                    //log.Info(string.Format("Skill step {0}", action_step));
                    this.increase_action_step_without_cool_down = false;
                }
                return action_step;
            }

            internal void ClearCD()
            {
                this.pass_time = TotalCDTime;
                CheckCDPasstime();
            }
            internal void DecreaseSkillCD(float timeMS)
            {
                if (!Data.IsCoolDownWithAction)
                {
                    this.pass_time += timeMS;
                }
            }
            internal void DecreaseSkillCD_Pct(float percent)
            {
                if (!Data.IsCoolDownWithAction)
                {
                    this.pass_time += (float)(TotalCDTime * percent);
                }
            }
            internal void SetPassTime(float passtime)
            {
                if (!Data.IsCoolDownWithAction)
                {
                    if (passtime != this.pass_time)
                    {
                        this.pass_time = passtime;
                        Owner.PostEvent(Owner.ObjectPool.Alloc<PlayerSkillTimeChangedEvent>().Init(Owner.ID, ID, this.pass_time, this.total_cd_time, this.FastCastRate));
                        if (pass_time < TotalCDTime)
                        {
                            is_cd = true;
                        }
                    }
                }
            }
            internal bool TrySetActive(SkillActiveState state)
            {
                if (state != current_state)
                {
                    this.current_state = state;
                    this.Owner.mSyncSkillActivesChanged = true;
                    return true;
                }
                return false;
            }

            internal void CheckCDPasstime()
            {
                if (is_cd && !Data.IsCoolDownWithAction)
                {
                    // 技能 CD
                    if (pass_time >= TotalCDTime)
                    {
                        this.is_cd = false;
                        this.stop_time = this.pass_time;
                    }
                }
            }
        }
        /// <summary>
        /// 设置技能可用性
        /// </summary>
        /// <param name="skillTemplateID"></param>
        /// <param name="active"></param>
        /// <param name="pause_on_deactive"></param>
        public void SetSkillActive(int skillTemplateID, bool active, bool pause_on_deactive = false)
        {
            EquipSkill st = GetSkillState(skillTemplateID);
            if (st != null)
            {
                if (active)
                    st.TrySetActive(SkillActiveState.Active);
                else if (pause_on_deactive)
                    st.TrySetActive(SkillActiveState.DeactiveAndPause);
                else
                    st.TrySetActive(SkillActiveState.Deactive);
            }
        }
        public void SetSkillActive(int skillTemplateID, SkillActiveState state)
        {
            EquipSkill st = GetSkillState(skillTemplateID);
            if (st != null)
            {
                st.TrySetActive(state);
            }
        }

        /// <summary>
        /// 清除技能CD
        /// </summary>
        /// <param name="skillTemplateID"></param>
        public void ClearSkillCD(int skillTemplateID)
        {
            EquipSkill ss = GetSkillState(skillTemplateID);
            if (ss != null)
            {
                ss.ClearCD();
                PlayerCDEvent evt = ObjectPool.Alloc<PlayerCDEvent>().Init(ID);
                evt.is_clear = true;
                evt.is_all = false;
                evt.skill_template_id = skillTemplateID;
                Parent.PostObjectEvent(this, evt);
            }
        }
        /// <summary>
        /// 清除所有技能CD
        /// </summary>
        public void ClearAllSkillCD()
        {
            using (var skills = AllocSkillsList())
            {
                foreach (EquipSkill ss in skills)
                {
                    ss.ClearCD();
                }
            }
            PlayerCDEvent evt = ObjectPool.Alloc<PlayerCDEvent>().Init(ID);
            evt.is_clear = true;
            evt.is_all = true;
            Parent.PostObjectEvent(this, evt);
        }
        //百分比cd

        public virtual void EnterAllSkillCD(int pct)
        {
            EnterAllSkillCD(0, pct);
        }

        //指定技能进入cd
        public void EnterAllSkillCD(int skillid, int pct, bool sendNty = false)
        {
            using (var skills = AllocSkillsList())
            {
                foreach (EquipSkill ss in skills)
                {
                    if (skillid == 0 || skillid == ss.ID)
                    {
                        ss.SetPassTime(ss.TotalCDTime * pct / 10000f);
                        if (sendNty)
                        {
                            this.Parent.PostObjectEvent(this, Parent.ObjectPool.Alloc<ObjectSkillTimeChangedEvent>().Init(this.ID, ss.ID, ss.PassTime, ss.TotalCDTime, ss.FastCastRate));
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 减少当前CD固定时间
        /// </summary>
        /// <param name="skillTemplateID"></param>
        /// <param name="updateTimeMS"></param>
        public void DecreaseSkillCD(int skillTemplateID, float updateTimeMS)
        {
            EquipSkill ss = GetSkillState(skillTemplateID);
            if (ss != null)
            {
                ss.DecreaseSkillCD(updateTimeMS);
                PlayerCDEvent evt = ObjectPool.Alloc<PlayerCDEvent>().Init(ID);
                evt.is_decrease_time = true;
                evt.is_all = false;
                evt.decrease_timeMS = updateTimeMS;
                evt.skill_template_id = skillTemplateID;
                Parent.PostObjectEvent(this, evt);
            }
        }
        /// <summary>
        /// 减少当前CD固定时间
        /// </summary>
        /// <param name="updateTimeMS"></param>
        public void DecreaseAllSkillCD(float updateTimeMS)
        {
            using (var skills = AllocSkillsList())
            {
                foreach (EquipSkill ss in skills)
                {
                    ss.DecreaseSkillCD(updateTimeMS);
                }
            }
            PlayerCDEvent evt = ObjectPool.Alloc<PlayerCDEvent>().Init(ID);
            evt.is_decrease_time = true;
            evt.is_all = true;
            evt.decrease_timeMS = updateTimeMS;
            Parent.PostObjectEvent(this, evt);
        }

        public void DecreaseSkillCD_Pct(int skillTemplateID, float percent)
        {
            EquipSkill ss = GetSkillState(skillTemplateID);
            if (ss != null)
            {
                ss.DecreaseSkillCD_Pct(percent);
                PlayerCDEvent evt = ObjectPool.Alloc<PlayerCDEvent>().Init(ID);
                evt.is_decrease_pct = true;
                evt.is_all = false;
                evt.decrease_pct = percent;
                evt.skill_template_id = skillTemplateID;
                Parent.PostObjectEvent(this, evt);
            }
        }
        public void DecreaseAllSkillCD_Pct(float percent)
        {
            mSkillStatus.ForEachSkills(percent, static (percent, ss) =>
            {

                ss.DecreaseSkillCD_Pct(percent);
                return false;
            });
            PlayerCDEvent evt = ObjectPool.Alloc<PlayerCDEvent>().Init(ID);
            evt.is_decrease_pct = true;
            evt.is_all = true;
            evt.decrease_pct = percent;
            Parent.PostObjectEvent(this, evt);
        }
        public void SetSkillPassTime(int skillID, float passTimeMS)
        {
            EquipSkill ss = this.GetSkillState(skillID);
            if (ss != null)
            {
                ss.SetPassTime(passTimeMS);
            }
        }
        public void StartSkillCD(int skillID, float? totalCDTimeMS = null)
        {
            EquipSkill ss = this.GetSkillState(skillID);
            if (ss != null)
            {
                if (totalCDTimeMS.HasValue)
                {
                    ss.SetTotalCD(totalCDTimeMS.Value);
                }
                ss.StartCD();
            }
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------------

    }
}
