using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.GUI.Data;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Template;
using System;
using System.Threading.Tasks;

namespace DeepMetaGame.Slave.GUI
{
    public interface ZoneGUIRuntime
    {
        IZoneGUIDialog ShowDialog(ILayerZoneListener zone, string guid, BattleUITemplate guiTemplateID);
        IZoneGUIForm ShowForm(ILayerZoneListener zone, string guid, BattleUITemplate guiTemplateID);
    }


    public interface IZoneGUIComponent : IDisposable
    {
        ILayerZoneListener Zone { get; }
        IZoneGUIForm Form { get; }
        string Name { get; }
        IZoneGUINode GetChild(string name);
    }

    public interface IZoneGUIForm : IZoneGUIComponent
    {
        string GUID { get; }
        BattleUITemplate Template { get; }
        bool CloseOnClick { get; }

        bool TryGetNode(string name, out IZoneGUINode node);
        void Show(bool clickOnClose, Action shown);
        void Close();
        void SetVisible(GUINodeArgs e, bool visible);
        void BindData(GUINodeArgs e, string key, bool deep, object zoneVar);
        void ControlNode(GUINodeArgs e, string name, int? index, object zoneVar);

        event Action OnDispose;
        event UIFormHandler OnShown;
        event UIFormHandler OnClose;
        event UINodeHandler OnNodeClick;
        event UINodeHandler OnNodeDataChanged;
        event UINodeDataHandler OnNodeBindData;
    }
    public interface IZoneGUIDialog : IZoneGUIForm
    {
        event SelectDialogHandler OnSelectDialog;
    }

    public interface IZoneGUINode : IZoneGUIComponent
    {
        UEComponentMeta Meta { get; }
        bool Visible { get; }
        object GetBindData(string key);
        string GetString();
        bool GetBool();
        double GetNumber();
        string DialogResult { get; set; }
    }

    public delegate void SelectDialogHandler(IZoneGUIComponent sender, string subName, string result);
    public delegate void UIFormHandler(IZoneGUIForm sender);
    public delegate void UINodeHandler(IZoneGUINode sender, string subName);
    public delegate void UINodeDataHandler(IZoneGUINode sender, GUINodeArgs e, string key, object zonevar);


}
