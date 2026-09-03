using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepCore.GameData.EventTrigger;
using DeepCore.Log;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace DeepCore.Game3D.Host.ZoneEditor
{
    public partial class EditorScene : InstanceZone
    {


        private List<ZoneEventTriggerCollection> mZoneEvents = new List<ZoneEventTriggerCollection>();
        private HashMap<int, List<ZoneRegion>> mStartRegions = new HashMap<int, List<ZoneRegion>>();
        public EditorScene(InstanceZoneListener listener, ZoneHostFactory hostFactory, EditorTemplates dataroot, SceneData data, int randomSeed = 1)
            : base(listener, hostFactory, dataroot, dataroot.Templates.SyncSceneLevel(data), randomSeed)
        {
            this.BeginInitScene(data);
            {
                {
                    var evt = HostFactory.CreateZoneEventCollection(this);
                    mZoneEvents.Add(evt);
                }
                this.BindAttributes(data.Attributes);
                this.InitEditorObjects(data);
                this.EndInitObjects(data);
            }
            this.EndInitScene(data);
        }
        protected virtual void InitEditorObjects(SceneData data)
        {
            // 初始化单位 //
            {
                InitRegions(data);
                InitPoints(data);
                InitDecorations(data);
                InitAreas(data);
            }
            // 完成初始化单位 //
            {
                BindRegions(data);
                BindPoints(data);
                BindDecorations(data);
                BindAreas(data);
                ForEachFlags(this, static (st, flag) => flag.InitNexts());
            }
            {
                InitItems(data);
                InitUnits(data);
                //                 BindItems(data);
                //                 BindUnits(data);
            }
        }
        protected override void Disposing()
        {
            foreach (var evt in mZoneEvents)
            {
                evt.Dispose();
            }
            mZoneEvents.Clear();
            mZoneEvents = null;
            mStartRegions.Clear();
            mStartRegions = null;
            base.Disposing();
            //mScriptAdapter = null;
        }
        protected override void ClearEvents()
        {
            base.ClearEvents();
        }
        protected override void FirstUpdate()
        {
            base.FirstUpdate();
            var host = Data.Host;
            if (host != null)
            {
                foreach (var evt in mZoneEvents)
                {
                    // 绑定变量 //
                    using (var var_api = ObjectPool.Alloc<BindValuesExecutor>().Init(this, evt))
                    {
                        foreach (ZoneVar v in host.EnvironmentVars)
                        {
                            BindZoneVar(v, var_api);
                        }
                        // 绑定触发器类型 //
                        evt.Start();
                    }
                }
            }
        }
        //---------------------------------------------------------------------------------------------------------
        #region InitScene-----------------------------------------------------------------------------------------------------------------------

        //         class StartPointTrigger
        //         {
        //             private readonly IGuardUnit guard;
        //             private readonly InstanceFlag start;
        //             public StartPointTrigger(IGuardUnit guard, InstanceFlag start)
        //             {
        //                 this.guard = guard;
        //                 this.start = start;
        //                 (this.guard as InstanceUnit).OnActivated += this.onUnitStartPoint;
        //             }
        //             private void onUnitStartPoint(InstanceUnit unit)
        //             {
        //                 guard.AttackTo(start as ZoneWayPoint);
        //             }
        //         }

        protected virtual void BeginInitScene(SceneData data)
        {

        }
        protected virtual void EndInitObjects(SceneData data)
        {

        }
        protected virtual void EndInitScene(SceneData data)
        {

        }

        protected virtual void InitRegions(SceneData data)
        {
            foreach (RegionData rdata in data.Regions)
            {
                AddEditRegion(rdata);
            }
        }
        protected virtual void InitPoints(SceneData data)
        {
            foreach (PointData rdata in data.Points)
            {
                AddEditPoint(rdata);
            }
        }
        protected virtual void InitDecorations(SceneData data)
        {
            foreach (DecorationData rdata in data.Decorations)
            {
                AddEditDecoration(rdata);
            }
        }
        protected virtual void InitAreas(SceneData data)
        {
            foreach (AreaData adata in data.Areas)
            {
                AddEditArea(adata);
            }
        }
        protected virtual void BindRegions(SceneData data)
        {
            foreach (RegionData rdata in data.Regions)
            {
                InitEditRegion(rdata);
            }
        }
        protected virtual void BindPoints(SceneData data)
        {
            foreach (PointData rdata in data.Points)
            {
                InitEditPoint(rdata);
            }
        }
        protected virtual void BindDecorations(SceneData data)
        {
            foreach (DecorationData rdata in data.Decorations)
            {
                InitEditDecoration(rdata);
            }
        }
        protected virtual void BindAreas(SceneData data)
        {
            foreach (AreaData adata in data.Areas)
            {
                InitEditArea(adata);
            }
        }
        //         protected virtual void BindItems(SceneData data)
        //         {
        //             foreach (ItemData rdata in data.Items)
        //             {
        //                 InitEditItem(rdata);
        //             }
        //         }
        //         protected virtual void BindUnits(SceneData data)
        //         {
        //             foreach (UnitData rdata in data.Units)
        //             {
        //                 InitEditUnit(rdata);
        //             }
        //         }


        //         private void BindAttributes(string[] attributes, InstanceAttributes unit)
        //         {
        //             if (attributes != null)
        //             {
        //                 foreach (string e in attributes)
        //                 {
        //                     string[] kv = e.Split('=');
        //                     if (kv.Length >= 2)
        //                     {
        //                         unit.SetAttribute(kv[0].Trim(), kv[1].Trim());
        //                     }
        //                 }
        //             }
        //         }
        //         [Obsolete("")]
        //         internal InstanceUnit AddUnit(UnitInfo info, string name, byte force, int level, float x, float y, float z, float direction,
        //     out AddUnitEvent add, InstanceUnit summoner = null)
        //         {
        //             var evt = new AddUnit();
        //             {
        //                 evt.info = info;
        //                 evt.editor_name = name;
        //                 evt.player_uuid = name;
        //                 evt.force = force;
        //                 evt.level = level;
        //                 evt.pos = new Vector3(x, y, z);
        //                 evt.direction = direction;
        //                 evt.summoner = summoner;
        //             }
        //             var ret = AddUnit(evt);
        //             add = evt.out_event;
        //             return ret;
        //         }

        protected virtual void InitItems(SceneData data)
        {
            this.QueueTask((z) =>
            {
                foreach (ItemData rdata in data.Items)
                {
                    AddEditItem(rdata);
                }
            });
        }
        protected virtual void InitUnits(SceneData data)
        {
            this.QueueTask((z) =>
            {
                foreach (UnitData rdata in data.Units)
                {
                    AddEditUnit(rdata);
                }
            });
        }

        virtual protected void AddEditUnit(UnitData data)
        {
            if (data.Enable)
            {
                UnitInfo info = Templates.GetUnit(data.UnitTemplateID);
                if (info != null)
                {
                    var add = new TAddUnit()
                    {
                        info = info,
                        editor_name = data.Name,
                        player_uuid = data.Name,
                        displayName = data.DisplayName,
                        force = data.Force,
                        level = data.UnitLevel,
                        pos = data.Position,
                        direction = data.Direction,
                        summoner = null,
                        alias = data.Alias,
                        overrideType = data.OverrideType,
                    };
                    //AddUnitEvent add;
                    //var unit = this.AddUnit(info, data.Name, data.Force, data.UnitLevel, data.X, data.Y, data.Z, data.Direction, out var add);
                    var unit = this.AddUnit(add);
                    if (unit != null)
                    {
                        //BindAttributes(data.Attributes, unit);
                        unit.BindAttributes(data.Attributes);
                        //unit.SetAttribute("UnitData", data);
                        unit.UnitTag = data.UnitTag;
                        unit.Pause(!data.Enable);
                        if (data.CopyDecorationShape != null && GetFlag(data.CopyDecorationShape) is ZoneDecoration flag)
                        {
                            unit.FaceTo(flag.Direction);
                            unit.Transport(flag.Position);
                            unit.ZoneShape = flag.ZoneShape;
                        }
                        if (data.Enable)
                        {
                            //add.out_event.Sync.Alias = unit.Alias;
                            if (!string.IsNullOrEmpty(data.StartPointName))
                            {
                                if (GetFlag(data.StartPointName) is ZoneWayPoint start)
                                {
                                    unit.OnFirstActivated += (g) =>
                                    {
                                        unit.StartAttackTo(start);
                                    };
                                    // new StartPointTrigger(guard, start);
                                }
                            }
                            else
                            {
                                if (data.MainStatus != DeepMetaGame.Data.Misc.UnitActionStatus.NA)
                                {
                                    unit.OnFirstActivated += (u =>
                                    {
                                        unit.ChangeState(InstanceUnit.StateDefinedAction.Alloc(unit, data.MainStatus, data.SubStatus));
                                    });
                                }
                            }
                        }
                        // 绑定脚本适配器
                        //if (mScriptAdapter != null) mScriptAdapter.BindUnit(unit);
                    }
                }
            }
        }

        virtual protected void AddEditItem(ItemData data)
        {
            if (data.Enable)
            {
                ItemTemplate info = Templates.GetItem(data.ItemTemplateID);
                if (info != null)
                {
                    // InstanceItem item = this.AddItem(info, data.Name, data.Position, data.Direction, data.Force, null);
                    var item = this.AddItem(new TAddItem()
                    {
                        template = info,
                        name = data.Name,
                        alias = data.Alias,
                        pos = data.Position,
                        direction = data.Direction,
                        force = data.Force,
                    });
                    if (item != null)
                    {
                        item.BindAttributes(data.Attributes);// HZDSB
                    }
                }
            }
        }

        virtual protected void AddEditRegion(RegionData data)
        {
            var flag = CreateFlag(data);
            if (AddFlag(flag))
            {
                //BindAttributes(data.Attributes, flag);// HZDSB
                flag.Enable = data.Enable;
                // 绑定脚本适配器
                //if (mScriptAdapter != null) mScriptAdapter.BindRegion(flag);
            }
            else
            {
                throw new Exception("Already Have Flag : " + data.Name);
            }
        }

        virtual protected void AddEditPoint(PointData data)
        {
            var flag = CreateFlag(data);
            if (AddFlag(flag))
            {
                //BindAttributes(data.Attributes, flag);// HZDSB
                flag.Enable = data.Enable;
                // 绑定脚本适配器
                //if (mScriptAdapter != null) mScriptAdapter.BindFlag(flag);
            }
            else
            {
                throw new Exception("Already Have Flag : " + data.Name);
            }
        }

        virtual protected void AddEditDecoration(DecorationData data)
        {
            var flag = CreateFlag(data);
            if (AddFlag(flag))
            {
                //BindAttributes(data.Attributes, flag);// HZDSB
                flag.Enable = data.Enable;
                // 绑定脚本适配器
                //if (mScriptAdapter != null) mScriptAdapter.BindFlag(flag);
            }
            else
            {
                throw new Exception("Already Have Flag : " + data.Name);
            }
        }

        virtual protected void AddEditArea(AreaData data)
        {
            var flag = CreateFlag(data);
            if (AddFlag(flag))
            {
                //BindAttributes(data.Attributes, area);// HZDSB
                flag.Enable = data.Enable;
                // 绑定脚本适配器
                //if (mScriptAdapter != null) mScriptAdapter.BindFlag(flag);
            }
            else
            {
                throw new Exception("Already Have Flag : " + data.Name);
            }
        }

        //         virtual protected void BindEditAbility(SceneObjectData data)
        //         {
        //             if (data is PointData)
        //             {
        //                 InitEditPoint(data as PointData);
        //             }
        //             else if (data is RegionData)
        //             {
        //                 InitEditRegion(data as RegionData);
        //             }
        //             else if (data is DecorationData )
        //             {
        //                 InitEditDecoration(data as DecorationData);
        //             }
        //             else if (data is AreaData)
        //             {
        //                 InitEditArea(data as AreaData);
        //             }
        //         }

        virtual protected void InitEditPoint(PointData data)
        {
            ZoneWayPoint flag = GetFlag(data.Name) as ZoneWayPoint;
            if (flag != null && data.Abilities != null)
            {
                flag.AddAbilities(data.Abilities.ToArray());
                //                 foreach (string nextname in data.NextNames)
                //                 {
                //                     ZoneWayPoint nextpoint = GetFlag(nextname) as ZoneWayPoint;
                //                     if (nextpoint != null)
                //                     {
                //                         flag.AddNext(nextpoint);
                //                     }
                //                     else
                //                     {
                //                         throw new Exception("can not find next point : " + data.Name + " -> " + nextname);
                //                     }
                //                 }
            }
        }
        virtual protected void InitEditRegion(RegionData rdata)
        {
            ZoneRegion flag = GetFlag(rdata.Name) as ZoneRegion;
            if (flag != null && rdata.Abilities != null)
            {
                flag.AddAbilities(rdata.Abilities.ToArray());
                foreach (EditorAbilityData td in rdata.Abilities)
                {
                    if (td is PlayerStartAbilityData)
                    {
                        PlayerStartAbilityData tgd = td as PlayerStartAbilityData;
                        if (!mStartRegions.ContainsKey(tgd.START_Force))
                        {
                            var list = new List<ZoneRegion>();
                            list.Add(flag);
                            mStartRegions.Put(tgd.START_Force, list);
                        }
                        else
                        {
                            var list = mStartRegions[tgd.START_Force];
                            list.Add(flag);
                        }

                    }
                    //                     else if (td is SpawnUnitAbilityData)
                    //                     {
                    //                         SpawnUnitAbilityData tgd = td as SpawnUnitAbilityData;
                    //                         SpawnUnitTrigger tg = tgd.CreateTrigger(this);
                    //                         tg.bindToRegion(flag);
                    //                     }
                    //                     else if (td is SpawnItemAbilityData)
                    //                     {
                    //                         SpawnItemAbilityData tgd = td as SpawnItemAbilityData;
                    //                         SpawnItemTrigger tg = tgd.CreateTrigger(this);
                    //                         tg.bindToRegion(flag);
                    //                     }
                    //                     else if (td is UnitTransportAbilityData)
                    //                     {
                    //                         UnitTransportAbilityData tp = td as UnitTransportAbilityData;
                    //                         TransportUnitTrigger tg = tp.CreateTrigger(this);
                    //                         tg.bindToRegion(flag);
                    //                     }
                }
            }
        }
        virtual protected void InitEditArea(AreaData data)
        {
            ZoneArea area = GetFlag(data.Name) as ZoneArea;
            if (area != null && data.Abilities != null)
            {
                area.AddAbilities(data.Abilities.ToArray());
                area.BindMapBlock();
            }
        }
        virtual protected void InitEditDecoration(DecorationData data)
        {
            ZoneDecoration deco = GetFlag(data.Name) as ZoneDecoration;
            if (deco != null && data.Abilities != null)
            {
                deco.AddAbilities(data.Abilities.ToArray());
            }
        }
        //         virtual protected void InitEditUnit(UnitData data)
        //         {
        // 
        //         }
        //         virtual protected void InitEditItem(ItemData data)
        //         {
        // 
        //         }


        #endregion
        //---------------------------------------------------------------------------------------------------------

        #region Flags-----------------------------------------------------------------------------------------------------------------------
        public ZoneRegion GetEditStartRegion(int force)
        {
            var list = mStartRegions.Get(force);
            if (list != null)
            {
                foreach (var zoneRegion in list)
                {
                    if (zoneRegion.Enable)
                    {
                        return zoneRegion;
                    }

                }
            }

            return null;
        }

        public void GetEditStartRegions(List<KeyValuePair<int, List<ZoneRegion>>> list)
        {
            list.AddRange(mStartRegions);
        }
        public void ForEachEditStartRegions(Action<int, ZoneRegion> action)
        {
            foreach (var kv in mStartRegions)
            {
                foreach (var zoneRegion in kv.Value)
                {
                    if (zoneRegion.Enable)
                        action(kv.Key, zoneRegion);
                }

            }
        }


        public bool TryGetTestRegion(out RegionData rg, out PlayerStartAbilityData start, Random random = null)
        {
            return Data.TryGetStartTestRegion(out rg, out start, random);
        }
        public bool TryGetTestUnit(out RegionData rg, out PlayerStartAbilityData start, out UnitInfo info, Random random = null)
        {
            return Data.TryGetStartTestUnit(this.Templates, out rg, out start, out info, random);
        }



        /// <summary>
        /// 开启/关闭 场景事件
        /// </summary>
        /// <param name="eName"></param>
        /// <param name="val"></param>
        public void SetEnableEditEvent(string eName, bool val)
        {
            foreach (var evt in mZoneEvents)
            {
                var ret = evt.GetEditEvent(eName);
                if (ret != null)
                {
                    ret.IsActive = val;
                }
            }
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------
        #region Events-----------------------------------------------------------------------------------------------------------------------

        public ZoneEventTriggerCollection BindEvent(int UnitEventTemplateID)
        {
            var uet = Templates.GetUnitEvent(UnitEventTemplateID);
            return BindEvent(uet);
        }
        public ZoneEventTriggerCollection BindEvent(UnitEventTemplate uet)
        {
            if (uet != null)
            {
                uet = CloneData(uet);
                var evt = HostFactory.CreateZoneEventCollection(this);
                mZoneEvents.Add(evt);
                var host = Data.Host;
                if (host != null)
                {
                    using (var var_api = ObjectPool.Alloc<BindValuesExecutor>().Init(this, evt))
                    {
                        foreach (ZoneVar v in host.EnvironmentVars)
                        {
                            BindZoneVar(v, var_api);
                        }
                    }
                }
                evt.Start();
                return evt;
            }
            return null;
        }

        #endregion

    }


}
