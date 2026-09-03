using DeepCore;
using DeepCore.Geometry;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace DeepMetaGame.Data.Helper
{
    public struct TeamFormationGen
    {
        private Random RandomN;
        private SingleThreadCollectionPool pool;
        public TeamFormationGen(Random random, SingleThreadCollectionPool pool)
        {
            this.RandomN = random;
            this.pool = pool;
        }

        /// <summary>
        /// 初始化，直接放置坐标位置
        /// </summary>
        public void GenPos(TeamFormation.Formation f, float spaceSize, ICollection<IVector2> mObjects, Vector2 center)
        {
            switch (f)
            {
                case TeamFormation.Formation.Random:
                    DistributeSpacingSizeRandom(center, mObjects, spaceSize);
                    break;
                case TeamFormation.Formation.RandomCycle:
                    DistributeSpacingSizeRandomCycle(center, mObjects, spaceSize);
                    break;
                case TeamFormation.Formation.Square:
                    DistributeSpacingSizeSquare(center, mObjects, spaceSize);
                    break;
                case TeamFormation.Formation.Round:
                    DistributeSpacingSizeRound(center, mObjects, spaceSize);
                    break;
                case TeamFormation.Formation.Cycle:
                    DistributeSpacingSizeCycle(center, mObjects, spaceSize);
                    break;
                case TeamFormation.Formation.Beehive:
                    DistributeSpacingSizeBeehive(center, mObjects, spaceSize);
                    break;
                case TeamFormation.Formation.Horizontal:
                    DistributeSpacingSizeHorizontal(center, mObjects, spaceSize);
                    break;
                case TeamFormation.Formation.Vertical:
                    DistributeSpacingSizeVertical(center, mObjects, spaceSize);
                    break;
                default:
                    DistributeSpacingSizeRandom(center, mObjects, spaceSize);
                    break;
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------

        public void GetCenterOf(ICollection<IVector2> vectors, out float cx, out float cy)
        {
            if (vectors.Count == 0)
            {
                cx = 0;
                cy = 0;
            }
            else
            {
                float min_x = float.MaxValue;
                float max_x = float.MinValue;
                float min_y = float.MaxValue;
                float max_y = float.MinValue;
                foreach (var a in vectors)
                {
                    min_x = Math.Min(min_x, a.X);
                    max_x = Math.Max(max_x, a.X);
                    min_y = Math.Min(min_y, a.Y);
                    max_y = Math.Max(max_y, a.Y);
                }
                cx = min_x + (max_x - min_x) / 2f;
                cy = min_y + (max_y - min_y) / 2f;
            }
        }

        public void MoveImpactInner(ICollection<IVector2> vectors, IVector2 obj, float spacing_size, float angle, float distance, int depth, int max_depth)
        {
            float dx = (float)(Math.Cos(angle) * distance);
            float dy = (float)(Math.Sin(angle) * distance);
            obj.X += dx;
            obj.Y += dy;
            if (depth < max_depth)
            {
                float dr2 = spacing_size * 2;
                foreach (var o in vectors)
                {
                    if (!o.Equals(obj))
                    {
                        float dr = MathVector.getDistance(o, obj) - dr2;
                        if (dr < 0)
                        {
                            float ta = MathVector.getDegree(obj.X, obj.Y, o.X, o.Y);
                            MoveImpactInner(vectors, o, spacing_size, ta, -dr, depth + 1, max_depth);
                        }
                    }
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 随机调整每个点，
        /// 使得距离都最小保持在spacing_size
        /// </summary>
        public void DistributeSpacingSizeRandom(Vector2 center, ICollection<IVector2> vectors, float spacing_size)
        {
            foreach (var o in vectors)
            {
                MoveImpactInner(vectors, o, spacing_size, (float)(RandomN.NextFloat() * CMath.RADIANS_360), 0, 0, 1);
            }
        }
        /// <summary>
        /// 按环形调整每个点，
        /// 使得距离都最小保持在spacing_size
        /// </summary>
        public void DistributeSpacingSizeRandomCycle(Vector2 center, ICollection<IVector2> vectors, float spacing_size)
        {
            float cx = center.X;
            float cy = center.Y;
            int count = vectors.Count;
            float total_len = count * spacing_size;
            float total_r = total_len / CMath.PI_F / 2f;
            int i = 0;
            var angleStep = CMath.PI_MUL_2 / count;
            var angleStart = 0f;
            foreach (var o in vectors)
            {
                float da = angleStart + (RandomN.NextFloat() * angleStep);
                o.X = cx + (float)Math.Cos(da) * total_r;
                o.Y = cy + (float)Math.Sin(da) * total_r;
                angleStart += angleStep;
                i++;
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 按正方形调整每个点，
        /// 使得距离都最小保持在spacing_size
        /// </summary>
        public void DistributeSpacingSizeSquare(Vector2 center, ICollection<IVector2> vectors, float spacing_size)
        {
            float cx = center.X;
            float cy = center.Y;
            int row_count = (int)Math.Round(Math.Sqrt(vectors.Count));
            //GetCenterOf(vectors, out cx, out cy);
            float sx = cx - (row_count - 1) * spacing_size / 2;
            float sy = cy - (vectors.Count / row_count - 1) * spacing_size / 2;
            int i = 0;
            foreach (var o in vectors)
            {
                int x = i % row_count;
                int y = i / row_count;
                o.X = sx + x * spacing_size;
                o.Y = sy + y * spacing_size;
                i++;
            }
        }
        /// <summary>
        /// 按圆形调整每个点，
        /// 使得距离都最小保持在spacing_size
        /// </summary>
        public void DistributeSpacingSizeRound(Vector2 center, ICollection<IVector2> vectors, float spacing_size)
        {
            float cx = center.X;
            float cy = center.Y;
            int count = vectors.Count;
            //T[] array = new T[count];
            //vectors.CopyTo(array, 0);
            int cycle = 0;
            int i = 0;
            using (var array = pool.AllocList<IVector2>(vectors))
            {
                while (i < count)
                {
                    if (i == 0)
                    {
                        array[i].X = cx;
                        array[i].Y = cy;
                        i++;
                    }
                    else
                    {
                        float cr = spacing_size * cycle;
                        float clen = cr * 2 * CMath.PI_F;
                        int ccount = (int)(clen / spacing_size);
                        float cangle = CMath.RADIANS_360 / ccount;
                        for (int j = 0; j < ccount && i < count; j++)
                        {
                            float da = cangle * j;
                            array[i].X = cx + (float)Math.Cos(da) * cr;
                            array[i].Y = cy + (float)Math.Sin(da) * cr;
                            i++;
                        }
                    }
                    cycle++;
                }
            }
        }
        /// <summary>
        /// 按环形调整每个点，
        /// 使得距离都最小保持在spacing_size
        /// </summary>
        public void DistributeSpacingSizeCycle(Vector2 center, ICollection<IVector2> vectors, float spacing_size)
        {
            float cx = center.X;
            float cy = center.Y;
            int count = vectors.Count;
            float total_len = count * spacing_size;
            float total_r = total_len / CMath.PI_F / 2f;
            float sangle = CMath.RADIANS_360 / count;
            int i = 0;
            foreach (var o in vectors)
            {
                float da = sangle * i;
                o.X = cx + (float)Math.Cos(da) * total_r;
                o.Y = cy + (float)Math.Sin(da) * total_r;
                i++;
            }
        }
        /// <summary>
        /// 按蜂窝状调整每个点，
        /// 使得距离都最小保持在spacing_size
        /// </summary>
        public void DistributeSpacingSizeBeehive(Vector2 center, ICollection<IVector2> vectors, float spacing_size)
        {
            float cx = center.X;
            float cy = center.Y;
            int count = vectors.Count;
            //T[] array = new T[count];
            //vectors.CopyTo(array, 0);
            int cycle = 0;
            int i = 0;
            float d_angle = CMath.RADIANS_360 / 6;
            using (var array = pool.AllocList<IVector2>(vectors))
            {
                while (i < count)
                {
                    if (i == 0)
                    {
                        array[i].X = cx;
                        array[i].Y = cy;
                        i++;
                    }
                    else
                    {
                        float c_r = spacing_size * cycle;
                        for (int j = 0; j < 6 && i < count; j++)
                        {
                            float s_angle = d_angle * j;
                            float s_x = cx + (float)Math.Cos(s_angle) * c_r;
                            float s_y = cy + (float)Math.Sin(s_angle) * c_r;
                            float b_angle = s_angle + d_angle * 2;
                            for (int aj = 0; aj < cycle && i < count; aj++)
                            {
                                float blen = aj * spacing_size;
                                array[i].X = s_x + (float)Math.Cos(b_angle) * blen;
                                array[i].Y = s_y + (float)Math.Sin(b_angle) * blen;
                                i++;
                            }
                        }
                    }
                    cycle++;
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------

        public void DistributeSpacingSizeHorizontal(Vector2 center, ICollection<IVector2> vectors, float spacing_size)
        {
            float cx = center.X;
            float cy = center.Y;
            float sx = cx - (vectors.Count) * spacing_size / 2;
            float sy = cy;
            int i = 0;
            foreach (var o in vectors)
            {
                o.X = sx + i * spacing_size;
                o.Y = sy;
                i++;
            }
        }
        public void DistributeSpacingSizeVertical(Vector2 center, ICollection<IVector2> vectors, float spacing_size)
        {
            float cx = center.X;
            float cy = center.Y;
            float sx = cx;
            float sy = cy - (vectors.Count) * spacing_size / 2;
            int i = 0;
            foreach (var o in vectors)
            {
                o.X = sx;
                o.Y = sy + i * spacing_size;
                i++;
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------
    }
}
