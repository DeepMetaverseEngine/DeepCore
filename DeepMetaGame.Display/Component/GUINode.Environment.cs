using DeepCore.Game3D.Slave.Layer;
using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.GUI.Meta;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepMetaGame.Display.GUI
{
    //--------------------------------------------------------------------------------------------------------------------
    public abstract class UEEnvironmentList<M> : UEListView<M> where M : UEEnvironmentListMeta
    {
        protected UEEnvironmentList(UIFactory editor, M e) : base(editor, e)
        {
        }
        public void BindEnvironmentObject(IEnumerable<KeyValuePair<string, object>> list)
        {
            foreach (var kv in list)
            {
                this.AddItem(new ListItem(kv)
                {
                    Text = $"{kv.Key} = {kv.Value}"
                }); ;
            }
        }
        protected override void DrawItem(GraphicsArgs args, ListItem item, in RectangleF itemBounds)
        {
            base.DrawItem(args, item, itemBounds);
        }
    }
    //--------------------------------------------------------------------------------------------------------------------
    [UEInstance(typeof(UeZoneEnvironmentListMeta))]
    public class UeZoneEnvironmentList : UEEnvironmentList<UeZoneEnvironmentListMeta>
    {
        public UeZoneEnvironmentList(UIFactory editor, UeZoneEnvironmentListMeta e) : base(editor, e)
        {
        }
        sealed protected override void DoBindData(string key, object value)
        {
            ClearItems();
            if (value is LayerZone zone)
            {
                base.BindEnvironmentObject(zone.ListEnvironmentValues());
            }
        }
    }
    //--------------------------------------------------------------------------------------------------------------------
    [UEInstance(typeof(UeUnitEnvironmentListMeta))]
    public class UeUnitEnvironmentList : UEEnvironmentList<UeUnitEnvironmentListMeta>
    {
        public UeUnitEnvironmentList(UIFactory editor, UeUnitEnvironmentListMeta e) : base(editor, e)
        {
        }
        sealed protected override void DoBindData(string key, object value)
        {
            ClearItems();
            if (value is LayerUnit unit)
            {
                base.BindEnvironmentObject(unit.ListEnvironmentValues());
                if (value is LayerPlayer player)
                {
                    base.BindEnvironmentObject(player.ListPlayerEnvironmentValues());
                }
            }
        }
    }
    //--------------------------------------------------------------------------------------------------------------------
    [UEInstance(typeof(UeZoneEnvironmentLabelMeta))]
    public class UEZoneEnvironmentVarLabel : UETextComponent<UeZoneEnvironmentLabelMeta>
    {
        LayerZone zone;
        public UEZoneEnvironmentVarLabel(UIFactory editor, UeZoneEnvironmentLabelMeta e) : base(editor, e)
        {
        }
        protected override void OnUpdate(UpdateArgs args)
        {
            if (Components.TryGetComponentAs<ZoneGUINode>(out var zone) && zone.Layer.EnvironmentVarMap.TryGetEnvironmentVar(Meta.Key, out var var))
            {
                Meta.Text = $"{var}";
            }
            else
            {
                Meta.Text = string.Empty;
            }
            base.OnUpdate(args);
        }
        sealed protected override void DoBindData(string key, object value)
        {
            if (value is LayerZone zone)
            {
                this.zone = zone;
            }
        }
    }

    [UEInstance(typeof(UeUnitEnvironmentLabelMeta))]
    public class UEUnitEnvironmentVarLabel : UETextComponent<UeUnitEnvironmentLabelMeta>
    {
        LayerUnit unit;
        public UEUnitEnvironmentVarLabel(UIFactory editor, UeUnitEnvironmentLabelMeta e) : base(editor, e)
        {
        }
        protected override void OnUpdate(UpdateArgs args)
        {
            if (unit != null && unit.EnvironmentVarMap.TryGetEnvironmentVar(Meta.Key, out var var))
            {
                Meta.Text = $"{var}";
            }
            else
            {
                Meta.Text = string.Empty;
            }
            base.OnUpdate(args);
        }
        sealed protected override void DoBindData(string key, object value)
        {
            if (value is LayerUnit unit)
            {
                this.unit = unit;
            }
        }
    }
    //--------------------------------------------------------------------------------------------------------------------
}
