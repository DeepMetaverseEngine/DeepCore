using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.Instance.Abilities;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Log;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneEditor.Prewview;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Text;
using static DeepCore.Colors;

namespace DeepCore.Game3D.Host.ZonePreview
{
    public class ZonePreviewComponent : InstanceZoneComponent
    {
        public static Logger log = new LazyLogger("Preview");
        public Vector3 Center { get; private set; }
        protected override void OnAdded()
        {
            base.OnAdded();
            if (this.Zone.Data.TryGetStartTestRegion(out var region, out var ab, Zone.RandomN))
            {
                this.Center = region.Position;
                if (Zone.Templates.TryGetUnit(ab.TestActorTemplateID, out var temp))
                {
                    this.AddActor(temp, this.Center);
                }
            }
            else
            {
                var layer = Zone.Terrain3D.GetVoxelLayerByPos(new Vector3(Zone.Terrain3D.TotalSizeX / 2, Zone.Terrain3D.TotalSizeY / 2, 0));
                if (layer != null)
                {
                    this.Center = layer.UpwardCenterPos;
                }
                else
                {
                    this.Center = Vector3.Zero;
                }
            }

        }

        //--------------------------------------------------------------------------------------------
        #region Actor & Target

        public InstanceUnit Actor => previewActor;
        public IReadOnlyList<InstanceUnit> Targets => previewTargets;
        private List<InstanceUnit> previewTargets = new List<InstanceUnit>();
        private InstanceUnit previewActor;
        public void ClearPreviewObjects()
        {
            foreach (var obj in previewTargets)
            {
                obj.RemoveFromParent();
            }
            this.previewTargets.Clear();
            this.previewActor?.RemoveFromParent();
            this.previewActor = null;
        }

        protected virtual InstanceUnit CreateActor(TAddUnit add) => Zone.AddUnit(add);
        protected virtual InstanceUnit CreateTarget(TAddUnit add) => Zone.AddUnit(add);

        public virtual InstanceUnit AddActor(UnitInfo temp, Vector3 pos, byte force = 2)
        {
            temp = Zone.CloneData(temp);
            var unit = CreateActor(new TAddUnit()
            {
                info = temp,
                pos = pos,
                force = force,
                overrideType = UnitType.TYPE_PLAYER,
            });
            if (unit != null)
            {
                var comp = unit.Components.GetOrAddComponentAs<UnitAutoAttackComponent>();
                {
                    comp.IsLaunchAnyway = true;
                    comp.IsFaceToTarget = true;
                    comp.IsIgnoreAutoLaunch = false;
                }
                this.previewActor = unit;
                this.OnAddActor?.Invoke(unit);
            }
            return unit;
        }
        public virtual InstanceUnit AddTarget(UnitInfo temp, Vector3 pos, byte force = 3)
        {
            temp = Zone.CloneData(temp);
            temp.DeadTimeMS = 1000;
            temp.HealthPoint = 999999;
            var unit = CreateTarget(new TAddUnit()
            {
                info = temp,
                pos = pos,
                force = force,
                overrideType = UnitType.TYPE_NEUTRALITY,
            });
            if (unit != null)
            {
                this.previewTargets.Add(unit);
                this.OnAddTarget?.Invoke(unit);
            }
            return unit;
        }

        public event Action<InstanceUnit> OnAddActor;
        public event Action<InstanceUnit> OnAddTarget;

        protected virtual void ResetTargetPos(float range)
        {
            if (previewTargets.Count >= 4)
            {
                previewTargets[0].Transport(Center + new DeepCore.Geometry.Vector3(+0.8f * range, 0, 0));
                previewTargets[1].Transport(Center + new DeepCore.Geometry.Vector3(-0.8f * range, 0, 0));
                previewTargets[2].Transport(Center + new DeepCore.Geometry.Vector3(0, +0.8f * range, 0));
                previewTargets[3].Transport(Center + new DeepCore.Geometry.Vector3(0, -0.8f * range, 0));
            }
            var spawn = Zone.FindFlagAs<ZoneRegion>(rg => rg.Data.TryGetAbilityOf<SpawnUnitAbilityData>(out var _));
            if (spawn != null)
            {
                foreach (var t in Targets)
                {
                    t.Transport(spawn.GetRandomPos());
                }
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------
        #region Messages

        /// <summary>
        /// 收到编辑器的事件，返回是否拦截消息并处理
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool TryProcessPreviewMessage(ISerializable data)
        {
            if (data is PreviewUpdate preview)
            {
                return ProcessPreview(preview);
            }
            if (data is UnitInfo unit)
            {
                return ProcessPreview(unit);
            }
            if (data is SpellTemplate spell)
            {
                return ProcessPreview(spell);
            }
            if (data is BuffTemplate buff)
            {
                return ProcessPreview(buff);
            }
            if (data is SkillTemplate skill)
            {
                return ProcessPreview(skill);
            }
            if (data is AuraTemplate aura)
            {
                return ProcessPreview(aura);
            }
            if (data is CardTemplate card)
            {
                return ProcessPreview(card);
            }
            if (data is BattleUITemplate ui)
            {
                return ProcessPreview(ui);
            }
            return false;
        }

        protected virtual bool ProcessPreview(PreviewUpdate preview)
        {
            if (preview.Template is UnitInfo unit)
            {
                if (ProcessPreview(unit))
                {
                    //                     if (preview.Relation is SkillTemplate _skill)
                    //                     {
                    //                         return ProcessPreview(_skill);
                    //                     }
                    //                     else if (preview.Relation is SpellTemplate _spell)
                    //                     {
                    //                         return ProcessPreview(_spell);
                    //                     }
                    //                     else if (preview.Relation is BuffTemplate _buff)
                    //                     {
                    //                         return ProcessPreview(_buff);
                    //                     }
                    //                     else if (preview.Relation is CardTemplate _card)
                    //                     {
                    //                         return ProcessPreview(_card);
                    //                     }
                    TryProcessPreviewMessage(preview.Relation);
                }
                return true;
            }
            return TryProcessPreviewMessage(preview.Template);
            //             else if (preview.Template is SkillTemplate skill)
            //             {
            //                 return ProcessPreview(skill);
            //             }
            //             else if (preview.Template is SpellTemplate spell)
            //             {
            //                 return ProcessPreview(spell);
            //             }
            //             else if (preview.Template is BuffTemplate buff)
            //             {
            //                 return ProcessPreview(buff);
            //             }
            //             else if (preview.Template is CardTemplate card)
            //             {
            //                 return ProcessPreview(card);
            //             }
            //return false;
        }
        protected virtual bool ProcessPreview(UnitInfo unit)
        {
            log.Info($"ProcessPreview : {unit}");
            if (Actor != null && unit.ID == Actor.Info.ID)
            {
                return true;
            }
            ClearPreviewObjects();
            var actor = AddActor(unit, Center);
            var temp = previewActor.Info;
            var range = previewActor.BodySize;
            if (actor.DefaultSkill != null)
            {
                range = Math.Max(range, actor.DefaultSkill.AttackRange);
            }
            range = Math.Max(range, previewActor.BodySize + 6f);
            var t1 = AddTarget((temp), Center + new DeepCore.Geometry.Vector3(+0.8f * range, 0, 0));
            var t2 = AddTarget((temp), Center + new DeepCore.Geometry.Vector3(-0.8f * range, 0, 0));
            var t3 = AddTarget((temp), Center + new DeepCore.Geometry.Vector3(0, +0.8f * range, 0));
            var t4 = AddTarget((temp), Center + new DeepCore.Geometry.Vector3(0, -0.8f * range, 0));
            ResetTargetPos(range);
            return true;
        }
        protected virtual bool ProcessPreview(SkillTemplate skill)
        {
            log.Info($"ProcessPreview : {skill}");
            if (previewActor != null)
            {
                previewActor.InitSkills(skill);
                {
                    var range = skill.AttackRange;
                    range = Math.Max(range, previewActor.BodySize + 6f);
                    ResetTargetPos(range);
                }
            }
            return true;
        }
        protected virtual bool ProcessPreview(SpellTemplate spell)
        {
            log.Info($"ProcessPreview : {spell}");

            return true;
        }
        protected virtual bool ProcessPreview(BuffTemplate buff)
        {
            log.Info($"ProcessPreview : {buff}");
            if (previewActor != null)
            {
                previewActor.AddBuff(buff.ID);
            }
            return true;
        }
        protected virtual bool ProcessPreview(AuraTemplate aura)
        {
            log.Info($"ProcessPreview : {aura}");
            if (previewActor != null)
            {
                previewActor.LaunchAura(aura.ID);
            }
            return true;
        }
        protected virtual bool ProcessPreview(CardTemplate card)
        {
            log.Info($"ProcessPreview : {card}");
            if (previewActor != null)
            {
                previewActor.Cartridge.PutCardSlot(new CardSlot()
                {
                    CardTemplateID = card.ID,
                    Op = CardSlot.CardSlotOperation.Upgrade,
                });
            }
            return true;
        }
        protected virtual bool ProcessPreview(BattleUITemplate ui)
        {
            log.Info($"ProcessPreview : {ui}");
            if (previewActor is InstancePlayer player)
            {
                Zone.ShowPlayerDialog(ui.ID, player, true);
            }
            else
            {
                Zone.ShowDialog(ui.ID, true);
            }
            return true;
        }
        #endregion
        //--------------------------------------------------------------------------------------------
    }
}
