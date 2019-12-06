using Unity.Entities;
using UnityEngine;

public class AnimConditionMovementAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AbilityMovement.LocoState requiredLocoState;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var state = AnimConditionMovement.State.Default;
        state.requiredLocoState = requiredLocoState;
        dstManager.AddComponentData(entity,state);
    }
}
