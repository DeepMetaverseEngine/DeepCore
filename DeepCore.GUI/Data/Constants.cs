using DeepCore.Geometry;

namespace DeepCore.GUI.Data
{
    public static class Constants
    {
        public const int MESSAGE_HEADER = 0x4560000;
    }
    public enum DockStyle
    {
        None = 0,

        Top = 1,
        Bottom = 2,
        Left = 3,
        Right = 4,
        Fill = 5,

        TopLeft = 11,
        TopRight = 12,
        BottomLeft = 13,
        BottomRight = 14,

    }
    //     public enum AnchorStyle
    //     {
    //         None = 0,
    //         Top = 0x01,
    //         Bottom = 0x02,
    //         TopBottom = 0x03,
    //         Left = 0x10,
    //         Right = 0x20,
    //         LeftRight = 0x30,
    //     }
    public enum AlignmentStyle
    {
        None = 0,

        MASK_LEFT    /**/= 0x10,
        MASK_CENTER  /**/= 0x20,
        MASK_RIGHT   /**/= 0x40,
        MASK_TOP     /**/= 0x01,
        MASK_MIDDLE  /**/= 0x02,
        MASK_BOTTOM  /**/= 0x04,

        TopLeft      /**/= MASK_LEFT | MASK_TOP,
        TopCenter    /**/= MASK_CENTER | MASK_TOP,
        TopRight     /**/= MASK_RIGHT | MASK_TOP,
        MiddleLeft   /**/= MASK_LEFT | MASK_MIDDLE,
        MiddleCenter /**/= MASK_CENTER | MASK_MIDDLE,
        MiddleRight  /**/= MASK_RIGHT | MASK_MIDDLE,
        BottomLeft   /**/= MASK_LEFT | MASK_BOTTOM,
        BottomCenter /**/= MASK_CENTER | MASK_BOTTOM,
        BottomRight  /**/= MASK_RIGHT | MASK_BOTTOM,

        //         L_T = TopLeft,
        //         C_T = TopCenter,
        //         R_T = TopRight,
        //         L_C = MiddleLeft,
        //         C_C = MiddleCenter,
        //         R_C = MiddleRight,
        //         L_B = BottomLeft,
        //         C_B = BottomCenter,
        //         R_B = BottomRight,

    }


    public enum TextFontStyle
    {
        //
        // 摘要:
        //     Normal text.
        Plain = 0,
        //
        // 摘要:
        //     Bold text.
        Bold = 1,
        //
        // 摘要:
        //     Italic text.
        Italic = 2,
        BoldAndItalic = Bold | Italic,
        //
        // 摘要:
        //     Underlined text.
        Underline = 4,
        //
        // 摘要:
        //     Text with a line through the middle.
        Strikeout = 8
    }

    public enum TextBorderStyle
    {
        None = 0,
        Shadow = 1,
        Border_4 = 4,
        Border = 8,
        Shadow_L_T = 10,
        Shadow_C_T = 11,
        Shadow_R_T = 12,
        Shadow_L_C = 13,
        Shadow_C_C = 14,
        Shadow_R_C = 15,
        Shadow_L_B = 16,
        Shadow_C_B = 17,
        Shadow_R_B = 18,
    }

    public enum Blend
    {
        BLEND_MODE_NORMAL = 0x00,
        BLEND_MODE_SCREEN = 0x03,
        BLEND_MODE_GRAY = 0xF0,
    }

    public enum Trans
    {
        TRANS_NONE = 0,
        TRANS_ROT90 = 1,
        TRANS_ROT180 = 2,
        TRANS_ROT270 = 3,

        TRANS_MIRROR = 4,
        TRANS_MIRROR_ROT90 = 5,
        TRANS_MIRROR_ROT180 = 6,
        TRANS_MIRROR_ROT270 = 7,

        TRANS_MIRROR_X = TRANS_MIRROR,
        TRANS_MIRROR_Y = TRANS_MIRROR_ROT180,
    };


    public enum UILayoutStyle
    {
        NULL = 0,
        COLOR = 1,
        SPRITE = 2,
        IMAGE_STYLE_ALL_9 = 4,
        IMAGE_STYLE_H_012 = 5,
        IMAGE_STYLE_V_036 = 6,
        IMAGE_STYLE_H_012_345 = 7,
        IMAGE_STYLE_V_036_147 = 8,
        IMAGE_STYLE_BACK_4 = 9,
        IMAGE_STYLE_BACK_4_CENTER = 10,
        IMAGE_STYLE_H_MIRROR = 11,
        IMAGE_STYLE_V_MIRROR = 12,
    }

    public enum GaugeOrientation
    {
        LEFT_2_RIGHT = 0,
        RIGTH_2_LEFT = 1,
        TOP_2_BOTTOM = 2,
        BOTTOM_2_TOP = 3,
        FAN = 4,
    }


    public static class GUIDataUtil
    {
        public static RectangleF AlignTo(this AlignmentStyle anchor, RectangleF rect, RectangleF container)
        {
            switch (anchor)
            {
                case DeepCore.GUI.Data.AlignmentStyle.TopLeft:
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleLeft:
                    container.Y = (container.Y + container.Height / 2) - rect.Height / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomLeft:
                    container.Y = (container.Y + container.Height) - rect.Height;
                    break;

                case DeepCore.GUI.Data.AlignmentStyle.TopCenter:
                    container.X = (container.X + container.Width / 2) - rect.Width / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleCenter:
                    container.X = (container.X + container.Width / 2) - rect.Width / 2;
                    container.Y = (container.Y + container.Height / 2) - rect.Height / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomCenter:
                    container.X = (container.X + container.Width / 2) - rect.Width / 2;
                    container.Y = (container.Y + container.Height) - rect.Height;
                    break;

                case DeepCore.GUI.Data.AlignmentStyle.TopRight:
                    container.X = (container.X + container.Width) - rect.Width;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleRight:
                    container.X = (container.X + container.Width) - rect.Width;
                    container.Y = (container.Y + container.Height / 2) - rect.Height / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomRight:
                    container.X = (container.X + container.Width) - rect.Width;
                    container.Y = (container.Y + container.Height) - rect.Height;
                    break;
            }
            return container;
        }
        public static RectangleF AlignTo(this AlignmentStyle anchor, RectangleF rect, Vector2 container)
        {
            switch (anchor)
            {
                case DeepCore.GUI.Data.AlignmentStyle.TopLeft:
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleLeft:
                    container.Y = (container.Y) - rect.Height / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomLeft:
                    container.Y = (container.Y) - rect.Height;
                    break;

                case DeepCore.GUI.Data.AlignmentStyle.TopCenter:
                    container.X = (container.X) - rect.Width / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleCenter:
                    container.X = (container.X) - rect.Width / 2;
                    container.Y = (container.Y) - rect.Height / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomCenter:
                    container.X = (container.X) - rect.Width / 2;
                    container.Y = (container.Y) - rect.Height;
                    break;

                case DeepCore.GUI.Data.AlignmentStyle.TopRight:
                    container.X = (container.X) - rect.Width;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleRight:
                    container.X = (container.X) - rect.Width;
                    container.Y = (container.Y) - rect.Height / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomRight:
                    container.X = (container.X) - rect.Width;
                    container.Y = (container.Y) - rect.Height;
                    break;
            }
            return new RectangleF(container, rect.Size);
        }

        public static RectangleF AlignTo(this AlignmentStyle anchor, Vector2 size, RectangleF container)
        {
            switch (anchor)
            {
                case DeepCore.GUI.Data.AlignmentStyle.TopLeft:
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleLeft:
                    container.Y = (container.Y + container.Y / 2) - size.Y / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomLeft:
                    container.Y = (container.Y + container.Y) - size.Y;
                    break;

                case DeepCore.GUI.Data.AlignmentStyle.TopCenter:
                    container.X = (container.X + container.X / 2) - size.X / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleCenter:
                    container.X = (container.X + container.X / 2) - size.X / 2;
                    container.Y = (container.Y + container.Y / 2) - size.Y / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomCenter:
                    container.X = (container.X + container.X / 2) - size.X / 2;
                    container.Y = (container.Y + container.Y) - size.Y;
                    break;

                case DeepCore.GUI.Data.AlignmentStyle.TopRight:
                    container.X = (container.X + container.X) - size.X;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleRight:
                    container.X = (container.X + container.X) - size.X;
                    container.Y = (container.Y + container.Y / 2) - size.Y / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomRight:
                    container.X = (container.X + container.X) - size.X;
                    container.Y = (container.Y + container.Y) - size.Y;
                    break;
            }
            return container;
        }
        public static RectangleF AlignTo(this AlignmentStyle anchor, Vector2 size, Vector2 container)
        {
            switch (anchor)
            {
                case DeepCore.GUI.Data.AlignmentStyle.TopLeft:
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleLeft:
                    container.Y = (container.Y) - size.Y / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomLeft:
                    container.Y = (container.Y) - size.Y;
                    break;

                case DeepCore.GUI.Data.AlignmentStyle.TopCenter:
                    container.X = (container.X) - size.X / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleCenter:
                    container.X = (container.X) - size.X / 2;
                    container.Y = (container.Y) - size.Y / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomCenter:
                    container.X = (container.X) - size.X / 2;
                    container.Y = (container.Y) - size.Y;
                    break;

                case DeepCore.GUI.Data.AlignmentStyle.TopRight:
                    container.X = (container.X) - size.X;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleRight:
                    container.X = (container.X) - size.X;
                    container.Y = (container.Y) - size.Y / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomRight:
                    container.X = (container.X) - size.X;
                    container.Y = (container.Y) - size.Y;
                    break;
            }
            return new RectangleF(container, size);
        }
    }


}
