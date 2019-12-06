using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

public class PlayerCharacterControlAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity,new PlayerCharacterControl.State
        {
            characterType = -1,
            requestedCharacterType = -1,
        });
    }
}
