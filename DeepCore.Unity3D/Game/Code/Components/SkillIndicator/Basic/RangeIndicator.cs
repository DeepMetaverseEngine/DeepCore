using IOGame.Core.Battle.Data;
using UnityEngine;

namespace SkillIndicator.Basic
{
    public class RangeIndicator : Splat
    {
        public override ScalingType ScalingType => ScalingType.LengthAndHeight;
        public override IOSkillPreWarning.PreWarningType IndicatorType => IOSkillPreWarning.PreWarningType.Round;

        public float DefaultScale;

        public override void OnShow()
        {
            UpdateSize();
        }

        private void UpdateSize()
        {
            Scale = DefaultScale;
        }
    }
}