using DeepCore.Reflection;

namespace DeepMetaGame.Data.Misc
{
    [Desc("攻击范围类型")]
    public enum AttackShape : byte
    {
        [Desc("单体")] Single = 255,
        [Desc("圆形")] Round = 0,
        [Desc("扇形")] Fan = 1,

        [Desc("胶囊条状")] Strip = 2,
        [Desc("胶囊射线（以原点出去）")] StripRay = 3,
        [Desc("胶囊射线，接触到最近")] StripRayTouchEnd = 4,



        [Desc("连线类型，比如激光塔持续造成伤害")] LineToTarget = 5,
        [Desc("圆环，中间是空的")] Circle = 6,
        [Desc("连线类型，比如伸出去的钩子")] LineToStart = 7,
        [Desc("连线类型，比如伸出去的钩子，链接施法者")] LineToSender = 8,
        [Desc("连线类型，比如激光塔持续造成伤害")] LineToTargetPos = 9,

        [Desc("方形条状")] RectStrip = 12,
        [Desc("方形射线（以原点出去）")] RectStripRay = 13,
        [Desc("横向胶囊条状")] WideStrip = 14,

    }

    [Desc("攻击原因")]
    public enum AttackReason : byte
    {
        [Desc("范围攻击或者技能攻击")]
        Attack = 0,
        [Desc("搜索范围内可攻击目标")]
        Look = 1,
        [Desc("被攻击后反击")]
        Damaged = 2,
        [Desc("移动被阻挡后攻击")]
        MoveBlocked = 3,
        [Desc("检测仇恨目标")]
        Tracing = 4,
    }

}
