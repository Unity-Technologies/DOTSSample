using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR

[DisallowMultipleComponent]
public class CharacterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{

    public AbilityCollectionAuthoring.AbilitySetup[] abilities = new AbilityCollectionAuthoring.AbilitySetup[0];

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Character.Settings());

        dstManager.AddComponentData(entity, new Character.InterpolatedData());
        dstManager.AddComponentData(entity, new Character.PredictedData());
        dstManager.AddComponentData(entity, new Character.ReplicatedData());

        dstManager.AddComponentData(entity, Player.OwnerPlayerId.Default);

        dstManager.AddComponentData(entity, new PlayerControlled.State());

        dstManager.AddComponentData(entity,AimData.Data.Default);

        dstManager.AddComponentData(entity, new HitColliderOwner.State
        {
            collisionEnabled = 1,
        });

        InventoryAuthoring.AddInventoryComponents(entity, dstManager, conversionSystem);

        dstManager.AddComponentData(entity, new HealthStateData());
        dstManager.AddBuffer<DamageEvent>(entity);
        dstManager.AddComponentData(entity, new DamageHistoryData());

        AbilityCollectionAuthoring.AddAbilityComponents(entity, dstManager, conversionSystem, abilities);

        AbilityOwnerAuthoring.AddAbilityOwnerComponents(entity, dstManager, conversionSystem);
    }
}

#endif
