using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor.Prewview;
using DeepMetaGame.Unity.Preview.Preview;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeepMetaGame.Unity.Preview.Resource
{
    public class ResourceView : DisplayObject
    {
        public static ResourceProxy Proxy { get => ResourceProxy.Proxy; }
        //--------------------------------------------------------------------------------------------------------------------------
        void Awake()
        {
            UnityIPC.RTG.AddEditorObject(gameObject);
        }
        void Start()
        {
            OnInitHeadText();
            RefreshGrid();
        }
        void Update()
        {
            UpdateTime();
            UpdateHeadText();
            UpdateResource();
        }
        void OnDestroy()
        {
            //CleanResource();
        }
        protected override void OnDisposing()
        {

        }
        //--------------------------------------------------------------------------------------------------------------------------

        #region Time

        public double PassTimeMS { get => passTime; }
        public float IntervalMS { get => interval; }

        private double startTime;
        private double lastTime;
        private double passTime;
        private float interval = 0;

        public void ResetTime()
        {
            startTime = (Time.timeSinceLevelLoad * 1000);
            interval = 0;
            passTime = 0;
            lastTime = 0;
        }
        private void UpdateTime()
        {
            lastTime = passTime;
            passTime = (Time.timeSinceLevelLoad * 1000 - startTime);
            interval = (float)(passTime - lastTime);
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------
        #region HeadText

        private Transform childText;
        protected virtual Transform OnInitHeadText()
        {
            if (RTG.TempHeadText)
            {
                childText = Instantiate(RTG.TempHeadText);
                childText.SetActive(false);

                if (childText && RTG.NodeHUD)
                {
                    childText.SetParent(RTG.NodeHUD, false);
                    RTG.SetHeadText(childText.gameObject, name);
                }
                return childText;
            }
            return null;
        }
        protected virtual void UpdateHeadText()
        {
            if (childText)
            {
                if (RTG.TargetObject == gameObject)
                {
                    childText.SetActive(true);
                    var pos = transform.position;
                    //pos.y -= 1f;
                    childText.transform.position = RTG.MainCamera.WorldToScreenPoint(pos, Camera.MonoOrStereoscopicEye.Mono);
                }
                else
                {
                    childText.SetActive(false);
                }
            }
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------
        #region Resource

        private List<IViewResource> resources = new List<IViewResource>();
        private ParticleSystem[] pss;
        private double totalTime;
        public string ResName { get; private set; } = "";
        public PreviewResource File { get; private set; }
        public IViewResource LoadRes(PreviewResource file)
        {
            try
            {
                this.File = file;
                var res = UnityIPC.RTG.LoadResource(file.ResID, file.ResType, this);
                if (res != null)
                {
                    res.transform.SetParent(this.transform, false);
                    ResName = file.ResID;
                    resources.Add(res);
                    totalTime = PlayEffect(res);
                    //res.PlaySound();
                    return res;
                }
                res = UnityIPC.RTG.LoadResource(file.FullName, file.ResType, this);
                if (res != null)
                {
                    res.transform.SetParent(this.transform, false);
                    ResName = file.FullName;
                    resources.Add(res);
                    totalTime = PlayEffect(res);
                    //res.PlaySound();
                    return res;
                }
            }
            catch (Exception e)
            {
                UnityIPC.PLog(e);
            }
            return null;
        }
        private void CleanResource()
        {
            try
            {
                foreach (var res in resources)
                {
                    res.Dispose();
                }
                resources.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        private void UpdateResource()
        {
            if (PassTimeMS > totalTime + 1000)
            {
                ResetTime();
                foreach (var res in resources)
                {
                    PlayEffect(res);
                }
            }
        }
        public float PlayEffect(IViewResource res, int effectTimeMS = 0, IViewResource binding = null)
        {
            if (res.resType.AnyFlag(ResourceType.Sound_All))
            {
                res.PlaySound(res.resType);
            }
            if (res?.gameObject != null)
            {
                res.gameObject.PlayParticle();
                if (effectTimeMS > 0)
                {
                    return effectTimeMS;
                }
                else if (effectTimeMS == 0 && res.gameObject.TryGetParticleDurationMS(out var durationMS, out var loop))
                {
                    return durationMS;
                }
                else
                {
                    return 1000;
                }
            }
            return 0;
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------
        public void RefreshGrid()
        {
            //this.transform.localRotation = Quaternion.Euler(0f, 180, 0f);
            var collider = gameObject.GetOrAddComponent<BoxCollider>();
            if (collider)
            {
                collider.size = new Vector3(Proxy.ResourceGridSize.x / 2f, 1, Proxy.ResourceGridSize.y / 2f);
            }
        }
     
    }
}
