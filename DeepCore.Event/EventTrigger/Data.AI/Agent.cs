using DeepCore.AI.LLM;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeepCore.EventTrigger.Data.AI
{
    public abstract class LLMAgentValue : AbstractValue<LLMAgent>
    {
        [Desc("绑定的AI会话", "[OpenAI]")]
        public class Binding : LLMAgentValue
        {
            protected override LLMAgent GetValue(EventExecutor api, IEventArguments args)
            {
                return api.AiAgent;
            }
        }
    }

    //------------------------------------------------------------------------------------------------------------------------------------
    #region INTERACTION


    [Desc("AI问答", "[OpenAI]")]
    public class QuestionAnswer : AsyncAbstractAction
    {
        [Desc("AI会话")]
        public AbstractValue<LLMAgent> Agent = new LLMAgentValue.Binding();
        [Desc("用户提示语")]
        public AbstractValue<string> UserPrompt = new StringValue.VALUE("What's the highest mountain in the world?");
        [Desc("Function Calling")]
        public AbstractValue<string> tools;
        [Desc("当AI回答时")]
        public AbstractAction OnResponse = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("向{0}提问:{1}", Agent, UserPrompt).AppendLine();
            if (!OnResponse.IsNullOrEmpty())
            {
                sw.IndentBegin("{");
                sw.AppendFormat("当AI回答时:{0}", OnResponse).AppendLine();
                sw.IndentEnd("}");
            }
        }
        protected override async Task<object> RunAsync(EventExecutor api, IEventArguments args)
        {
            var agent = Agent?.GetValueAs(api, args);
            if (agent != null)
            {
                var result = await agent.SendMessageAsync(agent.Env.CreateMessage(LLMRole.User, UserPrompt.GetValueAs(api, args))).ConfigureAwait(true);
                args.TriggingStringValue = $"{result}";
                OnResponse.Invoke(api, args);
                return result;
            }
            return null;
        }
        [TriggingArg("Result")] public string Result(IEventArguments args) => args.TriggingStringValue;
    }


    [Desc("AI添加提示语", "[OpenAI]")]
    public class AddPrompt : AbstractAction
    {
        [Desc("AI会话")]
        public AbstractValue<LLMAgent> Agent = new LLMAgentValue.Binding();
        [Desc("角色")]
        public LLMRole Role = LLMRole.Assistant;
        [Desc("提示语")]
        public AbstractValue<string> Prompt = new StringValue.VALUE("The user will provide some exam text.");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}添加{1}提示语:{2}", Agent, Role, Prompt);
        }
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            var agent = Agent.GetValueAs(api, args);
            if (agent != null)
            {
                var env = agent.Env;
                agent.Append(Role, env.CreateContent(Prompt.GetValueAs(api, args)));
            }
            return null;
        }
    }


    [Desc("重置System提示语", "[OpenAI]")]
    public class SetSystemPrompt : AbstractAction
    {
        [Desc("AI会话")]
        public AbstractValue<LLMAgent> Agent = new LLMAgentValue.Binding();
        [Desc("提示语")]
        public AbstractValue<string> Prompt = new StringValue.VALUE("");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("重置{0}的System提示语:{1}", Agent, Prompt);
        }
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            var agent = Agent.GetValueAs(api, args);
            if (agent != null)
            {
                agent.SetSystem(Prompt.GetValueAs(api, args));
            }
            return null;
        }
    }


    [Desc("AI清理提示语", "[OpenAI]")]
    public class ClearPrompt : AbstractAction
    {
        [Desc("AI会话")]
        public AbstractValue<LLMAgent> Agent = new LLMAgentValue.Binding();
        [Desc("角色")]
        public LLMRole Role = LLMRole.Assistant;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}清理{1}提示语", Agent, Role);
        }
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            var agent = Agent.GetValueAs(api, args);
            if (agent != null)
            {
                agent.Clear(Role);
            }
            return null;
        }
    }


    [Desc("AI清理所有提示语", "[OpenAI]")]
    public class ClearAllPrompt : AbstractAction
    {
        [Desc("AI会话")]
        public AbstractValue<LLMAgent> Agent = new LLMAgentValue.Binding();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}清理所有提示语", Agent);
        }
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            var agent = Agent.GetValueAs(api, args);
            if (agent != null)
            {
                agent.ClearAll();
            }
            return null;
        }
    }


    #endregion
    //------------------------------------------------------------------------------------------------------------------------------------
    #region RESULT


    [Desc("AI答案", "[OpenAI]")]
    public class AnswerText : StringValue
    {
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            return args.TriggingStringValue;
        }
    }

    [Desc("AI决策", "[OpenAI]")]
    public class AnswerCondition : AbstractCondition
    {
        [Desc("正则表达式")]
        public AbstractValue<string> regex = new StringValue.VALUE("\\B");
        protected override bool GetValue(EventExecutor api, IEventArguments args)
        {
            if (!string.IsNullOrEmpty(args.TriggingStringValue))
            {
                var r = new Regex(regex.GetValueAs(api, args));
                return r.IsMatch(args.TriggingStringValue);
            }
            return false;
        }
    }


    [Desc("AI问答-最后的回复", "[OpenAI]")]
    public class LastResponse : StringValue
    {
        [Desc("AI会话")]
        public AbstractValue<LLMAgent> Agent = new LLMAgentValue.Binding();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}最后的回复", Agent);
        }
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            var agent = Agent.GetValueAs(api, args);
            if (agent != null)
            {
                var result = agent.LastResponse;
                if (result != null)
                {
                    return result.ToString();
                }
            }
            return null;
        }
    }

    [Desc("AI问答-最后的回复选项数量", "[OpenAI]")]
    public class LastResponseChoicesCount : IntegerValue
    {
        [Desc("AI会话")]
        public AbstractValue<LLMAgent> Agent = new LLMAgentValue.Binding();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}最后的回复选项数量", Agent);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var agent = Agent.GetValueAs(api, args);
            if (agent != null)
            {
                var result = agent.LastResponse;
                if (result != null)
                {
                    return result.Choices.Count;
                }
            }
            return 0;
        }
    }

    [Desc("AI问答-最后的回复选项", "[OpenAI]")]
    public class LastResponseChoices : StringValue
    {
        [Desc("AI会话")]
        public AbstractValue<LLMAgent> Agent = new LLMAgentValue.Binding();
        [Desc("选项")]
        public AbstractValue<double> ChoiceIndex = new IntegerValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}最后的回复选项[{1}]", Agent, ChoiceIndex);
        }
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            var agent = Agent.GetValueAs(api, args);
            if (agent != null)
            {
                var result = agent.LastResponse;
                if (result != null)
                {
                    var index = (int)ChoiceIndex.GetValueAs(api, args);
                    return result.Choices[index].ToString();
                }
            }
            return null;
        }
    }

    [Desc("指定AI用JSON格式输出", "[OpenAI]")]
    public class UseJsonPrompt : StringValue
    {
        public string Format = "response_format={[\"name\": \"who\",\"talk\":\"content\"]}";
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            return Format;
        }
    }

    #endregion
    //------------------------------------------------------------------------------------------------------------------------------------
}
