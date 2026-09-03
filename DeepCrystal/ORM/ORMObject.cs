using DeepCore;
using DeepCore.Reflection;
using DeepCore.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCrystal.ORM
{
    public abstract class ORMObject : AsyncDisposable
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder("ORM:Object");
        public ORMObject()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~ORMObject()
        {
            Alloc.RecordDestructor(GetType());
            if (!IsDisposed)
            {
                RecordDisposing();
            }
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(GetType());
        }
    }

    internal class ObjectTransaction : ORMObject, IObjectTransaction
    {
        protected readonly ITransactionDatabase db;
        public int BatchCount { get => db.BatchCount; }
        public ITransactionDatabase Database { get => db; }
        public ObjectTransaction(IMappingAdapter adapter, params ICondition[] conditions)
        {
            this.db = adapter.CreateTransaction(conditions);
        }
        public ObjectTransaction(IMappingAdapter adapter)
        {
            this.db = adapter.CreateTransaction();
        }
        public void Enqueue(Task task)
        {
            this.db.Enqueue(task);
        }
        public void Enqueue(Func<Task> task)
        {
            this.db.Enqueue(task());
        }
        public async virtual Task<bool> ExecuteAsync()
        {
            var ret = await db.ExecuteAsync();
            await this.DisposeAsync();
            return ret;
        }
        protected override void Disposing()
        {
            this.mappingQueue.Clear();
            db.Dispose();
        }
        protected override async ValueTask DisposingAsync()
        {
            this.mappingQueue.Clear();
            await db.DisposeAsync();
        }

        protected readonly Queue<MappingObject> mappingQueue = new Queue<MappingObject>();
        public void DebugBeginMappingObject(MappingObject mapping)
        {
            if (ORMFactory.IsTest)
            {
                this.mappingQueue.Enqueue(mapping);
            }
        }
        public void DebugForEachMappingObject(Action<MappingObject> action)
        {
            if (ORMFactory.IsTest)
            {
                foreach (var mapping in mappingQueue)
                {
                    action(mapping);
                }
            }
        }
    }

}

