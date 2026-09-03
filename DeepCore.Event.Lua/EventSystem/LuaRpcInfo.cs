using DeepCore.Lua;

namespace DeepCore.Event.Lua.EventSystem
{
    internal struct LuaRpcInfo
    {
        public string ManagerName;
        public string UUID;
        public string Rpc;
        public bool IsStartEvent;
        public bool IsTriggerEvent;
        public bool Broadcast;
        public int ParentEvent;
        public ILuaFunction CallBack;
        public UnionValue Config;
        public UnionValue Arg;
    }
}