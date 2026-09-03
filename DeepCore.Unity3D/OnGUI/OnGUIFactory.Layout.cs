using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using DeepCore.Unity.OnGUI;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepCore.Unity3D.Impl.OnGUI
{
    public partial class OnGUIFactory
    {
        public override UILayout CreateLayout(UILayoutMeta meta)
        {
            if (meta == null) return null;
            var ret = new OnGUILayout(this, meta);
            //await ret.LoadAsync();
            return ret;
        }
        public override UITextLayer CreateTextLayer(string text, UIFontMeta font, UITextStyleMeta style)
        {
            var ret = new OnGUITextLayer(this, text, font, style);
            //await ret.LoadAsync();
            return ret;
        }
        public override UIImageLayer CreateImageLayer(CPJAtlasMeta meta, UIImageStyleMeta style)
        {
            var ret = new OnGUIImageLayer(this, meta, style);
            //await ret.LoadAsync();
            return ret;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        internal class OnGUILayout : UILayout
        {
            protected Texture2D backcolor;
            public OnGUILayout(UIFactory editor, UILayoutMeta meta) : base(editor, meta)
            {
                backcolor = GUITextureManager.MakeTexture(32, 32, Meta.BackColor.ToUnityColor());
            }
            protected override void Disposing()
            {
                base.Disposing();
                if (backcolor) GameObject.Destroy(backcolor);
            }
            public override void Render(GUI.Display.Graphics g, RectangleF bounds)
            {
                var style = Meta.Style;
                if (style == UILayoutStyle.NULL) return;
                else if (style == UILayoutStyle.COLOR)
                {
                    UnityEngine.GUI.DrawTexture(bounds.ToUnity(), backcolor, ScaleMode.StretchToFill);
                }
                else
                {
                    //UnityEngine.GUI.DrawTexture(bounds.ToUnity(), backcolor, ScaleMode.StretchToFill);
                    base.Render(g, bounds);
                }
            }
        }
        internal class OnGUIImageLayer : UIImageLayer
        {
            public OnGUIImageLayer(UIFactory editor, CPJAtlasMeta atlas, UIImageStyleMeta style) : base(editor, atlas, style)
            {
            }
            public override void Render(GUI.Display.Graphics g, RectangleF bounds)
            {
                //                 if (imageBuffer is UnityImage tex)
                //                 {
                //                     var uv = tex.ToUV(imageRegion);
                //                     if (Style != null)
                //                     {
                //                         bounds = Style.Padding.Cut(bounds);
                //                     }
                //                     if (Style != null)
                //                     {
                //                         UnityEngine.GUI.DrawTextureWithTexCoords(bounds.ToUnity(), tex.Texture2D, uv);
                //                     }
                //                     else
                //                     {
                //                         UnityEngine.GUI.DrawTextureWithTexCoords(bounds.ToUnity(), tex.Texture2D, uv);
                //                     }
                //                 }
                base.Render(g, bounds);
            }
        }
        internal class OnGUITextLayer : UITextLayer
        {
            //private GUIStyle style;
            public OnGUITextLayer(UIFactory editor, string text, UIFontMeta font, UITextStyleMeta meta) : base(editor, text, font, meta)
            {
                //                 if (meta != null)
                //                 {
                //                     style = new GUIStyle();
                //                     style.normal.textColor = Style.TextColor.ToUnityColor();
                //                     style.alignment = meta.Align.ToTextAnchor();
                //                     style.wordWrap = true;
                //                 }
            }
            //             public override void Render(GUI.Display.Graphics g, RectangleF bounds)
            //             {
            //                 if (style != null)
            //                 {
            //                     bounds = Style.Padding.Cut(bounds);
            //                     UnityEngine.GUI.contentColor = Style.TextColor.ToUnityColor();
            //                     UnityEngine.GUI.Box(bounds.ToUnity(), this.Text, style);
            //                 }
            //                 else
            //                 {
            //                     UnityEngine.GUI.Label(bounds.ToUnity(), this.Text);
            //                 }
            //             }
        }


        //-------------------------------------------------------------------------------------------------------------------------

    }


}
