using Code.Managers;
using IOGame.Core.Battle.Data;
using SkillIndicator.Basic;
using UnityEngine;

namespace SkillIndicator
{
    public class LineMissile : Basic.SpellIndicator
    {
        private float arrowScale;
        private Projector arrowProjector;

        [SerializeField] public GameObject Arrow;
        [SerializeField] public float MinimumRange;
        
        public override ScalingType ScalingType => ScalingType.LengthOnly;
        public override IOSkillPreWarning.PreWarningType IndicatorType => IOSkillPreWarning.PreWarningType.Line;
        
        public override void Init()
        {
            base.Init();
            arrowProjector = Arrow.GetComponent<Projector>();
            arrowScale = arrowProjector.orthographicSize;
        }

        public override void OnUpdate(Vector3 input, float range)
        {
            if (input != Vector3.zero)
            {
                SkillIndicatorManager.Instance.transform.rotation = Quaternion.LookRotation(input);
            }

            Scale = Mathf.Clamp(input.magnitude, MinimumRange, Range - ArrowDistance()) * 2;
            Arrow.transform.localPosition = new Vector3(0, Scale / 2f + ArrowDistance() - 0.12f, 0);
        }

        private float ArrowDistance()
        {
            return arrowProjector.orthographicSize * 0.96f;
        }

        public override void OnValueChanged()
        {
            base.OnValueChanged();
            arrowProjector.aspectRatio = 1f;
            arrowProjector.orthographicSize = arrowScale;
        }
    }
    
    
}