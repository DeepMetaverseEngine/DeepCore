using DeepCore.Geometry;
using DeepCore.GUI.Display;
using DeepCore.IO;
using DeepCore.Reflection;
using System.Collections.Generic;

namespace DeepCore.GUI.Data
{
    //----------------------------------------------------------------------------------------------------------
    #region Base----------------------------------------------------------------------------------------------
    public abstract class UEComponentMeta : ISerializable
    {
        [Desc(Category = "0.Type", Desc = "类型", Editable = true)] public string TypeName => this.GetType().Name;
        [Desc(Editable = false)] public string EditorName;

        [Desc("X", "0.位置")]
        public float X = 0;
        [Desc("Y", "0.位置")]
        public float Y = 0;
        [Desc("Z", "0.位置")]
        public float Z = 0;
        [Desc("宽", "0.位置")]
        public float Width = 100;
        [Desc("高", "0.位置")]
        public float Height = 100;

        [Desc("Position", "0.位置", Editable = true)]
        public Vector3 Position
        {
            get => new Vector3(X, Y, Z);
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }
        [Desc("Size", "0.位置", Editable = true)]
        public Vector2 Size
        {
            get => new Vector2(Width, Height);
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }


        [Desc("是否可见", "1.基础 属性")]
        public bool Visible = true;
        [Desc("组件是否可用", "1.基础 属性")]
        public bool Enable = true;
        [Desc("提示文本", "1.基础 属性")]
        public string ToolTipText;


        [Desc("与父节点对齐方式", "2.布局")]
        public AlignmentStyle Anchor = AlignmentStyle.None;
        [Desc("与父节点停靠方式", "2.布局")]
        public DockStyle Dock = DockStyle.None;
        [Desc("与父节点停靠方式", "2.布局")]
        public Padding Margin = new Padding(8, 8, 8, 8);

        [Desc("默认状态布局", "3.显示")]
        public UILayoutMeta Layout = new UILayoutMeta();
        [Desc("禁用状态布局", "3.显示")]
        public UILayoutMeta DisableLayout;

        [Desc("附加数据", "扩展")]
        public string UserData;
        [Desc("附加标记", "扩展")]
        public int UserTag;
        [Desc("附加属性", "扩展")]
        public string[] Attributes;

        [Desc("附加能力", "能力")]
        [NotNull]
        public ArrayList<IUEComponentMeta> Abilities = new ArrayList<IUEComponentMeta>();

        [Desc("触发事件文本", "触发事件")]
        public string DialogResult;

        [Desc("锁定", "编辑时")]
        public bool Edit_EnableLock;

        //--------------------------------------------------------------
        // Default data 
        public virtual string GetStringValue() => string.Empty;
        public virtual bool GetBoolValue() => false;
        public virtual double GetNumberValue() => 0;
        //--------------------------------------------------------------
    }
    public abstract class UEContainerMeta : UEComponentMeta
    {
        [Desc("组件子节点是否可用", "1.基础 属性")]
        public bool EnableChilds = true;
        [Desc(Editable = false)]
        public List<UEComponentMeta> Childs = new List<UEComponentMeta>(1);
        [Desc("内容间距", "2.布局")]
        public Padding Padding = new Padding(4, 4, 4, 4);

    }
    public abstract class UETextBoxBaseMeta : UEComponentMeta
    {
        [Desc("内容间距", "TextBox 基础属性")]
        public Padding Padding = new Padding(4, 4, 4, 4);
        [Desc("字体", "TextBox 基础属性")]
        public UIFontMeta Font = new UIFontMeta();
        [Desc("文本样式", "TextBox 基础属性")]
        public UITextStyleMeta TextStyle = new UITextStyleMeta();
    }
    public abstract class UETextInputBaseMeta : UEComponentMeta
    {
        [Desc("占位符", "TextInput 属性")]
        public string PlaceHolder = "Input Text";
        [Desc("文本", "TextInput 基础属性")]
        public string Text = "";
        [Desc("字体样式", "TextInput 基础属性")]
        public UIFontMeta Font;
        [Desc("文本样式", "TextInput 基础属性")]
        public UITextStyleMeta TextStyle = new UITextStyleMeta();
        [Desc("是否开启密码样式", "TextInput 基础属性")]
        public bool IsPassword = false;
        public override string GetStringValue()
        {
            return Text;
        }
    }
    abstract public class UETextComponentMeta : UEComponentMeta
    {
        [Desc("文本", "TextComponent 基础属性")]
        public string Text;
        [Desc("字体样式", "TextComponent 基础属性")]
        public UIFontMeta Font;
        [Desc("文本样式", "TextComponent 基础属性")]
        public UITextStyleMeta TextStyle = new UITextStyleMeta();
        public override string GetStringValue()
        {
            return Text;
        }
    }
    abstract public class UEButtonMeta : UEComponentMeta
    {
        [Desc("按钮默认文本", "Button 属性")]
        public string Text = "Button";
        [Desc("按钮按下时文本", "Button 属性")]
        public string DownText;
        [Desc("Font", "Button 属性")]
        public UIFontMeta Font;
        [Desc("TextStyle", "Button 属性")]
        public UITextStyleMeta TextStyle = new UITextStyleMeta();

        [Desc("按钮按下时布局", "Button 属性")]
        public UILayoutMeta DownLayout = new UILayoutMeta() { BackColor = Color.DeepSkyBlue };

        public UEButtonMeta()
        {
            base.Layout.BackColor = Color.DodgerBlue;
        }
        public override string GetStringValue()
        {
            return Text;
        }
    }

    #endregion Base----------------------------------------------------------------------------------------------
    //----------------------------------------------------------------------------------------------------------
    [MessageType(Constants.MESSAGE_HEADER + 1)]
    [Desc("编辑器根节点", "容器")]
    public class UERootMeta : UEContainerMeta
    {
        [Desc("标题文字", "Root 属性")]
        public string Text;
        public UERootMeta()
        {
            this.Width = 800;
            this.Height = 600;
        }
        public override string GetStringValue()
        {
            return Text;
        }
    }
    [MessageType(Constants.MESSAGE_HEADER + 2)]
    [Desc("编辑器容器", "容器")]
    public class UECanvasMeta : UEContainerMeta
    {
        public UECanvasMeta()
        {
            this.Width = 500;
            this.Height = 500;
        }
    }
    [MessageType(Constants.MESSAGE_HEADER + 3)]
    [Desc("滚动框", "容器")]
    public class UEScrollPanMeta : UEContainerMeta
    {
        [Desc("是否开启弹性", "ScrollPan 属性")]
        public bool EnableElasticity = true;
        [Desc("是否开启横向滚动", "ScrollPan 属性")]
        public bool EnableScrollH = false;
        [Desc("是否开启纵向滚动", "ScrollPan 属性")]
        public bool EnableScrollV = true;
        [Desc("是否显示滚动条滑块", "ScrollPan 属性")]
        public bool ShowSlider = true;
        [Desc("滚动条淡出淡入持续时间", "ScrollPan 属性")]
        public float ScrollFadeTimeMax;
        [Desc("滚动条纵向布局", "ScrollPan 属性")]
        public UILayoutMeta ScrollVLayout;
        [Desc("滚动条横向布局", "ScrollPan 属性")]
        public UILayoutMeta ScrollHLayout;
        public UEScrollPanMeta()
        {
            this.Width = 500;
            this.Height = 500;
        }
    }

    //----------------------------------------------------------------------------------------------------------
    [MessageType(Constants.MESSAGE_HEADER + 4)]
    [Desc("图片框", "基础控件")]
    public class UEImageBoxMeta : UEComponentMeta
    {
        [Desc("图集", "ImageBox 属性")]
        public CPJAtlasMeta ImageAtlas;
        [FilePath]
        [Desc("UIAsset", "ImageBox 属性")]
        public string UIAssetPath;
        [Desc("图片样式", "ImageBox 属性")]
        public UIImageStyleMeta ImageStyle;
        public UEImageBoxMeta()
        {
            this.Width = 200;
            this.Height = 200;
            this.Layout.Style = UILayoutStyle.NULL;
        }
    }
    //----------------------------------------------------------------------------------------------------------

    [MessageType(Constants.MESSAGE_HEADER + 5)]
    [Desc("文本输入框", "基础控件")]
    public class UETextInputMeta : UETextInputBaseMeta
    {
        public UETextInputMeta()
        {
            this.Width = 200;
            this.Height = 32;
        }
    }
    [MessageType(Constants.MESSAGE_HEADER + 6)]
    [Desc("多行文本输入框", "基础控件")]
    public class UETextInputMultilineMeta : UETextInputBaseMeta
    {
        public UETextInputMultilineMeta()
        {
            this.Width = 200;
            this.Height = 200;
        }
    }
    //----------------------------------------------------------------------------------------------------------

    [MessageType(Constants.MESSAGE_HEADER + 7)]
    [Desc("普通多行文本", "基础控件")]
    public class UETextBoxMeta : UETextBoxBaseMeta
    {
        [Desc("文本行", "TextBox 属性", Editable = true)]
        public string[] TextLines { get => CUtils.StringToLines(Text); set => Text = CUtils.StringFromLines(value); }

        [Desc("文本", "TextBox 属性")]
        public string Text = "Text Line 1\nText Line 2";

        public UETextBoxMeta()
        {
            this.Width = 300;
            this.Height = 300;
            this.Layout.Style = UILayoutStyle.COLOR;
            this.Layout.BackColor = Color.DarkGray;
            this.TextStyle.Align = AlignmentStyle.TopLeft;
        }
        public override string GetStringValue()
        {
            return Text;
        }
    }
    [MessageType(Constants.MESSAGE_HEADER + 8)]
    [Desc("多行富文本", "基础控件")]
    public class UERichTextBoxMeta : UETextBoxBaseMeta
    {
        [Desc("富文本", "XmlText 属性")]
        [RichText]
        public string XmlText = "Attributed Text";
        public UERichTextBoxMeta()
        {
            this.Width = 300;
            this.Height = 300;
            this.Layout.Style = UILayoutStyle.COLOR;
            this.Layout.BackColor = Color.DarkGray;
        }
        public override string GetStringValue()
        {
            return XmlText;
        }
    }
    //----------------------------------------------------------------------------------------------------------
    [MessageType(Constants.MESSAGE_HEADER + 9)]
    [Desc("引用控件", "基础控件")]
    public class UEReferenceNodeMeta : UEComponentMeta
    {
        [Desc("组件GUID", "Reference 属性")]
        public string ReferenceGUID;
        public UEReferenceNodeMeta()
        {
            this.Width = 500;
            this.Height = 500;
            this.Layout.Style = UILayoutStyle.NULL;
        }
        public override string GetStringValue()
        {
            return ReferenceGUID;
        }
    }
    //----------------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------------

    [MessageType(Constants.MESSAGE_HEADER + 10)]
    [Desc("文本标签", "基础控件")]
    public class UELabelMeta : UETextComponentMeta
    {
        public UELabelMeta()
        {
            this.Text = string.IsNullOrEmpty(Text) ? "Label" : Text;
            this.Width = 100;
            this.Height = 32;
            this.Layout.Style = UILayoutStyle.NULL;
        }
    }

    [MessageType(Constants.MESSAGE_HEADER + 11)]
    [Desc("进度", "基础控件")]
    public class UEGaugeMeta : UETextComponentMeta
    {
        [Desc("进度条最大值", "Gauge 属性")]
        public float GaugeMax = 1f;
        [Desc("进度条最小值", "Gauge 属性")]
        public float GaugeMin = 0f;
        [Desc("进度条当前值", "Gauge 属性")]
        public float GaugeValue = 0.5f;
        [Desc("自定义布局", "Gauge 属性")]
        public UILayoutMeta GaugeLayout = new UILayoutMeta() { BackColor = Color.Green };
        [Desc("进度条样式", "Gauge 属性")]
        public GaugeOrientation Orientation = GaugeOrientation.LEFT_2_RIGHT;
        [Desc("进度条内容间隔", "Gauge 属性")]
        public Padding GaugePadding = new Padding(2, 2, 2, 2);
        [Desc("是否显示百分比数字", "Gauge 属性")]
        public bool ShowPercent = true;
        [Desc("是否显示百分比数字", "Gauge 属性")]
        public string ShowPercentFormat = "{0}%";
        public UEGaugeMeta()
        {
            this.Text = "Gauge";
            this.Width = 200;
            this.Height = 32;
        }
        public override double GetNumberValue() => GaugeValue;
    }

    [MessageType(Constants.MESSAGE_HEADER + 12)]
    [Desc("单选框", "基础控件")]
    public class UECheckBoxMeta : UETextComponentMeta
    {

        [Desc("是否已选中", "CheckBox 属性")]
        public bool IsChecked;

        [Desc("Check文本", "CheckBox 属性")]
        public string TextChecked = "✔";
        [Desc("Check文本", "CheckBox 属性")]
        public string TextUnchecked = "";
        [Desc("Check文本", "CheckBox 属性")]
        public UITextStyleMeta CheckTextStyle = new UITextStyleMeta()
        {
            Align = AlignmentStyle.MiddleLeft,
            Padding = new Padding(8, 8, 8, 8),
            TextColor = Color.Green,
        };

        [Desc("选择图片底图", "CheckBox 属性")]
        public CPJAtlasMeta ImageBackAtlas;
        [Desc("已选择时的贴图", "CheckBox 属性")]
        public CPJAtlasMeta ImageCheckedAtlas;
        [Desc("未选择时的贴图", "CheckBox 属性")]
        public CPJAtlasMeta ImageUncheckedAtlas;

        [Desc("图片样式", "CheckBox 属性")]
        public UIImageStyleMeta ImageStyle;

        public UECheckBoxMeta()
        {
            this.Text = "Check Box";
            this.Width = 200;
            this.Height = 32;
            this.ImageStyle = new UIImageStyleMeta()
            {
                Align = AlignmentStyle.MiddleLeft,
            };
        }
        public override bool GetBoolValue() => IsChecked;
    }
    //----------------------------------------------------------------------------------------------------------



    [MessageType(Constants.MESSAGE_HEADER + 13)]
    [Desc("按钮", "基础控件")]
    public class UETextButtonMeta : UEButtonMeta
    {
        public UETextButtonMeta()
        {
            this.Text = "Text Button";
            this.Width = 200;
            this.Height = 48;
        }
    }

    [MessageType(Constants.MESSAGE_HEADER + 14)]
    [Desc("状态按钮", "基础控件")]
    public class UEToggleButtonMeta : UEButtonMeta
    {
        [Desc("是否已选择", "ToggleButton 属性")]
        public bool IsChecked;
        public UEToggleButtonMeta()
        {
            this.Text = "Toggle Button";
            this.Width = 200;
            this.Height = 48;
        }
        public override bool GetBoolValue() => IsChecked;
    }
    //----------------------------------------------------------------------------------------------------------
}