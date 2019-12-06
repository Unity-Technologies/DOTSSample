using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

[Serializable]
public class TeleporterClientDataClass : IComponentData, IEquatable<TeleporterClientDataClass>
{
    public VisualEffectAsset effect;

    public bool Equals(TeleporterClientDataClass other)
    {
        return effect != null && other.effect != null && other.effect.Equals(effect);
    }

    public override int GetHashCode()
    {
        return effect != null ? effect.GetHashCode() : 0;
    }
}

public struct TeleporterClientData : IComponentData
{
    public TickEventHandler effectEvent;
    public float3 effectPos;
}

[DisallowMultipleComponent]
[ClientOnlyComponent]
public class TeleporterClient : MonoBehaviour, IConvertGameObjectToEntity
{
    public VisualEffectAsset effect;
    public Transform effectTransform;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var shared = new TeleporterClientDataClass();
        shared.effect = effect;
        dstManager.AddComponentData(entity, shared);

        var data = new TeleporterClientData();
        data.effectEvent = new TickEventHandler(0.5f);
        data.effectPos = effectTransform.localPosition;
        dstManager.AddComponentData(entity, data);

    }
}
