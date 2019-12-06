using Unity.Entities;
using UnityEngine;

public class AnimConditionSprintAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var state = AnimConditionSprint.State.Default;
        dstManager.AddComponentData(entity,state);
    }
}
