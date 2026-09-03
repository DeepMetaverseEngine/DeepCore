using System;
using System.Collections;
using System.Collections.Generic;
using Code.System.AB;
using Code.System.Resource;
using Code.Utility;
using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity3D.Impl;
using UnityEngine;

namespace Code.System
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance /*= new Lazy<AudioManager>(() => new AudioManager()).Value*/;

        private HashMap<string, AudioClip> AudioClips;
        private HashMap<string, AudioSource> AudioSources;
        
        [Range(0,1)] public float audioVolume = 0.3f;
        private void Awake()
        {
            Instance = this;
            AudioClips = new HashMap<string, AudioClip>();
            AudioSources = new HashMap<string, AudioSource>();
        }

        private void OnDestroy()
        {
            Clear();
            AudioClips = null;
            AudioSources = null;
        }

        private void Clear()
        {
            AudioClips.Clear();
            AudioSources.Clear();
        }

        public void Log(string msg)
        {
#if UNITY_EDITOR
            Debug.Log(msg);
#endif
        }

        public void Play(string audio, float duration = -1, bool autoRelease = true)
        {
            string url = ABSystemImpl.Inst.GetResUrl(audio);
            var name = DeepCore.IO.Resource.GetFileNameWithoutExtension(url);
            AudioClip clip = null;
            if (!AudioClips.TryGetValue(name, out clip))
            {
                var ModelWrap = ResourceSystem.GetWrapAsset<AudioClip>(url, name);
                if (ModelWrap != null && ModelWrap.Asset)
                {
                    clip = ModelWrap.Asset;
                    AudioClips.Add(name, clip);
                }
            }
            if (clip)
            {
                var source = GetOrCreateAudioSource(name, clip);
                PlayClip(source, duration, autoRelease);
            }
        }

        private AudioSource GetOrCreateAudioSource(string audio, AudioClip clip)
        {
            if (!AudioSources.TryGetValue(audio, out var source))
            {
                source = new GameObject("AudioSource").Parent(gameObject).AddComponent<AudioSource>();
                AudioSources.Add(audio, source);
            }
            source.name = audio;
            source.loop = false;
            source.playOnAwake = false;
            source.volume = audioVolume;
            source.clip = clip;
            return source;
        }

        private void PlayClip(AudioSource source, float duration, bool autoRelease)
        {
            source.Play();
            Log($"[Play Audio Clip] {source.name}, duration: {duration}");
            if (autoRelease)
            {
                StartCoroutine(DestroyAudio(source, duration));
            }
        }

        private IEnumerator DestroyAudio(AudioSource source, float duration)
        {
            if (duration <= 0)
            {
                yield return null;
                if (!source.isPlaying)
                {
                    AudioSources.Remove(source.name);
                    Destroy(source.gameObject);
                }
                yield break;
            }
            yield return new WaitForSeconds(duration);
            AudioSources.Remove(source.name);
            Destroy(source.gameObject);
        }
        
        
    }
}