using DeepCore.AI.LLM;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Debug;
using DeepCore.Game3D.Host.FuncData;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.Instance.Abilities;
using DeepCore.GameData.EventTrigger;
using DeepMetaGame.Data;
using DeepMetaGame.Data.FuncData;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using static DeepMetaGame.Data.ZoneEditor.SceneData;


namespace DeepCore.Game3D.Host.ZoneEditor.EventTrigger
{
    #region BASE
    //---------------------------------------------------------------------------------
    /// <summary>
    /// 执行事件触发器的数据接口
    /// </summary>
    public abstract class HostEventTriggerAdapter : IEventTriggerAdapter, IDisposable
    {
        new public HostEventTriggerAdapter Init(ZoneEventTriggerAdapterAPI api, IEventDataNode evt, IEventExecutorCollection group, IEventRuntime runtime)
        {
            base.Init(api, evt, group, runtime);
            return this;
        }
        public override void Invoke(Action action)
        {
            ZoneAPI.QueueTask(action, static (z, action) => action.Invoke());
        }
        public override void Invoke<T>(T t, Action<T> action)
        {
            ZoneAPI.QueueTask((t, action), static (z, t) => t.action.Invoke(t.t));
        }
    }

    //---------------------------------------------------------------------------------
    public abstract class HostEventExecutorCollection<T> : Recyclable, IEventExecutorCollection where T : TemplateData
    {
        protected readonly HashMap<string, IEventTriggerAdapter> mEvents = new HashMap<string, IEventTriggerAdapter>();
        protected readonly ArrayList<IEventTriggerAdapter> mEventsArray = new ArrayList<IEventTriggerAdapter>();
        private bool started = false;
        public T Data { get; private set; }
        public string GUID { get; private set; }
        protected HostEventExecutorCollection<T> Init(T data)
        {
            this.Data = data;
            this.GUID = System.Guid.NewGuid().ToString();
            return this;
        }
        protected override void Disposing()
        {
            Stop();
        }
        public abstract EditorScene Zone { get; }
        public abstract string Name { get; }
        public abstract IEnumerable<IEventDataNode> DataNodes { get; }
        protected abstract IEventTriggerAdapter CreateAdapter(IEventDataNode e);
        public int TemplateID { get => Data.ID; }
        public Type TemplateType { get => Data.GetType(); }
        public void Start()
        {
            if (!started)
            {
                started = true;
                foreach (IEventDataNode e in DataNodes)
                {
                    if (e != null)
                    {
                        var trigger = CreateAdapter(e);
                        mEvents.Put(e.EventName, trigger);
                        mEventsArray.Add(trigger);
                    }
                }
                Zone.cb_BindEvents(this);
                foreach (var trigger in mEventsArray)
                {
                    trigger.Start();
                }
            }
        }
        private void Stop()
        {
            if (started)
            {
                started = false;
                foreach (var trigger in mEventsArray)
                {
                    trigger.Stop();
                }
                Zone.cb_DisposeEvents(this);
                foreach (var trigger in mEventsArray)
                {
                    trigger.Dispose();
                }
                mEventsArray.Clear();
                mEvents.Clear();
            }
        }
        public virtual void RefreshData(T data)
        {
//                    this.Data = data;
//                    if (started)
//                    {
//                        foreach (IEventDataNode e in DataNodes)
//                        {
//                            if (e != null && mEvents.TryGetValue(e.EventName, out var trigger))
//                            {
//                                trigger.RefreshData(e);
//                            }
//                        }
//                    }
            if (started)
            {
                if (this.Data != data)
                {
                    // 如果本地用的原始模板
                    if (this.Data.IsOriginal)
                    {
                        // 重启行为树
                        Stop();
                        this.Data = data;
                        Start();
                    }
                    else
                    {
                        // 复制给本地模板数据
                        using (var cards = Zone.ObjectPool.AllocList<CardTemplate>())
                        {
                            Zone.Templates.TryGetUsageCards(data, cards);
                            CardManager.CopyData(data, this.Data, cards);
                        }
                    }
                }
                else
                {
                    // 模板相同，不做任何处理                 
                }
            }
            else
            {
                this.Data = data;
            }
        }
        public void ForEachEvents(Action<EventExecutor> act)
        {
            foreach (var trigger in mEventsArray)
            {
                act(trigger);
            }
        }
        public EventExecutor GetEditEvent(string name)
        {
            if (name == null) { return null; }
            return mEvents.Get(name);
        }
        public void EventActive(string name)
        {
            var apt = this.GetEditEvent(name);
            if (apt != null)
            {
                apt.IsActive = true;
            }
        }
        public void EventDeactive(string name)
        {
            var apt = this.GetEditEvent(name);
            if (apt != null)
            {
                apt.IsActive = false;
            }
        }
        public IEnumerator<EventExecutor> GetEnumerator() => mEventsArray.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mEventsArray.GetEnumerator();
    }

    #endregion
    //---------------------------------------------------------------------------------
    /// <summary>
    /// 场景事件
    /// </summary>
    public class ZoneEventTriggerCollection : HostEventExecutorCollection<SceneData>
    {
        private EditorScene mScene;
        private readonly List<IEventDataNode> mDataNodes = new List<IEventDataNode>();
        public ZoneEventTriggerCollection Init(EditorScene scene)
        {
            this.mScene = scene;
            base.Init(scene.Data);
            if (Data.Host.Events != null) mDataNodes.AddRange(Data.Host.Events);
            if (Data.Events != null)
            {
                foreach (var ed in Data.Events)
                {
                    var ue = scene.Templates.GetUnitEvent(ed);
                    if (ue != null)
                    {
                        mDataNodes.AddRange(ue.Events);
                    }
                }
            }
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.mScene = null;
            this.mDataNodes.Clear();
        }
        public override EditorScene Zone => mScene;
        public override string Name { get => $"Zone/{mScene.Data}/{nameof(SceneHostData.Events)}"; }
        public override IEnumerable<IEventDataNode> DataNodes => mDataNodes;
        protected override IEventTriggerAdapter CreateAdapter(IEventDataNode e)
        {
            return mScene.ObjectPool.Alloc<ZoneEventTrigger>().Init(mScene, e, this);
        }
        public class ZoneEventTrigger : HostEventTriggerAdapter
        {
            private EditorScene mScene;
            private ZoneEventTriggerAdapterAPI api;
            public ZoneEventTrigger Init(EditorScene scene, IEventDataNode evt, ZoneEventTriggerCollection group)
            {
                this.api = scene.ObjectPool.Alloc<ZoneEventTriggerAdapterAPI>().Init(scene);
                this.mScene = scene;
                base.Init(api, evt, group, scene);
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                api?.Dispose();
                api = null;
                mScene = null;
            }
        }
    }
    //---------------------------------------------------------------------------------
    /// <summary>
    /// 单位事件
    /// </summary>
    public class UnitEventTriggerCollection : HostEventExecutorCollection<UnitEventTemplate>
    {
        protected InstanceUnit mUnit;
        public UnitEventTriggerCollection Init(InstanceUnit unit, UnitEventTemplate id)
        {
            base.Init(id);
            this.mUnit = unit;
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.mUnit = null;
        }
        public override EditorScene Zone => mUnit.Parent;
        public override string Name { get => $"Unit/{mUnit.Info}({mUnit.ID})/{base.Data}"; }
        public override IEnumerable<IEventDataNode> DataNodes => Data.Events;
        protected override IEventTriggerAdapter CreateAdapter(IEventDataNode e)
        {
            return mUnit.ObjectPool.Alloc<UnitEventTrigger>().Init(mUnit, e, this);
        }
        public class UnitEventTrigger : HostEventTriggerAdapter
        {
            private ZoneEventTriggerAdapterAPI api;
            private InstanceZone mScene;
            private InstanceUnit mUnit;
            public UnitEventTrigger Init(InstanceUnit unit, IEventDataNode evt, UnitEventTriggerCollection group)
            {
                this.api = unit.ObjectPool.Alloc<ZoneEventTriggerAdapterAPI>().Init(unit);
                this.mUnit = unit;
                this.mScene = unit.Parent;
                base.Init(api, evt, group, unit.Zone);
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.api?.Dispose();
                this.api = null;
                this.mUnit = null;
                this.mScene = null;
            }
        }
    }
    //---------------------------------------------------------------------------------
    /// <summary>
    /// UI 事件
    /// </summary>
    public class GUIEventTriggerCollection : HostEventExecutorCollection<BattleUITemplate>
    {
        protected InstanceZone.HostGUIForm mForm;
        public GUIEventTriggerCollection Init(InstanceZone.HostGUIForm form)
        {
            this.mForm = form;
            base.Init(form.Info);
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            mForm = null;
        }
        public override EditorScene Zone => mForm.Zone as EditorScene;
        public override string Name { get => $"[游戏]/GUI/{mForm.Info}({mForm.Name})/{nameof(BattleUITemplate.Events)}"; }
        public override IEnumerable<IEventDataNode> DataNodes => Data.Events;
        protected override IEventTriggerAdapter CreateAdapter(IEventDataNode e)
        {
            return mForm.Zone.ObjectPool.Alloc<GUIEventTrigger>().Init(mForm, e, this);
        }
        public class GUIEventTrigger : HostEventTriggerAdapter
        {
            private ZoneEventTriggerAdapterAPI api;
            public GUIEventTrigger Init(InstanceZone.HostGUIForm form, IEventDataNode evt, GUIEventTriggerCollection group)
            {
                this.api = form.Zone.ObjectPool.Alloc<ZoneEventTriggerAdapterAPI>().Init(form);
                base.Init(api, evt, group, form.Zone);
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.api?.Dispose();
                this.api = null;
            }
        }
    }
    //---------------------------------------------------------------------------------
    /// <summary>
    /// 单位，技能，法术，BUFF，Card，模板里带的事件
    /// </summary>
    public class CustomUnitEventTriggerCollection : HostEventExecutorCollection<CustomEventTemplateData>
    {
        protected InstanceUnit mUnit;
        public override EditorScene Zone => mUnit.Parent;
        public override string Name { get => $"CustomUnit/{mUnit.Info}({mUnit.ID})/{base.Data}"; }
        public override IEnumerable<IEventDataNode> DataNodes { get => base.Data.CustomEvents; }
        public CustomUnitEventTriggerCollection Init(InstanceUnit unit, CustomEventTemplateData id)
        {
            this.mUnit = unit;
            base.Init(id);
            return this;
        }
        protected override IEventTriggerAdapter CreateAdapter(IEventDataNode e)
        {
            return mUnit.ObjectPool.Alloc<UnitEventTrigger>().Init(mUnit, e, this);
        }
        public class UnitEventTrigger : HostEventTriggerAdapter
        {
            private ZoneEventTriggerAdapterAPI api;
            private InstanceZone mScene;
            private InstanceUnit mUnit;
            public UnitEventTrigger Init(InstanceUnit unit, IEventDataNode evt, CustomUnitEventTriggerCollection group)
            {
                this.api = unit.ObjectPool.Alloc<ZoneEventTriggerAdapterAPI>().Init(unit);
                this.mUnit = unit;
                this.mScene = unit.Parent;
                base.Init(api, evt, group, unit.Zone);
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.api?.Dispose();
                this.api = null;
                this.mUnit = null;
                this.mScene = null;
            }
        }
    }
    //---------------------------------------------------------------------------------

}
