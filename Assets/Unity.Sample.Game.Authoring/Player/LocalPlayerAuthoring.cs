using UnityEngine;
using Unity.Entities;

#if UNITY_EDITOR

public class LocalPlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IPrefabAsset
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var lp = LocalPlayer.Default();
        var buf = dstManager.AddBuffer<UserCommand>(entity);
        buf.ResizeUninitialized(PlayerModuleClient.commandClientBufferSize);
        dstManager.AddComponentData(entity, lp);
    }
}

#endif


