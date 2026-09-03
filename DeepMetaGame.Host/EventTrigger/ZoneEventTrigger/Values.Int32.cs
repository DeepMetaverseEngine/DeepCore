using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("整形")]
    public abstract class ZoneIntegerValue : IntegerValue
    {
        sealed protected override double GetValue(DeepCore.EventTrigger.EventExecutor api, DeepCore.EventTrigger.IEventArguments args)
        {
            return this.GetValue(api as IEventTriggerAdapter, (EventArguments)args);
        }
        protected abstract double GetValue(IEventTriggerAdapter api, EventArguments args);
        // 
        //         [Desc("值", "值")]
        //         public class VALUE : ZoneIntegerValue
        //         {
        //             [Desc("值")]
        //             public int Value = 10;
        //             public VALUE() { }
        //             public VALUE(int v)
        //             {
        //                 this.Value = v;
        //             }
        //             public override void ToFunctionText(EventStringBuilder sw)
        //             {
        //                 sw.Append("<c color='" + sw.COLOR_CONST + "'>").Append(Value).Append("</c>");
        //             }
        //             protected override double GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 return Value;
        //             }
        //         }
        // 
        //         [Desc("整形计算", "数学")]
        //         public class IntegerOP : ZoneIntegerValue
        //         {
        //             [Desc("值1")]
        //             public AbstractValue<double> Value1 = new IntegerValue.VALUE();
        //             [Desc("运算符")]
        //             public NumericOP OP = NumericOP.ADD;
        //             [Desc("值2")]
        //             public AbstractValue<double> Value2 = new IntegerValue.VALUE();
        //             public override void ToFunctionText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("({0}) {1} ({2})", Value1, FormulaHelper.ToString(OP), Value2);
        //             }
        //             protected override double GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 int ret = FormulaHelper.Calculate(Value1.GetValueAs(api, args), OP, Value2.GetValueAs(api, args));
        //                 return ret;
        //             }
        //         }
        // 
        //         [Desc("函数-返回最小值", "数学")]
        //         public class MinFunction : ZoneIntegerValue
        //         {
        //             [Desc("值1")]
        //             public AbstractValue<double> Value1 = new IntegerValue.VALUE();
        //             [Desc("值2")]
        //             public AbstractValue<double> Value2 = new IntegerValue.VALUE();
        // 
        //             public override void ToFunctionText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("取{0}和{1}最小值", Value1, Value2);
        //             }
        //             protected override double GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 int v1 = Value1.GetValueAs(api, args);
        //                 int v2 = Value2.GetValueAs(api, args);
        //                 return Math.Min(v1, v2);
        //             }
        //         }
        // 
        //         [Desc("函数-返回最大值", "数学")]
        //         public class MaxFunction : ZoneIntegerValue
        //         {
        //             [Desc("值1")]
        //             public AbstractValue<double> Value1 = new IntegerValue.VALUE();
        //             [Desc("值2")]
        //             public AbstractValue<double> Value2 = new IntegerValue.VALUE();
        // 
        //             public override void ToFunctionText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("取{0}和{1}最大值", Value1, Value2);
        //             }
        //             protected override double GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 int v1 = Value1.GetValueAs(api, args);
        //                 int v2 = Value2.GetValueAs(api, args);
        //                 return Math.Max(v1, v2);
        //             }
        //         }
        // 
        //         [Desc("随机整形", "数学")]
        //         public class RandomInt : ZoneIntegerValue
        //         {
        //             [Desc("最小值")]
        //             public AbstractValue<double> Min = new IntegerValue.VALUE(0);
        //             [Desc("最大值(小于)")]
        //             public AbstractValue<double> Max = new IntegerValue.VALUE(10);
        //             public override void ToFunctionText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("{0}~{1}(不包括)随机数", Min, Max);
        //             }
        //             protected override double GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 int max = Max.GetValueAs(api, args);
        //                 int min = Min.GetValueAs(api, args);
        //                 return api.ZoneAPI.RandomN.Next(min, max);
        //             }
        //         }
        // 
        // 
        //         [Desc("从小数转换", "转换")]
        //         public class ConvertFromReal : ZoneIntegerValue
        //         {
        //             [Desc("值")]
        //             public AbstractValue<double> Value = new RealValue.VALUE();
        // 
        //             public override void ToFunctionText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("从{0}转换", Value);
        //             }
        //             protected override double GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 float value = Value.GetValueAs(api, args);
        //                 return (int)value;
        //             }
        //         }
        // 
        //         [Desc("从字符串转换", "转换")]
        //         public class ParseFromString : ZoneIntegerValue
        //         {
        //             [Desc("值")]
        //             public AbstractValue<string> Value = new StringValue.VALUE("1");
        // 
        //             public override void ToFunctionText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("从{0}转换", Value);
        //             }
        //             protected override double GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 string value = Value.GetValueAs(api, args);
        //                 int ret = 0;
        //                 int.TryParse(value, out ret);
        //                 return ret;
        //             }
        //         }
        // 
        //         [Desc("迭代中的整形", "循环迭代")]
        //         public class PickingIteratorInt32 : ZoneIntegerValue
        //         {
        //             public override void ToFunctionText(EventStringBuilder sw)
        //             {
        //                 sw.Append("迭代中的整形");
        //             }
        //             protected override double GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 return args.IteratingInt32;
        //             }
        //         }

        //----------------------------------------------------------------------------------------------
        #region __场景__

        [Desc("游戏运行时间(秒)", "[游戏]/场景")]
        public class TotalTimeSEC : ZoneIntegerValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("游戏运行时间(秒)");
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return (int)api.ZoneAPI.PassTimeSEC;
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------


        [Desc("场景内所有物品数量", "[游戏]/场景")]
        public class ZoneTotalItemCount : ZoneIntegerValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景内所有物品数量");
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.AllItemsCount;
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------

        [Desc("阵营总共死亡数量", "[游戏]/场景")]
        public class TotalForceDeadCount : ZoneIntegerValue
        {
            [Desc("阵营")]
            public AbstractValue<double> SelectForce;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}阵营总共死亡数量", SelectForce);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                byte force = (byte)SelectForce.GetValueAs(api, args);
                return api.ZoneAPI.GetTotalForceDead(force); ;
            }
        }

        [Desc("场景内所有单位数量", "[游戏]/场景")]
        public class ZoneTotalUnitCount : ZoneIntegerValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景内所有单位数量");
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.AllUnitsCount;
            }
        }
        [Desc("场景内某阵营单位数量", "[游戏]/场景")]
        public class ZoneTotalForceUnitCount : ZoneIntegerValue
        {
            [Desc("阵营")]
            public AbstractValue<double> SelectForce;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景内阵营{0}单位数量", SelectForce);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                byte force = (byte)SelectForce.GetValueAs(api, args);
                return api.ZoneAPI.GetForceUnitsCount(force); ;
            }
        }
        
        [Desc("场景内某阵营单位存活数量", "[游戏]/场景")]
        public class ZoneTotalAliveUnitCount : ZoneIntegerValue
        {
            [Desc("阵营")]
            public AbstractValue<double> SelectForce;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景内阵营{0}单位存活数量", SelectForce);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                byte force = (byte)SelectForce.GetValueAs(api, args);
                return api.ZoneAPI.GetForceAliveUnitsCount(force); ;
            }
        }


        [Desc("场景用户自定义属性", "[游戏]/场景")]
        public class ZoneIntegerAttribute : ZoneIntegerValue
        {
            [Desc("键值")]
            public string Key;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景键值[{0}]", Key);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                try
                {
                    return Parser.ParseInt(api.ZoneAPI.GetAttribute(Key) as string);
                }
                catch
                {
                }
                return 0;
            }
        }

        [Desc("场景定义Flag地块", "[游戏]/场景")]
        public class ZoneTerrainDefintionFlag : ZoneIntegerValue
        {
            [Desc("键值")]
            public int Index;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景定义Flag地块[{0}]", Index);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                if (api.ZoneAPI.TerrainDefinition.TryGetMapBlockBrushByIndex(Index, out var brush))
                {
                    return brush.Value;
                }
                return 0;
            }
        }

        [Desc("场景定义Flag地块 ByFlag", "[游戏]/场景")]
        public class ZoneTerrainDefintionFlagByFlag : ZoneIntegerValue
        {
            [Desc("键值")]
            public MapBlockBrushFlag Flag = MapBlockBrushFlag.Walkable;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景定义Flag地块[{0}]", Flag);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                if (api.ZoneAPI.TerrainDefinition.TryGetMapBlockBrushByFlag(Flag, out var brush))
                {
                    return brush.Value;
                }
                return 0;
            }
        }

        [Desc("场景定义Flag地块 ByName", "[游戏]/场景")]
        public class ZoneTerrainDefintionFlagByName : ZoneIntegerValue
        {
            [Desc("键值")]
            public string Name = "Safe";
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("场景定义Flag地块[{0}]", Name);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                if (api.ZoneAPI.TerrainDefinition.TryGetMapBlockBrushByName(Name, out var brush))
                {
                    return brush.Value;
                }
                return 0;
            }
        }


        #endregion
        //----------------------------------------------------------------------------------------------
        #region __单位__

        [Desc("单位用户自定义属性", "[游戏]/单位-属性")]
        public class UnitIntegerAttribute : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("键值")]
            public string Key;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})键值[{1}]", Unit, Key);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    try
                    {
                        return Parser.ParseInt(unit.GetAttribute(Key) as string);
                    }
                    catch { }
                    return 0;
                }
                return 0;
            }
        }

        [Desc("单位模板ID", "[游戏]/单位-属性")]
        public class UnitTemplateID : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})模板ID", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.TemplateID;
                }
                return 0;
            }
        }

        [Desc("单位当前主状态", "[游戏]/单位-属性")]
        public class UnitCurrentMainState : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})当前主状态", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.CurrentActionStatusInt32;
                }
                return 0;
            }
        }
        [Desc("单位当前子状态", "[游戏]/单位-属性")]
        public class UnitCurrentSubState : ZoneStringValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})当前子状态", Unit);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.CurrentActionSubstate;
                }
                return null;
            }
        }



        [Desc("单位血量", "[游戏]/单位-属性")]
        public class UnitHP : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0}).HP", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.CurrentHP;
                }
                return 0;
            }
        }
        [Desc("单位最大血量", "[游戏]/单位-属性")]
        public class UnitMaxHP : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0}).MaxHP", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.MaxHP;
                }
                return 0;
            }
        }
        [Desc("单位等级", "[游戏]/单位-属性")]
        public class UnitLevel : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0}).Level", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.Level;
                }
                return 0;
            }
        }
        [Desc("单位阵营", "[游戏]/单位-属性")]
        public class UnitForce : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0}).阵营", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.Force;
                }
                return 0;
            }
        }
        [Desc("单位类型", "[游戏]/单位-属性")]
        public class UnitTypeInt32 : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0}).类型", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.UTypeAsInt;
                }
                return 0;
            }
        }
        [Desc("单位类型(常量)", "[游戏]/单位-属性")]
        public class UnitTypeConst : ZoneIntegerValue
        {
            [Desc("单位类型")]
            public UnitType UType =  UnitType.TYPE_PLAYER;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat($"{UType}");
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return (int)UType;
            }
        }

        [Desc("单位-死亡次数", "[游戏]/单位-统计")]
        public class UnitDeadCount : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})死亡次数", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.Statistic.DeadCount;
                }
                return 0;
            }
        }
        [Desc("单位-总共杀死怪物数量", "[游戏]/单位-统计")]
        public class UnitKillUnitCount : ZoneIntegerValue
        {
            [Desc("杀死的单位类型")]
            public UnitType KillType = UnitType.TYPE_MONSTER;
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})总共杀死怪物数量", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.Statistic.GetKillUnitCount(KillType);
                }
                return 0;
            }
        }

        [Desc("单位-总共杀死玩家数量", "[游戏]/单位-统计")]
        public class UnitKillPlayerCount : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}总共杀死玩家数量", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.Statistic.KillPlayerCount;
                }
                return 0;
            }
        }

        [Desc("单位拥有道具数量", "[游戏]/单位-属性")]
        public class UnitInventoryItemCount : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("物品ID")]
            public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})拥有道具{1}数量", Unit, Item);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                ItemTemplate temp = Item.GetValueAs(api, args);
                if (unit != null && temp != null)
                {
                    return unit.Bag.GetItemCountInInventory(temp.ID);
                }
                return 0;
            }
        }

        [Desc("单位拥有Buff层数", "[游戏]/单位-功能")]
        public class UnitBuffOverlay : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("Buff模板ID")]
            [TemplateIDAttribute(typeof(BuffTemplate))]
            public int BuffTemplateID = 0;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})拥有Buff({1})层数", Unit, BuffTemplateID.ToString());
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.GetBuffOverlay(BuffTemplateID);
                }
                return 0;
            }
        }

        [Desc("单位当前Flag地块", "[游戏]/单位-功能")]
        public class UnitCurrentZoneInfoFlag : ZoneIntegerValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位({0})当前Flag地块", Unit);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.CurrentZoneInfoFlagget;
                }
                return 0;
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------
        #region __FLAGS__


        [Desc("区域内单位数量", "[游戏]/区域")]
        public class RegionUnitCount : ZoneIntegerValue
        {
            [Desc("区域")]
            public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("区域({0})内单位数量", Region);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    //return region.getObjectsCountInRegion<InstanceUnit>();
                    return region.InRegionUnitCount;
                }
                return 0;
            }
        }

        [Desc("区域内指定阵营单位数量", "[游戏]/区域")]
        public class RegionForceUnitCount : ZoneIntegerValue
        {
            [Desc("区域")]
            public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

            [Desc("阵营")]
            public AbstractValue<double> Force = new IntegerValue.VALUE(0);

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("区域({0})内阵营{1}的单位数量", Region, Force);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    byte force = (byte)Force.GetValueAs(api, args);
                    //                     int ret = region.getObjectsCountInRegion<InstanceUnit>((InstanceUnit unit) =>
                    //                         {
                    //                             return (unit.Force == force);
                    //                         });
                    //                     return ret;
                    int ret = 0;
                    region.ForEachObjectsInRegion<InstanceUnit>(u =>
                    {
                        if (u.Force == force)
                        {
                            ret++;
                        }
                        return false;
                    });
                    return ret;
                }
                return 0;
            }
        }

        [Desc("区域内指定阵营和类型单位数量", "[游戏]/区域")]
        public class RegionForceTypeUnitCount : ZoneIntegerValue
        {
            [Desc("区域")]
            public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

            [Desc("阵营")]
            public AbstractValue<double> Force = new IntegerValue.VALUE(0);

            [Desc("类型")]
            public UnitType ObjType = UnitType.TYPE_PLAYER;

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("区域({0})内阵营{1}的类型为{2}单位数量", Region, Force, ObjType);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    byte force = (byte)Force.GetValueAs(api, args);
                    //                     int ret = region.getObjectsCountInRegion<InstanceUnit>((InstanceUnit unit) =>
                    //                     {
                    //                         if (unit.Info.UType == ObjType && unit.Force == force)
                    //                         {
                    //                             return true;
                    //                         }
                    //                         return false;
                    //                     });
                    //                     return ret;
                    int ret = 0;
                    region.ForEachObjectsInRegion<InstanceUnit>(u =>
                    {
                        if (u.UType == ObjType && u.Force == force)
                        {
                            ret++;
                        }
                        return false;
                    });
                    return ret;
                }
                return 0;
            }
        }


        [Desc("遍历区域内满足条件单位数量", "[游戏]/区域")]
        public class RegionExpectUnitCount : ZoneIntegerValue
        {
            [Desc("区域")]
            public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

            [Desc("条件")]
            public AbstractValue<bool> Condition = new ZoneBooleanValue.UnitIsAlived();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("遍历区域({0})内满足({1})的单位数量", Region, Condition);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                IEventTriggerAdapter evtapi = api as IEventTriggerAdapter;
                if (evtapi != null)
                {
                    ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
                    if (region != null)
                    {

                        int ret = 0;
                        //                      using (var units = new List<InstanceUnit>())
                        //                      {
                        //                          //args = args.Clone();
                        //                          region.getObjectsInRegion<InstanceUnit>(units);
                        //                          foreach (InstanceUnit u in units)
                        //                          {
                        //                              args.IteratingUnit = (u);
                        //                              bool coodi = Condition.GetValueAs(api, args);
                        //                              if (coodi)
                        //                              {
                        //                                  ret++;
                        //                              }
                        //                              args.IteratingUnit = (null);
                        //                          }
                        //                      }
                        region.ForEachObjectsInRegion<InstanceUnit>(u =>
                        {
                            args.IteratingObject = (u);
                            bool coodi = Condition.GetValueAs(api, args);
                            if (coodi)
                            {
                                ret++;
                            }
                            args.IteratingObject = (null);
                            return false;
                        });
                        return ret;
                    }
                }
                return 0;
            }
            [TriggingArg("迭代中的单位")] public InstanceUnit Iterating(EventArguments args) => args.IteratingObject as InstanceUnit;
        }

        #endregion
        //----------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------------------

    }

}
