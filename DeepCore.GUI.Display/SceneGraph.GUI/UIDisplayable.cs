using DeepCore.Geometry;
using DeepCore.GUI.Cell;
using DeepCore.GUI.Cell.Game;
using DeepCore.GUI.Data;
using System;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    public abstract class UIDisplayable : Disposable
    {
        public bool IsEditor => Editor.IsEditor;
        public UIFactory Editor { get; }
        public UIDisplayable(UIFactory editor)
        {
            this.Editor = editor;
        }
        //public abstract Task LoadAsync();
        public abstract void Render(Graphics g, RectangleF bounds);


    }
}
