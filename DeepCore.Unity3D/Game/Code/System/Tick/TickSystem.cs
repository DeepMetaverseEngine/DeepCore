namespace Code.System.Tick
{
    public static class TickSystem
    {
        public delegate void TickHandler(long serial, int index);

        public static long Tick(float intervalTime, TickHandler callback, int count = 1)
        {
            return TickSystemImpl.Inst.Tick(intervalTime, callback, count);
        }

        public static void TickCancel(long serial)
        {
            TickSystemImpl.Inst.TickCancel(serial);
        }
    }
}
