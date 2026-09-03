using DeepCore;
using DeepCore.Log;
using DeepCore.MinaClient;
using DeepCore.Protocol;
using DeepCrystal.Server;
using DeepCrystal.SharpMinaServer;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace DeepFrozen.Server.SSocket.NetServer
{
    public class SSMniaSession : AppSession<SSMniaSession, BinaryRequestInfo>, IMinaSession
    {
        private Logger log = LoggerFactory.GetLogger("SSSession");
        private HashMap<string, object> mAttributes = new HashMap<string, object>();
        private INetPackageCodec mCodec;
        private bool mClosing = false;
        private SSMinaServer mOwner;
        public IDictionary<string, object> Attributes { get; } = new Dictionary<string, object>();
        public SSMniaSession() { }

        public string ID { get { return base.SessionID; } }
        public bool IsConnected { get { return base.Connected; } }
        public long TotalSentBytes { get; private set; }
        public long TotalRecvBytes { get; private set; }
        public IMinaSessionListener Listener { get; private set; }

        internal void NewSessionConnected(SSMinaServer owner, IMinaSessionListener listener, INetPackageCodec codec)
        {
            this.Listener = listener;
            this.mCodec = codec;
            this.mOwner = owner;
        }
        public override string ToString()
        {
            return string.Format("{0}-{1}", base.SessionID, base.RemoteEndPoint);
        }
        private void send_internal(DeepCore.IO.MemoryStream stream)
        {
            //模拟网络卡//
            if (mOwner.EmulateLag)
            {
                mOwner.SendDelay(this, stream);
            }
            else
            {
                try
                {
                    base.Send(stream.GetBuffer(), 0, (int)stream.Position);
                }
                finally
                {
                    stream.Dispose();
                }
            }
        }
        private void recv_internal(object obj)
        {
            if (mOwner.EmulateLag)
            {
                mOwner.RecvDelay(this, obj);
            }
            else
            {
                this.Listener.OnReceivedMessage(this, obj);
            }
        }

        internal void NewRequestReceived(int bytes, object obj)
        {
            this.TotalRecvBytes += bytes;
            recv_internal(obj);
        }

        public bool Send(object message)
        {
            if (mClosing) return false;
            var stream = new DeepCore.IO.MemoryStream(SSMinaServerFactory.SEND_BUFF_SIZE) ;
            if (mOwner.DoEncodeInternal(this, message, stream))
            {
                int len = (int)stream.Position;
                try
                {
                    send_internal(stream);
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                    Disconnect(false);
                    return false;
                }
                this.TotalSentBytes += len;
                mOwner.mTotalSentBytes += len;
                Listener.OnSentMessage(this, message);
                return true;
            }
            else
            {
                stream.Dispose();
            }
            return false;
        }
        public bool Disconnect(bool force)
        {
            lock (this)
            {
                if (mClosing) return false;
                mClosing = true;
            }
            base.Close(CloseReason.ClientClosing);
            return true;
        }

        protected override void OnSessionStarted()
        {
            base.OnSessionStarted();
            Listener.OnConnected(this);
        }

        protected override void OnSessionClosed(CloseReason reason)
        {
            base.OnSessionClosed(reason);
            Listener.OnDisconnected(this, reason.ToString());
        }

        protected override void HandleException(Exception e)
        {
            base.HandleException(e);
            if (Listener != null)
            {
                Listener.OnError(this, e);
            }
        }

        public IPEndPoint GetRemoteAddress()
        {
            return base.RemoteEndPoint;
        }

        #region ________Attributes________

        public object GetAttribute(string key)
        {
            return mAttributes.Get(key);
        }

        public void SetAttribute(string key, object value)
        {
            mAttributes.Put(key, value);
        }

        public void RemoveAttribute(string key)
        {
            mAttributes.RemoveByKey(key);
        }

        public bool ContainsAttribute(string key)
        {
            return mAttributes.ContainsKey(key);
        }

        public IEnumerable<string> GetAttributeKeys()
        {
            return mAttributes.Keys;
        }

        #endregion
    }
}
