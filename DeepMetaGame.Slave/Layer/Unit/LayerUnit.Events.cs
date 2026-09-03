using DeepCore.Components;
using DeepCore.EventTrigger;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;
using DeepCore.Geometry;
using DeepMetaGame.Data;

namespace DeepCore.Game3D.Slave.Layer
{
    partial class LayerUnit
    {

        public delegate void OnSkillChangedHandler(LayerUnit unit, int baseSkillID, params int[] skills);
        public delegate void OnActionStatusChangedHandler(LayerUnit unit, UnitActionStatus status, string sub, IRecyclable msg);
        public delegate void OnMoneyChangedHandler(LayerUnit unit, long oldMoney, long newMoney);
        public delegate void OnChantSkillHandler(LayerUnit unit, SkillState skill, float chant_ms);
        public delegate void OnLaunchSkillHandler(LayerUnit unit, SkillState skill, ISkillAction action);
        public delegate void OnHPChangedHandler(LayerUnit unit, long oldHP, long newHP);
        public delegate void OnMPChangedHandler(LayerUnit unit, long oldMP, long newMP);
        public delegate void OnSpeedChangedHandler(LayerUnit unit);

        public delegate void OnMaxHPChangedHandler(LayerUnit unit, long oldMaxHP, long newMaxHP);
        public delegate void OnMaxMPChangedHandler(LayerUnit unit, long oldMaxMP, long newMaxMP);
        public delegate void OnVisibleChangedHandler(LayerUnit unit, IUnitVisibleData visible);
        public delegate void OnStartPickObjectHandler(LayerUnit unit, TimeExpire start, float PickTimeMS, uint PickObjectID, string PickStatus); 
        public delegate void OnStopPickObjectHandler(LayerUnit unit, TimeExpire start, string StopReason);
        public delegate void OnSkillActionChangedHandler(LayerUnit unit, SkillState skill, byte index);
        public delegate void OnBuffAddedHandler(LayerUnit unit, BuffState buff);
        public delegate void OnBuffChangedHandler(LayerUnit unit, BuffState buff);
        public delegate void OnBuffRemovedHandler(LayerUnit unit, BuffState buff);
        public delegate void OnScriptCommandHandler(LayerUnit unit, string msg);
        public delegate void OnSkillActionStartHandler(LayerUnit unit, ISkillAction action);

        public delegate void OnDamageHandler(LayerUnit unit, UnitDamageArgs damage);
        public delegate void OnDeadHandler(LayerUnit unit, bool Crushed, uint attackerID, float deadTimeMS);
        public delegate void OnHitHandler(LayerUnit unit, UnitHitArgs hit);

        public delegate void OnEnvironmentVarChangedHandler(LayerUnit unit, string key, object value);
        public delegate void OnKeyFrameCustomActionHandler(IKeyFrameProperties soundName);
        public delegate void OnDockingParentChangedHandler(LayerUnit unit, LayerZoneObject docking, DockingOffset dockingOffset);


        private OnSkillChangedHandler mOnSkillChanged;
        private OnActionStatusChangedHandler mOnActionChanged;
        private OnMoneyChangedHandler mOnMoneyChanged;
        private OnChantSkillHandler mOnChantSkill;
        private OnLaunchSkillHandler mOnLaunchSkill;
        private OnSpeedChangedHandler mOnSpeedChanged;
        private OnHPChangedHandler mOnHPChanged;
        private OnMPChangedHandler mOnMPChanged;
        private OnMaxHPChangedHandler mOnMaxHPChanged;
        private OnMaxMPChangedHandler mOnMaxMPChanged;
        private OnVisibleChangedHandler mOnVisibleChanged;
        private OnStartPickObjectHandler mOnStartPickObject;
        private OnStopPickObjectHandler mOnStopPickObject;
        private OnSkillActionChangedHandler mOnSkillActionChanged;
        private OnBuffAddedHandler mOnBuffAdded;
        private OnBuffRemovedHandler mOnBuffRemoved;
        private OnBuffChangedHandler mOnBuffChanged;
        private OnScriptCommandHandler mOnScriptCommand;
        private OnSkillActionStartHandler mOnSkillActionStart;
        private OnDamageHandler mOnDamage;
        private OnDeadHandler mOnDead;
        protected OnEnvironmentVarChangedHandler mOnEnvironmentVarChanged;
        private OnKeyFrameCustomActionHandler mOnKeyFrameCustomAction;
        private OnDockingParentChangedHandler mOnDockingParentChanged;

        public delegate void OnUnitFieldChangedHandler(LayerUnit unit, UnitFieldMask mask);
        public event OnUnitFieldChangedHandler OnUnitFieldChanged;


        public delegate void OnUnitAvatarChangedHandler(LayerUnit unit, string skin, string[] avatar);
        public event OnUnitAvatarChangedHandler OnUnitAvatarChanged;


        private void clearEvents()
        {
            this.OnUnitFieldChanged = null;
            this.OnUnitAvatarChanged = null;
            this.mOnSkillChanged = null;
            this.mOnActionChanged = null;
            this.mOnMoneyChanged = null;
            this.mOnChantSkill = null;
            this.mOnLaunchSkill = null;
            this.mOnSpeedChanged = null;
            this.mOnHPChanged = null;
            this.mOnMPChanged = null;
            this.mOnMaxHPChanged = null;
            this.mOnMaxMPChanged = null;
            this.mOnVisibleChanged = null;
            this.mOnStartPickObject = null;
            this.mOnStopPickObject = null;
            this.mOnSkillActionChanged = null;
            this.mOnBuffAdded = null;
            this.mOnBuffChanged = null;
            this.mOnBuffRemoved = null;
            this.mOnScriptCommand = null;
            this.mOnSkillActionStart = null;
            this.mOnDamage = null; mOnDead = null;
            this.mOnEnvironmentVarChanged = null;
            this.mOnKeyFrameCustomAction = null;
            this.mOnDockingParentChanged = null;
            this.OnHit = null;
        }

        [EventTriggerDescAttribute("单位持有技能发生变化时触发")]
        public event OnSkillChangedHandler OnSkillChanged { add { mOnSkillChanged += value; } remove { mOnSkillChanged -= value; } }
        [EventTriggerDescAttribute("单位状态发生变化时触发")]
        public event OnActionStatusChangedHandler OnActionChanged { add { mOnActionChanged += value; } remove { mOnActionChanged -= value; } }

        [EventTriggerDescAttribute("单位金币发生变化时触发")]
        public event OnMoneyChangedHandler OnMoneyChanged { add { mOnMoneyChanged += value; } remove { mOnMoneyChanged -= value; } }
        [EventTriggerDescAttribute("单位开始吟唱时触发")]
        public event OnChantSkillHandler OnChantSkill { add { mOnChantSkill += value; } remove { mOnChantSkill -= value; } }
        [EventTriggerDescAttribute("单位释放技能时触发")]
        public event OnLaunchSkillHandler OnLaunchSkill { add { mOnLaunchSkill += value; } remove { mOnLaunchSkill -= value; } }

        [EventTriggerDescAttribute("速度变化")]
        public event OnSpeedChangedHandler OnSpeedChanged { add { mOnSpeedChanged += value; } remove { mOnSpeedChanged -= value; } }
        [EventTriggerDescAttribute("HP变化")]
        public event OnHPChangedHandler OnHPChanged { add { mOnHPChanged += value; } remove { mOnHPChanged -= value; } }
        [EventTriggerDescAttribute("MP变化")]
        public event OnMPChangedHandler OnMPChanged { add { mOnMPChanged += value; } remove { mOnMPChanged -= value; } }

        [EventTriggerDescAttribute("MaxHP变化")]
        public event OnMaxHPChangedHandler OnMaxHPChanged { add { mOnMaxHPChanged += value; } remove { mOnMaxHPChanged -= value; } }
        [EventTriggerDescAttribute("MaxMP变化")]
        public event OnMaxMPChangedHandler OnMaxMPChanged { add { mOnMaxMPChanged += value; } remove { mOnMaxMPChanged -= value; } }

        [EventTriggerDescAttribute("外观变化")]
        public event OnVisibleChangedHandler OnVisibleChanged { add { mOnVisibleChanged += value; } remove { mOnVisibleChanged -= value; } }


        [EventTriggerDescAttribute("单位开始检取物品")]
        public event OnStartPickObjectHandler OnStartPickObject { add { mOnStartPickObject += value; } remove { mOnStartPickObject -= value; } }
        [EventTriggerDescAttribute("单位完成/结束检取物品")]
        public event OnStopPickObjectHandler OnStopPickObject { add { mOnStopPickObject += value; } remove { mOnStopPickObject -= value; } }
        [EventTriggerDescAttribute("单位技能动作发生变化")]
        public event OnSkillActionChangedHandler OnSkillActionChanged { add { mOnSkillActionChanged += value; } remove { mOnSkillActionChanged -= value; } }

        [EventTriggerDescAttribute("单位添加BUFF")]
        public event OnBuffAddedHandler OnBuffAdded { add { mOnBuffAdded += value; } remove { mOnBuffAdded -= value; } }
        [EventTriggerDescAttribute("单位BUFF状态改变")]
        public event OnBuffChangedHandler OnBuffChanged { add { mOnBuffChanged += value; } remove { mOnBuffChanged -= value; } }
        [EventTriggerDescAttribute("单位移除BUFF")]
        public event OnBuffRemovedHandler OnBuffRemoved { add { mOnBuffRemoved += value; } remove { mOnBuffRemoved -= value; } }

        [EventTriggerDescAttribute("服务端通知客户端执行指定脚本代码")]
        public event OnScriptCommandHandler OnScriptCommand { add { mOnScriptCommand += value; } remove { mOnScriptCommand -= value; } }

        [EventTriggerDescAttribute("技能动作开始")]
        public event OnSkillActionStartHandler OnSkillActionStart { add { mOnSkillActionStart += value; } remove { mOnSkillActionStart -= value; } }

        [EventTriggerDescAttribute("受击动作开始")]
        public event OnDamageHandler OnDamage { add { mOnDamage += value; } remove { mOnDamage -= value; } }
        [EventTriggerDescAttribute("被攻击，掉血")]
        public event OnHitHandler OnHit;

        [EventTriggerDescAttribute("环境变量发生变化")]
        public event OnEnvironmentVarChangedHandler OnEnvironmentVarChanged { add { mOnEnvironmentVarChanged += value; } remove { mOnEnvironmentVarChanged -= value; } }

        [EventTriggerDescAttribute("播放音效")]
        public event OnKeyFrameCustomActionHandler OnCustomKeyFrame { add { mOnKeyFrameCustomAction += value; } remove { mOnKeyFrameCustomAction -= value; } }

        [EventTriggerDescAttribute("停靠位置改变")]
        public event OnDockingParentChangedHandler OnDockingParentChanged { add { mOnDockingParentChanged += value; } remove { mOnDockingParentChanged -= value; } }



    }



}
