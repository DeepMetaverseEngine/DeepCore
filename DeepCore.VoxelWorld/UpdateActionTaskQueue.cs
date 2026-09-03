using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Threading
{
    public class UpdateActionTaskQueue : Disposable
    {
        private Queue<Action> actionBlock = new Queue<Action>();
        private SingleThreadCollectionPool collectionPool = new SingleThreadCollectionPool();

        public SingleThreadCollectionPool ObjectPool
        {
            get => collectionPool;
        }


        public void Update()
        {
            using (var list = collectionPool.AllocList<Action>())
            {
                lock (actionBlock)
                {
                    list.AddRange(actionBlock);
                    actionBlock.Clear();
                }
                foreach (var run in list)
                {
                    run.Invoke();
                }
            }
        }
        public void QueueTask(Action action)
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
            lock (actionBlock)
            {
                actionBlock.Enqueue(action);
            }
        }

        protected override void Disposing()
        {
            using (var list = collectionPool.AllocList<Action>())
            {
                lock (actionBlock)
                {
                    list.AddRange(actionBlock);
                }
                foreach (var run in list)
                {
                    run.Invoke();
                }
            }
            collectionPool.Dispose();
        }

    }
}
