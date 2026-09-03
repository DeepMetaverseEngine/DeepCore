using DeepCore.Protocol;
using Gate.Data.Sample;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gate.Client.Modules
{
    public class _dummy : GateClientModule<GateClient>
    {
        public _dummy(GateClient client) : base(client)
        {
        }
        [NotifyHandler]
        private void handle(SampleNotify notify)
        {
            log.Info($"Notify : {notify.time}");
        }
    }
}
