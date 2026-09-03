using DeepCore.Geometry;
using DeepCore.IO;
using System;

namespace DeepCore.Space
{
    public static class GridMatrix
    {
        public delegate bool ForEachCellsRayStepPredicate<T, ST>(ST input, T t, int cx, int cy, Vector2 current);
        public delegate void CellStepAction<ST>(ref ST input, Vector2 current);
        /// <summary>
        /// 线性延伸，扫描线段经过的所有节点，不进行碰撞检测
        /// </summary>
        public static T ForEachCellsRayStepPloar<T, ST>(
            this T[,] matrix2D,
            ref ST input,
            ref Vector2 pos, float dir, float len, float gridSize,
            ForEachCellsRayStepPredicate<T, ST> action,
            CellStepAction<ST> stepAction,
            bool breakOutBounds = true) where T : class
        {
            var inside = false;
            var step = gridSize / 2f;
            int old_bx = -1;
            int old_by = -1;
            var xcount = matrix2D.GetLength(0);
            var ycount = matrix2D.GetLength(1);
            var lastNode = default(T);
            var lastNodeX = default(T);
            var lastNodeY = default(T);
            var oldPos = pos;
            for (int ttt = xcount * ycount; ttt >= 0; --ttt)
            {
                var bx = CMath.CycDiv(pos.X, gridSize);
                var by = CMath.CycDiv(pos.Y, gridSize);
                if (bx < 0 || by < 0 || bx >= xcount || by >= ycount)
                {
                    if (inside || breakOutBounds)
                    {
                        //从里到外，则直接Break//
                        break;
                    }
                    else if (len > 0)
                    {
                        var rstep = Math.Min(step, len);
                        VectorHelper.MovePolar(ref pos, dir, step);
                        len -= rstep;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    inside = true;
                    if (bx != old_bx || by != old_by)
                    {
                        var node = matrix2D[bx, by];
                        if (node != null)
                        {
                            if ((node != lastNode && node != lastNodeX && node != lastNodeY))
                            {
                                if (action(input, node, bx, by, pos))
                                {
                                    pos = oldPos;
                                    return node;
                                }
                                lastNode = node;
                            }
                            if (old_bx >= 0)
                            {
                                node = matrix2D[old_bx, by];
                                if (node != null)
                                {
                                    if ((node != lastNode && node != lastNodeX && node != lastNodeY))
                                    {
                                        if (action(input, node, old_bx, by, pos))
                                        {
                                            pos = oldPos;
                                            return node;
                                        }
                                        lastNodeX = node;
                                    }
                                }
                            }
                            if (old_by >= 0)
                            {
                                node = matrix2D[bx, old_by];
                                if (node != null)
                                {
                                    if ((node != lastNode && node != lastNodeX && node != lastNodeY))
                                    {
                                        if (action(input, node, bx, old_by, pos))
                                        {
                                            pos = oldPos;
                                            return node;
                                        }
                                        lastNodeY = node;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (action(input, node, bx, by, pos))
                            {
                                pos = oldPos;
                                return node;
                            }
                        }
                        old_bx = bx;
                        old_by = by;
                    }
                    stepAction?.Invoke(ref input, pos);
                    if (len <= 0) break;
                    if (len > 0)
                    {
                        var rstep = Math.Min(step, len);
                        VectorHelper.MovePolar(ref pos, dir, rstep);
                        len -= rstep;
                    }
                }
            }
            return default(T);
        }
        public static T ForEachCellsRayStepPloar<T, ST>(
            this T[,] matrix2D,
            ref ST input,
            ref Vector2 pos, Vector2 target, float gridSize,
            ForEachCellsRayStepPredicate<T, ST> action,
            CellStepAction<ST> stepAction,
            bool breakOutBounds = true) where T : class
        {
            var dir = VectorHelper.GetDegree(pos, target);
            var len = Vector2.Distance(pos, target);
            return ForEachCellsRayStepPloar(matrix2D, ref input, ref pos, dir, len, gridSize, action, stepAction, breakOutBounds);
        }


        /*
        public static void LCD_Draw_BLine1<ST>(float x1, float y1, float x2, float y2, ref ST input, ForEachPredicateT<ST, T, int, int, float, float> action)
        {
            float t;
            float row = x1;
            float col = y1;
            float err = 0, xymax, xymin, delta_x, delta_y;  //定义偏量、xy中较大/较小值、x/y变化量
            float incx, incy;                          //x/y变化方向

            if (x2 > x1) { delta_x = x2 - x1; incx = 1; }       //分别获取两个方向的位移(正值)和方向变量
            else if (x2 == x1) { delta_x = 0; incx = 0; }
            else { delta_x = x1 - x2; incx = -1; }

            if (y2 >= y1) { delta_y = y2 - y1; incy = 1; }
            else if (y2 == y1) { delta_y = 0; incy = 0; }
            else { delta_y = y1 - y2; incy = -1; }

            if (delta_x > delta_y) { xymax = delta_x; xymin = delta_y; }  //区分最大轴(参考轴)位移、最小轴位移
            else { xymax = delta_y; xymin = delta_x; }

            for (t = 0; t <= xymax; t++)
            {
                LCD_Draw_Fill_Circle(row, col, size, color);      //描点函数(row,col)
                err += 2 * xymin;
                row += incx;
                col += incy;
                if (err > xymax) err -= 2 * xymax;
                else
                {
                    if (xymax == delta_x) col -= incy;
                    else row -= incx;
                }
            }
        }

        public static void LCD_Draw_BLine2(u16 x1, u16 y1, u16 x2, u16 y2, u8 size, u16 color)
        {
            u16 t;
            int xerr = 0, yerr = 0, delta_x, delta_y, xymax;
            int incx, incy, row, col;
            delta_x = x2 - x1; //计算坐标增量 
            delta_y = y2 - y1;
            row = x1;
            col = y1;
            if (delta_x > 0) incx = 1;            //取方向变量 
            else if (delta_x == 0) incx = 0;      //垂直线 
            else { incx = -1; delta_x = -delta_x; }
            if (delta_y > 0) incy = 1;
            else if (delta_y == 0) incy = 0;      //水平线 
            else { incy = -1; delta_y = -delta_y; }
            if (delta_x > delta_y) xymax = delta_x;            //选取参考轴 
            else xymax = delta_y;

            for (t = 0; t <= xymax; t++)
            {
                LCD_Draw_Fill_Circle(row, col, size, color); //描点函数(row,col)
                xerr += 2 * delta_x;
                yerr += 2 * delta_y;
                if (xerr > xymax)
                {
                    xerr -= 2 * xymax;
                    row += incx;
                }
                if (yerr > xymax)
                {
                    yerr -= 2 * xymax;
                    col += incy;
                }
            }
        }
        */














        public delegate bool TryGetMatrix3D<T, ST>(ST st, int x, int y, int z, out T data);

        private struct TryRayCast3DElement<T>
        {
            public Vector3 pos;
            public Location3D loc;
            public T data;
        }
        /// <summary>
        /// 线性延伸，扫描线段经过的所有节点，不进行碰撞检测
        /// </summary>
        public static bool TryRayCast3D<T, ST>(ST st,
            AbstractCollectionPool pool,
            TryGetMatrix3D<T, ST> matrix3D,
            Vector3 gridSize,
            RayCast ray,
            BreakPredicate<T, Location3D, Vector3, ST> action, out T touch)
        {
            var step_normal = Vector3.Normalize(ray.normal);
            var gridHalf = gridSize / 2f;
            float step = CMath.Min(gridSize.X, gridSize.Y, gridSize.Z) / 2f;

            var pos = ray.center;
            var len = ray.distance;

            var cpos = ray.center;
            var bx = CMath.CycDiv(pos.X, gridSize.X);
            var by = CMath.CycDiv(pos.Y, gridSize.Y);
            var bz = CMath.CycDiv(pos.Z, gridSize.Z);
            cpos.X = bx * gridSize.X + gridHalf.X;
            cpos.Y = by * gridSize.Y + gridHalf.Y;
            cpos.Z = bz * gridSize.Z + gridHalf.Z;
            if (matrix3D.Invoke(st, bx, by, bz, out var node))
            {
                if (action(node, new Location3D(bx, by, bz), pos, st))
                {
                    touch = node;
                    return true;
                }
            }
            int old_bx = bx;
            int old_by = by;
            int old_bz = bz;
            var old_cpos = cpos;
            using (var testMap = pool.AllocMap<Location3D, T>())
            {
                using (var testarray = pool.AllocList<TryRayCast3DElement<T>>())
                {
                    while (len > 0)
                    {
                        var rstep = Math.Min(step, len);
                        pos = pos + (step_normal * rstep);
                        len -= rstep;

                        bx = CMath.CycDiv(pos.X, gridSize.X);
                        by = CMath.CycDiv(pos.Y, gridSize.Y);
                        bz = CMath.CycDiv(pos.Z, gridSize.Z);

                        if (bx != old_bx || by != old_by || bz != old_bz)
                        {
                            cpos.X = bx * gridSize.X + gridHalf.X;
                            cpos.Y = by * gridSize.Y + gridHalf.Y;
                            cpos.Z = bz * gridSize.Z + gridHalf.Z;
                            {
                                _try_enter_test(
                                    new Location3D(bx, by, bz),
                                    new Vector3(cpos.X, cpos.Y, cpos.Z)
                                );
                            }
                            if (old_bx != bx)
                            {
                                _try_enter_test(
                                    new Location3D(old_bx, by, bz),
                                    new Vector3(old_cpos.X, cpos.Y, cpos.Z));
                            }
                            if (old_by != by)
                            {
                                _try_enter_test(
                                     new Location3D(bx, old_by, bz),
                                     new Vector3(cpos.X, old_cpos.Y, cpos.Z));
                            }
                            if (old_bz != bz)
                            {
                                _try_enter_test(
                                    new Location3D(bx, by, old_bz),
                                    new Vector3(old_cpos.X, cpos.Y, old_cpos.Z));
                            }
                            if (old_bx != bx && old_by != by)
                            {
                                _try_enter_test(
                                    new Location3D(old_bx, old_by, bz),
                                    new Vector3(old_cpos.X, old_cpos.Y, cpos.Z));
                            }
                            if (old_bx != bx && old_bz != bz)
                            {
                                _try_enter_test(
                                    new Location3D(old_bx, by, old_bz),
                                    new Vector3(old_cpos.X, cpos.Y, old_cpos.Z));
                            }
                            if (old_by != by && old_bz != bz)
                            {
                                _try_enter_test(
                                    new Location3D(bx, old_by, old_bz),
                                    new Vector3(cpos.X, old_cpos.Y, old_cpos.Z));
                            }
                            testarray.Sort((a, b) =>
                            {
                                var d = Vector3.DistanceSquared(ray.center, a.pos) - Vector3.DistanceSquared(ray.center, b.pos);
                                return CMath.GetDirect(d);
                            });
                            foreach (var test in testarray)
                            {
                                if (matrix3D(st, test.loc.X, test.loc.Y, test.loc.Z, out var data))
                                {
                                    if (action(data, test.loc, pos, st))
                                    {
                                        touch = data;
                                        return true;
                                    }
                                }
                            }
                            testarray.Clear();
                            old_cpos = cpos;
                            old_bx = bx;
                            old_by = by;
                            old_bz = bz;
                            bool _try_enter_test(in Location3D _loc, in Vector3 _pos)
                            {
                                if (matrix3D(st, _loc.X, _loc.Y, _loc.Z, out var tdata))
                                {
                                    if (!testMap.ContainsKey(_loc))
                                    {
                                        testMap.Add(_loc, tdata);
                                        testarray.Add(new TryRayCast3DElement<T>()
                                        {
                                            loc = _loc,
                                            pos = _pos,
                                            data = tdata,
                                        });
                                        return true;
                                    }
                                }
                                return false;
                            }
                        }
                    }
                }
            }
            touch = default(T);
            return false;
        }

    }


    public interface IShape : ISerializable
    {
        DeepCore.Geometry.Vector3 Position { get; }
        bool Touch(IShape shape);
        bool Include(float x, float y);
        bool MoveToBorder(ref DeepCore.Geometry.Vector2 p, float addX, float addY, float minStep);
    }

    public class ShapePoint : IShape
    {
        public float x;
        public float y;
        public float z;
        public Vector3 Position { get => new Vector3(x, y, z); }
        public bool Include(float x, float y)
        {
            return this.IntersectPointPoint(x, y);
        }
        public bool Touch(IShape shape)
        {
            if (shape is ShapePoint)/*     */{ return this.IntersectPointPoint(shape as ShapePoint); }
            if (shape is ShapeRect)/*      */{ return this.IntersectPointRect(shape as ShapeRect); }
            if (shape is ShapeRound)/*     */{ return this.IntersectPointRound(shape as ShapeRound); }
            if (shape is ShapeEllipse)/*   */{ return this.IntersectPointEllipse(shape as ShapeEllipse); }
            if (shape is ShapeLine)/*      */{ return this.IntersectPointLine(shape as ShapeLine); }
            if (shape is ShapeStripWidth)/**/{ return this.IntersectPointStripWidth(shape as ShapeStripWidth); }
            return false;
        }
        public bool MoveToBorder(ref DeepCore.Geometry.Vector2 p, float addX, float addY, float minStep)
        {
            return true;
        }

    }

    public class ShapeRect : IShape
    {
        /// <summary>左上角点</summary>
        public float x;
        /// <summary>左上角点</summary>
        public float y;
        public float z;
        /// <summary>宽</summary>
        public float w;
        /// <summary>高</summary>
        public float h;
        public Vector3 Position { get => new Vector3(x, y, z); }

        public bool Include(float x, float y)
        {
            return this.IntersectRectPoint(x, y);
        }
        public bool Touch(IShape shape)
        {
            if (shape is ShapePoint)/*     */{ return this.IntersectRectPoint(shape as ShapePoint); }
            if (shape is ShapeRect)/*      */{ return this.IntersectRectRect(shape as ShapeRect); }
            if (shape is ShapeRound)/*     */{ return this.IntersectRectRound(shape as ShapeRound); }
            if (shape is ShapeEllipse)/*   */{ return this.IntersectRectEllipse(shape as ShapeEllipse); }
            if (shape is ShapeLine)/*      */{ return this.IntersectRectLine(shape as ShapeLine); }
            if (shape is ShapeStripWidth)/**/{ return this.IntersectRectStripWidth(shape as ShapeStripWidth); }
            return false;
        }
        public bool MoveToBorder(ref DeepCore.Geometry.Vector2 p, float addX, float addY, float minStep)
        {
            if (Include(p.X, p.Y))
            {
                var touch = new Vector2();
                var ray0 = new Vector2(p.X - addX * 1000, p.Y - addY * 1000);//防止穿越，反向延长线//
                var ray1 = new Vector2(p.X + addX, p.Y + addY);
                Span<Vector2> poly = stackalloc Vector2[] {
                    new Vector2(x, y),
                    new Vector2(x + w, y),
                    new Vector2(x + w, y + h),
                    new Vector2(x, y + h),
                };
                if (CollisionMath.MoveToPolyBorder(ray0, ray1, poly, out touch))
                {
                    p = touch;
                    return true;
                }
                return false;
            }
            else
            {
                /*     int count = 0;
                     if (addX != 0)
                     {
                         p.X += addX;
                         if (Include(p.X, p.Y))
                         {
                             if (addX > 0) { p.X = x - minStep; }
                             else { p.X = x + w + minStep; }
                             count++;
                         }
                     }
                     if (addY != 0)
                     {
                         p.Y += addY;
                         if (Include(p.X, p.Y))
                         {
                             if (addY > 0) { p.Y = y - minStep; }
                             else { p.Y = y + h + minStep; }
                             count++;
                         }
                     }
                     return count < 2;*/
                // ---- BUGFIX: 先计算完整目标位置，再判断 ----
                float targetX = p.X + addX;
                float targetY = p.Y + addY;
                if (Include(targetX, targetY))
                {
                    // 目标在障碍物内，整体推到边界
                    if (addX != 0)
                    {
                        if (addX > 0) { targetX = x - minStep; }
                        else { targetX = x + w + minStep; }
                    }
                    if (addY != 0)
                    {
                        if (addY > 0) { targetY = y - minStep; }
                        else { targetY = y + h + minStep; }
                    }
                    p.X = targetX;
                    p.Y = targetY;
                    return false;  // 返回 false 表示无法绕过
                }
                else
                {
                    // 目标不在障碍物内，安全移动
                    p.X = targetX;
                    p.Y = targetY;
                    return true;
                }
            }
        }
    }

    public class ShapeRound : IShape
    {
        /// <summary>中心点</summary>
        public float x;
        /// <summary>中心点</summary>
        public float y;
        public float z;
        /// <summary>半径</summary>
        public float r;
        public Vector3 Position { get => new Vector3(x, y, z); }

        public bool Include(float x, float y)
        {
            return this.IntersectRoundPoint(x, y);
        }
        public bool Touch(IShape shape)
        {
            if (shape is ShapePoint)/*     */{ return this.IntersectRoundPoint(shape as ShapePoint); }
            if (shape is ShapeRect)/*      */{ return this.IntersectRoundRect(shape as ShapeRect); }
            if (shape is ShapeRound)/*     */{ return this.IntersectRoundRound(shape as ShapeRound); }
            if (shape is ShapeEllipse)/*   */{ return this.IntersectRoundEllipse(shape as ShapeEllipse); }
            if (shape is ShapeLine)/*      */{ return this.IntersectRoundLine(shape as ShapeLine); }
            if (shape is ShapeStripWidth)/**/{ return this.IntersectRoundStripWidth(shape as ShapeStripWidth); }
            return false;
        }
        public bool MoveToBorder(ref DeepCore.Geometry.Vector2 p, float addX, float addY, float minStep)
        {
            var dir = VectorHelper.GetDegree(x, y, p.X + addX, p.Y + addY);
            var pos = new Vector2(x, y);
            VectorHelper.MovePolar(ref pos, dir, r + minStep);
            p.X = pos.X;
            p.Y = pos.Y;
            return true;
        }
    }

    public class ShapeEllipse : IShape
    {
        /// <summary>椭圆圆心x</summary>
        public float cx;
        /// <summary>椭圆圆心y</summary>
        public float cy;
        /// <summary>椭圆x轴半径</summary>
        public float rx;
        /// <summary>椭圆y轴半径</summary>
        public float ry;
        public float z;
        public Vector3 Position { get => new Vector3(cx, cy, z); }


        public bool Include(float x, float y)
        {
            return this.IntersectEllipsePoint(x, y);
        }

        public bool Touch(IShape shape)
        {
            if (shape is ShapePoint)/*     */{ return this.IntersectEllipsePoint(shape as ShapePoint); }
            if (shape is ShapeRect)/*      */{ return this.IntersectEllipseRect(shape as ShapeRect); }
            if (shape is ShapeRound)/*     */{ return this.IntersectEllipseRound(shape as ShapeRound); }
            if (shape is ShapeEllipse)/*   */{ return this.IntersectEllipseEllipse(shape as ShapeEllipse); }
            if (shape is ShapeLine)/*      */{ return this.IntersectEllipseLine(shape as ShapeLine); }
            if (shape is ShapeStripWidth)/**/{ return this.IntersectEllipseStripWidth(shape as ShapeStripWidth); }
            return false;
        }
        public bool MoveToBorder(ref DeepCore.Geometry.Vector2 p, float addX, float addY, float minStep)
        {
            if (rx == ry)
            {
                var dir = VectorHelper.GetDegree(cx, cy, p.X + addX, p.Y + addY);
                var pos = new Vector2(cx, cy);
                VectorHelper.MovePolar(ref pos, dir, rx + minStep);
                p.X = pos.X;
                p.Y = pos.Y;
                return true;
            }
            else
            {
                var dir = VectorHelper.GetDegree(cx, cy, p.X + addX, p.Y + addY);
                addX = (float)(Math.Cos(dir) * rx);
                addY = (float)(Math.Sin(dir) * ry);
                p.X = cx + addX;
                p.Y = cy + addY;
                return true;
            }
        }
    }

    public class ShapeLine : IShape
    {
        public float sx;
        public float sy;
        public float dx;
        public float dy;
        public float z;
        public Vector3 Position { get => new Vector3(sx, sy, z); }

        public bool Include(float x, float y)
        {
            return this.IntersectLinePoint(x, y);
        }
        public bool Touch(IShape shape)
        {
            if (shape is ShapePoint)/*     */{ return this.IntersectLinePoint(shape as ShapePoint); }
            if (shape is ShapeRect)/*      */{ return this.IntersectLineRect(shape as ShapeRect); }
            if (shape is ShapeRound)/*     */{ return this.IntersectLineRound(shape as ShapeRound); }
            if (shape is ShapeEllipse)/*   */{ return this.IntersectLineEllipse(shape as ShapeEllipse); }
            if (shape is ShapeLine)/*      */{ return this.IntersectLineLine(shape as ShapeLine); }
            if (shape is ShapeStripWidth)/**/{ return this.IntersectLineStripWidth(shape as ShapeStripWidth); }
            return false;
        }
        public bool MoveToBorder(ref DeepCore.Geometry.Vector2 p, float addX, float addY, float minStep)
        {
            var touch = new Vector2();
            var ray0 = new Vector2(p.X - addX, p.Y - addY);
            var ray1 = new Vector2(p.X + addX, p.Y + addY);
            if (CollisionMath.MoveToLineBorder(ray0, ray1,
                new Vector2(sx, sy),
                new Vector2(dx, dy),
                out touch))
            {
                p = touch;
            }
            return true;
        }
    }

    public class ShapeStripWidth : IShape
    {
        public float sx;
        public float sy;
        public float dx;
        public float dy;
        public float z;
        /// <summary>线条半径宽度</summary>
        public float r_wide;

        public Vector3 Position { get => new Vector3(sx, sy, z); }
        public Span<Vector2> ToPolyList(Span<Vector2> list)
        {
            CMath.ToStripWidthPolygon(list, sx, sy, dx, dy, r_wide);
            return list;
        }
        public bool Include(float x, float y)
        {
            return this.IntersectStripPoint(x, y);
        }
        public bool Touch(IShape shape)
        {
            if (shape is ShapePoint)/*     */{ return this.IntersectStripPoint(shape as ShapePoint); }
            if (shape is ShapeRect)/*      */{ return this.IntersectStripRect(shape as ShapeRect); }
            if (shape is ShapeRound)/*     */{ return this.IntersectStripRound(shape as ShapeRound); }
            if (shape is ShapeEllipse)/*   */{ return this.IntersectStripEllipse(shape as ShapeEllipse); }
            if (shape is ShapeLine)/*      */{ return this.IntersectStripLine(shape as ShapeLine); }
            if (shape is ShapeStripWidth)/**/{ return this.IntersectStripStripWidth(shape as ShapeStripWidth); }
            return false;
        }
        public bool MoveToBorder(ref DeepCore.Geometry.Vector2 p, float addX, float addY, float minStep)
        {
            //var touch = new Vector2();
            //             var list = ToPolyList(stackalloc Vector2[4]);
            //             if (CollisionMath.PointInPolygon(p, list))
            //             {
            //                 var ray0 = new Vector2(p.X - addX * 1000, p.Y - addY * 1000);//防止穿越，反向延长线//
            //                 var ray1 = new Vector2(p.X + addX, p.Y + addY);
            //                 if (CollisionMath.MoveToPolyBorder(ray0, ray1, list, out touch))
            //                 {
            //                     p = touch;
            //                     return true;
            //                 }
            //                 return false;
            //             }
            //             else
            //             {
            //                 var ray0 = new Vector2(p.X - addX, p.Y - addY);//防止穿越，反向延长线//
            //                 var ray1 = new Vector2(p.X + addX, p.Y + addY);
            //                 if (CollisionMath.MoveToPolyBorder(ray0, ray1, list, out touch))
            //                 {
            //                     p = touch;
            //                     return true;
            //                 }
            //                 return false;
            //             }
            return false;
        }
    }


    //--------------------------------------------------------------------------------------------------------------------------

    public static class ShapeCollision
    {

        #region PointWith
        public static bool IntersectPointPoint(this ShapePoint a, float x, float y)
        {
            return a.x == x && a.y == y;
        }
        public static bool IntersectPointPoint(this ShapePoint a, ShapePoint b)
        {
            return a.x == b.x && a.y == b.y;
        }
        public static bool IntersectPointRect(this ShapePoint a, ShapeRect b)
        {
            return CMath.IncludeRectPointW(b.x, b.y, b.w, b.h, a.x, a.y);
        }
        public static bool IntersectPointRound(this ShapePoint a, ShapeRound b)
        {
            return CMath.IncludeRoundPoint(b.x, b.y, b.r, a.x, a.y);
        }
        public static bool IntersectPointEllipse(this ShapePoint a, ShapeEllipse b)
        {
            return CMath.IncludeEllipsePoint(b.cx, b.cy, b.rx, b.ry, a.x, a.y);
        }
        public static bool IntersectPointLine(this ShapePoint a, ShapeLine b)
        {
            return CMath.IntersectLinePoint(b.sx, b.sy, b.dx, b.dy, a.x, a.y);
        }
        public static bool IntersectPointStripWidth(this ShapePoint a, ShapeStripWidth b)
        {
            return b.IntersectStripPoint(a);
        }
        #endregion

        #region RectWith
        public static bool IntersectRectPoint(this ShapeRect a, float x, float y)
        {
            return CMath.IncludeRectPointW(a.x, a.y, a.w, a.h, x, y);
        }
        public static bool IntersectRectPoint(this ShapeRect a, ShapePoint b)
        {
            return CMath.IncludeRectPointW(a.x, a.y, a.w, a.h, b.x, b.y);
        }
        public static bool IntersectRectRect(this ShapeRect a, ShapeRect b)
        {
            return CMath.IntersectRectW(a.x, a.y, a.w, a.h, b.x, b.y, b.w, b.h);
        }
        public static bool IntersectRectRound(this ShapeRect a, ShapeRound b)
        {
            return CMath.IntersectRectRoundW(a.x, a.y, a.w, a.h, b.x, b.y, b.r);
        }
        public static bool IntersectRectEllipse(this ShapeRect a, ShapeEllipse b)
        {
            return CMath.IntersectRectEllipseW(a.x, a.y, a.w, a.h, b.cx, b.cy, b.rx, b.ry);
        }
        public static bool IntersectRectLine(this ShapeRect a, ShapeLine b)
        {
            return CMath.IntersectRectLineW(a.x, a.y, a.w, a.h, b.sx, b.sy, b.dx, b.dy);
        }
        public static bool IntersectRectStripWidth(this ShapeRect a, ShapeStripWidth b)
        {
            return CMath.IntersectRectStripWidthW(a.x, a.y, a.w, a.h, b.sx, b.sy, b.dx, b.dy, b.r_wide);
        }
        #endregion

        #region RoundWith
        public static bool IntersectRoundPoint(this ShapeRound a, float x, float y)
        {
            return CMath.IncludeRoundPoint(a.x, a.y, a.r, x, y);
        }
        public static bool IntersectRoundPoint(this ShapeRound a, ShapePoint b)
        {
            return CMath.IncludeRoundPoint(a.x, a.y, a.r, b.x, b.y);
        }
        public static bool IntersectRoundRect(this ShapeRound a, ShapeRect b)
        {
            return CMath.IntersectRectRoundW(b.x, b.y, b.w, b.h, a.x, a.y, a.r);
        }
        public static bool IntersectRoundRound(this ShapeRound a, ShapeRound b)
        {
            return CMath.IntersectRound(a.x, a.y, a.r, b.x, b.y, b.r);
        }
        public static bool IntersectRoundEllipse(this ShapeRound a, ShapeEllipse b)
        {
            return false;
        }
        public static bool IntersectRoundLine(this ShapeRound a, ShapeLine b)
        {
            return CMath.IntersectLineRound(b.sx, b.sy, b.dx, b.dy, a.x, a.y, a.r);
        }
        public static bool IntersectRoundStripWidth(this ShapeRound a, ShapeStripWidth b)
        {
            return CMath.IntersectRoundStripWidth(a.x, a.y, a.r, b.sx, b.sy, b.dx, b.dy, b.r_wide);
        }
        #endregion

        #region EllipseWith
        public static bool IntersectEllipsePoint(this ShapeEllipse a, float x, float y)
        {
            return CMath.IncludeEllipsePoint(a.cx, a.cy, a.rx, a.ry, x, y);
        }
        public static bool IntersectEllipsePoint(this ShapeEllipse a, ShapePoint b)
        {
            return CMath.IncludeEllipsePoint(a.cx, a.cy, a.rx, a.ry, b.x, b.y);
        }
        public static bool IntersectEllipseRect(this ShapeEllipse a, ShapeRect b)
        {
            return CMath.IntersectRectEllipseW(b.x, b.y, b.w, b.h, a.cx, a.cy, a.rx, a.ry);
        }
        public static bool IntersectEllipseRound(this ShapeEllipse a, ShapeRound b)
        {
            return false;
        }
        public static bool IntersectEllipseEllipse(this ShapeEllipse a, ShapeEllipse b)
        {
            return false;
        }
        public static bool IntersectEllipseLine(this ShapeEllipse a, ShapeLine b)
        {
            return false;
        }
        public static bool IntersectEllipseStripWidth(this ShapeEllipse a, ShapeStripWidth b)
        {
            return false;
        }
        #endregion

        #region LineWith
        public static bool IntersectLinePoint(this ShapeLine a, float x, float y)
        {
            return CMath.IntersectLinePoint(a.sx, a.sy, a.dx, a.dy, x, y);
        }
        public static bool IntersectLinePoint(this ShapeLine a, ShapePoint b)
        {
            return CMath.IntersectLinePoint(a.sx, a.sy, a.dx, a.dy, b.x, b.y);
        }
        public static bool IntersectLineRect(this ShapeLine a, ShapeRect b)
        {
            return CMath.IntersectRectLineW(b.x, b.y, b.w, b.h, a.sx, a.sy, a.dx, a.dy);
        }
        public static bool IntersectLineRound(this ShapeLine a, ShapeRound b)
        {
            return CMath.IntersectLineRound(a.sx, a.sy, a.dx, a.dy, b.x, b.y, b.r);
        }
        public static bool IntersectLineEllipse(this ShapeLine a, ShapeEllipse b)
        {
            return false;
        }
        public static bool IntersectLineLine(this ShapeLine a, ShapeLine b)
        {
            return CMath.IntersectLine(a.sx, a.sy, a.dx, a.dy, b.sx, b.sy, b.dx, b.dy);
        }
        public static bool IntersectLineStripWidth(this ShapeLine a, ShapeStripWidth b)
        {
            return false;
        }
        #endregion

        #region StripWith
        public static bool IntersectStripPoint(this ShapeStripWidth a, float x, float y)
        {
            CMath.MinMax(a.sx, a.dx, out var x1, out var x2);
            CMath.MinMax(a.sy, a.dy, out var y1, out var y2);
            if (CMath.IncludeRectPoint(x1, y1, x2, y2, x, y))
            {
                Span<Vector2> list = stackalloc Vector2[4];
                a.ToPolyList(list);
                return CMath.IncludePolygonPoint(list, x, y);
            }
            return false;
        }
        public static bool IntersectStripPoint(this ShapeStripWidth a, ShapePoint b)
        {
            return a.IntersectStripPoint(b.x, b.y);
        }
        public static bool IntersectStripRect(this ShapeStripWidth a, ShapeRect b)
        {
            return CMath.IntersectRectStripWidthW(b.x, b.y, b.w, b.h, a.sx, a.sy, a.dx, a.dy, a.r_wide);
        }
        public static bool IntersectStripRound(this ShapeStripWidth a, ShapeRound b)
        {
            return CMath.IntersectRoundStripWidth(b.x, b.y, b.r, a.sx, a.sy, a.dx, a.dy, a.r_wide);
        }
        public static bool IntersectStripEllipse(this ShapeStripWidth a, ShapeEllipse b)
        {
            return false;
        }
        public static bool IntersectStripLine(this ShapeStripWidth a, ShapeLine b)
        {
            return false;
        }
        public static bool IntersectStripStripWidth(this ShapeStripWidth a, ShapeStripWidth b)
        {
            return false;
        }
        #endregion
    }


    public struct GridTerrain
    {
        public int XCount;
        public int YCount;
        public float GridSize;
        public bool include;
        public bool ForEachByShape<ST>(IShape shape, ST st, ForEachPredicate<ST, int, int> action)
        {
            if (shape is ShapePoint)
            {
                if (include) return false;
                return ForEachTerrainPoint(shape as ShapePoint, st, action);
            }
            else if (shape is ShapeLine)
            {
                if (include) return false;
                return ForEachTerrainLine(shape as ShapeLine, st, action);
            }
            else if (shape is ShapeEllipse)
            {
                return ForEachTerrainEllipse(shape as ShapeEllipse, include, st, action);
            }
            else if (shape is ShapeRect)
            {
                return ForEachTerrainRect(shape as ShapeRect, include, st, action);
            }
            else if (shape is ShapeRound)
            {
                return ForEachTerrainRound(shape as ShapeRound, include, st, action);
            }
            else if (shape is ShapeStripWidth)
            {
                return ForEachTerrainStripWidth(shape as ShapeStripWidth, include, st, action);
            }
            return false;
        }

        public void NormalizeTerrainRegionByBlock(ref int min_x, ref int min_y, ref int max_x, ref int max_y)
        {
            min_x = Math.Max(min_x, 0);
            min_y = Math.Max(min_y, 0);
            max_x = Math.Min(max_x, XCount - 1);
            max_y = Math.Min(max_y, YCount - 1);
        }

        public bool ForEachTerrainPoint<ST>(ShapePoint p, ST st, ForEachPredicate<ST, int, int> action)
        {
            float sx = p.x, sy = p.y;
            int cx1 = (int)(sx / GridSize);
            int cy1 = (int)(sy / GridSize);
            if (cx1 >= 0 && cx1 < XCount && cy1 >= 0 && cy1 < YCount)
            {
                if (action(st, cx1, cy1)) return true;
            }
            return false;
        }
        public bool ForEachTerrainLine<ST>(ShapeLine line, ST st, ForEachPredicate<ST, int, int> action)
        {
            return ForEachTerrainRayStep<ST>(line.sx, line.sy, line.dx, line.dy, st, action);
        }
        public bool ForEachTerrainRect<ST>(ShapeRect rect, bool include, ST st, ForEachPredicate<ST, int, int> action)
        {
            float sx = rect.x, sy = rect.y, w = rect.w, h = rect.h;
            int cx1 = (int)(sx / GridSize);
            int cy1 = (int)(sy / GridSize);
            int cx2 = (int)((sx + w) / GridSize);
            int cy2 = (int)((sy + h) / GridSize);
            cx1 = Math.Max(cx1, 0);
            cy1 = Math.Max(cy1, 0);
            cx2 = Math.Min(cx2, XCount - 1);
            cy2 = Math.Min(cy2, YCount - 1);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    if (include)
                    {
                        if (CMath.IncludeRectRectW(sx, sy, w, h, cx * GridSize, cy * GridSize, GridSize, GridSize))
                        {
                            if (action(st, cx, cy)) return true;
                        }
                    }
                    else
                    {
                        if (action(st, cx, cy)) return true;
                    }
                }
            }
            return false;
        }
        public bool ForEachTerrainRound<ST>(ShapeRound round, bool include, ST st, ForEachPredicate<ST, int, int> action)
        {
            float sx = round.x;
            float sy = round.y;
            float r = round.r;
            int cx1 = (int)((sx - r) / GridSize);
            int cy1 = (int)((sy - r) / GridSize);
            int cx2 = (int)((sx + r) / GridSize);
            int cy2 = (int)((sy + r) / GridSize);
            cx1 = Math.Max(cx1, 0);
            cy1 = Math.Max(cy1, 0);
            cx2 = Math.Min(cx2, XCount - 1);
            cy2 = Math.Min(cy2, YCount - 1);
            float dx1, dy1;
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    dx1 = cx * GridSize;
                    dy1 = cy * GridSize;
                    if (include)
                    {
                        if (CMath.IncludeRoundRect(sx, sy, r, dx1, dy1, dx1 + GridSize, dy1 + GridSize))
                        {
                            if (action(st, cx, cy)) return true;
                        }
                    }
                    else
                    {
                        if (CMath.IntersectRectRound(dx1, dy1, dx1 + GridSize, dy1 + GridSize, sx, sy, r))
                        {
                            if (action(st, cx, cy)) return true;
                        }
                    }
                }
            }
            return false;
        }
        public bool ForEachTerrainEllipse<ST>(ShapeEllipse e, bool include, ST st, ForEachPredicate<ST, int, int> action)
        {
            float sx = e.cx - e.rx;
            float sy = e.cy - e.ry;
            float w = e.rx * 2;
            float h = e.ry * 2;

            float scrw = w / 2;
            float scrh = h / 2;
            float scx = sx + w / 2;
            float scy = sy + h / 2;
            int cx1 = (int)(sx / GridSize);
            int cy1 = (int)(sy / GridSize);
            int cx2 = (int)((sx + w) / GridSize);
            int cy2 = (int)((sy + h) / GridSize);
            cx1 = Math.Max(cx1, 0);
            cy1 = Math.Max(cy1, 0);
            cx2 = Math.Min(cx2, XCount - 1);
            cy2 = Math.Min(cy2, YCount - 1);
            float cx0;
            float cy0;
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    cx0 = cx * GridSize;
                    cy0 = cy * GridSize;
                    if (include)
                    {
                        if (CMath.IncludeEllipseRect(scx, scy, scrw, scrh, cx0, cy0, cx0 + GridSize, cy0 + GridSize))
                        {
                            if (action(st, cx, cy)) return true;
                        }
                    }
                    else
                    {
                        if (CMath.IntersectRectEllipse(cx0, cy0, cx0 + GridSize, cy0 + GridSize, scx, scy, scrw, scrh))
                        {
                            if (action(st, cx, cy)) return true;
                        }
                    }
                }
            }
            return false;
        }
        public bool ForEachTerrainStripWidth<ST>(ShapeStripWidth strip, bool include, ST st, ForEachPredicate<ST, int, int> action)
        {
            float x0 = strip.sx, y0 = strip.sy, x1 = strip.dx, y1 = strip.dy, line_r = strip.r_wide;
            Span<Vector2> points = stackalloc Vector2[4];
            CMath.ToStripWidthPolygon(points, x0, y0, x1, y1, line_r);
            CMath.NormalRect(strip.sx, strip.sy, strip.dx, strip.dy, out var minX, out var minY, out var maxX, out var maxY);
            //CMath.ToBoundingBox(points, out var min, out var max);
            int bx0 = (int)(minX / GridSize);
            int by0 = (int)(minY / GridSize);
            int bx1 = (int)(maxX / GridSize);
            int by1 = (int)(maxY / GridSize);
            if (bx0 < 0) bx0 = 0;
            if (by0 < 0) by0 = 0;
            if (bx1 >= XCount) bx1 = XCount - 1;
            if (by1 >= YCount) by1 = YCount - 1;
            float cx0;
            float cy0;
            for (int by = by0; by <= by1; by++)
            {
                for (int bx = bx0; bx <= bx1; bx++)
                {
                    cx0 = bx * GridSize;
                    cy0 = by * GridSize;
                    if (include)
                    {
                        if (CMath.IncludePolygonRect(points, cx0, cy0, cx0 + GridSize, cy0 + GridSize))
                        {
                            if (action(st, bx, by)) return true;
                        }
                    }
                    else
                    {
                        if (CMath.IntersectRectPolygon(cx0, cy0, cx0 + GridSize, cy0 + GridSize, points))
                        {
                            if (action(st, bx, by)) return true;
                        }
                    }
                }
            }
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 线性延伸，扫描线段经过的所有节点，不进行碰撞检测
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public bool ForEachTerrainRayStep<ST>(float x0, float y0, float x1, float y1, ST st, ForEachPredicate<ST, int, int> action)
        {
            float step = GridSize / 2f;
            float dir = VectorHelper.GetDegree(x0, y0, x1, y1);
            float len = VectorHelper.GetDistance(x0, y0, x1, y1);
            {
                int old_bx = -1;
                int old_by = -1;
                int bx = old_bx;
                int by = old_by;
                do
                {
                    bx = (int)(x0 / GridSize);
                    by = (int)(y0 / GridSize);
                    if (bx >= XCount) break;
                    if (by >= YCount) break;
                    if (bx < 0) break;
                    if (by < 0) break;
                    if (bx != old_bx || by != old_by)
                    {
                        if (action(st, bx, by)) return true;
                        if (old_bx >= 0)
                        {
                            if (action(st, old_bx, by)) return true;
                        }
                        if (old_by >= 0)
                        {
                            if (action(st, bx, old_by)) return true;
                        }
                        old_bx = bx;
                        old_by = by;
                    }
                    VectorHelper.MovePolar(ref x0, ref y0, dir, step);
                    len -= step;
                } while (len > 0);
            }
            return false;
        }
    }
}
