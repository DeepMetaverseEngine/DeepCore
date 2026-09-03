using Code.System.Resource;
using DeepCore.IO;
using DeepCore.Unity.ResourceViewer;
using DeepGame3D.Unity;
using UnityEngine;

namespace IOGame.Client.Unity.IOBattle
{
    public class IOUnityBattleFactory : UnityBattleFactory
    {
        public override IResourceObject GetEffectResource(string file, Transform parent)
        {
            var name = Resource.GetFileNameWithoutExtension(file);
            var wrap = ResourceSystem.GetWrapGO(file, name, null, parent);
            var ret = new IOResObject() { wrap = wrap };
            ret.InitEffect();
            return ret;
        }
        public override IResourceObject GetResourceObject(string file, Transform parent)
        {
            var name = Resource.GetFileNameWithoutExtension(file);
            var wrap = ResourceSystem.GetWrapGO(file, name, null, parent);
            return new IOResObject() { wrap = wrap };
        }
        public override void PlayBGM(string file)
        {
        }
        public override void PlaySound(string file, int durationMS, Vector3? position)
        {
        }
    }

    public struct IOResObject : IResourceObject
    {
        public WrapGO wrap;
        public int wrapTimeMS;
        public bool wrapLoop;
        public GameObject gameObject => wrap.GameObject;
        public int effectDurationMS => wrapTimeMS;
        public bool effectLoop => wrapLoop;
        public void InitEffect()
        {
            wrapTimeMS = EffectReplay.GetParticleDuriationMS(wrap.GameObject, out wrapLoop);
        }
        public void Dispose()
        {
            wrap.Dispose();
        }
    }

}
