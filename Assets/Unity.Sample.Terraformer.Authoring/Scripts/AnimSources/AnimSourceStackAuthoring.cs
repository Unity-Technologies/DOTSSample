#if UNITY_EDITOR

using Unity.Entities;


public class AnimSourceStackAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());
        dstManager.AddComponentData(entity, AnimSource.AllowWrite.Default);

        // Add child AnimSources
        var childAnimSourceEntities =  dstManager.AddBuffer<AnimSourceStack.AnimSourceEntities>(entity);
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);

            if (child.GetComponent<AnimSourceAuthoring>() == null)
                continue;

            var entryEntity = conversionSystem.GetPrimaryEntity(child.gameObject);
            var e = new AnimSourceStack.AnimSourceEntities { Value = entryEntity };
            childAnimSourceEntities.Add(e);
        }
    }
}

#endif
