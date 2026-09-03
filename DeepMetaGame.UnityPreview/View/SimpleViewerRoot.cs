using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity.Camera;
using DeepCore.Unity.ResourceViewer;
using DeepCore.Unity3D.AB;
using DeepCore.Unity3D.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class SimpleViewerRoot : MonoBehaviour
{

    public string ResourceFiles = @"res\fx_bloodsplatter.ab";
    public string ResourceDirectory = @"res";
    public string ResourceRoot = @"D:\dev\DeepMetaWork\Samples\Gate.Sample.Win32Editor\GameEditor";

    public string ResourceFileExtension = ".ab";
    public float ResourceGridSize = 5;
    public bool ResourceFixedGrid = false;
    public float ResourceTextScale = 0.01f;

    public Transform Land;
    //-------------------------------------------------------------------------------------------------------------------------
    class ResInfo
    {
        public FileInfo file;
        public string name;
        public WrapGO wrap;
        public Bounds aabb;
    }
    private List<ResInfo> resources = new List<ResInfo>();
    private bool isGrid = false;
    private bool isSingleView = false;
    private bool isPause = false;
    //-------------------------------------------------------------------------------------------------------------------------
    // Start is called before the first frame update
    private void Awake()
    {
        UnityDriver.SetDirver();
    }
    void Start()
    {
        var args = Environment.GetCommandLineArgs();
        //CFiles.WriteAllLines(Environment.CurrentDirectory + Path.DirectorySeparatorChar + "args.txt", args);
        var prop = Properties.ParseArgs(args);
        if (prop.Count > 0)
        {
            if (prop.TryGetAs<float>("-grid", out var grid))
            {
                ResourceGridSize = grid;
            }
            if (prop.TryGetAsBool("-fixedGrid", out var sqrt))
            {
                ResourceFixedGrid = sqrt;
            }
            if (prop.TryGetAs<float>("-textScale", out var textScale))
            {
                ResourceTextScale = textScale;
            }
            if (prop.TryGetValue("-ext", out var ext))
            {
                ResourceFileExtension = ext;
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
            if (prop.TryGetValue("-file", out var file))
            {
                ResourceFiles = file;
            }
            else if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                ResourceFiles = "";
            }
            if (prop.TryGetValue("-dir", out var dir))
            {
                ResourceDirectory = dir;
            }
            else if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                ResourceDirectory = "";
            }
            //-----------------------------------------------------------------------
        }
        ABSystem.RootPath = "file://";
        if (!string.IsNullOrEmpty(ResourceFiles))
        {
            var files = ResourceFiles.Split(';');
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    AddAssetbundles(new FileInfo(file));
                }
                else if (File.Exists(Path.Combine(ResourceRoot, file)))
                {
                    AddAssetbundles(new FileInfo(Path.Combine(ResourceRoot, file)));
                }
            }
        }
        if (!string.IsNullOrEmpty(ResourceDirectory))
        {
            if (Directory.Exists(ResourceDirectory))
            {
                AddAssetbundles(new DirectoryInfo(ResourceDirectory));
            }
            else if (Directory.Exists(Path.Combine(ResourceRoot, ResourceDirectory)))
            {
                AddAssetbundles(new DirectoryInfo(Path.Combine(ResourceRoot, ResourceDirectory)));
            }
        }
        if (resources.Count == 1)
        {
            var res = resources[0];
            SwapSelected(res.wrap.gameObject);
            isSingleView = true;
        }
        ResetCamera();
        ResetLayout();
    }

    private void AddAssetbundles(DirectoryInfo dir)
    {
        var list = new List<FileInfo>();
        GetAssetbundles(dir, list);
        if (Land != null)
        {
            Land.gameObject.SetActive(true);
            var width = list.Count * ResourceGridSize;
            Land.transform.localScale = new Vector3(width, Land.transform.localScale.y, width);
        }
        {
            for (int i = 0; i < list.Count; i++)
            {
                var file = list[i];
                var res = AddAssetbundles(file);
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
    private ResInfo AddAssetbundles(FileInfo file)
    {
        if (file.Exists)
        {
            var name = file.FullName.Substring(ResourceRoot.Length).ReplaceAll("\\", "/");
            var wrap = ABSystem.GetWrapGO(file.FullName, Path.GetFileNameWithoutExtension(file.Name), gameObject.transform);
            if (wrap != null && wrap.gameObject)
            {
                BindBundleComponent(wrap.gameObject);
                var t3d = Text3D.CreateText3D(wrap, name);
                t3d.TextScale = ResourceTextScale;
                var res = new ResInfo()
                {
                    file = file,
                    name = t3d.ResourceName,
                    wrap = wrap,
                };
                resources.Add(res);
                // #if UNITY_EDITOR
                //                 try
                //                 {
                //                     var snap = UnityEditor.AssetPreview.GetAssetPreview(wrap.GameObject);
                //                     snap.SaveTextureToFile(new FileInfo(file.FullName + ".snap.png"));
                //                 }
                //                 catch (Exception ex) { ex.PrintStackTrace(); }
                // #endif
                res.wrap.gameObject.transform.localPosition = Vector3.zero;
                return res;
            }
        }
        return null;
    }

    //-------------------------------------------------------------------------------------------------------------------------

    protected virtual void BindBundleComponent(GameObject go)
    {
        if (go.GetComponentInChildren<ParticleSystem>())
        {
            go.AddComponent<EffectReplay>();
        }
        if (go.GetComponentInChildren<Animator>() || go.GetComponentInChildren<Animation>())
        {
            go.AddComponent<AnimReplayController>();
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------
    // Update is called once per frame



    void Update()
    {
        if (resources.Count > 1)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000f, -1))
                {
                    var txt = hitInfo.transform.GetComponentInChildren<Text3D>();
                    SwapSelected(txt);
                }
                else
                {
                    SwapSelected(null as Text3D);
                }
            }
        }
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwapPause();
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Time.timeScale /= 1.25f;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Time.timeScale *= 1.25f;
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
            ResetCamera();
        }
        // swap single view
        if (isSingleView || _selectedWrapGO != null)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                NextSingleView(1);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                NextSingleView(-1);
            }
        }
    }

    private void OnGUI()
    {
        var sh = 32;
        var sw = 200;
        // TOP
        {
            var sy = 0;
            var sx = 0;
            {

            }
            var info = new StringBuilder();
            if (!string.IsNullOrEmpty(ResourceRoot))
            {
                info.Append($"Root={ResourceRoot}");
            }
            if (!string.IsNullOrEmpty(ResourceFiles))
            {
                info.Append($"  File={ResourceFiles}");
            }
            if (!string.IsNullOrEmpty(ResourceDirectory))
            {
                info.Append($"  Dir={ResourceDirectory}");
            }
            GUI.Label(new Rect(sx, sy, Screen.width, sh), info.ToString());
            sy += sh;
            GUI.Label(new Rect(sx, sy, Screen.width, sh), _selectedText);
            sy += sh;
            if (_selectedWrapGO != null && _selectedWrapGO.gameObject.TryGetComponent<AnimReplay>(out var anim))
            {
                GUI.Label(new Rect(sx, sy, Screen.width, sh), $"{anim.CurrentStateName} : {anim.CurrentDurationMS}ms");
                sy += sh;
            }
        }
        // Button
        {
            var sx = 0f;
            var sy = Screen.height - sh;
            // line 1
            {
                if (GUI.Button(new Rect(sx, sy, sw, sh), "Replay"))
                {
                  
                    ReplayAll();
                    ResetCamera();
                }
                sx += sw;
                if (GUI.Button(new Rect(sx, sy, sw, sh), $"{(isPause ? "Resume" : "Pause")}(1)"))
                {
                    SwapPause();
                }
                sx += sw;
                if (GUI.Button(new Rect(sx, sy, sw * 0.5f, sh), "Time-(2)"))
                {
                    Time.timeScale /= 1.25f;
                }
                sx += sw * 0.5f;
                if (GUI.Button(new Rect(sx, sy, sw * 0.5f, sh), "Time+(3)"))
                {
                    Time.timeScale *= 1.25f;
                }
                sx += sw * 0.5f;
                if (resources.Count > 1)
                {
                    if (GUI.Button(new Rect(sx, sy, sw, sh), $"Grid:{isGrid}"))
                    {
                        SwithGrid();
                        ResetCamera();
                    }
                    sx += sw;
                    if (GUI.Button(new Rect(sx, sy, sw, sh), $"SingleView:{isSingleView}"))
                    {
                        SwithSingle();
                        ResetCamera();
                    }
                    sx += sw;
                }
                if (GUI.Button(new Rect(sx, sy, sw, sh), $"TakeSnap"))
                {
                    TakeSnap();
                }
                sx += sw;
            }
            sx = 0;
            sy -= sh;
            // line 2
            {
                GUI.Label(new Rect(sx, sy, Screen.width, sh), $"{_singleViewIndex}/{resources.Count} | Time={Time.timeScale.ToString("##.##")}");
            }
            sy -= sh;
        }

    }
    public void ResetCamera()
    {
        if (resources.Count > 0)
        {
            var res = resources[0];
            Camera.main.transform.position = res.wrap.transform.position - new Vector3(0, 0, -20);
            Camera.main.transform.LookAt(res.wrap.transform, Vector3.up);
            if (Camera.main.TryGetComponent<FreeCamera>(out var free))
            {
                free.ResetFromTransform();
            }
        }
        else
        {
            Camera.main.transform.position = Vector3.zero;
            if (Camera.main.TryGetComponent<FreeCamera>(out var freeCamera))
            {
                freeCamera.ResetFromTransform();
            }
        }
    }
    private string _selectedText = string.Empty;
    private WrapGO _selectedWrapGO;
    private int _singleViewIndex = 0;
    //-------------------------------------------------------------------------------------------------------------------------
    private void SwapSelected(GameObject res)
    {
        if (res.TryGetComponentInChildren<Text3D>(out var txt))
        {
            SwapSelected(txt);
        }
        else
        {
            SwapSelected(null as Text3D);
        }
    }
    private void SwapSelected(Text3D txt)
    {
        _selectedText = txt?.ResourceName;
        _selectedWrapGO = txt?.wrap;
        if (!isSingleView)
        {
            if (txt != null)
            {
                foreach (var item in resources)
                {
                    try
                    {
                        item.wrap.gameObject.SetActive((_selectedWrapGO == item.wrap));
                    }
                    catch { }
                }
            }
            else
            {
                foreach (var item in resources)
                {
                    try
                    {
                        item.wrap.gameObject.SetActive(true);
                    }
                    catch { }
                }
            }
        }
    }

    private void SwapPause()
    {
        isPause = !isPause;
        if (isPause)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
        return;
        //         if (isPause)
        //         {
        //             foreach (var res in resources)
        //             {
        //                 if (res.wrap.GameObject.TryGetComponent<EffectReplay>(out var ef))
        //                 {
        //                     ef.Pause();
        //                 }
        //                 if (res.wrap.GameObject.TryGetComponent<AnimReplayController>(out var ani))
        //                 {
        //                     ani.Pause();
        //                 }
        //             }
        //         }
        //         else
        //         {
        //             foreach (var res in resources)
        //             {
        //                 if (res.wrap.GameObject.TryGetComponent<EffectReplay>(out var ef))
        //                 {
        //                     ef.Replay();
        //                 }
        //                 if (res.wrap.GameObject.TryGetComponent<AnimReplayController>(out var ani))
        //                 {
        //                     ani.Replay();
        //                 }
        //             }
        //         }
    }

    private void NextSingleView(int d)
    {
        if (isSingleView || _selectedWrapGO != null)
        {
            var res = resources[_singleViewIndex];
            res.wrap.gameObject.SetActive(false);
            if (d > 0)
            {
                _singleViewIndex = CMath.CycNum(_singleViewIndex, 1, resources.Count);
            }
            else if (d < 0)
            {
                _singleViewIndex = CMath.CycNum(_singleViewIndex, -1, resources.Count);
            }
            res = resources[_singleViewIndex];
            res.wrap.gameObject.SetActive(true);
            SwapSelected(res.wrap.gameObject);
            if (_selectedWrapGO != null)
            {
                Camera.main.transform.LookAt(_selectedWrapGO.transform.position);
                if (Camera.main.TryGetComponent<FreeCamera>(out var free))
                {
                    free.ResetFromTransform();
                }
            }
        }
    }
    private void ReplayAll()
    {
        foreach (var item in resources)
        {
            var effect = item.wrap.gameObject.GetComponentInChildren<EffectReplay>();
            if (effect != null)
            {
                effect.Replay();
            }
        }
    }
    private void SwithGrid()
    {
        isGrid = !isGrid;
        ResetLayout();
    }
    private void SwithSingle()
    {
        isSingleView = !isSingleView;
        ResetLayout();
    }

    public void ResetLayout()
    {
        _selectedText = string.Empty;
        _selectedWrapGO = null;
        _singleViewIndex = 0;
        if (isSingleView)
        {
            for (int i = 0; i < resources.Count; i++)
            {
                var file = resources[i];
                var res = file.wrap;
                res.gameObject.transform.localPosition = Vector3.zero;
                res.gameObject.SetActive(i == _singleViewIndex);
                if (i == _singleViewIndex)
                {
                    SwapSelected(res.gameObject);
                }
            }
        }
        else if (isGrid)
        {
            var gw = (int)(Math.Sqrt(resources.Count) + 1);
            var i = 0;
            for (int x = 0; x < gw; x++)
            {
                for (int y = 0; y < gw; y++)
                {
                    if (i < resources.Count)
                    {
                        var _res = resources[i];
                        var res = _res.wrap;
                        var pos = new Vector3(x * ResourceGridSize, 0, y * ResourceGridSize);
                        res.gameObject.transform.localPosition = pos;
                        res.gameObject.SetActive(true);
                    }
                    i++;
                }
            }
        }
        else
        {
            var sx = 0f;
            for (int i = 0; i < resources.Count; i++)
            {
                var file = resources[i];
                var res = file.wrap;
                var pos = new Vector3(sx, 0, 0);
                if (ResourceFixedGrid)
                {
                    sx += ResourceGridSize;
                }
                else
                {
                    var bounds = res.gameObject.CalculateRendererBounds();
                    sx += bounds.size.x;
                    sx += 1;
                }
                res.gameObject.transform.localPosition = pos;
                res.gameObject.SetActive(true);
            }
        }





    }

    //-------------------------------------------------------------------------------------------------------------------------
    private void TakeSnap()
    {
        foreach (var res in resources)
        {
            try
            {
                var text = ResourceInfo.GetAssetPreview(res.wrap.gameObject);
                text.SaveTextureToFile(new FileInfo(res.file.FullName + ".snap.png"));
                Texture2D.DestroyImmediate(text);
            }
            catch { }
        }
    }
}
