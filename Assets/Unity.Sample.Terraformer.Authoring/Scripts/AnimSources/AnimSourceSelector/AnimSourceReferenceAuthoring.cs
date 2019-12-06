using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR

public class AnimSourceReferenceAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public WeakAssetReference animSource;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var state = AnimSourceReference.State.Default;
        state.animSource = animSource;
        dstManager.AddComponentData(entity,state);
    }
}

#endif
