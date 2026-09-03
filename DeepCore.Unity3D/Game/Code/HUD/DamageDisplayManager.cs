using System;
using System.Collections.Generic;
using Code.Utility;
using DeepCore;
using UnityEngine;

namespace Code.HUD;

[Flags]
public enum DamageDisplayAminType
{
    EasyInOut,
    Floating,
    Scale,
}

public interface IDamageDisplay
{
    Action OnAnimPlayComplete { get; set; }
    void Play(float damage, DamageDisplayAminType display);
}

public class DamageDisplayManager : MonoBehaviour
{
    
    public static DamageDisplayManager Instance;
    
    [SerializeField] private DamageDisplayNode mTemp;
    [SerializeField] private static int mObjPoolSize = 8;
    [SerializeField] private GameObject mPoolRoot;
    private readonly int mMaxPoolSize= mObjPoolSize * 3;
    private Queue<IDamageDisplay> mDisplayPool;

    private void Awake()
    {
        Instance = this;
        if (!mPoolRoot)
        {
            mPoolRoot = new GameObject(nameof(mPoolRoot)) { transform = { position = new Vector3(5000, 5000) } };
        }
        InitPool();
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    
    private IDamageDisplay CreateDisplayNode()
    {
        var node = Instantiate(mTemp);
        node.gameObject.Parent(mPoolRoot);
        return node;
    }
    
    private void InitPool()
    {
        mDisplayPool ??= new Queue<IDamageDisplay>(mObjPoolSize);

        if (mDisplayPool.Count == 0)
        {
            for (int i = 0; i < mObjPoolSize; i++)
            {
                Enqueue(CreateDisplayNode());
            }
        }
    }
    
    private IDamageDisplay Dequeue()
    {
        InitPool();
        return mDisplayPool.Dequeue();
    }

    private void Enqueue(IDamageDisplay obj)
    {
        if (CanExpansion())
        {
            mDisplayPool.Enqueue(obj);
        }
    }

    private bool CanExpansion()
    {
        return (mDisplayPool ??= new Queue<IDamageDisplay>(mObjPoolSize)).Count < mMaxPoolSize;
    }


    #region API

    public void Play(float damage, Vector3 target, DamageDisplayAminType display)
    {
        var node = Dequeue();
        node.OnAnimPlayComplete += OnAnimPlayComplete;
        node.Play(damage, display);
    }

    private void OnAnimPlayComplete()
    {
        
    }

    #endregion



}