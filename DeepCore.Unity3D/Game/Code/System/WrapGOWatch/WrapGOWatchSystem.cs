using Code.System.Resource;
using UnityEngine;

namespace Code.System.WrapGOWatch
{
    public static class WrapGOWatchSystem
    {
        public static void Add(IWrapGO wrap)
        {
            WrapGOWatchSystemImpl.Inst.Add(wrap);
        }
        
        public static void Remove(IWrapGO wrap)
        {
            WrapGOWatchSystemImpl.Inst.Remove(wrap);
        }

        public static IWrapGO Get(GameObject go)
        {
            return WrapGOWatchSystemImpl.Inst.Get(go);
        }

        public static void ForceUpdate()
        {
            WrapGOWatchSystemImpl.Inst.ForceUpdate();
        }
    }
}
