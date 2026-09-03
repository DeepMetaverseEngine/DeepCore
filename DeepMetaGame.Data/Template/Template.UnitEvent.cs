
using DeepCore;
using DeepCore.EventTrigger;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.ZoneEditor;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Template
{
    [MessageType(BattleConstants.UnitEventTemplate)]
    [Desc("单位事件触发")]
    public class UnitEventTemplate : TemplateData, IEventsTemplateData
    {
        [Desc("是否包含脚本", "0.模板")]
        public bool HasEvent => Events != null && Events.Count > 0;
        public IReadOnlyList<IEventDataNode> EventDataNodes => Events.ConvertAll(t => (IEventDataNode)t);

        [Desc(Category = "1.基础", Desc = "触发器支持多个实例")]
        public bool IsDuplicating = false;
        [Desc(Category = "1.基础", Desc = "所有事件", Editable = false)]
        public ArrayList<UnitEvent> Events = new ArrayList<UnitEvent>();
        [Desc(Category = "9.扩展", Desc = "扩展属性")]
        [Expandable]
        [NotNull] 
        public IEventProperties Properties;
        public override IPropertiesData PropertiesData => Properties;

        public UnitEventTemplate()
        {
            Properties = ZoneDataFactory.Factory.CreateProperties<IEventProperties>(this);
        }


        public UnitEvent GetEvent(string name)
        {
            foreach (var e in Events)
            {
                if (e.EventName == name) { return e; }
            }
            return null;
        }
    }



    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------//
}
