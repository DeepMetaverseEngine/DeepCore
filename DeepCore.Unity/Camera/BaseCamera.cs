using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity.Camera
{
    public abstract class BaseCamera : MonoBehaviour
    {
        public abstract void ResetFromTransform();
    }
}
