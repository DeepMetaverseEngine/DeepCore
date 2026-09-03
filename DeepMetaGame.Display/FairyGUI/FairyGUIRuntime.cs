using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.GUI.Data;
using DeepMetaGame.Data.GUI.Meta;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Slave.GUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepMetaGame.Display.FairyGUI
{
    public abstract class FairyGUIRuntime
    {
        public static FairyGUIRuntime Instance { get; private set; }
        public FairyGUIRuntime()
        {
            Instance = this;
        }
        public abstract FairyComponent CreatePanel(UEFairyGUIComponentMeta meta);
    }

    public interface IFairyCanvas
    {
        bool IsDisposing { get; }
        void InvokeAsync(Func<IFairyCanvas, Task> invoke);
        void Invoke(Action<IFairyCanvas> invoke);
    }
    public interface IFairyTask
    {
        bool IsCompleted { get; }
    }
    //------------------------------------------------------------------------------------------------------------------------------

    public abstract class FairyDisplayObject : Disposable
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(FairyDisplayObject));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        public FairyDisplayObject()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~FairyDisplayObject()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        protected sealed override void RecordDisposing()
        {
            Alloc.RecordDispose(GetType());
        }
    }

    public partial class FairyForm : FairyDisplayObject, IZoneGUIForm, IZoneGUIDialog
    {
        private readonly string guid;
        private readonly BattleUITemplate form;
        private ILayerZoneListener layer;
        private HashMap<string, FairyComponent> nodes = new();
        public string GUID => guid;
        public BattleUITemplate Template => form;
        public IFairyCanvas Canvas { get; }
        public ILayerZoneListener Zone => layer;
        public IZoneGUIForm Form => this;
        public string Name => guid;
        public bool CloseOnClick { get; protected set; }
        public string DialogResult { get; set; }
        public FairyForm(IFairyCanvas canvas, ILayerZoneListener layer, BattleUITemplate form, string guid)
        {
            this.Canvas = canvas;
            this.guid = guid;
            this.layer = layer;
            this.form = form;
        }
        protected override void Disposing()
        {
            this.OnDispose?.Invoke();
            this.OnDispose = null;
            this.OnShown = null;
            this.OnClose = null;
            this.OnNodeClick = null;
            this.OnNodeDataChanged = null;
            this.OnSelectDialog = null;
            foreach (var e in nodes.Values)
            {
                (e as FairyComponent).Dispose();
            }
            nodes.Clear();
            this.layer = null;
        }

        private bool _initialized = false;
        private List<(GUINodeArgs e, string key, bool deep, object zoneVar)> _pendingBindData = new();
        private List<(GUINodeArgs, bool visible)> _pendingSetVisible = new();
        private List<(GUINodeArgs e, string name, int? index, object zoneVar)> _pendingControlNode = new();

        public event Action OnDispose;
        public event UIFormHandler OnShown;
        public event UIFormHandler OnClose;
        public event UINodeHandler OnNodeClick;
        public event UINodeHandler OnNodeDataChanged;
        public event UINodeDataHandler OnNodeBindData;
        public event SelectDialogHandler OnSelectDialog;

        bool IZoneGUIForm.TryGetNode(string name, out IZoneGUINode node)
        {
            var ret = nodes.TryGetValue(name, out var _node);
            node = _node as IZoneGUINode;
            return ret;
        }

        IZoneGUINode IZoneGUIComponent.GetChild(string name)
        {
            if (nodes.TryGetValue(name, out var node))
            {
                return node as IZoneGUINode;
            }
            return null;
        }

        void IZoneGUIForm.Show(bool closeOnClick, Action shown)
        {
            this.CloseOnClick = closeOnClick;
            Canvas.InvokeAsync((async (fgui) =>
            {
                if (fgui.IsDisposing)
                {
                    return;
                }
                await this.form.ForEachMetaAsync(this, async (gui, meta) =>
                {
                    if (meta is UEFairyGUIComponentMeta fgui)
                    {
                        var panel = FairyGUIRuntime.Instance.CreatePanel(fgui);
                        if (panel != null)
                        {
                            await panel.Init(fgui, this);
                            nodes.Put(fgui.EditorName, panel);
                        }
                    }
                });
                _initialized = true;
                foreach (var (e, key, deep, zoneVar) in _pendingBindData)
                {
                    ApplyBindData(e, key, deep, zoneVar);
                }
                _pendingBindData.Clear();
                foreach (var (e, visible) in _pendingSetVisible)
                {
                    ApplySetVisible(e, visible);
                }
                _pendingSetVisible.Clear();
                foreach (var (e, name, index, zoneVar) in _pendingControlNode)
                {
                    ApplyControlNode(e, name, index, zoneVar);
                }
                _pendingControlNode.Clear();
                Zone.QueueTask(() =>
                {
                    shown();
                    this.OnShown?.Invoke(this);
                });
            }));
        }

        void IZoneGUIForm.Close()
        {
            Canvas.Invoke((fgui) =>
            {
                if (fgui.IsDisposing) return;
                OnClose?.Invoke(this);
                this.Dispose();
            });
        }
        void IZoneGUIForm.BindData(GUINodeArgs e, string key, bool deep, object zoneVar)
        {
            Canvas.Invoke((fgui) =>
            {
                if (fgui.IsDisposing) return;
                if (!_initialized)
                {
                    _pendingBindData.Add((e, key, deep, zoneVar));
                    return;
                }
                ApplyBindData(e, key, deep, zoneVar);
            });
        }

        private void ApplyBindData(GUINodeArgs e, string key, bool deep, object zoneVar)
        {
            if (TryFindNode(e, out var node, out var subUrl))
            {
                node.BindData(subUrl, key, zoneVar, deep);
            }
            OnNodeBindData?.Invoke(node, e, key, zoneVar);
        }


        void IZoneGUIForm.SetVisible(GUINodeArgs e, bool visible)
        {
            Canvas.Invoke((fgui) =>
            {
                if (fgui.IsDisposing) return;
                if (!_initialized)
                {
                    _pendingSetVisible.Add((e, visible));
                    return;
                }
                ApplySetVisible(e, visible);
            });
        }

        private void ApplySetVisible(GUINodeArgs e, bool visible)
        {
            if (TryFindNode(e, out var node, out var subUrl))
            {
                node.SetVisible(subUrl, visible);
            }
        }

        void IZoneGUIForm.ControlNode(GUINodeArgs e, string name, int? index, object zoneVar)
        {
            Canvas.Invoke((fgui) =>
            {
                if (fgui.IsDisposing) return;
                if (!_initialized)
                {
                    _pendingControlNode.Add((e, name, index, zoneVar));
                    return;
                }
                ApplyControlNode(e, name, index, zoneVar);
            });
        }

        private void ApplyControlNode(GUINodeArgs e, string name, int? index, object zoneVar)
        {
            if (TryFindNode(e, out var node, out var subUrl))
            {
                node.ControlNode(subUrl, zoneVar);
            }
        }

        public bool TryFindNode(GUINodeArgs evt, out FairyComponent node, out string subUrl)
        {
            subUrl = evt.SubNodeURL;
            if (nodes.TryGetValue(evt.NodeName, out node))
            {
                if (subUrl != null && node.meta.gui_link != null && subUrl.StartsWith(node.meta.gui_link, StringComparison.OrdinalIgnoreCase))
                {
                    subUrl = subUrl.Substring(node.meta.gui_link.Length + 1);
                    if (subUrl.Length > 0 && subUrl[0] == '/')
                    {
                        subUrl = subUrl.Substring(1);
                    }
                }

                return true;
            }

            if (subUrl != null)
            {
                foreach (var e in nodes)
                {
                    if (e.Value.meta.gui_link != null && subUrl.StartsWith(e.Value.meta.gui_link, StringComparison.OrdinalIgnoreCase))
                    {
                        node = e.Value;
                        subUrl = subUrl.Substring(e.Value.meta.gui_link.Length + 1);
                        if (subUrl.Length > 0 && subUrl[0] == '/')
                        {
                            subUrl = subUrl.Substring(1);
                        }

                        return true;
                    }
                }
            }

            subUrl = null;
            return false;
        }

        //-----------------------------------------------------------------------------------------------------
        internal void HandleNodeClick(FairyComponent comp, IFairyNode node)
        {
            OnNodeClick?.Invoke(comp, comp.GetUrl(comp, node));
        }
        public void InvokeNodeClick(FairyComponent comp, IFairyNode node)
        {
            OnNodeClick?.Invoke(comp, comp.GetUrl(comp, node));
        }
    }

    //-----------------------------------------------------------------------------------------------------
    /// <summary>
    /// 这是一个主UI节点，对应一个FGUI组件
    /// </summary>
    public abstract class FairyComponent : FairyDisplayObject, IZoneGUINode
    {
        private HashMap<string, object> binddata = new HashMap<string, object>();
        public FairyForm Form { get; private set; }
        public UEFairyGUIComponentMeta meta { get; private set; }
        public LayerZone layer => Form.Zone?.Layer;
        public ILayerZoneListener Zone => Form.Zone;
        public UEComponentMeta Meta => meta;
        public string Name => meta.EditorName;
        public bool Visible => Root.Visible;
        IZoneGUIForm IZoneGUIComponent.Form => Form;
        public FairyComponent() { }
        internal protected virtual async Task Init(UEFairyGUIComponentMeta meta, FairyForm runtime)
        {
            this.meta = meta;
            this.Form = runtime;
            this.OnNodeClick += Form.HandleNodeClick;
            await this.OnInit(runtime);
        }
        protected override void Disposing()
        {
            this.OnNodeClick -= Form.HandleNodeClick;
            try
            {
                OnDestroy();
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            binddata.Clear();
            Form = null;
            meta = null;
        }
        internal void SetVisible(string subUrl, bool visible)
        {
            if (TryFindChild(subUrl, out var sub))
            {
                sub.Visible = visible;
            }
            OnSetVisible(sub, visible);
        }
        internal void BindData(string subUrl, string key, object zoneVar, bool deep)
        {
            binddata.Put(key, zoneVar);
            if (TryFindChild(subUrl, out var sub))
            {
                sub.BindData(key, zoneVar);
            }
            OnBindData(sub, key, zoneVar);
        }
        internal void ControlNode(string subUrl, object value)
        {
            if (TryFindController(subUrl, out var sub, out var ctrl))
            {
                sub.RunController(ctrl);
                OnController(sub, ctrl, value);
            }
        }
        public bool TryGetBindData(string key, out object value)
        {
            return binddata.TryGetValue(key, out value);
        }
        public object GetBindData(string key)
        {
            return binddata.Get(key);
        }
        public virtual string GetString() => string.Empty;
        public virtual bool GetBool() => false;
        public virtual double GetNumber() => 0;
        public virtual IZoneGUINode GetChild(string name)
        {
            return null;
        }
        public virtual string GetUrl(FairyComponent root, IFairyNode node)
        {
            var sb = new StringBuilder();
            while (node != null && node.Active)
            {
                if (node.Equals(root.Root))
                {
                    sb.Insert(0, $"{root.meta.gui_link}");
                    return sb.ToString();
                }
                sb.Insert(0, $"/{node.Name}");
                node = node.Parent;
            }
            sb.Insert(0, "ui:/");
            return sb.ToString();
        }
        public virtual bool TryFindChild(string subUrl, out IFairyNode node)
        {
            IFairyNode obj = this.Root;
            var paths = subUrl?.Split('/');
            if (paths != null)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    var path = paths[i];
                    if (string.IsNullOrEmpty(path)) continue;
                    if (obj.TryGetChild(path, out var child))
                    {
                        obj = child;
                    }
                    else
                    {
                        node = null;
                        return false;
                    }
                }
            }
            node = obj;
            return true;
        }

        public virtual bool TryFindController(string subUrl, out IFairyNode node, out IFairyController ctrl)
        {
            IFairyNode obj = this.Root;
            var paths = subUrl.Split('/');
            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                if (string.IsNullOrEmpty(path)) continue;
                if (i == paths.Length - 2)
                {
                    if (obj.TryGetController(path, FairyPage.Parse(paths[i + 1]), out ctrl))
                    {
                        node = obj;
                        return true;
                    }
                }
                else
                {
                    if (obj.TryGetController(path, out ctrl))
                    {
                        node = obj;
                        return true;
                    }
                    // if (obj.TryGetChild(path, out var child))
                    // {
                    //     obj = child;
                    // }
                    else
                    {
                        ctrl = null;
                        node = null;
                        return false;
                    }
                }
            }



            node = obj;
            ctrl = null;
            return false;
        }

        public abstract IFairyNode Root { get; }
        protected abstract Task OnInit(FairyForm runtime);
        protected abstract void OnDestroy();
        protected abstract void OnBindData(IFairyNode child, string key, object value);
        protected abstract void OnSetVisible(IFairyNode child, bool value);
        protected abstract void OnController(IFairyNode child, IFairyController ctrl, object value);

        abstract public event Action<FairyComponent, IFairyNode> OnNodeClick;
        public string DialogResult { get; set; }
    }


    public interface IFairyNode
    {
        bool Active { get; }
        string Name { get; }
        IFairyNode Parent { get; }
        bool Visible { get; set; }
        bool TryGetChild(string nodeName, out IFairyNode node);
        bool TryGetController(string nodeName, out IFairyController node);
        bool TryGetController(string controllerName, in FairyPage pageName, out IFairyController controller);
        void RunController(IFairyController controller);
        void BindData(string key, object zoneVar);
    }

    public interface IFairyController
    {
    }

    public struct FairyPage
    {
        public int? index;
        public string name;
        public string remark;

        public static FairyPage Parse(string _pageName)
        {
            FairyPage ret = default;
            try
            {
                var kvt = _pageName.Split(':', 3);
                if (kvt.Length == 3)
                {
                    ret.index = int.Parse(kvt[0]);
                    ret.name = kvt[1];
                    ret.remark = kvt[2];
                }
                else if (kvt.Length == 2)
                {
                    ret.index = int.Parse(kvt[0]);
                    ret.name = kvt[1];
                }
                else if (int.TryParse(_pageName, out var idx))
                {
                    ret.index = idx;
                }
                else
                {
                    ret.name = _pageName;
                }
            }
            catch
            {
                ret.name = _pageName;
            }

            return ret;
        }
    }
}