using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Formula;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using static DeepCore.Game3D.Host.Instance.InstanceZone;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("布尔型")]
    public abstract class ZoneBooleanValue : BooleanValue
    {
        sealed protected override bool GetValue(DeepCore.EventTrigger.EventExecutor api, DeepCore.EventTrigger.IEventArguments args)
        {
            return this.GetValue(api as IEventTriggerAdapter, (EventArguments)args);
        }
        protected abstract bool GetValue(IEventTriggerAdapter api, EventArguments args);
        //-------------------------------------------------------------------------------------------------------------------------------------------
        [Desc("单位比较", "[游戏]/比较")]
        public class UnitComparison : ZoneBooleanValue
        {
            [Desc("单位1")]
            public AbstractValue<InstanceUnit> Value1 = new UnitValue.NA();
            [Desc("比较符")]
            public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
            [Desc("单位2")]
            public AbstractValue<InstanceUnit> Value2 = new UnitValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}){1}({2})", Value1, FormulaHelper.ToString(Op), Value2);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit c1 = Value1.GetValueAs(api, args);
                InstanceUnit c2 = Value2.GetValueAs(api, args);
                return FormulaHelper.Compare(c1, Op, c2);
            }
        }
        [Desc("物品比较", "[游戏]/比较")]
        public class ItemComparison : ZoneBooleanValue
        {
            [Desc("物品1")]
            public AbstractValue<InstanceItem> Value1 = new ItemValue.NA();
            [Desc("比较符")]
            public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
            [Desc("物品2")]
            public AbstractValue<InstanceItem> Value2 = new ItemValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}){1}({2})", Value1, FormulaHelper.ToString(Op), Value2);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceItem c1 = Value1.GetValueAs(api, args);
                InstanceItem c2 = Value2.GetValueAs(api, args);
                return FormulaHelper.Compare(c1, Op, c2);
            }
        }
        [Desc("Flag比较", "[游戏]/比较")]
        public class FlagComparison : ZoneBooleanValue
        {
            [Desc("Flag1")]
            public AbstractValue<InstanceFlag> Value1 = new FlagValue.NA();
            [Desc("比较符")]
            public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
            [Desc("Flag2")]
            public AbstractValue<InstanceFlag> Value2 = new FlagValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}){1}({2})", Value1, FormulaHelper.ToString(Op), Value2);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceFlag c1 = Value1.GetValueAs(api, args);
                InstanceFlag c2 = Value2.GetValueAs(api, args);
                return FormulaHelper.Compare(c1, Op, c2);
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------
        [Desc("Flag是否开启", "[游戏]/Flag状态")]
        public class FlagEnabled : ZoneBooleanValue
        {
            [Desc("Flag")]
            public AbstractValue<InstanceFlag> Flag = new FlagValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}).Enable", Flag);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceFlag flag = Flag.GetValueAs(api, args);
                if (flag != null)
                {
                    return flag.Enable;
                }
                return false;
            }
        }
        [Desc("区域刷新点是否刷新完毕", "[游戏]/Flag状态")]
        public class FlagIsSpawnOver : ZoneBooleanValue
        {
            [Desc("区域")]
            public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})刷新完毕", Region);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    return region.SpawnCollection.IsSpawnOver;
                }
                return false;
            }
        }
        [Desc("区域刷新点是否没有存活单位", "[游戏]/Flag状态")]
        public class FlagIsSpawnNoneAlive : ZoneBooleanValue
        {
            [Desc("区域")]
            public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})没有存活单位", Region);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    return region.SpawnCollection.IsSpawnNoneAlive;
                }
                return false;
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------

        [Desc("场景用户自定义属性", "[游戏]/场景")]
        public class ZoneBooleanAttribute : ZoneBooleanValue
        {
            [Desc("键值")]
            public string Key;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景键值[{0}]", Key);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                try
                {
                    return Boolean.Parse(api.ZoneAPI.GetAttribute(Key) as string);
                }
                catch
                {
                }
                return false;
            }
        }

        [Desc("是否游戏结束", "[游戏]/场景")]
        public class IsGameOver : ZoneBooleanValue
        {
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastGameOver != null;
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------

        #region __UNIT__

        [Desc("单位用户自定义属性", "[游戏]/单位")]
        public class UnitBooleanAttribute : ZoneBooleanValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("键值")]
            public string Key;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})键值[{1}]", Unit, Key);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    try
                    {
                        return Boolean.Parse(unit.GetAttribute(Key) as string);
                    }
                    catch
                    {
                    }
                }
                return false;
            }
        }


        [Desc("单位存活", "[游戏]/单位")]
        public class UnitIsAlived : ZoneBooleanValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})活着", Unit);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return !unit.IsDead;
                }
                return false;
            }
        }

        [Desc("单位在某状态", "[游戏]/单位")]
        public class UnitInState : ZoneBooleanValue
        {
            [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("状态")] public UnitActionStatus Status = UnitActionStatus.Idle;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}在<c color='{2}'>{1}</c>状态", Unit, Status, sw.COLOR_CONST);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.CurrentActionStatus == Status;
                }
                return false;
            }
        }

        [Desc("单位死亡", "[游戏]/单位")]
        public class UnitIsDead : ZoneBooleanValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})死亡", Unit);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.IsDead;
                }
                //不存在则认为死亡
                return true;
            }
        }
        [Desc("单位存在", "[游戏]/单位")]
        public class UnitExist : ZoneBooleanValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})存在", Unit);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                return (unit != null);
            }
        }

        [Desc("单位是否拥有buff", "[游戏]/单位")]
        public class UnitExistsBuff : ZoneBooleanValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

            [Desc("Buff模板ID")]
            [TemplateIDAttribute(typeof(BuffTemplate))]
            public int BuffTemplateID = 0;

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})是否拥有buff({1})状态", Unit, BuffTemplateID);
            }

            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.HasBuff(BuffTemplateID);
                }
                return false;
            }
        }
        [Desc("单位是否拥有光环", "[游戏]/单位")]
        public class UnitExistsAura : ZoneBooleanValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

            [Desc("光环模板ID")]
            [TemplateIDAttribute(typeof(AuraTemplate))]
            public int AuraTemplateID = 0;

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})是否拥有光环{1}", Unit, AuraTemplateID);
            }

            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.HasAura(AuraTemplateID);
                }
                return false;
            }
        }

        [Desc("单位是否拥有道具", "[游戏]/单位")]
        public class UnitHasInventoryItem : ZoneBooleanValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("物品ID")]
            public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})是否拥有道具{1}", Unit, Item);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                ItemTemplate temp = Item.GetValueAs(api, args);
                if (unit != null && temp != null)
                {
                    return unit.Bag.ContainsItemInInventory(temp.ID);
                }
                return false;
            }
        }

        [Desc("玩家是否自动战斗", "[游戏]/单位玩家")]
        public class PlayerIsGuard : ZoneBooleanValue
        {
            [Desc("玩家单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("玩家单位({0})是否自动战斗", Unit);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit is InstancePlayer player)
                {
                    return player.IsGuard;
                }
                return false;
            }
        }

        [Desc("玩家是否准备完毕", "[游戏]/单位玩家")]
        public class PlayerIsReady : ZoneBooleanValue
        {
            [Desc("玩家单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("玩家单位({0})是否准备完毕", Unit);
            }

            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit is InstancePlayer player)
                {
                    return player.IsReady;
                }
                return false;
            }
        }

        [Desc("是否为玩家", "[游戏]/单位玩家")]
        public class UnitIsPlayer : ZoneBooleanValue
        {
            [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})是否为玩家", Unit);
            }

            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit is InstancePlayer player)
                {
                    return true;
                }
                return false;
            }
        }



        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------
        #region __TOUCH__

        [Desc("两单位是否碰撞", "[游戏]/碰撞检测")]
        public class UnitTouch : ZoneBooleanValue
        {
            [Desc("Unit1")]
            public AbstractValue<InstanceUnit> Unit1 = new UnitValue.Trigging();
            [Desc("Unit2")]
            public AbstractValue<InstanceUnit> Unit2 = new UnitValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})和({1})相碰撞", Unit1, Unit2);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var u1 = Unit1.GetValueAs(api, args);
                var u2 = Unit2.GetValueAs(api, args);
                if (u1 != null && u2 != null)
                {
                    return api.ZoneAPI.TouchObject2(u1, u2);
                }
                return false;
            }
        }
        [Desc("两道具是否碰撞", "[游戏]/碰撞检测")]
        public class ItemTouch : ZoneBooleanValue
        {
            [Desc("Item1")]
            public AbstractValue<InstanceItem> Item1 = new ItemValue.Trigging();
            [Desc("Item2")]
            public AbstractValue<InstanceItem> Item2 = new ItemValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})和({1})相碰撞", Item1, Item2);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var u1 = Item1.GetValueAs(api, args);
                var u2 = Item2.GetValueAs(api, args);
                if (u1 != null && u2 != null)
                {
                    return api.ZoneAPI.TouchObject2(u1, u2);
                }
                return false;
            }
        }




        [Desc("点是否在区域内", "[游戏]/碰撞检测")]
        public class PositionIsInRegion : ZoneBooleanValue
        {
            [Desc("坐标")]
            public AbstractValue<Vector3?> P = new PositionValue.VALUE();
            [Desc("区域")]
            public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})在({1})内", P, Region);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var u = P.GetValueAs(api, args);
                var r = Region.GetValueAs(api, args) as ZoneRegion;
                if (u != null && r != null)
                {
                    return r.IsInRegion(u.Value);
                }
                return false;
            }
        }
        [Desc("单位是否在区域内", "[游戏]/碰撞检测")]
        public class UnitIsInRegion : ZoneBooleanValue
        {
            [Desc("Unit")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("区域")]
            public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})在({1})内", Unit, Region);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var u = Unit.GetValueAs(api, args);
                var r = Region.GetValueAs(api, args) as ZoneRegion;
                if (u != null && r != null)
                {
                    return r.IsInRegion(u);
                }
                return false;
            }
        }
        [Desc("道具是否在区域内", "[游戏]/碰撞检测")]
        public class ItemIsInRegion : ZoneBooleanValue
        {
            [Desc("Item")]
            public AbstractValue<InstanceItem> Item = new ItemValue.Trigging();
            [Desc("区域")]
            public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})在({1})内", Item, Region);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var u = Item.GetValueAs(api, args);
                var r = Region.GetValueAs(api, args) as ZoneRegion;
                if (u != null && r != null)
                {
                    return r.IsInRegion(u);
                }
                return false;
            }
        }



        [Desc("是否可攻击", "[游戏]/攻击检测")]
        public class IsAttackableValue : ZoneBooleanValue
        {
            [Desc("攻击者")]
            public AbstractValue<InstanceUnit> Src = new UnitValue.LastHitted();
            [Desc("被攻击者")]
            public AbstractValue<InstanceUnit> Dst = new UnitValue.LastAttack();
            [Desc("希望目标")]
            public SkillTemplate.CastTarget ExpectTarget = SkillTemplate.CastTarget.Enemy;
            [Desc("攻击原因")]
            public DeepMetaGame.Data.Misc.AttackReason Reason = DeepMetaGame.Data.Misc.AttackReason.Attack;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("造成伤害的攻击特殊状态");
            }
            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var src = Src.GetValueAs(api, args);
                var dst = Dst.GetValueAs(api, args);
                if (src != null && dst != null)
                {
                    return api.ZoneAPI.Formula.IsAttackable(src, dst, ExpectTarget, Reason, src.Info);
                }
                return false;
            }
        }


        //         [Desc("点是否在Area内", "碰撞检测")]
        //         public class PositionIsInArea : ZoneBooleanValue
        //         {
        //             [Desc("坐标")]
        //             public AbstractValue<Vector3?> P = new PositionValue.VALUE();
        //             [Desc("Area")]
        //             public AbstractValue<InstanceFlag> Area = new FlagValue.EditorArea();
        //             protected override void GetText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("({0})在({1})内", P, Area);
        //             }
        //             protected override Boolean GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 var u = P.GetValueAs(api, args);
        //                 var r = Area.GetValueAs(api, args) as ZoneArea;
        //                 if (u != null && r != null)
        //                 {
        //                     return api.ZoneAPI.GetArea(u) == r;
        //                 }
        //                 return false;
        //             }
        //         }
        //         [Desc("单位是否在Area内", "碰撞检测")]
        //         public class UnitIsInArea : ZoneBooleanValue
        //         {
        //             [Desc("Unit")]
        //             public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        //             [Desc("Area")]
        //             public AbstractValue<InstanceFlag> Area = new FlagValue.EditorArea();
        //             protected override void GetText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("({0})在({1})内", Unit, Area);
        //             }
        //             protected override Boolean GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 var u = Unit.GetValueAs(api, args);
        //                 var r = Area.GetValueAs(api, args) as ZoneArea;
        //                 if (u != null && r != null)
        //                 {
        //                     return u.CurrentArea == r;
        //                 }
        //                 return false;
        //             }
        //         }
        //         [Desc("道具是否在Area内", "碰撞检测")]
        //         public class ItemIsInArea : ZoneBooleanValue
        //         {
        //             [Desc("Item")]
        //             public AbstractValue<InstanceItem> Item = new ItemValue.Trigging();
        //             [Desc("Area")]
        //             public AbstractValue<InstanceFlag> Area = new FlagValue.EditorArea();
        //             protected override void GetText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("({0})在({1})内", Item, Area);
        //             }
        //             protected override Boolean GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 var u = Item.GetValueAs(api, args);
        //                 var r = Area.GetValueAs(api, args) as ZoneArea;
        //                 if (u != null && r != null)
        //                 {
        //                     return u.CurrentArea == r;
        //                 }
        //                 return false;
        //             }
        //         }



        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------
        #region __NOT_NULL__

        [Desc("Flag是否存在", "[游戏]/Not Null")]
        public class FlagNotNull : ZoneBooleanValue
        {
            [Desc("Flag")]
            public AbstractValue<InstanceFlag> Flag = new FlagValue.TriggingRegion();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}).NotNull", Flag);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var flag = Flag.GetValueAs(api, args);
                if (flag != null)
                {
                    return true;
                }
                return false;
            }
        }

        [Desc("单位是否存在", "[游戏]/Not Null")]
        public class UnitNotNull : ZoneBooleanValue
        {
            [Desc("Unit")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}).NotNull", Unit);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var flag = Unit.GetValueAs(api, args);
                if (flag != null)
                {
                    return true;
                }
                return false;
            }
        }

        [Desc("物品是否存在", "[游戏]/Not Null")]
        public class ItemNotNull : ZoneBooleanValue
        {
            [Desc("Unit")]
            public AbstractValue<InstanceItem> Unit = new ItemValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}).NotNull", Unit);
            }
            protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var flag = Unit.GetValueAs(api, args);
                if (flag != null)
                {
                    return true;
                }
                return false;
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------

        //         [Desc("游戏是否暂停", "[游戏]")]
        //         public class GetGamePauseValue : ZoneBooleanValue
        //         {
        //             protected override bool GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 return api.ZoneAPI.IsPause;
        //             }
        //         }

        //------------------------------------------------------------------------------------------------------------
    }
}
