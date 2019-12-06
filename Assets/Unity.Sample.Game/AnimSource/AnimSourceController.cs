using System;
using Unity.Animation;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Sample.Core;
using Unity.Transforms;
using UnityEngine;

[ExecuteAlways]
[DisableAutoCreation]
public class AnimSourceRootSystemGroup : ManualComponentSystemGroup
{
}

// Update the conditions that inform the behavior tree in the state selector
[UpdateInGroup(typeof(AnimSourceRootSystemGroup))]
[DisableAutoCreation]
public class AnimConditionUpdate : ManualComponentSystemGroup// TODO (mogensh) this belongs together with AnimConditions. But that all needs to be rewritten anyway
{
}

// Update anim sources
// This is the only place new AnimSource entities can be created
// This is only place rig changes are allowed
[UpdateInGroup(typeof(AnimSourceRootSystemGroup))]
[UpdateAfter(typeof(AnimConditionUpdate))]
[DisableAutoCreation]
public class AnimSourcePreUpdateGroup : ManualComponentSystemGroup
{
}

// Initialize newly created AnimSources and deinitializes deleted.
// AnimSource output (and input if it has one) is required to be set in this update
// Done after update so newly instantiated sources will be initialized this frame
[UpdateInGroup(typeof(AnimSourceRootSystemGroup))]
[UpdateAfter(typeof(AnimSourcePreUpdateGroup))]
[DisableAutoCreation]
public class AnimSourceInitializationGroup : ManualComponentSystemGroup
{
}


// Anim state update groups
[UpdateInGroup(typeof(AnimSourceRootSystemGroup))]
[UpdateAfter(typeof(AnimSourceInitializationGroup))]
[DisableAutoCreation]
public class AnimSourceUpdateAGroup : ManualComponentSystemGroup
{
}

[UpdateInGroup(typeof(AnimSourceRootSystemGroup))]
[UpdateAfter(typeof(AnimSourceUpdateAGroup))]
[DisableAutoCreation]
public class AnimSourceUpdateBGroup : ManualComponentSystemGroup
{
}

[UpdateInGroup(typeof(AnimSourceRootSystemGroup))]
[UpdateAfter(typeof(AnimSourceUpdateBGroup))]
[DisableAutoCreation]
public class AnimSourceUpdateCGroup : ManualComponentSystemGroup
{
}

// Prepare graph for rendering. Connect AnimSources. Update rig
[UpdateInGroup(typeof(AnimSourceRootSystemGroup))]
[UpdateAfter(typeof(AnimSourceUpdateCGroup))]
[DisableAutoCreation]
public class AnimSourceApplyGroup : ManualComponentSystemGroup
{
}



public class AnimSourceController
{
    [ConfigVar(Name = "animsourcecontroller.show.lifetime", DefaultValue = "0", Description = "Show part conversion data")]
    public static ConfigVar ShowLifetime;


    public struct LOD : ISystemStateComponentData
    {
        public static LOD Default => new LOD {Value = -1};
        public int Value;
    }

    public struct RootAnimSource : ISystemStateComponentData
    {
        public static RootAnimSource Default => new RootAnimSource( );
        public Entity Value;
    }

    public struct OutputNode : ISystemStateComponentData
    {
        public static OutputNode Default => new OutputNode( );
        public NodeHandle<BoundaryNode> Value;
    }

    public struct Settings : IComponentData
    {
        public static Settings Default => new Settings();
        public WeakAssetReference RootAnimSource;
    }

    public struct RigData : IBufferElementData
    {
        public BlobAssetReference<RigDefinition> Rig;
        public float MaxDist;
    }

    public Settings settings;

    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(AnimSourcePreUpdateGroup))]
    [DisableAutoCreation]
    public class Initialization : JobComponentSystem
    {
        EntityQuery OutgoingGroup;

        AnimationGraphSystem m_AnimationGraphSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            OutgoingGroup = GetEntityQuery(typeof(LOD), ComponentType.Exclude<Settings>());

            m_AnimationGraphSystem = World.GetExistingSystem<AnimationGraphSystem>();
            m_AnimationGraphSystem.AddRef();
            m_AnimationGraphSystem.Set.RendererModel = RenderExecutionModel.Islands;
        }
        protected override void OnDestroy()
        {
            Entities
                .WithStructuralChanges()
                .WithAll<LOD>()
                .ForEach((Entity entity) =>
            {
                Deinitialize(EntityManager, entity, m_AnimationGraphSystem);
            }).Run();

            m_AnimationGraphSystem.RemoveRef();
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            inputDependencies.Complete();
            
            var animGraphSys = World.GetExistingSystem<AnimationGraphSystem>();

            // Initialize
            Entities
                .WithStructuralChanges()
                .WithNone<LOD>()
                .WithAll<Settings>()
                .ForEach((Entity entity) =>
            {
                GameDebug.Log(World, ShowLifetime, "InitSys: Initialize DotsAnimStateCtrl:{0}", entity);

                // Setup lowest LOD rig
                var rigDataBuffer = EntityManager.GetBuffer<RigData>(entity);
//                var lowestLod = rigDataBuffer.Length - 1;
                // TODO (mogensh) for now we only use Rig LOD for selecting low lod on server
                var isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;// TODO (mogensh) cant we find better way to test for server?
                var lod = isServer ? 1 : 0;
                var rigData = rigDataBuffer[lod];
                RigEntityBuilder.SetupRigEntity(entity, EntityManager, rigData.Rig);

                var animLocalToRigBuffer = EntityManager.AddBuffer<AnimatedLocalToRig>(entity);
                animLocalToRigBuffer.ResizeUninitialized(rigData.Rig.Value.Skeleton.Ids.Length);

                // Create root animsource
                var settings = EntityManager.GetComponentData<Settings>(entity);
                var rootAnimSourceEntity = PrefabAssetManager.CreateEntity(EntityManager, settings.RootAnimSource);
#if UNITY_EDITOR
                var name = EntityManager.GetName(rootAnimSourceEntity) + " -> Entity " + entity.Index + ".DotsAnimStateController.RootAnimSource";
                EntityManager.SetName(rootAnimSourceEntity, name);
#endif
                AnimSource.SetAnimStateEntityOnPrefab(EntityManager, rootAnimSourceEntity, entity);

                var rootAnimSource = RootAnimSource.Default;
                rootAnimSource.Value = rootAnimSourceEntity;
                EntityManager.AddComponentData(entity, rootAnimSource);

                EntityManager.AddComponentData(entity, new LOD { Value = lod, });
            }).Run();
            
            
            // Deinitialize
            Entities
                .WithStructuralChanges()
				.WithNone<Settings>()
				.WithAll<LOD>()
				.ForEach((Entity entity, ref RootAnimSource rootAnimSource, ref OutputNode outputNode, ref GraphOutput graphOutput) =>
            {
                Deinitialize(EntityManager, entity, animGraphSys);
            }).Run();

            return default;
        }
        
        void Deinitialize(EntityManager entityManager, Entity entity, AnimationGraphSystem animGraphSys)
        {
            GameDebug.Log(World, ShowLifetime, "InitSys: Deinit DotsAnimStateCtrl Entity:", entity);

            entityManager.RemoveComponent<LOD>(entity);

            if (entityManager.HasComponent<RootAnimSource>(entity))
            {
                var data = entityManager.GetComponentData<RootAnimSource>(entity);
                PrefabAssetManager.DestroyEntity(EntityManager, data.Value);
                entityManager.RemoveComponent<RootAnimSource>(entity);
            }

            if (entityManager.HasComponent<OutputNode>(entity))
            {
                var data = entityManager.GetComponentData<OutputNode>(entity);
                AnimationGraphHelper.DestroyNode(animGraphSys, data.Value);
                entityManager.RemoveComponent<OutputNode>(entity);
            }

            if (entityManager.HasComponent<GraphOutput>(entity))
            {
                var data = entityManager.GetComponentData<GraphOutput>(entity);
                animGraphSys.Set.ReleaseGraphValue(data.Buffer);
                entityManager.RemoveComponent<GraphOutput>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(AnimSourceApplyGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class PrepareGraph : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var animGraphSys = World.GetExistingSystem<AnimationGraphSystem>();
            var commands = new EntityCommandBuffer(Allocator.TempJob);

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<OutputNode>()
                .ForEach((Entity entity, ref RootAnimSource rootAnimSource) =>
            {
                var rootAnimSourceEntity = EntityManager.GetComponentData<AnimSource.Data>(rootAnimSource.Value);

                GameDebug.Assert(rootAnimSourceEntity.outputNode != default,"Root AnimSource has no output node");

                var rig = EntityManager.GetSharedComponentData<SharedRigDefinition>(entity).Value;

                var outputNode = OutputNode.Default;

                // Create root node and setup graph output

                outputNode.Value = AnimationGraphHelper.CreateNode<BoundaryNode>(animGraphSys,"Root");
                animGraphSys.Set.SendMessage(outputNode.Value, BoundaryNode.SimulationPorts.RigDefinition, rig);

                var graphOutput = new GraphOutput();
                graphOutput.Buffer = animGraphSys.Set.CreateGraphValue(outputNode.Value, BoundaryNode.KernelPorts.Output);
                commands.AddComponent(entity, graphOutput);

                // Attach root animsource to rootnode
                var rootNodeHandle = (NodeHandle)outputNode.Value;
                var rootNodeId = (InputPortID)BoundaryNode.KernelPorts.Input;
                animGraphSys.Set.Connect(rootAnimSourceEntity.outputNode, rootAnimSourceEntity.outputPortID, rootNodeHandle,rootNodeId);

                commands.AddComponent(entity, outputNode);
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }
    }



    [UpdateInGroup(typeof(AnimSourcePreUpdateGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class Update : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var animGraphSys = World.GetExistingSystem<AnimationGraphSystem>();
            var commands = new EntityCommandBuffer(Allocator.TempJob);

            // Update LOD
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref Translation translation, ref Settings settings, ref LOD lod, ref OutputNode outputNode) =>
            {
                // TODO (mogensh) for now we only use Rig LOD for selecting low lod on server
//                var camera = GameApp.CameraStack.TopCamera();
//                if (camera == null)
//                    return;
//
//                var camPos = (float3) camera.transform.position;
//                var charPos = translation.Value;
//
//                var dist = math.distance(camPos, charPos);
//                var rigDataBuffer = EntityManager.GetBuffer<RigData>(entity);
//                var newLod = rigDataBuffer.Length - 1;
//                for (int i = 0; i < rigDataBuffer.Length; i++)
//                {
//                    // TODO (mogensh) add threshold that needs to be passed before change (so it does not flicker)
//                    if (dist < rigDataBuffer[i].MaxDist)
//                    {
//                        newLod = i;
//                        break;
//                    }
//                }
//
//                if (newLod != lod.Value)
//                {
////                    GameDebug.Log("NEW LOD IS DIFFERENT");
//                    var changeRig = true; // newLod == 2 || lod.Value == 2;
////                    var rigRelativeLod = newLod == 2 ? 0 : newLod;
//                    var rig = rigDataBuffer[newLod].Rig;
//
//                    if (changeRig)
//                    {
//                        GameDebug.Log(World,null,"Setting up Rig for lod:{0}. bones:{1} Dist:{2}",newLod, rig.Value.Skeleton.Ids.Length, dist);
//
//                        commands.SetSharedComponent(entity, new SharedRigDefinition {Value = rig});
//
//                        var rigBuffers = new RigEntityBuilder.RigBuffers(EntityManager, entity);
//                        rigBuffers.ResizeBuffers(rig);
//                        rigBuffers.InitializeBuffers(rig);
//
//                        var animLocalToRigBuffer = EntityManager.GetBuffer<AnimatedLocalToRig>(entity);
//                        animLocalToRigBuffer.ResizeUninitialized(rig.Value.Skeleton.Ids.Length);
//
//                        animGraphSys.Set.SendMessage(rootNode.Value, BoundaryNode.SimulationPorts.RigDefinition, rig);
//                    }
//
//                    lod.Value = newLod;
//
//                }
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }
    }


}






