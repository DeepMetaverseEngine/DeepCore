using DeepCore;
using DeepCore.Protocol;
using DeepCore.Reflection;
using System.Collections.Generic;

namespace DeepCrystal.Server
{
    public class ServerConfig
    {
        public string Name = "TestServer";
        public string Host;
        public int Port;
        public Properties Config = new Properties();
    }


}
