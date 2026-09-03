using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class UnitHateComponent : UnitComponent
    {
        protected HateSystem mHateSystem;
        public HateSystem HateSystem { get => mHateSystem; }

        protected override void OnAdded()
        {
            this.mHateSystem = Zone.CreateHateSystem(Owner);
            Owner.OnHandleResetAI += Owner_OnHandleResetAI;
            Owner.OnDamage += Owner_OnDamage;
            base.OnAdded();
        }
        protected override void OnRemoved()
        {
            //OnEnemyAdded = null;
            base.OnRemoved();
            Owner.OnHandleResetAI -= Owner_OnHandleResetAI;
            Owner.OnDamage -= Owner_OnDamage;
            mHateSystem.Clear();
        }
        protected override void OnDispose(InstanceZoneObject owner)
        {
            base.OnDispose(owner);
            mHateSystem.Dispose();
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            mHateSystem.Update();
        }

        protected virtual void Owner_OnDamage(InstanceUnit obj, InstanceUnit attacker, long hp, in TAttackSource source, in TAttackResult result)
        {
            AddHateDamage(attacker, in source, in result, hp);
        }

        protected virtual void Owner_OnHandleResetAI(InstanceUnit obj)
        {
            mHateSystem.Clear();
        }

        protected virtual void onAddHateDamage(InstanceUnit attacker, in TAttackSource attack, in TAttackResult result, long reduceHP)
        {
            HateSystem.OnHitted(attacker, in attack, in result, reduceHP);
            //OnEnemyAdded?.Invoke(this, attacker, AttackReason.Damaged);
        }

        protected virtual void onAddHateLook(InstanceUnit target)
        {
            HateSystem.Add(target, AttackReason.Look, Owner.CFG.AI_HATE_SYSTEM_ENTER_VIEW_HATE_VALUE);
            //OnEnemyAdded?.Invoke(this, target, AttackReason.Look);
        }

        protected virtual void onAddHateGroup(InstanceUnit target)
        {
            if (Owner.AGuard && Owner.AGuard.GuardRangeGroup > 0)
            {
                using (var for1 = Owner.ObjectPool.AllocForEach2<InstanceZoneEntity, InstanceUnit, InstanceUnit>(Owner, target))
                {
                    Zone.ForEachNearObjects(Owner.X, Owner.Y, Owner.AGuard.GuardRangeGroup, for1, static (st) =>
                    {
                        if (st.Iterator is InstanceUnit o)
                        {
                            var _this = st.Arg1;
                            var _target = st.Arg2;
                            if ((o != _this) && o.AGuard && o.Force == _this.Force)
                            {
                                if (o.Components.TryGetComponentAs<UnitHateComponent>(out var otherHate))
                                {
                                    //精确过滤.
                                    if (Collider.Intersects(_this.Position, o.Position, _this.AGuard.GuardRangeGroup))
                                    {
                                        if (_this.Parent.Formula.IsAttackable(o, _target, SkillTemplate.CastTarget.Enemy, AttackReason.Look, _this.Info))
                                        {
                                            var limit = o.AGuard.GuardRange + o.AGuard.GuardRangeLimitAppend;
                                            if (Collider.Intersects(o.Position, _target.Position, limit))
                                            {
                                                otherHate.onAddHateLook(_target);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            }
        }

        public void AddHateDamage(InstanceUnit attacker, in TAttackSource attack, in TAttackResult result, long reduceHP)
        {
            onAddHateDamage(attacker, in attack, in result, reduceHP);
            onAddHateGroup(attacker);
        }
        public void AddHateLook(InstanceUnit target)
        {
            onAddHateLook(target);
            onAddHateGroup(target);
        }

        //----------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------
        #region EventAPI

        [Desc("开关仇恨系统", "[游戏]/单位/[组件]/仇恨")]
        public class UnitAddHateSystem : ZoneAbstractAction
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
                        unit.Components.GetOrAddComponentAs<UnitHateComponent>();
                    }
                    else
                    {
                        unit.Components.RemoveComponentAs<UnitHateComponent>();
                    }
                }
                return unit;
            }
        }


        [Desc("目标进入仇恨列表", "[游戏]/单位/[组件]/仇恨")]
        public class HateAddedTrigger : ZoneAbstractTrigger
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("目标进入({0})仇恨列表时", Unit);
            }
            protected override void Listen(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null && unit.Components.TryGetComponentAs<UnitHateComponent>(out var hate))
                {
                    var handler = new HateTargetHandler((h, t, r) =>
                    {
                        args.TriggingUnit = unit;
                        args.TriggingCounterPart = t;
                        api.TestAndDoAction(args);
                    });
                    api.Listen(hate.HateSystem, handler,
                        static (hate, handler) => hate.TargetAdded += handler,
                        static (hate, handler) => hate.TargetAdded -= handler);
                }
            }
            [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
            [TriggingArg("目标")] public InstanceUnit Target(EventArguments args) => args.TriggingCounterPart;
        }

        [Desc("目标离开仇恨列表", "[游戏]/单位/[组件]/仇恨")]
        public class HateRemovedTrigger : ZoneAbstractTrigger
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("目标离开({0})仇恨列表时", Unit);
            }
            protected override void Listen(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null && unit.Components.TryGetComponentAs<UnitHateComponent>(out var hate))
                {
                    var handler = new HateTargetHandler((h, t, r) =>
                    {
                        args.TriggingUnit = unit;
                        args.TriggingCounterPart = t;
                        api.TestAndDoAction(args);
                    });
                    api.Listen(hate.HateSystem, handler,
                        static (hate, handler) => hate.TargetRemoved += handler,
                        static (hate, handler) => hate.TargetRemoved -= handler);
                }
            }
            [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
            [TriggingArg("目标")] public InstanceUnit Target(EventArguments args) => args.TriggingCounterPart;
        }


        //---------------------------------------------------------------------------------------------------
        [Desc("单位仇恨目标", "[游戏]/单位/[组件]/仇恨")]
        public class GetHateUnit : UnitValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null && unit.Components.TryGetComponentAs<UnitHateComponent>(out var hate))
                {
                    return hate.HateSystem.GetHated();
                }
                return null;
            }
        }

        //---------------------------------------------------------------------------------------------------
        [Desc("单位仇恨目标数量", "[游戏]/单位/[组件]/仇恨")]
        public class GetHateTargetCount : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null && unit.Components.TryGetComponentAs<UnitHateComponent>(out var hate))
                {
                    return hate.HateSystem.Count;
                }
                return 0;
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------
    }



}
