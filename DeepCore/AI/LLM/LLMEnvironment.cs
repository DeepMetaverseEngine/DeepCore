using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using static DeepCore.Colors;

namespace DeepCore.AI.LLM
{
    //---------------------------------------------------------------------------------------
    public enum LLMRole
    {
        User, System, Assistant,
    }
    public interface LLMChatMessage
    {
        LLMRole Role { get; set; }
        string Text { get; set; }
        LLMContentCollection Contents { get; }
    }
    public interface LLMContent
    {
    }
    public interface LLMChatResponse
    {
        LLMChatMessageCollection Choices { get; }
        sealed public LLMChatMessage Message
        {
            get
            {
                var choices = Choices;
                if (choices.Count == 0)
                {
                    throw new InvalidOperationException("The ChatResponse instance does not contain any ChatMessage choices.");
                }
                return choices[0];
            }
        }
    }
    public interface LLMContentCollection : IReadOnlyList<LLMContent>
    {
        void Add(LLMContent content);
        void Insert(int index, LLMContent item);
        void RemoveAt(int index);
        void Clear();
    }
    public interface LLMChatMessageCollection : IReadOnlyList<LLMChatMessage>
    {
    }

    //---------------------------------------------------------------------------------------

    public interface LLMProxy
    {
        LLMEnvironment Env { get; }
        string UUID { get; }
        Task<LLMChatResponse> SendMessageAsync(IEnumerable<LLMChatMessage>[] contents);
        public Task<LLMChatResponse> SendMessageAsync(params LLMChatMessage[] contents) => this.SendMessageAsync([contents]);
        public Task<LLMChatResponse> SendMessageAsync(IEnumerable<LLMChatMessage> contents) => this.SendMessageAsync([contents]);
    }

    //---------------------------------------------------------------------------------------

    //---------------------------------------------------------------------------------------

    public abstract class LLMEnvironment
    {
        public static LLMEnvironment Instance { get; private set; } = new DummyEnv();
        public LLMEnvironment(bool makeInstance)
        {
            if (makeInstance) Instance = this;
        }
        public abstract bool Avaliable { get; }
        public abstract LLMProxy CreateProxy();
        public abstract LLMChatMessage CreateMessage();
        public abstract LLMChatMessage CreateMessage(LLMChatMessage src);
        public abstract LLMChatMessage CreateMessage(LLMRole role, IEnumerable<LLMContent> collection);
        public abstract LLMChatMessage CreateMessage(LLMRole role, string text);
        public abstract LLMContent CreateContent(string text);

    }

    //---------------------------------------------------------------------------------------

    //---------------------------------------------------------------------------------------
}
