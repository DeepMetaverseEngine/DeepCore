using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepMetaGame.Data.GUI.Meta
{
    //-----------------------------------------------------------------------
    [Desc("模板显示框", "战斗")]
    [MessageType(BattleConstants.UETemplateDataBoxMeta)]
    public class UETemplateDataBoxMeta : UEComponentMeta
    {
        [Desc("图标", "Icon")]
        public CPJAtlasMeta ImageAtlas;
        [Desc("图标样式", "Icon")]
        public UIImageStyleMeta ImageStyle;

        [Desc("字体", "文本")]
        public UIFontMeta Font = new UIFontMeta();

        [Desc("文本", "文本-标题")]
        public string Title = "文本-标题";
        [Desc("文本样式", "文本-标题")]
        public UITextStyleMeta TitleStyle = new UITextStyleMeta();

        [Desc("文本", "文本-介绍")]
        public string Text = "文本-介绍";
        [Desc("富文本", "文本-介绍")]
        [RichText]
        public string XmlText;
        [Desc("文本样式", "文本-介绍")]
        public UITextStyleMeta TextStyle = new UITextStyleMeta();


        public UETemplateDataBoxMeta()
        {
            this.Size = new DeepCore.Geometry.Vector2(300, 600);
            this.ImageStyle = new UIImageStyleMeta()
            {
                Align = AlignmentStyle.MiddleCenter,
                Padding = new DeepCore.Geometry.Padding(8, 8, 8, 8),
            };
            this.TitleStyle = new UITextStyleMeta()
            {
                Align = AlignmentStyle.TopCenter,
                Padding = new DeepCore.Geometry.Padding(8, 8, 8, 8),
            };
            this.TextStyle = new UITextStyleMeta()
            {
                Align = AlignmentStyle.BottomCenter,
                Padding = new DeepCore.Geometry.Padding(8, 8, 8, 8),
            };
        }
        public override string GetStringValue()        {            return Title;        }
    }

    //-----------------------------------------------------------------------
    
}
