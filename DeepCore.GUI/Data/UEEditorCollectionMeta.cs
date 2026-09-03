using DeepCore.Geometry;
using DeepCore.GUI.Display;
using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Data
{

    public enum ListOrientation
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
    }

    public abstract class UEListMeta : UEComponentMeta
    {
        [Desc("内容间距", "内容")]
        public Padding ContentPadding = Padding.One;
        [Desc("内容对齐方式", "内容")]
        public AlignmentStyle ContentAlign = AlignmentStyle.MiddleCenter;
        [Desc("内容排列方式", "内容")]
        public ListOrientation ContentOrientation = ListOrientation.Horizontal;

        [Desc("组件每个间隔", "组件")]
        public Padding ItemMargin = Padding.One;
        [Desc("组件状态布局", "组件")]
        public UILayoutMeta ItemLayout;
        [Desc("组件图标样式", "组件")]
        public UIImageStyleMeta ItemImageStyle;
        [Desc("组件文本字体", "组件")]
        public UIFontMeta ItemFont = new UIFontMeta();
        [Desc("组件文本样式", "组件")]
        public UITextStyleMeta ItemTextStyle = new UITextStyleMeta();
        [Desc("组件尺寸", "组件")]
        public Vector2 ItemSize = new Vector2(50, 50);
        [Desc("组件方式对齐", "组件")]
        public AlignmentStyle ItemAlign = AlignmentStyle.MiddleCenter;
        [Desc("组件是否填充", "组件")]
        public bool ItemSizeToFit = true;

        public UEListMeta()
        {
            this.Width = 300;
            this.Height = 60;
            this.Layout.Style = UILayoutStyle.NULL;
        }

    }



    [Desc("文本列表控件", "集合控件")]
    [MessageType(Constants.MESSAGE_HEADER + 0x101)]
    public class UETextListMeta : UEListMeta
    {
        [Desc("列表文本", "文本列表")]
        public string[] Items;

        public UETextListMeta()
        {
            this.Width = 100;
            this.Height = 200;
            this.Layout.Style = UILayoutStyle.NULL;
            this.ItemAlign = AlignmentStyle.MiddleLeft;
            this.ContentOrientation = ListOrientation.Vertical;
            this.ItemSize = new Vector2(100, 24);
        }
    }
}
