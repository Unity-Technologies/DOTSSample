using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct TeleporterServer : IComponentData
{
    public Entity targetTeleporter;
    public Entity entityInside;
    public float3 spawnPos;
    public float3 triggerPos;
    public float triggerDist;
    public quaternion spawnRot;
}

[DisallowMultipleComponent]
[ServerOnlyComponent]
public class TeleporterServerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public TeleporterServerAuthoring targetTeleporter;
    public Transform spawnMarker;
    public Transform triggerMarker;
    public float triggerDist = 1.0f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var t = new TeleporterServer();
        t.spawnPos = spawnMarker.transform.position;
        t.spawnRot = spawnMarker.transform.rotation;
        t.triggerPos = triggerMarker.transform.position;
        t.targetTeleporter = conversionSystem.GetPrimaryEntity(targetTeleporter);
        t.triggerDist = triggerDist;
        dstManager.AddComponentData(entity, t);
        dstManager.AddComponentData(entity, new TeleporterPresentationData());
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (UnityEditor.Selection.activeGameObject == null)
            return;
        if (triggerMarker != null && gameObject == UnityEditor.Selection.activeGameObject)
        {
            // If we are directly selected (and not just our parent is selected)
            // draw with negative size to get an 'inside out' cube we can see from the inside
            Gizmos.color = new Color(1.0f, 1.0f, 0.5f, 0.8f);
            Gizmos.DrawSphere(triggerMarker.position, triggerDist);
        }
    }

#endif
}
