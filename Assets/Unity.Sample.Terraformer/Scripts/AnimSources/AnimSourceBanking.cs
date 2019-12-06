using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Sample.Core;
using UnityEngine;


public class AnimSourceBanking
{
    [Serializable]
    public struct BoneReferences : IComponentData
    {
        public int NeckLeftRightIndex;
        public int HeadLeftRightIndex;

        public int SpineLeftRightIndex;
        public int ChestLeftRightIndex;
        public int UpperChestLeftRightIndex;

        public int LeftFootIKIndex;
        public int RightFootIKIndex;

        public int HipsIndex;
    }

    [Serializable]
    public struct Settings : IComponentData
    {
        [Range(0, 1)]
        public int DoOverrideBank;
        public float OverrideAmount;

        public float3 Position;
        public float3 EulerRotation;

        public float BankContribution;
        public float MaxBankContribution;
        public float BankDamp;
        public float BankMagnitude;
        [Range(0f, 1f)]
        public float FootMultiplier;
        [Range(-1f, 1f)]
        public float HeadMultiplier;
        [Range(-1f, 1f)]
        public float SpineMultiplier;
        public float sprintSpeed;

        public BoneReferences boneReferences;
        public BlobAssetReference<RigDefinition> rigReference;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public NodeHandle<BankingNode> BankingNode;
        public float3 PreviousVelocity;
    }

    [UpdateInGroup(typeof(AnimSourceInitializationGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class InitSystem : JobComponentSystem
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
            var animationGraphSystem = m_AnimationGraphSystem;

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(cmdBuffer, entity, animationGraphSystem, state);
            }).Run();

            cmdBuffer.Dispose();
            m_AnimationGraphSystem.RemoveRef();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var commands = new EntityCommandBuffer(Allocator.TempJob);
            var animationGraphSystem = m_AnimationGraphSystem;

            // Initialize
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings) =>
            {
                var state = SystemState.Default;
                state.BankingNode = AnimationGraphHelper.CreateNode<BankingNode>(animationGraphSystem, "BankingNode");

                commands.AddComponent(entity, state);

                animSource.inputNode = state.BankingNode;
                animSource.inputPortID = (InputPortID)BankingNode.KernelPorts.Input;
                animSource.outputNode = state.BankingNode;
                animSource.outputPortID = (OutputPortID)BankingNode.KernelPorts.Output;
            }).Run();

            // Deinitialize
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<Settings>()
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(commands, entity, animationGraphSystem, state);
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }

        static void Deinitialize(EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            if (state.BankingNode != default /*&& animGraphSys.Set.Exists(state.BankingNode)*/)
                AnimationGraphHelper.DestroyNode(animGraphSys, state.BankingNode);

            cmdBuffer.RemoveComponent<SystemState>(entity);
        }
    }

    [UpdateInGroup(typeof(AnimSourceUpdateCGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class UpdateSystem : JobComponentSystem
    {
        private EntityQuery m_GlobalGameTimeQuery;

        protected override void OnCreate()
        {
            m_GlobalGameTimeQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var vectorUp = new float3(0f, 1f, 0f);

            // TODO (mogensh) find cleaner way to get time
            var globalTime = m_GlobalGameTimeQuery.GetSingleton<GlobalGameTime>();
            var deltaTime = globalTime.frameDuration;

            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);
            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(true);
            var abilityInterpolatedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.InterpolatedState>(true);
            var ownedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);

            Entities
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state,
                    ref AnimSource.AllowWrite allowWrite) =>
            {
                if (!characterInterpolatedDataFromEntity.HasComponent(animSource.animStateEntity))
                {
                    //GameDebug.LogWarning(World,"AnimSource does not have Character.InterpolatedData components. Has it been deleted?");
                    return;
                }

                var animState = characterInterpolatedDataFromEntity[animSource.animStateEntity];
                var predictedState = characterPredictedDataFromEntity[animSource.animStateEntity];

                // TODO (mogens) dont query for ability entity every frame
                var abilityMovementEntity = Ability.FindAbility(ownedAbilityBufferFromEntity, animSource.animStateEntity, AbilityMovement.Tag);
                if (abilityMovementEntity == Entity.Null)
                    return;

                var abilityMovement = abilityInterpolatedStateFromEntity[abilityMovementEntity];
                animState.banking = MathHelper.MoveTowards(animState.banking, 0f, settings.BankDamp * deltaTime);

                if (abilityMovement.charLocoState == AbilityMovement.LocoState.GroundMove)
                {
                    var velocity = new float3(predictedState.velocity.x, 0f, predictedState.velocity.z);
                    var magnitude = math.length(velocity);

                    if (settings.sprintSpeed < 0.0001f)
                    {
                        magnitude = 0f;
                    }
                    else
                    {
                        magnitude *= 1 / settings.sprintSpeed;
                    }

                    var delta = -MathHelper.SignedAngle(state.PreviousVelocity, velocity, vectorUp) * settings.BankContribution * deltaTime;
//                    GameDebug.Log("VelocityDelta: " + delta + " Magnitude: " + magnitude);

                    // - Multiply the delta by the movement direction: Forward = 1, Strafe = 0, Backwards = -1

                    var deltaAngle = MathHelper.DeltaAngle(animState.rotation, animState.moveYaw);

                    delta *= (math.abs(deltaAngle) - 90f) / -90f;
                    delta *= magnitude;

                    // - Define max contribution
                    delta = math.clamp(delta, -settings.MaxBankContribution * deltaTime, settings.MaxBankContribution * deltaTime);
                    animState.banking = math.clamp(animState.banking + delta, -settings.BankMagnitude, settings.BankMagnitude);

                    state.PreviousVelocity = velocity;
                }

                characterInterpolatedDataFromEntity[animSource.animStateEntity] = animState;
            }).Run();

            return default;
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

            var nodeSet = World.GetExistingSystem<AnimationGraphSystem>().Set;

            var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
                .WithNone<AnimSource.HasValidRig>()
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;

                // Set the rig, bindings and values
                nodeSet.SendMessage(state.BankingNode, BankingNode.SimulationPorts.RigDefinition, rig);

                // Remap rig indexes
                BlobAssetReference<AnimationAssetDatabase.RigMap> rigMap;
                AnimationAssetDatabase.GetOrCreateRigMapping(World, settings.rigReference, rig, out rigMap);

                var currentSettings = settings;
                currentSettings.boneReferences.NeckLeftRightIndex = rigMap.Value.BoneMap[settings.boneReferences.NeckLeftRightIndex];
                currentSettings.boneReferences.HeadLeftRightIndex = rigMap.Value.BoneMap[settings.boneReferences.HeadLeftRightIndex];
                currentSettings.boneReferences.SpineLeftRightIndex = rigMap.Value.BoneMap[settings.boneReferences.SpineLeftRightIndex];
                currentSettings.boneReferences.ChestLeftRightIndex = rigMap.Value.BoneMap[settings.boneReferences.ChestLeftRightIndex];
                currentSettings.boneReferences.UpperChestLeftRightIndex = rigMap.Value.BoneMap[settings.boneReferences.UpperChestLeftRightIndex];
                currentSettings.boneReferences.LeftFootIKIndex = rigMap.Value.BoneMap[settings.boneReferences.LeftFootIKIndex];
                currentSettings.boneReferences.RightFootIKIndex = rigMap.Value.BoneMap[settings.boneReferences.RightFootIKIndex];
                currentSettings.boneReferences.HipsIndex = rigMap.Value.BoneMap[settings.boneReferences.HipsIndex];

                nodeSet.SendMessage(state.BankingNode, BankingNode.SimulationPorts.BankingSetup, currentSettings);

                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<Character.InterpolatedData>(animSource.animStateEntity))
                {
                    GameDebug.LogWarning(World,"AnimSource does not have Character.InterpolatedData components. Has it been deleted?");
                    return;
                }

                var animState = EntityManager.GetComponentData<Character.InterpolatedData>(animSource.animStateEntity);

                if (settings.DoOverrideBank == 1)
                {
                    nodeSet.SetData(state.BankingNode, BankingNode.KernelPorts.BankAmount, settings.OverrideAmount);
                }
                else
                {
                    nodeSet.SetData(state.BankingNode, BankingNode.KernelPorts.BankAmount, animState.banking);
                }
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}
