using Cysharp.Threading.Tasks;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.Unity.OnGUI;
using DeepCore.Unity3D.Impl.OnGUI;
using DeepMetaGame.Data.Template;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview.Preview
{
    public class TemplateBattleUIDisplay : PreviewObject<BattleUITemplate>
    {
        private OnGUICanvas canvas;
        protected override void Awake()
        {
            base.Awake();
        }
        protected override void DoInit(BattleUITemplate res)
        {

        }
        protected override void DoReplay()
        {
        }
        protected override void DoUpdate()
        {
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        protected override void OnInitGUI(GUICanvas canvas)
        {
            var factory = new UnityOnGUIFactory();
            base.OnInitGUI(canvas);
            this.canvas = gameObject.AddComponent<OnGUICanvas>();
            if (Data.Forms != null)
            {
                foreach (var ui in Data.Forms)
                {
                    var form = factory.CreateUI(ui);
                    this.canvas.RootNode.AddChild(form);
                    form.Decode();
                }
            }
        }

        protected override void OnDrawGUI()
        {
            try
            {
                GUI.Label(new Rect(0, Screen.height - 24 - 24, 400, 24), $"GUI : {Data}");
            }
            catch (Exception e)
            {
                PLog(e);
            }
        }
        class UnityOnGUIFactory : OnGUIFactory
        {
            public UnityOnGUIFactory() : base(UnityIPC.EditorRootDir)
            {
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
    }


}
