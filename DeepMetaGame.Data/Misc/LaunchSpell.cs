using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{

    /// <summary>
    /// 发起法术或者飞行道具
    /// </summary>
    [MessageType(BattleConstants.LaunchSpell)]
    [Desc("释放法术或者飞行道具")]
    [Expandable]
    public class LaunchSpell : ISNData
    {
        //----------------------------------------------------------------------------------------
        [Desc("触发新的法术"), TemplateIDAttribute(typeof(SpellTemplate))] public int SpellID;
        [Desc("法术等级")] public int SpellLevel;
        [Desc("触发百分比")] public float LaunchPercent = 100f;

        [Desc("触发新的法术运行方式")]
        public enum PosType : byte
        {
            [Desc("默认释放一个法术")] POS_TYPE_DEFAULT_SINGLE = 0,

            [Desc("打出扇形多个法术")] POS_TYPE_CYCLE = 2,
            [Desc("打出圆环多个法术")] POS_TYPE_FAN = 3,
            [Desc("打出横向并排多个法术")] POS_TYPE_HORIZONTAL = 4,

            [Desc("原始Spell内随机点")] POS_TYPE_RANDOM_FOR_SPELL = 1,
            [Desc("原始Spell内随机点(互相连接)")] POS_TYPE_RANDOM_FOR_SPELL_IN_CHAIN = 5,
            [Desc("发送者内随机点")] POS_TYPE_RANDOM_FOR_SENDER = 6,
            [Desc("发送者内随机点(互相连接)")] POS_TYPE_RANDOM_FOR_SENDER_IN_CHAIN = 7,
        }
        [Desc("触发新的法术运行方式")] public PosType PType = PosType.POS_TYPE_DEFAULT_SINGLE;

        //----------------------------------------------------------------------------------------
        [DependOnProperty(nameof(PType))] public bool MultiLaunch { get { return PType != PosType.POS_TYPE_DEFAULT_SINGLE; } }
        [Desc("触发新的法术数量", "散射"), DependOnProperty(nameof(MultiLaunch))] public int Count = 1;
        [Desc("触发新的法术间距", "散射"), DependOnProperty(nameof(MultiLaunch))] public float Step = 0.2f;

        [Desc("如果触发为扇形，则定义扇形范围弧度", "散射"), DependOnProperty(nameof(MultiLaunch))] public float Angle = 0;
        [Desc("如果触发为扇形，则定义扇形范围角度", "散射"), DependOnProperty(nameof(MultiLaunch))] public float Angle360 { get => CMath.RadianToAngle(Angle); set { Angle = CMath.AngleToRadian(value); } }
        //----------------------------------------------------------------------------------------
        public enum LaunchSpellStartDirection : byte
        {
            [Desc("面朝目标（位置或单位）")]
            FaceToTarget = 0,
            [Desc("和发送者一致")]
            Sender = 1,
            [Desc("和最初发射者一致")]
            Launcher = 2,

            [Desc("背朝目标（位置或单位）")]
            ReflectTarget = 3,
            [Desc("和发送者相反")]
            ReflectSender = 4,
            [Desc("和最初发射者相反")]
            ReflectLauncher = 5,

            [Desc("取反射角度")]
            ReflectDirection = 6,
        }
        [Desc("法术起始方向", "发射")] public LaunchSpellStartDirection StartDirection = LaunchSpellStartDirection.Sender;
        [Desc("弹射类子弹反弹角度", "发射")]
        public bool IsReflectAngle
        {
            get => StartDirection == LaunchSpellStartDirection.ReflectDirection;
            set
            {
                if (value)
                {
                    this.StartDirection = LaunchSpellStartDirection.ReflectDirection;
                }
                else if (StartDirection == LaunchSpellStartDirection.ReflectDirection)
                {
                    this.StartDirection = LaunchSpellStartDirection.Sender;
                }
            }
        }
        [Desc("发射初始弧度", "发射")] public float StartAngle = 0;
        [Desc("发射初始角度", "发射")] public float StartAngle360 { get => CMath.RadianToAngle(StartAngle); set { StartAngle = CMath.AngleToRadian(value); } }

        [Desc("随机角度", "发射")] public float RandomAngle = 0;
        [Desc("随机角度", "发射")] public float RandomAngle360 { get => CMath.RadianToAngle(RandomAngle); set { RandomAngle = CMath.AngleToRadian(value); } }


        [Desc("连续触发次数", "连射")] public int RepeatCount = 0;
        [Desc("连续触发间距", "连射")] public int RepeatIntervalMS = 100;

        //----------------------------------------------------------------------------------------
        public float AdjustRandomAngle(Random random)
        {
            if (RandomAngle != 0)
            {
                return random.NextFloat() * RandomAngle - RandomAngle / 2f;
            }
            return 0;
        }

        public enum LaunchSpellSenderUnit : byte
        {
            [Desc("默认")]
            Sender = 0,
            [Desc("强制为施法者")]
            Launcher = 1,
            [Desc("目标，如果是在放技能时")]
            Target = 2,
            [Desc("被攻击者")]
            DamagedUnit = 3,
        }
        [Desc("发射者", "发射者")] public LaunchSpellSenderUnit SenderUnit = LaunchSpellSenderUnit.Sender;
        [Desc("Spell发射Straight Spell时，是否传递速度权重(Normal and Speed)", "炮口")] public bool FromSpellMagnitude = false;
        [Desc("填True，下面无效，发射口是否以单位炮口作为参考", "炮口")] public bool FromUnitBody = true;
        [Desc("法术发射高度（炮口高度）", "炮口"), DependOnProperty(nameof(FromUnitBody), false)] public float LaunchSpellHeight;
        [Desc("法术发射弧度（炮口弧度）", "炮口"), DependOnProperty(nameof(FromUnitBody), false)] public float LaunchSpellAngle = 0;
        [Desc("法术发射半径（炮口半径）", "炮口"), DependOnProperty(nameof(FromUnitBody), false)] public float LaunchSpellRadius = 0;
        [Desc("法术发射角度（炮口角度）", "炮口"), DependOnProperty(nameof(FromUnitBody), false)] public float LaunchSpellAngle360 { get => CMath.RadianToAngle(LaunchSpellAngle); set { LaunchSpellAngle = CMath.AngleToRadian(value); } }
        //----------------------------------------------------------------------------------------
        [DependOnProperty(nameof(ChainLevel))] public bool IsChain { get => ChainLevel > 0; }
        [Desc("连锁等级，只在技能中LaunchSpell有效（闪电链类数量，如果Spell launch spell ID一致，则传递此值）如果为0，则不传递相同的Spell。", "连锁")] public int ChainLevel = 0;
        [Desc("连锁忽略发起者", "连锁")][DependOnProperty(nameof(IsChain))] public bool IgnoreSender;
        [Desc("弹射类子弹忽略弹射本体", "连锁")] public bool InheritDamageTargetList = false; [Desc("连锁结束时的最终触发", "连锁")] public LaunchSpell FinalChainSpell;
        //----------------------------------------------------------------------------------------
        [Desc("重新锁定范围内目标", "锁定")] public FocusTarget ResetSeekingTarget;
        [Desc("自动锁定范围内目标", "锁定")] public bool IsAutoSeekingTarget = false;
        [Desc("自动锁定范围内目标（范围）", "锁定")][DependOnProperty(nameof(IsAutoSeekingTarget))] public float SeekingTargetRange = 10f;
        [Desc("发射这面朝目标", "锁定")][DependOnProperty(nameof(IsAutoSeekingTarget))] public bool SenderFaceToTarget = false;

        //[Desc("自动锁定范围内目标（方式）", "锁定")]
        //         [DependOnProperty(nameof(IsAutoSeekingTarget))]
        //         public SpellTemplate.SeekingExpect SeekingTargetExpect = SpellTemplate.SeekingExpect.Random;
        [Desc("自动锁定范围内目标（方式）", "锁定")][DependOnProperty(nameof(IsAutoSeekingTarget))] public LaunchSkill.SeekingExpect SeekingTargetExpect = LaunchSkill.SeekingExpect.Random;
        [Desc("搜索单位(忽略链中)", "锁定")][DependOnProperty(nameof(IsAutoSeekingTarget))] public bool SeekingIgnoreInChain = false;
        [Desc("自动锁定范围内目标（身体）", "锁定")][DependOnProperty(nameof(IsAutoSeekingTarget))] public SeekingTargetAnchor SeekingAnchor = SeekingTargetAnchor.Waist;
        //----------------------------------------------------------------------------------------

        [Desc("嵌套法术", "嵌套")] public List<LaunchSpell> SubSpells;


        public LaunchSpell() { }

        public override string ToString()
        {
            return "触发法术:" + SpellID + " 数量:" + Count;
        }


    }

}
