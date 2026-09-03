using DeepCore.IO;
using DeepCore.Reflection;
using DeepCrystal.Server;
using System;

namespace DeepCrystal.NetServer
{

    [Reflectible]
    public abstract class ServerFactory
    {
        private static ServerFactory s_instance;
        public static ServerFactory Instance { get { return s_instance; } }
        public static ServerFactory SetFactory(Type type)
        {
            return DeepActivator.CreateInstance(type) as ServerFactory;
        }
        public static ServerFactory SetFactory(string fullName)
        {
            return DeepActivator.CreateInstance(ReflectionUtil.GetType(fullName)) as ServerFactory;
        }
        public ServerFactory() { s_instance = this; }
        public abstract IServer CreateServer(ServerConfig config, IExternalizableFactory codec);
        public abstract void Shutdown();
    }



}
