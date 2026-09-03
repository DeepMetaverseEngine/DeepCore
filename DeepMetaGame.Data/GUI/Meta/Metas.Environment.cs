using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepMetaGame.Data.GUI.Meta
{

    //-----------------------------------------------------------------------

    public abstract class UEEnvironmentListMeta : UEListMeta
    {
        public UEEnvironmentListMeta()
        {
            this.Width = 100;
            this.Height = 200;
            this.Layout.Style = UILayoutStyle.NULL;
            this.ItemAlign = AlignmentStyle.MiddleLeft;
            this.ContentOrientation = ListOrientation.Vertical;
            this.ItemSize = new Vector2(100, 24);
        }
    }
    [Desc("场景环境变量列表", "战斗")]
    [MessageType(BattleConstants.UeZoneEnvironmentListMeta)]
    public class UeZoneEnvironmentListMeta : UEEnvironmentListMeta
    {

    }
    [Desc("单位环境变量列表", "战斗")]
    [MessageType(BattleConstants.UeUnitEnvironmentListMeta)]
    public class UeUnitEnvironmentListMeta : UEEnvironmentListMeta
    {

    }

    //-----------------------------------------------------------------------
    [Desc("场景环境变量", "战斗")]
    [MessageType(BattleConstants.UeZoneEnvironmentLabelMeta)]
    public class UeZoneEnvironmentLabelMeta : UETextComponentMeta
    {
        public string Key;
    }
    [Desc("单位环境变量", "战斗")]
    [MessageType(BattleConstants.UeUnitEnvironmentLabelMeta)]
    public class UeUnitEnvironmentLabelMeta : UETextComponentMeta
    {
        public string Key;
    }
}
