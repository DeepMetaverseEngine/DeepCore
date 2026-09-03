using System;
using System.Collections.Generic;
using System.Text;
using DeepCore;
using DeepCore.GUI.Display;
using DeepCore.Xml;
using System.Xml;
using System.IO;
using DeepCore.IO;
using DeepCore.GUI.Data;

namespace DeepCore.GUI.Cell
{
    public class CPJLoader : Disposable
    {
        public string FileName { get; }
        public CPJFileSet File { get; }
        public string DefaultImageName { get; }
        public string DefaultSpriteName { get; }
        public string DefaultMapName { get; }
        public string DefaultWorldName { get; }
        public CPJLoader(string path, CPJFileSet file)
        {
            this.FileName = path;
            this.File = file;
            foreach (string name in file.SprTable.Keys)
            {
                DefaultSpriteName = name;
                break;
            }
            foreach (string name in file.ImgTable.Keys)
            {
                DefaultImageName = name;
                break;
            }
            foreach (string name in file.MapTable.Keys)
            {
                DefaultMapName = name;
                break;
            }
            foreach (string name in file.WorldTable.Keys)
            {
                DefaultWorldName = name;
                break;
            }
        }
        protected override void Disposing()
        {
        }

        public Image LoadImage(string imgpath)
        {
            string output_dir = Path.GetDirectoryName(FileName);
            Image ret = GraphicsDriver.Instance.CreateImage(output_dir + "/" + imgpath);
            return ret;
        }

    }


}
