using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Unity;
using DeepCore.Unity3D;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Unity;
using DeepMetaGame.Unity.BattleView;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using static DeepCore.Colors;
using static DeepCore.Game3D.Slave.Layer.LayerUnit;
using static DeepMetaGame.Data.Misc.UnitActionDefinitionMap;

namespace DeepGame3D.Unity.BattleView
{

    partial class UnityZoneUnit
    {
        public UnitActionMap DefineActionMap => _actionDefineMap;
        private readonly UnitActionMap _actionDefineMap = new UnitActionMap();
        //private readonly IDictionary<DeepMetaGame.Data.Misc.UnitActionStatus, ActionStack> _actionStatus = new HashMap<DeepMetaGame.Data.Misc.UnitActionStatus, ActionStack>();
        private UnityActionStatus _currentActionStatus;

        public UnityActionStatus CurrentActionStatus
        {
            get { return _currentActionStatus; }
        }

        protected virtual void InitActionStatus()
        {
            this._actionDefineMap.Append(layerZoneObject.Templates.DefaultUnitActionDefinition);
            if (layerUnit.AResource?.OverrideActionMap != null)
            {
                this._actionDefineMap.Append(layerUnit.AResource.OverrideActionMap);
            }
            ChangeAction(layerUnit.CurrentState, null, null, null);
        }
        protected virtual void CleanActionStatus()
        {
            this._actionDefineMap.Clear();
            this._currentActionStatus?.Dispose();
            this._currentActionStatus = null;
            //             foreach (var stack in _actionStatus.Values)
            //             {
            //                 stack.Dispose();
            //             }
            //             this._actionStatus.Clear();
        }
        protected virtual void UpdateActionStatus(float deltaMS)
        {
            if (_currentActionStatus != null)
            {
                _currentActionStatus.Update(deltaMS);
            }
        }
        //         public virtual UnityActionStatus RegistAction(DeepMetaGame.Data.Misc.UnitActionStatus st, UnityActionStatus status)
        //         {
        //             if (_actionStatus.TryGetValue(st, out var stack) == false)
        //             {
        //                 stack = ActionStack.Alloc(zone);
        //                 stack.Add(status);
        //                 _actionStatus.Add(st, stack);
        //             }
        //             else
        //             {
        //                 stack.Add(status);
        //             }
        //             return status;
        //         }
        protected virtual UnityActionStatus GetOrCreateActionStatus(DeepMetaGame.Data.Misc.UnitActionStatus st, string subState, string actionName, IRecyclable args)
        {
            if (args is UnitDoActionEvent && !string.IsNullOrEmpty(actionName))
            {
                return CustomActionStatus.Alloc(this, st, actionName);
            }
            if (st == DeepMetaGame.Data.Misc.UnitActionStatus.Skill)
            {
                return SkillActionStatus.Alloc(this);
            }
            else
            {
                var action = this._actionDefineMap.GetAction(st, subState);
                if (action != null)
                {
                    return DefinedActionStatus.Alloc(this, action);
                }
                else
                {
                    return CustomActionStatus.Alloc(this, st, actionName);
                }
            }
        }

        public virtual float CalcAnimateSpeed(UnitActionStatus action)
        {
            if (action.IsMoveable())
            {
                var rate = 1f;
                if (layerUnit.AUnitMotion)
                {
                    //rate = layerUnit.MoveSpeedSEC / layerUnit.BaseMoveSpeedSEC * layerUnit.AUnitMotion.MoveAnimateRate;
                    rate = layerUnit.AUnitMotion.MoveAnimateRate;
                    rate *= (1f - layerUnit.AUnitMotion.ScaleAnimateRate) + ((layerUnit.AUnitMotion.ScaleAnimateRate) * (1f / (layerUnit.BodyScale * layerUnit.ResScale)));
                }
                return layerUnit.FastMoveRate * rate;
            }
            else
            {
                return layerUnit.FastActionRate;
            }
        }

        protected virtual void ChangeActionSpeed()
        {
            _currentActionStatus?.SpeedChange();
        }

        protected void ChangeAction(DeepMetaGame.Data.Misc.UnitActionStatus st, string sub, string actionName, IRecyclable args)
        {
            var newAction = GetOrCreateActionStatus(st, sub, actionName, args);
            if (newAction != null)
            {
                _currentActionStatus?.Stop();
                _currentActionStatus?.Dispose();
                _currentActionStatus = newAction;
                _currentActionStatus?.Start(args);
            }
            else
            {
                //UnityEngine.Debug.LogError("ChangeAction Error: " + st + " " + sub + " not define!");
                if (ModelWrap != null)
                {
                    ModelWrap.PlayAnim(st, actionName, layerUnit.FastActionRate, ActionDefine.Instance.IsLoop(st));
                }
            }
        }
        protected override void OnPauseChanged(bool pause)
        {
            base.OnPauseChanged(pause);
            if (pause)
            {
                _currentActionStatus?.Pause();
            }
            else
            {
                _currentActionStatus?.Resume();
            }
        }
        //         public bool ForeachAction(DeepMetaGame.Data.Misc.UnitActionStatus st, Predicate<UnityActionStatus> action)
        //         {
        //             if (_actionStatus.TryGetValue(st, out var stack))
        //             {
        //                 if (stack.Foreach(action))
        //                 {
        //                     return true;
        //                 }
        //             }
        //             return false;
        //         }
        //         public bool ForeachAction(Predicate<UnityActionStatus> action)
        //         {
        //             foreach (var stack in _actionStatus.Values)
        //             {
        //                 if (stack.Foreach(action))
        //                 {
        //                     return true;
        //                 }
        //             }
        //             return false;
        //         }
        //---------------------------------------------------------------------------------------------------------------
        internal class ActionStack : Recyclable
        {
            readonly List<UnityActionStatus> List = new List<UnityActionStatus>();
            public static ActionStack Alloc(UnityZone zone)
            {
                var ret = zone.objectPool.AllocOrCreateAutoRelease<ActionStack>(static t => new ActionStack());
                return ret;
            }
            private ActionStack() { }
            protected override void Disposing()
            {
                foreach (var item in List)
                {
                    item.Dispose();
                }
                List.Clear();
            }
            protected override void Destructing()
            {

            }


            public UnityActionStatus Top
            {
                get
                {
                    if (List.Count > 0) return List[List.Count - 1];
                    return null;
                }
            }


            public bool Foreach(Predicate<UnityActionStatus> action)
            {
                var list = new List<UnityActionStatus>(List);
                foreach (var a in list)
                {
                    if (action(a))
                    {
                        return true;
                    }
                }

                return false;
            }

            public UnityActionStatus Add(UnityActionStatus status)
            {
                var tmp = Get(status.Key);
                if (tmp != null)
                {
                    List.Remove(tmp);
                }
                else
                {
                    tmp = status;
                }

                List.Add(tmp);

                return tmp;
            }

            public UnityActionStatus Get(string key)
            {
                return List.Find((st) =>
                {
                    if (st.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return true;
                    return false;
                });
            }
            public UnityActionStatus Remove(string key)
            {
                for (var i = 0; i < List.Count; i++)
                {
                    var tmp = List[i];
                    if (tmp.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        List.RemoveAt(i);
                        return tmp;
                    }
                }
                return null;
            }
        }
        //---------------------------------------------------------------------------------------------------------------
        public abstract class UnityActionStatus : Recyclable
        {
            public UnityZoneUnit Owner { get; private set; }
            public UnityZone Zone => Owner.zone;
            public DeepMetaGame.Data.Misc.UnitActionStatus ActionStatus { get; private set; }
            public int Priority { get; set; }
            public string StateName { get; set; } = string.Empty;
            public float NormalizeTime { get; set; } = 0;
            public float Speed { get; set; } = 1f;
            public bool IsLoop { get; set; } = false;
            public string Key { get; set; } = string.Empty;
            public string LayerName { get; set; } = string.Empty;
            public float LayerWeight { get; set; } = -1f;
            public abstract UnitActionDefinitionMap.UnitActionKeyFrame CurrentAction { get; }
            protected UnityActionStatus() { }
            protected virtual UnityActionStatus Init(UnityZoneUnit owner, DeepMetaGame.Data.Misc.UnitActionStatus status)
            {
                Owner = owner;
                ActionStatus = status;
                return this;
            }
            protected override void Disposing()
            {
                this.Owner = null;
                this.ActionStatus = default;
                this.Priority = default;
                this.StateName = string.Empty;
                this.NormalizeTime = 0;
                this.Speed = 1f;
                this.IsLoop = false;
                this.Key = string.Empty;
                this.LayerName = string.Empty;
                this.LayerWeight = -1f;
            }
            protected override void Destructing()
            {

            }
            //--------------------------------------------------------
            internal void Start(object args) => OnStart(args);
            internal void Pause() => OnPause();
            internal void Resume() => OnResume();
            internal void Stop() => OnStop();
            internal void Update(float deltaMS) => OnUpdate(deltaMS);
            internal void SpeedChange() => OnSpeedChange();
            //--------------------------------------------------------
            protected virtual void OnStart(object args)
            {
                PlayAnim();
            }
            protected virtual void OnPause()
            {
                Owner.ModelWrap?.PauseAnim();
            }
            protected virtual void OnResume()
            {
                Owner.ModelWrap?.ResumeAnim();
            }
            protected virtual void OnUpdate(float deltaMS)
            {
            }
            protected virtual void OnStop()
            {
                StopAnim();
            }
            protected virtual void OnSpeedChange()
            {
                Owner.ModelWrap?.SpeedChange(this);
            }
            //--------------------------------------------------------
            public void PlayAnim() { Owner.PlayAnim(this); }
            public void StopAnim() { Owner.StopAnim(this); }
            public void PlayAnim(UnitActionDefinitionMap.UnitActionKeyFrame act)
            {
                if (act != null && !string.IsNullOrEmpty(act.ActionName))
                {
                    this.StateName = act.ActionName;
                    this.NormalizeTime = act.CrossFadeTimeMS / 1000f;
                    this.IsLoop = act.Cycle;
                    this.LayerName = act.ActionLayer;
                    this.LayerWeight = act.ActionLayerWeight;
                    this.PlayAnim();
                }
            }

        }
        //---------------------------------------------------------------------------------------------------------------
        public class SkillActionStatus : UnityActionStatus
        {
            private LayerUnit.ISkillAction skillAction;
            public LayerUnit.ISkillAction SkillAction { get { return skillAction; } }
            public SkillTemplate Data { get { return skillAction.SkillData; } }
            public override UnitActionDefinitionMap.UnitActionKeyFrame CurrentAction { get=> skillAction?.CurrentAction?.Action; }
            public static SkillActionStatus Alloc(UnityZoneUnit owner)
            {
                var ret = owner.zone.objectPool.AllocOrCreateAutoRelease(static t => new SkillActionStatus());
                ret.Init(owner, DeepMetaGame.Data.Misc.UnitActionStatus.Skill);
                return ret;
            }
            protected SkillActionStatus() { }
            protected override UnityActionStatus Init(UnityZoneUnit owner, UnitActionStatus status)
            {
                return base.Init(owner, status);
            }
            protected override void Disposing()
            {
                base.Disposing();
                skillAction = null;
            }
            protected override void OnSpeedChange()
            {
                base.OnSpeedChange();
            }
            protected override void OnStart(object args)
            {
                if (args is LayerUnit.ISkillAction act)
                {
                    this.skillAction = act;
                }
                else
                {
                    this.skillAction = Owner.layerUnit.CurrentSkillAction;
                }
                this.Speed = Owner.layerUnit.FastActionRate;
                if (CurrentAction is UnitActionDefinitionMap.UnitActionKeyFrame action)
                {
                    this.Speed *= action.Speed;
                }
                if (skillAction != null)
                {
                    if (skillAction.CurrentAction?.Action != null)
                    {
                        this.IsLoop = skillAction.CurrentAction.Action.Cycle;
                    }
                    this.Speed *= skillAction.FastActionRate;
                    this.StateName = skillAction.CurrentActionName;          
                    this.LayerName = skillAction.CurrentAction?.Action?.ActionLayer;
                    this.LayerWeight = skillAction.CurrentAction?.Action?.ActionLayerWeight ?? -1f;
                }
                if (this.skillAction?.CurrentAction?.ActionEffect != null)
                {
                    Owner.parent.PlayObjectEffect(Owner, this.skillAction.CurrentAction.ActionEffect);
                }
                base.OnStart(args);
            }
            protected override void OnStop()
            {
                skillAction = null;
                base.OnStop();
            }
        }
        //---------------------------------------------------------------------------------------------------------------
        public class DefinedActionStatus : UnityActionStatus
        {
            protected UnitActionDefinitionMap.UnitAction mData;
            protected readonly Queue<UnitActionDefinitionMap.UnitActionKeyFrame> mActionQueue = new Queue<UnitActionDefinitionMap.UnitActionKeyFrame>();
            protected UnitActionDefinitionMap.UnitActionKeyFrame mCurrentAction;
            protected double mCurrentOverTime;
            protected double mCurrentPassTime;
            protected UnityZoneUnit.AppendModelWrap mCustomModel;
            protected UnityEffectPlay mEffect;
            public override UnitActionDefinitionMap.UnitActionKeyFrame CurrentAction => mCurrentAction;
            public static DefinedActionStatus Alloc(UnityZoneUnit owner, UnitActionDefinitionMap.UnitAction data)
            {
                var ret = owner.zone.objectPool.AllocOrCreateAutoRelease(static t => new DefinedActionStatus());
                ret.Init(owner, data);
                return ret;
            }
            protected DefinedActionStatus() { }
            protected virtual DefinedActionStatus Init(UnityZoneUnit owner, UnitActionDefinitionMap.UnitAction data)
            {
                this.mData = data;
                base.Init(owner, data.Action);
                if (data.ActionQueue.Count > 0)
                {
                    var a = data.ActionQueue[0];
                    this.StateName = a.ActionName;
                    this.NormalizeTime = a.CrossFadeTimeMS / 1000f;
                    this.Speed = a.Speed * owner.CalcAnimateSpeed(mData.Action);
                }
                return this;
            }
            protected override void Disposing()
            {
                mEffect?.Dispose();
                if (mCustomModel != null)
                {
                    Owner.RemoveModel(mCustomModel);
                }
                this.mCustomModel = null;
                base.Disposing();
                this.mData = default;
                this.mActionQueue.Clear();
                this.mCurrentAction = null;
                this.mCurrentOverTime = 0;
                this.mCurrentPassTime = 0;
            }
            protected override void OnStart(object args)
            {
                //base.OnStart(args);
                if (!string.IsNullOrEmpty(mData.CustomResource))
                {
                    this.mCustomModel = this.Owner.AppendModel(mData.CustomResource, mData.CustomResourceOverride);
                }
                mActionQueue.Clear();
                foreach (var act in mData.ActionQueue)
                {
                    this.mActionQueue.Enqueue(act);
                }
                mCurrentPassTime = 0;
                NextAction();
            }
            protected override void OnUpdate(float deltaTimeMS)
            {
                this.mCurrentPassTime += (deltaTimeMS);
                if (mActionQueue.Count > 0 && mCurrentAction != null)
                {
                    if (mCurrentPassTime >= mCurrentOverTime)
                    {
                        mCurrentPassTime = 0;
                        NextAction();
                    }
                }
            }
            protected override void OnSpeedChange()
            {
                this.Speed = Owner.CalcAnimateSpeed(mData.Action);
                if (mCurrentAction != null)
                {
                    this.Speed *= mCurrentAction.Speed;
                    this.PlayAnim(mCurrentAction);
                }
            }
            protected virtual void NextAction()
            {
                if (mActionQueue.Count > 0)
                {
                    mCurrentAction = mActionQueue.Dequeue();
                    if (mCurrentAction != null)
                    {
                        if (mCurrentAction.TimeMS == 0)
                        {
                            if (Owner.ModelWrap != null && Owner.ModelWrap.TryGetAnimatorStateDuriationMS(mCurrentAction.ActionName, out var timeMS))
                            {
                                this.mCurrentOverTime = timeMS;
                            }
                        }
                        else
                        {
                            this.mCurrentOverTime = mCurrentAction.TimeMS;
                        }
                        this.Speed = mCurrentAction.Speed * Owner.CalcAnimateSpeed(mData.Action);
                        this.PlayAnim(mCurrentAction);
                        if (mCurrentAction.ActionEffect != null)
                        {
                            mEffect?.Dispose();
                            mEffect = Zone.PlayObjectEffect(Owner, mCurrentAction.ActionEffect);
                        }
                    }
                }
            }
        }
        //---------------------------------------------------------------------------------------------------------------
        public class CustomActionStatus : UnityActionStatus
        {
            private UnitActionStatus st;
            private string actionName;
            public override UnitActionKeyFrame CurrentAction => null;
            public static CustomActionStatus Alloc(UnityZoneUnit owner, UnitActionStatus st, string actionName)
            {
                var ret = owner.zone.objectPool.AllocOrCreateAutoRelease(static t => new CustomActionStatus());
                ret.Init(owner, st, actionName);
                return ret;
            }
            protected CustomActionStatus() { }
            protected virtual CustomActionStatus Init(UnityZoneUnit owner, UnitActionStatus st, string actionName)
            {
                this.st = st;
                this.actionName = actionName;
                base.Init(owner, st);
                this.StateName = actionName;
                this.Speed = owner.layerUnit.FastActionRate;
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.st = default;
                this.actionName = default;
            }
        }
        //---------------------------------------------------------------------------------------------------------------
    }
}
