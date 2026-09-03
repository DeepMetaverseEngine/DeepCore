using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;

namespace DeepCore.Game3D.Host.Instance
{
    /// <summary>
    /// 中立AI
    /// </summary>
    public class InstanceNature : InstanceUnit
    {
        private VectorObject3 mBasePos = new VectorObject3();
        protected UnitHateComponent mHateComponent;
        private InstanceUnit mTracing;
        override public bool IsNature { get { return true; } }

        public InstanceNature(InstanceZone zone, TAddUnit add)
            : base(zone, add)
        {
            //mHateSystem = HostFactory.CreateHateSystem(this);
        }

        override protected void OnResetAI()
        {
            mTracing = null;
            //mHateSystem.Clear();
            DoSomething();
        }

        public bool followAndAttack(InstanceUnit src, AttackReason reason)
        {
            if (IsNoneSkill)
            {
                return false;
            }
            if (src != null)
            {
                if (Parent.Formula.IsAttackable(this, src, SkillTemplate.CastTarget.Enemy, reason, Info))
                {
                    mHateComponent.HateSystem.Add(src, reason);
                    if ((mTracing == null) || (mTracing != src))
                    {
                        mTracing = src;
                    }
                    if (mTracing != null)
                    {
                        return ChangeState(StateFollowAndAttack.Alloc(this, mTracing));
                    }
                }
                else
                {
                    mHateComponent.HateSystem.Remove(src);
                }
            }
            return false;
        }

        protected override void onAdded()
        {
            this.mHateComponent = this.Components.AddComponent<UnitHateComponent>();
            mBasePos.X = X;
            mBasePos.Y = Y;
            mBasePos.Z = Z;
            base.onAdded();
        }


        protected override void onStateChanged(State old_state, State state)
        {
            if (state is StateIdle)
            {
                followAndAttack(mHateComponent.HateSystem.GetHated(), AttackReason.Tracing);
            }
        }
        protected override void onMoveBlockWithObject(IEntityObject obj)
        {
            base.onMoveBlockWithObject(obj);
            if (obj is InstanceUnit)
            {
                followAndAttack(obj as InstanceUnit, AttackReason.MoveBlocked);
            }
        }
        protected override void onDamaged(InstanceUnit attacker, in TAttackSource attack, in TAttackResult result, long reduceHP)
        {
            mHateComponent.HateSystem.OnHitted(attacker, in attack, in result, reduceHP);
            followAndAttack(attacker, AttackReason.Damaged);
        }



    }
    //--------------------------------------------------------------------------------------------------------

}
