using Code.System.Pool;
using UnityEngine.SceneManagement;

namespace Code.System.Resource
{
    public class TaskStepWrapScene : ITaskStep, IPoolable
    {
        #region private WrapScene Wrap

        private WrapScene _wrap;

        private WrapScene Wrap
        {
            get
            {
                if (_wrap == null)
                {
                    _wrap = ObjectPool<WrapScene>.Get();
                }
                return _wrap;
            }
        }

        #endregion

        public string Url { get; internal set; }
        public bool IsCompleted { get; private set; }
        public bool Additive { get; internal set; }
        public SceneCompletedHandler Callback { get; internal set; }

        public void Start(bool bAsync = false)
        {
            var mode = Additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            Wrap.Url = Url;
#if _TF_RELEASE_
            //Wrap.Operate = Addressables.LoadSceneAsync(Url, mode);
            //Wrap.Operate.Completed += OnCompleted;
#else
            //Wrap.Scene = EditorSceneManager.LoadSceneInPlayMode(Url, new LoadSceneParameters(mode));
            //TickSystem.Tick(0, (serial, index) =>
            //{
            //    if (Wrap.Scene.isLoaded)
            //    {
            //        // OnCompleted(Wrap.Operate);
            //        TickSystem.TickCancel(serial);
            //    }
            //}, 10000);
#endif
        }

        // private void OnCompleted(AsyncOperationHandle<SceneInstance> op)
        // {
        //     IsCompleted = true;
        //     var sceneName = Path.GetFileNameWithoutExtension(Url);
        //     Wrap.GameObject = new GameObject($"[SceneRoot-{sceneName}]");
        //     SceneManager.MoveGameObjectToScene(Wrap.GameObject, SceneManager.GetSceneByName(sceneName));
        //     WrapGOWatchSystem.Add(Wrap);
        // }

        public void WaitForCompletion()
        {
#if _TF_RELEASE_
            _wrap.Operate.WaitForCompletion();
#endif
        }

        public void Invoke(long serial)
        {
            if (Callback == null) return;
            Callback.Invoke(serial, _wrap);
            Callback = null;
            _wrap = null;
        }

        public void Clear()
        {
            IsCompleted = false;
            
            if (_wrap != null)
            {
                _wrap.Dispose();
                _wrap = null;
            }

            if (Callback != null)
            {
                Callback.Invoke(0, null);
                Callback = null;
            }
        }

        public void Dispose()
        {
            Clear();
            ObjectPool<TaskStepWrapScene>.Release(this);
        }
    }
}