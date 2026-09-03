using DeepCore.IO;
using DeepCore.NetClient;
using DeepCore.PomeloClient;
using System;

namespace DeepCore.Event.Debug
{
    public class EventDebugSlave : Disposable
    {
        public INetClient Connector { get; }
        public EventDebugSlave(IExternalizableFactory codec)
        {
            this.Connector = PomeloClientFactory.IOInstance.CreateClient(new EventDebugProtocolFactory(codec), "event-debug", 3000);
            this.Connector.Listen<AddCollectionNotify>((e) => { OnAddCollection?.Invoke(this, e); });
            this.Connector.Listen<RemoveCollectionNotify>((e) => { OnRemoveCollection?.Invoke(this, e); });
            this.Connector.Listen<ExecutorChangedNotify>((e) => { OnExecutorChanged?.Invoke(this, e); });
            this.Connector.Listen<EventBeginTraceNotify>((e) => { OnBeginTrace?.Invoke(this, e); });
            this.Connector.Listen<EventTraceData>((e) => { OnTrace?.Invoke(this, e); });
        }
        public void Start(string hostAddress)
        {
            this.Connector.Connect(hostAddress, TimeSpan.FromSeconds(15), null, (err, init) =>
            {
                OnInit?.Invoke(this, init as EventRuntimeState);
            });
        }
        public void Stop()
        {
            this.Connector.Disconnect();
        }
        public void Update()
        {
            Connector.Update();
        }
        protected override void Disposing()
        {
            Connector.Dispose();
        }

        public event Action<EventDebugSlave, EventRuntimeState> OnInit;
        public event Action<EventDebugSlave, AddCollectionNotify> OnAddCollection;
        public event Action<EventDebugSlave, RemoveCollectionNotify> OnRemoveCollection;
        public event Action<EventDebugSlave, ExecutorChangedNotify> OnExecutorChanged;
        public event Action<EventDebugSlave, EventBeginTraceNotify> OnBeginTrace;
        public event Action<EventDebugSlave, EventTraceData> OnTrace;
    }
}
