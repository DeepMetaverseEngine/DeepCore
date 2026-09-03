using System;
using System.Collections.Generic;
using System.Linq;
using DeepCore;
using DeepCore.Event.EventSystem.Message;

namespace DeepCore.Event.EventSystem
{
    public class EventManagerFactory
    {
        private static EventManagerFactory sInstance;

        public static EventManagerFactory Instance
        {
            get
            {
                if (sInstance == null)
                {
                    SetFactory<EventManagerFactory>();
                }

                return sInstance;
            }
        }

        public static void SetFactory<T>() where T : EventManagerFactory
        {
            sInstance = Activator.CreateInstance<T>();
            EventManager.Init();
        }


        public delegate EventManager CreateManagerHandler(string type, string id);

        private readonly HashMap<string, EventManager> mEventManagers = new HashMap<string, EventManager>();
        private readonly HashMap<string, CreateManagerHandler> mNames = new HashMap<string, CreateManagerHandler>();


        public EventManager GetEventManager(string type, string id)
        {
            return GetEventManager(EventManager.GetAddress(type, id));
        }

        public EventManager GetEventManager(string address)
        {
            lock (mEventManagers)
            {
                return mEventManagers.Get(address);
            }
        }

        public bool ContainsName(string name, bool ignoreCase, out string realName)
        {
            lock (mNames)
            {
                if (!ignoreCase)
                {
                    var ret = mNames.ContainsKey(name);
                    realName = ret ? name : null;
                    return ret;
                }

                realName = mNames.FirstOrDefault(entry => string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase)).Key;
                return realName != null;
            }
        }

        public bool ContainsName(string name)
        {
            lock (mNames)
            {
                return mNames.ContainsKey(name);
            }
        }

        public EventManager FirstEventManager
        {
            get
            {
                lock (mEventManagers)
                {
                    return mEventManagers.FirstOrDefault().Value;
                }
            }
        }

        public ICollection<string> AllNames
        {
            get
            {
                lock (mNames)
                {
                    return mNames.Keys;
                }
            }
        }

        public Dictionary<string, List<EventManager>> AllEventManagerMap
        {
            get
            {
                var ret = new Dictionary<string, List<EventManager>>();
                lock (mEventManagers)
                {
                    foreach (var entry in mEventManagers)
                    {
                        List<EventManager> list;
                        if (!ret.TryGetValue(entry.Value.Name, out list))
                        {
                            list = new List<EventManager>();
                            ret.Add(entry.Value.Name, list);
                        }

                        list.Add(entry.Value);
                    }
                }

                return ret;
            }
        }

        public IEnumerable<EventManager> AllEventManager
        {
            get
            {
                lock (mEventManagers)
                {
                    return mEventManagers.Values;
                }
            }
        }


        public ICollection<EventManager> GetEventManageByName(string name)
        {
            lock (mEventManagers)
            {
                return (from m in mEventManagers where m.Value.Name == name select m.Value).ToArray();
            }
        }

        public EventManager CreateEventManager(string type, string id)
        {
            CreateManagerHandler fn;
            lock (mNames)
            {
                fn = mNames.Get(type);
            }

            var mgr = fn?.Invoke(type, id);
            if (mgr != null)
            {
                lock (mEventManagers)
                {
                    mEventManagers.Add(mgr.Address, mgr);
                }
            }

            return mgr;
        }

        internal void RemoveEventManager(string address)
        {
            lock (mEventManagers)
            {
                mEventManagers.Remove(address);
            }
        }

        public void RegisterName(string mgrName, CreateManagerHandler handler)
        {
            lock (mNames)
            {
                mNames.Add(mgrName, handler);
            }
        }


        //todo 是不是移入MessageBroker中更合适
        public virtual void BroadcastMessage(string managerName, EventMessage msg, UnionValue config, EventManager sender)
        {
            var all = GetEventManageByName(managerName);
            foreach (var mgr in all)
            {
                var uuid = config["UUID"];
                if (uuid.IsArray)
                {
                    if (uuid.ContainsValue(mgr.UUID))
                    {
                        mgr.OnReceiveMessage(msg);
                    }
                }
                else
                {
                    mgr.OnReceiveMessage(msg);
                }
            }
        }
    }
}