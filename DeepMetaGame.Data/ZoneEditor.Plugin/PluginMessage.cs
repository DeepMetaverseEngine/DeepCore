using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace DeepMetaGame.Data.ZoneEditor
{
    //--------------------------------------------------------------------------------------------------------
    public class MsgPluginLog : Notify
    {
        public string message;
        public override string ToString()
        {
            return $"{message}";
        }
    }
    public class MsgUnityToken : Notify
    {
        public IntPtr KeepHWND;
        public ISerializable State;
    }
    public class MsgUnityIsReady : Notify
    {
        public IntPtr UnityHWND;

    }
    public class MsgRefreshHWND : Notify
    {
        public string message;
        public override string ToString()
        {
            return $"{message}";
        }
    }
    public class MsgFocusHWND : Notify
    {
        public IntPtr message;
        public override string ToString()
        {
            return $"{message}";
        }
    }
    //--------------------------------------------------------------------------------------------------------
    public class SceneEditorStatus : Notify
    {
        [Desc("2D相机")] public bool Camera2D = false;

        [Desc("网格尺寸")] public float GridSize = 1f;
        [Desc("对齐到网格", "check", true)] public bool GridToSize = false;

        [Desc("仅显示单位", "check", true)] public bool OnlyShowUnit;
        [Desc("仅显示区域", "check", true)] public bool OnlyShowRegion;
        [Desc("仅显示物品", "check", true)] public bool OnlyShowItem;
        [Desc("仅显示路点", "check", true)] public bool OnlyShowPoint;
        [Desc("仅显示空气墙", "check", true)] public bool OnlyShowDecoration;
        [Desc("仅显示首都", "check", true)] public bool OnlyShowArea;
        [Desc("仅显未放置体素元素", "check", true)] public bool OnlyBlackHole;
        public bool ShowObjectsAll
        {
            get { return !(OnlyShowUnit || OnlyShowItem || OnlyShowDecoration || OnlyShowRegion || OnlyShowPoint || OnlyShowArea || OnlyBlackHole); }
        }

        [Desc("显示单位名字", "show", true)] public bool ShowObjectsName = true;
        [Desc("显示单位体积", "show", true)] public bool ShowObjectsBody = true;
        [Desc("显示单位资源", "show", true)] public bool ShowObjectsRes = true;
        [Desc("显示单位海拔", "show", true)] public bool ShowObjectsAltitude = true;
        [Desc("-", "show", true)] public string space1;

        [Desc("显示场景体素", "show", true)] public bool ShowSceneVoxel = true;
        [Desc("显示场景资源", "show", true)] public bool ShowSceneRes = true;
        [Desc("显示场景寻路", "show", true)] public bool ShowSceneNav = false;
        [Desc("-", "show", true)] public string space2;
    }
    //--------------------------------------------------------------------------------------------------------
    #region 编辑器发送到场景的消息
    namespace EditorToScene
    {
        /// <summary>
        /// 地形数据通知
        /// </summary>
        public class MsgInitPlugin : Notify
        {
            public string ZoneDataFactoryName;
            public MsgSetScene Scene;
            public Config CFG;
            public TerrainDefinitionMap TerrainMap;
            public UnitActionDefinitionMap UnitActionMap;
        }

        /// <summary>
        /// 地形数据通知
        /// </summary>
        public class MsgSetScene : Notify
        {
            public int SceneID;
            public string ProjectName;
            public string FileName;
            public string ResourceDir;
            public string ResourceProperty;
            public string VoxelFileName;
            public float ResourceStartX;
            public float ResourceStartY;
            public float SpaceDivSizeW;
            //public ISceneProperties SceneProperties;
            public ZoneInfo Data;

        }
        public abstract class MsgPutObject : Notify
        {
            public abstract SceneObjectData ObjData { get; }
        }

        /// <summary>
        /// 增加单位数据，如果单位已存在，则更新信息
        /// </summary>
        public class MsgPutUnit : MsgPutObject
        {
            public UnitData Data;
            public UnitInfo UnitData;
            public override SceneObjectData ObjData => Data;
        }

        /// <summary>
        /// 增加物品数据，如果单位已存在，则更新信息
        /// </summary>
        public class MsgPutItem : MsgPutObject
        {
            public ItemData Data;
            public ItemTemplate Item;
            public override SceneObjectData ObjData => Data;
        }

        /// <summary>
        /// 增加单位数据，如果单位已存在，则更新信息
        /// </summary>
        public class MsgPutPoint : MsgPutObject
        {
            public PointData Data;
            public override SceneObjectData ObjData => Data;
        }

        /// <summary>
        /// 增加单位数据，如果单位已存在，则更新信息
        /// </summary>
        public class MsgPutRegion : MsgPutObject
        {
            public RegionData Data;
            public override SceneObjectData ObjData => Data;
        }


        /// <summary>
        /// 增加装饰物数据，如果单位已存在，则更新信息
        /// </summary>
        public class MsgPutDecoration : MsgPutObject
        {
            public DecorationData Data;
            public override SceneObjectData ObjData => Data;
        }

        /// <summary>
        /// 增加区域数据，如果单位已存在，则更新信息
        /// </summary>
        public class MsgPutArea : MsgPutObject
        {
            public AreaData Data;
            public override SceneObjectData ObjData => Data;
        }

        /// <summary>
        /// 删除一个单位
        /// </summary>
        public class MsgRemoveObject : Notify
        {
            public string Name;
        }

        /// <summary>
        /// 单位被重命名
        /// </summary>
        public class MsgRenameObject : Notify
        {
            public string SrcName;
            public string DstName;
        }

        /// <summary>
        /// 由编辑器UI层选中一个单位
        /// </summary>
        public class MsgSelectObject : Notify
        {
            public string Name;
            /// <summary>
            /// 摄像机是否聚焦此单位
            /// </summary>
            public bool IsLocateCamera;
        }
        public class MsgObjectVisible : Notify
        {
            public HashMap<string, bool> state;
        }

        /// <summary>
        /// 设置是否显示地形阻挡
        /// </summary>
        public class MsgShowTerrain : Notify
        {
            public bool Show;
        }

        /// <summary>
        /// 定位摄像机
        /// </summary>
        public class MsgLocateCamera : Notify
        {
            public DeepCore.Geometry.Vector3 pos;
            public float X => pos.X;
            public float Y => pos.Y;
            public float Z => pos.Z;
        }

        /// <summary>
        /// 设置笔刷
        /// </summary>
        public class MsgSetTerrainBrush : Notify
        {
            public enum BrushType
            {
                Round,
                Rectangle,
            }

            public int ARGB = (int)(0x7F00FF00L);
            public int Size = 1;
            public BrushType Brush = BrushType.Round;

            public static float[] ToARGB_F(int ARGB)
            {
                float[] argb = new float[4];
                argb[0] = ((ARGB >> 24) & 0xFF) / 255f;
                argb[1] = ((ARGB >> 16) & 0xFF) / 255f;
                argb[2] = ((ARGB >> 8) & 0xFF) / 255f;
                argb[3] = ((ARGB) & 0xFF) / 255f;
                return argb;
            }

            public static int FromARGB_F(float[] argb)
            {
                int ARGB = 0;
                ARGB |= ((int)(argb[0] * 255)) << 24;
                ARGB |= ((int)(argb[1] * 255)) << 16;
                ARGB |= ((int)(argb[2] * 255)) << 8;
                ARGB |= ((int)(argb[3] * 255));
                return ARGB;
            }

            public static float[] ToRGBA_F(int ARGB)
            {
                float[] rgba = new float[4];
                rgba[3] = ((ARGB >> 24) & 0xFF) / 255f;
                rgba[0] = ((ARGB >> 16) & 0xFF) / 255f;
                rgba[1] = ((ARGB >> 8) & 0xFF) / 255f;
                rgba[2] = ((ARGB) & 0xFF) / 255f;
                return rgba;
            }

            public static int FromRGBA_F(float[] rgba)
            {
                int ARGB = 0;
                ARGB |= ((int)(rgba[3] * 255)) << 24;
                ARGB |= ((int)(rgba[0] * 255)) << 16;
                ARGB |= ((int)(rgba[1] * 255)) << 8;
                ARGB |= ((int)(rgba[2] * 255));
                return ARGB;
            }
        }

        /// <summary>
        /// 设置编辑模式
        /// </summary>
        public class MsgSetEditorMode : Notify
        {
            public const int MODE_OBJECT = 0;
            public const int MODE_TERRAIN = 1;

            public int Mode = MODE_OBJECT;

        }

        /// <summary>
        /// 编辑器关闭通知
        /// </summary>
        public class MsgEditorExit : Notify
        {
        }

        /// <summary>
        /// 编辑器保存通知
        /// </summary>
        public class MsgEditorSave : Notify
        {
        }


        public class MsgSceneResArgsChanged : Notify
        {
        }


        public class MsgAdjustAllObjectsPos : Notify
        {
            public float OffsetX;
            public float OffsetY;
            public float OffsetZ;
        }

        public class MsgDockObject : Notify
        {
            public enum Docking
            {
                All,
                Selected,
                Specify,
            }
            public Docking docking;
            public string objectName;
        }
    }
    #endregion
    //--------------------------------------------------------------------------------------------------------
    #region 场景发回编辑器的消息
    namespace SceneToEditor
    {
        /// <summary>
        /// 场景初始化状态
        /// </summary>
        public class RspEditorState : Notify
        {
            public const int STATE_SUCCEED = 1;

            public int State = STATE_SUCCEED;
        }
        /// <summary>
        /// 设置笔刷
        /// </summary>
        public class RspTerrainBrushChanged : Notify
        {
            public int Size = 1;
        }

        /// <summary>
        /// 有单位被选中
        /// </summary>
        public class RspOnObjectSelected : Notify
        {
            public string Name;
            public bool Selected;
        }

        /// <summary>
        /// 有单位位置改变
        /// </summary>
        public class RspObjectTransformChanged : Notify
        {
            public string Name;
            public float x;
            public float y;
            public float z;
            public float dir;
            public float scale = 1;
        }

        //         /// <summary>
        //         /// 有单位尺寸改变
        //         /// </summary>
        //         public class RspObjectSizeChanged : Notify
        //         {
        //             public string Name;
        //             public float x;
        //             public float y;
        //         }
        // 
        //         /// <summary>
        //         /// 路点链接数据改变
        //         /// </summary>
        //         public class RspPointLinkChanged : Notify
        //         {
        //             public string SrcPointName;
        //             public string DstPointName;
        //         }

        /// <summary>
        /// 最终回馈场景地形数据
        /// </summary>
        public class RspZoneFlagChanged : Notify
        {
            /// <summary>
            /// 场景坐标
            /// </summary>
            public int SceneX;
            /// <summary>
            /// 场景坐标
            /// </summary>
            public int SceneY;
            /// <summary>
            /// 标志
            /// </summary>
            public int Flag;


            public RspZoneFlagChanged() { }
            public RspZoneFlagChanged(int x, int y, int flag)
            {
                this.SceneX = x;
                this.SceneY = y;
                this.Flag = flag;
            }

        }

        /// <summary>
        /// 最终回馈场景地形数据
        /// </summary>
        public class RspZoneFlagBathChanged : Notify
        {
            public List<RspZoneFlagChanged> Flags = new List<RspZoneFlagChanged>();
            public bool TryGetBounds(out int minX, out int minY, out int maxX, out int maxY)
            {
                minX = int.MaxValue;
                minY = int.MaxValue;
                maxX = int.MinValue;
                maxY = int.MinValue;
                if (this.Flags.Count > 0)
                {
                    foreach (RspZoneFlagChanged dd in this.Flags)
                    {
                        minX = Math.Min(minX, dd.SceneX);
                        minY = Math.Min(minY, dd.SceneY);
                        maxX = Math.Max(maxX, dd.SceneX);
                        maxY = Math.Max(maxY, dd.SceneY);
                    }
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 摄像机位置或者尺寸改变
        /// </summary>
        public class RspCameraChanged : Notify
        {
            public bool RefreshMiniMap;
            public float X, Y, Z;
            public float X1, Y1, X2, Y2, X3, Y3, X4, Y4;
        }

        /// <summary>
        /// 编辑器存储高度图
        /// </summary>
        public class RspZoneHeightMapDataChanged : Notify, IExternalizable
        {
            public float[,] HeightMapData;
            public void WriteExternal(IOutputStream output)
            {
                if (HeightMapData != null)
                {
                    int w = HeightMapData.GetLength(0);
                    int h = HeightMapData.GetLength(1);
                    output.PutS32(w);
                    output.PutS32(h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            output.PutF32(HeightMapData[x, y]);
                        }
                    }
                }
                else
                {
                    output.PutS32(0);
                    output.PutS32(0);
                }
            }
            public void ReadExternal(IInputStream input)
            {
                int w = input.GetS32();
                int h = input.GetS32();
                if (w > 0 && h > 0)
                {
                    HeightMapData = new float[w, h];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            HeightMapData[x, y] = input.GetF32();
                        }
                    }
                }
                else
                {
                    HeightMapData = null;
                }
            }
        }


        /// <summary>
        /// 摄像机位置或者尺寸改变
        /// </summary>
        public class RspVoxelFileChanged : Notify
        {
            public string VoxelFileName;
        }


        public class RspObjectFieldChanged : Notify
        {
            public string Name;
            public string field;
            public object value;
            public object old_value;
        }
        public class RspMouseDown : Notify
        {
            public Vector3? rayTouchPlane;
            public Vector3? rayTouchVoxel;
        }
    }
    #endregion
    //--------------------------------------------------------------------------------------------------------
    #region 编辑器、场景请求事件 
    namespace SceneRequest
    {

        public class ReqAddObject : Request
        {
            public SceneObjectData Data;
        }
        public class RspAddObject : Response
        {
            public string ObjectName;
        }


        public class ReqUpdateObject : Request
        {
            public SceneObjectData Data;
            public string FieldName;
        }
        public class RspUpdateObject : Response
        {
            public string ObjectName;
        }


        public class ReqUpdateObjects : Request
        {
            public List<SceneObjectData> Datas;
        }
        public class RspUpdateObjects : Response
        {
            public List<string> ObjectNames;
        }
    }
    #endregion
    //--------------------------------------------------------------------------------------------------------
    #region 编辑器、场景请求事件 
    namespace Prewview
    {
        public class PreviewState : ISerializable
        {
            public bool ShowBody = true;
            public bool ShowScene = true;
            public bool ShowFlag = false;
        }
        public class PreviewUpdate : Notify
        {
            public ISerializable Template;
            public ISerializable Relation;
            public List<ISerializable> Templates;
            public Config GameConfig;
            public UnitActionDefinitionMap UnitActionMap;
            public ResourcePropertiesMap ResourcePropertiesMap;
            public bool CleanUp = false;
            public bool Refresh = true;
            public bool Focus;
            public override string ToString()
            {
                return $"PreviewUpdate : {Template} - {Relation}";
            }
        }
        public class PreviewResource : Notify
        {
            public string ResID;
            public ResourceType ResType;
            public string FullName;
            public bool Focus;
            public IResourceProperties PropertiesData;
            public PreviewResource() { }
            public PreviewResource(string resID, ResourceType resType, string fullName)
            {
                this.ResID = resID;
                this.ResType = resType;
                this.FullName = fullName;
            }
            public override string ToString()
            {
                return $"{ResID??FullName}";
            }
            public string Key => $"{ResType}:{ResID}";
        }


        public class PreviewResponse : Notify
        {
            public ISerializable Template;
            public ISerializable Relation;
        }

        public class PreviewResourceList : Notify
        {
            public List<PreviewResource> Resources = new List<PreviewResource>();
            public override string ToString()
            {
                return $"PreviewResourceList : {Resources.Count}";
            }
        }

    }
    #endregion
    //--------------------------------------------------------------------------------------------------------

}
