using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace DeepCore.UnityEditor
{

    public class LightControl : MonoBehaviour
    {
        public static HashSet<Light> AllSceneLight = new HashSet<Light>();

        public bool EnableShadow = false;
        // Use this for initialization
        void Awake()
        {
            Light l = GetComponent<Light>();
            if (EnableShadow)
            {
                l.shadows = LightShadows.Hard;
            }
            else
            {
                l.shadows = LightShadows.None;
            }
            AllSceneLight.Add(l);
        }

        void OnDestroy()
        {
            AllSceneLight.Remove(GetComponent<Light>());
        }

        // Update is called once per frame
        //void Update () {

        //}
    }
}