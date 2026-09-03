using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneExt
{
    public static Camera GetMainCamera(this Scene scene)
    {
        foreach (var o in scene.GetRootGameObjects())
        {
            var camera = o.GetComponent<Camera>();
            if (camera != null)
            {
                return camera;
            }
        }
        return null;
    }
    public static GameObject GetRootGameObject(this Scene scene, string childName)
    {
        foreach (var o in scene.GetRootGameObjects())
        {
            if (o.name == childName)
            {
                return o;
            }
        }
        return null;
    }
    public static GameObject[] GetRootGameObjects(this Scene scene, string childName)
    {
        var ret = new List<GameObject>();
        foreach (var o in scene.GetRootGameObjects())
        {
            if (o.name == childName)
            {
                ret.Add(o);
            }
        }
        return ret.ToArray();
    }
}

