using Unity.Entities;
using UnityEngine;

[ServerOnlyComponent]
public partial class SpawnPoint : MonoBehaviour, IConvertGameObjectToEntity
{
    public int teamIndex;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity,new State
        {
            teamIndex = teamIndex,
        });

#if UNITY_EDITOR
        dstManager.SetName(entity, "Entity " + entity.Index + " GameObject:" + name);
#endif

    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.DrawCube(transform.position + transform.up, new Vector3(0.5f, 2.0f, 0.5f));
        Gizmos.DrawRay(transform.position + transform.up * 1.5f, transform.forward);
    }

#endif
}



public partial class SpawnPoint
{
    public struct State : IComponentData
    {
        public int teamIndex;
    }
}
