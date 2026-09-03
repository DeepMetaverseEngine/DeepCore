using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepMetaGame.Data.GUI.Meta
{

    [Desc("战斗统计", "HOST战斗")]
    [MessageType(BattleConstants.UEUnitStatisticsMeta)]
    public class UEUnitStatisticsMeta : UETextBoxBaseMeta
    {

    }
    [Desc("战斗基础状态", "HOST战斗")]
    [MessageType(BattleConstants.UEUnitSyncInfoMeta)]
    public class UEUnitSyncInfoMeta : UETextBoxBaseMeta
    {

    }
}
