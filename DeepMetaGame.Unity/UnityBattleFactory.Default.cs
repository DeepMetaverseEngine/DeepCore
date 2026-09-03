using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity.ResourceViewer;
using DeepCore.Unity3D;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Playables;

namespace DeepMetaGame.Unity
{
    //-----------------------------------------------------------------------------------------------------
    public abstract class DefaultAssetsTuple : Disposable, IAssetsTemplate
    {
        public ResourceType resType { get; set; }
        public string resName { get; set; }
        public abstract object handler { get; }
        public abstract UnityEngine.Object template { get; }
        public virtual bool PlayEffect(IWrapAssets wrap, string action, bool loop, float speed, Transform binding)
        {
            if (wrap.instance is GameObject go)
            {
                go.SetParticleEmission(true);
                go.PlayParticle();
                return true;
            }
            return false;
        }
        public virtual bool PlayAnim(IWrapAssets wrap, string action, bool loop, float speed)
        {
            return PLAY_PREVIEW(wrap, action, speed, loop, 0);
        }
        public virtual bool PlayAction(IWrapAssets wrap, UnitActionStatus status, UnitActionDefinitionMap.UnitActionKeyFrame action)
        {
            return PLAY_PREVIEW(wrap, action.ActionName, action.Speed, action.Cycle, action.CrossFadeTimeMS);
        }
        public virtual bool PlaySound(IWrapAssets go, ResourceType? restype = null)
        {
            System.Console.Beep(1000, 200);
            return false;
        }
        public virtual bool TryGetDurationMS(out float ms, out bool loop)
        {
            if (template is GameObject res)
            {
                return res.TryGetParticleDurationMS(out ms, out loop);
            }
            ms = 0f;
            loop = false;
            return false;
        }
        public virtual bool TryListAnims(List<AnimInfo> actions, UnitInfo unit = null)
        {
            var actionMap = default(UnitActionDefinitionMap);//PreviewProxy.TemplateDatas?.DefaultUnitActionDefinitions?.ActionMap;
            if (unit != null)
            {
                if (unit.Abilities.TryGetComponentAs<UnitResourceAbility>(out var u_res))
                {
                    if (u_res.OverrideActionMap != null)
                    {
                        actionMap = u_res.OverrideActionMap;
                    }
                }
                if (actionMap?.ActionMap != null)
                {
                    foreach (var act in actionMap.ActionMap)
                    {
                        if (act.FirstKeyFrame is UnitActionDefinitionMap.UnitActionKeyFrame kf)
                        {
                            actions.Add(new AnimInfo() { Name = $"Define: {act.Action}", Action = kf.ActionName });
                        }
                    }
                }
            }
            if (template is GameObject res)
            {
                LIST_PREVIEW(res, actions);
            }
            return true;
        }

        public static void LIST_PREVIEW(GameObject res, List<AnimInfo> actions)
        {
            if (res.TryGetComponentsInChildren<PlayableDirector>(out var playables, true))
            {
                foreach (var p in playables)
                {
                    actions.Add(new AnimInfo() { Name = $"Playable: {p.name}", Action = p.name });
                }
            }
            if (UnityBattleFactory.Resource.TryGetSpine(res, out var Spine))
            {
                if (Spine != null)
                {
                    foreach (var anim in Spine.Animations)
                    {
                        actions.Add(new AnimInfo() { Name = $"Spine-Anim: {anim}", Action = anim });
                    }
                    if (Spine.Skins != null)
                    {
                        foreach (var skin in Spine.Skins)
                        {
                            actions.Add(new AnimInfo() { Name = $"Spine-Skin: {skin}", Action = $"skin:{skin}" });
                        }
                    }
                }
            }
            if (res.TryGetComponentInChildren<Animation>(out var animation))
            {
                foreach (AnimationState state in animation)
                {
                    actions.Add(new AnimInfo() { Name = $"Animation: {state.name}", Action = state.name });
                }
            }
            if (res.TryGetComponentInChildren<Animator>(out var animator))
            {
                if (animator?.runtimeAnimatorController?.animationClips != null)
                {
                    foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    {
                        actions.Add(new AnimInfo() { Name = $"Animator: {clip.name}", Action = clip.name });
                    }
                }
            }
        }

        public static bool PLAY_PREVIEW(IWrapAssets wrap, string StateName, float speed, bool loop, float NormalizeTime)
        {
            try
            {
                if (wrap.instance is GameObject go)
                {
                    if (go.transform.TryGetComponentsInChildren<PlayableDirector>(out var playables, true))
                    {
                        foreach (var p in playables)
                        {
                            if (p.name != StateName)
                            {
                                p.gameObject.SetActive(false);
                                p.Stop();
                            }
                            else
                            {
                                p.gameObject.SetActive(true);
                                p.Play();
                                return true;
                            }
                        }
                    }
                    if (UnityBattleFactory.Resource.TryGetSpine(go, out var Spine))
                    {
                        if (!string.IsNullOrEmpty(StateName))
                        {
                            if (StateName.StartsWith("skin:"))
                            {
                                var skinName = StateName.Substring("skin:".Length);
                                Spine.initialSkinName = skinName;
                                return true;
                            }
                            else if (Spine.HasAnimation(StateName))
                            {
                                Spine.playing = true;
                                Spine.loop = loop;
                                Spine.speed = speed;
                                Spine.AnimationName = StateName;
                                return true;
                            }
                        }
//                         else
//                         {
//                             Spine.playing = false;
//                         }
                    }
                    if (go.TryGetComponent<Animator>(out var Animator))
                    {
                        if (!string.IsNullOrEmpty(StateName) && Animator.HasState(0, Animator.StringToHash(StateName)))
                        {
                            Animator.enabled = true;
                            Animator.speed = speed;
                            try
                            {
                                if (NormalizeTime > 0)
                                {
                                    Animator.CrossFade(StateName, NormalizeTime);
                                }
                                else
                                {
                                    Animator.Play(StateName);
                                }
                                return true;
                            }
                            catch (System.Exception err)
                            {
                                Debug.LogWarning($"{StateName} : {err.Message}");
                            }
                        }
                        else
                        {
                            Animator.enabled = false;
                        }
                    }
                    if (go.TryGetComponent<Animation>(out var Animation))
                    {
                        if (!string.IsNullOrEmpty(StateName) && Animation[StateName] is AnimationState st)
                        {
                            try
                            {
                                Animation.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
                                if (NormalizeTime > 0)
                                {
                                    Animation.Play(StateName);
                                    Animation.CrossFade(StateName, NormalizeTime);
                                }
                                else
                                {
                                    Animation.Play(StateName);
                                }
                                st.speed = speed;
                                return true;
                            }
                            catch (System.Exception err)
                            {
                                Debug.LogWarning($"{StateName} : {err.Message}");
                            }
                        }
                        else
                        {
                            Animation.Stop();
                        }
                    }
                }
            }
            catch (System.Exception err)
            {
                Debug.LogError(err);
            }
            return false;
        }
    }

    //-----------------------------------------------------------------------------------------------------------------
    

    //-----------------------------------------------------------------------------------------------------
}
