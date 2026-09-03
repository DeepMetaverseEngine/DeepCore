using DeepCore.GUI.Data;
using UnityEngine;

namespace DeepCore.Unity3D.Platform
{
#if HZUI
    public interface IUnityPlatform
    {
        Texture2D SysFontTexture(
            string text,
            bool readable,
            TextFontStyle style,
            float fontSize,
            uint fontColor,
            TextBorderStyle borderTime,
            uint borderColor,
            Vector2 expectSize,
            out int boundW,
            out int boundH);

        bool TestTextLineBreak(string text, float size, TextFontStyle style,
            TextBorderStyle borderTime,
            float testWidth,
            out float realWidth,
            out float realHeight);

        void CopyPixels(Texture2D src, int sx, int sy, int sw, int sh, Texture2D dst, int dx, int dy);

        void OpenIME(DeepCore.GUI.UI.UITextInput input);
        void CloseIME();
#if MPQ
        bool NativeDecompressFile(MPQUpdater updater, MPQUpdater.RemoteFileInfo zip_file, MPQUpdater.RemoteFileInfo mpq_file, AtomicLong current_unzip_bytes);
        bool NativeDecompressMemory(ArraySegment<byte> src, ArraySegment<byte> dst);
#endif
}
#endif
}
