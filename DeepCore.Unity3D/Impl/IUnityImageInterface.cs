using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity3D.Impl
{
    public interface IUnityImageInterface
    {
        int Width { get; }
        int Height { get; }
        float MaxU { get; }
        float MaxV { get; }

        Texture Texture { get; }
        Texture TextureMask { get; }
    }
}
