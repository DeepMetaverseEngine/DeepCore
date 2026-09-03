using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.Display;
using DeepCore.GUI.SceneGraph;
using DeepMetaGame.Data.GUI.Meta;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Display.GUI
{
	[UEInstance(typeof(UEFairyGUIComponentMeta))]
	public class UEFairyGUIComponentNode : UEDisplayNode<UEFairyGUIComponentMeta>
	{
		public UEFairyGUIComponentNode(UIFactory editor, UEFairyGUIComponentMeta e) : base(editor, e)
		{
		}
		protected override void DrawLayout(GraphicsArgs args)
		{
			args.Graphics.DrawString($"{Meta.gui_link}", this.LocalBounds);
		}
	}
}
