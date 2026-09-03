using UnityEngine;

namespace DeepCore.Unity.ResourceSnap
{
    public class AnimationClipInfo
    {
        public string name;
        public float duration;
        public int durationMS => (int)(duration * 1000);
    }
    public class ParticlesInfo
    {
        public float duration;
        public bool effectLoop;
        public int durationMS => (int)(duration * 1000);
    }
    public class VoxelSceneInfo
    {
        public float centerX;
        public float centerY;
        public float centerZ;
        public float extentsX;
        public float extentsY;
        public float extentsZ;
    }
    public class ObjectResourceInfo
    {
        public string name;
        public VoxelSceneInfo voxel;
        public ParticlesInfo effect;
        public AnimationClipInfo[] animations;
        public static ObjectResourceInfo GetObjectInfo(GameObject go)
        {
            var ret = new ObjectResourceInfo();
            ret.name = go.name;
            if (go.TryGetParticleDuration(out var EffectDurationTime, out var EffectLoop))
            {
                ret.effect = new ParticlesInfo()
                {
                    duration = EffectDurationTime,
                    effectLoop = EffectLoop,
                };
            }
            if (go.TryGetAnimatorStates(out var a1, out var a2, ref ret.animations))
            {
                //ret.animations = anims;
            }
//             if (go.TryGetComponentInChildren<Unity3D.Voxel.VoxelAABB>(out var aabb))
//             {
//                 var b = aabb.GetBounds();
//                 ret.voxel = new VoxelSceneInfo()
//                 {
//                     centerX = b.center.x,
//                     centerY = b.center.y,
//                     centerZ = b.center.z,
//                     extentsX = b.extents.x,
//                     extentsY = b.extents.y,
//                     extentsZ = b.extents.z,
//                 };
//             }
            return ret;
        }
    }


}
