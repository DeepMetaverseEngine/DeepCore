using System;
using UnityEngine;

namespace Code;

public class GlobalCoroutine : MonoBehaviour
{
    public readonly WaitForSeconds WaitFor033S = new(0.33f);
    public readonly WaitForSeconds WaitFor1S = new(1f);
    public readonly WaitForSeconds WaitFor3S = new(3f);
    public readonly WaitForSeconds WaitFor5S = new(5f);
    
    
    public static GlobalCoroutine Instance;

    private void Awake()
    {
        Instance = this;
    }
    

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
    
    
    
}