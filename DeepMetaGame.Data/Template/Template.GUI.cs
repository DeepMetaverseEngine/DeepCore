using DeepCore;
using DeepCore.EventTrigger;
using DeepCore.GUI.Data;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.GUI.Meta;
using DeepMetaGame.Data.ZoneEditor;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepMetaGame.Data.Template
{
    [MessageType(BattleConstants.BattleUITemplate)]
    [Desc("界面")]
    public class BattleUITemplate : TemplateData, IEventsTemplateData
    {
        [Desc("是否包含脚本", "0.模板")]
        public bool HasEvent => Events != null && Events.Count > 0;
        public IReadOnlyList<IEventDataNode> EventDataNodes => Events.ConvertAll(t => (IEventDataNode)t);

        [Desc(Category = "1.窗体", Desc = "只允许单独实例")]
        public bool SingleInstance = true;

        [Desc(Editable = false)]
        public List<UEComponentMeta> Forms = new List<UEComponentMeta>();

        [Desc(Category = "1.基础", Desc = "所有事件", Editable = false)]
        public ArrayList<GUIEvent> Events = new ArrayList<GUIEvent>();
        public override IPropertiesData PropertiesData => null;

        //---------------------------------------------------------------------------------------------------------------

        public GUIEvent GetEvent(string name)
        {
            foreach (var e in Events)
            {
                if (e.EventName == name) { return e; }
            }
            return null;
        }

        public void ForEachMeta<ST>(ST st, ForEachAction<ST, UEComponentMeta> action)
        {
            if (Forms != null)
            {
                foreach (var meta in Forms)
                {
                    ForEachMeta(st, meta, action);
                }
            }
        }
        public void ForEachMeta<ST>(ST st, UEComponentMeta meta, ForEachAction<ST, UEComponentMeta> action)
        {
            action(st, meta);
            if (meta is UEContainerMeta containerMeta)
            {
                if (containerMeta.Childs != null)
                {
                    foreach (var subMeta in containerMeta.Childs)
                    {
                        ForEachMeta(st, subMeta, action);
                    }
                }
            }
        }
        public UEComponentMeta ForEachMeta<ST>(ST st, ForEachPredicate<ST, UEComponentMeta> action)
        {
            if (Forms != null)
            {
                foreach (var meta in Forms)
                {
                    if (ForEachMeta(st, meta, action) is UEComponentMeta ret)
                    {
                        return ret;
                    }
                }
            }
            return null;
        }
        public UEComponentMeta ForEachMeta<ST>(ST st, UEComponentMeta meta, ForEachPredicate<ST, UEComponentMeta> action)
        {
            if (action(st, meta))
            {
                return meta;
            }
            if (meta is UEContainerMeta containerMeta)
            {
                if (containerMeta.Childs != null)
                {
                    foreach (var subMeta in containerMeta.Childs)
                    {
                        if (ForEachMeta(st, subMeta, action) is UEComponentMeta ret)
                        {
                            return ret;
                        }
                    }
                }
            }
            return null;
        }



        public async Task ForEachMetaAsync<ST>(ST st, ForEachActionAsync<ST, UEComponentMeta> action)
        {
            if (Forms != null)
            {
                foreach (var meta in Forms)
                {
                    await ForEachMetaAsync(st, meta, action);
                }
            }
        }
        public async Task ForEachMetaAsync<ST>(ST st, UEComponentMeta meta, ForEachActionAsync<ST, UEComponentMeta> action)
        {
            await action(st, meta);
            if (meta is UEContainerMeta containerMeta)
            {
                if (containerMeta.Childs != null)
                {
                    foreach (var subMeta in containerMeta.Childs)
                    {
                        await ForEachMetaAsync(st, subMeta, action);
                    }
                }
            }
        }
        public async Task<UEComponentMeta> ForEachMetaAsync<ST>(ST st, ForEachPredicateAsync<ST, UEComponentMeta> action)
        {
            if (Forms != null)
            {
                foreach (var meta in Forms)
                {
                    if (await ForEachMetaAsync(st, meta, action) is UEComponentMeta ret)
                    {
                        return ret;
                    }
                }
            }
            return null;
        }
        public async Task<UEComponentMeta> ForEachMetaAsync<ST>(ST st, UEComponentMeta meta, ForEachPredicateAsync<ST, UEComponentMeta> action)
        {
            if (await action(st, meta))
            {
                return meta;
            }
            if (meta is UEContainerMeta containerMeta)
            {
                if (containerMeta.Childs != null)
                {
                    foreach (var subMeta in containerMeta.Childs)
                    {
                        if (await ForEachMetaAsync(st, subMeta, action) is UEComponentMeta ret)
                        {
                            return ret;
                        }
                    }
                }
            }
            return null;
        }

        public bool HasFairyGUIComponentMeta
        {
            get
            {
                if (ForEachMeta<UEComponentMeta>(null, (st, meta) =>
                {
                    if (meta is UEFairyGUIComponentMeta)
                    {
                        return true;
                    }
                    return false;
                }) is UEFairyGUIComponentMeta)
                {
                    return true;
                }
                return false;
            }
        }

        //---------------------------------------------------------------------------------------------------------------
    }



}
