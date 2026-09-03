using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.EventTrigger.Debug;
using DeepCore.Log;
using DeepCrystal.NetServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCore.Event.Debug
{
    public class EventDebugHost : Disposable
    {
        public static bool TRACE_CONSOLE = false;
        protected Logger log { get; }
        public IEventRuntime Runtime { get; }
        public IServer Server { get; }
        public EventDebugHost(IEventRuntime runtime, IServer server)
        {
            this.log = LoggerFactory.GetLogger($"{runtime}:DebugHost");
            this.Server = server;
            this.Server.OnSessionValidateAsync += Server_OnSessionValidateAsync;
            this.Runtime = runtime;
            this.Runtime.OnAddEventCollection += Runtime_OnAddEventCollection;
            this.Runtime.OnRemoveEventCollection += Runtime_OnRemoveEventCollection;
            this.Runtime.OnBeginTrace += Runtime_OnBeginTrace;
            this.Runtime.OnTrace += Runtime_OnTrace;

        }

        public Task StartAsync()
        {
            return this.Server.StartAsync();
        }
        public Task StopAsync()
        {
            return this.Server.StopAsync(string.Empty);
        }
        protected override void Disposing()
        {
            Server.Dispose();
        }

        private Task<ValidateResult> Server_OnSessionValidateAsync(ISession session, IO.ISerializable user)
        {
            var all = new List<EventCollectionData>(Runtime.AllEvents.ConvertAll<IEventExecutorCollection, EventCollectionData>(e => ToData(e)));
            var total = new EventRuntimeState()
            {
                Collections = all,
            };
            return Task.FromResult(new ValidateResult(true, total));
        }

        private void Runtime_OnAddEventCollection(EventTrigger.IEventExecutorCollection events)
        {
            foreach (var e in events)
            {
                e.OnActiveChanged += Runtime_OnExecutorChanged;
            }
            Server.Broadcast(new AddCollectionNotify()
            {
                Add = ToData(events),
            });
        }
        private void Runtime_OnRemoveEventCollection(EventTrigger.IEventExecutorCollection events)
        {
            Server.Broadcast(new RemoveCollectionNotify()
            {
                GUID = events.GUID,
            });
        }
        private void Runtime_OnExecutorChanged(EventExecutor exe)
        {
            Server.Broadcast(new ExecutorChangedNotify()
            {
                CollectionGUID = exe.Group.GUID,
                ExeData = ToData(exe),
            });
        }
        private void Runtime_OnBeginTrace(EventExecutor exe)
        {
            Server.Broadcast(new EventBeginTraceNotify()
            {
                CollectionGUID = exe.Group.GUID,
                ExeName = exe.Name,
            });
        }

        private void Runtime_OnTrace(IEventExecutorCollection events, EventExecutor exe, EventExternalizable msg)
        {
            Server.Broadcast(new EventTraceData()
            {
                CollectionGUID = events.GUID,
                ExeName = exe.Name,
                NodeGUID = msg.OwnerNode?.GUID,
            });
            if (TRACE_CONSOLE)
            {
                if (msg is AbstractTrigger)
                {
                    log.Color = ConsoleColor.Yellow;
                }
                else if (msg is AbstractCondition)
                {
                    log.Color = ConsoleColor.Magenta;
                }
                else if (msg is AbstractAction)
                {
                    log.Color = ConsoleColor.Blue;
                }
                else if (msg is AbstractValue)
                {
                    log.Color = ConsoleColor.Cyan;
                }
                log.Trace($"{events.Name} : {exe.Name} : {msg}");
            }
        }

        public static EventCollectionData ToData(IEventExecutorCollection events)
        {
            var data = new EventCollectionData();
            data.GUID = events.GUID;
            data.Name = events.Name;
            data.TemplateID = events.TemplateID;
            data.TemplateType = events.TemplateType;
            data.Events = new List<EventExecutorData>();
            events.ForEachEvents(e =>
            {
                data.Events.Add(ToData(e));
            });
            return data;
        }
        public static EventExecutorData ToData(EventExecutor e)
        {
            return new EventExecutorData()
            {
                Name = e.Name,
                IsActive = e.IsActive,
                EventData = e.Data,
                TracingNodes = new List<string>(e.TracingNodes),
            };
        }

    }
}
