using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Remote;
using System;
using System.Collections.Generic;

namespace DeepFrozen.RPC.Remote.GridImpl
{
    public static class RpcMessageFactory
    {
        private static MessageFactoryGenerator factory = new MessageFactoryGenerator("");
        public static MessageFactoryGenerator MessageFactory
        {
            get => factory;
        }
        static RpcMessageFactory()
        {
            var list = ReflectionUtil.FindTypesFromAssembly(typeof(RpcMessageFactory).Assembly, IsProtocol);
            list.Sort((a, b) => { return a.FullName.CompareTo(b.FullName); });
            int id = 1;
            foreach (var t in list)
            {
                factory.RegistExternalizable(t, id++);
            }
        }
        public static bool IsProtocol(Type t)
        {
            return (!t.IsAbstract && t.IsSubclassOf(typeof(RpcMessage)));
        }
    }
    //---------------------------------------------------------------------------------------------------------------------------------------
    #region Base
    public class RpcException : Exception
    {
        private string message;
        private string stack_trace;
        public RpcException() { }
        public RpcException(string message, string stack_trace) : base(message)
        {
            this.stack_trace = stack_trace;
        }
        public RpcException(Exception err) : base(err.Message, err)
        {
            this.stack_trace = err.StackTrace;
        }
        public override string Message => this.message;
        public override string StackTrace => this.stack_trace;

        public static void ReadExternal(RpcException err, IInputStream input)
        {
            err.message = input.GetUTF();
            err.stack_trace = input.GetUTF();
        }
        public static void WriteExternal(RpcException err, IOutputStream output)
        {
            output.PutUTF(err.message);
            output.PutUTF(err.stack_trace);
        }
    }
    public abstract class RpcMessage : IExternalizable
    {
        public abstract void ReadExternal(IInputStream input);
        public abstract void WriteExternal(IOutputStream output);
    }
    public abstract class RpcRequest : RpcMessage
    {
        public int messageID;
        public override void ReadExternal(IInputStream input)
        {
            this.messageID = input.GetS32();
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(messageID);
        }
    }
    public abstract class RpcResponse : RpcMessage
    {
        public int messageID;
        public bool result = true;
        public RpcException error;
        public override void ReadExternal(IInputStream input)
        {
            this.messageID = input.GetS32();
            this.result = input.GetBool();
            this.error = input.Decode<RpcException>(RpcException.ReadExternal);
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutS32(messageID);
            output.PutBool(result);
            output.Encode(error, RpcException.WriteExternal);
        }
    }

    #endregion
    //---------------------------------------------------------------------------------------------------------------------------------------

    public class n2s_DispatchCreateServiceREQ : RpcRequest
    {
        public RemoteAddress from;
        public RemoteAddress path;
        public Properties config;
        public bool is_static;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.from.ReadExternal(input);
            this.path.ReadExternal(input);
            this.config = input.Decode<Properties>(Properties.ReadExternal);
            this.is_static = input.GetBool();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            this.from.WriteExternal(output);
            this.path.WriteExternal(output);
            output.Encode(this.config, Properties.WriteExternal);
            output.PutBool(this.is_static);
        }
    }
    public class n2s_DispatchCreateServiceRSP : RpcResponse
    {
    }
    public class n2s_DispatchDestoryServiceREQ : RpcRequest
    {
        public RemoteAddress from;
        public RemoteAddress path;
        public string reason;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.from.ReadExternal(input);
            this.path.ReadExternal(input);
            this.reason = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            this.from.WriteExternal(output);
            this.path.WriteExternal(output);
            output.PutUTF(this.reason);
        }
    }
    public class n2s_DispatchDestoryServiceRSP : RpcResponse
    {
    }
    public class n2s_ServiceDisposingNTF : RpcMessage
    {
        public RemoteAddress addr;
        public override void ReadExternal(IInputStream input)
        {
            addr.ReadExternal(input);
        }
        public override void WriteExternal(IOutputStream output)
        {
            addr.WriteExternal(output);
        }
    }
    public class n2s_ServiceDestoryedNTF : RpcMessage
    {
        public RemoteAddress addr;
        public override void ReadExternal(IInputStream input)
        {
            addr.ReadExternal(input);
        }
        public override void WriteExternal(IOutputStream output)
        {
            addr.WriteExternal(output);
        }
    }

    public class s2n_RegistNodeREQ : RpcRequest
    {
        public ServiceNodeStartInfo info;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.info = input.Decode<ServiceNodeStartInfo>(ServiceNodeStartInfo.ReadExternal);
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.Encode(info, ServiceNodeStartInfo.WriteExternal);
        }
    }
    public class s2n_RegistNodeRSP : RpcResponse
    {
    }
    public class s2n_UnregistNodeREQ : RpcRequest
    {
        public string nodeName;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.nodeName = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(nodeName);
        }
    }
    public class s2n_UnregistNodeRSP : RpcResponse
    {

    }
    public class s2n_UpdateNodeStateNTF : RpcMessage
    {
        public ServiceNodeStateInfo info;
        public override void ReadExternal(IInputStream input)
        {
            this.info = input.Decode<ServiceNodeStateInfo>(ServiceNodeStateInfo.ReadExternal);
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.Encode(info, ServiceNodeStateInfo.WriteExternal);
        }
    }
    public class s2n_GetOrCreateRemoteServiceREQ : RpcRequest
    {
        public GetServiceOperation operation;
        public RemoteAddress from;
        public RemoteAddress path;
        public Properties config;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.operation = input.GetEnum8<GetServiceOperation>();
            this.from.ReadExternal(input);
            this.path.ReadExternal(input);
            this.config = input.Decode(new Properties(), Properties.ReadExternal);
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutEnum8(operation);
            this.from.WriteExternal(output);
            this.path.WriteExternal(output);
            output.Encode(this.config, Properties.WriteExternal);
        }
    }
    public class s2n_GetOrCreateRemoteServiceRSP : RpcResponse
    {
        public RemoteProxyInfo info;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.info = input.Decode(new RemoteProxyInfo(), RemoteProxyInfo.ReadExternal);
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.Encode(this.info, RemoteProxyInfo.WriteExternal);
        }
    }
    public class s2n_DestoryRemoteServiceREQ : RpcRequest
    {
        public RemoteAddress from;
        public RemoteAddress path;
        public string reason;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.from.ReadExternal(input);
            this.path.ReadExternal(input);
            this.reason = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            this.from.WriteExternal(output);
            this.path.WriteExternal(output);
            output.PutUTF(this.reason);
        }
    }
    public class s2n_DestoryRemoteServiceRSP : RpcResponse
    {
        public RemoteAddress path;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.path.ReadExternal(input);
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            this.path.WriteExternal(output);
        }
    }
    public class s2n_GetServiceCountREQ : RpcRequest
    {
        public string serviceNode;
        public string serviceType;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.serviceNode = input.GetUTF();
            this.serviceType = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(this.serviceNode);
            output.PutUTF(this.serviceType);
        }
    }
    public class s2n_GetServiceCountRSP : RpcResponse
    {
        public int count;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.count = input.GetS32();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(count);
        }
    }
    public class s2n_GetRemoteServicesREQ : RpcRequest
    {
        public RemoteAddress from;
        public string[] paths;
        public bool isStatic;
        public string pattern;
        public string where;
        public string orderBy;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.from.ReadExternal(input);
            this.paths = input.GetUTFArray();
            this.isStatic = input.GetBool();
            this.pattern = input.GetUTF();
            this.where = input.GetUTF();
            this.orderBy = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            this.from.WriteExternal(output);
            output.PutUTFArray(paths);
            output.PutBool(isStatic);
            output.PutUTF(pattern);
            output.PutUTF(where);
            output.PutUTF(orderBy);
        }
    }
    public class s2n_GetRemoteServicesRSP : RpcResponse
    {
        public List<RemoteProxyInfo> infos;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            var count = input.GetS32();
            this.infos = new List<RemoteProxyInfo>(count);
            for (int i = 0; i < count; i++)
            {
                infos.Add(input.Decode(new RemoteProxyInfo(), RemoteProxyInfo.ReadExternal));
            }
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(infos.Count);
            foreach (var info in infos)
            {
                output.Encode(info, RemoteProxyInfo.WriteExternal);
            }
        }
    }
    public class s2n_GetStaticNodesREQ : RpcRequest
    {
    }
    public class s2n_GetStaticNodesRSP : RpcResponse
    {
        public List<ServiceNodeStartInfo> infos;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            var count = input.GetS32();
            this.infos = new List<ServiceNodeStartInfo>(count);
            for (int i = 0; i < count; i++)
            {
                infos.Add(input.Decode(new ServiceNodeStartInfo(), ServiceNodeStartInfo.ReadExternal));
            }
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(infos.Count);
            foreach (var info in infos)
            {
                output.Encode(info, ServiceNodeStartInfo.WriteExternal);
            }
        }
    }
    public class ServiceBroadcastMessageNTF : RpcMessage
    {
        public RemoteAddress from;
        public BinaryMessage msg;
        public override void ReadExternal(IInputStream input)
        {
            this.from.ReadExternal(input);
            this.msg.ReadExternal(input);
        }
        public override void WriteExternal(IOutputStream output)
        {
            this.from.WriteExternal(output);
            this.msg.WriteExternal(output);
        }
    }
    public class AppBroadcastMessageNTF : RpcMessage
    {
        public BinaryMessage notify;
        public override void ReadExternal(IInputStream input)
        {
            notify.ReadExternal(input);
        }
        public override void WriteExternal(IOutputStream output)
        {
            notify.WriteExternal(output);
        }
    }
    public class AppBroadcastCommandREQ : RpcRequest
    {
        public string notify;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.notify = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(notify);
        }
    }
    public class AppBroadcastCommandRSP : RpcResponse
    {
        public string notify;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.notify = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(notify);
        }
    }

    public class RpcRequestMessageREQ : RpcRequest
    {
        public RemoteAddress from;
        public RemoteAddress to;
        public BinaryMessage msg;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.from.ReadExternal(input);
            this.to.ReadExternal(input);
            this.msg.ReadExternal(input);
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            this.from.WriteExternal(output);
            this.to.WriteExternal(output);
            this.msg.WriteExternal(output);
        }
    }
    public class RpcResponseMessageRSP : RpcResponse
    {
        public RemoteAddress from;
        public RemoteAddress to;
        public BinaryMessage msg;
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.from.ReadExternal(input);
            this.to.ReadExternal(input);
            this.msg.ReadExternal(input);
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            this.from.WriteExternal(output);
            this.to.WriteExternal(output);
            this.msg.WriteExternal(output);
        }
    }
    public class RpcNotifyMessageNTF : RpcMessage
    {
        public RemoteAddress from;
        public RemoteAddress to;
        public BinaryMessage msg;
        public override void ReadExternal(IInputStream input)
        {
            this.from.ReadExternal(input);
            this.to.ReadExternal(input);
            this.msg.ReadExternal(input);
        }
        public override void WriteExternal(IOutputStream output)
        {
            this.from.WriteExternal(output);
            this.to.WriteExternal(output);
            this.msg.WriteExternal(output);
        }
    }
    public class RpcNotifyBatchMessageNTF : RpcMessage
    {
        public RemoteAddress from;
        public RemoteAddress to;
        public List<BinaryMessage> batch;
        public override void ReadExternal(IInputStream input)
        {
            this.from.ReadExternal(input);
            this.to.ReadExternal(input);
            this.batch = input.GetList(static (i) =>
            {
                var msg = new BinaryMessage();
                msg.ReadExternal(i);
                return msg;
            });
        }
        public override void WriteExternal(IOutputStream output)
        {
            this.from.WriteExternal(output);
            this.to.WriteExternal(output);
            output.PutList<BinaryMessage>(batch, static (o, msg) =>
            {
                msg.WriteExternal(o);
            });
        }
    }
    public class RpcNotifyTypeMessageNTF : RpcMessage
    {
        public RemoteAddress from;
        public string serviceNode;
        public string serviceType;
        public BinaryMessage msg;
        public override void ReadExternal(IInputStream input)
        {
            this.from.ReadExternal(input);
            this.serviceNode = input.GetUTF();
            this.serviceType = input.GetUTF();
            this.msg.ReadExternal(input);
        }
        public override void WriteExternal(IOutputStream output)
        {
            this.from.WriteExternal(output);
            output.PutUTF(serviceNode);
            output.PutUTF(serviceType);
            this.msg.WriteExternal(output);
        }
    }

    public class RpcWormholeMessageNTF : RpcMessage
    {
        public RemoteAddress from;
        public RemoteAddress to;
        public BinaryMessage msg;
        public bool srcIsBin;
        public override void ReadExternal(IInputStream input)
        {
            this.from.ReadExternal(input);
            this.to.ReadExternal(input);
            this.msg.ReadExternal(input);
            this.srcIsBin = input.GetBool();
        }
        public override void WriteExternal(IOutputStream output)
        {
            this.from.WriteExternal(output);
            this.to.WriteExternal(output);
            this.msg.WriteExternal(output);
            output.PutBool(this.srcIsBin);
        }
    }
    public class RpcWormholeTypeMessageNTF : RpcMessage
    {
        public RemoteAddress from;
        public string serviceNode;
        public string serviceType;
        public BinaryMessage msg;
        public bool srcIsBin;
        public override void ReadExternal(IInputStream input)
        {
            this.from.ReadExternal(input);
            this.serviceNode = input.GetUTF();
            this.serviceType = input.GetUTF();
            this.msg.ReadExternal(input);
            this.srcIsBin = input.GetBool();
        }
        public override void WriteExternal(IOutputStream output)
        {
            this.from.WriteExternal(output);
            output.PutUTF(serviceNode);
            output.PutUTF(serviceType);
            this.msg.WriteExternal(output);
            output.PutBool(this.srcIsBin);
        }
    }


}
