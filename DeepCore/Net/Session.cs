using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace DeepCore.Net
{
    [Reflectible]
    public interface INetSession
    {
        bool IsConnected { get; }
        long TotalSentBytes { get; }
        long TotalRecvBytes { get; }
        IDictionary<string, object> Attributes { get; }
    }

    /*
    public abstract class AbstractSocketSession : IServerSession, IDisposable
    {
        protected readonly Logger log = LoggerFactory.GetLogger("SocketSession");

        private long mSendBytes = 0;
        private long mRecvBytes = 0;
        private long mSendPacks = 0;
        private long mRecvPacks = 0;
        private HashMap<string, object> mAttributes = new HashMap<string, object>();
        private TcpClient mTCP = null;
        private Queue<Object> mSendQueue = new Queue<Object>();
        private Thread mWriteThread;
        private Thread mReadThread;

        public IDictionary<string, object> Attributes => mAttributes;
        public int BufferSize { get; set; }
        public int TimeoutMS { get; set; }
        public bool NoDelay { get; set; }
        public long TotalSentBytes { get { return mSendBytes; } }
        public long TotalRecvBytes { get { return mRecvBytes; } }
        public long TotalSentPackages { get { return mSendPacks; } }
        public long TotalRecvPackages { get { return mRecvPacks; } }
        public bool IsConnected
        {
            get
            {
                var socket = mTCP;
                if (socket != null) { return socket.Connected; }
                return false;
            }
        }
        public TcpClient Client
        {
            get { return mTCP; }
        }

        //--------------------------------------------------------------------------------------------------------

        public AbstractSocketSession()
        {
            this.BufferSize = 4096;
            this.TimeoutMS = 30000;
            this.NoDelay = true;
        }


        public virtual bool ConnectAsync(string host, int port)
        {
            return innerOpen(host, port, (addr, so) =>
            {
                so.BeginConnect(addr, port, (result) =>
                {
                    try
                    {
                        var s = result.AsyncState as TcpClient;
                        s.EndConnect(result);
                        mReadThread.Start(s);
                        mWriteThread.Start(s);
                    }
                    catch (Exception err)
                    {
                        onError(err);
                    }
                }, so);
                return true;
            });
        }

        public virtual bool Connect(string host, int port)
        {
            return innerOpen(host, port, (addr, so) =>
            {
                so.Connect(addr, port);
                mReadThread.Start(so);
                mWriteThread.Start(so);
                return so.Connected;
            });
        }

        public virtual bool Close()
        {
            bool ret = false;
            lock (this)
            {
                if (mTCP != null)
                {
                    try
                    {
                        this.mTCP.Client.Disconnect(false);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    try
                    {
                        this.mTCP.Close();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    this.mTCP = null;
                    ret = true;
                }
            }
            if (ret)
            {
                if (mReadThread != null)
                {
                    try
                    {
                        this.mReadThread.Join(1000);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    this.mReadThread = null;
                }
                if (mWriteThread != null)
                {
                    try
                    {
                        this.mWriteThread.Join(1000);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    this.mWriteThread = null;
                }
                lock (mSendQueue)
                {
                    foreach (var o in mSendQueue) { onSent(o); }
                    this.mSendQueue.Clear();
                }
            }
            return ret;
        }

        /// <summary>
        /// 发送一个消息，该方法将立即返回。
        /// </summary>
        /// <param name="data"></param>
        public virtual void Send(Object data)
        {
            if (IsConnected)
            {
                lock (mSendQueue)
                {
                    mSendQueue.Enqueue(data);
                    // 通知写线程开始工作。
                    Monitor.PulseAll(mSendQueue);
                }
            }
        }

        //-------------------------------------------------------------------------------------
        #region Internal

        private bool innerOpen(string host, int port, Func<IPAddress[], TcpClient, bool> doConnect)
        {
            try
            {
                AddressFamily family;
                IPHostEntry ips;
                TcpClient so;
                var addrs = IPUtil.GetIPAddress(host, port, out family, out ips);
                lock (this)
                {
                    if (mTCP != null)
                    {
                        log.Error("the socket already connected!");
                        return false;
                    }
                    //创建读写线程对象//
                    lock (mSendQueue) this.mSendQueue.Clear();
                    this.mReadThread = new Thread(new ParameterizedThreadStart(this.runRead));
                    this.mReadThread.IsBackground = true;
                    this.mReadThread.Name = "SessionRead";
                    this.mWriteThread = new Thread(new ParameterizedThreadStart(this.runWrite));
                    this.mWriteThread.IsBackground = true;
                    this.mWriteThread.Name = "SessionWrite";
                    //创建套接字//
                    so = this.mTCP = new TcpClient(family);
                    so.SendTimeout = this.TimeoutMS;
                    so.ReceiveTimeout = this.TimeoutMS;
                    so.NoDelay = this.NoDelay;
                    so.ReceiveBufferSize = this.BufferSize;
                    so.SendBufferSize = this.BufferSize;
                    so.Client.Blocking = true;
                    return doConnect(addrs, so);
                }
            }
            catch (Exception err)
            {
                onError(err);
            }
            return false;
        }

        private void innerClose(TcpClient tcp)
        {
            if (tcp != null)
            {
                try
                {
                    tcp.Close();
                }
                catch (Exception err)
                {
                    onError(err);
                }
            }
        }

        private void runWrite(object state)
        {
            var _socket = state as TcpClient;
            try
            {
                onOpen();
                var output = _socket.GetStream();
                object sending = null;
                int sendBytes;
                while (_socket.Connected)
                {
                    lock (mSendQueue)
                    {
                        if (mSendQueue.Count > 0)
                        {
                            sending = mSendQueue.Dequeue();
                        }
                        else
                        {
                            // 如果没有待传输消息，则等待输入
                            Monitor.Wait(mSendQueue, 100);
                        }
                    }
                    if (sending != null)
                    {
                        try
                        {
                            if (doEncode(output, sending, out sendBytes))
                            {
                                mSendPacks++;
                                output.Flush();
                            }
                            mSendBytes += sendBytes;
                        }
                        catch (Exception err)
                        {
                            onError(err);
                            if (_socket.Connected)
                            {
                                innerClose(_socket);
                            }
                        }
                        finally
                        {
                            onSent(sending);
                            sending = null;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                onError(err);
            }
            finally
            {
                onClose();
            }
        }

        private void runRead(object state)
        {
            var _socket = state as TcpClient;
            try
            {
                var input = _socket.GetStream();
                while (_socket.Connected)
                {
                    try
                    {
                        Object msg = null;
                        int readBytes;
                        if (doDecode(input, out msg, out readBytes))
                        {
                            mRecvPacks++;
                            onReceive(msg);
                        }
                        mRecvBytes += readBytes;
                    }
                    catch (Exception err)
                    {
                        onError(err);
                        if (_socket.Connected)
                        {
                            innerClose(_socket);
                        }
                        break;
                    }
                }
            }
            catch (Exception err)
            {
                onError(err);
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------
        #region EncodeDecode
        protected static readonly byte[] ZERO_BUFF = new byte[0];

        protected virtual bool doEncode(Stream output, object send_msg, out int sendBytes)
        {
            sendBytes = 0;
            return false;
        }
        protected virtual bool doDecode(Stream input, out object msg, out int readBytes)
        {
            msg = null;
            readBytes = 0;
            return false;
        }

        //-------------------------------------------------------------------------------------
        #endregion
        //-------------------------------------------------------------------------------------
        #region Attributes

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

        #endregion
        //-------------------------------------------------------------------------------------
        #region Events

        private void onOpen()
        {
            if (event_OnConnected != null) event_OnConnected.Invoke(this);
        }
        private void onClose()
        {
            lock (mSendQueue)
            {
                mSendQueue.Clear();
            }
            if (event_OnClosed != null) event_OnClosed.Invoke(this);
        }
        private void onReceive(object message)
        {
            if (event_OnReceived != null) event_OnReceived.Invoke(this, message);
        }
        private void onSent(object message)
        {
            if (event_OnSent != null) event_OnSent.Invoke(this, message);
        }
        private void onError(Exception err)
        {
            log.Error(err.Message, err);
            if (event_OnError != null) event_OnError.Invoke(this, err);
        }

        public event Action<object> OnClosed { add { event_OnClosed += value; } remove { event_OnClosed -= value; } }
        public event Action<object> OnConnected { add { event_OnConnected += value; } remove { event_OnConnected -= value; } }
        public event Action<object, object> OnReceived { add { event_OnReceived += value; } remove { event_OnReceived -= value; } }
        public event Action<object, object> OnSent { add { event_OnSent += value; } remove { event_OnSent -= value; } }
        public event Action<object, Exception> OnError { add { event_OnError += value; } remove { event_OnError -= value; } }

        private Action<object> event_OnClosed;
        private Action<object> event_OnConnected;
        private Action<object, object> event_OnReceived;
        private Action<object, object> event_OnSent;
        private Action<object, Exception> event_OnError;

        public virtual void Dispose()
        {
            Close();
            event_OnClosed = null;
            event_OnConnected = null;
            event_OnReceived = null;
            event_OnSent = null;
            event_OnError = null;
            mAttributes.Clear();
        }
        #endregion
        //-------------------------------------------------------------------------------------
    }
    */

}
