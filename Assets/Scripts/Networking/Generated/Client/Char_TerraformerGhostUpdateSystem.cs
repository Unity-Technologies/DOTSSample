using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Networking.Transport.Utilities;
using Unity.NetCode;
using Unity.Entities;

[UpdateInGroup(typeof(GhostUpdateSystemGroup))]
public class Char_TerraformerGhostUpdateSystem : JobComponentSystem
{
    [BurstCompile]
    struct UpdateInterpolatedJob : IJobChunk
    {
        [ReadOnly] public NativeHashMap<int, GhostEntity> GhostMap;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [NativeDisableContainerSafetyRestriction] public NativeArray<uint> minMaxSnapshotTick;
#pragma warning disable 649
        [NativeSetThreadIndex]
        public int ThreadIndex;
#pragma warning restore 649
#endif
        [ReadOnly] public ArchetypeChunkBufferType<Char_TerraformerSnapshotData> ghostSnapshotDataType;
        [ReadOnly] public ArchetypeChunkEntityType ghostEntityType;
        public ArchetypeChunkComponentType<Character.InterpolatedData> ghostCharacterInterpolatedDataType;
        public ArchetypeChunkComponentType<Character.ReplicatedData> ghostCharacterReplicatedDataType;
        public ArchetypeChunkComponentType<CharacterControllerGroundSupportData> ghostCharacterControllerGroundSupportDataType;
        public ArchetypeChunkComponentType<CharacterControllerMoveResult> ghostCharacterControllerMoveResultType;
        public ArchetypeChunkComponentType<CharacterControllerVelocity> ghostCharacterControllerVelocityType;
        public ArchetypeChunkComponentType<HealthStateData> ghostHealthStateDataType;
        public ArchetypeChunkComponentType<Inventory.State> ghostInventoryStateType;
        public ArchetypeChunkComponentType<Player.OwnerPlayerId> ghostPlayerOwnerPlayerIdType;
        public ArchetypeChunkComponentType<PlayerControlled.State> ghostPlayerControlledStateType;
        [ReadOnly] public ArchetypeChunkBufferType<LinkedEntityGroup> ghostLinkedEntityGroupType;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Ability.AbilityControl> ghostAbilityAbilityControlFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AbilityMovement.InterpolatedState> ghostAbilityMovementInterpolatedStateFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AbilityMovement.PredictedState> ghostAbilityMovementPredictedStateFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AbilitySprint.PredictedState> ghostAbilitySprintPredictedStateFromEntity;

        public uint targetTick;
        public float targetTickFraction;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var deserializerState = new GhostDeserializerState
            {
                GhostMap = GhostMap
            };
            var ghostEntityArray = chunk.GetNativeArray(ghostEntityType);
            var ghostSnapshotDataArray = chunk.GetBufferAccessor(ghostSnapshotDataType);
            var ghostCharacterInterpolatedDataArray = chunk.GetNativeArray(ghostCharacterInterpolatedDataType);
            var ghostCharacterReplicatedDataArray = chunk.GetNativeArray(ghostCharacterReplicatedDataType);
            var ghostCharacterControllerGroundSupportDataArray = chunk.GetNativeArray(ghostCharacterControllerGroundSupportDataType);
            var ghostCharacterControllerMoveResultArray = chunk.GetNativeArray(ghostCharacterControllerMoveResultType);
            var ghostCharacterControllerVelocityArray = chunk.GetNativeArray(ghostCharacterControllerVelocityType);
            var ghostHealthStateDataArray = chunk.GetNativeArray(ghostHealthStateDataType);
            var ghostInventoryStateArray = chunk.GetNativeArray(ghostInventoryStateType);
            var ghostPlayerOwnerPlayerIdArray = chunk.GetNativeArray(ghostPlayerOwnerPlayerIdType);
            var ghostPlayerControlledStateArray = chunk.GetNativeArray(ghostPlayerControlledStateType);
            var ghostLinkedEntityGroupArray = chunk.GetBufferAccessor(ghostLinkedEntityGroupType);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var minMaxOffset = ThreadIndex * (JobsUtility.CacheLineSize/4);
#endif
            for (int entityIndex = 0; entityIndex < ghostEntityArray.Length; ++entityIndex)
            {
                var snapshot = ghostSnapshotDataArray[entityIndex];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var latestTick = snapshot.GetLatestTick();
                if (latestTick != 0)
                {
                    if (minMaxSnapshotTick[minMaxOffset] == 0 || SequenceHelpers.IsNewer(minMaxSnapshotTick[minMaxOffset], latestTick))
                        minMaxSnapshotTick[minMaxOffset] = latestTick;
                    if (minMaxSnapshotTick[minMaxOffset + 1] == 0 || SequenceHelpers.IsNewer(latestTick, minMaxSnapshotTick[minMaxOffset + 1]))
                        minMaxSnapshotTick[minMaxOffset + 1] = latestTick;
                }
#endif
                Char_TerraformerSnapshotData snapshotData;
                snapshot.GetDataAtTick(targetTick, targetTickFraction, out snapshotData);

                var ghostCharacterInterpolatedData = ghostCharacterInterpolatedDataArray[entityIndex];
                var ghostCharacterReplicatedData = ghostCharacterReplicatedDataArray[entityIndex];
                var ghostCharacterControllerGroundSupportData = ghostCharacterControllerGroundSupportDataArray[entityIndex];
                var ghostCharacterControllerMoveResult = ghostCharacterControllerMoveResultArray[entityIndex];
                var ghostCharacterControllerVelocity = ghostCharacterControllerVelocityArray[entityIndex];
                var ghostHealthStateData = ghostHealthStateDataArray[entityIndex];
                var ghostInventoryState = ghostInventoryStateArray[entityIndex];
                var ghostPlayerOwnerPlayerId = ghostPlayerOwnerPlayerIdArray[entityIndex];
                var ghostPlayerControlledState = ghostPlayerControlledStateArray[entityIndex];
                var ghostChild0AbilityAbilityControl = ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild0AbilityMovementInterpolatedState = ghostAbilityMovementInterpolatedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild0AbilityMovementPredictedState = ghostAbilityMovementPredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild1AbilityAbilityControl = ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][2].Value];
                var ghostChild1AbilitySprintPredictedState = ghostAbilitySprintPredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][2].Value];
                var ghostChild2AbilityAbilityControl = ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][3].Value];
                var ghostChild3AbilityAbilityControl = ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][4].Value];
                ghostCharacterInterpolatedData.Position = snapshotData.GetCharacterInterpolatedDataPosition(deserializerState);
                ghostCharacterInterpolatedData.rotation = snapshotData.GetCharacterInterpolatedDatarotation(deserializerState);
                ghostCharacterInterpolatedData.aimYaw = snapshotData.GetCharacterInterpolatedDataaimYaw(deserializerState);
                ghostCharacterInterpolatedData.aimPitch = snapshotData.GetCharacterInterpolatedDataaimPitch(deserializerState);
                ghostCharacterInterpolatedData.moveYaw = snapshotData.GetCharacterInterpolatedDatamoveYaw(deserializerState);
                ghostCharacterInterpolatedData.charAction = snapshotData.GetCharacterInterpolatedDatacharAction(deserializerState);
                ghostCharacterInterpolatedData.charActionTick = snapshotData.GetCharacterInterpolatedDatacharActionTick(deserializerState);
                ghostCharacterInterpolatedData.damageTick = snapshotData.GetCharacterInterpolatedDatadamageTick(deserializerState);
                ghostCharacterInterpolatedData.damageDirection = snapshotData.GetCharacterInterpolatedDatadamageDirection(deserializerState);
                ghostCharacterInterpolatedData.sprinting = snapshotData.GetCharacterInterpolatedDatasprinting(deserializerState);
                ghostCharacterInterpolatedData.sprintWeight = snapshotData.GetCharacterInterpolatedDatasprintWeight(deserializerState);
                ghostCharacterInterpolatedData.crouchWeight = snapshotData.GetCharacterInterpolatedDatacrouchWeight(deserializerState);
                ghostCharacterInterpolatedData.selectorTargetSource = snapshotData.GetCharacterInterpolatedDataselectorTargetSource(deserializerState);
                ghostCharacterInterpolatedData.moveAngleLocal = snapshotData.GetCharacterInterpolatedDatamoveAngleLocal(deserializerState);
                ghostCharacterInterpolatedData.shootPoseWeight = snapshotData.GetCharacterInterpolatedDatashootPoseWeight(deserializerState);
                ghostCharacterInterpolatedData.locomotionVector = snapshotData.GetCharacterInterpolatedDatalocomotionVector(deserializerState);
                ghostCharacterInterpolatedData.locomotionPhase = snapshotData.GetCharacterInterpolatedDatalocomotionPhase(deserializerState);
                ghostCharacterInterpolatedData.banking = snapshotData.GetCharacterInterpolatedDatabanking(deserializerState);
                ghostCharacterInterpolatedData.landAnticWeight = snapshotData.GetCharacterInterpolatedDatalandAnticWeight(deserializerState);
                ghostCharacterInterpolatedData.turnStartAngle = snapshotData.GetCharacterInterpolatedDataturnStartAngle(deserializerState);
                ghostCharacterInterpolatedData.turnDirection = snapshotData.GetCharacterInterpolatedDataturnDirection(deserializerState);
                ghostCharacterInterpolatedData.squashTime = snapshotData.GetCharacterInterpolatedDatasquashTime(deserializerState);
                ghostCharacterInterpolatedData.squashWeight = snapshotData.GetCharacterInterpolatedDatasquashWeight(deserializerState);
                ghostCharacterInterpolatedData.inAirTime = snapshotData.GetCharacterInterpolatedDatainAirTime(deserializerState);
                ghostCharacterInterpolatedData.jumpTime = snapshotData.GetCharacterInterpolatedDatajumpTime(deserializerState);
                ghostCharacterInterpolatedData.simpleTime = snapshotData.GetCharacterInterpolatedDatasimpleTime(deserializerState);
                ghostCharacterInterpolatedData.footIkOffset = snapshotData.GetCharacterInterpolatedDatafootIkOffset(deserializerState);
                ghostCharacterInterpolatedData.footIkNormalLeft = snapshotData.GetCharacterInterpolatedDatafootIkNormalLeft(deserializerState);
                ghostCharacterInterpolatedData.footIkNormalRight = snapshotData.GetCharacterInterpolatedDatafootIkNormalRight(deserializerState);
                ghostCharacterInterpolatedData.footIkWeight = snapshotData.GetCharacterInterpolatedDatafootIkWeight(deserializerState);
                ghostCharacterInterpolatedData.blendOutAim = snapshotData.GetCharacterInterpolatedDatablendOutAim(deserializerState);
                ghostCharacterReplicatedData.heroTypeIndex = snapshotData.GetCharacterReplicatedDataheroTypeIndex(deserializerState);
                ghostCharacterControllerGroundSupportData.SurfaceNormal = snapshotData.GetCharacterControllerGroundSupportDataSurfaceNormal(deserializerState);
                ghostCharacterControllerGroundSupportData.SurfaceVelocity = snapshotData.GetCharacterControllerGroundSupportDataSurfaceVelocity(deserializerState);
                ghostCharacterControllerGroundSupportData.SupportedState = snapshotData.GetCharacterControllerGroundSupportDataSupportedState(deserializerState);
                ghostCharacterControllerMoveResult.MoveResult = snapshotData.GetCharacterControllerMoveResultMoveResult(deserializerState);
                ghostCharacterControllerVelocity.Velocity = snapshotData.GetCharacterControllerVelocityVelocity(deserializerState);
                ghostHealthStateData.health = snapshotData.GetHealthStateDatahealth(deserializerState);
                ghostInventoryState.activeSlot = snapshotData.GetInventoryStateactiveSlot(deserializerState);
                ghostPlayerOwnerPlayerId.Value = snapshotData.GetPlayerOwnerPlayerIdValue(deserializerState);
                ghostPlayerControlledState.resetCommandTick = snapshotData.GetPlayerControlledStateresetCommandTick(deserializerState);
                ghostPlayerControlledState.resetCommandLookYaw = snapshotData.GetPlayerControlledStateresetCommandLookYaw(deserializerState);
                ghostPlayerControlledState.resetCommandLookPitch = snapshotData.GetPlayerControlledStateresetCommandLookPitch(deserializerState);
                ghostChild0AbilityAbilityControl.behaviorState = snapshotData.GetChild0AbilityAbilityControlbehaviorState(deserializerState);
                ghostChild0AbilityAbilityControl.requestDeactivate = snapshotData.GetChild0AbilityAbilityControlrequestDeactivate(deserializerState);
                ghostChild0AbilityMovementInterpolatedState.charLocoState = snapshotData.GetChild0AbilityMovementInterpolatedStatecharLocoState(deserializerState);
                ghostChild0AbilityMovementInterpolatedState.charLocoTick = snapshotData.GetChild0AbilityMovementInterpolatedStatecharLocoTick(deserializerState);
                ghostChild0AbilityMovementInterpolatedState.crouching = snapshotData.GetChild0AbilityMovementInterpolatedStatecrouching(deserializerState);
                ghostChild0AbilityMovementPredictedState.locoState = snapshotData.GetChild0AbilityMovementPredictedStatelocoState(deserializerState);
                ghostChild0AbilityMovementPredictedState.locoStartTick = snapshotData.GetChild0AbilityMovementPredictedStatelocoStartTick(deserializerState);
                ghostChild0AbilityMovementPredictedState.jumpCount = snapshotData.GetChild0AbilityMovementPredictedStatejumpCount(deserializerState);
                ghostChild0AbilityMovementPredictedState.crouching = snapshotData.GetChild0AbilityMovementPredictedStatecrouching(deserializerState);
                ghostChild1AbilityAbilityControl.behaviorState = snapshotData.GetChild1AbilityAbilityControlbehaviorState(deserializerState);
                ghostChild1AbilityAbilityControl.requestDeactivate = snapshotData.GetChild1AbilityAbilityControlrequestDeactivate(deserializerState);
                ghostChild1AbilitySprintPredictedState.active = snapshotData.GetChild1AbilitySprintPredictedStateactive(deserializerState);
                ghostChild1AbilitySprintPredictedState.terminating = snapshotData.GetChild1AbilitySprintPredictedStateterminating(deserializerState);
                ghostChild1AbilitySprintPredictedState.terminateStartTick = snapshotData.GetChild1AbilitySprintPredictedStateterminateStartTick(deserializerState);
                ghostChild2AbilityAbilityControl.behaviorState = snapshotData.GetChild2AbilityAbilityControlbehaviorState(deserializerState);
                ghostChild2AbilityAbilityControl.requestDeactivate = snapshotData.GetChild2AbilityAbilityControlrequestDeactivate(deserializerState);
                ghostChild3AbilityAbilityControl.behaviorState = snapshotData.GetChild3AbilityAbilityControlbehaviorState(deserializerState);
                ghostChild3AbilityAbilityControl.requestDeactivate = snapshotData.GetChild3AbilityAbilityControlrequestDeactivate(deserializerState);
                ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityAbilityControl;
                ghostAbilityMovementInterpolatedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityMovementInterpolatedState;
                ghostAbilityMovementPredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityMovementPredictedState;
                ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][2].Value] = ghostChild1AbilityAbilityControl;
                ghostAbilitySprintPredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][2].Value] = ghostChild1AbilitySprintPredictedState;
                ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][3].Value] = ghostChild2AbilityAbilityControl;
                ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][4].Value] = ghostChild3AbilityAbilityControl;
                ghostCharacterInterpolatedDataArray[entityIndex] = ghostCharacterInterpolatedData;
                ghostCharacterReplicatedDataArray[entityIndex] = ghostCharacterReplicatedData;
                ghostCharacterControllerGroundSupportDataArray[entityIndex] = ghostCharacterControllerGroundSupportData;
                ghostCharacterControllerMoveResultArray[entityIndex] = ghostCharacterControllerMoveResult;
                ghostCharacterControllerVelocityArray[entityIndex] = ghostCharacterControllerVelocity;
                ghostHealthStateDataArray[entityIndex] = ghostHealthStateData;
                ghostInventoryStateArray[entityIndex] = ghostInventoryState;
                ghostPlayerOwnerPlayerIdArray[entityIndex] = ghostPlayerOwnerPlayerId;
                ghostPlayerControlledStateArray[entityIndex] = ghostPlayerControlledState;
            }
        }
    }
    [BurstCompile]
    struct UpdatePredictedJob : IJobChunk
    {
        [ReadOnly] public NativeHashMap<int, GhostEntity> GhostMap;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [NativeDisableContainerSafetyRestriction] public NativeArray<uint> minMaxSnapshotTick;
#endif
#pragma warning disable 649
        [NativeSetThreadIndex]
        public int ThreadIndex;
#pragma warning restore 649
        [NativeDisableParallelForRestriction] public NativeArray<uint> minPredictedTick;
        [ReadOnly] public ArchetypeChunkBufferType<Char_TerraformerSnapshotData> ghostSnapshotDataType;
        [ReadOnly] public ArchetypeChunkEntityType ghostEntityType;
        public ArchetypeChunkComponentType<PredictedGhostComponent> predictedGhostComponentType;
        public ArchetypeChunkComponentType<Character.PredictedData> ghostCharacterPredictedDataType;
        public ArchetypeChunkComponentType<Character.ReplicatedData> ghostCharacterReplicatedDataType;
        public ArchetypeChunkComponentType<CharacterControllerGroundSupportData> ghostCharacterControllerGroundSupportDataType;
        public ArchetypeChunkComponentType<CharacterControllerMoveResult> ghostCharacterControllerMoveResultType;
        public ArchetypeChunkComponentType<CharacterControllerVelocity> ghostCharacterControllerVelocityType;
        public ArchetypeChunkComponentType<HealthStateData> ghostHealthStateDataType;
        public ArchetypeChunkComponentType<Inventory.State> ghostInventoryStateType;
        public ArchetypeChunkComponentType<Player.OwnerPlayerId> ghostPlayerOwnerPlayerIdType;
        public ArchetypeChunkComponentType<PlayerControlled.State> ghostPlayerControlledStateType;
        [ReadOnly] public ArchetypeChunkBufferType<LinkedEntityGroup> ghostLinkedEntityGroupType;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Ability.AbilityControl> ghostAbilityAbilityControlFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AbilityMovement.InterpolatedState> ghostAbilityMovementInterpolatedStateFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AbilityMovement.PredictedState> ghostAbilityMovementPredictedStateFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AbilitySprint.PredictedState> ghostAbilitySprintPredictedStateFromEntity;
        public uint targetTick;
        public uint lastPredictedTick;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var deserializerState = new GhostDeserializerState
            {
                GhostMap = GhostMap
            };
            var ghostEntityArray = chunk.GetNativeArray(ghostEntityType);
            var ghostSnapshotDataArray = chunk.GetBufferAccessor(ghostSnapshotDataType);
            var predictedGhostComponentArray = chunk.GetNativeArray(predictedGhostComponentType);
            var ghostCharacterPredictedDataArray = chunk.GetNativeArray(ghostCharacterPredictedDataType);
            var ghostCharacterReplicatedDataArray = chunk.GetNativeArray(ghostCharacterReplicatedDataType);
            var ghostCharacterControllerGroundSupportDataArray = chunk.GetNativeArray(ghostCharacterControllerGroundSupportDataType);
            var ghostCharacterControllerMoveResultArray = chunk.GetNativeArray(ghostCharacterControllerMoveResultType);
            var ghostCharacterControllerVelocityArray = chunk.GetNativeArray(ghostCharacterControllerVelocityType);
            var ghostHealthStateDataArray = chunk.GetNativeArray(ghostHealthStateDataType);
            var ghostInventoryStateArray = chunk.GetNativeArray(ghostInventoryStateType);
            var ghostPlayerOwnerPlayerIdArray = chunk.GetNativeArray(ghostPlayerOwnerPlayerIdType);
            var ghostPlayerControlledStateArray = chunk.GetNativeArray(ghostPlayerControlledStateType);
            var ghostLinkedEntityGroupArray = chunk.GetBufferAccessor(ghostLinkedEntityGroupType);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var minMaxOffset = ThreadIndex * (JobsUtility.CacheLineSize/4);
#endif
            for (int entityIndex = 0; entityIndex < ghostEntityArray.Length; ++entityIndex)
            {
                var snapshot = ghostSnapshotDataArray[entityIndex];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var latestTick = snapshot.GetLatestTick();
                if (latestTick != 0)
                {
                    if (minMaxSnapshotTick[minMaxOffset] == 0 || SequenceHelpers.IsNewer(minMaxSnapshotTick[minMaxOffset], latestTick))
                        minMaxSnapshotTick[minMaxOffset] = latestTick;
                    if (minMaxSnapshotTick[minMaxOffset + 1] == 0 || SequenceHelpers.IsNewer(latestTick, minMaxSnapshotTick[minMaxOffset + 1]))
                        minMaxSnapshotTick[minMaxOffset + 1] = latestTick;
                }
#endif
                Char_TerraformerSnapshotData snapshotData;
                snapshot.GetDataAtTick(targetTick, out snapshotData);

                var predictedData = predictedGhostComponentArray[entityIndex];
                var lastPredictedTickInst = lastPredictedTick;
                if (lastPredictedTickInst == 0 || predictedData.AppliedTick != snapshotData.Tick)
                    lastPredictedTickInst = snapshotData.Tick;
                else if (!SequenceHelpers.IsNewer(lastPredictedTickInst, snapshotData.Tick))
                    lastPredictedTickInst = snapshotData.Tick;
                if (minPredictedTick[ThreadIndex] == 0 || SequenceHelpers.IsNewer(minPredictedTick[ThreadIndex], lastPredictedTickInst))
                    minPredictedTick[ThreadIndex] = lastPredictedTickInst;
                predictedGhostComponentArray[entityIndex] = new PredictedGhostComponent{AppliedTick = snapshotData.Tick, PredictionStartTick = lastPredictedTickInst};
                if (lastPredictedTickInst != snapshotData.Tick)
                    continue;

                var ghostCharacterPredictedData = ghostCharacterPredictedDataArray[entityIndex];
                var ghostCharacterReplicatedData = ghostCharacterReplicatedDataArray[entityIndex];
                var ghostCharacterControllerGroundSupportData = ghostCharacterControllerGroundSupportDataArray[entityIndex];
                var ghostCharacterControllerMoveResult = ghostCharacterControllerMoveResultArray[entityIndex];
                var ghostCharacterControllerVelocity = ghostCharacterControllerVelocityArray[entityIndex];
                var ghostHealthStateData = ghostHealthStateDataArray[entityIndex];
                var ghostInventoryState = ghostInventoryStateArray[entityIndex];
                var ghostPlayerOwnerPlayerId = ghostPlayerOwnerPlayerIdArray[entityIndex];
                var ghostPlayerControlledState = ghostPlayerControlledStateArray[entityIndex];
                var ghostChild0AbilityAbilityControl = ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild0AbilityMovementInterpolatedState = ghostAbilityMovementInterpolatedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild0AbilityMovementPredictedState = ghostAbilityMovementPredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild1AbilityAbilityControl = ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][2].Value];
                var ghostChild1AbilitySprintPredictedState = ghostAbilitySprintPredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][2].Value];
                var ghostChild2AbilityAbilityControl = ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][3].Value];
                var ghostChild3AbilityAbilityControl = ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][4].Value];
                ghostCharacterPredictedData.tick = snapshotData.GetCharacterPredictedDatatick(deserializerState);
                ghostCharacterPredictedData.position = snapshotData.GetCharacterPredictedDataposition(deserializerState);
                ghostCharacterPredictedData.velocity = snapshotData.GetCharacterPredictedDatavelocity(deserializerState);
                ghostCharacterPredictedData.sprinting = snapshotData.GetCharacterPredictedDatasprinting(deserializerState);
                ghostCharacterPredictedData.cameraProfile = snapshotData.GetCharacterPredictedDatacameraProfile(deserializerState);
                ghostCharacterPredictedData.damageTick = snapshotData.GetCharacterPredictedDatadamageTick(deserializerState);
                ghostCharacterPredictedData.damageDirection = snapshotData.GetCharacterPredictedDatadamageDirection(deserializerState);
                ghostCharacterReplicatedData.heroTypeIndex = snapshotData.GetCharacterReplicatedDataheroTypeIndex(deserializerState);
                ghostCharacterControllerGroundSupportData.SurfaceNormal = snapshotData.GetCharacterControllerGroundSupportDataSurfaceNormal(deserializerState);
                ghostCharacterControllerGroundSupportData.SurfaceVelocity = snapshotData.GetCharacterControllerGroundSupportDataSurfaceVelocity(deserializerState);
                ghostCharacterControllerGroundSupportData.SupportedState = snapshotData.GetCharacterControllerGroundSupportDataSupportedState(deserializerState);
                ghostCharacterControllerMoveResult.MoveResult = snapshotData.GetCharacterControllerMoveResultMoveResult(deserializerState);
                ghostCharacterControllerVelocity.Velocity = snapshotData.GetCharacterControllerVelocityVelocity(deserializerState);
                ghostHealthStateData.health = snapshotData.GetHealthStateDatahealth(deserializerState);
                ghostInventoryState.activeSlot = snapshotData.GetInventoryStateactiveSlot(deserializerState);
                ghostPlayerOwnerPlayerId.Value = snapshotData.GetPlayerOwnerPlayerIdValue(deserializerState);
                ghostPlayerControlledState.resetCommandTick = snapshotData.GetPlayerControlledStateresetCommandTick(deserializerState);
                ghostPlayerControlledState.resetCommandLookYaw = snapshotData.GetPlayerControlledStateresetCommandLookYaw(deserializerState);
                ghostPlayerControlledState.resetCommandLookPitch = snapshotData.GetPlayerControlledStateresetCommandLookPitch(deserializerState);
                ghostChild0AbilityAbilityControl.behaviorState = snapshotData.GetChild0AbilityAbilityControlbehaviorState(deserializerState);
                ghostChild0AbilityAbilityControl.requestDeactivate = snapshotData.GetChild0AbilityAbilityControlrequestDeactivate(deserializerState);
                ghostChild0AbilityMovementInterpolatedState.charLocoState = snapshotData.GetChild0AbilityMovementInterpolatedStatecharLocoState(deserializerState);
                ghostChild0AbilityMovementInterpolatedState.charLocoTick = snapshotData.GetChild0AbilityMovementInterpolatedStatecharLocoTick(deserializerState);
                ghostChild0AbilityMovementInterpolatedState.crouching = snapshotData.GetChild0AbilityMovementInterpolatedStatecrouching(deserializerState);
                ghostChild0AbilityMovementPredictedState.locoState = snapshotData.GetChild0AbilityMovementPredictedStatelocoState(deserializerState);
                ghostChild0AbilityMovementPredictedState.locoStartTick = snapshotData.GetChild0AbilityMovementPredictedStatelocoStartTick(deserializerState);
                ghostChild0AbilityMovementPredictedState.jumpCount = snapshotData.GetChild0AbilityMovementPredictedStatejumpCount(deserializerState);
                ghostChild0AbilityMovementPredictedState.crouching = snapshotData.GetChild0AbilityMovementPredictedStatecrouching(deserializerState);
                ghostChild1AbilityAbilityControl.behaviorState = snapshotData.GetChild1AbilityAbilityControlbehaviorState(deserializerState);
                ghostChild1AbilityAbilityControl.requestDeactivate = snapshotData.GetChild1AbilityAbilityControlrequestDeactivate(deserializerState);
                ghostChild1AbilitySprintPredictedState.active = snapshotData.GetChild1AbilitySprintPredictedStateactive(deserializerState);
                ghostChild1AbilitySprintPredictedState.terminating = snapshotData.GetChild1AbilitySprintPredictedStateterminating(deserializerState);
                ghostChild1AbilitySprintPredictedState.terminateStartTick = snapshotData.GetChild1AbilitySprintPredictedStateterminateStartTick(deserializerState);
                ghostChild2AbilityAbilityControl.behaviorState = snapshotData.GetChild2AbilityAbilityControlbehaviorState(deserializerState);
                ghostChild2AbilityAbilityControl.requestDeactivate = snapshotData.GetChild2AbilityAbilityControlrequestDeactivate(deserializerState);
                ghostChild3AbilityAbilityControl.behaviorState = snapshotData.GetChild3AbilityAbilityControlbehaviorState(deserializerState);
                ghostChild3AbilityAbilityControl.requestDeactivate = snapshotData.GetChild3AbilityAbilityControlrequestDeactivate(deserializerState);
                ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityAbilityControl;
                ghostAbilityMovementInterpolatedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityMovementInterpolatedState;
                ghostAbilityMovementPredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityMovementPredictedState;
                ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][2].Value] = ghostChild1AbilityAbilityControl;
                ghostAbilitySprintPredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][2].Value] = ghostChild1AbilitySprintPredictedState;
                ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][3].Value] = ghostChild2AbilityAbilityControl;
                ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][4].Value] = ghostChild3AbilityAbilityControl;
                ghostCharacterPredictedDataArray[entityIndex] = ghostCharacterPredictedData;
                ghostCharacterReplicatedDataArray[entityIndex] = ghostCharacterReplicatedData;
                ghostCharacterControllerGroundSupportDataArray[entityIndex] = ghostCharacterControllerGroundSupportData;
                ghostCharacterControllerMoveResultArray[entityIndex] = ghostCharacterControllerMoveResult;
                ghostCharacterControllerVelocityArray[entityIndex] = ghostCharacterControllerVelocity;
                ghostHealthStateDataArray[entityIndex] = ghostHealthStateData;
                ghostInventoryStateArray[entityIndex] = ghostInventoryState;
                ghostPlayerOwnerPlayerIdArray[entityIndex] = ghostPlayerOwnerPlayerId;
                ghostPlayerControlledStateArray[entityIndex] = ghostPlayerControlledState;
            }
        }
    }
    private ClientSimulationSystemGroup m_ClientSimulationSystemGroup;
    private GhostPredictionSystemGroup m_GhostPredictionSystemGroup;
    private EntityQuery m_interpolatedQuery;
    private EntityQuery m_predictedQuery;
    private NativeHashMap<int, GhostEntity> m_ghostEntityMap;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private NativeArray<uint> m_ghostMinMaxSnapshotTick;
#endif
    private GhostUpdateSystemGroup m_GhostUpdateSystemGroup;
    private uint m_LastPredictedTick;
    protected override void OnCreate()
    {
        m_GhostUpdateSystemGroup = World.GetOrCreateSystem<GhostUpdateSystemGroup>();
        m_ghostEntityMap = m_GhostUpdateSystemGroup.GhostEntityMap;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        m_ghostMinMaxSnapshotTick = m_GhostUpdateSystemGroup.GhostSnapshotTickMinMax;
#endif
        m_ClientSimulationSystemGroup = World.GetOrCreateSystem<ClientSimulationSystemGroup>();
        m_GhostPredictionSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
        m_interpolatedQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new []{
                ComponentType.ReadWrite<Char_TerraformerSnapshotData>(),
                ComponentType.ReadOnly<GhostComponent>(),
                ComponentType.ReadWrite<Character.InterpolatedData>(),
                ComponentType.ReadWrite<Character.ReplicatedData>(),
                ComponentType.ReadWrite<CharacterControllerGroundSupportData>(),
                ComponentType.ReadWrite<CharacterControllerMoveResult>(),
                ComponentType.ReadWrite<CharacterControllerVelocity>(),
                ComponentType.ReadWrite<HealthStateData>(),
                ComponentType.ReadWrite<Inventory.State>(),
                ComponentType.ReadWrite<Player.OwnerPlayerId>(),
                ComponentType.ReadWrite<PlayerControlled.State>(),
                ComponentType.ReadOnly<LinkedEntityGroup>(),
            },
            None = new []{ComponentType.ReadWrite<PredictedGhostComponent>()}
        });
        m_predictedQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new []{
                ComponentType.ReadOnly<Char_TerraformerSnapshotData>(),
                ComponentType.ReadOnly<GhostComponent>(),
                ComponentType.ReadOnly<PredictedGhostComponent>(),
                ComponentType.ReadWrite<Character.PredictedData>(),
                ComponentType.ReadWrite<Character.ReplicatedData>(),
                ComponentType.ReadWrite<CharacterControllerGroundSupportData>(),
                ComponentType.ReadWrite<CharacterControllerMoveResult>(),
                ComponentType.ReadWrite<CharacterControllerVelocity>(),
                ComponentType.ReadWrite<HealthStateData>(),
                ComponentType.ReadWrite<Inventory.State>(),
                ComponentType.ReadWrite<Player.OwnerPlayerId>(),
                ComponentType.ReadWrite<PlayerControlled.State>(),
                ComponentType.ReadOnly<LinkedEntityGroup>(),
            }
        });
        RequireForUpdate(GetEntityQuery(ComponentType.ReadWrite<Char_TerraformerSnapshotData>(),
            ComponentType.ReadOnly<GhostComponent>()));
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (!m_predictedQuery.IsEmptyIgnoreFilter)
        {
            var updatePredictedJob = new UpdatePredictedJob
            {
                GhostMap = m_ghostEntityMap,
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                minMaxSnapshotTick = m_ghostMinMaxSnapshotTick,
#endif
                minPredictedTick = m_GhostPredictionSystemGroup.OldestPredictedTick,
                ghostSnapshotDataType = GetArchetypeChunkBufferType<Char_TerraformerSnapshotData>(true),
                ghostEntityType = GetArchetypeChunkEntityType(),
                predictedGhostComponentType = GetArchetypeChunkComponentType<PredictedGhostComponent>(),
                ghostCharacterPredictedDataType = GetArchetypeChunkComponentType<Character.PredictedData>(),
                ghostCharacterReplicatedDataType = GetArchetypeChunkComponentType<Character.ReplicatedData>(),
                ghostCharacterControllerGroundSupportDataType = GetArchetypeChunkComponentType<CharacterControllerGroundSupportData>(),
                ghostCharacterControllerMoveResultType = GetArchetypeChunkComponentType<CharacterControllerMoveResult>(),
                ghostCharacterControllerVelocityType = GetArchetypeChunkComponentType<CharacterControllerVelocity>(),
                ghostHealthStateDataType = GetArchetypeChunkComponentType<HealthStateData>(),
                ghostInventoryStateType = GetArchetypeChunkComponentType<Inventory.State>(),
                ghostPlayerOwnerPlayerIdType = GetArchetypeChunkComponentType<Player.OwnerPlayerId>(),
                ghostPlayerControlledStateType = GetArchetypeChunkComponentType<PlayerControlled.State>(),
                ghostLinkedEntityGroupType = GetArchetypeChunkBufferType<LinkedEntityGroup>(true),
                ghostAbilityAbilityControlFromEntity = GetComponentDataFromEntity<Ability.AbilityControl>(),
                ghostAbilityMovementInterpolatedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.InterpolatedState>(),
                ghostAbilityMovementPredictedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.PredictedState>(),
                ghostAbilitySprintPredictedStateFromEntity = GetComponentDataFromEntity<AbilitySprint.PredictedState>(),

                targetTick = m_ClientSimulationSystemGroup.ServerTick,
                lastPredictedTick = m_LastPredictedTick
            };
            m_LastPredictedTick = m_ClientSimulationSystemGroup.ServerTick;
            if (m_ClientSimulationSystemGroup.ServerTickFraction < 1)
                m_LastPredictedTick = 0;
            inputDeps = updatePredictedJob.Schedule(m_predictedQuery, JobHandle.CombineDependencies(inputDeps, m_GhostUpdateSystemGroup.LastGhostMapWriter));
            m_GhostPredictionSystemGroup.AddPredictedTickWriter(inputDeps);
        }
        if (!m_interpolatedQuery.IsEmptyIgnoreFilter)
        {
            var updateInterpolatedJob = new UpdateInterpolatedJob
            {
                GhostMap = m_ghostEntityMap,
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                minMaxSnapshotTick = m_ghostMinMaxSnapshotTick,
#endif
                ghostSnapshotDataType = GetArchetypeChunkBufferType<Char_TerraformerSnapshotData>(true),
                ghostEntityType = GetArchetypeChunkEntityType(),
                ghostCharacterInterpolatedDataType = GetArchetypeChunkComponentType<Character.InterpolatedData>(),
                ghostCharacterReplicatedDataType = GetArchetypeChunkComponentType<Character.ReplicatedData>(),
                ghostCharacterControllerGroundSupportDataType = GetArchetypeChunkComponentType<CharacterControllerGroundSupportData>(),
                ghostCharacterControllerMoveResultType = GetArchetypeChunkComponentType<CharacterControllerMoveResult>(),
                ghostCharacterControllerVelocityType = GetArchetypeChunkComponentType<CharacterControllerVelocity>(),
                ghostHealthStateDataType = GetArchetypeChunkComponentType<HealthStateData>(),
                ghostInventoryStateType = GetArchetypeChunkComponentType<Inventory.State>(),
                ghostPlayerOwnerPlayerIdType = GetArchetypeChunkComponentType<Player.OwnerPlayerId>(),
                ghostPlayerControlledStateType = GetArchetypeChunkComponentType<PlayerControlled.State>(),
                ghostLinkedEntityGroupType = GetArchetypeChunkBufferType<LinkedEntityGroup>(true),
                ghostAbilityAbilityControlFromEntity = GetComponentDataFromEntity<Ability.AbilityControl>(),
                ghostAbilityMovementInterpolatedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.InterpolatedState>(),
                ghostAbilityMovementPredictedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.PredictedState>(),
                ghostAbilitySprintPredictedStateFromEntity = GetComponentDataFromEntity<AbilitySprint.PredictedState>(),
                targetTick = m_ClientSimulationSystemGroup.InterpolationTick,
                targetTickFraction = m_ClientSimulationSystemGroup.InterpolationTickFraction
            };
            inputDeps = updateInterpolatedJob.Schedule(m_interpolatedQuery, JobHandle.CombineDependencies(inputDeps, m_GhostUpdateSystemGroup.LastGhostMapWriter));
        }
        return inputDeps;
    }
}
public partial class Char_TerraformerGhostSpawnSystem : DefaultGhostSpawnSystem<Char_TerraformerSnapshotData>
{
    struct SetPredictedDefault : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Char_TerraformerSnapshotData> snapshots;
        public NativeArray<int> predictionMask;
        [ReadOnly][DeallocateOnJobCompletion] public NativeArray<NetworkIdComponent> localPlayerId;
        public void Execute(int index)
        {
            if (localPlayerId.Length == 1 && snapshots[index].GetPlayerOwnerPlayerIdValue() == localPlayerId[0].Value)
                predictionMask[index] = 1;
        }
    }
    protected override JobHandle SetPredictedGhostDefaults(NativeArray<Char_TerraformerSnapshotData> snapshots, NativeArray<int> predictionMask, JobHandle inputDeps)
    {
        JobHandle playerHandle;
        var job = new SetPredictedDefault
        {
            snapshots = snapshots,
            predictionMask = predictionMask,
            localPlayerId = m_PlayerGroup.ToComponentDataArray<NetworkIdComponent>(Allocator.TempJob, out playerHandle),
        };
        return job.Schedule(predictionMask.Length, 8, JobHandle.CombineDependencies(playerHandle, inputDeps));
    }
}
