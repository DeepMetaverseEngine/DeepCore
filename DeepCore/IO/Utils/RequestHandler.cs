using DeepCore.Concurrent;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.IO.Utils
{

    public class RequestListener<T>
    {
        protected readonly Log.Logger log;
        private HashMap<int, RequestHandler> response_map = new HashMap<int, RequestHandler>();
        private readonly AtomicInteger req_id_gen = new AtomicInteger(1);
        private readonly int timeoutMS;

        public RequestListener(int requestTimeoutMS = 10000)
        {
            this.log = Log.LoggerFactory.GetLogger(GetType().Name);
            this.timeoutMS = requestTimeoutMS;
        }
        public void CheckRequestTimeout()
        {
            List<RequestHandler> removing = null;
            {
                var cur_time = CUtils.TickTimeMS;
                lock (response_map)
                {
                    if (response_map.Count > 0)
                    {
                        foreach (var req in response_map.Values)
                        {
                            if (req.CheckTimeout(timeoutMS, cur_time))
                            {
                                if (removing == null)
                                {
                                    removing = new();
                                }
                                removing.Add(req);
                            }
                        }
                        if (removing != null && removing.Count > 0)
                        {
                            foreach (var r in removing)
                            {
                                response_map.Remove(r.SendID);
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                if (removing != null && removing.Count > 0)
                {
                    foreach (var r in removing)
                    {
                        r.Invoke(default(T), new RequestTimeoutException(r));
                    }
                }
            }
        }
        public void Dispose()
        {
            List<RequestHandler> cbs = null;
            {
                lock (response_map)
                {
                    if (response_map.Count > 0)
                    {
                        if (cbs == null) cbs = new();
                        cbs.AddRange(response_map.Values);
                        response_map.Clear();
                    }
                    else
                    {
                        return;
                    }
                }
                if (cbs != null)
                {
                    foreach (var cb in cbs)
                    {
                        cb.Invoke(default(T), new RequestInterruptedException(cb));
                    }
                }
            }
        }
        public bool Listen(Action<T, Exception> cb, out int sendID)
        {
            if (cb != null)
            {
                try
                {
                    sendID = req_id_gen.GetAndIncrement();
                    lock (response_map)
                    {
                        this.response_map.Add(sendID, new RequestHandler(this, sendID, cb));
                        return true;
                    }
                }
                catch { }
            }
            sendID = 0;
            return false;
        }
        public bool OnHandleResponse(int sendID, T response, Exception err)
        {
            RequestHandler cb;
            lock (response_map)
            {
                if (!response_map.TryGetValue(sendID, out cb))
                {
                    return false;
                }
                response_map.Remove(sendID);
            }
            try
            {
                cb.Invoke(response, err);
            }
            catch (Exception cerror)
            {
                log.Error(cerror.Message, cerror);
            }
            return true;
        }

        public class RequestTimeoutException : Exception
        {
            public RequestHandler Request { get; private set; }
            public RequestTimeoutException(RequestHandler req)
                : base(string.Format("{0} : {1}", nameof(RequestTimeoutException), req.SendID))
            {
                this.Request = req;
            }
        }
        public class RequestInterruptedException : Exception
        {
            public RequestHandler Request { get; private set; }
            public RequestInterruptedException(RequestHandler req)
                : base(string.Format("{0} : {1}", nameof(RequestInterruptedException), req.SendID))
            {
                this.Request = req;
            }
        }
        public class RequestHandler
        {
            public RequestListener<T> Owner { get; private set; }
            public int SendID { get; private set; }
            public double StartTimeMS { get; private set; }
            private readonly Action<T, Exception> callback;

            internal RequestHandler(RequestListener<T> owner, int send_id, Action<T, Exception> callback)
            {
                this.Owner = owner;
                this.SendID = send_id;
                this.callback = callback;
                this.StartTimeMS = CUtils.TickTimeMS;
            }
            internal void Invoke(T rsp, Exception err)
            {
                callback(rsp, err);
            }
            internal bool CheckTimeout(int timeout_ms, double current_time_ms)
            {
                if (StartTimeMS + timeout_ms < current_time_ms)
                {
                    return true;
                }
                return false;
            }
        }
    }
}
