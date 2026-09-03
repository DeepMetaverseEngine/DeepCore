using DeepCore.Unity.ResourceSnap;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepCore.Unity
{
    //         public static class ExtensionMethods
    //         {
    //             public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
    //             {
    //                 var tcs = new TaskCompletionSource<bool>();
    //                 asyncOp.completed += obj => { tcs.SetResult(asyncOp.isDone); };
    //                 return ((Task)tcs.Task).GetAwaiter();
    //             }
    //             public static Task Async(this AsyncOperation asyncOp)
    //             {
    //                 var tcs = new TaskCompletionSource<bool>();
    //                 asyncOp.completed += obj => { tcs.SetResult(asyncOp.isDone); };
    //                 return tcs.Task;
    //             }
    //         }
    //     

    public static partial class UnityExtensions
    {
        //--------------------------------------------------------------------------------------------------------------
        public static bool TryGetAnimatorStates(this GameObject gameObject, ref AnimationClipInfo[] ret)
        {
            return TryGetAnimatorStates(gameObject, out var a1, out var a2, ref ret);
        }
        public static bool TryGetAnimatorStates(this GameObject gameObject, out Animator a1, out Animation a2, ref AnimationClipInfo[] ret)
        {
            a1 = null; a2 = null;
            if (ret == null)
            {
                if (gameObject.TryGetComponentInChildren<Animator>(out a1))
                {
                    if (a1.runtimeAnimatorController != null)
                    {
                        var clips = a1?.runtimeAnimatorController?.animationClips;
                        if (clips != null)
                        {
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
                            ret = list.ToArray();
                            return true;
                        }
                    }
                }
                if (gameObject.TryGetComponentInChildren<Animation>(out a2))
                {
                    var count = a2.GetClipCount();
                    var list = new List<AnimationClipInfo>(count);
                    foreach (var clip in a2.ToGenericList<AnimationState>())
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
                    ret = list.ToArray();
                    return true;
                }
                ret = null;
            }
            return false;
        }

        public static int GetAnimatorStateDuriationMS(this Animator animator, string stateName)
        {
            //动画片段时间长度
            var length = 0f;
            var clips = animator?.runtimeAnimatorController?.animationClips;
            if (clips != null)
            {
                foreach (var clip in clips)
                {
                    if (clip.name.Equals(stateName))
                    {
                        length = clip.length;
                        break;
                    }
                }
            }
            return (int)(length * 1000);
        }
        public static int GetAnimationStateDuriationMS(this Animation animator, string stateName)
        {
            //动画片段时间长度
            var length = 0f;
            //var clips = animator.ToGenericList<AnimationState>();
            foreach (var e in animator)
            {
                if (e is AnimationState clip)
                {
                    if (clip.name.Equals(stateName))
                    {
                        length = clip.length;
                        break;
                    }
                }
            }
            return (int)(length * 1000);
        }

        public static float GetAnimatorStateDuriation(this Animator animator, string stateName)
        {
            //动画片段时间长度
            var length = 0f;
            var clips = animator?.runtimeAnimatorController?.animationClips;
            if (clips != null)
            {
                foreach (var clip in clips)
                {
                    if (clip.name.Equals(stateName))
                    {
                        length = clip.length;
                        break;
                    }
                }
            }
            return length;
        }
        public static float GetAnimationStateDuriation(this Animation animator, string stateName)
        {
            //动画片段时间长度
            var length = 0f;
           // var clips = animator.ToGenericList<AnimationState>();
            foreach (var e in animator)
            {
                if (e is AnimationState clip)
                {
                    {
                        if (clip.name.Equals(stateName))
                        {
                            length = clip.length;
                            break;
                        }
                    }
                }
            }
            return length;
        }


        public static bool TryGetAnimatorStateDuriationMS(this GameObject go, string stateName, out int ms)
        {
            if (go.TryGetComponent<Animator>(out var a1))
            {
                ms = GetAnimatorStateDuriationMS(a1, stateName);
                return true;
            }
            if (go.TryGetComponent<Animation>(out var a2))
            {
                ms = GetAnimationStateDuriationMS(a2, stateName);
                return true;
            }
            ms = 0;
            return false;
        }
        public static bool TryGetAnimatorStateDuriation(this GameObject go, string stateName, out float length)
        {
            if (go.TryGetComponent<Animator>(out var a1))
            {
                length = GetAnimatorStateDuriation(a1, stateName);
                return true;
            }
            if (go.TryGetComponent<Animation>(out var a2))
            {
                length = GetAnimationStateDuriation(a2, stateName);
                return true;
            }
            length = 0;
            return false;
        }



        // 
        //         public static async Task<AsyncOperation> Async(this AsyncOperation self)
        //         {
        //             TaskCompletionSource<AsyncOperation> tcs = new TaskCompletionSource<AsyncOperation>();
        //             self.completed += (req) =>
        //             {
        //                 tcs.SetResult(self);
        //             };
        //             return await tcs.Task;
        //         }
        // 
        //         public static Transform SetActive(this Transform self, bool active)
        //         {
        //             var go = self.gameObject;
        //             if (go.activeSelf != active)
        //             {
        //                 go.SetActive(active);
        //             }
        // 
        //             return self;
        //         }
        // 
        //         public static GameObject ActiveSelf(this GameObject self, bool active)
        //         {
        //             if (self.activeSelf != active)
        //             {
        //                 self.SetActive(active);
        //             }
        // 
        //             return self;
        //         }
        // 
        //         public static LinkedListNode<T> SetValue<T>(this LinkedListNode<T> self, T value)
        //         {
        //             self.Value = value;
        //             return self;
        //         }

    }
}
