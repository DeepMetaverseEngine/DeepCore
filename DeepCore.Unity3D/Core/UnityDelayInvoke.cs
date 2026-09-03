using UnityEngine;
using System;
using System.Collections;

namespace DeepCore.Unity3D
{
    public class UnityDelayInvoke : MonoBehaviour
    {
        //TODO 比较非协程实现效率
        public void Delay(float sec, Action cb)
        {
            StartCoroutine(StartNew(sec, cb));
        }

        public void Delay<T>(float sec, T obj, Action<T> cb)
        {
            StartCoroutine(StartNew(sec, obj, cb));
        }


        IEnumerator StartNew(float sec, Action cb)
        {
            if (sec <= 0.0001)
            {
                yield return new WaitForEndOfFrame();
            }
            else
            {
                yield return new WaitForSeconds(sec);
            }

            cb.Invoke();
        }

        IEnumerator StartNew<T>(float sec, T obj, Action<T> cb)
        {
            if (sec <= 0.0001)
            {
                yield return new WaitForEndOfFrame();
            }
            else
            {
                yield return new WaitForSeconds(sec);
            }

            cb.Invoke(obj);
        }

    }
}
