using DeepCore;
using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Space;
using DeepCore.Voxel.Extensions.MagicaVoxel;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace DeepCore.Voxel.Data
{
    //--------------------------------------------------------------------------------------------------

    //--------------------------------------------------------------------------------------------------
    public enum VoxelCombineDirection : byte
    {
        [Desc("从上合并")]
        Up = 0,
        [Desc("从下合并")]
        Down = 1,
        [Desc("从上同色合并")]
        UpSameColor = 2,
        [Desc("从下同色合并")]
        DownSameColor = 3,

        [Desc("只留最上层")]
        Up2D = 4,
        [Desc("只留最下层")]
        Down2D = 5,

        NA = 255,
    }
    public enum VoxelLinkDirection : byte
    {
        [Desc("九宫格")]
        Sudoku = 0,
        [Desc("十字")]
        Cross = 1,
        NA = 255,
    }
    public enum VoxelClipHorizon : byte
    {
        [Desc("合并")]
        Combine = 0,
        [Desc("丢弃")]
        Drop = 1,
        [Desc("限定在AABB")]
        AABB = 2,
        NA = 255,
    }
    public enum AstarType
    {
        [Desc("单格网络")]
        Voxel = 1,
        [Desc("N叉树静态网格")]
        Space = 2,
    }
    public class VoxelBuildConfig
    {
        [Desc("体素最小厚度，防止太薄")]
        public float VoxelMinHeight = 1f;
        [Desc("最小体素间距，两个体素如果小于该值，则合并，防止个头太高")]
        public float VoxelMinDistance = 2f;
        [Desc("体素合并时，Color值覆盖方式")]
        public VoxelCombineDirection CombineDir = VoxelCombineDirection.Up;
        [Desc("体素行走面链接方式")]
        public VoxelLinkDirection LinkDir = VoxelLinkDirection.Sudoku;
        [Desc("移动时可通过的高度差，阶梯高度")]
        public float StepIntercept = 1f;
        [Desc("体素导入时，是否翻转Y坐标")]
        public bool FlipY = true;
        [Desc("切割地平线")]
        public VoxelClipHorizon ClipHorizon = VoxelClipHorizon.AABB;
        [Desc("是否切割地平线海拔高度")]
        public float ClipHorizonAltitude = 0;
        [Desc("重写网格尺寸，大于0启效")]
        public float OverrideCellSize = 0f;
        [Desc("是否所有地形允许行走")]
        public bool AllWorkable = true;
        [Desc("最大颜色数")]
        public int MaxColor = 256;
        [Desc("寻路颜色标志")]
        public VoxelColorFlag[] ColorWalkable;
        [Desc("抹除颜色标志")]
        public VoxelColorFlag[] ColorIgnore;
        [Desc("是否压缩")]
        public bool IsGZip = true;
        [Desc("寻路类型")]
        public AstarType AstarType = AstarType.Space;
        [Desc("浮点精度，大于1有效")]
        public int FloatPrecision = 100;
        public VoxelBuildConfig()
        {
            this.ColorWalkable = new VoxelColorFlag[]
            {
                new VoxelColorFlag() { Color = 0xFF00FF00 } ,
            };
            this.ColorIgnore = new VoxelColorFlag[]
            {
                new VoxelColorFlag() { Color = 0x00000000 } ,
                new VoxelColorFlag() { Color = 0xFF000000 } ,
            };
        }

        public bool IsWalkableColor(TerrainColor color)
        {
            if (AllWorkable) return true;
            if (ColorWalkable != null)
            {
                foreach (var cf in ColorWalkable)
                {
                    if (cf.Color == color) return true;
                }
            }
            return false;
        }
        public bool IsIgnoreColor(uint color)
        {
            if (ColorIgnore != null)
            {
                foreach (var cf in ColorIgnore)
                {
                    if (cf.Color == color) return true;
                }
            }
            return false;
        }
        public override string ToString()
        {
            return $"StepIntercept={StepIntercept}";
        }
    }
    //--------------------------------------------------------------------------------------------------

    [Expandable]
    public class VoxelColorFlag
    {
        [Desc("标记颜色")]
        public TerrainColor Color = 0x00FF00;
        public override string ToString()
        {
            return Color.ARGB.ToString("X8");
        }
    }

    //--------------------------------------------------------------------------------------------------


}
