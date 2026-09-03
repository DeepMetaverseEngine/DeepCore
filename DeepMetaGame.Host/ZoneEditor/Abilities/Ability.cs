using DeepMetaGame.Data.ZoneEditor;


namespace DeepCore.Game3D.Host.Instance.Abilities
{

    public abstract class Ability : Disposable
    {
        public InstanceZone Zone { get; private set; }
        public EditorAbilityData Data { get; private set; }
        public string Name { get; private set; }
        public Ability(InstanceZone zone, EditorAbilityData data)
        {
            this.Zone = zone;
            this.Data = data;
            this.Name = data.Name;
        }
        internal void Start(InstanceAttributes obj)
        {
            this.OnStart(obj);
        }
        protected virtual void OnStart(InstanceAttributes obj)
        {
        }
        protected override void Disposing()
        {
        }
    }

}
