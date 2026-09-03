using DeepCore;
using DeepCore.EventTrigger;
using DeepCore.FuncData;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DeepMetaGame.Data.ZoneEditor
{
    //--------------------------------------------------------------------------------------------------------

    [MessageType(BattleConstants.SceneData)]
    [Desc("关卡数据")]
    [TableClass("ID")]
    public class SceneData : TemplateData, IExternalizable, IEventsTemplateData
    {
        [Desc("是否包含脚本", "0.模板")]
        public bool HasEvent => Host?.Events != null && Host.Events.Count > 0;
        public IReadOnlyList<IEventDataNode> EventDataNodes => Host?.Events.ConvertAll(t => (IEventDataNode)t);

        [LocalizationText]
        [Desc("场景简介", "1.基础", true)]
        public string Desc;


        [Desc("场景资源", "2.资源", true)]
        [ResourceID(ResourceType.Scene)] public string FileName;

        [Desc("模型资源ID", "2.资源")]
        public int FileResId
        {
            get
            {
                if (Parser.TryParseInt(FileName, out var resId))
                    return resId;
                return 0;
            }
        }

        [Desc("体素资源", "2.资源", true)]
        [ResourceID(ResourceType.Binary)] public string VoxelFileName;

        [Desc("场景背景音乐", "2.资源", true)]
        [ResourceID(ResourceType.Sound_BGM)] public string BGM;

        [Desc("场景资源属性", "2.资源", true)]
        public string ResourceProperty;

        [Desc("空间分割尺寸", "2.资源")]
        public int SpaceDivW = 18;
        //---------------------------------------------------------
        [Expandable]
        [Desc("体素信息", "3.体素信息")]
        public VoxelInfo Voxel = new VoxelInfo();
        [Desc("", "", false)]
        public TerrainData Terrain = new TerrainData();
        [Desc("", "", Editable = false)]
        public TerrainDefinitionMap OverrideTerrainDefinition;

        //---------------------------------------------------------


        //---------------------------------------------------------

        [Desc("可进入玩家数量", "4.房间")]
        public int FullPlayer = 1000;
        [Desc("最大支持玩家数量", "4.房间")]
        public int MaxPlayer = 1100;
        [Desc("最大单位数量", "4.房间")]
        public int MaxUnit = 10000;
        [Desc("是否为公共房间", "4.房间")]
        public bool IsPublicMap = false;
        [Desc("默认单位等级", "4.房间")]
        [TemplateLevel]
        public int DefaultUnitLevel = 0;
        [Desc("单位掉线即离开场景", "4.房间")]
        public bool RemoveUnitOnDisconnect = false;
        [Desc("启用ServerAOI", "4.房间")]
        public bool EnableServerAOI = true;
        [Desc("游戏总时间，0表示无限制", "4.房间", true)]
        public int TotalTimeLimitSEC;

        [Desc("覆盖配置", "5.配置")]
        public Config OverrideConfig;
        [Desc("覆盖扩展配置", "5.配置")]
        public ICommonConfig OverrideExtConfig;
        //---------------------------------------------------------
        [Desc("所有路点", "单位", false)]
        public ArrayList<PointData> Points = new ArrayList<PointData>();
        [Desc("所有区域", "单位", false)]
        public ArrayList<RegionData> Regions = new ArrayList<RegionData>();
        [Desc("所有装饰物", "单位", false)]
        public ArrayList<DecorationData> Decorations = new ArrayList<DecorationData>();
        [Desc("所有单位", "单位", false)]
        public ArrayList<UnitData> Units = new ArrayList<UnitData>();
        [Desc("所有物品", "单位", false)]
        public ArrayList<ItemData> Items = new ArrayList<ItemData>();
        [Desc("所有区域", "单位", false)]
        public ArrayList<AreaData> Areas = new ArrayList<AreaData>();
        //---------------------------------------------------------
        public List<SceneObjectData> AllObjects
        {
            get
            {
                var list = new List<SceneObjectData>();
                list.AddRange(Points);
                list.AddRange(Regions);
                list.AddRange(Decorations);
                list.AddRange(Units);
                list.AddRange(Items);
                list.AddRange(Areas);
                return list;
            }
        }

        [Desc("所有能力", "5.能力", true)]
        public ArrayList<SceneAbilityData> Abilities = new ArrayList<SceneAbilityData>();
        public T GetAbilityOf<T>() where T : EditorAbilityData
        {
            foreach (var ab in Abilities)
            {
                if (ab is T)
                {
                    return ab as T;
                }
            }
            return null;
        }
        //---------------------------------------------------------
        public class SceneHostData
        {
            [Desc("脚本", "5.事件 - 服务端", false)]
            [SceneScriptID]
            public string Script;
            [Desc("事件", "5.事件 - 服务端", false)]
            public ArrayList<ZoneEvent> Events = new ArrayList<ZoneEvent>();
            [Desc("事件", "5.事件 - 服务端", false)]
            public ArrayList<IZoneEnvironmentVar> EnvironmentVars = new ArrayList<IZoneEnvironmentVar>();
        }
        [Desc(Editable = false)]
        //[XmlSerializable(XmlProperty.NoSerialize)]
        public SceneHostData Host = new SceneHostData();
        //---------------------------------------------------------        
        [Desc(Category = "6.事件", Desc = "绑定公共触发事件ID")]
        [TemplatesID(typeof(UnitEventTemplate)), Expandable]
        public ArrayList<int> Events = new ArrayList<int>();
        //------------------------------------------------------------------------------------------

        [Desc("扩展属性", "7.扩展")]
        [Expandable]
        [NotNull]
        public ISceneProperties Properties;
        public override IPropertiesData PropertiesData => this.Properties;
        //---------------------------------------------------------
        public SceneData()
        {
            Properties = ZoneDataFactory.Factory.CreateProperties<ISceneProperties>(this);
        }
        //---------------------------------------------------------
        public ZoneInfo ZoneData
        {
            get { return Terrain.ZoneData; }
        }
        public void SetTerrain(ZoneInfo zonedata, bool save)
        {
            Terrain.SetTerrain(zonedata, save);
        }
        //---------------------------------------------------------
        public int GetID()
        {
            return ID;
        }


        public bool IsDecorationChanged
        {
            get
            {
                bool ret = false;
                foreach (var dd in Decorations) { if (dd.Blockable) return true; }
                return ret;
            }
        }

        //---------------------------------------------------------
        [Desc("仅在保存时，用于缓存当前场景所有SN序列号", "", false)]
        private ArrayList<ISNData> serial_datas;
        public ArrayList<ISNData> GetSerialDatas()
        {
            if (serial_datas == null)
            {
                serial_datas = new ArrayList<ISNData>();
                PropertyUtil.CollectFieldTypeValues(this, serial_datas);
                serial_datas.TrimExcess();
            }
            return serial_datas;
        }

        //---------------------------------------------------------------------------------------------------------------
        #region RegionUtil
        //---------------------------------------------------------------------------------------------------------------
        public SceneObjectData GetFlagByName(string name)
        {
            return FindFlag(e => e.Name == name);
        }
        public bool TryFindFlagByName(string name, out SceneObjectData ret)
        {
            return TryFindFlag(e => e.Name == name, out ret);
        }
        public bool TryFindFlag(Predicate<SceneObjectData> func, out SceneObjectData ret)
        {
            ret = FindFlag(func);
            return ret != null;
        }
        public SceneObjectData FindFlag(Predicate<SceneObjectData> func)
        {
            if (Regions.TryFind(func, out var rd)) return rd;
            if (Decorations.TryFind(func, out var dd)) return dd;
            if (Points.TryFind(func, out var pd)) return pd;
            if (Units.TryFind(func, out var ud)) return ud;
            if (Items.TryFind(func, out var id)) return id;
            if (Areas.TryFind(func, out var ad)) return ad;
            return null;
        }
        public bool RemoveFlag(Predicate<SceneObjectData> func)
        {
            if (Regions.TryFind(func, out var rd)) return Regions.Remove(rd);
            if (Decorations.TryFind(func, out var dd)) return Decorations.Remove(dd);
            if (Points.TryFind(func, out var pd)) return Points.Remove(pd);
            if (Units.TryFind(func, out var ud)) return Units.Remove(ud);
            if (Items.TryFind(func, out var id)) return Items.Remove(id);
            if (Areas.TryFind(func, out var ad)) return Areas.Remove(ad);
            return false;
        }
        public bool RemoveFlag(SceneObjectData rd)
        {
            return RemoveFlag(f => f == rd);
        }
        public bool AddFlag(SceneObjectData data)
        {
            if (!TryFindFlagByName(data.Name, out _))
            {
                if (data is RegionData rg) this.Regions.Add(rg);
                else if (data is DecorationData dc) this.Decorations.Add(dc);
                else if (data is PointData pt) this.Points.Add(pt);
                else if (data is UnitData ud) this.Units.Add(ud);
                else if (data is ItemData id) this.Items.Add(id);
                else if (data is AreaData ad) this.Areas.Add(ad);
                return true;
            }
            return false;
        }
        public delegate bool UpdateFlagAction(SceneObjectData src, SceneObjectData dst);
        public bool AddOrUpdateFlag(SceneObjectData dstFlag, UpdateFlagAction func)
        {
            if (GetFlagByName(dstFlag.Name) is SceneObjectData srcFlag)
            {
                if (srcFlag.GetType() == dstFlag.GetType())
                {
                    if (func(srcFlag, dstFlag))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return AddFlag(dstFlag);
            }
        }
        public bool ForEachFlags<ST>(ST st, ForEachPredicate<ST, SceneObjectData> action)
        {
            foreach (var rg in Regions.ToArray()) { if (action(st, rg)) return true; }
            foreach (var rg in Decorations.ToArray()) { if (action(st, rg)) return true; }
            foreach (var rg in Points.ToArray()) { if (action(st, rg)) return true; }
            foreach (var rg in Units.ToArray()) { if (action(st, rg)) return true; }
            foreach (var rg in Items.ToArray()) { if (action(st, rg)) return true; }
            foreach (var rg in Areas.ToArray()) { if (action(st, rg)) return true; }
            return false;
        }
        //---------------------------------------------------------------------------------------------------------------
        public T GetFlagByName<T>(string name) where T : SceneObjectData
        {
            return FindFlagAs<T>(e => e.Name == name);
        }
        public bool TryFindFlagByNameAs<T>(string name, out T ret) where T : SceneObjectData
        {
            return TryFindFlagAs(e => e.Name == name, out ret);
        }
        public bool TryFindFlagAs<T>(Predicate<T> func, out T ret) where T : SceneObjectData
        {
            ret = FindFlagAs(func);
            return ret != null;
        }
        public T FindFlagAs<T>(Predicate<T> func) where T : SceneObjectData
        {
            if (typeof(T) == typeof(RegionData) && Regions.TryFind(r => func(r as T), out var rd)) return rd as T;
            if (typeof(T) == typeof(DecorationData) && Decorations.TryFind(r => func(r as T), out var dd)) return dd as T;
            if (typeof(T) == typeof(PointData) && Points.TryFind(r => func(r as T), out var pd)) return pd as T;
            if (typeof(T) == typeof(UnitData) && Units.TryFind(r => func(r as T), out var ud)) return ud as T;
            if (typeof(T) == typeof(ItemData) && Items.TryFind(r => func(r as T), out var id)) return id as T;
            if (typeof(T) == typeof(AreaData) && Areas.TryFind(r => func(r as T), out var ad)) return ad as T;
            return null;
        }
        //---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 获取场景中，玩家出生点对应的UnitTemplateID
        /// </summary>
        /// <param name="mData"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public int GetTestActorTemplateID(Random random = null)
        {
            var list = new List<PlayerStartAbilityData>();
            {
                foreach (RegionData rd in Regions)
                {
                    if (rd.Abilities != null)
                    {
                        foreach (EditorAbilityData tg in rd.Abilities)
                        {
                            if (tg is PlayerStartAbilityData start)
                            {
                                list.Add(start);
                            }
                        }
                    }
                }
                if (list.Count > 0)
                {
                    if (random == null) { random = CUtils.Random; }
                    return random.GetRandomInCollection(list).TestActorTemplateID;
                }
            }
            return 0;
        }


        public HashMap<int, RegionData> GetStartRegionsForceMap()
        {
            var ret = new HashMap<int, RegionData>();
            foreach (RegionData rdata in Regions)
            {
                if (rdata.Abilities != null && rdata.Enable)
                {
                    foreach (EditorAbilityData td in rdata.Abilities)
                    {
                        if (td is PlayerStartAbilityData)
                        {
                            PlayerStartAbilityData tgd = td as PlayerStartAbilityData;
                            ret.Put(tgd.START_Force, rdata);
                        }
                    }
                }
            }
            return ret;
        }

        public ArrayList<RegionData> GetStartRegionsList()
        {
            ArrayList<RegionData> ret = new ArrayList<RegionData>();
            foreach (RegionData rdata in Regions)
            {
                if (rdata.Abilities != null)
                {
                    foreach (EditorAbilityData td in rdata.Abilities)
                    {
                        if (td is PlayerStartAbilityData)
                        {
                            PlayerStartAbilityData tgd = td as PlayerStartAbilityData;
                            ret.Add(rdata);
                        }
                    }
                }
            }
            return ret;
        }
        public bool TryGetStartTestRegion(out RegionData region, out PlayerStartAbilityData start, Random random = null)
        {
            var list = GetStartRegionsList();
            if (list.Count > 0)
            {
                if (random != null)
                {
                    random.RandomList(list);
                }
                var ret = list[0];
                start = ret.GetAbilityOf<PlayerStartAbilityData>();
                region = ret;
                return true;
            }
            start = null;
            region = null;
            return false;
        }

        public bool TryGetStartTestUnit(TemplateManager templates, out RegionData region, out PlayerStartAbilityData start, out UnitInfo info, Random random = null)
        {
            var list = GetStartRegionsList();
            if (list.Count > 0)
            {
                if (random != null)
                {
                    random.RandomList(list);
                }
                foreach (RegionData r in list)
                {
                    if (TryGetStartTestUnit(templates, r, out info, out start))
                    {
                        region = r;
                        return true;
                    }
                }
            }
            info = null;
            start = null;
            region = null;
            return false;
        }
        private bool TryGetStartTestUnit(TemplateManager templates, RegionData startRegion, out UnitInfo info, out PlayerStartAbilityData start)
        {
            PlayerStartAbilityData tgd = startRegion.GetAbilityOf<PlayerStartAbilityData>();
            int actorTemplateID = tgd.TestActorTemplateID;
            start = tgd;
            info = templates.GetUnit(actorTemplateID);
            if (info != null)
            {
                return true;
            }
            return false;
        }

        public void CleanupNexts()
        {
            ForEachFlags(this, (st, obj) =>
            {
                if (obj is SceneVirtualObjectData vobj && vobj.NextNames != null)
                {
                    foreach (var nextName in vobj.NextNames.ToArray())
                    {
                        if (!TryFindFlagByName(nextName, out var next))
                        {
                            vobj.NextNames.Remove(nextName);
                        }
                    }
                }
                return false;
            });
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------
        #region Combine
        public class CombineData<T>
        {
            public SceneData Owner;
            public T Data;
        }
        public void Combine(ICollection<SceneData> scenes)
        {
            Host = new SceneHostData();
            var conflicts = new ArrayList<CombineConflictInfo>();
            {
                Points = new ArrayList<PointData>();
                Regions = new ArrayList<RegionData>();
                Decorations = new ArrayList<DecorationData>();
                Units = new ArrayList<UnitData>();
                Items = new ArrayList<ItemData>();
                Areas = new ArrayList<AreaData>();
                this.Abilities = new ArrayList<SceneAbilityData>();
                Host.Events = new ArrayList<ZoneEvent>();
                Host.EnvironmentVars = new ArrayList<IZoneEnvironmentVar>();

                var scenes_Attributes = new ArrayList<CombineData<string>>();
                var scenes_Points = new ArrayList<CombineData<PointData>>();
                var scenes_Regions = new ArrayList<CombineData<RegionData>>();
                var scenes_Decorations = new ArrayList<CombineData<DecorationData>>();
                var scenes_Units = new ArrayList<CombineData<UnitData>>();
                var scenes_Items = new ArrayList<CombineData<ItemData>>();
                var scenes_Areas = new ArrayList<CombineData<AreaData>>();
                var scenes_Abilities = new ArrayList<CombineData<SceneAbilityData>>();
                var scenes_Events = new ArrayList<CombineData<ZoneEvent>>();
                var scenes_EnvironmentVars = new ArrayList<CombineData<IZoneEnvironmentVar>>();

                foreach (var sd in scenes)
                {
                    AddRange(sd, scenes_Attributes, sd.Attributes, (a, b) => false);
                    AddRange(sd, scenes_Points, sd.Points, (a, b) => a.Name == b.Name);
                    AddRange(sd, scenes_Regions, sd.Regions, (a, b) => a.Name == b.Name);
                    AddRange(sd, scenes_Decorations, sd.Decorations, (a, b) => a.Name == b.Name);
                    AddRange(sd, scenes_Units, sd.Units, (a, b) => a.Name == b.Name);
                    AddRange(sd, scenes_Items, sd.Items, (a, b) => a.Name == b.Name);
                    AddRange(sd, scenes_Areas, sd.Areas, (a, b) => a.Name == b.Name);
                    AddRange(sd, scenes_Abilities, sd?.Abilities, (a, b) => false);
                    AddRange(sd, scenes_Events, sd.Host?.Events, (a, b) => a.EventName == b.EventName);
                    AddRange(sd, scenes_EnvironmentVars, sd.Host?.EnvironmentVars, (a, b) => a.Key == b.Key);
                }
                if (conflicts.Count > 0)
                {
                    throw new CombineDataException(conflicts);
                }
                Attributes.AddRange(scenes_Attributes.ConvertAll(t => t.Data).ToArray());
                Points.AddRange(scenes_Points.ConvertAll(t => t.Data));
                Regions.AddRange(scenes_Regions.ConvertAll(t => t.Data));
                Decorations.AddRange(scenes_Decorations.ConvertAll(t => t.Data));
                Units.AddRange(scenes_Units.ConvertAll(t => t.Data));
                Items.AddRange(scenes_Items.ConvertAll(t => t.Data));
                Areas.AddRange(scenes_Areas.ConvertAll(t => t.Data));
                this.Abilities.AddRange(scenes_Abilities.ConvertAll(t => t.Data));
                Host.Events.AddRange(scenes_Events.ConvertAll(t => t.Data));
                Host.EnvironmentVars.AddRange(scenes_EnvironmentVars.ConvertAll(t => t.Data));
            }

            void AddRange<T>(SceneData sa, ArrayList<CombineData<T>> list, IList<T> add, Func<T, T, bool> find)
            {
                if (add == null) return;
                foreach (var da in add)
                {
                    if (da != null)
                    {
                        var tb = list.Find(ttb => find(ttb.Data, da));
                        if (tb != null)
                        {
                            var db = tb.Data;
                            var sb = tb.Owner;
                            var xa = XmlUtil.ObjectToXmlString(da);
                            var xb = XmlUtil.ObjectToXmlString(db);
                            if (xa != xb)
                            {
                                conflicts.Add(new CombineConflictInfo(sa, da, xa, sb, db, xb));
                            }
                        }
                        else
                        {
                            list.Add(new CombineData<T>() { Owner = sa, Data = da });
                        }
                    }
                }
            }
        }
        public class CombineConflictInfo
        {
            public SceneData SceneA { get; }
            public SceneData SceneB { get; }
            public object DataA { get; }
            public object DataB { get; }
            public string XmlA { get; }
            public string XmlB { get; }
            public CombineConflictInfo(SceneData sa, object da, string xa, SceneData sb, object db, string xb)
            {
                SceneA = sa;
                SceneB = sb;
                DataA = da;
                DataB = db;
                XmlA = xa;
                XmlB = xb;
            }
        }

        public class CombineDataException : Exception
        {
            public List<CombineConflictInfo> Conflicts { get; }
            public CombineDataException(List<CombineConflictInfo> list) : base("名字重复！！！")
            {
                Conflicts = list;
            }
        }
        #endregion

        //--------------------------------------------------------- 



        protected void WriteExternalBase(IOutputStream output)
        {
            output.PutS32(base.ID);
            output.PutUTF(base.Name);
            output.PutUTF(base.IconName);
            output.PutUTF(base.Comment);
            output.WriteFuncID(base.FuncID);
            output.PutUTF(base.UserTag);
            output.PutUTFArray(base.Attributes);
            output.PutUTF(base.EditorPath);

            output.PutS32(this.ID);
            output.PutUTF(this.FileName);
            output.PutUTF(this.VoxelFileName);
            output.PutUTF(this.BGM);
            output.PutS32(this.TotalTimeLimitSEC);
            output.PutUTF(this.ResourceProperty);
            output.PutUTF(this.Name);
            output.PutUTF(this.Desc);
            output.PutArray<string>(this.Attributes, static (output, v) => output.PutUTF(v));
            output.PutS32(this.SpaceDivW);

            output.PutS32(this.FullPlayer);
            output.PutS32(this.MaxPlayer);
            output.PutS32(this.MaxUnit);
            output.PutBool(this.IsPublicMap);
            output.PutS32(this.DefaultUnitLevel);
            output.PutBool(this.RemoveUnitOnDisconnect);
            output.PutBool(this.EnableServerAOI);
            output.PutObj(this.OverrideTerrainDefinition);
            output.PutF32(0/*this.ClientSyncRange*/);
            output.PutObj(this.OverrideConfig);
            output.PutObj(this.OverrideExtConfig);

            output.PutList<PointData>(this.Points, static (output, v) => output.PutObj(v));
            output.PutList<RegionData>(this.Regions, static (output, v) => output.PutObj(v));
            output.PutList<DecorationData>(this.Decorations, static (output, v) => output.PutObj(v));
            output.PutList<UnitData>(this.Units, static (output, v) => output.PutObj(v));
            output.PutList<ItemData>(this.Items, static (output, v) => output.PutObj(v));
            output.PutList<AreaData>(this.Areas, static (output, v) => output.PutObj(v));
            output.PutObj(this.Properties);
            output.PutList<SceneAbilityData>(this?.Abilities, static (output, v) => output.PutObj(v));
            {
                this.serial_datas = new ArrayList<ISNData>();
                PropertyUtil.CollectFieldTypeValues<ISNData>(this, serial_datas);
                output.PutList(GetSerialDatas(), static (output, v) => output.PutObj(v));
            }
            Voxel.WriteExternal(output);

        }
        protected void ReadExternalBase(IInputStream input)
        {
            base.ID = input.GetS32();
            base.Name = input.GetUTF();
            base.IconName = input.GetUTF();
            base.Comment = input.GetUTF();
            base.FuncID = input.ReadFuncID();
            base.UserTag = input.GetUTF();
            base.Attributes = input.GetUTFArray();
            base.EditorPath = input.GetUTF();

            this.ID = input.GetS32();
            this.FileName = input.GetUTF();
            this.VoxelFileName = input.GetUTF();
            this.BGM = input.GetUTF();
            this.TotalTimeLimitSEC = input.GetS32();
            this.ResourceProperty = input.GetUTF();
            this.Name = input.GetUTF();
            this.Desc = input.GetUTF();
            this.Attributes = input.GetUTFArray();
            this.SpaceDivW = input.GetS32();

            this.FullPlayer = input.GetS32();
            this.MaxPlayer = input.GetS32();
            this.MaxUnit = input.GetS32();
            this.IsPublicMap = input.GetBool();
            this.DefaultUnitLevel = input.GetS32();
            this.RemoveUnitOnDisconnect = input.GetBool();
            this.EnableServerAOI = input.GetBool();
            this.OverrideTerrainDefinition = input.GetObjAs<TerrainDefinitionMap>();
            var ClientSyncRange = input.GetF32();
            this.OverrideConfig = input.GetObj<Config>();
            this.OverrideExtConfig = input.GetObj<ICommonConfig>();

            this.Points = input.GetList(static input => input.GetObj<PointData>(), this.Points);
            this.Regions = input.GetList(static input => input.GetObj<RegionData>(), this.Regions);
            this.Decorations = input.GetList(static input => input.GetObj<DecorationData>(), this.Decorations);
            this.Units = input.GetList(static input => input.GetObj<UnitData>(), this.Units);
            this.Items = input.GetList(static input => input.GetObj<ItemData>(), this.Items);
            this.Areas = input.GetList(static input => input.GetObj<AreaData>(), this.Areas);
            this.Properties = input.GetObj<ISceneProperties>(this.Properties);
            this.Abilities = input.GetList(static input => input.GetObjAs<SceneAbilityData>(), this.Abilities);
            {
                this.serial_datas = input.GetList(static input => input.GetObjAs<ISNData>(), this.serial_datas);
            }
            Voxel.ReadExternal(input);
        }

        public void WriteExternalByClient(IOutputStream output)
        {
            this.WriteExternalBase(output);
            this.Terrain.WriteExternal(output, true);
        }
        public void ReadExternalByClient(IInputStream input)
        {
            this.ReadExternalBase(input);
            this.Terrain.ReadExternal(input, true);
        }

        public void WriteExternal(IOutputStream output)
        {
            this.WriteExternalBase(output);
            this.Terrain.WriteExternal(output, false);
            output.PutUTF(this.Host?.Script);
            output.PutList<ZoneEvent>(this.Host?.Events, static (output, v) => output.PutExt(v));
            output.PutList<IZoneEnvironmentVar>(this.Host?.EnvironmentVars, static (output, v) => output.PutObj(v));
            output.PutList(this.Events, static (o, v) => o.PutS32(v));
        }
        public void ReadExternal(IInputStream input)
        {
            this.ReadExternalBase(input);
            this.Terrain.ReadExternal(input, false);
            this.Host = new SceneHostData();
            this.Host.Script = input.GetUTF();
            this.Host.Events = input.GetList(static input => input.GetExt<ZoneEvent>(), this.Host.Events);
            this.Host.EnvironmentVars = input.GetList(static input => input.GetObj<IZoneEnvironmentVar>(), this.Host.EnvironmentVars);
            this.Events = input.GetList(static input => input.GetS32(), this.Events);
        }


    }

    // ----------------------------------------------------------------------------------
    [Expandable]
    [MessageType(BattleConstants.VoxelInfo)]
    [Desc("体素数据")]
    public class VoxelInfo
    {
        [ReadOnly]
        [Desc("宽（格子）", "体素地形")]
        public int VoxelXCount = 0;
        [ReadOnly]
        [Desc("高（格子）", "体素地形")]
        public int VoxelYCount = 0;
        [ReadOnly]
        [Desc("每格宽", "体素地形")]
        public float VoxelGridCellW = 0;
        [ReadOnly]
        [Desc("每格高", "体素地形")]
        public float VoxelGridCellH = 0;
        [ReadOnly]
        [Desc("阶梯高度", "体素地形")]
        public float VoxelStepIntercept = 1;
        [ReadOnly]
        [Desc("资源位置", "体素地形")]
        public float ResourceStartX = 0;
        [ReadOnly]
        [Desc("资源位置", "体素地形")]
        public float ResourceStartY = 0;
        [ReadOnly]
        [Desc("包围盒", "体素地形")]
        public DeepCore.Geometry.BoundingBox VoxelBoundingBox;
        [ReadOnly]
        [Desc("资源位置", "体素翻转")]
        public bool FlipY = true;
        [ReadOnly]
        [Desc("配置", "体素地形")]
        public string ConfigXML;

        public float TerrainH { get => VoxelYCount * VoxelGridCellH; }
        public float TerrainW { get => VoxelXCount * VoxelGridCellW; }

        public override string ToString()
        {
            return $"CW={VoxelGridCellW},CH={VoxelGridCellH},Step={VoxelStepIntercept}";
        }

        public void WriteExternal(IOutputStream output)
        {
            output.PutS32(VoxelXCount);
            output.PutS32(VoxelYCount);
            output.PutF32(VoxelGridCellW);
            output.PutF32(VoxelGridCellH);
            output.PutF32(VoxelStepIntercept);
            output.PutStruct(VoxelBoundingBox);
            output.PutF32(ResourceStartX);
            output.PutF32(ResourceStartY);
            output.PutBool(FlipY);
            output.PutUTF(ConfigXML);
        }
        public void ReadExternal(IInputStream input)
        {
            VoxelXCount = input.GetS32();
            VoxelYCount = input.GetS32();
            VoxelGridCellW = input.GetF32();
            VoxelGridCellH = input.GetF32();
            VoxelStepIntercept = input.GetF32();
            VoxelBoundingBox = input.GetStruct<BoundingBox>();
            ResourceStartX = input.GetF32();
            ResourceStartY = input.GetF32();
            FlipY = input.GetBool();
            ConfigXML = input.GetUTF();
        }
    }

    [MessageType(BattleConstants.TerrainData)]
    [Desc("地形数据")]
    public class TerrainData
    {
        //---------------------------------------------------------
        [Desc("宽（格子）", "地形", false)]
        public int XCount = 100;
        [Desc("高（格子）", "地形", false)]
        public int YCount = 100;
        [Desc("每格宽", "地形", false)]
        public int GridCellW = 1;
        [Desc("每格高", "地形", false)]
        public int GridCellH = 1;
        [Desc("地形数据", "地形", false)]
        public string TerrainTextData;
        //------------------------------------------------------------
        public int TotalWidth { get => XCount * GridCellW; }
        public int TotalHeight { get => YCount * GridCellH; }
        [Desc("", "", false)]
        public int TotalTop = int.MaxValue;
        [Desc("", "", false)]
        public int TotalBottom = int.MinValue;
        //------------------------------------------------------------
        public void WriteExternal(IOutputStream output, bool client)
        {
            output.PutS32(XCount);
            output.PutS32(YCount);
            output.PutS32(GridCellW);
            output.PutS32(GridCellH);
            output.PutS32(TotalTop);
            output.PutS32(TotalBottom);
            if (client == false)
            {
                var zd = ZoneData;
                output.PutExt(zd);
            }
        }
        public void ReadExternal(IInputStream input, bool client)
        {
            XCount = input.GetS32();
            YCount = input.GetS32();
            GridCellW = input.GetS32();
            GridCellH = input.GetS32();
            TotalTop = input.GetS32();
            TotalBottom = input.GetS32();
            if (client == false)
            {
                var zd = input.GetExt<ZoneInfo>();
                SetTerrain(zd, true);
            }
            else
            {
                var zd = ToZoneData(this, true);
                TerrainTextData = string.Empty;
                mZoneData = zd;
            }
        }

        //------------------------------------------------------------
        private ZoneInfo mZoneData;
        public ZoneInfo ZoneData
        {
            get
            {
                if (mZoneData == null) mZoneData = ToZoneData(this, false);
                return mZoneData;
            }
        }
        public void SetTerrain(ZoneInfo zonedata, bool save)
        {
            mZoneData = zonedata;
            if (save || TemplateManager.IsEditor)
            {
                int gridW = zonedata.GridCellW;
                int gridH = zonedata.GridCellH;
                GridCellW = gridW;
                GridCellH = gridH;
                XCount = zonedata.XCount;
                YCount = zonedata.YCount;
                TotalTop = zonedata.TotalTop;
                TotalBottom = zonedata.TotalBottom;
                TerrainTextData = WriteMap(zonedata, (v) => v.ToString("X"));
            }
        }
        public void CleanTerrain()
        {
            this.TerrainTextData = null;
            ZoneData.CleanTerrainMatrix();
        }

        //------------------------------------------------------------
        private static ZoneInfo ToZoneData(TerrainData td, bool client)
        {
            ZoneInfo info = new ZoneInfo(td.XCount, td.YCount, td.GridCellW, td.GridCellH);
            info.TotalTop = td.TotalTop;
            info.TotalBottom = td.TotalBottom;
            if (client == false) ReadMap(info, td.TerrainTextData, (v) => int.Parse(v, NumberStyles.HexNumber));
            else info.CleanTerrainMatrix();
            return info;
        }
        private static string WriteMap(ZoneInfo map, Func<int, string> toString)
        {
            if (map == null || !map.HasFlag) return null;
            var XCount = map.XCount;
            var YCount = map.YCount;
            var sb = new StringBuilder();
            sb.AppendLine();
            for (int y = 0; y < YCount; y++)
            {
                sb.Append("{");
                for (int x = 0; x < XCount; x++)
                {
                    sb.Append(toString(map[x, y]));
                    if (x < XCount - 1)
                    {
                        sb.Append(',');
                    }
                }
                sb.Append("},");
                sb.AppendLine();
            }
            return sb.ToString();
        }
        private static void ReadMap(ZoneInfo map, string src, Func<string, int> parse)
        {
            if (map == null) return;
            if (string.IsNullOrEmpty(src)) return;
            var xcount = map.XCount;
            var ycount = map.YCount;
            int pos = 1;
            for (int y = 0; y < ycount; y++)
            {
                pos = src.IndexOf('{', pos);
                if (pos >= 0)
                {
                    int old_pos = pos + 1;
                    int len = 0;
                    string num = null;
                    int last_x = xcount - 1;
                    for (int x = 0; x < xcount; x++)
                    {
                        if (x == last_x)
                        {
                            pos = src.IndexOf('}', old_pos);
                        }
                        else
                        {
                            pos = src.IndexOf(',', old_pos);
                        }
                        if (pos > old_pos)
                        {
                            len = pos - old_pos;
                            num = src.Substring(old_pos, len);
                            //info.mTerrainMatrix[x, y] = int.Parse(num, NumberStyles.HexNumber);
                            map[x, y] = parse(num);
                        }
                        pos += 1;
                        old_pos = pos;
                    }
                }
            }
        }

    }

    //--------------------------------------------------------------------------------------------------------

    // ----------------------------------------------------------------------------------


}
