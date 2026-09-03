using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace DeepMetaGame.Data.Misc
{

    [MessageType(BattleConstants.UnitActionDefinitionMap)]
    [Desc("动作定义信息")]
    [Expandable]
    public class UnitActionDefinitionMap : ISerializable
    {
        [Desc("动作定义")]
        [Expandable]
        [MessageType(BattleConstants.UnitAction)]
        public class UnitAction : ISerializable
        {
            [Desc("动作", "动作")] public UnitActionStatus Action = UnitActionStatus.Idle;
            [Desc("子状态", "动作")] public string SubState;
            [Desc("动作序列", "动作"), NotNull] public ArrayList<UnitActionKeyFrame> ActionQueue = new ArrayList<UnitActionKeyFrame>();
            [Desc("自定义的模型", "模型"), ResourceID(ResourceType.Object)] public string CustomResource;
            [Desc("自定义的模型是否覆盖本体", "模型")] public bool CustomResourceOverride = false;

            [Desc("首帧动作", "动作")]
            public UnitActionKeyFrame FirstKeyFrame
            {
                get => (ActionQueue != null && ActionQueue.Count > 0) ? ActionQueue[0] : null;
                set
                {
                    if (ActionQueue == null) ActionQueue = new ArrayList<UnitActionKeyFrame>();
                    if (ActionQueue.Count > 0) ActionQueue[0] = value;
                    else ActionQueue.Add(value);
                }
            }
            public UnitAction()
            {
                ActionQueue.Add(new UnitActionKeyFrame());
            }
            public override string ToString()
            {
                if (string.IsNullOrEmpty(SubState))
                {
                    return ($"{Action} : {ActionQueue.ListToString(",", "\"", "\"")}");
                }
                else
                {
                    return ($"{Action}-{SubState} : {ActionQueue.ListToString(",", "\"", "\"")}");
                }
            }
        }

        [Desc("动作帧定义")]
        [Expandable]
        [MessageType(BattleConstants.UnitActionKeyFrame)]
        public class UnitActionKeyFrame : ISerializable, IKeyFrame, IPropertiesOwner
        {
            int IKeyFrame.FrameMS => TimeMS;
            [Desc("播放时间（如果多段动作，则需要指定每段时间）", "动画")] public int TimeMS = 1000;
            //-----------------------------------------------------------------
            [Desc("动作资源Id", "1.State")] public int ActionResId { get { if (Parser.TryParseInt(ActionName, out var resId)) return resId; return 0; } }
            [Desc("动作名", "1.State")] public string ActionName { get => DefaultState?.StateName; set => DefaultState?.StateName = value; } 
            [Desc("动作分层", "1.State")] public string ActionLayer { get => DefaultState?.LayerName; set => DefaultState?.LayerName = value; }
            [Desc("动作分层权重", "1.State")] public float ActionLayerWeight { get => DefaultState?.LayerWeight ?? 0; set => DefaultState?.LayerWeight = value; } 
            [Desc("是否淡出", "1.State")] public int CrossFadeTimeMS { get => DefaultState?.CrossFadeTimeMS ?? 0; set => DefaultState?.CrossFadeTimeMS = value; }
            [Desc("默认状态", "1.State"), NotNull] public Layer DefaultState = new Layer();
            [Desc("分层状态", "1.State"), NotNull] public ArrayList<Layer> SubStates = new ArrayList<Layer>();
            //-----------------------------------------------------------------
            [Desc("动作参数", "2.Param"), NotNull] public ArrayList<Param> ActionParams = new ArrayList<Param>();
            [Desc("动作触发器", "2.Param"), NotNull] public ArrayList<Trigger> ActionTriggers = new ArrayList<Trigger>();
            //-----------------------------------------------------------------
            [Desc("是否循环", "动画")] public bool Cycle = true;
            [Desc("播放速度", "动画")] public float Speed = 1f;
            [Desc("子状态起效", "动画")] public string SubStateKey;
            [Desc("循环播几次", "动画")] public int RepeatCount = 0;
            [Desc("绑定的特效", "动画")] public LaunchEffect ActionEffect;
            [Desc("声音名", "声音"), ResourceID(ResourceType.Sound_Effect)] public string SoundName;
            //-----------------------------------------------------------------
            [Desc("Tag")] public string Tag = "";
            [Desc("自定义动作")] public IKeyFrameProperties CustomAction;
            IPropertiesData IPropertiesOwner.PropertiesData => CustomAction;
            //-----------------------------------------------------------------
            public UnitActionKeyFrame()
            {
                CustomAction = ZoneDataFactory.Factory.CreateProperties<IKeyFrameProperties>(this);
            }
            public override string ToString()
            {
                return $"{ActionName}";
            }
            [Desc("动作帧状态")]
            [MessageType(BattleConstants.UnitActionKeyFrameState)]
            public class Layer
            {
                [Desc("动作名", "动画"), ResourceID(ResourceType.Animation)] public string StateName = "Idle";
                [Desc("动作分层", "动画")] public string LayerName;
                [Desc("动作分层权重", "动画")] public float LayerWeight = -1f;
                [Desc("是否淡出", "动画")] public int CrossFadeTimeMS;
                public override string ToString()
                {
                    var sb = new System.Text.StringBuilder();
                    if (!string.IsNullOrEmpty(StateName)) sb.Append($"StateName:{StateName} ");
                    if (!string.IsNullOrEmpty(LayerName)) sb.Append($"LayerName:{LayerName} ");
                    if (LayerWeight >= 0f) sb.Append($"LayerWeight:{LayerWeight} ");
                    return sb.ToString().TrimEnd();
                }
            }
            public enum ParamType
            {
                Integer, Float, Boolean,
            }
            [Desc("动作帧参数")]
            [MessageType(BattleConstants.UnitActionKeyFrameParam)]
            public class Param
            {
                public string ParamName;
                public ParamType ValueType;
                [DependOnProperty(nameof(IsFloat))] public float FloatValue;
                [DependOnProperty(nameof(IsInteger))] public int IntValue;
                [DependOnProperty(nameof(IsBoolean))] public bool BoolValue;
                public override string ToString()
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append($"{ParamName}:");
                    if (IsFloat) sb.Append($"{FloatValue}");
                    if (IsInteger) sb.Append($"{IntValue}");
                    if (IsBoolean) sb.Append($"{BoolValue}");
                    return sb.ToString().TrimEnd();
                }
                public bool IsFloat => ValueType == ParamType.Float;
                public bool IsInteger => ValueType == ParamType.Integer;
                public bool IsBoolean => ValueType == ParamType.Boolean;
            }
            [Desc("动作帧触发器")]
            [MessageType(BattleConstants.UnitActionKeyFrameTrigger)]
            public class Trigger
            {                
                public string TriggerName;
                public bool Enable;
                public override string ToString()
                {
                    var e = Enable ? "On" : "Off";
                    return $"{TriggerName}:{e}";
                }
            }
        }

        [Desc("动作集合")]
        public ArrayList<UnitAction> ActionMap = new ArrayList<UnitAction>();


    }


    //     public static class SubStateKey
    //     {
    //         public const string walk = "walk";
    //     }


    public class UnitActionMap
    {
        private HashMap<UnitActionStatus, HashMap<string, UnitActionDefinitionMap.UnitAction>> actionMap = new();
        public UnitActionMap()
        {

        }
        public void Clear()
        {
            foreach (var submap in actionMap.Values)
            {
                submap.Clear();
            }
        }
        public void Append(UnitActionDefinitionMap actionDefMap)
        {
            if (actionDefMap != null)
            {
                foreach (var a in actionDefMap.ActionMap)
                {
                    if (a != null)
                    {
                        var subMap = actionMap.GetOrNew(a.Action);
                        if (a.SubState.IsNullOrEmpty())
                        {
                            subMap.Put(string.Empty, a);
                        }
                        else
                        {
                            subMap.Put(a.SubState, a);
                        }
                    }
                }
            }
        }
        public bool TryGetAction(UnitActionStatus action, string subState, out UnitActionDefinitionMap.UnitAction ret)
        {
            ret = GetAction(action, subState);
            return ret != null;
        }
        public bool TryGetActionGroup(UnitActionStatus action, out IReadOnlyDictionary<string, UnitActionDefinitionMap.UnitAction> map)
        {
            if (this.actionMap.TryGetValue(action, out var subMap))
            {
                map = subMap;
                return true;
            }
            map = null;
            return false;
        }
        public bool TryGetDefaultAction(UnitActionStatus action, out UnitActionDefinitionMap.UnitAction ret)
        {
            if (this.actionMap.TryGetValue(action, out var subMap))
            {
                if (subMap.Count > 0)
                {
                    foreach (var v in subMap.Values)
                    {
                        ret = v;
                        return true;
                    }
                }
            }
            ret = null;
            return false;
        }
        public UnitActionDefinitionMap.UnitAction GetAction(UnitActionStatus action, string subState)
        {
            if (this.actionMap.TryGetValue(action, out var subMap))
            {
                if (subMap.Count > 0)
                {
                    if (subState.IsNullOrEmpty())
                    {
                        if (subMap.TryGetValue(string.Empty, out var ret))
                        {
                            return ret;
                        }
                        return subMap.First().Value;
                    }
                    else
                    {
                        return subMap.Get(subState);
                    }
                }
            }
            return null;
        }
    }
}
