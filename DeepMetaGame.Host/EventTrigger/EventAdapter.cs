using DeepCore.AI.LLM;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.EventTrigger.Debug;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepMetaGame.Data.FuncData;
using DeepMetaGame.Data.GUI;
using DeepMetaGame.Data.Template;
using System;
using static DeepCore.Game3D.Host.Instance.InstanceZone;

namespace DeepCore.GameData.EventTrigger
{
    public abstract class IEventTriggerAdapter : DeepCore.EventTrigger.EventExecutor
    {
        private ZoneEventTriggerAdapterAPI api;
        public ZoneEventTriggerAdapterAPI ZoneEventAPI { get => api; }
        public override IEventAPI API { get => api; }
        public EditorScene ZoneAPI { get => ZoneEventAPI.Zone; }
        public InstanceUnit UnitAPI { get => ZoneEventAPI.Unit; }
        public HostGUIForm FormAPI { get => ZoneEventAPI.Form; }
        public TemplateManager Templates { get { return ZoneEventAPI.Templates; } }
        public virtual IEventTriggerAdapter Init(ZoneEventTriggerAdapterAPI api, IEventDataNode evt, IEventExecutorCollection group, IEventRuntime runtime)
        {
            this.api = api;
            base.Init(ZoneValueTypeNameSpace.Instance, evt, group, runtime);
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.api = null;
        }
    }

    //---------------------------------------------------------------------------------
    /// <summary>
    /// 绑定环境变量用的数据接口
    /// </summary>
    public class BindValuesExecutor : IEventTriggerAdapter
    {
        private ZoneEventTriggerAdapterAPI api;
        public BindValuesExecutor Init(EditorScene scene, IEventExecutorCollection group)
        {
            this.api = scene.ObjectPool.Alloc<ZoneEventTriggerAdapterAPI>().Init(scene);
            base.Init(api, null, group, scene);
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.api?.Dispose();
            this.api = null;
        }
        public override void Invoke(Action action)
        {
        }
        public override void Invoke<T>(T t, Action<T> action)
        {
        }
    }

    //---------------------------------------------------------------------------------
}
