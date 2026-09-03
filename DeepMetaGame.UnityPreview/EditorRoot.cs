using DeepCore;
using DeepCore.Unity;
using DeepMetaGame.Unity.Preview.Battle;
using DeepMetaGame.Unity.Preview.Preview;
using DeepMetaGame.Unity.Preview.Resource;
using DeepMetaGame.Unity.Preview.SceneEditor;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview.Root
{
    public class EditorRoot : MonoBehaviour
    {

        [SerializeField] public Transform SceneEditor;
        [SerializeField] public Transform Preview;
        [SerializeField] public Transform Resource;
        [SerializeField] public Transform Battle;

        void Awake()
        {
            var prop = UnityBattleFactory.CommandLineArgs;
            OnAwake(prop);
        }
        protected virtual void OnAwake(Properties prop)
        {
            if (prop.TryGetAsBool("-Preview", out var preview) && preview)
            {
                this.Preview = AddProxy<PreviewProxy>(Preview);
                this.Preview.gameObject.SetActive(true);
            }
            else if (prop.TryGetAsBool("-Resource", out var resource) && resource)
            {
                this.Resource = AddProxy<ResourceProxy>(Resource);
                this.Resource.gameObject.SetActive(true);
            }
            else if (prop.TryGetAsBool("-SceneEditor", out var sceneEditor) && sceneEditor)
            {
                this.SceneEditor = AddProxy<SceneEditorProxy>(SceneEditor);
                this.SceneEditor?.gameObject?.SetActive(true);
            }
            else if (prop.TryGetAsBool("-Battle", out var battle) && battle)
            {
                this.Battle = AddProxy<BattleProxy>(Battle);
                this.Battle.gameObject.SetActive(true);
            }
            else if (SceneEditor && SceneEditor.gameObject.activeSelf)
            {
                AddProxy<SceneEditorProxy>(SceneEditor);
            }
            else if (Preview && Preview.gameObject.activeSelf)
            {
                AddProxy<PreviewProxy>(Preview);
            }
            else if (Resource && Resource.gameObject.activeSelf)
            {
                AddProxy<ResourceProxy>(Resource);
            }
            else if (Battle && Battle.gameObject.activeSelf)
            {
                AddProxy<BattleProxy>(Battle);
            }
            else
            {
                this.Preview = AddProxy<PreviewProxy>(Preview);
                this.Preview.gameObject.SetActive(true);
            }
        }
        protected Transform AddProxy<T>(Transform proxy) where T : UnityIPC
        {
            if (proxy == null)
            {
                var go = new GameObject(typeof(T).Name);
                proxy = go.transform;
                proxy.SetParent(this.transform, false);
            }
            if (!proxy.TryGetComponentInChildren<T>(out var preview))
            {
                preview = proxy.gameObject.AddComponent<T>();
            }
            return proxy;
        }
    }
}
