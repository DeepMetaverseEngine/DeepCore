using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity.ResourceViewer;
using DeepCore.Unity3D.AB;
using DeepCore.Unity3D.Impl.OnGUI;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Unity.BattleView;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview
{
    //     public abstract class RTGFactory
    //     {
    //         public static RTGFactory Instance { get; private set; }
    //         public RTGFactory()
    //         {
    //             Instance = this;
    //         }
    //         public abstract UnityZoneSpaceTransverter TransHelper { get; }
    //         public IViewRes LoadResource(string resName, ResourceType resType, object sender)=>UnityBattleFactory.Resource.LoadViewResource(resName, resType, sender);
    //         public virtual List<ResInfo> ListResources()
    //         {
    //             return new List<ResInfo>();
    //         }
    //         public virtual void PlayKeyFrame(Transform obj, IKeyFrameProperties keyframe)
    //         {
    //         }
    //         public virtual void StopSound() { }
    //         public virtual bool SoundOn { get; set; }
    //     }

    //--------------------------------------------------------------------------------------------------------------------------

    public abstract class UnityRTG : MonoBehaviour
    {
        public static int LOAD_RES_TIMEOUT_MS = 10000;
        //public static RTGFactory Factory => RTGFactory.Instance;
        public static UnityRTG RTG { get; private set; }
        public static TimeTaskQueue TimeTasks { get => UnityIPC.TimeTasks; }
        public UnityZoneSpaceTransverter TransHelper => spaceTransverter.Value;
        //--------------------------------------------------------------------------------------------------------------------------
        [SerializeField]
        public string RayCastObjectLayerName = "Default";
        [SerializeField] public string RayCastTerrainLayerName = "Default";
        [SerializeField] public float RayCastMaxDistance = 10000f;
        [SerializeField] public float HeadVisibleDistance = 100f;

        [SerializeField] public Camera MainCamera;

        [SerializeField] public Transform NodeHUD;
        [SerializeField] public Transform NodeTemplates;

        [SerializeField] public Transform TempVoxel;
        [SerializeField] public Transform TempNavMesh;
        [SerializeField] public Transform TempGizmoz; // Matrial Mode Transparent not dynamic
        [SerializeField] public Transform TempGround;
        [SerializeField] public Transform TempHeadText;

        private Lazy<UnityZoneSpaceTransverter> spaceTransverter = new Lazy<UnityZoneSpaceTransverter>(() => UnityBattleFactory.Instance?.CreateZoneSpaceTransverter());
        // 
        //         [SerializeField] public string RayCastObjectLayerName = "RayCast";
        //         [SerializeField] public string RayCastTerrainLayerName = "Terrain";
        //         [SerializeField] public float RayCastMaxDistance = 10000f;
        //         [SerializeField] public float HeadVisibleDistance = 100f;
        // 
        //         [SerializeField] public Camera MainCamera;
        //         [SerializeField] public Transform TempGizmoz; // Matrial Mode Transparent not dynamic
        //         [SerializeField] public Transform TempGround;
        //         [SerializeField] public Transform TempHeadText;
        //         [SerializeField] public Transform NodeHUD;

        //         [SerializeField] public string RayCastObjectLayerName = "RayCast";
        //         [SerializeField] public string RayCastTerrainLayerName = "Terrain";
        //         [SerializeField] public float RayCastMaxDistance = 10000f;
        //         [SerializeField] public float HeadVisibleDistance = 100f;
        // 
        //         [SerializeField] public Camera MainCamera;
        //         [SerializeField] public Transform TempGizmoz; // Matrial Mode Transparent not dynamic
        //         [SerializeField] public Transform TempGround;
        //         [SerializeField] public Transform TempHeadText;
        //         [SerializeField] public Transform NodeHUD;

        //--------------------------------------------------------------------------------------------------------------------------

        public abstract bool IsDebug { get; set; }
        protected virtual void Awake()
        {
            RTG = this;
        }
        protected virtual void Start()
        {
            TempVoxel.gameObject.SetActive(false);
            TempNavMesh.gameObject.SetActive(false);
            TempGizmoz.gameObject.SetActive(false);
            TempGround.gameObject.SetActive(false);
            TempHeadText.gameObject.SetActive(false);
        }
        //--------------------------------------------------------------------------------------------------------------------------
        [SerializeField]
        public Font ongui_font;
        private bool ongui_font_loaded = false;
        protected virtual void OnGUI()
        {
            if (ongui_font_loaded == false)
            {
                if (ongui_font != null)
                {
                    var oldfont = GUI.skin.font;
                    GUI.skin.font = ongui_font;
                    GUI.skin.label.normal.textColor = UnityEngine.Color.white;
                }
                ongui_font_loaded = true;
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------
        #region Gizmos
        //         public abstract Matrix4x4 GizmosMatrix { get; set; }
        //         public abstract Color GizmosColor { get; set; }
        //         public abstract void GizmosDrawLine(Vector3 p1, Vector3 p2);
        //         public abstract void GizmosDrawCube(Vector3 center, Vector3 size);
        public abstract GameObject TargetObject { get; set; }
        public abstract bool IsDraggingTarget { get; }
        public abstract GameObject DraggingTarget { get; }
        public abstract void SetSnapToGrid(bool snapToGrid, float gridOfSize);
        public abstract void SetCameraMode(CameraMode mode);

        //--------------------------------------------------------------------------------------------------------------------------
        public abstract void SetHeadText(GameObject obj, string text);
        //--------------------------------------------------------------------------------------------------------------------------


        public abstract void AddEditorVoxel(GameObject obj);
        public abstract void AddEditorScene(GameObject obj);
        //--------------------------------------------------------------------------------------------------------------------------
        public abstract IEditorObject AddEditorObject(GameObject obj);
        //--------------------------------------------------------------------------------------------------------------------------
        public abstract void SetCamera(Vector3 pos, Vector3 target);
        public abstract void LookAt(Transform target, bool focuse = false, float? bodySize = null);
        public abstract void LookAt(Vector3 target, bool focuse = false, float? bodySize = null);

        public abstract event TargetSelectChanged OnTargetSelectChanged;
        public abstract event TargetPropertyChanged OnTargetPropertyChanged;
        public abstract event TargetTransformChanged OnTargetTransformChanged;

        #endregion

        //---------------------------------------------------------------------------------------------------
        public IViewResource LoadResource(string resName, ResourceType resType, object sender)
        {
            UnityIPC.IPC.TryGetResourceProperties(resName, out var resProp);
            return UnityPreviewFactory.Instance.LoadViewResource(sender, resName, resType, resProp);
        }
    }

    public enum CameraMode
    {
        Mode3D,
        Mode2D,
    }
    public struct ResInfo
    {
        public string ResName;
        public object ResData;
    }


    public delegate void TargetSelectChanged(GameObject @old, GameObject @new);
    public delegate void TargetPropertyChanged(GameObject obj);
    public delegate void TargetTransformChanged(GameObject obj);

    public interface IEditorObject
    {
        bool Selectable { get; set; }
        GameObject gameObject { get; }
        Transform transform { get; }
    }

}
