using DeepCore.IO;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Remote.NameServer;
using DeepFrozen.RPC.Remote.ServiceNode;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Remote.ServiceNode
{
    public class RpcApplication : IRpcApplication
    {
        public static RpcApplication Instance { get; private set; } = new RpcApplication();

        private RpcServiceNode node;
        private Func<string, Task<string>> event_OnHandleAppCommand;
        private Action<ISerializable> event_OnHandleAppMessage;

        //-------------------------------------------------------------------------------------------
        internal void Bind(RpcServiceNode node)
        {
            this.node = node;
        }
        internal async Task<string> HandleAppCommandAsync(string cmd)
        {
            var sb = new StringBuilder();
            if (event_OnHandleAppCommand != null)
            {
                foreach (Func<string, Task<string>> evt in event_OnHandleAppCommand.GetInvocationList())
                {
                    var line = await evt.Invoke(cmd);
                    if (line != null)
                    {
                        sb.Append(line);
                    }
                }
            }
            return sb.ToString();
        }
        internal void HandleAppMessage(BinaryMessage bin)
        {
            event_OnHandleAppMessage?.Invoke(node.RpcCodec.ToSerializable(bin));
        }
        //-------------------------------------------------------------------------------------------
        public Task<string> AppCommandAsync(string bin)
        {
            return node.Adapter.s2n_BroadcastAppCommandAsync(bin);
        }
        public void BroadcastAppMessage(ISerializable msg)
        {
            node.Adapter.s2n_BroadcastAppMessage(node.RpcCodec.ToBinary(msg));
        }
        //-------------------------------------------------------------------------------------------
        public event Func<string, Task<string>> OnAppCommandAsync
        {
            add { event_OnHandleAppCommand += value; }
            remove { event_OnHandleAppCommand -= value; }
        }
        public event Action<ISerializable> OnAppMessage
        {
            add { event_OnHandleAppMessage += value; }
            remove { event_OnHandleAppMessage -= value; }
        }
        //-------------------------------------------------------------------------------------------
        

    }
}
