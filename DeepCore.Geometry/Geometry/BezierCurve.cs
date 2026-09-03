using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Geometry
{
    public abstract class IBezierCurve : Recyclable
    {
        protected readonly List<Vector3> templist = new List<Vector3>();
        /// <summary>
        /// 计算t(0~1)时的曲线点
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public abstract Vector3 PointAt(float t);
        protected override void Disposing()
        {
            templist.Clear();
        }
        /// <summary>
        /// 按照步长采样
        /// </summary>
        /// <param name="points"></param>
        /// <param name="step"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public int SampleStep(ICollection<Vector3> points, float step, int n = 100)
        {
            float len = EstimateLength(n);
            return Sample(points, (int)MathF.Ceiling(len / step));
        }
        /// <summary>
        /// 按照步长采样
        /// </summary>
        /// <param name="step"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public List<Vector3> SampleStep(float step, int n = 100)
        {
            var points = new List<Vector3>();
            SampleStep(points, step, n);
            return points;
        }

        /// <summary>
        /// 采样n个点
        /// </summary>
        /// <param name="points"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public int Sample(ICollection<Vector3> points, int n = 100)
        {
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                points.Add(PointAt(t));
                count++;
            }
            return count;
        }

        /// <summary>
        /// 采样n个点
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public List<Vector3> Sample(int n = 100)
        {
            var points = new List<Vector3>();
            Sample(points, n);
            return points;
        }

        /// <summary>
        /// 用采样点近似计算曲线长度：
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public float EstimateLength(int n = 100)
        {
            float len = 0;
            try
            {
                Sample(templist, n);
                for (int i = 1; i < templist.Count; i++)
                {
                    float dx = templist[i].X - templist[i - 1].X;
                    float dy = templist[i].Y - templist[i - 1].Y;
                    len += (float)Math.Sqrt(dx * dx + dy * dy);
                }
            }
            finally
            {
                templist.Clear();
            }
            return len;
        }

        /// <summary>
        /// 切线是对t求导，可以用微分近似：
        /// </summary>
        /// <param name="t"></param>
        /// <param name="delta"></param>
        /// <returns></returns>
        public Vector3 TangentAt(float t, float delta = 1e-4f)
        {
            float t1 = Math.Max(0, t - delta);
            float t2 = Math.Min(1, t + delta);
            var p1 = PointAt(t1);
            var p2 = PointAt(t2);
            return new Vector3(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
        }

        /// <summary>
        /// 切线是对t求导，可以用微分近似：
        /// </summary>
        /// <param name="t"></param>
        /// <param name="delta"></param>
        /// <returns></returns>
        public Vector3 NormalAt(float t, float delta = 1e-4f)
        {
            var tan = TangentAt(t, delta);
            // 法线为切线逆时针旋转90度
            return new Vector3(-tan.Y, tan.X, tan.Z);
        }
    }

    public class BezierCurve : IBezierCurve
    {
        public List<Vector3> ControlPoints { get; }
        public BezierCurve()
        {
            this.ControlPoints = new List<Vector3>();
        }
        public BezierCurve(IEnumerable<Vector3> controlPoints)
        {
            Init(controlPoints);
        }
        public BezierCurve Init(IEnumerable<Vector3> controlPoints)
        {
            this.ControlPoints.AddRange(controlPoints);
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.ControlPoints.Clear();
        }
        public override Vector3 PointAt(float t)
        {
            return DeCasteljau(ControlPoints, t);
        }

        // De Casteljau算法递归实现
        protected virtual Vector3 DeCasteljau(List<Vector3> points, float t)
        {
            if (points.Count == 1)
                return points[0];
            if (AllocPools != null)
            {
                using (var next = AllocPools.AllocList<Vector3>())
                {
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        float x = (1 - t) * points[i].X + t * points[i + 1].X;
                        float y = (1 - t) * points[i].Y + t * points[i + 1].Y;
                        float z = (1 - t) * points[i].Z + t * points[i + 1].Z;
                        next.Add(new Vector3(x, y, z));
                    }
                    return DeCasteljau(next, t);
                }
            }
            else
            {
                var next = new List<Vector3>();
                for (int i = 0; i < points.Count - 1; i++)
                {
                    float x = (1 - t) * points[i].X + t * points[i + 1].X;
                    float y = (1 - t) * points[i].Y + t * points[i + 1].Y;
                    float z = (1 - t) * points[i].Z + t * points[i + 1].Z;
                    next.Add(new Vector3(x, y, z));
                }
                return DeCasteljau(next, t);
            }
        }
    }

    public class CubicBezier : IBezierCurve
    {
        public Vector3 P0, P1, P2, P3;
        public CubicBezier()
        {
        }
        public CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Init(p0, p1, p2, p3);
        }
        public CubicBezier Init(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            P0 = p0; P1 = p1; P2 = p2; P3 = p3;
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            P0 = default;
            P1 = default;
            P2 = default;
            P3 = default;
        }

        // 计算t(0~1)时的曲线点
        public override Vector3 PointAt(float t)
        {
            float x = (float)(
                Math.Pow(1 - t, 3) * P0.X +
                3 * Math.Pow(1 - t, 2) * t * P1.X +
                3 * (1 - t) * t * t * P2.X +
                t * t * t * P3.X
            );
            float y = (float)(
                Math.Pow(1 - t, 3) * P0.Y +
                3 * Math.Pow(1 - t, 2) * t * P1.Y +
                3 * (1 - t) * t * t * P2.Y +
                t * t * t * P3.Y
            );
            float z = (float)(
                Math.Pow(1 - t, 3) * P0.Z +
                3 * Math.Pow(1 - t, 2) * t * P1.Z +
                3 * (1 - t) * t * t * P2.Z +
                t * t * t * P3.Z
            );
            return new Vector3(x, y, z);
        }

    }

}
