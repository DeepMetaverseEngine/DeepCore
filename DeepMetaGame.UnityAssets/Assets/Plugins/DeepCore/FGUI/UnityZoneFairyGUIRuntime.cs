

using Cysharp.Threading.Tasks;
using DeepCore;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Reflection;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data.GUI.Meta;
using DeepMetaGame.Display.FairyGUI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DeepCore.Threading;
using DeepCore.Unity;



#if FGUI
using FairyGUI;
#endif

namespace DeepMetaGame.Unity.FGUI
{
    public class FairyCanvas : MonoBehaviour, IFairyCanvas
    {
        private bool isDisposing = false;
        private UpdateTaskQueue<IFairyCanvas> invoking;
        void Awake()
        {
            invoking = new UpdateTaskQueue<IFairyCanvas>(this);
        }
        void Update()
        {
            invoking.Update();
            //InvokingAsync().Forget();
        }
        void OnDestroy()
        {
            this.isDisposing = true;
            this.invoking.Dispose();
        }
        //         private List<Func<UniTask>> invoking = new List<Func<UniTask>>();
        //         private List<Func<UniTask>> updating = new List<Func<UniTask>>();
        public bool IsDisposing => isDisposing;
        public void InvokeAsync(Func<IFairyCanvas, Task> invoke)
        {
            invoking.Add(invoke);
        }
        public void Invoke(Action<IFairyCanvas> invoke)
        {
            invoking.Add(invoke);
        }
        //         async UniTask InvokingAsync()
        //         {
        //             if (invoking.Count > 0)
        //             {
        //                 updating.AddRange(invoking);
        //                 invoking.Clear();
        //                 try
        //                 {
        //                     foreach (var func in updating)
        //                     {
        //                         try
        //                         {
        //                             await func();
        //                         }
        //                         catch (Exception err) { err.PrintStackTrace(); }
        //                     }
        //                 }
        //                 finally
        //                 {
        //                     updating.Clear();
        //                 }
        //             }
        //         }
    }

    public abstract class UnityZoneFairyGUIRuntime : FairyGUIRuntime
    {
        new static public UnityZoneFairyGUIRuntime Instance { get; private set; }
        private HashMap<string, Type> subTypes = new();
        private HashMap<string, FairyForm> showing = new HashMap<string, FairyForm>();
        public UnityZoneFairyGUIRuntime()
        {
            UnityZoneFairyGUIRuntime.Instance = this;
            foreach (var e in GetUITypes())
            {
                var sub = e.Value;
                var url = e.Key;
                Regist(url, sub);
            }
        }
        public bool Regist(string url, Type sub)
        {
            if (typeof(ZoneFairyComponent).IsAssignableFrom(sub))
            {
                Debug.Log($"FGUI 映射 : {url} => {sub.Name}");
                subTypes.Add(url, sub);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 获得所有IBattleUI
        /// </summary>
        /// <returns></returns>
        public abstract HashMap<string, Type> GetUITypes();
        public virtual void Start(UnityZone zone)
        {
            zone.layer.CustomShowForm += (layer, form, guid, dialog) =>
            {
                try
                {
                    if (form.HasFairyGUIComponentMeta)
                    {
                        var canvas = zone.gameObject.GetOrAddComponent<FairyCanvas>();
                        var current = new FairyForm(canvas, layer, form, guid);
                        showing.Add(guid, current);
                        current.OnClose += e =>
                        {
                            showing.Remove(guid);
                        };
                        return current;
                    }
                }
                catch (Exception err)
                {
                    Debug.LogError(err);
                }
                return null;
            };
            zone.OnDispose += (z) =>
            {
                foreach (var sub in showing)
                {
                    sub.Value.Dispose();
                }
                showing.Clear();
            };
        }
        public override FairyComponent CreatePanel(DeepMetaGame.Data.GUI.Meta.UEFairyGUIComponentMeta meta)
        {
            if (!string.IsNullOrEmpty(meta.gui_link) && subTypes.TryGetValue(meta.gui_link, out var subType))
            {
                return DeepActivator.CreateInstance(subType) as FairyComponent;
            }
            return null;
        }
    }
    //---------------------------------------------------------------------------------------------------------------------------
    public abstract partial class ZoneFairyComponent : FairyComponent
    {
        public override event Action<FairyComponent, IFairyNode> OnNodeClick;

    }

#if FGUI

    partial class ZoneFairyComponent
    {
        private Dictionary<GObject, object> nodeCache = new Dictionary<GObject, object>();
        private Dictionary<GButton, EventCallback1> btnHandler = new Dictionary<GButton, EventCallback1>();
        protected abstract UniTask<GComponent> LoadGRootAsync();
        public GComponent GRoot { get; private set; }
        public override IFairyNode Root => new FairyNode() { obj = GRoot };
        public GObject FindChild(string subUrl)
        {
            GObject obj = GRoot;
            var paths = subUrl.Split('/');
            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                if (string.IsNullOrEmpty(path)) continue;
                if (obj is GComponent comp)
                {
                    obj = comp.GetChild(path);
                    if (obj == null)
                    {
                        return null;
                    }
                }
                else if (obj is GGroup group)
                {
                    obj = group.parent.GetChild(path);
                    if (obj == null)
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            return obj;
        }
        sealed protected override async Task Init(UEFairyGUIComponentMeta meta, FairyForm runtime)
        {
            try
            {
                this.GRoot = await LoadGRootAsync();
                if (this.GRoot != null)
                {
                    this.GRoot.ForEachGObject(this, static (t, obj) =>
                    {
                        if (obj is GButton btn)
                        {
                            var handler = new EventCallback1(s =>
                            {
                                t.OnNodeClick?.Invoke(t, new FairyNode() { obj = btn });
                            });
                            t.btnHandler[btn] = handler;
                            btn.onClick.Add(handler);
                        }
                    });
                }
                else
                {
                    Debug.LogError($"Can Not Create FGUI GRoot : {meta.gui_link}");
                }
            }
            catch (Exception ex)
            {
                ex.PrintStackTrace($"Create FGUI Error : {meta.gui_link}");
            }
            await base.Init(meta, runtime);
        }
        sealed protected override void Disposing()
        {
            foreach (var e in btnHandler)
            {
                e.Key.onClick.Remove(e.Value);
            }
            btnHandler.Clear();
            foreach (var childnode in nodeCache.Keys)
            {
                if (childnode != null)
                {
                    childnode.data = null;
                }
            }
            nodeCache.Clear();
            this.onBindData = null;
            this.onSetVisible = null;
            this.onController = null;
            base.Disposing();
            this.OnNodeClick = null;
        }
        sealed protected override void OnBindData(IFairyNode child, string key, object value)
        {
            if (child != null)
            {
                var gc = ((FairyNode)child).obj;
                gc.data = value;
                nodeCache[gc] = value;
                onBindData?.Invoke(this, gc, key, value);
            }
            else
            {
                onBindData?.Invoke(this, null, key, value);
            }
        }
        sealed protected override void OnSetVisible(IFairyNode child, bool value)
        {
            onSetVisible?.Invoke(this, ((FairyNode)child).obj, value);
        }
        sealed protected override void OnController(IFairyNode child, IFairyController ctrl, object value)
        {
            onController?.Invoke(this, ((FairyNode)child).obj, ((FairyController)ctrl).ctrl, value);
        }

        public event OnBindDataHandler onBindData;
        public event OnSetVisibleHandler onSetVisible;
        public event OnControllerHandler onController;

        public delegate void OnBindDataHandler(ZoneFairyComponent sender, GObject child, string key, object value);
        public delegate void OnSetVisibleHandler(ZoneFairyComponent sender, GObject child, bool value);
        public delegate void OnControllerHandler(ZoneFairyComponent sender, GObject child, Controller ctrl, object value);

        public void InvokeNodeClick(GObject node, string dialogResult = null)
        {
            this.DialogResult = dialogResult;
            Form.InvokeNodeClick(this, new FairyNode() { obj = node });
        }
    }
    //------------------------------------------------------------------------------------------------------------------------------
    public struct FairyNode : IFairyNode
    {
        public GObject obj;
        public string Name => obj?.name;
        public bool Active => obj != null;
        public IFairyNode Parent => new FairyNode() { obj = obj?.parent };
        public bool Visible
        {
            get => obj.visible;
            set
            {
                obj.visible = value;
                if (obj is GGroup group)
                {
                    int cnt = obj.root.numChildren;
                    for (int i = 0; i < cnt; i++)
                    {
                        GObject child = obj.root.GetChildAt(i);
                        if (child.group == group)
                        {
                            child.visible = value;
                        }
                    }
                }
            }
        }
        public void RunController(IFairyController controller)
        {
            if (obj is GComponent component)
            {
                var ctrl = ((FairyController)controller);
                if (ctrl.pageIndex.HasValue)
                {
                    ctrl.ctrl.selectedIndex = ctrl.pageIndex.Value;
                }
                else if (!string.IsNullOrEmpty(ctrl.pageName))
                {
                    ctrl.ctrl.selectedPage = ctrl.pageName;
                }
            }
        }
        public void BindData(string key, object zoneVar)
        {
            if (zoneVar is string varString)
            {
                obj.text = varString;
            }
            else if (obj is GLabel)
            {
                obj.text = $"{zoneVar}";
            }
            else if (obj is GTextField)
            {
                obj.text = $"{zoneVar}";
            }
            else if (obj is GRichTextField)
            {
                obj.text = $"{zoneVar}";
            }
            else if (obj is GImage img)
            {
                if (zoneVar is Texture2D texture)
                {
                    img.texture = new NTexture(texture);
                }
                else
                {
                    img.texture = null;
                }
            }
        }
        public bool TryGetChild(string nodeName, out IFairyNode node)
        {
            if (obj is GComponent comp)
            {
                var child = comp.GetChild(nodeName);
                if (child != null)
                {
                    node = new FairyNode() { obj = child };
                    return true;
                }
            }

            if (obj is GGroup group)
            {
                var child = group.parent.GetChild(nodeName);
                if (child != null)
                {
                    node = new FairyNode() { obj = child };
                    return true;
                }
            }
            node = default;
            return false;
        }
        public bool TryGetController(string _controllerName, out IFairyController controller)
        {
            try
            {
                if (obj is GComponent component)
                {
                    var ctrl = component.GetController(_controllerName);
                    if (ctrl != null)
                    {
                        controller = new FairyController()
                        {
                            ctrl = ctrl,
                            pageIndex = ctrl.selectedIndex,
                            pageName = ctrl.name,
                        };
                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
            controller = null;
            return false;
        }
        public bool TryGetController(string _controllerName, in FairyPage _page, out IFairyController controller)
        {
            try
            {
                if (obj is GComponent component)
                {
                    var ctrl = component.GetController(_controllerName);
                    if (ctrl != null)
                    {
                        controller = new FairyController()
                        {
                            ctrl = ctrl,
                            pageIndex = _page.index,
                            pageName = _page.name,
                        };
                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
            controller = null;
            return false;
        }
        public override bool Equals(object obj)
        {
            if (obj is FairyNode fobj)
            {
                return this.obj == fobj.obj;
            }
            return false;
        }
        public override int GetHashCode()
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
        public static bool operator ==(in FairyNode value1, in FairyNode value2) => value1.Equals(value2);
        public static bool operator !=(in FairyNode value1, in FairyNode value2) => !value1.Equals(value2);
    }
    //------------------------------------------------------------------------------------------------------------------------------
    public struct FairyController : IFairyController
    {
        public Controller ctrl;
        public string pageName;
        public int? pageIndex;
    }
    //------------------------------------------------------------------------------------------------------------------------------
    public static class FairyBattleExt
    {
        public static InstanceUnit GetBindingUnit(this GObject obj)
        {
            if (obj.data is LayerUnit unit && unit.EventSender is InstanceUnit zu)
            {
                return zu;
            }
            return null;
        }
        public static LayerUnit GetBindingLayerUnit(this object obj)
        {
            if (obj is LayerUnit unit)
            {
                return unit;
            }
            return null;
        }
        public static LayerUnit GetBindingLayerUnit(this GObject obj)
        {
            if (obj.data is LayerUnit unit)
            {
                return unit;
            }
            return null;
        }
        public static void ForEachGObject<ST>(this GComponent obj, ST st, Action<ST, GObject> action)
        {
            //foreach (var child in obj._children)
            var count = obj.numChildren;
            for (int i = 0; i < count; i++)
            {
                var child = obj.GetChildAt(i);
                action(st, child);
                if (child is GComponent comp)
                {
                    ForEachGObject<ST>(comp, st, action);
                }
            }
        }
    }
#endif
    //------------------------------------------------------------------------------------------------------------------------------
}