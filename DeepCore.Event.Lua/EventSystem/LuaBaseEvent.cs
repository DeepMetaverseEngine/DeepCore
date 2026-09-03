using DeepCore.Event.EventSystem.Events;

namespace DeepCore.Event.Lua.EventSystem
{
    public class LuaBaseEvent : CustomEvent
    {
        public new LuaEventManager Mgr => base.Mgr as LuaEventManager;
    }
}
