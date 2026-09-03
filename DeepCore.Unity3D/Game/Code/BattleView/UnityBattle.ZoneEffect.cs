using System;
using Code.System.Resource;
using Code.System.Tick;
using DeepCore.GameData.Zone;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DeepCore.IO;

namespace Code.BattleView
{
    public partial class UnityBattle
    {
        private HashSet<long> _effectSerials = new HashSet<long>();

        public virtual void PlayZoneEffect(AddEffectEvent ev)
        {
            var eff = ev.effect;
            if (ev.effect == null) return;
            if (!string.IsNullOrEmpty(eff.SoundName))
            {
                // SoundManager.Instance.PlaySound(eff.SoundName, eff.EffectTimeMS, pos, eff.IsLoop);
            }
            //特效
            var url = eff.Name;
            if (!string.IsNullOrEmpty(eff.Name))
            {
                var name = Resource.GetFileNameWithoutExtension(url);
                var duration = eff.IsLoop ? 999999 : 1000;
                var wrap = ResourceSystem.GetWrapGO(url, name, null, EffectsNode.transform);
                wrap.Transform.localPosition = BattleToUnityPosition(ev.pos);
                wrap.Transform.localRotation = BattleToUnityRotation(ev.direction);
                wrap.Transform.localScale = ev.effect.ScaleToBodySize * Vector3.one;
                TickSystem.Tick(duration, (serial, index) =>
                {
                    wrap.Dispose();
                });
            }
        }
        public long AddEffect(string file, int durationMS, float scale, in Vector3 position,
            in Quaternion rotation)
        {
            var serial = 0L;
            if (!string.IsNullOrEmpty(file))
            {
                var name = Resource.GetFileNameWithoutExtension(file);
                try
                {
                    var wrap = ResourceSystem.GetWrapGO(file, name, null, EffectsNode.transform);
                    if (wrap != null && wrap.GameObject)
                    {
                        wrap.Transform.localPosition = position;
                        wrap.Transform.localRotation = rotation;
                        wrap.Transform.localScale *=  scale;
                        serial = TickSystem.Tick(Mathf.Max(1f, durationMS / 1000f), (s, index) =>
                        {
                            wrap.Dispose();
                        });
                        _effectSerials.Add(serial);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"!!! {file} ===== {e}");
                }
                
            }

            return serial;
        }

        public void RemoveEffect(long serial)
        {
            _effectSerials.Remove(serial);
            TickSystem.TickCancel(serial);
        }
    }
}
