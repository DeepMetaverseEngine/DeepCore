using System;
using UnityEngine;

namespace Code.SceneInteract;

/// <summary>
/// 草丛交互
/// </summary>
public class SceneInteract_Grass : MonoBehaviour
{
    private Collider ColliderBox;
    private Bounds bounds;
    private Rigidbody Rig;
    
    private Collider[] OverlapBox = new Collider[10];

    private void Awake()
    {
        ColliderBox = GetComponent<Collider>();
        // Rig = gameObject.AddComponent<Rigidbody>();
        // Rig.useGravity = false;
        // Rig.isKinematic = true;
    }
    

    private void OnTriggerEnter(Collider from)
    {
        Debug.LogError($"~~~~  OnCollisionEnter : {from.gameObject.name}");
        
    }
    
    
    
}