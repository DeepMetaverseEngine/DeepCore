using Code.Managers;
using IOGame.Core.Battle.Data;
using SkillIndicator.Basic;
using UnityEngine;

namespace SkillIndicator
{
    /// <summary>
    /// 指向型预警
    /// </summary>
    public class AngleMissile : Basic.SpellIndicator
    {
        public override ScalingType ScalingType => ScalingType.LengthAndHeight;
        public override IOSkillPreWarning.PreWarningType IndicatorType => IOSkillPreWarning.PreWarningType.Arrow;
        

        public override void OnUpdate(Vector3 input, float _)
        {
            if (input != Vector3.zero)
            {
                SkillIndicatorManager.Instance.transform.rotation = Quaternion.LookRotation(input);
            }
        }
        
        
        
    }
}