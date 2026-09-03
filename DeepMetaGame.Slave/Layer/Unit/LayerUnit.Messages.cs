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
    public partial class LayerUnit : LayerZoneEntity, IZoneUnit, IEnvironmentObject
    {
        //--------------------------------------------------------------------------------

        #region Events ---------------------------------------------------------------------------------

        override internal protected void DoEvent(ObjectNotify e)
        {
            if (e is UnitDamageEvent)
            {
                DoDamage(e as UnitDamageEvent);
            }
            else if (e is UnitLaunchSkillEvent)
            {
                DoLaunchSkill(e as UnitLaunchSkillEvent);
            }
            else if (e is UnitSkillActionChangeEvent)
            {
                DoChangeAction(e as UnitSkillActionChangeEvent);
            }
            else if (e is UnitDeadEvent)
            {
                DoDead(e as UnitDeadEvent);
            }
            else if (e is UnitHitEvent)
            {
                DoHit(e as UnitHitEvent);
            }
            //-------------------------------------------------------
            else if (e is UnitLaunchBuffEvent)
            {
                DoLaunchBuff(e as UnitLaunchBuffEvent);
            }
            else if (e is UnitStopBuffEvent)
            {
                DoStopBuff(e as UnitStopBuffEvent);
            }
            else if (e is UnitSyncBuffEvent)
            {
                DoSyncBuff(e as UnitSyncBuffEvent);
            }
            //-------------------------------------------------------
            else if (e is UnitLaunchAuraEvent)
            {
                DoLaunchAura(e as UnitLaunchAuraEvent);
            }
            else if (e is UnitStopAuraEvent)
            {
                DoStopAura(e as UnitStopAuraEvent);
            }
            //-------------------------------------------------------
            else if (e is UnitFieldChangedEvent)
            {
                DoSyncFields(e as UnitFieldChangedEvent);
            }
            else if (e is UnitVisibleChangedEvent)
            {
                DoUnitVisibleChangedEvent(e as UnitVisibleChangedEvent);
            }
            else if (e is UnitStartPickObjectEvent)
            {
                DoStartPick(e as UnitStartPickObjectEvent);
            }
            else if (e is UnitStopPickObjectEvent)
            {
                DoStopPick(e as UnitStopPickObjectEvent);
            }
            else if (e is UnitJumpEvent)
            {
                DoJump(e as UnitJumpEvent);
            }
            else if (e is UnitForceSyncPosEvent)
            {
                DoForceSyncPosEvent(e as UnitForceSyncPosEvent);
            }
            else if (e is UnitForceSyncStateEvent)
            {
                DoForceSyncStateEvent(e as UnitForceSyncStateEvent);
            }
            else if (e is ObjectForceSyncFaceEvent)
            {
                DoForceSyncFaceEvent(e as ObjectForceSyncFaceEvent);
            }
            else if (e is UnitChantSkillEvent)
            {
                DoUnitChantSkillEvent(e as UnitChantSkillEvent);
            }
            else if (e is UnitSyncMultiTimeLine)
            {
                DoUnitSyncMultiTimeLine(e as UnitSyncMultiTimeLine);
            }
            else if (e is UnitSyncEnvironmentVarEvent)
            {
                DoSyncUnitVarEvent(e as UnitSyncEnvironmentVarEvent);
            }
            //----------------------- player ----------------------//
            else if (e is PlayerCDEvent)
            {
                DoSkillCDChanged(e as PlayerCDEvent);
            }
            else if (e is PlayerSkillChangedEvent)
            {
                DoSkillChanged(e as PlayerSkillChangedEvent);
            }
            else if (e is PlayerSkillAddedEvent)
            {
                DoSkillAdded(e as PlayerSkillAddedEvent);
            }
            else if (e is PlayerSkillRefreshEvent)
            {
                DoSkillRefresh(e as PlayerSkillRefreshEvent);
            }
            else if (e is PlayerSkillRemovedEvent)
            {
                DoSkillRemoved(e as PlayerSkillRemovedEvent);
            }
            else if (e is PlayerSkillTimeChangedEvent)
            {
                DoPlayerSkillTimeChangedEvent(e as PlayerSkillTimeChangedEvent);
            }
            else if (e is ObjectSkillTimeChangedEvent evt)
            {
                DoObjectSkillTimeChangedEvent(evt);
            }
            else if (e is PlayerSkillStopEvent)
            {
                DoPlayerSkillStopEvent(e as PlayerSkillStopEvent);
            }
            else if (e is PlayerScriptCommandEvent)
            {
                DoPlayerScriptCommandEvent(e as PlayerScriptCommandEvent);
            }
            else if (e is PlayerSkillActiveChangedEvent)
            {
                DoPlayerSkillActiveChangedEvent(e as PlayerSkillActiveChangedEvent);
            }
            else if (e is ComponentFieldChangeEvent compEvt)
            {
                //      Components.SyncComponentFields(compEvt.ComponentTag, compEvt.Fields, true, true);
            }
            else if (e is PlayerSyncCardsEvent)
            {
                SyncCards(e as PlayerSyncCardsEvent);
            }
            //----------------------- player ----------------------//
        }

        protected virtual void DoSyncFields(UnitFieldChangedEvent syn)
        {
            if ((syn.mask & UnitFieldMask.MASK_PAUSED) != 0)
            {
                if (this.mPaused != syn.paused)
                {
                    this.mPaused = syn.paused;
                }
            }
            if ((syn.mask & UnitFieldMask.MASK_HP) != 0)
            {
                if (mHP != syn.currentHP)
                {
                    var old = mHP;
                    this.mHP = syn.currentHP;
                    this.mOnHPChanged?.Invoke(this, old, syn.currentHP);
                }
            }
            if ((syn.mask & UnitFieldMask.MASK_MP) != 0)
            {
                if (mMP != syn.currentMP)
                {
                    var old = mMP;
                    this.mMP = syn.currentMP;
                    this.mOnMPChanged?.Invoke(this, old, syn.currentMP);
                }
            }
            if ((syn.mask & UnitFieldMask.MASK_MAX_HP) != 0)
            {
                if (this.mMaxHP != syn.maxHP)
                {
                    var old = mMaxHP;
                    this.mMaxHP = syn.maxHP;
                    this.mOnMaxHPChanged?.Invoke(this, old, syn.maxHP);
                }
            }
            if ((syn.mask & UnitFieldMask.MASK_MAX_MP) != 0)
            {
                if (this.mMaxMP != syn.maxMP)
                {
                    var old = mMaxMP;
                    this.mMaxMP = syn.maxMP;
                    this.mOnMaxMPChanged?.Invoke(this, old, syn.maxMP);
                }
            }
            if ((syn.mask & UnitFieldMask.MASK_SP) != 0)
            {
                this.mSP = syn.currentSP;
            }
            if ((syn.mask & UnitFieldMask.MASK_MAX_SP) != 0)
            {
                this.mMaxSP = syn.maxSP;
            }
            {
                var speedChange = false;
                if ((syn.mask & UnitFieldMask.MASK_SPEED) != 0)
                {
                    this.mMoveSpeedSEC = syn.currentSpeed;
                    speedChange = true;
                }
                if ((syn.mask & UnitFieldMask.MASK_FCR) != 0)
                {
                    this.mFastCastRate = syn.currentFCR;
                    speedChange = true;
                }
                if ((syn.mask & UnitFieldMask.MASK_FAR) != 0)
                {
                    this.mFastActionRate = syn.currentFAR;
                    speedChange = true;
                }
                if ((syn.mask & UnitFieldMask.MASK_FMR) != 0)
                {
                    this.mFastMoveRate = syn.currentFMR;
                    speedChange = true;
                }
                if (speedChange)
                {
                    this.mOnSpeedChanged?.Invoke(this);
                }
            }
            if ((syn.mask & UnitFieldMask.MASK_MONEY) != 0)
            {
                if (mMoney != syn.currentMoney)
                {
                    var old = this.mMoney;
                    this.mMoney = syn.currentMoney;
                    this.mOnMoneyChanged?.Invoke(this, old, syn.currentMoney);
                }
            }
            if ((syn.mask & UnitFieldMask.MASK_LEVEL) != 0)
            {
                this.mLevel = syn.level;
                this.mNextExp = Parent.DataRoot.DataCenter.GetUnitNeedExp(Parent.Data, this.Info, this.Level + 1);
                var org = Parent.DataRoot.DataCenter.GetUnitNeedExp(Parent.Data, this.Info, this.Level);
                this.ExpAmount = (float)((double)(mExp - org) / (double)(mNextExp - org));
            }
            if ((syn.mask & UnitFieldMask.MASK_EXP) != 0)
            {
                this.mExp = syn.exp;
                var org = Parent.DataRoot.DataCenter.GetUnitNeedExp(Parent.Data, this.Info, this.Level);
                this.ExpAmount = (float)((double)(mExp - org) / (double)(mNextExp - org));
            }
            if ((syn.mask & UnitFieldMask.MASK_INVENTORY) != 0) { this.mInventorySize = syn.inventorySize; }
            if ((syn.mask & UnitFieldMask.MASK_DISPLAY_NAME) != 0) { this.mDisplayName = syn.displayName; }

            if ((syn.mask & UnitFieldMask.MASK_ZONE_SHAPE) != 0) { this.mZoneShape = syn.zoneShape; }
            if ((syn.mask & UnitFieldMask.MASK_GRAVITY) != 0) { this.Gravity = syn.currentGravity; }
            if ((syn.mask & UnitFieldMask.MASK_CURRENTTARGET) != 0) { this.CurrentTarget = syn.currentTarget; }
            if ((syn.mask & UnitFieldMask.MASK_PICK_RANGE) != 0) { this.mPickRange = syn.pickRange; }
            if ((syn.mask & UnitFieldMask.MASK_BODY_SCALE) != 0) { this.mBodyScale = syn.bodyScale; }
            if ((syn.mask & UnitFieldMask.MASK_RES_SCALE) != 0) { this.mResScale = syn.resScale; }

            if ((syn.mask & UnitFieldMask.MASK_DOCKING_POS) != 0) { this.DockingOffset = syn.dockingOffset; }
            if ((syn.mask & UnitFieldMask.MASK_DOCKING_OBJ) != 0)
            {
                var oldDocking = this.DockingParentID;
                this.DockingParentID = syn.dockingObj;
                if (DockingParentID != oldDocking)
                {
                    mOnDockingParentChanged?.Invoke(this, GetDockingParent(), DockingOffset);
                }
            }


            if ((syn.mask & UnitFieldMask.MASK_SKIN) != 0) { this.Skin = syn.skin; }
            if ((syn.mask & UnitFieldMask.MASK_AVATAR) != 0) { this.Avatar = syn.avatar; }
            if ((syn.mask & (UnitFieldMask.MASK_AVATAR | UnitFieldMask.MASK_SKIN)) != 0)
            {
                OnUnitAvatarChanged?.Invoke(this, this.Skin, this.Avatar);
            }

            if ((syn.mask & UnitFieldMask.MASK_DUMMY_0) != 0) { this.Dummy_0 = syn.dummy_0; }
            if ((syn.mask & UnitFieldMask.MASK_DUMMY_1) != 0) { this.Dummy_1 = syn.dummy_1; }
            if ((syn.mask & UnitFieldMask.MASK_DUMMY_2) != 0) { this.Dummy_2 = syn.dummy_2; }
            if ((syn.mask & UnitFieldMask.MASK_DUMMY_3) != 0) { this.Dummy_3 = syn.dummy_3; }
            if ((syn.mask & UnitFieldMask.MASK_DUMMY_4) != 0) { this.Dummy_4 = syn.dummy_4; }
            if ((syn.mask & UnitFieldMask.MASK_DUMMY_5) != 0) { this.Dummy_5 = syn.dummy_5; }

            OnUnitFieldChanged?.Invoke(this, syn.mask);
        }
        protected virtual void DoForceSyncPosEvent(UnitForceSyncPosEvent e)
        {
            mRemotePos.X = e.Position.X;
            mRemotePos.Y = e.Position.Y;
            mRemotePos.Z = e.Position.Z;
            mLocalPos.SetPos(e.Position);

            base.mDirection.ForceSync(e.Direction, e.BodyDirection);

            mRemoteState.UnitMainState = (UnitActionStatus)e.UnitMainState;
            mRemoteState.UnitSubState = e.UnitSubState;
            LayerUpward = e.LayerUpward;
            this.ForceSyncCurrentState((UnitActionStatus)e.UnitMainState, e.UnitSubState, e);
        }
        protected virtual void DoForceSyncStateEvent(UnitForceSyncStateEvent e)
        {
            mRemoteState.UnitMainState = (UnitActionStatus)e.UnitMainState;
            mRemoteState.UnitSubState = e.UnitSubState;
            this.ForceSyncCurrentState((UnitActionStatus)e.UnitMainState, e.UnitSubState, e);
        }
        protected override void DoForceSyncFaceEvent(ObjectForceSyncFaceEvent e)
        {
            mDirection.ForceSync(e.Direction, e.BodyDirection);
        }
        protected override void DoForceSyncPosEvent(ObjectForceSyncPosEvent e)
        {
            mRemotePos.X = e.Pos.X;
            mRemotePos.Y = e.Pos.Y;
            mRemotePos.Z = e.Pos.Z;
            mLocalPos.SetPos(e.Pos);
            mDirection.ForceSync(e.Direction, e.BodyDirection);
        }
        protected virtual void DoSyncUnitVarEvent(UnitSyncEnvironmentVarEvent e)
        {
            if (mEnvironmentVarMap.TrySet(e.Var, out var k, out var v))
            {
                mOnEnvironmentVarChanged?.Invoke(this, k, v);
            }
        }
        protected virtual void DoPlayerScriptCommandEvent(PlayerScriptCommandEvent e)
        {
            if (mOnScriptCommand != null)
            {
                mOnScriptCommand.Invoke(this, e.message);
            }
        }
        protected virtual void DoHit(UnitHitEvent me)
        {
            //this.mHP -= me.hp;
            if (me.effect != null)
            {
                Parent.PreQueueEvent(ObjectPool.Alloc<UnitEffectEvent>().Init (this.ObjectID, me.effect));
            }
            OnHit?.Invoke(this, me.ToArgs());
        }
        protected virtual void DoUnitVisibleChangedEvent(UnitVisibleChangedEvent evt)
        {
            this.SyncInfo.VisibleInfo = evt.data;
            mOnVisibleChanged?.Invoke(this, SyncInfo.VisibleInfo);
        }
        protected virtual void DoKeyFrameCustomAction(IKeyFrameProperties soundName)
        {
            mOnKeyFrameCustomAction?.Invoke(soundName);
        }

        #endregion
        //--------------------------------------------------------------------------------



    }



}
