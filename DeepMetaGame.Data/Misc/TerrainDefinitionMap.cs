using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{
    [MessageType(BattleConstants.TerrainDefinitionMap)]
    [Desc("地块定义信息")]
    [Expandable]
    public class TerrainDefinitionMap : ISerializable
    {
        [Desc("地块定义")]
        [MessageType(BattleConstants.MapBlockBrush)]
        [Expandable]
        public class MapBlockBrush : ISerializable
        {
            [ColorValueAttribute]
            [Desc("颜色值ARGB", "Key")]
            public int Value = ColorValueAttribute.COLOR_GREEN;
            [Desc("地图颜色标记")]
            public MapBlockBrushFlag Flag = MapBlockBrushFlag.Walkable;
            [Desc("名字")]
            public string Name = "";
            [Desc("注释")]
            public string Desc = "";
            [Desc("Tag")]
            public string Tag = "";

            public MapBlockBrush() { }
            public MapBlockBrush(int value, MapBlockBrushFlag flag, string name)
            {
                Value = value;
                Flag = flag;
                Name = name;
            }

            [Desc("是否阻挡", "Key")]
            public bool IsBlock
            {
                get => (Flag & MapBlockBrushFlag.Block) != 0;
                set
                {
                    if (value) Flag |= MapBlockBrushFlag.Block;
                    else Flag &= ~MapBlockBrushFlag.Block;
                }
            }
            public override string ToString()
            {
                return string.Format("[{1}] {2} : {0}", Value.ToString("X8"), Flag, Name);
            }
        }

        [NotNull]
        [Desc("地块定义列表")]
        public ArrayList<MapBlockBrush> Brushes = new ArrayList<MapBlockBrush>();

        public TerrainDefinitionMap()
        {
            Brushes.Add(new MapBlockBrush(0, MapBlockBrushFlag.NA, "NA"));
            Brushes.Add(new MapBlockBrush(ColorValueAttribute.COLOR_GREEN, MapBlockBrushFlag.Block, "Block"));
            Brushes.Add(new MapBlockBrush(ColorValueAttribute.COLOR_DARK_GRAY, MapBlockBrushFlag.Safe, "Safe"));
        }

        /// <summary>
        /// 去从
        /// </summary>
        public bool ContainsValue(int value)
        {
            foreach (var b in Brushes)
            {
                if (b.Value == value)
                {
                    return true;
                }
            }
            return false;
        }
        public MapBlockBrush GetMapBlockBrush(int value)
        {
            foreach (var b in Brushes)
            {
                if (b.Value == value)
                {
                    return b;
                }
            }
            return null;
        }
        public bool TryGetMapBlockBrushByIndex(int index, out MapBlockBrush brush)
        {
            if (index >= 0 && index < Brushes.Count)
            {
                brush = Brushes[index];
                return true;
            }
            brush = null;
            return false;
        }
        public bool TryGetMapBlockBrushByFlag(MapBlockBrushFlag flag, out MapBlockBrush brush)
        {
            return Brushes.TryFind(b => b.Flag == flag, out brush);
        }
        public bool TryGetMapBlockBrushByName(string name, out MapBlockBrush brush)
        {
            return Brushes.TryFind(b => b.Name == name, out brush);
        }
    }


    [Desc("地图颜色标记")]
    public enum MapBlockBrushFlag
    {
        [Desc("NA")]
        NA = 0,
        [Desc("行走面")]
        Walkable = 1,
        [Desc("阻挡")]
        Block = 2,
        [Desc("安全区")]
        Safe = 4,
    }


}
