using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.DataFlowGraph;
using Unity.NetCode;
using Unity.Sample.Core;
using UnityEngine;


// Defines a MonoBehaviours that define a transition. Used to map conditions in conversion
public interface ITransitionBehaviour
{
    void AddTransition(EntityManager dstManager, Entity entity);
}

public class AnimSourceSelector
{
    [ConfigVar(Name = "animsource.show.animsourceselector", DefaultValue = "0", Description = "Show animsourceselector info")]
    public static ConfigVar ShowDebug;

    public struct InputState : IComponentData
    {
        public static InputState Default => new InputState();
        public Entity AnimSourceDecisionTree;
        public BlobAssetReference<AssetBlob> Resource;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState { CurrentAnimSource = -1};

//        public WeakAssetReference TargetAsset;
        public int CurrentAnimSource;

        public Entity IncommingSourceEntity;
        public Entity TargeSourceEntity;

        public float BlendVel;

        public int previousAnimSource;
    }

    public struct TransitionDuration
    {
        public WeakAssetReference From;
        public WeakAssetReference To;
        public float Duration;
    }

    public struct AssetBlob
    {
        public float DefaultTransitionDuration;
        public BlobArray<TransitionDuration> TransitionDuration;
        public BlobArray<WeakAssetReference> AnimSourceAssets;

        public float GetTransitionDuration(WeakAssetReference from, WeakAssetReference to)
        {
            var duration = DefaultTransitionDuration;
            for (int i = 0; i <TransitionDuration.Length; i++)
            {
                if (TransitionDuration[i].From != WeakAssetReference.Default)
                {
                    if (TransitionDuration[i].From == from && TransitionDuration[i].To == to)
                    {
                        duration = TransitionDuration[i].Duration;
                        break;
                    }

                    continue;
                }

                if (TransitionDuration[i].To == to)
                {
                    duration = TransitionDuration[i].Duration;
                }
            }

            return duration;
        }

        public int FindAssetIndex(in WeakAssetReference asset)
        {
            for (int i = 0; i < AnimSourceAssets.Length; i++)
            {
                if (AnimSourceAssets[i] == asset)
                    return i;
            }
            return -1;
        }

        public void GetAsset(int index, out WeakAssetReference asset)
        {
            asset = AnimSourceAssets[index];
        }
    }


    [UpdateInGroup(typeof(AnimSourceInitializationGroup))]
    [DisableAutoCreation]
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
            var nodeSet = m_AnimationGraphSystem.Set;
            Entities
                .WithStructuralChanges()
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(EntityManager, cmdBuffer, entity, nodeSet, state);
            }).Run();
            //            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            m_AnimationGraphSystem.RemoveRef();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var nodeSet = m_AnimationGraphSystem.Set;

            var commands = new EntityCommandBuffer(Allocator.TempJob);

            // Initialize newly create AnimSources
            Entities
                .WithoutBurst()
                .WithNone<SystemState>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref InputState inputState) =>
            {
                GameDebug.Log(ShowDebug, "AnimSourceSelector.Initialize. Entity:{0} Anim State:{1}", entity, animSource.animStateEntity);

                DecisionTree.SetOwner(EntityManager, inputState.AnimSourceDecisionTree, animSource.animStateEntity);
                var systemState = SystemState.Default;
                commands.AddComponent(entity, systemState);

                var dynamicMixer = DynamicMixer.AddComponents(commands, nodeSet, entity);

                animSource.outputNode = dynamicMixer.MixerEnd;
                animSource.outputPortID = (OutputPortID)MixerEndNode.KernelPorts.Output;
            }).Run();

            // Handled deleted AnimSources
            Entities
                .WithStructuralChanges()
                .WithNone<InputState>()
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(EntityManager, commands, entity, nodeSet, state);
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }

        static void Deinitialize(EntityManager entityManager, EntityCommandBuffer cmdBuffer, Entity entity, NodeSet nodeSet, SystemState state)
        {
            GameDebug.Log(ShowDebug, "AnimSourceSelector.Deinitialize. Entity:{0}", entity);


            var pendingDelete = new NativeList<Entity>(Allocator.Temp);
            var inputs = entityManager.GetBuffer<DynamicMixerInput>(entity);
            foreach (var input in inputs)
            {
                if (input.SourceEntity != Entity.Null)
                {
                    GameDebug.Log(entityManager.World, ShowDebug, "  Destroying source:{0}", input.SourceEntity);
                    pendingDelete.Add(input.SourceEntity);
                }
            }

            if (state.IncommingSourceEntity != Entity.Null)
            {
                GameDebug.Log(entityManager.World, ShowDebug, "  Destroying incomming source:{0}", state.IncommingSourceEntity);
                pendingDelete.Add(state.IncommingSourceEntity);
            }

            for (int i = 0; i < pendingDelete.Length; i++)
                PrefabAssetManager.DestroyEntity(entityManager, pendingDelete[i]);



            var dynamicMixer = entityManager.GetComponentData<DynamicMixer>(entity);
            dynamicMixer.Dispose(entityManager, entity, nodeSet);
            cmdBuffer.RemoveComponent<DynamicMixerInput>(entity);
            cmdBuffer.RemoveComponent<DynamicMixer>(entity);


            cmdBuffer.RemoveComponent<SystemState>(entity);
        }
    }

    [UpdateInGroup(typeof(AnimSourcePreUpdateGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class Update : JobComponentSystem
    {
        EntityQuery Query;

        protected override void OnCreate()
        {
            base.OnCreate();
            Query = GetEntityQuery( typeof(AnimSource.Data), typeof(InputState),typeof(SystemState));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var predictTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
            var commands = new EntityCommandBuffer(Allocator.TempJob);

            var predictedGhostComponentFromEntity = GetComponentDataFromEntity<PredictedGhostComponent>(true);
            var decisionTreeNodeSubtreeElementBufferFromEntity = GetBufferFromEntity<DecisionTreeNode.SubtreeElement>(true);
            var decisionTreeNodeStateFromEntity = GetComponentDataFromEntity<DecisionTreeNode.State>(true);
            var animSourceReferenceStateFromEntity = GetComponentDataFromEntity<AnimSourceReference.State>(true);
            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);

            // Update target. Only done on server+predicting
            Entities
                .ForEach((ref AnimSource.Data animSource, ref InputState inputState, ref SystemState state) =>
            {
                if (!AnimSource.ShouldPredict(predictedGhostComponentFromEntity, in animSource, predictTick))
                    return;

                // TODO (mogensh) This is really slow. Change decision tree data to blob asset?. Unpack condition tree for faster lookup? Linear list with calculated index?
                var decisionNode =
                    DecisionTree.FindValidDecisionNode(decisionTreeNodeSubtreeElementBufferFromEntity, decisionTreeNodeStateFromEntity, inputState.AnimSourceDecisionTree);

                //GameDebug.Assert(decisionNode != Entity.Null, "failed to find valid decision node");
                //GameDebug.Assert(EntityManager.HasComponent<AnimSourceReference.State>(decisionNode),
                //    "decision node has no animsourcereference component");

                // TODO (mogensh) Always to asset INDEX when referencing animsource resource. Dont pass WeakAssetRef around
                var newTargetAsset = animSourceReferenceStateFromEntity[decisionNode].animSource;

                var newIndex = newTargetAsset.IsSet() ? inputState.Resource.Value.FindAssetIndex(newTargetAsset) : -1;

                if (newIndex != -1 && newIndex != state.CurrentAnimSource)
                {
                    var charInterpState = characterInterpolatedDataFromEntity[animSource.animStateEntity];
                    charInterpState.selectorTargetSource = inputState.Resource.Value.FindAssetIndex(newTargetAsset);
                    characterInterpolatedDataFromEntity[animSource.animStateEntity] = charInterpState;

                    //GameDebug.Log(World,ShowDebug, "New target animsource set on interp state. Asset:{0}", newTargetAsset.ToGuidStr());
                }
            }).Run();


            // TODO (mogensh) This is a case where we want to run code in update also for NON predicting. We want to run it here as predicting needs AnimSources to be initialized in this frame
            // Find animsources needing changes
            var entityList = new NativeList<Entity>(Allocator.TempJob);
            var animSourceList = new NativeList<AnimSource.Data>(Allocator.TempJob);
            var inputStateList = new NativeList<InputState>(Allocator.TempJob);
            var systemStateList = new NativeList<SystemState>(Allocator.TempJob);
            Entities
                .ForEach((Entity entity, in AnimSource.Data animSource, in InputState inputState, in SystemState state) =>
            {
                if (!characterInterpolatedDataFromEntity.HasComponent(animSource.animStateEntity))
                    return;
                var charInterpState = characterInterpolatedDataFromEntity[animSource.animStateEntity];
                if (charInterpState.selectorTargetSource == state.CurrentAnimSource)
                    return;
                entityList.Add(entity);
                animSourceList.Add(animSource);
                inputStateList.Add(inputState);
                systemStateList.Add(state);
            }).Run();

            // Update animsources needing changes
            for (int j = 0; j < entityList.Length; j++)
            {
                var entity = entityList[j];
                var state = systemStateList[j];
                var animSource = animSourceList[j];
                var inputState = inputStateList[j];

                var charInterpState = EntityManager.GetComponentData<Character.InterpolatedData>(animSource.animStateEntity);

                WeakAssetReference targetAsset;
                inputState.Resource.Value.GetAsset(charInterpState.selectorTargetSource, out targetAsset);

                WeakAssetReference oldAsset;
                if (state.CurrentAnimSource != -1)
                    inputState.Resource.Value.GetAsset(state.CurrentAnimSource, out oldAsset);
                else
                    oldAsset = WeakAssetReference.Default;

                state.CurrentAnimSource = charInterpState.selectorTargetSource;

                var newTargetEntity = PrefabAssetManager.CreateEntity(EntityManager, targetAsset);
#if UNITY_EDITOR
                EntityManager.SetName(newTargetEntity, "Entity " + newTargetEntity.Index + " Anim Source:" + targetAsset.ToGuidStr());
#endif

                GameDebug.Log(World,ShowDebug, "Creating new target animsource. Asset:{0} Entity:{1}", targetAsset.ToGuidStr(), newTargetEntity);

                AnimSource.SetAnimStateEntityOnPrefab(EntityManager, newTargetEntity, animSource.animStateEntity, commands);

                state.IncommingSourceEntity = newTargetEntity;
                if (oldAsset == WeakAssetReference.Default)
                {
                    state.BlendVel = float.MaxValue;
//                        GameDebug.Log("FIRST STATE:");
                }
                else
                {
                    var transitionDuration = inputState.Resource.Value.GetTransitionDuration(oldAsset, targetAsset);
                    state.BlendVel = 1.0f/transitionDuration;
                    GameDebug.Log(World, ShowDebug, "TRANSITION from:" + oldAsset.ToGuidStr() + " to:" + targetAsset.ToGuidStr() + " duration:" + transitionDuration);
                }

                var previousTargetEntity = state.TargeSourceEntity;
                var isFirstSource = previousTargetEntity == Entity.Null;
                if (isFirstSource)
                {
                    if (!EntityManager.HasComponent<AnimSource.AllowWrite>(newTargetEntity))
                    {
                        commands.AddComponent<AnimSource.AllowWrite>(newTargetEntity);
                    }
                    commands.SetComponent(newTargetEntity,new AnimSource.AllowWrite {FirstUpdate = true});
                }
                else
                {
                    commands.RemoveComponent<AnimSource.AllowWrite>(previousTargetEntity);
                    if (!EntityManager.HasComponent<AnimSource.AllowWrite>(newTargetEntity))
                    {
                        commands.AddComponent<AnimSource.AllowWrite>(newTargetEntity);
                    }
                    commands.SetComponent(newTargetEntity,new AnimSource.AllowWrite {FirstUpdate = true});
                }

                commands.SetComponent(entity,state);
            }

            entityList.Dispose();
            animSourceList.Dispose();
            systemStateList.Dispose();
            inputStateList.Dispose();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }
    }

    [UpdateInGroup(typeof(AnimSourceApplyGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class PrepareGraph : JobComponentSystem
    {
        private EntityQuery TimeQuery;

        protected override void OnCreate()
        {
            TimeQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var time = TimeQuery.GetSingleton<GlobalGameTime>();
            var nodeSet = World.GetExistingSystem<AnimationGraphSystem>().Set;

            var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

            // Handle rig changes
            Entities
                .WithNone<AnimSource.HasValidRig>()
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref InputState inputState, ref SystemState state) =>
                {
                    if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                        return;

                    var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                    var rig = sharedRigDef.Value;

                    var dynamicMixer = EntityManager.GetComponentData<DynamicMixer>(entity);
                    var inputs = EntityManager.GetBuffer<DynamicMixerInput>(entity);
                    DynamicMixer.SetRig(nodeSet, ref dynamicMixer, inputs, rig);
                    EntityManager.SetComponentData(entity, dynamicMixer);

                    //                    nodeSet.SendMessage(state.Mixer, MixerNode.SimulationPorts.RigDefinition, rig);

                    cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
                }).Run();

            NativeList<Entity> pendingDeleteList = new NativeList<Entity>(Allocator.TempJob);

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref SystemState state, ref DynamicMixer dynamicMixer) =>
                {
                    var inputs = EntityManager.GetBuffer<DynamicMixerInput>(entity);

                    // Handle new incomming target
                    if (state.IncommingSourceEntity != Entity.Null)
                    {

                        // Make sure incomming target has valid output
                        var incommingTargetSource = EntityManager.GetComponentData<AnimSource.Data>(state.IncommingSourceEntity);
                        GameDebug.Assert(incommingTargetSource.outputNode != default,
                            "AnimSource has no output node setup.");

                        GameDebug.Log(World, ShowDebug, "Adding new animsource. Entity:{0} port:{1}", state.IncommingSourceEntity,
                            incommingTargetSource.outputPortID);

                        DynamicMixer.AddInput(nodeSet, ref dynamicMixer, inputs, state.IncommingSourceEntity,
                            incommingTargetSource.outputNode, incommingTargetSource.outputPortID);

                        state.TargeSourceEntity = state.IncommingSourceEntity;
                        state.IncommingSourceEntity = Entity.Null;
                    }

                    // Update weights
                    if (inputs.Length > 0)
                    {
                        var targetIndex = -1;
                        for (int i = 0; i < inputs.Length; i++)
                        {
                            if (inputs[i].SourceEntity == state.TargeSourceEntity)
                            {
                                targetIndex = i;
                                break;
                            }
                        }
                        GameDebug.Assert(targetIndex != -1, "Can find target input");

                        float targetWeight = inputs[targetIndex].weight;
                        if (targetWeight != 1.0f)
                        {
                            targetWeight = Mathf.Clamp(targetWeight + state.BlendVel * time.frameDuration, 0, 1);

                            var input = inputs[targetIndex];
                            input.weight = targetWeight;
                            inputs[targetIndex] = input;
                        }

                        // Get total weight of all non target ports
                        float nonTargetWeightSum = 0;
                        for (int i = 0; i < inputs.Length; i++)
                        {
                            if (i == targetIndex)
                                continue;

                            nonTargetWeightSum += inputs[i].weight;
                        }

                        if (nonTargetWeightSum > 0)
                        {
                            // Adjust weight of other states and ensure total weight is 1
                            var weighLeft = 1.0f - targetWeight;
                            var fraction = weighLeft / nonTargetWeightSum;
                            for (int i = 0; i < inputs.Length; i++)
                            {
                                if (i == targetIndex)
                                    continue;

                                var input = inputs[i];
                                input.weight = input.weight * fraction;
                                inputs[i] = input;
                            }
                        }

                        // Remove zero weigth nodes
                        {
                            for (int i = 0; i < inputs.Length; i++)
                            {
                                var input = inputs[i];
                                if (input.SourceEntity == Entity.Null)
                                    continue;

                                if (input.SourceEntity == state.TargeSourceEntity)
                                    continue;

                                if (input.weight < 0.01f)
                                {
                                    DynamicMixer.RemoveInput(nodeSet, inputs, i);
                                    pendingDeleteList.Add(input.SourceEntity);
                                }
                            }
                        }
                        DynamicMixer.ApplyWeight(nodeSet, inputs);
                    }
                }).Run();

            // Delete sources pending deletion
            for (int i = 0; i < pendingDeleteList.Length; i++)
                PrefabAssetManager.DestroyEntity(EntityManager, pendingDeleteList[i]);
            pendingDeleteList.Dispose();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}
