using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static DeepCore.Colors;

namespace DeepCore.AI.LLM
{
    //---------------------------------------------------------------------------------------

    public class LLMAgent
    {
        public LLMProxy Proxy { get; }
        public LLMEnvironment Env => Proxy.Env;
        public LLMChatResponse LastResponse { get; private set; }

        protected List<LLMChatMessage> systems;
        protected List<LLMChatMessage> assistants;

        public LLMAgent(LLMProxy proxy)
        {
            this.Proxy = proxy;
        }
        public void ClearAll()
        {
            systems?.Clear();
            assistants?.Clear();
        }
        public void ClearSystem()
        {
            systems?.Clear();
        }
        public void ClearAssistant()
        {
            assistants?.Clear();
        }
        public void Clear(LLMRole role)
        {
            if (role == LLMRole.Assistant)
            {
                assistants?.Clear();
            }
            else if (role == LLMRole.System)
            {
                systems?.Clear();
            }
        }

        public void Append(LLMRole role, params LLMContent[] contents)
        {
            if (role == LLMRole.Assistant)
            {
                AppendAssistant(contents);
            }
            else if (role == LLMRole.System)
            {
                AppendSystem(contents);
            }
        }
        public void Append(LLMRole role, IEnumerable<LLMContent> contents)
        {
            if (role == LLMRole.Assistant)
            {
                AppendAssistant(contents);
            }
            else if (role == LLMRole.System)
            {
                AppendSystem(contents);
            }
        }
        public void Append(LLMRole role, string text)
        {
            Append(role, Env.CreateContent(text));
        }

        public void AppendSystem(params LLMContent[] contents)
        {
            if (systems == null) systems = new List<LLMChatMessage>(contents.Length);
            systems.Add(Env.CreateMessage(LLMRole.System, contents));
        }
        public void AppendSystem(IEnumerable<LLMContent> contents)
        {
            if (systems == null) systems = new List<LLMChatMessage>();
            systems.Add(Env.CreateMessage(LLMRole.System, contents));
        }
        public void AppendSystem(string text)
        {
            AppendSystem(Env.CreateContent(text));
        }

        public void SetSystem(string text)
        {
            systems?.Clear();
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (systems == null) systems = new List<LLMChatMessage>(1);
                systems.Add(Env.CreateMessage(LLMRole.System, text));
            }
        }
        public void AppendAssistant(params LLMContent[] contents)
        {
            if (assistants == null) assistants = new List<LLMChatMessage>(contents.Length);
            assistants.Add(Env.CreateMessage(LLMRole.Assistant, contents));
        }
        public void AppendAssistant(IEnumerable<LLMContent> contents)
        {
            if (assistants == null) assistants = new List<LLMChatMessage>();
            assistants.Add(Env.CreateMessage(LLMRole.Assistant, contents));
        }
        public void AppendAssistant(string text)
        {
            AppendAssistant(Env.CreateContent(text));
        }


        public virtual async Task<LLMChatResponse> SendMessageAsync(IEnumerable<LLMChatMessage> contents)
        {
            var post = new List<LLMChatMessage>();
            if (systems != null) post.AddRange(systems);
            if (assistants != null) post.AddRange(assistants);
            post.AddRange(contents);
            var result = await Proxy.SendMessageAsync(post);
            if (result != null)
            {
                if (assistants == null) assistants = new List<LLMChatMessage>();
                assistants.AddRange(contents);
                foreach (var r in result.Choices)
                {
                    AppendAssistant(r.Contents);
                }
                this.LastResponse = result;
            }
            return result;
        }
        public Task<LLMChatResponse> SendMessageAsync(params LLMChatMessage[] contents) => this.SendMessageAsync((IEnumerable<LLMChatMessage>)contents);
        public Task<LLMChatResponse> SendMessageAsync(string userPrompt) => this.SendMessageAsync(Env.CreateMessage(LLMRole.User, userPrompt));
    }

    //---------------------------------------------------------------------------------------
    public static class LLMEXT
    {
        public static bool TryRetrieveJsonList(this LLMChatResponse result, out string json)
        {
            return TryRetrieveJsonList($"{result}", out json);
        }
        public static bool TryRetrieveJsonList(this string input, out string json)
        {
            json = input;
            if (json.TryIndexOf('[', out var L) && json.TryLastIndexOf(']', out var R) && R > L)
            {
                json = json.Substring(L, R - L + 1);
                return true;
            }
            return false;
        }
        public static bool TryRetrieveJsonObject(this LLMChatResponse result, out string json)
        {
            return TryRetrieveJsonObject($"{result}", out json);
        }
        public static bool TryRetrieveJsonObject(this string input, out string json)
        {
            json = input;
            if (json.TryIndexOf('{', out var L) && json.TryLastIndexOf('}', out var R) && R > L)
            {
                json = json.Substring(L, R - L + 1);
                return true;
            }
            return false;
        }
    }
}
