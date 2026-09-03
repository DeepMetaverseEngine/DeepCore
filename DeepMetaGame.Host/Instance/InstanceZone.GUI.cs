using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepCore.GUI.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.Game3D.Host.Instance
{
    /// <summary>
    /// 代理用类
    /// </summary>
    partial class InstanceZone
    {
        private HashMap<string, HostGUIForm> active_forms = new HashMap<string, HostGUIForm>();
        private HashMap<int, HostGUIForm> single_forms = new HashMap<int, HostGUIForm>();
        private uint mGUIIDIndexer = 0;
        public HostGUIForm LastShownForm { get; private set; }
        internal uint genGUIID()
        {
            mGUIIDIndexer++;
            return mGUIIDIndexer;
        }

        public event OnFormHandler OnFormShown;
        public event OnFormHandler OnFormClosed;
        public event OnNodeEventHandler<GUINodeClickAction> OnFormNodeClick;
        public event OnNodeEventHandler<GUINodeDataChangedAction> OnFormNodeDataChanged;

        public delegate void OnFormHandler(InstanceZone zone, HostGUIForm form);
        public delegate void OnNodeHandler(InstanceZone zone, HostGUIForm form, HostGUINode node);
        public delegate void OnNodeEventHandler<T>(InstanceZone zone, HostGUIForm form, HostGUINode node, T action) where T : GUINodeAction;

        protected virtual void InitGUI()
        {

        }
        protected virtual void ProcessGUIMessage(BattleAction act)
        {
            if (act is GUINodeAction action)
            {
                if (!string.IsNullOrEmpty(action.GUID))
                {
                    if (active_forms.TryGetValue(action.GUID, out var form))
                    {
                        form.Handle(action);
                    }
                    else
                    {
                        log.Warn($"Can not find GUI : {action.GUID}");
                    }
                }
            }
        }
        protected virtual void ClearGUIEvents()
        {
            OnFormShown = null;
            OnFormClosed = null;
            OnFormNodeClick = null;
            foreach (var f in active_forms.Values.ToArray())
            {
                f.Dispose();
            }
            active_forms.Clear();
            single_forms.Clear();
        }
        void RemoveForm(HostGUIForm form)
        {
            if (form != null)
            {
                if (active_forms.Remove(form.Name, out var removed))
                {
                }
                if (form.Info.SingleInstance)
                {
                    single_forms.Remove(form.Info.ID);
                }
            }
        }
        public HostGUIDialog ShowDialog(int guiTemplateID, bool closeOnClick, object state = null)
        {
            if (DataRoot.Templates.TryGetTemplate<BattleUITemplate>(guiTemplateID, out var Data))
            {
                if (Data.SingleInstance && single_forms.TryGetValue(guiTemplateID, out var form))
                {
                    return form as HostGUIDialog;
                }
                if (Data.Forms != null)
                {
                    Data = CloneData(Data);
                    var dialog = new HostGUIDialog(this, state, Data);
                    if (dialog != null)
                    {
                        if (Data.SingleInstance) single_forms.Add(guiTemplateID, dialog);
                        active_forms.Add(dialog.Name, dialog);
                        dialog.ShowDialog(null, closeOnClick);
                    }
                    return dialog;
                }
            }
            return null;
        }

        public HostGUIForm ShowForm(int guiTemplateID, object state = null)
        {
            if (DataRoot.Templates.TryGetTemplate<BattleUITemplate>(guiTemplateID, out var Data))
            {
                if (Data.SingleInstance && single_forms.TryGetValue(guiTemplateID, out var form))
                {
                    return form as HostGUIForm;
                }
                if (Data.Forms != null)
                {
                    Data = CloneData(Data);
                    var dialog = new HostGUIForm(this, state, Data);
                    if (dialog != null)
                    {
                        if (Data.SingleInstance) single_forms.Add(guiTemplateID, dialog);
                        active_forms.Add(dialog.Name, dialog);
                        dialog.Show(null);
                    }
                    return dialog;
                }
            }
            return null;
        }
        public HostGUIDialog ShowPlayerDialog(int guiTemplateID, InstancePlayer player, bool closeOnClick, object state = null)
        {
            if (DataRoot.Templates.TryGetTemplate<BattleUITemplate>(guiTemplateID, out var Data))
            {
                if (Data.SingleInstance && single_forms.TryGetValue(guiTemplateID, out var form))
                {
                    return form as HostGUIDialog;
                }
                if (Data.Forms != null)
                {
                    Data = CloneData(Data);
                    var dialog = new HostGUIDialog(this, state, Data);
                    if (dialog != null)
                    {
                        if (Data.SingleInstance) single_forms.Add(guiTemplateID, dialog);
                        active_forms.Add(dialog.Name, dialog);
                        dialog.ShowDialog(player, closeOnClick);
                    }
                    return dialog;
                }
            }
            return null;
        }
        public HostGUIForm ShowPlayerForm(InstancePlayer player, int guiTemplateID, object state = null)
        {
            if (DataRoot.Templates.TryGetTemplate<BattleUITemplate>(guiTemplateID, out var Data))
            {
                if (Data.Forms != null)
                {
                    if (Data.SingleInstance && single_forms.TryGetValue(guiTemplateID, out var form))
                    {
                        return form as HostGUIForm;
                    }
                    Data = CloneData(Data);
                    var dialog = new HostGUIForm(this, state, Data);
                    if (dialog != null)
                    {
                        if (Data.SingleInstance) single_forms.Add(guiTemplateID, dialog);
                        active_forms.Add(dialog.Name, dialog);
                        dialog.Show(player);
                    }
                    return dialog;
                }
            }
            return null;
        }

        private void Dialog_OnShown(HostGUIForm sender)
        {
            this.OnFormShown?.Invoke(this, sender);
        }
        private void Dialog_OnClose(HostGUIForm sender)
        {
            this.OnFormClosed?.Invoke(this, sender);
        }
        private void Dialog_OnNodeClick(HostGUIForm form, HostGUINode sender, GUINodeClickAction action)
        {
            this.OnFormNodeClick?.Invoke(this, form, sender, action);
        }
        private void Dialog_OnNodeDataChanged(HostGUIForm form, HostGUINode sender, GUINodeDataChangedAction action)
        {
            this.OnFormNodeDataChanged?.Invoke(this, form, sender, action);
        }






        public abstract class HostGUIComponent : Disposable
        {
            private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(HostGUIComponent));
            new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
            new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }

            private HashMap<string, object> bindData = new();
            private bool visible = true;
            private HashMap<string, HostGUIComponent> childs = new HashMap<string, HostGUIComponent>();
            public InstanceZone Zone { get; }
            public string Name { get; }
            public abstract HostGUIForm Form { get; }
            public HostGUIComponent(InstanceZone zone, string name, bool visible)
            {
                Alloc.RecordConstructor(GetType());
                this.Zone = zone;
                this.Name = name;
                this.visible = visible;
            }
            ~HostGUIComponent()
            {
                if (!IsDisposed)
                {
                    Alloc.RecordDispose(GetType());
                }
                Alloc.RecordDestructor(GetType());
            }
            sealed protected override void RecordDisposing()
            {
                Alloc.RecordDispose(this.GetType());
            }
            protected override void Disposing()
            {
                bindData.Clear();
                foreach (var close in childs.Values)
                {
                    close.Dispose();
                }
            }
            protected void AddChild(string name, HostGUIComponent comp)
            {
                this.childs.Add(name, comp);
            }
            public HostGUINode GetChild(string name)
            {
                {
                    if (childs.TryGetValue(name, out var child) && child is HostGUINode node)
                    {
                        return node;
                    }
                }
                foreach (var child in childs.Values)
                {
                    if (child.GetChild(name) is HostGUINode node)
                    {
                        return node;
                    }
                }
                return null;
            }
            public bool Visible
            {
                get => visible;
                set
                {
                    if (visible != value)
                    {
                        visible = value;
                        var msg = Zone.objectPool.Alloc<GUINodeVisibleEvent>();
                        {
                            msg.GUID = Form.Name;
                            msg.NodeName = this.Name;
                            msg.Visible = visible;
                        }                        
                        Zone.PostEvent(msg);
                    }
                }
            }
            public void BindData(string key, object zoneVar, bool deep, string subNodeURL = null)
            {
                this.bindData.Put(key, zoneVar);
                var msg = Zone.objectPool.Alloc<GUINodeBindDataEvent>();
                {
                    msg.GUID = Form.Name;
                    msg.NodeName = this.Name;
                    msg.Key = key;
                    msg.ZoneVar = Zone.HostFactory.EncodeZoneVar(zoneVar);
                    msg.Deep = deep;
                    msg.SubNodeURL = subNodeURL;
                }
                Zone.PostEvent(msg);
            }
            public void SubVisible(string subNodeURL, bool value)
            {
                var msg = Zone.objectPool.Alloc<GUINodeVisibleEvent>();
                {
                    msg.GUID = Form.Name;
                    msg.NodeName = this.Name;
                    msg.Visible = value;
                    msg.SubNodeURL = subNodeURL;
                }
                Zone.PostEvent(msg);
            }
            public void SubControl(string subNodeURL, object value)
            {
                var msg = Zone.objectPool.Alloc<GUINodeControlEvent>();
                {
                    msg.GUID = Form.Name;
                    msg.NodeName = this.Name;
                    msg.SubNodeURL = subNodeURL;
                    msg.ZoneVar = Zone.HostFactory.EncodeZoneVar(value);
                }
                Zone.PostEvent(msg);
            }
            public object GetBindData(string key)
            {
                return bindData.Get(key);
            }
        }
        public class HostGUINode : HostGUIComponent
        {
            public override HostGUIForm Form { get; }
            public UEComponentMeta Meta { get; }
            public HostGUINode(InstanceZone zone, HostGUIForm form, UEComponentMeta meta)
                : base(zone, meta.EditorName, meta.Visible)
            {
                this.Form = form;
                this.Meta = meta;
                this.StringValue = meta.GetStringValue();
                this.BoolValue = meta.GetBoolValue();
                this.NumberValue = meta.GetNumberValue();
                if (meta is UEContainerMeta containerMeta)
                {
                    if (containerMeta.Childs != null)
                    {
                        foreach (var subMeta in containerMeta.Childs)
                        {
                            var node = new HostGUINode(zone, form, subMeta);
                            base.AddChild(subMeta.EditorName, node);
                        }
                    }
                }
            }
            protected override void Disposing()
            {
                this.OnClick = null;
                this.OnDataChanged = null;
                base.Disposing();
            }
            internal void Handle(GUINodeClickAction click)
            {
                this.OnClick?.Invoke(Form, this, click);
            }
            internal void Handle(GUINodeDataChangedAction change)
            {
                this.StringValue = change.TextValue;
                this.BoolValue = change.BooleanValue;
                this.NumberValue = change.NumberValue;
                this.OnDataChanged?.Invoke(Form, this, change);
            }
            public string StringValue { get; set; }
            public bool BoolValue { get; set; }
            public double NumberValue { get; set; }
            public event UINodeEventHandle<GUINodeClickAction> OnClick;
            public event UINodeEventHandle<GUINodeDataChangedAction> OnDataChanged;
        }

        public class HostGUIForm : HostGUIComponent
        {
            private GUIEventTriggerCollection mBindEvents;
            public override HostGUIForm Form { get => this; }
            public BattleUITemplate Info { get; }
            public object State { get; }
            public InstancePlayer BindingPlayer { get; private set; }
            public string DialogResult { get; private set; }
            public HostGUIForm(InstanceZone zone, object sender, BattleUITemplate gui)
                : base(zone, zone.genGUIID().ToString(), true)
            {
                this.State = sender;
                this.Info = gui;
                if (gui.Forms != null)
                {
                    foreach (var meta in gui.Forms)
                    {
                        var node = new HostGUINode(zone, this, meta);
                        base.AddChild(meta.EditorName, node);
                    }
                }
            }
            protected override void Disposing()
            {
                OnShown = null;
                OnClose = null;
                OnNodeClick = null;
                mBindEvents.Dispose();
                Zone.RemoveForm(this);
                base.Disposing();
            }
            public void Show(InstancePlayer player)
            {
                this.BindingPlayer = player;
                this.Zone.LastShownForm = this;
                this.mBindEvents = Zone.HostFactory.CreateGUIEventCollection(this);//new GUIEventTriggerCollection(this);
                this.mBindEvents.Start();
                this.OnNodeClick += Zone.Dialog_OnNodeClick;
                this.OnNodeDataChanged += Zone.Dialog_OnNodeDataChanged;
                this.OnShown += Zone.Dialog_OnShown;
                this.OnClose += Zone.Dialog_OnClose;
                if (player != null)
                {
                    var show = Zone.ObjectPool.Alloc<ShowPlayerFormEvent>();
                    {
                        show.object_id = player.ObjectID;
                        show.GUID = this.Name;
                        show.GUITemplateID = Info.ID;
                        show.IsDialog = this is HostGUIDialog;
                    }
                    Zone.PostObjectEvent(player, show);
                }
                else
                {
                    var show = Zone.ObjectPool.Alloc<ShowFormEvent>();
                    {
                        show.GUID = this.Name;
                        show.GUITemplateID = Info.ID;
                        show.IsDialog = this is HostGUIDialog;
                    }
                    Zone.PostEvent(show);
                }
                this.OnShown?.Invoke(this);
            }

            public void Close()
            {
                var close = Zone.ObjectPool.Alloc<CloseFormEvent>();
                {
                    close.GUID = this.Name;
                }
                Zone.PostEvent(close);
                this.OnClose?.Invoke(this);
                this.Dispose();
            }
            internal void Handle(GUINodeAction act)
            {
                var node = (GetChild(act.NodeName) as HostGUINode);
                if (act is GUINodeClickAction clickAction) Handle(node, clickAction);
                else if (act is GUINodeDataChangedAction dataChange) Handle(node, dataChange);
            }
            protected virtual void Handle(HostGUINode node, GUINodeClickAction click)
            {
                if (node != null)
                {
                    node.Handle(click);
                }
                DialogResult = click.DialogResult;
                OnNodeClick?.Invoke(this, node, click);
            }
            protected virtual void Handle(HostGUINode node, GUINodeDataChangedAction change)
            {
                if (node != null)
                {
                    node.Handle(change);
                }
                OnNodeDataChanged?.Invoke(this, node, change);
            }
            public event UIFormHandler OnShown;
            public event UIFormHandler OnClose;
            public event UINodeEventHandle<GUINodeClickAction> OnNodeClick;
            public event UINodeEventHandle<GUINodeDataChangedAction> OnNodeDataChanged;
        }

        public class HostGUIDialog : HostGUIForm
        {
            public bool CloseOnClick { get; private set; } = true;
            public HostGUIDialog(InstanceZone zone, object sender, BattleUITemplate gui) : base(zone, sender, gui)
            {
            }
            public void ShowDialog(InstancePlayer player, bool closeOnClick)
            {
                this.CloseOnClick = closeOnClick;
                base.Show(player);
            }
            protected override void Handle(HostGUINode node, GUINodeClickAction click)
            {
                base.Handle(node, click);
                if (!string.IsNullOrEmpty(click.DialogResult))
                {
                    OnSelectDialog?.Invoke(this, node, click, this.State);
                    this.Close();
                }
                else if (CloseOnClick)
                {
                    this.Close();
                }
            }
            public event SelectDialogHandler OnSelectDialog;
        }




        public delegate void SelectDialogHandler(HostGUIForm form, HostGUIComponent node, GUINodeClickAction result, object state);

        public delegate void UIFormHandler(HostGUIForm form);
        public delegate void UINodeHandler(HostGUIForm form, HostGUINode node);
        public delegate void UINodeEventHandle<T>(HostGUIForm form, HostGUINode node, T action) where T : GUINodeAction;



    }
}

