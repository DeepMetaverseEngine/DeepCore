using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;

namespace DeepMetaGame.Data.Template
{
    //---------------------------------------------------------------------------------//


    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//


    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//


    /// <summary>
    /// 光环
    /// </summary>
    [MessageType(BattleConstants.AuraTemplate)]
    [Desc("光环")]
    public class AuraTemplate : TemplateData
    {

        [Desc(Category = "1.基础", Desc = "范围")]
        public float Range = 10f;
        [Desc(Category = "1.基础", Desc = "光环期望作用目标")]
        public SkillTemplate.CastTarget ExpectTarget = SkillTemplate.CastTarget.AlliesIncludeSelf;
        [Desc(Category = "1.基础", Desc = "生命周期(毫秒)，0表示无限")]
        public int LifeTimeMS = 0;
        [Desc(Category = "1.基础", Desc = "当技能停用时移除光环")]
        public bool RemoveOnSkillDeactivated = false;

        [Desc(Category = "2.动作", Desc = "绑定BUFF")]
        public ArrayList<LaunchBuff> BindingBuffs = new ArrayList<LaunchBuff>();

        [Desc(Category = "3.资源", Desc = "模型名字或者Perfab名字")]
        [ResourceID(ResourceType.Object)] public string ResourceFileName;

        [Desc(Category = "3.资源", Desc = "模型名字或者Perfab名字")]
        public int ResourceFileID
        {
            get
            {
                if (Parser.TryParseInt(ResourceFileName, out var resId))
                    return resId;
                return 0;
            }
        }

        [Desc(Category = "3.资源", Desc = "是否循环播放动画")]
        public bool IsCycAnim = true;

        [Desc(Category = "9.扩展", Desc = "扩展属性")]
        [Expandable]
        [NotNull]
        public IAuraProperties Properties;
        public override IPropertiesData PropertiesData => this.Properties;

        public AuraTemplate()
        {
            Properties = ZoneDataFactory.Factory.CreateProperties<IAuraProperties>(this);
        }
    }


    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//
}
