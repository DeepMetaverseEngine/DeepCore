using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Geometry
{


    public struct Location2D
    {
        public int X;
        public int Y;
        public Location2D(int x, int y)
        {
            X = x;
            Y = y;
        }
        public static implicit operator Location2D(in Size2D value)
        {
            return new Location2D()
            {
                X = value.X,
                Y = value.Y,
            };
        }
        public static implicit operator Location2D(in int[] value)
        {
            return new Location2D()
            {
                X = value[0],
                Y = value[1],
            };
        }
        public static implicit operator Vector2(in Location2D value)
        {
            return new Vector2()
            {
                X = value.X,
                Y = value.Y,
            };
        }
        public static bool operator ==(in Location2D value1, in Location2D value2)
        {
            return value1.X == value2.X && value1.Y == value2.Y;
        }
        public static bool operator !=(in Location2D value1, in Location2D value2)
        {
            return value1.X != value2.X || value1.Y != value2.Y;
        }
        public override bool Equals(object obj)
        {
            if (obj is Location2D other) return this == other;
            return false;
        }
        public bool Equals(in Location2D other)
        {
            return this == other;
        }
        public bool Equals(Location2D other)
        {
            return this == other;
        }
        public override int GetHashCode()
        {
            return (int)((this.X * 7 + this.Y * 3));
        }
        public override string ToString()
        {
            return $"[{X} {Y}]";
        }
    }
    /// <summary>
    /// 3D坐标
    /// </summary>
    public struct Location3D
    {
        public int X;
        public int Y;
        public int Z;
        public Location3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public static implicit operator Location3D(in Size3D value)
        {
            return new Location3D()
            {
                X = value.X,
                Y = value.Y,
                Z = value.Z,
            };
        }
        public static implicit operator Location3D(in int[] value)
        {
            return new Location3D()
            {
                X = value[0],
                Y = value[1],
                Z = value[2],
            };
        }
        public static implicit operator Location3D(in Vector3 value)
        {
            return new Location3D()
            {
                X = (int)value.X,
                Y = (int)value.Y,
                Z = (int)value.Z,
            };
        }
        public static implicit operator Vector3(in Location3D value)
        {
            return new Vector3()
            {
                X = value.X,
                Y = value.Y,
                Z = value.Z,
            };
        }
        public static bool operator ==(in Location3D value1, in Location3D value2)
        {
            return value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z;
        }
        public static bool operator !=(in Location3D value1, in Location3D value2)
        {
            return value1.X != value2.X || value1.Y != value2.Y || value1.Z != value2.Z;
        }
        public override bool Equals(object obj)
        {
            if (obj is Location3D other) return this == other;
            return false;
        }
        public bool Equals(in Location3D other)
        {
            return this == other;
        }
        public bool Equals(Location3D other)
        {
            return this == other;
        }
        public override int GetHashCode()
        {
            return (int)((this.X * 7 + this.Y * 3 + this.Z * 13));
        }
        public override string ToString()
        {
            return $"[{X} {Y} {Z}]";
        }
    }

    /// <summary>
    /// 2D尺寸
    /// </summary>
    public struct Size2D
    {
        public int X;
        public int Y;
        public int Width { get => X; set => X = value; }
        public int Height { get => Y; set => Y = value; }
        public Size2D(int x, int y)
        {
            X = x;
            Y = y;
        }
        public static implicit operator Size2D(in Location2D value)
        {
            return new Size2D()
            {
                X = value.X,
                Y = value.Y,
            };
        }
        public static implicit operator Size2D(in int[] value)
        {
            return new Size2D()
            {
                X = value[0],
                Y = value[1],
            };
        }
        public static implicit operator Vector2(in Size2D value)
        {
            return new Vector2()
            {
                X = value.X,
                Y = value.Y,
            };
        }
        public static bool operator ==(in Size2D value1, in Size2D value2)
        {
            return value1.X == value2.X && value1.Y == value2.Y;
        }
        public static bool operator !=(in Size2D value1, in Size2D value2)
        {
            return value1.X != value2.X || value1.Y != value2.Y;
        }
        public override bool Equals(object obj)
        {
            if (obj is Size2D other) return this == other;
            return false;
        }
        public bool Equals(in Size2D other)
        {
            return this == other;
        }
        public bool Equals(Size2D other)
        {
            return this == other;
        }
        public override int GetHashCode()
        {
            return (int)((this.X * 7 + this.Y * 3));
        }
        public override string ToString()
        {
            return $"[{X} {Y}]";
        }

        public static Size2D Maximums(in Size2D size)
        {
            var max = CMath.Max(size.X, size.Y);
            return new Size2D(max, max);
        }
    }
    /// <summary>
    /// 3D尺寸
    /// </summary>
    public struct Size3D
    {
        public int X;
        public int Y;
        public int Z;
        public int Width => X;
        public int Length => Y;
        public int Height => Z;
        public Size3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public static implicit operator Size3D(in Location3D value)
        {
            return new Size3D()
            {
                X = value.X,
                Y = value.Y,
                Z = value.Z,
            };
        }
        public static implicit operator Size3D(in int[] value)
        {
            return new Size3D()
            {
                X = value[0],
                Y = value[1],
                Z = value[2],
            };
        }
        public static implicit operator Size3D(in Vector3 value)
        {
            return new Size3D()
            {
                X = (int)value.X,
                Y = (int)value.Y,
                Z = (int)value.Z,
            };
        }
        public static implicit operator Vector3(in Size3D value)
        {
            return new Vector3()
            {
                X = value.X,
                Y = value.Y,
                Z = value.Z,
            };
        }
        public static bool operator ==(in Size3D value1, in Size3D value2)
        {
            return value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z;
        }
        public static bool operator !=(in Size3D value1, in Size3D value2)
        {
            return value1.X != value2.X || value1.Y != value2.Y || value1.Z != value2.Z;
        }
        public override bool Equals(object obj)
        {
            if (obj is Size3D other) return this == other;
            return false;
        }
        public bool Equals(in Size3D other)
        {
            return this == other;
        }
        public bool Equals(Size3D other)
        {
            return this == other;
        }
        public override int GetHashCode()
        {
            return (int)((this.X * 7 + this.Y * 3 + this.Z * 13));
        }
        public override string ToString()
        {
            return $"[{X} {Y} {Z}]";
        }

        public static Size3D Maximums(in Size3D size)
        {
            var max = CMath.Max(size.X, size.Y, size.Z);
            return new Size3D(max, max, max);
        }
    }

    public static class DataUtil
    {
        /// <summary>
        /// 将pos坐标对齐到ChunkSize
        /// </summary>
        /// <param name="ChunkSize"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Location3D AligningChunkLocation(this Size3D ChunkSize, in Location3D pos)
        {
            return new Location3D(
                CMath.CycDiv(pos.X, ChunkSize.X) * ChunkSize.X,
                CMath.CycDiv(pos.Y, ChunkSize.Y) * ChunkSize.Y,
                CMath.CycDiv(pos.Z, ChunkSize.Z) * ChunkSize.Z);
        }

        public static Location3D CycDivChunkLocation(this Location3D pos, in Size3D gridSize)
        {
            var dbx = CMath.CycDiv(pos.X, gridSize.X);
            var dby = CMath.CycDiv(pos.Y, gridSize.Y);
            var dbz = CMath.CycDiv(pos.Z, gridSize.Z);
            return new Location3D(dbx, dby, dbz);
        }

        /// <summary>
        /// 在Box范围内，获取所有被ChunkSize整除的部分
        /// </summary>
        /// <param name="ChunkSize"></param>
        /// <param name="box"></param>
        /// <param name="action"></param>
        public static void FoeEachChunkLocation(this Size3D ChunkSize, in BoundingBox box, Action<Location3D> action)
        {
            int sx = CMath.CycDiv(box.Min.X, ChunkSize.X);
            int sy = CMath.CycDiv(box.Min.Y, ChunkSize.Y);
            int sz = CMath.CycDiv(box.Min.Z, ChunkSize.Z);
            int dx = CMath.CycDiv(box.Max.X, ChunkSize.X);
            int dy = CMath.CycDiv(box.Max.Y, ChunkSize.Y);
            int dz = CMath.CycDiv(box.Max.Z, ChunkSize.Z);
            for (int x = sx; x <= dx; x++)
            {
                for (int y = sy; y <= dy; y++)
                {
                    for (int z = sz; z <= dz; z++)
                    {
                        action(new Location3D(
                            x * ChunkSize.X,
                            y * ChunkSize.Y,
                            z * ChunkSize.Z));
                    }
                }
            }
        }
        public static bool FoeEachChunkLocation(this Size3D ChunkSize, in BoundingBox box, BreakPredicate<Location3D> action)
        {
            int sx = CMath.CycDiv(box.Min.X, ChunkSize.X);
            int sy = CMath.CycDiv(box.Min.Y, ChunkSize.Y);
            int sz = CMath.CycDiv(box.Min.Z, ChunkSize.Z);
            int dx = CMath.CycDiv(box.Max.X, ChunkSize.X);
            int dy = CMath.CycDiv(box.Max.Y, ChunkSize.Y);
            int dz = CMath.CycDiv(box.Max.Z, ChunkSize.Z);
            for (int x = sx; x <= dx; x++)
            {
                for (int y = sy; y <= dy; y++)
                {
                    for (int z = sz; z <= dz; z++)
                    {
                        if (action(new Location3D(
                            x * ChunkSize.X,
                            y * ChunkSize.Y,
                            z * ChunkSize.Z))) { return true; }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 将size按照slice切分
        /// </summary>
        /// <param name="size"></param>
        /// <param name="slice"></param>
        /// <param name="action"></param>
        public static void FoeEachSlice(this Size3D size, int slice, Action<Location3D> action)
        {
            int sx = 0;
            int sy = 0;
            int sz = 0;
            int dx = CMath.CycDiv(size.X, slice);
            int dy = CMath.CycDiv(size.Y, slice);
            int dz = CMath.CycDiv(size.Z, slice);
            for (int x = sx; x <= dx; x++)
            {
                for (int y = sy; y <= dy; y++)
                {
                    for (int z = sz; z <= dz; z++)
                    {
                        action(new Location3D(
                            x * slice,
                            y * slice,
                            z * slice));
                    }
                }
            }
        }

        public static void FoeEachSlice(this Size2D size, int slice, Action<Location2D> action)
        {
            int sx = 0;
            int sy = 0;
            int dx = CMath.CycDiv(size.X, slice);
            int dy = CMath.CycDiv(size.Y, slice);
            for (int x = sx; x <= dx; x++)
            {
                for (int y = sy; y <= dy; y++)
                {
                    action(new Location2D(
                        x * slice,
                        y * slice));
                }
            }
        }
    }
}
