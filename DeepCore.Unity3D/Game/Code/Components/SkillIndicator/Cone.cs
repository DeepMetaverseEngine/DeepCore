using System.Collections;
using IOGame.Client.Unity.Code.Utility;
using IOGame.Core.Battle.Data;
using SkillIndicator.Basic;
using UnityEngine;

namespace SkillIndicator
{
    public class Cone : Basic.SpellIndicator
    {
        [SerializeField, Range(0, 1)] private float mAngle;
        [SerializeField] private static float Anim_Speed = Time.deltaTime;
        public Projector LEdge, REdge;
        public override ScalingType ScalingType => ScalingType.LengthAndHeight;
        public override IOSkillPreWarning.PreWarningType IndicatorType => IOSkillPreWarning.PreWarningType.Sector;

        public float Angle
        {
            get => mAngle;
            set
            {
                mAngle = value;
                OnAngleChanged();
            }
        }

        public override void OnShow()
        {
            base.OnShow();
            StartCoroutine(FadeIn());
        }

        public override void OnHide()
        {
            base.OnHide();
            StopCoroutine(FadeIn());
        }

        public override void OnValueChanged()
        {
            base.OnValueChanged();
            OnAngleChanged();
        }


        private void OnAngleChanged()
        {
            ModifyShader("_Expand", Util.NormalizeClamp(mAngle - 1, 360));
            LEdge.transform.localEulerAngles = new Vector3(0, 0, (Angle + 2) / 2);
            REdge.transform.localEulerAngles = new Vector3(0, 0, -(Angle + 2) / 2);
        }
        
        
        private IEnumerator FadeIn()
        {
            float finale = Angle;
            float current = 0f;

            foreach (var projector in mProjector) 
                projector.enabled = true;
            
            while (current < finale)
            {
                Angle = current;
                current += finale * Anim_Speed;
                yield return null;
            }

            Angle = finale;
            yield return null;
        }
        
        

    }
}