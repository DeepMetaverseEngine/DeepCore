using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepMetaGame.Data.Template
{
    //---------------------------------------------------------------------------------//
    /// <summary>
    /// 场景基础数据
    /// </summary>
    [MessageType(BattleConstants.ZoneInfo)]
    public class ZoneInfo : IExternalizable
    {
        public int GridCellW { get; private set; }
        public int GridCellH { get; private set; }
        public int XCount { get; private set; }
        public int YCount { get; private set; }
        public int DefaultFlag { get; private set; }

        [Desc("", "", false)]
        public int TotalTop = int.MaxValue;
        [Desc("", "", false)]
        public int TotalBottom = int.MinValue;
        //------------------------------------------------------------
        [Desc("", "", false)]
        private int[,] mTerrainMatrix;
        //------------------------------------------------------------
        /// <summary>
        /// 地图总宽
        /// </summary>
        public int TotalWidth { get { return XCount * GridCellW; } }
        /// <summary>
        /// 地图总高
        /// </summary>
        public int TotalHeight { get { return YCount * GridCellH; } }
        public bool HasFlag { get => mTerrainMatrix != null; }
        public int this[int x, int y]
        {
            get { return GetFlag(x, y); }
            set { PutFlag(x, y, value); }
        }
        //------------------------------------------------------------
        public ZoneInfo(int xcount, int ycount, int gridW, int gridH, int defaultFlag = 0)
        {
            XCount = xcount;
            YCount = ycount;
            GridCellW = gridW;
            GridCellH = gridH;
            DefaultFlag = defaultFlag;
            //mTerrainMatrix = new int[xcount, ycount];
        }
        public ZoneInfo() { }
        //----------------------------------------------------------------------------------------------
        public bool CheckHasFlag()
        {
            if (mTerrainMatrix != null)
            {
                for (int x = 0; x < XCount; x++)
                {
                    for (int y = 0; y < YCount; y++)
                    {
                        if (mTerrainMatrix[x, y] != DefaultFlag) return true;
                    }
                }
            }
            return false;
        }
        public void CleanTerrainMatrix()
        {
            mTerrainMatrix = null;
        }
        public int GetFlag(int bx, int by)
        {
            if (mTerrainMatrix != null)
            {
                return mTerrainMatrix[bx, by];
            }
            return DefaultFlag;
        }
        public void PutFlag(int bx, int by, int flag)
        {
            if (DefaultFlag != flag)
            {
                DefaultFlag = flag;
                if (mTerrainMatrix == null)
                {
                    mTerrainMatrix = new int[XCount, YCount];
                }
                mTerrainMatrix[bx, by] = flag;
            }
            else
            {
                if (mTerrainMatrix != null)
                {
                    mTerrainMatrix[bx, by] = flag;
                }
            }
        }
        public bool TryGetFlag(int bx, int by, out int flag)
        {
            if (bx < XCount && bx >= 0 && by < YCount && by >= 0)
            {
                if (mTerrainMatrix != null)
                {
                    flag = mTerrainMatrix[bx, by];
                }
                else
                {
                    flag = DefaultFlag;
                }
                return true;
            }
            else
            {
                flag = DefaultFlag;
                return false;
            }
        }
        public bool TryGetFlagByPos(float x, float y, out int flag)
        {
            return TryGetFlag((int)(x / GridCellW), (int)(y / GridCellH), out flag);
        }
        public float GetFlagByPos(float x, float y)
        {
            TryGetFlag((int)(x / GridCellW), (int)(y / GridCellH), out var flag);
            return flag;
        }
        //----------------------------------------------------------------------------------------------
        public void ForEach<ST>(ST st, ForEachFlagAction<ST> action)
        {
            if (mTerrainMatrix != null)
            {
                for (int x = 0; x < XCount; x++)
                {
                    for (int y = 0; y < YCount; y++)
                    {
                        action(st, x, y, mTerrainMatrix[x, y]);
                    }
                }
            }
        }
        public delegate void ForEachFlagAction<ST>(ST st, int x, int y, int flag);
        //----------------------------------------------------------------------------------------------
        public void WriteExternal(IOutputStream output)
        {
            output.PutS32(XCount);
            output.PutS32(YCount);
            output.PutS32(GridCellW);
            output.PutS32(GridCellH);
            output.PutS32(TotalTop);
            output.PutS32(TotalBottom);
            output.PutS32(DefaultFlag);
            output.PutBool(mTerrainMatrix != null);
            if (mTerrainMatrix != null)
            {
                for (int x = 0; x < XCount; x++)
                {
                    for (int y = 0; y < YCount; y++)
                    {
                        output.PutS32(mTerrainMatrix[x, y]);
                    }
                }
            }
        }
        public void ReadExternal(IInputStream input)
        {
            XCount = input.GetS32();
            YCount = input.GetS32();
            GridCellW = input.GetS32();
            GridCellH = input.GetS32();
            TotalTop = input.GetS32();
            TotalBottom = input.GetS32();
            DefaultFlag = input.GetS32();
            if (input.GetBool())
            {
                mTerrainMatrix = new int[XCount, YCount];
                for (int x = 0; x < XCount; x++)
                {
                    for (int y = 0; y < YCount; y++)
                    {
                        mTerrainMatrix[x, y] = input.GetS32();
                    }
                }
            }
        }
    }

    //---------------------------------------------------------------------------------//


    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//


    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//


    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//
}
