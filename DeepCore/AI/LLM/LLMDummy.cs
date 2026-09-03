using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using static DeepCore.Colors;

namespace DeepCore.AI.LLM
{
    //---------------------------------------------------------------------------------------
    class DummyEnv : LLMEnvironment
    {
        public DummyEnv() : base(true) { }
        public override bool Avaliable => false;
        public override LLMProxy CreateProxy() => new DummyAgent(this);
        public override LLMChatMessage CreateMessage() => new DummyChatMessage();
        public override LLMChatMessage CreateMessage(LLMChatMessage src) => new DummyChatMessage();
        public override LLMChatMessage CreateMessage(LLMRole role, string text) => new DummyChatMessage() { Text = text };
        public override LLMChatMessage CreateMessage(LLMRole role, IEnumerable<LLMContent> c) => new DummyChatMessage();
        public override LLMContent CreateContent(string text) => new DummyContent() { Text = text };

        class DummyAgent : LLMProxy
        {
            public LLMEnvironment Env { get; }
            public string UUID => string.Empty;
            public DummyAgent(DummyEnv env) { Env = env; }
            public async Task<LLMChatResponse> SendMessageAsync(IEnumerable<LLMChatMessage>[] contents) => await Task.FromResult(new DummyChatResponse());
        }
        class DummyContent : LLMContent
        {
            public string Text { get; set; } = "Dummy";
            public override string ToString() { return this.Text; }
        }
        class DummyContentCollection : LLMContentCollection
        {
            public readonly List<DummyContent> contents = new List<DummyContent>();
            public LLMContent this[int index] => contents[index];
            public int Count => contents.Count;
            public void Add(LLMContent content) => contents.Add(new DummyContent() { Text = content.ToString() });
            public void Clear() => contents.Clear();
            public void Insert(int index, LLMContent item) => contents.Insert(index, new DummyContent() { Text = item.ToString() });
            public void RemoveAt(int index) => contents.RemoveAt(index);
            public IEnumerator<LLMContent> GetEnumerator() => contents.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => contents.GetEnumerator();
        }
        class DummyChatMessage : LLMChatMessage
        {
            public LLMRole Role { get; set; } = LLMRole.User;
            public string Text { get; set; } = "Dummy";
            public LLMContentCollection Contents { get; } = new DummyContentCollection();
            public override string ToString() { return this.Text; }
        }
        class DummyChatMessageCollection : LLMChatMessageCollection
        {
            public readonly List<DummyChatMessage> messages = new List<DummyChatMessage>();
            public LLMChatMessage this[int index] => messages[index];
            public int Count => messages.Count;
            public IEnumerator<LLMChatMessage> GetEnumerator() => messages.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => messages.GetEnumerator();
        }
        class DummyChatResponse : LLMChatResponse
        {
            public LLMChatMessageCollection Choices { get; } = new DummyChatMessageCollection();
        }

    }
    //---------------------------------------------------------------------------------------
}
