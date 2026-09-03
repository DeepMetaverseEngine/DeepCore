using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Code.System.AB;
using Code.System.Pool;
using Code.System.World;
using Code.Utility;
using DeepCore.Unity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.System.Resource
{
    internal class ResourceSystemImpl : SingleSystem<ResourceSystemImpl>
    {
        private Task _lastTask;
        private LinkedList<Task> _tasks = new LinkedList<Task>();
        private LinkedList<Task> _activeTasks = new LinkedList<Task>();
        private short _maxActiveTask = 10;

        public long Task(bool top = false, short discardMS = 0)
        {
            _lastTask = ObjectPool<Task>.Get();
            _lastTask.Serial = WorldSystem.GenerateSerial();
            _lastTask.DiscardMS = discardMS;
            var node = LinkedListNodePool<Task>.Get().SetValue(_lastTask);
            if (top)
                _tasks.AddFirst(node);
            else
                _tasks.AddLast(node);
            return _lastTask.Serial;
        }

        public bool ContainsTask(long serial)
        {
            return GetTask(serial) != null;
        }
        
        public async Task<bool> TaskAsync(long serial)
        {
            var tcs = new TaskCompletionSource<bool>();
            var task = GetTask(serial);
            if (task != null)
            {
                task.Completed += (b) => { tcs.SetResult(b); };
            }
            else
            {
                Debug.LogError("TASK NOT EXIST!");
                tcs.SetResult(false);
            }

            return await tcs.Task;
        }

        public IEnumerator TaskCoroutine(long serial)
        {
            var task = GetTask(serial);
            while (task != null)
            {
                yield return null;
                task = GetTask(serial);
            }
        }

        public void TaskStepWrapBundle(long serial, string url, BundleCompletedHandler callback)
        {
            
        }
        
        public void TaskStepWrapAsset<T>(long serial, string url, string name, AssetCompletedHandler<T> callback)
            where T : Object
        {
            var task = GetTask(serial);
            if (task == null)
            {
                callback.Invoke(0, null);
                return;
            }
            
            var step = ObjectPool<TaskStepWrapAsset<T>>.Get();
            step.Init(url, name, callback);
            task.AddStep(step);
        }
        
        public void TaskStepWrapGO(long serial, string url, string name, GameObjectCompletedHandler callback
            , WrapGOCache cache = null, Transform parent = null)
        {
            var task = GetTask(serial);
            if (task == null)
            {
                callback.Invoke(0, null);
                return;
            }

            var step = ObjectPool<TaskStepWrapGO>.Get();
            step.Init(url, name, callback, cache, parent);
            task.AddStep(step);
        }

        public void TaskStepWrapScene(long serial, string url, bool additive, SceneCompletedHandler callback)
        {
            var task = GetTask(serial);
            if (task == null)
            {
                callback.Invoke(0, null);
                return;
            }

            var step = ObjectPool<TaskStepWrapScene>.Get();
            step.Url = url;
            step.Additive = additive;
            step.Callback = callback;
            task.AddStep(step);
        }

        public void TaskStepWrapAssetWebRequest<T>(long serial, string url, AssetCompletedHandler<T> callback)
            where T : Object
        {
            var task = GetTask(serial);
            if (task == null)
            {
                callback.Invoke(0, null);
                return;
            }
            
            var step = ObjectPool<TaskStepWrapAssetWebRequest<T>>.Get();
            step.Init(url, callback);
            task.AddStep(step);
        }

        public void TaskCancel(long serial)
        {
            var node = _tasks.FindNode((task) => task.Serial == serial) ??
                       _activeTasks.FindNode((task) => task.Serial == serial);
            if (node == null) return;
            node.List.Remove(node);
            node.Value.Dispose();
        }

        public async Task<WrapAsset<T>> GetWrapAssetAsync<T>(string url, string name, bool top = false) where T : Object
        {
            var tcs = new TaskCompletionSource<WrapAsset<T>>();
            var serial = Task(top);
            if (serial > 0)
            {
                TaskStepWrapAsset<T>(serial, url, name, (i, asset) =>
                {
                    tcs.SetResult(asset);
                });
            }
            else
            {
                tcs.SetResult(null);
            }

            return await tcs.Task;
        }

        public async Task<WrapGO> GetWrapGOAsync(string url, string name, bool top = false, short discardMS = 0
            , WrapGOCache cache = null, Transform parent = null)
        {
            var tcs = new TaskCompletionSource<WrapGO>();
            var serial = Task(top, discardMS);
            if (serial > 0)
            {
                TaskStepWrapGO(serial, url, name, (i, wrap) =>
                {
                    tcs.SetResult(wrap);
                }, cache, parent);
            }
            else
            {
                tcs.SetResult(null);
            }

            return await tcs.Task;
        }

        public void GetWrapGOInvoke(Action<WrapGO> cb, string url, string name, bool top = false, short discardMS = 0          , WrapGOCache cache = null, Transform parent = null)
        {
            var serial = Task(top, discardMS);
            if (serial > 0)
            {
                TaskStepWrapGO(serial, url, name, (i, wrap) =>
                {
                    cb(wrap);
                }, cache, parent);
            }
            else
            {
                cb(null);
            }
        }

        public async Task<WrapScene> GetWrapSceneAsync(string url, bool additive, bool top = false, short discardMS = 0)
        {
            var tcs = new TaskCompletionSource<WrapScene>();
            var serial = Task(top, discardMS);
            if (serial > 0)
            {
                TaskStepWrapScene(serial, url, additive, (i, wrap) => { tcs.SetResult(wrap); });
            }
            else
            {
                tcs.SetResult(null);
            }

            return await tcs.Task;
        }

        private Task GetTask(long serial)
        {
            if (IsDisposed)
            {
                Debug.LogError("ResourceSystem Has Disposed!");
                return null;
            }

            var task = _lastTask;
            if (task == null || task.Serial != serial)
            {
                task = _tasks.FindNode((val => val.Serial == serial))?.Value ??
                       _activeTasks.FindNode((val => val.Serial == serial))?.Value;
                _lastTask = task;
            }

            return task;
        }

        public WrapAsset<T> GetWrapAsset<T>(string url, string name)
            where T : Object
        {
            WrapAsset<T> retVal = null;
            TaskStepWrapAsset<T> step = ObjectPool<TaskStepWrapAsset<T>>.Get();
            using (step)
            {
                step.Init(url, name, (serial, wrap) =>
                {
                    retVal = wrap;
                });
                step.Start(false);
                step.Invoke(WorldSystem.GenerateSerial());
            }
            return retVal;
        }
        
        public WrapAsset<T> GetWrapAssetWebRequest<T>(string url, string name) where T : Object
        {
            WrapAsset<T> retVal = null;
            TaskStepWrapAsset<T> step = ObjectPool<TaskStepWrapAssetWebRequest<T>>.Get();
            using (step)
            {
                step.Init(url, name, (serial, wrap) =>
                {
                    retVal = wrap;
                });
                step.Start(false);
                step.Invoke(WorldSystem.GenerateSerial());
            }
            return retVal;
        }

        public WrapGO GetWrapGO(string url, string name, WrapGOCache cache = null, Transform parent = null)
        {
            WrapGO wrapGo = null;
            using (var step = ObjectPool<TaskStepWrapGO>.Get())
            {
                step.Init(url, name, (serial, wrap) =>
                {
                    wrapGo = wrap;
                }, cache, parent);
                step.Start(false);
                step.Invoke(WorldSystem.GenerateSerial());
            }
            return wrapGo;
        }

        protected override void OnUpdate(float deltaTime)
        {
            var deltaMS = (short)(deltaTime * 1000);
            var node = _tasks.First;
            while (node != null)
            {
                var tmp = node;
                node = node.Next;
            
                var task = tmp.Value;
                if (task.DiscardMS <= 0) continue;
                task.DiscardMS -= deltaMS;
                if (task.DiscardMS > 0) continue;
                _tasks.Remove(tmp);
                task.Dispose();
                if (task == _lastTask)
                {
                    _lastTask = null;
                }
                LinkedListNodePool<Task>.Release(tmp);
            }

            node = _activeTasks.First;
            while (node != null)
            {
                var tmp = node;
                node = node.Next;
                var task = tmp.Value;
                if (!task.IsCompleted) continue;
                _activeTasks.Remove(tmp);
                task.Invoke();
                task.Dispose();
                if (task == _lastTask)
                {
                    _lastTask = null;
                }
                LinkedListNodePool<Task>.Release(tmp);
            }

            if (_activeTasks.Count < _maxActiveTask && _tasks.Count > 0)
            {
                var count = _maxActiveTask - _activeTasks.Count;
                count = count > _tasks.Count ? _tasks.Count : count;
                while (count > 0)
                {
                    var tmp = _tasks.First;
                    _tasks.RemoveFirst();
                    tmp.Value.Start();
                    _activeTasks.AddLast(tmp);
                    count--;
                }
            }
        }

        protected override void Disposing()
        {
            _lastTask = null;
            foreach (var task in _tasks)
            {
                task.Dispose();
            }

            _tasks.Clear();
            _tasks = null;
            foreach (var activeTask in _activeTasks)
            {
                activeTask.Dispose();
            }

            _activeTasks.Clear();
            _activeTasks = null;
        }
    }
}