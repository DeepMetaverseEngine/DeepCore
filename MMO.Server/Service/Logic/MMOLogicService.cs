using DeepCore;
using DeepCore.Protocol;
using DeepCore.Statistics;
using DeepCrystal.ORM;
using DeepCrystal.ORM.Generic;
using DeepCrystal.RPC;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using Gate.Data;
using Gate.Data.Protocol;
using Gate.Server.Protocol;
using Gate.Server.Service.Logic.Module;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Gate.Server.Service.Logic
{
    public partial class MMOLogicService : LogicService
    {
        public static TemplateManager Templates => TemplateManager.Instance;
        public static EditorTemplates DataRoot => EditorTemplates.Instance;
        public MMOLogicService(ServiceStartInfo start) : base(start)
        {
        }
        public AreaModule AreaModule { get; protected set; }
        protected override void OnInitModules()
        {
            base.OnInitModules();
            this.AreaModule = new AreaModule(this);
        }
        protected override void OnClearModules()
        {
            base.OnClearModules();
            this.AreaModule = null;
        }
    }

    public abstract class MMOLogicModule<L> : IServiceModule<L> where L : MMOLogicService
    {
        public static TemplateManager Templates => TemplateManager.Instance;
        public static EditorTemplates DataRoot => EditorTemplates.Instance;
        protected MMOLogicModule(L service) : base(service)
        {
        }
    }
}
