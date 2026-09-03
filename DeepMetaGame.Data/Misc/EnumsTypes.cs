using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{
    public enum VoxelAnchor : byte
    {
        [Desc("浮动")]
        Floating = 0,
        [Desc("地板")]
        Flooring = 1,
        [Desc("天花板")]
        Ceiling = 2,
    }
}
