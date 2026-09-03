using DeepCore.Astar;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepMetaGame.Data.Misc;
using System;

namespace DeepCore.Game3D.Host.Instance
{
    public class InstancePet : InstanceUnit
    {
        protected InstanceUnit mMaster;
        public PetAIComponent PetAI { get; private set; }
        /// <summary>
        /// 获取主人
        /// </summary>
        public virtual InstanceUnit Master
        {
            get { return mMaster; }
            set
            {
                if (value != this.mMaster)
                {
                    this.mMaster = value;
                    this.OnPetChangeMaster?.Invoke(this, value);
                }
            }
        }
        public override bool IntersectMap { get { return BodyBlockSize > 0; } }
        public override bool IntersectObj { get { return false; } }

        public InstancePet(InstanceZone zone, TAddUnit add)
            : base(zone, add)
        {
            this.PetAI.SetFollowRange(
                CFG.PET_FOLLOW_DISTANCE_MIN, 
                CFG.PET_FOLLOW_DISTANCE_MAX_ADD,
                CFG.PET_FOLLOW_DISTANCE_LIMIT_ADD);
        }
        //---------------------------------------------------------------------------------------------------------
        protected virtual PetAIComponent CreateAI() { return this.Components.AddComponentAs<PetAIComponent>(); }
        protected override void Disposing()
        {
            this.mMaster = null;
            base.Disposing();
        }
        protected override void OnInitFormula(InstanceUnitFormula Formula, TAddUnit add)
        {
            this.Master = Summoner;
            this.PetAI = CreateAI();
            base.OnInitFormula(Formula, add);
        }
        //---------------------------------------------------------------------------------------------------------
        #region ToMaster
        protected override bool TryAddExp(ref long value)
        {
            if (Master != null)
            {
                Master.AddExp(value);
                return false;
            }
            return base.TryAddExp(ref value);
        }
        protected override bool TryAddMoney(ref long value)
        {
            if (Master != null)
            {
                Master.AddMoney(value);
                return false;
            }
            return base.TryAddMoney(ref value);
        }
        protected override void LogAttack(InstanceUnit target, long reduceHP)
        {
            if (Master != null)
            {
                Master.Statistic.onAttack(target, reduceHP);
            }
            else
            {
                base.LogAttack(target, reduceHP);
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------
        public delegate void PetChangeMasterHalder(InstancePet pet, InstanceUnit master);
        public event PetChangeMasterHalder OnPetChangeMaster;
        protected override void ClearEvents()
        {
            OnPetChangeMaster = null;
            base.ClearEvents();
        }
    }
}
