using DeepCore.AI.LLM;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.EventTrigger.Data.AI;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    public abstract class ZoneLLMAgentValue : ZoneAbstractValue<LLMAgent>
    {
        [Desc("场景AI会话", "[OpenAI]")]
        public class ZoneDialog : ZoneLLMAgentValue
        {
            protected override LLMAgent GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.AiAgent;
            }
        }

        [Desc("单位AI会话", "[OpenAI]")]
        public class UnitDialog : ZoneLLMAgentValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位{0}AI会话", Unit);
            }
            protected override LLMAgent GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit?.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.AiAgent;
                }
                return null;
            }
        }
        [Desc("绑定单位AI会话", "[OpenAI]")]
        public class BindingUnitDialog : ZoneLLMAgentValue
        {
            protected override LLMAgent GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = api.UnitAPI;
                if (unit != null)
                {
                    return unit.AiAgent;
                }
                return null;
            }
        }


    }


    [Desc("单位提示词", "[OpenAI]")]
    public class UnitPrompt : ZoneStringValue
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位{0}提示词", Unit);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit?.GetValueAs(api, args);
            if (unit != null /*&& unit.Components.TryGetComponentAs<UnitOpenAIComponent>(out var prompt, true)*/)
            {
                return unit.Prompt;
            }
            return string.Empty;
        }
    }
    [Desc("场景提示词", "[OpenAI]")]
    public class ZonePrompt : ZoneStringValue
    {
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
           // if (api.ZoneAPI.Components.TryGetComponentAs<ZoneOpenAIComponent>(out var prompt, true))
            {
                return api.ZoneAPI.Prompt;
            }
            //return string.Empty;
        }
    }
}
