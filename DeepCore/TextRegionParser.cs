using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore
{
    public class TextRegion
    {
        public string Text;
        public int Begin;
        public int End;
        protected List<TextRegion> Childs = new List<TextRegion>();
        public TextRegion this[int index] { get => this.Childs[index]; }
        public override string ToString()
        {
            return Text;
        }

        protected virtual TextRegion CreateChild() => new TextRegion();
        protected virtual void OnAttachToParent(TextRegion root) { }

        static TextRegion BeginChild(TextRegion root, int i, TextRegionConfig cfg)
        {
            var child = root.CreateChild();
            child.Begin = i + cfg.Begin.Length;
            return child;
        }
        static void EndChild(TextRegion root, TextRegion child, int i, TextRegionConfig cfg)
        {
            child.End = i - 1;
            child.Text = root.Text.Substring(child.Begin, child.End - child.Begin + 1);
            root.Childs.Add(child);
            root.OnAttachToParent(root);
            GetTextRegions(child, cfg);
        }
        static void GetTextRegions(TextRegion root, TextRegionConfig cfg)
        {
            int deep = 0;
            TextRegion child = null;
            for (int i = 0; i < root.Text.Length; i++)
            {
                if (string.Compare(root.Text, i, cfg.Begin, 0, cfg.Begin.Length) == 0)
                {
                    if (deep == 0)
                    {
                        child = BeginChild(root, i, cfg);
                    }
                    deep++;
                }
                if (string.Compare(root.Text, i, cfg.End, 0, cfg.End.Length) == 0)
                {
                    deep--;
                    if (deep == 0)
                    {
                        EndChild(root, child, i, cfg);
                        child = null;
                    }
                }
            }
        }
        public static TextRegion GetTextRegion(string text, TextRegionConfig cfg)
        {
            var root = new TextRegion() { Text = text, Begin = 0, End = text.Length - 1 };
            GetTextRegions(root, cfg);
            return root;
        }
        public static T GetTextRegion<T>(string text, TextRegionConfig cfg) where T : TextRegion, new()
        {
            var root = new T() { Text = text, Begin = 0, End = text.Length - 1 };
            TextRegion.GetTextRegions(root, cfg);
            return root;
        }
    }

    public class TextRegion<T> : TextRegion where T : TextRegion, new()
    {
        new public T this[int index] { get => base.Childs[index] as T; }
        protected override TextRegion CreateChild() { return new T(); }
    }


    public class TextRegionConfig
    {
        public string Begin = "[";
        public string End = "]";
        public string Split = ",";
        public static readonly TextRegionConfig GET_TYPES = new TextRegionConfig();
    }
    //     public static class TextRegionParser
    //     {
    // 
    //         //DeepCore.HashMap`2[[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[DeepCore.ArrayList`1[[DeepMetaGame.Data.Template.IUnitTemplateAbility, DeepMetaGame.Data, Version=1.2.10.0, Culture=neutral, PublicKeyToken=null]], DeepCore, Version=1.2.17.0, Culture=neutral, PublicKeyToken=null]]
    //         //[DeepCore.ArrayList`1[[DeepMetaGame.Data.Template.IUnitTemplateAbility, DeepMetaGame.Data, Version=1.2.10.0, Culture=neutral, PublicKeyToken=null]]
    //        
    // 
    // 
    //     }
}
