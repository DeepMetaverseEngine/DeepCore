using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("场景初始化", "[游戏]/场景")]
    public class SceneInitialized : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当场景初始化时");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            // // args = args.Clone();
            var handler = new InstanceZone.InitHandler((z) =>
            {
                api.TestAndDoAction(args);
            });
            api.Listen(
                () => { api.ZoneAPI.OnInit += handler; },
                () => { api.ZoneAPI.OnInit -= handler; });
        }
    }

    [Desc("时间逝去", "[游戏]/时间")]
    public class TimeElapsed : ZoneAbstractTrigger
    {
        [Desc("时间(秒)")]
        public float TimeSEC = 5.0f;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当{0}秒之后", TimeSEC);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            api.listen_TimeDelaySEC(args, TimeSEC);
        }
    }

    [Desc("时间间隔", "[游戏]/时间")]
    public class TimePeriodic : ZoneAbstractTrigger
    {
        [Desc("时间(秒)")]
        public float EveryTimeSEC = 5.0f;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("每隔{0}秒", EveryTimeSEC);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            api.listen_TimePeriodicSEC(args, EveryTimeSEC);
        }
    }

    [Desc("时间间隔按次数", "[游戏]/时间")]
    public class TimeTask : ZoneAbstractTrigger
    {
        [Desc("延时时间(秒)")]
        public float DelayTimeSEC = 0f;
        [Desc("间隔时间(秒)")]
        public float EveryTimeSEC = 5.0f;
        [Desc("重复次数")]
        public int RepeatCount = 0;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("延时{0}秒，每隔{1}秒，执行{2}次", DelayTimeSEC, EveryTimeSEC, RepeatCount);

        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            api.listen_TimeTaskSEC(args, EveryTimeSEC, DelayTimeSEC, RepeatCount);
        }
    }



    [Desc("（变量）时间逝去", "[游戏]/时间")]
    public class ValuedTimeElapsed : ZoneAbstractTrigger
    {
        [Desc("时间(秒)")]
        public AbstractValue<double> TimeSEC = new RealValue.VALUE(5);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当{0}秒之后", TimeSEC);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            api.listen_TimeDelaySEC(args, (float)TimeSEC.GetValueAs(api, args));
        }
    }

    [Desc("（变量）时间间隔", "[游戏]/时间")]
    public class ValuedTimePeriodic : ZoneAbstractTrigger
    {
        [Desc("时间(秒)")]
        public AbstractValue<double> EveryTimeSEC = new RealValue.VALUE(5);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("每隔{0}秒", EveryTimeSEC);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            api.listen_TimePeriodicSEC(args, (float)EveryTimeSEC.GetValueAs(api, args));
        }
    }

    [Desc("（变量）时间间隔按次数", "[游戏]/时间")]
    public class ValuedTimeTask : ZoneAbstractTrigger
    {
        [Desc("延时时间(秒)")]
        public AbstractValue<double> DelayTimeSEC = new RealValue.VALUE(5);
        [Desc("间隔时间(秒)")]
        public AbstractValue<double> EveryTimeSEC = new RealValue.VALUE(5);
        [Desc("重复次数")]
        public AbstractValue<double> RepeatCount = new IntegerValue.VALUE(0);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("延时{0}秒，每隔{1}秒，执行{2}次", DelayTimeSEC, EveryTimeSEC, RepeatCount);

        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            api.listen_TimeTaskSEC(args, (float)EveryTimeSEC.GetValueAs(api, args), (float)DelayTimeSEC.GetValueAs(api, args), (int)RepeatCount.GetValueAs(api, args));
        }
    }

    // 
    //     [Desc("当从游戏服收到消息", "游戏服")]
    //     public class RecvMessageFromGS : ZoneAbstractTrigger
    //     {
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("当从游戏服收到消息");
    //         }
    //         protected override void Listen(IEventTriggerAdapter api, EventArguments args)
    //         {
    //             api.listen_RecvMessageFromGS(args, api.ZoneAPI);
    //         }
    //     }

    [Desc("场景变量发生变化", "[游戏]/场景")]
    public class EnvironmentVarChange : ZoneAbstractTrigger
    {

        [Desc("变量名")]
        public AbstractValue<string> VarKey = new StringValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat($"场景变量{VarKey}发生变化");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var varKey = VarKey.GetValueAs(api, args);
            var handler = new Action<string, object>((arg1, arg2) =>
            {
                if (varKey == arg1)
                {
                    api.TestAndDoAction(args);
                }
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnEnvironmentVarChangeHandler += handler,
                static (zone, handler) => zone.OnEnvironmentVarChangeHandler -= handler);
        }
    }

    [Desc("玩家进入场景", "[游戏]/场景")]
    public class PlayerEnterScene : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当玩家进入场景");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitActivatedHandler((z, u) =>
            {
                if (u is InstancePlayer player)
                {
                    args.TriggingUnit = player;
                    api.TestAndDoAction(args);
                }
            });
            api.Listen(
                () => { api.ZoneAPI.OnUnitActivated += handler; },
                () => { api.ZoneAPI.OnUnitActivated -= handler; });

        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

}
