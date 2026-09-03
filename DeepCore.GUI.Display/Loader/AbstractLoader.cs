using System;
using DeepCore.GUI.Display;
using DeepCore.GUI.Cell;

namespace DeepCore.GUI.Loader
{
    public abstract partial class AbstractLoader : IDisposable
    {
        readonly private string mFileName;

        public AbstractLoader(string name)
        {
            mFileName = name;
        }

        public virtual string FileName
        {
            get { return mFileName; }
        }
        public abstract void ReleaseTexture();
        public abstract Image GetImage(string filePath);
        public abstract CPJResource GetAtlasResource(string filePath);
        public abstract bool IsLoaded();

        public virtual void Dispose()
        {
            //mFileName = null;
        }
    }
}