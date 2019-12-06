using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SoundRequest : IComponentData
{
    public WeakAssetReference soundDefRef;
    public float3 position;
    public Entity trackEntity;
    public SoundSystem.SoundHandle soundHandle;
}
