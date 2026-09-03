using DeepCore.GUI.Data;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public partial class UEFileNode : UIComponent
    {
        public string FileNodeName { get; private set; }
        public UERoot FileNodeRoot { get; private set; }

        protected override void DecodeChilds(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeChilds(editor, e);
            this.FileNodeName = (e as UEFileNodeMeta).fileName;
            if (!string.IsNullOrEmpty(this.FileNodeName))
            {
                this.FileNodeRoot = editor.CreateFromFile(FileNodeName) as UERoot;
                this.AddChild(FileNodeRoot);
            }
        }

        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            this.Enable = false;
            this.EnableChildren = true;

            if(UIEditorMeta.UIEditorRunTime)
            {
                this.Enable = e.Enable;
                this.EnableChildren = e.EnableChilds;
                this.IsInteractive = true;
            }
        }
    }
}
