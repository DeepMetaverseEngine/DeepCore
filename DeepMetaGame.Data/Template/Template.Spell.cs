using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Template
{
    /// <summary>
    /// 法术/飞行道具模板
    /// </summary>
    [MessageType(BattleConstants.SpellTemplate)]
    [Desc("法术/飞行道具模板")]
    public class SpellTemplate : TemplateData
    {
        /// <summary>
        /// 生命周期，帧
        /// </summary>
        [Desc(Category = "1.Base", Desc = "生命周期(毫秒)，如果为0，则跟着绑定单位走")]
        public int LifeTimeMS = 1000;
        [Desc(Category = "1.Base", Desc = "无判定时间(毫秒)")]
        public int NoTouchTimeMS = 0;
        [Desc(Category = "1.Base", Desc = "销毁时间(毫秒)，延迟移除")]
        public int DestoryTimeMS = 0;
        [Desc(Category = "1.Base", Desc = "客户端是否可见")]
        public bool ClientVisible = true;
        //--------------------------------------------

        //-------------------------------------------------------------------------------------------------------------------
        #region 形状
        [Desc("法术形状")]
        public enum Shape : byte
        {
            [Desc("圆形（BodySize=半径尺寸）")] Round = AttackShape.Round,
            [Desc("扇形（BodySize=半径尺寸；FanAngle=角度）")] Fan = AttackShape.Fan,

            [Desc("胶囊条状（Distance=长度；RectWide=宽度）")] Strip = AttackShape.Strip,
            [Desc("胶囊射线，以原点出去（Distance=长度；RectWide=宽度）")] StripRay = AttackShape.StripRay,
            [Desc("胶囊射线，接触到最近（Distance=最大长度；RectWide=宽度）")] StripRayTouchEnd = AttackShape.StripRayTouchEnd,

            [Desc("方形条状")] RectStrip = AttackShape.RectStrip,
            [Desc("方形射线（以原点出去）")] RectStripRay = AttackShape.RectStripRay,
            [Desc("横向胶囊条状")] WideStrip = AttackShape.WideStrip,

            [Desc("连线类型（单体攻击），比如激光塔（Distance=判定距离）")] LineToTarget = AttackShape.LineToTarget,
            [Desc("连线类型（多体攻击），比如激光塔（Distance=判定距离）")] LineToTargetPos = AttackShape.LineToTargetPos,
            [Desc("连线类型（单体攻击），比如伸出去的钩子（Distance=判定距离）")] LineToStart = AttackShape.LineToStart,
            [Desc("连线类型（单体攻击），比如伸出去的钩子（Distance=判定距离）")] LineToSender = AttackShape.LineToSender,
            [Desc("圆环，中间是空的（BodySize=外环半径；BodySize-RectWide=内环半径）")] Circle = AttackShape.Circle,
        }
        public bool IsDistance
        {
            get
            {
                switch (BodyShape)
                {
                    case SpellTemplate.Shape.LineToTarget:
                    case SpellTemplate.Shape.LineToTargetPos:
                    case SpellTemplate.Shape.LineToStart:
                    case SpellTemplate.Shape.LineToSender:
                        return false;
                    case SpellTemplate.Shape.Strip:
                    case SpellTemplate.Shape.StripRay:
                    case SpellTemplate.Shape.StripRayTouchEnd:
                    case SpellTemplate.Shape.RectStrip:
                    case SpellTemplate.Shape.RectStripRay:
                    case SpellTemplate.Shape.WideStrip:
                        return true;
                    default:
                        return false;
                }
            }
        }
        public bool IsBodySize
        {
            get
            {
                switch (BodyShape)
                {
                    case SpellTemplate.Shape.LineToTarget:
                    case SpellTemplate.Shape.LineToTargetPos:
                    case SpellTemplate.Shape.LineToStart:
                    case SpellTemplate.Shape.LineToSender:
                        return false;
                    case SpellTemplate.Shape.Strip:
                    case SpellTemplate.Shape.StripRay:
                    case SpellTemplate.Shape.StripRayTouchEnd:
                    case SpellTemplate.Shape.RectStrip:
                    case SpellTemplate.Shape.RectStripRay:
                    case SpellTemplate.Shape.WideStrip:
                        return false;
                    default:
                        return true;
                }
            }
        }

        [Desc("体素判定方式")]
        public enum HitVoxelAnchor : byte
        {
            [Desc("未指定,跟VoxelAnchor")] NA = 0,
            [Desc("判定区域在锚点上方 ╨")] Up = 1,
            [Desc("锚点在判定区当中 ╫")] Middle = 2,
            [Desc("判定区域在锚点下方 ╥")] Down = 3,
        }
        [DependOnProperty(nameof(BodyShape))]
        public bool IsShapeFan { get { return BodyShape == Shape.Fan; } }
        [DependOnProperty(nameof(BodyShape))]
        public bool IsShapeStrip
        {
            get
            {
                switch (BodyShape)
                {
                    case Shape.Strip:
                    case Shape.StripRay:
                    case Shape.StripRayTouchEnd:
                    case Shape.Circle:
                    case Shape.RectStrip:
                    case Shape.RectStripRay:
                    case Shape.WideStrip:
                        return true;
                }
                return false;
            }
        }
        [DependOnProperty(nameof(BodyShape))]
        public bool IsShapeWide
        {
            get
            {
                switch (BodyShape)
                {
                    case Shape.Strip:
                    case Shape.StripRay:
                    case Shape.StripRayTouchEnd:
                    case Shape.Circle:
                    case Shape.RectStrip:
                    case Shape.RectStripRay:
                    case Shape.WideStrip:
                    case Shape.LineToSender:
                    case Shape.LineToStart:
                    case Shape.LineToTarget:
                    case Shape.LineToTargetPos:
                        return true;
                }
                return false;
            }
        }
        [DependOnProperty(nameof(BodyShape))]
        public bool IsShapeHeight
        {
            get
            {
                switch (BodyShape)
                {
                    case Shape.LineToStart:
                    case Shape.LineToTarget:
                    case Shape.LineToSender:
                        return false;
                }
                return true;
            }
        }
        [DependOnProperty(nameof(BodyShape))]
        public bool IsShapeDistance
        {
            get
            {
                switch (BodyShape)
                {
                    case Shape.Strip:
                    case Shape.StripRay:
                    case Shape.StripRayTouchEnd:
                    case Shape.LineToTarget:
                    case Shape.LineToTargetPos:
                    case Shape.LineToStart:
                    case Shape.LineToSender:
                    case Shape.RectStrip:
                    case Shape.RectStripRay:
                    case Shape.WideStrip:
                        return true;
                    default:
                        return false;
                }
            }
        }
        //--------------------------------------------
        [Desc(Category = "2.攻击范围", Desc = "攻击范围类型")]
        public Shape BodyShape = Shape.Round;
        public Misc.AttackShape AsBodyShape => (Misc.AttackShape)BodyShape;

        [Desc(Category = "2.攻击范围", Desc = "尺寸，半径（Round, Fan, Circle）")] public float BodySize = 1;
        [Desc(Category = "2.攻击范围", Desc = "弧度（Fan）")][DependOnProperty(nameof(IsShapeFan))] public float FanAngle;
        [Desc(Category = "2.攻击范围", Desc = "角度（Fan）")][DependOnProperty(nameof(IsShapeFan))] public float FanAngle360 { get => CMath.RadianToAngle(FanAngle); set => FanAngle = CMath.AngleToRadian(value); }

        [Desc(Category = "2.攻击范围", Desc = "宽度，粗度(Strip, StripRay，StripRayTouchEnd，RectStrip, RectStripRay，Circle, WideStrip)")]
        [DependOnProperty(nameof(IsShapeWide))] public float RectWide = 1;

        [Desc(Category = "2.攻击范围", Desc = "长度(Strip, StripRay，StripRayTouchEnd, RectStrip, RectStripRay，LineToTarget, LineToStart, WideStrip)")]
        [DependOnProperty(nameof(IsShapeDistance))] public float Distance = 10;

        [Desc(Category = "2.攻击范围", Desc = "法术自身高度")]
        [DependOnProperty(nameof(IsShapeHeight))]
        public float BodyHeight = 1;
        [Desc(Category = "2.攻击范围", Desc = "法术高度对齐方式")]
        public VoxelAnchor BodyVoxelAnchor = VoxelAnchor.Floating;
        [Desc(Category = "2.攻击范围", Desc = "法术判定范围对齐方式")]
        public HitVoxelAnchor BodyHitVoxelAnchor = HitVoxelAnchor.NA;
        //--------------------------------------------
        public DeepCore.Geometry.Vector3 AdjustVoxelAnchor(DeepCore.Geometry.Vector3 pos, ref float height)
        {
            //var height = BodyHeight;
            //             switch (BodyShape)
            //             {
            //                 case SpellTemplate.Shape.LineToStart:
            //                 case SpellTemplate.Shape.LineToTarget:
            //                 case SpellTemplate.Shape.LineToTargetPos:
            //                     //height = 0;
            //                     break;
            //                 case SpellTemplate.Shape.LineToSender:
            //                     //height = 0;
            //                     break;
            //             }
            switch (BodyHitVoxelAnchor)
            {
                case SpellTemplate.HitVoxelAnchor.NA:
                    switch (BodyVoxelAnchor)
                    {
                        case VoxelAnchor.Floating:
                            pos.Z -= height / 2;
                            break;
                        case VoxelAnchor.Flooring:
                            break;
                        case VoxelAnchor.Ceiling:
                            pos.Z -= height;
                            break;
                    }
                    break;
                case SpellTemplate.HitVoxelAnchor.Up:
                    break;
                case SpellTemplate.HitVoxelAnchor.Middle:
                    pos.Z -= height / 2;
                    break;
                case SpellTemplate.HitVoxelAnchor.Down:
                    pos.Z -= height;
                    break;
            }
            return pos;
        }
        //--------------------------------------------

        #endregion
        //-------------------------------------------------------------------------------------------------------------------
        #region 运动
        [Desc("法术运动方式")]
        public enum MotionType : byte
        {
            [Desc("在原地不动")]
            Immovability = 1,

            [Desc("按直线运动，射出时移动方向就已经确定")]
            Straight = 2,
            [Desc("先按直线运动，命中后返回到发射者")]
            StraightPingPong = 104,
            [Desc("回旋镖，速度耗尽后飞回发射者")]
            Boomerang = 105,

            [Desc("按朝向运动，朝向过程中可以改变")]
            Forward = 102,
            [Desc("向发射者方向移动，回旋镖")]
            Backward = 103,

            [Desc("跟随目标，直到击中")]
            Missile = 3,

            [Desc("向周围扩散")]
            AOE = 4,
            [Desc("向周围扩散，绑定发射者")]
            AOE_Binding = 11,
            [Desc("向周围扩散，绑定被攻击者")]
            AOE_BindingTarget = 12,

            [Desc("绑定发射者")]
            Binding = 6,
            [Desc("绑定被攻击者")]
            BindingTarget = 7,

            [Desc("炮弹类")]
            Cannon = 8,
            [Desc("直接在目标生效")]
            SelectTarget = 9,
            [Desc("直接在自身生效")]
            SelectLauncher = 15,

            [Desc("先按直线运动，过程中锁定目标")]
            SeekerMissile = 10,

            [Desc("锁定并命中目标，和SeekerMissile区别是没有过程，直接命中。")]
            SeekerSelectTarget = 13,

            [Desc("发射者和目标绑定（Distance必须在范围内）")]
            Chain = 14,

            [Desc("自定义")]
            Custom = 14,
        }

        [Desc("是否为投射物")]
        public bool IsProjectile
        {
            get
            {
                switch (MType)
                {
                    case MotionType.Straight:
                    case MotionType.StraightPingPong:
                    case MotionType.Boomerang:
                    case MotionType.Forward:
                    case MotionType.Backward:
                    case MotionType.Missile:
                    case MotionType.SeekerMissile:
                    case MotionType.Cannon:
                        return true;
                    default:
                        return false;
                }
            }
        }
        [Desc("法术AOE运动方式")]
        public enum AoeMotionType : byte
        {
            [Desc("线性，递增或递减")]
            Linear = 0,
            [Desc("正弦，单次周期为PI，象限0～1")]
            Sine = 1,
        }
        [DependOnProperty(nameof(MType))]
        public bool IsOnlyHitOnExplosionKeyFrame
        {
            get
            {
                switch (MType)
                {
                    case MotionType.Missile:
                    case MotionType.SeekerMissile:
                    case MotionType.Cannon:
                        return true;
                    default:
                        return false;
                }
            }
        }
        [DependOnProperty(nameof(MType))]
        public bool IsLaunchSpellEventSyncPos
        {
            get
            {
                switch (MType)
                {
                    case MotionType.AOE_Binding:
                    case MotionType.AOE_BindingTarget:
                    case MotionType.Binding:
                    case MotionType.BindingTarget:
                        return false;
                    case MotionType.SelectLauncher:
                    case MotionType.SelectTarget:
                    case MotionType.Immovability:
                    case MotionType.Cannon:
                    case MotionType.AOE:
                    case MotionType.Straight:
                    case MotionType.StraightPingPong:
                    case MotionType.Boomerang:
                    case MotionType.Forward:
                    case MotionType.Backward:
                    case MotionType.Missile:
                    case MotionType.SeekerMissile:
                    case MotionType.SeekerSelectTarget:
                    case MotionType.Chain:
                    default:
                        return true;
                }
            }
        }
        [DependOnProperty(nameof(MType))]
        public bool IsMoveable
        {
            get
            {
                switch (MType)
                {
                    case MotionType.Cannon:
                    case MotionType.Missile:
                    case MotionType.SeekerMissile:
                    case MotionType.Forward:
                    case MotionType.Backward:
                    case MotionType.Straight:
                    case MotionType.StraightPingPong:
                    case MotionType.Boomerang:
                    case MotionType.AOE:
                    case MotionType.AOE_Binding:
                    case MotionType.AOE_BindingTarget:
                        return true;
                }
                return false;
            }
        }
        [DependOnProperty(nameof(MType))]
        public bool IsBinding
        {
            get
            {
                switch (MType)
                {
                    case MotionType.Binding:
                    case MotionType.BindingTarget:
                    case MotionType.AOE_Binding:
                    case MotionType.AOE_BindingTarget:
                    case MotionType.Chain:
                        return true;
                }
                return false;
            }
        }
        [DependOnProperty(nameof(MType))]
        public bool IsSeekingTarget
        {
            get
            {
                switch (MType)
                {
                    case MotionType.SeekerMissile:
                    case MotionType.SeekerSelectTarget:
                        return true;
                }
                return false;
            }
        }
        [DependOnProperty(nameof(MType))]
        public bool IsFollowTarget
        {
            get
            {
                switch (MType)
                {
                    case MotionType.SeekerMissile:
                    case MotionType.SeekerSelectTarget:
                    case MotionType.Missile:
                        return true;
                }
                return false;
            }
        }
        [DependOnProperty(nameof(MType))]
        public bool IsFaceToTarget
        {
            get
            {
                switch (BodyShape)
                {
                    case SpellTemplate.Shape.LineToTarget:
                    case SpellTemplate.Shape.LineToTargetPos:
                        return true;
                }
                switch (MType)
                {
                    case MotionType.SeekerMissile:
                    case MotionType.Missile:
                    case MotionType.Chain:
                        return true;
                }
                return false;
            }
        }
        [DependOnProperty(nameof(MType))]
        public bool IsAOE
        {
            get
            {
                switch (MType)
                {
                    case MotionType.AOE:
                    case MotionType.AOE_Binding:
                    case MotionType.AOE_BindingTarget:
                        return true;
                }
                return false;
            }
        }
        public bool IsNeedTarget
        {
            get
            {
                switch (MType)
                {
                    case MotionType.Missile:
                    case MotionType.BindingTarget:
                    case MotionType.SelectTarget:
                    case MotionType.Chain:
                    case MotionType.AOE_BindingTarget:
                        return true;
                }
                return false;
            }
        }

        [Desc(Category = "3.运动", Desc = "运动类型")]
        public MotionType MType = MotionType.Straight;
        [Desc(Category = "3.运动", Desc = "运动速度,AOE或者扩散类，缩放速度(距离 / 每秒)")]
        [DependOnProperty(nameof(IsMoveable))]
        public float MSpeedSEC = 9f;
        [Desc(Category = "3.运动", Desc = "加速度(距离/每秒)(最终速度=速度+加速度)")]
        [DependOnProperty(nameof(IsMoveable))]
        public float MSpeedAdd = 0f;
        [Desc(Category = "3.运动", Desc = "阻力(每秒递减速度百分比)")]
        [DependOnProperty(nameof(IsMoveable))]
        public float MSpeedAcc = 0f;

        [Desc(Category = "3.运动", Desc = "最大限速")]
        [DependOnProperty(nameof(IsMoveable))]
        public float MSpeed_MAX = 100f;
        [Desc(Category = "3.运动", Desc = "最小限速")]
        [DependOnProperty(nameof(IsMoveable))]
        public float MSpeed_MIN = -100f;
        //--------------------------------------------
        [Desc(Category = "3.运动", Desc = "AOE方式")]
        [DependOnProperty(nameof(IsAOE))]
        public AoeMotionType AOEMType = AoeMotionType.Linear;

        [Desc(Category = "3.运动", Desc = "自转转动速度(弧度/每秒)")]
        public float RotateSpeedSEC;
        [Desc(Category = "3.运动", Desc = "自转转动速度(角度/每秒)")]
        public float RotateSpeedSEC360
        {
            get => CMath.RadianToAngle(RotateSpeedSEC);
            set => RotateSpeedSEC = CMath.AngleToRadian(value);
        }
        [Desc(Category = "3.运动", Desc = "自转转动速度增加(弧度/每秒)")]
        public float RotateSpeedAdd;
        [Desc(Category = "3.运动", Desc = "自转转动速度增加(角度/每秒)")]
        public float RotateSpeedAdd360
        {
            get => CMath.RadianToAngle(RotateSpeedAdd);
            set => RotateSpeedAdd = CMath.AngleToRadian(value);
        }
        [Desc(Category = "3.运动", Desc = "自转转动阻力")]
        public float RotateSpeedAcc;

        //         [Desc(Category = "3.运动", Desc = "抛物线上抛（迫击炮）")]
        //         public bool MCannonUpThrow = false;
        //         [Desc(Category = "3.运动", Desc = "尽量45度角")]
        //         public bool MCannonExpect45 = false;
        //--------------------------------------------
        [Desc("法术抛物线方式")]
        public enum CannonThrow : byte
        {
            [Desc("平抛")]
            Horizontal = 0,
            [Desc("抛物线上抛（迫击炮）")]
            UpThrow = 1,
            [Desc("尽量45度角")]
            Expect45 = 2,
        }

        [Desc(Category = "3.运动", Desc = "抛物线类型")]
        public CannonThrow MCannonThrow = CannonThrow.Horizontal;

        [Desc(Category = "3.运动", Desc = "重力")]
        public float MCannonGravitySEC = 0;

        //--------------------------------------------
        //         [Desc("法术锁敌方式")]
        //         public enum SeekingExpect : byte
        //         {
        //             [Desc("搜索随机单位")]
        //             Random,
        //             [Desc("搜索最近单位")]
        //             Nearest,
        //             [Desc("搜索最远单位")]
        //             Farthest,
        //             [Desc("搜索随机单位(忽略链中)")]
        //             RandomIgnoreInChain,
        //             [Desc("搜索最近单位(忽略链中)")]
        //             NearestIgnoreInChain,
        //             [Desc("搜索最远单位(忽略链中)")]
        //             FarthestIgnoreInChain,
        //         }
        //--------------------------------------------

        [Desc(Category = "4.运动-自动跟踪", Desc = "如果是自动搜敌导弹SeekerMissile，则表示搜索范围，如果是LineToTarget则表示伤害距离")]
        [DependOnProperty(nameof(IsSeekingTarget))]
        public float SeekingRange = 10;
        [Desc(Category = "4.运动-自动跟踪", Desc = "自动锁敌冷却时间（毫秒）")]
        [DependOnProperty(nameof(IsSeekingTarget))]
        public int SeekingCooldownMS = 0;

        [Desc(Category = "4.运动-自动跟踪", Desc = "自动锁敌运动时，转角弧度")]
        [DependOnProperty(nameof(IsFollowTarget))]
        public float SeekingTurningAngleSEC = 3f;
        [Desc(Category = "4.运动-自动跟踪", Desc = "自动锁敌运动时，转角角度")]
        [DependOnProperty(nameof(IsFollowTarget))]
        public float SeekingTurningAngleSEC360
        {
            get => CMath.RadianToAngle(SeekingTurningAngleSEC);
            set => SeekingTurningAngleSEC = CMath.AngleToRadian(value);
        }


        [Desc(Category = "4.运动-自动跟踪", Desc = "自动锁敌检测方式")]
        [DependOnProperty(nameof(IsSeekingTarget))]
        public LaunchSkill.SeekingExpect SeekingExpectTarget = LaunchSkill.SeekingExpect.Random;
        [Desc(Category = "4.运动-自动跟踪", Desc = "自动锁敌忽略链中")]
        [DependOnProperty(nameof(IsSeekingTarget))]
        public bool SeekingIgnoreInChain = false;
        //--------------------------------------------

        [Desc(Category = "4.运动-绑定", Desc = "如果是绑定则围绕绑定者公转")]
        [DependOnProperty(nameof(IsBinding))]
        public bool IsBindingOrbit = false;
        [Desc(Category = "4.运动-绑定", Desc = "绑定公转距离")]
        [DependOnProperty(nameof(IsBinding))]
        public float OrbitDistance = 2;
        [Desc(Category = "4.运动-绑定", Desc = "如果为绑定，是否绑定方向")]
        [DependOnProperty(nameof(IsBinding))]
        public bool IsBindingDirection = false;

        [Desc(Category = "4.运动-绑定", Desc = "如果为绑定，绑定Z高度偏移量")]
        [DependOnProperty(nameof(IsBinding))]
        public float BindingOffsetZ = 0f;
        [Desc(Category = "4.运动-绑定", Desc = "如果为绑定，绑定角度偏移量")]
        [DependOnProperty(nameof(IsBinding))]
        public float BindingOffsetAngle360 = 0f;
        [Desc(Category = "4.运动-绑定", Desc = "如果为绑定，绑定长度偏移量")]
        [DependOnProperty(nameof(IsBinding))]
        public float BindingOffsetDistance = 0f;

        [Desc(Category = "4.运动-绑定", Desc = "距离变化速度(距离/每秒)")]
        [DependOnProperty(nameof(IsBinding))]
        public float MDistanceSpeedSEC = 0f;
        [Desc(Category = "4.运动-绑定", Desc = "距离变化加速度(距离/每秒)(最终速度=速度+加速度)")]
        [DependOnProperty(nameof(IsBinding))]
        public float MDistanceSpeedAdd = 0f;
        [Desc(Category = "4.运动-绑定", Desc = "距离变化阻力(每秒递减速度百分比)")]
        [DependOnProperty(nameof(IsBinding))]
        public float MDistanceSpeedAcc = 0f;

        [Desc(Category = "4.运动-绑定", Desc = "距离变化最大限速")]
        [DependOnProperty(nameof(IsBinding))]
        public float MDistanceSpeed_MAX = 100f;
        [Desc(Category = "4.运动-绑定", Desc = "距离变化最小限速")]
        [DependOnProperty(nameof(IsBinding))]
        public float MDistanceSpeed_MIN = -100f;

        //--------------------------------------------
        #endregion
        //-------------------------------------------------------------------------------------------------------------------

        #region 作用单位
        //--------------------------------------------
        [Desc(Category = "5.目标", Desc = "法术期望作用目标")]
        public SkillTemplate.CastTarget ExpectTarget = SkillTemplate.CastTarget.Enemy;
        [Desc(Category = "5.目标", Desc = "总共会造成多少次受击(穿透次数)，0表示无限制")]
        public int MaxHitCount = 0;
        [Desc(Category = "5.目标", Desc = "单次受击最大影响（受击）单位，0表示无限制")]
        public int MaxAffectUnit = 0;
        [Desc(Category = "5.目标", Desc = "过滤范围内目标")]
        public LaunchSkill.SeekingExpect FilterAffect = LaunchSkill.SeekingExpect.Random;
        [Desc(Category = "5.目标", Desc = "目标丢失后，重新锁定目标")]
        public FocusTarget ResetSeekingTarget;
        //--------------------------------------------
        [Desc(Category = "5.目标-移除条件", Desc = "绑定目标不可操控时移除自己")]
        public bool RemoveOnBindingUncontrollable = false;
        [Desc(Category = "5.目标-移除条件", Desc = "绑定目标非技能状态时移除自己")]
        public bool RemoveOnBindingSkillOver = false;
        [Desc(Category = "5.目标-移除条件", Desc = "当Spell被移除停止释放它的技能")]
        public bool StopBindingSkillOnRemoved = false;
        //--------------------------------------------

        #endregion
        //--------------------------------------------
        #region 关键帧判定
        [Desc(Category = "6.关键帧(爆炸)", Desc = "击中就消失(爆炸)，和HitIntervalMS冲突")]
        public bool HitOnExplosion;
        [Desc(Category = "6.关键帧(爆炸)", Desc = "击中就消失(爆炸)关键帧，适用于打到就爆和Cannon类法术")]
        [DependOnProperty(nameof(HitOnExplosion))]
        public KeyFrame HitOnExplosionKeyFrame;
        [Desc(Category = "6.关键帧(爆炸)", Desc = "触碰地图就消失(爆炸)，触发HitOnExplosion")]
        public bool MapBlockExplosion = false;
        //--------------------------------------------
        [Desc(Category = "6.关键帧", Desc = "每间隔多少时间就触发一次，0 适用于穿透性，但只对单位造成一次伤害（毫秒）")]
        [DependOnProperty(nameof(IsOnlyHitOnExplosionKeyFrame), false)]
        public int HitIntervalMS;
        [DependOnProperty(nameof(HitIntervalMS))] public bool IsIntervalHit { get => HitIntervalMS > 0; }
        [DependOnProperty(nameof(HitIntervalMS))] public bool IsOnceHit { get => HitIntervalMS == 0; }
        //--------------------------------------------
        [Desc(Category = "6.关键帧", Desc = "多长时间可以再次造成伤害")]
        [DependOnProperty(nameof(IsOnlyHitOnExplosionKeyFrame), false)]
        public int CleanHitIntervalMS;
        //--------------------------------------------

        [DependOnProperty(nameof(IsOnlyHitOnExplosionKeyFrame), false)]
        [Desc(Category = "6.关键帧", Desc = "只造成一次触发的关键帧 or 每隔一段时间触发的关键帧", Editable = false)]
        public KeyFrame HitIntervalKeyFrame;

        [DependOnProperty(nameof(IsIntervalHit))]
        [DependOnProperty(nameof(IsOnlyHitOnExplosionKeyFrame), false)]
        [Desc(Category = "6.关键帧", Desc = "每隔一段时间触发的关键帧", Editable = true)]
        public KeyFrame IntervalHitKeyFrame
        {
            get => HitIntervalKeyFrame;
            set => HitIntervalKeyFrame = value;
        }

        [DependOnProperty(nameof(IsOnlyHitOnExplosionKeyFrame), false)]
        [DependOnProperty(nameof(IsOnceHit))]
        [Desc(Category = "6.关键帧", Desc = "只造成一次触发的关键帧", Editable = true)]
        public KeyFrame OnceHitKeyFrame
        {
            get => HitIntervalKeyFrame;
            set => HitIntervalKeyFrame = value;
        }

        //--------------------------------------------
        [Desc(Category = "6.关键帧", Desc = "按顺序触发的所有关键帧")]
        [DependOnProperty(nameof(IsOnlyHitOnExplosionKeyFrame), false)]
        public ArrayList<KeyFrame> KeyFrames;

        [Desc(Category = "6.关键帧", Desc = "最后一帧，仅在生命消亡（LifeTimeMS）到最后时触发，不会产生伤害")]
        public KeyFrame LastKeyFrame;

        //--------------------------------------------
        #endregion
        //--------------------------------------------
        #region 资源
        [Desc(Category = "7.资源", Desc = "模型名字或者Perfab名字")]
        [ResourceID(ResourceType.Object)] public string FileName;
        [Desc("模型名字或者Perfab名字对应的资源Id", "资源")]
        public int FileResId
        {
            get
            {
                if (Parser.TryParseInt(FileName, out var resId))
                    return resId;
                return 0;
            }
        }

        [Desc("声音播放资源名", "资源")]
        [ResourceID(ResourceType.Sound_Effect)] public string SoundName;

        [Desc(Category = "7.资源", Desc = "模型名字或者Perfab名字（起始）")]
        [ResourceID(ResourceType.Effect)] public string FileNameSpawn;
        [Desc("模型名字或者Perfab名字对应的资源Id（起始）", "资源")]
        public int FileResIdSpawn
        {
            get
            {
                if (Parser.TryParseInt(FileNameSpawn, out var resId))
                    return resId;
                return 0;
            }
        }
        [Desc(Category = "7.资源", Desc = "模型名字或者Perfab名字（结束）")]
        [ResourceID(ResourceType.Effect)] public string FileNameDestory;
        [Desc("模型名字或者Perfab名字对应的资源Id（结束）", "资源")]
        public int FileResIdDestory
        {
            get
            {
                if (Parser.TryParseInt(FileNameDestory, out var resId))
                    return resId;
                return 0;
            }
        }

        [Desc(Category = "7.资源", Desc = "骨骼挂载点（Chain头）")]
        public string BonesBegin;
        [Desc(Category = "7.资源", Desc = "骨骼挂载点（Chain尾）")]
        public string BonesEnd;


        [Desc(Category = "7.资源", Desc = "资源一直朝向目标（如果为Missile，Line，则也会一直朝向目标）")]
        public bool ResFaceToMotion = true;

        [Desc(Category = "7.资源", Desc = "是否循环播放动画")]
        public bool IsCycAnim = true;
        [Desc(Category = "7.资源", Desc = "缩放比率")]
        public float FileBodyScale = 1;
        [Desc(Category = "7.资源", Desc = "是否需要适配宿主比例（原始资源需要1倍大小）")]
        public bool FitOwnerScale = false;
        [Desc(Category = "7.资源", Desc = "绑定特效")]
        public LaunchEffect BindingEffect;
        [Desc(Category = "7.资源", Desc = "法术释放时，在目标点释放特效，可作为技能提示")]
        public LaunchEffect TargetEffect;


        [Desc(Category = "7.资源", Desc = "起始时特效")]
        public LaunchEffect SpawnEffect;
        [Desc(Category = "7.资源", Desc = "销毁时特效")]
        public LaunchEffect DestoryEffect;
        #endregion

        //------------------------------------

        [Desc(Category = "9.扩展", Desc = "能力")]
        [NotNull]
        public ArrayList<ISpellTemplateAbility> Abilities = new ArrayList<ISpellTemplateAbility>();
        /// <summary>
        /// 用户自定义扩展属性
        /// </summary>
        [Desc(Category = "9.扩展", Desc = "扩展属性")]
        [Expandable]
        [NotNull]
        public ISpellProperties Properties;
        public override IPropertiesData PropertiesData => this.Properties;
        //------------------------------------


        public SpellTemplate()
        {
            Properties = ZoneDataFactory.Factory.CreateProperties<ISpellProperties>(this);
        }
        //------------------------------------------------------------

        //------------------------------------------------------------


        /// <summary>
        /// 法术伤害或特效关键帧
        /// </summary>
        [MessageType(BattleConstants.SpellKeyFrame)]
        [Desc("法术伤害或特效关键帧")]
        [Expandable]
        public class KeyFrame : BaseKeyFrame
        {
            /// <summary>
            /// 触发的特效
            /// </summary>
            [Desc("触发的特效")]
            public LaunchEffect Effect;

            /// <summary>
            /// 触发新的法术
            /// </summary>
            [Desc("触发新的法术")]
            public LaunchSpell Spell;

            /// <summary>
            /// 攻击伤害
            /// </summary>
            [Desc("攻击伤害")]
            public AttackProp Attack;

            /// <summary>
            /// 召唤小弟
            /// </summary>
            [Desc("召唤小弟")]
            public SummonUnit Summon;

            public override string ToString()
            {
                return "Frame: @" + FrameMS;
            }



        }

    }
    //---------------------------------------------------------------------------------//
    public abstract class ISpellTemplateAbility : IDataAbility
    {
    }
    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//
}
