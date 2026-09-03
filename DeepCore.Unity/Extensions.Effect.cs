using DeepCore.IO;
using DeepCore.Unity.ResourceSnap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeepCore.Unity
{
    public static partial class UnityExtensions
    {
        //--------------------------------------------------------------------------------------------------------------
        public static int GetParticleDurationMS(ParticleSystem pss, out bool bLoop)
        {
            return (int)(GetParticleDuration(pss, out bLoop, true) * 1000);
        }
        public static float GetParticleDuration(this ParticleSystem pss, out bool loop, bool allowLoop = true)
        {
            //if (!particle.emission.enabled) { loop = false; return 0f; }
            if (pss.main.loop)
            {
                loop = true;
                if (!allowLoop) return -1f;
            }
            else { loop = false; }
            return pss.main.duration;
        }
        public static bool TryGetParticleDuration(this IEnumerable<ParticleSystem>  pss, out float duration, out bool loop, bool includeInactive = true, bool allowLoop = true)
        {
            loop = false;
            duration = -1f;
            foreach (var ps in pss)
            {
                var time = ps.GetParticleDuration(out var _loop, allowLoop);
                if (time > duration)
                {
                    duration = time;
                }
                loop |= _loop;
            }
            return duration > 0;
        }
        //--------------------------------------------------------------------------------------------------------------
        [Obsolete]
        public static bool TryGetParticleDurationMS(this GameObject go, out float duration, out bool bLoop)
        {
            if (TryGetParticleDuration(go, out var bDuration, out bLoop, true, true, true))
            {
                duration = (bDuration * 1000);
                return true;
            }
            duration = -1;
            return false;
        }
        [Obsolete]
        public static bool TryGetParticleDuration(this GameObject gameObject, out float duration, out bool loop, bool includeChildren = true, bool includeInactive = true, bool allowLoop = true)
        {
            loop = false;
            duration = -1f;
            var pss = includeChildren ? gameObject.GetComponentsInChildren<ParticleSystem>(includeInactive) : gameObject.GetComponents<ParticleSystem>();
            foreach (var ps in pss)
            {
                var time = ps.GetParticleDuration(out var _loop, allowLoop);
                if (time > duration)
                {
                    duration = time;
                }
                loop |= _loop;
            }
            return duration > 0;
        }
        [Obsolete]
        public static bool SetParticleEmission(this GameObject gameObject, bool enable)
        {
            var pss = gameObject.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pss)
            {
                try
                {
                    var em = ps.emission;
                    em.enabled = enable;
                }
                catch { }
            }
            return pss.Length>0;
        }
        [Obsolete]
        public static bool PlayParticle(this GameObject gameObject)
        {
            var pss = gameObject.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pss)
            {
                ps.Simulate(0, true, true);
                ps.Play();
            }
            return pss.Length > 0;
        }
        //--------------------------------------------------------------------------------------------------------------
        public static bool TryGetParticleDurationMS(this GameObject go, ref ParticleSystem[] pss, out float duration, out bool bLoop)
        {
            if (TryGetParticleDuration(go, ref pss, out var bDuration, out bLoop, true, true, true))
            {
                duration = (bDuration * 1000);
                return true;
            }
            duration = -1;
            return false;
        }
        public static bool TryGetParticleDuration(this GameObject gameObject, ref ParticleSystem[] pss, out float duration, out bool loop, bool includeChildren = true, bool includeInactive = true, bool allowLoop = true)
        {
            loop = false;
            duration = -1f;
            pss = pss ?? (includeChildren ? gameObject.GetComponentsInChildren<ParticleSystem>(includeInactive) : gameObject.GetComponents<ParticleSystem>());
            foreach (var ps in pss)
            {
                var time = ps.GetParticleDuration(out var _loop, allowLoop);
                if (time > duration)
                {
                    duration = time;
                }
                loop |= _loop;
            }
            return duration > 0;
        }
        public static void SetParticleEmission(this GameObject gameObject, ref ParticleSystem[] pss, bool enable)
        {
            pss = pss ?? gameObject.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pss)
            {
                try
                {
                    var em = ps.emission;
                    em.enabled = enable;
                }
                catch { }
            }
        }
        public static void PlayParticle(this GameObject gameObject, ref ParticleSystem[] pss)
        {
            pss = pss ?? gameObject.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pss)
            {
                ps.Simulate(0, true, true);
                ps.Play();
            }
        }
        public static void PlayParticle(this IEnumerable<ParticleSystem> pss)
        {
            foreach (var ps in pss)
            {
                ps.Simulate(0, true, true);
                ps.Play();
            }
        }
        public static void ResetTrailRender(this GameObject gameObject, ref TrailRenderer[] pss)
        {
            pss = pss ?? gameObject.GetComponentsInChildren<TrailRenderer>(true);
            foreach (var ps in pss)
            {
                ps.Clear();
            }  
            
        }

        //--------------------------------------------------------------------------------------------------------------


    }
}
