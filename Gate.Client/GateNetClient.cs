using DeepCore.IO;
using DeepCore.NetClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gate.Client
{
    public class GateNetClient : INetClient
    {
        public GateNetClient(IExternalizableFactory codec, string name = null, int request_timer_tick_ms = 5000) : base(codec, name, request_timer_tick_ms)
        {
        }
        protected override IClientAdapter CreateAdapter(string addr) => GateClientManager.Instance.CreateNetClientAdapter(addr, this);
    }
}
