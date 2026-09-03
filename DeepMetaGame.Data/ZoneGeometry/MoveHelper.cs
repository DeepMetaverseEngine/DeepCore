using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Security.Claims;
using static DeepMetaGame.Data.ZoneGeometry.MoveHelper;

namespace DeepMetaGame.Data.ZoneGeometry
{
    //-----------------------------------------------------------------------------------------------------------------
    public enum FindPathResult : byte
    {
        /// <summary>
        /// 可通过
        /// </summary>
        Cross = 0,
        /// <summary>
        /// 没有路径
        /// </summary>
        NoWay = 1,
        /// <summary>
        /// 原地
        /// </summary>
        Destination = 2,
        /// <summary>
        /// 寻路范围超出地图范围
        /// </summary>
        OutOfMap = 3,
    }
    //-----------------------------------------------------------------------------------------------------------------
    //     public enum TryMoveToMapBorderResult : byte
    //     {
    //         ARRIVE = 0,
    //         TOUCH = 1,
    //         BLOCK = 2,
    //     }
    //-----------------------------------------------------------------------------------------------------------------
    public enum MoveResult : ushort
    {
        MOVE_SMOOTH = 0,


        /// <summary>
        /// 标志判断，到达目标
        /// </summary>
        MOVE_RESULT_ARRIVED = 0x01,
        /// <summary>
        /// 标志判断，被单位阻挡
        /// </summary>
        MOVE_RESULT_BLOCK_OBJ = 0x02,
        /// <summary>
        /// 标志判断，被地图阻挡
        /// </summary>
        MOVE_RESULT_BLOCK_MAP = 0x04,
        /// <summary>
        /// 标志判断，没有达到预期距离或者移动距离太小
        /// </summary>
        MOVE_RESULT_MIN_STEP = 0x08,



        /// <summary>
        /// 标志状态，本次移动任何方向接触过地图
        /// </summary>
        MOVE_RESULT_TOUCH_MAP_ALL = 0x10,
        /// <summary>
        /// 标志状态，触碰到单位
        /// </summary>
        MOVE_RESULT_TOUCH_OBJ = 0x20,
        /// <summary>
        /// 标志状态，阻挡的单位同意让开
        /// </summary>
        MOVE_RESULT_TOUCH_OBJ_GETAWAY = 0x40,
        /// <summary>
        /// 需要重新寻路
        /// </summary>
        MOVE_RESULT_RESET_PATH = 0x80,

        /// <summary>
        /// 标志状态，本次移动等一会
        /// </summary>
        MOVE_RESULT_HOLD = 0x100,
        /// <summary>
        /// 标志状态，无法移动
        /// </summary>
        MOVE_RESULT_NO_WAY = 0x200,


        /// <summary>
        /// 【标志判断组】，被任何东西阻挡
        /// </summary>
        RESULTS_BLOCK_ANY = MOVE_RESULT_BLOCK_OBJ | MOVE_RESULT_BLOCK_MAP,
        /// <summary>
        /// 【标志判断】，移动结束（被阻挡或者到达目标）
        /// </summary>
        RESULTS_MOVE_END = MOVE_RESULT_ARRIVED | MOVE_RESULT_BLOCK_OBJ | MOVE_RESULT_BLOCK_MAP | MOVE_RESULT_NO_WAY,
    }
    //-----------------------------------------------------------------------------------------------------------------
    //     public enum MoveImpactResult : byte
    //     {
    //         MOVE_SMOOTH = MoveResult.MOVE_SMOOTH,
    // 
    //         /// <summary>
    //         /// 标志状态，本次移动任何方向接触过地图
    //         /// </summary>
    //         MOVE_RESULT_TOUCH_MAP = MoveResult.MOVE_RESULT_TOUCH_MAP_ALL,
    //         /// <summary>
    //         /// 标志状态，阻挡的单位同意让开
    //         /// </summary>
    //         MOVE_RESULT_TOUCH_OBJ = MoveResult.MOVE_RESULT_TOUCH_OBJ_GETAWAY,
    // 
    // 
    //         /// <summary>
    //         /// 标志判断，被单位阻挡
    //         /// </summary>
    //         MOVE_RESULT_BLOCK_OBJ = MoveResult.MOVE_RESULT_BLOCK_OBJ,
    //         /// <summary>
    //         /// 标志判断，被地图阻挡
    //         /// </summary>
    //         MOVE_RESULT_BLOCK_MAP = MoveResult.MOVE_RESULT_BLOCK_MAP,
    //         /// <summary>
    //         /// 标志判断，没有达到预期距离或者移动距离太小
    //         /// </summary>
    //         MOVE_RESULT_MIN_STEP = MoveResult.MOVE_RESULT_MIN_STEP,
    // 
    //         /// <summary>
    //         /// 【标志判断组】，被任何东西阻挡
    //         /// </summary>
    //         RESULTS_CAN_NOT_MOVE = MOVE_RESULT_BLOCK_OBJ | MOVE_RESULT_BLOCK_MAP | MOVE_RESULT_MIN_STEP,
    //     }

    //-----------------------------------------------------------------------------------------------------------------


    public static class MoveHelper
    {
        /// <summary>
        /// 更新速度
        /// </summary>
        public static float UpdateSpeed(float intervalMS, float speedSec, float addSec, float accSec)
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
            speedSec += add + acc;
            return speedSec;
        }
        public static float UpdateSpeed(float intervalMS, float speedSec, float addSec, float accSec, float speedMin, float speedMax)
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
            speedSec += add + acc;
            if (speedSec > speedMax) { speedSec = speedMax; }
            if (speedSec < speedMin) { speedSec = speedMin; }

            return speedSec;
        }


        //------------------------------------------------------------------------------------

        public static float GetDistance(float timeMS, float speedSEC)
        {
            return speedSEC * timeMS / 1000f;
        }
        //         public static float GetDistance(float timeMS, float speedSEC)
        //         {
        //             return speedSEC * timeMS / 1000f;
        //         }

        /// <summary>
        /// 转身算法
        /// </summary>
        /// <param name="dstD">目标角度</param>
        /// <param name="curD">当前角度</param>
        /// <param name="turnSpeedSEC">角速度</param>
        /// <param name="intervalMS">时间间隔</param>
        /// <returns></returns>
        public static float DirectionChange(float curD, float dstD, float delta)
        {
            if (delta > 0)
            {
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
        public static float GetTurnSpeed(float turnSpeedSEC, float intervalMS)
        {
            return turnSpeedSEC * intervalMS / 1000f;
        }
        public static float DirectionChange(float curD, float dstD, float turnSpeedSEC, float intervalMS)
        {
            float deltaD = turnSpeedSEC * intervalMS / 1000f;
            return DirectionChange(curD, dstD, deltaD);
        }
        public static float DirectionChange(
            in DeepCore.Geometry.Vector2 src,
            float curD,
            in DeepCore.Geometry.Vector2 target,
            float turnSpeedSEC,
            float intervalMS)
        {
            if (src == target)
            {
                return curD;
            }
            var dstD = CMath.GetDegree(target.X - src.X, target.Y - src.Y);
            return DirectionChange(curD, dstD, turnSpeedSEC, intervalMS);
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
        //         public static bool MoveToTargetTunning(
        //             ref DeepCore.Geometry.Vector3 pos,
        //             ref float direction,
        //             in DeepCore.Geometry.Vector3 target,
        //             float speedSEC,
        //             float turnSpeedSEC,
        //             float intervalMS)
        //         {
        //             float step = GetDistance(intervalMS, speedSEC);
        //             float deltaD = turnSpeedSEC * intervalMS / 1000f;
        //             // pos.Z = Geometry.MathHelper.Lerp(pos.Z, target.Z, step);
        //             float dstD = MathVector.getDegree(target.X - pos.X, target.Y - pos.Y);
        //             direction = DirectionChange(direction, dstD, deltaD);
        //             var vt = pos;
        //             vt.Z = target.Z;
        //             DeepCore.Geometry.VectorHelper.MovePolar(ref vt, direction, step * 10);
        //             //Geometry.VectorHelper.Rotate(ref vt.X, ref vt.Y, pos.X, pos.Y, deltaD);
        //             return DeepCore.Geometry.VectorHelper.MoveTo3D(ref pos, in vt, step);
        //             // MathVector.movePolar(ref pos.X, ref pos.Y, direction, speedSEC, intervalMS);
        //         }
        public static bool MoveToTargetTunning(
            ref DeepCore.Geometry.Vector3 pos,
            ref float direction,
            in DeepCore.Geometry.Vector3 target,
            float speedSEC,
            float turnSpeedSEC,
            float intervalMS)
        {
            float step = GetDistance(intervalMS, speedSEC);
            float deltaD = turnSpeedSEC * intervalMS / 1000f;
            // pos.Z = Geometry.MathHelper.Lerp(pos.Z, target.Z, step);
            float dstD = MathVector.getDegree(target.X - pos.X, target.Y - pos.Y);
            direction = DirectionChange(direction, dstD, deltaD);
            var vt = pos;
            vt.Z = target.Z;
            DeepCore.Geometry.VectorHelper.MovePolar(ref vt, direction, step * 10);
            //Geometry.VectorHelper.Rotate(ref vt.X, ref vt.Y, pos.X, pos.Y, deltaD);
            return DeepCore.Geometry.VectorHelper.MoveTo3D(ref pos, in vt, step);
            // MathVector.movePolar(ref pos.X, ref pos.Y, direction, speedSEC, intervalMS);
        }
        public static float CalculateHitMoveDirection(
           DeepCore.Geometry.Vector3 damage,
           float damageDir,
           DeepCore.Geometry.Vector3 attacker,
           float attackerDir,
           AttackProp.HitMoveType mtype)
        {
            switch (mtype)
            {
                case AttackProp.HitMoveType.BySenderPosition:
                    return MathVector.getDegree(attacker.X, attacker.Y, damage.X, damage.Y);
                case AttackProp.HitMoveType.BySenderDirection:
                    return attackerDir;
                case AttackProp.HitMoveType.BySenderLeftRight:
                    float fx = attacker.X;
                    float fy = attacker.Y;
                    MathVector.movePolar(ref fx, ref fy, attackerDir, 10);
                    if (CMath.PointOnLine(attacker.X, attacker.Y, fx, fy, damage.X, damage.Y) == CMath.PointOnLineResult.Left)
                    {
                        return attackerDir - CMath.PI_DIV_2;
                    }
                    else
                    {
                        return attackerDir + CMath.PI_DIV_2;
                    }
                case AttackProp.HitMoveType.ToSenderCenter:
                case AttackProp.HitMoveType.ToSenderBodySize:
                    return attackerDir + CMath.PI_F;
            }
            return damageDir + CMath.PI_F;
        }

        public static bool CalculateSpellLaunchAngle(
            SpellTemplate mInfo,
            in DeepCore.Geometry.Vector3 mStartPos,
            in DeepCore.Geometry.Vector3 mTargetPos,
            float gravity,
            out float muzzleAngle,
            out float mLaunchDirection,
            out float mSpeedX,
            out float mSpeedZ)
        {
            var distance = DeepCore.Geometry.Vector2.Distance(mStartPos.XY, mTargetPos.XY);
            gravity = mInfo.MCannonGravitySEC > 0 ? mInfo.MCannonGravitySEC : gravity;
            mLaunchDirection = VectorHelper.GetDegree(mStartPos, mTargetPos);
            if (mInfo.MCannonThrow == SpellTemplate.CannonThrow.Expect45)
            {
                var angle = muzzleAngle = CMath.RADIANS_45;
                var speed = MotionHelper.ProjectileLaunchSpeed(
                    distance,
                    mTargetPos.Z - mStartPos.Z,
                    gravity,
                    angle);
                var outrange = speed > mInfo.MSpeedSEC;
                if (outrange)
                {
                    speed = mInfo.MSpeedSEC;
                }
                mSpeedZ = (float)(Math.Sin(angle) * speed);
                mSpeedX = (float)(Math.Cos(angle) * speed);
                return !outrange;
            }
            else
            {
                var result = MotionHelper.ProjectileLaunchAngle(
                   mInfo.MSpeedSEC,
                   distance,
                   mTargetPos.Z - mStartPos.Z,
                   gravity,
                   out var angle0,
                   out var angle1);
                var angle = (mInfo.MCannonThrow == SpellTemplate.CannonThrow.UpThrow) ?
                    Math.Max(angle0, angle1) :
                    Math.Min(angle0, angle1);
                muzzleAngle = (float)angle;
                mSpeedZ = (float)(Math.Sin(angle) * mInfo.MSpeedSEC);
                mSpeedX = (float)(Math.Cos(angle) * mInfo.MSpeedSEC);
                return result;
            }
        }

        /*
        /// <summary>
        /// 预估击飞到落下的时间
        /// </summary>
        /// <param name="z">要计算的高度</param>
        /// <param name="z_speed">Z速度</param>
        /// <param name="z_limit">Z坐标上限</param>
        /// <param name="gravity">重力加速度</param>
        /// <param name="intervalMS">时间精度，越小越精确，一般10足够精度</param>
        /// <returns></returns>
        public static int CalculateFlyTimeMS(float z, float z_speed, float z_limit, float gravity, int intervalMS = 10)
        {
            int time = 0;
            float tick_g = GetDistance(intervalMS, gravity);
            do
            {
                time += intervalMS;
                float sd = GetDistance(intervalMS, z_speed);
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
        public static float CalulateParabolicHeight(float maxHeight, int totalTimeMS, int currentTimeMS)
        {
            float pct = currentTimeMS / (float)totalTimeMS;
            // 用正弦函数模拟 //
            float y = (float)(Math.Sin(pct * CMath.PI_F) * maxHeight);
            return y;
        }
        */
        //         /// <summary>
        //         /// 计算单位击退方向
        //         /// </summary>
        //         /// <param name="damage">受击者</param>
        //         /// <param name="attacker">攻击者</param>
        //         /// <param name="mtype"></param>
        //         /// <returns></returns>
        //         public static float CalculateHitMoveDirection(IMoveableZoneObject damage, IMoveableZoneObject attacker, GameData.Zone.AttackProp.HitMoveType mtype)
        //         {
        //             switch (mtype)
        //             {
        //                 case GameData.Zone.AttackProp.HitMoveType.BySenderPosition:
        //                     return MathVector.getDegree(attacker.X, attacker.Y, damage.X, damage.Y);
        //                 case GameData.Zone.AttackProp.HitMoveType.BySenderDirection:
        //                     return attacker.Direction;
        //                 case GameData.Zone.AttackProp.HitMoveType.BySenderLeftRight:
        //                     float fx = attacker.X;
        //                     float fy = attacker.Y;
        //                     MathVector.movePolar(ref fx, ref fy, attacker.Direction, 10);
        //                     if (CMath.PointOnLine(attacker.X, attacker.Y, fx, fy, damage.X, damage.Y) == CMath.PointOnLineResult.Left)
        //                     {
        //                         return attacker.Direction - CMath.PI_DIV_2;
        //                     }
        //                     else
        //                     {
        //                         return attacker.Direction + CMath.PI_DIV_2;
        //                     }
        //                 case GameData.Zone.AttackProp.HitMoveType.ToSenderCenter:
        //                 case GameData.Zone.AttackProp.HitMoveType.ToSenderBodySize:
        //                     return attacker.Direction + CMath.PI_F;
        //             }
        //             return damage.Direction + CMath.PI_F;
        //         }
        // 
        //         /// <summary>
        //         /// 尝试从目标身边挤过去（沿着边界移动）
        //         /// </summary>
        //         /// <param name="u"></param>
        //         /// <param name="x"></param>
        //         /// <param name="y"></param>
        //         /// <param name="touched"></param>
        //         /// <param name="addX"></param>
        //         /// <param name="addY"></param>
        //         /// <returns>True，可以挤过去</returns>
        //         public static bool PreMoveToBorder(IMoveableZoneObject u, ref float x, ref float y, IZoneObject touched, float addX, float addY)
        //         {
        //             var zone = touched.Parent;
        //             if (touched is IMoveableZoneObject)
        //             {
        //                 var objT = touched as IMoveableZoneObject;
        //                 var dirD = MathVector.getDegree(addX, addY);
        //                 var dirS = MathVector.getDegree(x, y, touched.X, touched.Y);
        //                 if (CMath.RadiansInRange(dirS, dirD - zone.ElasticAngle, zone.ElasticAngle * 2f) == false)
        //                 {
        //                     var dir = MathVector.getDegree(touched.X, touched.Y, x + addX, y + addY);
        //                     var pos = new TVector2(touched.X, touched.Y);
        //                     MathVector.movePolar(ref pos.x, ref pos.y, dir, u.BodyBlockSize + objT.BodyBlockSize + zone.MinStep);
        //                     if (!u.TouchMap || !zone.TryTouchMap(u, pos.x, pos.y))
        //                     {
        //                         x = pos.x;
        //                         y = pos.y;
        //                         return true;
        //                     }
        //                 }
        //             }
        //             else if (touched is IZoneFlag)
        //             {
        //                 var objT = touched as IZoneFlag;
        //                 if (objT.ZoneShape != null)
        //                 {
        //                     var pos = new ShapePoint();
        //                     pos.x = x;
        //                     pos.y = y;
        //                     if (objT.ZoneShape.MoveToBorder(pos, addX, addY, zone.MinStep))
        //                     {
        //                         if (!u.TouchMap || !zone.TryTouchMap(u, pos.x, pos.y))
        //                         {
        //                             x = pos.x;
        //                             y = pos.y;
        //                             return true;
        //                         }
        //                     }
        //                 }
        //             }
        //             return false;
        //         }




    }

    //-----------------------------------------------------------------------------------------------------------------
    // 
    //     public enum FindPathResult : byte
    //     {
    //         /// <summary>
    //         /// 可通过
    //         /// </summary>
    //         Cross = 0,
    //         /// <summary>
    //         /// 没有路径
    //         /// </summary>
    //         NoWay = 1,
    //         /// <summary>
    //         /// 原地
    //         /// </summary>
    //         Destination = 2,
    //         /// <summary>
    //         /// 寻路范围超出地图范围
    //         /// </summary>
    //         OutOfMap = 3,
    //     }
    // 
    //     //-----------------------------------------------------------------------------------------------------------------
    // 
    //     public enum TryMoveToMapBorderResult : byte
    //     {
    //         ARRIVE = 0,
    //         TOUCH = 1,
    //         BLOCK = 2,
    //     }


}
