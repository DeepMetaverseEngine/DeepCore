using DeepCore.GUI.Display;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using DeepCore.Threading;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeepCore.Unity3D.Impl.OnGUI
{
    public class OnGUICanvas : MonoBehaviour, IDisplayCanvas
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(OnGUICanvas)) { };
        void Awake()
        {
            Alloc.RecordConstructor(GetType());
            //             invoking = new List<Func<Task>>();
            //             updating = new List<Func<Task>>();
            this.invoking = new UpdateTaskQueue<IDisplayCanvas>(this);
            if (RootNode == null)
            {
                RootNode = new OnGUIRoot(this);
            }
        }
        void OnDestroy()
        {
            this.IsDisposing = true;
            this.IDisplayCanvas_KeyDown = null;
            this.IDisplayCanvas_KeyPress = null;
            this.IDisplayCanvas_KeyUp = null;
            this.IDisplayCanvas_MouseDown = null;
            this.IDisplayCanvas_MouseUp = null;
            this.IDisplayCanvas_MouseMove = null;
            this.IDisplayCanvas_MouseWheel = null;
            this.IDisplayCanvas_Update = null;
            this.IDisplayCanvas_Paint = null;
            this.IDisplayCanvas_Resize = null;
            this.invoking.Dispose();
            Alloc.RecordDispose(GetType());
            Alloc.RecordDestructor(GetType());
            if (RootNode != null)
            {
                RootNode.Dispose();
                RootNode = null;
            }
        }

        protected virtual void Start()
        {

        }
        protected virtual void OnGUI()
        {
            try
            {
                invoking.Update();
                try
                {
                    // size change
                    if (lastSize.X != Screen.width || lastSize.Y != Screen.height)
                    {
                        lastSize = new Geometry.Vector2(Screen.width, Screen.height);
                        IDisplayCanvas_Resize?.Invoke(this, lastSize);
                    }
                    // mouse down
                    if (InputHelper.IsMouseDown(out var mouseDown))
                    {
                        IDisplayCanvas_MouseDown?.Invoke(this, NewMouseArgs());
                    }
                    // mouse move
                    if (lastMousePos.X != Input.mousePosition.x || lastMousePos.Y != Input.mousePosition.y)
                    {
                        lastMousePos = new Geometry.Vector2(Input.mousePosition.x, Input.mousePosition.y);
                        IDisplayCanvas_MouseMove?.Invoke(this, NewMouseArgs());
                    }
                    // mouse up
                    if (InputHelper.IsMouseUp(out var mouseUp))
                    {
                        IDisplayCanvas_MouseUp?.Invoke(this, NewMouseArgs());
                    }
                    // mouse scroll
                    if (Input.mouseScrollDelta != Vector2.zero)
                    {
                        IDisplayCanvas_MouseWheel?.Invoke(this, NewMouseArgs());
                    }
                    if (Input.anyKeyDown)
                    {
                        //IDisplayCanvas_KeyDown?.Invoke(this, e.ToKeyArgs());              
                        //IDisplayCanvas_KeyUp?.Invoke(this, e.ToKeyArgs());               
                        //IDisplayCanvas_KeyPress?.Invoke(this, e.ToKeyArgs());       
                    }
                }
                catch (Exception err)
                {
                    Debug.LogError("OnGUI " + err.Message);
                    Debug.LogError("OnGUI " + err.StackTrace);
                    Debug.LogException(err);
                }
                IDisplayCanvas_Update?.Invoke(this);
                using (var gfx = new OnGUIGraphics())
                {
                    IDisplayCanvas_Paint?.Invoke(this, gfx);
                }
                //                 if (UnityEngine.GUI.changed)
                //                 {
                //                     Input.ResetInputAxes();
                //                 }
                //Event.current.Use();
                GUIUtils.AutoTooltips();
            }
            catch (Exception err)
            {
                Debug.LogError("OnGUI " + err.Message);
                Debug.LogError("OnGUI " + err.StackTrace);
                Debug.LogException(err);
            }
            finally
            {
            }
        }

        public static MouseArgs NewMouseArgs()
        {
            return new MouseArgs()
            {
                Button = InputHelper.GetMouseButton(),
                Location = new Geometry.Vector2(Input.mousePosition.x, Input.mousePosition.y),
                IsCtrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                Delta = Input.mouseScrollDelta.y,
            };
        }

        //-----------------------------------------------------------------------------------------------------
        #region Canvas
        private Geometry.Vector2 lastSize;

        private Geometry.Vector2 lastMousePos;
        public bool Focused { get => true; }
        public Geometry.Vector2 Size => lastSize;
        public Geometry.Vector2 MouseLocation => lastMousePos;
        public GUI.Input.MouseButton MouseButtons => InputHelper.GetMouseButton();
        public GUI.Input.KeyCode KeyStatus => GUI.Input.KeyCode.None;
        public bool IsDisposing { get; private set; } = false;
        //         void IDisplayCanvas.PostToEditor(DisplayNode e, object arg)
        //         {
        //         }
        void IDisplayCanvas.RequestRepaint()
        {
        }
        public OnGUIRoot RootNode { get; private set; }
        public class OnGUIRoot : DisplayRoot
        {
            internal OnGUIRoot(OnGUICanvas canvas) : base(canvas)
            {
                IsShowCameraCross = false;
                EnableCamera = false;
                EnableMouseRightMoveCamera = false;
                EnableMouseWheelZoomCamera = false;
                EnableDragMoveNode = false;
                EnableDragResizeNode = false;
                EnableDrawHUD = false;
            }
            protected override void UpdateCamera(GUI.Display.Graphics g)
            {
                base.UpdateCamera(g);
            }
            protected override bool RayCastInteractive(Geometry.Vector2 rootLocation, out DisplayNode hitted, out InteractiveComponent hitted_comp)
            {
                var ret = base.RayCastInteractive(rootLocation, out hitted, out hitted_comp);
                //                 if (ret)
                //                 {
                //                     Debug.Log($"RayCastInteractive : {hitted}");
                //                 }
                return ret;
            }
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------
        #region Events

        private CanvasKeyboardEvent IDisplayCanvas_KeyDown;
        private CanvasKeyboardEvent IDisplayCanvas_KeyPress;
        private CanvasKeyboardEvent IDisplayCanvas_KeyUp;
        private CanvasMouseEvent IDisplayCanvas_MouseDown;
        private CanvasMouseEvent IDisplayCanvas_MouseUp;
        private CanvasMouseEvent IDisplayCanvas_MouseMove;
        private CanvasMouseEvent IDisplayCanvas_MouseWheel;
        private CanvasUpdateEvent IDisplayCanvas_Update;
        private CanvasPaintEvent IDisplayCanvas_Paint;
        private CanvasResizeEvent IDisplayCanvas_Resize;

        event CanvasKeyboardEvent IDisplayCanvas.KeyDown { add { IDisplayCanvas_KeyDown += value; } remove { IDisplayCanvas_KeyDown -= value; } }
        event CanvasKeyboardEvent IDisplayCanvas.KeyPress { add { IDisplayCanvas_KeyPress += value; } remove { IDisplayCanvas_KeyPress -= value; } }
        event CanvasKeyboardEvent IDisplayCanvas.KeyUp { add { IDisplayCanvas_KeyUp += value; } remove { IDisplayCanvas_KeyUp -= value; } }
        event CanvasMouseEvent IDisplayCanvas.MouseDown { add { IDisplayCanvas_MouseDown += value; } remove { IDisplayCanvas_MouseDown -= value; } }
        event CanvasMouseEvent IDisplayCanvas.MouseUp { add { IDisplayCanvas_MouseUp += value; } remove { IDisplayCanvas_MouseUp -= value; } }
        event CanvasMouseEvent IDisplayCanvas.MouseMove { add { IDisplayCanvas_MouseMove += value; } remove { IDisplayCanvas_MouseMove -= value; } }
        event CanvasMouseEvent IDisplayCanvas.MouseWheel { add { IDisplayCanvas_MouseWheel += value; } remove { IDisplayCanvas_MouseWheel -= value; } }
        event CanvasUpdateEvent IDisplayCanvas.Update { add { IDisplayCanvas_Update += value; } remove { IDisplayCanvas_Update -= value; } }
        event CanvasPaintEvent IDisplayCanvas.Paint { add { IDisplayCanvas_Paint += value; } remove { IDisplayCanvas_Paint -= value; } }
        event CanvasResizeEvent IDisplayCanvas.Resize { add { IDisplayCanvas_Resize += value; } remove { IDisplayCanvas_Resize -= value; } }

        #endregion
        //-----------------------------------------------------------------------------------------------------
        #region MainThread
        private UpdateTaskQueue<IDisplayCanvas> invoking;
        //         private List<Func<Task>> invoking;
        //         private List<Func<Task>> updating;
        public void Invoke(Func<IDisplayCanvas, Task> invoke)
        {
            invoking.Add(invoke);
        }
        public void Invoke(Action<IDisplayCanvas> invoke)
        {
            invoking.Add(invoke);
        }
        //         async Task InvokingAsync()
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
        #endregion
        //-----------------------------------------------------------------------------------------------------



    }
}
