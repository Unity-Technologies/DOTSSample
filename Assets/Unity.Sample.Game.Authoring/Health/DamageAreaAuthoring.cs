using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class DamageAreaAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public bool instantKill = false;
    public float hitsPerSecond = 3;
    public float damagePerHit = 25;
    public float3 size = new float3(5, 5, 5);

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var da = new DamageArea();
        da.instantKill = instantKill;
        da.hitsPerSecond = hitsPerSecond;
        da.damagePerHit = damagePerHit;
        da.size = size*0.5f; // we prefer the runtime to be the extend along each axis, not total size
        dstManager.AddComponentData(entity, da);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        if (gameObject == UnityEditor.Selection.activeGameObject)
        {
            // If we are directly selected (and not just our parent is selected)
            // draw with negative size to get an 'inside out' cube we can see from the inside
            Gizmos.color = new Color(1.0f, 1.0f, 0.5f, 0.8f);
            Gizmos.DrawCube(Vector3.zero, -size);
        }
        Gizmos.color = new Color(1.0f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawCube(Vector3.zero, size);
    }

#endif
}
