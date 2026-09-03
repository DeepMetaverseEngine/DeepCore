using System;
using UnityEngine;

namespace DeepCore.Unity.AwaitHelper;

public class AsyncCoroutineRunner : MonoBehaviour
{
    private static AsyncCoroutineRunner instance;

    public static AsyncCoroutineRunner Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("AsyncCoroutineRunner")
                    .AddComponent<AsyncCoroutineRunner>();
                
            }
            
            return instance;
        }

    }
    
    private void Awake()
    {
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(gameObject);
    }
}