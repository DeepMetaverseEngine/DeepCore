using System;
using System.Collections;
using UnityEngine;

namespace Code.BattleView
{
    public class DrawDragPlot : MonoBehaviour
    {
        private void OnEnable()
        {
            gameObject.AddComponent<MeshFilter>().mesh = new Mesh { 
                vertices = new Vector3 [] 
                {
                    new (0.5f, -0.5f, 0.5f),
                    new (-0.5f, -0.5f, 0.5f),
                    new (0.5f, 0.5f, 0.5f),
                    new (-0.5f, 0.5f, 0.5f),
                    new (0.5f, 0.5f, -0.5f),
                    new (-0.5f, 0.5f, -0.5f),
                    new (0.5f, -0.5f, -0.5f),
                    new (-0.5f, -0.5f, -0.5f),
                    new (0.5f, 0.5f, 0.5f),
                    new (-0.5f, 0.5f, 0.5f),
                    new (0.5f, 0.5f, -0.5f),
                    new (-0.5f, 0.5f, -0.5f),
                    new (0.5f, -0.5f, -0.5f),
                    new (0.5f, -0.5f, 0.5f),
                    new (-0.5f, -0.5f, 0.5f),
                    new (-0.5f, -0.5f, -0.5f),
                    new (-0.5f, -0.5f, 0.5f),
                    new (-0.5f, 0.5f, 0.5f),
                    new (-0.5f, 0.5f, -0.5f),
                    new (-0.5f, -0.5f, -0.5f),
                    new (0.5f, -0.5f, -0.5f),
                    new (0.5f, 0.5f, -0.5f),
                    new (0.5f, 0.5f, 0.5f),
                    new (0.5f, -0.5f, 0.5f),
                },
                
                triangles = new [] 
                {
                    0, 2, 3,
                    0, 3, 1,
                    8, 4, 5,
                    8, 5, 9,
                    10, 6, 7,
                    10, 7, 11,
                    12, 13, 14,
                    12, 14, 15,
                    16, 17, 18,
                    16, 18, 19,
                    20, 21, 22,
                    20, 22, 23,
                }
            };
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) 
            {
                color = new Color(154,0,31,7)
            };
        }
    }
}