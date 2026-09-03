using System;
using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.GameData.Zone;
using UnityEngine;

namespace Code.BattleView
{
    public partial class UnityBattle
    {
        private Action<UnityBattleActor> OnActorCreate;

        private HashMap<Type, Action<ZoneEvent>> _zoneEvens = new HashMap<Type, Action<ZoneEvent>>();
        

        protected virtual void RegistAllZoneEvent()
        {
            RegistZoneEvent<AddEffectEvent>(ZoneEvent_AddEffectEvent);
        }

        protected void RegistZoneEvent<T>(Action<T> action) where T :ZoneEvent
        {
            Type type = typeof(T);
            Action<ZoneEvent> outVal = null;
            if (!_zoneEvens.TryGetValue(type, out outVal))
            {
                _zoneEvens.Add(type, (e) =>
                {
                    action((T)e);
                });
            }
        }

        protected virtual void ZoneEvent_AddEffectEvent(AddEffectEvent ev)
        {
            PlayZoneEffect(ev);
        }

        private void Layer_GameOver(LayerZone layer, int winforce, string msg)
        {
            if (winforce == layer.Actor.Force)
            {
                Debug.Log("胜利!");
            }
        }
    }
}
