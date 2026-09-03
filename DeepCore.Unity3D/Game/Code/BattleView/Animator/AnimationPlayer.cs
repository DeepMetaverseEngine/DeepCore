using System;
using Code.System.Resource;
using DeepCore;
using UnityEngine;

namespace Code.BattleView;

public class DisplayNode
{
    protected HashMap<string, DisplayNode> DisplayNodes = new();
    private GameObject gameObject;
    private WrapGO WrapGo;
    private Animator Animator;
    
    public void CrossFade(string name, float duration, int layer = 0, float normalized = 0)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError($"[{nameof(DisplayNode)}] State name got null value!");
            return;
        }

        if (!Animator)
            return;
        
        if (Animator.HasState(layer, Animator.StringToHash(name)))
        {
            Animator.CrossFade(name, duration, layer, normalized);
        }
    }

    public float GetDuration(string name)
    {
        float ret = 0f;
        
        if (string.IsNullOrEmpty(name) 
            || Animator == null 
            || Animator.runtimeAnimatorController == null)
            return ret;

        var clips = Animator.runtimeAnimatorController.animationClips;
        if (clips == null)
            return ret;
        
        foreach (var clip in clips)
        {
            if (clip.name.Equals(name))
            {
                ret = clip.length;
                break;
            }
        }
        
        return ret;
    }


    public bool IsPlayOver(string name)
    {
        if (Animator == null)
            return false;
        
        var info = Animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(name) && info.normalizedTime >= 1;
    }


    public void Play(string name, int layer = 0, float normalized = 0)
    {
        if(string.IsNullOrEmpty(name) || !Animator)
            return;

        if (Animator.HasState(layer, Animator.StringToHash(name)))
        {
            Animator.Play(name, layer, normalized);
        }
        else
        {
            
        }
    }
    
}


public class AnimationPlayer
{
    
}