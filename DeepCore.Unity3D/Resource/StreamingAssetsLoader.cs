using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepCore.Unity3D
{
    public class StreamingAssetsLoader : IResourceLoader
    {
        public StreamingAssetsLoader()
        {
            Resource.AddLoader(this);
        }
        public static bool TryGetPath(string path, out string suffix)
        {
            if (Resource.IsStartWith(path, Application.streamingAssetsPath, out suffix))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IsStartWith(string path)
        {
            return TryGetPath(path, out _);
        }


    }
}
