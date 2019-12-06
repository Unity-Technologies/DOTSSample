using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public partial class PresentationUI : MonoBehaviour, IConvertGameObjectToEntity
{
    public WeakAssetReference[] uiPrefabs;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var assetBuffer = dstManager.AddBuffer<UIElementAsset>(entity);
        for (int i = 0; i < uiPrefabs.Length; i++)
        {
            assetBuffer.Add(new UIElementAsset
            {
                asset = uiPrefabs[i],
            });
        }
    }
}


public partial class PresentationUI
{
    public struct UIElementAsset : IBufferElementData
    {
        public WeakAssetReference asset;
    }

    public struct UIElementEntity : ISystemStateBufferElementData
    {
        public Entity entity;
    }

// TODO: mogensh I disabled this out when removing presentationentity
//    public class UpdateVisibility : ComponentSystem
//    {
//        EntityQuery PresentationOwnerQuery;
//        EntityQuery PresentationQuery;
//        EntityQuery DeletedQuery;
//        protected override void OnCreate()
//        {
//            base.OnCreate();
//            PresentationOwnerQuery = GetEntityQuery(typeof(ServerEntity),
//                typeof(PresentationOwner.State), typeof(UIElementAsset));
//            PresentationQuery = GetEntityQuery(typeof(UIElementAsset), typeof(UIElementEntity),
//                typeof(PresentationEntity));
//            DeletedQuery = GetEntityQuery(ComponentType.Exclude<UIElementAsset>(), typeof(UIElementEntity));
//        }
//
//        protected override void OnUpdate()
//        {
//
//            // Deinitialize when entity has been destroyes
//            // TODO (mogensh) cant use foreach as UIElm still uses GameObjectEntity (because of RectTransform)
//            {
//                var entityArray = DeletedQuery.ToEntityArray(Allocator.TempJob);
//                for(int j=0;j<entityArray.Length;j++)
//                {
//                    var entity = entityArray[j];
//                    var buffer = EntityManager.GetBuffer<UIElementEntity>(entity).ToNativeArray(Allocator.Temp);
//                    for (int i = 0; i < buffer.Length; i++)
//                    {
//                        PrefabAssetManager.DestroyEntity(EntityManager,buffer[i].entity);
//                    }
//                    buffer.Dispose();
//
//                    PostUpdateCommands.RemoveComponent<UIElementEntity>(entity);
//                };
//                entityArray.Dispose();
//            }
//
//
//
//            // TODO (mogensh) cant use foreach as UIElm still uses GameObjectEntity (because of RectTransform)
//            // Make sure currenly visible should remain visible
//            {
//                var entityArray = PresentationQuery.ToEntityArray(Allocator.TempJob);
//                var presentationArray = PresentationQuery.ToComponentDataArray<PresentationEntity>(Allocator.TempJob);
//               // Entities.WithAll<Visible,UIElement>().ForEach((Entity entity, ref PresentationEntity presentation) =>
//                for(int j=0;j<entityArray.Length;j++)
//                {
//                    var entity = entityArray[j];
//                    var presentation = presentationArray[j];
//
//                    var visible = EntityManager.HasComponent<ServerEntity>(presentation.ownerEntity) &&
//                                  EntityManager.HasComponent<PresentationOwner.Visible>(presentation.ownerEntity);
//
//                    if (!visible)
//                    {
//                        var entityBuffer = EntityManager.GetBuffer<UIElementEntity>(entity).ToNativeArray(Allocator.Temp);
//                        for (int i = 0; i < entityBuffer.Length; i++)
//                        {
//                            PrefabAssetManager.DestroyEntity(EntityManager,entityBuffer[i].entity);
//                        }
//                        entityBuffer.Dispose();
//                        PostUpdateCommands.RemoveComponent<UIElementEntity>(entity);
//                    }
//                };
//                entityArray.Dispose();
//                presentationArray.Dispose();
//            }
//
//            // Find all server presentation owners and check their presentationUI is set visible
//            {
//                var entityArray = PresentationOwnerQuery.ToEntityArray(Allocator.TempJob);
//                var presentationOwnerArray = PresentationOwnerQuery.ToComponentDataArray<PresentationOwner.State>(Allocator.TempJob);
//
//                for(int j=0;j<presentationOwnerArray.Length;j++)
//                {
//                    // Filter out invisible presentation owners
//                    var presentationOwner = presentationOwnerArray[j];
//                    var presentationOwnerEntity = entityArray[j];
//                    if (!EntityManager.HasComponent<PresentationOwner.Visible>(presentationOwnerEntity))
//                        continue;
//
//                    // Filter out owners without presentation
//                    var presentation = presentationOwner.currentPresentation;
//                    if (presentation == Entity.Null)
//                        continue;
//
//                    // Filter out presentationUI that already has been set visible
//                    if (EntityManager.HasComponent<UIElementEntity>(presentation))
//                        continue;
//
//                    var entityBuffer = PostUpdateCommands.AddBuffer<UIElementEntity>(presentation);
//
//                    var count = EntityManager.GetBuffer<UIElementAsset>(presentation).Length;
//                    for (int i = 0; i < count; i++)
//                    {
//                        var element = EntityManager.GetBuffer<UIElementAsset>(presentation)[i];
//                        var entity = PrefabAssetManager.CreateEntity(World, element.asset);
//
//                        var abilityUI = EntityManager.GetComponentData<AbilityUI.State>(entity);
//                        abilityUI.presentationOwner = entityArray[j];
//                        EntityManager.SetComponentData(entity,abilityUI);
//
//                        var hud = GameObject.FindObjectOfType<IngameHUD>(); // TODO (mogensh) FindObjectOfType YAY !
//
//                        var transform = EntityManager.GetComponentObject<RectTransform>(entity);
//                        transform.SetParent(hud.transform, false);
//
//                        entityBuffer.Add( new UIElementEntity
//                        {
//                            entity = entity,
//                        });
//                    }
//                }
//                entityArray.Dispose();
//                presentationOwnerArray.Dispose();
//            }
//        }
//    }
}
