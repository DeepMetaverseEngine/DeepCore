using System;

namespace Code.BattleView.UnitActionStatuses
{
    public class ActionStatus : IComparable<ActionStatus>
    {
        protected static ActionStatus gPrevActionStatus { get; private set; }

        private readonly string mKey;
        public DeepCore.GameData.Data.UnitActionStatus UnitActionStatus { get; private set; }

        public string Key
        {
            get { return mKey; }
        }

        public int Priority { get; set; }
        public string ActionName { get; set; }
        public bool CrossFade { get; set; }
        public float Speed { get; set; }

        public ActionStatus(DeepCore.GameData.Data.UnitActionStatus status, string key, string animName = "", bool crossFade = false,
            float speed = 1f)
        {
            this.UnitActionStatus = status;
            this.mKey = key;
            this.ActionName = animName;
            this.CrossFade = true;
            this.Speed = speed;
        }

        public void Start(UnityBattleUnit owner)
        {
            OnStart(owner);
        }

        protected virtual void OnStart(UnityBattleUnit owner)
        {
           if (owner.Anim)
           {
               owner.Anim.speed = Speed;
               if (CrossFade)
               {
                   owner.Anim.CrossFade(ActionName, 0.15f);
               }
               else
               {
                   owner.Anim.Play(ActionName);
               }
           }
        }

        public void Stop(UnityBattleUnit owner)
        {
            gPrevActionStatus = this;
            OnStop(owner);
        }

        protected virtual void OnStop(UnityBattleUnit owner)
        {
        }

        public void Update(UnityBattleUnit owner, float deltaTime)
        {
            OnUpdate(owner, deltaTime);
        }

        protected virtual void OnUpdate(UnityBattleUnit owner, float deltaTime)
        {
        }

        public virtual int CompareTo(ActionStatus other)
        {
            return this.Priority - other.Priority;
        }
    }
}
