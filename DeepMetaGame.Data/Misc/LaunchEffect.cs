using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Misc
{
    [Desc("资源变形"), Expandable, MessageType(BattleConstants.ResourceTransform)]
    public class ResourceTransform : ISerializable
    {
        [Desc("特效资源偏移", "0.资源")] public Vector3 localPosition;
        [Desc("特效资源旋转", "0.资源")] public Vector3 localEuler;
        [Desc("特效资源缩放", "0.资源")] public Vector3 localScale = Vector3.One;
    }
    /// <summary>
    /// 触发特效，一般只用于显示
    /// </summary>
    [MessageType(BattleConstants.LaunchEffect)]
    [Desc("触发特效，一般只用于显示")]
    [Expandable]
    public class LaunchEffect : ISNData, IPropertiesOwner
    {
        //--------------------------------------------------------------------------------------------------------------------------------
        [Desc("特效资源名", "0.资源")][ResourceID(ResourceType.Effect)] public string Name;
        [Desc("特效资源变形", "0.资源")] public ResourceTransform ResTransform;

        [Desc("音效", "0.资源")][ResourceID(ResourceType.Sound_Effect)] public string SoundName;
        [Desc("特效动画名", "0.资源")][ResourceID(ResourceType.Animation)] public string AnimName;
        [Desc("文字颜色", "0.资源"), ColorValue] public int TextColor = 0;
        [Desc("特效资源Id", "0.资源")] public int ResId { get { if (Parser.TryParseInt(Name, out var resId)) return resId; return 0; } }
        [Desc("音效", "0.资源")] public int SoundID { get { if (Parser.TryParseInt(SoundName, out var resId)) return resId; return 0; } }
        //--------------------------------------------------------------------------------------------------------------------------------
        [Desc(Category = "4.运动-绑定", Desc = "如果为绑定，绑定Z高度偏移量")] public float BindingOffsetZ = 0f;
        [Desc(Category = "4.运动-绑定", Desc = "如果为绑定，绑定角度偏移量")] public float BindingOffsetAngle360 = 0f;
        [Desc(Category = "4.运动-绑定", Desc = "如果为绑定，绑定长度偏移量")] public float BindingOffsetDistance = 0f;

        [Desc("触发特效是否绑定在单位(坐标)", "绑定")] public bool BindBody;
        [Desc("触发特效是否绑定在单位(方向)", "绑定")] public bool BindBodyDirection = true;
        [Desc("绑定挂载点位置", "绑定")] public string BindPartName;
        [Desc("如果不为0，则特效以该尺寸缩放", "绑定")] public float ScaleToBodySize = 1f;
        [Desc("是否根据绑定物bodysize缩放")] public bool AutoFitBindBodySize = false;
        [Desc("高度对齐方式", "绑定")] public VoxelAnchor BodyVoxelAnchor = VoxelAnchor.Floating;
        //--------------------------------------------------------------------------------------------------------------------------------
        [Desc("特效持续时间。（0表示只播放一次；-1表示无限循环。）", "时间")] public int EffectTimeMS = 0;
        [Desc("特效消亡后的持续时间", "时间")] public int DeadTimeMS = 0;
        [Desc("是否循环", "时间")] public bool IsLoop = false;
        [Desc("特效动画速度", "时间")] public float TimeScale = 1f;
        //--------------------------------------------------------------------------------------------------------------------------------
        [Desc("模糊", "特效能力")] public EffectBlur Blur = null;
        [Desc("摄像机", "特效能力")] public EffectCameraMotion Camera = null;
        [Desc("预警形状", "特效能力")] public UnitActionData.AttackShape WarningShape = null;

        [Desc("嵌套特效", "特效扩展")] public List<LaunchEffect> SubEffects;
        [Desc("自定义字段", "特效扩展")] public string Tag;
        //--------------------------------------------------------------------------------------------------------------------------------
        [MessageType(BattleConstants.EffectBlur)]
        [Desc("模糊")]
        public class EffectBlur : IBaseFuncData
        {

            [Desc("模糊强度（1-100）", "运动模糊")]
            public float BlurStrength = 0;
            [Desc("模糊半径（0-1）", "运动模糊")]
            public float BlurRadius = 1;
            [Desc("开始模糊需要时间(毫秒)", "运动模糊")]
            public int BlurBeginTime = 0;
            [Desc("模糊后持续时间(毫秒)", "运动模糊")]
            public int BlurWaitTime = 0;
            [Desc("模糊消失需要时间(毫秒)", "运动模糊")]
            public int BlurEndTime = 0;
        }

        [MessageType(BattleConstants.EffectCameraMotion)]
        [Desc("摄像机")]
        public class EffectCameraMotion : IBaseFuncData
        {
            [Desc("摄像机动画名字")]
            public string CameraAnimation;

            [Desc("震屏时长(毫秒)", "震屏")]
            public int EarthQuakeMS;
            [Desc("震屏幅度", "震屏")]
            public float EarthQuakeXYZ;

            [Desc("镜头拉近距离，正值为拉近，负值为拉远", "镜头拉近")]
            public float CameraDistance = 0;
            [Desc("镜头拉到位置所需时间", "镜头拉近")]
            public int CameraBeginTime = 0;
            [Desc("镜头到位后持续时间(毫秒)", "镜头拉近")]
            public int CameraWaitTime = 0;
            [Desc("镜头还原需要时间(毫秒)", "镜头拉近")]
            public int CameraEndTime = 0;
        }

        [MessageType(BattleConstants.EffectWarning)]
        [Desc("（瘸的）预警")]
        public class EffectWarning : IBaseFuncData
        {
            [Desc("预警显示类型", "预警")]
            public WarningType WarnType = WarningType.WARNING_TYPE_NONE;
            [Desc("x轴缩放比例", "预警")]
            public float WarnScaleX = 1;
            [Desc("z轴缩放比例", "预警")]
            public float WarnScaleZ = 1;
            [Desc("播放速度", "预警")]
            public float WarnSpeed = 1;
            [Desc("角度360(仅对扇形预警有效)", "预警")]
            public float WarnDegree = 80;
        }
        public enum WarningType : byte
        {
            [Desc("无预警")]
            WARNING_TYPE_NONE = 0,
            [Desc("矩形预警")]
            WARNING_TYPE_SQUARE = 1,
            [Desc("圆形预警")]
            WARNING_TYPE_CIRCLE = 2,
            [Desc("扇形预警")]
            WARNING_TYPE_SECTOR = 3,

        }



        /// <summary>
        /// 用户自定义扩展属性
        /// </summary>
        [Desc("用户自定义扩展属性", "扩展")]
        [Expandable]
        [NotNull]
        public IEffectProperties Properties;
        public IPropertiesData PropertiesData => this.Properties;

        public LaunchEffect()
        {
            Properties = ZoneDataFactory.Factory.CreateProperties<IEffectProperties>(this);
        }

        public override string ToString()
        {
            return $"触发特效:{Name}";
        }

    }

}
