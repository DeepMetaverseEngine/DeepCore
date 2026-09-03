using DeepCore.Concurrent;
using System;
using System.Collections.Generic;

namespace DeepCore.GUI.Display
{
    //	------------------------------------------------------------------------------------------
    //	-by zhangyifei
    //	------------------------------------------------------------------------------------------

    public abstract partial class Image : IDisposable
    {
        public static TypeAllocRecorder Alloc { get; private set; } = new TypeAllocRecorder(typeof(Image)) { Verbos = false };
        public static long RefCount { get => Alloc.AllocCount; }
        public static long AliveCount { get => Alloc.ActiveCount; }
        private bool m_disposed = false;
        public string FileName { get; set; }

        protected Image()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~Image()
        {
            if (!m_disposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        public void Dispose()
        {
            if (m_disposed) { return; }
            Alloc.RecordDispose(GetType());
            Disposing();
            m_disposed = true;
        }
        public virtual void ReleaseTexture() { }
        protected abstract void Disposing();

        public string name = "Image";

        public virtual int Width { get { return 0; } }
        public virtual int Height { get { return 0; } }
        public virtual float MaxU { get { return 0; } }
        public virtual float MaxV { get { return 0; } }

        /// <summary>
        /// 从原图片复制像素到当前图片
        /// </summary>
        /// <param name="srci"></param>
        /// <param name="sx"></param>
        /// <param name="sy"></param>
        /// <param name="sw"></param>
        /// <param name="sh"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public abstract void CopyPixels(Image srci, int sx, int sy, int sw, int sh, int dx, int dy);

        /// <summary>
        /// 复制完像素后刷新缓冲区
        /// </summary>
        public abstract void Flush();

        //         #region Count
        //         private static List<Image> mReferenceList = new List<Image>();
        //         public static string DumpImageList()
        //         {
        //             string sout = string.Empty;
        // 
        //             for(int i = mReferenceList.Count - 1; i >= 0; --i)
        //             {
        //                 sout += ((Image)mReferenceList[i]).name;
        //                 sout += "\r\n";
        //             }
        // 
        //             return sout;
        //         }
        //         #endregion

    }

    public struct TImageRegion
    {
        public Image image;
        public float sx, sy, sw, sh;
    }
}
