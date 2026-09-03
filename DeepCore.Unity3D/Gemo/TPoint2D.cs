using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepCore.GUI.Gemo
{
   public struct TPoint2D
    {
        public float X;
        public float Y;
    
        public TPoint2D(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public float Length
        {
            get { return Distance(this, new TPoint2D(0, 0)); }
        }

        /// <summary>
        /// make x y swap
        /// </summary>
        public void swap()
        {
            float t = X;
            this.X = Y;
            this.Y = t;
        }

        public void flip()
        {
            this.X = -X;
            this.Y = -Y;
        }

        public static float Distance(TPoint2D p1, TPoint2D p2)
        {
            double a = p2.X - p1.X;
            double b = p2.Y - p1.Y;
            double rlt = Math.Pow(a, 2) + Math.Pow(b, 2);
            rlt = Math.Sqrt(rlt);
            return (float)rlt;
        }

        private readonly static TPoint2D c_Zero = new TPoint2D();
        public static TPoint2D Zero
        {
            get
            {
                return c_Zero;
            }
        }
   }
}
