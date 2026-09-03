using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Slave.GUI;
using System;
using System.Threading.Tasks;
using static DeepCore.Colors;

namespace DeepMetaGame.Display.GUI
{
    sealed public class ZoneGUINode : DisplayNodeComponent, IZoneGUINode
    {
        public ILayerZoneListener Zone { get => Form.Zone; }
        public LayerZone Layer { get => Form.Zone.Layer; }
        public string Name { get => Meta.EditorName; }
        public IZoneGUIForm Form { get; private set; }
        public UEComponentMeta Meta => (base.Owner as UEComponentNode).Meta;
        new public UEComponentNode Owner { get => (base.Owner as UEComponentNode); }
        public string DialogResult { get; set; }
        public bool Visible { get => Owner.IsVisible; internal set { Owner.IsVisible = value; } }
        internal void Init(IZoneGUIForm form)
        {
            this.Form = form;
            this.DialogResult = Meta.DialogResult;
        }
        public IZoneGUINode GetChild(string name)
        {
            var node = this.Owner.FindNodeByName(name);
            return node?.Components.GetComponentAs<ZoneGUINode>();
        }

        public object GetBindData(string key)
        {
            if (Owner.TryGetBindData(key, out var value))
            {
                return value;
            }
            return null;
        }

        public string GetString()
        {
            return Owner.GetTextValue();
        }
        public bool GetBool()
        {
            if (Owner is UEToggleButton toggle) { return toggle.IsChecked; }
            if (Owner is UECheckBox checkbox) { return checkbox.IsChecked; }
            return false;
        }
        public double GetNumber()
        {
            if (Owner is UEGauge gauge) { return gauge.GaugeRate; }
            return 0;
        }

    }
    public abstract class ZoneGUIForm : Disposable, IZoneGUIForm, IZoneGUIDialog
    {
        public ILayerZoneListener Zone { get; private set; }
        public UIFactory GUIFactory { get; }
        public string Name { get; }
        public string GUID { get => Name; }
        public BattleUITemplate Template { get; }
        public UEComponentMeta Meta { get; }
        public IZoneGUIForm Form { get => this; }
        abstract public DisplayNode RootNode { get; }
        public bool IsDialog { get; set; }
        public bool CloseOnClick { get; private set; }
        public ZoneGUIForm(UIFactory factory, ILayerZoneListener zone, string guid, BattleUITemplate gui)
        {
            this.Name = guid;
            this.Zone = zone;
            this.Template = gui;
            this.GUIFactory = factory;
            this.GUIFactory.OnCreateNode += GUIFactory_OnCreateNode;
        }
        protected override void Disposing()
        {
            OnDispose?.Invoke();
            OnDispose = null;
            this.GUIFactory.OnCreateNode -= GUIFactory_OnCreateNode;
            this.OnShown = null;
            this.OnClose = null;
            this.OnNodeClick = null;
            this.OnNodeDataChanged = null;
            this.OnSelectDialog = null;
            this.Zone = null;
        }
        public void SetVisible(GUINodeArgs e, bool v)
        {
            if (TryGetNode(e.NodeName, out var node))
            {
                node.Visible = v;
            }
        }
        public void BindData(GUINodeArgs e, string key, bool deep, object zoneVar)
        {
            RootNode.Canvas.Invoke((canvas) =>
            {
                if (!canvas.IsDisposing)
                {
                    if (TryGetNode(e.NodeName, out var node))
                    {
                        node.Owner.BindData(key, zoneVar, deep);
                    }
                    OnNodeBindData?.Invoke(node, e, key, zoneVar);
                }
            });
        }
        void IZoneGUIForm.ControlNode(GUINodeArgs e, string name, int? index, object zoneVar)
        {
        }
        public virtual void Show(bool closeOnClick, Action shown)
        {
            this.CloseOnClick = closeOnClick;
            RootNode.Canvas.Invoke((canvas) =>
            {
                if (!canvas.IsDisposing)
                {
                    if (Template.Forms != null)
                    {
                        foreach (var ui in Template.Forms)
                        {
                            var form = GUIFactory.CreateUI(ui);
                            RootNode.AddChild(form);
                            form.Decode();
                            form.BindData(string.Empty, Form.Zone.Layer, true);
                        }
                    }
                }
                Zone?.QueueTask(() =>
                {
                    shown();
                    this.OnShown?.Invoke(this);
                });
            });
        }

        protected virtual void GUIFactory_OnCreateNode(UEComponentMeta arg1, UEComponentNode arg2)
        {
            if (arg2 is UEComponentNode child)
            {
                var comp = child.Components.AddComponent<ZoneGUINode>();
                comp.Init(this);
                if (arg1 is UERootMeta rootMeta)
                {
                    arg2.Rect.IsDragMoveable = true;
                }
                else
                {
                    arg2.Rect.IsDragMoveable = false;
                }
                OnAddChild(child, comp);
            }
        }
        protected virtual void OnAddChild(UEComponentNode childNode, ZoneGUINode child)
        {
            childNode.Rect.MouseClick += (sender, args) =>
            {
                OnChildNodeClick(child, null);
            };
            childNode.OnTextChanged += (sender, newText, oldText) =>
            {
                OnChildNodeDataChanged(child, null);
            };
            childNode.OnCheckedChanged += (sender, isChecked) =>
            {
                OnChildNodeDataChanged(child, null);
            };
        }
        protected virtual void OnChildNodeClick(ZoneGUINode child, string subName)
        {
            Zone.QueueTask(() =>
            {
                OnNodeClick?.Invoke(child, subName);
            });
            if (IsDialog)
            {
                Zone.QueueTask(() =>
                {
                    if (!string.IsNullOrEmpty(child?.Meta?.DialogResult))
                    {
                        this.OnSelectDialog?.Invoke(child, subName, child.Meta.DialogResult);
                        this.Close();
                    }
                    else if (CloseOnClick)
                    {
                        this.Close();
                    }
                });
            }
        }
        protected virtual void OnChildNodeDataChanged(ZoneGUINode child, string subName)
        {
            Zone.QueueTask(() =>
            {
                OnNodeDataChanged?.Invoke(child, subName);
            });
        }

        public void Close()
        {
            Zone.QueueTask(() =>
            {
                OnClose?.Invoke(this);
                RootNode.Canvas.Invoke((canvas) =>
                {
                    this.Dispose();
                });
            });
        }
        public IZoneGUINode GetChild(string name)
        {
            var node = this.RootNode.FindNodeByName(name);
            return node?.Components.GetComponentAs<ZoneGUINode>();
        }
        bool IZoneGUIForm.TryGetNode(string name, out IZoneGUINode node)
        {
            if (TryGetNode(name, out var _node))
            {
                node = _node;
                return true;
            }
            node = null;
            return false;
        }
        public bool TryGetNode(string name, out ZoneGUINode node)
        {
            var enode = this.RootNode.FindNodeByName(name);
            var comp = enode?.Components.GetComponentAs<ZoneGUINode>();
            node = comp;
            return comp != null;
        }
        public event Action OnDispose;
        public event UIFormHandler OnShown;
        public event UIFormHandler OnClose;
        public event UINodeHandler OnNodeClick;
        public event UINodeHandler OnNodeDataChanged;
        public event UINodeDataHandler OnNodeBindData;
        public event SelectDialogHandler OnSelectDialog;
    }
}
