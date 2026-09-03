using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.IO.Utils
{
    public class NotifyInvokers
    {
        private HashMap<int, List<NotifyHandler>> push_handler = new HashMap<int, List<NotifyHandler>>();
        private IOStreamPool codec;

        public NotifyInvokers(IOStreamPool codec)
        {
            this.codec = codec;
        }
        protected virtual NotifyHandler CreatePushHandler(Type route_type, int route_id, Action<ISerializable> cb, Action<BinaryMessage> cbb, bool recursion_base_type)
        {
            return new NotifyHandler(this, route_type, route_id, cb, cbb, recursion_base_type);
        }
        public NotifyHandler ListenPush(Type route_type, int route_id, Action<ISerializable> cb, Action<BinaryMessage> cbb, bool recursion_base_type)
        {
            if (route_type != null)
            {
                route_id = codec.Factory.GetTypeID(route_type);
            }
            else if (route_id != IOStream.INVALID_MESSAGE_CODE)
            {
                route_type = codec.Factory.GetType(route_id);
            }
            var ret = this.CreatePushHandler(route_type, route_id, cb, cbb, recursion_base_type);
            lock (push_handler)
            {
                var act = push_handler.GetOrAdd(route_id, static (id) => { return new List<NotifyHandler>(); });
                act.Add(ret);
                if (recursion_base_type && route_type != null)
                {
                    var sub_types = new List<TypeCodec>();
                    {
                        IOUtil.GetAllSubTypes(codec.Factory, route_type, sub_types);
                        foreach (var sub_codec in sub_types)
                        {
                            var sub_act = push_handler.Get(sub_codec.MessageID);
                            if (sub_act == null)
                            {
                                sub_act = new List<NotifyHandler>();
                                push_handler.Put(sub_codec.MessageID, sub_act);
                            }
                            sub_act.Add(ret);
                        }
                    }
                }
            }
            return ret;
        }
        public void Notify(ISerializable msg)
        {
            if (msg != null)
            {
                TypeCodec codec = this.codec.Factory.GetCodec(msg.GetType());
                if (codec != null)
                {
                    process_push(msg, codec);
                }
            }
        }
        public void Notify(BinaryMessage msg)
        {
            if (!msg.IsNoRoute)
            {
                TypeCodec codec = this.codec.Factory.GetCodec(msg.Route);
                if (codec != null)
                {
                    process_push(msg, codec);
                }
            }
        }
        public void ClearPush()
        {
            lock (push_handler)
            {
                foreach (var list in push_handler.Values)
                {
                    foreach (var e in list)
                    {
                        e.Dispose();
                    }
                }
                push_handler.Clear();
            }
        }
        private void process_push(object msg, TypeCodec codec)
        {
            List<NotifyHandler> all = null;
            {
                lock (push_handler)
                {
                    var list = push_handler.Get(IOStream.INVALID_MESSAGE_CODE);
                    if (list != null)
                    {
                        if (all == null) all = new();
                        all.AddRange(list);
                    }
                    list = push_handler.Get(codec.MessageID);
                    if (list != null)
                    {
                        if (all == null) all = new();
                        all.AddRange(list);
                    }
                }
                if (all != null)
                {
                    for (int i = 0; i < all.Count; i++)
                    {
                        var handler = all[i];
                        if (handler.IsBinary)
                        {
                            if (msg is ISerializable)
                            {
                                handler.InvokeBin(this.codec.EncodeBinary(msg, codec));
                            }
                            else if (msg is BinaryMessage)
                            {
                                handler.InvokeBin((BinaryMessage)msg);
                            }
                        }
                        else
                        {
                            if (msg is BinaryMessage)
                            {
                                handler.Invoke((ISerializable)this.codec.DecodeBinary((BinaryMessage)msg));
                            }
                            else if (msg is ISerializable)
                            {
                                handler.Invoke((ISerializable)msg);
                            }
                        }
                    }
                }
            }
        }
        private void remove_push(NotifyHandler handler)
        {
            lock (push_handler)
            {
                var act = push_handler.Get(handler.route_id);
                if (act != null)
                {
                    act.Remove(handler);
                }
                if (handler.IsRecursion && handler.route_type != null)
                {
                    var sub_types = new List<TypeCodec>();
                    {
                        IOUtil.GetAllSubTypes(codec.Factory, handler.route_type, sub_types);
                        foreach (var sub_codec in sub_types)
                        {
                            var sub_act = push_handler.Get(sub_codec.MessageID);
                            if (sub_act != null)
                            {
                                sub_act.Remove(handler);
                            }
                        }
                    }
                }
            }
        }

        public class NotifyHandler
        {
            public bool IsDisposed { get { return is_disposed; } }
            public bool IsRecursion { get { return recursion; } }
            public Type RouteType { get { return route_type; } }
            public int RouteID { get { return route_id; } }
            public bool IsBinary { get { return callback_bin != null; } }

            internal readonly NotifyInvokers client;
            internal readonly int route_id;
            internal readonly Type route_type;
            internal readonly bool recursion;
            private bool is_disposed;
            private Action<ISerializable> callback;
            private Action<BinaryMessage> callback_bin;

            public NotifyHandler(NotifyInvokers client, Type route_type, int route_id, Action<ISerializable> cb, Action<BinaryMessage> cbb, bool recursion)
            {
                this.client = client;
                this.route_type = route_type;
                this.route_id = route_id;
                this.recursion = recursion;
                this.is_disposed = false;
                this.callback = cb;
                this.callback_bin = cbb;
            }
            internal void Invoke(ISerializable data)
            {
                if (!is_disposed) callback(data);
            }
            internal void InvokeBin(BinaryMessage data)
            {
                if (!is_disposed) callback_bin(data);
            }
            public void Dispose()
            {
                client.remove_push(this);
                this.is_disposed = true;
                this.callback = null;
                this.callback_bin = null;
            }
        }

    }
}
