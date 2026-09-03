using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.GUI.Display;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using DeepMetaGame.Data.GUI.Meta;
using DeepMetaGame.Slave.GUI;
using System.Threading.Tasks;

namespace DeepMetaGame.Display.GUI
{

    [UEInstance(typeof(UEUnitStatisticsMeta))]
    public class UEUnitStatisticsInfo : UEInfo<UEUnitStatisticsMeta>
    {
        public InstanceUnit BindingUnit { get; private set; }
        public UEUnitStatisticsInfo(UIFactory editor, UEUnitStatisticsMeta e) : base(editor, e)
        {
        }
        sealed protected override void DoBindData(string key, object value)
        {
            if (value is LayerUnit unit && unit.EventSender is InstanceUnit zu)
            {
                BindingUnit = zu;
            }
            else
            {
                BindingUnit = null;
            }
        }
        protected override string GetInfoText()
        {
            if (BindingUnit != null)
            {
                var st = BindingUnit.Statistic;
                return st.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
    }

    [UEInstance(typeof(UEUnitSyncInfoMeta))]
    public class UEUnitSyncInfo : UEInfo<UEUnitSyncInfoMeta>
    {
        public LayerUnit BindingUnit { get; private set; }
        public UEUnitSyncInfo(UIFactory editor, UEUnitSyncInfoMeta e) : base(editor, e)
        {
        }
        sealed protected override void DoBindData(string key, object value)
        {
            if (value is LayerUnit unit )
            {
                BindingUnit = unit;
            }
            else
            {
                BindingUnit = null;
            }
        }
        protected override string GetInfoText()
        {
            if (BindingUnit != null)
            {
                var st = BindingUnit.ToStatusText();
                return st.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
    }

}
