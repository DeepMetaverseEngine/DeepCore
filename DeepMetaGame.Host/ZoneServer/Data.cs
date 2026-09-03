using DeepCore.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepCore.Game3D.Host.ZoneServer
{
    public struct PlayerMessageEntry
    {
        public IMessage message;
        public DeepCore.IO.MemoryStream buffer;
    }

}
