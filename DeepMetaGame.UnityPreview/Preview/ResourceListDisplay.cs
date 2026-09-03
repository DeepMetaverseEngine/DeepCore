using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor.Prewview;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview.Preview
{
    public class ResourceListDisplay : PreviewObject<PreviewResourceList>
    {
        private readonly List<ResourceView> ResourceDisplays = new List<ResourceView>();
        public static Vector2 ResourceGridSize = new Vector2(5, 5);
        protected override void DoInit(PreviewResourceList resList)
        {
            if (resList.Resources != null)
            {
                foreach (var id in resList.Resources)
                {
                    if (id != null)
                    {
                        var display = IPC.CreateDisplay<ResourceView>(Path.GetFileName($"{id}"));
                        display.LoadRes(id);
                        this.ResourceDisplays.Add(display);
                    }
                }
                RefreshGrid();
            }
        }
        protected override void DoDestory()
        {
            foreach (var id in this.ResourceDisplays)
            {
                id.Dispose();
            }
            base.DoDestory();
        }
        private void RefreshGrid()
        {
            var resources = ResourceDisplays;
            {
                var gw = (int)(Math.Sqrt(resources.Count) + 1);            
                var i = 0;
                for (int x = 0; x < gw; x++)
                {
                    for (int y = 0; y < gw; y++)
                    {
                        if (i < resources.Count)
                        {
                            var res = resources[i];
                            var pos = new Vector3(
                                x * ResourceGridSize.x,
                                0,
                                y * ResourceGridSize.y);
                            res.gameObject.transform.localPosition = pos;
                            res.gameObject.SetActive(true);
                            var collider = res.gameObject.GetOrAddComponent<BoxCollider>();
                            if (collider)
                            {
                                collider.size = new Vector3(ResourceGridSize.x / 2f, 1, ResourceGridSize.y / 2f);
                            }
                        }
                        i++;
                    }
                }
            }
        }

        protected override void OnDrawGUI()
        {
            base.OnDrawGUI();
            try
            {
                var dummy = new DrawGridActionRect(rect =>
                {
                });
                var focus = new DrawGridActionRect(rect =>
                {

                });
                var top = new DrawGridActionRect(rect =>
                {
                    if (GUI.Button(rect, "H+"))
                    {
                        ResourceGridSize.y += 1;
                        RefreshGrid();
                    }
                });
                var bottom = new DrawGridActionRect(rect =>
                {
                    if (GUI.Button(rect, "H-"))
                    {
                        ResourceGridSize.y -= 1;
                        RefreshGrid();
                    }
                });
                var left = new DrawGridActionRect(rect =>
                {
                    if (GUI.Button(rect, "W-"))
                    {
                        ResourceGridSize.x -= 1;
                        RefreshGrid();
                    }
                });
                var right = new DrawGridActionRect(rect =>
                {
                    if (GUI.Button(rect, "W+"))
                    {
                        ResourceGridSize.x += 1;
                        RefreshGrid();
                    }
                });

                var seeedAdd = new DrawGridActionRect(rect =>
                {
                });
                var seeedDec = new DrawGridActionRect(rect =>
                {
                });

                var rect = GUIUtils.DrawGrid(new Vector2(0, Screen.height - 24), new Vector2(64, -24), new DrawGridActionRect[,] {
                    { dummy, bottom, dummy },
                    { left, focus, right, },
                    { seeedAdd, top, seeedDec, },
                });
                rect.y -= 24;
                GUI.Label(rect, $"GRID[{ResourceGridSize.x.ToString("0.0")},{ResourceGridSize.y.ToString("0.0")}]");
            }
            catch { }
        }

        //--------------------------------------------------------------------------------------------------------------------------

        public class ResourceView : DisplayObject
        {
            void Awake()
            {
                UnityIPC.RTG.AddEditorObject(gameObject);
            }
            void Start()
            {
                OnInitHeadText();
            }
            void Update()
            {
                UpdateTime();
            }
            void LateUpdate()
            {
                UpdateHeadText();
                UpdateResource();
            }
        protected override void OnDisposing()
            {
                CleanHeadText();
                CleanResource();
            }
            
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

            #region HeadText

            private Transform childText;
            protected virtual Transform OnInitHeadText()
            {
                if (RTG.TempHeadText)
                {
                    childText = Instantiate(RTG.TempHeadText);
                    childText.SetActive(true);
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
                    var pos = transform.position;
                    //pos.y -= 1f;
                    childText.transform.position = RTG.MainCamera.WorldToScreenPoint(pos, Camera.MonoOrStereoscopicEye.Mono);
                }
            }
            private void CleanHeadText()
            {
                if (childText)
                {
                    Destroy(childText.gameObject);
                    childText = null;
                }
            }
            #endregion

        }

        //--------------------------------------------------------------------------------------------------------------------------
    }


}
