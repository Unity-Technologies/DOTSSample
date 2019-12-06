
using Unity.Animation;
using Unity.Entities;
using Unity.Sample.Core;
using UnityEngine;

[DisableAutoCreation]
public class PartSystemUpdateGroup : ComponentSystemGroup
{}



public class Part
{
    [ConfigVar(Name = "part.show.conversion", DefaultValue = "0", Description = "Show part conversion data")]
    public static ConfigVar ShowConversion;

    [ConfigVar(Name = "part.show.lifetime", DefaultValue = "0", Description = "Show part conversion data")]
    public static ConfigVar ShowLifetime;

    public struct Owner : IComponentData
    {
        public static Owner Default => new Owner();
        public Entity Value;
    }
    
    public struct AttachmentsMapped : ISystemStateComponentData
    {}

    public struct SkinnedMeshMapped : ISystemStateComponentData
    {}

    [UpdateInGroup(typeof(PartSystemUpdateGroup))]
    [UpdateAfter(typeof(PartOwner.Update)) ]
    class Initialize : ComponentSystem
    {
        protected override void OnUpdate()
        {
            // Initialize skinned mesh
            Entities.WithNone<SkinnedMeshMapped>().WithAll<RigBasedAsset.SkinnedMeshRenderer>().ForEach((Entity entity,
                ref Owner partOwner) =>
            {
                PostUpdateCommands.AddComponent(entity, new SkinnedMeshMapped());

                // TODO (mogensh) make it optional if part should remap to owner (or use remap node to update part rig)
                // TODO (mogensh) delete rig setup on part if we remap                 
                
                var buffer = EntityManager.GetBuffer<RigBasedAsset.SkinnedMeshRenderer>(entity);
                for (int i = 0; i < buffer.Length; i++)
                {
                    // TODO (mogensh) do rig remapping (here we assume the same rig)

                    PostUpdateCommands.SetComponent(buffer[i].Value, new SkinnedMeshRigEntity
                    {
                        Value = partOwner.Value,
                    });
                }
            });
            
            Entities.WithNone<Owner>().WithAll<SkinnedMeshMapped>().ForEach((Entity entity) =>
            {
                PostUpdateCommands.RemoveComponent<SkinnedMeshMapped>(entity);
            });
            
            // Initialize RigAttacher
            Entities.WithNone<AttachmentsMapped>().WithAll<RigBasedAsset.Attachment>().ForEach((Entity entity,
                ref Owner partOwner) =>
            {
                PostUpdateCommands.AddComponent(entity, new AttachmentsMapped());

                var buffer = EntityManager.GetBuffer<RigBasedAsset.Attachment>(entity);
                for (int i = 0; i < buffer.Length; i++)
                {
                    // TODO (mogensh) do rig remapping (here we assume the same rig)

                    PostUpdateCommands.SetComponent(buffer[i].Value, new RigAttacher.AttachEntity()
                    {
                        Value = partOwner.Value,
                    });
                    
                    // Map HitCollider (if any)
                    // TODO (mogensh) can we avoid that part knows about HitColliders? Should HitCollider know about part concept?
                    if (EntityManager.HasComponent<HitCollider.Owner>(buffer[i].Value))
                    {
                        PostUpdateCommands.SetComponent(buffer[i].Value,new HitCollider.Owner
                        {
                            Value = partOwner.Value,
                        });
                    }
                }
            });

            Entities.WithNone<Owner>().WithAll<AttachmentsMapped>().ForEach((Entity entity) =>
            {
                PostUpdateCommands.RemoveComponent<AttachmentsMapped>(entity);
            });
        }
    }
}
