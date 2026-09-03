using DeepCore;
using DeepCore.Unity;
using DeepCore.XCSV;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Unity;
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.BattleView.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepGame3D.Unity.BattleView
{
    partial class UnityZone
    {
        private RecycleLinkList<IAutoUpdateRecyclable> updateables = new RecycleLinkList<IAutoUpdateRecyclable>();
        public void AddUpdate(IAutoUpdateRecyclable updateable)
        {
            updateables.AddLast(updateable);
        }
        private void UpdateUpdateables()
        {
            for (var it = updateables.First; it != null; it = it.Next)
            {
                it.Value.Update();
            }
            for (var it = updateables.First; it != null;)
            {
                if (it.Value.IsDone)
                {
                    it.Value.Dispose();
                    it = updateables.Remove(it);
                    continue;
                }
                else
                {
                    it = it.Next;
                }
            }
        }
        private void ClearUpdateables()
        {
            for (var it = updateables.First; it != null; it = it.Next)
            {
                it.Value.Dispose();
            }
            updateables.Clear();
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public virtual UnityEffectPlay LoadEffect(LaunchEffect effect)
        {
            if (IsDisposing) return null;
            var file = effect.Name;
            if (effect.SubEffects != null)
            {
                foreach (var subeffect in effect.SubEffects)
                {
                    LoadEffect(subeffect);
                }
            }
            if (!string.IsNullOrEmpty(file))
            {
                return UnityEffectPlay.Alloc(this, file, childEffectsNode.transform, null, effect);
            }
            return null;
        }
        public virtual UnityEffectPlay LoadEffect(string file)
        {
            if (IsDisposing) return null;
            if (!string.IsNullOrEmpty(file))
            {
                return UnityEffectPlay.Alloc(this, file, childEffectsNode.transform);
            }
            return null;
        }

        //----------------------------------------------------------------------------------------------------------------------------

        public virtual void PlayZoneEffect(AddEffectEvent ev)
        {
            var eff = ev.effect;
            PlayEffect(eff, ev.pos, ev.direction);
        }

        public virtual UnityEffectPlay PlayEffect(LaunchEffect effect, DeepCore.Geometry.Vector3 _pos, float _direction)
        {
            if (IsDisposing) return null;
            if (effect == null) return null;
            var eff = effect;
            if (effect.SubEffects != null)
            {
                foreach (var subeffect in effect.SubEffects)
                {
                    PlayEffect(subeffect, _pos, _direction);
                }
            }
            //特效
            if (!string.IsNullOrEmpty(eff.Name))
            {
                var res = UnityEffectPlay.Alloc(this, eff.Name, childEffectsNode.transform, null, effect);
                {
                    res.transform.position = this.BattleToUnityWorldPosition(_pos);
                    res.transform.rotation = this.BattleToUnityRotation(_direction);
                    res.Play(effect);
                }
                return res;
            }
            return null;
        }

        public virtual UnityEffectPlay PlayEffect(string file, in DeepCore.Geometry.Vector3 position, in float dir, float scale = 1f, int durationMS = 0)
        {
            if (IsDisposing) return null;
            if (!string.IsNullOrEmpty(file))
            {
                var res = UnityEffectPlay.Alloc(this, file, childEffectsNode.transform);
                {
                    res.transform.position = this.BattleToUnityWorldPosition(position);
                    res.transform.rotation = this.BattleToUnityRotation(dir);
                    res.transform.localScale = Vector3.one * scale;
                    res.Play(durationMS);
                }
                return res;
            }
            return null;
        }

        //----------------------------------------------------------------------------------------------------------------------------
        public virtual UnityEffectPlay PlayObjectEffect(UnityLayerObject owner, LaunchEffect effect)
        {
            if (IsDisposing) return null;
            var file = effect.Name;
            if (effect.SubEffects != null)
            {
                foreach (var subeffect in effect.SubEffects)
                {
                    PlayObjectEffect(owner, subeffect);
                }
            }
            if (!string.IsNullOrEmpty(file))
            {
                var res = UnityEffectPlay.Alloc(this, file, effect.BindBody ? owner.transform : childEffectsNode.transform, owner, effect);
                {
                    res.Play(effect);
                }
                return res;
            }
            return null;
        }
        public virtual UnityEffectPlay PlayObjectEffect(UnityLayerObject owner, string file, string bindPart, float scale = 1f, int durationMS = 0)
        {
            if (IsDisposing) return null;
            if (!string.IsNullOrEmpty(file))
            {
                var part = owner.transform.FindDeep(bindPart);
                if (!part)
                {
                    part = owner.transform;
                }
                var res = UnityEffectPlay.Alloc(this, file, part, owner);
                {
                    res.transform.localScale = Vector3.one * scale;
                    res.Play(durationMS);
                }
                return res;
            }
            return null;
        }


        //----------------------------------------------------------------------------------------------------------------------------
        public virtual UnityEffectPlay BindObjectEffect(UnityLayerObject owner, LaunchEffect effect)
        {
            if (IsDisposing) return null;
            var file = effect.Name;
            if (effect.SubEffects != null)
            {
                foreach (var subeffect in effect.SubEffects)
                {
                    BindObjectEffect(owner, subeffect);
                }
            }
            if (!string.IsNullOrEmpty(file))
            {
                var res = UnityEffectPlay.Alloc(this, file, owner.transform, owner, effect, true);
                {
                    res.StartBind(owner);
                }
                return res;
            }
            return null;
        }

        public virtual UnityEffectPlay BindObjectEffect(UnityLayerObject owner, string file, string bindPart, float scale = 1f)
        {
            if (IsDisposing) return null;
            if (!string.IsNullOrEmpty(file))
            {
                var part = owner.transform.FindDeep(bindPart);
                if (!part)
                {
                    part = owner.transform;
                }
                var res = UnityEffectPlay.Alloc(this, file, part, owner);
                {
                    res.transform.localScale = Vector3.one * scale;
                    res.StartBind(owner);
                }
                return res;
            }
            return null;
        }

        //----------------------------------------------------------------------------------------------------------------------------

        public virtual UnityTextPlay PlayObjectText(UnityZoneObject target, object value, object status = null, UnityEngine.Color? color = null, float? size = null)
        {
            if (value == null) return null;
            if (HUD.SHOW_DAMAGE_TEXT && config.GameCamera)
            {
                if (config.GameCamera.IsInCamera(target.transform))
                {
                    var ret = UnityTextPlay.Alloc(this)?.Init(target, value, status, color, size);
                    return ret;
                }
                //                 var bar = UnityBattleFactory.Instance.CreateHUDDamageText(target.transform);
                //                 if (bar != null)
                //                 {
                //                     bar.text = text;
                //                     if (size.HasValue)
                //                     {
                //                         bar.size = size.Value;
                //                     }
                //                     if (color.HasValue)
                //                     {
                //                         bar.color = color.Value;
                //                     }
                //                 }
            }
            return null;
        }
        public virtual UnityTextPlay PlayObjectHitEvent(UnityZoneObject target, UnitHitArgs hit, object status = null)
        {
            if (HUD.SHOW_DAMAGE_TEXT && config.GameCamera)
            {
                if (config.GameCamera.IsInCamera(target.transform))
                {
                    var ret = UnityTextPlay.Alloc(this)?.InitHit(target, hit, status);
                    return ret;
                }
                //                 var bar = UnityBattleFactory.Instance.CreateHUDDamageText(target.transform);
                //                 if (bar != null)
                //                 {
                //                     bar.HitEvent = hit;
                //                     var ret = UnityTextPlay.Alloc(this, bar, target.transform);
                //                     return ret;
                //                 }
                //                 var reduce = -hit.hp;
                //                 var color = reduce > 0 ? UnityEngine.Color.green : UnityEngine.Color.white;
                //                 if (hit.effect != null && hit.effect.TextColor != 0)
                //                 {
                //                     ColorValueAttribute.ToARGB(hit.effect.TextColor, out color.a, out color.r, out color.g, out color.b);
                //                 }
                //                 var text = reduce > 0 ? $"{reduce}" : $"{-reduce}";
                //                 return PlayObjectText(target, text, color, hit.isCritical ? 32 : null);
            }
            return null;
        }
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------

    public interface IAutoUpdateRecyclable : IRecyclable, IPoolingObject
    {
        bool IsDone { get; }
        void Update();
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------

    public class UnityEffectPlay : UnityPoolingObject, IAutoUpdateRecyclable
    {
        new public UnityZone zone { get => parent; }
        public string File { get; private set; }
        public IEffectResourceObject res { get; private set; }
        public bool IsDone { get => is_done; }
        public float BindDurationMS { get; set; }
        public LaunchEffect BindEffect { get; set; }
        public bool ForceBind { get; set; } = false;
        public UnityLayerObject BindOwner { get; private set; }
        public bool IsBindOwner { get; private set; }

        private bool is_done = false;
        private float delayDestoryTimeMS;
        private IAssetLoadingTask assetLoading;
        private TimeTaskMS<UnityEffectPlay> timeTask;
        private UnityLayerObject bindingOwner;
        //-------------------------------------------------------------
        public event Action<UnityEffectPlay, IEffectResourceObject> OnResourceLoaded;

        public static UnityEffectPlay Alloc(UnityZone zone, string file, Transform parent, UnityLayerObject owner = null, LaunchEffect effect = null, bool forceBind = false)
        {
            var ret = zone.objectPool.AllocOrCreateAutoRelease((zone), static (z, t) => new UnityEffectPlay(z), zone);
            ret.gameObject.name = file;
            ret.File = file;
            ret.BindEffect = effect;
            ret.ForceBind = forceBind;
            ret.IsBindOwner = (forceBind || (effect != null && effect.BindBody));
            if (ret.IsBindOwner)
            {
                ret.BindOwner = owner;
                ret.BindOwner?.Retain();
                ret.BindOwner?.layerObject?.Retain();
            }
            {
                if (effect != null)
                {
                    ret.delayDestoryTimeMS = effect.DeadTimeMS;
                    if (!string.IsNullOrEmpty(effect.SoundName))
                    {
                        UnityBattleFactory.Audio.PlaySound(
                            ret.transform,
                            DeepMetaGame.Data.ResourceType.Sound_Effect,
                            effect.SoundName,
                            effect.IsLoop ? 0 : effect.EffectTimeMS);
                    }
                    if (owner != null)
                    {
                        if (ret.IsBindOwner)
                        {
                            ret.transform.SetParent(owner.transform, false);
                            ret.transform.localPosition = Vector3.zero;
                            ret.transform.localRotation = Quaternion.identity;
                            ret.transform.localScale = Vector3.one;
                        }
                        else
                        {
                            ret.gameObject.transform.SetParent(parent, false);
                            ret.transform.position = owner.transform.position;
                            ret.transform.rotation = owner.transform.rotation;
                            ret.transform.localScale = Vector3.one;
                        }
                        ret.transform.localPosition += zone.Space.BattleToUnityVoxelAnchorOffset(owner.layerObject.BodyHeight, effect.BodyVoxelAnchor);
                    }
                    else
                    {
                        ret.gameObject.transform.SetParent(parent, false);
                        ret.transform.localPosition = Vector3.zero;
                        ret.transform.localRotation = Quaternion.identity;
                        ret.transform.localScale = Vector3.one;
                    }
                    if (effect.ScaleToBodySize != 0)
                    {
                        ret.transform.localScale *= effect.ScaleToBodySize;
                    }
                }
                else
                {
                    ret.gameObject.transform.SetParent(parent, false);
                    ret.transform.localPosition = Vector3.zero;
                    ret.transform.localRotation = Quaternion.identity;
                    ret.transform.localScale = Vector3.one;
                }
                ret.gameObject.SetActive(true);
            }
            ret.is_done = false;
            zone.AddUpdate(ret);
            return ret;
        }
        private UnityEffectPlay(UnityZone zone) : base(zone) { }
        protected override void Disposing()
        {
            this.bindingOwner = null;
            this.timeTask?.Cancel();
            this.timeTask = null;
            this.assetLoading?.Dispose();
            this.assetLoading = null;
            if (IsBindOwner)
            {
                this.BindOwner?.Release();
                this.BindOwner?.layerObject?.Release();
            }
            if (this.gameObject && !this.gameObject.Equals(null))
            {
                this.gameObject.SetActive(false);
                this.gameObject.transform.SetParent(zone.childEffectsNode.transform, false);
            }
            this.OnResourceLoaded = null;
            this.res?.Dispose();
            this.is_done = true;
            this.File = null;
            this.res = null;
            this.BindDurationMS = 0;
            this.BindEffect = default;
            this.ForceBind = false;
            this.BindOwner = null;
            this.IsBindOwner = false;

        }
        private void Finish()
        {
            this.is_done = true;
            this.timeTask?.Cancel();
            this.timeTask = null;
            this.assetLoading?.Dispose();
            this.assetLoading = null;
        }
        private void LoadResource()
        {
            this.Retain();
            this.assetLoading = UnityBattleFactory.Resource.LoadEffectResource(this, static (effect, res, err) =>
            {
                try
                {
                    if (res != null)
                    {
                        if (effect.IsDisposing)
                        {
                            res.Dispose();
                            return;
                        }
                        if (effect.BindEffect != null)
                        {
                            if (!string.IsNullOrEmpty(effect.BindEffect.AnimName))
                            {
                                res.PlayAnim(effect.BindEffect);
                            }
                        }
                        effect.InvokeResourceLoaded(res);
                    }
                    else if (err != null)
                    {
                        Debug.LogError($"Init Effect Resource Error : {effect.File} : {err.Message}");
                    }
                }
                catch (Exception err2)
                {
                    Debug.LogError(err2);
                }
                finally
                {
                    effect.Release();
                }
            });
        }
        protected void InvokeResourceLoaded(IEffectResourceObject res)
        {
            this.res = res;
            //res.transform.gameObject.AddComponent<AnimReplayController>();
            //res.InitEffect(this.BindEffect, this.BindOwner, this.ForceBind);
            OnResourceLoaded?.Invoke(this, res);
        }
        public void Update()
        {
            if (this.bindingOwner != null)
            {
                if (this.bindingOwner.IsDisposing)
                {
                    this.Stop();
                    return;
                }
            }
            if (this.BindEffect != null && this.BindOwner != null)
            {
                var offset = this.BindOwner.Space.BattleToUnityVoxelAnchorOffset(this.BindOwner.layerObject.BodyHeight, BindEffect.BodyVoxelAnchor);
                if (this.BindEffect.BindingOffsetDistance != 0)
                {
                    DeepCore.Geometry.Vector3 offset2 = DeepCore.Geometry.VectorHelper.Polar(
                        BindOwner.layerObject.Direction + CMath.ToPI(this.BindEffect.BindingOffsetAngle360),
                        this.BindEffect.BindingOffsetDistance);
                    offset2.Z = this.BindEffect.BindingOffsetZ;
                    offset += BindOwner.zone.Space.BattleToUnityOffset(offset2);
                }
                else
                {
                    DeepCore.Geometry.Vector3 offset2 = new DeepCore.Geometry.Vector3(0, 0, this.BindEffect.BindingOffsetZ);
                    offset += BindOwner.zone.Space.BattleToUnityOffset(offset2);
                }
                if (this.transform.localPosition != offset)
                {
                    this.transform.localPosition = (offset);
                }
            }
            if (res != null)
            {
                res.UpdateResource(this);
            }
            if (assetLoading != null && assetLoading.IsRunning)
            {
                return;
            }
            else if (IsBindOwner && BindOwner != null && !BindOwner.IsActive)
            {
                this.Finish();
            }
        }
        protected override void OnDestory()
        {
        }
        private void InternalPlay(float durationMS)
        {
            if (res == null)
            {
                this.Finish();
                return;
            }
            try
            {
                if (durationMS > 0)
                {
                    this.timeTask = zone.layer.AddTimeDelayMS(this, durationMS, static (st, t) =>
                    {
                        st.Finish();
                    });
                }
                else if (durationMS < 0)
                {
                    // If bind owner not null
                }
                else if (res.TryGetEffectTimeMS(out var ms, out var loop))
                {
                    this.timeTask = zone.layer.AddTimeDelayMS(this, ms, static (st, t) =>
                    {
                        st.Finish();
                    });
                }
                else
                {
                    this.timeTask = zone.layer.AddTimeDelayMS(this, 1000, static (st, t) =>
                    {
                        st.Finish();
                    });
                }
            }
            catch (Exception ex)
            {
                ex.PrintStackTrace();
                this.Finish();
            }
        }
        private void InternalPlay(LaunchEffect effect)
        {
            if (res == null)
            {
                this.Finish();
                return;
            }
            try
            {
                if (effect.EffectTimeMS > 0)
                {
                    this.timeTask = zone.layer.AddTimeDelayMS(this, effect.EffectTimeMS, static (st, t) =>
                    {
                        st.Finish();
                    });
                }
                else if (effect.EffectTimeMS < 0)
                {
                    // If bind owner not null
                }
                else if (res.TryGetEffectTimeMS(out var ms, out var loop))
                {
                    this.timeTask = zone.layer.AddTimeDelayMS(this, ms, static (st, t) =>
                    {
                        st.Finish();
                    });
                }
                else
                {
                    this.timeTask = zone.layer.AddTimeDelayMS(this, 1000, static (st, t) =>
                    {
                        st.Finish();
                    });
                }
            }
            catch (Exception ex)
            {
                ex.PrintStackTrace();
                this.Finish();
            }
        }
        public void StartBind(UnityLayerObject owner)
        {
            this.bindingOwner = owner;
            LoadResource();
        }
        public void Play(float durationMS)
        {
            this.BindDurationMS = durationMS;
            this.OnResourceLoaded += static (e, res) =>
            {
                e.InternalPlay(e.BindDurationMS);
            };
            LoadResource();
        }
        public void Play(LaunchEffect effect)
        {
            this.BindEffect = effect;
            this.OnResourceLoaded += static (e, res) =>
            {
                e.InternalPlay(e.BindEffect);
            };
            LoadResource();
        }

        public void Stop()
        {
            if (zone.IsDisposing)
            {
                this.Finish();
            }
            else
            {
                var wrap = res;
                if (res == null) return;
                if (delayDestoryTimeMS > 0)
                {
                    this.gameObject.transform.SetParent(zone.childEffectsNode.transform, true);
                    this.timeTask = zone.layer.AddTimeDelayMS(this, delayDestoryTimeMS, static (st, t) =>
                    {
                        st.Finish();
                    });
                }
                //                 else if (wrap.TryGetEffectTimeMS(out var ms, out var loop))
                //                 {
                //                     this.gameObject.transform.SetParent(zone.childEffectsNode.transform, true);
                //                     zone.layer.AddTimeDelayMS(this, ms, static (st, t) =>
                //                     {
                //                         st.Finish();
                //                     });
                //                 }
                else
                {
                    this.Finish();
                }
            }
        }

    }

    //---------------------------------------------------------------------------------------------------------------------------------------------

    public class UnityTextPlay : Recyclable, IAutoUpdateRecyclable
    {
        public UnityZone Zone { get; private set; }
        public HUDDamageText Text { get; private set; }
        public bool IsDone { get; private set; } = false;
        public static UnityTextPlay Alloc(UnityZone zone)
        {
            var ret = zone.objectPool.AllocOrCreateAutoRelease(zone, static (z, p) => new UnityTextPlay(z));
            ret.Zone = zone;
            ret.IsDone = false;
            zone.AddUpdate(ret);
            return ret;
        }
        private UnityTextPlay(UnityZone zone)
        {
            this.Text = UnityBattleFactory.Instance.CreateHUDDamageText(zone);
        }
        public UnityTextPlay Init(UnityZoneObject target, object value, object status, UnityEngine.Color? color = null, float? size = null)
        {
            if (Text != null)
            {
                this.Text.status = status;
                this.Text.HitEvent = default;
                this.Text.value = value;
                this.Text.size = size;
                this.Text.color = color;
                this.Text.Start(target, target.transform);
                this.Zone.layer.AddTimeDelayMS(this, Text.timeMS, static (st, t) =>
                {
                    st.IsDone = true;
                });
                return this;
            }
            else
            {
                this.IsDone = true;
                return null;
            }
        }
        public UnityTextPlay InitHit(UnityZoneObject target, UnitHitArgs hit, object status)
        {
            if (Text != null)
            {
                this.Text.status = status;
                this.Text.HitEvent = hit;
                this.Text.value = null;
                this.Text.size = null;
                this.Text.color = null;
                this.Text.Start(target, target.transform);
                this.Zone.layer.AddTimeDelayMS(this, Text.timeMS, static (st, t) =>
                {
                    st.IsDone = true;
                });
                return this;
            }
            else
            {
                this.IsDone = true;
                return null;
            }
        }
        protected override void Disposing()
        {
            this.IsDone = true;
            this.Text?.Hide();
        }
        protected override void Destructing()
        {
            this.Text?.Release();
            this.Text = null;
        }
        public void Update()
        {
            this.Text?.Update();
        }
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------
}