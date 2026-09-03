using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DeepCore.MinaClient.Sockets
{
    public class MinaSocketSession : BaseNetSession
    {
        private Logger log = LoggerFactory.GetLogger("NetSession");

        private TcpClient mTCP = null;
        protected INetPackageCodec mCodec;
        private IMinaClientSessionListener mListener;

        private Queue<Object> mSendQueue = new Queue<Object>();
        private List<object> mSendingArray = new List<object>(11);

        private Thread mWriteThread;
        private Thread mReadThread;

        protected DeepCore.IO.MemoryStream send_buff;
        protected DeepCore.IO.MemoryStream recv_buff;
        public MinaSocketSession()
        {
            this.send_buff = new DeepCore.IO.MemoryStream(8192);
            this.recv_buff = new DeepCore.IO.MemoryStream(8192);
        }
        /// <summary>
        /// 判断当前网络是否已经连接
        /// </summary>
        /// <returns></returns>
        public override bool IsConnected
        {
            get
            {
                TcpClient tcp = mTCP;
                if (tcp != null)
                {
                    return tcp.Connected;
                }
                return false;
            }
        }
        public override INetPackageCodec Codec
        {
            get { return mCodec; }
        }
        public override IPEndPoint RemoteAddress
        {
            get
            {
                if (mTCP != null) return mTCP.Client.RemoteEndPoint as IPEndPoint;
                return null;
            }
        }
        public TcpClient Session
        {
            get { return mTCP; }
        }

        public override bool Open(string url, INetPackageCodec codec, IMinaClientSessionListener listener)
        {
            bool ret = false;
            try
            {
                lock (this)
                {
                    if (mTCP == null)
                    {
                        this.mCodec = codec;
                        this.mListener = listener;
                        this.mURL = url;
                        string[] url_kv = url.Split(':');
                        //this.mRemoteAddress = IPUtil.ToEndPoint(url_kv[0], int.Parse(url_kv[1]));
                        //new IPEndPoint(IPAddress.Parse(url_kv[0]), int.Parse(url_kv[1]));
                        //this.mLocalAddress = IPUtil.CreateLocalConnectorEndPoint();//new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
                        //this.mRecvQueue.Clear();
                        lock (mSendQueue) this.mSendQueue.Clear();
                        this.mReadThread = new Thread(new ThreadStart(this.runRead));
                        this.mReadThread.IsBackground = true;
                        this.mWriteThread = new Thread(new ThreadStart(this.runWrite));
                        this.mWriteThread.IsBackground = true;
                        // 建立SOCKET链接
                        this.mTCP = new TcpClient();
                        //this.mTCP.ReceiveTimeout = 5000;
                        //this.mTCP.SendTimeout = 5000;
                        this.mTCP.NoDelay = true;
                        this.mTCP.Connect(url_kv[0], Parser.ParseInt(url_kv[1]));
                        //创建读写线程对象
                        this.mReadThread.Start();
                        this.mWriteThread.Start();
                        ret = true;
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                onException(new NetException("/n[Open:]" + URL +
                                                "/n[InnerException:]" + err.InnerException +
                                                "/n[Exception:]" + err.Message +
                                                "/n[Source:]" + err.Source +
                                                "/n[StackTrace:]" + err.StackTrace));
            }
            return ret;
        }

        public override bool Close()
        {
            bool ret = false;
            lock (this)
            {
                if (mTCP != null)
                {
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
                lock (mSendQueue)
                {
                    this.mSendQueue.Clear();
                }
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
                lock (mSendQueue)
                {
                    this.mSendQueue.Clear();
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
            }
            return ret;
        }

        //-------------------------------------------------------------------------------------

        /// <summary>
        /// 发送一个消息，该方法将立即返回。
        /// </summary>
        /// <param name="data"></param>
        public override void Send(Object data)
        {
            if (mTCP != null)
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

        override public string ToString()
        {
            return "Session[" + URL + "](" + GetHashCode() + ")";
        }

        //-------------------------------------------------------------------------------------

        private void onClose()
        {
            lock (mSendQueue)
            {
                mSendQueue.Clear();
                try
                {
                    if (send_buff != null) send_buff.Dispose();
                    if (recv_buff != null) recv_buff.Dispose();
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
                send_buff = null;
                recv_buff = null;
            }
            mListener.OnSessionClosed(this);
            if (mOnSessionClosed != null)
            {
                mOnSessionClosed.Invoke(this);
            }
        }
        private void onOpen()
        {
            mListener.OnSessionOpened(this);
            if (mOnSessionOpened != null)
            {
                mOnSessionOpened.Invoke(this);
            }
        }
        private void onReceive(Object message)
        {
            try
            {
                mListener.OnMessageReceived(this, message);
                if (mOnMessageReceived != null)
                {
                    mOnMessageReceived.Invoke(this, message);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                onException(err);
            }
        }
        private void onSent(Object message)
        {
            mListener.OnMessageSent(this, message);
            if (mOnMessageSent != null)
            {
                mOnMessageSent.Invoke(this, message);
            }
        }
        private void onException(Exception err)
        {
            mListener.OnError(this, err);
            if (mOnError != null)
            {
                mOnError.Invoke(this, err);
            }
        }

        //-------------------------------------------------------------------------------------

        private void innerClose()
        {
            lock (this)
            {
                if (mTCP != null)
                {
                    try
                    {
                        this.mTCP.Close();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }

        private void runWrite()
        {
            TcpClient _socket = mTCP;
            try
            {
                onOpen();
                Stream output = _socket.GetStream();
                while (_socket.Connected)
                {
                    lock (mSendQueue)
                    {
                        mSendingArray.Clear();
                        if (mSendQueue.Count > 0)
                        {
                            mSendingArray.AddRange(mSendQueue);
                            mSendQueue.Clear();
                        }
                        else
                        {
                            // 如果没有待传输消息，则等待输入
                            Monitor.Wait(mSendQueue, 100);
                        }
                    }
                    if (mSendingArray.Count > 0 && _socket.Connected)
                    {
                        for (int i = 0; i < mSendingArray.Count; i++)
                        {
                            object send_msg = mSendingArray[i];
                            try
                            {
                                int sendBytes;
                                if (doEncode(output, send_msg, out sendBytes))
                                {
                                    output.Flush();
                                    mSendPacks++;
                                    onSent(send_msg);
                                }
                                mSendBytes += sendBytes;
                            }
                            catch (Exception err)
                            {
                                try
                                {
                                    if (_socket.Connected)
                                    {
                                        log.Error(err.Message, err);
                                        innerClose();
                                        onException(new NetException(err.Message, err));
                                    }
                                }
                                catch (Exception err2)
                                {
                                    log.Error(err.Message, err2);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                onException(new NetException("/n[runWrite:]" + URL +
                            "/n[InnerException:]" + err.InnerException +
                            "/n[Exception:]" + err.Message +
                            "/n[Source:]" + err.Source +
                            "/n[StackTrace:]" + err.StackTrace, err));
            }
            finally
            {
                onClose();
            }
        }

        private void runRead()
        {
            TcpClient _socket = mTCP;
            try
            {
                Stream input = _socket.GetStream();
                while (_socket.Connected)
                {
                    try
                    {
                        Object msg = null;
                        int readBytes = 0;
                        if (_socket.Available > 0 && doDecode(input, out msg, out readBytes))
                        {
                            mRecvPacks++;
                            onReceive(msg);
                        }
                        mRecvBytes += readBytes;
                    }
                    catch (Exception err)
                    {
                        try
                        {
                            if (_socket.Connected)
                            {
                                if (err is EOFException) { }
                                else
                                {
                                    log.Error(err.Message, err);
                                    onException(new NetException(err.Message, err));
                                }
                                innerClose();
                            }
                        }
                        catch (Exception err2)
                        {
                            log.Error(err.Message, err2);
                        }
                        break;
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                onException(new NetException("runRead: " + err.Message));
            }
        }

        protected static readonly byte[] ZERO_BUFF = new byte[0];
        protected virtual bool doEncode(Stream output, object send_msg, out int sendBytes)
        {
            sendBytes = 0;
            send_buff.Position = 0;
            if (Codec.DoEncode(send_buff, send_msg))
            {
                byte[] h_body = send_buff.GetBuffer();
                int length = (int)send_buff.Position;
                LittleEdian.PutS32(output, length);
                output.Write(h_body, 0, length);
                sendBytes = length + 4;
                return true;
            }
            return false;
        }
        protected virtual bool doDecode(Stream input, out object msg, out int readBytes)
        {
            int length = LittleEdian.GetS32(input);
            if (length == -1)
            {
                msg = null;
                readBytes = 0;
                return false;
            }
            else if (length <= 0)
            {
                throw new Exception("received negative trunk size=" + length);
            }
            if (recv_buff.Capacity < length)
            {
                recv_buff.Capacity = length;
            }
            recv_buff.Position = 0;
            recv_buff.SetLength(length);
            byte[] buffer = recv_buff.GetBuffer();
            IOUtil.ReadToEnd(input, buffer, 0, length);
            readBytes = length + 4;
            recv_buff.Position = 0;
            if (Codec.DoDecode(recv_buff, out msg))
            {
                if (recv_buff.Position != length)
                {
                    throw new Exception(string.Format("can not decode full trunk size={0} type={1}", length, msg != null ? msg.GetType().FullName : msg));
                }
                return true;
            }
            return false;
        }


    }


}
