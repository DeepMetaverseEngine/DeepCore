using DeepCore.Astar;
using DeepCore.Game3D.Host.Helper;
using DeepMetaGame.Data.Misc;
using System;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class PetAIComponent : UnitComponent<InstancePet>
    {
        private float followMin;
        private float followMax;
        private float followLimit;
        new public InstancePet Owner => base.Owner as InstancePet;
        public InstanceUnit Master => this.Owner.Master;
        public float FollowDistanceMin { get { return followMin; } }
        public float FollowDistanceMax { get { return followMax; } }
        public float FollowDistanceLimit { get { return followLimit; } }

        public void SetFollowRange(float min, float maxAdd, float limitAdd)
        {
            if (min > 0)
            {
                this.followMin = Math.Max(min, Owner.Master.BodySize + Owner.BodyBlockSize);
                this.followMax = followMin + maxAdd;
                this.followLimit = followMax + limitAdd;
            }
        }
        public float GetFollowDistanceMin(IPositionObject target)
        {
            return Math.Max(FollowDistanceMin, target.BodySize + Owner.BodyBlockSize);
        }
        public float GetFollowDistanceMax(IPositionObject target)
        {
            return Math.Max(FollowDistanceMax, target.BodySize + Owner.BodyBlockSize);
        }
        public float GetFollowDistanceLimit(IPositionObject target)
        {
            return Math.Max(FollowDistanceLimit, target.BodySize + Owner.BodyBlockSize);
        }
        public bool IntersectsFollowDistanceMin(IPositionObject target)
        {
            return Collider.Intersects(Owner.Position, target.Position, GetFollowDistanceMin(target));
        }
        public bool IntersectsFollowDistanceMax(IPositionObject target)
        {
            return Collider.Intersects(Owner.Position, target.Position, GetFollowDistanceMax(target));
        }
        public bool IntersectsFollowDistanceLimit(IPositionObject target)
        {
            return Collider.Intersects(Owner.Position, target.Position, GetFollowDistanceLimit(target));
        }
        public bool IntersectsGuardRange(IPositionObject target)
        {
            var origin = target.Position;
            var limit = Owner.AGuard.GuardRange;
            var crossed = (!Collider.Intersects(origin, Owner.Position, limit));
            if (crossed)
            {
                return false;
            }
            return true;
        }
        public bool IntersectsGuardRangeLimit(IPositionObject target)
        {
            var origin = target.Position;
            var limit = Owner.AGuard.GuardRange + Owner.AGuard.GuardRangeLimitAppend;
            var crossed = (!Collider.Intersects(origin, Owner.Position, limit));
            if (crossed)
            {
                return false;
            }
            return true;
        }

    }

    public class SimplePetAIComponent : PetAIComponent
    {
        protected override void OnUpdateAI()
        {
            if (!Active) return;
            if (Master != null)
            {
                if (Master.IsDead || !Master.Enable)
                {
                    Owner.Kill(Owner);
                }
                //else if (!CMath.includeRoundPoint(X, Y, CFG.PET_FOLLOW_DISTANCE_LIMIT, mMaster.X, mMaster.Y))
                else if (!IntersectsFollowDistanceLimit(Master))
                {
                    if (this.transportToMaster())
                    {
                        return;
                    }
                }
                //else if (!CMath.includeRoundPoint(X, Y, CFG.PET_FOLLOW_DISTANCE_MAX, mMaster.X, mMaster.Y))
                else if (!IntersectsFollowDistanceMax(Master))
                {
                    if (this.followMaster())
                    {
                        return;
                    }
                }
            }
            if (Owner.CurrentState is StateIdle)
            {
                doSomething2();
            }
        }

        protected bool transportToMaster()
        {

            if ((Master as InstanceUnit).CurrentActionStatus == UnitActionStatus.Jump)
            {
                var destPos = new Geometry.Vector3();
                destPos.X = Master.Position.X;
                destPos.Y = Master.Position.Y;
                destPos.Z = Master.CurrentLayer.Upward;
                Owner.Transport(destPos);
                Owner.FaceTo(destPos.X, destPos.Y);

            }
            else
            {
                CMath.RandomPosInRound(Owner.Parent.RandomN, Master.Position, FollowDistanceMax, out var dpos);
                if (Owner.IntersectMap && Owner.Parent.TryTouchMap(Owner, dpos, out var layer))
                {
                    dpos.X = Master.X;
                    dpos.Y = Master.Y;
                    dpos.Z = layer.Upward;
                }

                Owner.Transport(dpos);
                Owner.FaceTo(dpos.X, dpos.Y);

            }

            return followMaster();

        }

        protected virtual bool followMaster()
        {
            //Owner.SetMoveSpeed(Master.MoveSpeedSEC);
            if (Owner.CurrentState is StateFollowMaster)
            {
                return true;
            }
            else
            {
                return Owner.ChangeState(StateFollowMaster.Alloc(Owner, Master));
            }
        }


        protected void doSomething2()
        {
            if (!Owner.IsNoneSkill)
            {
                if (Owner.AGuard)
                {
                    using (var list = Owner.Parent.ObjectPool.AllocList<InstanceUnit>())
                    {
                        var sp = new Geometry.BoundingSphere(Owner.Position, Owner.AGuard.GuardRange);
                        //随机找个目标施法//
                        Owner.Parent.GetObjectsInSphere(this, Collider.Sphere_Touch_Position, sp, list);
                        Owner.Parent.ObjectPool.UpdateAndRemove<InstanceUnit>(list, static (InstanceUnit u) =>
                        {
                            return !u.IsActive;
                        });
                        CUtils.RandomList(Owner.Parent.RandomN, list);
                        foreach (EquipSkill skill in Owner.SkillStatus.Values)
                        {
                            if (skill.CheckAutoLaunch(true) && skill.TryLaunch())
                            {
                                foreach (InstanceUnit u in list)
                                {
                                    if (Owner.Parent.Formula.IsAttackableBySkill(Owner, u, skill, AttackReason.Look))
                                    {
                                        //检测是否有可释放技能//
                                        if (tryAutoLaunch(skill, u))
                                        {
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual bool tryAutoLaunch(EquipSkill st, InstanceUnit target)
        {
            if (st != null && st.CheckAutoLaunch())
            {
                Owner.ChangeState(StateFollowAndAttack.Alloc(Owner, target, st.Data.ExpectTarget));
                return true;
            }
            return false;
        }
        //         protected override void LogDamage(InstanceUnit attacker, int reduceHP)
        //         {
        //             base.LogDamage(attacker, reduceHP);
        //         }
        //         protected override IUnitStatistic CreateUnitStatistic()
        //         {
        //             return base.CreateUnitStatistic();
        //         }
        //         public class PetUnitStatistic : UnitStatistic
        //         {
        //             private InstancePet owner;
        //             public PetUnitStatistic(InstancePet owner) : base(owner)
        //             {
        //                 this.owner = owner;
        //             }
        //             public override void onAttack(InstanceUnit target, int reduceHP)
        //             {
        //                 //伤害统计重定向到主人//
        //                 if (owner.Master != null)
        //                 {
        //                     owner.Master.Statistic.onAttack(target, reduceHP);
        //                 }
        //                 else
        //                 {
        //                     base.onAttack(target, reduceHP);
        //                 }
        //             }
        //         }
        //----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 移动状态
        /// </summary>
        public class StateFollowMaster : UnitState<InstancePet>
        {
            // 被追目标
            private IEntityObject master;
            private MoveAI move;
            private Geometry.Vector3 destPos = new Geometry.Vector3();
            private bool moveToPos = false;
            private TimeExpire holdTime;

            public static StateFollowMaster Alloc(InstancePet unit, IEntityObject target)
            {
                var ret = unit.AllocState<StateFollowMaster>();
                ret.master = target;
                ret.holdTime = unit.AllocTimeExpire(unit.CFG.AI_MOVE_NOWAY_HOLD_TIME_MS);
                ret.move = unit.CreateMoveAI();
                return ret;
            }
            protected override void Disposing()
            {
                this.master = default;
                this.move?.Dispose();
                this.move = null;
                this.destPos = default;
                this.moveToPos = false;
                this.holdTime?.Dispose();
                this.holdTime = null;
            }


            public InstancePet pet => unit as InstancePet;
            override public bool OnBlock(State new_state)
            {
                if (new_state is StateDead)
                {
                    return true;
                }
                if (new_state is StateFollowMaster)
                {
                    return true;
                }
                //float r = Math.Max(pet.PetAI.FollowDistanceMin, master.BodySize + unit.BodyBlockSize);
                var r = pet.PetAI.GetFollowDistanceMin(master);
                var targetPos = destPos;

                if (!moveToPos)
                {
                    targetPos = master.Position;
                }

                if (Collider.Intersects(unit.Position, targetPos, r))
                {
                    return true;
                }
                return false;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(unit.GetStartMoveStatus());

                if ((master as InstanceUnit).CurrentActionStatus == UnitActionStatus.Jump)
                {
                    destPos.X = master.Position.X;
                    destPos.Y = master.Position.Y;
                    destPos.Z = master.CurrentLayer.Upward;
                    this.move.FindPath(destPos);
                    moveToPos = true;
                    //Console.WriteLine("master Jump, StateFollowMaster start From {0} To {1},MasterPos={2}",unit.Position,destPos, master.Position);                                   
                }
                else
                {
                    this.move.FindPath(master);
                    moveToPos = false;
                    //Console.WriteLine("StateFollowMaster start From {0} To {1},MasterPos={2}", unit.Position, master.Position, master.Position);
                }



            }

            override protected void OnUpdate()
            {
                //float r = Math.Max(pet.PetAI.FollowDistanceMin, master.BodySize + unit.BodyBlockSize);

                var r = pet.PetAI.GetFollowDistanceMin(master);
                if (moveToPos == false && Collider.Intersects(unit.Position, master.Position, r))
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                    unit.DoSomething();
                }
                else if (moveToPos == true && Collider.Intersects(unit.Position, destPos, r))
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                    unit.DoSomething();
                }
                else
                {
                    move.Update();
                    if (move.IsNoWay)
                    {
                        if (holdTime.IsEnd)
                        {
                            holdTime.Reset(unit.CFG.AI_MOVE_NOWAY_HOLD_TIME_MS);
                        }
                        else if (holdTime.Update(zone.UpdateIntervalMS))
                        {
                            if (moveToPos == true)
                            {
                                unit.Transport(destPos);
                                unit.FaceTo(destPos.X, destPos.Y);
                            }
                            else if (unit.PetAI is SimplePetAIComponent simple)
                            {
                                simple.transportToMaster();
                            }
                            unit.DoSomething();
                        }
                    }
                    else
                    {
                        holdTime.Reset(0);
                    }
                }
            }

            override protected void OnStop()
            {

            }

        }


    }
}
