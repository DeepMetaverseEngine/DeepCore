using System.Collections.Generic;
using Code.BattleView.UnitActionStatuses;
using DeepCore.GameData.Data;
using DeepCore.GameData.Zone;

namespace Code.BattleView.UnitActionStatuses
{
    public class DefinedActionStatus : ActionStatus
    {
        protected UnitActionDefinitionMap.UnitAction mData;

        protected Queue<UnitActionDefinitionMap.UnitActionKeyFrame> mActionQueue =
            new Queue<UnitActionDefinitionMap.UnitActionKeyFrame>();

        protected UnitActionDefinitionMap.UnitActionKeyFrame mCurrentAction;
        protected int mCurrentPassTime;

        public DefinedActionStatus(DeepCore.GameData.Data.UnitActionStatus status, string key, UnitActionDefinitionMap.UnitAction data)
            : base(status, key)
        {
            this.mData = data;
            if (data.ActionQueue.Count > 0)
            {
                var a = data.ActionQueue[0];
                this.ActionName = a.ActionName;
                this.CrossFade = a.CrossFade;
                this.Speed = a.Speed;
            }
        }

        protected override void OnStart(UnityBattleUnit owner)
        {
            mActionQueue.Clear();
            for (int i = 0; i < mData.ActionQueue.Count; i++)
            {
                this.mActionQueue.Enqueue(mData.ActionQueue[i]);
            }

            NextAction(owner);
        }

        protected override void OnUpdate(UnityBattleUnit owner, float deltaTime)
        {
            this.mCurrentPassTime += (int) (owner.ZoneUnit.Parent.CurrentIntervalMS);
            if (mActionQueue.Count > 0 && mCurrentAction != null)
            {
                if (mCurrentPassTime >= mCurrentAction.TimeMS)
                {
                    NextAction(owner);
                }
            }
        }

        protected virtual void NextAction(UnityBattleUnit owner)
        {
            if (mActionQueue.Count > 0)
            {
                mCurrentAction = mActionQueue.Dequeue();
                if (mCurrentAction != null && !string.IsNullOrEmpty(mCurrentAction.ActionName))
                {
                    this.ActionName = mCurrentAction.ActionName;
                    this.CrossFade = mCurrentAction.CrossFade;
                    this.Speed = mCurrentAction.Speed;
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
            }

            mCurrentPassTime = 0;
        }
    }
}

