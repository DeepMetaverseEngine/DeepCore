using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform _cameraTransform;
    public Transform CameraTransform
    {
        get
        {
            if (!_cameraTransform && Camera.main)
            {
                var camera = Camera.allCameras.FirstOrDefault(c => c.name == "BattleCamera");
                if (camera != null)
                    _cameraTransform = camera.transform;
                else
                    _cameraTransform = Camera.main.transform;
            }

            return _cameraTransform;
        }
    }

    public bool Enable = true;
    
    private void OnEnable()
    {
        LookAt.SetLookRotation(-CameraTransform.forward, CameraTransform.up);
        transform.rotation = LookAt;
    }

    private Quaternion LookAt = Quaternion.identity;
    void Update()
    {
        if (CameraTransform && Enable)
        {
            LookAt.SetLookRotation(-CameraTransform.forward, CameraTransform.up);
            // transform.rotation = Quaternion.Slerp(transform.rotation, LookAt, Time.deltaTime * 3);
            transform.rotation = LookAt;
        }
    }
}
