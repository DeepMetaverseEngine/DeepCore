using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Host.EventTrigger.ZoneEventTrigger
{

    public abstract class PlayerGuardAction : ZoneAbstractAction
    {
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.UnitAPI();

        [Desc("设置自动战斗", "[游戏]/单位/玩家AI")]
        public class PlayerSetGuardAction : PlayerGuardAction
        {
            [Desc("自动战斗")]
            public AbstractValue<bool> IsGuard = new BooleanValue.VALUE(true);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})设置自动战斗{1};", Player, IsGuard);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var p = Player.GetValueAs(api, args) as InstancePlayer;
                if (p != null)
                {
                    p.SetGuard(IsGuard.GetValueAs(api, args), true);
                }
                return null;
            }
        }

        [Desc("重置AI", "[游戏]/单位/玩家AI")]
        public class PlayerResetAIAction : PlayerGuardAction
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})重置AI;", Player);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var p = Player.GetValueAs(api, args);
                if (p != null)
                {
                    p.ResetAI();
                }
                return null;
            }
        }



        [Desc("跟随目标", "[游戏]/单位/玩家AI")]
        public class PlayerFollowTargetAction : PlayerGuardAction
        {
            [Desc("目标")]
            public AbstractValue<InstanceUnit> Target = new UnitValue.Trigging();
            [Desc("是否自动攻击")]
            public AbstractValue<bool> AutoAttack = new BooleanValue.VALUE(true);
            [Desc("最小距离")]
            public AbstractValue<double> MinDistance = new RealValue.VALUE(5);
            [Desc("最大距离")]
            public AbstractValue<double> MaxDistance = new RealValue.VALUE(10);
            [Desc("传送硬拉距离")]
            public AbstractValue<double> TpDistance = new RealValue.VALUE(50);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})跟随目标({1});", Player, Target);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var p = Player.GetValueAs(api, args);
                var t = Target.GetValueAs(api, args);
                if (p != null && t != null)
                {
                    p.doAction(new UnitFollowTargetAction()
                    {
                        object_id = p.ObjectID,
                        targetUnitID = t.ObjectID,
                        autoAttack = AutoAttack.GetValueAs(api, args),
                        maxDistance = (float)MaxDistance.GetValueAs(api, args),
                        minDistance = (float)MinDistance.GetValueAs(api, args),
                        tpDistance = (float)TpDistance.GetValueAs(api, args),
                    });
                }
                return null;
            }
        }



        [Desc("锁定目标", "[游戏]/单位/玩家AI")]
        public class PlayerFocusUnitTargetAction : PlayerGuardAction
        {
            [Desc("目标")]
            public AbstractValue<InstanceUnit> Target = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})锁定目标({1});", Player, Target);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var p = Player.GetValueAs(api, args);
                var t = Target.GetValueAs(api, args);
                if (p != null && t != null)
                {
                    p.doAction(new UnitFocuseTargetAction()
                    {
                        object_id = p.ObjectID,
                        targetUnitID = t.ObjectID,
                    });
                }
                return null;
            }
        }



        [Desc("捡取物品", "[游戏]/单位/玩家AI")]
        public class PlayerFocusPickItemAction : PlayerGuardAction
        {
            [Desc("目标")]
            public AbstractValue<InstanceItem> Target = new ItemValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})捡取物品({1});", Player, Target);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var p = Player.GetValueAs(api, args);
                var t = Target.GetValueAs(api, args);
                if (p != null && t != null)
                {
                    p.doAction(new UnitFocuseTargetAction()
                    {
                        object_id = p.ObjectID,
                        targetUnitID = t.ObjectID,
                    });
                }
                return null;
            }
        }



        [Desc("A过去", "[游戏]/单位/玩家AI")]
        public class PlayerAttackToAction : PlayerGuardAction
        {
            [Desc("目的地")]
            public AbstractValue<Vector3?> TargetPos = new PositionValue.RandomMovablePointInFlag()
            {
                Flag = new FlagValue.EditorRegion(),
            };
            [Desc("是否自动攻击")]
            public AbstractValue<bool> AutoAttack = new BooleanValue.VALUE(true);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})A过去({1})并还击({2});", Player, TargetPos, AutoAttack);
            }
            override protected object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var p = Player.GetValueAs(api, args);
                var t = TargetPos.GetValueAs(api, args);
                if (p != null && t.HasValue)
                {
                    p.doAction(new UnitAttackToAction()
                    {
                        object_id = p.ObjectID,
                        attack = AutoAttack.GetValueAs(api, args),
                        target = t,
                        name = TargetPos.ToString(),
                    });
                }
                return null;
            }
        }


    }



}
