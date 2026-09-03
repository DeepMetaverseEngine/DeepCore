using DeepCore;
using DeepCore.EventTrigger;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Data.ZoneEditor
{

    // ----------------------------------------------------------------------

    abstract public class EventDataNode : ICloneable, IEventDataNode, IExternalizable, IEventData
    {
        //----------------------------------------------------------------------
        // runtime 
        //----------------------------------------------------------------------
        public string EventName { get { return Name; } set { Name = value; } }
        public string EventTreePath { get { return SavePath; } set { SavePath = value; } }
        public string EditorPath { get { return SavePath; } set { SavePath = value; } }
        public bool EventIsActive { get { return Active; } set { Active = value; } }
        public string EventComment { get { return Comment; } set { Comment = value; } }
        public List<DeepCore.EventTrigger.Data.EventLocalVar> EventLocalVars => LocalVars.Datas;
        public List<DeepCore.EventTrigger.Data.AbstractTrigger> EventTriggers => Triggers.Datas;
        public List<DeepCore.EventTrigger.Data.AbstractCondition> EventConditions => Conditions.Datas;
        public List<DeepCore.EventTrigger.Data.AbstractAction> EventActions => Actions.Datas;
        public EventBehaviorData EventBehavior => Behavior;

        private EventBehaviorData runtimeBehavior;
        public EventBehaviorData GetRuntimeBehavior()
        {
            if (runtimeBehavior == null && Behavior != null)
            {
                runtimeBehavior = Behavior;
                new EventBehaviorAssembly().Init(runtimeBehavior);
            }
            return runtimeBehavior;
        }
        //----------------------------------------------------------------------
        public string Name = "Unnamed";
        public bool Active = true;
        public string Comment = "";
        public string SavePath;
        public EventLocalVars LocalVars = new EventLocalVars();
        public EventTriggers Triggers = new EventTriggers();
        public EventConditions Conditions = new EventConditions();
        public EventActions Actions = new EventActions();
        public EventBehaviorData Behavior = new EventBehaviorData();

        public EventDataNode()
        {
        }

        sealed public override string ToString()
        {
            return $"{Name}";
        }

        public object Clone()
        {
            return ZoneDataFactory.Factory.PersistCodec.Clone(this);
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(Name);
            output.PutBool(Active);
            output.PutUTF(Comment);
            output.PutUTF(SavePath);
            output.PutS32(0);

            output.PutExt(LocalVars);
            output.PutExt(Triggers);
            output.PutExt(Conditions);
            output.PutExt(Actions);
            output.PutExt(Behavior);

        }

        public void ReadExternal(IInputStream input)
        {
            Name = input.GetUTF();
            Active = input.GetBool();
            Comment = input.GetUTF();
            SavePath = input.GetUTF();
            input.GetS32();

            LocalVars = input.GetExt<EventLocalVars>();
            Triggers = input.GetExt<EventTriggers>();
            Conditions = input.GetExt<EventConditions>();
            Actions = input.GetExt<EventActions>();
            Behavior = input.GetExt<EventBehaviorData>();
        }

        public void CopyFrom(EventDataNode other)
        {
            this.Name = other.Name;
            this.Active = other.Active;
            this.Comment = other.Comment;
            this.SavePath = other.SavePath;
            this.LocalVars = other.LocalVars;
            this.Triggers = other.Triggers;
            this.Conditions = other.Conditions;
            this.Actions = other.Actions;
            this.Behavior = other.Behavior;
        }

    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// 事件动作触发器
    /// </summary>
    [MessageType(BattleConstants.ZoneEvent)]
    [TableClass("Name")]
    public class ZoneEvent : EventDataNode
    {
        public ZoneEvent() { }
        public ZoneEvent(EventDataNode other) { CopyFrom(other); }
    }
    // ----------------------------------------------------------------------
    /// 事件动作触发器
    /// </summary>
    [MessageType(BattleConstants.UnitEvent)]
    [TableClass("Name")]
    public class UnitEvent : EventDataNode
    {
        public UnitEvent() { }
        public UnitEvent(EventDataNode other) { CopyFrom(other); }
    }
    // ----------------------------------------------------------------------
    /// 事件动作触发器
    /// </summary>
    [MessageType(BattleConstants.GUIEvent)]
    [TableClass("Name")]
    public class GUIEvent : EventDataNode
    {
        public GUIEvent() { }
        public GUIEvent(EventDataNode other) { CopyFrom(other); }
    }
    // ----------------------------------------------------------------------
    /// 事件动作触发器
    /// </summary>
    [MessageType(BattleConstants.UnitCustomEvent)]
    [TableClass("Name")]
    public class UnitCustomEvent : EventDataNode
    {
        public UnitCustomEvent() { }
        public UnitCustomEvent(EventDataNode other) { CopyFrom(other); }
    }

    // ----------------------------------------------------------------------
    [MessageType(BattleConstants.EventLocalVars)]
    public class EventLocalVars : IExternalizable
    {
        public List<DeepCore.EventTrigger.Data.EventLocalVar> Datas = new List<DeepCore.EventTrigger.Data.EventLocalVar>();

        public void WriteExternal(IOutputStream output)
        {
            output.PutList(Datas, static (output, v) => output.PutExt(v));
        }
        public void ReadExternal(IInputStream input)
        {
            Datas = input.GetListAny<DeepCore.EventTrigger.Data.EventLocalVar>();
        }
    }
    [MessageType(BattleConstants.EventTriggers)]
    public class EventTriggers : IExternalizable
    {
        public List<DeepCore.EventTrigger.Data.AbstractTrigger> Datas = new List<DeepCore.EventTrigger.Data.AbstractTrigger>();
        public void WriteExternal(IOutputStream output)
        {
            output.PutList(Datas, static (output, v) => output.PutExt(v));
        }
        public void ReadExternal(IInputStream input)
        {
            Datas = input.GetListAny<DeepCore.EventTrigger.Data.AbstractTrigger>();
        }
    }
    [MessageType(BattleConstants.EventConditions)]
    public class EventConditions : IExternalizable
    {
        public List<DeepCore.EventTrigger.Data.AbstractCondition> Datas = new List<DeepCore.EventTrigger.Data.AbstractCondition>();
        public void WriteExternal(IOutputStream output)
        {
            output.PutList(Datas, static (output, v) => output.PutExt(v));
        }
        public void ReadExternal(IInputStream input)
        {
            Datas = input.GetListAny<DeepCore.EventTrigger.Data.AbstractCondition>();
        }
    }
    [MessageType(BattleConstants.EventActions)]
    public class EventActions : IExternalizable
    {
        public List<DeepCore.EventTrigger.Data.AbstractAction> Datas = new List<DeepCore.EventTrigger.Data.AbstractAction>();
        public void WriteExternal(IOutputStream output)
        {
            output.PutList(Datas, static (output, v) => output.PutExt(v));
        }
        public void ReadExternal(IInputStream input)
        {
            Datas = input.GetListAny<DeepCore.EventTrigger.Data.AbstractAction>();
        }
    }
}
