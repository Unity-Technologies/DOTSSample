using Unity.Entities;
using UnityEngine;

public class AnimConditionDeadAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var state = AnimConditionDead.State.Default;
        dstManager.AddComponentData(entity,state);
    }
}
