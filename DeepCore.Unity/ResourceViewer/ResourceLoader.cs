using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeepCore.Unity.ResourceViewer
{
    public class ResourceLoader : MonoBehaviour
    {
        public string ResourceSingleFile = @"\res\voxel_effect\ammopowerupicon.ab";
        public string ResourceDirectory = @"\res\voxel_effect";
        public string ResourceRoot = @"D:\dev\Kyberdyne\hopeProjectn\OpenCards\data\GameEditor";

        public string ResourceFileExtension = ".ab";
        public float ResourceGridSize = 5;
        public float ResourceTextScale = 0.01f;
        public bool ResourceSingleView = true;
        //-------------------------------------------------------------------------------------------------------------------------
        private string _selectedText = string.Empty;
        private GameObject _mainCamera;
        private GameObject _land;
        private List<Tuple<string, GameObject>> _resources = new List<Tuple<string, GameObject>>();
        private int _singleViewIndex = 0;
        //-------------------------------------------------------------------------------------------------------------------------
        // Start is called before the first frame update
        void Start()
        {
            var args = Environment.GetCommandLineArgs();
            var prop = Properties.ParseArgs(args);
            if (prop.Count > 0)
            {
                if (prop.TryGetAs<float>("-grid", out var grid))
                {
                    ResourceGridSize = grid;
                }
                if (prop.TryGetAs<float>("-textScale", out var textScale))
                {
                    ResourceTextScale = textScale;
                }
                if (prop.TryGetValue("-ext", out var ext))
                {
                    ResourceFileExtension = ext;
                }
                if (prop.TryGetAsBool("-singleView", out var singleView))
                {
                    ResourceSingleView = singleView;
                }
                //-----------------------------------------------------------------------
                if (prop.TryGetValue("-root", out var root) && Directory.Exists(root))
                {
                    ResourceRoot = Path.GetFullPath(root);
                }
                else if (Application.platform != RuntimePlatform.WindowsEditor)
                {
                    ResourceRoot = System.Environment.CurrentDirectory;
                }
                if (prop.TryGetValue("-file", out var file) && File.Exists(ResourceRoot + file))
                {
                    ResourceSingleFile = file;
                }
                else if (Application.platform != RuntimePlatform.WindowsEditor)
                {
                    ResourceSingleFile = "";
                }
                if (prop.TryGetValue("-dir", out var dir) && Directory.Exists(ResourceRoot + dir))
                {
                    ResourceDirectory = dir;
                }
                else if (Application.platform != RuntimePlatform.WindowsEditor)
                {
                    ResourceDirectory = "";
                }
                //-----------------------------------------------------------------------
            }
            this._land = GameObject.Find("Land");
            this._land.SetActive(false);
            this._mainCamera = GameObject.Find("Main Camera");
            InitResources();
        }
        //-------------------------------------------------------------------------------------------------------------------------
        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var mask = LayerMask.GetMask("UI");
                if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000f, -1))
                {
                    var txt = hitInfo.transform.GetComponent<Text3D>();
                    if (txt != null)
                    {
                        var point = hitInfo.point;
                        _selectedText = txt.ResourceName;
                    }
                    else
                    {
                        _selectedText = string.Empty;
                    }
                }
                else
                {
                    _selectedText = string.Empty;
                }
            }
            // copy res name
            if (Input.GetKeyDown(KeyCode.C) && Input.GetKey(KeyCode.LeftControl))
            {
                if (!string.IsNullOrEmpty(_selectedText))
                {
                    GUIUtility.systemCopyBuffer = _selectedText;
                    Debug.Log(_selectedText);
                }
            }
            // reset camera pos
            if (Input.GetKeyDown(KeyCode.H))
            {
                if (_mainCamera != null)
                {
                    _mainCamera.transform.position = Vector3.zero;
                    if (_mainCamera.TryGetComponent<FreeCamera>(out var freeCamera))
                    {
                        freeCamera.ResetFromTransform();
                    }
                }
            }
            // show hide land
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (_land != null)
                {
                    _land.SetActive(!_land.activeSelf);
                }
            }
            // swap single view
            if (ResourceSingleView)
            {
                if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    NextSingleView(1);
                }
                if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    NextSingleView(-1);
                }
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------
        private void OnGUI()
        {
            if (GUI.Button(new Rect(1, 1, 200, 100), "Replay"))
            {
                foreach (var item in _resources)
                {
                    var effect = item.Item2.GetComponentInChildren<EffectReplay>();

                    if (effect != null)
                    {
                        effect.Replay();
                    }
                }
            };
            GUI.Label(new Rect(1, 1, 500, 60), _selectedText);
            GUI.Label(new Rect(1, Screen.height - 20, 500, 60), $"{_singleViewIndex}/{_resources.Count}");
        }
        //-------------------------------------------------------------------------------------------------------------------------
        private void InitResources()
        {
            ABSystemImpl.RootPath = "file://";
            if (!string.IsNullOrEmpty(ResourceSingleFile) && File.Exists(ResourceRoot + ResourceSingleFile))
            {
                AddAssetbundles(new FileInfo(ResourceRoot + ResourceSingleFile), Vector3.zero);
            }
            if (!string.IsNullOrEmpty(ResourceDirectory) && Directory.Exists(ResourceRoot + ResourceDirectory))
            {
                var list = new List<FileInfo>();
                GetAssetbundles(new DirectoryInfo(ResourceRoot + ResourceDirectory), list);
                var width = (int)Math.Sqrt(list.Count);
                if (_land != null)
                {
                    _land.transform.localScale = new Vector3(
                        width * ResourceGridSize,
                        _land.transform.localScale.y,
                        width * ResourceGridSize);
                    _land.transform.localPosition = new Vector3(
                        width * ResourceGridSize / 2f,
                        _land.transform.localPosition.y,
                        width * ResourceGridSize / 2f);
                }
                for (int x = 0, i = 0; x < width; x++)
                {
                    for (int y = 0; i < list.Count && y < width; y++, i++)
                    {
                        var file = list[i];
                        var pos = new Vector3(x * ResourceGridSize, 0, y * ResourceGridSize);
                        if (ResourceSingleView)
                        {
                            pos = Vector3.zero;
                        }
                        AddAssetbundles(file, pos);
                    }
                }
                void GetAssetbundles(DirectoryInfo dir, List<FileInfo> list)
                {
                    foreach (var sfile in dir.GetFiles())
                    {
                        if (sfile.Extension.Equals(ResourceFileExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(sfile);
                        }
                    }
                    foreach (var sdir in dir.GetDirectories())
                    {
                        GetAssetbundles(sdir, list);
                    }
                }
            }
            if (ResourceSingleView)
            {
                for (int i = 0; i < _resources.Count; i++)
                {
                    var res = _resources[i];
                    res.Item2.SetActive(i == _singleViewIndex);
                }
            }
        }
        private GameObject AddAssetbundles(FileInfo file, in Vector3 position)
        {
            if (file.Exists)
            {
                var name = file.FullName.Substring(ResourceRoot.Length).ReplaceAll("\\", "/");
                var wrap = ResourceSystem.GetWrapGO(file.FullName, Path.GetFileNameWithoutExtension(file.Name), null, gameObject.transform);
                if (wrap != null && wrap.GameObject)
                {
                    wrap.GameObject.name = name;
                    wrap.Transform.localPosition = position;
                    var info = BindBundleComponent(wrap.GameObject);
                    var t3d = CreateText3D();
                    t3d.ResourceName = name;
                    t3d.ResourceInfo = info;
                    t3d.transform.SetParent(wrap.Transform);
                    t3d.transform.localPosition = Vector3.zero;
                    t3d.TextScale = ResourceTextScale;

                    _resources.Add(new Tuple<string, GameObject>(t3d.ResourceName, wrap));
                    return wrap;
                }
            }
            return null;
        }
        private Text3D CreateText3D()
        {
            var prefab = Resources.Load<GameObject>("Text3D");
            var go = Instantiate<GameObject>(prefab);
            return go.GetComponent<Text3D>();
        }
        private void NextSingleView(int d)
        {
            if (ResourceSingleView)
            {
                _resources[_singleViewIndex].Item2.SetActive(false);
                if (ResourceSingleView) { }
                if (d > 0)
                {
                    _singleViewIndex = CMath.CycNum(_singleViewIndex, 1, _resources.Count);
                }
                else if (d < 0)
                {
                    _singleViewIndex = CMath.CycNum(_singleViewIndex, -1, _resources.Count);
                }
                _resources[_singleViewIndex].Item2.SetActive(true);
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------

        protected virtual Component BindBundleComponent(GameObject go)
        {
            var pss = go.GetComponentsInChildren<ParticleSystem>();
            if (pss != null && pss.Length > 0)
            {
                var replay = go.AddComponent<EffectReplay>();
                return replay;
            }
            var anim = go.GetComponentsInChildren<Animator>();
            if (anim != null && anim.Length > 0)
            {
                var replay = go.AddComponent<AnimReplay>();
                return replay;
            }
            return null;
        }

        //-------------------------------------------------------------------------------------------------------------------------

    }
}