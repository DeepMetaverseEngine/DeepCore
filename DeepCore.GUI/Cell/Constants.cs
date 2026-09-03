using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace DeepCore.GUI.Cell
{
    public static class Constants
    {
        public const int MSG_HEADER = 0x4570000;
    }
    public enum BlockType : int
    {

        CD_TYPE_RECT = 1,
        CD_TYPE_LINE = 2,
        CD_TYPE_POINT = 3,
    }
}
