using SkillIndicator.Basic;
using UnityEngine;

namespace Utils
{
    public partial class Util
    {
        public static void Resize(Projector projector, float scale)
        {
            if (projector) projector.orthographicSize = scale / 2;
        }

        public static void Resize(Projector projector, ScalingType type, float scale, float width)
        {
            if (projector == null) 
                return;

            switch (type)
            {
                default:
                case ScalingType.LengthAndHeight:
                    projector.aspectRatio = 1f;
                    break;
                case ScalingType.LengthOnly:
                    projector.aspectRatio = width / scale;
                    break;
                case ScalingType.None:
                    return;
            }

            projector.orthographicSize = scale / 2;
        }

        public static void Resize(Projector[] projector, ScalingType scaling, float scale, float width)
        {
            foreach (var p in projector) 
                Resize(p, scaling, scale, width);
        }
        
        
        
        public static void Resize(Projector[] projector, float scale)
        {
            foreach (var p in projector) 
                Resize(p, scale);
        }
        
        
        
    }
}