using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("聚焦单位组", "[游戏]/客户端")]
    public class ClientFocusAction : ZoneAbstractAction
    {
        [Desc("单位组")]
        public Focus[] Units = new Focus[0];
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("聚焦单位组({0});", Units);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var focus = api.ZoneAPI.ObjectPool.Alloc < ClientFocusUnits>();
            //             focus.FocusUnitsID = Array.ConvertAll(Units, (u) =>
            //             {
            //                 var uu = u?.Unit?.GetValueAs(api, args);
            //                 return uu != null ? uu.ObjectID : 0;
            //             });
            foreach (var u in Units)
            {
                var uu = u?.Unit?.GetValueAs(api, args);
                var oid = uu != null ? uu.ObjectID : 0;
                focus.FocusUnitsID.Add(oid);
            }
            api.ZoneAPI.PostEvent(focus);
            return null;
        }

        public class Focus
        {
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
            public override string ToString()
            {
                return Unit?.ToString() ?? "null";
            }
        }
    }



    [Desc("客户端执行脚本文件", "[游戏]/客户端")]
    public class RunClientScriptFileAction : ZoneAbstractAction
    {
        [Desc("脚本文件")]
        public AbstractValue<string> Script = new StringValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("客户端执行脚本文件:{0};", Script);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            string sc = Script.GetValueAs(api, args);
            if (sc != null)
            {
                api.ZoneAPI.SendEvent(api.ZoneAPI.ObjectPool.Alloc<DoScriptEvent>().Init (sc));
            }
            return null;
        }
    }

    [Desc("客户端执行脚本代码", "[游戏]/客户端")]
    public class RunScriptCodeAction : ZoneAbstractAction
    {
        [Desc("脚本代码")]
        public AbstractValue<string> Script = new StringValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("客户端执行脚本代码:{0};", Script);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            string sc = Script.GetValueAs(api, args);
            if (sc != null)
            {
                api.ZoneAPI.SendEvent(api.ZoneAPI.ObjectPool.Alloc<ScriptCommandEvent>().Init (sc));
            }
            return null;
        }
    }

    [Desc("气泡聊天", "[游戏]/客户端")]
    public class BubbleTalk : ZoneAbstractAction
    {
        [Desc("是否暂停战斗")]
        public bool PauseBattle = false;

        [Desc("内容")]
        [ListDescAttribute(typeof(TalkInfo))]
        public List<TalkInfo> Talks = new List<TalkInfo>();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("气泡聊天;");
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            BubbleTalkNotify e = api.ZoneAPI.ObjectPool.Alloc<BubbleTalkNotify>();
            e.PauseBattle = PauseBattle;
            e.TalkInfos.Clear();
            foreach (TalkInfo t in Talks)
            {
                if (t.IsNarrator)
                {
                    BubbleTalkNotify.TalkInfo info = new BubbleTalkNotify.TalkInfo(0, t.TalkContent, t.TalkActionType, t.TalkDelayTimeMS, t.TalkKeepTimeMS);
                    e.TalkInfos.Add(info);
                }
                else
                {
                    InstanceUnit unit = t.TalkUnit.GetValueAs(api, args);
                    if (unit != null)
                    {
                        BubbleTalkNotify.TalkInfo info = new BubbleTalkNotify.TalkInfo(unit.ObjectID, t.TalkContent, t.TalkActionType, t.TalkDelayTimeMS, t.TalkKeepTimeMS);
                        e.TalkInfos.Add(info);
                    }
                }
            }
            api.ZoneAPI.SendEvent(e); return null;
        }

        [Desc("聊天内容")]
        [Expandable]
        public class TalkInfo
        {
            [Desc("单位 - 某个单位")]
            public AbstractValue<InstanceUnit> TalkUnit = new UnitValue.NA();
            [Desc("是否是旁边白")]
            public bool IsNarrator = false;
            [LocalizationTextAttribute]
            [Desc("内容")]
            public string TalkContent;
            [Desc("动作")]
            public string TalkActionType;
            [Desc("延迟时间")]
            public int TalkDelayTimeMS;
            [Desc("持续时间")]
            public int TalkKeepTimeMS;
        }
    }



}
