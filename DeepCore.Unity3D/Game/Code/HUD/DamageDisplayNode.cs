using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Code.HUD;
public class Gradient
{
    public Color Top;
    public Color Bottom;
}

[Serializable]
public class DamageConfig
{
    public Gradient Gradient;
    public float Scale;
    public int FontSize;
}
public class DamageDisplayNode : MonoBehaviour, IDamageDisplay
{
    protected const string damage_formater = "-#;+#;0";
    [SerializeField, Range(0, 3)] protected float mDuration = 1f;
    protected DamageDisplayAminType mAminType;

    public Action OnAnimPlayComplete { get; set; }

    public void Play(float damage, DamageDisplayAminType display)
    {
        mAminType = display;
        SetDamage(damage);
        PlayAnim();
    }

    protected virtual void SetDamage(float damage) { }

    protected virtual void PlayAnim() { }
    
    protected virtual void OnComplete() { OnAnimPlayComplete?.Invoke(); }

    protected virtual void Hide()
    {
        gameObject.SetActive(false);
        OnComplete();
    }
    
}