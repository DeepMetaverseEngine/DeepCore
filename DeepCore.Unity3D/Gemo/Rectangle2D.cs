using System;

namespace DeepCore.GUI.Gemo
{
    public class Rectangle2D
    {
        public float x;

        public float y;

        public float width;

        public float height;

        public Rectangle2D()
        {
            this.x = 0;
            this.y = 0;
            this.width = 0;
            this.height = 0;
        }

        public float Right
        {
            get { return x + width; }
        }

        public float Bottom
        {
            get { return y + height; }
        }

        public static implicit operator Rectangle2D(in DeepCore.Geometry.RectangleF rect)
        {
            return new Rectangle2D(rect.X, rect.Y, rect.width, rect.height);
        }
        public static implicit operator Rectangle2D(in DeepCore.Geometry.Rectangle rect)
        {
            return new Rectangle2D(rect.X, rect.Y, rect.width, rect.height);
        }
        public Rectangle2D(Rectangle2D r)
        {
            this.x = r.x;
            this.y = r.y;
            this.width = r.width;
            this.height = r.height;
        }

        public Rectangle2D(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public Rectangle2D(float width, float height)
        {
            this.x = 0;
            this.y = 0;
            this.width = width;
            this.height = height;
        }


        public void setBounds(Rectangle2D r)
        {
            setBounds(r.x, r.y, r.width, r.height);
        }

        public void setBounds(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public bool contains(float x, float y)
        {
            return inside(x, y);
        }

        public bool contains(Rectangle2D r)
        {
            return contains(r.x, r.y, r.width, r.height);
        }

        public bool contains(float X, float Y, float W, float H)
        {
            float w = this.width;
            float h = this.height;

            float x = this.x;
            float y = this.y;
            if (X < x || Y < y)
            {
                return false;
            }
            w += x;
            W += X;
            if (W <= X)
            {
                if (w >= x || W > w)
                    return false;
            }
            else
            {
                if (w >= x && W > w)
                    return false;
            }
            h += y;
            H += Y;
            if (H <= Y)
            {
                if (h >= y || H > h)
                    return false;
            }
            else
            {
                if (h >= y && H > h)
                    return false;
            }
            return true;
        }


        public bool inside(float X, float Y)
        {
            float w = this.width;
            float h = this.height;

            float x = this.x;
            float y = this.y;
            if (X < x || Y < y)
            {
                return false;
            }
            w += x;
            h += y;
            return ((w < x || w > X) && (h < y || h > Y));
        }

        public bool floatersects(Rectangle2D r)
        {
            float tw = this.width;
            float th = this.height;
            float rw = r.width;
            float rh = r.height;
            if (rw <= 0 || rh <= 0 || tw <= 0 || th <= 0)
            {
                return false;
            }
            float tx = this.x;
            float ty = this.y;
            float rx = r.x;
            float ry = r.y;
            rw += rx;
            rh += ry;
            tw += tx;
            th += ty;
            // overflow || floatersect
            return ((rw < rx || rw > tx) && (rh < ry || rh > ty)
                    && (tw < tx || tw > rx) && (th < ty || th > ry));
        }


        public Rectangle2D floatersection(Rectangle2D r)
        {
            return floatersection(r.x, r.y, r.width, r.height);
        }

        public Rectangle2D floatersection(float x, float y, float w, float h)
        {
            float tx1 = this.x;
            float ty1 = this.y;
            float rx1 = x;
            float ry1 = y;
            float tx2 = tx1;
            tx2 += this.width;
            float ty2 = ty1;
            ty2 += this.height;
            float rx2 = rx1;
            rx2 += w;
            float ry2 = ry1;
            ry2 += h;
            if (tx1 < rx1)
                tx1 = rx1;
            if (ty1 < ry1)
                ty1 = ry1;
            if (tx2 > rx2)
                tx2 = rx2;
            if (ty2 > ry2)
                ty2 = ry2;
            tx2 -= tx1;
            ty2 -= ty1;
            // tx2,ty2 will never overflow (they will never be
            // larger than the smallest of the two source w,h)
            // they might underflow, though...
            if (tx2 < float.MinValue)
                tx2 = float.MinValue;
            if (ty2 < float.MinValue)
                ty2 = float.MinValue;
            return new Rectangle2D(tx1, ty1, (float)tx2, (float)ty2);
        }


        public Rectangle2D union(Rectangle2D r)
        {
            float tx2 = this.width;
            float ty2 = this.height;

            float rx2 = r.width;
            float ry2 = r.height;


            float tx1 = this.x;
            float ty1 = this.y;
            tx2 += tx1;
            ty2 += ty1;
            float rx1 = r.x;
            float ry1 = r.y;
            rx2 += rx1;
            ry2 += ry1;
            if (tx1 > rx1) tx1 = rx1;
            if (ty1 > ry1) ty1 = ry1;
            if (tx2 < rx2) tx2 = rx2;
            if (ty2 < ry2) ty2 = ry2;
            tx2 -= tx1;
            ty2 -= ty1;
            // tx2,ty2 will never underflow since both original rectangles
            // were already proven to be non-empty
            // they might overflow, though...
            if (tx2 > float.MaxValue) tx2 = float.MaxValue;
            if (ty2 > float.MaxValue) ty2 = float.MaxValue;
            return new Rectangle2D(tx1, ty1, (float)tx2, (float)ty2);
        }

        public bool isEmpty()
        {
            return (width <= 0) || (height <= 0);
        }

        public Rectangle2D createfloatersection(Rectangle2D r)
        {
            return floatersection(r);
        }

        public Rectangle2D createUnion(Rectangle2D r)
        {
            return union(r);
        }

        public bool Equals(Rectangle2D r)
        {
            if (r != null)
            {
                return ((x == r.x) &&
                        (y == r.y) &&
                        (width == r.width) &&
                        (height == r.height));
            }
            return false;
        }

        override public String ToString()
        {
            return "Rectangle[x=" + x + ",y=" + y + ",width=" + width + ",height=" + height + "]";
        }

        public Rectangle2D Clone()
        {
            return new Rectangle2D(x, y, width, height);
        }

        public static bool IntersectRectWH(
            float sx1, float sy1, float sw, float sh,
            float dx1, float dy1, float dw, float dh)
        {
            float sx2 = sx1 + sw;
            float sy2 = sy1 + sh;
            float dx2 = dx1 + dw;
            float dy2 = dy1 + dh;
            if (sx2 < dx1) return false;
            if (sx1 > dx2) return false;
            if (sy2 < dy1) return false;
            if (sy1 > dy2) return false;
            return true;
        }

        public static bool IncludeRectPointWH(
            float sx1, float sy1,
            float sw, float sh,
            float dx, float dy)
        {
            float sx2 = sx1 + sw;
            float sy2 = sy1 + sh;
            if (sx2 < dx) return false;
            if (sx1 > dx) return false;
            if (sy2 < dy) return false;
            if (sy1 > dy) return false;
            return true;
        }

    }
}

