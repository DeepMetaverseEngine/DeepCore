using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity
{
    public static class BoundsExt
    {

        public static Bounds CalculateColliderBounds(this GameObject go)
        {
            var bounds = new Bounds();  
            var colliders = go.GetComponentsInChildren<Collider>();
            if (colliders != null)
            {
                foreach(var bc in colliders)
                {
                    bounds.Encapsulate(bc.bounds);
                }
            }
            return bounds;
        }
        public static Bounds CalculateMeshBounds(this GameObject go)
        {
            var bounds = new Bounds();
            var colliders = go.GetComponentsInChildren<MeshRenderer>();
            if (colliders != null)
            {
                foreach (var bc in colliders)
                {
                    bounds.Encapsulate(bc.bounds);
                }
            }
            return bounds;
        }
        public static Bounds CalculateRendererBounds(this GameObject go)
        {
            var bounds = new Bounds();
            var colliders = go.GetComponentsInChildren<Renderer>();
            if (colliders != null)
            {
                foreach (var bc in colliders)
                {
                    bounds.Encapsulate(bc.bounds);
                }
            }
            return bounds;
        }
    }
}
