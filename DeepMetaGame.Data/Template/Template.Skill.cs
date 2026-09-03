using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneEditor;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using static DeepMetaGame.Data.Misc.UnitActionDefinitionMap;

namespace DeepMetaGame.Data.Template
{
    //---------------------------------------------------------------------------------//


    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//


    /// <summary>
    /// 技能模板
    /// </summary>
    [MessageType(BattleConstants.SkillTemplate)]
    [Desc("技能模板")]
    public class SkillTemplate : CustomEventTemplateData
    {
        [Desc(Category = "1.基础", Desc = "技能冷却时间(毫秒)")]
        [DependOnProperty(nameof(IsCoolDownWithAction), false)]
        public int CoolDownMS = 1000;
        //------------------------------------
        //------------------------------------
        /// <summary>
        /// 攻击动作序列
        /// </summary>
        [Desc(Category = "2.动作", Desc = "攻击动作序列")]
        [ListDesc(typeof(UnitActionData))]
        public ArrayList<UnitActionData> ActionQueue = new ArrayList<UnitActionData>();
        //------------------------------------
        [Desc(Category = "2.动作", Desc = "以动作时间作为冷却时间")]
        public bool IsCoolDownWithAction = false;
        [Desc(Category = "2.动作", Desc = "可以打断其他技能和受击状态，受身技能")]
        public bool IsCounter = false;
        [Desc(Category = "2.动作", Desc = "技能优先级，高优先级可打断低优先级")]
        public int ActionPriority = 0;
        [Desc(Category = "2.动作", Desc = "动作播放速率")]
        public float ActionSpeedRate = 1f;
        [Desc(Category = "2.动作", Desc = "技能释放期间，模型缩放（仅客户端有效）")]
        public float BodyScale = 1f;
        [Desc(Category = "2.动作", Desc = "技能释放期间，可手动撤销（取消旋风斩动作）")]
        public bool IsManuallyCancelable = false;
        [Desc(Category = "2.动作-多段攻击", Desc = "每次释放只做一个动作，多段攻击")]
        public bool IsSingleAction = false;
        [DependOnProperty(nameof(IsSingleAction))]
        [Desc(Category = "2.动作-多段攻击", Desc = "多段攻击间隔有效时间")]
        public int SingleActionCoolDownMS = 500;
        //------------------------------------

        public float AttackRange { get => AttackShape.AttackRange + AttackShape.OffsetRadius; }
        public float AttackAngle { get => AttackShape.IsShapeFan ? AttackShape.AttackAngle : 0f; }

        [Desc(Category = "3.攻击范围", Desc = "攻击距离", Editable = true)]
        public float AttackShapeRange { get => AttackShape.AttackRange; set => this.AttackShape.AttackRange = value; }

        [Desc(Category = "3.攻击范围", Desc = "攻击范围")]
        public UnitActionData.AttackShape AttackShape = new UnitActionData.AttackShape()
        {
            AShape = UnitActionData.AttackShape.Shape.Single,
            AttackAngle = 0.3f,
            AttackRange = 3f,
            StripWide = 0.5f,
        };
        [Desc(Category = "3.攻击范围", Desc = "技能和目标的保持距离")]
        public float AttackBodyTouchRange = 0f;
        [Desc(Category = "3.攻击范围", Desc = "单体攻击，只在目标处于攻击范围才产生伤害")]
        public bool AttackMustBeInRange = true;
        [Desc(Category = "3.攻击范围", Desc = "保持攻击距离（NPC会保持一段距离再攻击）大于0启效")]
        public float AttackKeepRange = 0;
        [Desc(Category = "3.攻击范围", Desc = "追踪状态单位开始攻击距离(必须小于技能攻击范围)，大于0起效")]
        public float AttackFollowRange = 0;
        //------------------------------------
        [Desc("技能释放目标")]
        public enum CastTarget
        {
            [Desc("无类型")]/*         */ NA = 0,
            [Desc("任何单位")]/*       */ EveryOne /*            */= 0xFFFFFF,
            [Desc("除自己之外所有")]/* */ EveryOneExcludeSelf /* */= 0xFFFFFF - Self,

            [Desc("敌人")]/*           */ Enemy /*               */= 0x00FF,
            [Desc("敌对怪物")]/*       */ Enemy_Monster /*       */= 0x0020,
            [Desc("敌对玩家")]/*       */ Enemy_Player /*        */= 0x0040,

            [Desc("包括自己友军")]/*   */ AlliesIncludeSelf /*   */= 0xFF00,
            [Desc("自己")]/*           */ Self /*                */= 0x0100,
            [Desc("除自己之外友军")]/* */ AlliesExcludeSelf /*   */= 0xFF00 - Self,
            [Desc("除自己之外友军")]/* */ Allies /*              */= AlliesExcludeSelf,
            [Desc("宠物为主人")]/*     */ PetForMaster /*        */= 0x0200,
            [Desc("宠物和主人")]/*     */ PetAndMaster /*        */= 0x0400,
            [Desc("主人")]/*           */ Master /*              */= 0x4000,
            [Desc("召唤单位")]/*       */ SummonUnit /*          */= 0x8000,

        }
        [Desc("技能选择区域")]
        public enum SelectRange : byte
        {
            [Desc("无")]
            NA,
            [Desc("圆形区域")]
            Round,
            [Desc("直线")]
            Line,
            [Desc("矩形区域")]
            Rect,
            [Desc("扇形区域")]
            Fan,
        }
        //------------------------------------

        [Desc(Category = "4.释放", Desc = "吟唱或发动技能时的特效")]
        public LaunchEffect CastEffect;

        [Desc(Category = "4.释放", Desc = "法术是否从自己身上发出")]
        public bool IsLaunchBody = true;
        [Desc(Category = "4.释放", Desc = "吟唱时间（毫秒）大于0启效")]
        public int ChantTimeMS = 0;

        [Desc(Category = "4.释放", Desc = "技能期望作用目标")]
        public CastTarget ExpectTarget = CastTarget.Enemy;

        [Desc(Category = "4.指示器", Desc = "是否显示指示器")]
        public bool IsShowIndicator = false;

        [Desc(Category = "4.释放范围", Desc = "释放时是否选择方向")]
        public bool IsSelectRange = false;
        [DependOnProperty(nameof(IsSelectRange))]
        [Desc(Category = "4.释放范围", Desc = "选择方向类型")]
        public SelectRange SelectRangeType = SelectRange.NA;
        [DependOnProperty(nameof(IsSelectRange))]
        [Desc(Category = "4.释放范围", Desc = "选择方向尺寸")]
        public float SelectRangeSize;

        [Desc("技能释放模式")]
        public UnitActionData.LaunchMode SkillLaunchMode;

        //------------------------------------

        [Desc(Category = "5.消耗", Desc = "释放消耗血量")]
        public int CostHP;
        [Desc(Category = "5.消耗", Desc = "释放消耗法力")]
        public int CostMP;

        //------------------------------------

        [Desc(Category = "9.扩展", Desc = "能力")]
        [NotNull]
        public ArrayList<ISkillTemplateAbility> Abilities = new ArrayList<ISkillTemplateAbility>();
        /// <summary>
        /// 用户自定义扩展属性
        /// </summary>
        [Desc(Category = "9.扩展", Desc = "扩展属性")]
        [Expandable]
        [NotNull]
        public ISkillProperties Properties;
        public override IPropertiesData PropertiesData => this.Properties;
        //--------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------

        public SkillTemplate()
        {
            Properties = ZoneDataFactory.Factory.CreateProperties<ISkillProperties>(this);
        }
        public UnitActionData GetActionByName(string name)
        {
            foreach (UnitActionData act in ActionQueue)
            {
                if (name.Equals(act.ActionName))
                {
                    return act;
                }
            }
            return null;
        }

        public int TotalActionQueueTimeMS
        {
            get
            {
                int ret = 0;
                if (ActionQueue != null)
                {
                    for (int i = 0; i < ActionQueue.Count; i++)
                    {
                        ret += ActionQueue[i].TotalTimeMS;
                    }
                }
                return ret;
            }
        }

        public void ActionQueueTimeArray(IList<float> ret)
        {
            for (int i = 0; i < ActionQueue.Count; i++)
            {
                ret.Add(ActionQueue[i].TotalTimeMS);
            }
        }

    }

    //---------------------------------------------------------------------------------//
    public abstract class ISkillTemplateAbility : IDataAbility
    {
    }
    //---------------------------------------------------------------------------------//
    /// <summary>
    /// 单位动作模板
    /// </summary>
    [MessageType(BattleConstants.SkillUnitActionData)]
    [Desc("单位动作")]
    [Expandable]
    public class UnitActionData : IBaseFuncData
    {
        [Desc("播放时间（如果多段动作，则需要指定每段时间）", "Action", editable: true)]
        public int TotalTimeMS
        {
            get => Action != null ? Action.TimeMS : 0;
            set
            {
                if (Action == null) Action = new UnitActionKeyFrame() { TimeMS = value };
                else Action.TimeMS = value;
            }
        }
        public string ActionName { get => Action?.ActionName; }
        public int ActionResId { get => Action != null ? Action.ActionResId : 0; }


        [Desc("动作", "Action")] public UnitActionKeyFrame Action = new UnitActionKeyFrame();

        [Desc("动作对应的特效，通常用作刀光", "Action")] public LaunchEffect ActionEffect;

        [Desc("动作对应的特效名字，通常用作刀光", "Action")]
        public string ActionEffectFileName
        {
            get => ActionEffect?.Name;
            set
            {
                if (ActionEffect == null) ActionEffect = new LaunchEffect();
                ActionEffect.Name = value;
            }
        }



        [Desc("移动可取消动作", "状态")] public bool IsCancelable = false;
        [Desc("技能可取消动作", "状态")] public bool IsCancelableBySkill = false;

        [Desc("是否进入霸体状态，不会被打断", "状态")] public bool IsNoneBlock = false;
        [Desc("动作期间是否无碰撞", "状态")] public bool IsNoneTouch = false;
        [Desc("动作中是否面向目标", "状态")] public bool IsFaceToTarget = false;
        [Desc("动作是否隐身", "状态")] public bool IsInvisible = false;
        [Desc("动作中是否可以控制移动", "状态")] public bool IsControlMoveable = false;
        [Desc("动作中可以控制转向", "状态")] public bool IsControlFaceable = false;

        [DependOnProperty(nameof(IsFaceToTarget))][Desc("转身速度（弧度/秒）", "状态")] public float TurnSpeedSEC = float.NaN;
        [DependOnProperty(nameof(IsFaceToTarget))]
        [Desc("转身速度（角度/秒）", "状态")]
        public float TurnSpeedSEC360
        {
            get => CMath.RadianToAngle(TurnSpeedSEC);
            set => TurnSpeedSEC = CMath.AngleToRadian(value);
        }



        [Desc("攻击范围改变", "攻击范围")]
        public AttackShape OverrideAttackShape;


        [Desc("所有关键帧", "关键帧")]
        public ArrayList<KeyFrame> KeyFrames;

        [Desc("可触发下一段攻击的等待时间（毫秒）", "连击")]
        public int TriggerNextActionWaitTimeMS = 0;

        [DependOnProperty(nameof(BodyHit))]
        [DependOnProperty(nameof(IsMoveToTarget))]
        [DependOnProperty(nameof(IsJumpToTarget))]
        public bool IsBodyHit
        {
            get
            {
                if (IsMoveToTarget) return false;
                if (IsJumpToTarget) return false;
                return BodyHit != null;
            }
        }
        //[DependOnProperty(nameof(IsMoveToTarget))]
        [Desc("身体攻击判定", "身体攻击")]
        public AttackProp BodyHit;
        [DependOnProperty(nameof(IsBodyHit))]
        [Desc("身体攻击判定范围", "身体攻击")]
        public float BodyHitSize;
        [DependOnProperty(nameof(IsBodyHit))]
        [Desc("身体攻击后立即切换动作", "身体攻击")]
        public bool BodyHitNextAction;


        [Desc("(冲锋)：移动到目标。", "位移 - 冲锋")]
        public bool IsMoveToTarget = false;

        [Desc("移动到目标速度", "位移 - 冲锋")]
        public float MoveToTargetSpeedSEC = 10;

        [DescAttribute("冲锋或跳跃到目标时是否立即结束当前动作", "位移 - 冲锋")]
        public bool IsMoveToTargetStopAction = false;

        public enum TargetPosEnum : byte
        {
            [Desc("目标重合")]
            Body = 0,
            [Desc("目标前面")]
            Face = 1,
        }
        [Desc("位移到目标位置", "位移 - 冲锋/跳跃")]
        public TargetPosEnum TargetPos = TargetPosEnum.Body;
        [Desc("位移到目标位偏移量", "位移 - 冲锋/跳跃")]
        public float TargetOffset = 0;

        [Desc("(冲锋)：移动到目标根据(距离/动作时间)来算速度，如果目标在原地，则原地跳跃。", "位移 - 跳跃冲锋")]
        public bool IsJumpToTarget = false;
        [Desc("起跳速度", "位移 - 跳跃冲锋")]
        [DependOnProperty(nameof(IsJumpToTarget))]
        [DependOnProperty(nameof(IsMoveToTarget), false)]
        public float JumpToTargetSpeedZ = 3;
        [Desc("跳跃落地关键帧", "位移 - 跳跃冲锋")]
        [DependOnProperty(nameof(IsJumpToTarget))]
        [DependOnProperty(nameof(IsMoveToTarget), false)]
        public KeyFrame JumpFallenDownKeyFrame;
        [Desc("跳跃锁定目标，目标必须在攻击范围内", "位移 - 跳跃冲锋")]
        public bool IsJumpLockTarget = false;
        [DependOnProperty(nameof(IsJumpLockTarget))]
        [Desc("跳向目标时间(毫秒)", "位移 - 跳跃冲锋")]
        public int JumpLockTimeMS;
        [DependOnProperty(nameof(IsJumpLockTarget))]
        [Desc("跳向目标距离上限,与目标距离在[AttackRange,JumpLockMaxRange]范围时跳向目标", "位移 - 跳跃冲锋")]
        public float JumpLockMaxRange = 10f;


        [Desc("在攻击范围内，如果位移则挤开目标", "碰撞")]
        [DependOnProperty(nameof(IsMoveToTarget), false)]
        [DependOnProperty(nameof(IsJumpToTarget), false)]
        public bool BodyBlockOnAttackRange = true;



        public UnitActionData() { }
        public override string ToString()
        {
            return $"动作：{ActionName}({ActionResId}) 时长：{TotalTimeMS}(毫秒)";
        }
        //------------------------------------------------------------
        /// <summary>
        /// 动作状态数据
        /// </summary>
        [MessageType(BattleConstants.SkillStatusChange)]
        [Desc("动作关键帧数据")]
        [Expandable]
        public class StatusChange : IBaseFuncData
        {
            [Desc("移动可取消动作", "状态")]
            public bool IsCancelable = false;
            [Desc("技能可取消动作", "状态")]
            public bool IsCancelableBySkill = false;

            [Desc("是否进入霸体状态，不会被打断", "状态")]
            public bool IsNoneBlock = false;
            [Desc("动作期间是否无碰撞", "状态")]
            public bool IsNoneTouch = false;
            [Desc("动作中是否面向目标", "状态")]
            public bool IsFaceToTarget = false;

            [Desc("动作是否隐身", "状态")]
            public bool IsInvisible = false;
            [Desc("动作中是否可以控制移动", "状态")]

            public bool IsControlMoveable = false;
            [Desc("动作中可以控制转向", "状态")]
            public bool IsControlFaceable = false;

            [DescAttribute("客户端本地移动可取消动作", "客户端本地状态")]
            public bool LocalClient_IsCancelable = false;
            [DescAttribute("客户端本地技能可取消动作", "客户端本地状态")]
            public bool LocalClient_IsCancelableBySkill = false;
            public override string ToString()
            {
                return string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}",
                    IsCancelable ? "移动可取消," : "",
                    IsCancelableBySkill ? "技能可取消," : "",
                    IsNoneBlock ? "霸体," : "",
                    IsNoneTouch ? "无碰撞," : "",
                    IsFaceToTarget ? "面向目标," : "",
                    IsInvisible ? "隐身," : "",
                    IsControlMoveable ? "可控制移动," : "",
                    IsControlFaceable ? "可控制转向," : "",
                    LocalClient_IsCancelable ? "客户端本地移动可取消动作," : "",
                    LocalClient_IsCancelableBySkill ? "客户端本地技能可取消动作," : "");
            }

        }

        //------------------------------------------------------------

        [MessageType(BattleConstants.SkillAttackShape)]
        [Desc("动作攻击范围")]
        [Expandable]
        public class AttackShape : IBaseFuncData
        {
            public enum Shape : byte
            {
                [Desc("单体")]
                Single = Misc.AttackShape.Single,
                [Desc("圆形")]
                Round = Misc.AttackShape.Round,
                [Desc("扇形")]
                Fan = Misc.AttackShape.Fan,

                [Desc("胶囊条状")]
                Strip = Misc.AttackShape.Strip,
                [Desc("胶囊射线（以原点出去）")]
                StripRay = Misc.AttackShape.StripRay,
                [Desc("胶囊射线，接触到最近")]
                StripRayTouchEnd = Misc.AttackShape.StripRayTouchEnd,

                [Desc("方形条状")]
                RectStrip = Misc.AttackShape.RectStrip,
                [Desc("方形射线（以原点出去）")]
                RectStripRay = Misc.AttackShape.RectStripRay,

                [Desc("横向胶囊条状")]
                WideStrip = Misc.AttackShape.WideStrip,

                [Desc("圆环，中间是空的")]
                Circle = Misc.AttackShape.Circle,
            }
            [Desc("攻击范围类型", "攻击范围")]
            public Shape AShape = Shape.Round;
            public Misc.AttackShape AsShape { get => (Misc.AttackShape)AShape; }

            [Desc("半径（Round, Fan, Circle）长度(Strip, StripRay，StripRayTouchEnd, RectStrip, RectStripRay, LineToTarget, WideStrip)", "攻击范围")]
            public float AttackRange = 1;

            [Desc("弧度（Fan）", "攻击范围")]
            [DependOnProperty(nameof(IsShapeFan))]
            public float AttackAngle = 1;
            [Desc("角度（Fan）", "攻击范围")]
            [DependOnProperty(nameof(IsShapeFan))]
            public float AttackAngle360
            {
                get => CMath.RadianToAngle(AttackAngle);
                set => AttackAngle = CMath.AngleToRadian(value);
            }


            [Desc("宽度，粗度(Strip, StripRay，StripRayTouchEnd，RectStrip, RectStripRay, WideStrip)，环粗度(Circle)", "攻击范围")]
            [DependOnProperty(nameof(IsShapeStrip))]
            public float StripWide = 1;

            [Desc("初始点半径偏移", "身体")]
            public float OffsetRadius = 0;

            [DependOnProperty(nameof(AShape))]
            public bool IsSingle { get { return AShape == Shape.Single; } }
            [DependOnProperty(nameof(AShape))]
            public bool IsShapeFan { get { return AShape == Shape.Fan; } }
            [DependOnProperty(nameof(AShape))]
            public bool IsShapeStrip
            {
                get
                {
                    switch (AShape)
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

            public override string ToString()
            {
                return string.Format("重定向攻击范围:{0}", AShape);
            }


        }
        //-------------------------------------------------------------------------------------

        //------------------------------------------------------------

        //         [MessageType(0x4109)]
        //         [Desc("动作攻击范围")]
        //         [Expandable]
        //         public class ThrowTarget : IBaseFuncData
        //         {
        //             public override string ToString()
        //             {
        //                 return string.Format("重定向攻击范");
        //             }
        // 
        // 
        //         }
        //-------------------------------------------------------------------------------------


        //------------------------------------------------------------
        /// <summary>
        /// 动作关键帧数据
        /// </summary>
        [MessageType(BattleConstants.SkillKeyFrame)]
        [Desc("动作关键帧数据")]
        [Expandable]
        public class KeyFrame : BaseKeyFrame
        {
            [Desc("关键帧产生的法术或远程道具单位模板ID")]
            public LaunchSpell Spell;

            [Desc("当前关键帧特效ID")]
            public LaunchEffect Effect;

            [Desc("攻击属性ID")]
            public AttackProp Attack;

            [Desc("对自己产生BUFF")]
            public LaunchBuff SelfBuff;

            [Desc("开启光环")]
            public LaunchAura SelfAura;

            [Desc("此动作关键帧产生的位移")]
            public StartMove Move;

            [Desc("闪现位移")]
            public BlinkMove Blink;

            [Desc("动作状态改变")]
            public StatusChange ChangeStatus;

            [Desc("改变目标")]
            [Expandable]
            public FocusTarget ChangeTarget;

            [Desc("召唤小弟")]
            public SummonUnit Summon;

            public override string ToString()
            {
                return "Frame: @" + FrameMS;
            }
            public string ToShortText()
            {
                if (Attack != null) return "A";
                if (Spell != null) return "S";
                if (SelfBuff != null) return "B";
                if (SelfAura != null) return "R";
                if (Summon != null) return "O";
                if (Move != null) return "M";
                if (Blink != null) return "L";
                if (Effect != null) return "E";
                if (ChangeStatus != null) return "T";
                if (ChangeTarget != null) return "F";
                if (CustomAction != null) return "C";
                return "";
            }
            public string ToToolText()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Time: {FrameMS}");
                if (Attack != null) sb.AppendLine($"Attack: {Attack}");
                if (Spell != null) sb.AppendLine($"Spell: {Spell}");
                if (SelfBuff != null) sb.AppendLine($"SelfBuff: {SelfBuff}");
                if (SelfAura != null) sb.AppendLine($"SelfAura: {SelfAura}");
                if (Summon != null) sb.AppendLine($"Summon: {Summon}");
                if (Move != null) sb.AppendLine($"Move: {Move}");
                if (Blink != null) sb.AppendLine($"Blink: {Blink}");
                if (Effect != null) sb.AppendLine($"Effect: {Effect}");
                if (ChangeStatus != null) sb.AppendLine($"ChangeStatus: {ChangeStatus}");
                if (CustomAction != null) sb.AppendLine($"CustomAction: {CustomAction}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// 技能释放模式
        /// </summary>
        [MessageType(BattleConstants.SkillLaunchMode)]
        [DescAttribute("技能释放模式")]
        [Expandable]
        public class LaunchMode : IBaseFuncData
        {
            /// <summary>
            /// 释放模式（传目标，传方向...)
            /// </summary>
            public enum LaunchModeType : byte
            {
                [Desc("目标-给定目标")] Mode_Target = 0,
                [Desc("方向-给定朝向")] Mode_Dir = 1,
            }

            /// <summary>
            /// 目标检测范围
            /// </summary>
            public enum SearchType : byte
            {
                [Desc("圆形")] SearchType_Round,
                [Desc("矩形")] SearchType_Rect,
                [Desc("扇形")] SearchType_Fan,
            }

            [Desc("技能释放模式")]
            public LaunchModeType SkillLaunchType = LaunchModeType.Mode_Target;
            [Desc("技能目标检索类型")]
            public SearchType SkillSearchType;

            [Desc("检索范围参数 - Round:半径 Fan:范围 Rect:长")]
            public float Shape_Range = 0;
            [Desc("检索范围参数 - Rect:宽 ")]
            public float Shape_Width = 0;
            [Desc("检索范围参数 - 距中心点偏移：+向前偏移，-向后偏移 ")]
            public float OffsetFromCenter = 0;
            [Desc("检索范围参数 - 扇形开口弧度 180° = 3.14,90° = 1.57,45° = 0.785,30° = 0.523")]
            public float Shape_Fan_Angle = 0;
            [Desc("检索范围参数 - 扇形开口角度 180° = 3.14,90° = 1.57,45° = 0.785,30° = 0.523")]
            public float Shape_Fan_Angle360
            {
                get => CMath.RadianToAngle(Shape_Fan_Angle);
                set => Shape_Fan_Angle = CMath.AngleToRadian(value);
            }

            public object Clone()
            {
                LaunchMode ret = new LaunchMode();
                ret.SkillLaunchType = SkillLaunchType;
                ret.SkillSearchType = SkillSearchType;
                ret.Shape_Range = Shape_Range;
                ret.Shape_Width = Shape_Width;
                ret.Shape_Fan_Angle = Shape_Fan_Angle;
                ret.OffsetFromCenter = OffsetFromCenter;
                return ret;
            }


        }
    }
    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    [MessageType(BattleConstants.SkillInit)]
    public class SkillInit : ISerializable
    {
        public SkillTemplate Skill;
        public LaunchSkill Launch;
    }

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//
}
