using Unity.Entities;

public class InventoryAuthoring
{
    public static void AddInventoryComponents(Entity entity, EntityManager dstManager,
        GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<Inventory.ItemEntry>(entity);

        dstManager.AddComponentData(entity, new Inventory.State
        {
            activeSlot = 0,
        });

        dstManager.AddComponentData(entity, new Inventory.InternalState
        {
            lastActiveInventorySlot = -1,
        });
    }
}

