using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Unity.Transforms;

public struct Char_TerraformerGhostSerializer : IGhostSerializer<Char_TerraformerSnapshotData>
{
    private ComponentType componentTypeAbilityCollectionAbilityEntry;
    private ComponentType componentTypeAbilityCollectionState;
    private ComponentType componentTypeAbilityOwnerOwnedAbility;
    private ComponentType componentTypeAbilityOwnerOwnedCollection;
    private ComponentType componentTypeAbilityOwnerState;
    private ComponentType componentTypeAimDataData;
    private ComponentType componentTypeAnimSourceControllerRigData;
    private ComponentType componentTypeAnimSourceControllerSettings;
    private ComponentType componentTypeCharacterInterpolatedData;
    private ComponentType componentTypeCharacterPredictedData;
    private ComponentType componentTypeCharacterReplicatedData;
    private ComponentType componentTypeCharacterSettings;
    private ComponentType componentTypeCharacterControllerComponentData;
    private ComponentType componentTypeCharacterControllerGroundSupportData;
    private ComponentType componentTypeCharacterControllerInitializationData;
    private ComponentType componentTypeCharacterControllerMoveQuery;
    private ComponentType componentTypeCharacterControllerMoveResult;
    private ComponentType componentTypeCharacterControllerVelocity;
    private ComponentType componentTypeDamageEvent;
    private ComponentType componentTypeDamageHistoryData;
    private ComponentType componentTypeHealthStateData;
    private ComponentType componentTypeHitColliderOwnerState;
    private ComponentType componentTypeInventoryInternalState;
    private ComponentType componentTypeInventoryItemEntry;
    private ComponentType componentTypeInventoryState;
    private ComponentType componentTypePartOwnerInputState;
    private ComponentType componentTypePartOwnerRegistryAsset;
    private ComponentType componentTypePlayerOwnerPlayerId;
    private ComponentType componentTypePlayerControlledState;
    private ComponentType componentTypeSkeletonRenderer;
    private ComponentType componentTypeLocalToWorld;
    private ComponentType componentTypeRotation;
    private ComponentType componentTypeTranslation;
    private ComponentType componentTypeLinkedEntityGroup;
    // FIXME: These disable safety since all serializers have an instance of the same type - causing aliasing. Should be fixed in a cleaner way
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Character.InterpolatedData> ghostCharacterInterpolatedDataType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Character.PredictedData> ghostCharacterPredictedDataType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Character.ReplicatedData> ghostCharacterReplicatedDataType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<CharacterControllerGroundSupportData> ghostCharacterControllerGroundSupportDataType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<CharacterControllerMoveResult> ghostCharacterControllerMoveResultType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<CharacterControllerVelocity> ghostCharacterControllerVelocityType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<HealthStateData> ghostHealthStateDataType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Inventory.State> ghostInventoryStateType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Player.OwnerPlayerId> ghostPlayerOwnerPlayerIdType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<PlayerControlled.State> ghostPlayerControlledStateType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkBufferType<LinkedEntityGroup> ghostLinkedEntityGroupType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<Ability.AbilityControl> ghostChild0AbilityAbilityControlType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<AbilityMovement.InterpolatedState> ghostChild0AbilityMovementInterpolatedStateType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<AbilityMovement.PredictedState> ghostChild0AbilityMovementPredictedStateType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<Ability.AbilityControl> ghostChild1AbilityAbilityControlType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<AbilitySprint.PredictedState> ghostChild1AbilitySprintPredictedStateType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<Ability.AbilityControl> ghostChild2AbilityAbilityControlType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<Ability.AbilityControl> ghostChild3AbilityAbilityControlType;


    public int CalculateImportance(ArchetypeChunk chunk)
    {
        return 1;
    }

    public bool WantsPredictionDelta => true;

    public int SnapshotSize => UnsafeUtility.SizeOf<Char_TerraformerSnapshotData>();
    public void BeginSerialize(ComponentSystemBase system)
    {
        componentTypeAbilityCollectionAbilityEntry = ComponentType.ReadWrite<AbilityCollection.AbilityEntry>();
        componentTypeAbilityCollectionState = ComponentType.ReadWrite<AbilityCollection.State>();
        componentTypeAbilityOwnerOwnedAbility = ComponentType.ReadWrite<AbilityOwner.OwnedAbility>();
        componentTypeAbilityOwnerOwnedCollection = ComponentType.ReadWrite<AbilityOwner.OwnedCollection>();
        componentTypeAbilityOwnerState = ComponentType.ReadWrite<AbilityOwner.State>();
        componentTypeAimDataData = ComponentType.ReadWrite<AimData.Data>();
        componentTypeAnimSourceControllerRigData = ComponentType.ReadWrite<AnimSourceController.RigData>();
        componentTypeAnimSourceControllerSettings = ComponentType.ReadWrite<AnimSourceController.Settings>();
        componentTypeCharacterInterpolatedData = ComponentType.ReadWrite<Character.InterpolatedData>();
        componentTypeCharacterPredictedData = ComponentType.ReadWrite<Character.PredictedData>();
        componentTypeCharacterReplicatedData = ComponentType.ReadWrite<Character.ReplicatedData>();
        componentTypeCharacterSettings = ComponentType.ReadWrite<Character.Settings>();
        componentTypeCharacterControllerComponentData = ComponentType.ReadWrite<CharacterControllerComponentData>();
        componentTypeCharacterControllerGroundSupportData = ComponentType.ReadWrite<CharacterControllerGroundSupportData>();
        componentTypeCharacterControllerInitializationData = ComponentType.ReadWrite<CharacterControllerInitializationData>();
        componentTypeCharacterControllerMoveQuery = ComponentType.ReadWrite<CharacterControllerMoveQuery>();
        componentTypeCharacterControllerMoveResult = ComponentType.ReadWrite<CharacterControllerMoveResult>();
        componentTypeCharacterControllerVelocity = ComponentType.ReadWrite<CharacterControllerVelocity>();
        componentTypeDamageEvent = ComponentType.ReadWrite<DamageEvent>();
        componentTypeDamageHistoryData = ComponentType.ReadWrite<DamageHistoryData>();
        componentTypeHealthStateData = ComponentType.ReadWrite<HealthStateData>();
        componentTypeHitColliderOwnerState = ComponentType.ReadWrite<HitColliderOwner.State>();
        componentTypeInventoryInternalState = ComponentType.ReadWrite<Inventory.InternalState>();
        componentTypeInventoryItemEntry = ComponentType.ReadWrite<Inventory.ItemEntry>();
        componentTypeInventoryState = ComponentType.ReadWrite<Inventory.State>();
        componentTypePartOwnerInputState = ComponentType.ReadWrite<PartOwner.InputState>();
        componentTypePartOwnerRegistryAsset = ComponentType.ReadWrite<PartOwner.RegistryAsset>();
        componentTypePlayerOwnerPlayerId = ComponentType.ReadWrite<Player.OwnerPlayerId>();
        componentTypePlayerControlledState = ComponentType.ReadWrite<PlayerControlled.State>();
        componentTypeSkeletonRenderer = ComponentType.ReadWrite<SkeletonRenderer>();
        componentTypeLocalToWorld = ComponentType.ReadWrite<LocalToWorld>();
        componentTypeRotation = ComponentType.ReadWrite<Rotation>();
        componentTypeTranslation = ComponentType.ReadWrite<Translation>();
        componentTypeLinkedEntityGroup = ComponentType.ReadWrite<LinkedEntityGroup>();
        ghostCharacterInterpolatedDataType = system.GetArchetypeChunkComponentType<Character.InterpolatedData>(true);
        ghostCharacterPredictedDataType = system.GetArchetypeChunkComponentType<Character.PredictedData>(true);
        ghostCharacterReplicatedDataType = system.GetArchetypeChunkComponentType<Character.ReplicatedData>(true);
        ghostCharacterControllerGroundSupportDataType = system.GetArchetypeChunkComponentType<CharacterControllerGroundSupportData>(true);
        ghostCharacterControllerMoveResultType = system.GetArchetypeChunkComponentType<CharacterControllerMoveResult>(true);
        ghostCharacterControllerVelocityType = system.GetArchetypeChunkComponentType<CharacterControllerVelocity>(true);
        ghostHealthStateDataType = system.GetArchetypeChunkComponentType<HealthStateData>(true);
        ghostInventoryStateType = system.GetArchetypeChunkComponentType<Inventory.State>(true);
        ghostPlayerOwnerPlayerIdType = system.GetArchetypeChunkComponentType<Player.OwnerPlayerId>(true);
        ghostPlayerControlledStateType = system.GetArchetypeChunkComponentType<PlayerControlled.State>(true);
        ghostLinkedEntityGroupType = system.GetArchetypeChunkBufferType<LinkedEntityGroup>(true);
        ghostChild0AbilityAbilityControlType = system.GetComponentDataFromEntity<Ability.AbilityControl>(true);
        ghostChild0AbilityMovementInterpolatedStateType = system.GetComponentDataFromEntity<AbilityMovement.InterpolatedState>(true);
        ghostChild0AbilityMovementPredictedStateType = system.GetComponentDataFromEntity<AbilityMovement.PredictedState>(true);
        ghostChild1AbilityAbilityControlType = system.GetComponentDataFromEntity<Ability.AbilityControl>(true);
        ghostChild1AbilitySprintPredictedStateType = system.GetComponentDataFromEntity<AbilitySprint.PredictedState>(true);
        ghostChild2AbilityAbilityControlType = system.GetComponentDataFromEntity<Ability.AbilityControl>(true);
        ghostChild3AbilityAbilityControlType = system.GetComponentDataFromEntity<Ability.AbilityControl>(true);
    }

    public bool CanSerialize(EntityArchetype arch)
    {
        var components = arch.GetComponentTypes();
        int matches = 0;
        for (int i = 0; i < components.Length; ++i)
        {
            if (components[i] == componentTypeAbilityCollectionAbilityEntry)
                ++matches;
            if (components[i] == componentTypeAbilityCollectionState)
                ++matches;
            if (components[i] == componentTypeAbilityOwnerOwnedAbility)
                ++matches;
            if (components[i] == componentTypeAbilityOwnerOwnedCollection)
                ++matches;
            if (components[i] == componentTypeAbilityOwnerState)
                ++matches;
            if (components[i] == componentTypeAimDataData)
                ++matches;
            if (components[i] == componentTypeAnimSourceControllerRigData)
                ++matches;
            if (components[i] == componentTypeAnimSourceControllerSettings)
                ++matches;
            if (components[i] == componentTypeCharacterInterpolatedData)
                ++matches;
            if (components[i] == componentTypeCharacterPredictedData)
                ++matches;
            if (components[i] == componentTypeCharacterReplicatedData)
                ++matches;
            if (components[i] == componentTypeCharacterSettings)
                ++matches;
            if (components[i] == componentTypeCharacterControllerComponentData)
                ++matches;
            if (components[i] == componentTypeCharacterControllerGroundSupportData)
                ++matches;
            if (components[i] == componentTypeCharacterControllerInitializationData)
                ++matches;
            if (components[i] == componentTypeCharacterControllerMoveQuery)
                ++matches;
            if (components[i] == componentTypeCharacterControllerMoveResult)
                ++matches;
            if (components[i] == componentTypeCharacterControllerVelocity)
                ++matches;
            if (components[i] == componentTypeDamageEvent)
                ++matches;
            if (components[i] == componentTypeDamageHistoryData)
                ++matches;
            if (components[i] == componentTypeHealthStateData)
                ++matches;
            if (components[i] == componentTypeHitColliderOwnerState)
                ++matches;
            if (components[i] == componentTypeInventoryInternalState)
                ++matches;
            if (components[i] == componentTypeInventoryItemEntry)
                ++matches;
            if (components[i] == componentTypeInventoryState)
                ++matches;
            if (components[i] == componentTypePartOwnerInputState)
                ++matches;
            if (components[i] == componentTypePartOwnerRegistryAsset)
                ++matches;
            if (components[i] == componentTypePlayerOwnerPlayerId)
                ++matches;
            if (components[i] == componentTypePlayerControlledState)
                ++matches;
            if (components[i] == componentTypeSkeletonRenderer)
                ++matches;
            if (components[i] == componentTypeLocalToWorld)
                ++matches;
            if (components[i] == componentTypeRotation)
                ++matches;
            if (components[i] == componentTypeTranslation)
                ++matches;
            if (components[i] == componentTypeLinkedEntityGroup)
                ++matches;
        }
        return (matches == 34);
    }

    public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref Char_TerraformerSnapshotData snapshot, GhostSerializerState serializerState)
    {
        snapshot.tick = tick;
        var chunkDataCharacterInterpolatedData = chunk.GetNativeArray(ghostCharacterInterpolatedDataType);
        var chunkDataCharacterPredictedData = chunk.GetNativeArray(ghostCharacterPredictedDataType);
        var chunkDataCharacterReplicatedData = chunk.GetNativeArray(ghostCharacterReplicatedDataType);
        var chunkDataCharacterControllerGroundSupportData = chunk.GetNativeArray(ghostCharacterControllerGroundSupportDataType);
        var chunkDataCharacterControllerMoveResult = chunk.GetNativeArray(ghostCharacterControllerMoveResultType);
        var chunkDataCharacterControllerVelocity = chunk.GetNativeArray(ghostCharacterControllerVelocityType);
        var chunkDataHealthStateData = chunk.GetNativeArray(ghostHealthStateDataType);
        var chunkDataInventoryState = chunk.GetNativeArray(ghostInventoryStateType);
        var chunkDataPlayerOwnerPlayerId = chunk.GetNativeArray(ghostPlayerOwnerPlayerIdType);
        var chunkDataPlayerControlledState = chunk.GetNativeArray(ghostPlayerControlledStateType);
        var chunkDataLinkedEntityGroup = chunk.GetBufferAccessor(ghostLinkedEntityGroupType);
        snapshot.SetCharacterInterpolatedDataPosition(chunkDataCharacterInterpolatedData[ent].Position, serializerState);
        snapshot.SetCharacterInterpolatedDatarotation(chunkDataCharacterInterpolatedData[ent].rotation, serializerState);
        snapshot.SetCharacterInterpolatedDataaimYaw(chunkDataCharacterInterpolatedData[ent].aimYaw, serializerState);
        snapshot.SetCharacterInterpolatedDataaimPitch(chunkDataCharacterInterpolatedData[ent].aimPitch, serializerState);
        snapshot.SetCharacterInterpolatedDatamoveYaw(chunkDataCharacterInterpolatedData[ent].moveYaw, serializerState);
        snapshot.SetCharacterInterpolatedDatacharAction(chunkDataCharacterInterpolatedData[ent].charAction, serializerState);
        snapshot.SetCharacterInterpolatedDatacharActionTick(chunkDataCharacterInterpolatedData[ent].charActionTick, serializerState);
        snapshot.SetCharacterInterpolatedDatadamageTick(chunkDataCharacterInterpolatedData[ent].damageTick, serializerState);
        snapshot.SetCharacterInterpolatedDatadamageDirection(chunkDataCharacterInterpolatedData[ent].damageDirection, serializerState);
        snapshot.SetCharacterInterpolatedDatasprinting(chunkDataCharacterInterpolatedData[ent].sprinting, serializerState);
        snapshot.SetCharacterInterpolatedDatasprintWeight(chunkDataCharacterInterpolatedData[ent].sprintWeight, serializerState);
        snapshot.SetCharacterInterpolatedDatacrouchWeight(chunkDataCharacterInterpolatedData[ent].crouchWeight, serializerState);
        snapshot.SetCharacterInterpolatedDataselectorTargetSource(chunkDataCharacterInterpolatedData[ent].selectorTargetSource, serializerState);
        snapshot.SetCharacterInterpolatedDatamoveAngleLocal(chunkDataCharacterInterpolatedData[ent].moveAngleLocal, serializerState);
        snapshot.SetCharacterInterpolatedDatashootPoseWeight(chunkDataCharacterInterpolatedData[ent].shootPoseWeight, serializerState);
        snapshot.SetCharacterInterpolatedDatalocomotionVector(chunkDataCharacterInterpolatedData[ent].locomotionVector, serializerState);
        snapshot.SetCharacterInterpolatedDatalocomotionPhase(chunkDataCharacterInterpolatedData[ent].locomotionPhase, serializerState);
        snapshot.SetCharacterInterpolatedDatabanking(chunkDataCharacterInterpolatedData[ent].banking, serializerState);
        snapshot.SetCharacterInterpolatedDatalandAnticWeight(chunkDataCharacterInterpolatedData[ent].landAnticWeight, serializerState);
        snapshot.SetCharacterInterpolatedDataturnStartAngle(chunkDataCharacterInterpolatedData[ent].turnStartAngle, serializerState);
        snapshot.SetCharacterInterpolatedDataturnDirection(chunkDataCharacterInterpolatedData[ent].turnDirection, serializerState);
        snapshot.SetCharacterInterpolatedDatasquashTime(chunkDataCharacterInterpolatedData[ent].squashTime, serializerState);
        snapshot.SetCharacterInterpolatedDatasquashWeight(chunkDataCharacterInterpolatedData[ent].squashWeight, serializerState);
        snapshot.SetCharacterInterpolatedDatainAirTime(chunkDataCharacterInterpolatedData[ent].inAirTime, serializerState);
        snapshot.SetCharacterInterpolatedDatajumpTime(chunkDataCharacterInterpolatedData[ent].jumpTime, serializerState);
        snapshot.SetCharacterInterpolatedDatasimpleTime(chunkDataCharacterInterpolatedData[ent].simpleTime, serializerState);
        snapshot.SetCharacterInterpolatedDatafootIkOffset(chunkDataCharacterInterpolatedData[ent].footIkOffset, serializerState);
        snapshot.SetCharacterInterpolatedDatafootIkNormalLeft(chunkDataCharacterInterpolatedData[ent].footIkNormalLeft, serializerState);
        snapshot.SetCharacterInterpolatedDatafootIkNormalRight(chunkDataCharacterInterpolatedData[ent].footIkNormalRight, serializerState);
        snapshot.SetCharacterInterpolatedDatafootIkWeight(chunkDataCharacterInterpolatedData[ent].footIkWeight, serializerState);
        snapshot.SetCharacterInterpolatedDatablendOutAim(chunkDataCharacterInterpolatedData[ent].blendOutAim, serializerState);
        snapshot.SetCharacterPredictedDatatick(chunkDataCharacterPredictedData[ent].tick, serializerState);
        snapshot.SetCharacterPredictedDataposition(chunkDataCharacterPredictedData[ent].position, serializerState);
        snapshot.SetCharacterPredictedDatavelocity(chunkDataCharacterPredictedData[ent].velocity, serializerState);
        snapshot.SetCharacterPredictedDatasprinting(chunkDataCharacterPredictedData[ent].sprinting, serializerState);
        snapshot.SetCharacterPredictedDatacameraProfile(chunkDataCharacterPredictedData[ent].cameraProfile, serializerState);
        snapshot.SetCharacterPredictedDatadamageTick(chunkDataCharacterPredictedData[ent].damageTick, serializerState);
        snapshot.SetCharacterPredictedDatadamageDirection(chunkDataCharacterPredictedData[ent].damageDirection, serializerState);
        snapshot.SetCharacterReplicatedDataheroTypeIndex(chunkDataCharacterReplicatedData[ent].heroTypeIndex, serializerState);
        snapshot.SetCharacterControllerGroundSupportDataSurfaceNormal(chunkDataCharacterControllerGroundSupportData[ent].SurfaceNormal, serializerState);
        snapshot.SetCharacterControllerGroundSupportDataSurfaceVelocity(chunkDataCharacterControllerGroundSupportData[ent].SurfaceVelocity, serializerState);
        snapshot.SetCharacterControllerGroundSupportDataSupportedState(chunkDataCharacterControllerGroundSupportData[ent].SupportedState, serializerState);
        snapshot.SetCharacterControllerMoveResultMoveResult(chunkDataCharacterControllerMoveResult[ent].MoveResult, serializerState);
        snapshot.SetCharacterControllerVelocityVelocity(chunkDataCharacterControllerVelocity[ent].Velocity, serializerState);
        snapshot.SetHealthStateDatahealth(chunkDataHealthStateData[ent].health, serializerState);
        snapshot.SetInventoryStateactiveSlot(chunkDataInventoryState[ent].activeSlot, serializerState);
        snapshot.SetPlayerOwnerPlayerIdValue(chunkDataPlayerOwnerPlayerId[ent].Value, serializerState);
        snapshot.SetPlayerControlledStateresetCommandTick(chunkDataPlayerControlledState[ent].resetCommandTick, serializerState);
        snapshot.SetPlayerControlledStateresetCommandLookYaw(chunkDataPlayerControlledState[ent].resetCommandLookYaw, serializerState);
        snapshot.SetPlayerControlledStateresetCommandLookPitch(chunkDataPlayerControlledState[ent].resetCommandLookPitch, serializerState);
        snapshot.SetChild0AbilityAbilityControlbehaviorState(ghostChild0AbilityAbilityControlType[chunkDataLinkedEntityGroup[ent][1].Value].behaviorState, serializerState);
        snapshot.SetChild0AbilityAbilityControlrequestDeactivate(ghostChild0AbilityAbilityControlType[chunkDataLinkedEntityGroup[ent][1].Value].requestDeactivate, serializerState);
        snapshot.SetChild0AbilityMovementInterpolatedStatecharLocoState(ghostChild0AbilityMovementInterpolatedStateType[chunkDataLinkedEntityGroup[ent][1].Value].charLocoState, serializerState);
        snapshot.SetChild0AbilityMovementInterpolatedStatecharLocoTick(ghostChild0AbilityMovementInterpolatedStateType[chunkDataLinkedEntityGroup[ent][1].Value].charLocoTick, serializerState);
        snapshot.SetChild0AbilityMovementInterpolatedStatecrouching(ghostChild0AbilityMovementInterpolatedStateType[chunkDataLinkedEntityGroup[ent][1].Value].crouching, serializerState);
        snapshot.SetChild0AbilityMovementPredictedStatelocoState(ghostChild0AbilityMovementPredictedStateType[chunkDataLinkedEntityGroup[ent][1].Value].locoState, serializerState);
        snapshot.SetChild0AbilityMovementPredictedStatelocoStartTick(ghostChild0AbilityMovementPredictedStateType[chunkDataLinkedEntityGroup[ent][1].Value].locoStartTick, serializerState);
        snapshot.SetChild0AbilityMovementPredictedStatejumpCount(ghostChild0AbilityMovementPredictedStateType[chunkDataLinkedEntityGroup[ent][1].Value].jumpCount, serializerState);
        snapshot.SetChild0AbilityMovementPredictedStatecrouching(ghostChild0AbilityMovementPredictedStateType[chunkDataLinkedEntityGroup[ent][1].Value].crouching, serializerState);
        snapshot.SetChild1AbilityAbilityControlbehaviorState(ghostChild1AbilityAbilityControlType[chunkDataLinkedEntityGroup[ent][2].Value].behaviorState, serializerState);
        snapshot.SetChild1AbilityAbilityControlrequestDeactivate(ghostChild1AbilityAbilityControlType[chunkDataLinkedEntityGroup[ent][2].Value].requestDeactivate, serializerState);
        snapshot.SetChild1AbilitySprintPredictedStateactive(ghostChild1AbilitySprintPredictedStateType[chunkDataLinkedEntityGroup[ent][2].Value].active, serializerState);
        snapshot.SetChild1AbilitySprintPredictedStateterminating(ghostChild1AbilitySprintPredictedStateType[chunkDataLinkedEntityGroup[ent][2].Value].terminating, serializerState);
        snapshot.SetChild1AbilitySprintPredictedStateterminateStartTick(ghostChild1AbilitySprintPredictedStateType[chunkDataLinkedEntityGroup[ent][2].Value].terminateStartTick, serializerState);
        snapshot.SetChild2AbilityAbilityControlbehaviorState(ghostChild2AbilityAbilityControlType[chunkDataLinkedEntityGroup[ent][3].Value].behaviorState, serializerState);
        snapshot.SetChild2AbilityAbilityControlrequestDeactivate(ghostChild2AbilityAbilityControlType[chunkDataLinkedEntityGroup[ent][3].Value].requestDeactivate, serializerState);
        snapshot.SetChild3AbilityAbilityControlbehaviorState(ghostChild3AbilityAbilityControlType[chunkDataLinkedEntityGroup[ent][4].Value].behaviorState, serializerState);
        snapshot.SetChild3AbilityAbilityControlrequestDeactivate(ghostChild3AbilityAbilityControlType[chunkDataLinkedEntityGroup[ent][4].Value].requestDeactivate, serializerState);
    }
}
