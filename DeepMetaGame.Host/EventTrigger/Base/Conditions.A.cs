using System;
using System.Collections.Generic;
using System.Text;
using DeepCore.Reflection;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.GameData.EventTrigger;
using DeepCore.EventTrigger;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;


namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    public abstract class ZoneAbstractCondition : DeepCore.EventTrigger.Data.AbstractCondition
    {
        sealed protected override bool GetValue(EventExecutor api, IEventArguments args)
        {
            return GetValue(api as IEventTriggerAdapter, (EventArguments)args);
        }
        abstract protected bool GetValue(IEventTriggerAdapter api, EventArguments args);
    }

    [Desc("是否为编辑器模式", "[游戏]/编辑器")]
    [Expandable]
    public class IsEditorMode : ZoneAbstractCondition
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("是否为编辑器模式");
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return TemplateManager.IsEditor;
        }
    }
    [Desc("是否为编辑器模式", "[游戏]/编辑器")]
    [Expandable]
    public class IsEditorModeValue : ZoneBooleanValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("是否为编辑器模式");
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return TemplateManager.IsEditor;
        }
    }
}
