using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using System;
using System.Collections.Generic;

namespace DeepCore.Voxel.Data
{

    public class VoxelPalette : TerrainPalette
    {
        public VoxelPalette Clone()
        {
            var pal = new VoxelPalette();
            if (this.Colors != null)
            {
                pal.Colors = new TerrainColor[this.Colors.Length];
                Array.Copy(this.Colors, pal.Colors, this.Colors.Length);
            }
            return pal;
        }

        public void Load(VoxelTerrainData data, VoxelBuildConfig cfg)
        {
            this.Colors = VoxelOctreePaletteQuantizer.Build(data, Math.Min(cfg.MaxColor, 256));
        }
        public void Load(InputStream inputT)
        {
            this.Colors = new TerrainColor[inputT.GetU16()];
            for (int i = 0; i < this.Colors.Length; i++)
            {
                this.Colors[i] = TerrainColor.FromARGB(inputT.GetU32());
            }
        }
        public void Save(OutputStream outputT)
        {
            outputT.PutU16((ushort)this.Colors.Length);
            for (int i = 0; i < this.Colors.Length; i++)
            {
                outputT.PutU32(this.Colors[i].ARGB);
            }
        }
    }

    public class VoxelOctreePaletteQuantizer : TerrainPaletteOctreeQuantizer
    {
        public static TerrainColor[] Build(VoxelTerrainData matrix, int maxColors)
        {
            var root = Node.CreateRoot();
            foreach (var cell in matrix.Grids)
            {
                if (cell != null)
                {
                    foreach (var voxel in cell)
                    {
                        root.AddColor2Root(voxel.Color, 8);
                    }
                }
            }
            return Node.MergeColors(root, maxColors);
        }
    }

#if false

    package com.gys.pngquant.octree;

    import java.util.ArrayList;
    import java.util.HashMap;
    import java.util.List;
    import java.util.Map;

    public class Node
    {

        private int depth = 0;// 为0时为root节点
        private Node parent;
        private Node[] children = new Node[8];
        private boolean isLeaf = false;
        private int rNum = 0;
        private int gNum = 0;
        private int bNum = 0;
        private int piexls = 0;
        private Map<Integer, List<Node>> levelMapping;// 存放层次和node的关系

        public int getRGBValue()
        {

            int r = this.rNum / this.piexls;
            int g = this.gNum / this.piexls;
            int b = this.bNum / this.piexls;

            return (r << 16 | g << 8 | b);
        }


        public Map<Integer, List<Node>> getLevelMapping()
        {
            return levelMapping;
        }

        public void afterSetParam()
        {
            if (this.getParent() == null && this.depth == 0)
            {
                levelMapping = new HashMap<Integer, List<Node>>();
                for (int i = 1; i <= 8; i++)
                {
                    levelMapping.put(i, new ArrayList<Node>());
                }
            }
        }

        public int getrNum()
        {
            return rNum;
        }

        public void setrNum(int rNum)
        {
            if (!isLeaf)
            {
                throw new UnsupportedOperationException();
            }
            this.rNum = rNum;
        }

        public int getgNum()
        {
            return gNum;
        }

        public void setgNum(int gNum)
        {
            if (!isLeaf)
            {
                throw new UnsupportedOperationException();
            }
            this.gNum = gNum;
        }

        public int getbNum()
        {
            return bNum;
        }

        public void setbNum(int bNum)
        {
            if (!isLeaf)
            {
                throw new UnsupportedOperationException();
            }
            this.bNum = bNum;
        }

        public int getPiexls()
        {
            return piexls;
        }

        public void setPiexls(int piexls)
        {
            if (!isLeaf)
            {
                throw new UnsupportedOperationException();
            }
            this.piexls = piexls;
        }

        public int getDepth()
        {
            return depth;
        }

        // 返回节点原有的子节点数量
        public int mergerLeafNode()
        {
            if (this.isLeaf)
            {
                return 1;
            }

            this.setLeaf(true);
            int rNum = 0;
            int gNum = 0;
            int bNum = 0;
            int pixel = 0;
            int i = 0;
            for (Node child : this.children)
            {
                if (child == null)
                {
                    continue;
                }
                rNum += child.getrNum();
                gNum += child.getgNum();
                bNum += child.getbNum();
                pixel += child.getPiexls();
                i += 1;
            }
            this.setrNum(rNum);
            this.setgNum(gNum);
            this.setbNum(bNum);
            this.setPiexls(pixel);
            this.children = null;
            return i;
        }
        // 获取最深层次的node
        public Node getDepestNode()
        {
            for (int i = 7; i > 0; i--)
            {
                List<Node> levelList = this.levelMapping.get(i);
                if (!levelList.isEmpty())
                {
                    return levelList.remove(levelList.size() - 1);
                }
            }
            return null;
        }
        // 获取叶子节点的数量
        public int getLeafNum()
        {
            if (isLeaf)
            {
                return 1;
            }
            int i = 0;
            for (Node child : this.children)
            {
                if (child != null)
                {
                    i += child.getLeafNum();
                }
            }
            return i;
        }

        public void setDepth(int depth)
        {
            this.depth = depth;
        }

        public Node getParent()
        {
            return parent;
        }

        public void setParent(Node parent)
        {
            this.parent = parent;
        }

        public Node[] getChildren()
        {
            return children;
        }


        public Node getChild(int index)
        {
            return children[index];
        }

        public void setChild(int index, Node node)
        {
            children[index] = node;
        }

        public boolean isLeaf()
        {
            return isLeaf;
        }

        public void setPixel(int r, int g, int b)
        {
            this.rNum += r;
            this.gNum += g;
            this.bNum += b;
            this.piexls += 1;
        }

        public void setLeaf(boolean isLeaf)
        {
            this.isLeaf = isLeaf;
        }

        public void add8Bite2Root(int _taget, int _speed)
        {

            if (depth != 0 || this.parent != null)
            {
                throw new UnsupportedOperationException();
            }

            int speed = 7 + 1 - _speed;

            int r = _taget >> 16 & 0xFF;
            int g = _taget >> 8 & 0xFF;
            int b = _taget & 0xFF;
            Node proNode = this;
            for (int i = 7; i >= speed; i--)
            {
                int item = ((r >> i & 1) << 2) + ((g >> i & 1) << 1) + (b >> i & 1);
                Node child = proNode.getChild(item);
                if (child == null)
                {
                    child = new Node();
                    child.setDepth(8 - i);
                    child.setParent(proNode);
                    child.afterSetParam();
                    this.levelMapping.get(child.getDepth()).add(child);
                    proNode.setChild(item, child);
                }

                if (i == speed)
                {
                    child.setLeaf(true);
                }
                if (child.isLeaf())
                {
                    child.setPixel(r, g, b);
                    break;
                }
                proNode = child;
            }

        }

        public static Node build(int[][] matrix, int speed)
        {
            Node root = new Node();
            root.afterSetParam();
            for (int[] row : matrix)
            {
                for (int cell : row)
                {
                    root.add8Bite2Root(cell, speed);
                }
            }
            return root;
        }

        public static byte[] mergeColors(Node root, int maxColors)
        {

            byte[] byteArray = new byte[maxColors * 3];
            List<Byte> result = new ArrayList<Byte>();

            int leafNum = root.getLeafNum();
            try
            {
                while (leafNum > maxColors)
                {
                    int mergerLeafNode = root.getDepestNode().mergerLeafNode();
                    leafNum -= (mergerLeafNode - 1);
                }
            }
            catch (Exception e)
            {
                e.printStackTrace();
            }
            fillArray(root, result, 0);
            int i = 0;
            for (Byte byte1 : result)
            {
                byteArray[i++] = byte1;
            }
            return byteArray;
        }

        private static void fillArray(Node node, List<Byte> result, int offset)
        {

            if (node == null)
            {
                return;
            }
            if (node.isLeaf())
            {
                result.add((byte)(node.getrNum() / node.getPiexls()));
                result.add((byte)(node.getgNum() / node.getPiexls()));
                result.add((byte)(node.getbNum() / node.getPiexls()));
            }
            else
            {
                for (Node child : node.getChildren())
                {
                    fillArray(child, result, offset);
                }
            }
        }
    }
#endif
}
