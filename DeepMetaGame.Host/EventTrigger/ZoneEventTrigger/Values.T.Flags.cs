using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor;
using System.Linq;
using System.Text.RegularExpressions;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("场景-Flag")]
    public abstract class FlagValue : ZoneAbstractValue<InstanceFlag>
    {
        public enum FlagType
        {
            Region, Point, Decoration, Area,
        }
        public static InstanceFlag FlagAs(InstanceFlag flag, FlagType ftype)
        {
            if (flag is ZoneRegion rg && ftype == FlagType.Region) return rg;
            if (flag is ZoneDecoration deco && ftype == FlagType.Decoration) return deco;
            if (flag is ZoneWayPoint wp && ftype == FlagType.Point) return wp;
            if (flag is ZoneArea area && ftype == FlagType.Area) return area;
            return null;
        }

        [Desc("值 - 没有Flag", "[游戏]/值")]
        public class NA : FlagValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("没有Flag");
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return null;
            }
        }
        [Desc("遍历迭代中的 - Flag", "[游戏]/循环迭代")]
        public class Iterating : FlagValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("遍历迭代中的Flag");
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.IteratingObject as InstanceFlag;
            }
        }
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : FlagValue
        {
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                try
                {
                    if (args.ReturnValue is InstanceFlag v3) { return v3; }
                }
                catch { }
                return null;
            }
        }

        //---------------------------------------------------------------------------------------

        [Desc("是否为区域", "[游戏]/Flag")]
        public class FlagIsRegion : ZoneBooleanValue
        {
            [Desc("Flag")]
            public FlagValue Flag = new FlagValue.Iterating();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}是否为区域", Flag);
            }
            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var flag = Flag.GetValueAs(api, args);
                return flag is ZoneRegion;
            }
        }
        [Desc("是否为路点", "[游戏]/Flag")]
        public class FlagIsPoint : ZoneBooleanValue
        {
            [Desc("Flag")]
            public FlagValue Flag = new FlagValue.Iterating();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}是否为路点", Flag);
            }
            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var flag = Flag.GetValueAs(api, args);
                return flag is ZoneWayPoint;
            }
        }
        [Desc("是否为装饰物", "[游戏]/Flag")]
        public class FlagIsDecoration : ZoneBooleanValue
        {
            [Desc("Flag")]
            public FlagValue Flag = new FlagValue.Iterating();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}是否为装饰物", Flag);
            }
            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var flag = Flag.GetValueAs(api, args);
                return flag is ZoneDecoration;
            }
        }
        [Desc("是否为Area", "[游戏]/Flag")]
        public class FlagIsArea : ZoneBooleanValue
        {
            [Desc("Flag")]
            public FlagValue Flag = new FlagValue.Iterating();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}是否为Area", Flag);
            }
            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var flag = Flag.GetValueAs(api, args);
                return flag is ZoneArea;
            }
        }

        //---------------------------------------------------------------------------------------
        #region Editor




        [Desc("编辑器 - 区域", "[游戏]/编辑器")]
        public class EditorRegion : FlagValue
        {
            [Desc("场景中的名字")]
            [SceneObjectIDAttribute(typeof(RegionData))]
            public string EditorName;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("区域:<c color='" + sw.COLOR_CONST + "'>{0}</c>", EditorName);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetFlag<ZoneRegion>(EditorName);
            }
        }

        [Desc("编辑器 - 路点", "[游戏]/编辑器")]
        public class EditorPoint : FlagValue
        {
            [Desc("场景中的名字")]
            [SceneObjectIDAttribute(typeof(PointData))]
            public string EditorName;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("路点:<c color='" + sw.COLOR_CONST + "'>{0}</c>", EditorName);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetFlag<ZoneWayPoint>(EditorName);
            }
        }

        [Desc("编辑器 - 装饰物", "[游戏]/编辑器")]
        public class EditorDecoration : FlagValue
        {
            [Desc("场景中的名字")]
            [SceneObjectIDAttribute(typeof(DecorationData))]
            public string EditorName;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("装饰物:<c color='" + sw.COLOR_CONST + "'>{0}</c>", EditorName);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetFlag<ZoneDecoration>(EditorName);
            }
        }


        [Desc("编辑器 - Area", "[游戏]/编辑器")]
        public class EditorArea : FlagValue
        {
            [Desc("场景中的名字")]
            [SceneObjectIDAttribute(typeof(AreaData))]
            public string EditorName;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("Area:<c color='" + sw.COLOR_CONST + "'>{0}</c>", EditorName);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetFlag<ZoneArea>(EditorName);
            }
        }

        //--------------------------------------------------------------------------------------------------

        [Desc("编辑器 - 区域（变量）", "[游戏]/编辑器")]
        public class EditorRegionVAR : FlagValue
        {
            [Desc("场景中的名字")]
            public AbstractValue<string> EditorName = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("区域:{0}", EditorName);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetFlag<ZoneRegion>(EditorName.GetValueAs(api, args));
            }
        }

        [Desc("编辑器 - 路点（变量）", "[游戏]/编辑器")]
        public class EditorPointVAR : FlagValue
        {
            [Desc("场景中的名字")]
            public AbstractValue<string> EditorName = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("路点:{0}", EditorName);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetFlag<ZoneWayPoint>(EditorName.GetValueAs(api, args));
            }
        }

        [Desc("编辑器 - 装饰物（变量）", "[游戏]/编辑器")]
        public class EditorDecorationVAR : FlagValue
        {
            [Desc("场景中的名字")]
            public AbstractValue<string> EditorName = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("装饰物:{0}", EditorName);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetFlag<ZoneDecoration>(EditorName.GetValueAs(api, args));
            }
        }


        [Desc("编辑器 - Area（变量）", "[游戏]/编辑器")]
        public class EditorAreaVAR : FlagValue
        {
            [Desc("场景中的名字")]
            public AbstractValue<string> EditorName = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("Area:{0}", EditorName);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetFlag<ZoneArea>(EditorName.GetValueAs(api,args));
            }
        }

        #endregion
        //---------------------------------------------------------------------------------------
        #region Trigging
        [Desc("功能 - TriggingFlag", "[游戏]/功能")]
        public class TriggingFlag : FlagValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("TriggingFlag");
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingFlag;
            }
        }

        [Desc("功能 - 触发的区域", "[游戏]/功能")]
        public class TriggingRegion : FlagValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("触发的区域");
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingRegion;
            }
        }
        [Desc("功能 - 触发的装饰物", "[游戏]/功能")]
        public class TriggingDecoration : FlagValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的装饰物");
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingDecoration;
            }
        }
        [Desc("功能 - 触发的Area", "[游戏]/功能")]
        public class TriggingArea : FlagValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的Area");
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingArea;
            }
        }
        [Desc("功能 - 触发的路点", "[游戏]/功能")]
        public class TriggingPoint : FlagValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("触发的路点");
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingPoint;
            }
        }


        [Desc("距离最近的Flag", "[游戏]/功能")]
        public class NearestFlag : FlagValue
        {
            [Desc("Flag类型")]
            public FlagType FType = FlagType.Point;
            [Desc("位置")]
            public AbstractValue<Vector3?> Origin = new PositionValue.PositionOfUnit() { Unit = new UnitValue.Trigging(), };
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("距离{0}最近的{1}", Origin, FType);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var min = float.MaxValue;
                var ret = null as InstanceFlag;
                var orgin = Origin.GetValueAs(api, args);
                if (orgin.HasValue)
                {
                    foreach (var flag in api.ZoneAPI.AllFlags)
                    {
                        var fv = FlagAs(flag, FType);
                        if (fv != null)
                        {
                            var pos = fv.Position;
                            var dis = Vector3.DistanceSquared(orgin.Value, in pos);
                            if (dis < min)
                            {
                                min = dis;
                                ret = fv;
                            }
                        }
                    }
                }
                return ret;
            }
        }

        #endregion
        //---------------------------------------------------------------------------------------
        #region Field


        //         [Desc("单位当前Area", "Area")]
        //         public class UnitCurrentArea : FlagValue
        //         {
        //             [Desc("单位")]
        //             public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        // 
        //             protected override void GetText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("单位({0})当前Area", Unit);
        //             }
        //             protected override InstanceFlag GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 var unit = Unit.GetValueAs(api, args);
        //                 if (unit != null)
        //                 {
        //                     return unit.CurrentArea;
        //                 }
        //                 return null;
        //             }
        //         }
        // 
        //         [Desc("坐标所在Area", "Area")]
        //         public class PositionArea : FlagValue
        //         {
        //             [Desc("坐标")]
        //             public AbstractValue<Vector3?> Pos = new PositionValue.VALUE();
        // 
        //             protected override void GetText(EventStringBuilder sw)
        //             {
        //                 sw.AppendFormat("坐标({0})位置Area", Pos);
        //             }
        //             protected override InstanceFlag GetValue(IEditorValueAdapter api, EventArguments args)
        //             {
        //                 var pos = Pos.GetValueAs(api, args);
        //                 if (pos != null)
        //                 {
        //                     return api.ZoneAPI.GetArea(pos);
        //                 }
        //                 return null;
        //             }
        //         }

        #endregion
        //---------------------------------------------------------------------------------------


        [Desc("从坐标获取区域", "[游戏]/获取Flag")]
        public class GetRegionWithPoint : FlagValue
        {
            [Desc("坐标")]
            public PositionValue Pos = new PositionValue.PositionOfUnit();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("从{0}坐标获取区域", Pos);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var pos = Pos.GetValueAs(api, args);
                if (pos.HasValue)
                {
                    return api.ZoneAPI.GetRegionWithPoint(pos.Value);
                }
                return default;
            }
        }

        //---------------------------------------------------------------------------------------

        [Desc("查找(前后缀)的随机Flag", "[游戏]/查找Flag")]
        public class FindSceneRandomFlagWithPrefixSuffix : FlagValue
        {
            [Desc("名字前缀")]
            public AbstractValue<string> Prefix = new ZoneStringValue.VALUE();
            [Desc("名字后缀")]
            public AbstractValue<string> Suffix = new ZoneStringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("查找名字(前缀:\"{0}\")并且(后缀:\"{1}\")的随机Flag", Prefix, Suffix);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var prefix = Prefix?.GetValueAs(api, args);
                var suffix = Suffix?.GetValueAs(api, args);
                var all = api.ZoneAPI.AllFlags;
                using (var list = api.ZoneAPI.ObjectPool.AllocList<InstanceFlag>(all.Count))
                {
                    foreach (var flag in all)
                    {
                        if ((string.IsNullOrEmpty(prefix) || flag.Name.StartsWith(prefix)) &&
                            (string.IsNullOrEmpty(suffix) || flag.Name.EndsWith(suffix)))
                        {
                            list.Add(flag);
                        }
                    }
                    return api.ZoneAPI.RandomN.GetRandomInCollection(list);
                }
            }
        }
        [Desc("查找(正则表达式)的随机Flag", "[游戏]/查找Flag")]
        public class FindSceneRandomFlagWithRegex : FlagValue
        {
            [Desc("正则表达式")]
            public string Regex = "abc";
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("查找名字(正则表达式:\"{0}\")的随机Flag", Regex);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var regex = new Regex(Regex);
                var all = api.ZoneAPI.AllFlags;
                using (var list = api.ZoneAPI.ObjectPool.AllocList<InstanceFlag>(all.Count))
                {
                    foreach (var flag in all)
                    {
                        if (regex.IsMatch(flag.Name))
                        {
                            list.Add(flag);
                        }
                    }
                    return api.ZoneAPI.RandomN.GetRandomInCollection(list);
                }
            }
        }

        [Desc("随机获取下个Flag", "[游戏]/查找Flag")]
        public class FlagPopRandomNext : FlagValue
        {
            [Desc("Flag")]
            public AbstractValue<InstanceFlag> Flag = new FlagValue.EditorPoint();
            [Desc("排除的下一个Flag")]
            public AbstractValue<InstanceFlag> Prev;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.随机获取下个Flag", Flag);
            }
            protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                if (Flag?.GetValueAs(api, args) is InstanceFlag flag)
                {
                    var prev = Prev?.GetValueAs(api, args);
                    return flag.PopRandomNext(prev);
                }
                return null;
            }
        }
    }

  

    //     [Desc("基础-Flag物品")]
    //     public abstract class FlagArrayValue : ZoneAbstractArrayValue<InstanceFlag>
    //     {
    //         [Desc("Flag数组", "值")] public class VALUE : ArrayValue<AbstractValue<InstanceFlag>, InstanceFlag> { }
    //         [Desc("Flag数组索引", "数组")] public class INDEX : ArrayIndexValue<InstanceFlag> { }
    //         [Desc("Flag数组随机", "数组")] public class RANDOM : ArrayRandomValue<InstanceFlag> { }
    //         [Desc("迭代中的Flag", "数组")] public class ITERATOR : ArrayIteratingValue<InstanceFlag> { }
    // 
    //         //---------------------------------------------------------------------------------------------------------
    //         [Desc("场景内的Flag数组", "[游戏]/数组")]
    //         public class SceneFlags : FlagArrayValue
    //         {
    //             protected override void GetText(EventStringBuilder sw)
    //             {
    //                 sw.Append("场景内所有的Flag");
    //             }
    //             protected override InstanceFlag[] GetValue(IEditorValueAdapter api, EventArguments args)
    //             {
    //                 return api.ZoneAPI.AllFlags.ToArray();
    //             }
    //         }
    // 
    //         [Desc("场景内的Flag数组-名字前缀", "[游戏]/数组")]
    //         public class SceneFlagsPrefix : FlagArrayValue
    //         {
    //             [Desc("名字前缀")]
    //             public AbstractValue<string> Prefix;
    //             protected override void GetText(EventStringBuilder sw)
    //             {
    //                 sw.Append("场景内名字前缀为").Append(Prefix).Append("的Flag");
    //             }
    //             protected override InstanceFlag[] GetValue(IEditorValueAdapter api, EventArguments args)
    //             {
    //                 var all = api.ZoneAPI.AllFlags.ToArray();
    //                 var prefix = Prefix?.GetValueAs(api, args);
    //                 if (string.IsNullOrEmpty(prefix)) return all;
    //                 using (var list = api.ZoneAPI.ObjectPool.AllocList<InstanceFlag>(all.Length))
    //                 {
    //                     foreach (var flag in all)
    //                     {
    //                         if (flag.Name.StartsWith(prefix))
    //                         {
    //                             list.Add(flag);
    //                         }
    //                     }
    //                     return list.ToArray();
    //                 }
    //             }
    //         }
    // 
    //         [Desc("场景内的Flag数组-名字后缀", "[游戏]/数组")]
    //         public class SceneFlagsSuffix : FlagArrayValue
    //         {
    //             [Desc("名字后缀")]
    //             public AbstractValue<string> Suffix;
    //             protected override void GetText(EventStringBuilder sw)
    //             {
    //                 sw.Append("场景内名字后缀为").Append(Suffix).Append("的Flag");
    //             }
    //             protected override InstanceFlag[] GetValue(IEditorValueAdapter api, EventArguments args)
    //             {
    //                 var all = api.ZoneAPI.AllFlags.ToArray();
    //                 var suffix = Suffix?.GetValueAs(api, args);
    //                 if (string.IsNullOrEmpty(suffix)) return all;
    //                 using (var list = api.ZoneAPI.ObjectPool.AllocList<InstanceFlag>(all.Length))
    //                 {
    //                     foreach (var flag in all)
    //                     {
    //                         if (flag.Name.EndsWith(suffix))
    //                         {
    //                             list.Add(flag);
    //                         }
    //                     }
    //                     return list.ToArray();
    //                 }
    //             }
    //         }
    // 
    //         [Desc("场景内的Flag数组-匹配名字", "[游戏]/数组")]
    //         public class SceneFlagsRegex : FlagArrayValue
    //         {
    //             [Desc("正则表达式")]
    //             public AbstractValue<string> RegexPattern;
    //             protected override void GetText(EventStringBuilder sw)
    //             {
    //                 sw.Append("场景内名字匹配正则").Append(RegexPattern).Append("的Flag");
    //             }
    //             protected override InstanceFlag[] GetValue(IEditorValueAdapter api, EventArguments args)
    //             {
    //                 var all = api.ZoneAPI.AllFlags.ToArray();
    //                 var pattern = RegexPattern?.GetValueAs(api, args);
    //                 if (string.IsNullOrEmpty(pattern)) return all;
    //                 var reg = new Regex(pattern);
    //                 using (var list = api.ZoneAPI.ObjectPool.AllocList<InstanceFlag>(all.Length))
    //                 {
    //                     foreach (var flag in all)
    //                     {
    //                         if (reg.IsMatch(flag.Name))
    //                         {
    //                             list.Add(flag);
    //                         }
    //                     }
    //                     return list.ToArray();
    //                 }
    //             }
    //         }
    //         //---------------------------------------------------------------------------------------------------------
    //     }
}
