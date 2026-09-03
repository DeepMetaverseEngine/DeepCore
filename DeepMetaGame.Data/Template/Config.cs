using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data;
using DeepCore;
using DeepMetaGame.Data.ZoneEditor;

namespace DeepMetaGame.Data.Template
{
    //---------------------------------------------------------------------------------//

    [Desc("公共配置"), MessageType(BattleConstants.Config), Reflectible]
    public class Config : ISerializable, IFuncData
    {
        [Desc(category: "1.系统", desc: "系统帧率（服务端）")]
        public int SYSTEM_FPS = 30;
        [Desc(category: "1.系统", desc: "全局重力")]
        public float GLOBAL_GRAVITY = 9.8f;
        [Desc(category: "1.系统", desc: "全局挤开移动最大递归深度")]
        public uint GLOBAL_MOVE_IMPACT_DEPTH = 10;
        //-------------------------------------------------------------------------------
        [Desc(category: "1.默认", desc: "默认场景")]
        [TemplateID(typeof(SceneData))]
        public int DEFAULT_SCENE = 0;
        [Desc(category: "1.默认", desc: "默认角色")]
        [TemplateID(typeof(UnitInfo))]
        public int DEFAULT_UNIT = 0;
        //-------------------------------------------------------------------------------
        [Desc(category: "1.默认", desc: "预览场景")]
        [TemplateID(typeof(SceneData))]
        public int PREVIEW_SCENE = 0;
        //-------------------------------------------------------------------------------
        [Desc("资源后缀", "资源")]
        public string RESOURCE_EXTENSIONS = ".ab|.assetbundle|.png|.jpg|.mp3|.ogg|.cpj";
        [Desc("表格后缀", "资源")]
        public string TABLE_EXTENSIONS = ".xls|.xlsx|.json|.lua";
        //-------------------------------------------------------------------------------
        [Desc("在此范围内，可允许误差", "客户端")]
        public float CLIENT_UNIT_MOVE_MODIFY_MIN_RANGE = 2f;
        [Desc("超出此同步范围，立即将坐标修正", "客户端")]
        public float CLIENT_UNIT_MOVE_MODIFY_MAX_RANGE = 20f;
        //-------------------------------------------------------------------------------
        [Desc("单位进视野范围", "AOI")]
        public int CLIENT_SYNC_UNIT_MIN_RANGE = 20;
        [Desc("单位出视野范围", "AOI")]
        public int CLIENT_SYNC_UNIT_MAX_RANGE = 25;
        [Desc("客户端多久检测一次进入视野", "AOI")]
        public int CLIENT_UPDATE_LOOK_IN_INTERVAL_MS = 1000;
        [Desc("客户端多久检测一次进入视野", "AOI")]
        public int CLIENT_UPDATE_LOOK_OUT_INTERVAL_MS = 1000;
        //-------------------------------------------------------------------------------

        [Desc("单位移动最慢速度(低于最慢速度表示不能移动)", "移动")]
        public float OBJECT_MOVE_TO_MIN_STEP_SEC = 0.3f;
        [Desc("单位被单位阻挡，多少角度开始挤开（角度）", "移动")]
        public float OBJECT_MOVE_BLOCK_ELASTIC_ANGLE = 10f;
        //-------------------------------------------------------------------------------
        [Desc("单位间无碰撞总开关", "战斗")]
        public bool OBJECT_NONE_TOUCH = false;
        [Desc("玩家无碰撞", "战斗")]
        public bool PLAYER_NONE_TOUCH = false;

        [Desc("单位默认转身速度（弧度/秒）", "战斗")]
        public float UNIT_TURN_SPEED_SEC = 3.14f;
        [Desc("单位允许被鞭尸", "战斗")]
        public bool UNIT_CAN_WHIPLASH_BODY = false;
        //-------------------------------------------------------------------------------
        [Desc("单位被击飞向上速度", "受击")]
        public float OBJECT_DAMAGE_FLY_ZSPEED_SEC = 10.0f;
        [Desc("单位被击飞速度(距离/秒)", "受击")]
        public float OBJECT_DAMAGE_FLY_SPEED_SEC = 10.0f;
        [Desc("单位被击飞加速度(距离/秒) => (速度=速度+加速度)", "受击")]
        public float OBJECT_DAMAGE_FLY_SPEED_ADD = 0f;
        [Desc("单位被击飞阻力(每秒递减速度百分比)", "受击")]
        public float OBJECT_DAMAGE_FLY_SPEED_ACC = 0f;
        [Desc("单位被击飞各项参数浮动值百分比", "受击")]
        public float OBJECT_DAMAGE_FLY_ARGS_FACTOR_PCT = 0f;
        [Desc("单位受击时间(毫秒)", "受击")]
        public int OBJECT_DAMAGE_TIME_MS = 1000;
        //-------------------------------------------------------------------------------
        [Desc("单位AI更新时间(毫秒)，0表示每帧更新", "AI")]
        public int UNIT_AI_INTERVAL_MS = 0;

        [Desc("每隔一段时间检测一下是否要追(防止抖动)", "AI")]
        public int AI_FOLLOW_AND_ATTACK_HOLD_TIME_MS = 1000;
        [Desc("尝试追击过程中，调整射击范围(逃离)的比率", "AI")]
        public float AI_FOLLOW_AND_ATTACK_ADJUST_ESCAPE_PCT = 50;
        [Desc("尝试追击过程中，调整射击范围(反向弧度)", "AI")]
        public float AI_FOLLOW_AND_ATTACK_ADJUST_ESCAPE_ANGLE = 1;
        [Desc("观察一定范围的触发器，固定时间固定检测周围变化", "AI")]
        public int AI_VIEW_TRIGGER_CHECK_TIME_MS = 1000;
        [Desc("NPC每隔多长毫秒检测一次最大警戒距离", "AI")]
        public int AI_NPC_CHECK_IN_GUARD_LIMIT_TIME_MS = 500;
        [Desc("NPC攻击间歇时，有多少几率检测旁边有人，并散开，防止堆积在一个点。（百分比）", "AI")]
        public float AI_NPC_ATTACK_IDLE_SCATTER_PCT = 25;

        [Desc("仇恨列表上限", "AI")]
        public int AI_HATE_SYSTEM_CAPACITY = 10;
        [Desc("视野仇恨值", "AI")]
        public int AI_HATE_SYSTEM_ENTER_VIEW_HATE_VALUE = 1;
        //-------------------------------------------------------------------------------
        [Desc("寻路步长限制", "MOVE AI")]
        public int AI_FIND_PATH_STEP_LIMIT = 1000;
        [Desc("首次移动就寻路", "MOVE AI")]
        public bool AI_MOVE_FIRST_FIND_PATH = false;
        [Desc("移动过程中走不动时等待时间", "MOVE AI")]
        public int AI_MOVE_NOWAY_HOLD_TIME_MS = 1000;
        [Desc("移动过程中每次AI变化的停顿时间", "MOVE AI")]
        public int AI_MOVE_AI_HOLD_TIME_MS = 1000;
        [Desc("移动过程中绕过体积尺寸", "MOVE AI")]
        public float AI_MOVE_AI_BYPASS_SCALE = 2f;
        //-------------------------------------------------------------------------------
        [Desc("宠物跟随距离（靠近主人多少停止）", "宠物")]
        public float PET_FOLLOW_DISTANCE_MIN = 2f;
        [Desc("宠物跟随距离（离主人多远时跟随）", "宠物")]
        public float PET_FOLLOW_DISTANCE_MAX_ADD = 1f;
        [Desc("宠物强制跟随距离（距离太远，直接瞬移到主人）", "宠物")]
        public float PET_FOLLOW_DISTANCE_LIMIT_ADD = 10f;

        //-------------------------------------------------------------------------------
        [Desc("更新附近玩家距离", "空间")]
        public int SPACE_UPDATE_NEAR_PLAYER_RANGE = 1;
        [Desc("更新附近玩家间隔", "空间")]
        public int SPACE_UPDATE_NEAR_PLAYER_INTERVAL = 3000;
        //-------------------------------------------------------------------------------
        [DescAttribute("单位错误触发回收阈值", "容错")]
        public byte UNIT_ERROR_RECYCLE_THRESHOLD = 3;

        [DescAttribute("是否开启性能追踪", "性能追踪")]
        public bool ENABLE_PERFORMANCE_TRACE = false;
        [DescAttribute("执行时间触发警报阈值", "性能追踪")]
        public long MONITOR_WARNING_THRESHOLDMS = 9;
        [DescAttribute("是否开启异常弹框", "性能追踪")]
        public bool ENABLE_SHOW_ERROR_MSG_BOX = false;
        //-------------------------------------------------------------------------------

        [Desc(Editable = false)]
        public FuncTableGroup FuncID;
        IFuncTableGroup IFuncData.Tables { get => this.FuncID; set => this.FuncID = (FuncTableGroup)value; }
    }

}
