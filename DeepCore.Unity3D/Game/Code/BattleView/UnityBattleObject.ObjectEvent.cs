using System;
using Code.BattleView.MaterialActions;
using Code.System;
using DeepCore;
using DeepCore.GameData.Zone;
using DeepGame3D.Unity.BattleView;
using IOGame.Core.Battle.Data;

namespace Code.BattleView
{
    public partial class UnityBattleObject
    {
        private HashMap<Type, Action<ObjectEvent>> _objectEvens = new HashMap<Type, Action<ObjectEvent>>();

        public void DoObjectEvent(ObjectEvent ev)
        {
            if (_objectEvens.TryGetValue(ev.GetType(), out var action))
            {
                action(ev);
            }
        }

        protected virtual void RegistAllObjectEvent()
        {
            RegistObjectEvent<UnitEffectEvent>(ObjectEvent_UnitEffectEvent);
            RegistObjectEvent<UnitHitEvent>(ObjectEvent_UnitHitEvent);
            RegistObjectEvent<UnitDeadEvent>(ObjectEvent_UnitDeadEvent);
            RegistObjectEvent<UnitChantSkillEvent>(ObjectEvent_UnitChantSkillEvent);
        }

        public void RegistObjectEvent<T>(Action<T> action) where T :ObjectEvent
        {
            Type type = typeof(T);
            Action<ObjectEvent> outVal = null;
            if (!_objectEvens.TryGetValue(type, out outVal))
            {
                _objectEvens.Add(type, (e) =>
                {
                    action((T)e);
                });
            }
        }

        protected virtual void ObjectEvent_UnitDeadEvent(UnitDeadEvent ev)
        {
            var action = System.Pool.ObjectPool<DissolveAction>.Get();
            action.Init(GameObject, 1000);
            DoMaterialAction(action, false);
        }

        protected virtual void ObjectEvent_UnitHitEvent(UnitHitEvent ev)
        {
            var action = System.Pool.ObjectPool<HitBlinAction>.Get();
            action.Init(GameObject);
            DoMaterialAction(action, false);

            LaunchEffect(ev.effect);
        }

        protected virtual void ObjectEvent_UnitChantSkillEvent(UnitChantSkillEvent ev)
        {
            var action = System.Pool.ObjectPool<ChantSkillAction>.Get();
            DoMaterialAction(action.Init(GameObject, ev.chant_ms), false);
        }

        private void ObjectEvent_UnitDamageEvent(UnitDamageEvent ev)
        {
            if (GameObject.transform.childCount > 0)
            {
                var action = System.Pool.ObjectPool<DamageAction>.Get();
                DoMaterialAction(action.Init(GameObject, ev), false);
            }
        }

        protected virtual void ObjectEvent_UnitEffectEvent(UnitEffectEvent ev)
        {
            LaunchEffect(ev.effect);
        }
        public long LaunchEffect(LaunchEffect effect)
        {
            var serial = 0L;
            
            if (effect != null)
            {
                var offset = UnityEngine.Vector3.zero;
                int dir = ZoneObject.Direction > 0 ? 1 : -1;
                if (effect.Properties is IOEffectProperties prop)
                {
                    offset.x += prop.OffsetX * dir;
                    offset.y += prop.OffsetY;
                    offset.z += prop.OffsetZ;
                }
            
                if (!string.IsNullOrEmpty(effect.SoundName))
                {
                    AudioManager.Instance.Play(effect.SoundName, effect.EffectTimeMS);
                }
                if (effect.BindBody)
                {
                    serial = AddEffect(effect.Name, effect.BindPartName, effect.EffectTimeMS, effect.ScaleToBodySize);
                }
                else
                {
                    serial = Battle.AddEffect(effect.Name, effect.EffectTimeMS, effect.ScaleToBodySize, ZoneObject.ToUnityPosition() + offset, ZoneObject.ToUnityRotation());
                }
            }

            return serial;
        }
    }
}
