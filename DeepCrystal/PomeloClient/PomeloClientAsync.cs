using DeepCore.IO;
using DeepCore.NetClient;
using DeepCore.Threading;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DeepCore.PomeloClient
{
    public class PomeloClientAsync : INetClient
    {
        private readonly ActionBlock<Action> taskqueue;
        private readonly TaskCompletionSourcePool tcsPool;
        public PomeloClientAsync(IExternalizableFactory codec, string name = null, int request_timer_tick_ms = 5000, TaskCompletionSourcePool tpool = null)
            : base(codec, name, request_timer_tick_ms)
        {
            this.taskqueue = new ActionBlock<Action>(new Action<Action>(act=> act()));
            if (tpool == null) tpool = new TaskCompletionSourcePool("TCS:" + name, CollectionPool.Shared);
            this.tcsPool = tpool;
            this.tcsPool.CreateTimer(TimeSpan.FromMilliseconds(request_timer_tick_ms), TimerTick, this);
        }
        private void TimerTick(object st)
        {
            taskqueue.Post(this.Update);
        }
        public override void Update()
        {
            tcsPool.Update();
            base.Update();
        }
        protected override void Disposing()
        {
            tcsPool.Dispose();
            taskqueue.Complete();
            taskqueue.Completion.Wait();
            base.Disposing();
        }
        protected override void net_process_message(IRecvMessage msg)
        {
            base.net_process_message(msg); 
            taskqueue.Post(this.Update);
        }
        public virtual Task<RSP> ConnectAsync<RSP>(string address, TimeSpan timeout, ISerializable user = null) where RSP : ISerializable
        {
            string[] kvs = address.Split(':');
            return ConnectAsync<RSP>(kvs[0], Parser.ParseInt(kvs[1]), timeout, user);
        }
        public virtual Task<RSP> ConnectAsync<RSP>(string host, int port, TimeSpan timeout, ISerializable user = null) where RSP : ISerializable
        {
            var tcs = tcsPool.CreateTaskCompletionSource<RSP>(string.Format("ConnectAsync:{0}:{1}", host, port), new StackTrace(),
                TaskCreationOptions.AttachedToParent,
                TimeSpan.FromMilliseconds(this.RequestTimerTickMS + timeout.TotalMilliseconds));
            this.Connect(host, port, timeout, user, callback);
            void callback(Exception err, ISerializable rsp)
            {
                if (err != null)
                {
                    tcs.TrySetException(err);
                }
                else
                {
                    tcs.TrySetResult((RSP)rsp);
                }
            }
            return tcs.Task;
        }
        protected override TaskCompletionSource<T> CreateTCS<T>(string msg, bool infinity)
        {
            if (infinity)
            {
                var tcs = tcsPool.CreateTaskCompletionSource<T>(msg, new StackTrace(), TaskCreationOptions.AttachedToParent, Timeout.InfiniteTimeSpan);
                return tcs;

            }
            else
            {
                var timeout = TimeSpan.FromMilliseconds(this.RequestTimeout.TotalMilliseconds + this.RequestTimerTickMS);
                var tcs = tcsPool.CreateTaskCompletionSource<T>(msg, new StackTrace(), TaskCreationOptions.AttachedToParent, timeout);
                return tcs;
            }
        }
// 
//         public override Task<BinaryMessage> RequestBinaryAsync(BinaryMessage req)
//         {
//         
//             this.RequestBinary(req, callback);
//             void callback(PomeloException err, BinaryMessage rsp)
//             {
//                 if (err != null)
//                 {
//                     tcs.TrySetException(err);
//                 }
//                 else
//                 {
//                     tcs.TrySetResult(rsp);
//                 }
//             }
//             return tcs.Task;
//         }
//         public override Task<ISerializable> RequestAsync(ISerializable req, object state = null)
//         {
//             var timeout = TimeSpan.FromMilliseconds(this.RequestTimeoutMS + this.RequestTimerTickMS);
//             var tcs = tcsPool.CreateTaskCompletionSource<ISerializable>(string.Format("RequestAsync:{0}", req.GetType().Name), new StackTrace(),
//                 TaskCreationOptions.AttachedToParent, timeout);
//             this.Request(req, callback, state);
//             void callback(PomeloException err, ISerializable rsp)
//             {
//                 if (err != null)
//                 {
//                     tcs.TrySetException(err);
//                 }
//                 else
//                 {
//                     tcs.TrySetResult(rsp);
//                 }
//             }
//             return tcs.Task;
//         }
//         public override Task<RSP> RequestAsync<RSP>(ISerializable req, object state = null)
//         {
//             var timeout = TimeSpan.FromMilliseconds(this.RequestTimeoutMS + this.RequestTimerTickMS);
//             var tcs = tcsPool.CreateTaskCompletionSource<RSP>(string.Format("RequestAsync<{1}>:{0}", req.GetType().Name, typeof(RSP).Name), new StackTrace(),
//                 TaskCreationOptions.AttachedToParent, timeout);
//             this.Request<RSP>(req, callback, state);
//             void callback(PomeloException err, RSP rsp)
//             {
//                 if (err != null)
//                 {
//                     tcs.TrySetException(err);
//                 }
//                 else
//                 {
//                     tcs.TrySetResult(rsp);
//                 }
//             }
//             return tcs.Task;
//         }

        public IRpcRequestHandler HandleRpcAsync<REQ, RSP>(Func<REQ, Task<RSP>> handle) where REQ : ISerializable where RSP : ISerializable
        {
            return base.HandleRpcRequest((req, cb) =>
            {
                if (req is REQ)
                {
                    handle.Invoke((REQ)req).ContinueWith(t =>
                    {
                        cb(t.GetResultAs());
                    });
                    return true;
                }
                return false;
            });
        }
    }

    //     public class SinglePomeloClientAsync : PomeloClientAsync
    //     {
    //         private readonly Timer update_timer;
    //         public SinglePomeloClientAsync(IExternalizableFactory codec, string name = null, int request_timer_tick_ms = 1000) : base(codec, name, request_timer_tick_ms)
    //         {
    //             this.update_timer = new Timer(timer_tick, this, request_timer_tick_ms, request_timer_tick_ms);
    //         }
    //         protected override void Disposing()
    //         {
    //             this.update_timer.Dispose();
    //             base.Disposing();
    //         }
    //         private void timer_tick(object state)
    //         {
    //             Update();
    //         }
    //     }
}
