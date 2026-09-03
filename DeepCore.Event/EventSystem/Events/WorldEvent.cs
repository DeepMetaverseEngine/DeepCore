namespace DeepCore.Event.EventSystem.Events
{
    public abstract class CustomEvent : BaseEvent
    {
        public void Do(BaseEvent e)
        {
            AddChild(e);
        }


        public SelectorEvent Selector(params BaseEvent[] events)
        {
            var e = new SelectorEvent();
            e.AddChild(events);
            AddChild(e);
            return e;
        }

        public SelectorEvent Selector(int count, params BaseEvent[] events)
        {
            var e = new SelectorEvent(count);
            e.AddChild(events);
            AddChild(e);
            return e;
        }

        public SequenceEvent Sequence(params BaseEvent[] events)
        {
            var e = new SequenceEvent();
            e.AddChild(events);
            AddChild(e);
            return e;
        }

        public ParallelEvent Parallel(params BaseEvent[] events)
        {
            var e = new ParallelEvent();
            e.AddChild(events);
            AddChild(e);
            return e;
        }

    }

}
