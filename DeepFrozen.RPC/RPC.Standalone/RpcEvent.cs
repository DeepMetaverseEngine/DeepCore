using DeepCore;
using DeepCore.IO;
using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCrystal.RpcTest
{
    internal enum RpcEvent
    {
        NA,
        START,
        REQUEST_OBJ,
        RESPONSE_OBJ,
        REQUEST_BIN,
        RESPONSE_BIN,
        NOTIFY,
        DESTORY,
        EXECUTE,
        CALLBACK,
    }

    internal class RpcMessage
    {
        public RpcEvent rpc_event { get; private set; }
        public RemoteAddress from { get; private set; }
        public RemoteAddress to { get; private set; }
        public bool AutoRelease { get; private set; }

        public BinaryMessage bin;
        public ISerializable obj;
        public OnRpcBinaryReturn callback_bin;
        public OnRpcReturn<ISerializable> callback_obj;
        private List<Exception> err_stack;

        public object state;
        public Action<object, Exception> callback;

        public string destroy_reason;
        public bool callback_async = false;

        public override string ToString()
        {
            switch (rpc_event)
            {
                case RpcEvent.START:
                case RpcEvent.DESTORY:
                case RpcEvent.CALLBACK:
                case RpcEvent.EXECUTE:
                    return string.Format("{0} : {1} -> {2}", rpc_event, AddressName(from), AddressName(to));
                case RpcEvent.REQUEST_BIN:
                case RpcEvent.REQUEST_OBJ:
                case RpcEvent.RESPONSE_BIN:
                case RpcEvent.RESPONSE_OBJ:
                case RpcEvent.NOTIFY:
                    return string.Format("{0} : {1} -> {2} : {3}", rpc_event, AddressName(from), AddressName(to), 
                        (obj != null ? obj.ToString() : bin.Route.ToString()));
            }
            return rpc_event.ToString();
        }

        private static string AddressName(RemoteAddress addr)
        {
            if (addr == null) return "";
            return addr.ServiceName;
        }

        public void AppendError(RpcMessage msg)
        {
            if (msg.err_stack != null)
            {
                if (this.err_stack == null) this.err_stack = new List<Exception>();
                this.err_stack.AddRange(msg.err_stack);
            }
        }
        public void AppendError(Exception err)
        {
            if (err != null)
            {
                if (this.err_stack == null) this.err_stack = new List<Exception>();
                this.err_stack.Add(err);
            }
        }
        public Exception Error
        {
            get
            {
                if (err_stack != null)
                {
                    return new RpcException(err_stack);
                }
                return null;
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------
        private static ObjectPool<RpcMessage> s_SendPool = new ObjectPool<RpcMessage>(() => { return new RpcMessage(); }, OnGet, OnRelease);
        public static RpcMessage AllocAutoRelease(RpcEvent evt, RemoteAddress from, RemoteAddress to)
        {
            var ret = s_SendPool.Get();
            ret.AutoRelease = true;
            ret.rpc_event = evt;
            ret.from = from;
            ret.to = to;
            return ret;
        }
        public static RpcMessage AllocRetain(RpcEvent evt, RemoteAddress from, RemoteAddress to)
        {
            var ret = new RpcMessage();
            ret.AutoRelease = false;
            ret.rpc_event = evt;
            ret.from = from;
            ret.to = to;
            return ret;
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        private static void OnGet(RpcMessage msg)
        {
        }
        private static void OnRelease(RpcMessage msg)
        {
            msg.rpc_event = RpcEvent.NA;
            msg.from = null;
            msg.to = null;
            msg.bin = BinaryMessage.NULL;
            msg.obj = null;
            msg.err_stack = null;
            msg.callback_bin = null;
            msg.callback_obj = null;
            msg.state = null;
            msg.callback = null;
            msg.destroy_reason = null;
            msg.callback_async = false;
        }
        private RpcMessage()
        {
        }
        public void Dispose()
        {
            if (AutoRelease) s_SendPool.Release(this);
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
    }

    internal class RpcException : Exception
    {
        private List<Exception> stack;
        public RpcException(List<Exception> errlist) : base(errlist[errlist.Count - 1].Message)
        {
            this.stack = errlist;
        }
        public override string StackTrace
        {
            get
            {
                using (var sb = StringBuilderObjectPool.AllocAutoRelease())
                {
                    for (int i = stack.Count - 1; i >= 0; --i)
                    {
                        sb.WriteLine(stack[i].StackTrace);
                        if (i != 0)
                        {
                            sb.WriteLine("----------------------------------");
                        }
                    }
                    return sb.ToString();
                }
            }
        }
    }
}
