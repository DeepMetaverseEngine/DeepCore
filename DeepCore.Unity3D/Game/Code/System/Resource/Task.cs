using System;
using System.Collections.Generic;
using System.Linq;
using Code.System.Pool;
using Code.Utility;

namespace Code.System.Resource
{
    public sealed class Task : ICleanable, IPoolable
    {
        public long Serial { get; internal set; }
        public short DiscardMS { get; internal set; }
        internal Action<bool> Completed;
        private readonly LinkedList<ITaskStep> _steps = new LinkedList<ITaskStep>();
        public bool IsCompleted => _steps.All(data => data.IsCompleted);

        public void AddStep(ITaskStep step)
        {
            _steps.AddLast(step);
        }

        public void Start()
        {
            foreach (var step in _steps)
            {
                step.Start();
            }
        }

        public void Invoke()
        {
            foreach (var step in _steps)
            {
                step.Invoke(Serial);
                step.Dispose();
            }

            _steps.Clear();
            Completed?.Invoke(true);
            Completed = null;
        }

        public void Clear()
        {
            foreach (var step in _steps)
            {
                step.Dispose();
            }

            _steps.Clear();
        }

        public void Dispose()
        {
            Clear();
            ObjectPool<Task>.Release(this);
        }
    }
}