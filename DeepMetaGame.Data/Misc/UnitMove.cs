using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{

    [MessageType(BattleConstants.BlinkMove)]
    [Desc("闪现移动")]
    [Expandable]
    public class BlinkMove : ISNData
    {
        [Desc("移动距离", "移动")]
        public float Distance = 5;
        [Desc("弧度偏移", "移动")]
        public float DirectionOffset;
        [Desc("角度偏移", "移动")]
        public float DirectionOffset360
        {
            get => CMath.RadianToAngle(DirectionOffset);
            set { DirectionOffset = CMath.AngleToRadian(value); }
        }


        [Desc("移动方式", "移动")]
        public BlinkMoveType MType = BlinkMoveType.MoveToForward;

        [Desc("忽略地形", "碰撞")]
        public bool NoneTouchMap = false;
        [Desc("忽略单位", "碰撞")]
        public bool NoneTouchObj = true;

        [Desc("闪现起始点特效", "特效")]
        public LaunchEffect BeginEffect;
        [Desc("闪现目标点特效", "特效")]
        public LaunchEffect TargetEffect;

        public enum BlinkMoveType : byte
        {
            [Desc("向身前移动")]
            MoveToForward,
            [Desc("向身后移动")]
            MoveToBackward,
            [Desc("向技能释放目标点移动")]
            MoveToTargetPos,
            [Desc("向技能释放目标单位移动(面前)")]
            MoveToTargetUnitFace,
            [Desc("向技能释放目标单位移动(背后)")]
            MoveToTargetUnitBack,
        }
        public override string ToString()
        {
            return $"闪现移动：距离：{Distance}";
        }
    }


    /// <summary>
    /// 移动距离
    /// </summary>
    [MessageType(BattleConstants.StartMove)]
    [Desc("移动距离")]
    [Expandable]
    public class StartMove : ISNData
    {
        [Desc("速度(距离/每秒)", "位移")]
        public float SpeedSEC = 10f;
        [Desc("加速度(距离/每秒)(最终速度=速度+加速度)", "位移")]
        public float SpeedAdd = 0f;
        [Desc("阻力(每秒递减速度百分比)", "位移")]
        public float SpeedAcc = 0f;
        [Desc("被击飞的向上速度(距离/每秒)", "位移")]
        public float ZSpeedSEC = 0f;

        [Desc("重设重力（非0有效）", "位移")]
        public float OverrideGravity = 0f;
        [Desc("起始移动方向修正", "位移")]
        public float Direction;
        [Desc("起始移动方向修正", "位移")]
        public float Direction360
        {
            get => CMath.RadianToAngle(Direction);
            set { Direction = CMath.AngleToRadian(value); }
        }


        [Desc("移动过程中是否忽略碰撞", "位移")]
        public bool IsNoneTouch = false;

        [Desc("自身转动速度(弧度/每秒)", "旋转")]
        public float RotateSpeedSEC;
        [Desc("自身转动速度(角度/每秒)", "旋转")]
        public float RotateSpeedSEC360
        {
            get => CMath.RadianToAngle(RotateSpeedSEC);
            set { RotateSpeedSEC = CMath.AngleToRadian(value); }
        }



        [Desc("持续时间(毫秒)，如果是击飞此处无效，击飞时间按落地时间计算，运动时间不计算在DamageTime内", "时间")]
        public int KeepTimeMS = 500;
        [Desc("", "", false)]
        public float ArgsFactor = 0;

        public bool HasFly { get { return ZSpeedSEC != 0; } }
        public StartMove() { }
        public override string ToString()
        {
            return "移动速度:" + SpeedSEC + "(每秒) 持续:" + KeepTimeMS + "(毫秒)";
        }

    }

}
