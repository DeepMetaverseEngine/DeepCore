using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Geometry.Terrain
{

    public struct TerrainColor
    {
        public readonly static TerrainColor Zero = new TerrainColor() { A = 0, R = 0, G = 0, B = 0, };

        public byte A;
        public byte R;
        public byte G;
        public byte B;
        public uint ARGB
        {
            get
            {
                uint rgb = 0;
                rgb |= ((uint)(A)) << 24;
                rgb |= ((uint)(R)) << 16;
                rgb |= ((uint)(G)) << 8;
                rgb |= ((uint)(B));
                return rgb;
            }
        }
        public static TerrainColor FromARGB(uint argb)
        {
            return new TerrainColor()
            {
                A = (byte)((0xff000000 & argb) >> 24),
                R = (byte)((0x00ff0000 & argb) >> 16),
                G = (byte)((0x0000ff00 & argb) >> 8),
                B = (byte)((0x000000ff & argb) >> 0),
            };
        }
        public static TerrainColor FromARGB(int argb)
        {
            return new TerrainColor()
            {
                A = (byte)((0xff000000 & argb) >> 24),
                R = (byte)((0x00ff0000 & argb) >> 16),
                G = (byte)((0x0000ff00 & argb) >> 8),
                B = (byte)((0x000000ff & argb) >> 0),
            };
        }
        public static implicit operator TerrainColor(in uint argb)
        {
            return TerrainColor.FromARGB(argb);
        }
        public static implicit operator TerrainColor(in int argb)
        {
            return TerrainColor.FromARGB(argb);
        }
        public static implicit operator uint(in TerrainColor argb)
        {
            return argb.ARGB;
        }
        public static bool operator ==(in TerrainColor value1, in TerrainColor value2)
        {
            return value1.R == value2.R
                && value1.G == value2.G
                && value1.B == value2.B;
        }
        public static bool operator !=(in TerrainColor value1, in TerrainColor value2)
        {
            return value1.R != value2.R
                || value1.G != value2.G
                || value1.B != value2.B;
        }
        public override bool Equals(object obj)
        {
            if (obj is TerrainColor value2)
                return this.R == value2.R && this.G == value2.G && this.B == value2.B;
            return false;
        }
        public bool Equals(in TerrainColor value2)
        {
            return this.R == value2.R && this.G == value2.G && this.B == value2.B;
        }
        public override int GetHashCode()
        {
            return (int)this.ARGB;
        }
        public override string ToString()
        {
            return ARGB.ToString("X8");
        }
    }
    public class TerrainPalette
    {
        public TerrainColor[] Colors;


        public TerrainColor GetColor(byte index)
        {
            return Colors[index];
        }
        public byte IndexOfColor(int argb, out TerrainColor color)
        {
            color = TerrainColor.FromARGB(argb);
            var index = SelectClosestColorIndex(color);
            if (index >= 0 && index < Colors.Length)
            {
                color = Colors[index];
                return (byte)index;
            }
            return 0;
        }
        public byte IndexOfColor(uint argb, out TerrainColor color)
        {
            color = TerrainColor.FromARGB(argb);
            var index = SelectClosestColorIndex(color);
            if (index >= 0 && index < Colors.Length)
            {
                color = Colors[index];
                return (byte)index;
            }
            return 0;
        }
        int SelectClosestColorIndex(TerrainColor color)
        {
            long min = 256 * 256 * 3 + 1;
            int min_idx = -1;
            for (int i = 0; i < Colors.Length; i++)
            {
                var pal = Colors[i];
                var dx = ((int)pal.R - color.R);
                var dy = ((int)pal.G - color.G);
                var dz = ((int)pal.B - color.B);
                long x = dx * dx + dy * dy + dz * dz;
                if (x == 0) return i;
                if (x < min)
                {
                    min_idx = i;
                    min = x;
                }
            }
            return min_idx;//给定某颜色，返回其在调色板中最近似颜色的索引值；
        }
    }



    public class TerrainPaletteOctreeQuantizer
    {
        public class Node
        {

            private int depth = 0;// 为0时为root节点
            private Node parent;
            private Node[] children = new Node[8];

            private bool _isLeaf = false;
            private int _rNum = 0;
            private int _gNum = 0;
            private int _bNum = 0;
            private int _piexls = 0;
            private Dictionary<int, List<Node>> levelMapping;// 存放层次和node的关系

            public int Depth
            {
                get => depth;
                private set => this.depth = value;
            }
            public Node Parent
            {
                get => this.parent;
                private set => this.parent = value;
            }
            public Node[] Children
            {
                get => children;
            }

            public bool IsLeaf
            {
                get => _isLeaf;
                private set => this._isLeaf = value;
            }
            // 获取叶子节点的数量
            public int LeafNum
            {
                get
                {
                    if (_isLeaf)
                    {
                        return 1;
                    }
                    int i = 0;
                    foreach (var child in this.children)
                    {
                        if (child != null)
                        {
                            i += child.LeafNum;
                        }
                    }
                    return i;
                }
            }

            public int NumR
            {
                get => _rNum;
                private set
                {
                    if (!_isLeaf) { throw new Exception(); }
                    this._rNum = value;
                }
            }
            public int NumG
            {
                get => _gNum;
                private set
                {
                    if (!_isLeaf) { throw new Exception(); }
                    this._gNum = value;
                }
            }
            public int NumB
            {
                get => _bNum;
                private set
                {
                    if (!_isLeaf) { throw new Exception(); }
                    this._bNum = value;
                }
            }
            public TerrainColor RGB
            {
                get
                {
                    byte r = (byte)(this._rNum / this._piexls);
                    byte g = (byte)(this._gNum / this._piexls);
                    byte b = (byte)(this._bNum / this._piexls);
                    return new TerrainColor() { A = 0xff, R = r, G = g, B = b, };
                    // (r << 16 | g << 8 | b);
                }
            }
            public int Piexls
            {
                get => _piexls;
                private set
                {
                    if (!_isLeaf) { throw new Exception(); }
                    this._piexls = value;
                }
            }
            private Dictionary<int, List<Node>> LevelMapping
            {
                get => levelMapping;
            }

            public void AfterSetParam()
            {
                if (this.Parent == null && this.depth == 0)
                {
                    levelMapping = new HashMap<int, List<Node>>();
                    for (int i = 1; i <= 8; i++)
                    {
                        levelMapping[i] = new ArrayList<Node>();
                    }
                }
            }




            // 返回节点原有的子节点数量
            private int MergerLeafNode()
            {
                if (this._isLeaf)
                {
                    return 1;
                }

                this.IsLeaf = (true);
                int rNum = 0;
                int gNum = 0;
                int bNum = 0;
                int pixel = 0;
                int i = 0;
                foreach (var child in this.children)
                {
                    if (child == null)
                    {
                        continue;
                    }
                    rNum += child.NumR;
                    gNum += child.NumG;
                    bNum += child.NumB;
                    pixel += child.Piexls;
                    i += 1;
                }
                this.NumR = (rNum);
                this.NumG = (gNum);
                this.NumB = (bNum);
                this.Piexls = (pixel);
                this.children = null;
                return i;
            }

            // 获取最深层次的node
            private Node GetDepestNode()
            {
                for (int i = 7; i > 0; i--)
                {
                    var levelList = this.levelMapping[(i)];
                    if (levelList.TryRemoveAt(levelList.Count - 1, out var ret))
                    {
                        return ret;
                    }
                }
                return null;
            }
            private Node GetChild(int index)
            {
                return children[index];
            }
            private void SetChild(int index, Node node)
            {
                children[index] = node;
            }
            private void SetPixel(int r, int g, int b)
            {
                this._rNum += r;
                this._gNum += g;
                this._bNum += b;
                this._piexls += 1;
            }
            public void AddColor2Root(TerrainColor _tagetColor, int _speed)
            {

                if (depth != 0 || this.parent != null)
                {
                    throw new Exception();
                }

                int speed = 7 + 1 - _speed;

                int r = _tagetColor.R;
                int g = _tagetColor.G;
                int b = _tagetColor.B;
                Node proNode = this;
                for (int i = 7; i >= speed; i--)
                {
                    int item = ((r >> i & 1) << 2) + ((g >> i & 1) << 1) + (b >> i & 1);
                    Node child = proNode.GetChild(item);
                    if (child == null)
                    {
                        child = new Node();
                        child.Depth = (8 - i);
                        child.Parent = (proNode);
                        child.AfterSetParam();
                        this.levelMapping[child.Depth].Add(child);
                        proNode.SetChild(item, child);
                    }

                    if (i == speed)
                    {
                        child.IsLeaf = (true);
                    }
                    if (child.IsLeaf)
                    {
                        child.SetPixel(r, g, b);
                        break;
                    }
                    proNode = child;
                }

            }

            public static Node CreateRoot()
            {
                var root = new Node();
                root.AfterSetParam();
                return root;
            }

            public static TerrainColor[] MergeColors(Node root, int maxColors)
            {
                var result = new ArrayList<TerrainColor>();
                int leafNum = root.LeafNum;
                try
                {
                    while (leafNum > maxColors)
                    {
                        int mergerLeafNode = root.GetDepestNode().MergerLeafNode();
                        leafNum -= (mergerLeafNode - 1);
                    }
                }
                catch (Exception e)
                {
                    e.PrintStackTrace();
                }
                FillArray(root, result, 0);
                return result.ToArray();
            }

            private static void FillArray(Node node, List<TerrainColor> result, int offset)
            {
                if (node == null)
                {
                    return;
                }
                if (node.IsLeaf)
                {
                    result.Add(new TerrainColor()
                    {
                        A = 0xff,
                        R = (byte)(node.NumR / node.Piexls),
                        G = (byte)(node.NumG / node.Piexls),
                        B = (byte)(node.NumB / node.Piexls),
                    });
                }
                else
                {
                    foreach (Node child in node.Children)
                    {
                        FillArray(child, result, offset);
                    }
                }
            }
        }
    }

}
