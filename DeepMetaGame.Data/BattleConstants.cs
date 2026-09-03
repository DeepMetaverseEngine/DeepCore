using DeepCore;
using DeepMetaGame.Data.Misc;

namespace DeepMetaGame.Data
{
    internal enum BattleConstants
    {
        // 0x4000 - 0x9FFFF
        //-------------------------------------------------------------------------
        FUNC_TABLE_GROUP = 0x080FF, 
        FUNC_CARD_AFFECTS = 0x081FF,
        //-------------------------------------------------------------------------
        #region GUI

        GUI_HEADER = 0x7300,
        UEBindDataMeta,
        UEMemoryAllocInfoMeta,
        UeZoneEnvironmentListMeta,
        UeUnitEnvironmentListMeta,
        UeZoneEnvironmentLabelMeta,
        UeUnitEnvironmentLabelMeta,
        UETemplateDataBoxMeta,
        UEUnitBuffListMeta,
        UEUnitSkillListMeta,
        UEUnitHPBar,
        UEUnitMPBar,
        UEUnitExpBar,
        UEUnitSPBar,
        UEUnitStatisticsMeta, UEUnitSyncInfoMeta,
        UEFairyGUIComponentMeta,

        #endregion
        //-------------------------------------------------------------------------
        #region Messages

        CLIENT_MSG_HEADER = 0x9000,
        SyncUnitInfo = 0x9010,
        SyncItemInfo,
        SyncSpellInfo,
        SyncObjectsEvent = 0x9001,
        LockActorEvent,
        ConnectToProxy,
        DisconnectFromProxy,
        ClientEnterScene,
        PlayerLeaveScene,
        ClientFocusUnits,

        CLIENT_EVENTS_0x8300 = 0x8300,
        LookAtEvent = 0x8300,
        ChangeTimeScaleEvent,
        GamePauseEvent,
        GameResumeEvent,
        CameraMoveToEvent,
        CameraFocusUnitEvent,
        CameraZoomToEvent,
        CameraRotateToEvent,
        CameraResetEvent,
        ClientEventQueue,
        CameraHoldEvent,
        CameraControlEvent,

        MESSAGES_CONTROL = 0x8200,
        UnitGuardAction = 0x8200,
        UnitAttackToAction,
        UnitFaceToAction,
        UnitJumpAction,
        UnitStopMoveAction,
        UnitFocuseTargetAction,
        UnitLaunchSkillRequest,
        UnitLaunchSkillResponse,
        UnitCancelSkillRequest,
        UnitPickObjectAction,
        UnitStopPickObjectAction,
        UnitUseItemAction,
        UnitAxisAction,
        UnitCustomAxisAction,
        UnitUpdatePosAction,
        UnitSetSyncModeAction,
        UnitCancelBuffAction,
        UnitGetStatisticRequest,
        UnitGetStatisticResponse,
        UnitSetSubStateAction,
        UnitReadyAction,
        UnitFollowTargetAction,
        UnitClientCustomMoveAction,
        UnitClimbAction,
        UnitStatisticData,
        UnitAxis3DAction,
        ComponentFieldChangeAction,
        PlayerGuardEvent,
        UnitStartSomersaultAction,
        UnitStopSomersaultAction,
        UnitSkipCGAction,
        PlayerSetEnvVarAction,

        MESSAGES_GUI = 0x8A00,
        ShowFormEvent,
        CloseFormEvent,
        CloseFormAction,
        ShowPlayerFormEvent,
        GUINodeClickAction = 0x8A81,
        GUINodeBindDataEvent,
        GUINodeDataChangedAction,
        GUINodeVisibleEvent,
        GUINodeControlEvent,

        MESSAGES_HUD = 0x8900,
        Raycast = 0x8900,
        MouseDownAction, MouseUpAction,
        MouseMoveAction,
        MouseClickAction,
        KeyDownAction,
        KeyUpAction,
        CameraOffset,
        MouseSelectObjectAction,

        MESSAGES_OBJECT = 0x8100,
        UnitFieldChangedEvent = 0x8100,
        UnitChantSkillEvent,
        UnitLaunchSkillEvent,
        UnitEffectEvent,
        UnitDoActionEvent,
        UnitHitEvent,
        UnitHitMoveEvent,
        UnitDeadEvent,
        UnitLaunchBuffEvent,
        UnitStopBuffEvent,
        UnitSyncBuffEvent,
        UnitSyncInventoryItemEvent,
        UnitUseItemEvent,
        UnitSyncMultiTimeLine,
        UnitRebirthEvent,
        UnitDamageEvent,
        UnitSkillActionChangeEvent,
        UnitStartPickObjectEvent,
        UnitStopPickObjectEvent,
        UnitGotZoneItemEvent,
        UnitJumpEvent,
        UnitForceSyncPosEvent,
        ObjectForceSyncPosEvent,
        ObjectForceSyncFaceEvent,
        UnitForceSyncStateEvent,
        SpellLockTargetEvent,
        SpellSyncEvent,
        UnitLaunchAuraEvent,
        UnitStopAuraEvent,
        UnitVisibleChangedEvent,
        ComponentFieldChangeEvent,
        ObjectSkillTimeChangedEvent,

        MESSAGES_PLAYER = 0x8400,
        PlayerCDEvent = 0x8400,
        PlayerSkillChangedEvent,
        UnitSyncEnvironmentVarEvent,
        PlayerSyncEnvironmentVarEvent,
        PlayerSkillStopEvent,
        PlayerSkillAddedEvent,
        PlayerSkillRemovedEvent,
        PlayerSkillRefreshEvent,
        PlayerScriptCommandEvent,
        PlayerSkillActiveChangedEvent,
        PlayerSkillTimeChangedEvent,
        PlayerFocuseTargetEvent,
        PlayerSyncCardsEvent,

        MESSAGES_SYSTEM = 0x8600,
        Ping = 0x8600,
        Pong,
        NetPong,
        TestMessageBox,
        ServerStatusB2C,
        ServerExceptionB2C,
        PackAction,
        PackNotify,
        ZonePauseNotify,

        MESSAGES_TEXT = 0x8500,
        TextMessage,
        ChatAction,
        ChatNotify,
        BubbleTalkNotify,

        MESSAGES_ZONE = 0x8000,
        AddUnitEvent,
        AddSpellEvent,
        AddEffectEvent,
        AddItemEvent,
        RemoveObjectEvent,
        SyncPosEvent,
        DoScriptEvent,
        ScriptCommandEvent,
        GameOverEvent,
        DecorationChangedEvent,
        SyncEnvironmentVarEvent,
        ChangeBGMEvent,
        FlagTagChangedEvent, FlagEnableChangedEvent,
        SyncFlagsEvent,
        ZoneVarTemplate = 0x8F01,
        ZoneVarObject,
        ZoneVarObjectBuff,
        ZoneVarObjectSkill,
        ZoneVarObjectAura,

        #endregion
        //-------------------------------------------------------------------------
        #region MISC

        MISC = 0x4000,
        AttackProp,
        CardSlot,
        FocusTarget,
        DropItem,
        DropItemList,
        InventoryItem,
        UseItem,
        LaunchAura,
        LaunchBuff,
        LaunchEffect,
        EffectBlur,
        EffectCameraMotion,
        EffectWarning,
        LaunchSkill,
        LaunchSpell,

        ZoneShapePoint,
        ZoneShapeRect,
        ZoneShapeRound,
        ZoneShapeEllipse,
        ZoneShapeLine,
        ZoneShapeStripWidth,
        DockingOffset,

        SummonUnit,
        TeamFormation,
        TerrainDefinitionMap,
        MapBlockBrush,
        UnitActionDefinitionMap,
        UnitAction,
        UnitActionKeyFrame, UnitActionKeyFrameState, UnitActionKeyFrameParam, UnitActionKeyFrameTrigger,
        UnitAnimation,
        UnitAttachment,
        UnitFlyOpt,
        BlinkMove,
        StartMove,
        ResourceDesc,
        ResourcePropertiesMap, ResourcePropertiesTuple,
        ResourceTransform,

        SCENE_GRAPH = 0x4C00,
        SceneNextLink,
        SceneMapNode,
        SceneGraphData,


        #endregion
        //-------------------------------------------------------------------------
        #region TEMPLATES

        TEMPLATES = 0x4700,
        Config,

        AuraTemplate,

        BuffTemplate,
        BuffTemplateKeyFrame,
        BuffStateChangeAbility,
        BuffSpeedChangeAbility,
        BuffEffectAbility,
        BuffOverlayAbility,
        BuffAvatarChangeAbility,

        CardTemplate,
        CardTemplateCardField,
        CardTemplateCardFieldCell,
        CardTemplateCardReference,
        CardTemplateCardDependence,

        BattleUITemplate,

        ItemTemplate,
        ItemResource, ItemUseValue, ItemMotion, ItemBuyable, ItemInventory, ItemUseable, ItemEquip, ItemPickable,

        SkillTemplate,
        SkillUnitActionData, SkillStatusChange, SkillAttackShape, SkillKeyFrame, SkillLaunchMode,
        SkillInit,

        SpellTemplate,
        SpellKeyFrame,

        UnitInfo,
        UnitResourceAbility, UnitGuardAbility, UnitRecoverAbility, UnitMotionAbility, UnitSkillAbility,
        UnitDropItemAbility, UnitInventoryAbility, UnitSpawnAbility, UnitResourceBodyAbility, UnitAttachmentAbility, UnitDragAndDropAbility,

        UnitEventTemplate,

        ZoneInfo,

        #endregion
        //-------------------------------------------------------------------------
        #region ZONE_EDITOR


        PlayerStartAbilityData = 0x4401,
        SpawnUnitAbilityData, SpawnUnit, SpawnUnitGroup,
        UnitTransportAbilityData,
        SceneTransportAbilityData,
        SpawnItemAbilityData, 
        SpawnItem,

        CameraFocusAbilityData,
        CameraPositionAbilityData, 
        CameraTargetAbilityData,

        PointHoldAbility,
        SceneUIAbility,

        SceneData = 0x4300,
        VoxelInfo,
        TerrainData,

        SceneUnitData,
        SceneItemData,
        SceneRegionData,
        SceneDecorationData,
        ScenePointData,
        SceneAreaData,

        ZoneVar= 0x4500,
        ZoneEvent = 0x4501,
        UnitEvent,
        GUIEvent, 
        EventLocalVars,
        EventTriggers,
        EventConditions,
        EventActions,
        UnitCustomEvent,



        EditorTemplatesMeta = 0x4FFF,
        #endregion
        //-------------------------------------------------------------------------



        //-------------------------------------------------------------------------
    }

}
