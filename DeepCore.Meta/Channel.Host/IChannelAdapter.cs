using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.Meta.Channel.Host
{
    public interface ISession
    {
        event Action<ISerializable> HandleC2S;
        void PostS2C(ISerializable msg);
        void Flush();
    }
}
