using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepCore.Unity3D
{
    public static partial class UnityHelper
    {
        private static GameObject sCacheNode;

        private static UnityTotalComponent sHelper;

        private static UnityTotalComponent HelperComponent
        {
            get
            {
                if (!sHelper)
                {
                    InitHelperGameObject();
                }

                return sHelper;
            }
        }

        public static Transform DisableParent
        {
            get
            {
                if (!sCacheNode)
                {
                    InitHelperGameObject();
                }

                return sCacheNode.transform;
            }
        }

        [RuntimeInitializeOnLoadMethod]
        static void InitHelperGameObject()
        {
            if (!sHelper)
            {
                var obj = new GameObject("UnityHelperObject");
                sHelper = obj.AddComponent<UnityTotalComponent>();
                Object.DontDestroyOnLoad(obj);
            }

            if (!sCacheNode)
            {
                sCacheNode = new GameObject("_CacheNode_");
                Object.DontDestroyOnLoad(sCacheNode);
                sCacheNode.SetActive(false);
            }
        }

        public static void WaitForEndOfFrame(Action cb)
        {
            WaitForSeconds(0, cb);
        }

        public static void WaitForSeconds(float sec, Action cb)
        {
            HelperComponent.DelayInvoke.Delay(sec, cb);
        }

        public static void WaitForEndOfFrame<T>(T obj, Action<T> cb)
        {
            WaitForSeconds(0, obj, cb);
        }

        public static void WaitForSeconds<T>(float sec, T obj, Action<T> cb)
        {
            HelperComponent.DelayInvoke.Delay(sec, obj, cb);
        }

        public static Coroutine StartCoroutine(IEnumerator ator)
        {
            return HelperComponent.DelayInvoke.StartCoroutine(ator);
        }

        public static void StopCoroutine(Coroutine routine)
        {
            HelperComponent.DelayInvoke.StopCoroutine(routine);
        }

        public static void StopCoroutine(IEnumerator routine)
        {
            HelperComponent.DelayInvoke.StopCoroutine(routine);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="act"></param>
        public static void MainThreadInvoke(Action act)
        {
            HelperComponent.MainThreadDispatcher.Enqueue(act);
        }


        public static T AddGlobalComponent<T>() where T : Component
        {
            return HelperComponent.gameObject.AddComponent<T>();
        }

        public static T GetOrAddGlobalComponent<T>() where T : Component
        {
            return HelperComponent.GetOrAddComponent<T>();
        }
    }
}