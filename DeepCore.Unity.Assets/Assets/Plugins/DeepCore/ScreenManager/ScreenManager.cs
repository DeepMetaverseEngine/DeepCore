using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace DeepCore.Unity
{
    public class ScreenManager
    {
        public static ScreenManager Instance { get; private set; } = new ScreenManager();
        public ScreenManager() { Instance = this; }

        private Scene currentScene;
        public Scene CurrentScene => currentScene;
        public async UniTask ChangeScreenAsync(string sceneName, object args = null)
        {
            //             UniTask.RunOnThreadPool(async () =>
            //             {
            //                 await UniTask.SwitchToMainThread();
            //                 try
            //                 {
            // 
            //                 }
            //                 catch { }
            //             });
            if (OnBeginChangeScreenAsync != null)
            {
                await OnBeginChangeScreenAsync.Invoke(sceneName);
            }
            if (currentScene.IsValid())
            {
                await SceneManager.UnloadSceneAsync(currentScene);
            }
            var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            await async;
            var scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            if (scene != null)
            {
                this.currentScene = scene;
            }
            if (OnEndChangeScreenAsync != null)
            {
                await OnEndChangeScreenAsync.Invoke(scene);
            }
        }
        public event BeginChangeScreenAsync OnBeginChangeScreenAsync;
        public event EndChangeScreenAsync OnEndChangeScreenAsync;
    }

    public delegate UniTask BeginChangeScreenAsync(string sceneName);
    public delegate UniTask EndChangeScreenAsync(Scene scene);
}