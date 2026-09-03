using DeepCore;
using DeepCore.IO;
using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Remote.ServiceNode
{
    internal enum RpcEvent
    {
        NA,
        REQUEST_START,

        REQUEST_OBJ,
        RESPONSE_OBJ,

        REQUEST_BIN,
        RESPONSE_BIN,

        RESPONSE_VOID,

        REQUEST_NOTIFY_OBJ,
        REQUEST_NOTIFY_BIN,
        REQUEST_BATCH_NOTIFY_OBJ,
        REQUEST_BATCH_NOTIFY_BIN,

        REQUEST_DESTORY,
        RESPONES_EXECUTE,
        RESPONES_CALLBACK,
    }

    internal class RpcMessage : IDisposable
    {
        public bool AutoRelease { get; private set; }
        public RpcEvent Event { get; private set; }
        public RemoteAddress From { get; private set; }
        public RemoteAddress To { get; private set; }
        public DateTime StartTime { get; private set; }
        public object State { get; set; }

        private Action<object, Exception> callback;
        private RpcEventException err_stack;

        private RpcMessage() { }
        public override string ToString()
        {
            return string.Format("{0} : From={1} To={2} State={3}", Event, From.ServiceName, To.ServiceName, State);
        }
        public void AppendError(Exception err)
        {
            if (err != null)
            {
                if (this.err_stack == null)
                {
                    this.err_stack = new RpcEventException(this, err);
                }
                else
                {
                    this.err_stack.AppendException(err);
                }
            }
        }
        public Exception Error
        {
            get { return err_stack; }
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        private static ObjectPool<RpcMessage> s_SendPool = new ConcurrentObjectPool<RpcMessage>();

        public static RpcMessage AllocAutoRelease(RpcEvent evt, RemoteAddress from, RemoteAddress to)
        {
            var ret = s_SendPool.Get(0, static (t, p) => new RpcMessage());
            ret.AutoRelease = true;
            ret.Event = evt;
            ret.From = from;
            ret.To = to;
            return ret;
        }
        public static RpcMessage AllocRetain(RpcEvent evt, RemoteAddress from, RemoteAddress to)
        {
            var ret = new RpcMessage();
            ret.AutoRelease = false;
            ret.Event = evt;
            ret.From = from;
            ret.To = to;
            return ret;
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        public void Dispose()
        {
            this.Event = RpcEvent.NA;
            this.From = RemoteAddress.NULL;
            this.To = RemoteAddress.NULL;
            this.StartTime = DateTime.MinValue;
            this.State = null;
            this.err_stack = null;
            this.callback = null;
            if (AutoRelease) s_SendPool.Release(this);
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        public Action<object, Exception> GetCallback()
        {
            return callback;
        }
        public void SetCallback(Action<object, Exception> callback)
        {
            this.callback = callback;
        }
        public void SetCallbackRsp(OnRpcReturnBinary callback)
        {
            this.callback = cb_Bin;
            void cb_Bin(object rsp, Exception err)
            {
                if (err != null)
                {
                    callback(BinaryMessage.NULL, err);
                }
                else
                {
                    callback((BinaryMessage)rsp, err);
                }
            }
        }
        public void SetCallbackRsp<RSP>(OnRpcReturn<RSP> callback) where RSP : ISerializable
        {
            this.callback = cb_Rsp;
            void cb_Rsp(object rsp, Exception err)
            {
                var bin_rsp = default(RSP);
                if (err != null)
                {
                    callback(bin_rsp, err);
                }
                else
                {
                    try
                    {
                        bin_rsp = (RSP)rsp;
                    }
                    catch (Exception err2)
                    {
                        callback(bin_rsp, err2);
                        return;
                    }
                    callback(bin_rsp, err);
                }
            }
        }
        public void SetCallbackRsp(OnRpcReturnVoid callback)
        {
            this.callback = cb_Rsp;
            void cb_Rsp(object rsp, Exception err)
            {
                callback(err);
            }
        }
        public void SetCallbackTcs(TaskCompletionSource<BinaryMessage> taskCompletion)
        {
            this.callback = cb_Call;
            void cb_Call(object rsp, Exception err)
            {
                if (err != null)
                {
                    taskCompletion.TrySetException(err);
                }
                else
                {
                    taskCompletion.TrySetResult((BinaryMessage)rsp);
                }
            }
        }
        public void SetCallbackTcs<RSP>(TaskCompletionSource<RSP> taskCompletion)
        {
            this.callback = cb_Call;
            void cb_Call(object rsp, Exception err)
            {
                if (err != null)
                {
                    taskCompletion.TrySetException(err);
                }
                else
                {
                    var bin_rsp = default(RSP);
                    try
                    {
                        bin_rsp = (RSP)rsp;// (RSP)Convert.ChangeType(rsp, typeof(RSP));
                    }
                    catch
                    {

                    }
                    taskCompletion.TrySetResult(bin_rsp);
                }
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        public void Invoke(object obj, Exception err)
        {
            callback(obj, err);
        }
        public void InvokeError(Exception err)
        {
            callback?.Invoke(null, err);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------
        class RpcEventException : Exception
        {
            private List<Exception> appendStack = new List<Exception>(1);

            public RpcEventException(RpcMessage msg, Exception err)
                : base($"RPC RpcException : From={msg.From} Event={msg.Event}")
            {
                appendStack.Add(err);
            }
            public void AppendException(Exception err)
            {
                appendStack.Add(err);
            }
            public override string StackTrace
            {
                get
                {
                    var sb = new StringBuilder();
                    if (!string.IsNullOrWhiteSpace(base.StackTrace))
                    {
                        sb.AppendLine(base.StackTrace.TrimEnd());
                    }
                    foreach (var err in appendStack)
                    {
                        AppendTrace(sb, err);
                    }
                    return sb.ToString();
                }
            }
            private void AppendTrace(StringBuilder sb, Exception err)
            {
                sb.AppendLine("Inner: " + err.Message + err.StackTrim());
                if (err.InnerException != null)
                {
                    AppendTrace(sb, err.InnerException);
                }
            }

        }
    }



}
