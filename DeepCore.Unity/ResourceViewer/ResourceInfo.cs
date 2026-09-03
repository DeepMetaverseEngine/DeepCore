using DeepCore.Unity;
using DeepCore.Unity.ResourceSnap;
using System;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity.ResourceViewer
{
    public class ResourceInfo : MonoBehaviour
    {
        public float EffectDurationTime;
        public bool EffectLoop;
        public AnimationClipInfo[] AnimateStates;
        [SerializeField]
        private Animation OwnerAnimation;
        [SerializeField]
        private Animator OwnerAnimator;
        [SerializeField]
        private ParticleSystem OwnerParticleSystem;
        [SerializeField]
        private Material OwnerMaterial;

        private string info;
        public override string ToString()
        {
            return info;
        }

        void Awake()
        {
        }
        void Start()
        {
            this.gameObject.TryGetParticleDuration(out EffectDurationTime, out EffectLoop);
            this.gameObject.TryGetAnimatorStates(out OwnerAnimator, out OwnerAnimation, ref AnimateStates);
        }
        public void Refresh()
        {
            var sb = new StringBuilder();
            if (this.gameObject.TryGetComponentInChildren<SkinnedMeshRenderer>(out var render))
            {
                OwnerMaterial = render.sharedMaterial;
            }
            if (this.gameObject.TryGetComponentInChildren<ParticleSystem>(out OwnerParticleSystem))
            {
                sb.AppendLine("----EffectMS----");
                this.gameObject.TryGetParticleDurationMS(out var ms, out var loop);
                sb.AppendLine($"duration:{ms}  loop:{loop}");
            }
            if (gameObject.TryGetAnimatorStates(out OwnerAnimator, out OwnerAnimation, ref AnimateStates))
            {
                sb.AppendLine("----Animate----");
                foreach (var clip in AnimateStates)
                {
                    sb.AppendLine(clip.name + " : " + clip.durationMS);
                }
            }
            if (gameObject.TryGetComponentInChildren<SkinnedMeshRenderer>(out var skr, true))
            {
                sb.AppendLine("----Bones----");
                foreach (var bone in skr.bones)
                {
                    sb.AppendLine(bone.name);
                }
            }
            info = sb.ToString();
        }



        /*
        public static bool Gen(string file, string outfile)
        {
            try
            {
                var name = Resource.GetFileNameWithoutExtension(file);
                var wrap = ResourceSystem.GetWrapGO(file, name, null, null);
                if (Application.isEditor) { wrap.GameObject.AddComponent<ResourceInfo>(); }
                var meta = new ResourceMeta();
                wrap.GameObject.TryGetParticleDuriationMS(out meta.TotalEffectTimeMS, out meta.TotalEffectLoop);
                wrap.GameObject.TryGetAnimatorStates(out var clips);
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
        }*/


        public static Texture2D GetAssetPreview(GameObject obj)
        {
            return GetAssetPreview(obj, 128, 128, new Color(0, 0, 0, 1f));
        }
        public static Texture2D GetAssetPreview(GameObject obj, int _width, int _height, Color backColor)
        {
            GameObject clone = GameObject.Instantiate(obj);
            Transform cloneTransform = clone.transform;
            cloneTransform.position = new Vector3(-1000, -1000, -1000);
            //cloneTransform.localRotation = new Quaternion(0, 0, 0, 1);

            Transform[] all = clone.GetComponentsInChildren<Transform>();
            foreach (Transform trans in all)
            {
                trans.gameObject.layer = 21;
            }

            Bounds bounds = GetAssetBounds(clone);
            Vector3 Min = bounds.min;
            Vector3 Max = bounds.max;
            GameObject cameraObj = new GameObject("render camera");
            cameraObj.transform.position = new Vector3(cloneTransform.position.x, (Max.y + Min.y) / 2f, Max.z + (Max.z - Min.z));
            Vector3 center = new Vector3(cloneTransform.position.x, (Max.y + Min.y) / 2f, cloneTransform.position.z);
            cameraObj.transform.LookAt(center);

            var renderCamera = cameraObj.AddComponent<UnityEngine.Camera>();
            renderCamera.backgroundColor = backColor;// new Color(0.8f, 0.8f, 0.8f, 1f);
            renderCamera.clearFlags = CameraClearFlags.Color;
            renderCamera.cameraType = CameraType.Preview;
            renderCamera.cullingMask = 1 << 21;
            int angle = (int)(Mathf.Atan2((Max.y - Min.y) / 2, (Max.z - Min.z)) * 180 / 3.1415f * 2);
            renderCamera.fieldOfView = angle;

            RenderTexture texture = new RenderTexture(_width, _height, 0, RenderTextureFormat.Default);
            renderCamera.targetTexture = texture;

            renderCamera.RenderDontRestore();

            RenderTexture tex = new RenderTexture(_width, _height, 0, RenderTextureFormat.Default);
            Graphics.Blit(texture, tex);

            int width = tex.width;
            int height = tex.height;
            Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);
            try
            {
                RenderTexture.active = tex;
                texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture2D.Apply();
            }
            finally { RenderTexture.active = null; }

            UnityEngine.Object.DestroyImmediate(clone);
            UnityEngine.Object.DestroyImmediate(cameraObj);
            UnityEngine.Object.DestroyImmediate(tex);


            return texture2D;
        }
        /// <summary>
        /// 获得某物体的bounds
        /// </summary>
        /// <param name="obj"></param>
        private static Bounds GetAssetBounds(GameObject obj)
        {
            Vector3 Min = new Vector3(99999, 99999, 99999);
            Vector3 Max = new Vector3(-99999, -99999, -99999);
            MeshRenderer[] renders = obj.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renders.Length; i++)
            {
                if (renders[i].bounds.min.x < Min.x)
                    Min.x = renders[i].bounds.min.x;
                if (renders[i].bounds.min.y < Min.y)
                    Min.y = renders[i].bounds.min.y;
                if (renders[i].bounds.min.z < Min.z)
                    Min.z = renders[i].bounds.min.z;

                if (renders[i].bounds.max.x > Max.x)
                    Max.x = renders[i].bounds.max.x;
                if (renders[i].bounds.max.y > Max.y)
                    Max.y = renders[i].bounds.max.y;
                if (renders[i].bounds.max.z > Max.z)
                    Max.z = renders[i].bounds.max.z;
            }

            Vector3 center = (Min + Max) / 2;
            Vector3 size = new Vector3(Max.x - Min.x, Max.y - Min.y, Max.z - Min.z);
            return new Bounds(center, size);
        }

    }



}
