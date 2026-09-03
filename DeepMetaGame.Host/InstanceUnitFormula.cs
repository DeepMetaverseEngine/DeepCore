using DeepCore.Game3D.Host.FuncData;

namespace DeepCore.Game3D.Host.Instance
{
    public class InstanceUnitFormula : Recyclable
    {
        public static T Alloc<T>(InstanceUnit owner) where T : InstanceUnitFormula, new()
        {
            return owner.ObjectPool.Alloc<T>().Init(owner) as T;
        }
        public InstanceUnit Owner { get; private set; }
        public virtual InstanceUnitFormula Init(InstanceUnit owner)
        {
            this.Owner = owner;
            return this;
        }
        protected override void Disposing()
        {
            this.Owner = null;
        }

        protected virtual internal void OnInit() { }
        protected virtual internal void OnLatedInit()
        {
        }
    }
}
