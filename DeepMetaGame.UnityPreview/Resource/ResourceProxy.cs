using DeepCore;
using DeepCore.IO;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepCore.Xml;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor.Prewview;
using DeepMetaGame.Unity.Preview.Preview;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeepMetaGame.Unity.Preview.Resource
{
    public class ResourceProxy : UnityIPC
    {
        public static ResourceProxy Proxy { get; private set; }

        public Vector2 ResourceGridSize = new Vector2(5, 5);
        //public readonly List<string> ResourceList = new List<string>();
        public string ExtensionName = ".ab";
        private readonly List<ResourceView> ResourceDisplays = new List<ResourceView>();

        protected override void Awake()
        {
            Proxy = this;
            base.Awake();
        }
        protected override void Start()
        {
            Host = null;
            base.Start();
            RTG.OnTargetSelectChanged += RTG_OnTargetEditorObjectChanged;
            RTG.OnTargetPropertyChanged += RTG_OnTargetEditorPropertyChanged;
            //             var reses = RTG.ListResources();
            //             foreach (var res in reses)
            //             {
            //                 Debug.Log($"{res.ResID} : {res.ResName}");
            //             }

            if (CommandArgs.TryGetValue("-ext", out var _ext))
            {
                ExtensionName = _ext;
            }
            else if (string.IsNullOrWhiteSpace(ExtensionName))
            {
                ExtensionName = ".ab";
            }

            var ResourceList = new List<PreviewResource>();
            var ResType = ResourceType.Any;
            if (CommandArgs.TryGetAs<ResourceType>("-resType", out var _resType))
            {
                ResType = _resType;
            }
            try
            {
                var json = GetClipboardTransform("previewlist.json");
                if (CommandArgs.TryGetAsBool("-resList", out var _resList) && _resList)
                {
                    var list = XmlUtil.JsonToObject<ArrayList<PreviewResource>>(json);
                    ResourceList.AddRange(list);
                }
                else if (CommandArgs.TryGetAsBool("-resBuffer", out var _buffer) && _buffer)
                {
                    ResourceList.AddRange(json.Split(';').ConvertAll(t => new PreviewResource() { FullName = t, ResType = ResType }));
                }
                else if (CommandArgs.TryGetValue("-resID", out var _resID) && !string.IsNullOrEmpty(_resID))
                {
                    ResourceList.AddRange(_resID.Split(',').ConvertAll(t => new PreviewResource() { FullName = t, ResType = ResType }));
                }
                else if (CommandArgs.TryGetValue("-file", out var _file) && File.Exists(_file))
                {
                    ResourceList.Add(new PreviewResource() { FullName = _file, ResType = ResType });
                }
                else if (CommandArgs.TryGetValue("-dir", out var _dir) && Directory.Exists(_dir))
                {
                    var files = CFiles.ListAllFiles(_dir, f => f.Extension.EndsWith(ExtensionName, StringComparison.OrdinalIgnoreCase), true);
                    ResourceList.AddRange(files.ConvertAll(f => f.FullName).ConvertAll(t => new PreviewResource() { FullName = t, ResType = ResType }));
                }
                else
                {
                    var list = XmlUtil.JsonToObject<ArrayList<PreviewResource>>(json);
                    ResourceList.AddRange(list);
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
                throw;
            }
            if (CommandArgs.TryGetAsInt("-grid", out var _grid))
            {
                ResourceGridSize = new Vector2(_grid, _grid);
            }
            else
            {
                ResourceGridSize = new Vector2(5, 5);
            }

            this.ResourceDisplays.Clear();
            if (ResourceList != null)
            {
                foreach (var id in ResourceList)
                {
                    if (id != null)
                    {
                        var display = CreateDisplay<ResourceView>($"{id}");
                        display.LoadRes(id);
                        this.ResourceDisplays.Add(display);
                    }
                }
            }

            RefreshGrid();

            RTG.MainCamera.clearFlags = CameraClearFlags.SolidColor;
            RTG.MainCamera.backgroundColor = Color.black;

        }

        //---------------------------------------------------------------------------
        public void FocusDefault()
        {
            if (ResourceDisplays.Count > 0)
            {
                //TimeTasks.AddTimeDelayMS(1000, t =>
                //{
                var index = ResourceDisplays.Count > 1 ? ResourceDisplays.Count / 2 : 0;
                var first = ResourceDisplays[index];
                if (first != null)
                {
                    RTG.TargetObject = (first.gameObject);
                    RTG.LookAt(first.transform);
                }
                //});
            }
        }
        private void RefreshGrid()
        {
            if (gameObject.TryGetComponentsInChildren<ResourceView>(out var resources))
            {
                var gw = (int)(Math.Sqrt(resources.Length) + 1);
                RTG.TempGround.localScale = new Vector3(
                    gw * ResourceGridSize.x,
                    1,
                    gw * ResourceGridSize.y);
                RTG.TempGround.position = new Vector3(
                    gw * ResourceGridSize.x / 2f,
                    0,
                    gw * ResourceGridSize.y / 2f);
                var i = 0;
                for (int x = 0; x < gw; x++)
                {
                    for (int y = 0; y < gw; y++)
                    {
                        if (i < resources.Length)
                        {
                            var res = resources[i];
                            var pos = new Vector3(
                                x * ResourceGridSize.x,
                                0,
                                y * ResourceGridSize.y);
                            res.gameObject.transform.localPosition = pos;
                            res.gameObject.SetActive(true);
                            res.RefreshGrid();
                        }
                        i++;
                    }
                }
            }
        }
        private void RTG_OnTargetEditorPropertyChanged(GameObject obj)
        {
            if (obj != null && obj.TryGetComponent<ResourceView>(out var edit))
            {

            }
        }

        private void RTG_OnTargetEditorObjectChanged(GameObject _old, GameObject _new)
        {
            if (_new != null && _new.TryGetComponent<ResourceView>(out var nedit))
            {
                SetSelectedText(nedit);
                foreach (var display in ResourceDisplays)
                {
                    if (display.gameObject != _new.gameObject)
                    {
                        display.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                SetSelectedText(null);
                foreach (var display in ResourceDisplays)
                {
                    display.gameObject.SetActive(true);
                }
            }

        }

        //---------------------------------------------------------------------------

        private string selectedText = "";
        private ResourceView selectedRes;
        public void SetSelectedText(ResourceView res)
        {
            if (res != null)
            {
                selectedText = res.ResName;
                selectedRes = res;
            }
            else
            {
                selectedText = string.Empty;
                selectedRes = null;
            }
        }
        protected override void OnDrawGUI()
        {
            if (Input.GetKeyDown(KeyCode.C) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                GUIUtility.systemCopyBuffer = selectedText;
            }
            base.OnDrawGUI();
            try
            {
                if (GUI.Button(new Rect(0, Screen.height - 24, Screen.width, 24), selectedText))
                {
                    GUIUtility.systemCopyBuffer = selectedText;
                    if (selectedRes != null)
                    {
                        RTG.LookAt(selectedRes.transform);
                        //RTG.TargetObject = selectedRes.gameObject;
                    }
                }
                var dummy = new DrawGridActionRect(rect =>
                {
                });
                var focus = new DrawGridActionRect(rect =>
                {
                    if (GUI.Button(rect, "居中"))
                    {
                        FocusDefault();
                    }
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
                    if (GUI.Button(rect, "S+"))
                    {
                        if (Time.timeScale >= 1)
                        {
                            Time.timeScale += 1;
                        }
                        else
                        {
                            Time.timeScale *= 2f;
                        }
                    }
                }); 
                var seeedDec = new DrawGridActionRect(rect =>
                {
                    if (GUI.Button(rect, "S-"))
                    {
                        if (Time.timeScale > 1)
                        {
                            Time.timeScale -= 1;
                        }
                        else
                        {
                            Time.timeScale /= 2f;
                        }
                    }
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

        protected override void OnInitGUI(GUICanvas canvas)
        {
            var partWindow = new ResourceListWindow(this);
            partWindow.Position = new Vector2(Screen.width - partWindow.Width, 100);
            //partWindow.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            partWindow.AnchorPadding = new Padding(0, 100, 0, 60);
            RootCanvas.AddChild(partWindow);
        }

        public class ResourceListWindow : GUIWindow
        {
            public ResourceListWindow(ResourceProxy view)
            {
                this.Text = "资产列表";
                var panel = new GUIPanel();
                panel.Dock = DockStyle.Fill;
                var sx = 0;
                var sy = 0;
                var sh = 26;
                var tex_copy = view.Textures.MakeAssemblyTexture(this.GetType().Assembly, "icon_common_19.png");
                var tex_play = view.Textures.MakeAssemblyTexture(this.GetType().Assembly, "icon_simpleshape_23.png");
                {
                    foreach (var res in view.ResourceDisplays)
                    {
                        var head = new GUITextField()
                        {
                            Bounds = new Rect(sx, sy, 160, sh),
                            Text = $"{res}",
                        };
                        panel.AddChild(head);
                        var btn = new GUIButton()
                        {
                            Bounds = new Rect(sx + 160, sy, 20, sh),
                            Image = tex_play,
                            Tooltip = "复制文本到剪贴板",
                        };
                        btn.Click += new Action<GUIButton>(btn =>
                        {
                            GUIUtility.systemCopyBuffer = res.File.ResID;
                            RTG.LookAt(res.transform);
                        });
                        panel.AddChild(btn);
                        sy += sh + 1;
                    }
                }
                this.Bounds = new Rect(0, 100, 200, Math.Min(sy + 60, Screen.height - 160));
                this.AddChild(panel);
            }

        }
        //---------------------------------------------------------------------------
    }



}
