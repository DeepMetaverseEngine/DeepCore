using DeepCore.Game3D.Slave.Layer;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using DeepCore.Threading;
using DeepCore.Unity3D.Impl.OnGUI;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Display.GUI;
using DeepMetaGame.Slave.GUI;
using DeepMetaGame.Unity.BattleView;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

namespace DeepMetaGame.Unity.OnGUI
{
    public class UnityZoneOnGUIRuntime : MonoBehaviour, ZoneGUIRuntime
    {
        private MessageActionQueue<UnityZoneOnGUIRuntime> tasks;
        public MessageActionQueue<UnityZoneOnGUIRuntime> OnGUITaskQueue { get { return tasks; } }

        void Awake()
        {
            this.tasks = new();
        }
        void OnGUI()
        {
            tasks?.ProcessMessages(this);
        }
        void OnDestroy()
        {
            tasks?.Dispose();
        }
        //----------------------------------------------------------------------------------------------------
        public IZoneGUIDialog ShowDialog(ILayerZoneListener zone, string guid, BattleUITemplate Data)
        {
            var canvas = gameObject.AddComponent<OnGUIRoot>();
            canvas.Init(this, zone, guid, Data);
            canvas.Form.IsDialog = true;
            return canvas.Form;
        }
        public IZoneGUIForm ShowForm(ILayerZoneListener zone, string guid, BattleUITemplate Data)
        {
            var canvas = gameObject.AddComponent<OnGUIRoot>();
            canvas.Init(this, zone, guid, Data);
            canvas.Form.IsDialog = false;
            return canvas.Form;
        }

        public void ClearAllForm()
        {
            var roots = gameObject.GetComponents<OnGUIRoot>();
            if (roots != null)
            {
                for (int i = 0; i < roots.Length; i++)
                {
                    GameObject.Destroy(roots[i]);
                }
            }
        }
        protected virtual UnityOnGUIFactory CreateFactory(string editorRoot)
        {
            return new UnityOnGUIFactory(editorRoot);
        }
        //----------------------------------------------------------------------------------------------------
        public class UnityOnGUIFactory : OnGUIFactory
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
            public UnityZoneOnGUIRuntime RT { get; private set; }
            public ZoneGUIForm Form { get; private set; }
            public void Init(UnityZoneOnGUIRuntime rt, ILayerZoneListener zone, string guid, BattleUITemplate gui)
            {
                var GUIFactory = rt.CreateFactory(zone.Templates.DataRoot.EditorRoot);
                this.RT = rt;
                this.Form = new OnGUIForm(this, GUIFactory, zone, guid, gui);
                this.RootNode.CameraPos = new DeepCore.Geometry.Vector2(0, 0);
            }
            protected override void OnGUI()
            {
                if (UnityBattleConfig.ENABLE_BATTLE_DEBUG_GUI)
                {
                    base.OnGUI();
                }
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
                base.Disposing();
                GameObject.Destroy(Root);
            }
        }
        //----------------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------------------------
    }
}
