using DeepCore.AI.LLM;
using DeepCore.Crypto;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Xml;
using Microsoft.Extensions.AI;
using System;
using System.ClientModel;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DeepOpenAI
{
    public class LLMConfig
    {
        public string url = "https://api.siliconflow.cn/v1";
        public string apiKey = "sk-xxxxxxxxxxxxxxxxxxxxx";
        public string model = "Pro/deepseek-ai/DeepSeek-V3";//"deepseek-ai/DeepSeek-V2.5";

        public static LLMConfig LoadFromEnvironments(string encryptKey = "wazazhang@gmail.com")
        {
            //string configFile = Path.Combine(GameEditor.EditorRootDir, ".openai", $"{nameof(OpenAIEnvironment)}.config");
            //if (File.Exists(configFile))
            var prop = DeepCore.Properties.ParseEnvironmentVariables().SubProperties("OPENAIENVIRONMENT_");
            if (prop.Count > 0)
            {
                var cfg = prop.LoadInstance<LLMConfig>();
                //var xml = XmlUtil.LoadXML(configFile);
                //var cfg = XmlUtil.XmlToObject<LLMConfig>(xml);
                cfg.apiKey = EncryptHelper.AESDecrypt(cfg.apiKey, encryptKey);
                return cfg;
            }
            else if (DEFAULT_CFG != null)
            {
                return new LLMConfig()
                {
                    apiKey = DEFAULT_CFG.apiKey,
                    model = DEFAULT_CFG.model,
                    url = DEFAULT_CFG.url,
                };
            }
            else
            {
                var apiKey = EncryptHelper.AESDecrypt("sUxPVOIEHM55yKT6vxDDNiExoqPXJsBqFO7kfU/Pm8AP/AfpDkWBKaaiQhaRJN8BX+LG7j4e4+HG2ClKxmj1SQ==", "DeepOpenAI");
                return new LLMConfig() { apiKey = apiKey };
            }
        }
        public static void SaveToEnvironments(LLMConfig cfg, string encryptKey = "wazazhang@gmail.com")
        {
            var save = new LLMConfig()
            {
                apiKey = EncryptHelper.AESEncrypt(cfg.apiKey, encryptKey),
                model = cfg.model,
                url = cfg.url,
            };
            var prop = DeepCore.Properties.SaveInstance(save).Indent("OPENAIENVIRONMENT_");
            prop.SaveEnvironmentVariables();
            //                 string configFile = Path.Combine(GameEditor.EditorRootDir, ".openai", $"{nameof(OpenAIEnvironment)}.config");
            //                 DeepCore.IO.CFiles.CreateFile(configFile);
            //                 var xml = XmlUtil.ObjectToXml(save);
            //                 XmlUtil.SaveXML(configFile, xml);
        }

        public static LLMConfig LoadConfigFile(string configFile, string encryptKey = "wazazhang@gmail.com")
        {
            try
            {
                if (File.Exists(configFile))
                {
                    var xml = XmlUtil.LoadXML(configFile);
                    var cfg = XmlUtil.XmlToObject<LLMConfig>(xml);
                    cfg.apiKey = EncryptHelper.AESDecrypt(cfg.apiKey, encryptKey);
                    return cfg;
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }
        public static void SaveConfigFile(LLMConfig cfg, string configFile, string encryptKey = "wazazhang@gmail.com")
        {
            try
            {
                var save = new LLMConfig()
                {
                    apiKey = EncryptHelper.AESEncrypt(cfg.apiKey, encryptKey),
                    model = cfg.model,
                    url = cfg.url,
                };
                DeepCore.IO.CFiles.CreateFile(configFile);
                var xml = XmlUtil.ObjectToXml(save);
                XmlUtil.SaveXML(configFile, xml);
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
        }

        private static LLMConfig DEFAULT_CFG;
        public static void SetDefaultConfig(LLMConfig cfg)
        {
            DEFAULT_CFG = cfg;
        }
    }

    public class OpenAIEnvironment : LLMEnvironment
    {
        new public static OpenAIEnvironment Instance { get; private set; }
        public override bool Avaliable => Config != null;
        public static Logger LogR => logR;
        public static Logger LogW => logW;

        private static Logger logR = new LazyLogger("LLM");
        private static Logger logW = new LazyLogger("LLM");
        private LLMConfig Config;
        public OpenAIEnvironment(LLMConfig config, bool makeInstance = true) : base(makeInstance)
        {
            Instance = this;
            logR.Color = ConsoleColor.Cyan;
            logW.Color = ConsoleColor.Magenta;
            this.Config = config;
            logR.Info($"OpenAIEnvironment : {Config.url} : {Config.model}");
        }

        public override LLMProxy CreateProxy() => new OpenAIAgent(this);
        public override LLMChatMessage CreateMessage() => new ChatMessageImpl() { Src = new ChatMessage() };
        public override LLMChatMessage CreateMessage(LLMChatMessage src) => new ChatMessageImpl(src);
        public override LLMChatMessage CreateMessage(LLMRole role, IEnumerable<LLMContent> c) => new ChatMessageImpl(role, c);
        public override LLMChatMessage CreateMessage(LLMRole role, string text) => new ChatMessageImpl() { Src = new ChatMessage(GetRole(role), text) };
        public override LLMContent CreateContent(string text) => new ContentImpl() { Src = new TextContent(text) };

        //-------------------------------------------------------------------------------------------------------------
        class OpenAIAgent : LLMProxy
        {
            private StringBuilder sb = new StringBuilder();
            private OpenAIChatClient openAIChatClient;
            public OpenAIEnvironment Env { get; }
            public string UUID { get; }
            LLMEnvironment LLMProxy.Env => this.Env;
            public OpenAIAgent(OpenAIEnvironment env)
            {
                this.Env = env;
                this.UUID = Guid.NewGuid().ToString();
                this.openAIChatClient = new OpenAIChatClient(
                    new OpenAI.OpenAIClient(new ApiKeyCredential(env.Config.apiKey),
                    new OpenAI.OpenAIClientOptions() { Endpoint = new Uri(env.Config.url) }),
                    env.Config.model);
                //log.Info($"Crate Agent : {env.Config.url} : {env.Config.model}");
            }
            public async Task<LLMChatResponse> SendMessageAsync(IEnumerable<LLMChatMessage>[] contents)
            {
                var chatMessages = new List<ChatMessage>
                {
                    //  new ChatMessage(ChatRole.User, "Hello, Ollama!"),
                    //  new ChatMessage(ChatRole.User, "余华是一个作家吗")
                };
                sb.Clear();
                foreach (var list in contents)
                {
                    foreach (ChatMessageImpl msg in list)
                    {
                        chatMessages.Add(msg.Src);
                        sb.AppendLine($"{msg.Role} : {msg.Text}");
                    }
                }
                logW.Trace($" Talk : \n{sb}");
                sb.Clear();
                var reply = await openAIChatClient.GetResponseAsync(chatMessages, new ChatOptions() { });
                sb.Clear();
                foreach (var msg in reply.Choices)
                {
                    sb.AppendLine($"{msg.Role} : {msg.Text}");
                }
                logR.Trace($" Reply : \n{sb}");
                sb.Clear();
                return new ChatResponseImpl() { Src = reply };
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        #region Impl
        //-------------------------------------------------------------------------------------------------------------

        private static ChatRole GetRole(LLMRole role)
        {
            switch (role)
            {
                case LLMRole.User: return ChatRole.User;
                case LLMRole.System: return ChatRole.System;
                case LLMRole.Assistant: return ChatRole.Assistant;
            }
            return ChatRole.User;
        }
        private static LLMRole GetRole(ChatRole role)
        {
            if (ChatRole.User == role) return LLMRole.User;
            if (ChatRole.System == role) return LLMRole.System;
            if (ChatRole.Assistant == role) return LLMRole.Assistant;
            return LLMRole.User;
        }

        //-------------------------------------------------------------------------------------------------------------

        struct ContentImpl : LLMContent
        {
            public AIContent Src;
            public override string ToString() => Src.ToString();
        }
        struct ContentCollectionImpl : LLMContentCollection
        {
            public IList<AIContent> Src;
            public LLMContent this[int index] { get => new ContentImpl() { Src = Src[index] }; set => Src[index] = ((ContentImpl)value).Src; }
            public int Count => Src.Count;
            public void Add(LLMContent content) => Src.Add(((ContentImpl)content).Src);
            public void Clear() => Src.Clear();
            public void Insert(int index, LLMContent item) => Src.Insert(index, ((ContentImpl)item).Src);
            public void RemoveAt(int index) => Src.RemoveAt(index);
            public IEnumerator<LLMContent> GetEnumerator() => new It() { Src = Src.GetEnumerator() };
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            struct It : IEnumerator<LLMContent>
            {
                public IEnumerator<AIContent> Src;
                public LLMContent Current => new ContentImpl() { Src = Src.Current };
                object IEnumerator.Current => Current;
                public void Dispose() => Src.Dispose();
                public bool MoveNext() => Src.MoveNext();
                public void Reset() => Src.Reset();
            }
            public override string ToString() => Src.ToString();
        }
        struct ChatMessageImpl : LLMChatMessage
        {
            public ChatMessage Src;
            public ChatMessageImpl() { }
            public ChatMessageImpl(LLMRole role, IEnumerable<LLMContent> contents)
            {
                this.Src = new ChatMessage();
                this.Src.Role = GetRole(role);
                foreach (var c in contents)
                {
                    this.Src.Contents.Add(new TextContent(c.ToString()));
                }
            }
            public ChatMessageImpl(LLMChatMessage src)
            {
                this.Src = new ChatMessage();
                this.Src.Role = GetRole(src.Role);
                if (src.Contents != null)
                {
                    foreach (var c in src.Contents)
                    {
                        this.Src.Contents.Add(new TextContent(c.ToString()));
                    }
                }
            }
            public LLMRole Role { get => GetRole(Src.Role); set => Src.Role = GetRole(value); }
            public string Text { get => Src.Text; set => Src.Text = value; }
            public LLMContentCollection Contents => new ContentCollectionImpl() { Src = Src.Contents };
            public override string ToString() => Src.ToString();
        }
        struct ChatMessageCollectionImpl : LLMChatMessageCollection
        {
            public IList<ChatMessage> Src;
            public LLMChatMessage this[int index] { get => new ChatMessageImpl() { Src = Src[index] }; }
            public int Count => Src.Count;
            public IEnumerator<LLMChatMessage> GetEnumerator() => new It() { Src = Src.GetEnumerator() };
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            struct It : IEnumerator<LLMChatMessage>
            {
                public IEnumerator<ChatMessage> Src;
                public LLMChatMessage Current => new ChatMessageImpl() { Src = Src.Current };
                object IEnumerator.Current => Current;
                public void Dispose() => Src.Dispose();
                public bool MoveNext() => Src.MoveNext();
                public void Reset() => Src.Reset();
            }
            public override string ToString() => Src.ToString();
        }
        struct ChatResponseImpl : LLMChatResponse
        {
            public ChatResponse Src;
            public LLMChatMessageCollection Choices => new ChatMessageCollectionImpl() { Src = Src.Choices };
            public override string ToString() => Src.ToString();
        }
        #endregion
    }
}
