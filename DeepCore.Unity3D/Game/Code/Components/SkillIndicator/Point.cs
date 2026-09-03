using Code.Managers;
using IOGame.Core.Battle.Data;
using SkillIndicator.Basic;
using UnityEngine;

namespace SkillIndicator
{
    public class Point : Basic.SpellIndicator
    {
        public override ScalingType ScalingType => ScalingType.LengthAndHeight;
        public override IOSkillPreWarning.PreWarningType IndicatorType => IOSkillPreWarning.PreWarningType.Point;


        [SerializeField, InspectorName("RestrictToRange")] 
        protected bool mRestrictToRange = true;


        public override void OnUpdate(Vector3 input, float range)
        {
            base.OnUpdate(input, range);
#if UNITY_EDITOR || UNITY_STANDALONE
            UpdateProjectorPosition(input, range);
#endif
        }

        private void UpdateProjectorPosition(Vector3 pos, float inputRange)
        {
            
            if (Application.platform is RuntimePlatform.Android or RuntimePlatform.IPhonePlayer)
            {
                //射界与摇杆拖拽的比例
                var zoom = Range / inputRange;
                pos *= zoom;
            }
            
            transform.position = pos;
            if (mRestrictToRange) 
                RestrictToRange();
        }



        private void RestrictToRange()
        {
            var sub_pos = SkillIndicatorManager.Instance.transform.position;
            if (Vector3.Distance(sub_pos, transform.position) > Range)
            {
                transform.position = sub_pos + Vector3.ClampMagnitude(transform.position - sub_pos, Range);
            }
        }

    }
}