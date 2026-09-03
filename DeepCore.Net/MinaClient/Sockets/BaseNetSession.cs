using DeepCore.Protocol;
using System;
using System.Collections.Generic;
using System.Net;


namespace DeepCore.MinaClient.Sockets
{
    public abstract class BaseNetSession : IMinaClientSession
    {
        protected string mURL;
        protected long mSendBytes = 0;
        protected long mRecvBytes = 0;
        protected long mSendPacks = 0;
        protected long mRecvPacks = 0;
        private HashMap<string, object> mAttributes = new HashMap<string, object>();

        public IDictionary<string, object> Attributes { get; } = new Dictionary<string, object>();
        public long TotalSentBytes { get { return mSendBytes; } }
        public long TotalRecvBytes { get { return mRecvBytes; } }
        public long TotalSentPackages { get { return mSendPacks; } }
        public long TotalRecvPackages { get { return mRecvPacks; } }
        public string URL { get { return mURL; } }
        public abstract IPEndPoint RemoteAddress { get; }
        public abstract INetPackageCodec Codec { get; }
        /// <summary>
        /// 判断当前网络是否已经连接
        /// </summary>
        /// <returns></returns>
        public abstract bool IsConnected { get; }

        public abstract bool Open(string url, INetPackageCodec codec, IMinaClientSessionListener listener);

        public abstract bool Close();

        public virtual void Dispose()
        {
            Close();
        }

        //-------------------------------------------------------------------------------------

        /// <summary>
        /// 发送一个消息，该方法将立即返回。
        /// </summary>
        /// <param name="data"></param>
        public abstract void Send(Object data);
        
        //-------------------------------------------------------------------------------------

        public object GetAttribute(string key)
        {
            return mAttributes[key];
        }

        public void SetAttribute(string key, object value)
        {
            mAttributes[key] = value;
        }

        public void RemoveAttribute(string key)
        {
            mAttributes.Remove(key);
        }

        public bool ContainsAttribute(string key)
        {
            return mAttributes.ContainsKey(key);
        }

        public IEnumerable<string> GetAttributeKeys()
        {
            return mAttributes.Keys;
        }

        public override string ToString()
        {
            return "Session[" + URL + "](" + GetHashCode() + ")";
        }

        //-------------------------------------------------------------------------------------

        protected OnSessionOpenedHandler mOnSessionOpened;
        protected OnSessionClosedHandler mOnSessionClosed;
        protected OnMessageReceivedHandler mOnMessageReceived;
        protected OnMessageSentHandler mOnMessageSent;
        protected OnErrorHandler mOnError;

        public event OnSessionOpenedHandler OnSessionOpened { add { mOnSessionOpened += value; } remove { mOnSessionOpened -= value; } }
        public event OnSessionClosedHandler OnSessionClosed { add { mOnSessionClosed += value; } remove { mOnSessionClosed -= value; } }
        public event OnMessageReceivedHandler OnMessageReceived { add { mOnMessageReceived += value; } remove { mOnMessageReceived -= value; } }
        public event OnMessageSentHandler OnMessageSent { add { mOnMessageSent += value; } remove { mOnMessageSent -= value; } }
        public event OnErrorHandler OnError { add { mOnError += value; } remove { mOnError -= value; } }

        //-------------------------------------------------------------------------------------

    }


}
