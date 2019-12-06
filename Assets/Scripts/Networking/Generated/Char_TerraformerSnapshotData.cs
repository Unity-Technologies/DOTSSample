using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;

public struct Char_TerraformerSnapshotData : ISnapshotData<Char_TerraformerSnapshotData>
{
    public uint tick;
    private int CharacterInterpolatedDataPositionX;
    private int CharacterInterpolatedDataPositionY;
    private int CharacterInterpolatedDataPositionZ;
    private int CharacterInterpolatedDatarotation;
    private int CharacterInterpolatedDataaimYaw;
    private int CharacterInterpolatedDataaimPitch;
    private int CharacterInterpolatedDatamoveYaw;
    private int CharacterInterpolatedDatacharAction;
    private int CharacterInterpolatedDatacharActionTick;
    private int CharacterInterpolatedDatadamageTick;
    private int CharacterInterpolatedDatadamageDirection;
    private uint CharacterInterpolatedDatasprinting;
    private int CharacterInterpolatedDatasprintWeight;
    private int CharacterInterpolatedDatacrouchWeight;
    private int CharacterInterpolatedDataselectorTargetSource;
    private int CharacterInterpolatedDatamoveAngleLocal;
    private int CharacterInterpolatedDatashootPoseWeight;
    private int CharacterInterpolatedDatalocomotionVectorX;
    private int CharacterInterpolatedDatalocomotionVectorY;
    private int CharacterInterpolatedDatalocomotionPhase;
    private int CharacterInterpolatedDatabanking;
    private int CharacterInterpolatedDatalandAnticWeight;
    private int CharacterInterpolatedDataturnStartAngle;
    private int CharacterInterpolatedDataturnDirection;
    private int CharacterInterpolatedDatasquashTime;
    private int CharacterInterpolatedDatasquashWeight;
    private int CharacterInterpolatedDatainAirTime;
    private int CharacterInterpolatedDatajumpTime;
    private int CharacterInterpolatedDatasimpleTime;
    private int CharacterInterpolatedDatafootIkOffsetX;
    private int CharacterInterpolatedDatafootIkOffsetY;
    private int CharacterInterpolatedDatafootIkNormalLeftX;
    private int CharacterInterpolatedDatafootIkNormalLeftY;
    private int CharacterInterpolatedDatafootIkNormalLeftZ;
    private int CharacterInterpolatedDatafootIkNormalRightX;
    private int CharacterInterpolatedDatafootIkNormalRightY;
    private int CharacterInterpolatedDatafootIkNormalRightZ;
    private int CharacterInterpolatedDatafootIkWeight;
    private int CharacterInterpolatedDatablendOutAim;
    private int CharacterPredictedDatatick;
    private float CharacterPredictedDatapositionX;
    private float CharacterPredictedDatapositionY;
    private float CharacterPredictedDatapositionZ;
    private float CharacterPredictedDatavelocityX;
    private float CharacterPredictedDatavelocityY;
    private float CharacterPredictedDatavelocityZ;
    private uint CharacterPredictedDatasprinting;
    private int CharacterPredictedDatacameraProfile;
    private int CharacterPredictedDatadamageTick;
    private int CharacterPredictedDatadamageDirectionX;
    private int CharacterPredictedDatadamageDirectionY;
    private int CharacterPredictedDatadamageDirectionZ;
    private int CharacterReplicatedDataheroTypeIndex;
    private float CharacterControllerGroundSupportDataSurfaceNormalX;
    private float CharacterControllerGroundSupportDataSurfaceNormalY;
    private float CharacterControllerGroundSupportDataSurfaceNormalZ;
    private float CharacterControllerGroundSupportDataSurfaceVelocityX;
    private float CharacterControllerGroundSupportDataSurfaceVelocityY;
    private float CharacterControllerGroundSupportDataSurfaceVelocityZ;
    private int CharacterControllerGroundSupportDataSupportedState;
    private float CharacterControllerMoveResultMoveResultX;
    private float CharacterControllerMoveResultMoveResultY;
    private float CharacterControllerMoveResultMoveResultZ;
    private float CharacterControllerVelocityVelocityX;
    private float CharacterControllerVelocityVelocityY;
    private float CharacterControllerVelocityVelocityZ;
    private int HealthStateDatahealth;
    private int InventoryStateactiveSlot;
    private int PlayerOwnerPlayerIdValue;
    private int PlayerControlledStateresetCommandTick;
    private int PlayerControlledStateresetCommandLookYaw;
    private int PlayerControlledStateresetCommandLookPitch;
    private int Child0AbilityAbilityControlbehaviorState;
    private uint Child0AbilityAbilityControlrequestDeactivate;
    private int Child0AbilityMovementInterpolatedStatecharLocoState;
    private int Child0AbilityMovementInterpolatedStatecharLocoTick;
    private uint Child0AbilityMovementInterpolatedStatecrouching;
    private int Child0AbilityMovementPredictedStatelocoState;
    private int Child0AbilityMovementPredictedStatelocoStartTick;
    private int Child0AbilityMovementPredictedStatejumpCount;
    private uint Child0AbilityMovementPredictedStatecrouching;
    private int Child1AbilityAbilityControlbehaviorState;
    private uint Child1AbilityAbilityControlrequestDeactivate;
    private int Child1AbilitySprintPredictedStateactive;
    private int Child1AbilitySprintPredictedStateterminating;
    private int Child1AbilitySprintPredictedStateterminateStartTick;
    private int Child2AbilityAbilityControlbehaviorState;
    private uint Child2AbilityAbilityControlrequestDeactivate;
    private int Child3AbilityAbilityControlbehaviorState;
    private uint Child3AbilityAbilityControlrequestDeactivate;
    uint changeMask0;
    uint changeMask1;
    uint changeMask2;

    public uint Tick => tick;
    public float3 GetCharacterInterpolatedDataPosition(GhostDeserializerState deserializerState)
    {
        return GetCharacterInterpolatedDataPosition();
    }
    public float3 GetCharacterInterpolatedDataPosition()
    {
        return new float3(CharacterInterpolatedDataPositionX * 0.01f, CharacterInterpolatedDataPositionY * 0.01f, CharacterInterpolatedDataPositionZ * 0.01f);
    }
    public void SetCharacterInterpolatedDataPosition(float3 val, GhostSerializerState serializerState)
    {
        SetCharacterInterpolatedDataPosition(val);
    }
    public void SetCharacterInterpolatedDataPosition(float3 val)
    {
        CharacterInterpolatedDataPositionX = (int)(val.x * 100);
        CharacterInterpolatedDataPositionY = (int)(val.y * 100);
        CharacterInterpolatedDataPositionZ = (int)(val.z * 100);
    }
    public float GetCharacterInterpolatedDatarotation(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatarotation * 1f;
    }
    public float GetCharacterInterpolatedDatarotation()
    {
        return CharacterInterpolatedDatarotation * 1f;
    }
    public void SetCharacterInterpolatedDatarotation(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatarotation = (int)(val * 1);
    }
    public void SetCharacterInterpolatedDatarotation(float val)
    {
        CharacterInterpolatedDatarotation = (int)(val * 1);
    }
    public float GetCharacterInterpolatedDataaimYaw(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDataaimYaw * 1f;
    }
    public float GetCharacterInterpolatedDataaimYaw()
    {
        return CharacterInterpolatedDataaimYaw * 1f;
    }
    public void SetCharacterInterpolatedDataaimYaw(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDataaimYaw = (int)(val * 1);
    }
    public void SetCharacterInterpolatedDataaimYaw(float val)
    {
        CharacterInterpolatedDataaimYaw = (int)(val * 1);
    }
    public float GetCharacterInterpolatedDataaimPitch(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDataaimPitch * 1f;
    }
    public float GetCharacterInterpolatedDataaimPitch()
    {
        return CharacterInterpolatedDataaimPitch * 1f;
    }
    public void SetCharacterInterpolatedDataaimPitch(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDataaimPitch = (int)(val * 1);
    }
    public void SetCharacterInterpolatedDataaimPitch(float val)
    {
        CharacterInterpolatedDataaimPitch = (int)(val * 1);
    }
    public float GetCharacterInterpolatedDatamoveYaw(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatamoveYaw * 1f;
    }
    public float GetCharacterInterpolatedDatamoveYaw()
    {
        return CharacterInterpolatedDatamoveYaw * 1f;
    }
    public void SetCharacterInterpolatedDatamoveYaw(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatamoveYaw = (int)(val * 1);
    }
    public void SetCharacterInterpolatedDatamoveYaw(float val)
    {
        CharacterInterpolatedDatamoveYaw = (int)(val * 1);
    }
    public Ability.AbilityAction.Action GetCharacterInterpolatedDatacharAction(GhostDeserializerState deserializerState)
    {
        return (Ability.AbilityAction.Action)CharacterInterpolatedDatacharAction;
    }
    public Ability.AbilityAction.Action GetCharacterInterpolatedDatacharAction()
    {
        return (Ability.AbilityAction.Action)CharacterInterpolatedDatacharAction;
    }
    public void SetCharacterInterpolatedDatacharAction(Ability.AbilityAction.Action val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatacharAction = (int)val;
    }
    public void SetCharacterInterpolatedDatacharAction(Ability.AbilityAction.Action val)
    {
        CharacterInterpolatedDatacharAction = (int)val;
    }
    public int GetCharacterInterpolatedDatacharActionTick(GhostDeserializerState deserializerState)
    {
        return (int)CharacterInterpolatedDatacharActionTick;
    }
    public int GetCharacterInterpolatedDatacharActionTick()
    {
        return (int)CharacterInterpolatedDatacharActionTick;
    }
    public void SetCharacterInterpolatedDatacharActionTick(int val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatacharActionTick = (int)val;
    }
    public void SetCharacterInterpolatedDatacharActionTick(int val)
    {
        CharacterInterpolatedDatacharActionTick = (int)val;
    }
    public int GetCharacterInterpolatedDatadamageTick(GhostDeserializerState deserializerState)
    {
        return (int)CharacterInterpolatedDatadamageTick;
    }
    public int GetCharacterInterpolatedDatadamageTick()
    {
        return (int)CharacterInterpolatedDatadamageTick;
    }
    public void SetCharacterInterpolatedDatadamageTick(int val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatadamageTick = (int)val;
    }
    public void SetCharacterInterpolatedDatadamageTick(int val)
    {
        CharacterInterpolatedDatadamageTick = (int)val;
    }
    public float GetCharacterInterpolatedDatadamageDirection(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatadamageDirection * 0.1f;
    }
    public float GetCharacterInterpolatedDatadamageDirection()
    {
        return CharacterInterpolatedDatadamageDirection * 0.1f;
    }
    public void SetCharacterInterpolatedDatadamageDirection(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatadamageDirection = (int)(val * 10);
    }
    public void SetCharacterInterpolatedDatadamageDirection(float val)
    {
        CharacterInterpolatedDatadamageDirection = (int)(val * 10);
    }
    public bool GetCharacterInterpolatedDatasprinting(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatasprinting!=0;
    }
    public bool GetCharacterInterpolatedDatasprinting()
    {
        return CharacterInterpolatedDatasprinting!=0;
    }
    public void SetCharacterInterpolatedDatasprinting(bool val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatasprinting = val?1u:0;
    }
    public void SetCharacterInterpolatedDatasprinting(bool val)
    {
        CharacterInterpolatedDatasprinting = val?1u:0;
    }
    public float GetCharacterInterpolatedDatasprintWeight(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatasprintWeight * 0.01f;
    }
    public float GetCharacterInterpolatedDatasprintWeight()
    {
        return CharacterInterpolatedDatasprintWeight * 0.01f;
    }
    public void SetCharacterInterpolatedDatasprintWeight(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatasprintWeight = (int)(val * 100);
    }
    public void SetCharacterInterpolatedDatasprintWeight(float val)
    {
        CharacterInterpolatedDatasprintWeight = (int)(val * 100);
    }
    public float GetCharacterInterpolatedDatacrouchWeight(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatacrouchWeight * 0.01f;
    }
    public float GetCharacterInterpolatedDatacrouchWeight()
    {
        return CharacterInterpolatedDatacrouchWeight * 0.01f;
    }
    public void SetCharacterInterpolatedDatacrouchWeight(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatacrouchWeight = (int)(val * 100);
    }
    public void SetCharacterInterpolatedDatacrouchWeight(float val)
    {
        CharacterInterpolatedDatacrouchWeight = (int)(val * 100);
    }
    public int GetCharacterInterpolatedDataselectorTargetSource(GhostDeserializerState deserializerState)
    {
        return (int)CharacterInterpolatedDataselectorTargetSource;
    }
    public int GetCharacterInterpolatedDataselectorTargetSource()
    {
        return (int)CharacterInterpolatedDataselectorTargetSource;
    }
    public void SetCharacterInterpolatedDataselectorTargetSource(int val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDataselectorTargetSource = (int)val;
    }
    public void SetCharacterInterpolatedDataselectorTargetSource(int val)
    {
        CharacterInterpolatedDataselectorTargetSource = (int)val;
    }
    public float GetCharacterInterpolatedDatamoveAngleLocal(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatamoveAngleLocal * 1f;
    }
    public float GetCharacterInterpolatedDatamoveAngleLocal()
    {
        return CharacterInterpolatedDatamoveAngleLocal * 1f;
    }
    public void SetCharacterInterpolatedDatamoveAngleLocal(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatamoveAngleLocal = (int)(val * 1);
    }
    public void SetCharacterInterpolatedDatamoveAngleLocal(float val)
    {
        CharacterInterpolatedDatamoveAngleLocal = (int)(val * 1);
    }
    public float GetCharacterInterpolatedDatashootPoseWeight(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatashootPoseWeight * 0.001f;
    }
    public float GetCharacterInterpolatedDatashootPoseWeight()
    {
        return CharacterInterpolatedDatashootPoseWeight * 0.001f;
    }
    public void SetCharacterInterpolatedDatashootPoseWeight(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatashootPoseWeight = (int)(val * 1000);
    }
    public void SetCharacterInterpolatedDatashootPoseWeight(float val)
    {
        CharacterInterpolatedDatashootPoseWeight = (int)(val * 1000);
    }
    public float2 GetCharacterInterpolatedDatalocomotionVector(GhostDeserializerState deserializerState)
    {
        return GetCharacterInterpolatedDatalocomotionVector();
    }
    public float2 GetCharacterInterpolatedDatalocomotionVector()
    {
        return new float2(CharacterInterpolatedDatalocomotionVectorX * 0.001f, CharacterInterpolatedDatalocomotionVectorY * 0.001f);
    }
    public void SetCharacterInterpolatedDatalocomotionVector(float2 val, GhostSerializerState serializerState)
    {
        SetCharacterInterpolatedDatalocomotionVector(val);
    }
    public void SetCharacterInterpolatedDatalocomotionVector(float2 val)
    {
        CharacterInterpolatedDatalocomotionVectorX = (int)(val.x * 1000);
        CharacterInterpolatedDatalocomotionVectorY = (int)(val.y * 1000);
    }
    public float GetCharacterInterpolatedDatalocomotionPhase(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatalocomotionPhase * 0.001f;
    }
    public float GetCharacterInterpolatedDatalocomotionPhase()
    {
        return CharacterInterpolatedDatalocomotionPhase * 0.001f;
    }
    public void SetCharacterInterpolatedDatalocomotionPhase(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatalocomotionPhase = (int)(val * 1000);
    }
    public void SetCharacterInterpolatedDatalocomotionPhase(float val)
    {
        CharacterInterpolatedDatalocomotionPhase = (int)(val * 1000);
    }
    public float GetCharacterInterpolatedDatabanking(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatabanking * 0.001f;
    }
    public float GetCharacterInterpolatedDatabanking()
    {
        return CharacterInterpolatedDatabanking * 0.001f;
    }
    public void SetCharacterInterpolatedDatabanking(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatabanking = (int)(val * 1000);
    }
    public void SetCharacterInterpolatedDatabanking(float val)
    {
        CharacterInterpolatedDatabanking = (int)(val * 1000);
    }
    public float GetCharacterInterpolatedDatalandAnticWeight(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatalandAnticWeight * 0.01f;
    }
    public float GetCharacterInterpolatedDatalandAnticWeight()
    {
        return CharacterInterpolatedDatalandAnticWeight * 0.01f;
    }
    public void SetCharacterInterpolatedDatalandAnticWeight(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatalandAnticWeight = (int)(val * 100);
    }
    public void SetCharacterInterpolatedDatalandAnticWeight(float val)
    {
        CharacterInterpolatedDatalandAnticWeight = (int)(val * 100);
    }
    public float GetCharacterInterpolatedDataturnStartAngle(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDataturnStartAngle * 1f;
    }
    public float GetCharacterInterpolatedDataturnStartAngle()
    {
        return CharacterInterpolatedDataturnStartAngle * 1f;
    }
    public void SetCharacterInterpolatedDataturnStartAngle(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDataturnStartAngle = (int)(val * 1);
    }
    public void SetCharacterInterpolatedDataturnStartAngle(float val)
    {
        CharacterInterpolatedDataturnStartAngle = (int)(val * 1);
    }
    public short GetCharacterInterpolatedDataturnDirection(GhostDeserializerState deserializerState)
    {
        return (short)CharacterInterpolatedDataturnDirection;
    }
    public short GetCharacterInterpolatedDataturnDirection()
    {
        return (short)CharacterInterpolatedDataturnDirection;
    }
    public void SetCharacterInterpolatedDataturnDirection(short val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDataturnDirection = (int)val;
    }
    public void SetCharacterInterpolatedDataturnDirection(short val)
    {
        CharacterInterpolatedDataturnDirection = (int)val;
    }
    public float GetCharacterInterpolatedDatasquashTime(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatasquashTime * 0.01f;
    }
    public float GetCharacterInterpolatedDatasquashTime()
    {
        return CharacterInterpolatedDatasquashTime * 0.01f;
    }
    public void SetCharacterInterpolatedDatasquashTime(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatasquashTime = (int)(val * 100);
    }
    public void SetCharacterInterpolatedDatasquashTime(float val)
    {
        CharacterInterpolatedDatasquashTime = (int)(val * 100);
    }
    public float GetCharacterInterpolatedDatasquashWeight(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatasquashWeight * 0.01f;
    }
    public float GetCharacterInterpolatedDatasquashWeight()
    {
        return CharacterInterpolatedDatasquashWeight * 0.01f;
    }
    public void SetCharacterInterpolatedDatasquashWeight(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatasquashWeight = (int)(val * 100);
    }
    public void SetCharacterInterpolatedDatasquashWeight(float val)
    {
        CharacterInterpolatedDatasquashWeight = (int)(val * 100);
    }
    public float GetCharacterInterpolatedDatainAirTime(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatainAirTime * 0.01f;
    }
    public float GetCharacterInterpolatedDatainAirTime()
    {
        return CharacterInterpolatedDatainAirTime * 0.01f;
    }
    public void SetCharacterInterpolatedDatainAirTime(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatainAirTime = (int)(val * 100);
    }
    public void SetCharacterInterpolatedDatainAirTime(float val)
    {
        CharacterInterpolatedDatainAirTime = (int)(val * 100);
    }
    public float GetCharacterInterpolatedDatajumpTime(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatajumpTime * 0.01f;
    }
    public float GetCharacterInterpolatedDatajumpTime()
    {
        return CharacterInterpolatedDatajumpTime * 0.01f;
    }
    public void SetCharacterInterpolatedDatajumpTime(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatajumpTime = (int)(val * 100);
    }
    public void SetCharacterInterpolatedDatajumpTime(float val)
    {
        CharacterInterpolatedDatajumpTime = (int)(val * 100);
    }
    public float GetCharacterInterpolatedDatasimpleTime(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatasimpleTime * 0.01f;
    }
    public float GetCharacterInterpolatedDatasimpleTime()
    {
        return CharacterInterpolatedDatasimpleTime * 0.01f;
    }
    public void SetCharacterInterpolatedDatasimpleTime(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatasimpleTime = (int)(val * 100);
    }
    public void SetCharacterInterpolatedDatasimpleTime(float val)
    {
        CharacterInterpolatedDatasimpleTime = (int)(val * 100);
    }
    public float2 GetCharacterInterpolatedDatafootIkOffset(GhostDeserializerState deserializerState)
    {
        return GetCharacterInterpolatedDatafootIkOffset();
    }
    public float2 GetCharacterInterpolatedDatafootIkOffset()
    {
        return new float2(CharacterInterpolatedDatafootIkOffsetX * 0.01f, CharacterInterpolatedDatafootIkOffsetY * 0.01f);
    }
    public void SetCharacterInterpolatedDatafootIkOffset(float2 val, GhostSerializerState serializerState)
    {
        SetCharacterInterpolatedDatafootIkOffset(val);
    }
    public void SetCharacterInterpolatedDatafootIkOffset(float2 val)
    {
        CharacterInterpolatedDatafootIkOffsetX = (int)(val.x * 100);
        CharacterInterpolatedDatafootIkOffsetY = (int)(val.y * 100);
    }
    public float3 GetCharacterInterpolatedDatafootIkNormalLeft(GhostDeserializerState deserializerState)
    {
        return GetCharacterInterpolatedDatafootIkNormalLeft();
    }
    public float3 GetCharacterInterpolatedDatafootIkNormalLeft()
    {
        return new float3(CharacterInterpolatedDatafootIkNormalLeftX * 0.01f, CharacterInterpolatedDatafootIkNormalLeftY * 0.01f, CharacterInterpolatedDatafootIkNormalLeftZ * 0.01f);
    }
    public void SetCharacterInterpolatedDatafootIkNormalLeft(float3 val, GhostSerializerState serializerState)
    {
        SetCharacterInterpolatedDatafootIkNormalLeft(val);
    }
    public void SetCharacterInterpolatedDatafootIkNormalLeft(float3 val)
    {
        CharacterInterpolatedDatafootIkNormalLeftX = (int)(val.x * 100);
        CharacterInterpolatedDatafootIkNormalLeftY = (int)(val.y * 100);
        CharacterInterpolatedDatafootIkNormalLeftZ = (int)(val.z * 100);
    }
    public float3 GetCharacterInterpolatedDatafootIkNormalRight(GhostDeserializerState deserializerState)
    {
        return GetCharacterInterpolatedDatafootIkNormalRight();
    }
    public float3 GetCharacterInterpolatedDatafootIkNormalRight()
    {
        return new float3(CharacterInterpolatedDatafootIkNormalRightX * 0.01f, CharacterInterpolatedDatafootIkNormalRightY * 0.01f, CharacterInterpolatedDatafootIkNormalRightZ * 0.01f);
    }
    public void SetCharacterInterpolatedDatafootIkNormalRight(float3 val, GhostSerializerState serializerState)
    {
        SetCharacterInterpolatedDatafootIkNormalRight(val);
    }
    public void SetCharacterInterpolatedDatafootIkNormalRight(float3 val)
    {
        CharacterInterpolatedDatafootIkNormalRightX = (int)(val.x * 100);
        CharacterInterpolatedDatafootIkNormalRightY = (int)(val.y * 100);
        CharacterInterpolatedDatafootIkNormalRightZ = (int)(val.z * 100);
    }
    public float GetCharacterInterpolatedDatafootIkWeight(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatafootIkWeight * 0.01f;
    }
    public float GetCharacterInterpolatedDatafootIkWeight()
    {
        return CharacterInterpolatedDatafootIkWeight * 0.01f;
    }
    public void SetCharacterInterpolatedDatafootIkWeight(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatafootIkWeight = (int)(val * 100);
    }
    public void SetCharacterInterpolatedDatafootIkWeight(float val)
    {
        CharacterInterpolatedDatafootIkWeight = (int)(val * 100);
    }
    public float GetCharacterInterpolatedDatablendOutAim(GhostDeserializerState deserializerState)
    {
        return CharacterInterpolatedDatablendOutAim * 0.01f;
    }
    public float GetCharacterInterpolatedDatablendOutAim()
    {
        return CharacterInterpolatedDatablendOutAim * 0.01f;
    }
    public void SetCharacterInterpolatedDatablendOutAim(float val, GhostSerializerState serializerState)
    {
        CharacterInterpolatedDatablendOutAim = (int)(val * 100);
    }
    public void SetCharacterInterpolatedDatablendOutAim(float val)
    {
        CharacterInterpolatedDatablendOutAim = (int)(val * 100);
    }
    public int GetCharacterPredictedDatatick(GhostDeserializerState deserializerState)
    {
        return (int)CharacterPredictedDatatick;
    }
    public int GetCharacterPredictedDatatick()
    {
        return (int)CharacterPredictedDatatick;
    }
    public void SetCharacterPredictedDatatick(int val, GhostSerializerState serializerState)
    {
        CharacterPredictedDatatick = (int)val;
    }
    public void SetCharacterPredictedDatatick(int val)
    {
        CharacterPredictedDatatick = (int)val;
    }
    public float3 GetCharacterPredictedDataposition(GhostDeserializerState deserializerState)
    {
        return GetCharacterPredictedDataposition();
    }
    public float3 GetCharacterPredictedDataposition()
    {
        return new float3(CharacterPredictedDatapositionX, CharacterPredictedDatapositionY, CharacterPredictedDatapositionZ);
    }
    public void SetCharacterPredictedDataposition(float3 val, GhostSerializerState serializerState)
    {
        SetCharacterPredictedDataposition(val);
    }
    public void SetCharacterPredictedDataposition(float3 val)
    {
        CharacterPredictedDatapositionX = val.x;
        CharacterPredictedDatapositionY = val.y;
        CharacterPredictedDatapositionZ = val.z;
    }
    public float3 GetCharacterPredictedDatavelocity(GhostDeserializerState deserializerState)
    {
        return GetCharacterPredictedDatavelocity();
    }
    public float3 GetCharacterPredictedDatavelocity()
    {
        return new float3(CharacterPredictedDatavelocityX, CharacterPredictedDatavelocityY, CharacterPredictedDatavelocityZ);
    }
    public void SetCharacterPredictedDatavelocity(float3 val, GhostSerializerState serializerState)
    {
        SetCharacterPredictedDatavelocity(val);
    }
    public void SetCharacterPredictedDatavelocity(float3 val)
    {
        CharacterPredictedDatavelocityX = val.x;
        CharacterPredictedDatavelocityY = val.y;
        CharacterPredictedDatavelocityZ = val.z;
    }
    public bool GetCharacterPredictedDatasprinting(GhostDeserializerState deserializerState)
    {
        return CharacterPredictedDatasprinting!=0;
    }
    public bool GetCharacterPredictedDatasprinting()
    {
        return CharacterPredictedDatasprinting!=0;
    }
    public void SetCharacterPredictedDatasprinting(bool val, GhostSerializerState serializerState)
    {
        CharacterPredictedDatasprinting = val?1u:0;
    }
    public void SetCharacterPredictedDatasprinting(bool val)
    {
        CharacterPredictedDatasprinting = val?1u:0;
    }
    public CameraProfile GetCharacterPredictedDatacameraProfile(GhostDeserializerState deserializerState)
    {
        return (CameraProfile)CharacterPredictedDatacameraProfile;
    }
    public CameraProfile GetCharacterPredictedDatacameraProfile()
    {
        return (CameraProfile)CharacterPredictedDatacameraProfile;
    }
    public void SetCharacterPredictedDatacameraProfile(CameraProfile val, GhostSerializerState serializerState)
    {
        CharacterPredictedDatacameraProfile = (int)val;
    }
    public void SetCharacterPredictedDatacameraProfile(CameraProfile val)
    {
        CharacterPredictedDatacameraProfile = (int)val;
    }
    public int GetCharacterPredictedDatadamageTick(GhostDeserializerState deserializerState)
    {
        return (int)CharacterPredictedDatadamageTick;
    }
    public int GetCharacterPredictedDatadamageTick()
    {
        return (int)CharacterPredictedDatadamageTick;
    }
    public void SetCharacterPredictedDatadamageTick(int val, GhostSerializerState serializerState)
    {
        CharacterPredictedDatadamageTick = (int)val;
    }
    public void SetCharacterPredictedDatadamageTick(int val)
    {
        CharacterPredictedDatadamageTick = (int)val;
    }
    public float3 GetCharacterPredictedDatadamageDirection(GhostDeserializerState deserializerState)
    {
        return GetCharacterPredictedDatadamageDirection();
    }
    public float3 GetCharacterPredictedDatadamageDirection()
    {
        return new float3(CharacterPredictedDatadamageDirectionX * 0.001f, CharacterPredictedDatadamageDirectionY * 0.001f, CharacterPredictedDatadamageDirectionZ * 0.001f);
    }
    public void SetCharacterPredictedDatadamageDirection(float3 val, GhostSerializerState serializerState)
    {
        SetCharacterPredictedDatadamageDirection(val);
    }
    public void SetCharacterPredictedDatadamageDirection(float3 val)
    {
        CharacterPredictedDatadamageDirectionX = (int)(val.x * 1000);
        CharacterPredictedDatadamageDirectionY = (int)(val.y * 1000);
        CharacterPredictedDatadamageDirectionZ = (int)(val.z * 1000);
    }
    public int GetCharacterReplicatedDataheroTypeIndex(GhostDeserializerState deserializerState)
    {
        return (int)CharacterReplicatedDataheroTypeIndex;
    }
    public int GetCharacterReplicatedDataheroTypeIndex()
    {
        return (int)CharacterReplicatedDataheroTypeIndex;
    }
    public void SetCharacterReplicatedDataheroTypeIndex(int val, GhostSerializerState serializerState)
    {
        CharacterReplicatedDataheroTypeIndex = (int)val;
    }
    public void SetCharacterReplicatedDataheroTypeIndex(int val)
    {
        CharacterReplicatedDataheroTypeIndex = (int)val;
    }
    public float3 GetCharacterControllerGroundSupportDataSurfaceNormal(GhostDeserializerState deserializerState)
    {
        return GetCharacterControllerGroundSupportDataSurfaceNormal();
    }
    public float3 GetCharacterControllerGroundSupportDataSurfaceNormal()
    {
        return new float3(CharacterControllerGroundSupportDataSurfaceNormalX, CharacterControllerGroundSupportDataSurfaceNormalY, CharacterControllerGroundSupportDataSurfaceNormalZ);
    }
    public void SetCharacterControllerGroundSupportDataSurfaceNormal(float3 val, GhostSerializerState serializerState)
    {
        SetCharacterControllerGroundSupportDataSurfaceNormal(val);
    }
    public void SetCharacterControllerGroundSupportDataSurfaceNormal(float3 val)
    {
        CharacterControllerGroundSupportDataSurfaceNormalX = val.x;
        CharacterControllerGroundSupportDataSurfaceNormalY = val.y;
        CharacterControllerGroundSupportDataSurfaceNormalZ = val.z;
    }
    public float3 GetCharacterControllerGroundSupportDataSurfaceVelocity(GhostDeserializerState deserializerState)
    {
        return GetCharacterControllerGroundSupportDataSurfaceVelocity();
    }
    public float3 GetCharacterControllerGroundSupportDataSurfaceVelocity()
    {
        return new float3(CharacterControllerGroundSupportDataSurfaceVelocityX, CharacterControllerGroundSupportDataSurfaceVelocityY, CharacterControllerGroundSupportDataSurfaceVelocityZ);
    }
    public void SetCharacterControllerGroundSupportDataSurfaceVelocity(float3 val, GhostSerializerState serializerState)
    {
        SetCharacterControllerGroundSupportDataSurfaceVelocity(val);
    }
    public void SetCharacterControllerGroundSupportDataSurfaceVelocity(float3 val)
    {
        CharacterControllerGroundSupportDataSurfaceVelocityX = val.x;
        CharacterControllerGroundSupportDataSurfaceVelocityY = val.y;
        CharacterControllerGroundSupportDataSurfaceVelocityZ = val.z;
    }
    public CharacterControllerUtilities.CharacterSupportState GetCharacterControllerGroundSupportDataSupportedState(GhostDeserializerState deserializerState)
    {
        return (CharacterControllerUtilities.CharacterSupportState)CharacterControllerGroundSupportDataSupportedState;
    }
    public CharacterControllerUtilities.CharacterSupportState GetCharacterControllerGroundSupportDataSupportedState()
    {
        return (CharacterControllerUtilities.CharacterSupportState)CharacterControllerGroundSupportDataSupportedState;
    }
    public void SetCharacterControllerGroundSupportDataSupportedState(CharacterControllerUtilities.CharacterSupportState val, GhostSerializerState serializerState)
    {
        CharacterControllerGroundSupportDataSupportedState = (int)val;
    }
    public void SetCharacterControllerGroundSupportDataSupportedState(CharacterControllerUtilities.CharacterSupportState val)
    {
        CharacterControllerGroundSupportDataSupportedState = (int)val;
    }
    public float3 GetCharacterControllerMoveResultMoveResult(GhostDeserializerState deserializerState)
    {
        return GetCharacterControllerMoveResultMoveResult();
    }
    public float3 GetCharacterControllerMoveResultMoveResult()
    {
        return new float3(CharacterControllerMoveResultMoveResultX, CharacterControllerMoveResultMoveResultY, CharacterControllerMoveResultMoveResultZ);
    }
    public void SetCharacterControllerMoveResultMoveResult(float3 val, GhostSerializerState serializerState)
    {
        SetCharacterControllerMoveResultMoveResult(val);
    }
    public void SetCharacterControllerMoveResultMoveResult(float3 val)
    {
        CharacterControllerMoveResultMoveResultX = val.x;
        CharacterControllerMoveResultMoveResultY = val.y;
        CharacterControllerMoveResultMoveResultZ = val.z;
    }
    public float3 GetCharacterControllerVelocityVelocity(GhostDeserializerState deserializerState)
    {
        return GetCharacterControllerVelocityVelocity();
    }
    public float3 GetCharacterControllerVelocityVelocity()
    {
        return new float3(CharacterControllerVelocityVelocityX, CharacterControllerVelocityVelocityY, CharacterControllerVelocityVelocityZ);
    }
    public void SetCharacterControllerVelocityVelocity(float3 val, GhostSerializerState serializerState)
    {
        SetCharacterControllerVelocityVelocity(val);
    }
    public void SetCharacterControllerVelocityVelocity(float3 val)
    {
        CharacterControllerVelocityVelocityX = val.x;
        CharacterControllerVelocityVelocityY = val.y;
        CharacterControllerVelocityVelocityZ = val.z;
    }
    public float GetHealthStateDatahealth(GhostDeserializerState deserializerState)
    {
        return HealthStateDatahealth * 1f;
    }
    public float GetHealthStateDatahealth()
    {
        return HealthStateDatahealth * 1f;
    }
    public void SetHealthStateDatahealth(float val, GhostSerializerState serializerState)
    {
        HealthStateDatahealth = (int)(val * 1);
    }
    public void SetHealthStateDatahealth(float val)
    {
        HealthStateDatahealth = (int)(val * 1);
    }
    public sbyte GetInventoryStateactiveSlot(GhostDeserializerState deserializerState)
    {
        return (sbyte)InventoryStateactiveSlot;
    }
    public sbyte GetInventoryStateactiveSlot()
    {
        return (sbyte)InventoryStateactiveSlot;
    }
    public void SetInventoryStateactiveSlot(sbyte val, GhostSerializerState serializerState)
    {
        InventoryStateactiveSlot = (int)val;
    }
    public void SetInventoryStateactiveSlot(sbyte val)
    {
        InventoryStateactiveSlot = (int)val;
    }
    public int GetPlayerOwnerPlayerIdValue(GhostDeserializerState deserializerState)
    {
        return (int)PlayerOwnerPlayerIdValue;
    }
    public int GetPlayerOwnerPlayerIdValue()
    {
        return (int)PlayerOwnerPlayerIdValue;
    }
    public void SetPlayerOwnerPlayerIdValue(int val, GhostSerializerState serializerState)
    {
        PlayerOwnerPlayerIdValue = (int)val;
    }
    public void SetPlayerOwnerPlayerIdValue(int val)
    {
        PlayerOwnerPlayerIdValue = (int)val;
    }
    public int GetPlayerControlledStateresetCommandTick(GhostDeserializerState deserializerState)
    {
        return (int)PlayerControlledStateresetCommandTick;
    }
    public int GetPlayerControlledStateresetCommandTick()
    {
        return (int)PlayerControlledStateresetCommandTick;
    }
    public void SetPlayerControlledStateresetCommandTick(int val, GhostSerializerState serializerState)
    {
        PlayerControlledStateresetCommandTick = (int)val;
    }
    public void SetPlayerControlledStateresetCommandTick(int val)
    {
        PlayerControlledStateresetCommandTick = (int)val;
    }
    public float GetPlayerControlledStateresetCommandLookYaw(GhostDeserializerState deserializerState)
    {
        return PlayerControlledStateresetCommandLookYaw * 0.1f;
    }
    public float GetPlayerControlledStateresetCommandLookYaw()
    {
        return PlayerControlledStateresetCommandLookYaw * 0.1f;
    }
    public void SetPlayerControlledStateresetCommandLookYaw(float val, GhostSerializerState serializerState)
    {
        PlayerControlledStateresetCommandLookYaw = (int)(val * 10);
    }
    public void SetPlayerControlledStateresetCommandLookYaw(float val)
    {
        PlayerControlledStateresetCommandLookYaw = (int)(val * 10);
    }
    public float GetPlayerControlledStateresetCommandLookPitch(GhostDeserializerState deserializerState)
    {
        return PlayerControlledStateresetCommandLookPitch * 0.1f;
    }
    public float GetPlayerControlledStateresetCommandLookPitch()
    {
        return PlayerControlledStateresetCommandLookPitch * 0.1f;
    }
    public void SetPlayerControlledStateresetCommandLookPitch(float val, GhostSerializerState serializerState)
    {
        PlayerControlledStateresetCommandLookPitch = (int)(val * 10);
    }
    public void SetPlayerControlledStateresetCommandLookPitch(float val)
    {
        PlayerControlledStateresetCommandLookPitch = (int)(val * 10);
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
    public AbilityMovement.LocoState GetChild0AbilityMovementInterpolatedStatecharLocoState(GhostDeserializerState deserializerState)
    {
        return (AbilityMovement.LocoState)Child0AbilityMovementInterpolatedStatecharLocoState;
    }
    public AbilityMovement.LocoState GetChild0AbilityMovementInterpolatedStatecharLocoState()
    {
        return (AbilityMovement.LocoState)Child0AbilityMovementInterpolatedStatecharLocoState;
    }
    public void SetChild0AbilityMovementInterpolatedStatecharLocoState(AbilityMovement.LocoState val, GhostSerializerState serializerState)
    {
        Child0AbilityMovementInterpolatedStatecharLocoState = (int)val;
    }
    public void SetChild0AbilityMovementInterpolatedStatecharLocoState(AbilityMovement.LocoState val)
    {
        Child0AbilityMovementInterpolatedStatecharLocoState = (int)val;
    }
    public int GetChild0AbilityMovementInterpolatedStatecharLocoTick(GhostDeserializerState deserializerState)
    {
        return (int)Child0AbilityMovementInterpolatedStatecharLocoTick;
    }
    public int GetChild0AbilityMovementInterpolatedStatecharLocoTick()
    {
        return (int)Child0AbilityMovementInterpolatedStatecharLocoTick;
    }
    public void SetChild0AbilityMovementInterpolatedStatecharLocoTick(int val, GhostSerializerState serializerState)
    {
        Child0AbilityMovementInterpolatedStatecharLocoTick = (int)val;
    }
    public void SetChild0AbilityMovementInterpolatedStatecharLocoTick(int val)
    {
        Child0AbilityMovementInterpolatedStatecharLocoTick = (int)val;
    }
    public bool GetChild0AbilityMovementInterpolatedStatecrouching(GhostDeserializerState deserializerState)
    {
        return Child0AbilityMovementInterpolatedStatecrouching!=0;
    }
    public bool GetChild0AbilityMovementInterpolatedStatecrouching()
    {
        return Child0AbilityMovementInterpolatedStatecrouching!=0;
    }
    public void SetChild0AbilityMovementInterpolatedStatecrouching(bool val, GhostSerializerState serializerState)
    {
        Child0AbilityMovementInterpolatedStatecrouching = val?1u:0;
    }
    public void SetChild0AbilityMovementInterpolatedStatecrouching(bool val)
    {
        Child0AbilityMovementInterpolatedStatecrouching = val?1u:0;
    }
    public AbilityMovement.LocoState GetChild0AbilityMovementPredictedStatelocoState(GhostDeserializerState deserializerState)
    {
        return (AbilityMovement.LocoState)Child0AbilityMovementPredictedStatelocoState;
    }
    public AbilityMovement.LocoState GetChild0AbilityMovementPredictedStatelocoState()
    {
        return (AbilityMovement.LocoState)Child0AbilityMovementPredictedStatelocoState;
    }
    public void SetChild0AbilityMovementPredictedStatelocoState(AbilityMovement.LocoState val, GhostSerializerState serializerState)
    {
        Child0AbilityMovementPredictedStatelocoState = (int)val;
    }
    public void SetChild0AbilityMovementPredictedStatelocoState(AbilityMovement.LocoState val)
    {
        Child0AbilityMovementPredictedStatelocoState = (int)val;
    }
    public int GetChild0AbilityMovementPredictedStatelocoStartTick(GhostDeserializerState deserializerState)
    {
        return (int)Child0AbilityMovementPredictedStatelocoStartTick;
    }
    public int GetChild0AbilityMovementPredictedStatelocoStartTick()
    {
        return (int)Child0AbilityMovementPredictedStatelocoStartTick;
    }
    public void SetChild0AbilityMovementPredictedStatelocoStartTick(int val, GhostSerializerState serializerState)
    {
        Child0AbilityMovementPredictedStatelocoStartTick = (int)val;
    }
    public void SetChild0AbilityMovementPredictedStatelocoStartTick(int val)
    {
        Child0AbilityMovementPredictedStatelocoStartTick = (int)val;
    }
    public int GetChild0AbilityMovementPredictedStatejumpCount(GhostDeserializerState deserializerState)
    {
        return (int)Child0AbilityMovementPredictedStatejumpCount;
    }
    public int GetChild0AbilityMovementPredictedStatejumpCount()
    {
        return (int)Child0AbilityMovementPredictedStatejumpCount;
    }
    public void SetChild0AbilityMovementPredictedStatejumpCount(int val, GhostSerializerState serializerState)
    {
        Child0AbilityMovementPredictedStatejumpCount = (int)val;
    }
    public void SetChild0AbilityMovementPredictedStatejumpCount(int val)
    {
        Child0AbilityMovementPredictedStatejumpCount = (int)val;
    }
    public bool GetChild0AbilityMovementPredictedStatecrouching(GhostDeserializerState deserializerState)
    {
        return Child0AbilityMovementPredictedStatecrouching!=0;
    }
    public bool GetChild0AbilityMovementPredictedStatecrouching()
    {
        return Child0AbilityMovementPredictedStatecrouching!=0;
    }
    public void SetChild0AbilityMovementPredictedStatecrouching(bool val, GhostSerializerState serializerState)
    {
        Child0AbilityMovementPredictedStatecrouching = val?1u:0;
    }
    public void SetChild0AbilityMovementPredictedStatecrouching(bool val)
    {
        Child0AbilityMovementPredictedStatecrouching = val?1u:0;
    }
    public Ability.AbilityControl.State GetChild1AbilityAbilityControlbehaviorState(GhostDeserializerState deserializerState)
    {
        return (Ability.AbilityControl.State)Child1AbilityAbilityControlbehaviorState;
    }
    public Ability.AbilityControl.State GetChild1AbilityAbilityControlbehaviorState()
    {
        return (Ability.AbilityControl.State)Child1AbilityAbilityControlbehaviorState;
    }
    public void SetChild1AbilityAbilityControlbehaviorState(Ability.AbilityControl.State val, GhostSerializerState serializerState)
    {
        Child1AbilityAbilityControlbehaviorState = (int)val;
    }
    public void SetChild1AbilityAbilityControlbehaviorState(Ability.AbilityControl.State val)
    {
        Child1AbilityAbilityControlbehaviorState = (int)val;
    }
    public bool GetChild1AbilityAbilityControlrequestDeactivate(GhostDeserializerState deserializerState)
    {
        return Child1AbilityAbilityControlrequestDeactivate!=0;
    }
    public bool GetChild1AbilityAbilityControlrequestDeactivate()
    {
        return Child1AbilityAbilityControlrequestDeactivate!=0;
    }
    public void SetChild1AbilityAbilityControlrequestDeactivate(bool val, GhostSerializerState serializerState)
    {
        Child1AbilityAbilityControlrequestDeactivate = val?1u:0;
    }
    public void SetChild1AbilityAbilityControlrequestDeactivate(bool val)
    {
        Child1AbilityAbilityControlrequestDeactivate = val?1u:0;
    }
    public int GetChild1AbilitySprintPredictedStateactive(GhostDeserializerState deserializerState)
    {
        return (int)Child1AbilitySprintPredictedStateactive;
    }
    public int GetChild1AbilitySprintPredictedStateactive()
    {
        return (int)Child1AbilitySprintPredictedStateactive;
    }
    public void SetChild1AbilitySprintPredictedStateactive(int val, GhostSerializerState serializerState)
    {
        Child1AbilitySprintPredictedStateactive = (int)val;
    }
    public void SetChild1AbilitySprintPredictedStateactive(int val)
    {
        Child1AbilitySprintPredictedStateactive = (int)val;
    }
    public int GetChild1AbilitySprintPredictedStateterminating(GhostDeserializerState deserializerState)
    {
        return (int)Child1AbilitySprintPredictedStateterminating;
    }
    public int GetChild1AbilitySprintPredictedStateterminating()
    {
        return (int)Child1AbilitySprintPredictedStateterminating;
    }
    public void SetChild1AbilitySprintPredictedStateterminating(int val, GhostSerializerState serializerState)
    {
        Child1AbilitySprintPredictedStateterminating = (int)val;
    }
    public void SetChild1AbilitySprintPredictedStateterminating(int val)
    {
        Child1AbilitySprintPredictedStateterminating = (int)val;
    }
    public int GetChild1AbilitySprintPredictedStateterminateStartTick(GhostDeserializerState deserializerState)
    {
        return (int)Child1AbilitySprintPredictedStateterminateStartTick;
    }
    public int GetChild1AbilitySprintPredictedStateterminateStartTick()
    {
        return (int)Child1AbilitySprintPredictedStateterminateStartTick;
    }
    public void SetChild1AbilitySprintPredictedStateterminateStartTick(int val, GhostSerializerState serializerState)
    {
        Child1AbilitySprintPredictedStateterminateStartTick = (int)val;
    }
    public void SetChild1AbilitySprintPredictedStateterminateStartTick(int val)
    {
        Child1AbilitySprintPredictedStateterminateStartTick = (int)val;
    }
    public Ability.AbilityControl.State GetChild2AbilityAbilityControlbehaviorState(GhostDeserializerState deserializerState)
    {
        return (Ability.AbilityControl.State)Child2AbilityAbilityControlbehaviorState;
    }
    public Ability.AbilityControl.State GetChild2AbilityAbilityControlbehaviorState()
    {
        return (Ability.AbilityControl.State)Child2AbilityAbilityControlbehaviorState;
    }
    public void SetChild2AbilityAbilityControlbehaviorState(Ability.AbilityControl.State val, GhostSerializerState serializerState)
    {
        Child2AbilityAbilityControlbehaviorState = (int)val;
    }
    public void SetChild2AbilityAbilityControlbehaviorState(Ability.AbilityControl.State val)
    {
        Child2AbilityAbilityControlbehaviorState = (int)val;
    }
    public bool GetChild2AbilityAbilityControlrequestDeactivate(GhostDeserializerState deserializerState)
    {
        return Child2AbilityAbilityControlrequestDeactivate!=0;
    }
    public bool GetChild2AbilityAbilityControlrequestDeactivate()
    {
        return Child2AbilityAbilityControlrequestDeactivate!=0;
    }
    public void SetChild2AbilityAbilityControlrequestDeactivate(bool val, GhostSerializerState serializerState)
    {
        Child2AbilityAbilityControlrequestDeactivate = val?1u:0;
    }
    public void SetChild2AbilityAbilityControlrequestDeactivate(bool val)
    {
        Child2AbilityAbilityControlrequestDeactivate = val?1u:0;
    }
    public Ability.AbilityControl.State GetChild3AbilityAbilityControlbehaviorState(GhostDeserializerState deserializerState)
    {
        return (Ability.AbilityControl.State)Child3AbilityAbilityControlbehaviorState;
    }
    public Ability.AbilityControl.State GetChild3AbilityAbilityControlbehaviorState()
    {
        return (Ability.AbilityControl.State)Child3AbilityAbilityControlbehaviorState;
    }
    public void SetChild3AbilityAbilityControlbehaviorState(Ability.AbilityControl.State val, GhostSerializerState serializerState)
    {
        Child3AbilityAbilityControlbehaviorState = (int)val;
    }
    public void SetChild3AbilityAbilityControlbehaviorState(Ability.AbilityControl.State val)
    {
        Child3AbilityAbilityControlbehaviorState = (int)val;
    }
    public bool GetChild3AbilityAbilityControlrequestDeactivate(GhostDeserializerState deserializerState)
    {
        return Child3AbilityAbilityControlrequestDeactivate!=0;
    }
    public bool GetChild3AbilityAbilityControlrequestDeactivate()
    {
        return Child3AbilityAbilityControlrequestDeactivate!=0;
    }
    public void SetChild3AbilityAbilityControlrequestDeactivate(bool val, GhostSerializerState serializerState)
    {
        Child3AbilityAbilityControlrequestDeactivate = val?1u:0;
    }
    public void SetChild3AbilityAbilityControlrequestDeactivate(bool val)
    {
        Child3AbilityAbilityControlrequestDeactivate = val?1u:0;
    }

    public void PredictDelta(uint tick, ref Char_TerraformerSnapshotData baseline1, ref Char_TerraformerSnapshotData baseline2)
    {
        var predictor = new GhostDeltaPredictor(tick, this.tick, baseline1.tick, baseline2.tick);
        CharacterInterpolatedDataPositionX = predictor.PredictInt(CharacterInterpolatedDataPositionX, baseline1.CharacterInterpolatedDataPositionX, baseline2.CharacterInterpolatedDataPositionX);
        CharacterInterpolatedDataPositionY = predictor.PredictInt(CharacterInterpolatedDataPositionY, baseline1.CharacterInterpolatedDataPositionY, baseline2.CharacterInterpolatedDataPositionY);
        CharacterInterpolatedDataPositionZ = predictor.PredictInt(CharacterInterpolatedDataPositionZ, baseline1.CharacterInterpolatedDataPositionZ, baseline2.CharacterInterpolatedDataPositionZ);
        CharacterInterpolatedDatarotation = predictor.PredictInt(CharacterInterpolatedDatarotation, baseline1.CharacterInterpolatedDatarotation, baseline2.CharacterInterpolatedDatarotation);
        CharacterInterpolatedDataaimYaw = predictor.PredictInt(CharacterInterpolatedDataaimYaw, baseline1.CharacterInterpolatedDataaimYaw, baseline2.CharacterInterpolatedDataaimYaw);
        CharacterInterpolatedDataaimPitch = predictor.PredictInt(CharacterInterpolatedDataaimPitch, baseline1.CharacterInterpolatedDataaimPitch, baseline2.CharacterInterpolatedDataaimPitch);
        CharacterInterpolatedDatamoveYaw = predictor.PredictInt(CharacterInterpolatedDatamoveYaw, baseline1.CharacterInterpolatedDatamoveYaw, baseline2.CharacterInterpolatedDatamoveYaw);
        CharacterInterpolatedDatacharAction = predictor.PredictInt(CharacterInterpolatedDatacharAction, baseline1.CharacterInterpolatedDatacharAction, baseline2.CharacterInterpolatedDatacharAction);
        CharacterInterpolatedDatacharActionTick = predictor.PredictInt(CharacterInterpolatedDatacharActionTick, baseline1.CharacterInterpolatedDatacharActionTick, baseline2.CharacterInterpolatedDatacharActionTick);
        CharacterInterpolatedDatadamageTick = predictor.PredictInt(CharacterInterpolatedDatadamageTick, baseline1.CharacterInterpolatedDatadamageTick, baseline2.CharacterInterpolatedDatadamageTick);
        CharacterInterpolatedDatadamageDirection = predictor.PredictInt(CharacterInterpolatedDatadamageDirection, baseline1.CharacterInterpolatedDatadamageDirection, baseline2.CharacterInterpolatedDatadamageDirection);
        CharacterInterpolatedDatasprinting = (uint)predictor.PredictInt((int)CharacterInterpolatedDatasprinting, (int)baseline1.CharacterInterpolatedDatasprinting, (int)baseline2.CharacterInterpolatedDatasprinting);
        CharacterInterpolatedDatasprintWeight = predictor.PredictInt(CharacterInterpolatedDatasprintWeight, baseline1.CharacterInterpolatedDatasprintWeight, baseline2.CharacterInterpolatedDatasprintWeight);
        CharacterInterpolatedDatacrouchWeight = predictor.PredictInt(CharacterInterpolatedDatacrouchWeight, baseline1.CharacterInterpolatedDatacrouchWeight, baseline2.CharacterInterpolatedDatacrouchWeight);
        CharacterInterpolatedDataselectorTargetSource = predictor.PredictInt(CharacterInterpolatedDataselectorTargetSource, baseline1.CharacterInterpolatedDataselectorTargetSource, baseline2.CharacterInterpolatedDataselectorTargetSource);
        CharacterInterpolatedDatamoveAngleLocal = predictor.PredictInt(CharacterInterpolatedDatamoveAngleLocal, baseline1.CharacterInterpolatedDatamoveAngleLocal, baseline2.CharacterInterpolatedDatamoveAngleLocal);
        CharacterInterpolatedDatashootPoseWeight = predictor.PredictInt(CharacterInterpolatedDatashootPoseWeight, baseline1.CharacterInterpolatedDatashootPoseWeight, baseline2.CharacterInterpolatedDatashootPoseWeight);
        CharacterInterpolatedDatalocomotionVectorX = predictor.PredictInt(CharacterInterpolatedDatalocomotionVectorX, baseline1.CharacterInterpolatedDatalocomotionVectorX, baseline2.CharacterInterpolatedDatalocomotionVectorX);
        CharacterInterpolatedDatalocomotionVectorY = predictor.PredictInt(CharacterInterpolatedDatalocomotionVectorY, baseline1.CharacterInterpolatedDatalocomotionVectorY, baseline2.CharacterInterpolatedDatalocomotionVectorY);
        CharacterInterpolatedDatalocomotionPhase = predictor.PredictInt(CharacterInterpolatedDatalocomotionPhase, baseline1.CharacterInterpolatedDatalocomotionPhase, baseline2.CharacterInterpolatedDatalocomotionPhase);
        CharacterInterpolatedDatabanking = predictor.PredictInt(CharacterInterpolatedDatabanking, baseline1.CharacterInterpolatedDatabanking, baseline2.CharacterInterpolatedDatabanking);
        CharacterInterpolatedDatalandAnticWeight = predictor.PredictInt(CharacterInterpolatedDatalandAnticWeight, baseline1.CharacterInterpolatedDatalandAnticWeight, baseline2.CharacterInterpolatedDatalandAnticWeight);
        CharacterInterpolatedDataturnStartAngle = predictor.PredictInt(CharacterInterpolatedDataturnStartAngle, baseline1.CharacterInterpolatedDataturnStartAngle, baseline2.CharacterInterpolatedDataturnStartAngle);
        CharacterInterpolatedDataturnDirection = predictor.PredictInt(CharacterInterpolatedDataturnDirection, baseline1.CharacterInterpolatedDataturnDirection, baseline2.CharacterInterpolatedDataturnDirection);
        CharacterInterpolatedDatasquashTime = predictor.PredictInt(CharacterInterpolatedDatasquashTime, baseline1.CharacterInterpolatedDatasquashTime, baseline2.CharacterInterpolatedDatasquashTime);
        CharacterInterpolatedDatasquashWeight = predictor.PredictInt(CharacterInterpolatedDatasquashWeight, baseline1.CharacterInterpolatedDatasquashWeight, baseline2.CharacterInterpolatedDatasquashWeight);
        CharacterInterpolatedDatainAirTime = predictor.PredictInt(CharacterInterpolatedDatainAirTime, baseline1.CharacterInterpolatedDatainAirTime, baseline2.CharacterInterpolatedDatainAirTime);
        CharacterInterpolatedDatajumpTime = predictor.PredictInt(CharacterInterpolatedDatajumpTime, baseline1.CharacterInterpolatedDatajumpTime, baseline2.CharacterInterpolatedDatajumpTime);
        CharacterInterpolatedDatasimpleTime = predictor.PredictInt(CharacterInterpolatedDatasimpleTime, baseline1.CharacterInterpolatedDatasimpleTime, baseline2.CharacterInterpolatedDatasimpleTime);
        CharacterInterpolatedDatafootIkOffsetX = predictor.PredictInt(CharacterInterpolatedDatafootIkOffsetX, baseline1.CharacterInterpolatedDatafootIkOffsetX, baseline2.CharacterInterpolatedDatafootIkOffsetX);
        CharacterInterpolatedDatafootIkOffsetY = predictor.PredictInt(CharacterInterpolatedDatafootIkOffsetY, baseline1.CharacterInterpolatedDatafootIkOffsetY, baseline2.CharacterInterpolatedDatafootIkOffsetY);
        CharacterInterpolatedDatafootIkNormalLeftX = predictor.PredictInt(CharacterInterpolatedDatafootIkNormalLeftX, baseline1.CharacterInterpolatedDatafootIkNormalLeftX, baseline2.CharacterInterpolatedDatafootIkNormalLeftX);
        CharacterInterpolatedDatafootIkNormalLeftY = predictor.PredictInt(CharacterInterpolatedDatafootIkNormalLeftY, baseline1.CharacterInterpolatedDatafootIkNormalLeftY, baseline2.CharacterInterpolatedDatafootIkNormalLeftY);
        CharacterInterpolatedDatafootIkNormalLeftZ = predictor.PredictInt(CharacterInterpolatedDatafootIkNormalLeftZ, baseline1.CharacterInterpolatedDatafootIkNormalLeftZ, baseline2.CharacterInterpolatedDatafootIkNormalLeftZ);
        CharacterInterpolatedDatafootIkNormalRightX = predictor.PredictInt(CharacterInterpolatedDatafootIkNormalRightX, baseline1.CharacterInterpolatedDatafootIkNormalRightX, baseline2.CharacterInterpolatedDatafootIkNormalRightX);
        CharacterInterpolatedDatafootIkNormalRightY = predictor.PredictInt(CharacterInterpolatedDatafootIkNormalRightY, baseline1.CharacterInterpolatedDatafootIkNormalRightY, baseline2.CharacterInterpolatedDatafootIkNormalRightY);
        CharacterInterpolatedDatafootIkNormalRightZ = predictor.PredictInt(CharacterInterpolatedDatafootIkNormalRightZ, baseline1.CharacterInterpolatedDatafootIkNormalRightZ, baseline2.CharacterInterpolatedDatafootIkNormalRightZ);
        CharacterInterpolatedDatafootIkWeight = predictor.PredictInt(CharacterInterpolatedDatafootIkWeight, baseline1.CharacterInterpolatedDatafootIkWeight, baseline2.CharacterInterpolatedDatafootIkWeight);
        CharacterInterpolatedDatablendOutAim = predictor.PredictInt(CharacterInterpolatedDatablendOutAim, baseline1.CharacterInterpolatedDatablendOutAim, baseline2.CharacterInterpolatedDatablendOutAim);
        CharacterPredictedDatatick = predictor.PredictInt(CharacterPredictedDatatick, baseline1.CharacterPredictedDatatick, baseline2.CharacterPredictedDatatick);
        CharacterPredictedDatasprinting = (uint)predictor.PredictInt((int)CharacterPredictedDatasprinting, (int)baseline1.CharacterPredictedDatasprinting, (int)baseline2.CharacterPredictedDatasprinting);
        CharacterPredictedDatacameraProfile = predictor.PredictInt(CharacterPredictedDatacameraProfile, baseline1.CharacterPredictedDatacameraProfile, baseline2.CharacterPredictedDatacameraProfile);
        CharacterPredictedDatadamageTick = predictor.PredictInt(CharacterPredictedDatadamageTick, baseline1.CharacterPredictedDatadamageTick, baseline2.CharacterPredictedDatadamageTick);
        CharacterPredictedDatadamageDirectionX = predictor.PredictInt(CharacterPredictedDatadamageDirectionX, baseline1.CharacterPredictedDatadamageDirectionX, baseline2.CharacterPredictedDatadamageDirectionX);
        CharacterPredictedDatadamageDirectionY = predictor.PredictInt(CharacterPredictedDatadamageDirectionY, baseline1.CharacterPredictedDatadamageDirectionY, baseline2.CharacterPredictedDatadamageDirectionY);
        CharacterPredictedDatadamageDirectionZ = predictor.PredictInt(CharacterPredictedDatadamageDirectionZ, baseline1.CharacterPredictedDatadamageDirectionZ, baseline2.CharacterPredictedDatadamageDirectionZ);
        CharacterReplicatedDataheroTypeIndex = predictor.PredictInt(CharacterReplicatedDataheroTypeIndex, baseline1.CharacterReplicatedDataheroTypeIndex, baseline2.CharacterReplicatedDataheroTypeIndex);
        CharacterControllerGroundSupportDataSupportedState = predictor.PredictInt(CharacterControllerGroundSupportDataSupportedState, baseline1.CharacterControllerGroundSupportDataSupportedState, baseline2.CharacterControllerGroundSupportDataSupportedState);
        HealthStateDatahealth = predictor.PredictInt(HealthStateDatahealth, baseline1.HealthStateDatahealth, baseline2.HealthStateDatahealth);
        InventoryStateactiveSlot = predictor.PredictInt(InventoryStateactiveSlot, baseline1.InventoryStateactiveSlot, baseline2.InventoryStateactiveSlot);
        PlayerOwnerPlayerIdValue = predictor.PredictInt(PlayerOwnerPlayerIdValue, baseline1.PlayerOwnerPlayerIdValue, baseline2.PlayerOwnerPlayerIdValue);
        PlayerControlledStateresetCommandTick = predictor.PredictInt(PlayerControlledStateresetCommandTick, baseline1.PlayerControlledStateresetCommandTick, baseline2.PlayerControlledStateresetCommandTick);
        PlayerControlledStateresetCommandLookYaw = predictor.PredictInt(PlayerControlledStateresetCommandLookYaw, baseline1.PlayerControlledStateresetCommandLookYaw, baseline2.PlayerControlledStateresetCommandLookYaw);
        PlayerControlledStateresetCommandLookPitch = predictor.PredictInt(PlayerControlledStateresetCommandLookPitch, baseline1.PlayerControlledStateresetCommandLookPitch, baseline2.PlayerControlledStateresetCommandLookPitch);
        Child0AbilityAbilityControlbehaviorState = predictor.PredictInt(Child0AbilityAbilityControlbehaviorState, baseline1.Child0AbilityAbilityControlbehaviorState, baseline2.Child0AbilityAbilityControlbehaviorState);
        Child0AbilityAbilityControlrequestDeactivate = (uint)predictor.PredictInt((int)Child0AbilityAbilityControlrequestDeactivate, (int)baseline1.Child0AbilityAbilityControlrequestDeactivate, (int)baseline2.Child0AbilityAbilityControlrequestDeactivate);
        Child0AbilityMovementInterpolatedStatecharLocoState = predictor.PredictInt(Child0AbilityMovementInterpolatedStatecharLocoState, baseline1.Child0AbilityMovementInterpolatedStatecharLocoState, baseline2.Child0AbilityMovementInterpolatedStatecharLocoState);
        Child0AbilityMovementInterpolatedStatecharLocoTick = predictor.PredictInt(Child0AbilityMovementInterpolatedStatecharLocoTick, baseline1.Child0AbilityMovementInterpolatedStatecharLocoTick, baseline2.Child0AbilityMovementInterpolatedStatecharLocoTick);
        Child0AbilityMovementInterpolatedStatecrouching = (uint)predictor.PredictInt((int)Child0AbilityMovementInterpolatedStatecrouching, (int)baseline1.Child0AbilityMovementInterpolatedStatecrouching, (int)baseline2.Child0AbilityMovementInterpolatedStatecrouching);
        Child0AbilityMovementPredictedStatelocoState = predictor.PredictInt(Child0AbilityMovementPredictedStatelocoState, baseline1.Child0AbilityMovementPredictedStatelocoState, baseline2.Child0AbilityMovementPredictedStatelocoState);
        Child0AbilityMovementPredictedStatelocoStartTick = predictor.PredictInt(Child0AbilityMovementPredictedStatelocoStartTick, baseline1.Child0AbilityMovementPredictedStatelocoStartTick, baseline2.Child0AbilityMovementPredictedStatelocoStartTick);
        Child0AbilityMovementPredictedStatejumpCount = predictor.PredictInt(Child0AbilityMovementPredictedStatejumpCount, baseline1.Child0AbilityMovementPredictedStatejumpCount, baseline2.Child0AbilityMovementPredictedStatejumpCount);
        Child0AbilityMovementPredictedStatecrouching = (uint)predictor.PredictInt((int)Child0AbilityMovementPredictedStatecrouching, (int)baseline1.Child0AbilityMovementPredictedStatecrouching, (int)baseline2.Child0AbilityMovementPredictedStatecrouching);
        Child1AbilityAbilityControlbehaviorState = predictor.PredictInt(Child1AbilityAbilityControlbehaviorState, baseline1.Child1AbilityAbilityControlbehaviorState, baseline2.Child1AbilityAbilityControlbehaviorState);
        Child1AbilityAbilityControlrequestDeactivate = (uint)predictor.PredictInt((int)Child1AbilityAbilityControlrequestDeactivate, (int)baseline1.Child1AbilityAbilityControlrequestDeactivate, (int)baseline2.Child1AbilityAbilityControlrequestDeactivate);
        Child1AbilitySprintPredictedStateactive = predictor.PredictInt(Child1AbilitySprintPredictedStateactive, baseline1.Child1AbilitySprintPredictedStateactive, baseline2.Child1AbilitySprintPredictedStateactive);
        Child1AbilitySprintPredictedStateterminating = predictor.PredictInt(Child1AbilitySprintPredictedStateterminating, baseline1.Child1AbilitySprintPredictedStateterminating, baseline2.Child1AbilitySprintPredictedStateterminating);
        Child1AbilitySprintPredictedStateterminateStartTick = predictor.PredictInt(Child1AbilitySprintPredictedStateterminateStartTick, baseline1.Child1AbilitySprintPredictedStateterminateStartTick, baseline2.Child1AbilitySprintPredictedStateterminateStartTick);
        Child2AbilityAbilityControlbehaviorState = predictor.PredictInt(Child2AbilityAbilityControlbehaviorState, baseline1.Child2AbilityAbilityControlbehaviorState, baseline2.Child2AbilityAbilityControlbehaviorState);
        Child2AbilityAbilityControlrequestDeactivate = (uint)predictor.PredictInt((int)Child2AbilityAbilityControlrequestDeactivate, (int)baseline1.Child2AbilityAbilityControlrequestDeactivate, (int)baseline2.Child2AbilityAbilityControlrequestDeactivate);
        Child3AbilityAbilityControlbehaviorState = predictor.PredictInt(Child3AbilityAbilityControlbehaviorState, baseline1.Child3AbilityAbilityControlbehaviorState, baseline2.Child3AbilityAbilityControlbehaviorState);
        Child3AbilityAbilityControlrequestDeactivate = (uint)predictor.PredictInt((int)Child3AbilityAbilityControlrequestDeactivate, (int)baseline1.Child3AbilityAbilityControlrequestDeactivate, (int)baseline2.Child3AbilityAbilityControlrequestDeactivate);
    }

    public void Serialize(int networkId, ref Char_TerraformerSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
    {
        changeMask0 = (CharacterInterpolatedDataPositionX != baseline.CharacterInterpolatedDataPositionX ||
                                          CharacterInterpolatedDataPositionY != baseline.CharacterInterpolatedDataPositionY ||
                                          CharacterInterpolatedDataPositionZ != baseline.CharacterInterpolatedDataPositionZ) ? 1u : 0;
        changeMask0 |= (CharacterInterpolatedDatarotation != baseline.CharacterInterpolatedDatarotation) ? (1u<<1) : 0;
        changeMask0 |= (CharacterInterpolatedDataaimYaw != baseline.CharacterInterpolatedDataaimYaw) ? (1u<<2) : 0;
        changeMask0 |= (CharacterInterpolatedDataaimPitch != baseline.CharacterInterpolatedDataaimPitch) ? (1u<<3) : 0;
        changeMask0 |= (CharacterInterpolatedDatamoveYaw != baseline.CharacterInterpolatedDatamoveYaw) ? (1u<<4) : 0;
        changeMask0 |= (CharacterInterpolatedDatacharAction != baseline.CharacterInterpolatedDatacharAction) ? (1u<<5) : 0;
        changeMask0 |= (CharacterInterpolatedDatacharActionTick != baseline.CharacterInterpolatedDatacharActionTick) ? (1u<<6) : 0;
        changeMask0 |= (CharacterInterpolatedDatadamageTick != baseline.CharacterInterpolatedDatadamageTick) ? (1u<<7) : 0;
        changeMask0 |= (CharacterInterpolatedDatadamageDirection != baseline.CharacterInterpolatedDatadamageDirection) ? (1u<<8) : 0;
        changeMask0 |= (CharacterInterpolatedDatasprinting != baseline.CharacterInterpolatedDatasprinting) ? (1u<<9) : 0;
        changeMask0 |= (CharacterInterpolatedDatasprintWeight != baseline.CharacterInterpolatedDatasprintWeight) ? (1u<<10) : 0;
        changeMask0 |= (CharacterInterpolatedDatacrouchWeight != baseline.CharacterInterpolatedDatacrouchWeight) ? (1u<<11) : 0;
        changeMask0 |= (CharacterInterpolatedDataselectorTargetSource != baseline.CharacterInterpolatedDataselectorTargetSource) ? (1u<<12) : 0;
        changeMask0 |= (CharacterInterpolatedDatamoveAngleLocal != baseline.CharacterInterpolatedDatamoveAngleLocal) ? (1u<<13) : 0;
        changeMask0 |= (CharacterInterpolatedDatashootPoseWeight != baseline.CharacterInterpolatedDatashootPoseWeight) ? (1u<<14) : 0;
        changeMask0 |= (CharacterInterpolatedDatalocomotionVectorX != baseline.CharacterInterpolatedDatalocomotionVectorX ||
                                           CharacterInterpolatedDatalocomotionVectorY != baseline.CharacterInterpolatedDatalocomotionVectorY) ? (1u<<15) : 0;
        changeMask0 |= (CharacterInterpolatedDatalocomotionPhase != baseline.CharacterInterpolatedDatalocomotionPhase) ? (1u<<16) : 0;
        changeMask0 |= (CharacterInterpolatedDatabanking != baseline.CharacterInterpolatedDatabanking) ? (1u<<17) : 0;
        changeMask0 |= (CharacterInterpolatedDatalandAnticWeight != baseline.CharacterInterpolatedDatalandAnticWeight) ? (1u<<18) : 0;
        changeMask0 |= (CharacterInterpolatedDataturnStartAngle != baseline.CharacterInterpolatedDataturnStartAngle) ? (1u<<19) : 0;
        changeMask0 |= (CharacterInterpolatedDataturnDirection != baseline.CharacterInterpolatedDataturnDirection) ? (1u<<20) : 0;
        changeMask0 |= (CharacterInterpolatedDatasquashTime != baseline.CharacterInterpolatedDatasquashTime) ? (1u<<21) : 0;
        changeMask0 |= (CharacterInterpolatedDatasquashWeight != baseline.CharacterInterpolatedDatasquashWeight) ? (1u<<22) : 0;
        changeMask0 |= (CharacterInterpolatedDatainAirTime != baseline.CharacterInterpolatedDatainAirTime) ? (1u<<23) : 0;
        changeMask0 |= (CharacterInterpolatedDatajumpTime != baseline.CharacterInterpolatedDatajumpTime) ? (1u<<24) : 0;
        changeMask0 |= (CharacterInterpolatedDatasimpleTime != baseline.CharacterInterpolatedDatasimpleTime) ? (1u<<25) : 0;
        changeMask0 |= (CharacterInterpolatedDatafootIkOffsetX != baseline.CharacterInterpolatedDatafootIkOffsetX ||
                                           CharacterInterpolatedDatafootIkOffsetY != baseline.CharacterInterpolatedDatafootIkOffsetY) ? (1u<<26) : 0;
        changeMask0 |= (CharacterInterpolatedDatafootIkNormalLeftX != baseline.CharacterInterpolatedDatafootIkNormalLeftX ||
                                           CharacterInterpolatedDatafootIkNormalLeftY != baseline.CharacterInterpolatedDatafootIkNormalLeftY ||
                                           CharacterInterpolatedDatafootIkNormalLeftZ != baseline.CharacterInterpolatedDatafootIkNormalLeftZ) ? (1u<<27) : 0;
        changeMask0 |= (CharacterInterpolatedDatafootIkNormalRightX != baseline.CharacterInterpolatedDatafootIkNormalRightX ||
                                           CharacterInterpolatedDatafootIkNormalRightY != baseline.CharacterInterpolatedDatafootIkNormalRightY ||
                                           CharacterInterpolatedDatafootIkNormalRightZ != baseline.CharacterInterpolatedDatafootIkNormalRightZ) ? (1u<<28) : 0;
        changeMask0 |= (CharacterInterpolatedDatafootIkWeight != baseline.CharacterInterpolatedDatafootIkWeight) ? (1u<<29) : 0;
        changeMask0 |= (CharacterInterpolatedDatablendOutAim != baseline.CharacterInterpolatedDatablendOutAim) ? (1u<<30) : 0;
        changeMask0 |= (CharacterPredictedDatatick != baseline.CharacterPredictedDatatick) ? (1u<<31) : 0;
        changeMask1 = (CharacterPredictedDatapositionX != baseline.CharacterPredictedDatapositionX ||
                                          CharacterPredictedDatapositionY != baseline.CharacterPredictedDatapositionY ||
                                          CharacterPredictedDatapositionZ != baseline.CharacterPredictedDatapositionZ) ? 1u : 0;
        changeMask1 |= (CharacterPredictedDatavelocityX != baseline.CharacterPredictedDatavelocityX ||
                                           CharacterPredictedDatavelocityY != baseline.CharacterPredictedDatavelocityY ||
                                           CharacterPredictedDatavelocityZ != baseline.CharacterPredictedDatavelocityZ) ? (1u<<1) : 0;
        changeMask1 |= (CharacterPredictedDatasprinting != baseline.CharacterPredictedDatasprinting) ? (1u<<2) : 0;
        changeMask1 |= (CharacterPredictedDatacameraProfile != baseline.CharacterPredictedDatacameraProfile) ? (1u<<3) : 0;
        changeMask1 |= (CharacterPredictedDatadamageTick != baseline.CharacterPredictedDatadamageTick) ? (1u<<4) : 0;
        changeMask1 |= (CharacterPredictedDatadamageDirectionX != baseline.CharacterPredictedDatadamageDirectionX ||
                                           CharacterPredictedDatadamageDirectionY != baseline.CharacterPredictedDatadamageDirectionY ||
                                           CharacterPredictedDatadamageDirectionZ != baseline.CharacterPredictedDatadamageDirectionZ) ? (1u<<5) : 0;
        changeMask1 |= (CharacterReplicatedDataheroTypeIndex != baseline.CharacterReplicatedDataheroTypeIndex) ? (1u<<6) : 0;
        changeMask1 |= (CharacterControllerGroundSupportDataSurfaceNormalX != baseline.CharacterControllerGroundSupportDataSurfaceNormalX ||
                                           CharacterControllerGroundSupportDataSurfaceNormalY != baseline.CharacterControllerGroundSupportDataSurfaceNormalY ||
                                           CharacterControllerGroundSupportDataSurfaceNormalZ != baseline.CharacterControllerGroundSupportDataSurfaceNormalZ) ? (1u<<7) : 0;
        changeMask1 |= (CharacterControllerGroundSupportDataSurfaceVelocityX != baseline.CharacterControllerGroundSupportDataSurfaceVelocityX ||
                                           CharacterControllerGroundSupportDataSurfaceVelocityY != baseline.CharacterControllerGroundSupportDataSurfaceVelocityY ||
                                           CharacterControllerGroundSupportDataSurfaceVelocityZ != baseline.CharacterControllerGroundSupportDataSurfaceVelocityZ) ? (1u<<8) : 0;
        changeMask1 |= (CharacterControllerGroundSupportDataSupportedState != baseline.CharacterControllerGroundSupportDataSupportedState) ? (1u<<9) : 0;
        changeMask1 |= (CharacterControllerMoveResultMoveResultX != baseline.CharacterControllerMoveResultMoveResultX ||
                                           CharacterControllerMoveResultMoveResultY != baseline.CharacterControllerMoveResultMoveResultY ||
                                           CharacterControllerMoveResultMoveResultZ != baseline.CharacterControllerMoveResultMoveResultZ) ? (1u<<10) : 0;
        changeMask1 |= (CharacterControllerVelocityVelocityX != baseline.CharacterControllerVelocityVelocityX ||
                                           CharacterControllerVelocityVelocityY != baseline.CharacterControllerVelocityVelocityY ||
                                           CharacterControllerVelocityVelocityZ != baseline.CharacterControllerVelocityVelocityZ) ? (1u<<11) : 0;
        changeMask1 |= (HealthStateDatahealth != baseline.HealthStateDatahealth) ? (1u<<12) : 0;
        changeMask1 |= (InventoryStateactiveSlot != baseline.InventoryStateactiveSlot) ? (1u<<13) : 0;
        changeMask1 |= (PlayerOwnerPlayerIdValue != baseline.PlayerOwnerPlayerIdValue) ? (1u<<14) : 0;
        changeMask1 |= (PlayerControlledStateresetCommandTick != baseline.PlayerControlledStateresetCommandTick) ? (1u<<15) : 0;
        changeMask1 |= (PlayerControlledStateresetCommandLookYaw != baseline.PlayerControlledStateresetCommandLookYaw) ? (1u<<16) : 0;
        changeMask1 |= (PlayerControlledStateresetCommandLookPitch != baseline.PlayerControlledStateresetCommandLookPitch) ? (1u<<17) : 0;
        changeMask1 |= (Child0AbilityAbilityControlbehaviorState != baseline.Child0AbilityAbilityControlbehaviorState) ? (1u<<18) : 0;
        changeMask1 |= (Child0AbilityAbilityControlrequestDeactivate != baseline.Child0AbilityAbilityControlrequestDeactivate) ? (1u<<19) : 0;
        changeMask1 |= (Child0AbilityMovementInterpolatedStatecharLocoState != baseline.Child0AbilityMovementInterpolatedStatecharLocoState) ? (1u<<20) : 0;
        changeMask1 |= (Child0AbilityMovementInterpolatedStatecharLocoTick != baseline.Child0AbilityMovementInterpolatedStatecharLocoTick) ? (1u<<21) : 0;
        changeMask1 |= (Child0AbilityMovementInterpolatedStatecrouching != baseline.Child0AbilityMovementInterpolatedStatecrouching) ? (1u<<22) : 0;
        changeMask1 |= (Child0AbilityMovementPredictedStatelocoState != baseline.Child0AbilityMovementPredictedStatelocoState) ? (1u<<23) : 0;
        changeMask1 |= (Child0AbilityMovementPredictedStatelocoStartTick != baseline.Child0AbilityMovementPredictedStatelocoStartTick) ? (1u<<24) : 0;
        changeMask1 |= (Child0AbilityMovementPredictedStatejumpCount != baseline.Child0AbilityMovementPredictedStatejumpCount) ? (1u<<25) : 0;
        changeMask1 |= (Child0AbilityMovementPredictedStatecrouching != baseline.Child0AbilityMovementPredictedStatecrouching) ? (1u<<26) : 0;
        changeMask1 |= (Child1AbilityAbilityControlbehaviorState != baseline.Child1AbilityAbilityControlbehaviorState) ? (1u<<27) : 0;
        changeMask1 |= (Child1AbilityAbilityControlrequestDeactivate != baseline.Child1AbilityAbilityControlrequestDeactivate) ? (1u<<28) : 0;
        changeMask1 |= (Child1AbilitySprintPredictedStateactive != baseline.Child1AbilitySprintPredictedStateactive) ? (1u<<29) : 0;
        changeMask1 |= (Child1AbilitySprintPredictedStateterminating != baseline.Child1AbilitySprintPredictedStateterminating) ? (1u<<30) : 0;
        changeMask1 |= (Child1AbilitySprintPredictedStateterminateStartTick != baseline.Child1AbilitySprintPredictedStateterminateStartTick) ? (1u<<31) : 0;
        changeMask2 = (Child2AbilityAbilityControlbehaviorState != baseline.Child2AbilityAbilityControlbehaviorState) ? 1u : 0;
        changeMask2 |= (Child2AbilityAbilityControlrequestDeactivate != baseline.Child2AbilityAbilityControlrequestDeactivate) ? (1u<<1) : 0;
        changeMask2 |= (Child3AbilityAbilityControlbehaviorState != baseline.Child3AbilityAbilityControlbehaviorState) ? (1u<<2) : 0;
        changeMask2 |= (Child3AbilityAbilityControlrequestDeactivate != baseline.Child3AbilityAbilityControlrequestDeactivate) ? (1u<<3) : 0;
        writer.WritePackedUIntDelta(changeMask0, baseline.changeMask0, compressionModel);
        writer.WritePackedUIntDelta(changeMask1, baseline.changeMask1, compressionModel);
        writer.WritePackedUIntDelta(changeMask2, baseline.changeMask2, compressionModel);
        bool isPredicted = GetPlayerOwnerPlayerIdValue() == networkId;
        writer.WritePackedUInt(isPredicted?1u:0, compressionModel);
        if ((changeMask1 & (1 << 6)) != 0)
            writer.WritePackedIntDelta(CharacterReplicatedDataheroTypeIndex, baseline.CharacterReplicatedDataheroTypeIndex, compressionModel);
        if ((changeMask1 & (1 << 7)) != 0)
        {
            writer.WritePackedFloatDelta(CharacterControllerGroundSupportDataSurfaceNormalX, baseline.CharacterControllerGroundSupportDataSurfaceNormalX, compressionModel);
            writer.WritePackedFloatDelta(CharacterControllerGroundSupportDataSurfaceNormalY, baseline.CharacterControllerGroundSupportDataSurfaceNormalY, compressionModel);
            writer.WritePackedFloatDelta(CharacterControllerGroundSupportDataSurfaceNormalZ, baseline.CharacterControllerGroundSupportDataSurfaceNormalZ, compressionModel);
        }
        if ((changeMask1 & (1 << 8)) != 0)
        {
            writer.WritePackedFloatDelta(CharacterControllerGroundSupportDataSurfaceVelocityX, baseline.CharacterControllerGroundSupportDataSurfaceVelocityX, compressionModel);
            writer.WritePackedFloatDelta(CharacterControllerGroundSupportDataSurfaceVelocityY, baseline.CharacterControllerGroundSupportDataSurfaceVelocityY, compressionModel);
            writer.WritePackedFloatDelta(CharacterControllerGroundSupportDataSurfaceVelocityZ, baseline.CharacterControllerGroundSupportDataSurfaceVelocityZ, compressionModel);
        }
        if ((changeMask1 & (1 << 9)) != 0)
            writer.WritePackedIntDelta(CharacterControllerGroundSupportDataSupportedState, baseline.CharacterControllerGroundSupportDataSupportedState, compressionModel);
        if ((changeMask1 & (1 << 10)) != 0)
        {
            writer.WritePackedFloatDelta(CharacterControllerMoveResultMoveResultX, baseline.CharacterControllerMoveResultMoveResultX, compressionModel);
            writer.WritePackedFloatDelta(CharacterControllerMoveResultMoveResultY, baseline.CharacterControllerMoveResultMoveResultY, compressionModel);
            writer.WritePackedFloatDelta(CharacterControllerMoveResultMoveResultZ, baseline.CharacterControllerMoveResultMoveResultZ, compressionModel);
        }
        if ((changeMask1 & (1 << 11)) != 0)
        {
            writer.WritePackedFloatDelta(CharacterControllerVelocityVelocityX, baseline.CharacterControllerVelocityVelocityX, compressionModel);
            writer.WritePackedFloatDelta(CharacterControllerVelocityVelocityY, baseline.CharacterControllerVelocityVelocityY, compressionModel);
            writer.WritePackedFloatDelta(CharacterControllerVelocityVelocityZ, baseline.CharacterControllerVelocityVelocityZ, compressionModel);
        }
        if ((changeMask1 & (1 << 12)) != 0)
            writer.WritePackedIntDelta(HealthStateDatahealth, baseline.HealthStateDatahealth, compressionModel);
        if ((changeMask1 & (1 << 13)) != 0)
            writer.WritePackedIntDelta(InventoryStateactiveSlot, baseline.InventoryStateactiveSlot, compressionModel);
        if ((changeMask1 & (1 << 14)) != 0)
            writer.WritePackedIntDelta(PlayerOwnerPlayerIdValue, baseline.PlayerOwnerPlayerIdValue, compressionModel);
        if ((changeMask1 & (1 << 15)) != 0)
            writer.WritePackedIntDelta(PlayerControlledStateresetCommandTick, baseline.PlayerControlledStateresetCommandTick, compressionModel);
        if ((changeMask1 & (1 << 16)) != 0)
            writer.WritePackedIntDelta(PlayerControlledStateresetCommandLookYaw, baseline.PlayerControlledStateresetCommandLookYaw, compressionModel);
        if ((changeMask1 & (1 << 17)) != 0)
            writer.WritePackedIntDelta(PlayerControlledStateresetCommandLookPitch, baseline.PlayerControlledStateresetCommandLookPitch, compressionModel);
        if ((changeMask1 & (1 << 18)) != 0)
            writer.WritePackedIntDelta(Child0AbilityAbilityControlbehaviorState, baseline.Child0AbilityAbilityControlbehaviorState, compressionModel);
        if ((changeMask1 & (1 << 19)) != 0)
            writer.WritePackedUIntDelta(Child0AbilityAbilityControlrequestDeactivate, baseline.Child0AbilityAbilityControlrequestDeactivate, compressionModel);
        if ((changeMask1 & (1 << 20)) != 0)
            writer.WritePackedIntDelta(Child0AbilityMovementInterpolatedStatecharLocoState, baseline.Child0AbilityMovementInterpolatedStatecharLocoState, compressionModel);
        if ((changeMask1 & (1 << 21)) != 0)
            writer.WritePackedIntDelta(Child0AbilityMovementInterpolatedStatecharLocoTick, baseline.Child0AbilityMovementInterpolatedStatecharLocoTick, compressionModel);
        if ((changeMask1 & (1 << 22)) != 0)
            writer.WritePackedUIntDelta(Child0AbilityMovementInterpolatedStatecrouching, baseline.Child0AbilityMovementInterpolatedStatecrouching, compressionModel);
        if ((changeMask1 & (1 << 23)) != 0)
            writer.WritePackedIntDelta(Child0AbilityMovementPredictedStatelocoState, baseline.Child0AbilityMovementPredictedStatelocoState, compressionModel);
        if ((changeMask1 & (1 << 24)) != 0)
            writer.WritePackedIntDelta(Child0AbilityMovementPredictedStatelocoStartTick, baseline.Child0AbilityMovementPredictedStatelocoStartTick, compressionModel);
        if ((changeMask1 & (1 << 25)) != 0)
            writer.WritePackedIntDelta(Child0AbilityMovementPredictedStatejumpCount, baseline.Child0AbilityMovementPredictedStatejumpCount, compressionModel);
        if ((changeMask1 & (1 << 26)) != 0)
            writer.WritePackedUIntDelta(Child0AbilityMovementPredictedStatecrouching, baseline.Child0AbilityMovementPredictedStatecrouching, compressionModel);
        if ((changeMask1 & (1 << 27)) != 0)
            writer.WritePackedIntDelta(Child1AbilityAbilityControlbehaviorState, baseline.Child1AbilityAbilityControlbehaviorState, compressionModel);
        if ((changeMask1 & (1 << 28)) != 0)
            writer.WritePackedUIntDelta(Child1AbilityAbilityControlrequestDeactivate, baseline.Child1AbilityAbilityControlrequestDeactivate, compressionModel);
        if ((changeMask1 & (1 << 29)) != 0)
            writer.WritePackedIntDelta(Child1AbilitySprintPredictedStateactive, baseline.Child1AbilitySprintPredictedStateactive, compressionModel);
        if ((changeMask1 & (1 << 30)) != 0)
            writer.WritePackedIntDelta(Child1AbilitySprintPredictedStateterminating, baseline.Child1AbilitySprintPredictedStateterminating, compressionModel);
        if ((changeMask1 & (1 << 31)) != 0)
            writer.WritePackedIntDelta(Child1AbilitySprintPredictedStateterminateStartTick, baseline.Child1AbilitySprintPredictedStateterminateStartTick, compressionModel);
        if ((changeMask2 & (1 << 0)) != 0)
            writer.WritePackedIntDelta(Child2AbilityAbilityControlbehaviorState, baseline.Child2AbilityAbilityControlbehaviorState, compressionModel);
        if ((changeMask2 & (1 << 1)) != 0)
            writer.WritePackedUIntDelta(Child2AbilityAbilityControlrequestDeactivate, baseline.Child2AbilityAbilityControlrequestDeactivate, compressionModel);
        if ((changeMask2 & (1 << 2)) != 0)
            writer.WritePackedIntDelta(Child3AbilityAbilityControlbehaviorState, baseline.Child3AbilityAbilityControlbehaviorState, compressionModel);
        if ((changeMask2 & (1 << 3)) != 0)
            writer.WritePackedUIntDelta(Child3AbilityAbilityControlrequestDeactivate, baseline.Child3AbilityAbilityControlrequestDeactivate, compressionModel);
        if (isPredicted)
        {
            if ((changeMask0 & (1 << 31)) != 0)
                writer.WritePackedIntDelta(CharacterPredictedDatatick, baseline.CharacterPredictedDatatick, compressionModel);
            if ((changeMask1 & (1 << 0)) != 0)
            {
                writer.WritePackedFloatDelta(CharacterPredictedDatapositionX, baseline.CharacterPredictedDatapositionX, compressionModel);
                writer.WritePackedFloatDelta(CharacterPredictedDatapositionY, baseline.CharacterPredictedDatapositionY, compressionModel);
                writer.WritePackedFloatDelta(CharacterPredictedDatapositionZ, baseline.CharacterPredictedDatapositionZ, compressionModel);
            }
            if ((changeMask1 & (1 << 1)) != 0)
            {
                writer.WritePackedFloatDelta(CharacterPredictedDatavelocityX, baseline.CharacterPredictedDatavelocityX, compressionModel);
                writer.WritePackedFloatDelta(CharacterPredictedDatavelocityY, baseline.CharacterPredictedDatavelocityY, compressionModel);
                writer.WritePackedFloatDelta(CharacterPredictedDatavelocityZ, baseline.CharacterPredictedDatavelocityZ, compressionModel);
            }
            if ((changeMask1 & (1 << 2)) != 0)
                writer.WritePackedUIntDelta(CharacterPredictedDatasprinting, baseline.CharacterPredictedDatasprinting, compressionModel);
            if ((changeMask1 & (1 << 3)) != 0)
                writer.WritePackedIntDelta(CharacterPredictedDatacameraProfile, baseline.CharacterPredictedDatacameraProfile, compressionModel);
            if ((changeMask1 & (1 << 4)) != 0)
                writer.WritePackedIntDelta(CharacterPredictedDatadamageTick, baseline.CharacterPredictedDatadamageTick, compressionModel);
            if ((changeMask1 & (1 << 5)) != 0)
            {
                writer.WritePackedIntDelta(CharacterPredictedDatadamageDirectionX, baseline.CharacterPredictedDatadamageDirectionX, compressionModel);
                writer.WritePackedIntDelta(CharacterPredictedDatadamageDirectionY, baseline.CharacterPredictedDatadamageDirectionY, compressionModel);
                writer.WritePackedIntDelta(CharacterPredictedDatadamageDirectionZ, baseline.CharacterPredictedDatadamageDirectionZ, compressionModel);
            }
        }
        if (!isPredicted)
        {
            if ((changeMask0 & (1 << 0)) != 0)
            {
                writer.WritePackedIntDelta(CharacterInterpolatedDataPositionX, baseline.CharacterInterpolatedDataPositionX, compressionModel);
                writer.WritePackedIntDelta(CharacterInterpolatedDataPositionY, baseline.CharacterInterpolatedDataPositionY, compressionModel);
                writer.WritePackedIntDelta(CharacterInterpolatedDataPositionZ, baseline.CharacterInterpolatedDataPositionZ, compressionModel);
            }
            if ((changeMask0 & (1 << 1)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatarotation, baseline.CharacterInterpolatedDatarotation, compressionModel);
            if ((changeMask0 & (1 << 2)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDataaimYaw, baseline.CharacterInterpolatedDataaimYaw, compressionModel);
            if ((changeMask0 & (1 << 3)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDataaimPitch, baseline.CharacterInterpolatedDataaimPitch, compressionModel);
            if ((changeMask0 & (1 << 4)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatamoveYaw, baseline.CharacterInterpolatedDatamoveYaw, compressionModel);
            if ((changeMask0 & (1 << 5)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatacharAction, baseline.CharacterInterpolatedDatacharAction, compressionModel);
            if ((changeMask0 & (1 << 6)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatacharActionTick, baseline.CharacterInterpolatedDatacharActionTick, compressionModel);
            if ((changeMask0 & (1 << 7)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatadamageTick, baseline.CharacterInterpolatedDatadamageTick, compressionModel);
            if ((changeMask0 & (1 << 8)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatadamageDirection, baseline.CharacterInterpolatedDatadamageDirection, compressionModel);
            if ((changeMask0 & (1 << 9)) != 0)
                writer.WritePackedUIntDelta(CharacterInterpolatedDatasprinting, baseline.CharacterInterpolatedDatasprinting, compressionModel);
            if ((changeMask0 & (1 << 10)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatasprintWeight, baseline.CharacterInterpolatedDatasprintWeight, compressionModel);
            if ((changeMask0 & (1 << 11)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatacrouchWeight, baseline.CharacterInterpolatedDatacrouchWeight, compressionModel);
            if ((changeMask0 & (1 << 12)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDataselectorTargetSource, baseline.CharacterInterpolatedDataselectorTargetSource, compressionModel);
            if ((changeMask0 & (1 << 13)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatamoveAngleLocal, baseline.CharacterInterpolatedDatamoveAngleLocal, compressionModel);
            if ((changeMask0 & (1 << 14)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatashootPoseWeight, baseline.CharacterInterpolatedDatashootPoseWeight, compressionModel);
            if ((changeMask0 & (1 << 15)) != 0)
            {
                writer.WritePackedIntDelta(CharacterInterpolatedDatalocomotionVectorX, baseline.CharacterInterpolatedDatalocomotionVectorX, compressionModel);
                writer.WritePackedIntDelta(CharacterInterpolatedDatalocomotionVectorY, baseline.CharacterInterpolatedDatalocomotionVectorY, compressionModel);
            }
            if ((changeMask0 & (1 << 16)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatalocomotionPhase, baseline.CharacterInterpolatedDatalocomotionPhase, compressionModel);
            if ((changeMask0 & (1 << 17)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatabanking, baseline.CharacterInterpolatedDatabanking, compressionModel);
            if ((changeMask0 & (1 << 18)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatalandAnticWeight, baseline.CharacterInterpolatedDatalandAnticWeight, compressionModel);
            if ((changeMask0 & (1 << 19)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDataturnStartAngle, baseline.CharacterInterpolatedDataturnStartAngle, compressionModel);
            if ((changeMask0 & (1 << 20)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDataturnDirection, baseline.CharacterInterpolatedDataturnDirection, compressionModel);
            if ((changeMask0 & (1 << 21)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatasquashTime, baseline.CharacterInterpolatedDatasquashTime, compressionModel);
            if ((changeMask0 & (1 << 22)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatasquashWeight, baseline.CharacterInterpolatedDatasquashWeight, compressionModel);
            if ((changeMask0 & (1 << 23)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatainAirTime, baseline.CharacterInterpolatedDatainAirTime, compressionModel);
            if ((changeMask0 & (1 << 24)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatajumpTime, baseline.CharacterInterpolatedDatajumpTime, compressionModel);
            if ((changeMask0 & (1 << 25)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatasimpleTime, baseline.CharacterInterpolatedDatasimpleTime, compressionModel);
            if ((changeMask0 & (1 << 26)) != 0)
            {
                writer.WritePackedIntDelta(CharacterInterpolatedDatafootIkOffsetX, baseline.CharacterInterpolatedDatafootIkOffsetX, compressionModel);
                writer.WritePackedIntDelta(CharacterInterpolatedDatafootIkOffsetY, baseline.CharacterInterpolatedDatafootIkOffsetY, compressionModel);
            }
            if ((changeMask0 & (1 << 27)) != 0)
            {
                writer.WritePackedIntDelta(CharacterInterpolatedDatafootIkNormalLeftX, baseline.CharacterInterpolatedDatafootIkNormalLeftX, compressionModel);
                writer.WritePackedIntDelta(CharacterInterpolatedDatafootIkNormalLeftY, baseline.CharacterInterpolatedDatafootIkNormalLeftY, compressionModel);
                writer.WritePackedIntDelta(CharacterInterpolatedDatafootIkNormalLeftZ, baseline.CharacterInterpolatedDatafootIkNormalLeftZ, compressionModel);
            }
            if ((changeMask0 & (1 << 28)) != 0)
            {
                writer.WritePackedIntDelta(CharacterInterpolatedDatafootIkNormalRightX, baseline.CharacterInterpolatedDatafootIkNormalRightX, compressionModel);
                writer.WritePackedIntDelta(CharacterInterpolatedDatafootIkNormalRightY, baseline.CharacterInterpolatedDatafootIkNormalRightY, compressionModel);
                writer.WritePackedIntDelta(CharacterInterpolatedDatafootIkNormalRightZ, baseline.CharacterInterpolatedDatafootIkNormalRightZ, compressionModel);
            }
            if ((changeMask0 & (1 << 29)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatafootIkWeight, baseline.CharacterInterpolatedDatafootIkWeight, compressionModel);
            if ((changeMask0 & (1 << 30)) != 0)
                writer.WritePackedIntDelta(CharacterInterpolatedDatablendOutAim, baseline.CharacterInterpolatedDatablendOutAim, compressionModel);
        }
    }

    public void Deserialize(uint tick, ref Char_TerraformerSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx,
        NetworkCompressionModel compressionModel)
    {
        this.tick = tick;
        changeMask0 = reader.ReadPackedUIntDelta(ref ctx, baseline.changeMask0, compressionModel);
        changeMask1 = reader.ReadPackedUIntDelta(ref ctx, baseline.changeMask1, compressionModel);
        changeMask2 = reader.ReadPackedUIntDelta(ref ctx, baseline.changeMask2, compressionModel);
        bool isPredicted = reader.ReadPackedUInt(ref ctx, compressionModel)!=0;
        if ((changeMask1 & (1 << 6)) != 0)
            CharacterReplicatedDataheroTypeIndex = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterReplicatedDataheroTypeIndex, compressionModel);
        else
            CharacterReplicatedDataheroTypeIndex = baseline.CharacterReplicatedDataheroTypeIndex;
        if ((changeMask1 & (1 << 7)) != 0)
        {
            CharacterControllerGroundSupportDataSurfaceNormalX = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerGroundSupportDataSurfaceNormalX, compressionModel);
            CharacterControllerGroundSupportDataSurfaceNormalY = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerGroundSupportDataSurfaceNormalY, compressionModel);
            CharacterControllerGroundSupportDataSurfaceNormalZ = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerGroundSupportDataSurfaceNormalZ, compressionModel);
        }
        else
        {
            CharacterControllerGroundSupportDataSurfaceNormalX = baseline.CharacterControllerGroundSupportDataSurfaceNormalX;
            CharacterControllerGroundSupportDataSurfaceNormalY = baseline.CharacterControllerGroundSupportDataSurfaceNormalY;
            CharacterControllerGroundSupportDataSurfaceNormalZ = baseline.CharacterControllerGroundSupportDataSurfaceNormalZ;
        }
        if ((changeMask1 & (1 << 8)) != 0)
        {
            CharacterControllerGroundSupportDataSurfaceVelocityX = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerGroundSupportDataSurfaceVelocityX, compressionModel);
            CharacterControllerGroundSupportDataSurfaceVelocityY = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerGroundSupportDataSurfaceVelocityY, compressionModel);
            CharacterControllerGroundSupportDataSurfaceVelocityZ = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerGroundSupportDataSurfaceVelocityZ, compressionModel);
        }
        else
        {
            CharacterControllerGroundSupportDataSurfaceVelocityX = baseline.CharacterControllerGroundSupportDataSurfaceVelocityX;
            CharacterControllerGroundSupportDataSurfaceVelocityY = baseline.CharacterControllerGroundSupportDataSurfaceVelocityY;
            CharacterControllerGroundSupportDataSurfaceVelocityZ = baseline.CharacterControllerGroundSupportDataSurfaceVelocityZ;
        }
        if ((changeMask1 & (1 << 9)) != 0)
            CharacterControllerGroundSupportDataSupportedState = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterControllerGroundSupportDataSupportedState, compressionModel);
        else
            CharacterControllerGroundSupportDataSupportedState = baseline.CharacterControllerGroundSupportDataSupportedState;
        if ((changeMask1 & (1 << 10)) != 0)
        {
            CharacterControllerMoveResultMoveResultX = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerMoveResultMoveResultX, compressionModel);
            CharacterControllerMoveResultMoveResultY = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerMoveResultMoveResultY, compressionModel);
            CharacterControllerMoveResultMoveResultZ = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerMoveResultMoveResultZ, compressionModel);
        }
        else
        {
            CharacterControllerMoveResultMoveResultX = baseline.CharacterControllerMoveResultMoveResultX;
            CharacterControllerMoveResultMoveResultY = baseline.CharacterControllerMoveResultMoveResultY;
            CharacterControllerMoveResultMoveResultZ = baseline.CharacterControllerMoveResultMoveResultZ;
        }
        if ((changeMask1 & (1 << 11)) != 0)
        {
            CharacterControllerVelocityVelocityX = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerVelocityVelocityX, compressionModel);
            CharacterControllerVelocityVelocityY = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerVelocityVelocityY, compressionModel);
            CharacterControllerVelocityVelocityZ = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterControllerVelocityVelocityZ, compressionModel);
        }
        else
        {
            CharacterControllerVelocityVelocityX = baseline.CharacterControllerVelocityVelocityX;
            CharacterControllerVelocityVelocityY = baseline.CharacterControllerVelocityVelocityY;
            CharacterControllerVelocityVelocityZ = baseline.CharacterControllerVelocityVelocityZ;
        }
        if ((changeMask1 & (1 << 12)) != 0)
            HealthStateDatahealth = reader.ReadPackedIntDelta(ref ctx, baseline.HealthStateDatahealth, compressionModel);
        else
            HealthStateDatahealth = baseline.HealthStateDatahealth;
        if ((changeMask1 & (1 << 13)) != 0)
            InventoryStateactiveSlot = reader.ReadPackedIntDelta(ref ctx, baseline.InventoryStateactiveSlot, compressionModel);
        else
            InventoryStateactiveSlot = baseline.InventoryStateactiveSlot;
        if ((changeMask1 & (1 << 14)) != 0)
            PlayerOwnerPlayerIdValue = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerOwnerPlayerIdValue, compressionModel);
        else
            PlayerOwnerPlayerIdValue = baseline.PlayerOwnerPlayerIdValue;
        if ((changeMask1 & (1 << 15)) != 0)
            PlayerControlledStateresetCommandTick = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerControlledStateresetCommandTick, compressionModel);
        else
            PlayerControlledStateresetCommandTick = baseline.PlayerControlledStateresetCommandTick;
        if ((changeMask1 & (1 << 16)) != 0)
            PlayerControlledStateresetCommandLookYaw = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerControlledStateresetCommandLookYaw, compressionModel);
        else
            PlayerControlledStateresetCommandLookYaw = baseline.PlayerControlledStateresetCommandLookYaw;
        if ((changeMask1 & (1 << 17)) != 0)
            PlayerControlledStateresetCommandLookPitch = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerControlledStateresetCommandLookPitch, compressionModel);
        else
            PlayerControlledStateresetCommandLookPitch = baseline.PlayerControlledStateresetCommandLookPitch;
        if ((changeMask1 & (1 << 18)) != 0)
            Child0AbilityAbilityControlbehaviorState = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityAbilityControlbehaviorState, compressionModel);
        else
            Child0AbilityAbilityControlbehaviorState = baseline.Child0AbilityAbilityControlbehaviorState;
        if ((changeMask1 & (1 << 19)) != 0)
            Child0AbilityAbilityControlrequestDeactivate = reader.ReadPackedUIntDelta(ref ctx, baseline.Child0AbilityAbilityControlrequestDeactivate, compressionModel);
        else
            Child0AbilityAbilityControlrequestDeactivate = baseline.Child0AbilityAbilityControlrequestDeactivate;
        if ((changeMask1 & (1 << 20)) != 0)
            Child0AbilityMovementInterpolatedStatecharLocoState = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityMovementInterpolatedStatecharLocoState, compressionModel);
        else
            Child0AbilityMovementInterpolatedStatecharLocoState = baseline.Child0AbilityMovementInterpolatedStatecharLocoState;
        if ((changeMask1 & (1 << 21)) != 0)
            Child0AbilityMovementInterpolatedStatecharLocoTick = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityMovementInterpolatedStatecharLocoTick, compressionModel);
        else
            Child0AbilityMovementInterpolatedStatecharLocoTick = baseline.Child0AbilityMovementInterpolatedStatecharLocoTick;
        if ((changeMask1 & (1 << 22)) != 0)
            Child0AbilityMovementInterpolatedStatecrouching = reader.ReadPackedUIntDelta(ref ctx, baseline.Child0AbilityMovementInterpolatedStatecrouching, compressionModel);
        else
            Child0AbilityMovementInterpolatedStatecrouching = baseline.Child0AbilityMovementInterpolatedStatecrouching;
        if ((changeMask1 & (1 << 23)) != 0)
            Child0AbilityMovementPredictedStatelocoState = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityMovementPredictedStatelocoState, compressionModel);
        else
            Child0AbilityMovementPredictedStatelocoState = baseline.Child0AbilityMovementPredictedStatelocoState;
        if ((changeMask1 & (1 << 24)) != 0)
            Child0AbilityMovementPredictedStatelocoStartTick = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityMovementPredictedStatelocoStartTick, compressionModel);
        else
            Child0AbilityMovementPredictedStatelocoStartTick = baseline.Child0AbilityMovementPredictedStatelocoStartTick;
        if ((changeMask1 & (1 << 25)) != 0)
            Child0AbilityMovementPredictedStatejumpCount = reader.ReadPackedIntDelta(ref ctx, baseline.Child0AbilityMovementPredictedStatejumpCount, compressionModel);
        else
            Child0AbilityMovementPredictedStatejumpCount = baseline.Child0AbilityMovementPredictedStatejumpCount;
        if ((changeMask1 & (1 << 26)) != 0)
            Child0AbilityMovementPredictedStatecrouching = reader.ReadPackedUIntDelta(ref ctx, baseline.Child0AbilityMovementPredictedStatecrouching, compressionModel);
        else
            Child0AbilityMovementPredictedStatecrouching = baseline.Child0AbilityMovementPredictedStatecrouching;
        if ((changeMask1 & (1 << 27)) != 0)
            Child1AbilityAbilityControlbehaviorState = reader.ReadPackedIntDelta(ref ctx, baseline.Child1AbilityAbilityControlbehaviorState, compressionModel);
        else
            Child1AbilityAbilityControlbehaviorState = baseline.Child1AbilityAbilityControlbehaviorState;
        if ((changeMask1 & (1 << 28)) != 0)
            Child1AbilityAbilityControlrequestDeactivate = reader.ReadPackedUIntDelta(ref ctx, baseline.Child1AbilityAbilityControlrequestDeactivate, compressionModel);
        else
            Child1AbilityAbilityControlrequestDeactivate = baseline.Child1AbilityAbilityControlrequestDeactivate;
        if ((changeMask1 & (1 << 29)) != 0)
            Child1AbilitySprintPredictedStateactive = reader.ReadPackedIntDelta(ref ctx, baseline.Child1AbilitySprintPredictedStateactive, compressionModel);
        else
            Child1AbilitySprintPredictedStateactive = baseline.Child1AbilitySprintPredictedStateactive;
        if ((changeMask1 & (1 << 30)) != 0)
            Child1AbilitySprintPredictedStateterminating = reader.ReadPackedIntDelta(ref ctx, baseline.Child1AbilitySprintPredictedStateterminating, compressionModel);
        else
            Child1AbilitySprintPredictedStateterminating = baseline.Child1AbilitySprintPredictedStateterminating;
        if ((changeMask1 & (1 << 31)) != 0)
            Child1AbilitySprintPredictedStateterminateStartTick = reader.ReadPackedIntDelta(ref ctx, baseline.Child1AbilitySprintPredictedStateterminateStartTick, compressionModel);
        else
            Child1AbilitySprintPredictedStateterminateStartTick = baseline.Child1AbilitySprintPredictedStateterminateStartTick;
        if ((changeMask2 & (1 << 0)) != 0)
            Child2AbilityAbilityControlbehaviorState = reader.ReadPackedIntDelta(ref ctx, baseline.Child2AbilityAbilityControlbehaviorState, compressionModel);
        else
            Child2AbilityAbilityControlbehaviorState = baseline.Child2AbilityAbilityControlbehaviorState;
        if ((changeMask2 & (1 << 1)) != 0)
            Child2AbilityAbilityControlrequestDeactivate = reader.ReadPackedUIntDelta(ref ctx, baseline.Child2AbilityAbilityControlrequestDeactivate, compressionModel);
        else
            Child2AbilityAbilityControlrequestDeactivate = baseline.Child2AbilityAbilityControlrequestDeactivate;
        if ((changeMask2 & (1 << 2)) != 0)
            Child3AbilityAbilityControlbehaviorState = reader.ReadPackedIntDelta(ref ctx, baseline.Child3AbilityAbilityControlbehaviorState, compressionModel);
        else
            Child3AbilityAbilityControlbehaviorState = baseline.Child3AbilityAbilityControlbehaviorState;
        if ((changeMask2 & (1 << 3)) != 0)
            Child3AbilityAbilityControlrequestDeactivate = reader.ReadPackedUIntDelta(ref ctx, baseline.Child3AbilityAbilityControlrequestDeactivate, compressionModel);
        else
            Child3AbilityAbilityControlrequestDeactivate = baseline.Child3AbilityAbilityControlrequestDeactivate;
        if (isPredicted)
        {
            if ((changeMask0 & (1 << 31)) != 0)
                CharacterPredictedDatatick = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterPredictedDatatick, compressionModel);
            else
                CharacterPredictedDatatick = baseline.CharacterPredictedDatatick;
            if ((changeMask1 & (1 << 0)) != 0)
            {
                CharacterPredictedDatapositionX = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterPredictedDatapositionX, compressionModel);
                CharacterPredictedDatapositionY = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterPredictedDatapositionY, compressionModel);
                CharacterPredictedDatapositionZ = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterPredictedDatapositionZ, compressionModel);
            }
            else
            {
                CharacterPredictedDatapositionX = baseline.CharacterPredictedDatapositionX;
                CharacterPredictedDatapositionY = baseline.CharacterPredictedDatapositionY;
                CharacterPredictedDatapositionZ = baseline.CharacterPredictedDatapositionZ;
            }
            if ((changeMask1 & (1 << 1)) != 0)
            {
                CharacterPredictedDatavelocityX = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterPredictedDatavelocityX, compressionModel);
                CharacterPredictedDatavelocityY = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterPredictedDatavelocityY, compressionModel);
                CharacterPredictedDatavelocityZ = reader.ReadPackedFloatDelta(ref ctx, baseline.CharacterPredictedDatavelocityZ, compressionModel);
            }
            else
            {
                CharacterPredictedDatavelocityX = baseline.CharacterPredictedDatavelocityX;
                CharacterPredictedDatavelocityY = baseline.CharacterPredictedDatavelocityY;
                CharacterPredictedDatavelocityZ = baseline.CharacterPredictedDatavelocityZ;
            }
            if ((changeMask1 & (1 << 2)) != 0)
                CharacterPredictedDatasprinting = reader.ReadPackedUIntDelta(ref ctx, baseline.CharacterPredictedDatasprinting, compressionModel);
            else
                CharacterPredictedDatasprinting = baseline.CharacterPredictedDatasprinting;
            if ((changeMask1 & (1 << 3)) != 0)
                CharacterPredictedDatacameraProfile = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterPredictedDatacameraProfile, compressionModel);
            else
                CharacterPredictedDatacameraProfile = baseline.CharacterPredictedDatacameraProfile;
            if ((changeMask1 & (1 << 4)) != 0)
                CharacterPredictedDatadamageTick = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterPredictedDatadamageTick, compressionModel);
            else
                CharacterPredictedDatadamageTick = baseline.CharacterPredictedDatadamageTick;
            if ((changeMask1 & (1 << 5)) != 0)
            {
                CharacterPredictedDatadamageDirectionX = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterPredictedDatadamageDirectionX, compressionModel);
                CharacterPredictedDatadamageDirectionY = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterPredictedDatadamageDirectionY, compressionModel);
                CharacterPredictedDatadamageDirectionZ = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterPredictedDatadamageDirectionZ, compressionModel);
            }
            else
            {
                CharacterPredictedDatadamageDirectionX = baseline.CharacterPredictedDatadamageDirectionX;
                CharacterPredictedDatadamageDirectionY = baseline.CharacterPredictedDatadamageDirectionY;
                CharacterPredictedDatadamageDirectionZ = baseline.CharacterPredictedDatadamageDirectionZ;
            }
        }
        if (!isPredicted)
        {
            if ((changeMask0 & (1 << 0)) != 0)
            {
                CharacterInterpolatedDataPositionX = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDataPositionX, compressionModel);
                CharacterInterpolatedDataPositionY = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDataPositionY, compressionModel);
                CharacterInterpolatedDataPositionZ = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDataPositionZ, compressionModel);
            }
            else
            {
                CharacterInterpolatedDataPositionX = baseline.CharacterInterpolatedDataPositionX;
                CharacterInterpolatedDataPositionY = baseline.CharacterInterpolatedDataPositionY;
                CharacterInterpolatedDataPositionZ = baseline.CharacterInterpolatedDataPositionZ;
            }
            if ((changeMask0 & (1 << 1)) != 0)
                CharacterInterpolatedDatarotation = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatarotation, compressionModel);
            else
                CharacterInterpolatedDatarotation = baseline.CharacterInterpolatedDatarotation;
            if ((changeMask0 & (1 << 2)) != 0)
                CharacterInterpolatedDataaimYaw = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDataaimYaw, compressionModel);
            else
                CharacterInterpolatedDataaimYaw = baseline.CharacterInterpolatedDataaimYaw;
            if ((changeMask0 & (1 << 3)) != 0)
                CharacterInterpolatedDataaimPitch = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDataaimPitch, compressionModel);
            else
                CharacterInterpolatedDataaimPitch = baseline.CharacterInterpolatedDataaimPitch;
            if ((changeMask0 & (1 << 4)) != 0)
                CharacterInterpolatedDatamoveYaw = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatamoveYaw, compressionModel);
            else
                CharacterInterpolatedDatamoveYaw = baseline.CharacterInterpolatedDatamoveYaw;
            if ((changeMask0 & (1 << 5)) != 0)
                CharacterInterpolatedDatacharAction = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatacharAction, compressionModel);
            else
                CharacterInterpolatedDatacharAction = baseline.CharacterInterpolatedDatacharAction;
            if ((changeMask0 & (1 << 6)) != 0)
                CharacterInterpolatedDatacharActionTick = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatacharActionTick, compressionModel);
            else
                CharacterInterpolatedDatacharActionTick = baseline.CharacterInterpolatedDatacharActionTick;
            if ((changeMask0 & (1 << 7)) != 0)
                CharacterInterpolatedDatadamageTick = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatadamageTick, compressionModel);
            else
                CharacterInterpolatedDatadamageTick = baseline.CharacterInterpolatedDatadamageTick;
            if ((changeMask0 & (1 << 8)) != 0)
                CharacterInterpolatedDatadamageDirection = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatadamageDirection, compressionModel);
            else
                CharacterInterpolatedDatadamageDirection = baseline.CharacterInterpolatedDatadamageDirection;
            if ((changeMask0 & (1 << 9)) != 0)
                CharacterInterpolatedDatasprinting = reader.ReadPackedUIntDelta(ref ctx, baseline.CharacterInterpolatedDatasprinting, compressionModel);
            else
                CharacterInterpolatedDatasprinting = baseline.CharacterInterpolatedDatasprinting;
            if ((changeMask0 & (1 << 10)) != 0)
                CharacterInterpolatedDatasprintWeight = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatasprintWeight, compressionModel);
            else
                CharacterInterpolatedDatasprintWeight = baseline.CharacterInterpolatedDatasprintWeight;
            if ((changeMask0 & (1 << 11)) != 0)
                CharacterInterpolatedDatacrouchWeight = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatacrouchWeight, compressionModel);
            else
                CharacterInterpolatedDatacrouchWeight = baseline.CharacterInterpolatedDatacrouchWeight;
            if ((changeMask0 & (1 << 12)) != 0)
                CharacterInterpolatedDataselectorTargetSource = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDataselectorTargetSource, compressionModel);
            else
                CharacterInterpolatedDataselectorTargetSource = baseline.CharacterInterpolatedDataselectorTargetSource;
            if ((changeMask0 & (1 << 13)) != 0)
                CharacterInterpolatedDatamoveAngleLocal = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatamoveAngleLocal, compressionModel);
            else
                CharacterInterpolatedDatamoveAngleLocal = baseline.CharacterInterpolatedDatamoveAngleLocal;
            if ((changeMask0 & (1 << 14)) != 0)
                CharacterInterpolatedDatashootPoseWeight = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatashootPoseWeight, compressionModel);
            else
                CharacterInterpolatedDatashootPoseWeight = baseline.CharacterInterpolatedDatashootPoseWeight;
            if ((changeMask0 & (1 << 15)) != 0)
            {
                CharacterInterpolatedDatalocomotionVectorX = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatalocomotionVectorX, compressionModel);
                CharacterInterpolatedDatalocomotionVectorY = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatalocomotionVectorY, compressionModel);
            }
            else
            {
                CharacterInterpolatedDatalocomotionVectorX = baseline.CharacterInterpolatedDatalocomotionVectorX;
                CharacterInterpolatedDatalocomotionVectorY = baseline.CharacterInterpolatedDatalocomotionVectorY;
            }
            if ((changeMask0 & (1 << 16)) != 0)
                CharacterInterpolatedDatalocomotionPhase = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatalocomotionPhase, compressionModel);
            else
                CharacterInterpolatedDatalocomotionPhase = baseline.CharacterInterpolatedDatalocomotionPhase;
            if ((changeMask0 & (1 << 17)) != 0)
                CharacterInterpolatedDatabanking = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatabanking, compressionModel);
            else
                CharacterInterpolatedDatabanking = baseline.CharacterInterpolatedDatabanking;
            if ((changeMask0 & (1 << 18)) != 0)
                CharacterInterpolatedDatalandAnticWeight = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatalandAnticWeight, compressionModel);
            else
                CharacterInterpolatedDatalandAnticWeight = baseline.CharacterInterpolatedDatalandAnticWeight;
            if ((changeMask0 & (1 << 19)) != 0)
                CharacterInterpolatedDataturnStartAngle = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDataturnStartAngle, compressionModel);
            else
                CharacterInterpolatedDataturnStartAngle = baseline.CharacterInterpolatedDataturnStartAngle;
            if ((changeMask0 & (1 << 20)) != 0)
                CharacterInterpolatedDataturnDirection = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDataturnDirection, compressionModel);
            else
                CharacterInterpolatedDataturnDirection = baseline.CharacterInterpolatedDataturnDirection;
            if ((changeMask0 & (1 << 21)) != 0)
                CharacterInterpolatedDatasquashTime = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatasquashTime, compressionModel);
            else
                CharacterInterpolatedDatasquashTime = baseline.CharacterInterpolatedDatasquashTime;
            if ((changeMask0 & (1 << 22)) != 0)
                CharacterInterpolatedDatasquashWeight = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatasquashWeight, compressionModel);
            else
                CharacterInterpolatedDatasquashWeight = baseline.CharacterInterpolatedDatasquashWeight;
            if ((changeMask0 & (1 << 23)) != 0)
                CharacterInterpolatedDatainAirTime = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatainAirTime, compressionModel);
            else
                CharacterInterpolatedDatainAirTime = baseline.CharacterInterpolatedDatainAirTime;
            if ((changeMask0 & (1 << 24)) != 0)
                CharacterInterpolatedDatajumpTime = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatajumpTime, compressionModel);
            else
                CharacterInterpolatedDatajumpTime = baseline.CharacterInterpolatedDatajumpTime;
            if ((changeMask0 & (1 << 25)) != 0)
                CharacterInterpolatedDatasimpleTime = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatasimpleTime, compressionModel);
            else
                CharacterInterpolatedDatasimpleTime = baseline.CharacterInterpolatedDatasimpleTime;
            if ((changeMask0 & (1 << 26)) != 0)
            {
                CharacterInterpolatedDatafootIkOffsetX = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatafootIkOffsetX, compressionModel);
                CharacterInterpolatedDatafootIkOffsetY = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatafootIkOffsetY, compressionModel);
            }
            else
            {
                CharacterInterpolatedDatafootIkOffsetX = baseline.CharacterInterpolatedDatafootIkOffsetX;
                CharacterInterpolatedDatafootIkOffsetY = baseline.CharacterInterpolatedDatafootIkOffsetY;
            }
            if ((changeMask0 & (1 << 27)) != 0)
            {
                CharacterInterpolatedDatafootIkNormalLeftX = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatafootIkNormalLeftX, compressionModel);
                CharacterInterpolatedDatafootIkNormalLeftY = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatafootIkNormalLeftY, compressionModel);
                CharacterInterpolatedDatafootIkNormalLeftZ = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatafootIkNormalLeftZ, compressionModel);
            }
            else
            {
                CharacterInterpolatedDatafootIkNormalLeftX = baseline.CharacterInterpolatedDatafootIkNormalLeftX;
                CharacterInterpolatedDatafootIkNormalLeftY = baseline.CharacterInterpolatedDatafootIkNormalLeftY;
                CharacterInterpolatedDatafootIkNormalLeftZ = baseline.CharacterInterpolatedDatafootIkNormalLeftZ;
            }
            if ((changeMask0 & (1 << 28)) != 0)
            {
                CharacterInterpolatedDatafootIkNormalRightX = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatafootIkNormalRightX, compressionModel);
                CharacterInterpolatedDatafootIkNormalRightY = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatafootIkNormalRightY, compressionModel);
                CharacterInterpolatedDatafootIkNormalRightZ = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatafootIkNormalRightZ, compressionModel);
            }
            else
            {
                CharacterInterpolatedDatafootIkNormalRightX = baseline.CharacterInterpolatedDatafootIkNormalRightX;
                CharacterInterpolatedDatafootIkNormalRightY = baseline.CharacterInterpolatedDatafootIkNormalRightY;
                CharacterInterpolatedDatafootIkNormalRightZ = baseline.CharacterInterpolatedDatafootIkNormalRightZ;
            }
            if ((changeMask0 & (1 << 29)) != 0)
                CharacterInterpolatedDatafootIkWeight = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatafootIkWeight, compressionModel);
            else
                CharacterInterpolatedDatafootIkWeight = baseline.CharacterInterpolatedDatafootIkWeight;
            if ((changeMask0 & (1 << 30)) != 0)
                CharacterInterpolatedDatablendOutAim = reader.ReadPackedIntDelta(ref ctx, baseline.CharacterInterpolatedDatablendOutAim, compressionModel);
            else
                CharacterInterpolatedDatablendOutAim = baseline.CharacterInterpolatedDatablendOutAim;
        }
    }
    public void Interpolate(ref Char_TerraformerSnapshotData target, float factor)
    {
        SetCharacterInterpolatedDataPosition(math.lerp(GetCharacterInterpolatedDataPosition(), target.GetCharacterInterpolatedDataPosition(), factor));
        SetCharacterInterpolatedDatarotation(Mathf.LerpAngle(GetCharacterInterpolatedDatarotation(), target.GetCharacterInterpolatedDatarotation(), factor));
        SetCharacterInterpolatedDataaimYaw(Mathf.LerpAngle(GetCharacterInterpolatedDataaimYaw(), target.GetCharacterInterpolatedDataaimYaw(), factor));
        SetCharacterInterpolatedDataaimPitch(Mathf.LerpAngle(GetCharacterInterpolatedDataaimPitch(), target.GetCharacterInterpolatedDataaimPitch(), factor));
        SetCharacterInterpolatedDatamoveYaw(Mathf.LerpAngle(GetCharacterInterpolatedDatamoveYaw(), target.GetCharacterInterpolatedDatamoveYaw(), factor));
        SetCharacterInterpolatedDatasprintWeight(math.lerp(GetCharacterInterpolatedDatasprintWeight(), target.GetCharacterInterpolatedDatasprintWeight(), factor));
        SetCharacterInterpolatedDatacrouchWeight(math.lerp(GetCharacterInterpolatedDatacrouchWeight(), target.GetCharacterInterpolatedDatacrouchWeight(), factor));
        SetCharacterInterpolatedDatamoveAngleLocal(Mathf.LerpAngle(GetCharacterInterpolatedDatamoveAngleLocal(), target.GetCharacterInterpolatedDatamoveAngleLocal(), factor));
        SetCharacterInterpolatedDatashootPoseWeight(math.lerp(GetCharacterInterpolatedDatashootPoseWeight(), target.GetCharacterInterpolatedDatashootPoseWeight(), factor));
        SetCharacterInterpolatedDatalocomotionVector(math.lerp(GetCharacterInterpolatedDatalocomotionVector(), target.GetCharacterInterpolatedDatalocomotionVector(), factor));
        SetCharacterInterpolatedDatalocomotionPhase(math.lerp(GetCharacterInterpolatedDatalocomotionPhase(), target.GetCharacterInterpolatedDatalocomotionPhase(), factor));
        SetCharacterInterpolatedDatabanking(math.lerp(GetCharacterInterpolatedDatabanking(), target.GetCharacterInterpolatedDatabanking(), factor));
        SetCharacterInterpolatedDatalandAnticWeight(math.lerp(GetCharacterInterpolatedDatalandAnticWeight(), target.GetCharacterInterpolatedDatalandAnticWeight(), factor));
        SetCharacterInterpolatedDatasquashTime(math.lerp(GetCharacterInterpolatedDatasquashTime(), target.GetCharacterInterpolatedDatasquashTime(), factor));
        SetCharacterInterpolatedDatasquashWeight(math.lerp(GetCharacterInterpolatedDatasquashWeight(), target.GetCharacterInterpolatedDatasquashWeight(), factor));
        SetCharacterInterpolatedDatainAirTime(math.lerp(GetCharacterInterpolatedDatainAirTime(), target.GetCharacterInterpolatedDatainAirTime(), factor));
        SetCharacterInterpolatedDatajumpTime(math.lerp(GetCharacterInterpolatedDatajumpTime(), target.GetCharacterInterpolatedDatajumpTime(), factor));
        SetCharacterInterpolatedDatasimpleTime(math.lerp(GetCharacterInterpolatedDatasimpleTime(), target.GetCharacterInterpolatedDatasimpleTime(), factor));
        SetCharacterInterpolatedDatafootIkOffset(math.lerp(GetCharacterInterpolatedDatafootIkOffset(), target.GetCharacterInterpolatedDatafootIkOffset(), factor));
        SetCharacterInterpolatedDatafootIkNormalLeft(math.lerp(GetCharacterInterpolatedDatafootIkNormalLeft(), target.GetCharacterInterpolatedDatafootIkNormalLeft(), factor));
        SetCharacterInterpolatedDatafootIkNormalRight(math.lerp(GetCharacterInterpolatedDatafootIkNormalRight(), target.GetCharacterInterpolatedDatafootIkNormalRight(), factor));
        SetCharacterInterpolatedDatafootIkWeight(math.lerp(GetCharacterInterpolatedDatafootIkWeight(), target.GetCharacterInterpolatedDatafootIkWeight(), factor));
        SetCharacterInterpolatedDatablendOutAim(math.lerp(GetCharacterInterpolatedDatablendOutAim(), target.GetCharacterInterpolatedDatablendOutAim(), factor));
    }
}
