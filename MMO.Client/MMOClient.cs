using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using Gate.Client.Modules;

namespace Gate.Client
{
    public partial class MMOClient : GateClient
    {
        public static TemplateManager Templates => TemplateManager.Instance;
        public static EditorTemplates DataRoot => EditorTemplates.Instance;
        protected override void OnInitModules()
        {
            base.OnInitModules();
            this.AreaModule = AddModule(new AreaModule(this));
        }
        public AreaModule AreaModule { get; protected set; }
    }

    public abstract class MMOClientModule<C> : GateClientModule<C> where C : MMOClient
    {
        public static TemplateManager Templates => TemplateManager.Instance;
        public static EditorTemplates DataRoot => EditorTemplates.Instance;
        protected MMOClientModule(C client) : base(client)
        {
        }
    }
}
