using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Unity3D.Cell
{
    public static class CPJEnviroment
    {
        public static float GLOBAL_PIXEL_PER_UNIT = 16;
        public static float GLOBAL_TICK_PER_SECOND = 10;
        public static float GLOBAL_TICK_INTERVAL_MS => 1000f / GLOBAL_TICK_PER_SECOND;
    }
}
