using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace DeepCore.MinaClient.Sockets
{
    public class MinaSocketSessionAsync : BaseNetSession
    {
        private Logger log = LoggerFactory.GetLogger("NetSessionAsync");

        private Socket mTCP = null;
        protected INetPackageCodec mCodec;
        private IMinaClientSessionListener mListener;

        private Queue<Object> mSendQueue = new Queue<Object>();

        public MinaSocketSessionAsync()
        {
        }

        /// <summary>
        /// 判断当前网络是否已经连接
        /// </summary>
        /// <returns></returns>
        public override bool IsConnected
        {
            get
            {
                Socket tcp = mTCP;
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
                if (mTCP != null) return mTCP.RemoteEndPoint as IPEndPoint;
                return null;
            }
        }
        public Socket Session
        {
            get { return mTCP; }
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
        #region Open

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
                        lock (mSendQueue) this.mSendQueue.Clear();
                        // 建立SOCKET链接
                        this.mTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        //this.mTCP.ReceiveTimeout = 5000;
                        //this.mTCP.SendTimeout = 5000;
                        this.mTCP.NoDelay = true;
                        this.mTCP.BeginConnect(url_kv[0], Parser.ParseInt(url_kv[1]), endConnect, mTCP);
                        //创建读写线程对象
                        ret = true;
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                onException(new NetException("\n[Open:]" + URL +
                                                "\n[InnerException:]" + err.InnerException +
                                                "\n[Exception:]" + err.Message +
                                                "\n[Source:]" + err.Source +
                                                "\n[StackTrace:]" + err.StackTrace));
            }
            return ret;
        }

        private void endConnect(IAsyncResult result)
        {
            mTCP.EndConnect(result);
            if (result.IsCompleted)
            {
                onOpen();
                startReceiveHead();
            }
            else
            {
                Close();
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

        #endregion
        //-------------------------------------------------------------------------------------
        #region Close

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
                    finally
                    {
                        lock (mSendQueue)
                        {
                            this.mSendQueue.Clear();
                        }
                    }
                    this.mTCP = null;
                    ret = true;
                    onClose();
                }
            }
            return ret;
        }

        private void onClose()
        {
            lock (mSendQueue)
            {
                mSendQueue.Clear();
            }
            mListener.OnSessionClosed(this);
            if (mOnSessionClosed != null)
            {
                mOnSessionClosed.Invoke(this);
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------------------------------------------------
        #region Send

        /// <summary>
        /// 发送一个消息，该方法将立即返回。
        /// </summary>
        /// <param name="data"></param>
        public override void Send(Object data)
        {
            lock (this)
            {
                if (mTCP != null)
                {
                    lock (mSendQueue)
                    {
                        mSendQueue.Enqueue(data);
                    }
                    // 通知写线程开始工作。
                    startSend();
                }
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

        private class SendObject
        {
            public readonly List<object> sending = new List<object>();
            public readonly DeepCore.IO.MemoryStream buffer = new DeepCore.IO.MemoryStream(1024);

        }
        private class SendObjectPool
        {
            private ObjectPool<SendObject> s_Pool = new ConcurrentObjectPool<SendObject>();          
            public SendObject Alloc()
            {
                SendObject ret = s_Pool.Get(this, static (t, p) => new SendObject());
                ret.buffer.Position = 0;
                ret.buffer.SetLength(0);
                ret.sending.Clear();
                return ret;
            }
            public void Release(SendObject toRelease)
            {
                toRelease.sending.Clear();
                s_Pool.Release(toRelease);
            }
        }
        private SendObjectPool mSendPool = new SendObjectPool();

        private void startSend()
        {
            var sending = mSendPool.Alloc();
            try
            {
                lock (mSendQueue)
                {
                    if (mSendQueue.Count > 0)
                    {
                        sending.sending.AddRange(mSendQueue);
                        mSendQueue.Clear();
                    }
                }
                if (sending.sending.Count > 0 && mTCP.Connected)
                {
                    for (int i = 0; i < sending.sending.Count; i++)
                    {
                        object send_msg = sending.sending[i];
                        doEncode(sending.buffer, send_msg);
                    }
                }
                mTCP.BeginSend(sending.buffer.GetBuffer(), 0, (int)sending.buffer.Length, SocketFlags.None, endSend, sending);
            }
            catch (Exception err)
            {
                mSendPool.Release(sending);
                log.Error(err.Message, err);
                onException(new NetException("\n[runWrite:]" + URL +
                            "\n[InnerException:]" + err.InnerException +
                            "\n[Exception:]" + err.Message +
                            "\n[Source:]" + err.Source +
                            "\n[StackTrace:]" + err.StackTrace, err));
                this.Close();
            }
        }
        private void endSend(IAsyncResult result)
        {
            if (result.IsCompleted)
            {
                var sending = result.AsyncState as SendObject;
                try
                {
                    int length = mTCP.EndSend(result);
                    if (length > 0)
                    {
                        mSendBytes += length;
                        mSendPacks += sending.sending.Count;
                        for (int i = 0; i < sending.sending.Count; i++)
                        {
                            object send_msg = sending.sending[i];
                            onSent(send_msg);
                        }
                        sending.sending.Clear();
                    }
                    else
                    {
                        Close();
                    }
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                    onException(new NetException("endReceive: " + err.Message));
                    this.Close();
                }
                finally
                {
                    mSendPool.Release(sending);
                }
            }
            else
            {
                log.Trace("Continue send !");
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------
        #region Receive

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
        private class ReceiveObject
        {
            public readonly byte[] head = new byte[4];
            public int head_position = 0;
            public int body_length = 0;
            public int body_position = 0;
            public readonly DeepCore.IO.MemoryStream body_buffer = new DeepCore.IO.MemoryStream(1024);
        }
        private class ReceiveObjectPool
        {
            private ObjectPool<ReceiveObject> s_Pool = new ConcurrentObjectPool<ReceiveObject>();     
            public ReceiveObject Alloc()
            {
                ReceiveObject ret = s_Pool.Get(this,static (t,p)=> new ReceiveObject());
                ret.body_buffer.Position = 0;
                ret.body_buffer.SetLength(0);
                ret.head_position = 0;
                ret.body_length = 0;
                ret.body_position = 0;
                return ret;
            }
            public void Release(ReceiveObject toRelease)
            {
                s_Pool.Release(toRelease);
            }
        }
        private ReceiveObjectPool mReceivePool = new ReceiveObjectPool();

        private void startReceiveHead()
        {
            var recv_object = mReceivePool.Alloc();
            try
            {
                mTCP.BeginReceive(
                    recv_object.head,
                    recv_object.head_position,
                    recv_object.head.Length - recv_object.head_position,
                    SocketFlags.None, endReceiveHead, recv_object);
            }
            catch (Exception err)
            {
                mReceivePool.Release(recv_object);
                log.Error(err.Message, err);
                onException(new NetException("endReceive: " + err.Message));
                this.Close();
            }
        }
        private void endReceiveHead(IAsyncResult result)
        {
            var recv_object = result.AsyncState as ReceiveObject;
            try
            {
                int length = mTCP.EndReceive(result);
                if (length > 0)
                {
                    mRecvBytes += length;
                    recv_object.head_position += length;
                    if (recv_object.head_position == recv_object.head.Length)
                    {
                        recv_object.body_length = GetBodyLength(recv_object.head);
                        recv_object.body_position = 0;
                        if (recv_object.body_buffer.Capacity < recv_object.body_length)
                        {
                            recv_object.body_buffer.Capacity = recv_object.body_length;
                        }
                        recv_object.body_buffer.SetLength(recv_object.body_length);
                        startReceiveBody(recv_object);
                    }
                    else if (recv_object.head_position > recv_object.head.Length)
                    {
                        throw new NetException("Receive head overfollow");
                    }
                    else
                    {
                        startReceiveHead();
                    }
                }
                else
                {
                    throw new NetException("Receive 0 bytes!");
                }
            }
            catch (Exception err)
            {
                mReceivePool.Release(recv_object);
                log.Error(err.Message, err);
                onException(new NetException("endReceive: " + err.Message));
                this.Close();
            }
        }
        private void startReceiveBody(ReceiveObject recv_object)
        {
            try
            {
                mTCP.BeginReceive(
                    recv_object.body_buffer.GetBuffer(),
                    recv_object.body_position,
                    recv_object.body_length - recv_object.body_position,
                    SocketFlags.None, endReceiveBody, recv_object);
            }
            catch (Exception err)
            {
                mReceivePool.Release(recv_object);
                log.Error(err.Message, err);
                onException(new NetException("endReceive: " + err.Message));
                this.Close();
            }
        }
        private void endReceiveBody(IAsyncResult result)
        {
            var recv_object = result.AsyncState as ReceiveObject;
            try
            {
                int length = mTCP.EndReceive(result);
                if (length > 0)
                {
                    mRecvBytes += length;
                    recv_object.body_position += length;
                    if (recv_object.body_position == recv_object.body_length)
                    {
                        Object msg = null;
                        var input = recv_object.body_buffer;
                        input.Position = 0;
                        if (doDecode(input, out msg))
                        {
                            mRecvPacks++;
                            onReceive(msg);
                        }
                        recv_object.head_position = 0;
                        startReceiveHead();
                        mReceivePool.Release(recv_object);
                    }
                    else if (recv_object.body_position > recv_object.body_length)
                    {
                        throw new NetException("Receive body overfollow");
                    }
                    else
                    {
                        startReceiveBody(recv_object);
                    }
                }
                else
                {
                    throw new NetException("Receive 0 bytes!");
                }
            }
            catch (Exception err)
            {
                mReceivePool.Release(recv_object);
                log.Error(err.Message, err);
                onException(new NetException("endReceive: " + err.Message));
                this.Close();
            }
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------
        protected virtual int GetBodyLength(byte[] buffer)
        {
            int pos = 0;
            int length = LittleEdian.GetS32(buffer, ref pos);
            return length;
        }
        protected virtual bool doEncode(Stream output, object send_msg)
        {
            long old_position = output.Position;
            LittleEdian.PutS32(output, 0);
            if (Codec.DoEncode(output, send_msg))
            {
                int full_length = (int)(output.Position - old_position);
                output.Position = old_position;
                LittleEdian.PutS32(output, full_length - 4);
                output.Position = old_position + full_length;
                return true;
            }
            return false;
        }
        protected virtual bool doDecode(Stream input, out object msg)
        {
            if (Codec.DoDecode(input, out msg))
            {
                if (input.Position != input.Length)
                {
                    throw new Exception(string.Format("can not decode full trunk size={0} type={1}", input.Length, msg != null ? msg.GetType().FullName : msg));
                }
                return true;
            }
            return false;
        }

    }


}
