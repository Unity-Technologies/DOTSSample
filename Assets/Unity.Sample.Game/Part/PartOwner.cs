using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Sample.Core;
using Unity.Transforms;
using UnityEngine;


public  class PartOwner
{
    public struct Visible : IComponentData
    {}

    public struct RegistryAsset : IComponentData
    {
        public RegistryAsset(WeakAssetReference value)
        {
            Value = value;
        }

        public WeakAssetReference Value;
    }

    public struct InputState : IComponentData //, IReplicatedComponent
    {
        public static InputState Default => new InputState { };
        public uint PackedPartIds;

//        public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
//        {
//            return new ReplicatedComponentSerializerFactory<InputState>();
//        }
//
//        public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
//        {
//            writer.WriteUInt32("parts",PackedPartIds);
//        }
//
//        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
//        {
//            PackedPartIds = reader.ReadUInt32();
//        }
    }

    struct State : ISystemStateComponentData
    {
        public static State Default => new State { currentLOD = -1, };

        public uint CurrentPackedPartIds;
        public int currentLOD;
        public BlobAssetReference<RigDefinition> currentRig;
    }

    struct PartElement : ISystemStateBufferElementData
    {
        public static PartElement Default => new PartElement();
        public Entity PartEntity;
        public int PartId;
        public WeakAssetReference Asset;
    }

    [UpdateInGroup(typeof(PartSystemUpdateGroup))]
    class Initialization : ComponentSystem
    {
        protected override void OnUpdate()
        {

            Entities.WithNone<State>().ForEach((Entity entity, ref RegistryAsset registryAsset) =>
            {
                GameDebug.Log(World, Part.ShowLifetime, "Initializing PartOwner:{0}", entity);

                var registry = PartRegistry.GetPartRegistry(World, registryAsset.Value);
                var categoryCount = registry.Value.GetCategoryCount();

                var buffer = PostUpdateCommands.AddBuffer<PartElement>(entity);
                for(int i=0;i<categoryCount;i++)
                    buffer.Add(PartElement.Default);

                PostUpdateCommands.AddComponent(entity,State.Default);
            });
            
            Entities.WithNone<InputState>().ForEach((Entity entity, ref State state) =>
            {
                GameDebug.Log(World,Part.ShowLifetime, "Deinitializing PartOwner:{0}", entity);

                var partBuffer = EntityManager.GetBuffer<PartElement>(entity);
                for (int i = 0; i < partBuffer.Length; i++)
                {
                    if (partBuffer[i].PartEntity != Entity.Null)
                    {
                        PostUpdateCommands.DestroyEntity(partBuffer[i].PartEntity);
                        GameDebug.Log(Part.ShowLifetime, "   destroying part:{0}", partBuffer[i].PartEntity);
                    }

                }
                PostUpdateCommands.RemoveComponent<State>(entity);
                PostUpdateCommands.RemoveComponent<PartElement>(entity);
            });
        }
    }

    [UpdateInGroup(typeof(PartSystemUpdateGroup))]
    [UpdateAfter(typeof(Initialization))]
    public class Update : ComponentSystem
    {
        private Camera MainCamera;

        protected override void OnCreate()
        {
            base.OnCreate();
            
        }

        protected override void OnUpdate()
        {
            // Camera.main may not be available when the system is created, so need this to set it for the first time
            if (MainCamera == null)
            {
                if (Camera.main != null)
                {
                    MainCamera = Camera.main;
                }
                else
                {
                    GameDebug.LogWarning("PartOwner update: No camera.main");
                    return;
                }
            }

            var camPos = (float3) MainCamera.transform.position;
            
            // TODO: Jobified ForEach blocked by PrefabAssetRegistry.CreateEntity
            Entities.ForEach((Entity partOwnerEntity, ref Translation translation, ref RegistryAsset registryAsset, ref InputState inputState,
                ref State state) =>
            {
                var registry = PartRegistry.GetPartRegistry(World, registryAsset.Value);

                // Calc lod
                var charPos = translation.Value;
                var dist = math.distance(camPos, charPos);
                var newLod = -1;
                // TODO (mogensh) add threshold that needs to be passed before change (so it does not flicker)
                for (int lod = 0; lod < registry.Value.LODLevels.Length; lod++)
                {
                    if (dist <= registry.Value.LODLevels[lod].EndDist)
                    {
                        newLod = lod;
                        break;
                    }
                }


                // TODO (mogensh) hack: force LOD 0
                newLod = 0;


                // Handle out of lod distance specifically
                if (newLod == -1)
                {
                    if (state.currentLOD != newLod)
                    {
                        state.currentLOD = newLod;

                        GameDebug.Log(Part.ShowLifetime, "Out of LOD distance");

                        var partBuf = EntityManager.GetBuffer<PartElement>(partOwnerEntity)
                            .ToNativeArray(Allocator.Temp);
                        var partOutBuf = PostUpdateCommands.SetBuffer<PartElement>(partOwnerEntity);
                        partOutBuf.ResizeUninitialized(partBuf.Length);
                        for (int j = 0; j < partBuf.Length; j++)
                        {
                            var partElement = partBuf[j];

                            // Destroy old part
                            if (partElement.PartId != 0)
                            {
                                if (partElement.PartEntity != Entity.Null)
                                {
                                    GameDebug.Log(Part.ShowLifetime, "Destroying part. Category:{0} partId:{1}", j,
                                        partElement.PartId);
                                    PostUpdateCommands.DestroyEntity(partElement.PartEntity);
                                }

                                partElement.PartEntity = Entity.Null;
                                partElement.PartId = 0;
                                partElement.Asset = WeakAssetReference.Default;
                            }

                            partOutBuf[j] = partElement;
                        }

                        PostUpdateCommands.SetComponent(partOwnerEntity, state);
                    }


                    return;
                }



                var newRig = BlobAssetReference<RigDefinition>.Null;
                if (EntityManager.HasComponent<SharedRigDefinition>(partOwnerEntity))
                {
                    newRig = EntityManager.GetSharedComponentData<SharedRigDefinition>(partOwnerEntity).Value;
                }


                // Change bodypart if LOD or rig changed
                var packedPartIds = inputState.PackedPartIds;
                if (packedPartIds != state.CurrentPackedPartIds ||
                    newLod != state.currentLOD ||
                    (newRig != BlobAssetReference<RigDefinition>.Null && newRig != state.currentRig))
                {
                    var partBuf = EntityManager.GetBuffer<PartElement>(partOwnerEntity).ToNativeArray(Allocator.Temp);


                    var partIds = new NativeArray<int>(partBuf.Length, Allocator.Temp);
                    registry.Value.UnpackPartsList(inputState.PackedPartIds, partIds);

                    GameDebug.Log(World, Part.ShowLifetime, "Property changed. Lod:{0}", newLod);

                    var partOutBuf = PostUpdateCommands.SetBuffer<PartElement>(partOwnerEntity);
                    partOutBuf.ResizeUninitialized(partBuf.Length);
                    for (int j = 0; j < partBuf.Length; j++)
                    {
                        var partId = partIds[j];
                        var partElement = partBuf[j];

                        // Find new asset given the new properties
                        var asset = new WeakAssetReference();
                        if (partId > 0)
                        {
                            var skeletonHash = newRig.IsCreated ? newRig.Value.GetHashCode() : 0;
                            var found = registry.Value.FindAsset(j, partId, skeletonHash, newLod, ref asset);
                            if (!found)
                                GameDebug.Log(World, Part.ShowLifetime,
                                    "Failed to find valid part. Category:{0} PartId:{1}", j, partId);
                        }


                        // No change if asset has not changed
                        if (partElement.Asset == asset)
                        {
                            partOutBuf[j] = partElement;
                            continue;
                        }

                        
                        // Destroy old part
                        if (partElement.PartId != 0)
                        {
                            if (partElement.PartEntity != Entity.Null)
                            {
                                GameDebug.Log(World, Part.ShowLifetime, "Destroying part. Category:{0} partId:", j,
                                    partElement.PartId);
                                PostUpdateCommands.DestroyEntity(partElement.PartEntity);
                            }

                            partElement.PartEntity = Entity.Null;
                            partElement.PartId = 0;
                            partElement.Asset = WeakAssetReference.Default;
                        }

                        // Create new part
                        if (partId != 0 && asset.IsSet())
                        {
                            partElement.PartEntity = PrefabAssetManager.CreateEntity(EntityManager, asset);
                            partElement.PartId = partId;
                            partElement.Asset = asset;

                            if (partElement.PartEntity != Entity.Null)
                            {
                                GameDebug.Log(World, Part.ShowLifetime,
                                    "Creating part. Owner:{0} Cat:{1} PartId:{2} Asset:{3} part:{4}", partOwnerEntity,
                                    j, partId, asset.ToGuidStr(), partElement.PartEntity);
                                var part = Part.Owner.Default;
                                part.Value = partOwnerEntity;
                                PostUpdateCommands.SetComponent(partElement.PartEntity, part);

                                // TODO (mogensh) add "static" property on owner (or get somehow). If static just set world transform
                                PostUpdateCommands.AddComponent(partElement.PartEntity,
                                    new Parent {Value = partOwnerEntity});
                                PostUpdateCommands.AddComponent(partElement.PartEntity, new LocalToParent());
                            }
                            else
                            {
                                GameDebug.LogError("Failed to create part. Asset:" + asset.ToGuidStr());
                            }
                        }

                        partOutBuf[j] = partElement;
                    }

                    state.CurrentPackedPartIds = packedPartIds;
                    state.currentRig = newRig;
                    state.currentLOD = newLod;
                    PostUpdateCommands.SetComponent(partOwnerEntity, state);
                }
            });
        }
    }
}

