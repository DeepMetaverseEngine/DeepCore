#if !UNITY_WEBGL || UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace NativeWebSocket
{
    public class WebSocket : IWebSocket
    {
        private static readonly SendOrPostCallback InvokeQueuedEventCallback = InvokeQueuedEvent;
        private static readonly SendOrPostCallback DispatchQueuedMessagesCallback = DispatchQueuedMessagesOnSyncContext;

        public event WebSocketOpenEventHandler OnOpen;
        public event WebSocketMessageEventHandler OnMessage;
        public event WebSocketErrorEventHandler OnError;
        public event WebSocketCloseEventHandler OnClose;

        private Uri uri;
        private Dictionary<string, string> headers;
        private List<string> subprotocols;
        private bool isDisposed = false;
        private ClientWebSocket m_Socket;

        private CancellationTokenSource m_TokenSource;
        private CancellationToken m_CancellationToken;

        private readonly SynchronizationContext _syncContext;
        private List<Action> m_EventQueue = new List<Action>();
        private List<Action> m_DispatchQueue = new List<Action>();
        private readonly object EventQueueLock = new object();

        private List<byte[]> m_MessageQueue = new List<byte[]>();
        private List<byte[]> m_MessageDispatchQueue = new List<byte[]>();
        private readonly object MessageQueueLock = new object();
        private int isMessageDispatchScheduled = 0;

        private readonly object OutgoingMessageLock = new object();
        private bool isSending = false;
        private Queue<MemoryStream> sendBytesQueue = new Queue<MemoryStream>();
        private Queue<MemoryStream> sendTextQueue = new Queue<MemoryStream>();

        public WebSocket(string url, Dictionary<string, string> headers = null)
        {
            uri = new Uri(url);
            _syncContext = SynchronizationContext.Current;
            this.headers = headers;
            subprotocols = null;

            string protocol = uri.Scheme;
            if (!protocol.Equals("ws") && !protocol.Equals("wss"))
                throw new ArgumentException("Unsupported protocol: " + protocol);
        }

        public WebSocket(string url, string subprotocol, Dictionary<string, string> headers = null)
        {
            uri = new Uri(url);
            _syncContext = SynchronizationContext.Current;
            this.headers = headers;
            subprotocols = new List<string>(1) { subprotocol };

            string protocol = uri.Scheme;
            if (!protocol.Equals("ws") && !protocol.Equals("wss"))
                throw new ArgumentException("Unsupported protocol: " + protocol);
        }

        public WebSocket(string url, List<string> subprotocols, Dictionary<string, string> headers = null)
        {
            uri = new Uri(url);
            _syncContext = SynchronizationContext.Current;
            this.headers = headers;
            this.subprotocols = subprotocols;

            string protocol = uri.Scheme;
            if (!protocol.Equals("ws") && !protocol.Equals("wss"))
                throw new ArgumentException("Unsupported protocol: " + protocol);
        }
        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            memPool.Clear();
            sendBytesQueue.Clear();
            sendTextQueue.Clear();
        }
        private void EnqueueEvent(Action action)
        {
            if (_syncContext != null)
            {
                _syncContext.Post(InvokeQueuedEventCallback, action);
            }
            else
            {
                lock (EventQueueLock)
                {
                    m_EventQueue.Add(action);
                }
            }
        }

        private void EnqueueMessage(byte[] data)
        {
            lock (MessageQueueLock)
            {
                m_MessageQueue.Add(data);
            }

            if (_syncContext != null && Interlocked.CompareExchange(ref isMessageDispatchScheduled, 1, 0) == 0)
            {
                _syncContext.Post(DispatchQueuedMessagesCallback, this);
            }
        }

        private static void InvokeQueuedEvent(object action)
        {
            ((Action)action)();
        }

        private static void DispatchQueuedMessagesOnSyncContext(object socket)
        {
            ((WebSocket)socket).DispatchQueuedMessagesOnSyncContext();
        }

        private void DispatchQueuedMessagesOnSyncContext()
        {
            while (true)
            {
                DispatchQueuedMessages();
                Interlocked.Exchange(ref isMessageDispatchScheduled, 0);

                lock (MessageQueueLock)
                {
                    if (m_MessageQueue.Count == 0 || Interlocked.CompareExchange(ref isMessageDispatchScheduled, 1, 0) != 0)
                    {
                        return;
                    }
                }
            }
        }

        private void DispatchQueuedMessages()
        {
            if (m_MessageQueue.Count == 0)
            {
                return;
            }

            lock (MessageQueueLock)
            {
                if (m_MessageQueue.Count == 0)
                {
                    return;
                }

                var tmp = m_MessageDispatchQueue;
                m_MessageDispatchQueue = m_MessageQueue;
                m_MessageQueue = tmp;
            }

            for (int i = 0; i < m_MessageDispatchQueue.Count; i++)
            {
                OnMessage?.Invoke(m_MessageDispatchQueue[i]);
            }

            m_MessageDispatchQueue.Clear();
        }

        private void DispatchQueuedEvents()
        {
            if (m_EventQueue.Count == 0)
            {
                return;
            }

            lock (EventQueueLock)
            {
                if (m_EventQueue.Count == 0)
                {
                    return;
                }

                var tmp = m_DispatchQueue;
                m_DispatchQueue = m_EventQueue;
                m_EventQueue = tmp;
            }

            foreach (var action in m_DispatchQueue)
            {
                action();
            }

            m_DispatchQueue.Clear();
        }

        /// <summary>
        /// Dispatches queued events when no SynchronizationContext is available.
        /// Not needed when a SynchronizationContext is present (Unity, Godot, MonoGame with WebSocketGameComponent).
        /// </summary>
        public void DispatchMessageQueue()
        {
            // Hot path: dispatch messages without closure overhead
            DispatchQueuedMessages();

            // Rare events: OnOpen, OnError, OnClose
            DispatchQueuedEvents();
        }

        public void CancelConnection()
        {
            m_TokenSource?.Cancel();
        }

        public async Task Connect()
        {
            try
            {
                m_TokenSource = new CancellationTokenSource();
                m_CancellationToken = m_TokenSource.Token;

                m_Socket = new ClientWebSocket();

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        m_Socket.Options.SetRequestHeader(header.Key, header.Value);
                    }
                }

                if (subprotocols != null)
                {
                    foreach (string subprotocol in subprotocols)
                    {
                        m_Socket.Options.AddSubProtocol(subprotocol);
                    }
                }
                m_Socket.Options.RemoteCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) =>
                {
                    Console.WriteLine("RemoteCertificateValidationCallback");
                    return true;
                };
                await m_Socket.ConnectAsync(uri, m_CancellationToken).ConfigureAwait(false);

                EnqueueEvent(() => OnOpen?.Invoke());

                await Receive().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                EnqueueEvent(() => OnError?.Invoke(ex.Message));
                EnqueueEvent(() => OnClose?.Invoke(WebSocketCloseCode.Abnormal));
            }
            finally
            {
                if (m_Socket != null)
                {
                    m_TokenSource.Cancel();
                    m_Socket.Dispose();
                }
            }
        }

        public WebSocketState State
        {
            get
            {
                if (m_Socket == null)
                {
                    return WebSocketState.Closed;
                }

                switch (m_Socket.State)
                {
                    case System.Net.WebSockets.WebSocketState.Connecting:
                        return WebSocketState.Connecting;

                    case System.Net.WebSockets.WebSocketState.Open:
                        return WebSocketState.Open;

                    case System.Net.WebSockets.WebSocketState.CloseSent:
                    case System.Net.WebSockets.WebSocketState.CloseReceived:
                        return WebSocketState.Closing;

                    case System.Net.WebSockets.WebSocketState.Closed:
                        return WebSocketState.Closed;

                    default:
                        return WebSocketState.Closed;
                }
            }
        }

        public Task Send(ArraySegment<byte> segment)
        {
            var mem = AllocAutoReleaseBuffer().Init(segment);

            lock (OutgoingMessageLock)
            {
                if (isSending)
                {
                    sendBytesQueue.Enqueue(mem);
                    return Task.CompletedTask;
                }
                isSending = true;
            }

            return SendAndDrainAsync(sendBytesQueue, WebSocketMessageType.Binary, mem);
        }
        public Task Send(byte[] bytes)
        {
            if (bytes.Length == 0 || State != WebSocketState.Open)
                return Task.CompletedTask;

            //var segment = new ArraySegment<byte>(bytes);
            var mem = AllocAutoReleaseBuffer().Init(bytes);

            lock (OutgoingMessageLock)
            {
                if (isSending)
                {
                    //sendBytesQueue.Enqueue(segment);
                    sendBytesQueue.Enqueue(mem);
                    return Task.CompletedTask;
                }
                isSending = true;
            }

            return SendAndDrainAsync(sendBytesQueue, WebSocketMessageType.Binary, mem);
        }

        public Task SendText(string message)
        {
            if (State != WebSocketState.Open)
                return Task.CompletedTask;

            var encoded = Encoding.UTF8.GetBytes(message);
            if (encoded.Length == 0)
                return Task.CompletedTask;

            //var segment = new ArraySegment<byte>(encoded, 0, encoded.Length);
            var mem = AllocAutoReleaseBuffer().Init(encoded);

            lock (OutgoingMessageLock)
            {
                if (isSending)
                {
                    sendTextQueue.Enqueue(mem);
                    return Task.CompletedTask;
                }
                isSending = true;
            }

            return SendAndDrainAsync(sendTextQueue, WebSocketMessageType.Text, mem);
        }

#if NET6_0_OR_GREATER
        private Task SendAndDrainAsync(Queue<ArraySegment<byte>> queue, WebSocketMessageType messageType, ArraySegment<byte> buffer)
        {
            var sendTask = SendSocketAsync(buffer, messageType);
            if (sendTask.IsCompletedSuccessfully)
            {
                return DrainQueueSync(queue, messageType);
            }

            return AwaitAndDrainAsync(sendTask, queue, messageType);
        }

        private Task DrainQueueSync(Queue<ArraySegment<byte>> queue, WebSocketMessageType messageType)
        {
            while (true)
            {
                ArraySegment<byte> next;
                lock (OutgoingMessageLock)
                {
                    if (queue.Count == 0)
                    {
                        isSending = false;
                        return Task.CompletedTask;
                    }

                    next = queue.Dequeue();
                }

                var sendTask = SendSocketAsync(next, messageType);
                if (!sendTask.IsCompletedSuccessfully)
                {
                    return AwaitAndDrainAsync(sendTask, queue, messageType);
                }
            }
        }

        private async Task AwaitAndDrainAsync(ValueTask pendingSend, Queue<ArraySegment<byte>> queue, WebSocketMessageType messageType)
        {
            try
            {
                await pendingSend.ConfigureAwait(false);

                while (true)
                {
                    ArraySegment<byte> next;
                    lock (OutgoingMessageLock)
                    {
                        if (queue.Count == 0)
                            break;

                        next = queue.Dequeue();
                    }

                    await SendSocketAsync(next, messageType).ConfigureAwait(false);
                }
            }
            finally
            {
                lock (OutgoingMessageLock)
                {
                    isSending = false;
                }
            }
        }

        private ValueTask SendSocketAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType)
        {
            return m_Socket.SendAsync(
                new ReadOnlyMemory<byte>(buffer.Array, buffer.Offset, buffer.Count),
                messageType,
                true,
                m_CancellationToken);
        }
#else
        private async Task SendAndDrainAsync(Queue<MemoryStream> queue, WebSocketMessageType messageType, MemoryStream buffer)
        {
            var segment = new ArraySegment<byte>(buffer.GetBuffer(), 0, (int)buffer.Length);
            // Try synchronous fast path: avoid async state machine when SendAsync completes inline
            //             var sendTask = m_Socket.SendAsync(segment, messageType, true, m_CancellationToken);
            //             if (sendTask.Status == TaskStatus.RanToCompletion)
            //             {
            //                 buffer.Dispose();
            //                 return DrainQueueSync(queue, messageType);
            //             }
            //             return AwaitAndDrainAsync(sendTask, queue, messageType);
            try
            {
                await m_Socket.SendAsync(segment, messageType, true, m_CancellationToken);
                //if (sendTask.Status == TaskStatus.RanToCompletion)
                {
                    await DrainQueueSync(queue, messageType);
                }
                await AwaitAndDrainAsync(queue, messageType);
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private async Task DrainQueueSync(Queue<MemoryStream> queue, WebSocketMessageType messageType)
        {
            while (true)
            {
                MemoryStream next;
                lock (OutgoingMessageLock)
                {
                    if (queue.Count == 0)
                    {
                        isSending = false;
                        return;
                    }
                    next = queue.Dequeue();
                }
                try
                {
                    var nextSegment = new ArraySegment<byte>(next.GetBuffer(), 0, (int)next.Length);
                    await m_Socket.SendAsync(nextSegment, messageType, true, m_CancellationToken).ConfigureAwait(false);
                    //if (sendTask.Status != TaskStatus.RanToCompletion)
                    {
                        //await pendingSend.ConfigureAwait(false);
                        // This send went async — fall through to async drain
                        await AwaitAndDrainAsync(queue, messageType);
                    }
                }
                finally
                {
                    next.Dispose();
                }
            }
        }

        private async Task AwaitAndDrainAsync(Queue<MemoryStream> queue, WebSocketMessageType messageType)
        {
            try
            {
                while (true)
                {
                    MemoryStream next;
                    lock (OutgoingMessageLock)
                    {
                        if (queue.Count == 0)
                            break;
                        next = queue.Dequeue();
                    }
                    try
                    {
                        var nextSegment = new ArraySegment<byte>(next.GetBuffer(), 0, (int)next.Length);
                        await m_Socket.SendAsync(nextSegment, messageType, true, m_CancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        next.Dispose();
                    }
                }
            }
            finally
            {
                lock (OutgoingMessageLock)
                {
                    isSending = false;
                }
            }
        }
#endif

        public async Task Receive()
        {
            WebSocketCloseCode closeCode = WebSocketCloseCode.Abnormal;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[65536]);
            try
            {
                while (m_Socket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    var result = await m_Socket.ReceiveAsync(buffer, m_CancellationToken).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await Close().ConfigureAwait(false);
                        closeCode = WebSocketHelpers.ParseCloseCodeEnum((int)result.CloseStatus);
                        break;
                    }

                    if (result.EndOfMessage)
                    {
                        // Fast path: single-frame message, avoid MemoryStream
                        var data = new byte[result.Count];
                        Buffer.BlockCopy(buffer.Array, buffer.Offset, data, 0, result.Count);
                        EnqueueMessage(data);
                    }
                    else
                    {
                        // Multi-frame message: accumulate in MemoryStream
                        using (var ms = new MemoryStream())
                        {
                            ms.Write(buffer.Array, buffer.Offset, result.Count);

                            do
                            {
                                result = await m_Socket.ReceiveAsync(buffer, m_CancellationToken).ConfigureAwait(false);
                                ms.Write(buffer.Array, buffer.Offset, result.Count);
                            }
                            while (!result.EndOfMessage);

                            var data = ms.ToArray();
                            EnqueueMessage(data);
                        }
                    }
                }
            }
            catch (Exception)
            {
                m_TokenSource.Cancel();
            }
            finally
            {
                EnqueueEvent(() => OnClose?.Invoke(closeCode));
            }
        }

        public async Task Close(WebSocketCloseCode code = WebSocketCloseCode.Normal, string reason = null)
        {
            if (State != WebSocketState.Open)
                return;

            await m_Socket.CloseAsync((WebSocketCloseStatus)(int)code, reason ?? string.Empty, CancellationToken.None).ConfigureAwait(false);
        }
        //------------------------------------------------------------------------------------------------------------
        private Queue<AutoRelease> memPool = new Queue<AutoRelease>();
        public AutoRelease AllocAutoReleaseBuffer()
        {
            lock (memPool)
            {
                if (memPool.TryDequeue(out var buffer))
                {
                    return buffer;
                }
            }
            return new AutoRelease(this);
        }
        public class AutoRelease : System.IO.MemoryStream
        {
            private readonly WebSocket pool;
            internal AutoRelease(WebSocket pool) { this.pool = pool; }
            internal AutoRelease(WebSocket pool, byte[] buffer) : base(buffer) { this.pool = pool; }
            internal AutoRelease(WebSocket pool, byte[] buffer, int index, int count) : base(buffer, index, count) { this.pool = pool; }
            internal AutoRelease(WebSocket pool, int capacity) : base(capacity) { this.pool = pool; }
            public AutoRelease Init(byte[] buffer)
            {
                var ret = this;
                ret.Capacity = Math.Max(buffer.Length, ret.Capacity);
                ret.SetLength(buffer.Length);
                Buffer.BlockCopy(buffer, 0, ret.GetBuffer(), 0, buffer.Length);
                ret.Position = 0;
                return ret;
            }
            public AutoRelease Init(byte[] buffer, int offset, int length)
            {
                var ret = this;
                ret.Capacity = Math.Max(buffer.Length, ret.Capacity);
                ret.SetLength(length);
                Buffer.BlockCopy(buffer, offset, ret.GetBuffer(), 0, length);
                ret.Position = 0;
                return ret;
            }
            public AutoRelease Init(ArraySegment<byte> buffer)
            {
                var ret = this;
                ret.Capacity = Math.Max(buffer.Count, ret.Capacity);
                ret.SetLength(buffer.Count);
                Buffer.BlockCopy(buffer.Array, buffer.Offset, ret.GetBuffer(), 0, buffer.Count);
                ret.Position = 0;
                return ret;
            }
            protected override void Dispose(bool disposing)
            {
                this.Position = 0;
                this.SetLength(0);
                lock (pool.memPool)
                {
                    pool.memPool.Enqueue(this);
                }
            }
        }
        //------------------------------------------------------------------------------------------------------------
    }

    /// <summary>
    /// Factory for creating WebSocket instances.
    /// </summary>
    public static class WebSocketFactory
    {
        /// <summary>
        /// Create WebSocket client instance
        /// </summary>
        /// <returns>The WebSocket instance.</returns>
        /// <param name="url">WebSocket valid URL.</param>
        public static WebSocket CreateInstance(string url)
        {
            return new WebSocket(url);
        }
    }

}

#endif
