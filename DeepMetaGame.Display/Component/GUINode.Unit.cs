using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Geometry;
using DeepCore.GUI.Display;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.GUI.Meta;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DeepMetaGame.Display.GUI
{
    //--------------------------------------------------------------------------------------------------------------------
    public abstract class UEUnitStatusList<M> : UEListView<M> where M : UEUnitStatusListMeta
    {
        public LayerUnit BindingUnit { get; private set; }
        public UILayout LayoutGaugeItem { get; protected set; }
        protected UEUnitStatusList(UIFactory editor, M e) : base(editor, e)
        {
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.LayoutGaugeItem = Editor.CreateLayout(Meta.ItemGaugeLayout);
            this.AutoRelease(this.LayoutGaugeItem);
        }
        sealed protected override void DoBindData(string key, object value)
        {
            ClearItems();
            if (value is LayerUnit unit)
            {
                BindingUnit = unit;
                DoBindData(key, unit);
            }
            else
            {
                BindingUnit = null;
            }
        }
        protected abstract float GetPercent(ListItem item);
        protected abstract void DoBindData(string key, LayerUnit unit);
        protected override void DrawItem(GraphicsArgs args, ListItem item, in RectangleF itemBounds)
        {
            LayoutItem?.Render(args.Graphics, itemBounds);
            if (item.Icon?.Image != null)
            {
                args.Graphics.BeginImage(item.Icon.Image);
                args.Graphics.DrawImageZoom(itemBounds);
            }
            LayoutGaugeItem?.RenderGauge(args.Graphics, itemBounds, Meta.ItemGauge, GetPercent(item));
            if (Meta.ItemTextStyle != null) { args.Graphics.SetColor(Meta.ItemTextStyle.TextColor); }
            args.Graphics.DrawString(item.Text, itemBounds, Meta.ItemAlign);
        }
    }
    //--------------------------------------------------------------------------------------------------------------------

    [UEInstance(typeof(UEUnitSkillListMeta))]
    public class UEUnitSkillList : UEUnitStatusList<UEUnitSkillListMeta>
    {
        public UEUnitSkillList(UIFactory editor, UEUnitSkillListMeta e) : base(editor, e)
        {
        }
        protected override void DoBindData(string key, LayerUnit unit)
        {
            using (var skills = unit.ObjectPool.AllocList<LayerUnit.SkillState>())
            {
                unit.GetSkillStatus(skills);
                foreach (var skill in skills)
                {
                    AddItem(new ListItem(skill)
                    {
                        Text = skill.Data.Name,
                        Icon = Editor.AddImage(skill.Data.IconName),
                    });
                }
            }
        }
        protected override float GetPercent(ListItem item)
        {
            if (item.Tag is LayerUnit.SkillState skill)
            {
                return skill.CDAmount;
            }
            return 1;
        }
    }
    //--------------------------------------------------------------------------------------------------------------------
    [UEInstance(typeof(UEUnitBuffListMeta))]
    public class UEUnitBuffList : UEUnitStatusList<UEUnitBuffListMeta>
    {
        public UEUnitBuffList(UIFactory editor, UEUnitBuffListMeta e) : base(editor, e)
        {
        }
        protected override void DoBindData(string key, LayerUnit unit)
        {
            using (var buffs = unit.ObjectPool.AllocList<LayerUnit.BuffState>())
            {
                unit.GetBuffStatus(buffs);
                foreach (var buff in buffs)
                {
                    AddItem(new ListItem(buff)
                    {
                        Text = buff.Data.Name,
                        Icon = Editor.AddImage(buff.Data.IconName),
                    });
                }
            }
        }
        protected override float GetPercent(ListItem item)
        {
            if (item.Tag is LayerUnit.BuffState buff)
            {
                return buff.CDAmount;
            }
            return 1;
        }
    }

    //--------------------------------------------------------------------------------------------------------------------


}
