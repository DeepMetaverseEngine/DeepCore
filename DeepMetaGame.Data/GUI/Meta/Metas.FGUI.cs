using DeepCore.GUI.Data;
using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepMetaGame.Data.GUI.Meta
{

    //------------------------------------------------------------------------------------

    [Desc("FairyGUI绑定", "FairyGUI")]
	[MessageType(BattleConstants.UEFairyGUIComponentMeta)]
	public class UEFairyGUIComponentMeta : UEComponentMeta
	{
		[ResourceID(ResourceType.GUIForm)]
		public string gui_link;//= "ui://Hero/btn_filter";
	}

	//------------------------------------------------------------------------------------
}
