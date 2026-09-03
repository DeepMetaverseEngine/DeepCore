using DeepCore.Game3D.Host.Helper;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Log;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static DeepMetaGame.Data.Template.SkillTemplate;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceUnit
    {

        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 寻路保持距离
        /// </summary>
        /// <returns></returns>
        public float GetKeepRange(InstanceUnit target)
        {
            var bsize = this.BodyBlockSize;
            if (mDefaultSkill != null)
            {
                bsize = Math.Max(bsize, GetSkillAttackRange(mDefaultSkill.Data));
            }
            return bsize;
        }
        public virtual float GetSkillAttackRange(SkillTemplate skill)
        {
            return (BodyBlockSize + skill.AttackRange * BodyScale);
        }
        public virtual float GetSkillAttackRange(float range)
        {
            return (BodyBlockSize + range * BodyScale);
        }

        //-----------------------------------------------------------------------------------------------------//
        public bool IsInGuardRange(InstanceZoneObject target)
        {
            if (AGuard)
            {
                var dis = Geometry.Vector3.Distance(this.Position, target.Position);
                return dis - target.BodyHitSize - BodyBlockSize <= this.AGuard.GuardRange;
            }
            return false;
        }
        public bool IsInGuardLimit(InstanceZoneObject target)
        {
            if (AGuard)
            {
                var limit = AGuard.GuardRange + AGuard.GuardRangeLimitAppend;
                var dis = Geometry.Vector3.Distance(this.Position, target.Position);
                return dis - target.BodyHitSize - BodyBlockSize <= limit;
            }
            return false;
        }
        //点对点按距离计算
        public bool IsInAttackRange(SkillTemplate skill, InstanceUnit target)
        {
            var dis = Geometry.Vector3.Distance(this.Position, target.Position);
            var range = GetSkillAttackRange(skill);
            return dis - target.BodyHitSize <= range;
        }

        public virtual void GetFollowRange(
            InstanceUnit targetUnit,
            SkillTemplate expect_skill,
            out float min_distance,
            out float max_distance)
        {
            min_distance = this.BodyBlockSize + targetUnit.BodyBlockSize;
            max_distance = Math.Max(min_distance, this.BodyBlockSize + targetUnit.BodyHitSize);
            if (expect_skill != null)
            {
                float skill_distance = this.GetSkillAttackRange(expect_skill);
                if (expect_skill.AttackFollowRange > 0 && expect_skill.AttackRange > expect_skill.AttackFollowRange)
                {
                    var follow_distance = this.GetSkillAttackRange(expect_skill.AttackFollowRange);
                    min_distance = Math.Max(min_distance, follow_distance + targetUnit.BodyHitSize);
                    max_distance = Math.Max(max_distance, skill_distance + targetUnit.BodyHitSize);
                }
                else if (skill_distance > min_distance)
                {
                    min_distance = skill_distance + targetUnit.BodyHitSize * 0.85f;
                    max_distance = skill_distance + targetUnit.BodyHitSize;
                }
                else
                {
                    min_distance = skill_distance + targetUnit.BodyHitSize * 0.75f;
                    max_distance = skill_distance + targetUnit.BodyHitSize;
                }
            }
        }

        /// <summary>
        /// 判断当前目标在攻击范围内
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public bool IsTargetInSkillRange(SkillTemplate skill, InstanceUnit unit)
        {
            float rg = GetSkillAttackRange(skill);
            float dr = skill.AttackAngle / 2;
            var fan = new Geometry.VoxelFan(Position, rg, this.BodyHeight, this.Direction - dr, this.Direction + dr);
            if (Collider.Fan_Touch_HitBody(this, unit, in fan))
            {
                return true;
            }

            return false;
        }

        public void getSkillAttackableTargets(SkillTemplate skill, List<InstanceUnit> list, AttackReason reason)
        {
            float rg = GetSkillAttackRange(skill);
            var cylinder = new Geometry.VoxelCylinder(this.Position, rg, this.BodyHeight);
            Parent.GetObjectsInCylinder(this, Collider.Cylinder_Touch_HitBody, cylinder, list);
            Parent.GetAttackableUnits(this, list, skill.ExpectTarget, reason, skill);
        }

        public void GetSkillAttackableTargets(float range, CastTarget expectTarget, List<InstanceUnit> list, AttackReason reason)
        {
            float rg = GetSkillAttackRange(range);
            var cylinder = new Geometry.VoxelCylinder(this.Position, rg, this.BodyHeight);
            Parent.GetObjectsInCylinder(this, Collider.Cylinder_Touch_HitBody, cylinder, list);
            Parent.GetAttackableUnits(this, list, expectTarget, reason, null);
        }

        /// <summary>
        /// 获得可用的技能
        /// </summary>
        /// <param name="expect"></param>
        /// <param name="ret"></param>
        public void GetAvailableSkills(SkillTemplate.CastTarget expect, List<EquipSkill> ret)
        {
            ForEachSkills((expect, ret), static (st, sst) =>
            {
                if (sst.Data.ExpectTarget == st.expect && sst.IsAvaliable)
                {
                    st.ret.Add(sst);
                }
            });
        }

        /// <summary>
        /// 获得当前可用的技能
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="checkAutoLaunch">检查是否可自动释放，是:过滤非自动释放的技能</param>
        public void GetAvailableSkills(IList<EquipSkill> ret, bool checkAutoLaunch = false)
        {
            ForEachSkills((checkAutoLaunch, ret), static (st, sst) =>
            {
                if (sst.IsAvaliable)
                {
                    if (sst.CheckAutoLaunch(st.checkAutoLaunch))
                    {
                        st.ret.Add(sst);
                    }
                }
            });
        }

        /// <summary>
        ///  获得可用的技能
        /// </summary>
        /// <param name="expect"></param>
        /// <param name="ret"></param>
        /// <param name="checkAutoLaunch"></param>
        public void GetAvailableSkills(SkillTemplate.CastTarget expect, IList<EquipSkill> ret, bool checkAutoLaunch = false)
        {
            ForEachSkills((expect, ret, checkAutoLaunch), static (st, sst) =>
            {
                if (sst.Data.ExpectTarget == st.expect && sst.IsAvaliable)
                {
                    if (sst.CheckAutoLaunch(st.checkAutoLaunch))
                    {
                        st.ret.Add(sst);
                    }
                }
            });
        }

        /// <summary>
        /// 获得可用的技能
        /// </summary>
        /// <param name="expect"></param>
        /// <returns></returns>
        public EquipSkill GetAvailableSkill(SkillTemplate.CastTarget expect)
        {
            foreach (var sst in mSkillStatus.SkillsMap.Values)
            {
                if (sst != null)
                {
                    if (sst.Data.ExpectTarget == expect && sst.IsAvaliable)
                    {
                        return sst;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获得可用的技能
        /// </summary>
        /// <param name="expect"></param>
        /// <returns></returns>
        public EquipSkill GetAvailableSkill()
        {
            foreach (var sst in mSkillStatus.SkillsMap.Values)
            {
                if (sst != null)
                {
                    if (sst.IsAvaliable)
                    {
                        return sst;
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// 获取当前最适合攻击的目标
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="reason"></param>
        /// <param name="directionChange"></param>
        /// <returns></returns>
        public InstanceUnit getSkillAttackableFirstTarget(SkillTemplate skill, AttackReason reason,
            ref bool directionChange)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                getSkillAttackableTargets(skill, list, reason);
                if (list.Count > 0)
                {
                    float rg = GetSkillAttackRange(skill);
                    float dr = skill.AttackAngle / 2;
                    var fan = new Geometry.VoxelFan(Position, rg, BodyHeight, Direction - dr, Direction + dr);
                    // 检测攻击范围内的单位 //
                    for (int i = 0; i < list.Count; i++)
                    {
                        InstanceUnit u = list[i];
                        if (Collider.Fan_Touch_HitBody(this, u, in fan))
                        {
                            directionChange = false;
                            return u;
                        }
                    }

                    // 优先当前朝向的目标 //
                    dr = CMath.PI_DIV_2;
                    directionChange = true;
                    for (int i = 0; i < list.Count; i++)
                    {
                        InstanceUnit u = list[i];
                        if (Collider.Fan_Touch_HitBody(this, u, in fan))
                        {
                            return u;
                        }
                    }

                    // 最后选取最近的目标 //
                    InstanceUnit min = null;
                    float min_len = float.MaxValue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        InstanceUnit u = list[i];
                        float len = MathVector.getDistanceSquare(u.X, u.Y, X, Y);
                        if (min_len > len)
                        {
                            min_len = len;
                            min = u;
                        }
                    }

                    return min;
                }
            }

            return null;
        }

        public struct TLaunchSkillParam
        {
            public uint TargetUnitID;
            public Geometry.Vector3? SpellTargetPos;
            public bool AutoFocusNearTarget;
            public int SkillID;
            public int SkillLv;
            public int SummonID;
            public double LaunchTimeMS;
            public uint RelatedPetId;
            public string LaunchArgs;
            public ISerializable LaunchTag;
            public bool BlockCurrentSkill;
            /// <summary>
            /// 公式指定CD加速
            /// </summary>
            public float? OverrideFastCastRate = null;
            /// <summary>
            /// 公式指定动作加速
            /// </summary>
            public float? OverrideFastActionRate = null;
            public Action<EquipSkill, StateSkill> over;
            public TLaunchSkillParam(uint targetID = 0)
            {
                this.TargetUnitID = targetID;
                this.SpellTargetPos = null;
                this.AutoFocusNearTarget = false;
                this.SkillID = 0;
                this.SkillLv = 1;
                this.SummonID = 0;
                this.LaunchArgs = null;
                this.LaunchTimeMS = 0;
                this.RelatedPetId = 0;
                this.over = null;
            }
        }
        /*
        public StateSkill LaunchSkill(EquipSkill ss, LaunchSkillParam param)
        {
            if (IsDead) return null;
            this.lastLaunchSkillTarget = Parent.GetUnit(param.TargetUnitID);
            if (this.lastLaunchSkillTarget != null)
            {
                this.CurrentTargetID = lastLaunchSkillTarget.ID;
            }

            if (ss == null) return null;
            StateSkill current = CurrentState as StateSkill;
            if (current != null && (!force && current.SkillData.IsManuallyCancelable))
            {
                //如果当前技能为手动取消//
                if (current.Skill.ID == ss.ID)
                {
                    //判断停止当前技能//
                    current.block();
                }
                //手动取消技能禁止其他技能打断//
                return null;
            }
            //沉默不能释放其他技能//
            if (IsSilent && ss != mDefaultSkill)
            {
                return null;
            }
            if (ss.Data.AttackMustBeInRange)
            {
                if (!ss.checkTargetRange(lastLaunchSkillTarget))
                {
                    return null;
                }
            }
            if (ss.TryLaunch() && Parent.Formula.TryLaunchSkill(this, ss, ref param))
            {
                StateSkill state = new StateSkill(this, ss, param, (st) =>
                {
                    OnOverLaunchSkill(ss, st);
                    Parent.cb_unitLaunchSkill(this, ss);
                });
                if (state.tryLaunch())
                {
                    if (changeState(state))
                    {
                        return state;
                    }
                }
            }

            return null;
        }
        */
        public bool CancelSkill(int skillID)
        {
            if (IsDead) return false;
            StateSkill current = CurrentState as StateSkill;
            if (current != null)
            {
                //如果当前技能为手动取消//
                if (current.Skill.ID == skillID && current.SkillData.IsManuallyCancelable)
                {
                    current.block();
                    return true;
                }
            }
            return false;
        }
        public bool CancelCurrentSkill()
        {
            if (IsDead) return false;
            StateSkill current = CurrentState as StateSkill;
            if (current != null)
            {
                //如果当前技能为手动取消//
                if (current.SkillData.IsManuallyCancelable)
                {
                    current.block();
                    return true;
                }
            }
            return false;
        }

        public EquipSkill LaunchSkill(EquipSkill ss, TLaunchSkillParam param, Action<EquipSkill, StateSkill> over = null)
        {
            if (IsDead) return null;
            this.lastLaunchSkillTarget = Parent.GetUnit(param.TargetUnitID);
            if (this.lastLaunchSkillTarget != null)
            {
                this.CurrentTargetID = lastLaunchSkillTarget.ID;
            }

            if (ss == null) return null;
            StateSkill current = CurrentState as StateSkill;
            if (current != null && param.BlockCurrentSkill)
            {
                //手动取消技能禁止其他技能打断//
                current.block();
            }
            //沉默不能释放其他技能//
            if (IsSilent && ss != mDefaultSkill)
            {
                return null;
            }
            if (ss.Data.AttackMustBeInRange)
            {
                if (!ss.CheckTargetRange(lastLaunchSkillTarget))
                {
                    return null;
                }
            }
            if (cb_TryLaunchSkill(ss, ref param) == false)
            {
                return null;
            }
            if (ss.TryLaunch() && Parent.Formula.TryLaunchSkill(this, ss, ref param))
            {
                param.over = over;
                var state = StateSkill.Alloc(this, ss, param, static (st) =>
                {
                    var unit = st.unit;
                    var ss = st.Skill;
                    var over = st.StartParam.over;
                    unit.OnOverLaunchSkill(ss, st);
                    over?.Invoke(ss, st);
                    unit.Parent.cb_unitLaunchSkill(unit, ss, st);
                });
                if (state.tryLaunch())
                {
                    if (ChangeState(state))
                    {
                        return ss;
                    }
                }
            }
            return null;
        }
        protected virtual void OnOverLaunchSkill(EquipSkill ss, StateSkill state)
        {
            this.AddHP(-ss.Data.CostHP);
            this.AddMP(-ss.Data.CostMP);
        }

        /// <summary>
        /// 释放默认技能
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public EquipSkill LaunchDefaultSkill(TLaunchSkillParam param)
        {
            return LaunchSkill(mDefaultSkill, param);
        }

        /// <summary>
        /// 单位释放技能
        /// </summary>
        /// <param name="skillID"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public EquipSkill LaunchSkill(int skillID, TLaunchSkillParam param)
        {
            EquipSkill skill = GetSkillState(skillID);
            return LaunchSkill(skill, param);
        }

        /// 释放随机技能，一般用于AI
        /// </summary>
        /// <param name="expectTarget"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual EquipSkill LaunchRandomSkill(SkillTemplate.CastTarget expectTarget, TLaunchSkillParam param, bool checkAutoLaunch = true)
        {
            StateSkill current = CurrentState as StateSkill;
            if (current != null && !current.IsCancelableBySkill)
            {
                return null;
            }
            int rand = RandomN.Next(0, mAllSkills.Count);
            for (int si = mAllSkills.Count - 1; si >= 0; --si)
            {
                SkillTemplate st = mAllSkills[CMath.CycNum(rand, si, mAllSkills.Count)];
                if (st.ExpectTarget == expectTarget)
                {
                    EquipSkill sst = mSkillStatus.Get(st.ID);
                    if (sst.CheckAutoLaunch(checkAutoLaunch))
                    {
                        if (LaunchSkill(sst, param) is EquipSkill ss)
                        {
                            return ss;
                        }
                    }
                }
            }
            return null;
        }

        public virtual EquipSkill LaunchRandomSkillForAll(TLaunchSkillParam param, bool checkAutoLaunch = true)
        {
            StateSkill current = CurrentState as StateSkill;
            if (current != null && !current.IsCancelableBySkill)
            {
                return null;
            }
            int rand = RandomN.Next(0, mAllSkills.Count);
            for (int si = mAllSkills.Count - 1; si >= 0; --si)
            {
                SkillTemplate st = mAllSkills[CMath.CycNum(rand, si, mAllSkills.Count)];
                EquipSkill sst = mSkillStatus.Get(st.ID);
                if (sst.CheckAutoLaunch(checkAutoLaunch))
                {
                    if (LaunchSkill(sst, param) is EquipSkill ss)
                    {
                        return ss;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 尝试取消当前技能，打出连击
        /// </summary>
        /// <param name="target"></param>
        /// <param name="autoFocusNearTarget"></param>
        /// <returns></returns>
        public virtual EquipSkill TryLaunchRandomSkillAndCancelCurrentSkill(
            InstanceUnit target,
            bool autoFocusNearTarget = false,
            bool checkAutoLaunch = true)
        {
            StateSkill current = CurrentState as StateSkill;
            if (current != null && !current.IsChanting && current.IsCancelableBySkill)
            {
                var param = new TLaunchSkillParam(target.ID) { AutoFocusNearTarget = autoFocusNearTarget };
                //优先多段攻击//
                if (current.SkillData.IsSingleAction)
                {
                    if (IsTargetInSkillRange(current.SkillData, target))
                    {
                        if (LaunchSkill(current.SkillData.ID, param) is EquipSkill ss)
                        {
                            return ss;
                        }
                    }
                }

                //随机其他技能//
                using (var skills = ObjectPool.AllocList<EquipSkill>(SkillStatus.Values))
                {
                    if (skills.Count > 0)
                    {
                        int rand = RandomN.Next(0, skills.Count);
                        for (int si = skills.Count - 1; si >= 0; --si)
                        {
                            var st = skills[CMath.CycNum(rand, si, mAllSkills.Count)];
                            if (Parent.Formula.IsAttackableBySkill(this, target, st, AttackReason.Attack))
                            {
                                EquipSkill sst = mSkillStatus.Get(st.ID);
                                if (sst.CheckAutoLaunch(checkAutoLaunch) && IsTargetInSkillRange(st.Data, target))
                                {
                                    if (LaunchSkill(sst, param) is EquipSkill ss)
                                    {
                                        return ss;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// 尝试在范围内找到目标释放技能，并进入StateFollowAndAttack，自己走过去打。
        /// </summary>
        /// <param name="range"></param>
        /// <param name="skill_auto_launchable"></param>
        /// <returns></returns>
        public virtual bool TryFollowAndLaunchRandomSkillToTargetInRange(float range, bool skill_auto_launchable = true)
        {
            if (!IsNoneSkill && AGuard)
            {
                using (var skills = AllocSkillsList())
                {
                    CUtils.RandomList(RandomN, skills);
                    using (var list = ObjectPool.AllocList<InstanceUnit>())
                    {
                        var sp = new Geometry.BoundingSphere(Position, AGuard.GuardRange);
                        //随机找个目标施法//
                        Parent.GetObjectsInSphere(this, Collider.Sphere_Touch_Position, sp, list);
                        Parent.ObjectPool.UpdateAndRemove<InstanceUnit>(list, static (InstanceUnit u) => { return !u.IsActive; });
                        if (list.Count == 0)
                        {
                            return false;
                        }

                        CUtils.RandomList(Parent.RandomN, list);
                        foreach (EquipSkill skill in skills)
                        {
                            if (skill.CheckAutoLaunch(skill_auto_launchable) && skill.TryLaunch())
                            {
                                for (int i = 0; i < list.Count; i++)
                                {
                                    InstanceUnit u = list[i];
                                    if (Parent.Formula.IsAttackableBySkill(this, u, skill, AttackReason.Attack))
                                    {
                                        //检测是否有可释放技能//                                    
                                        return ChangeState(StateFollowAndAttack.Alloc(this, u, skill.Data.ExpectTarget));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


    }
}