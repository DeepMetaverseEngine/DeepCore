using System;
using System.Linq;
using UnityEngine;

namespace Code.Lightmap
{
    public class LightmapLoader : MonoBehaviour
    {
        public LightmapParam[] LightmapParams;
        public Texture2D[] LightmapColor;
        public Texture2D[] LightmapDir;

        private void Start()
        {
            if (LightmapColor == null 
                || LightmapDir == null 
                || LightmapColor.Length == 0 
                || LightmapDir.Length == 0)
                return;
            
            UpdateLightmapSetting();
            
            if (LightmapParams == null || LightmapParams.Length == 0)
                return;
            
            UpdateLightmap();
        }

        private void UpdateLightmapSetting()
        {
            var lightmaps = new LightmapData[LightmapColor.Length];
            for (int i = 0; i < LightmapColor.Length; i++)
            {
                lightmaps[i] = new LightmapData
                {
                    lightmapColor = LightmapColor[i],
                    lightmapDir = LightmapDir[i]
                };
            }

            LightmapSettings.lightmaps = lightmaps;
        }

        private void UpdateLightmap()
        {
            var baked = GetComponentsInChildren<LightmapParam>();
            Array.Sort(baked, (a,b) => a.index - b.index);
            foreach (var single in baked)
            {
                var render = single.gameObject.GetComponent<MeshRenderer>();
                if (render)
                {
                    render.lightmapIndex = single.lightmapIndex;
                    render.lightmapScaleOffset = single.offsetScale;
                }
            }
        }

        #region UNITY_EDITOR Resolve Lightmap

        [ContextMenu("Build lightmap data")]
        private void Resolver()
        {
            ResolverLightmap();
            CheckLightmapParams();
        }

        private void ResolverLightmap()
        {
            LightmapColor = new Texture2D[LightmapSettings.lightmaps.Length];
            LightmapDir = new Texture2D[LightmapSettings.lightmaps.Length];
            for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
            {
                var data = LightmapSettings.lightmaps[i];
                LightmapColor[i] = data.lightmapColor;
                LightmapDir[i] = data.lightmapDir;
            }
        }
        
        private void CheckLightmapParams()
        {
            var renderers = gameObject.GetComponentsInChildren<MeshRenderer>();
            LightmapParams = new LightmapParam[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                var render = renderers[i];
                var param = GetLightmapParam(render.gameObject);
                param.lightmapIndex = render.lightmapIndex;
                param.offsetScale = render.lightmapScaleOffset;
                param.index = i;
                LightmapParams[i] = param;
            }
        }

        private LightmapParam GetLightmapParam(GameObject go)
        {
            var param = go.GetComponent<LightmapParam>();
            if (param) return param;
            return go.AddComponent<LightmapParam>();
        }
        
     

        #endregion
        
        
    }
}