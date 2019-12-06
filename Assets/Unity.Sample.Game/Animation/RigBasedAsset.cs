using System.Collections;
using System.Collections.Generic;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;

public class RigBasedAsset 
{
    public struct Base : IComponentData
    {}
    
    public struct Attachment : IBufferElementData
    {
        public Entity Value;
    }

    public struct SkinnedMeshRenderer : IBufferElementData
    {
        public Entity Value;
    }
    
    public struct Initialized : ISystemStateComponentData
    {}
    
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    class Initialize : ComponentSystem
    {
        protected override void OnUpdate()
        {
            // Initialize
            Entities.WithNone<Initialized>().WithAll<Base>().ForEach((Entity entity, ref RigDefinitionSetup rigDefSetup) =>
            {
                // TODO (mogensh) make this an option. If we remap content to another rigentity we dont want to update rig here
                RigEntityBuilder.SetupRigEntity(entity, EntityManager, rigDefSetup.Value);
                if (EntityManager.HasComponent<SkinnedMeshRenderer>(entity))
                {
                    var animatedSkinMatricesArray = EntityManager.AddBuffer<AnimatedLocalToRig>(entity);
                    animatedSkinMatricesArray.ResizeUninitialized(rigDefSetup.Value.Value.Skeleton.BoneCount);
                }
                
                EntityManager.SetSharedComponentData(entity, new SharedRigDefinition {Value = rigDefSetup.Value});
                
                PostUpdateCommands.AddComponent(entity, new Initialized());
            });

            // Deinitialize
            Entities.WithNone<Base>().WithAll<Initialized>().ForEach((Entity entity) =>
            {
                PostUpdateCommands.RemoveComponent<Initialized>(entity);
            });
        }
    }
 
    
}
