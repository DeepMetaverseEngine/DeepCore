using DeepCore.Concurrent;
using DeepCore.Log;
using DeepCore.MinaClient;
using DeepCore.Protocol;
using System;
using System.Collections.Generic;

namespace DeepCore.MinaClient
{
    /// <summary>
    /// 用于监听请求回馈的客户端程序
    /// </summary>
    public class MinaClient : IMinaClientSessionListener, IDisposable
    {
        private HashMap<int, object> mListenRequests = new HashMap<int, object>();

        private System.Threading.Timer mCheckTimer;

        public INetPackageCodec Codec {get; private set;}
        public IMinaClientSession Session { get; private set; }
        public bool IsConnected { get { return Session.IsConnected; } }



        public MinaClient(IMinaClientSession session, INetPackageCodec codec)
        {
            this.Session = session;
            this.Codec = codec;
            this.mCheckTimer = new System.Threading.Timer(check_request_timeout, this, 1000, 1000);
        }

        public bool Connect(string url)
        {
            return Session.Open(url, Codec, this);
        }

        public void Dispose()
        {
            mCheckTimer.Dispose();
            lock (mListenRequests)
            {
                mListenRequests.Clear();
            }
            Session.Close();
            Session.Dispose();
        }

        public void Send(IMessage msg)
        {
            Session.Send(msg);
        }

        public void SendResponse(IMessage rsponse, int requestMessageID)
        {
            rsponse.MessageID = requestMessageID;
            Session.Send(rsponse);
        }

        // -----------------------------------------------------------------------------------
        #region _REQUEST_RESPONSE_

        private AtomicInteger MessageIDGen = new AtomicInteger(1);

        public class Request
        {
            internal static Logger log = new LazyLogger("NetClient.Request");
            
            private OnResponseHandler mHandler;
            private OnRequestTimeoutHandler mTimeout;

            public MinaClient Client { get; private set; }
            public int TimeOutMS { get; private set; }
            public double EndTime { get; private set; }
            public double SendTime { get; private set; }
            public int MessageID { get; private set; }
            public IMessage RequestMessage { get; protected set; }
            public IMessage ResponseMessage { get; protected set; }

            public Request(MinaClient client, int timeOutMS, IMessage request, OnResponseHandler handler = null, OnRequestTimeoutHandler timeout = null)
            {
                this.Client = client;
                this.MessageID = request.MessageID = client.MessageIDGen.GetAndIncrement();
                this.RequestMessage = request;
                this.TimeOutMS = timeOutMS;
                this.SendTime = CUtils.TickTimeMS;
                this.EndTime = SendTime + timeOutMS;

                this.mHandler = handler;
                this.mTimeout = timeout;
            }
            virtual internal void onRecivedMessage(IMessage msg)
            {
                ResponseMessage = msg;
                if (mHandler != null)
                {
                    try
                    {
                        mHandler.Invoke(this);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
                mHandler = null;
                mTimeout = null;
            }
            virtual internal void onTimeout()
            {
                if (mTimeout != null)
                {
                    try
                    {
                        mTimeout.Invoke(this);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
                mHandler = null;
                mTimeout = null;
            }
        }

        public delegate void OnResponseHandler(Request req);
        public delegate void OnRequestTimeoutHandler(Request req);

        public Request SendRequest(IMessage msg, OnResponseHandler handler, OnRequestTimeoutHandler timeout = null, int timeOutMS = 15000)
        {
            Request req = new Request(this, timeOutMS, msg, handler, timeout);
            lock (mListenRequests)
            {
                mListenRequests.Add(req.MessageID, req);
            }
            Session.Send(msg);
            return req;
        }

        #endregion
        // -----------------------------------------------------------------------------------
        #region _GENERIC_REQUEST_RESPONSE_

        public class TRequest<REQ, RES> : Request
            where REQ : IMessage
            where RES : IMessage
        {
            private OnTResponseHandler<REQ, RES> mTHandler;
            private OnTRequestTimeoutHandler<REQ, RES> mTTimeout;

            public TRequest(MinaClient client, int timeOutMS, IMessage request, OnTResponseHandler<REQ, RES> handler = null, OnTRequestTimeoutHandler<REQ, RES> timeout = null)
                : base(client, timeOutMS, request)
            {
                this.mTHandler = handler;
                this.mTTimeout = timeout;
            }
            public REQ TRequestMessage { get { return (REQ)base.RequestMessage; } }
            public RES TResponseMessage { get { return (RES)base.ResponseMessage; } }

            override internal void onRecivedMessage(IMessage msg)
            {
                base.ResponseMessage = msg;
                if (mTHandler != null)
                {
                    try
                    {
                        mTHandler.Invoke(this);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
                mTHandler = null;
                mTTimeout = null;
            }
            override internal void onTimeout()
            {
                if (mTTimeout != null)
                {
                    try
                    {
                        mTTimeout.Invoke(this);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
                mTHandler = null;
                mTTimeout = null;
            }
        }

        public delegate void OnTResponseHandler<REQ, RES>(TRequest<REQ, RES> req)
            where REQ : IMessage
            where RES : IMessage;

        public delegate void OnTRequestTimeoutHandler<REQ, RES>(TRequest<REQ, RES> req)
            where REQ : IMessage
            where RES : IMessage;
        
        public TRequest<REQ, RES> SendTRequest<REQ, RES>(REQ msg, OnTResponseHandler<REQ, RES> handler, OnTRequestTimeoutHandler<REQ, RES> timeout = null, int timeOutMS = 15000)
            where REQ : IMessage
            where RES : IMessage
        {
            TRequest<REQ, RES> req = new TRequest<REQ, RES>(this, timeOutMS, msg, handler, timeout); 
            lock (mListenRequests)
            {
                mListenRequests.Add(req.MessageID, req);
            }
            Session.Send(msg);
            return req;
        }

        #endregion
        // -----------------------------------------------------------------------------------

        private void check_request_timeout(object state)
        {
            double curTime = CUtils.TickTimeMS;
            lock (mListenRequests)
            {
                List<Request> removing = null;
                foreach (Request req in mListenRequests.Values)
                {
                    if (req.EndTime < curTime)
                    {
                        if (removing == null)
                        {
                            removing = new List<Request>();
                        }
                        removing.Add(req);
                    }
                }
                if (removing != null)
                {
                    foreach (Request remove in removing)
                    {
                        mListenRequests.RemoveByKey(remove.MessageID);
                        remove.onTimeout();
                    }
                }
            }
        }


        // -----------------------------------------------------------------------------------
        #region SessionListener

        void IMinaClientSessionListener.OnSessionOpened(IMinaClientSession session)
        {
        }
        void IMinaClientSessionListener.OnSessionClosed(IMinaClientSession session)
        {
            lock (mListenRequests)
            {
                mListenRequests.Clear();
            }
        }
        void IMinaClientSessionListener.OnMessageReceived(IMinaClientSession session, object data)
        {
            if (data is IMessage)
            {
                IMessage msg = data as IMessage;
                Request request = null;
                lock (mListenRequests)
                {
                    request = mListenRequests.RemoveByKey(msg.MessageID) as Request;
                }
                if (request != null)
                {
                    request.onRecivedMessage(msg);
                }
            }
        }
        void IMinaClientSessionListener.OnMessageSent(IMinaClientSession session, object data)
        {
        }
        void IMinaClientSessionListener.OnError(IMinaClientSession session, Exception err)
        {
        }

        #endregion

        // -----------------------------------------------------------------------------------
    }
}
