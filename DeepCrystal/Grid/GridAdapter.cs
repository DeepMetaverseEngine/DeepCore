using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCrystal.NetServer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal.Grid
{
    //-----------------------------------------------------------------------------------------------------------------------------

    [Reflectible]
    public abstract class GridFactory
    {
        public static GridFactory Instance { get; private set; }
        public GridFactory() { Instance = this; }
        abstract public IGridAdapter CreateAdapter(IExternalizableFactory codec, Properties cfg);
    }
    //-----------------------------------------------------------------------------------------------------------------------------

    [Reflectible]
    public interface IGridAdapter : IDisposable
    {
        Task<bool> StartAsync(string localAddress);
        IGridProxy GetProxy(string remoteAddress);

        event HandleBinary OnHandleBinary;
        event HandleMessage OnHandleMessage;
        event HandleBinaryAsync OnHandleBinaryAsync;
        event HandleMessageAsync OnHandleMessageAsync;
    }
    //-----------------------------------------------------------------------------------------------------------------------------

    [Reflectible]
    public interface IGridProxy
    {
        IGridAdapter Adapter { get; }
        string RemoteAddress { get; }
        object UserTag { get; set; }

        void Send(BinaryMessage msg);
        void Send(ISerializable msg);

        Task<bool> SendAsync(BinaryMessage msg);
        Task<bool> SendAsync(ISerializable msg);

        Task<BinaryMessage> SendRequestAsync(BinaryMessage msg);
        Task<ISerializable> SendRequestAsync(ISerializable msg);
    }
    //-----------------------------------------------------------------------------------------------------------------------------
    public delegate void HandleBinary(IGridProxy proxy, BinaryMessage bin);
    public delegate void HandleMessage(IGridProxy proxy, ISerializable msg);
    public delegate Task<BinaryMessage> HandleBinaryAsync(IGridProxy proxy, BinaryMessage bin);
    public delegate Task<ISerializable> HandleMessageAsync(IGridProxy proxy, ISerializable msg);
    //-----------------------------------------------------------------------------------------------------------------------------
}
