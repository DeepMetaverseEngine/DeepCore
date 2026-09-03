using DeepCore.GUI.Data;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DeepCore.GUI.Display
{
    //	------------------------------------------------------------------------------------------
    //	-by zhangyifei
    //	------------------------------------------------------------------------------------------

    public abstract class GraphicsDriver
    {
        //------------------------------------------------------------------
        private static GraphicsDriver s_instance = null;
        public static GraphicsDriver Instance { get { return s_instance; } }
        protected GraphicsDriver() { s_instance = this; }
        private char[] space_char = new char[] { ' ', '\t' };
        //------------------------------------------------------------------

        public virtual void Assert(bool cond, string msg) { }
        public virtual void OpenIME(ITextInput input) { }
        public virtual void CloseIME() { }

        //------------------------------------------------------------------

        /// <summary>
        /// 检测是否为表情符号
        /// </summary>
        /// <param name="text"></param>
        /// <param name="i"></param>
        /// <param name="len">表情符号所占长度</param>
        /// <returns></returns>
        public virtual bool IsEmoji(string text, int i, out int len)
        {
            if (i + 1 < text.Length)
            {
                int c0 = text[i];
                int c1 = text[i + 1];
                if (c0 >= 0xD800 && c0 <= 0xDBFF)
                {
                    len = 2;
                    return true;
                }
            }
            len = 1;
            return false;
        }


        /// <summary>
        /// 防止英文单词被切断
        /// </summary>
        /// <param name="regionText"></param>
        /// <param name="splitIndex"></param>
        /// <param name="spellSplitIndex"></param>
        /// <returns></returns>
        public virtual bool TestTextSpellBreak(string regionText, int splitIndex, out int spellSplitIndex)
        {
            if (MatchWordBeginEnd(regionText, splitIndex, out var begin, out var end))
            {
                spellSplitIndex = begin;
                return true;
            }
            spellSplitIndex = splitIndex;
            return false;
        }

        public static bool MatchWordBeginEnd(string txt, int index, out int begin, out int end)
        {
            const int MAX_WORDS_CHAR_COUNT = 8;
            begin = -1;
            end = -1;
            var c = MAX_WORDS_CHAR_COUNT;
            for (int i = index; i >= 0 && c > 0; i--)
            {
                if (IsWord(txt[i]))
                {
                    begin = i;
                }
                else
                {
                    break;
                }
                c--;
            }
            c = MAX_WORDS_CHAR_COUNT;
            for (int i = index; i < txt.Length && c > 0; i++)
            {
                if (IsWord(txt[i]))
                {
                    end = i;
                }
                else
                {
                    break;
                }
                c--;
            }
            if (begin >= 0 && end >= 0 && begin < end)
            {
                return true;
            }
            return false;
        }
        public static bool IsWord(char c)
        {
            if (c >= 'a' && c <= 'z' ||
                c >= 'A' && c <= 'Z' ||
                c >= '0' && c <= '9' ||
                c == '_' ||
                c == '%')
            {
                return true;
            }
            return false;
        }


        abstract public bool TestTextLineBreak(string text, object fontName, float size, TextFontStyle style, TextBorderStyle borderTime, float testWidth, out float realWidth, out float realHeight);
        //------------------------------------------------------------------

        public abstract void ReloadImage(Image img);
        public abstract Image CreateImage(String resource);
        public abstract Task<Image> CreateImageAsync(String resource);
        public abstract Image CreateImage(Stream stream);
        public abstract Image CreateImage(byte[] imageData, int imageOffset, int imageLength);
        public abstract Image CreateRGBImage(int width, int height, uint[] rgba);
        public virtual Image CreateRGBImage(int width, int height)
        {
            uint[] rgba = new uint[width * height];
            return this.CreateRGBImage(width, height, rgba);
        }
        abstract public TextLayer CreateTextLayer(string text, object fontName, float size, TextFontStyle style, TextBorderStyle border);
        abstract public VertexBuffer CreateVertexBuffer(int capacity);

    }
}
