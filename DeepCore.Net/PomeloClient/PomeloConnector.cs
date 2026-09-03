using DeepCore.IO;
using DeepCore.NetClient;
using System;
using System.Threading.Tasks;

namespace DeepCore.PomeloClient
{
    public class PomeloClient : INetClient
    {
        //---------------------------------------------------------------------------------------------------------------------
        public PomeloClient(IExternalizableFactory codec, string name = null, int request_timer_tick_ms = 5000)
            : base(codec, name, request_timer_tick_ms)
        {

        }
        protected override IClientAdapter CreateAdapter(string addr) => PomeloClientFactory.IOInstance.CreateAdapter(this);
        //---------------------------------------------------------------------------------------------------------------------



    }
}