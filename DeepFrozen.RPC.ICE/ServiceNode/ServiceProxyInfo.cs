using System;
using System.Collections.Generic;
using System.Text;

namespace DeepFrozen.ICE.ServiceNode
{
    public class ServiceProxyInfo
    {
        public readonly DeepFrozenIceImpl.IRpcServiceAdapterPrx prx;
        public readonly DeepFrozenIceImpl.IRpcServiceAdapterPrx prx_oneway;
        public readonly DeepFrozenIceImpl.IRpcServiceAdapterPrx prx_towway;
        public readonly DeepCore.PomeloClient.PomeloTCP wormhole;
        public ServiceProxyInfo(DeepFrozenIceImpl.IRpcServiceAdapterPrx prx)
        {
            this.prx = prx;
            this.prx_oneway = DeepFrozenIceImpl.IRpcServiceAdapterPrxHelper.uncheckedCast(prx.ice_oneway());
            this.prx_towway = DeepFrozenIceImpl.IRpcServiceAdapterPrxHelper.uncheckedCast(prx.ice_twoway());
        }
        public void BatchFlush()
        {
            // TODO Auto flush in 10 ms
        }
    }
}
