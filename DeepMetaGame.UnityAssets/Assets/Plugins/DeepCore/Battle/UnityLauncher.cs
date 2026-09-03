using Cysharp.Threading.Tasks;
using DeepMetaGame.Unity.Preview;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepMetaGame.Unity
{

    public class UnityLauncher : MonoBehaviour
    {
        public static UnityLauncher Instance { get; private set; }
        public AssetMode AssetMode = AssetMode.Bundle;
        void Awake()
        {
            Instance = this;
            if (!Application.isEditor)
            {
                Application.quitting += static () =>
                {
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                };
                this.AssetMode = AssetMode.Bundle;
            }
            OnAwake();
            BattleBootstrap.OnFinish(Bootstrap_OnFinish);
        }
        protected virtual void OnAwake()
        {
        }
        [SerializeField] public string SceneName_EditorMain = "EditorMain";
        [SerializeField] public string SceneName_EditorGame = "EditorGame";
        [SerializeField] public string SceneName_GameRoot = "GameRoot";
        protected virtual async UniTask Bootstrap_OnFinish()
        {
            var GameMode = BattleBootstrap.RuntimeGameMode;
            var prop = UnityBattleFactory.CommandLineArgs;
            switch (GameMode)
            {
                case EGameMode.Editor_Preview:
                    // 进入编辑器 预览
                    UnityBattleFactory.CommandLineArgs.Put("-Preview", "true");
                    SceneManager.LoadScene(SceneName_EditorMain, LoadSceneMode.Additive);
                    break;
                case EGameMode.Editor_Resource:
                    // 进入编辑器 资源
                    UnityBattleFactory.CommandLineArgs.Put("-Resource", "true");
                    SceneManager.LoadScene(SceneName_EditorMain, LoadSceneMode.Additive);
                    break;
                case EGameMode.Editor_Scene:
                    // 进入编辑器 场景
                    UnityBattleFactory.CommandLineArgs.Put("-SceneEditor", "true");
                    SceneManager.LoadScene(SceneName_EditorMain, LoadSceneMode.Additive);
                    break;
                case EGameMode.BattleTest:
                    // 进入编辑器 战斗测试 BattleTest 场景
                    SceneManager.LoadScene(SceneName_EditorGame, LoadSceneMode.Additive);
                    break;
                case EGameMode.GameClient:
                    // 进入游戏
                    SceneManager.LoadScene(SceneName_GameRoot, LoadSceneMode.Additive);
                    break;
            }
            await UniTask.CompletedTask;
        }


        // Start is called before the first frame update
        protected virtual void Start()
        {
        }

        // Update is called once per frame
        protected virtual void Update()
        {

        }



    }
    public enum AssetMode
    {
        Prefab,
        Bundle,
    }


}