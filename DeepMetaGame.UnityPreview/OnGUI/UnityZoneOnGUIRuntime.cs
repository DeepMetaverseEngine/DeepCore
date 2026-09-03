using DeepCore.Game3D.Slave.Layer;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using DeepCore.Unity3D.Impl.OnGUI;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Display.GUI;
using DeepMetaGame.Slave.GUI;
using UnityEngine;

namespace DeepMetaGame.Unity.OnGUI
{
    public class UnityZoneOnGUIRuntime : MonoBehaviour, ZoneGUIRuntime
    {
        public static TemplateManager Templates { get; private set; }
        public static void Init(TemplateManager battle)
        {
            Templates = battle;
        }

        void OnGUI()
        {
            //UnityEngine.GUI.Label(new Rect(100, 100, 200, 40), "ZoneOnGUIRuntime");
        }
        //----------------------------------------------------------------------------------------------------
        public IZoneGUIDialog ShowDialog(ILayerZoneListener zone, string guid, BattleUITemplate Data)
        {
            var canvas = gameObject.AddComponent<OnGUIRoot>();
            canvas.Init(zone, guid, Data);
            canvas.Form.IsDialog = true;
            return canvas.Form;
        }
        public IZoneGUIForm ShowForm(ILayerZoneListener zone, string guid, BattleUITemplate Data)
        {
            var canvas = gameObject.AddComponent<OnGUIRoot>();
            canvas.Init(zone, guid, Data);
            canvas.Form.IsDialog = false;
            return canvas.Form;
        }

        public void ClearAllForm()
        {
            var roots = gameObject.GetComponents<OnGUIRoot>();
            if(roots != null) 
            {
                for (int i = 0; i < roots.Length; i++)
                {
                    GameObject.Destroy(roots[i]);
                }
            }
        }

        //----------------------------------------------------------------------------------------------------
        class UnityOnGUIFactory : OnGUIFactory
        {
            public UnityOnGUIFactory(string rootDir) : base(rootDir)
            {
                this.IsEditor = false;
            }
            protected override UEComponentNode DoCreateUI(UEComponentMeta meta)
            {
//                 var bnode = BattleUIFactory.CreateUI(this, meta);
//                 if (bnode != null)
//                 {
//                     return bnode;
//                 }
                return base.DoCreateUI(meta);
            }
        }
        class OnGUIRoot : OnGUICanvas
        {
            public ZoneGUIForm Form { get; private set; }
            public void Init(ILayerZoneListener zone, string guid, BattleUITemplate gui)
            {
                var GUIFactory = new UnityOnGUIFactory(Templates.DataRoot.EditorRoot);
                this.Form = new OnGUIForm(this, GUIFactory, zone, guid, gui);
                this.RootNode.CameraPos = new DeepCore.Geometry.Vector2(0, 0);
            }
        }
        class OnGUIForm : ZoneGUIForm
        {
            private OnGUIRoot Root { get; }
            public override DisplayNode RootNode => Root.RootNode;
            public OnGUIForm(OnGUIRoot root, UIFactory factory, ILayerZoneListener zone, string guid, BattleUITemplate gui) : base(factory, zone, guid, gui)
            {
                this.Root = root;
            }
            protected override void Disposing()
            {
                GameObject.Destroy(Root);
            }
        }
        //----------------------------------------------------------------------------------------------------
#if false
        class OnGUIForm : OnGUICanvas, IZoneGUIForm
        {
            public ILayerZoneListener Zone { get; private set; }
            public string Name { get ; private set; }
            public UnityOnGUIFactory GUIFactory { get; private set; }
            public IZoneGUIForm Form { get => this; }
            public BattleUITemplate Template { get; private set; }
            public void Init(ILayerZoneListener zone, string guid, BattleUITemplate gui)
            {
                this.Name = guid;
                this.Zone = zone;
                this.Template = gui;
                this.GUIFactory = new UnityOnGUIFactory(Templates.DataRoot.EditorRoot);
                this.GUIFactory.OnCreateNode += GUIFactory_OnCreateNode;
                if (Template.Forms != null)
                {
                    foreach (var ui in Template.Forms)
                    {
                        var form = GUIFactory.CreateUI(ui);
                        this.RootNode.AddChild(form);
                        form.Decode();
                    }
                }
            }
            public void Show(Action shown)
            {
                this.Invoke(() =>
                {
                    Zone.QueueTask(() =>
                    {
                        shown();
                        OnShown?.Invoke(this);
                    });
                });
            }

            private void GUIFactory_OnCreateNode(UEComponentMeta arg1, UEComponentNode arg2)
            {
                if (arg2 is UEComponentNode child)
                {
                    var comp = child.Components.AddComponent<OnGUINode>();
                    comp.Init(this);
                    OnAddChild(child, comp);
                }
            }
            protected virtual void OnAddChild(UEComponentNode childNode, OnGUINode child)
            {
                childNode.Rect.MouseClick += (sender, args) =>
                {
                    OnChildNodeClick(child);
                }; 
                if (childNode.Components.TryGetComponentAs<InputComponent>(out var input))
                {
                    input.OnTextChanged += (sender, newText, oldText) =>
                    {
                        OnChildNodeDataChanged(child);
                    };
                }
            }
            protected virtual void OnChildNodeClick(OnGUINode child)
            {
                Zone.QueueTask(() =>
                {
                    OnNodeClick?.Invoke(child);
                });
            }
            protected virtual void OnChildNodeDataChanged(OnGUINode child)
            {
                Zone.QueueTask(() =>
                {
                    OnNodeDataChanged?.Invoke(child);
                });
            }
            public void Close()
            {
                Zone.QueueTask(() =>
                {
                    OnClose?.Invoke(this);
                    this.Invoke(() =>
                    {
                        GameObject.Destroy(this);
                    });
                });
            }
            public IZoneGUINode GetChild(string name)
            {
                var node = this.RootNode.FindNodeByName(name);
                return node?.Components.GetComponentAs<OnGUINode>();
            }
            public bool TryGetNode(string name, out IZoneGUINode node)
            {
                var enode = this.RootNode.FindNodeByName(name);
                var comp = enode?.Components.GetComponentAs<OnGUINode>();
                node = comp;
                return comp != null;
            }
            public event UIFormHandler OnShown;
            public event UIFormHandler OnClose;
            public event UINodeHandler OnNodeClick;
            public event UINodeHandler OnNodeDataChanged;
        }
             //----------------------------------------------------------------------------------------------------
        class OnGUIDialog : OnGUIForm, IZoneGUIDialog
        {
            protected override void OnChildNodeClick(OnGUINode child)
            {
                base.OnChildNodeClick(child);
                if (!string.IsNullOrEmpty(child.Meta.DialogResult))
                {
                    Zone.QueueTask(() =>
                    {
                        this.OnSelectDialog?.Invoke(child, child.Meta.DialogResult);
                        this.Close();
                    });
                }
            }
            public event SelectDialogHandler OnSelectDialog;
        }
#endif

        //----------------------------------------------------------------------------------------------------
    }
}
