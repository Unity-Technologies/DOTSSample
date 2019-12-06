using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Unity.Transforms;

public struct Weapon_TerraformerGhostSerializer : IGhostSerializer<Weapon_TerraformerSnapshotData>
{
    private ComponentType componentTypeAbilityCollectionAbilityEntry;
    private ComponentType componentTypeAbilityCollectionState;
    private ComponentType componentTypeItemInputState;
    private ComponentType componentTypePartOwnerInputState;
    private ComponentType componentTypePartOwnerRegistryAsset;
    private ComponentType componentTypeRigAttacherAttachBone;
    private ComponentType componentTypeRigAttacherState;
    private ComponentType componentTypeLocalToWorld;
    private ComponentType componentTypeRotation;
    private ComponentType componentTypeTranslation;
    private ComponentType componentTypeLinkedEntityGroup;
    // FIXME: These disable safety since all serializers have an instance of the same type - causing aliasing. Should be fixed in a cleaner way
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Item.InputState> ghostItemInputStateType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkBufferType<LinkedEntityGroup> ghostLinkedEntityGroupType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<Ability.AbilityAction> ghostChild0AbilityAbilityActionType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<Ability.AbilityControl> ghostChild0AbilityAbilityControlType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<AbilityAutoRifle.InterpolatedState> ghostChild0AbilityAutoRifleInterpolatedStateType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<AbilityAutoRifle.PredictedState> ghostChild0AbilityAutoRiflePredictedStateType;


    public int CalculateImportance(ArchetypeChunk chunk)
    {
        return 1;
    }

    public bool WantsPredictionDelta => true;

    public int SnapshotSize => UnsafeUtility.SizeOf<Weapon_TerraformerSnapshotData>();
    public void BeginSerialize(ComponentSystemBase system)
    {
        componentTypeAbilityCollectionAbilityEntry = ComponentType.ReadWrite<AbilityCollection.AbilityEntry>();
        componentTypeAbilityCollectionState = ComponentType.ReadWrite<AbilityCollection.State>();
        componentTypeItemInputState = ComponentType.ReadWrite<Item.InputState>();
        componentTypePartOwnerInputState = ComponentType.ReadWrite<PartOwner.InputState>();
        componentTypePartOwnerRegistryAsset = ComponentType.ReadWrite<PartOwner.RegistryAsset>();
        componentTypeRigAttacherAttachBone = ComponentType.ReadWrite<RigAttacher.AttachBone>();
        componentTypeRigAttacherState = ComponentType.ReadWrite<RigAttacher.State>();
        componentTypeLocalToWorld = ComponentType.ReadWrite<LocalToWorld>();
        componentTypeRotation = ComponentType.ReadWrite<Rotation>();
        componentTypeTranslation = ComponentType.ReadWrite<Translation>();
        componentTypeLinkedEntityGroup = ComponentType.ReadWrite<LinkedEntityGroup>();
        ghostItemInputStateType = system.GetArchetypeChunkComponentType<Item.InputState>(true);
        ghostLinkedEntityGroupType = system.GetArchetypeChunkBufferType<LinkedEntityGroup>(true);
        ghostChild0AbilityAbilityActionType = system.GetComponentDataFromEntity<Ability.AbilityAction>(true);
        ghostChild0AbilityAbilityControlType = system.GetComponentDataFromEntity<Ability.AbilityControl>(true);
        ghostChild0AbilityAutoRifleInterpolatedStateType = system.GetComponentDataFromEntity<AbilityAutoRifle.InterpolatedState>(true);
        ghostChild0AbilityAutoRiflePredictedStateType = system.GetComponentDataFromEntity<AbilityAutoRifle.PredictedState>(true);
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
            if (components[i] == componentTypeItemInputState)
                ++matches;
            if (components[i] == componentTypePartOwnerInputState)
                ++matches;
            if (components[i] == componentTypePartOwnerRegistryAsset)
                ++matches;
            if (components[i] == componentTypeRigAttacherAttachBone)
                ++matches;
            if (components[i] == componentTypeRigAttacherState)
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
        return (matches == 11);
    }

    public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref Weapon_TerraformerSnapshotData snapshot, GhostSerializerState serializerState)
    {
        snapshot.tick = tick;
        var chunkDataItemInputState = chunk.GetNativeArray(ghostItemInputStateType);
        var chunkDataLinkedEntityGroup = chunk.GetBufferAccessor(ghostLinkedEntityGroupType);
        snapshot.SetItemInputStateowner(chunkDataItemInputState[ent].owner, serializerState);
        snapshot.SetItemInputStateslot(chunkDataItemInputState[ent].slot, serializerState);
        snapshot.SetItemInputStateplayerId(chunkDataItemInputState[ent].playerId, serializerState);
        snapshot.SetChild0AbilityAbilityActionaction(ghostChild0AbilityAbilityActionType[chunkDataLinkedEntityGroup[ent][1].Value].action, serializerState);
        snapshot.SetChild0AbilityAbilityActionactionStartTick(ghostChild0AbilityAbilityActionType[chunkDataLinkedEntityGroup[ent][1].Value].actionStartTick, serializerState);
        snapshot.SetChild0AbilityAbilityControlbehaviorState(ghostChild0AbilityAbilityControlType[chunkDataLinkedEntityGroup[ent][1].Value].behaviorState, serializerState);
        snapshot.SetChild0AbilityAbilityControlrequestDeactivate(ghostChild0AbilityAbilityControlType[chunkDataLinkedEntityGroup[ent][1].Value].requestDeactivate, serializerState);
        snapshot.SetChild0AbilityAutoRifleInterpolatedStatefireTick(ghostChild0AbilityAutoRifleInterpolatedStateType[chunkDataLinkedEntityGroup[ent][1].Value].fireTick, serializerState);
        snapshot.SetChild0AbilityAutoRifleInterpolatedStatefireEndPos(ghostChild0AbilityAutoRifleInterpolatedStateType[chunkDataLinkedEntityGroup[ent][1].Value].fireEndPos, serializerState);
        snapshot.SetChild0AbilityAutoRifleInterpolatedStateimpactType(ghostChild0AbilityAutoRifleInterpolatedStateType[chunkDataLinkedEntityGroup[ent][1].Value].impactType, serializerState);
        snapshot.SetChild0AbilityAutoRifleInterpolatedStateimpactNormal(ghostChild0AbilityAutoRifleInterpolatedStateType[chunkDataLinkedEntityGroup[ent][1].Value].impactNormal, serializerState);
        snapshot.SetChild0AbilityAutoRiflePredictedStateaction(ghostChild0AbilityAutoRiflePredictedStateType[chunkDataLinkedEntityGroup[ent][1].Value].action, serializerState);
        snapshot.SetChild0AbilityAutoRiflePredictedStatephaseStartTick(ghostChild0AbilityAutoRiflePredictedStateType[chunkDataLinkedEntityGroup[ent][1].Value].phaseStartTick, serializerState);
        snapshot.SetChild0AbilityAutoRiflePredictedStateammoInClip(ghostChild0AbilityAutoRiflePredictedStateType[chunkDataLinkedEntityGroup[ent][1].Value].ammoInClip, serializerState);
        snapshot.SetChild0AbilityAutoRiflePredictedStateCOF(ghostChild0AbilityAutoRiflePredictedStateType[chunkDataLinkedEntityGroup[ent][1].Value].COF, serializerState);
    }
}
