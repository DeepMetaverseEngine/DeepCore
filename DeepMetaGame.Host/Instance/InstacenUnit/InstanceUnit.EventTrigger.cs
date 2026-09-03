using DeepCore.Game3D.Host.FuncData;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class InstanceUnit
    {
        //-----------------------------------------------------------------------------------------------------//




        #region SceneEvents
        //-----------------------------------------------------------------------------------------------------//

        private HashSet<CustomUnitEventTriggerCollection> mCustomEvents = new HashSet<CustomUnitEventTriggerCollection>(0);

        public CustomUnitEventTriggerCollection BindCustomUnitEvent(CustomEventTemplateData uet)
        {
            if (uet == null || uet.CustomEvents == null || uet.CustomEvents.Count == 0)
                return null;
            var bind_event = this.HostFactory.CreateCustomUnitEventCollection(this, uet/*Zone.CloneData(uet)*/);
            bind_event.Start();
            mCustomEvents.Add(bind_event);
            return bind_event;
        }
        public bool RemoveCustomEvent(CustomUnitEventTriggerCollection evt)
        {
            if (evt == null) return false;
            if (mCustomEvents.Remove(evt))
            {
                evt.Dispose();
                return true;
            }
            return false;
        }


        //-----------------------------------------------------------------------------------------------------//

        private HashMap<int, List<UnitEventTriggerCollection>> mBindEvents = new HashMap<int, List<UnitEventTriggerCollection>>(0);
        public UnitEventTriggerCollection BindUnitEvent(int unit_event_id)
        {
            UnitEventTemplate uet = Cartridge.GetUnitEvent(unit_event_id);

            if (uet == null)
                return null;

            return BindUnitEvent(uet);
        }

        public bool ContainsUnitEvent(int unit_event_id)
        {
            if (mBindEvents.TryGetValue(unit_event_id, out var lt))
            {
                return lt.Count > 0;
            }
            return false;
        }

        public virtual UnitEventTriggerCollection LearnUnitEvent(UnitCartridge cartridge, CardTemplate card, UnitEventTemplate uet)
        {
            if (mBindEvents.TryGetValue(uet.ID, out var lt) && lt.Count > 0)
            {
                return null;
            }
            return BindUnitEvent(uet);
        }

        public UnitEventTriggerCollection BindUnitEvent(UnitEventTemplate uet)
        {
            if (uet == null)
                return null;
            //uet = Zone.CloneData(uet);

            if (mBindEvents.TryGetValue(uet.ID, out var lt))
            {
                if (!uet.IsDuplicating && lt.Count > 0)
                    return null;
                var bind_event = HostFactory.CreateUnitEventCollection(this, uet);//new UnitEventTriggerCollection(this, uet);
                bind_event.Start();
                lt.Add(bind_event);
                callback_onBindEvent(uet);
                return bind_event;
            }
            else
            {
                lt = new List<UnitEventTriggerCollection>();
                mBindEvents.Add(uet.ID, lt);
                var bind_event = HostFactory.CreateUnitEventCollection(this, uet);//new UnitEventTriggerCollection(this, uet);
                bind_event.Start();
                lt.Add(bind_event);
                callback_onBindEvent(uet);
                return bind_event;
            }
        }

        public void RemoveBindEvent(int unit_event_id)
        {
            if (mBindEvents.TryRemove(unit_event_id, out var lt))
            {
                for (int i = 0; i < lt.Count; i++)
                {
                    lt[i].Dispose();
                    callback_onUnBindEvent(unit_event_id);
                }

                lt.Clear();
            }
        }

        public bool RemoveBindEvent(UnitEventTriggerCollection evt)
        {
            if (evt == null) return false;
            if (mBindEvents.TryGetValue(evt.TemplateID, out var lt))
            {
                evt.Dispose();
                return lt.Remove(evt);
            }
            return false;
        }

        internal void RefreshUnitEventData(UnitEventTemplate uet)
        {
            if (mBindEvents.TryGetValue(uet.ID, out var lt))
            {
                foreach (var bind_event in lt)
                {
                    bind_event.RefreshData(uet);
                }
            }
        }
        internal void CleanBindEvents()
        {
            {
                foreach (var evt in mCustomEvents)
                {
                    evt.Dispose();
                }
                mCustomEvents.Clear();
                _bindEvent = null;
            }
            {
                foreach (var list in mBindEvents.Values)
                {
                    var lt = list;
                    for (int i = 0; i < lt.Count; i++)
                    {
                        lt[i]?.Dispose();
                    }
                    lt.Clear();
                }
                mBindEvents.Clear();
            }
        }


        #endregion

        //-----------------------------------------------------------------------------------------------------//
        public UnitInfo TemplateData => this.Info;
        public InstanceUnit LastLaunchSkillTargetUnit { get { return this.lastLaunchSkillTarget; } }

        public UnitComponent GetComponent(System.Type ctype)
        {
            return this.Components.GetComponent(ctype, true) as UnitComponent;
        }
        public void Pause(int timeMS)
        {
            this.Pause(true, timeMS);
        }
        public void Resume()
        {
            this.Pause(false);
        }
        public void LaunchSpell(object from, LaunchSpell launch, Geometry.Vector3 startPos, Geometry.Vector3? target, int? skillID = null)
        {
            var skill = skillID.HasValue ? GetSkillState(skillID.Value) : null;
            this.Parent.UnitLaunchSpell(
                launcher: this,
                sender: this,
                launch: launch,
                from: from,
                startPos: startPos,
                fromeSkillTemplateID: skill,
                targetUnit: null,
                targetPos: target);
        }
        public void LaunchSpell(object from, LaunchSpell launch, Geometry.Vector3 startPos, InstanceUnit target, int? skillID = null)
        {
            var skill = skillID.HasValue ? GetSkillState(skillID.Value) : null;
            this.Parent.UnitLaunchSpell(
                launcher: this,
                sender: this,
                launch: launch,
                from: from,
                startPos: startPos,
                fromeSkillTemplateID: skill,
                targetUnit: target,
                targetPos: null);
        }
        public void LaunchSkill(int skillTemplateID, DeepCore.GameData.Data.LaunchSkillParam param)
        {
            this.LaunchSkill(skillTemplateID, new TLaunchSkillParam()
            {
                AutoFocusNearTarget = param.AutoFocusNearTarget,
                SpellTargetPos = param.SpellTargetPos,
                TargetUnitID = param.TargetUnitID
            });
        }
        public virtual void QueueUnitAction(IEventTriggerAdapter api, EventArguments args, ForceUnitAction action, System.Action onStop = null)
        {
            if (action is ForceUnitIdleAction idle)
            {
                var state = ForceStateIdleTime.Alloc(this, (float)idle.TimeSEC.GetValueAs(api, args));
                state.OnStopOnce += ((u, os) => { onStop?.Invoke(); });
                this.QueueCurrentState(state);
            }
            else if (action is ForceUnitMoveAction move)
            {
                var pos = move.Pos.GetValueAs(api, args);
                if (pos.HasValue)
                {
                    var state = ForceStateMoveTo.Alloc(this, pos.Value);
                    state.OnStopOnce += ((u, os) => { onStop?.Invoke(); });
                    this.QueueCurrentState(state);
                }
            }
            else if (action is ForceUnitDirMoveAction dirMoveAction)
            {
                var region = dirMoveAction.Region.GetValueAs(api, args) as ZoneRegion;
                var state = ForceStateMoveToZoneRegion.Alloc(this, dirMoveAction.Angle * CMath.Angle2Radian, region);
                state.OnStopOnce += ((u, os) => { onStop?.Invoke(); });
                this.QueueCurrentState(state);
            }
            else if (action is ForceUnitFaceToAction faceTo)
            {
                this.FaceTo((float)faceTo.Direction.GetValueAs(api, args));
                this.Parent.QueueTask((onStop), static (z, st) => { st?.Invoke(); });
            }
            else if (action is ForceUnitLaunchSkillAction skill)
            {
                var state = ForceStateLaunchSkill.Alloc(this, skill.SkillTemplateID, skill.RandomSkill.GetValueAs(api, args), (u, os) =>
                {
                    onStop?.Invoke();
                });
                this.QueueCurrentState(state);
            }
            else if (action is ForceUnitDoAction doAction)
            {
                var state = ForceStateActionTime.Alloc(this, (float)doAction.TimeSEC.GetValueAs(api, args), doAction.ActionName);
                state.OnStopOnce += ((u, os) => { onStop?.Invoke(); });
                this.QueueCurrentState(state);
            }
        }

        public bool HasBuff(int buffID)
        {
            return GetBuffByID(buffID) != null;
        }

        public int GetBuffOverlay(int buffID)
        {
            var bs = GetBuffByID(buffID);
            if (bs != null)
            {
                return bs.OverlayLevel;
            }
            return 0;
        }
    }


}
