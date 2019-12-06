using System.Text;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Jobs;
using Unity.Sample.Core;


public class AnimSourceStack
{
    public struct AnimSourceEntities : IBufferElementData
    {
        public Entity Value;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();
        public NodeHandle<BoundaryNode> inputNode;
        public NodeHandle<BoundaryNode> outputNode;
    }

    public struct ConnectionsUpdated : IComponentData
    {}

    [UpdateInGroup(typeof(AnimSourceInitializationGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class Initialize : JobComponentSystem
    {

        AnimationGraphSystem m_AnimationGraphSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_AnimationGraphSystem = World.GetExistingSystem<AnimationGraphSystem>();
            m_AnimationGraphSystem.AddRef();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            var animGraphSystem = m_AnimationGraphSystem;
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(cmdBuffer, entity, animGraphSystem, state);
            }).Run();

            cmdBuffer.Dispose();
            m_AnimationGraphSystem.RemoveRef();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var animGraphSystem = m_AnimationGraphSystem;
            var commands = new EntityCommandBuffer(Allocator.TempJob);

            // Handle create AnimSources
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .WithAll<AnimSourceEntities>()
                .ForEach((Entity entity, ref AnimSource.Data animSource) =>
            {
                var systemState = SystemState.Default;

                systemState.inputNode =  AnimationGraphHelper.CreateNode<BoundaryNode>(animGraphSystem, "inputNode");
                systemState.outputNode =  AnimationGraphHelper.CreateNode<BoundaryNode>(animGraphSystem, "outputNode");

                animSource.inputNode = systemState.inputNode;
                animSource.inputPortID = (InputPortID)BoundaryNode.KernelPorts.Input;
                animSource.outputNode = systemState.outputNode;
                animSource.outputPortID = (OutputPortID)BoundaryNode.KernelPorts.Output;

                commands.AddComponent(entity, systemState);
            }).Run();

            // Handle destroyed AnimSources
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<AnimSourceEntities>()
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(commands, entity, animGraphSystem, state);
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }

        static void Deinitialize(EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            AnimationGraphHelper.DestroyNode(animGraphSys,state.inputNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.outputNode);

            cmdBuffer.RemoveComponent<SystemState>(entity);
        }
    }


    [UpdateInGroup(typeof(AnimSourceUpdateAGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class UpdateSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);

            var animSourceEntitiesFromEntity = GetBufferFromEntity<AnimSourceEntities>(true);
            var animSourceAllowWriteFromEntity = GetComponentDataFromEntity<AnimSource.AllowWrite>(true);

            // Update state
            Entities
                .ForEach((Entity entity, ref AnimSource.Data animSource,
                ref SystemState state, ref AnimSource.AllowWrite allowWrite) =>
            {
                var childSources = animSourceEntitiesFromEntity[entity];
                var len = childSources.Length;

                for (var i = 0; i < len; i++)
                {
                    if (!animSourceAllowWriteFromEntity.HasComponent(childSources[i].Value))
                    {
                        PostUpdateCommands.AddComponent(childSources[i].Value, new AnimSource.AllowWrite {FirstUpdate = allowWrite.FirstUpdate});
                    }
                    else
                    {
                        PostUpdateCommands.SetComponent(childSources[i].Value, new AnimSource.AllowWrite {FirstUpdate = allowWrite.FirstUpdate});
                    }
                }

                if (allowWrite.FirstUpdate)
                {
                    allowWrite.FirstUpdate = false;
                }
            }).Run();

            Entities
                .WithNone<AnimSource.AllowWrite>()
                .ForEach((Entity entity, ref AnimSource.Data animSource,
                ref SystemState state) =>
            {
                var childSources = animSourceEntitiesFromEntity[entity];
                var len = childSources.Length;

                for (var i = 0; i < len; i++)
                {
                    if (animSourceAllowWriteFromEntity.HasComponent(childSources[i].Value))
                    {
                        PostUpdateCommands.RemoveComponent<AnimSource.AllowWrite>(childSources[i].Value);
                    }
                }
            }).Run();

            PostUpdateCommands.Playback(EntityManager);
            PostUpdateCommands.Dispose();

            return default;
        }
    }

    public static string GetEntityName(EntityManager entityManager, Entity entity, bool showTypes = true)
    {
        var strBuilder = new StringBuilder();
        strBuilder.Append("Entity:" + entity.Index + " <");

        foreach (var type in entityManager.GetComponentTypes(entity))
        {
            strBuilder.Append(type.GetManagedType().Name + ",");
        }
        strBuilder.Append(">");
        return strBuilder.ToString();
    }


    [UpdateInGroup(typeof(AnimSourceApplyGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class PrepareGraph : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var nodeSet = World.GetExistingSystem<AnimationGraphSystem>().Set;

            var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<AnimSource.HasValidRig>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;

                nodeSet.SendMessage(state.inputNode, BoundaryNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.outputNode, BoundaryNode.SimulationPorts.RigDefinition, rig);

                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<ConnectionsUpdated>()
                .WithAll<AnimSourceEntities>()
                .ForEach((Entity entity, ref AnimSource.Data stackAnimSource) =>
            {
                var numChildAnimSources = EntityManager.GetBuffer<AnimSourceEntities>(entity).Length;
                GameDebug.Assert(numChildAnimSources > 0, "Stack cannot be empty");

                var prevNode = stackAnimSource.inputNode;
                var prevNodeOutPort = (OutputPortID)BoundaryNode.KernelPorts.Output;

                // Chain stack nodes
                for (var i = 0; i < numChildAnimSources; i++)
                {
                    var childAnimSourceEntities = EntityManager.GetBuffer<AnimSourceEntities>(entity);
                    var childAnimEntity = childAnimSourceEntities[i].Value;
                    var childAnimSource = EntityManager.GetComponentData<AnimSource.Data>(childAnimEntity);

                    GameDebug.Assert(childAnimSource.outputNode != default,"AnimSource has no output node defined. Entity:" + GetEntityName(EntityManager,childAnimEntity));

                    if (childAnimSource.inputNode != default && prevNode != default)
                    {
                        nodeSet.Connect(prevNode, prevNodeOutPort,
                            childAnimSource.inputNode, childAnimSource.inputPortID);
                    }

                    prevNode = childAnimSource.outputNode;
                    prevNodeOutPort = childAnimSource.outputPortID;
                }

                // Hook prevNode to output node
                nodeSet.Connect(prevNode, prevNodeOutPort,
                    stackAnimSource.outputNode, (InputPortID)BoundaryNode.KernelPorts.Input);

                cmdBuffer.AddComponent(entity, new ConnectionsUpdated());
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}
