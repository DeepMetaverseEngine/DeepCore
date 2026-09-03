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
using DeepMetaGame.Data.ZoneGeometry;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class UnitTouchBodyComponent : UnitComponent, IViewTriggerListener<InstanceUnit>
    {
        protected ViewTrigger<InstanceUnit> mViewTrigger;
        public virtual bool Enable
        {
            get => this.mViewTrigger.Enable;
            set => this.mViewTrigger.Enable = value;
        }
        //------------------------------------------------------------------------------------------------------------------------
        protected override void OnAdded()
        {
            base.OnAdded();
            this.mViewTrigger = new ViewTriggerCylinderCenter<InstanceUnit>(Zone, Owner.Position, Owner.BodyBlockSize, Owner.BodyHeight);
            this.mViewTrigger.SetListener(this);
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            this.mViewTrigger?.Dispose();
            this.mViewTrigger = null;
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (this.mViewTrigger != null)
            {
                this.mViewTrigger.Enable = Active;
                this.mViewTrigger.LookUpdate(Owner.Position);
            }
        }
        //------------------------------------------------------------------------------------------------------------------------
        public virtual void ClearViewd()
        {
            mViewTrigger?.ClearViewd();
        }
        protected virtual bool CanView(InstanceUnit obj)
        {
            if (obj == Owner)
            {
                return false;
            }
            else
            {
                return true;
            }
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
        public delegate void ObjectViewHandler(UnitTouchBodyComponent sender, InstanceUnit obj);

        //----------------------------------------------------------------------------------------------------------------------------------------
        #region EventAPI

        [Desc("单位进入碰撞", "[游戏]/单位/[组件]/身体碰撞检测")]
        public class OnObjectEnterViewTrigger : ZoneAbstractTrigger
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("当单位进入({0})碰撞", Unit);
            }
            protected override void Listen(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    var guard = unit.Components.GetOrAddComponentAs<UnitTouchBodyComponent>();
                    var handler = new ObjectViewHandler(( sender,  obj) =>
                    {
                        args.TriggingUnit = unit;
                        args.TriggingCounterPart = obj;
                        api.TestAndDoAction(args);
                    });
                    api.Listen(guard, handler,
                        static (a, b) => a.OnObjectEnterView += b,
                        static (a, b) => a.OnObjectEnterView -= b);
                }
            }
            [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
            [TriggingArg("碰撞的单位")] public InstanceUnit Owner(EventArguments args) => args.TriggingCounterPart;
        }
        [Desc("单位离开碰撞", "[游戏]/单位/[组件]/身体碰撞检测")]
        public class OnObjectLeaveViewTrigger : ZoneAbstractTrigger
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("当单位离开({0})碰撞", Unit);
            }
            protected override void Listen(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    var guard = unit.Components.GetOrAddComponentAs<UnitTouchBodyComponent>();
                    var handler = new ObjectViewHandler(( sender,  obj) =>
                    {
                        args.TriggingUnit = unit;
                        args.TriggingCounterPart = obj;
                        api.TestAndDoAction(args);
                    });
                    api.Listen(guard, handler,
                        static (a, b) => a.OnObjectLeaveView += b,
                        static (a, b) => a.OnObjectLeaveView -= b);
                }
            }
            [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
            [TriggingArg("碰撞的单位")] public InstanceUnit Owner(EventArguments args) => args.TriggingCounterPart;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------


        [Desc("开关身体碰撞检测", "[游戏]/单位/[组件]/身体碰撞检测")]
        public class UnitTouchBodyAction : ZoneAbstractAction
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
                        unit.Components.GetOrAddComponentAs<UnitTouchBodyComponent>();
                    }
                    else
                    {
                        unit.Components.RemoveComponentAs<UnitTouchBodyComponent>();
                    }
                }
                return null;
            }
        }

        #endregion

    }
}
