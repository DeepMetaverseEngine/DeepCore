using UnityEngine;

namespace Code.Utility { 

public partial class Util
{
    public static float NormalizeClamp(float portion, float max)
    {
        return Mathf.Clamp(portion / max, 0f, 1f);
    }
}
}