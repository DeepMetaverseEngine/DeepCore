
using System;
using DeepCore.GameData.Zone;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace Code.BattleView.MaterialActions
{
    public class DamageAction : MaterialAction
    {
        private UnitDamageEvent EventData;
        private GameObject Owner;
        private Transform OwnerTrans;
        private Vector3 StartRotation;
        private Vector3 StartScale;
        private float passtimeMS;
        
        protected override void OnUpdate(int deltaMS)
        {
            if (EventData == null)
            {
                IsDone = true;
                return;
            }

            IsDone = Math.Abs(passtimeMS - EventData.DamageTimeMS) < 100f;
            passtimeMS += deltaMS;
            if (EventData.HasFly)
            {
                PlayDamageFly();
            }
            else
            {
                PlayDamage();
            }
        }

        private readonly Vector3 Scale = new (0.7f, 1.1f);
        
        private void PlayDamage()
        {
            if (Vector3.Distance(OwnerTrans.localScale, Scale) <= 0.01)
            {
                OwnerTrans.localScale = Vector3.Lerp(OwnerTrans.localScale, StartScale, Time.deltaTime * 2);
                if (Vector3.Distance(OwnerTrans.localScale, StartScale) <= 0.01f)
                {
                    IsDone = true;
                }
            }
            else
            {
                OwnerTrans.localScale = Vector3.Lerp(OwnerTrans.localScale, Scale, Time.deltaTime);
            }
        }

        private void PlayDamageFly()
        {
            OwnerTrans.Rotate(new Vector3(0, 0, 2*Mathf.PI));
        }

        protected override void Disposing()
        {
            OwnerTrans.localScale = StartScale;
            OwnerTrans.rotation = Quaternion.Euler(StartRotation);
            Owner = null;
            OwnerTrans = null;
            EventData = null;
        }

        protected override void OnClear()
        {
            
        }

        public DamageAction Init(GameObject go, UnitDamageEvent ev)
        {
            EventData = ev;
            // var go = .gameObject;
            
            OwnerTrans = go.transform.GetChild(0);
            Owner = OwnerTrans.gameObject;
            
            StartRotation = OwnerTrans.localRotation.eulerAngles;
            StartScale = OwnerTrans.localScale;
            return this;
        }
        
        
        
    }
}