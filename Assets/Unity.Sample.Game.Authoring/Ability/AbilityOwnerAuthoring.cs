using Unity.Entities;

#if UNITY_EDITOR

public class AbilityOwnerAuthoring
{
    public static void AddAbilityOwnerComponents(Entity entity, EntityManager dstManager,
        GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity,new AbilityOwner.State());

        dstManager.AddBuffer<AbilityOwner.OwnedCollection>(entity);
        dstManager.AddBuffer<AbilityOwner.OwnedAbility>(entity);
    }
}

#endif

