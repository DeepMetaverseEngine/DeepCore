using DeepMetaGame.Data.ZoneGeometry;

namespace DeepCore.Game3D.Slave.Helper
{
    public enum TryMoveToMapBorderResult : byte
    {
        ARRIVE = 0,
        TOUCH = 1,
        BLOCK = 2,
    }
    public struct MoveBlockResult
    {
        public MoveResult result;
        public ILayerZoneEntity touched;

        public MoveBlockResult(MoveResult r)
        {
            this.result = r;
            this.touched = null;
        }
        public bool HasFlag(MoveResult flag)
        {
            return (result & flag) != 0;
        }
    }
}
