using DeepCore.EventTrigger.Data;
using DeepCore.EventTrigger;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneEditor;
using DeepCore.Game3D.Host.Helper;
using System;
using DeepMetaGame.Data.ZoneGeometry;
using System.Collections.Generic;
using DeepCore.Geometry;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class UnitMoveTrackComponent : UnitComponent
    {
        public bool IsPause { get; set; } = false;
        public bool FaceToNext { get; set; } = true;
        protected InstanceFlag current;
        protected InstanceFlag prev;
        protected ZoneTimeExpire hold_time;
        public float HoldTimeExpire { get => (float)(hold_time != null ? hold_time.ExpireTimeMS : 0); }
        protected override void OnDispose(InstanceZoneObject owner)
        {
            this.OnHold = null;
            this.OnPassPoint = null;
            base.OnDispose(owner);
            this.hold_time?.Dispose();
            this.hold_time = null;
        }
        protected override void OnUpdate()
        {
            if (!Active) return;
            base.OnUpdate();
            if (IsPause)
            {

            }
            else
            {
                if (this.hold_time != null)
                {
                    if (this.hold_time.Update())
                    {
                        this.hold_time = null;
                    }
                    else
                    {
                        return;
                    }
                }
                if (current != null)
                {
                    if (FaceToNext)
                    {
                        Owner.FaceTo(current.Position);
                    }
                    if (Owner.Move3DNoneTouch(current, Owner.MoveSpeedSEC, Zone.UpdateIntervalMS))
                    {
                        if (TryHold(current))
                        {

                        }
                        this.prev = current;
                        this.current = current.PopRandomNext(prev);
                        OnPassPoint?.Invoke(this, prev, current);
                        this.prev.InvokePathPass(Owner, current);
                    }
                }
            }
        }

        public virtual void Start(InstanceFlag start)
        {
            this.prev = current;
            this.current = start;
        }
        protected virtual bool TryHold(InstanceFlag flag)
        {
            if (flag.InvokeTryPathHold(Owner, out var hold))
            {
                this.hold_time?.Dispose();
                this.hold_time = Zone.AllocTimeExpire(Owner.RandomN.Next(hold.HoldMinTimeMS, hold.HoldMaxTimeMS));
                OnHold?.Invoke(this, flag, hold);
                return true;
            }
            return false;
        }
        public delegate void OnPassPointDelegate(UnitMoveTrackComponent comp, InstanceFlag prev, InstanceFlag next);
        public delegate void OnHoldDelegate(UnitMoveTrackComponent comp, InstanceFlag point, PointHoldAbility hold);
        public event OnPassPointDelegate OnPassPoint;
        public event OnHoldDelegate OnHold;
        //----------------------------------------------------------------------------------------------------------------------------------------
        #region EventAPI

        [Desc("强制按轨道移动", "[游戏]/单位/[组件]/轨道移动")]
        public class ForceUnitMoveTrack : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("开始点")]
            public AbstractValue<InstanceFlag> StartPoint = new FlagValue.EditorPoint();
            [Desc("是否面向下一个点")]
            public AbstractValue<bool> FaceToNext = new ZoneBooleanValue.VALUE(true);
            [Desc("当经过路点")]
            public AbstractAction OnPassAction;
            [Desc("当遇到可暂停路点")]
            public AbstractAction OnHoldAction;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})强制按轨道({1})移动;", Unit, StartPoint);
                if (OnPassAction != null)
                {
                    sw.AppendLine("当经过路点后:");
                    sw.IndentBegin("{");
                    sw.AppendFormat("{0}", OnPassAction).AppendLine();
                    sw.IndentEnd("}");
                }
                if (OnPassAction != null && OnHoldAction != null)
                {
                    sw.AppendLine();
                }
                if (OnHoldAction != null)
                {
                    sw.AppendLine("当遇到可暂停路点后:");
                    sw.IndentBegin("{");
                    sw.AppendFormat("{0}", OnHoldAction).AppendLine();
                    sw.IndentEnd("}");
                }
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit && StartPoint.GetValueAs(api, args) is InstanceFlag start)
                {
                    var comp = unit.Components.GetOrAddComponentAs<UnitMoveTrackComponent>();
                    comp.Start(start);
                    comp.FaceToNext = FaceToNext.GetValueAs(api, args);
                    if (OnPassAction != null)
                    {
                        var handler = new OnPassPointDelegate((comp, prev, next) =>
                        {
                            var targs = args;
                            targs.TriggingFlag = prev;
                            targs.TriggingUnit = comp.Unit;
                            OnPassAction.Invoke(api, targs);
                        });
                        api.Listen(comp, handler,
                            static (comp, handler) => comp.OnPassPoint += handler,
                            static (comp, handler) => comp.OnPassPoint -= handler);
                    }
                    if (OnHoldAction != null)
                    {
                        var handler = new OnHoldDelegate((comp, point, hold) =>
                        {
                            var targs = args;
                            targs.TriggingFlag = point;
                            targs.TriggingUnit = comp.Unit;
                            OnHoldAction.Invoke(api, targs);
                        });
                        api.Listen(comp, handler,
                            static (comp, handler) => comp.OnHold += handler,
                            static (comp, handler) => comp.OnHold -= handler);
                    }
                }
                return null;
            }
            [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
            [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        }
        [Desc("暂停按轨道移动", "[游戏]/单位/[组件]/轨道移动")]
        public class ForceUnitMoveTrackPause : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("是否暂停")]
            public AbstractValue<bool> IsPause = new ZoneBooleanValue.VALUE(true);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})轨道移动暂停({1});", Unit, IsPause);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit && unit.Components.TryGetComponentAs<UnitMoveTrackComponent>(out var track))
                {
                    track.IsPause = IsPause.GetValueAs(api, args);
                }
                return null;
            }
        }

        [Desc("继续按轨道移动", "[游戏]/单位/[组件]/轨道移动")]
        public class ForceUnitMoveTrackResume : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})轨道移动继续;", Unit);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit && unit.Components.TryGetComponentAs<UnitMoveTrackComponent>(out var track))
                {
                    track.IsPause = false;
                }
                return null;
            }
        }


        [Desc("终止按轨道移动", "[游戏]/单位/[组件]/轨道移动")]
        public class ForceUnitMoveTrackStop : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})终止按轨道移动;", Unit);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit && unit.Components.TryGetComponentAs<UnitMoveTrackComponent>(out var track))
                {
                    unit.Components.RemoveComponent(track);
                }
                return null;
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------
    }



    public class UnitMoveBezierTrackComponent : UnitComponent
    {
        public bool IsPause { get; set; } = false;
        public bool FaceToNext { get; set; } = true;

        public float Step = 0.1f;

        private readonly BezierCurveTrack track = new BezierCurveTrack();
        private BezierCurveTrack.Node current;
        protected override void OnDispose(InstanceZoneObject owner)
        {
            this.current = null;
            this.track.Dispose();
            base.OnDispose(owner);
        }
        protected override void OnUpdate()
        {
            if (!Active) return;
            base.OnUpdate();
            if (IsPause)
            {

            }
            else
            {
                if (current != null)
                {
                    if (FaceToNext)
                    {
                        Owner.FaceTo(current.Position);
                    }
                    float distance = MoveHelper.GetDistance(Zone.UpdateIntervalMS, Owner.MoveSpeedSEC);
                    while (distance > 0 && current != null)
                    {
                        float step = Owner.MoveLerpTo(current.Position, distance);
                        if (step >= 0)
                        {
                            distance = step;
                            current = current.Next;
                        }
                        else if (step < 0)
                        {
                            break;
                        }
                    }
                }
            }
        }

        public virtual void Start(ZoneWayPoint start)
        {
            this.track.Clear();
            this.track.AddPoint(Zone.Data, start.Data, Step);
            this.current = track.First;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------
        #region EventAPI

        [Desc("强制按贝塞尔轨道移动", "[游戏]/单位/[组件]/轨道移动")]
        public class ForceUnitMoveTrack : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("开始点")]
            public AbstractValue<InstanceFlag> StartPoint = new FlagValue.EditorPoint();
            [Desc("是否面向下一个点")]
            public AbstractValue<bool> FaceToNext = new ZoneBooleanValue.VALUE(true);
            [Desc("是否面向下一个点")]
            public AbstractValue<double> Step = new ZoneRealValue.VALUE(0.1f);

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})强制按贝塞尔轨道({1})移动;", Unit, StartPoint);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit && StartPoint.GetValueAs(api, args) is ZoneWayPoint start)
                {
                    var comp = unit.Components.GetOrAddComponentAs<UnitMoveBezierTrackComponent>();
                    comp.FaceToNext = FaceToNext.GetValueAs(api, args);
                    comp.Step = Step.GetValueAs<float>(api, args);
                    comp.Start(start);
                }
                return null;
            }
        }
        [Desc("暂停按贝塞尔轨道移动", "[游戏]/单位/[组件]/轨道移动")]
        public class ForceUnitMoveTrackPause : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("是否暂停")]
            public AbstractValue<bool> IsPause = new ZoneBooleanValue.VALUE(true);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})贝塞尔轨道移动暂停({1});", Unit, IsPause);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit && unit.Components.TryGetComponentAs<UnitMoveBezierTrackComponent>(out var track))
                {
                    track.IsPause = IsPause.GetValueAs(api, args);
                }
                return null;
            }
        }

        [Desc("继续按贝塞尔轨道移动", "[游戏]/单位/[组件]/轨道移动")]
        public class ForceUnitMoveTrackResume : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})贝塞尔轨道移动继续;", Unit);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit && unit.Components.TryGetComponentAs<UnitMoveBezierTrackComponent>(out var track))
                {
                    track.IsPause = false;
                }
                return null;
            }
        }


        [Desc("终止按贝塞尔轨道移动", "[游戏]/单位/[组件]/轨道移动")]
        public class ForceUnitMoveTrackStop : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})终止按贝塞尔轨道移动;", Unit);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit && unit.Components.TryGetComponentAs<UnitMoveBezierTrackComponent>(out var track))
                {
                    unit.Components.RemoveComponent(track);
                }
                return null;
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------
    }

}
