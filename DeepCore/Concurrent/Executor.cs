using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepCore.Concurrent
{
    public interface Executor
    {
        Future Execute(Action command);

        Future Schedule(Action r, int delay);

        Future ScheduleAtFixedRate(Action r, int initial, int period);
    }

    public interface Future
    {
        long ID { get; }

        bool Cancel();

        bool IsCancelled { get; }
    }


}
