using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using DeepCrystal;
using System.Linq;
using DeepCore.Statistics;
using DeepCore.Threading;
using DeepCrystal.RPC;
using static DeepCore.Colors;

namespace DeepFrozen.RPC.Invoker
{

    //---------------------------------------------------------------------------------------------------------------------

    public class RpcInvokerManager
    {
        private readonly IOStreamPool codec;
        private readonly HashMap<Type, RpcServiceInvoker> invokers = new HashMap<Type, RpcServiceInvoker>();

        public RpcInvokerManager(IOStreamPool codec)
        {
            this.codec = codec;
        }

        public RpcServiceInvoker GetServiceInvoker(Type serviceType)
        {
            if (invokers.TryGetValue(serviceType, out var ret))
            {
                return ret;
            }
            lock (invokers)
            {
                if (!invokers.TryGetValue(serviceType, out ret))
                {
                    ret = new RpcServiceInvoker(serviceType, codec);
                    invokers.Add(serviceType, ret);
                }
            }
            return ret;
        }

    }
    public class NoHandlerException : Exception
    {
        public NoHandlerException(string message) : base(message)
        {
        }
        public NoHandlerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    //---------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 扫描Service所有的RpcHandler
    /// </summary>
    public class RpcServiceInvoker
    {
        public static TimeStatisticsRecoder Statistics { get; private set; } = new TimeStatisticsRecoder("RpcServiceInvoker");
        private readonly IOStreamPool iostream;
        private HashMap<int, RpcInvoker> rpc_call_map_is = new HashMap<int, RpcInvoker>();
        private HashMap<Type, RpcInvoker> rpc_call_map_ts = new HashMap<Type, RpcInvoker>();
        private HashMap<Type, RpcInvoker> local_call_map = new HashMap<Type, RpcInvoker>();
        private RpcInvoker rpc_any_call_all;
        private HashMap<int, WormholeInvoker> wormhole_call_map_is = new HashMap<int, WormholeInvoker>();
        private HashMap<Type, WormholeInvoker> wormhole_call_map_ts = new HashMap<Type, WormholeInvoker>();

        public RpcServiceInvoker(Type serviceType, IOStreamPool iostream)
        {
            this.iostream = iostream;
            var methods = serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                try
                {
                    if (PropertyUtil.TryGetAttribute<RpcHandlerAttribute>(method, out var attr_msg))
                    {
                        if (ValidateMethod(attr_msg, method, out var type_route, out var type_return))
                        {
                            var invoker = new RpcInvoker(iostream, attr_msg, method, type_route, type_return);
                            if (invoker.codec_route != null)
                            {
                                rpc_call_map_is.Add(invoker.codec_route.MessageID, invoker);
                                rpc_call_map_ts.Add(invoker.codec_route.MessageType, invoker);
                            }
                            else if (invoker.IsHandleAny)
                            {
                                if (rpc_any_call_all == null)
                                {
                                    rpc_any_call_all = (invoker);
                                }
                                else
                                {
                                    throw new Exception(string.Format("Duplicate Rpc Handle AnyType : {0} : {1}", method.DeclaringType.FullName, attr_msg.Route));
                                }
                            }
                            else
                            {
                                local_call_map.Add(attr_msg.Route, invoker);
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                        }
                    }
                    else if (PropertyUtil.TryGetAttribute<WormholeHandlerAttribute>(method, out var attr_wormhole))
                    {
                        if (ValidateWormhole(attr_wormhole, method, out var type_route, out var type_return))
                        {
                            var invoker = new WormholeInvoker(iostream, attr_msg, method, type_route, type_return);
                            wormhole_call_map_is.Add(invoker.codec_route.MessageID, invoker);
                            wormhole_call_map_ts.Add(invoker.codec_route.MessageType, invoker);
                        }
                        else
                        {
                            throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                        }
                    }
                }
                catch (Exception err)
                {
                    throw new Exception(string.Format("Service Rpc Handler Error : {0}.{1} : {2}", serviceType.FullName, method.Name, err.Message), err);
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        #region RPC
        public bool RpcInvoke(in RemoteAddress from, object svc, BinaryMessage bin, Action<BinaryMessage, Exception> callback)
        {
            var watch = CUtils.TickTimeMS;
            try
            {
                var rpc_call = rpc_call_map_is.Get(bin.Route);
                if (rpc_call != null)
                {
                    rpc_call.Invoke(svc, bin, callback, in from);
                    return true;
                }
                if (rpc_any_call_all != null)
                {
                    rpc_any_call_all.Invoke(svc, bin, callback, in from);
                    return true;
                }
                return false;
            }
            catch (Exception err)
            {
                var route = bin.Route;
                var codec = iostream.Factory.GetCodec(route);
                callback(BinaryMessage.NULL, new Exception(
                    $"RPC Invoke Error : Rote={route}({codec?.MessageType.FullName}) From={from} To={svc.GetType().FullName}", err));
                return true;
            }
            finally
            {
                Statistics.LogTime($"Route={bin.Route} From={from} To={svc.GetType().Name}", CUtils.TickTimeMS - watch);
            }
        }
        public bool RpcInvoke(in RemoteAddress from, object svc, ISerializable data, Action<ISerializable, Exception> callback)
        {
            var watch = CUtils.TickTimeMS;
            try
            {
                var rpc_call = rpc_call_map_ts.Get(data.GetType());
                if (rpc_call != null)
                {
                    rpc_call.Invoke(svc, data, callback, in from);
                    return true;
                }
                var rpc_call_local = local_call_map.Get(data.GetType());
                if (rpc_call_local != null)
                {
                    rpc_call_local.Invoke(svc, data, callback, in from);
                    return true;
                }
                if (rpc_any_call_all != null)
                {
                    rpc_any_call_all.Invoke(svc, data, callback, in from);
                    return true;
                }
                return false;
            }
            catch (Exception err)
            {
                callback(null, new Exception(
                    $"RPC Invoke Error : Rote={data?.GetType().FullName} From={from} To={svc.GetType().FullName}", err));
                return true;
            }
            finally
            {
                Statistics.LogTime($"Route={data?.GetType().Name} From={from} To={svc.GetType().Name}", CUtils.TickTimeMS - watch);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------


        public class RpcInvoker
        {
            private static ArraySegment<byte> zero_bin = new ArraySegment<byte>(new byte[0]);
            public readonly IOStreamPool codec;
            public readonly TypeCodec codec_route;
            public readonly TypeCodec codec_return;
            public readonly MethodInfo method;
            public readonly DynamicMethodInvoker invokerMethod;
            public readonly bool IsAsync;
            public readonly bool IsFromArgument;
            public readonly bool IsReturnVoid;
            public readonly bool IsBinary;
            public readonly bool IsHandleAny;
            public RpcInvoker(IOStreamPool codec, RpcHandlerAttribute attr, MethodInfo method, Type type_route, Type type_return)
            {
                this.codec = codec;
                this.method = method;

                this.IsAsync = IsAsyncMethod(method);
                this.IsFromArgument = IsFromMethod(method);
                this.IsReturnVoid = (type_return == null || type_return == typeof(void));
                this.IsBinary = attr.IsBinary || type_route == typeof(BinaryMessage);
                this.IsHandleAny = attr.IsHandleAny || type_route == typeof(ISerializable);

                this.invokerMethod = DynamicMethodHelper.GetMethodInvoker(method);
                this.codec_route = IsHandleAny ? null : codec.Factory.GetCodec(type_route);
                this.codec_return = !IsReturnVoid ? codec.Factory.GetCodec(type_return) : null;
            }

            private bool TryEncodeBin(ISerializable msg, TypeCodec c, out BinaryMessage ret, out Exception err)
            {
                err = null;
                ret = BinaryMessage.NULL;
                try
                {
                    if (msg == null)
                    {
                        return false;
                    }
                    if (c == null)
                    {
                        c = codec.Factory.GetCodec(msg.GetType());
                        if (c == null) throw new Exception("Cant Find Codec : " + msg.GetType());
                    }
                    ret = codec.EncodeBinary(msg, c);
                    return true;
                }
                catch (Exception ex)
                {
                    err = ex;
                    return false;
                }
            }
            private bool TryDecodeBin(BinaryMessage bin, TypeCodec c, out ISerializable ret, out Exception err)
            {
                err = null;
                ret = null;
                try
                {
                    if (bin.IsNoRoute)
                    {
                        return false;
                    }
                    if (c == null)
                    {
                        c = codec.Factory.GetCodec(bin.Route);
                        if (c == null) throw new Exception("Cant Find Codec : " + bin.Route);
                    }
                    ret = (ISerializable)codec.DecodeBinary(bin, c);
                    return true;
                }
                catch (Exception ex)
                {
                    err = ex;
                    return false;
                }
            }

            internal void Invoke(object svc, BinaryMessage bin, Action<BinaryMessage, Exception> callback, in RemoteAddress from)
            {
                if (IsAsync)
                {
                    InvokeAsync(svc, bin, callback, from);
                }
                else
                {
                    InvokeSync(svc, bin, callback, from);
                }
            }
            internal void Invoke(object svc, ISerializable msg, Action<ISerializable, Exception> callback, in RemoteAddress from)
            {
                if (IsAsync)
                {
                    InvokeAsync(svc, msg, callback, from);
                }
                else
                {
                    InvokeSync(svc, msg, callback, from);
                }
            }

            private void InvokeSync(object svc, BinaryMessage bin, Action<BinaryMessage, Exception> callback, in RemoteAddress from)
            {
                if (IsBinary)
                {
                    if (IsReturnVoid)
                    {
                        invoker(svc, from, bin);
                    }
                    else
                    {
                        invoker(svc, from, bin, new OnRpcReturnBinary((rsp, err) => { callback(rsp, err); }));
                    }
                }
                else
                {
                    if (TryDecodeBin(bin, this.codec_route, out var msg, out var encode_err))
                    {
                        if (IsReturnVoid)
                        {
                            invoker(svc, from, msg);
                        }
                        else
                        {
                            var handler = new OnRpcReturn<ISerializable>((rsp, err) =>
                            {
                                if (err != null)
                                {
                                    callback(BinaryMessage.NULL, err);
                                }
                                else
                                {
                                    TryEncodeBin(rsp, this.codec_return, out var ret, out var ret_err);
                                    callback(ret, ret_err);
                                }
                            });
                            invoker(svc, from, msg, handler);
                        }
                    }
                    else
                    {
                        callback(BinaryMessage.NULL, encode_err);
                    }
                }
            }
            private void InvokeSync(object svc, ISerializable msg, Action<ISerializable, Exception> callback, in RemoteAddress from)
            {
                if (IsBinary)
                {
                    if (TryEncodeBin(msg, codec_route, out var bin, out var bin_err))
                    {
                        if (IsReturnVoid)
                        {
                            invoker(svc, from, bin);
                        }
                        else
                        {
                            var handler = new OnRpcReturnBinary((rsp, err) =>
                            {
                                if (err != null)
                                {
                                    callback(null, err);
                                }
                                else
                                {
                                    TryDecodeBin(rsp, codec_return, out var ret, out var ret_err);
                                    callback(ret, ret_err);
                                }
                            });
                            invoker(svc, from, bin, handler);
                        }
                    }
                    else
                    {
                        callback(null, bin_err);
                    }
                }
                else
                {
                    if (IsReturnVoid)
                    {
                        invoker(svc, from, msg);
                    }
                    else
                    {
                        invoker(svc, from, msg, new OnRpcReturn<ISerializable>((rsp, err) => { callback(rsp, err); }));
                    }
                }
            }

            private void InvokeAsync(object svc, BinaryMessage bin, Action<BinaryMessage, Exception> callback, in RemoteAddress from)
            {
                if (IsBinary)
                {
                    if (IsReturnVoid)
                    {
                        ((Task)invoker(svc, from, bin)).ContinueWith(task => ContinueWithVoid(task, callback));
                    }
                    else
                    {
                        ((Task<BinaryMessage>)invoker(svc, from, bin)).ContinueWith(task => ContinueWith(task, callback));
                    }
                }
                else
                {
                    if (TryDecodeBin(bin, this.codec_route, out var msg, out var msg_err))
                    {
                        if (IsReturnVoid)
                        {
                            ((Task)invoker(svc, from, msg)).ContinueWith(task => ContinueWithVoid(task, callback));
                        }
                        else
                        {
                            ((Task)invoker(svc, from, msg)).ContinueWith(task => ContinueWith(task, callback));
                        }
                    }
                    else
                    {
                        callback(BinaryMessage.NULL, msg_err);
                    }
                }
            }
            private void InvokeAsync(object svc, ISerializable msg, Action<ISerializable, Exception> callback, in RemoteAddress from)
            {
                if (IsBinary)
                {
                    if (TryEncodeBin(msg, codec_route, out var bin, out var bin_err))
                    {
                        if (IsReturnVoid)
                        {
                            ((Task)invoker(svc, from, bin)).ContinueWith(task => ContinueWithVoid(task, callback));
                        }
                        else
                        {
                            ((Task<BinaryMessage>)invoker(svc, from, bin)).ContinueWith(task => ContinueWith(task, callback));
                        }
                    }
                    else
                    {
                        callback(null, bin_err);
                    }
                }
                else
                {
                    if (IsReturnVoid)
                    {
                        ((Task)invoker(svc, from, msg)).ContinueWith(task => ContinueWithVoid(task, callback));
                    }
                    else
                    {
                        ((Task)invoker(svc, from, msg)).ContinueWith(task => ContinueWith(task, callback));
                    }
                }
            }

            private object invoker(object svc, in RemoteAddress from, object msg, Delegate callback = null)
            {
                if (callback != null)
                {
                    if (IsFromArgument)
                    {
                        return invokerMethod.Invoke(svc, new object[] { from, msg, callback });
                    }
                    else
                    {
                        return invokerMethod.Invoke(svc, new object[] { msg, callback });
                    }
                }
                else
                {
                    if (IsFromArgument)
                    {
                        return invokerMethod.Invoke(svc, new object[] { from, msg, });
                    }
                    else
                    {
                        return invokerMethod.Invoke(svc, new object[] { msg, });
                    }
                }
            }

            //---------------------------------------------------------------------------------------------------------------------
            private void ContinueWith(Task task, Action<BinaryMessage, Exception> callback)
            {
                if (task.Exception != null)
                {
                    callback(BinaryMessage.NULL, task.Exception);
                }
                else if (task.IsCanceled)
                {
                    callback(BinaryMessage.NULL, new Exception("Task Timeout"));
                }
                else
                {
                    BinaryMessage bin_rsp;
                    Exception bin_err;
                    try
                    {
                        dynamic r_task = task;
                        ISerializable rsp = r_task.Result;
                        TryEncodeBin(rsp, this.codec_return, out bin_rsp, out bin_err);
                    }
                    catch (Exception err)
                    {
                        callback(BinaryMessage.NULL, err);
                        return;
                    }
                    callback(bin_rsp, bin_err);
                }
            }
            private void ContinueWith(Task task, Action<ISerializable, Exception> callback)
            {
                if (task.Exception != null)
                {
                    callback(null, task.Exception);
                }
                else if (task.IsCanceled)
                {
                    callback(null, new Exception("Task Timeout"));
                }
                else
                {
                    ISerializable rsp;
                    try
                    {
                        dynamic r_task = task;
                        rsp = r_task.Result;
                    }
                    catch (Exception err)
                    {
                        callback(null, err);
                        return;
                    }
                    callback(rsp, null);
                }
            }
            private void ContinueWith(Task<BinaryMessage> task, Action<ISerializable, Exception> callback)
            {
                if (task.Exception != null)
                {
                    callback(null, task.Exception);
                }
                else if (task.IsCanceled)
                {
                    callback(null, new Exception("Task Timeout"));
                }
                else
                {
                    ISerializable rsp;
                    Exception rsp_err;
                    try
                    {
                        TryDecodeBin(task.GetResultAs(), this.codec_return, out rsp, out rsp_err);
                    }
                    catch (Exception err)
                    {
                        callback(null, err);
                        return;
                    }
                    callback(rsp, rsp_err);
                }
            }
            private void ContinueWith(Task<BinaryMessage> task, Action<BinaryMessage, Exception> callback)
            {
                if (task.Exception != null)
                {
                    callback(BinaryMessage.NULL, task.Exception);
                }
                else if (task.IsCanceled)
                {
                    callback(BinaryMessage.NULL, new Exception("Task Timeout"));
                }
                else
                {
                    callback(task.GetResultAs(), task.Exception);
                }
            }

            private void ContinueWithVoid(Task task, Action<BinaryMessage, Exception> callback)
            {
                if (task.Exception != null)
                {
                    callback(BinaryMessage.NULL, task.Exception);
                }
                else if (task.IsCanceled)
                {
                    callback(BinaryMessage.NULL, new Exception("Task Timeout"));
                }
                else
                {
                    callback(BinaryMessage.NULL, null);
                }
            }
            private void ContinueWithVoid(Task task, Action<ISerializable, Exception> callback)
            {
                if (task.Exception != null)
                {
                    callback(null, task.Exception);
                }
                else if (task.IsCanceled)
                {
                    callback(null, new Exception("Task Timeout"));
                }
                else
                {
                    callback(null, null);
                }
            }
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------------------
        #region Wormhole

        public bool WormholeInvoke(in RemoteAddress from, object svc, object data)
        {
            if (data is BinaryMessage binary)
            {
                var wormhole_call = wormhole_call_map_is.Get(binary.Route);
                if (wormhole_call != null)
                {
                    wormhole_call.Invoke(svc, binary, from);
                    return true;
                }
                return false;
            }
            else if (data is ISerializable msg)
            {
                var wormhole_call = wormhole_call_map_ts.Get(msg.GetType());
                if (wormhole_call != null)
                {
                    wormhole_call.Invoke(svc, msg, from);
                    return true;
                }
                return false;
            }
            return false;
        }
        public async Task<object> WormholeInvokeAsync(RemoteAddress from, object svc, object data)
        {
            if (data is BinaryMessage binary)
            {
                var wormhole_call = wormhole_call_map_is.Get(binary.Route);
                if (wormhole_call != null)
                {
                    var ret = await wormhole_call.InvokeAsync(svc, binary, from);
                    return ret;
                }
            }
            else if (data is ISerializable msg)
            {
                var wormhole_call = wormhole_call_map_ts.Get(msg.GetType());
                if (wormhole_call != null)
                {
                    var ret = await wormhole_call.InvokeAsync(svc, msg, from);
                    return ret;
                }
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------
        public class WormholeInvoker
        {
            public readonly IOStreamPool codec;
            public readonly TypeCodec codec_route;
            public readonly TypeCodec codec_return;
            public readonly MethodInfo method;
            public readonly DynamicMethodInvoker invokerMethod;
            public readonly bool IsAsync;
            public readonly bool IsFromArgument;
            public WormholeInvoker(IOStreamPool codec, RpcHandlerAttribute attr, MethodInfo method, Type type_route, Type type_return)
            {
                this.codec = codec;
                this.method = method;
                this.IsAsync = IsAsyncMethod(method);
                this.IsFromArgument = IsFromMethod(method);
                this.invokerMethod = DynamicMethodHelper.GetMethodInvoker(method);
                this.codec_route = codec.Factory.GetCodec(type_route);
                this.codec_return = IsAsync ? codec.Factory.GetCodec(type_return) : null;
            }
            internal void Invoke(object svc, BinaryMessage bin, in RemoteAddress from)
            {
                var msg = (ISerializable)codec.DecodeBinary(bin, codec_route);
                Invoke(svc, msg, in from);
            }
            internal void Invoke(object svc, ISerializable msg, in RemoteAddress from)
            {
                var args = IsFromArgument ? new object[] { from, msg } : new object[] { msg };
                invokerMethod.Invoke(svc, args);
            }
            internal Task<BinaryMessage> InvokeAsync(object svc, BinaryMessage bin, in RemoteAddress from)
            {
                var msg = (ISerializable)codec.DecodeBinary(bin, codec_route);
                var task = InvokeAsync(svc, msg, in from);
                return task.ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        return BinaryMessage.NULL;
                    }
                    var rsp = task.Result;
                    return codec.EncodeBinary(rsp, codec_return);
                });
            }
            internal Task<ISerializable> InvokeAsync(object svc, ISerializable msg, in RemoteAddress from)
            {
                var args = IsFromArgument ? new object[] { from, msg } : new object[] { msg };
                return ((Task)invokerMethod.Invoke(svc, args)).ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        return null;
                    }
                    ISerializable rsp;
                    dynamic r_task = task;
                    rsp = r_task.Result;
                    return rsp;
                });
            }

        }

        #endregion
        //---------------------------------------------------------------------------------------------------------------------
        #region __ValidateMethod__
        public static bool HasAttribute<TAttribute>(MethodInfo method) where TAttribute : Attribute
        {
            return method.GetCustomAttributes(typeof(TAttribute), false).Any();
        }

        public static bool IsAsyncMethod(MethodInfo method)
        {
            if (method.ReturnType == typeof(void)) return false;
            if (method.ReturnType.GUID == typeof(Task<>).GUID) return true;
            if (method.ReturnType == typeof(Task)) return true;
            return false;
        }
        public static bool IsFromMethod(MethodInfo method)
        {
            var args = method.GetParameters();
            if (args.Length == 0) return false;
            if (args[0].ParameterType == typeof(RemoteAddress)) return true;
            return false;
        }

        private static bool ValidateMethod(RpcHandlerAttribute attr, MethodInfo method, out Type type_route, out Type type_return)
        {
            var args = method.GetParameters();
            if (args.Length > 0)
            {
                if (args[0].ParameterType == typeof(RemoteAddress))
                {
                    args = CUtils.SubArray(args, 1);
                }
                if (attr.Route != null)
                {
                    type_route = attr.Route;
                    type_return = attr.Return;
                    if (attr.IsBinary)
                    {
                        if (method.ReturnType == typeof(void))
                            ValidateBinaryMethod_Sync(attr, method, args);
                        else
                            ValidateBinaryMethod_Async(attr, method, args);
                        return true;
                    }
                    else
                    {
                        if (method.ReturnType == typeof(void))
                            ValidateMethod_Sync(attr, method, args);
                        else
                            ValidateMethod_Async(attr, method, args);
                        return true;
                    }
                }
                else if (args.Length > 0)
                {
                    type_route = args[0].ParameterType;
                    if (type_route == typeof(BinaryMessage))
                    {
                        if (method.ReturnType == typeof(void))
                            ValidateBinaryMethod_Sync(out type_return, method, args);
                        else
                            ValidateBinaryMethod_Async(out type_return, method, args);
                        return true;
                    }
                    else if (typeof(ISerializable).IsAssignableFrom(type_route))
                    {
                        if (method.ReturnType == typeof(void))
                            ValidateMethod_Sync(out type_return, method, args);
                        else
                            ValidateMethod_Async(out type_return, method, args);
                        return true;
                    }
                    else
                    {
                        throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                    }
                }
            }
            type_route = null;
            type_return = null;
            return false;
        }
        private static bool ValidateWormhole(WormholeHandlerAttribute attr, MethodInfo method, out Type type_route, out Type type_return)
        {
            var args = method.GetParameters();
            if (args.Length > 0)
            {
                if (args[0].ParameterType == typeof(RemoteAddress))
                {
                    args = CUtils.SubArray(args, 1);
                }
                if (args.Length > 0)
                {
                    type_route = args[0].ParameterType;
                    if (typeof(ISerializable).IsAssignableFrom(type_route))
                    {
                        if (method.ReturnType == typeof(void))
                        {
                            type_return = null;
                            return true;
                        }
                        else
                        {
                            if (method.ReturnType.GUID == typeof(Task<>).GUID)
                            {
                                var ret_gargs = method.ReturnType.GetGenericArguments();
                                if (ret_gargs == null || ret_gargs.Length != 1)
                                    throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                                if (typeof(ISerializable).IsAssignableFrom(ret_gargs[0]))
                                {
                                    type_return = ret_gargs[0];
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            type_route = null;
            type_return = null;
            return false;
        }

        private static void ValidateMethod_Sync(RpcHandlerAttribute attr, MethodInfo method, ParameterInfo[] args)
        {
            if (method.ReturnType != typeof(void))
                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            if (HasAttribute<AsyncStateMachineAttribute>(method))
                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));

            // check route //
            if (attr.IsHandleAny)
            {
                if (args[0].ParameterType != typeof(ISerializable))
                    throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            else
            {
                if (args[0].ParameterType != attr.Route)
                    throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            // check return //
            if (attr.IsReturnVoid)
            {
                if (args.Length != 1)
                    throw new Exception(string.Format("Parameter Count Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            else
            {
                if (args.Length != 2)
                    throw new Exception(string.Format("Parameter Count Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                // check return type //
                {
                    if (args[1].ParameterType.BaseType != typeof(MulticastDelegate) || args[1].ParameterType.GenericTypeArguments.Length != 1)
                        throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                    if (args[1].ParameterType.Name != (typeof(OnRpcReturn<ISerializable>).Name))
                        throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                    if (attr.IsHandleAny)
                    {
                        if (args[1].ParameterType.GenericTypeArguments[0] != typeof(ISerializable))
                            throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                    }
                    else
                    {
                        if (args[1].ParameterType.GenericTypeArguments[0] != attr.Return)
                            throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                    }
                    //if (args[1].ParameterType.ge)
                }
            }
        }
        private static void ValidateMethod_Async(RpcHandlerAttribute attr, MethodInfo method, ParameterInfo[] args)
        {
            var ret_gargs = method.ReturnType.GetGenericArguments();
            if (method.ReturnType == typeof(Task))
            {

            }
            else if (method.ReturnType.GUID == typeof(Task<>).GUID)
            {
                if (ret_gargs == null || ret_gargs.Length != 1)
                    throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            else
            {
                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            if (args.Length != 1)
                throw new Exception(string.Format("Parameter Count Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            // check route //
            if (attr.IsHandleAny)
            {
                if (args[0].ParameterType != typeof(ISerializable))
                    throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            else
            {
                if (args[0].ParameterType != attr.Route)
                    throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            // check return type //
            if (ret_gargs != null && ret_gargs.Length == 1)
            {
                var ret_result_type = ret_gargs[0];
                if (attr.IsHandleAny)
                {
                    if (ret_result_type != typeof(ISerializable))
                        throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                }
                else
                {
                    if (ret_result_type != attr.Return)
                        throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                }
                //if (args[1].ParameterType.ge)
            }
        }
        private static void ValidateBinaryMethod_Sync(RpcHandlerAttribute attr, MethodInfo method, ParameterInfo[] args)
        {
            if (method.ReturnType != typeof(void))
                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));

            // check route //
            {
                if (args[0].ParameterType != typeof(BinaryMessage))
                    throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            // check return //
            if (attr.IsReturnVoid)
            {
                if (args.Length != 1)
                    throw new Exception(string.Format("Parameter Count Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            else
            {
                if (args.Length != 2)
                    throw new Exception(string.Format("Parameter Count Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                // check return type //

                {
                    if (args[1].ParameterType != typeof(OnRpcReturnBinary))
                        throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                }
            }
        }
        private static void ValidateBinaryMethod_Async(RpcHandlerAttribute attr, MethodInfo method, ParameterInfo[] args)
        {
            var ret_gargs = method.ReturnType.GetGenericArguments();
            if (method.ReturnType == typeof(Task))
            {

            }
            else if (method.ReturnType.GUID == typeof(Task<>).GUID)
            {
                if (ret_gargs == null || ret_gargs.Length != 1)
                    throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                if (ret_gargs[0] != typeof(BinaryMessage))
                    throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            else
            {
                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            if (args.Length != 1)
                throw new Exception(string.Format("Parameter Count Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            // check route //
            if (args[0].ParameterType != typeof(BinaryMessage))
                throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
        }


        private static void ValidateMethod_Sync(out Type type_return, MethodInfo method, ParameterInfo[] args)
        {
            if (method.ReturnType != typeof(void))
                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            if (HasAttribute<AsyncStateMachineAttribute>(method))
                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            // check route //
            if (!typeof(ISerializable).IsAssignableFrom(args[0].ParameterType))
                throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            // check return //
            if (args.Length == 1)
            {
                type_return = null;
            }
            else if (args.Length == 2)
            {
                if (args[1].ParameterType.BaseType != typeof(MulticastDelegate) || args[1].ParameterType.GenericTypeArguments.Length != 1)
                    throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                if (args[1].ParameterType.Name != (typeof(OnRpcReturn<>).Name))
                    throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                if (!typeof(ISerializable).IsAssignableFrom(args[1].ParameterType.GenericTypeArguments[0]))
                    throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                type_return = args[1].ParameterType.GenericTypeArguments[0];
            }
            else
            {
                throw new Exception(string.Format("Parameter Count Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
        }
        private static void ValidateMethod_Async(out Type type_return, MethodInfo method, ParameterInfo[] args)
        {
            var ret_gargs = method.ReturnType.GetGenericArguments();
            if (method.ReturnType == typeof(Task))
            {
                type_return = null;
            }
            else if (method.ReturnType.GUID == typeof(Task<>).GUID)
            {
                if (ret_gargs == null || ret_gargs.Length != 1)
                    throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                if (!typeof(ISerializable).IsAssignableFrom(ret_gargs[0]))
                    throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                type_return = ret_gargs[0];
            }
            else
            {
                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            // check route //
            if (args.Length != 1)
                throw new Exception(string.Format("Parameter Count Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            if (!typeof(ISerializable).IsAssignableFrom(args[0].ParameterType))
                throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
        }
        private static void ValidateBinaryMethod_Sync(out Type type_return, MethodInfo method, ParameterInfo[] args)
        {
            if (method.ReturnType != typeof(void))
                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            // check route //
            if (args[0].ParameterType != typeof(BinaryMessage))
                throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            // check return //
            if (args.Length == 1)
            {
                type_return = null;
            }
            else if (args.Length == 2)
            {
                if (args[1].ParameterType != typeof(OnRpcReturnBinary))
                    throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                type_return = typeof(BinaryMessage);
            }
            else
            {
                throw new Exception(string.Format("Parameter Count Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }

        }
        private static void ValidateBinaryMethod_Async(out Type type_return, MethodInfo method, ParameterInfo[] args)
        {
            var ret_gargs = method.ReturnType.GetGenericArguments();
            if (method.ReturnType == typeof(Task))
            {
                type_return = null;
            }
            else if (method.ReturnType.GUID == typeof(Task<>).GUID)
            {
                if (ret_gargs == null || ret_gargs.Length != 1)
                    throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                if (ret_gargs[0] != typeof(BinaryMessage))
                    throw new Exception(string.Format("Return Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                type_return = ret_gargs[0];
            }
            else
            {
                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            }
            // check route //
            if (args.Length != 1)
                throw new Exception(string.Format("Parameter Count Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
            if (args[0].ParameterType != typeof(BinaryMessage))
                throw new Exception(string.Format("Parameter Type Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
        }


        #endregion
    }


    //---------------------------------------------------------------------------------------------------------------------
}
