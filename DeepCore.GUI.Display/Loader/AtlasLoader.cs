using System;
using System.Collections.Generic;
using System.Text;
using DeepCore.GUI.Cell;

namespace DeepCore.GUI.Loader
{
    public partial class AtlasLoader : AbstractLoader
    {
        private CPJResource cpj = null;

        public override void ReleaseTexture()
        {
            if(cpj != null)
            {
                cpj.ReleaseTexture();
            }   
        }

        public AtlasLoader(string name) : base(name) { }

        public override bool IsLoaded()
        {
            return cpj != null;
        }

        public override void Dispose()
        {
            if (cpj != null) 
            {
                cpj.Dispose();
                cpj = null;
            }
            base.Dispose();
        }

        public override Display.Image GetImage(string filePath)
        {
            return null; 
        }

        public override CPJResource GetAtlasResource(string filePath)
        {
            if (FileName == null) { return null; }
            if (cpj == null) { cpj = CPJResource.CreateResource(filePath); }
            return cpj;
        }
    }
}
