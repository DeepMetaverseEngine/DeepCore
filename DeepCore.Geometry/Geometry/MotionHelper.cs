using DeepCore.Geometry;
using DeepCore.XCSV;
using System;

namespace DeepCore.Geometry
{
    public static class MotionHelper
    {
        /// <summary>
        /// 更新速度
        /// </summary>
        public static void UpdateSpeed(float intervalMS, ref float speedSec, float addSec, float accSec)
        {
            float add = 0;
            float acc = 0;
            if (addSec != 0)
            {
                add = GetDistance(intervalMS, addSec);
            }
            if (accSec != 0)
            {
                acc = GetDistance(intervalMS, speedSec * accSec);
            }
            speedSec += (add + acc);
        }

        /// <summary>
        /// 更新速度
        /// </summary>
        public static void UpdateSpeed(float intervalMS, ref float speedSec, float addSec, float accSec, float speedMin, float speedMax)
        {
            float add = 0;
            float acc = 0;
            if (addSec != 0)
            {
                add = GetDistance(intervalMS, addSec);
            }
            if (accSec != 0)
            {
                acc = GetDistance(intervalMS, speedSec * accSec);
            }
            speedSec += (add + acc);
            if (speedSec > speedMax) { speedSec = speedMax; }
            if (speedSec < speedMin) { speedSec = speedMin; }
        }


        //------------------------------------------------------------------------------------

        public static float GetDistance(float timeMS, float speedSEC)
        {
            return speedSEC * timeMS / 1000f;
        }
        public static double GetDistance(double timeMS, double speedSEC)
        {
            return speedSEC * timeMS / 1000f;
        }

        /// <summary>
        /// 转身算法
        /// </summary>
        /// <param name="dstD">目标角度</param>
        /// <param name="curD">当前角度</param>
        /// <param name="turnSpeedSEC">角速度</param>
        /// <param name="intervalMS">时间间隔</param>
        /// <returns></returns>
        public static float DirectionChange(float dstD, float curD, float turnSpeedSEC, float intervalMS)
        {
            if (turnSpeedSEC > 0)
            {
                float delta = turnSpeedSEC * intervalMS / 1000f;
                float dd = CMath.OpitimizeRadians(dstD) + CMath.PI_MUL_2;
                float cd = CMath.OpitimizeRadians(curD) + CMath.PI_MUL_2;
                float distanceL = Math.Abs(dd - cd);
                float distanceR = CMath.PI_MUL_2 - distanceL;
                if (distanceL > distanceR)
                {
                    if (distanceR <= delta)
                    {
                        return dstD;
                    }
                    if (dd > cd)
                    {
                        curD -= delta;
                    }
                    else
                    {
                        curD += delta;
                    }
                }
                else
                {
                    if (distanceL <= delta)
                    {
                        return dstD;
                    }
                    if (dd > cd)
                    {
                        curD += delta;
                    }
                    else
                    {
                        curD -= delta;
                    }
                }
                return curD;
            }
            return dstD;
        }

        /// <summary>
        /// 导弹一样飞向目标，并自动调整方向
        /// </summary>
        /// <param name="sx"></param>
        /// <param name="sy"></param>
        /// <param name="direction"></param>
        /// <param name="targetX"></param>
        /// <param name="targetY"></param>
        /// <param name="speedSEC"></param>
        /// <param name="turnSpeedSEC"></param>
        /// <param name="intervalMS"></param>
        public static void MoveToTargetTunning(ref Vector2 src, ref float direction, in Vector2 target, float speedSEC, float turnSpeedSEC, float intervalMS)
        {
            float dstD = VectorHelper.GetDegree(target.X - src.X, target.Y - src.Y);
            direction = DirectionChange(dstD, direction, turnSpeedSEC, intervalMS);
            VectorHelper.MovePolar(ref src, direction, speedSEC, intervalMS);
        }


        /// <summary>
        /// 预估击飞到落下的时间
        /// </summary>
        /// <param name="z">要计算的高度</param>
        /// <param name="z_speed">Z速度</param>
        /// <param name="z_limit">Z坐标上限</param>
        /// <param name="gravity">重力加速度</param>
        /// <param name="intervalMS">时间精度，越小越精确，一般10足够精度</param>
        /// <returns></returns>
        public static double CalculateFlyTimeMS(double z, double z_speed, double z_limit, double gravity, double intervalMS = 10)
        {
            double time = 0;
            double tick_g = GetDistance(intervalMS, gravity);
            do
            {
                time += intervalMS;
                double sd = GetDistance(intervalMS, z_speed);
                z += sd;
                if (z < 0)
                {
                    time -= (int)Math.Abs(intervalMS * (z / sd));
                }
                if (z_limit != 0 && z_speed > 0 && z > z_limit)
                {
                    z_speed = 0;
                }
                z_speed -= tick_g;
            }
            while (z > 0);
            return time;
        }

        /// <summary>
        /// 给定最大距离和总时间，计算当前抛物线高度
        /// y = ax2 + bx + c;
        /// </summary>
        /// <param name="maxHeight"></param>
        /// <param name="totalTimeMS"></param>
        /// <param name="currentTimeMS"></param>
        /// <returns></returns>
        public static double CalulateParabolicHeight(double maxHeight, double totalTimeMS, double currentTimeMS)
        {
            var pct = currentTimeMS / totalTimeMS;
            // 用正弦函数模拟 //
            double y = (Math.Sin(pct * CMath.PI_F) * maxHeight);
            return y;
        }

        /*
        /// <summary>
        /// 计算单位击退方向
        /// </summary>
        /// <param name="damage">受击者</param>
        /// <param name="attacker">攻击者</param>
        /// <param name="mtype"></param>
        /// <returns></returns>
        public static float CalculateHitMoveDirection(IZoneObject damage, IZoneObject attacker, Zone.AttackProp.HitMoveType mtype)
        {
            var apos = attacker.Position;
            var dpos = damage.Position;
            switch (mtype)
            {
                case Zone.AttackProp.HitMoveType.BySenderPosition:
                    return VectorHelper.GetDegree(apos.X, apos.Y, dpos.X, dpos.Y);
                case Zone.AttackProp.HitMoveType.BySenderDirection:
                    return attacker.Direction;
                case Zone.AttackProp.HitMoveType.BySenderLeftRight:
                    float fx = apos.X;
                    float fy = apos.Y;
                    VectorHelper.MovePolar(ref fx, ref fy, attacker.Direction, 10);
                    if (CMath.PointOnLine(apos.X, apos.Y, fx, fy, dpos.X, dpos.Y) == CMath.PointOnLineResult.Left)
                    {
                        return attacker.Direction - CMath.PI_DIV_2;
                    }
                    else
                    {
                        return attacker.Direction + CMath.PI_DIV_2;
                    }
                case Zone.AttackProp.HitMoveType.ToSenderCenter:
                case Zone.AttackProp.HitMoveType.ToSenderBodySize:
                    return attacker.Direction + CMath.PI_F;
            }
            return damage.Direction + CMath.PI_F;
        }
        /// <summary>
        /// 尝试从目标身边挤过去（沿着边界移动）
        /// </summary>
        /// <param name="u"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="touched"></param>
        /// <param name="addX"></param>
        /// <param name="addY"></param>
        /// <returns>True，可以挤过去</returns>
        public static bool PreMoveToBorder(IZonePositionObject u, ref float x, ref float y, IZoneObject touched, float addX, float addY)
        {
            var zone = touched.Parent;
            if (touched is IMoveableZoneObject)
            {
                var objT = touched as IMoveableZoneObject;
                var dirD = MathVector.getDegree(addX, addY);
                var dirS = MathVector.getDegree(x, y, touched.X, touched.Y);
                if (CMath.RadiansInRange(dirS, dirD - zone.ElasticAngle, zone.ElasticAngle * 2f) == false)
                {
                    var dir = MathVector.getDegree(touched.X, touched.Y, x + addX, y + addY);
                    var pos = new TVector2(touched.X, touched.Y);
                    MathVector.movePolar(ref pos.x, ref pos.y, dir, u.BodyBlockSize + objT.BodyBlockSize + zone.MinStep);
                    if (!u.TouchMap || !zone.TryTouchMap(u, pos.x, pos.y))
                    {
                        x = pos.x;
                        y = pos.y;
                        return true;
                    }
                }
            }
            else if (touched is IZoneFlag)
            {
                var objT = touched as IZoneFlag;
                if (objT.ZoneShape != null)
                {
                    var pos = new ShapePoint();
                    pos.x = x;
                    pos.y = y;
                    if (objT.ZoneShape.MoveToBorder(pos, addX, addY, zone.MinStep))
                    {
                        if (!u.TouchMap || !zone.TryTouchMap(u, pos.x, pos.y))
                        {
                            x = pos.x;
                            y = pos.y;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        */

        //------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 根据发射角度，求初速度
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="yOffset"></param>
        /// <param name="gravity"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double ProjectileLaunchSpeed(double distanceX, double yOffset, double gravity, double angle)
        {
            var speed = (distanceX
                * Math.Sqrt(gravity)
                * Math.Sqrt(1 / Math.Cos(angle)))
                / Math.Sqrt(2 * distanceX * Math.Sin(angle) + 2 * (-yOffset) * Math.Cos(angle));
            return speed;
        }
        public static double ProjectileLaunchSpeed(in Vector3 source, in Vector3 target, double gravity, double angle)
        {
            var distanceX = Vector2.Distance(source.XY, target.XY);
            var yOffset = target.Z - source.Z;
            return ProjectileLaunchSpeed(distanceX, yOffset, gravity, angle);
        }
        public static double ProjectileLaunchSpeed(in Vector3 source, in Vector3 target, double gravity, double angle, out Vector3 speedOut)
        {
            var distanceX = Vector2.Distance(source.XY, target.XY);
            var yOffset = target.Z - source.Z;
            var speed = ProjectileLaunchSpeed(distanceX, yOffset, gravity, angle);
            speedOut = new Vector3();
            speedOut.Z = (float)(Math.Sin(angle) * speed);
            speedOut.X = (float)(Math.Cos(angle) * speed);
            return speed;
        }
        /// <summary>
        /// 根据发射速度，求角度  
        /// 注意根号不能为负数，也就是初始速度不足以到达目的地
        /// 求解会得到两个角度，一般都用小的角度
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="distanceX"></param>
        /// <param name="yOffset"></param>
        /// <param name="gravity"></param>
        /// <param name="angle0"></param>
        /// <param name="angle1"></param>
        /// <returns></returns>
        public static bool ProjectileLaunchAngle(double speed, double distanceX, double yOffset, double gravity, out double angle0, out double angle1)
        {
            angle0 = angle1 = CMath.RADIANS_45;

            var speedSquared = speed * speed;
            var operandA = Math.Pow(speed, 4);
            var operandB = gravity * (gravity * (distanceX * distanceX) + (2 * yOffset * speedSquared));

            // Target is not in range
            if (operandB > operandA)
            {
                return false;
            }

            var root = Math.Sqrt(operandA - operandB);

            angle0 = Math.Atan((speedSquared + root) / (gravity * distanceX));
            angle1 = Math.Atan((speedSquared - root) / (gravity * distanceX));

            return true;
        }
        public static bool ProjectileLaunchAngle(in Vector3 source, in Vector3 target, double speed, double gravity, out double angle0, out double angle1)
        {
            var distanceX = Vector2.Distance(source.XY, target.XY);
            var yOffset = target.Z - source.Z;
            return ProjectileLaunchAngle(speed, distanceX, yOffset, gravity, out angle0, out angle1);
        }

        public static bool ProjectileLaunch(in Vector3 source, in Vector3 target, double speed, double gravity, bool upthrow, out Vector3 speedOut)
        {
            var yOffset = target.Z - source.Z;
            var distanceX = Vector2.Distance(source.XY, target.XY);
            var ret = Geometry.MotionHelper.ProjectileLaunchAngle(
               speed,
               distanceX,
               yOffset,
               gravity,
               out var angle0,
               out var angle1);
            var angle = upthrow ? Math.Max(angle0, angle1) : Math.Min(angle0, angle1);
            speedOut = new Vector3();
            speedOut.Z = (float)(Math.Sin(angle) * speed);
            speedOut.X = (float)(Math.Cos(angle) * speed);
            return ret;
        }



        public static double ProjectileTimeOfFlight(double speed, double angle, double yOffset, double gravity)
        {
            var ySpeed = speed * Math.Sin(angle);

            var time = (ySpeed + Math.Sqrt((ySpeed * ySpeed) + 2 * gravity * yOffset)) / gravity;

            return time;
        }

        public static Vector3 ProjectVectorOnPlane(in Vector3 planeNormal, in Vector3 vector)
        {

            return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
        }

        //------------------------------------------------------------------------------------------------------------
    }


}
