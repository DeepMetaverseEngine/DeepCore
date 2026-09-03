using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;
using static DeepCore.Game3D.Host.Instance.InstanceZone;
using static DeepCore.GameData.Zone.ZoneEditor.EventTrigger.FlagValue;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("关闭Flag(包括空气墙)", "[游戏]/Flag")]
    public class FlagCloseAction : ZoneAbstractAction
    {
        [Desc("Flag")]
        public AbstractValue<InstanceFlag> Flag = new FlagValue.EditorPoint();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("关闭({0});", Flag);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var flag = Flag.GetValueAs(api, args);
            if (flag != null)
            {
                flag.Enable = (false);
            }
            return flag;
        }
    }

    [Desc("开启Flag(包括空气墙)", "[游戏]/Flag")]
    public class FlagOpenAction : ZoneAbstractAction
    {
        [Desc("Flag")]
        public AbstractValue<InstanceFlag> Flag = new FlagValue.EditorPoint();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("开启({0});", Flag);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var flag = Flag.GetValueAs(api, args);
            if (flag != null)
            {
                flag.Enable = (true);
            }
            return flag;
        }
    }

    [Desc("开启/关闭Flag(包括空气墙)", "[游戏]/Flag")]
    public class FlagOnOffAction : ZoneAbstractAction
    {
        [Desc("Flag")]
        public AbstractValue<InstanceFlag> Flag = new FlagValue.EditorPoint();
        [Desc("Enable")]
        public AbstractValue<bool> Enable = new BooleanValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.Enable={1};", Flag, Enable);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var flag = Flag.GetValueAs(api, args);
            if (flag != null)
            {
                flag.Enable = Enable.GetValueAs(api, args);
            }
            return flag;
        }
    }

    [Desc("切换Flag(包括空气墙)", "[游戏]/Flag")]
    public class FlagSwitchAction : ZoneAbstractAction
    {
        [Desc("Flag")]
        public AbstractValue<InstanceFlag> Flag = new FlagValue.EditorPoint();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("切换({0});", Flag);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var flag = Flag.GetValueAs(api, args);
            if (flag != null)
            {
                flag.Enable = (!flag.Enable);
            }
            return flag;
        }
    }

    [Desc("关闭FlagGroup(包括空气墙)", "[游戏]/Flag")]
    public class FlagGroupCloseAction : ZoneAbstractAction
    {
        [Desc("FlagGroup")]
        [SceneObjectGroup]
        public string FlagGroup = "";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("关闭({0})所有Flag;", FlagGroup);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.ForEachFlags((api, args, FlagGroup), static (st, flag) =>
            {
                if (flag.EditorPath.StartsWith(st.FlagGroup))
                {
                    flag.Enable = (false);
                }
                return false;
            });
            return null;
        }
    }

    [Desc("开启FlagGroup(包括空气墙)", "[游戏]/Flag")]
    public class FlagGroupOpenAction : ZoneAbstractAction
    {
        [Desc("FlagGroup")]
        [SceneObjectGroup]
        public string FlagGroup = "";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("开启({0})所有Flag;", FlagGroup);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.ForEachFlags((api, args, FlagGroup), static (st, flag) =>
            {
                if (flag.EditorPath.StartsWith(st.FlagGroup))
                {
                    flag.Enable = (true);
                }
                return false;
            });
            return null;
        }
    }




    [Desc("设置Flag的Tag", "[游戏]/Flag")]
    public class FlagTagAction : ZoneAbstractAction
    {
        [Desc("Flag")]
        public AbstractValue<InstanceFlag> Flag = new FlagValue.EditorPoint();
        [Desc("Tag")]
        public AbstractValue<string> Tag = new StringValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.Tag={1};", Flag, Tag);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var flag = Flag.GetValueAs(api, args);
            var tag = Tag.GetValueAs(api, args);
            if (flag != null)
            {
                flag.Tag = tag;
            }
            return flag;
        }
    }



    [Desc("当指定单位进入区域一次DO", "[游戏]/Flag")]
    public class WhenUnitEnterRegionOnceDo : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
        [Desc("动作")]
        public AbstractAction Action = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("<c color='" + sw.COLOR_KEYWORKD + "'>WHEN</c> ").Append(Unit).Append("进入").Append(Region).Append("一次 <c color='" + sw.COLOR_KEYWORKD + "'>DO</c>").AppendLine();
            sw.IndentBegin("{");
            sw.AppendLine(Action);
            sw.IndentEnd("}");
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var region = Region.GetValueAs(api, args) as ZoneRegion;
            var unit = Unit.GetValueAs(api, args);
            if (region != null && unit != null)
            {
                region.ListenUnitEnterOnce(unit, (r, u) =>
                {
                    //var args2 = args.Clone();
                    args.TriggingFlag = r;
                    args.TriggingUnit = u;
                    Action.Invoke(api, args);
                });
            }
            return null;
        }
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }
    [Desc("当指定单位离开区域一次DO", "[游戏]/Flag")]
    public class WhenUnitLeaveRegionOnceDo : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
        [Desc("动作")]
        public AbstractAction Action = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("<c color='" + sw.COLOR_KEYWORKD + "'>WHEN</c> ").Append(Unit).Append("离开").Append(Region).Append("一次 <c color='" + sw.COLOR_KEYWORKD + "'>DO</c>").AppendLine();
            sw.IndentBegin("{");
            sw.AppendLine(Action);
            sw.IndentEnd("}");
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var region = Region.GetValueAs(api, args) as ZoneRegion;
            var unit = Unit.GetValueAs(api, args);
            if (region != null && unit != null)
            {
                region.ListenUnitLeaveOnce(unit, (r, u) =>
                {
                    //var args2 = args.Clone();
                    args.TriggingFlag = r;
                    args.TriggingUnit = u;
                    Action.Invoke(api, args);
                });
            }
            return null;
        }
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }


    [Desc("当指定单位进入Area一次DO", "[游戏]/Flag")]
    public class WhenUnitEnterAreaOnceDo : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("Area")]
        public AbstractValue<InstanceFlag> Area = new FlagValue.EditorArea();
        [Desc("动作")]
        public AbstractAction Action = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("<c color='" + sw.COLOR_KEYWORKD + "'>WHEN</c> ").Append(Unit).Append("进入").Append(Area).Append("一次 <c color='" + sw.COLOR_KEYWORKD + "'>DO</c>").AppendLine();
            sw.IndentBegin("{");
            sw.AppendLine(Action);
            sw.IndentEnd("}");
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var area = Area.GetValueAs(api, args) as ZoneArea;
            var unit = Unit.GetValueAs(api, args);
            if (area != null && unit != null)
            {
                area.ListenUnitEnterOnce(unit, (r, u) =>
                {
                    //var args2 = args.Clone();
                    args.TriggingFlag = r;
                    args.TriggingUnit = u;
                    Action.Invoke(api, args);
                });
            }
            return null;
        }
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }
    [Desc("当指定单位离开Area一次DO", "[游戏]/Flag")]
    public class WhenUnitLeaveAreaOnceDo : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("Area")]
        public AbstractValue<InstanceFlag> Area = new FlagValue.EditorArea();
        [Desc("动作")]
        public AbstractAction Action = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("<c color='" + sw.COLOR_KEYWORKD + "'>WHEN</c> ").Append(Unit).Append("离开").Append(Area).Append("一次 <c color='" + sw.COLOR_KEYWORKD + "'>DO</c>").AppendLine();
            sw.IndentBegin("{");
            sw.AppendLine(Action);
            sw.IndentEnd("}");
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var area = Area.GetValueAs(api, args) as ZoneArea;
            var unit = Unit.GetValueAs(api, args);
            if (area != null && unit != null)
            {
                area.ListenUnitLeaveOnce(unit, (r, u) =>
                {
                    args.TriggingFlag = r;
                    args.TriggingUnit = u;
                    Action.Invoke(api, args);
                });
            }
            return null;
        }
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("添加位面空气墙", "[游戏]/Flag")]
    public class AddAoiDecorationAction : ZoneAbstractAction
    {
        [Desc("位面宿主单位")]
        public AbstractValue<InstanceUnit> AoiMasterUnit = new UnitValue.Trigging();
        [Desc("空气墙位置")]
        public AbstractValue<InstanceFlag> CopyDecoration = new FlagValue.EditorDecoration();

        [Desc("建筑模板ID")]
        [TemplateIDAttribute(typeof(UnitInfo))]
        public int BuildingTemplateID = 0;
        [Desc("单位等级")]
        [TemplateLevelAttribute]
        public AbstractValue<double> UnitLevel = new IntegerValue.VALUE(0);
        [Desc("单位阵营")]
        public AbstractValue<double> Force = new IntegerValue.VALUE(0);
        [Desc("用户定义名字(编辑器名字)")]
        public AbstractValue<string> UnitName = new StringValue.VALUE(null);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}添加位面空气墙{1};", AoiMasterUnit, CopyDecoration);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var owner = AoiMasterUnit.GetValueAs(api, args);
            var decoration = CopyDecoration.GetValueAs(api, args);
            var template = api.Templates.GetUnit(BuildingTemplateID);
            if (owner != null && decoration != null && template != null)
            {
                var unit = api.ZoneAPI.AddUnit(new Data.AddUnitParam()
                {
                    template = template,
                    direction = decoration.Direction,
                    pos = decoration.Position,
                    force = (byte)Force.GetValueAs(api, args),
                    level = (int)UnitLevel.GetValueAs(api, args),
                    name = UnitName.GetValueAs(api, args),
                });
                unit.AoiStatus = owner.AoiStatus;
                unit.ZoneShape = decoration.ZoneShape;
                return unit;
            }
            return null;
        }
    }

    [Desc("遍所有Flag", "[游戏]/Flag")]
    public class EveryFlagDoAction : ZoneAbstractAction
    {
        [Desc("动作")]
        public AbstractAction Action = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendForEach(
                sw1 => sw.AppendFormat("遍所有Flag"),
                sw2 => sw.AppendLine(Action));
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.ForEachFlags((api, args, Action), static (st, u) =>
            {
                st.args.IteratingObject = (u);
                st.Action.Invoke(st.api, st.args);
                st.args.IteratingObject = (null);
                return false;
            });
            return null;
        }
        [TriggingArg("迭代中的Flag")] public InstanceFlag Iterating(EventArguments args) => args.IteratingObject as InstanceFlag;
    }


    [Desc("Flag重置单位刷新", "[游戏]/Flag")]
    public class FlagResetSpawnAction : ZoneAbstractAction
    {
        [Desc("Flag")]
        public AbstractValue<InstanceFlag> Flag = new FlagValue.EditorPoint();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})重置单位刷新;", Flag);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var flag = Flag.GetValueAs(api, args);
            if (flag is ZoneRegion region)
            {
                region.SpawnCollection.ResetSpawn();
            }
            return flag;
        }
    }





    [Desc("Flag手动刷一次怪", "[游戏]/Flag")]
    public class RegionManualSpawn : ZoneAbstractAction
    {
        [Desc("Flag")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
        [Desc("一次刷多少个")]
        public AbstractValue<double> TotalCount = new ZoneIntegerValue.VALUE(5);
        [Desc("刷新一只后")]
        public AbstractAction SpawnedAction;
        [Desc("刷新怪物全部死亡后")]
        public AbstractAction AllDeadAction;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})区域手动刷({1})个怪", Region, TotalCount);
            if (SpawnedAction != null)
            {
                sw.AppendLine();
                sw.AppendLine("刷新一只后：");
                sw.IndentBegin("{");
                sw.AppendLine(SpawnedAction);
                sw.IndentEnd("}");
            }
            if (AllDeadAction != null)
            {
                sw.AppendLine();
                sw.AppendLine("刷新怪物全部死亡后：");
                sw.IndentBegin("{");
                sw.AppendLine(AllDeadAction);
                sw.IndentEnd("}");
            }
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.ZoneAPI is InstanceZone zone)
            {
                var flag = Region?.GetValueAs<ZoneRegion>(api, args);
                if (flag is ZoneRegion)
                {
                    var totalCount = (int)TotalCount.GetValueAs(api, args);
                    var spawned_map = new HashSet<InstanceUnit>();
                    flag.SpawnCollection.ManualSpawn(spawned_map, totalCount, (spawned_map, spawn, obj) =>
                    {
                        if (obj is InstanceUnit unit)
                        {
                            spawned_map.Add(unit);
                            if (SpawnedAction != null)
                            {
                                var arg2 = args;
                                arg2.TriggingFlag = spawn.Container as InstanceFlag;
                                unit.OnFirstActivated += (u) =>
                                {
                                    arg2.TriggingUnit = u;
                                    SpawnedAction.Invoke(api, arg2);
                                };
                            }
                            if (AllDeadAction != null)
                            {
                                var arg2 = args;
                                arg2.TriggingFlag = spawn.Container as InstanceFlag;
                                unit.OnDead += (unit, attacker) =>
                                {
                                    spawned_map.Remove(unit);
                                    if (spawned_map.Count == 0)
                                    {
                                        // 全部怪物死亡
                                        arg2.TriggingUnit = unit;
                                        AllDeadAction.Invoke(api, arg2);
                                    }
                                };
                            }
                        }
                    });
                }
            }
            return 0;
        }
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }




    [Desc(Category = "[游戏]/Flag", Desc = "区域内刷一波指定怪（保持阵型）")]
    public class SpawnAttachmentInRegion : ZoneAbstractAction
    {
        [Desc("召唤者")]
        public UnitValue Summoner = new UnitValue.Trigging();
        [Desc("区域")]
        public FlagValue Region = new EditorRegion();
        [Desc("召唤的单位")]
        public UnitTemplateValue SummonTempID = new UnitTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}在区域{1}内召唤单位{2};", Summoner, Region, SummonTempID);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            if (Summoner.GetValueAs(api, args) is InstanceUnit pet)
            {
                if (Region.GetValueAs(api, args) is ZoneRegion region)
                {
                    if (SummonTempID.GetValueAs(api, args) is UnitInfo temp)
                    {
                        region.SpawnCollection.SpawnOnce(this, (ab, st, pos) =>
                        {
                            if (api.ZoneAPI is InstanceZone zone)
                            {
                                var mpos = pos != null ? pos.Position : region.GetSpawnPos(ab);
                                if (zone.Terrain3D.TryGetVoxelLayerByPos(mpos, out var layer))
                                {
                                    mpos.Z = layer.Upward;
                                    var direction = pos != null ? pos.Direction : region.Direction;
                                    var info = temp;
                                    var unit = zone.AddUnit(new TAddUnit()
                                    {
                                        info = info,
                                        editor_name = "",
                                        force = pet.Force,
                                        pos = mpos,
                                        direction = direction,
                                        summoner = pet,
                                    });
                                    return unit;
                                }
                            }
                            return null;
                        });
                    }
                }
            }
            return null;
        }
    }



}
