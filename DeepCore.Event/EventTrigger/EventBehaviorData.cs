using DeepCore.EventTrigger.Data;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace DeepCore.EventTrigger
{
    //------------------------------------------------------------------------------------------------------------------------------

    [MessageType(0x3509)]
    public class EventBehaviorData : IExternalizable
    {

        public List<EventBehaviorNode> Nodes = new List<EventBehaviorNode>();
        public void WriteExternal(IOutputStream output)
        {
            output.PutList(Nodes, static (output, v) => output.PutExt(v));
        }
        public void ReadExternal(IInputStream input)
        {
            Nodes = input.GetListAny<EventBehaviorNode>();
        }
        public void ReHash()
        {
            foreach (var node in Nodes)
            {
                var oldGUID = node.GUID;
                node.GUID = Guid.NewGuid().ToString();
                ReHashAllOptions(node.GUID, oldGUID);
            }
        }
        private void ReHashAllOptions(string newGUID, string oldGUID)
        {
            foreach (var node in Nodes)
            {
                node.ForEachOptions(0, (st, op, dock) =>
                {
                    if (op.NextGUID == oldGUID) op.NextGUID = newGUID;
                });
            }
        }
        public List<EventLocalVar> GetEventLocalVars()
        {
            var localVars = new List<EventLocalVar>();
            foreach (var node in Nodes)
            {
                var nodeData = node?.EventData;
                if (nodeData is EventLocalVar v)
                {
                    localVars.Add(v);
                }
            }
            return localVars;
        }

    }
    //------------------------------------------------------------------------------------------------------------------------------

    public class EventBehaviorDataCollection
    {
        public List<EventBehaviorNode> Nodes = new List<EventBehaviorNode>();
        public EventBehaviorData ToData()
        {
            return new EventBehaviorData()
            {
                Nodes = Nodes
            };
        }
        public void ForEachFields(EventBehaviorNode mainNode, Action<EventBehaviorNode, IDynamicFieldInfo, EventBehaviorNode> action)
        {
            //    var fieldValues = new HashMap<string, EventExternalizable>();
            EventBehaviorNode.ForEachEventDataValueFields(mainNode.EventData, (mainData, field) =>
            {
                if (mainNode.TryGetFieldConnection(field.Field.Name, out var connection))
                {
                    var fv = Nodes.Find(tn => tn.GUID == connection[0].NextGUID);
                    if (fv != null)
                    {
                        action(mainNode, field, fv);
                    }
                }
            });
        }

    }

    public class EventBehaviorDataConveter
    {
        public int StartX = 100;
        public int StartY = 100;
        public int BlockW = 200;
        public int BlockH = 80;

        private ListDictionary<EventExternalizable, EventBehaviorNode> NodeDatas = new ListDictionary<EventExternalizable, EventBehaviorNode>();

        public EventBehaviorDataConveter() { }

        public EventBehaviorDataCollection ConvertTo(EventExternalizable src, out EventBehaviorNode main)
        {
            src = XmlUtil.CloneObject(src);
            main = InitNode(src, 0);
            LinkNodes(src, StartX, StartY);
            return new EventBehaviorDataCollection()
            {
                Nodes = new List<EventBehaviorNode>(NodeDatas.Values.ToArray()),
            };
        }

        public EventBehaviorDataCollection ConvertTo(IEventDataNode src)
        {
            src = XmlUtil.CloneObject(src);

            foreach (var var in src.EventLocalVars)
            {
                InitNode(var, 0);
            }
            var MainAction = InitCondition(src);
            {
                InitNode(MainAction, 1);
                foreach (var trigger in src.EventTriggers)
                {
                    InitNode(trigger, 0);
                }
            }
            {
                foreach (var var in src.EventLocalVars)
                {
                    LinkNodes(var, StartX + BlockW + BlockW, StartY);
                    StartY += BlockH;
                }
            }
            {
                StartX += BlockW + BlockW;
                var Main = LinkNodes(MainAction, StartX, StartY) as EventBehaviorAction;
                foreach (var trigger in src.EventTriggers)
                {
                    var triggerNode = LinkNodes(trigger, StartX, StartY) as EventBehaviorTrigger;
                    if (Main != null)
                    {
                        triggerNode.AppendCall(Main);
                    }
                    StartY += BlockH;
                }
            }
            return new EventBehaviorDataCollection()
            {
                Nodes = new List<EventBehaviorNode>(NodeDatas.Values.ToArray()),
            };
        }
        private AbstractAction InitCondition(IEventDataNode src)
        {
            if (src.EventActions == null || src.EventActions.Count == 0)
            {
                return null;
            }
            if (src.EventConditions == null || src.EventConditions.Count == 0 || (src.EventConditions.Count == 1 && src.EventConditions[0] is AlwaysTrue))
            {
                if (src.EventActions.Count == 1)
                {
                    return src.EventActions[0];
                }
                else
                {
                    var queue = new DoActionQueue();
                    foreach (var action in src.EventActions)
                    {
                        queue.ActionQueue.Add(action);
                    }
                    return queue;
                }
            }
            else
            {
                var IF_THEN = new ConditionAction() { };
                // if conditions
                if (src.EventConditions.Count > 1)
                {
                    var conds = new BooleanValue.BooleanOperatorGroup()
                    {
                        Op = Formula.BooleanOP.AND
                    };
                    foreach (var codition in src.EventConditions)
                    {
                        if (codition is BooleanCondition bc)
                        {
                            conds.Cases.Add(bc.Value);
                        }
                        else
                        {
                            conds.Cases.Add(codition);
                        }
                    }
                    IF_THEN.Condition = conds;
                }
                else if (src.EventConditions.Count == 1)
                {
                    var codition = src.EventConditions[0];
                    if (codition is BooleanCondition bc)
                    {
                        IF_THEN.Condition = bc.Value;
                    }
                    else
                    {
                        IF_THEN.Condition = codition;
                    }
                }
                // then
                {
                    if (src.EventActions.Count == 1)
                    {
                        IF_THEN.Action = src.EventActions[0];
                    }
                    else
                    {
                        var queue = new DoActionQueue();
                        foreach (var action in src.EventActions)
                        {
                            queue.ActionQueue.Add(action);
                        }
                        IF_THEN.Action = queue;
                    }
                }
                return IF_THEN;
            }
        }

        private EventBehaviorNode InitNode(EventExternalizable nodeData, int depth)
        {
            if (nodeData != null)
            {
                var node = EventBehaviorNode.CreateNode(nodeData);
                node.GUID = Guid.NewGuid().ToString();
                node.Inputs = new List<LinkOption>();
                node.Outputs = new List<LinkOption>();
                var dtype = nodeData.GetTypeInfo();
                NodeDatas.Add(nodeData, node);
                foreach (var dfield in dtype.GetFields())
                {
                    if (typeof(EventExternalizable).IsAssignableFrom(dfield.MemberType))
                    {
                        InitNode(dfield.GetValue(nodeData) as EventExternalizable, depth + 1);
                    }
                    else if (EventBehaviorNode.IsMonoList(dfield.MemberType, out var memberType))
                    {
                        var list = dfield.GetValue(nodeData) as IList;
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (list[i] is EventExternalizable ve)
                            {
                                InitNode(ve, depth + 1);
                            }
                        }
                    }
                    else if (EventBehaviorNode.IsStereoList(dfield.MemberType, out memberType))
                    {
                        var list = dfield.GetValue(nodeData) as IList;
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (list[i] is IStereoOption stereo)
                            {
                                InitNode(stereo.Input as EventExternalizable, depth + 1);
                                InitNode(stereo.Output as EventExternalizable, depth + 1);
                            }
                        }
                    }
                }
                return node;
            }
            return null;
        }
        private EventBehaviorNode LinkNodes(EventExternalizable nodeData, int x, int y)
        {
            if (nodeData != null)
            {
                var node = NodeDatas.Get(nodeData);
                node.EditorX = x;
                node.EditorY = y;
                var dtype = nodeData.GetTypeInfo();
                int index = 0;
                foreach (var dfield in dtype.GetFields())
                {
                    if (typeof(EventExternalizable).IsAssignableFrom(dfield.MemberType))
                    {
                        var fieldData = dfield.GetValue(nodeData) as EventExternalizable;
                        if (fieldData is AbstractValue)
                        {
                            var fieldNode = LinkNodes(fieldData, x - BlockW, y + BlockH * index);
                            EventBehaviorNode.SetToField(node, dfield.Name, LinkDock.Input, fieldNode, EventBehaviorValue.KEY_OUT, LinkDock.Output);
                        }
                        else if (fieldData is AbstractAction)
                        {
                            var fieldNode = LinkNodes(fieldData, x + BlockW + BlockW, y + BlockH * index);
                            EventBehaviorNode.SetToField(node, dfield.Name, LinkDock.Output, fieldNode, EventBehaviorAction.KEY_ENTRY, LinkDock.Input);
                        }
                        else
                        {
                            var fieldNode = LinkNodes(fieldData, x - BlockW, y + BlockH * index);
                        }
                        dfield.SetValue(nodeData, null);
                    }
                    else if (EventBehaviorNode.IsMonoList(dfield.MemberType, out var memberType))
                    {
                        var list = dfield.GetValue(nodeData) as IList;
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (list[i] is EventExternalizable fieldData)
                            {
                                EventBehaviorNode.ToMonoMemberFieldName(dfield.Name, i, out var fname);
                                if (fieldData is AbstractValue)
                                {
                                    var fieldNode = LinkNodes(fieldData, x - BlockW, y + BlockH * index);
                                    EventBehaviorNode.SetToField(node, fname, LinkDock.Input, fieldNode, EventBehaviorValue.KEY_OUT, LinkDock.Output);
                                }
                                else if (fieldData is AbstractAction)
                                {
                                    var fieldNode = LinkNodes(fieldData, x + BlockW + BlockW, y + BlockH * index);
                                    EventBehaviorNode.SetToField(node, fname, LinkDock.Output, fieldNode, EventBehaviorAction.KEY_ENTRY, LinkDock.Input);
                                }
                                else
                                {
                                    var fieldNode = LinkNodes(fieldData, x - BlockW, y + BlockH * index);
                                }
                            }
                        }
                        list.Clear();
                    }
                    else if (EventBehaviorNode.IsStereoList(dfield.MemberType, out memberType))
                    {
                        var list = dfield.GetValue(nodeData) as IList;
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (list[i] is IStereoOption stereo)
                            {
                                EventBehaviorNode.ToStereoMemberFieldName(dfield.Name, stereo, i, out var inFieldName, out var outFieldName);
                                if (stereo.Input is AbstractValue inputData)
                                {
                                    var fieldNodeA = LinkNodes(inputData, x - BlockW, y + BlockH * index);
                                    EventBehaviorNode.SetToField(node, inFieldName, LinkDock.Input, fieldNodeA, EventBehaviorValue.KEY_OUT, LinkDock.Output);
                                }
                                if (stereo.Output is AbstractAction outputData)
                                {
                                    var fieldNodeB = LinkNodes(outputData, x + BlockW + BlockW, y + BlockH * index);
                                    EventBehaviorNode.SetToField(node, outFieldName, LinkDock.Output, fieldNodeB, EventBehaviorAction.KEY_ENTRY, LinkDock.Input);
                                }
                            }
                        }
                        list.Clear();
                    }
                    index++;
                }
                return node;
            }
            return null;
        }

    }
    //------------------------------------------------------------------------------------------------------------------------------

}

namespace DeepCore.EventTrigger
{
    //------------------------------------------------------------------------------------------------------------------------------
    #region BASE

    public abstract class BehaviorNode : IExternalizable
    {
        public string GUID;
        public int EditorX;
        public int EditorY;
        public uint EditorARGB;
        public int EditorTag;
        public virtual void WriteExternal(IOutputStream output)
        {
            output.PutUTF(GUID);
            output.PutS32(EditorX);
            output.PutS32(EditorY);
            output.PutU32(EditorARGB);
            output.PutS32(EditorTag);
        }
        public virtual void ReadExternal(IInputStream input)
        {
            this.GUID = input.GetUTF();
            this.EditorX = input.GetS32();
            this.EditorY = input.GetS32();
            this.EditorARGB = input.GetU32();
            this.EditorTag = input.GetS32();
        }
    }

    //------------------------------------------------------------------------------------------------------------------------------

    public enum LinkDock : byte
    {
        Input = 1,
        Output = 2,
    }
    public class LinkOption
    {
        public string NextGUID;
        public string NextFieldName;
        public LinkDock NextFieldState;
        public string OwnerFieldName;
        public LinkDock OwnerFieldState;
    }
    public abstract class EventBehaviorNode : BehaviorNode, IXmlBeforeExternalizable, IXmlAfterExternalizable
    {
        public abstract EventExternalizable EventData { get; set; }
        public List<LinkOption> Inputs;
        public List<LinkOption> Outputs;
        public void ForEachOptions<ST>(in ST state, ForEachAction<ST, LinkOption, LinkDock> action)
        {
            if (this.Inputs != null)
            {
                foreach (var link in this.Inputs.ToArray())
                {
                    action(state, link, LinkDock.Input);
                }
            }
            if (this.Outputs != null)
            {
                foreach (var link in this.Outputs.ToArray())
                {
                    action(state, link, LinkDock.Output);
                }
            }
        }
        public bool TryGetFieldConnection(string fieldName, out List<LinkOption> connection)
        {
            connection = new List<LinkOption>();
            ForEachOptions(connection, (connection, link, dock) =>
            {
                if (link.OwnerFieldName == fieldName)
                {
                    connection.Add(link);
                }
            });
            return connection.Count > 0;
        }
        public bool TryGetFieldConnection(string nextGUID, string nextFieldName, out LinkOption connection)
        {
            if (this.Inputs != null)
            {
                foreach (var link in this.Inputs)
                {
                    if (nextGUID == link.NextGUID && nextFieldName == link.NextFieldName)
                    {
                        connection = link;
                        return true;
                    }
                }
            }
            if (this.Outputs != null)
            {
                foreach (var link in this.Outputs)
                {
                    if (nextGUID == link.NextGUID && nextFieldName == link.NextFieldName)
                    {
                        connection = link;
                        return true;
                    }
                }
            }
            connection = default;
            return false;
        }

        public static EventBehaviorNode CreateNode(EventExternalizable data)
        {
            if (data is EventLocalVar var) return new EventBehaviorLocalVar() { EventData = var };
            if (data is AbstractTrigger trigger) return new EventBehaviorTrigger() { EventData = trigger };
            if (data is AbstractAction action) return new EventBehaviorAction() { EventData = action };
            if (data is AbstractValue value) return new EventBehaviorValue() { EventData = value };
            if (data is BehaviorGroup group) return new EventBehaviorGroup() { EventData = group };
            return null;
        }
        public void CleanFields()
        {
            if (EventData != null)
            {
                var typeInfo = EventData.GetTypeInfo();
                foreach (var field in typeInfo.GetFields())
                {
                    if (typeof(AbstractTrigger).IsAssignableFrom(field.Field.FieldType))
                    {
                        field.SetValue(this.EventData, null);
                    }
                    else if (typeof(AbstractAction).IsAssignableFrom(field.Field.FieldType))
                    {
                        field.SetValue(this.EventData, null);
                    }
                    else if (typeof(AbstractValue).IsAssignableFrom(field.Field.FieldType))
                    {
                        field.SetValue(this.EventData, null);
                    }
                    else
                    {
                        // Keep Primitive
                    }
                }
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------
        #region IO -----------------------------------------------------------
        public virtual void BeforeEncode(XmlElement e)
        {
            CleanFields();
        }
        public virtual void AfterEncode(XmlElement e)
        {
        }
        public virtual void BeforeDecode(XmlElement e)
        {
        }
        public virtual void AfterDecode(XmlElement e)
        {
            CleanFields();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutExt(EventData);
            output.PutList(Inputs, static (output, v) =>
            {
                output.PutUTF(v.NextGUID);
                output.PutUTF(v.NextFieldName);
                output.PutEnum8(v.NextFieldState);
                output.PutUTF(v.OwnerFieldName);
                output.PutEnum8(v.OwnerFieldState);
            });
            output.PutList(Outputs, static (output, v) =>
            {
                output.PutUTF(v.NextGUID);
                output.PutUTF(v.NextFieldName);
                output.PutEnum8(v.NextFieldState);
                output.PutUTF(v.OwnerFieldName);
                output.PutEnum8(v.OwnerFieldState);
            });
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.EventData = input.GetExt<EventExternalizable>();
            this.Inputs = input.GetList(static (input) => new LinkOption()
            {
                NextGUID = input.GetUTF(),
                NextFieldName = input.GetUTF(),
                NextFieldState = input.GetEnum8<LinkDock>(),
                OwnerFieldName = input.GetUTF(),
                OwnerFieldState = input.GetEnum8<LinkDock>(),
            });
            this.Outputs = input.GetList(static (input) => new LinkOption()
            {
                NextGUID = input.GetUTF(),
                NextFieldName = input.GetUTF(),
                NextFieldState = input.GetEnum8<LinkDock>(),
                OwnerFieldName = input.GetUTF(),
                OwnerFieldState = input.GetEnum8<LinkDock>(),
            });
        }
        #endregion
        //------------------------------------------------------------------------------------------------------------------------------
        #region Runtime -----------------------------------------------------------
        public IDynamicTypeInfo TypeInfo { get; private set; }
        public virtual void Init(EventBehaviorAssembly panel)
        {
            if (EventData != null)
            {
                this.TypeInfo = this.EventData.GetTypeInfo();
                foreach (var field in TypeInfo.GetFields())
                {
                    if (typeof(AbstractTrigger).IsAssignableFrom(field.Field.FieldType))
                    {
                        field.SetValue(this.EventData, null);
                    }
                    else if (EventBehaviorNode.IsMonoList(field.Field.FieldType, out var memberType))
                    {
                        field.SetValue(this.EventData, (IList)DeepActivator.CreateInstance(field.Field.FieldType));
                    }
                    else if (EventBehaviorNode.IsStereoList(field.Field.FieldType, out memberType))
                    {
                        field.SetValue(this.EventData, (IList)DeepActivator.CreateInstance(field.Field.FieldType));
                    }
                    else if (typeof(AbstractAction).IsAssignableFrom(field.Field.FieldType))
                    {
                        field.SetValue(this.EventData, new DoNoting());
                    }
                    else if (typeof(AbstractValue).IsAssignableFrom(field.Field.FieldType))
                    {
                        field.SetValue(this.EventData, ValueTypeNameSpace.Instance.MakeDefault(field.Field.FieldType));
                    }
                    else
                    {
                        // Keep Primitive
                    }
                }
            }
        }
        public virtual void Bind(EventBehaviorAssembly panel)
        {
            if (EventData != null)
            {
                EventData.OwnerNode = this;
                if (Inputs != null)
                {
                    foreach (var input in Inputs)
                    {
                        TryLoadField(panel, input);
                    }
                    Inputs.TrimExcess();
                }
                if (Outputs != null)
                {
                    foreach (var output in Outputs)
                    {
                        TryLoadField(panel, output);
                    }
                    Outputs.TrimExcess();
                }
            }
        }
        public virtual void InitEnd(EventBehaviorAssembly panel) { }
        protected bool TryLoadField(EventBehaviorAssembly panel, LinkOption link)
        {
            return TrySetFieldInfo(panel, link.OwnerFieldName, link.NextGUID, link.NextFieldName);
        }
        public bool TrySetFieldInfo(EventBehaviorAssembly panel, string ownerFieldName, string linkGUID, string linkFieldName)
        {
            if (TryParseMonoField(ownerFieldName, out var dataFieldName, out int index))
            {
                var dfield = TypeInfo.GetField(dataFieldName);
                if (dfield != null && IsMonoList(dfield.Field.FieldType, out var memberType))
                {
                    var list = dfield.GetValue(this.EventData) as IList;
                    if (panel.TryGetNode(linkGUID, out var next))
                    {
                        list.TryGetOrCreateListData(index, out var member, next.EventData, static (data) => data);
                        return true;
                    }
                }
            }
            else if (TryParseStereoField(ownerFieldName, out dataFieldName, out var attrName, out index))
            {
                var dfield = TypeInfo.GetField(dataFieldName);
                if (dfield != null && IsStereoList(dfield.Field.FieldType, out var memberType))
                {
                    var list = dfield.GetValue(this.EventData) as IList;
                    if (!list.TryGetOrCreateListData(index, out IStereoOption member, memberType, static (memberType) => (IStereoOption)DeepActivator.CreateInstance(memberType)))
                    {
                        member.Input = null;
                        member.Output = null;
                    }
                    if (panel.TryGetNode(linkGUID, out var next))
                    {
                        if (attrName == member.InputName)
                        {
                            member.Input = next.EventData;
                            return true;
                        }
                        if (attrName == member.OutputName)
                        {
                            member.Output = next.EventData;
                            return true;
                        }
                    }
                }
            }
            else
            {
                var dfield = TypeInfo.GetField(ownerFieldName);
                if (dfield != null && panel.TryGetNode(linkGUID, out var next))
                {
                    if (dfield.Field.FieldType.IsInstanceOfType(next.EventData))
                    {
                        dfield.SetValue(this.EventData, next.EventData);
                        return true;
                    }
                    else if (next.TryGetFieldConnection(this.GUID, ownerFieldName, out var conn))
                    {
                        if (conn.OwnerFieldName.TryIndexOf("arg:", out var aIndex))
                        {
                            var methodName = conn.OwnerFieldName.Substring(aIndex + "arg:".Length);
                            var method = next.TypeInfo.DataType.GetMethod(methodName);
                            if (method != null)
                            {
                                // 调用对方的方法给自己
                                var dtype = typeof(RuntimeAbstractValue<>).MakeGenericType(dfield.Field.FieldType.GenericTypeArguments);
                                var value = DeepActivator.CreateInstance(dtype) as IRuntimeValue;
                                value.Init(next.EventData, method, conn);
                                dfield.SetValue(this.EventData, value);
                                return true;
                            }
                            else
                            {
                                throw new Exception($"找不到参数节点: {next.EventData} : {conn.OwnerFieldName}");
                            }
                        }
                        else if (conn.OwnerFieldName.TryIndexOf("return:", out aIndex))
                        {
                            var methodName = conn.OwnerFieldName.Substring(aIndex + "return:".Length);
                            var method = next.TypeInfo.DataType.GetMethod(methodName);
                            if (method != null)
                            {
                                // 调用对方的方法给自己
                                var dtype = typeof(RuntimeAbstractValue<>).MakeGenericType(dfield.Field.FieldType.GenericTypeArguments);
                                var value = DeepActivator.CreateInstance(dtype) as IRuntimeValue;
                                value.Init(next.EventData, method, conn);
                                dfield.SetValue(this.EventData, value);
                                return true;
                            }
                            else
                            {
                                throw new Exception($"找不到参数节点: {next.EventData} : {conn.OwnerFieldName}");
                            }
                        }
                        // Trigger 下的参数是假节点
                    }
                }
            }
            return false;
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------------------------
        #region Field Layout -----------------------------------------------------------
        public static bool TryGetValueField(EventExternalizable data, string fieldName, out IDynamicFieldInfo field)
        {
            if (data != null)
            {
                var TypeInfo = data.GetTypeInfo();
                field = TypeInfo.GetField(fieldName);
                return field != null;
            }
            field = null;
            return false;
        }
        public static void ForEachEventDataValueFields(EventExternalizable data, Action<EventExternalizable, IDynamicFieldInfo> action)
        {
            if (data != null)
            {
                var TypeInfo = data.GetTypeInfo();

                foreach (var field in TypeInfo.GetFields())
                {
                    if (typeof(AbstractTrigger).IsAssignableFrom(field.Field.FieldType))
                    {
                        action(data, field);
                    }
                    else if (EventBehaviorNode.IsMonoList(field.Field.FieldType, out var memberType))
                    {
                        action(data, field);
                    }
                    else if (EventBehaviorNode.IsStereoList(field.Field.FieldType, out memberType))
                    {
                        action(data, field);
                    }
                    else if (typeof(AbstractAction).IsAssignableFrom(field.Field.FieldType))
                    {
                        action(data, field);
                    }
                    else if (typeof(AbstractValue).IsAssignableFrom(field.Field.FieldType))
                    {
                        action(data, field);
                    }
                    else
                    {
                        // Keep Primitive
                    }
                }
            }
        }

        public static bool IsPrimitiveField(MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                if (typeof(AbstractValue).IsAssignableFrom(field.FieldType))
                {
                    return false;
                }
                if (typeof(EventExternalizable).IsAssignableFrom(field.FieldType))
                {
                    return false;
                }
                if (IsMonoList(field.FieldType, out var memberType))
                {
                    return false;
                }
                if (IsStereoList(field.FieldType, out memberType))
                {
                    return false;
                }
            }
            return true;
        }
        public static bool IsListType(Type type, out Type memberType)
        {
            memberType = null;
            //             if (type.IsArray && type.GetArrayRank() == 1)
            //             {
            //                 indexType = typeof(int);
            //                 memberType = type.GetElementType();
            //                 return true;
            //             }
            if (type.IsInterfaceOf(typeof(IList)))
            {
                var gargs = type.GetGenericArguments();
                if (gargs.Length == 1)
                {
                    memberType = gargs[0];
                    return true;
                }
            }
            //             if (type.IsInterfaceOf(typeof(IDictionary)))
            //             {
            //                 var gargs = type.GetGenericArguments();
            //                 if (gargs.Length == 2)
            //                 {
            //                     indexType = gargs[0];
            //                     memberType = gargs[1];
            //                     return true;
            //                 }
            //             }
            return false;
        }
        public static bool IsMonoList(Type type, out Type memberType)
        {
            if (IsListType(type, out memberType))
            {
                if (typeof(IStereoOption).IsAssignableFrom(memberType))
                {
                    return false;
                }
                if (typeof(AbstractAction).IsAssignableFrom(memberType))
                {
                    return true;
                }
                if (typeof(AbstractValue).IsAssignableFrom(memberType))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsStereoList(Type type, out Type memberType)
        {
            if (IsListType(type, out memberType))
            {
                if (typeof(IStereoOption).IsAssignableFrom(memberType))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsStereoList(Type type, out Type memberType, out StereoOptionAttribute attr)
        {
            if (IsListType(type, out memberType))
            {
                if (typeof(IStereoOption).IsAssignableFrom(memberType) && memberType.TryGetAttribute<StereoOptionAttribute>(out var stereo))
                {
                    attr = stereo;
                    return true;
                }
            }
            attr = null;
            return false;
        }
        public static void ToMonoMemberFieldName(string FieldName, int index, out string fieldName)
        {
            fieldName = $"{FieldName}#{index}";
        }
        public static void ToStereoMemberFieldName(string FieldName, IStereoOption attr, int index, out string inFieldName, out string outFieldName)
        {
            inFieldName = $"{FieldName}.{attr.InputName}#{index}";
            outFieldName = $"{FieldName}.{attr.OutputName}#{index}";
        }
        public static void ToStereoMemberFieldName(string FieldName, StereoOptionAttribute attr, int index, out string inFieldName, out string outFieldName)
        {
            inFieldName = $"{FieldName}.{attr.InputName}#{index}";
            outFieldName = $"{FieldName}.{attr.OutputName}#{index}";
        }
        public static bool TryParseMonoField(string fieldName, out string dataFieldName, out int index)
        {
            if (!fieldName.TryIndexOf('.', out var subNameIndex) && fieldName.TryIndexOf('#', out var numberIndex))
            {
                if (Parser.TryParseInt(fieldName.Substring(numberIndex + 1), out index))
                {
                    dataFieldName = fieldName.Substring(0, numberIndex);
                    return true;
                }
            }
            dataFieldName = null;
            index = 0;
            return false;
        }
        public static bool TryParseStereoField(string fieldName, out string dataFieldName, out string attrName, out int index)
        {
            if (fieldName.TryIndexOf('.', out var subNameIndex) && fieldName.TryIndexOf('#', out var numberIndex))
            {
                if (Parser.TryParseInt(fieldName.Substring(numberIndex + 1), out index))
                {
                    dataFieldName = fieldName.Substring(0, subNameIndex);
                    attrName = fieldName.Substring(subNameIndex + 1, numberIndex - subNameIndex - 1);
                    return true;
                }
            }
            attrName = null;
            dataFieldName = null;
            index = 0;
            return false;
        }


        //------------------------------------------------------------------------------------------------------------------------------
        public static void SetToField(
            EventBehaviorNode parent,
            string parentFieldName,
            LinkDock parentDock,
            EventBehaviorNode thisData,
            string thisFieldName,
            LinkDock thisDock)
        {
            if (parentDock == LinkDock.Output)
            {
                parent.Outputs.Add(new LinkOption()
                {
                    OwnerFieldName = parentFieldName,
                    OwnerFieldState = LinkDock.Output,
                    NextGUID = thisData.GUID,
                    NextFieldName = thisFieldName,
                    NextFieldState = LinkDock.Input
                });
            }
            else if (parentDock == LinkDock.Input)
            {
                parent.Inputs.Add(new LinkOption()
                {
                    OwnerFieldName = parentFieldName,
                    OwnerFieldState = LinkDock.Input,
                    NextGUID = thisData.GUID,
                    NextFieldName = thisFieldName,
                    NextFieldState = LinkDock.Output
                });
            }

            if (thisDock == LinkDock.Output)
            {
                thisData.Outputs.Add(new LinkOption()
                {
                    OwnerFieldName = thisFieldName,
                    OwnerFieldState = LinkDock.Output,
                    NextGUID = parent.GUID,
                    NextFieldName = parentFieldName,
                    NextFieldState = LinkDock.Input
                });
            }
            else if (thisDock == LinkDock.Input)
            {
                thisData.Inputs.Add(new LinkOption()
                {
                    OwnerFieldName = thisFieldName,
                    OwnerFieldState = LinkDock.Input,
                    NextGUID = parent.GUID,
                    NextFieldName = parentFieldName,
                    NextFieldState = LinkDock.Output
                });
            }
        }

        #endregion
    }
    public abstract class EventBehaviorNode<T> : EventBehaviorNode where T : EventExternalizable
    {
    }

    #endregion
    //------------------------------------------------------------------------------------------------------------------------------
    [Desc("行为变量")]
    public class EventBehaviorLocalVar : EventBehaviorNode<EventLocalVar>
    {
        public EventLocalVar Data;
        public EventLocalVar VAR { get => Data; }
        public override EventExternalizable EventData { get => Data; set => Data = value as EventLocalVar; }

        //         public override void Bind(EventBehaviorAssembly panel)
        //         {
        //             base.Bind(panel);
        //         }
    }
    //------------------------------------------------------------------------------------------------------------------------------
    [Desc("行为起始点")]
    public class EventBehaviorTrigger : EventBehaviorNode<AbstractTrigger>
    {
        public const string KEY_CALL = "@CALL";
        public const string KEY_ENTRY = "@LISTEN"; 
        public AbstractTrigger Data;
        public AbstractTrigger Trigger { get => Data; }
        public override EventExternalizable EventData { get => Data; set => Data = value as AbstractTrigger; }   
        public List<EventBehaviorNode> CALL { get; } = new List<EventBehaviorNode>(0);
        public override void InitEnd(EventBehaviorAssembly panel)
        {
            base.InitEnd(panel);
            CALL.Clear();
            panel.GetMonoOptionLinkNodes<EventBehaviorNode>(Outputs, EventBehaviorTrigger.KEY_CALL, (linkNext, next, index) =>
            {
                if (index >= 0)
                {
                    CUtils.SetListLength<EventBehaviorNode>(CALL, Math.Max(index + 1, CALL.Count));
                    CALL[index] = (next);
                }
                else
                {
                    CALL.Add(next);
                }
            });
            CALL.TrimExcess();
        }
        public void AppendCall(EventBehaviorAction call)
        {
            SetToField(this, KEY_CALL, LinkDock.Output, call, EventBehaviorAction.KEY_ENTRY, LinkDock.Input);
        }
    }
    //------------------------------------------------------------------------------------------------------------------------------
    [Desc("行为动作")]
    public class EventBehaviorAction : EventBehaviorNode<AbstractAction>
    {
        public const string KEY_ENTRY = "@ENTRY";
        public const string KEY_NEXT = "@NEXT";
        public AbstractAction Data;
        public AbstractAction Action { get => Data; }
        public override EventExternalizable EventData { get => Data; set => Data = value as AbstractAction; }
        public List<EventBehaviorNode> NEXT { get; } = new List<EventBehaviorNode>(0);
        public override void InitEnd(EventBehaviorAssembly panel)
        {
            base.InitEnd(panel);
            NEXT.Clear();
            panel.GetOptionLinkNodes(Inputs, KEY_ENTRY, (linkEntry, entry) =>
            {
                //入口有可能是 Trigging 或者 NEXT
            });
            panel.GetMonoOptionLinkNodes<EventBehaviorNode>(Outputs, EventBehaviorAction.KEY_NEXT, (linkNext, next, index) =>
            {
                if (index >= 0)
                {
                    CUtils.SetListLength<EventBehaviorNode>(NEXT, Math.Max(index + 1, NEXT.Count));
                    NEXT[index] = (next);
                }
                else
                {
                    NEXT.Add(next);
                }
            });
            NEXT.TrimExcess();
        }
        public override void AfterDecode(XmlElement e)
        {
            base.AfterDecode(e);
        }
    }
    //------------------------------------------------------------------------------------------------------------------------------
    [Desc("行为参数")]
    public class EventBehaviorValue : EventBehaviorNode<AbstractValue>
    {
        public const string KEY_OUT = "@OUT";
        public AbstractValue Data;
        public AbstractValue Value { get => Data; }
        public override EventExternalizable EventData { get => Data; set => Data = value as AbstractValue; }
        public override void Bind(EventBehaviorAssembly panel)
        {
            base.Bind(panel);
            panel.GetOptionLinkNodes(Outputs, KEY_OUT, (linkOut, output) =>
            {
                // 反向设置回来
                if (output.TrySetFieldInfo(panel, linkOut.NextFieldName, this.GUID, linkOut.OwnerFieldName))
                {

                }
            });
        }
    }
    //------------------------------------------------------------------------------------------------------------------------------
    [Desc("行为分组")]
    public class EventBehaviorGroup : EventBehaviorNode<BehaviorGroup>
    {
        public int Width = 160;
        public int Height = 120;
        public BehaviorGroup Data;
        public BehaviorGroup Group { get => Data; }
        public override EventExternalizable EventData { get => Data; set => Data = value as BehaviorGroup; }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(Width);
            output.PutS32(Height);
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.Width = input.GetS32();
            this.Height = input.GetS32();
        }
    }
    //------------------------------------------------------------------------------------------------------------------------------
    public interface IRuntimeValue
    {
        void Init(EventExternalizable data, MethodInfo method, LinkOption link);
    }
    [IgnoreGenerate]
    public class RuntimeAbstractValue<T> : AbstractValue<T>, IRuntimeValue
    {
        private EventExternalizable owner { get; set; }
        private MethodInfo method { get; set; }
        private LinkOption link { get; set; }
        private object[] args { get; set; } = new object[1];
        public void Init(EventExternalizable data, MethodInfo method, LinkOption link)
        {
            this.owner = data;
            this.method = method;
            this.link = link;
        }
        protected override T GetValue(EventExecutor api, IEventArguments args)
        {
            this.args[0] = args;
            var ret = method.Invoke(owner, this.args);
            return (T)ret;
        }
        protected override void GetText(EventStringBuilder sw)
        {
            if (method.TryGetAttribute<TriggingArgAttribute>(out var desc))
            {
                sw.Append(desc.Desc);
            }
            else if (method.TryGetAttribute<ReturnValueAttribute>(out var rdesc))
            {
                sw.Append(rdesc.Desc);
            }
            else
            {
                sw.Append(method.Name);
            }
        }
    }
}
