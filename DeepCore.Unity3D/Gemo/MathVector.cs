using System;

namespace DeepCore.GUI.Gemo
{
	public class MathVector
	{
		/**
		 * 移动指定偏移
		 * @param v
		 * @param dx x距离
		 * @param dy y距离
		 */
		public static void move(Point2D v, float dx, float dy){
			v.x += (dx);
			v.y += (dy);
		}
		
		/**
	 * 通过极坐标来移动
	 * @param v
	 * @param degree 弧度
	 * @param distance 距离
	 */
		public static void movePolar(Point2D v, float degree, float distance){
			float dx = (float) Math.Cos(degree) * distance;
			float dy = (float) Math.Sin(degree) * distance;
			move(v, dx, dy);
		}

		/**
	 * 通过极坐标来移动
	 * @param v
	 * @param degree 弧度
	 * @param speed  速度 (单位距离/秒)
	 * @param interval_ms 毫秒时间
	 */
		public static void movePolar(Point2D v, float degree, float speed, int interval_ms) {
			float distance = getDistance(speed, interval_ms);
			movePolar(v, degree, distance);
		}
		
		/**
	 * 向目标移动
	 * @param v
	 * @param x 目标x
	 * @param y 目标y
	 * @return 是否到达目的地
	 */
		public static bool moveTo(Point2D v, float x, float y, float distance){
			float ddx = x - v.x;
			float ddy = y - v.y;
			if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance) {
				v.x = (x);
				v.y = (y);
				return true;
			} else {
				float angle = (float)Math.Atan2(ddy, ddx);
				movePolar(v, angle, distance);
				return false;
			}
		}
		
		
		public static bool moveToX(Point2D v, float x, float distance){
			float ddx = x - v.x;
			if (Math.Abs(ddx) < distance) {
				v.x = (x);
				return true;
			} else {
				if (ddx > 0) {
					v.x += (distance);
				} else {
					v.x += (-distance);
				}
				return false;
			}
		}
		public static bool moveToY(Point2D v, float y, float distance){
			float ddy = y - v.y;
			if (Math.Abs(ddy) < distance) {
				v.y = (y);
				return true;
			} else {
				if (ddy > 0) {
					v.y += (distance);
				} else {
					v.y += (-distance);
				}
				return false;
			}
		}
		
		/**
	 * 向量缩放
	 * @param v
	 * @param scale
	 */
		public static void scale(Point2D v, float scale){
			v.x = (v.x * scale);
			v.y = (v.y * scale);
		}
		
		/**
	 * 向量缩放
	 * @param v
	 * @param scale
	 */
		public static void scale(Point2D v, float scale_x, float scale_y){
			v.x = (v.x * scale_x);
			v.y = (v.y * scale_y);
		}
		
		/**
	 * 向量按照{0,0}点旋转
	 * @param v
	 * @param degree 弧度
	 */
		public static void rotate(Point2D v, float degree)
		{
			float cos_v = (float)Math.Cos(degree);
			float sin_v = (float)Math.Sin(degree);
			float x = (v.x) * cos_v - (v.y) * sin_v;
			float y = (v.y) * cos_v + (v.x) * sin_v; 
			v.x = (x);
			v.y = (y);
		}
		
		/**
	 * 向量按照p0点旋转
	 * @param v
	 * @param p0
	 * @param degree 弧度
	 */
		public static void rotate(Point2D v, Point2D p0, float degree)
		{
			float dx = v.x - p0.x;
			float dy = v.y - p0.y;
			float cos_v = (float)Math.Cos(degree);
			float sin_v = (float)Math.Sin(degree);
			float x = p0.x + dx * cos_v - dy * sin_v;
			float y = p0.y + dy * cos_v + dx * sin_v;
			v.x = (x);
			v.y = (y);
		}
		
		/**
	 * 向量按照p0点旋转
	 * @param v
	 * @param p0
	 * @param degree 弧度
	 */
		public static void rotate(Point2D v, float px, float py, float degree) {
			float dx = v.x - px;
			float dy = v.y - py;
			float cos_v = (float)Math.Cos(degree);
			float sin_v = (float)Math.Sin(degree);
			float x = px + dx * cos_v - dy * sin_v;
			float y = py + dy * cos_v + dx * sin_v;
			v.x = (x);
			v.y = (y);
		}
		/**
	 * 得到速度和时间产生的距离
	 * @param speed 速度 (单位距离/秒)
	 * @param interval_ms 毫秒时间
	 * @return
	 */
		public static float getDistance(float speed, int interval_ms){
			float rate = interval_ms / 1000f ;
			return speed * rate;
		}
		
		public static float getDistance(Point2D v) {
			return getDistance(0, 0, v.x, v.y);
		}
		
		public static float getDistance(float x1, float y1, float x2, float y2){
			float r1 = x1-x2;
			float r2 = y1-y2;
			return (float)Math.Sqrt(r1*r1+r2*r2);
		}
		
		
		/**
		 * 得到弧度
	 * @param dx x向量
	 * @param dy y向量
	 * @return
	 */
		public static float getDegree(float dx, float dy)
		{
			return (float)Math.Atan2(dy, dx);
		}
		
		/**
	 * 得到弧度
	 * @param v 向量
	 * @return
	 */
		public static float getDegree(Point2D v)
		{
			return (float)Math.Atan2(v.y, v.x);
		}
		
		
		/**
	 * 将2个向量相加得到一个新的向量
	 * @param a
	 * @param b
	 * @return
	 */
		public static Point2D vectorAdd(Point2D a, Point2D b){
			Point2D v = new Point2D();
			v.x = (a.x + b.x);
			v.y = (a.y + b.y);
			return v;
		}
		
		/**
	 * 将2个向量相减得到一个新的向量
	 * @param a
	 * @param b
	 * @return
	 */
		public static Point2D vectorSub(Point2D a, Point2D b){
			Point2D v = new Point2D();
			v.x = (a.x - b.x);
			v.y = (a.y - b.y);
			return v;
		}
		
		/**
	 * 将一个向量加上新的向量，得到一个新的向量
	 * @param a
	 * @param degree
	 * @param distance
	 * @return
	 */
		public static Point2D vectorAdd(Point2D a, float degree, float distance){
			Point2D v = new Point2D();
			v.x = (a.x);
			v.y = (a.y);
			movePolar(v, degree, distance);
			return v;
		}
		
		/**
	 * 把一个向量向自己本身的方向相加，得到一个新的向量
	 * @param a
	 * @param distance
	 * @return
	 */
		public static Point2D vectorAdd(Point2D a, float distance){
			Point2D v = new Point2D();
			v.x = (a.x);
			v.y = (a.y);
			movePolar(v, getDegree(v), distance);
			return v;
		}
		
		/**
	 * 将一个向量缩放一定比率后，得到一个新的向量
	 * @param a
	 * @param scale
	 * @return
	 */
		public static Point2D vectorScale(Point2D a, float scale){
			Point2D v = new Point2D();
			v.x = (a.x * scale);
			v.y = (a.y * scale);
			return v;
		}
		
		
		
		
		
		
		
		
		
		
		
		
		
		

	}
}

