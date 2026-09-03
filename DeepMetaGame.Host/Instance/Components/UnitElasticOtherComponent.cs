using DeepCore.EventTrigger.Data;
using DeepCore.EventTrigger;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using DeepMetaGame.Data.ZoneGeometry;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class UnitElasticOtherComponent : UnitComponent
    {
 
        protected override void OnUpdate()
        {
            if (!Active) return;
            base.OnUpdate();
            ElasticOtherObjects(Owner);
        }

        /// <summary>
        /// 挤开其他和自己重叠的单位，或者被Weight大于自己的人挤开
        /// </summary>
        /// <returns></returns>
        public static bool ElasticOtherObjects(InstanceUnit owner)
        {
            using (var it = owner.ObjectPool.AllocForEach2<InstanceZoneEntity, InstanceUnit, bool>(owner, false))
            {
                owner. Zone.ForEachNearObjects(owner.X, owner.Y, it, static (st) =>
                {
                    if (st.Iterator is InstanceUnit o)
                    {
                        var _this = st.Arg1;
                        if ((o != _this) && o.Parent.TouchObject2(_this, o))
                        {
                            if (ElasticOtherObject(_this, o))
                            {
                                st.Arg2 = true;
                            }
                        }
                    }
                });
                return it.Arg2;
            }
        }

        /// <summary>
        /// 挤开其他和自己重叠的单位，或者被Weight大于自己的人挤开
        /// </summary>
        /// <param name="o"></param>
        /// <returns>自己发生位移</returns>
        public static bool ElasticOtherObject(InstanceUnit Owner, InstanceUnit o)
        {
            float targetAngle = Owner.Direction;
            float ddr = MathVector.getDistance(Owner.X, Owner.Y, o.X, o.Y);
            if (ddr > 0)
            {
                targetAngle = MathVector.getDegree(Owner.X, Owner.Y, o.X, o.Y);
            }
            else
            {
                targetAngle = (Owner.RandomN.NextFloat() * CMath.PI_MUL_2);
            }
            float bdr = (Owner.BodyBlockSize + o.BodyBlockSize);
            float d = (bdr - ddr);
            if (!o.Moveable)
            {
                Owner.MoveAirTo(targetAngle, -d, Owner.Zone.UpdateIntervalMS);
                return true;
            }
            else if (o.Weight > Owner.Weight)
            {
                Owner.MoveAirTo(targetAngle, -d, Owner.Zone.UpdateIntervalMS);
                return true;
            }
            else
            {
                o.MoveAirTo(targetAngle, d, Owner.Zone.UpdateIntervalMS);
                return true;
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------
        #region EventAPI

        [Desc("挤开其他单位", "[游戏]/单位/[组件]/挤开")]
        public class UnitElasticOther : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})开始挤开其他单位;", Unit);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit)
                {
                    ElasticOtherObjects(unit);
                }
                return null;
            }
        }

        [Desc("开始挤开其他单位", "[游戏]/单位/[组件]/挤开")]
        public class UnitElasticOtherStart : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})开始挤开其他单位;", Unit);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit)
                {
                    var comp = unit.Components.AddComponent<UnitElasticOtherComponent>();
                }
                return null;
            }
        }
        [Desc("终止挤开其他单位", "[游戏]/单位/[组件]/挤开")]
        public class UnitElasticOtherStop : ForceUnitAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})终止挤开其他单位;", Unit);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                if (Unit.GetValueAs(api, args) is InstanceUnit unit && unit.Components.TryGetComponentAs<UnitElasticOtherComponent>(out var track))
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
