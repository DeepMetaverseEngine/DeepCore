using DeepCore.EventTrigger.Data;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static DeepCore.AbstractCollectionPool;

namespace DeepCore.EventTrigger
{
    public abstract class EventBehaviorRuntime : Recyclable
    {
        protected EventBehaviorData data;
        protected readonly HashMap<string, EventBehaviorNode> nodeMap = new HashMap<string, EventBehaviorNode>();
        public EventBehaviorData Data { get => data; }
        public IEnumerable<EventBehaviorNode> Nodes => nodeMap.Values;
        protected virtual EventBehaviorRuntime Init(EventBehaviorData data)
        {
            this.data = data;
            foreach (var node in data.Nodes)
            {
                if (node?.EventData != null)
                {
                    nodeMap.Add(node.GUID, node);
                }
            }
            return this;
        }
        protected override void Disposing()
        {
            nodeMap.Clear();
        }
        public bool TryGetNode(string nodeGUID, out EventBehaviorNode node)
        {
            if (nodeGUID == null)
            {
                node = null;
                return false;
            }
            return nodeMap.TryGetValue(nodeGUID, out node);
        }


        public delegate void SetNodeAction(LinkOption link, EventBehaviorNode next);
        public delegate void SetNodeAction<T>(LinkOption link, T next) where T : EventBehaviorNode;
        public delegate void SetMonoNodeAction<T>(LinkOption link, T next, int index) where T : EventBehaviorNode;

        public void GetOptionLinkNodes(List<LinkOption> options, in string ownerFieldName, SetNodeAction action)
        {
            //if (options.TryFind(ch => ch.OwnerFieldName == ownerFieldName, out link))
            if (options == null) return;
            foreach (var link in options)
            {
                if (link.OwnerFieldName == ownerFieldName && TryGetNode(link.NextGUID, out var next))
                {
                    action(link, next);
                }
            }
        }
        public void GetOptionLinkNodes<T>(List<LinkOption> options, in string ownerFieldName, SetNodeAction<T> action) where T : EventBehaviorNode
        {
            //if (options.TryFind(ch => ch.OwnerFieldName == ownerFieldName, out link))
            if (options == null) return;
            foreach (var link in options)
            {
                if (link.OwnerFieldName == ownerFieldName && TryGetNode(link.NextGUID, out var next) && next is T tnode)
                {
                    action(link, tnode);
                }
            }
        }
        public void GetMonoOptionLinkNodes<T>(List<LinkOption> options, in string ownerFieldName, SetMonoNodeAction<T> action) where T : EventBehaviorNode
        {
            //if (options.TryFind(ch => ch.OwnerFieldName == ownerFieldName, out link))
            if (options == null) return;
            foreach (var link in options)
            {
                if (EventBehaviorNode.TryParseMonoField(link.OwnerFieldName, out var name, out var findex))
                {
                    if (name == ownerFieldName && TryGetNode(link.NextGUID, out var next) && next is T tnode)
                    {
                        action(link, tnode, findex);
                    }
                }
                else if (link.OwnerFieldName == ownerFieldName)
                {
                    if (TryGetNode(link.NextGUID, out var next) && next is T tnode)
                    {
                        action(link, tnode, -1);
                    }
                }
            }
        }

    }
    //------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 行为树执行器
    /// </summary>
    public class EventBehaviorExecutor : EventBehaviorRuntime
    {
        private AbstractCollectionPool pool;
        private readonly List<EventLocalVar> localVars = new();
        private readonly List<ExecutorBehaviorTrigger> triggers = new();
        private readonly List<ExecutorBehaviorAction> actions = new();
        private readonly HashMap<AbstractTrigger, AutoReleaseList<TriggingHandler>> triggingHandlers = new();
        private readonly HashMap<AbstractAction, AutoReleaseList<TriggingHandler>> actionDones = new();
        private readonly HashMap<AbstractAction, object> actionReturns = new();
        private readonly HashMap<string, ExecutorNode> executorNodes = new();

        public EventBehaviorExecutor InitExecutor(AbstractCollectionPool pool, EventBehaviorData data)
        {
            this.pool = pool;
            base.Init(data);
            foreach (var node in base.Nodes)
            {
                var enode = default(ExecutorNode);
                if (node is EventBehaviorTrigger trigger)
                {
                    enode = pool.Alloc<ExecutorBehaviorTrigger>().Init(trigger);
                    triggers.Add(enode as ExecutorBehaviorTrigger);
                }
                else if (node is EventBehaviorAction action)
                {
                    enode = pool.Alloc<ExecutorBehaviorAction>().Init(action);
                    actions.Add(enode as ExecutorBehaviorAction);
                }
                if (enode != null)
                {
                    executorNodes.Add(node.GUID, enode);
                }
            }
            foreach (var enode in executorNodes.Values)
            {
                enode.Begin(this);
            }
            foreach (var node in data.Nodes)
            {
                var nodeData = node?.EventData;
                if (nodeData is EventLocalVar v)
                {
                    localVars.Add(v);
                }
            }
            return this;
        }
        public void Dispose(EventExecutor exe)
        {
            if (IsDisposing) return;
            foreach (var t in triggers)
            {
                t.BNode.Data.InvokeDispose(exe);
            }
            foreach (var a in actions)
            {
                a.BNode.Data.InvokeDispose(exe);
            }
            this.Dispose();
        }
        protected override void Disposing()
        {
            foreach (var t in triggingHandlers)
            {
                t.Value.Dispose();
            }
            this.triggingHandlers.Clear();
            foreach (var t in actionDones)
            {
                t.Value.Dispose();
            }
            this.actionDones.Clear();
            this.actionReturns.Clear();
            this.triggers.Clear();
            this.actions.Clear();
            this.localVars.Clear();
            foreach (var enode in executorNodes.Values)
            {
                enode.Dispose();
            }
            this.executorNodes.Clear();
            base.Disposing();
        }

//         internal void RefreshData(EventBehaviorData data)
//         {
//             if (data.Nodes != null)
//             {
//                 foreach (var bnode in data.Nodes)
//                 {
//                     if (executorNodes.TryGetValue(bnode.GUID, out var enode))
//                     {
//                         enode.RefreshData(this, bnode);
//                     }
//                 }
//             }
//         }
        public IEnumerable<string> TracingNodes
        {
            get
            {
                return executorNodes.Where(e => e.Value.DebugTraced).Select(t => t.Value.BNode.GUID);
            }
        }
        internal void BeginTrace()
        {
            foreach (var node in executorNodes)
            {
                node.Value.DebugTraced = false;
            }
        }
        internal void Trace(EventExternalizable msg)
        {
            if (msg.OwnerNode is EventBehaviorNode node)
            {
                if (executorNodes.TryGetValue(node.GUID, out var enode))
                {
                    enode.DebugTraced = true;
                }
            }
        }
        internal void Start(EventExecutor exe)
        {
            foreach (var klv in localVars)
            {
                using (var args = exe.API.AllocEventArguments(exe, null, this))
                {
                    var obj = klv.GetLocalVar(exe, args);
                    exe.SetLocalVar(klv.Key, obj);
                }
            }
            foreach (var t in triggers)
            {
                if (t.HasEntry == false)
                {
                    using (var args = exe.API.AllocEventArguments(exe, t.BNode.Data, this))
                    {
                        t.BNode.Data.StartListen(exe, args);
                    }
                }
            }
        }
        internal void AddTriggerCall(AbstractTrigger trigger, TriggingHandler handler)
        {
            if (!triggingHandlers.TryGetValue(trigger, out var list))
            {
                list = pool.AllocList<TriggingHandler>();
                triggingHandlers.Add(trigger, list);
            }
            list.Add(handler);
        }
        internal void InvokeTrigging(EventExecutor api, IEventArguments args, AbstractTrigger trigger)
        {
            if (triggingHandlers.TryGetValue(trigger, out var handler))
            {
                trigger.InvokeTrigging(api, args, handler);
            }
        }
        internal void AddActionDone(AbstractAction action, TriggingHandler handler)
        {
            if (!actionDones.TryGetValue(action, out var list))
            {
                list = pool.AllocList<TriggingHandler>();
                actionDones.Add(action, list);
            }
            list.Add(handler);
        }
        internal object InvokeActionDone(EventExecutor api, IEventArguments args, AbstractAction action)
        {
            this.actionReturns.Put(action, args.ReturnValue);
            if (actionDones.TryGetValue(action, out var handler))
            {
                for (int i = 0; i < handler.Count; i++)
                {
                    handler[i]?.Invoke(api, args);
                }
            }
            return null;
        }
        internal object GetActionReturn(AbstractAction action)
        {
            return actionDones.Get(action);
        }
    }
    //------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 用于重组事件节点，初始化行为树数据
    /// </summary>
    public class EventBehaviorAssembly : EventBehaviorRuntime
    {
        private readonly List<EventBehaviorLocalVar> localVars = new();
        private readonly List<EventBehaviorTrigger> triggers = new();
        public IReadOnlyList<EventBehaviorLocalVar> LocalVars => localVars;
        public IReadOnlyList<EventBehaviorTrigger> Triggers => triggers;
        public new EventBehaviorAssembly Init(EventBehaviorData data)
        {
            base.Init(data);
            foreach (var node in this.Nodes)
            {
                node.Init(this);
            }
            foreach (var node in this.Nodes)
            {
                node.Bind(this);
            }
            foreach (var node in data.Nodes)
            {
                if (node is EventBehaviorLocalVar v && v.VAR != null)
                {
                    localVars.Add(v);
                }
                else if (node is EventBehaviorTrigger t && t.Trigger != null)
                {
                    triggers.Add(t);
                }
            }
            foreach (var node in this.Nodes)
            {
                node.InitEnd(this);
            }
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            triggers.Clear();
            localVars.Clear();
            nodeMap.Clear();
            data = null;
        }

    }


    //------------------------------------------------------------------------------------------------------------------------------

    public abstract class ExecutorNode : Recyclable
    {
        public EventBehaviorNode BNode { get; private set; }
        public bool DebugTraced { get; set; } = false;
        public ExecutorNode Init(EventBehaviorNode bNode)
        {
            this.DebugTraced = false;
            this.BNode = bNode;
            return this;
        }
        protected override void Disposing()
        {
            BNode = null;

        }
        public abstract void Begin(EventBehaviorExecutor exe);
//         public virtual void RefreshData(EventBehaviorExecutor exe, EventBehaviorNode bNode)
//         {
//             // TODO 无法恢复Fields
//         }
    }
    public abstract class ExecutorNode<T> : ExecutorNode where T : EventBehaviorNode
    {
        new public T BNode => base.BNode as T;
        public ExecutorNode<T> Init(T bNode)
        {
            base.Init(bNode);
            return this;
        }
    }
    public class ExecutorBehaviorTrigger : ExecutorNode<EventBehaviorTrigger>
    {
        public bool HasEntry { get; private set; } = false;
        protected override void Disposing()
        {
            base.Disposing();
            this.HasEntry = false;
        }
        public override void Begin(EventBehaviorExecutor exe)
        {
            exe.GetOptionLinkNodes(BNode.Inputs, EventBehaviorTrigger.KEY_ENTRY, (linkEntry, entry) =>
            {
                if (entry is EventBehaviorAction)
                {
                    this.HasEntry = true;
                }
                else if (entry is EventBehaviorTrigger)
                {
                    this.HasEntry = true;
                }
            });
            if (BNode.CALL.Count > 0)
            {
                for (int i = 0; i < BNode.CALL.Count; i++)
                {
                    var call = BNode.CALL[i];
                    if (call is EventBehaviorAction callAction)
                    {
                        if (callAction.Action != null)
                        {
                            exe.AddTriggerCall(BNode.Trigger, callAction.Action.Invoke);
                        }
                    }
                    else if (call is EventBehaviorTrigger callTrigger)
                    {
                        if (callTrigger.Trigger != null)
                        {
                            exe.AddTriggerCall(BNode.Trigger, callTrigger.Trigger.StartListen);
                        }
                    }
                }
            }
        }
    }

    public class ExecutorBehaviorAction : ExecutorNode<EventBehaviorAction>
    {
        protected override void Disposing()
        {
            base.Disposing();
        }
        public override void Begin(EventBehaviorExecutor exe)
        {
            //Action.OnDone += OnDone;
            if (BNode.NEXT.Count > 0)
            {
                for (int i = 0; i < BNode.NEXT.Count; i++)
                {
                    var next = BNode.NEXT[i];
                    if (next is EventBehaviorAction nextAction)
                    {
                        if (nextAction.Action != null)
                        {
                            exe.AddActionDone(BNode.Action, nextAction.Action.Invoke);
                        }
                    }
                    else if (next is EventBehaviorTrigger nextTrigger)
                    {
                        if (nextTrigger.Trigger != null)
                        {
                            exe.AddActionDone(BNode.Action, nextTrigger.Trigger.StartListen);
                        }
                    }
                }
            }

        }

    }
}
