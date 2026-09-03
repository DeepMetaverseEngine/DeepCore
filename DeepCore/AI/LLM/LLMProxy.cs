using DeepCore.IO;
using DeepCore.Log;
using DeepCore.NetClient;
using DeepCore.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.AI.LLM.Proxy
{
    public class ProxyLLMEnvironment : LLMEnvironment
    {
        new public static ProxyLLMEnvironment Instance { get; private set; }
        public override bool Avaliable => connector != null && connector.IsConnected;

        private Logger log = new LazyLogger("ProxyLLM");
        private INetClient connector;
        public ProxyLLMEnvironment(INetClient connector, bool makeInstance = true) : base(makeInstance)
        {
            Instance = this;
            log.Color = ConsoleColor.Cyan;
            this.connector = connector;
        }
        public override LLMProxy CreateProxy() => new ProxyAIAgent(this);
        public override LLMChatMessage CreateMessage() => new ChatMessageImpl() { };
        public override LLMChatMessage CreateMessage(LLMChatMessage src) => new ChatMessageImpl(src);
        public override LLMChatMessage CreateMessage(LLMRole role, IEnumerable<LLMContent> c) => new ChatMessageImpl(role, c);
        public override LLMChatMessage CreateMessage(LLMRole role, string text)
        {
            var ret = new ChatMessageImpl() { role = role };
            ret.Contents.Add(new ContentImpl() { content = text });
            return ret;
        }
        public override LLMContent CreateContent(string text)
        {
            return new ContentImpl() { content = text };
        }
        //-------------------------------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------------------------------
        class ProxyAIAgent : LLMProxy
        {
            public ProxyLLMEnvironment Env { get; }
            public string UUID { get; }
            LLMEnvironment LLMProxy.Env => this.Env;
            public ProxyAIAgent(ProxyLLMEnvironment env)
            {
                this.Env = env;
                this.UUID = Guid.NewGuid().ToString();
            }
            public async Task<LLMChatResponse> SendMessageAsync(IEnumerable<LLMChatMessage>[] contents)
            {
                var req = new ChatRequestImpl();
                foreach (var list in contents)
                {
                    foreach (ChatMessageImpl msg in list)
                    {
                        req.chatMessages.Add(msg);
                    }
                }
                var reply = await Env.connector.DemandAsync<ChatResponseImpl>(req);
                return reply;
            }
        }
        //-------------------------------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------------------------------
        public static async Task<ChatResponseImpl> RequestDelegateMessage(LLMProxy agent, ChatRequestImpl req)
        {
            try
            {
                var env = agent.Env;
                var messages = new List<LLMChatMessage>();
                foreach (var msg in req.chatMessages)
                {
                    messages.Add(env.CreateMessage(msg));
                }
                var rsp = await agent.SendMessageAsync(messages);
                return new ChatResponseImpl(rsp);
            }
            catch (Exception err)
            {
                return new ChatResponseImpl(new ChatMessageImpl() { Text = err.Message });
            }
        }
        //-------------------------------------------------------------------------------------------------------------
    }
    //-------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------
    [MessageType(0x3101)]
    public class ContentImpl : IExternalizable, LLMContent
    {
        public string content;
        public ContentImpl() { }
        public ContentImpl(LLMContent src) { this.content = src.ToString(); }
        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(content);
        }
        public void ReadExternal(IInputStream input)
        {
            this.content = input.GetUTF();
        }
        public override string ToString() => content ?? string.Empty;
    }
    //-------------------------------------------------------------------------------------------------------------
    [MessageType(0x3102)]
    public class ContentCollectionImpl : IExternalizable, LLMContentCollection
    {
        public readonly List<ContentImpl> contents = new List<ContentImpl>();
        public ContentCollectionImpl() { }
        public ContentCollectionImpl(LLMContentCollection src)
        {
            foreach (var c in src) contents.Add(new ContentImpl(c));
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutList(contents, static (o, c) => o.PutExt((ContentImpl)c));
        }
        public void ReadExternal(IInputStream input)
        {
            input.GetList(static (i) => (i.GetExt<ContentImpl>()), contents);
        }
        public LLMContent this[int index] { get => contents[index]; set => contents[index] = new ContentImpl(value); }
        public int Count => contents.Count;
        public void Add(LLMContent content) => contents.Add(((ContentImpl)content));
        public void Clear() => contents.Clear();
        public void Insert(int index, LLMContent item) => contents.Insert(index, ((ContentImpl)item));
        public void RemoveAt(int index) => contents.RemoveAt(index);
        public IEnumerator<LLMContent> GetEnumerator() => contents.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public override string ToString() => CUtils.ListToString(contents);
    }
    //-------------------------------------------------------------------------------------------------------------
    [MessageType(0x3103)]
    public class ChatMessageImpl : IExternalizable, LLMChatMessage
    {
        public LLMRole role;
        public readonly ContentCollectionImpl contents = new ContentCollectionImpl();
        public ChatMessageImpl() { }
        public ChatMessageImpl(LLMRole role, IEnumerable<LLMContent> contents)
        {
            this.role = role;
            foreach (var c in contents)
            {
                this.contents.Add(new ContentImpl(c));
            }
        }
        public ChatMessageImpl(LLMChatMessage src)
        {
            this.role = src.Role;
            if (src.Contents != null)
            {
                foreach (var c in src.Contents) contents.Add(new ContentImpl(c));
            }
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutEnum(role);
            output.PutExt(contents);
        }
        public void ReadExternal(IInputStream input)
        {
            this.role = input.GetEnum<LLMRole>();
            input.GetExt<ContentCollectionImpl>(contents);
        }
        public LLMRole Role { get => role; set => role = value; }
        public string Text
        {
            get => (contents.Count > 0) ? contents[0].ToString() : string.Empty;
            set
            {
                if (contents.Count > 0)
                {
                    ((ContentImpl)contents[0]).content = value;
                }
                else if (value != null)
                {
                    contents.Add(new ContentImpl() { content = value });
                }
            }
        }
        public LLMContentCollection Contents => contents;
        public override string ToString() => contents.ToString();
    }
    //-------------------------------------------------------------------------------------------------------------
    [MessageType(0x3104)]
    public class ChatMessageCollectionImpl : IExternalizable, LLMChatMessageCollection
    {
        public readonly List<ChatMessageImpl> messages = new List<ChatMessageImpl>();
        public ChatMessageCollectionImpl() { }
        public ChatMessageCollectionImpl(LLMChatMessageCollection src)
        {
            foreach (var c in src) messages.Add(new ChatMessageImpl(c));
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutList(messages, static (o, c) => o.PutExt((ChatMessageImpl)c));
        }
        public void ReadExternal(IInputStream input)
        {
            input.GetList(static (i) => (i.GetExt<ChatMessageImpl>()), messages);
        }
        public LLMChatMessage this[int index] { get => messages[index]; }
        public int Count => messages.Count;
        public IEnumerator<LLMChatMessage> GetEnumerator() => messages.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public override string ToString() => CUtils.ListToString(messages);
    }
    //-------------------------------------------------------------------------------------------------------------
    [MessageType(0x3105)]
    public class ChatResponseImpl : INetResponse, IWormholeProtocol, IExternalizable, LLMChatResponse
    {
        readonly public ChatMessageCollectionImpl choices = new ChatMessageCollectionImpl();
        public ChatResponseImpl() { }
        public ChatResponseImpl(ChatMessageImpl chat) { choices.messages.Add(chat); }
        public ChatResponseImpl(LLMChatResponse src)
        {
            if (src.Choices != null)
            {
                foreach (var c in src.Choices) choices.messages.Add(new ChatMessageImpl(c));
            }
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutExt(choices);
        }
        public void ReadExternal(IInputStream input)
        {
            input.GetExt<ChatMessageCollectionImpl>(this.choices);
        }
        public LLMChatMessageCollection Choices => choices;
        public override string ToString()
        {
            if (Choices.Count == 1)
            {
                return Choices[0].ToString();
            }
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < Choices.Count; i++)
            {
                if (i > 0)
                {
                    stringBuilder.AppendLine().AppendLine();
                }
                stringBuilder.Append("Choice ").Append(i).AppendLine(":").Append(Choices[i]);
            }
            return stringBuilder.ToString();
        }
    }
    //-------------------------------------------------------------------------------------------------------------
    [MessageType(0x3106)]
    public class ChatRequestImpl : INetRequest, IWormholeProtocol, IExternalizable
    {
        readonly public List<ChatMessageImpl> chatMessages = new List<ChatMessageImpl>();
        public void WriteExternal(IOutputStream output)
        {
            output.PutList(chatMessages, static (o, c) => o.PutExt((ChatMessageImpl)c));
        }
        public void ReadExternal(IInputStream input)
        {
            input.GetList(static (i) => (ChatMessageImpl)(i.GetExt<ChatMessageImpl>()), chatMessages);
        }
    }
    //-------------------------------------------------------------------------------------------------------------
}
