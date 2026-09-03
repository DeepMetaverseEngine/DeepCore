using DeepCore.Components;
using DeepCore.Game3D.Host.Instance.Components;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceZone
    {
        private ZoneComponentCollection _components;
        public ZoneComponentCollection Components
        {
            get
            {
                if (_components == null)
                {
                    _components = new (this, static (a, b) => a.Priority - b.Priority);
                }
                return _components;
            }
        }
        private void UpdateComponents(float intervalMS)
        {
            _components?.ForEach(intervalMS, static (st, c) => c.InternalUpdate());
        }

    }
}
