using DeepCore.Game3D.Host.Instance;
using DeepCore.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Host.ZoneServer
{
    public class ZoneNodeChannel : IPostChannel
    {
        public object Owner { get; }
        public ZoneNodeChannel(object owner)
        {
            this.Owner = owner;
        }
        public void Post(IMessage msg)
        {
        }
        public void Flush(object owner)
        {
        }

    }
}
