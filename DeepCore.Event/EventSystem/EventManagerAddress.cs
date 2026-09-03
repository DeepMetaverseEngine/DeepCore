using System;

namespace DeepCore.Event.EventSystem
{
    public class EventManagerAddress
    {
        public readonly string Name;
        public readonly string UUID;
        public readonly string Address;

        public static EventManagerAddress Parse(string address)
        {
            var all = address.Split(new[] { EventManager.AddressSeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            if (all.Length == 0)
            {
                return null;
            }
            if (all.Length > 1)
            {
                return new EventManagerAddress(all[0], all[1]);
            }
            return new EventManagerAddress(all[0], null);
        }

        public EventManagerAddress(string name, string uuid)
        {
            Name = name;
            UUID = uuid;
            Address = name;
            if (!string.IsNullOrEmpty(uuid))
            {
                Address = Address + EventManager.AddressSeparatorChar + uuid;
            }
        }
    }

}
