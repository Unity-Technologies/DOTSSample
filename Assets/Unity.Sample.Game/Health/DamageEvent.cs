using UnityEngine;
using Unity.Entities;

public struct DamageEvent : IBufferElementData
{
    public Entity Target;
    public Entity Instigator;
    public float Damage;
    public Vector3 Direction;
    public float Impulse;
}
