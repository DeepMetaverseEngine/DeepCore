using DeepCore.Geometry.Terrain;
using DeepCore.Geometry;
using DeepCore.Voxel.Data;
using DeepCore;
using System;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.Misc;

namespace DeepMetaGame.Unity.GizmosUtils
{
    public static class DrawingVoxelObject
    {
        //---------------------------------------------------------------------------------------------------------------------
        public static void DrawAttackShape(UnityEngine.Color color, AttackShape shape,
           Vector3 localPos,
           Vector3 startPos,
           float bodyHeight,
           SpellTemplate.VoxelAnchor anchor,
           float direction, float size, float distance, float fan_angle, float strip_wide,
           DeepCore.Geometry.VoxelCylinder? target = null)
        {
            var pos1 = localPos;
            var pos2 = localPos;
            switch (anchor)
            {
                case SpellTemplate.VoxelAnchor.Flooring:
                    if (bodyHeight != 0)
                    {
                        DrawingVoxelObject.DrawHightZ(color, localPos, bodyHeight);
                        pos2.Z += bodyHeight;
                    }
                    break;
                case SpellTemplate.VoxelAnchor.Floating:
                    if (bodyHeight != 0)
                    {
                        DrawingVoxelObject.DrawHightZ(color, localPos + new Vector3(0, 0, -bodyHeight / 2f), bodyHeight);
                        pos2.Z -= bodyHeight / 2f;
                        pos1.Z += bodyHeight / 2f;
                    }
                    break;
                case SpellTemplate.VoxelAnchor.Ceiling:
                    if (bodyHeight != 0)
                    {
                        DrawingVoxelObject.DrawHightZ(color, localPos, -bodyHeight);
                        pos2.Z -= bodyHeight;
                    }
                    break;
            }
            switch (shape)
            {
                case AttackShape.Round:
                    DrawingVoxelObject.DrawCycle(color, pos1, size);
                    DrawingVoxelObject.DrawCycle(color, pos2, size);
                    //g.DrawEllipse(pen, new RectangleF(-size, -size, size * 2, size * 2));
                    break;
                case AttackShape.Circle:
                    DrawingVoxelObject.DrawCycle(color, pos1, size);
                    DrawingVoxelObject.DrawCycle(color, pos2, size);
                    //g.DrawEllipse(pen, new RectangleF(-size, -size, size * 2, size * 2));
                    float sr = size - strip_wide;
                    DrawingVoxelObject.DrawCycle(color, pos1, sr);
                    DrawingVoxelObject.DrawCycle(color, pos2, sr);
                    //g.DrawEllipse(pen, new RectangleF(-sr, -sr, sr * 2, sr * 2));
                    break;
                case AttackShape.Fan:
                    //g.DrawFan(pen, direction, size, angle);
                    DrawingVoxelObject.DrawFan(color, pos1, direction, fan_angle, size);
                    DrawingVoxelObject.DrawFan(color, pos2, direction, fan_angle, size);
                    break;
                case AttackShape.Strip:
                    {
                        float d_width = strip_wide / 2f;
                        float d_distance = distance / 2f;
                        var p0 = pos1;
                        var p1 = pos1;
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p0.X, ref p0.Y, direction, -d_distance);
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p1.X, ref p1.Y, direction, +d_distance);
                        DrawingVoxelObject.DrawStripe(color, p0.X, p0.Y, p1.X, p1.Y, d_width, pos1.Z);
                        DrawingVoxelObject.DrawStripe(color, p0.X, p0.Y, p1.X, p1.Y, d_width, pos2.Z);
                    }
                    break;
                case AttackShape.StripRay:
                case AttackShape.StripRayTouchEnd:
                    {
                        float d_width = strip_wide / 2f;
                        var p1 = pos1;
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p1.X, ref p1.Y, direction, distance);
                        DrawingVoxelObject.DrawStripe(color, pos1.X, pos1.Y, p1.X, p1.Y, d_width, pos1.Z);
                        DrawingVoxelObject.DrawStripe(color, pos1.X, pos1.Y, p1.X, p1.Y, d_width, pos2.Z);
                    }
                    break;
                case AttackShape.RectStrip:
                    {
                        float d_width = strip_wide / 2f;
                        float d_distance = distance / 2f;
                        var p0 = pos1;
                        var p1 = pos1;
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p0.X, ref p0.Y, direction, -d_distance);
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p1.X, ref p1.Y, direction, +d_distance);
                        DrawingVoxelObject.DrawStripeRect(color, p0.X, p0.Y, p1.X, p1.Y, d_width, pos1.Z);
                        DrawingVoxelObject.DrawStripeRect(color, p0.X, p0.Y, p1.X, p1.Y, d_width, pos2.Z);
                    }
                    break;
                case AttackShape.RectStripRay:
                    {
                        float d_width = strip_wide / 2f;
                        var p1 = pos1;
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p1.X, ref p1.Y, direction, distance);
                        DrawingVoxelObject.DrawStripeRect(color, pos1.X, pos1.Y, p1.X, p1.Y, d_width, pos1.Z);
                        DrawingVoxelObject.DrawStripeRect(color, pos1.X, pos1.Y, p1.X, p1.Y, d_width, pos2.Z);
                    }
                    break;
                case AttackShape.WideStrip:
                    {
                        float d_width = strip_wide / 2f;
                        float d_angle = direction + CMath.PI_DIV_2;
                        float d_distance = distance / 2f;
                        var p0 = pos1;
                        var p1 = pos1;
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p0.X, ref p1.Y, d_angle, -d_width);
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p1.X, ref p1.Y, d_angle, +d_width);
                        DrawingVoxelObject.DrawStripe(color, p0.X, p0.Y, p1.X, p1.Y, d_width, pos1.Z);
                        DrawingVoxelObject.DrawStripe(color, p0.X, p0.Y, p1.X, p1.Y, d_width, pos2.Z);
                    }
                    break;
                case AttackShape.LineToTarget:
                    if (target != null)
                    {
                        var p1 = pos1;
                        var p2 = target.Value.Center + new DeepCore.Geometry.Vector3(0, 0, target.Value.Height / 2);
                        DrawingVoxelObject.DrawCursor(color, p1, p2.ToGL(), strip_wide / 2, strip_wide, strip_wide);
                    }
                    break;
                case AttackShape.LineToStart:
                    {
                        var p1 = pos1;
                        var p2 = startPos;
                        DrawingVoxelObject.DrawCursor(color, p1, p2, strip_wide / 2, strip_wide, strip_wide);
                    }
                    break;
            }

        }

        //---------------------------------------------------------------------------------------------------------------------
        public static void FillRectW(UnityEngine.Color color, float x, float y, float w, float h, float z)
        {
            DrawRectW(PrimitiveType.Quads, color, x, y, w, h, z);
        }
        public static void DrawRectW(UnityEngine.Color color, float x, float y, float w, float h, float z)
        {
            DrawRectW(PrimitiveType.LineLoop, color, x, y, w, h, z);
        }
        public static void DrawRectW(PrimitiveType type, UnityEngine.Color color, float x, float y, float w, float h, float z)
        {
            GL.Begin(type);
            GL.UnityEngine.Color(color);
            GL.Vertex3(x, z, y);
            GL.Vertex3(x + w, z, y);
            GL.Vertex3(x + w, z, y + h);
            GL.Vertex3(x, z, y + h);
            GL.End();
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void FillRect(UnityEngine.Color color, Vector3 center, float w, float h)
        {
            DrawRect(PrimitiveType.Quads, color, center, w, h);
        }
        public static void DrawRect(UnityEngine.Color color, Vector3 center, float w, float h)
        {
            DrawRect(PrimitiveType.LineLoop, color, center, w, h);
        }
        public static void DrawRect(PrimitiveType type, UnityEngine.Color color, Vector3 center, float w, float h)
        {
            GL.Begin(type);
            var rw = w / 2;
            var rh = h / 2;
            GL.UnityEngine.Color(color);
            GL.Vertex3(center.X - rw, center.Z, center.Y - rh);
            GL.Vertex3(center.X + rw, center.Z, center.Y - rh);
            GL.Vertex3(center.X + rw, center.Z, center.Y + rh);
            GL.Vertex3(center.X - rw, center.Z, center.Y + rh);
            GL.End();
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void FillCycle(UnityEngine.Color color, Vector3 center, float radius)
        {
            DrawCycle(PrimitiveType.TriangleFan, color, center, radius);
        }
        public static void DrawCycle(UnityEngine.Color color, Vector3 center, float radius)
        {
            DrawCycle(PrimitiveType.LineLoop, color, center, radius);
        }
        public static void DrawCycle(PrimitiveType type, UnityEngine.Color color, Vector3 center, float radius)
        {
            int count = (int)(16 * Math.Max(radius, 1));
            float rstep = CMath.RADIANS_360 / count;
            float degInRad = 0;
            GL.Begin(type);
            GL.UnityEngine.Color(color);
            for (int i = 0; i < count; i++)
            {
                float px = (float)(center.X + Math.Cos(degInRad) * radius);
                float py = (float)(center.Y + Math.Sin(degInRad) * radius);
                GL.Vertex3(new Vector3(px, center.Z, py));
                degInRad += rstep;
            }
            GL.End();
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void ForArc(Action<Vector3> list, Vector3 center, float startAngle, float endAngle, float radius)
        {
            var degrees = Math.Abs(endAngle - startAngle);
            var count = (int)(16 * Math.Max(radius, 1));
            float rstep = degrees / count;
            float degInRad = startAngle;
            for (int i = 0; i < count; i++)
            {
                float px = (float)(center.X + Math.Cos(degInRad) * radius);
                float py = (float)(center.Y + Math.Sin(degInRad) * radius);
                list(new Vector3(px, py, center.Z));
                degInRad += rstep;
            }
        }
        public static void ForArcW(Action<Vector3> list, Vector3 center, float w, float h, float startAngle, float arcAngle)
        {
            int point_count = 32;
            float sw = w / 2;
            float sh = h / 2;
            float degree_start = CMath.AngleToRadian(startAngle);
            float degree_delta = CMath.PI_F * 2 / point_count;
            point_count++;
            for (int i = 0; i < point_count; i++)
            {
                float idegree = degree_start + i * degree_delta;
                list(new Vector3(
                    (float)(center.X + Math.Cos(idegree) * sw),
                    (float)(center.Y + Math.Sin(idegree) * sh),
                    center.Z));
            }
        }
        public static void FillArc(UnityEngine.Color color, Vector3 center, float w, float h, float startAngle, float arcAngle)
        {
            GL.Begin(PrimitiveType.TriangleFan);
            GL.UnityEngine.Color(color);
            GL.Vertex3(center.X, center.Z, center.Y);
            ForArcW(v3 => GL.Vertex3(v3.X, v3.Z, v3.Y), center, w, h, startAngle, arcAngle);
            GL.End();
        }
        public static void DrawArc(UnityEngine.Color color, Vector3 center, float w, float h, float startAngle, float arcAngle)
        {
            GL.Begin(PrimitiveType.LineLoop);
            GL.UnityEngine.Color(color);
            ForArcW(v3 => GL.Vertex3(v3.X, v3.Z, v3.Y), center, w, h, startAngle, arcAngle);
            GL.End();
        }
        public static void DrawArc(PrimitiveType type, UnityEngine.Color color, Vector3 center, float w, float h, float startAngle, float arcAngle)
        {
            GL.Begin(type);
            GL.UnityEngine.Color(color);
            ForArcW(v3 => { GL.Vertex3(center.X, center.Z, center.Y); GL.Vertex3(v3.X, v3.Z, v3.Y); }, center, w, h, startAngle, arcAngle);
            GL.End();
        }

        //---------------------------------------------------------------------------------------------------------------------
        public static void FillFan(UnityEngine.Color color, Vector3 center, float direction, float degrees, float radius)
        {
            DrawFan(PrimitiveType.TriangleFan, color, center, direction, degrees, radius);
        }
        public static void DrawFan(UnityEngine.Color color, Vector3 center, float direction, float degrees, float radius)
        {
            DrawFan(PrimitiveType.LineLoop, color, center, direction, degrees, radius);
        }
        public static void DrawFan(PrimitiveType type, UnityEngine.Color color, Vector3 center, float direction, float degrees, float radius)
        {
            if (degrees != 0)
            {
                GL.Begin(type);
                GL.UnityEngine.Color(color);
                int count = (int)(16 * Math.Max(radius, 1));
                float rstep = degrees / count;
                float degInRad = direction - degrees / 2;
                GL.Vertex3(new Vector3(center.X, center.Z, center.Y));
                for (int i = 0; i < count; i++)
                {
                    float px = (float)(center.X + Math.Cos(degInRad) * radius);
                    float py = (float)(center.Y + Math.Sin(degInRad) * radius);
                    GL.Vertex3(new Vector3(px, center.Z, py));
                    degInRad += rstep;
                }
                GL.End();
            }
            else
            {
                DrawDirection(color, center, direction, radius);
            }
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void FillStripeRect(UnityEngine.Color color, float sx, float sy, float dx, float dy, float line_r, float z)
        {
            DrawStripeRect(PrimitiveType.Quads, color, sx, sy, dx, dy, line_r, z);
        }
        public static void DrawStripeRect(UnityEngine.Color color, float sx, float sy, float dx, float dy, float line_r, float z)
        {
            DrawStripeRect(PrimitiveType.LineLoop, color, sx, sy, dx, dy, line_r, z);
        }
        public static void DrawStripeRect(PrimitiveType type, UnityEngine.Color color, float sx, float sy, float dx, float dy, float line_r, float z)
        {
            float direction = DeepCore.Geometry.VectorHelper.GetDegree(sx, sy, dx, dy);
            Vector2 s_l = new Vector2(sx, sy);
            Vector2 s_r = new Vector2(sx, sy);
            Vector2 d_l = new Vector2(dx, dy);
            Vector2 d_r = new Vector2(dx, dy);
            DeepCore.Geometry.VectorHelper.MovePolar(ref s_l.X, ref s_l.Y, direction + CMath.PI_DIV_2, line_r);
            DeepCore.Geometry.VectorHelper.MovePolar(ref d_l.X, ref d_l.Y, direction + CMath.PI_DIV_2, line_r);
            DeepCore.Geometry.VectorHelper.MovePolar(ref s_r.X, ref s_r.Y, direction - CMath.PI_DIV_2, line_r);
            DeepCore.Geometry.VectorHelper.MovePolar(ref d_r.X, ref d_r.Y, direction - CMath.PI_DIV_2, line_r);
            GL.Begin(type);
            GL.UnityEngine.Color(color);
            {
                GL.Vertex3(s_l.X, z, s_l.Y);
                GL.Vertex3(s_r.X, z, s_r.Y);
                GL.Vertex3(d_r.X, z, d_r.Y);
                GL.Vertex3(d_l.X, z, d_l.Y);
            }
            GL.End();
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void DrawStripe(PrimitiveType type, UnityEngine.Color color, float sx, float sy, float dx, float dy, float line_r, float z)
        {
            DrawCycle(color, new Vector3(sx, sy, z), line_r);
            DrawCycle(color, new Vector3(dx, dy, z), line_r);
            GL.Begin(type);
            GL.UnityEngine.Color(color);
            {
                float direction = DeepCore.Geometry.VectorHelper.GetDegree(sx, sy, dx, dy);
                Vector2 s_l = new Vector2(sx, sy);
                Vector2 s_r = new Vector2(sx, sy);
                Vector2 d_l = new Vector2(dx, dy);
                Vector2 d_r = new Vector2(dx, dy);
                DeepCore.Geometry.VectorHelper.MovePolar(ref s_l.X, ref s_l.Y, direction + CMath.PI_DIV_2, line_r);
                DeepCore.Geometry.VectorHelper.MovePolar(ref d_l.X, ref d_l.Y, direction + CMath.PI_DIV_2, line_r);
                DeepCore.Geometry.VectorHelper.MovePolar(ref s_r.X, ref s_r.Y, direction - CMath.PI_DIV_2, line_r);
                DeepCore.Geometry.VectorHelper.MovePolar(ref d_r.X, ref d_r.Y, direction - CMath.PI_DIV_2, line_r);
                //float size = line_r * 2;
                GL.Vertex3(s_l.X, z, s_l.Y);
                GL.Vertex3(s_r.X, z, s_r.Y);
                //                 float angle = CMath.RadianToAngle(direction);
                //                 VertexUtil.ForArc(new Vector2(sx - line_r, sy - line_r), angle - MathHelper.PiOver2, angle + MathHelper.PiOver2, size, v => GL.Vertex3(v.X, z, v.Y));
                GL.Vertex3(d_r.X, z, d_r.Y);
                GL.Vertex3(d_l.X, z, d_l.Y);
                //                 angle = angle + MathHelper.TwoPi;
                //                 VertexUtil.ForArc(new Vector2(sx - line_r, sy - line_r), angle - MathHelper.PiOver2, angle + MathHelper.PiOver2, size, v => GL.Vertex3(v.X, z, v.Y));
            }
            GL.End();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static void ForStar(Action<Vector3> list, Vector3 center, float radius, float rotate = 0)
        {
            var delta = MathHelper.TwoPi / 5;
            var start = rotate - MathHelper.Pi / 2;
            var v0 = new Vector3();
            var v1 = new Vector3();
            var v2 = new Vector3();
            var v3 = new Vector3();
            var v4 = new Vector3();
            DeepCore.Geometry.VectorHelper.MovePolar(ref v0.X, ref v0.Y, start + delta * 0, radius);
            DeepCore.Geometry.VectorHelper.MovePolar(ref v1.X, ref v1.Y, start + delta * 1, radius);
            DeepCore.Geometry.VectorHelper.MovePolar(ref v2.X, ref v2.Y, start + delta * 2, radius);
            DeepCore.Geometry.VectorHelper.MovePolar(ref v3.X, ref v3.Y, start + delta * 3, radius);
            DeepCore.Geometry.VectorHelper.MovePolar(ref v4.X, ref v4.Y, start + delta * 4, radius);
            list(new Vector3(center.X + v0.X, center.Y + v0.Y, center.Z));
            list(new Vector3(center.X + v2.X, center.Y + v2.Y, center.Z));
            list(new Vector3(center.X + v4.X, center.Y + v4.Y, center.Z));
            list(new Vector3(center.X + v1.X, center.Y + v1.Y, center.Z));
            list(new Vector3(center.X + v3.X, center.Y + v3.Y, center.Z));
        }
        public static Vector3[] ToStar(Vector3 center, float radius, float rotate = 0)
        {
            var ret = new Vector3[5];
            int i = 0;
            ForStar(v3 => { ret[i++] = v3; }, center, radius, rotate);
            return ret;
        }
        public static void DrawStar(UnityEngine.Color color, Vector3 center, float radius, float rotate = 0)
        {
            GL.Begin(PrimitiveType.LineLoop);
            GL.UnityEngine.Color(color);
            ForStar(v3 => GL.Vertex3(v3.X, v3.Z, v3.Y), center, radius, rotate);
            GL.End();
        }

        //---------------------------------------------------------------------------------------------------------------------
        public static void DrawEquilateralTrangle(PrimitiveType type, UnityEngine.Color color, Vector3 center, float rotate, float radius)
        {
            var delta = MathHelper.TwoPi / 3;
            var start = rotate - MathHelper.Pi / 2;
            var v0 = new Vector3();
            var v1 = new Vector3();
            var v2 = new Vector3();
            DeepCore.Geometry.VectorHelper.MovePolar(ref v0.X, ref v0.Y, start + delta * 0, radius);
            DeepCore.Geometry.VectorHelper.MovePolar(ref v1.X, ref v1.Y, start + delta * 1, radius);
            DeepCore.Geometry.VectorHelper.MovePolar(ref v2.X, ref v2.Y, start + delta * 2, radius);
            GL.Begin(type);
            GL.UnityEngine.Color(color);
            {
                GL.Vertex3(center.X + v0.X, center.Z, center.Y + v0.Y);
                GL.Vertex3(center.X + v1.X, center.Z, center.Y + v1.Y);
                GL.Vertex3(center.X + v2.X, center.Z, center.Y + v2.Y);
            }
            GL.End();
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void DrawCursor(PrimitiveType type, UnityEngine.Color color, Vector3 p1, Vector3 p2, float width, float cursor_width, float cursor_height)
        {
            if (cursor_width < width)
                cursor_width = width;

            float ox = p1.X - p2.X;
            float oy = p1.Y - p2.Y;
            float ds = CMath.GetDistance(p1.X, p1.Y, p2.X, p2.Y);
            float hw = width / 2f;
            float cw = cursor_width / 2;
            float od = (float)Math.Atan2(oy, ox);

            var sC = new Vector3(0, p1.Z, 0);
            var sL = new Vector3(-hw, p1.Z, 0);
            var sR = new Vector3(+hw, p1.Z, 0);
            var dC = new Vector3(0, p2.Z, ds);
            var dL = new Vector3(-hw, p2.Z, ds - cursor_height);
            var dR = new Vector3(+hw, p2.Z, ds - cursor_height);
            var dLL = new Vector3(-cw, p2.Z, ds - cursor_height);
            var dRR = new Vector3(+cw, p2.Z, ds - cursor_height);

            var points = new Vector3[] { sC, sL, dL, dLL, dC, dRR, dR, sR, sC };

            GL.Begin(type);
            GL.UnityEngine.Color(color);
            for (int i = 0; i < points.Length; i++)
            {
                float x = points[i].X;
                float y = points[i].Z;
                DeepCore.Geometry.VectorHelper.Rotate(ref x, ref y, 0, 0, od + CMath.PI_DIV_2);
                points[i].X = x + p1.X;
                points[i].Z = y + p1.Y;
                GL.Vertex3(points[i]);
            }
            GL.End();
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void DrawDirection(UnityEngine.Color color, Vector3 center, float direction, float radius)
        {
            GL.Begin(PrimitiveType.Lines);
            GL.UnityEngine.Color(color);
            var px = (float)(center.X + Math.Cos(direction) * radius);
            var py = (float)(center.Y + Math.Sin(direction) * radius);
            GL.Vertex3(new Vector3(center.X, center.Z, center.Y));
            GL.Vertex3(new Vector3(px, center.Z, py));
            GL.End();
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void DrawCross(UnityEngine.Color color, Vector3 center, float radius)
        {
            GL.Begin(PrimitiveType.Lines);
            GL.UnityEngine.Color(color);
            GL.Vertex3(center.X - radius, center.Z, center.Y);
            GL.Vertex3(center.X + radius, center.Z, center.Y);
            GL.Vertex3(center.X, center.Z, center.Y + radius);
            GL.Vertex3(center.X, center.Z, center.Y - radius);
            GL.End();
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void DrawBody3D(UnityEngine.Color headColor, UnityEngine.Color bodyColor, UnityEngine.Color footColor, Vector3 center, float height, float radius)
        {
            int count = (int)(16 * Math.Max(radius, 1));
            float rstep = CMath.RADIANS_360 / count;
            float degInRad = 0;
            using (var foot = CollectionObjectPool<Vector3>.AllocList(count))
            using (var head = CollectionObjectPool<Vector3>.AllocList(count))
            using (var body = CollectionObjectPool<Vector3>.AllocList(count))
            {
                for (int i = 0; i < count; i++)
                {
                    float px = (float)(center.X + Math.Cos(degInRad) * radius);
                    float py = (float)(center.Y + Math.Sin(degInRad) * radius);
                    head.Add(new Vector3(px, center.Z + height, py));
                    foot.Add(new Vector3(px, center.Z, py));
                    body.Add(head.Last());
                    body.Add(foot.Last());
                    degInRad += rstep;
                }
                {
                    GL.Begin(PrimitiveType.TriangleFan);
                    GL.UnityEngine.Color(headColor);
                    GL.Vertex3(center.X, center.Z + height, center.Y);
                    for (int i = 0; i < count; i++) { GL.Vertex3(head[i]); }
                    if (head.Count > 0) GL.Vertex3(head[0]);
                    GL.End();
                }
                {
                    GL.Begin(PrimitiveType.Lines);
                    GL.UnityEngine.Color(bodyColor);
                    for (int i = 0; i < count; i++)
                    {
                        GL.Vertex3(body[i * 2 + 0]);
                        GL.Vertex3(body[i * 2 + 1]);
                    }
                    GL.End();
                }
                {
                    GL.Begin(PrimitiveType.LineLoop);
                    GL.UnityEngine.Color(footColor);
                    for (int i = 0; i < count; i++) { GL.Vertex3(foot[i]); }
                    GL.End();
                }
            }
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void DrawSphere3D(UnityEngine.Color color, Vector3 center, float radius)
        {
            int count = (int)(16 * Math.Max(radius, 1));
            float rstep = CMath.RADIANS_360 / count;
            float ladeg = 0, lodeg = 0;
            for (int i = 0; i < count; i++)
            {
                //latitude纬度//
                {
                    float pw = (float)Math.Sin(ladeg) * radius;
                    float ph = (float)Math.Cos(ladeg) * radius;
                    GL.Begin(PrimitiveType.LineLoop);
                    GL.UnityEngine.Color(color);
                    lodeg = 0;
                    for (int j = 0; j < count; j++)
                    {
                        float px = (float)(center.X + Math.Cos(lodeg) * pw);
                        float py = (float)(center.Y + Math.Sin(lodeg) * pw);
                        GL.Vertex3(new Vector3(px, center.Z + ph, py));
                        lodeg += rstep;
                    }
                    GL.End();
                }
                //longitude经度//
                {
                    GL.Begin(PrimitiveType.LineLoop);
                    GL.UnityEngine.Color(color);
                    lodeg = 0;
                    for (int j = 0; j < count; j++)
                    {
                        float r = (float)(Math.Cos(lodeg) * radius);
                        float py = (float)(Math.Sin(lodeg) * radius);
                        float px = (float)(Math.Cos(ladeg) * r);
                        float pz = (float)(Math.Sin(ladeg) * r);
                        GL.Vertex3(center.X + px, center.Z + py, center.Y + pz);
                        lodeg += rstep;
                    }
                    GL.End();
                }
                ladeg += rstep;
            }
        }
        //---------------------------------------------------------------------------------------------------------------------
        public static void DrawHightZ(UnityEngine.Color color, Vector3 pos)
        {
            GL.Begin(PrimitiveType.Lines);
            GL.UnityEngine.Color(color);
            GL.Vertex3(pos.X, pos.Z, pos.Y);
            GL.Vertex3(pos.X, 0, pos.Y);
            GL.End();
        }
        public static void DrawHightZ(UnityEngine.Color color, Vector3 pos, float height)
        {
            GL.Begin(PrimitiveType.Lines);
            GL.UnityEngine.Color(color);
            GL.Vertex3(pos.X, pos.Z, pos.Y);
            GL.Vertex3(pos.X, pos.Z + height, pos.Y);
            GL.End();
        }
        public static void DrawLine(UnityEngine.Color color, Vector3 pos1, Vector3 pos2)
        {
            GL.Begin(PrimitiveType.Lines);
            GL.UnityEngine.Color(color);
            GL.Vertex3(pos1.X, pos1.Z, pos1.Y);
            GL.Vertex3(pos2.X, pos2.Z, pos2.Y);
            GL.End();
        }
        //---------------------------------------------------------------------------------------------------------------------
        /*
        public static void DrawAttackShape(UnityEngine.Color color, AttackShape shape, Vector3 pos, Vector3 startPos,
            float bodyHeight, DeepCore.GameData.Zone.SpellTemplate.VoxelAnchor anchor,
            float direction, float size, float distance, float fan_angle, float strip_wide,
            DeepCore.Geometry.VoxelCylinder? target = null)
        {
            switch (anchor)
            {
                case SpellTemplate.VoxelAnchor.Flooring:
                    if (bodyHeight != 0)
                    {
                        DrawHightZ(color, pos, bodyHeight);
                    }
                    break;
                case SpellTemplate.VoxelAnchor.Floating:
                    if (bodyHeight != 0)
                    {
                        DrawHightZ(color, pos + new Vector3(0, 0, -bodyHeight / 2), bodyHeight);
                    }
                    break;
                case SpellTemplate.VoxelAnchor.Ceiling:
                    if (bodyHeight != 0)
                    {
                        DrawHightZ(color, pos, -bodyHeight);
                    }
                    break;
            }
            switch (shape)
            {
                case AttackShape.Round:
                    DrawCycle(color, pos, size);
                    //g.DrawEllipse(pen, new RectangleF(-size, -size, size * 2, size * 2));
                    break;
                case AttackShape.Circle:
                    DrawCycle(color, pos, size);
                    //g.DrawEllipse(pen, new RectangleF(-size, -size, size * 2, size * 2));
                    float sr = size - strip_wide;
                    DrawCycle(color, pos, sr);
                    //g.DrawEllipse(pen, new RectangleF(-sr, -sr, sr * 2, sr * 2));
                    break;
                case AttackShape.Fan:
                    //g.DrawFan(pen, direction, size, angle);
                    DrawFan(color, pos, direction, fan_angle, size);
                    break;
                case AttackShape.Strip:
                    {
                        float d_width = strip_wide / 2f;
                        float d_distance = distance / 2f;
                        var p0 = pos;
                        var p1 = pos;
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p0.X, ref p0.Y, direction, -d_distance);
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p1.X, ref p1.Y, direction, +d_distance);
                        DrawStripe(PrimitiveType.LineLoop, color, p0.X, p0.Y, p1.X, p1.Y, d_width, pos.Z);
                    }
                    break;
                case AttackShape.StripRay:
                case AttackShape.StripRayTouchEnd:
                    {
                        float d_width = strip_wide / 2f;
                        var p1 = pos;
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p1.X, ref p1.Y, direction, distance);
                        DrawStripe(PrimitiveType.LineLoop, color, pos.X, pos.Y, p1.X, p1.Y, d_width, pos.Z);
                    }
                    break;
                case AttackShape.RectStrip:
                    {
                        float d_width = strip_wide / 2f;
                        float d_distance = distance / 2f;
                        var p0 = pos;
                        var p1 = pos;
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p0.X, ref p0.Y, direction, -d_distance);
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p1.X, ref p1.Y, direction, +d_distance);
                        DrawStripeRect(PrimitiveType.LineLoop, color, p0.X, p0.Y, p1.X, p1.Y, d_width, pos.Z);
                    }
                    break;
                case AttackShape.RectStripRay:
                    {
                        float d_width = strip_wide / 2f;
                        var p1 = pos;
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p1.X, ref p1.Y, direction, distance);
                        DrawStripeRect(PrimitiveType.LineLoop, color, pos.X, pos.Y, p1.X, p1.Y, d_width, pos.Z);
                    }
                    break;
                case AttackShape.WideStrip:
                    {
                        float d_width = strip_wide / 2f;
                        float d_angle = direction + CMath.PI_DIV_2;
                        float d_distance = distance / 2f;
                        var p0 = pos;
                        var p1 = pos;
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p0.X, ref p1.Y, d_angle, -d_width);
                        DeepCore.Geometry.VectorHelper.MovePolar(ref p1.X, ref p1.Y, d_angle, +d_width);
                        DrawStripe(PrimitiveType.LineLoop, color, p0.X, p0.Y, p1.X, p1.Y, d_width, pos.Z);
                    }
                    break;
                case AttackShape.LineToTarget:
                    if (target != null)
                    {
                        var p1 = pos;
                        var p2 = target.Value.Center + new DeepCore.Geometry.Vector3(0, 0, target.Value.Height / 2);
                        DrawCursor(PrimitiveType.LineLoop, color, p1, p2.ToGL(), strip_wide / 2, strip_wide, strip_wide);
                    }
                    break;
                case AttackShape.LineToStart:
                    {
                        var p1 = pos;
                        var p2 = startPos;
                        DrawCursor(PrimitiveType.LineLoop, color, p1, p2, strip_wide / 2, strip_wide, strip_wide);
                    }
                    break;
            }

        }
        */
        public static void DrawWayPoint(UnityEngine.Color lineColor, UnityEngine.Color quardsColor, ITerrainWayPoint wp, float gridSize, Vector3 offset)
        {
            GL.Begin(PrimitiveType.LineStrip);
            GL.UnityEngine.Color(lineColor);
            foreach (var p in wp)
            {
                var pos = p.Position.ToGL() + offset + new Vector3(0, 0, 0.1f);
                GL.Vertex3(pos.X, pos.Z, pos.Y);
            }
            GL.End();
            GL.Begin(PrimitiveType.Quads);
            GL.UnityEngine.Color(quardsColor);
            foreach (var p in wp)
            {
                var pos = p.Position.ToGL() + offset;
                var rect = p.Range.GetRangeSize(gridSize);
                GL.Vertex3(rect.X/*         */, pos.Z, rect.Y);
                GL.Vertex3(rect.X + rect.Width, pos.Z, rect.Y);
                GL.Vertex3(rect.X + rect.Width, pos.Z, rect.Y + rect.Height);
                GL.Vertex3(rect.X/*         */, pos.Z, rect.Y + rect.Height);
                //                 var pos = new Vector3(p.Position.X * gridSize, p.Position.Y * gridSize, p.Position.Z) + offset;
                //                 GL.Vertex3(pos.X, pos.Z, pos.Y);
                //                 GL.Vertex3(pos.X + gridSize, pos.Z, pos.Y);
                //                 GL.Vertex3(pos.X + gridSize, pos.Z, pos.Y + gridSize);
                //                 GL.Vertex3(pos.X, pos.Z, pos.Y + gridSize);
            }
            GL.End();
        }

        public static void ForBoundingBox(Action<Vector3> list, Vector3 min, Vector3 max)
        {
            // TOP
            list(new Vector3(min.X, max.Z, min.Y));
            list(new Vector3(max.X, max.Z, min.Y));
            list(new Vector3(max.X, max.Z, max.Y));
            list(new Vector3(min.X, max.Z, max.Y));
            // Bottom
            list(new Vector3(min.X, min.Z, min.Y));
            list(new Vector3(max.X, min.Z, min.Y));
            list(new Vector3(max.X, min.Z, max.Y));
            list(new Vector3(min.X, min.Z, max.Y));
            // FORTH                             
            list(new Vector3(min.X, min.Z, max.Y));
            list(new Vector3(max.X, min.Z, max.Y));
            list(new Vector3(max.X, max.Z, max.Y));
            list(new Vector3(min.X, max.Z, max.Y));
            // BACK                              
            list(new Vector3(min.X, min.Z, min.Y));
            list(new Vector3(max.X, min.Z, min.Y));
            list(new Vector3(max.X, max.Z, min.Y));
            list(new Vector3(min.X, max.Z, min.Y));
            // LEFT                              
            list(new Vector3(min.X, min.Z, min.Y));
            list(new Vector3(min.X, min.Z, max.Y));
            list(new Vector3(min.X, max.Z, max.Y));
            list(new Vector3(min.X, max.Z, min.Y));
            // RIGHT                             
            list(new Vector3(max.X, min.Z, min.Y));
            list(new Vector3(max.X, min.Z, max.Y));
            list(new Vector3(max.X, max.Z, max.Y));
            list(new Vector3(max.X, max.Z, min.Y));
        }
        public static void DrawBoundingBox(PrimitiveType type, UnityEngine.Color color, DeepCore.Geometry.BoundingBox box)
        {
            DrawBoundingBox(type, color, box.Min.ToGL(), box.Max.ToGL());
        }
        public static void DrawBoundingBox(PrimitiveType type, UnityEngine.Color color, Vector3 min, Vector3 max)
        {
            GL.UnityEngine.Color(color);
            // TOP
            GL.Begin(type);
            GL.Vertex3(min.X, max.Z, min.Y);
            GL.Vertex3(max.X, max.Z, min.Y);
            GL.Vertex3(max.X, max.Z, max.Y);
            GL.Vertex3(min.X, max.Z, max.Y);
            GL.End();

            // Bottom
            GL.Begin(type);
            GL.Vertex3(min.X, min.Z, min.Y);
            GL.Vertex3(max.X, min.Z, min.Y);
            GL.Vertex3(max.X, min.Z, max.Y);
            GL.Vertex3(min.X, min.Z, max.Y);
            GL.End();

            // FORTH
            GL.Begin(type);
            GL.Vertex3(min.X, min.Z, max.Y);
            GL.Vertex3(max.X, min.Z, max.Y);
            GL.Vertex3(max.X, max.Z, max.Y);
            GL.Vertex3(min.X, max.Z, max.Y);
            GL.End();
            // BACK
            GL.Begin(type);
            GL.Vertex3(min.X, min.Z, min.Y);
            GL.Vertex3(max.X, min.Z, min.Y);
            GL.Vertex3(max.X, max.Z, min.Y);
            GL.Vertex3(min.X, max.Z, min.Y);
            GL.End();

            // LEFT
            GL.Begin(type);
            GL.Vertex3(min.X, min.Z, min.Y);
            GL.Vertex3(min.X, min.Z, max.Y);
            GL.Vertex3(min.X, max.Z, max.Y);
            GL.Vertex3(min.X, max.Z, min.Y);
            GL.End();
            // RIGHT
            GL.Begin(type);
            GL.Vertex3(max.X, min.Z, min.Y);
            GL.Vertex3(max.X, min.Z, max.Y);
            GL.Vertex3(max.X, max.Z, max.Y);
            GL.Vertex3(max.X, max.Z, min.Y);
            GL.End();
        }
        public static void DrawVoxel(PrimitiveType type, UnityEngine.Color color, VoxelLayer layer)
        {
            var box = layer.GetBlockBoundingBox();
            var min = box.Min.ToGL();
            var max = box.Max.ToGL();
            DrawBoundingBox(type, color, min, max);
        }

        /*
        public static void DrawZoneShape(UnityEngine.Color color, Vector3 pos, IZoneShape shape)
        {
            if (shape is ShapeRound round)
            {
                DrawingObject.DrawCycle(color, pos, round.r);
            }
            else if (shape is ShapeRect rect)
            {
                DrawingObject.DrawRect(color, pos, rect.w, rect.h);
            }
            else if (shape is ShapeStripWidth strip)
            {
                DrawingObject.DrawStripeRect(color, strip.sx, strip.sy, strip.dx, strip.dy, strip.r_wide, pos.Z);
            }
        }
        */
        public static void DrawBounds(UnityEngine.Color color, float x, float y, float w, float h, float grid)
        {
            GL.Begin(PrimitiveType.Lines);
            GL.UnityEngine.Color(color);
            GL.Vertex3(new Vector3(x - grid, 0, y)); GL.Vertex3(new Vector3(x + grid + w, 0, y));
            GL.Vertex3(new Vector3(x, 0, y - grid)); GL.Vertex3(new Vector3(x, 0, y + grid + h));
            GL.Vertex3(new Vector3(x + w, 0, y + h)); GL.Vertex3(new Vector3(x, 0, y + h));
            GL.Vertex3(new Vector3(x + w, 0, y + h)); GL.Vertex3(new Vector3(x + w, 0, y));
            GL.End();
        }

        public static void DrawGridLines(UnityEngine.Color color, float startX, float startY, float gridW, float gridH, int xcount, int ycount)
        {
            GL.Begin(PrimitiveType.Lines);
            GL.UnityEngine.Color(color);
            var tw = xcount * gridW;
            var th = ycount * gridH;
            for (var x = 0; x <= xcount; x++)
            {
                float dx = startX + x * gridW;
                GL.Vertex3(dx, 0.5f, startY);
                GL.Vertex3(dx, 0.5f, startY + th);
            }
            for (var y = 0; y <= ycount; y++)
            {
                float dy = startY + y * gridH;
                GL.Vertex3(startX, 0.5f, dy);
                GL.Vertex3(startX + tw, 0.5f, dy);
            }
            GL.End();
        }


    }
}
