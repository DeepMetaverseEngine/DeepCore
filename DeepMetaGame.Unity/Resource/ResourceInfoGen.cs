using DeepCore.IO;
using DeepCore.Unity;
using DeepCore.Unity.ResourceSnap;
using DeepCore.Unity.ResourceViewer;
using DeepCore.Unity3D.AB;
using DeepCore.Xml;
using DeepMetaGame.Data.Misc;
using System;
using System.IO;
using UnityEngine;

namespace DeepMetaGame.Unity.Resource
{
    public class ResourceInfoGen
    {
        public ResourceInfoGen()
        {
            //ABSystemImpl.RootPath = Path.Combine(DataRootPath, "GameEditor");
        }
        public static bool Gen(string file, string outfile)
        {
            try
            {
                var name = DeepCore.IO.Resource.GetFileNameWithoutExtension(file);
                var wrap = ABSystem.GetWrapGO(file, name, null);
                if (Application.isEditor) { 
                    wrap.gameObject.AddComponent<ResourceInfo>();
                }
                AnimationClipInfo[] clips = null;
                var meta = new ResourceMeta();
                wrap.gameObject.TryGetParticleDurationMS(out meta.TotalEffectTimeMS, out meta.TotalEffectLoop);      
                wrap.gameObject.TryGetAnimatorStates(ref clips);
                meta.Animates = Array.ConvertAll(clips, t => new AnimationMeta()
                {
                    StateName = t.name,
                    DurationMS = t.durationMS,
                });
                var xmeta = XmlUtil.ObjectToXml(meta);
                if (xmeta != null)
                {
                    XmlUtil.SaveXML(outfile, xmeta);
                    return true;
                }
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
            return false;
        }
        public static int GenAll(string rdir, string filter)
        {
            int count = 0;
            var files = CFiles.ListAllFiles(rdir, new FileFilters(filter));
            foreach (var f in files)
            {
                if (Gen(f.FullName, Path.Combine(f.FullName + ".meta")))
                {
                    count++;
                }
            }
            return count;
        }

    }
}
