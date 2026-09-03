using DeepCore.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepFrozen.ICE
{
    public class IceProxyFactory
    {
        public static IceProxyFactory Instance { get; private set; } = new IceProxyFactory();

        public IceProxyFactory()
        {
            Instance = this;
        }
        public virtual DeepFrozenIceImpl.IRpcNameServerAdapterPrx CreateNameServerProxy(Ice.Communicator communicator, string endPoint)
        {
            //Console.WriteLine("CreateNameServerProxy : " + endPoint);
            if (IPUtil.TryParseHostPort(endPoint, out var host, out var port))
            {
                return DeepFrozenIceImpl.IRpcNameServerAdapterPrxHelper.uncheckedCast(communicator.stringToProxy(string.Format("NameServer:default -h {0} -p {1}", host, port)));
            }
            else
            {
                return DeepFrozenIceImpl.IRpcNameServerAdapterPrxHelper.uncheckedCast(communicator.stringToProxy(endPoint));
            }
        }
        public virtual DeepFrozenIceImpl.IRpcServiceAdapterPrx CreateNodeServiceProxy(Ice.Communicator communicator, string nodeName, string endPoint = null)
        {
            //Console.WriteLine("CreateNodeServiceProxy : " + endPoint);
            if (IPUtil.TryParseHostPort(endPoint, out var host, out var port))
            {
                return DeepFrozenIceImpl.IRpcServiceAdapterPrxHelper.uncheckedCast(communicator.stringToProxy(string.Format("{0}:default -h {1} -p {2}", nodeName, host, port)));
            }
            else
            {
                return DeepFrozenIceImpl.IRpcServiceAdapterPrxHelper.uncheckedCast(communicator.stringToProxy(endPoint));
            }
        }
    }
    
}
