using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Sample.Core;
using Unity.Transforms;
using UnityEngine.Profiling;

public class RigAttacher
{
    public struct AttachEntity : IComponentData
    {
        public Entity Value;
    }

    public struct AttachBone : IComponentData
    {
        public RuntimeBoneReference Value;
    }

    public struct State : IComponentData
    {
        public static State Default => new State { LastMappedBoneRef = RuntimeBoneReference.Default, boneIndex = -1,};

        public Entity rigEntity;

        public RuntimeBoneReference LastMappedBoneRef;

        public int boneIndex;
    }

    public static void AddRigAttacher(Entity entity, EntityManager dstManager, RuntimeBoneReference boneRef)
    {
        var attachBone = new AttachBone
        {
            Value = boneRef,
        };
        dstManager.AddComponentData(entity, attachBone);
        dstManager.AddComponentData(entity, State.Default);

        if(dstManager.HasComponent<Static>(entity))
            dstManager.AddComponentData(entity,new Static());

        if(dstManager.HasComponent<Static>(entity))
            dstManager.AddComponentData(entity,new Static());

        if(dstManager.HasComponent<Parent>(entity))
            dstManager.RemoveComponent<Parent>(entity);
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AnimationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    [AlwaysSynchronizeSystem]
    public class Update : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // TODO (mogensh) When all rig remapping happens we animstream, we no longer need to handle rig change
            Profiler.BeginSample("RigAttach.HandleChange");
            Entities
                .WithoutBurst() // EntityManager.Exists() and EntityManager.GetSharedComponentData() are not Burst-compatible
                .ForEach((Entity entity, ref AttachEntity attachEntity, ref AttachBone attachBone, ref State state) =>
            {
                if (!EntityManager.Exists(attachEntity.Value))
                {
                    GameDebug.LogWarning(World,"Attach entity:{0}" + attachEntity.Value + " does no longer exist");
                    return;
                }

                // TODO (mogensh) dont check this every frame. Instead 
                // Find bone index
                if (!attachBone.Value.Equals(state.LastMappedBoneRef) ||
                    attachEntity.Value != state.rigEntity)
                {
                    if (attachEntity.Value != Entity.Null)
                    {
                        Profiler.BeginSample("GetSharedRigDef");
                        var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(attachEntity.Value);
                        Profiler.EndSample();
                        
                        if (attachBone.Value.ReferenceRig.Value.GetHashCode() == sharedRigDef.Value.Value.GetHashCode())
                        {
                            state.boneIndex = attachBone.Value.BoneIndex;
                        }
                        else
                        {
                            Profiler.BeginSample("GetOrCreateRigMapping");
                            BlobAssetReference<AnimationAssetDatabase.RigMap> rigMap;
                            AnimationAssetDatabase.GetOrCreateRigMapping(World, attachBone.Value.ReferenceRig, sharedRigDef.Value, out rigMap);
                            Profiler.EndSample();
                            state.boneIndex = rigMap.Value.BoneMap[attachBone.Value.BoneIndex];
                        }
                    }
                    else
                    {
                        state.boneIndex = -1;
                    }

                    state.LastMappedBoneRef = attachBone.Value; // new RuntimeBoneReference(attachBone.Value);
                    state.rigEntity = attachEntity.Value;
                }
            }).Run();
            Profiler.EndSample();

            var AnimatedLocalToWorldFromEntity = GetBufferFromEntity<AnimatedLocalToWorld>(true);
            var LocalToParentFromEntity = GetComponentDataFromEntity<LocalToParent>(true);
            Profiler.BeginSample("RigAttach.Move");
            Entities
                .WithReadOnly(AnimatedLocalToWorldFromEntity)
                .WithReadOnly(LocalToParentFromEntity)
                .ForEach((Entity entity, ref LocalToWorld localToWorld, ref Translation translation, ref Rotation rotation,
                    in AttachEntity attachEntity, in AttachBone attachBone, in State state) =>
            {
                if (!AnimatedLocalToWorldFromEntity.Exists(attachEntity.Value))
                {
                    //GameDebug.LogWarning(World, string.Format("RigAttacher:{0} attacheEntity:{1} has not AnimatedLocalToWorld", entity,attachEntity.Value));
                    return;
                }

                // Move
                if (state.boneIndex != -1)
                {
                    var localToWorldBuffer = AnimatedLocalToWorldFromEntity[attachEntity.Value];
                    var boneLocalToWorld = localToWorldBuffer[state.boneIndex].Value;

                    if (LocalToParentFromEntity.HasComponent(entity))
                    {
                        var attacherLocalToParent = LocalToParentFromEntity[entity];
                        boneLocalToWorld = math.mul(boneLocalToWorld, attacherLocalToParent.Value);
                    }
                    localToWorld = new LocalToWorld
                    {
                        Value = boneLocalToWorld
                    };


                    // TODO (mogensh) Unity.Physics uses translation+rotation as input so they also need to be set.
                    translation = new Translation
                    {
                        Value = boneLocalToWorld.c3.xyz,
                    };
                    rotation = new Rotation
                    {
                        Value = new quaternion(boneLocalToWorld),
                    };
                }
            }).Run();
            Profiler.EndSample();
            return default;
        }
    }

}
