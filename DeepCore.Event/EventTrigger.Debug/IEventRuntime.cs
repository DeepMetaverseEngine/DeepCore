using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.EventTrigger.Debug
{
    public delegate void EventCollectionHander(IEventExecutorCollection events);
    public delegate void EventExecutorHander(EventExecutor exe);
    public delegate void EventTraceHander(IEventExecutorCollection events, EventExecutor exe, EventExternalizable data);
    public interface IEventRuntime
    {
        AbstractCollectionPool ObjectPool { get; }

        void BeginTrace(EventExecutor exe);
        void EventTrace(IEventExecutorCollection collection, EventExecutor exe, EventExternalizable data);

        IEnumerable<IEventExecutorCollection> AllEvents { get; }

        event EventCollectionHander OnAddEventCollection;
        event EventCollectionHander OnRemoveEventCollection;
        event EventExecutorHander OnBeginTrace; 
        event EventTraceHander OnTrace;
    }
}
