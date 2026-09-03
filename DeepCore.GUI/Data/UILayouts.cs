using DeepCore.IO;
using DeepCore.Xml;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using DeepCore.Geometry;
using DeepCore.GUI.Display;
using DeepCore.GUI.Cell;

namespace DeepCore.GUI.Data
{
    //----------------------------------------------------------------------------------------------------------


    [MessageType(Constants.MESSAGE_HEADER + 0x1000)]
    public class UILayoutMeta : ISerializable
    {
        [Desc("样式")]
        public UILayoutStyle Style = UILayoutStyle.COLOR;

        [Desc("背景色")]
        public Color BackColor = Color.Silver;
        [Desc("边宽")]
        public int BorderSize = 1;
        [Desc("边框色")]
        public Color BorderColor = Color.SlateGray;

        [Desc("九宫格切片")]
        public Padding ImageClip = new Padding(8,8,8,8);
        [Desc("图片平铺")]
        public bool ImageRepeat = false;

        [Desc("图集")]
        public CPJAtlasMeta ImageAtlas;

        public override string ToString()
        {
            return $"[{Style}]";
        }
    }

    [MessageType(Constants.MESSAGE_HEADER + 0x1001)]
    public class UIFontMeta : ISerializable
    {
        [Desc("文本字体")]
        public string FontName;
        [Desc("文本字体样式")]
        public TextFontStyle Style = TextFontStyle.Plain;
        [Desc("文本字体大小")]
        public float Size = 12;
        [FilePath]
        public string CPJFile ;
        public string ImagesName;
        public override string ToString()
        {
            return $"[{FontName}][{Style}][{Size}]";
        }
    }

    [MessageType(Constants.MESSAGE_HEADER + 0x1002)]
    public class UITextStyleMeta : ISerializable
    {
        [Desc("对齐方式")]
        public AlignmentStyle Align = AlignmentStyle.MiddleCenter;
        [Desc("颜色")]
        public Color TextColor = Color.Beige;
        [Desc("描边样式")]
        public TextBorderStyle BorderStyle = TextBorderStyle.Border;
        [Desc("文本描边颜色")]
        public Color BorderColor = Color.MidnightBlue;
        [Desc("绘制偏移量")]
        public Padding Padding = new Padding(4, 4, 4, 4);
        public override string ToString()
        {
            return $"[{Align}][{BorderStyle}]";
        }
    }

    [MessageType(Constants.MESSAGE_HEADER + 0x1003)]
    public class UIImageStyleMeta : ISerializable
    {
        [Desc("对齐方式")]
        public AlignmentStyle Align = AlignmentStyle.MiddleCenter;

        [Desc("停靠方式")]
        public DockStyle Dock = DockStyle.None;

        [Desc("旋转角度")]
        public float Rotate = 0;
        [Desc("缩放")]
        public Vector2 Scale = Vector2.One;
        [Desc("绘制偏移量")]
        public Padding Padding = new Padding(4, 4, 4, 4);
        public override string ToString()
        {
            return $"[{Align}]";
        }
    }

    [MessageType(Constants.MESSAGE_HEADER + 0x1004)]
    public class CPJAtlasMeta : ISerializable
    {
        [FilePath]
        public string ImagePath;
        [FilePath]
        public string CPJFile;
        public string ImagesName ;
        public int ImageIndex;
        public string ImageKey;
        public string SpriteName ;
        public int SpriteAnimIndex = 0;
        public string SpriteAnimKey ;
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(CPJFile))
            {
                if (!string.IsNullOrEmpty(SpriteName))
                {
                    return $"[{Path.GetFileName(CPJFile)}][{SpriteName}][{SpriteAnimIndex}]";
                }
                if (!string.IsNullOrEmpty(ImagesName))
                {
                    return $"[{Path.GetFileName(CPJFile)}][{ImagesName}][{ImageKey}][{ImageIndex}]";
                }
            }
            if (!string.IsNullOrEmpty(ImagePath))
            {
                return $"[{Path.GetFileName(ImagePath)}]";
            }
            return "[NULL]";
        }
    }


}