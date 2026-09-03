using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DeepCore.UnityEditor
{
    public class GetDepth : MonoBehaviour
    {

        void Start()
        {
            Camera _cam = this.gameObject.GetComponent<Camera>();
            if (_cam != null)
            {
                _cam.depthTextureMode = DepthTextureMode.Depth;
            }

        }



    }
}