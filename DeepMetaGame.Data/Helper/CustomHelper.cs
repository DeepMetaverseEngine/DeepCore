using DeepCore.Geometry;
using DVector3 = DeepCore.Geometry.Vector3;

namespace DeepMetaGame.Data.Helper
{
    public static class CustomHelper
    {
        public static Quaternion ToRotation(float direction)
        {
            float degree = MathHelper.ToDegrees(direction) + 90;
            return Quaternion.CreateFromAxisAngle(DVector3.Up, MathHelper.ToRadians(degree));
        }
    }
}