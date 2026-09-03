using System.Collections.Generic;
using Code.Utility;
using DeepCore.Unity;
using UnityEngine;

namespace Code.System.World
{
    public class WorldSystemImpl : MonoBehaviour
    {
        private LinkedList<BaseSystem> _systems = new LinkedList<BaseSystem>();
        private LinkedListNode<BaseSystem> _head;
        private long _serialGenerator;

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            var head = _systems.First;
            while (head != null)
            {
                head.Value.Update(deltaTime);
                head = head.Next;
            }
        }

        public long GenerateSerial()
        {
            return ++_serialGenerator;
        }

        public T CreateSystem<T>() where T : BaseSystem, new()
        {
            return new T();
        }

        public T GetOrCreateSystem<T>() where T : BaseSystem, new()
        {
            var type = typeof(T);
            var node = _systems.FindNode((sys) => type.IsInstanceOfType(sys));
            if (node != null) return node.Value as T;

            return CreateSystem<T>();
        }

        internal void AddSystem(BaseSystem system)
        {
            _systems.AddFirst(system);
            system.Create();
        }

        public void ReleaseSystem(BaseSystem system)
        {
            var node = _systems.FindNode((sys) => sys == system);
            if (node == null) return;
            node.List.Remove(node);
            node.Value.Dispose();
        }
    }
}