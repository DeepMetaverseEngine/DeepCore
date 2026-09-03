using System.Text;
using UnityEngine;
using DeepCore.Unity;
using DeepCore.Unity.ResourceSnap;
using System.Collections.Generic;

namespace DeepCore.Unity.ResourceViewer
{

    public class AnimReplayController : MonoBehaviour
    {
        void Start()
        {
            if (gameObject.TryGetComponentsInChildren<Animator>(out var a1))
            {
                foreach (var anim in a1)
                {
                    if (anim.runtimeAnimatorController != null)
                    {
                        var clips = anim.runtimeAnimatorController.animationClips;
                        var list = new List<AnimationClipInfo>(clips.Length);
                        foreach (var clip in clips)
                        {
                            if (clip != null && !string.IsNullOrEmpty(clip.name))
                            {
                                list.Add(new AnimationClipInfo()
                                {
                                    name = clip.name,
                                    duration = clip.length,
                                });
                            }
                        }
                        var play = gameObject.AddComponent<AnimatorReplay>();
                        play._animator = anim;
                        play._clips = list.ToArray();
                        play.StartPlay(0);
                    }
                }
            }
            if (gameObject.TryGetComponentsInChildren<Animation>(out var a2))
            {
                foreach (var anim in a2)
                {
                    var count = anim.GetClipCount();
                    var list = new List<AnimationClipInfo>(count);
                    foreach (var clip in anim.ToGenericList<AnimationState>())
                    {
                        if (clip != null && !string.IsNullOrEmpty(clip.name))
                        {
                            list.Add(new AnimationClipInfo()
                            {
                                name = clip.name,
                                duration = clip.length,
                            });
                        }
                    }
                    var play = gameObject.AddComponent<AnimationReplay>();
                    play._animation = anim;
                    play._clips = list.ToArray();
                    play.StartPlay(0);
                }
            }
        }
        public void Replay()
        {
            foreach (var r1 in gameObject.GetComponentsInChildren<AnimatorReplay>()) { r1.StartPlay(0); }
            foreach (var r2 in gameObject.GetComponentsInChildren<AnimationReplay>()) { r2.StartPlay(0); }
        }
        public void Pause()
        {
            foreach (var r1 in gameObject.GetComponentsInChildren<AnimatorReplay>()) { r1.Pause(); }
            foreach (var r2 in gameObject.GetComponentsInChildren<AnimationReplay>()) { r2.Pause(); }
        }
    }

    public abstract class AnimReplay : MonoBehaviour
    {
        internal AnimationClipInfo[] _clips;
        protected bool pause = false;
        protected int currentClipIndex = 0;
        protected float currentPlayTime = 0;
        protected float currentEndPlayTime = 0;
        public float NormalizeTime { get; set; } = 0.1f;
        public string CurrentStateName { get; private set; }
        public int CurrentDurationMS { get; private set; }
        void Update()
        {
            if (pause) return;
            if (IsPlayOver())
            {
                PlayNextClip();
            }
            currentPlayTime += Time.deltaTime;
        }
        public void StartPlay(int index)
        {
            pause = false;
            if (_clips != null && _clips.Length > 0 && index >= 0 && index < _clips.Length)
            {
                Play(_clips[index]);
            }
        }
        public void PlayNextClip()
        {
            pause = false;
            if (_clips != null && _clips.Length > 0)
            {
                currentClipIndex = CMath.CycNum(currentClipIndex, 1, _clips.Length);
                var clip = _clips[currentClipIndex];
                Play(clip);
            }
        }
        public bool IsPlayOver()
        {
            return currentPlayTime > currentEndPlayTime;
        }
        public void Play(AnimationClipInfo clip)
        {
            CurrentStateName = clip.name;
            CurrentDurationMS = clip.durationMS;
            currentPlayTime = 0;
            currentEndPlayTime = clip.duration + NormalizeTime;
            OnPlayState(clip);
        }
        public void Pause()
        {
            pause = true;
            this.OnPauseState();
        }
        protected abstract void OnPlayState(AnimationClipInfo stateName);
        protected abstract void OnPauseState();

    }

    public class AnimatorReplay : AnimReplay
    {
        internal Animator _animator;
        protected override void OnPlayState(AnimationClipInfo c)
        {
            if (_animator != null && _animator.runtimeAnimatorController)
            {
                if (NormalizeTime > 0)
                {
                    _animator.CrossFade(c.name, NormalizeTime, 0);
                }
                else
                {
                    _animator.Play(c.name, 0);
                }
            }
        }
        protected override void OnPauseState()
        {
            if (_animator != null && _animator.runtimeAnimatorController)
            {
                _animator.StopPlayback();

            }
        }
    }


    public class AnimationReplay : AnimReplay
    {
        internal Animation _animation;
        protected override void OnPlayState(AnimationClipInfo c)
        {
            if (_animation != null)
            {
                if (NormalizeTime > 0)
                {
                    _animation.CrossFade(c.name, NormalizeTime, 0);
                }
                else
                {
                    _animation.Play(c.name);
                }
            }
        }
        protected override void OnPauseState()
        {
            if (_animation != null)
            {
                _animation.Stop();
            }
        }
    }

}