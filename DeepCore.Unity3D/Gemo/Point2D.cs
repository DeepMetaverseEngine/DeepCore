using System;

namespace DeepCore.GUI.Gemo
{
    public class Point2D
    {
        public float x;

        public float y;

        public Point2D()
        {

        }
        public Point2D(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float Length
        {
            get { return Distance(this, new Point2D(0, 0)); }
        }

        /// <summary>
        /// make x y swap
        /// </summary>
        public void swap()
        {
            float t = x;
            this.x = y;
            this.y = t;
        }

        public void flip()
        {
            this.x = -x;
            this.y = -y;
        }

        public static float Distance(Point2D p1, Point2D p2)
        {

            double a = p2.x - p1.x;
            double b = p2.y - p1.y;
            double rlt = Math.Pow(a, 2) + Math.Pow(b, 2);
            rlt = Math.Sqrt(rlt);
            return (float)rlt;
        }

    }
}

