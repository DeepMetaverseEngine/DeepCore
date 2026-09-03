using System;
using System.Collections.Generic;
using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepCore.Protocol
{
    [Reflectible]
    public interface IMessage : IExternalizable
    {
        int MessageID { get; set; }
    }

    [Reflectible]
    abstract public class NetMessage : IExternalizable, IMessage
    {
        public int MessageID { get; set; }

        abstract public void WriteExternal(IOutputStream output);
        abstract public void ReadExternal(IInputStream input);

    }
}
