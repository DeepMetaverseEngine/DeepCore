using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepMetaGame.Data.GUI.Meta
{
    //-----------------------------------------------------------------------
    public abstract class UEUnitStatusListMeta : UEListMeta
    {
        [Desc("状态CD显示前景", "单位状态")]
        public UILayoutMeta ItemGaugeLayout = new UILayoutMeta()
        {
            Style = UILayoutStyle.COLOR,
            BackColor = Color.LemonChiffon,
        };
        [Desc("状态CD显示类型", "单位状态")]
        public GaugeOrientation ItemGauge = GaugeOrientation.FAN;
    }


    [Desc("BUFF列表", "战斗")]
    [MessageType(BattleConstants.UEUnitBuffListMeta)]
    public class UEUnitBuffListMeta : UEUnitStatusListMeta
    {

    }

    [Desc("技能列表", "战斗")]
    [MessageType(BattleConstants.UEUnitSkillListMeta)]
    public class UEUnitSkillListMeta : UEUnitStatusListMeta
    {

    }

    //-----------------------------------------------------------------------



    [Desc("HP血量条", "战斗")]
    [MessageType(BattleConstants.UEUnitHPBar)]
    public class UEUnitHPBar : UEGaugeMeta
    {

    }
    [Desc("MP法力条", "战斗")]
    [MessageType(BattleConstants.UEUnitMPBar)]
    public class UEUnitMPBar : UEGaugeMeta
    {

    }
    [Desc("经验条", "战斗")]
    [MessageType(BattleConstants.UEUnitExpBar)]
    public class UEUnitExpBar : UEGaugeMeta
    {

    }
    [Desc("体力条", "战斗")]
    [MessageType(BattleConstants.UEUnitSPBar)]
    public class UEUnitSPBar : UEGaugeMeta
    {

    }

}
