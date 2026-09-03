using DeepCore.GUI.Cell;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    public abstract class UIResource
    {
        private int refCount = 1;
        public int RetainCount => refCount;
        public int Release()
        {
            refCount--;
            if (refCount == 0)
            {
                Destory();
            }
            return refCount;
        }
        public int Retain()
        {
            return refCount++;
        }
        public abstract void Destory();
    }
    public class UIResourceImage : UIResource
    {
        public Image Image { get; }
        public UIResourceImage(string res)
        {
            var src = GraphicsDriver.Instance.CreateImage(res);
            this.Image = src;
        }
        public UIResourceImage(Image res)
        {
            this.Image = res;
        }
        public override void Destory()
        {
            Image.Dispose();
        }
    }
    public class UIResourceCPJ : UIResource
    {
        public CPJResource CPJ { get; }
        public UIResourceCPJ(string res)
        {
            var src = CPJResource.CreateResource(res);
            this.CPJ = src;
        }
        public UIResourceCPJ(CPJResource res)
        {
            this.CPJ = res;
        }
        public override void Destory()
        {
            CPJ.Dispose();
        }
    }
}
