using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("设置游戏暂停/继续", "[游戏]")]
    public class GamePauseAction : ZoneAbstractAction
    {
        [Desc("是否暂停")]
        public AbstractValue<bool> Pause = new BooleanValue.VALUE(true);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置游戏(暂停:{0});", Pause);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var pause = Pause.GetValueAs(api, args);
            api.ZoneAPI.PostSystemMessage(api.ZoneAPI.ObjectPool.Alloc<ZonePauseNotify>().Init(pause, null));
            return null;
        }
    }

    [Desc("设置游戏暂停", "[游戏]")]
    public class GamePauseOnAction : ZoneAbstractAction
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置游戏暂停;");
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.PostSystemMessage(api.ZoneAPI.ObjectPool.Alloc<ZonePauseNotify>().Init(true, null));
            return null;
        }
    }
    [Desc("设置游戏继续", "[游戏]")]
    public class GamePauseOffAction : ZoneAbstractAction
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置游戏继续;");
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.PostSystemMessage(api.ZoneAPI.ObjectPool.Alloc<ZonePauseNotify>().Init(false, null));
            return null;
        }
    }


    [Desc("设置游戏时间加速", "[游戏]")]
    public class GameTimeScaleAction : ZoneAbstractAction
    {
        [Desc("加速值")]
        public AbstractValue<double> TimeScale = new RealValue.VALUE(1);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置游戏时间加速:{0};", TimeScale);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var timescale = (float)TimeScale.GetValueAs(api, args);
            api.ZoneAPI.PostSystemMessage(api.ZoneAPI.ObjectPool.Alloc<ZonePauseNotify>().Init(null, timescale));
            return null;
        }
    }


    [Desc("游戏结束", "[游戏]")]
    public class GameOverAction : ZoneAbstractAction
    {
        [Desc("胜利方")]
        public AbstractValue<double> WinForce = new IntegerValue.VALUE(0);
        [Desc("消息")]
        public string Message;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("游戏结束 胜利方:{0} 消息:\"{1}\";", WinForce, Message);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.SendGameOver((byte)WinForce.GetValueAs(api, args), Message); return null;
        }
    }
    [Desc("游戏结束(变量)", "[游戏]")]
    public class GameOverActionVar : ZoneAbstractAction
    {
        [Desc("胜利方")]
        public AbstractValue<double> WinForce = new IntegerValue.VALUE(0);
        [Desc("消息")]
        public AbstractValue<string> Message = new StringValue.VALUE("");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("游戏结束 胜利方:{0} 消息:\"{1}\";", WinForce, Message);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.SendGameOver((byte)WinForce.GetValueAs(api, args), Message.GetValueAs(api, args)); return null;
        }
    }

    //---------------------------------------------------------------------------------------------------

    [Desc("改变背景音乐", "[游戏]/场景")]
    public class ChangeBGMAction : ZoneAbstractAction
    {
        [Desc("背景音乐文件")]
        [ResourceID(ResourceType.Sound_BGM)] public string MusicFile;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("改变背景音乐{0};", MusicFile);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.SendEvent(api.ZoneAPI.ObjectPool.Alloc<ChangeBGMEvent>().Init(MusicFile)); return null;
        }
    }

    [Desc("激活场景事件触发器", "[游戏]/场景/场景触发器")]
    public class EventTriggerActive : ZoneAbstractAction
    {
        [Desc("事件触发器名字")]
        [SceneEventIDAttribute]
        public string EventName;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("激活场景事件触发器({0});", EventName);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.Group.EventActive(EventName); return null;
        }
    }
    [Desc("关闭场景事件触发器", "[游戏]/场景/场景触发器")]
    public class EventTriggerDeactive : ZoneAbstractAction
    {
        [Desc("事件触发器名字")]
        [SceneEventIDAttribute]
        public string EventName;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("关闭场景事件触发器({0});", EventName);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.Group.EventDeactive(EventName); return null;
        }
    }
    //---------------------------------------------------------------------------------------------------
    [Desc("激活场景事件触发器组", "[游戏]/场景/场景触发器")]
    public class EventGroupTriggerActive : ZoneAbstractAction
    {
        [Desc("事件触发器组")]
        [SceneEventGroup]
        public string EventGroup;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("激活场景事件触发器组({0});", EventGroup);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.Group.ForEachEvents(e =>
            {
                if (e.EditorPath.StartsWith(EventGroup))
                {
                    e.IsActive = true;
                }
            }); return null;
        }
    }

    [Desc("关闭场景事件触发器组", "[游戏]/场景/场景触发器")]
    public class EventGroupTriggerDeactive : ZoneAbstractAction
    {
        [Desc("事件触发器组")]
        [SceneEventGroup]
        public string EventGroup;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("关闭场景事件触发器组({0});", EventGroup);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.Group.ForEachEvents(e =>
            {
                if (e.EditorPath.StartsWith(EventGroup))
                {
                    e.IsActive = false;
                }
            }); return null;
        }
    }

    [Desc("场景绑定事件", "[游戏]/场景/场景触发器")]
    public class ZoneBindEventAction : ZoneAbstractAction
    {
        [Desc("事件触发器名字")]
        [TemplateID(typeof(UnitEventTemplate))]
        public int EventID;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("场景绑定事件({0});", EventID);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.BindEvent(EventID);
            return null;
        }
    }

    //---------------------------------------------------------------------------------------------------



    [Desc("在指定地点放一个特效", "[游戏]/场景")]
    public class AddZoneEffect : ZoneAbstractAction
    {
        [Desc("特效")]
        public LaunchEffect Effect = new LaunchEffect();
        [Desc("位置")]
        public AbstractValue<Vector3?> Pos = new PositionValue.VALUE();
        [Desc("方向")]
        public AbstractValue<double> Direction = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("在({0})位置添加特效({1});", Pos, Effect);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Pos.GetValueAs(api, args);
            if (pos != null)
            {
                float d = (float)Direction.GetValueAs(api, args);
                api.ZoneAPI.SendEvent(api.ZoneAPI.ObjectPool.Alloc<AddEffectEvent>().Init(0, pos.Value, d, Effect));
            }
            return null;
        }
    }

    [Desc("在指定单位身上放一个特效", "[游戏]/场景")]
    public class AddUnitEffect : ZoneAbstractAction
    {
        [Desc("特效")]
        public LaunchEffect Effect = new LaunchEffect();
        [Desc("单位 - 某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("在({0})添加特效({1});", Unit, Effect);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit u = Unit.GetValueAs(api, args);
            if (u != null)
            {
                u.PostEvent(api.ZoneAPI.ObjectPool.Alloc<UnitEffectEvent>().Init(u.ObjectID, Effect));
            }
            return null;
        }
    }


    //     [Desc("发送消息到游戏服", "游戏服")]
    //     public class SendMessageToGS : ZoneAbstractAction
    //     {
    //         [Desc("消息")]
    //         public AbstractValue<string> Message = new StringValue.VALUE("msg");
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("发送消息到游戏服:{0}", Message);
    //         }
    //         override protected object Run(IEventTriggerAdapter api, EventArguments args)
    //         {
    //             string msg = Message.GetValueAs(api, args);
    //             if (msg != null)
    //             {
    //                 api.ZoneAPI.SendMessageToGameServer(msg);
    //             }
    //         }
    //     }


}
