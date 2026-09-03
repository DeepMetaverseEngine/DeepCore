using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Geometry
{
    public struct Padding
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;
        public float L { get => Left; set => Left = value; }
        public float T { get => Top; set => Top = value; }
        public float R { get => Right; set => Right = value; }
        public float B { get => Bottom; set => Bottom = value; }
        public float ALL { set => Left = Right = Top = Bottom = value; }
        public Vector2 LT => new Vector2(L, T);
        public Vector2 LB => new Vector2(L, B);
        public Vector2 RT => new Vector2(R, T);
        public Vector2 RB => new Vector2(R, B);
        public Vector2 CutSize => new Vector2(L + R, T + B);
        public float CutWidth => L + R;
        public float CutHeight => T + B;
        public Padding(float padding)
        {
            this.Left = padding;
            this.Top = padding;
            this.Right = padding;
            this.Bottom = padding;
        }
        public Padding(float left, float top, float right, float bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }

        public static Padding Zero = new Padding() { Left = 0, Right = 0, Top = 0, Bottom = 0, };
        public static Padding One = new Padding() { Left = 1, Right = 1, Top = 1, Bottom = 1, };



        public override string ToString()
        {
            return $"{{{Left}, {Top}, {Right}, {Bottom}}}";
        }

        public RectangleF Cut(in RectangleF src)
        {
            return new RectangleF()
            {
                X = src.x + Left,
                Y = src.y + Top,
                Width = src.Width - Right - Left,
                height = src.height - Bottom - Top,
            };
        }
    }
}
