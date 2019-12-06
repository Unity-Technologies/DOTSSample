using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Entities;

public struct Weapon_TerraformerSnapshotData : ISnapshotData<Weapon_TerraformerSnapshotData>
{
    public uint tick;
    private int ItemInputStateowner;
    private uint ItemInputStateslot;
    private int ItemInputStateplayerId;
    private int Child0AbilityAbilityActionaction;
    private int Child0AbilityAbilityActionactionStartTick;
    private int Child0AbilityAbilityControlbehaviorState;
    private uint Child0AbilityAbilityControlrequestDeactivate;
    private int Child0AbilityAutoRifleInterpolatedStatefireTick;
    private int Child0AbilityAutoRifleInterpolatedStatefireEndPosX;
    private int Child0AbilityAutoRifleInterpolatedStatefireEndPosY;
    private int Child0AbilityAutoRifleInterpolatedStatefireEndPosZ;
    private int Child0AbilityAutoRifleInterpolatedStateimpactType;
    private int Child0AbilityAutoRifleInterpolatedStateimpactNormalX;
    private int Child0AbilityAutoRifleInterpolatedStateimpactNormalY;
    private int Child0AbilityAutoRifleInterpolatedStateimpactNormalZ;
    private int Child0AbilityAutoRiflePredictedStateaction;
    private int Child0AbilityAutoRiflePredictedStatephaseStartTick;
    private int Child0AbilityAutoRiflePredictedStateammoInClip;
    private int Child0AbilityAutoRiflePredictedStateCOF;
    uint changeMask0;

    public uint Tick => tick;
    public Entity GetItemInputStateowner(GhostDeserializerState deserializerState)
    {
        if (ItemInputStateowner == 0)
            return Entity.Null;
        if (!deserializerState.GhostMap.TryGetValue(ItemInputStateowner, out var ghostEnt))
            return Entity.Null;
        if (Unity.Networking.Transport.Utilities.SequenceHelpers.IsNewer(ghostEnt.spawnTick, Tick))
            return Entity.Null;
        return ghostEnt.entity;
    }
    public void SetItemInputStateowner(Entity val, GhostSerializerState serializerState)
    {
        ItemInputStateowner = 0;
        if (serializerState.GhostStateFromEntity.Exists(val))
        {
            var ghostState = serializerState.GhostStateFromEntity[val];
            if (ghostState.despawnTick == 0)
                ItemInputStateowner = ghostState.ghostId;
        }
    }
    public void SetItemInputStateowner(int val)
    {
        ItemInputStateowner = val;
    }
    public byte GetItemInputStateslot(GhostDeserializerState deserializerState)
    {
        return (byte)ItemInputStateslot;
    }
    public byte GetItemInputStateslot()
    {
        return (byte)ItemInputStateslot;
    }
    public void SetItemInputStateslot(byte val, GhostSerializerState serializerState)
    {
        ItemInputStateslot = (uint)val;
    }
    public void SetItemInputStateslot(byte val)
    {
        ItemInputStateslot = (uint)val;
    }
    public int GetItemInputStateplayerId(GhostDeserializerState deserializerState)
    {
        return (int)ItemInputStateplayerId;
    }
    public int GetItemInputStateplayerId()
    {
        return (int)ItemInputStateplayerId;
    }
    public void SetItemInputStateplayerId(int val, GhostSerializerState serializerState)
    {
        ItemInputStateplayerId = (int)val;
    }
    public void SetItemInputStateplayerId(int val)
    {
        ItemInputStateplayerId = (int)val;
    }
    public Ability.AbilityAction.Action GetChild0AbilityAbilityActionaction(GhostDeserializerState deserializerState)
    {
        return (Ability.AbilityAction.Action)Child0AbilityAbilityActionaction;
    }
    public Ability.AbilityAction.Action GetChild0AbilityAbilityActionaction()
    {
        return (Ability.AbilityAction.Action)Child0AbilityAbilityActionaction;
    }
    public void SetChild0AbilityAbilityActionaction(Ability.AbilityAction.Action val, GhostSerializerState serializerState)
    {
        Child0AbilityAbilityActionaction = (int)val;
    }
    public void SetChild0AbilityAbilityActionaction(Ability.AbilityAction.Action val)
    {
        Child0AbilityAbilityActionaction = (int)val;
    }
    public int GetChild0AbilityAbilityActionactionStartTick(GhostDeserializerState deserializerState)
    {
        return (int)Child0AbilityAbilityActionactionStartTick;
    }
    public int GetChild0AbilityAbilityActionactionStartTick()
    {
        return (int)Child0AbilityAbilityActionactionStartTick;
    }
    public void SetChild0AbilityAbilityActionactionStartTick(int val, GhostSerializerState serializerState)
    {
        Child0AbilityAbilityActionactionStartTick = (int)val;
    }
    public void SetChild0AbilityAbilityActionactionStartTick(int val)
    {
        Child0AbilityAbilityActionactionStartTick = (int)val;
    }
    public Ability.AbilityControl.State GetChild0AbilityAbilityControlbehaviorState(GhostDeserializerState deserializerState)
    {
        return (Ability.AbilityControl.State)Child0AbilityAbilityControlbehaviorState;
    }
    public Ability.AbilityControl.State GetChild0AbilityAbilityControlbehaviorState()
    {
        return (Ability.AbilityControl.State)Child0AbilityAbilityControlbehaviorState;
    }
    public void SetChild0AbilityAbilityControlbehaviorState(Ability.AbilityControl.State val, GhostSerializerState serializerState)
    {
        Child0AbilityAbilityControlbehaviorState = (int)val;
    }
    public void SetChild0AbilityAbilityControlbehaviorState(Ability.AbilityControl.State val)
    {
        Child0AbilityAbilityControlbehaviorState = (int)val;
    }
    public bool GetChild0AbilityAbilityControlrequestDeactivate(GhostDeserializerState deserializerState)
    {
        return Child0AbilityAbilityControlrequestDeactivate!=0;
    }
    public bool GetChild0AbilityAbilityControlrequestDeactivate()
    {
        return Child0AbilityAbilityControlrequestDeactivate!=0;
    }
    public void SetChild0AbilityAbilityControlrequestDeactivate(bool val, GhostSerializerState serializerState)
    {
        Child0AbilityAbilityControlrequestDeactivate = val?1u:0;
    }
    public void SetChild0AbilityAbilityControlrequestDeactivate(bool val)
    {
        Child0AbilityAbilityControlrequestDeactivate = val?1u:0;
    }
    public int GetChild0AbilityAutoRifleInterpolatedStatefireTick(GhostDeserializerState deserializerState)
    {
        return (int)Child0AbilityAutoRifleInterpolatedStatefireTick;
    }
    public int GetChild0AbilityAutoRifleInterpolatedStatefireTick()
    {
        return (int)Child0AbilityAutoRifleInterpolatedStatefireTick;
    }
    public void SetChild0AbilityAutoRifleInterpolatedStatefireTick(int val, GhostSerializerState serializerState)
    {
        Child0AbilityAutoRifleInterpolatedStatefireTick = (int)val;
    }
    public void SetChild0AbilityAutoRifleInterpolatedStatefireTick(int val)
    {
        Child0AbilityAutoRifleInterpolatedStatefireTick = (int)val;
    }
    public float3 GetChild0AbilityAutoRifleInterpolatedStatefireEndPos(GhostDeserializerState deserializerState)
    {
        return GetChild0AbilityAutoRifleInterpolatedStatefireEndPos();
    }
    public float3 GetChild0AbilityAutoRifleInterpolatedStatefireEndPos()
    {
        return new float3(Child0AbilityAutoRifleInterpolatedStatefireEndPosX * 0.01f, Child0AbilityAutoRifleInterpolatedStatefireEndPosY * 0.01f, Child0AbilityAutoRifleInterpolatedStatefireEndPosZ * 0.01f);
    }
    public void SetChild0AbilityAutoRifleInterpolatedStatefireEndPos(float3 val, GhostSerializerState serializerState)
    {
        SetChild0AbilityAutoRifleInterpolatedStatefireEndPos(val);
    }
    public void SetChild0AbilityAutoRifleInterpolatedStatefireEndPos(float3 val)
    {
        Child0AbilityAutoRifleInterpolatedStatefireEndPosX = (int)(val.x * 100);
        Child0AbilityAutoRifleInterpolatedStatefireEndPosY = (int)(val.y * 100);
        Child0AbilityAutoRifleInterpolatedStatefireEndPosZ = (int)(val.z * 100);
    }
    public AbilityAutoRifle.ImpactType GetChild0AbilityAutoRifleInterpolatedStateimpactType(GhostDeserializerState deserializerState)
    {
        return (AbilityAutoRifle.ImpactType)Child0AbilityAutoRifleInterpolatedStateimpactType;
    }
    public AbilityAutoRifle.ImpactType GetChild0AbilityAutoRifleInterpolatedStateimpactType()
    {
        return (AbilityAutoRifle.ImpactType)Child0AbilityAutoRifleInterpolatedStateimpactType;
    }
    public void SetChild0AbilityAutoRifleInterpolatedStateimpactType(AbilityAutoRifle.ImpactType val, GhostSerializerState serializerState)
    {
        Child0AbilityAutoRifleInterpolatedStateimpactType = (int)val;
    }
    public void SetChild0AbilityAutoRifleInterpolatedStateimpactType(AbilityAutoRifle.ImpactType val)
    {
        Child0AbilityAutoRifleInterpolatedStateimpactType = (int)val;
    }
    public float3 GetChild0AbilityAutoRifleInterpolatedStateimpactNormal(GhostDeserializerState deserializerState)
    {
        return GetChild0AbilityAutoRifleInterpolatedStateimpactNormal();
    }
    public float3 GetChild0AbilityAutoRifleInterpolatedStateimpactNormal()
    {
        return new float3(Child0AbilityAutoRifleInterpolatedStateimpactNormalX * 0.1f, Child0AbilityAutoRifleInterpolatedStateimpactNormalY * 0.1f, Child0AbilityAutoRifleInterpolatedStateimpactNormalZ * 0.1f);
    }
    public void SetChild0AbilityAutoRifleInterpolatedStateimpactNormal(float3 val, GhostSerializerState serializerState)
    {
        SetChild0AbilityAutoRifleInterpolatedStateimpactNormal(val);
    }
    public void SetChild0AbilityAutoRifleInterpolatedStateimpactNormal(float3 val)
    {
        Child0AbilityAutoRifleInterpolatedStateimpactNormalX = (int)(val.x * 10);
        Child0AbilityAutoRifleInterpolatedStateimpactNormalY = (int)(val.y * 10);
        Child0AbilityAutoRifleInterpolatedStateimpactNormalZ = (int)(val.z * 10);
    }
    public AbilityAutoRifle.Phase GetChild0AbilityAutoRiflePredictedStateaction(GhostDeserializerState deserializerState)
    {
        return (AbilityAutoRifle.Phase)Child0AbilityAutoRiflePredictedStateaction;
    }
    public AbilityAutoRifle.Phase GetChild0AbilityAutoRiflePredictedStateaction()
    {
        return (AbilityAutoRifle.Phase)Child0AbilityAutoRiflePredictedStateaction;
    }
    public void SetChild0AbilityAutoRiflePredictedStateaction(AbilityAutoRifle.Phase val, GhostSerializerState serializerState)
    {
        Child0AbilityAutoRiflePredictedStateaction = (int)val;
    }
    public void SetChild0AbilityAutoRiflePredictedStateaction(AbilityAutoRifle.Phase val)
    {
        Child0AbilityAutoRiflePredictedStateaction = (int)val;
    }
    public int GetChild0AbilityAutoRiflePredictedStatephaseStartTick(GhostDeserializerState deserializerState)
    {
        return (int)Child0AbilityAutoRiflePredictedStatephaseStartTick;
    }
    public int GetChild0AbilityAutoRiflePredictedStatephaseStartTick()
    {
        return (int)Child0AbilityAutoRiflePredictedStatephaseStartTick;
    }
    public void SetChild0AbilityAutoRiflePredictedStatephaseStartTick(int val, GhostSerializerState serializerState)
    {
        Child0AbilityAutoRiflePredictedStatephaseStartTick = (int)val;
    }
    public void SetChild0AbilityAutoRiflePredictedStatephaseStartTick(int val)
    {
        Child0AbilityAutoRiflePredictedStatephaseStartTick = (int)val;
    }
    public int GetChild0AbilityAutoRiflePredictedStateammoInClip(GhostDeserializerState deserializerState)
    {
        return (int)Child0AbilityAutoRiflePredictedStateammoInClip;
    }
    public int GetChild0AbilityAutoRiflePredictedStateammoInClip()
    {
        return (int)Child0AbilityAutoRiflePredictedStateammoInClip;
    }
    public void SetChild0AbilityAutoRiflePredictedStateammoInClip(int val, GhostSerializerState serializerState)
    {
        Child0AbilityAutoRiflePredictedStateammoInClip = (int)val;
    }
    public void SetChild0AbilityAutoRiflePredictedStateammoInClip(int val)
    {
        Child0AbilityAutoRiflePredictedStateammoInClip = (int)val;
    }
    public float GetChild0AbilityAutoRiflePredictedStateCOF(GhostDeserializerState deserializerState)
    {
        return Child0AbilityAutoRiflePredictedStateCOF * 1f;
    }
    public float GetChild0AbilityAutoRiflePredictedStateCOF()
    {
        return Child0AbilityAutoRiflePredictedStateCOF * 1f;
    }
    public void SetChild0AbilityAutoRiflePredictedStateCOF(float val, GhostSerializerState serializerState)
    {
        Child0AbilityAutoRiflePredictedStateCOF = (int)(val * 1);
    }
    public void SetChild0AbilityAutoRiflePredictedStateCOF(float val)
    {
        Child0AbilityAutoRiflePredictedStateCOF = (int)(val * 1);
    }

    public void PredictDelta(uint tick, ref Weapon_TerraformerSnapshotData baseline1, ref Weapon_TerraformerSnapshotData baseline2)
    {
        var predictor = new GhostDeltaPredictor(tick, this.tick, baseline1.tick, baseline2.tick);
        ItemInputStateowner = predictor.PredictInt(ItemInputStateowner, baseline1.ItemInputStateowner, baseline2.ItemInputStateowner);
        ItemInputStateslot = (uint)predictor.PredictInt((int)ItemInputStateslot, (int)baseline1.ItemInputStateslot, (int)baseline2.ItemInputStateslot);
        ItemInputStateplayerId = predictor.PredictInt(ItemInputStateplayerId, baseline1.ItemInputStateplayerId, baseline2.ItemInputStateplayerId);
        Child0AbilityAbilityActionaction = predictor.PredictInt(Child0AbilityAbilityActionaction, baseline1.Child0AbilityAbilityActionaction, baseline2.Child0AbilityAbilityActionaction);
        Child0AbilityAbilityActionactionStartTick = predictor.PredictInt(Child0AbilityAbilityActionactionStartTick, baseline1.Child0AbilityAbilityActionactionStartTick, baseline2.Child0AbilityAbilityActionactionStartTick);
        Child0AbilityAbilityControlbehaviorState = predictor.PredictInt(Child0AbilityAbilityControlbehaviorState, baseline1.Child0AbilityAbilityControlbehaviorState, baseline2.Child0AbilityAbilityControlbehaviorState);
        Child0AbilityAbilityControlrequestDeactivate = (uint)predictor.PredictInt((int)Child0AbilityAbilityControlrequestDeactivate, (int)baseline1.Child0AbilityAbilityControlrequestDeactivate, (int)baseline2.Child0AbilityAbilityControlrequestDeactivate);
        Child0AbilityAutoRifleInterpolatedStatefireTick = predictor.PredictInt(Child0AbilityAutoRifleInterpolatedStatefireTick, baseline1.Child0AbilityAutoRifleInterpolatedStatefireTick, baseline2.Child0AbilityAutoRifleInterpolatedStatefireTick);
        Child0AbilityAutoRifleInterpolatedStatefireEndPosX = predictor.PredictInt(Child0AbilityAutoRifleInterpolatedStatefireEndPosX, baseline1.Child0AbilityAutoRifleInterpolatedStatefireEndPosX, baseline2.Child0AbilityAutoRifleInterpolatedStatefireEndPosX);
        Child0AbilityAutoRifleInterpolatedStatefireEndPosY = predictor.PredictInt(Child0AbilityAutoRifleInterpolatedStatefireEndPosY, baseline1.Child0AbilityAutoRifleInterpolatedStatefireEndPosY, baseline2.Child0AbilityAutoRifleInterpolatedStatefireEndPosY);
        Child0AbilityAutoRifleInterpolatedStatefireEndPosZ = predictor.PredictInt(Child0AbilityAutoRifleInterpolatedStatefireEndPosZ, baseline1.Child0AbilityAutoRifleInterpolatedStatefireEndPosZ, baseline2.Child0AbilityAutoRifleInterpolatedStatefireEndPosZ);
        Child0AbilityAutoRifleInterpolatedStateimpactType = predictor.PredictInt(Child0AbilityAutoRifleInterpolatedStateimpactType, baseline1.Child0AbilityAutoRifleInterpolatedStateimpactType, baseline2.Child0AbilityAutoRifleInterpolatedStateimpactType);
        Child0AbilityAutoRifleInterpolatedStateimpactNormalX = predictor.PredictInt(Child0AbilityAutoRifleInterpolatedStateimpactNormalX, baseline1.Child0AbilityAutoRifleInterpolatedStateimpactNormalX, baseline2.Child0AbilityAutoRifleInterpolatedStateimpactNormalX);
        Child0AbilityAutoRifleInterpolatedStateimpactNormalY = predictor.PredictInt(Child0AbilityAutoRifleInterpolatedStateimpactNormalY, baseline1.Child0AbilityAutoRifleInterpolatedStateimpactNormalY, baseline2.Child0AbilityAutoRifleInterpolatedStateimpactNormalY);
        Child0AbilityAutoRifleInterpolatedStateimpactNormalZ = predictor.PredictInt(Child0AbilityAutoRifleInterpolatedStateimpactNormalZ, baseline1.Child0AbilityAutoRifleInterpolatedStateimpactNormalZ, baseline2.Child0AbilityAutoRifleInterpolatedStateimpactNormalZ);
        Child0AbilityAutoRiflePredictedStateaction = predictor.PredictInt(Child0AbilityAutoRiflePredictedStateaction, baseline1.Child0AbilityAutoRiflePredictedStateaction, baseline2.Child0AbilityAutoRiflePredictedStateaction);
        Child0AbilityAutoRiflePredictedStatephaseStartTick = predictor.PredictInt(Child0AbilityAutoRiflePredictedStatephaseStartTick, baseline1.Child0AbilityAutoRiflePredictedStatephaseStartTick, baseline2.Child0AbilityAutoRiflePredictedStatephaseStartTick);
        Child0AbilityAutoRiflePredictedStateammoInClip = predictor.PredictInt(Child0AbilityAutoRiflePredictedStateammoInClip, baseline1.Child0AbilityAutoRiflePredictedStateammoInClip, baseline2.Child0AbilityAutoRiflePredictedStateammoInClip);
        Child0AbilityAutoRiflePredictedStateCOF = predictor.PredictInt(Child0AbilityAutoRiflePredictedStateCOF, baseline1.Child0AbilityAutoRiflePredictedStateCOF, baseline2.Child0AbilityAutoRiflePredictedStateCOF);
    }

    public void Serialize(int networkId, ref Weapon_TerraformerSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
    {
        changeMask0 = (ItemInputStateowner != baseline.ItemInputStateowner) ? 1u : 0;
        changeMask0 |= (ItemInputStateslot != baseline.ItemInputStateslot) ? (1u<<1) : 0;
        changeMask0 |= (ItemInputStateplayerId != baseline.ItemInputStateplayerId) ? (1u<<2) : 0;
        changeMask0 |= (Child0AbilityAbilityActionaction != baseline.Child0AbilityAbilityActionaction) ? (1u<<3) : 0;
        changeMask0 |= (Child0AbilityAbilityActionactionStartTick != baseline.Child0AbilityAbilityActionactionStartTick) ? (1u<<4) : 0;
        changeMask0 |= (Child0AbilityAbilityControlbehaviorState != baseline.Child0AbilityAbilityControlbehaviorState) ? (1u<<5) : 0;
        changeMask0 |= (Child0AbilityAbilityControlrequestDeactivate != baseline.Child0AbilityAbilityControlrequestDeactivate) ? (1u<<6) : 0;
        changeMask0 |= (Child0AbilityAutoRifleInterpolatedStatefireTick != baseline.Child0AbilityAutoRifleInterpolatedStatefireTick) ? (1u<<7) : 0;
        changeMask0 |= (Child0AbilityAutoRifleInterpolatedStatefireEndPosX != baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosX ||
                                           Child0AbilityAutoRifleInterpolatedStatefireEndPosY != baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosY ||
                                           Child0AbilityAutoRifleInterpolatedStatefireEndPosZ != baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosZ) ? (1u<<8) : 0;
        changeMask0 |= (Child0AbilityAutoRifleInterpolatedStateimpactType != baseline.Child0AbilityAutoRifleInterpolatedStateimpactType) ? (1u<<9) : 0;
        changeMask0 |= (Child0AbilityAutoRifleInterpolatedStateimpactNormalX != baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalX ||
                                           Child0AbilityAutoRifleInterpolatedStateimpactNormalY != baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalY ||
                                           Child0AbilityAutoRifleInterpolatedStateimpactNormalZ != baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalZ) ? (1u<<10) : 0;
        changeMask0 |= (Child0AbilityAutoRiflePredictedStateaction != baseline.Child0AbilityAutoRiflePredictedStateaction) ? (1u<<11) : 0;
        changeMask0 |= (Child0AbilityAutoRiflePredictedStatephaseStartTick != baseline.Child0AbilityAutoRiflePredictedStatephaseStartTick) ? (1u<<12) : 0;
        changeMask0 |= (Child0AbilityAutoRiflePredictedStateammoInClip != baseline.Child0AbilityAutoRiflePredictedStateammoInClip) ? (1u<<13) : 0;
        changeMask0 |= (Child0AbilityAutoRiflePredictedStateCOF != baseline.Child0AbilityAutoRiflePredictedStateCOF) ? (1u<<14) : 0;
        writer.WritePackedUIntDelta(changeMask0, baseline.changeMask0, compressionModel);
        if ((changeMask0 & (1 << 0)) != 0)
            writer.WritePackedIntDelta(ItemInputStateowner, baseline.ItemInputStateowner, compressionModel);
        if ((changeMask0 & (1 << 1)) != 0)
            writer.WritePackedUIntDelta(ItemInputStateslot, baseline.ItemInputStateslot, compressionModel);
        if ((changeMask0 & (1 << 2)) != 0)
            writer.WritePackedIntDelta(ItemInputStateplayerId, baseline.ItemInputStateplayerId, compressionModel);
        if ((changeMask0 & (1 << 3)) != 0)
            writer.WritePackedIntDelta(Child0AbilityAbilityActionaction, baseline.Child0AbilityAbilityActionaction, compressionModel);
        if ((changeMask0 & (1 << 4)) != 0)
            writer.WritePackedIntDelta(Child0AbilityAbilityActionactionStartTick, baseline.Child0AbilityAbilityActionactionStartTick, compressionModel);
        if ((changeMask0 & (1 << 5)) != 0)
            writer.WritePackedIntDelta(Child0AbilityAbilityControlbehaviorState, baseline.Child0AbilityAbilityControlbehaviorState, compressionModel);
        if ((changeMask0 & (1 << 6)) != 0)
            writer.WritePackedUIntDelta(Child0AbilityAbilityControlrequestDeactivate, baseline.Child0AbilityAbilityControlrequestDeactivate, compressionModel);
        if ((changeMask0 & (1 << 7)) != 0)
            writer.WritePackedIntDelta(Child0AbilityAutoRifleInterpolatedStatefireTick, baseline.Child0AbilityAutoRifleInterpolatedStatefireTick, compressionModel);
        if ((changeMask0 & (1 << 8)) != 0)
        {
            writer.WritePackedIntDelta(Child0AbilityAutoRifleInterpolatedStatefireEndPosX, baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosX, compressionModel);
            writer.WritePackedIntDelta(Child0AbilityAutoRifleInterpolatedStatefireEndPosY, baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosY, compressionModel);
            writer.WritePackedIntDelta(Child0AbilityAutoRifleInterpolatedStatefireEndPosZ, baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosZ, compressionModel);
        }
        if ((changeMask0 & (1 << 9)) != 0)
            writer.WritePackedIntDelta(Child0AbilityAutoRifleInterpolatedStateimpactType, baseline.Child0AbilityAutoRifleInterpolatedStateimpactType, compressionModel);
        if ((changeMask0 & (1 << 10)) != 0)
        {
            writer.WritePackedIntDelta(Child0AbilityAutoRifleInterpolatedStateimpactNormalX, baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalX, compressionModel);
            writer.WritePackedIntDelta(Child0AbilityAutoRifleInterpolatedStateimpactNormalY, baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalY, compressionModel);
            writer.WritePackedIntDelta(Child0AbilityAutoRifleInterpolatedStateimpactNormalZ, baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalZ, compressionModel);
        }
        if ((changeMask0 & (1 << 11)) != 0)
            writer.WritePackedIntDelta(Child0AbilityAutoRiflePredictedStateaction, baseline.Child0AbilityAutoRiflePredictedStateaction, compressionModel);
        if ((changeMask0 & (1 << 12)) != 0)
            writer.WritePackedIntDelta(Child0AbilityAutoRiflePredictedStatephaseStartTick, baseline.Child0AbilityAutoRiflePredictedStatephaseStartTick, compressionModel);
        if ((changeMask0 & (1 << 13)) != 0)
            writer.WritePackedIntDelta(Child0AbilityAutoRiflePredictedStateammoInClip, baseline.Child0AbilityAutoRiflePredictedStateammoInClip, compressionModel);
        if ((changeMask0 & (1 << 14)) != 0)
            writer.WritePackedIntDelta(Child0AbilityAutoRiflePredictedStateCOF, baseline.Child0AbilityAutoRiflePredictedStateCOF, compressionModel);
    }

    public void Deserialize(uint tick, ref Weapon_TerraformerSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx,
        NetworkCompressionModel compressionModel)
    {
        this.tick = tick;
        changeMask0 = reader.ReadPackedUIntDelta(ref ctx, baseline.changeMask0, compressionModel);
        if ((changeMask0 & (1 << 0)) != 0)
            ItemInputStateowner = reader.ReadPackedIntDelta(ref ctx, baseline.ItemInputStateowner, compressionModel);
        else
            ItemInputStateowner = baseline.ItemInputStateowner;
        if ((changeMask0 & (1 << 1)) != 0)
            ItemInputStateslot = reader.ReadPackedUIntDelta(ref ctx, baseline.ItemInputStateslot, compressionModel);
        else
            ItemInputStateslot = baseline.ItemInputStateslot;
        if ((changeMask0 & (1 << 2)) != 0)
            ItemInputStateplayerId = reader.ReadPackedIntDelta(ref ctx, baseline.ItemInputStateplayerId, compressionModel);
        else
            ItemInputStateplayerId = baseline.ItemInputStateplayerId;
        if ((changeMask0 & (1 << 3)) != 0)
            Child0AbilityAbilityActionaction = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAbilityActionaction, compressionModel);
        else
            Child0AbilityAbilityActionaction = baseline.Child0AbilityAbilityActionaction;
        if ((changeMask0 & (1 << 4)) != 0)
            Child0AbilityAbilityActionactionStartTick = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAbilityActionactionStartTick, compressionModel);
        else
            Child0AbilityAbilityActionactionStartTick = baseline.Child0AbilityAbilityActionactionStartTick;
        if ((changeMask0 & (1 << 5)) != 0)
            Child0AbilityAbilityControlbehaviorState = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAbilityControlbehaviorState, compressionModel);
        else
            Child0AbilityAbilityControlbehaviorState = baseline.Child0AbilityAbilityControlbehaviorState;
        if ((changeMask0 & (1 << 6)) != 0)
            Child0AbilityAbilityControlrequestDeactivate = reader.ReadPackedUIntDelta(ref ctx, baseline.Child0AbilityAbilityControlrequestDeactivate, compressionModel);
        else
            Child0AbilityAbilityControlrequestDeactivate = baseline.Child0AbilityAbilityControlrequestDeactivate;
        if ((changeMask0 & (1 << 7)) != 0)
            Child0AbilityAutoRifleInterpolatedStatefireTick = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRifleInterpolatedStatefireTick, compressionModel);
        else
            Child0AbilityAutoRifleInterpolatedStatefireTick = baseline.Child0AbilityAutoRifleInterpolatedStatefireTick;
        if ((changeMask0 & (1 << 8)) != 0)
        {
            Child0AbilityAutoRifleInterpolatedStatefireEndPosX = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosX, compressionModel);
            Child0AbilityAutoRifleInterpolatedStatefireEndPosY = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosY, compressionModel);
            Child0AbilityAutoRifleInterpolatedStatefireEndPosZ = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosZ, compressionModel);
        }
        else
        {
            Child0AbilityAutoRifleInterpolatedStatefireEndPosX = baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosX;
            Child0AbilityAutoRifleInterpolatedStatefireEndPosY = baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosY;
            Child0AbilityAutoRifleInterpolatedStatefireEndPosZ = baseline.Child0AbilityAutoRifleInterpolatedStatefireEndPosZ;
        }
        if ((changeMask0 & (1 << 9)) != 0)
            Child0AbilityAutoRifleInterpolatedStateimpactType = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRifleInterpolatedStateimpactType, compressionModel);
        else
            Child0AbilityAutoRifleInterpolatedStateimpactType = baseline.Child0AbilityAutoRifleInterpolatedStateimpactType;
        if ((changeMask0 & (1 << 10)) != 0)
        {
            Child0AbilityAutoRifleInterpolatedStateimpactNormalX = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalX, compressionModel);
            Child0AbilityAutoRifleInterpolatedStateimpactNormalY = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalY, compressionModel);
            Child0AbilityAutoRifleInterpolatedStateimpactNormalZ = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalZ, compressionModel);
        }
        else
        {
            Child0AbilityAutoRifleInterpolatedStateimpactNormalX = baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalX;
            Child0AbilityAutoRifleInterpolatedStateimpactNormalY = baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalY;
            Child0AbilityAutoRifleInterpolatedStateimpactNormalZ = baseline.Child0AbilityAutoRifleInterpolatedStateimpactNormalZ;
        }
        if ((changeMask0 & (1 << 11)) != 0)
            Child0AbilityAutoRiflePredictedStateaction = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRiflePredictedStateaction, compressionModel);
        else
            Child0AbilityAutoRiflePredictedStateaction = baseline.Child0AbilityAutoRiflePredictedStateaction;
        if ((changeMask0 & (1 << 12)) != 0)
            Child0AbilityAutoRiflePredictedStatephaseStartTick = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRiflePredictedStatephaseStartTick, compressionModel);
        else
            Child0AbilityAutoRiflePredictedStatephaseStartTick = baseline.Child0AbilityAutoRiflePredictedStatephaseStartTick;
        if ((changeMask0 & (1 << 13)) != 0)
            Child0AbilityAutoRiflePredictedStateammoInClip = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRiflePredictedStateammoInClip, compressionModel);
        else
            Child0AbilityAutoRiflePredictedStateammoInClip = baseline.Child0AbilityAutoRiflePredictedStateammoInClip;
        if ((changeMask0 & (1 << 14)) != 0)
            Child0AbilityAutoRiflePredictedStateCOF = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAutoRiflePredictedStateCOF, compressionModel);
        else
            Child0AbilityAutoRiflePredictedStateCOF = baseline.Child0AbilityAutoRiflePredictedStateCOF;
    }
    public void Interpolate(ref Weapon_TerraformerSnapshotData target, float factor)
    {
    }
}
