using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Triggers;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class UnitGuardViewComponent : UnitComponent, IViewTriggerListener<InstanceUnit>
    {
        protected ViewTrigger<InstanceUnit> mViewTrigger;
        protected Geometry.Vector3? mOrginPosition;
        protected TimeInterval mCheckInGuardLimit;

        public Geometry.Vector3? OriginPosition
        {
            get => mOrginPosition;
            set => mOrginPosition = value;
        }
        public virtual bool Enable
        {
            get => mViewTrigger != null && mViewTrigger.Enable;
            set
            {
                if (mViewTrigger != null)
                {
                    this.mViewTrigger.Enable = value && !Owner.IsNature && !Owner.IsNoneSkill;
                }
            }
        }
        //------------------------------------------------------------------------------------------------------------------------
        protected override void OnAdded()
        {
            this.mOrginPosition = Owner.Position;
            this.mCheckInGuardLimit = Owner.AllocTimeInterval(Zone.CFG.AI_NPC_CHECK_IN_GUARD_LIMIT_TIME_MS);
            base.OnAdded();
            ResetViewTrigger();
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            mViewTrigger?.Dispose();
            mCheckInGuardLimit?.Dispose();
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (mViewTrigger != null)
            {
                mViewTrigger.Enable = Active;
                mViewTrigger.LookUpdate(Owner.Position);
            }
            if (!Active) return;
            if (mOrginPosition.HasValue)
            {
                if (Owner.AGuard && Owner.AGuard.GuardRangeLimitAppend > 0)
                {
                    if (mCheckInGuardLimit.Update(Zone.UpdateIntervalMS))
                    {
                        var origin = mOrginPosition.Value;
                        var limit = Owner.AGuard.GuardRange + Owner.AGuard.GuardRangeLimitAppend;
                        //if (!CMath.includeRoundPoint(X, Y, Info.GuardRangeLimit, mOrginPosition.X, mOrginPosition.Y))
                        var crossed = (!Collider.Intersects(origin, Owner.Position, limit));
                        if (crossed)
                        {
                            NeedBackToOrigin?.Invoke(this, origin, limit);
                        }
                        else
                        {
                            OnInOriginRange?.Invoke(this, origin, limit);
                        }
                        //                         else if (mTracingTarget != null)
                        //                         {
                        //                             var r2 = limit + mTracingTarget.TargetUnit.BodyHitSize;
                        //                             if (!mTracingTarget.IsActive || !(Collider.Intersects(this.Position, mTracingTarget.TargetUnit.Position, r2)))
                        //                             {
                        //                                 backToOrgin();
                        //                                 return;
                        //                             }
                        //                         }
                    }
                }
            }
        }
        //------------------------------------------------------------------------------------------------------------------------
        public virtual void ClearViewd()
        {
            mViewTrigger?.ClearViewd();
        }
        public virtual void ResetViewTrigger()
        {
            if (this.mViewTrigger != null)
            {
                this.mViewTrigger.Dispose();
            }
            this.mViewTrigger = CreateViewTrigger(Zone);
            if (this.mViewTrigger != null)
            {
                this.mViewTrigger.SetListener(this);
            }
        }
        public void SetViewTrigger(ViewTrigger<InstanceUnit> vt)
        {
            if (this.mViewTrigger != null)
            {
                this.mViewTrigger.Dispose();
            }
            if (vt != null)
            {
                this.mViewTrigger = vt;
                this.mViewTrigger.SetListener(this);
            }
        }

        protected virtual ViewTrigger<InstanceUnit> CreateViewTrigger(InstanceZone zone)
        {
            if (Owner.AGuard && Owner.AGuard.GuardRange > 0)
            {
                return new ViewTriggerSphereBody<InstanceUnit>(zone, Owner.Position, Owner.AGuard.GuardRange/*, this.BodyHeight*/);
            }
            else
            {
                return new ViewTriggerBlind<InstanceUnit>(zone);
            }
        }
        protected virtual bool CanView(InstanceUnit obj)
        {
            if (obj == Owner)
            {
                return false;
            }
            else
            {
                if (!obj.IsNature && Zone.Formula.IsAttackable(Owner, obj, SkillTemplate.CastTarget.Enemy, AttackReason.Look, Owner.Info))
                {
                    return true;
                }
            }
            return false;
        }

        void IViewTriggerListener<InstanceUnit>.OnObjectEnterView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            OnObjectEnterView?.Invoke(this, obj);
        }
        void IViewTriggerListener<InstanceUnit>.OnObjectLeaveView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            OnObjectLeaveView?.Invoke(this, obj);
        }
        bool IViewTriggerListener<InstanceUnit>.Select(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            return CanView(obj);
        }

        public event ObjectViewHandler OnObjectEnterView;
        public event ObjectViewHandler OnObjectLeaveView;
        public event OriginHandler NeedBackToOrigin;
        public event OriginHandler OnInOriginRange;
        public delegate void ObjectViewHandler(UnitGuardViewComponent sender, InstanceUnit obj);
        public delegate void OriginHandler(UnitGuardViewComponent sender, Vector3 origin, float limit);

        //----------------------------------------------------------------------------------------------------------------------------------------
        #region EventAPI

        [Desc("单位进入视野", "[游戏]/单位/[组件]/警戒")]
        public class OnObjectEnterViewTrigger : ZoneAbstractTrigger
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("当单位进入({0})视野", Unit);
            }
            protected override void Listen(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    var comp = unit.Components.GetOrAddComponentAs<UnitGuardViewComponent>();
                    var handler = new ObjectViewHandler((UnitGuardViewComponent sender, InstanceUnit obj) =>
                    {
                        args.TriggingUnit = unit;
                        args.TriggingCounterPart = obj;
                        api.TestAndDoAction(args);
                    });
                    api.Listen(comp, handler,
                        static (a, b) => a.OnObjectEnterView += b,
                        static (a, b) => a.OnObjectEnterView -= b);
                }
            }
            [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
            [TriggingArg("进入的单位")] public InstanceUnit Owner(EventArguments args) => args.TriggingCounterPart;
        }
        [Desc("单位离开视野", "[游戏]/单位/[组件]/警戒")]
        public class OnObjectLeaveViewTrigger : ZoneAbstractTrigger
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("当单位离开({0})视野", Unit);
            }
            protected override void Listen(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    var comp = unit.Components.GetOrAddComponentAs<UnitGuardViewComponent>();
                    var handler = new ObjectViewHandler((UnitGuardViewComponent sender, InstanceUnit obj) =>
                    {
                        args.TriggingUnit = unit;
                        args.TriggingCounterPart = obj;
                        api.TestAndDoAction(args);
                    });
                    api.Listen(comp, handler,
                        static (a, b) => a.OnObjectLeaveView += b,
                        static (a, b) => a.OnObjectLeaveView -= b);
                }
            }
            [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
            [TriggingArg("离开的单位")] public InstanceUnit Owner(EventArguments args) => args.TriggingCounterPart;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------


        [Desc("开关警戒范围", "[游戏]/单位/[组件]/警戒")]
        public class UnitGuardViewAction : ZoneAbstractAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("开关")]
            public AbstractValue<bool> On = new ZoneBooleanValue.VALUE(true);
            protected override object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    if (On.GetValueAs(api, args))
                    {
                        unit.Components.GetOrAddComponentAs<UnitGuardViewComponent>();
                    }
                    else
                    {
                        unit.Components.RemoveComponentAs<UnitGuardViewComponent>();
                    }
                }
                return null;
            }
        }


        [Desc("获得警戒原点", "[游戏]/单位/[组件]/警戒")]
        public class GetOrigionValue : PositionValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null && unit.Components.TryGetComponentAs<UnitGuardViewComponent>(out var comp) && comp.OriginPosition.HasValue)
                {
                    return comp.OriginPosition.Value;
                }
                return unit.Position;
            }
        }


        [Desc("设置警戒原点", "[游戏]/单位/[组件]/警戒")]
        public class SetOrigionAction : ZoneAbstractAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("原点")]
            public AbstractValue<Vector3?> Origion = new GetOrigionValue();
            protected override object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    var comp = unit.Components.GetOrAddComponentAs<UnitGuardViewComponent>();
                    comp.OriginPosition = Origion.GetValueAs(api, args);
                }
                return null;
            }
        }
        [Desc("清除警戒原点", "[游戏]/单位/[组件]/警戒")]
        public class ClearOrigionAction : ZoneAbstractAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null && unit.Components.TryGetComponentAs<UnitGuardViewComponent>(out var comp))
                {
                    comp.OriginPosition = null;
                }
                return null;
            }
        }


        [Desc("清除所有观察", "[游戏]/单位/[组件]/警戒")]
        public class ClearViewdAction : ZoneAbstractAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null && unit.Components.TryGetComponentAs<UnitGuardViewComponent>(out var comp))
                {
                    comp.ClearViewd();
                }
                return null;
            }
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------
    }
}
