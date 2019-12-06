using Unity.Entities;
using UnityEngine;

public struct HitCollider
{
    public struct Owner : IComponentData
    {
        public Entity Value;
    }
}
