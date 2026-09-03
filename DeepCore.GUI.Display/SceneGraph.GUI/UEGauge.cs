using DeepCore.GUI.Data;
using DeepCore.GUI.SceneGraph;
using System;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    [UEInstance(typeof(UEGaugeMeta))]
    public class UEGauge : UEDisplayNode<UEGaugeMeta>
    {
        public UITextLayer TextLayer { get; private set; }
        public UILayout LayoutGauge { get; protected set; }
        public float GaugeRate
        {
            get => (float)((Meta.GaugeValue - Meta.GaugeMin) / (Meta.GaugeMax - Meta.GaugeMin));
        }

        public UEGauge(UIFactory editor, UEGaugeMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.Text;
        }
        protected override void DoBindData(string key, object value)
        {
            if (value is float ftext)
            {
                this.Meta.GaugeValue = ftext;
            }
            else
            {
                Meta.Text = $"{value}";
            }
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.TextLayer = Editor.CreateTextLayer(Meta.Text, Meta.Font, Meta.TextStyle);
            this.AutoRelease(this.TextLayer);
            this.LayoutGauge = Editor.CreateLayout(Meta.GaugeLayout);
            this.AutoRelease(this.LayoutGauge);
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            DrawGaugeStrip(args);
            DrawGaugeText(args);
        }

        protected virtual void DrawGaugeStrip(GraphicsArgs args)
        {
            var grate = (float)((Meta.GaugeValue - Meta.GaugeMin) / (Meta.GaugeMax - Meta.GaugeMin));
            {
                var gbounds = this.Meta.GaugePadding.Cut(this.LocalBounds);
                //                 switch (Meta.Orientation)
                //                 {
                //                     case GaugeOrientation.RIGTH_2_LEFT:
                //                     case GaugeOrientation.LEFT_2_RIGHT:
                //                     case GaugeOrientation.TOP_2_BOTTOM:
                //                     case GaugeOrientation.BOTTOM_2_TOP:
                //                         break;
                //                 }
                //                 gbounds.width = gbounds.Width * grate;
                this.LayoutGauge?.RenderGauge(args.Graphics, gbounds, Meta.Orientation, grate);
            }
        }

        protected virtual void DrawGaugeText(GraphicsArgs args)
        {
            if (this.TextLayer != null)
            {
                var grate = GaugeRate;
                if (this.Meta.ShowPercent)
                {
                    try
                    {
                        var pct = (int)(100f * grate);
                        this.TextLayer.Text = $"{Meta.Text} {string.Format(Meta.ShowPercentFormat, pct)}";
                    }
                    catch (Exception err)
                    {
                        this.TextLayer.Text = err.Message;
                    }
                }
                else
                {
                    this.TextLayer.Text = Meta.Text;
                }
                this.TextLayer.Render(args.Graphics, this.LocalBounds);
            }
        }
    }
}
