using UnityEngine;

namespace DeepCore.Unity3D.UGUI
{
    public abstract partial class DisplayText : DisplayNode, ITextComponent
    {
        protected DisplayText(string name = "") : base(name)
        {

        }

        public DisplayNode Binding { get { return this; } }
        public abstract string Text { get; set; }
        public abstract int FontSize { get; set; }
        public abstract Color FontColor { get; set; }
        public abstract Vector2 TextOffset { get; set; }
        public abstract GUI.Data.TextAnchor Anchor { get; set; }
        public abstract GUI.Data.FontStyle Style { get; set; }
        public abstract bool IsUnderline { get; set; }

        public abstract Vector2 PreferredSize { get; }
        public abstract Rect LastCaretPosition { get; }

        public abstract void SetBorder(UnityEngine.Color bc, Vector2 distance);
        public abstract void SetShadow(UnityEngine.Color bc, Vector2 distance);
        public abstract void SetFont(UnityEngine.Font font);
    }

    public interface ITextComponent
    {
        DisplayNode Binding { get; }
        string Text { get; set; }
                
        int FontSize { get; set; }
        Color FontColor { get; set; }
        GUI.Data.FontStyle Style { get; set; }
        bool IsUnderline { get; set; }

        Vector2 TextOffset { get; set; }
        GUI.Data.TextAnchor Anchor { get; set; }

        Vector2 PreferredSize { get; }
        Rect LastCaretPosition { get; }

        void SetBorder(UnityEngine.Color bc, Vector2 distance);
        void SetShadow(UnityEngine.Color bc, Vector2 distance);
        void SetFont(UnityEngine.Font font);
    }
}
