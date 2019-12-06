using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Sample.Core;
using UnityEngine;


public class AnimSourceKnockBack
{
    [Serializable]
    public struct BoneReferences : IComponentData
    {
        public int hipBoneIndex;
    }

    public struct Settings : IComponentData
    {
        public BlobAssetReference<Clip> animShootPose;
        public BlobAssetReference<Clip> animReferenceShootPose;
        public float shootPoseMagnitude;
        public float shootPoseEnterSpeed;
        public float shootPoseExitSpeed;
        public BlobAssetReference<KeyframeCurveBlob> shootPoseEnter;
        public BlobAssetReference<KeyframeCurveBlob> shootPoseExit;
        public float positionMultiplier;
        public float angleMultiplier;
        public BlobAssetReference<RigDefinition> rigReference;
        public BoneReferences boneReferences;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public NodeHandle<LayerMixerNode> MixerNode;
        public NodeHandle<ClipNode> ClipNode;
        public NodeHandle<ClipNode> SubtractClipNode;
        public NodeHandle<DeltaNode> DeltaNode;
        public NodeHandle<OffsetTransformNode> OffsetNode;
        public BoneReferences currentRigBoneIdx;
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
                Deinitialize(World, cmdBuffer, entity, m_AnimationGraphSystem, state);
            }).Run();

            cmdBuffer.Dispose();
            m_AnimationGraphSystem.RemoveRef();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var nodeSet = m_AnimationGraphSystem.Set;
            var commands = new EntityCommandBuffer(Allocator.TempJob);
            var animationGraphSystem = m_AnimationGraphSystem;

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings) =>
            {
                GameDebug.Log(World,AnimSource.ShowLifetime,"Init KnockBack entity:{0} state entity:{1}", entity, animSource.animStateEntity);

                var state = SystemState.Default;

                state.MixerNode = AnimationGraphHelper.CreateNode<LayerMixerNode>(animationGraphSystem, "MixerNode");
                state.ClipNode = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "ClipNode");
                state.OffsetNode = AnimationGraphHelper.CreateNode<OffsetTransformNode>(animationGraphSystem, "OffsetNode");
                state.SubtractClipNode = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "SubtractClipNode");
                state.DeltaNode = AnimationGraphHelper.CreateNode<DeltaNode>(animationGraphSystem, "DeltaNode");

                nodeSet.Connect(state.ClipNode, ClipNode.KernelPorts.Output, state.DeltaNode, DeltaNode.KernelPorts.Input);
                nodeSet.Connect(state.SubtractClipNode, ClipNode.KernelPorts.Output, state.DeltaNode, DeltaNode.KernelPorts.Subtract);
                nodeSet.Connect(state.DeltaNode, DeltaNode.KernelPorts.Output, state.MixerNode, LayerMixerNode.KernelPorts.Input1);

                nodeSet.Connect(state.MixerNode, LayerMixerNode.KernelPorts.Output, state.OffsetNode, OffsetTransformNode.KernelPorts.Input);

                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.BlendModeInput0, BlendingMode.Override);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput0, 1f);

                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput1, 1f);

                // Expose input and outputs
                animSource.inputNode = state.MixerNode;
                animSource.inputPortID = (InputPortID)LayerMixerNode.KernelPorts.Input0;
                animSource.outputNode = state.OffsetNode;
                animSource.outputPortID = (OutputPortID)OffsetTransformNode.KernelPorts.Output;

                commands.AddComponent(entity, state);
            }).Run();

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<Settings>()
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(World, commands, entity, animationGraphSystem, state);
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }

        static void Deinitialize(World world,EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            GameDebug.Log(world,AnimSource.ShowLifetime,"Deinit KnockBack entity:{0}", entity);

            if (state.MixerNode != default && animGraphSys.Set.Exists(state.MixerNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.MixerNode);

            if (state.ClipNode != default && animGraphSys.Set.Exists(state.ClipNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.ClipNode);

            if (state.SubtractClipNode != default && animGraphSys.Set.Exists(state.SubtractClipNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.SubtractClipNode);

            if (state.DeltaNode != default && animGraphSys.Set.Exists(state.DeltaNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.DeltaNode);

            if (state.OffsetNode != default && animGraphSys.Set.Exists(state.OffsetNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.OffsetNode);

            cmdBuffer.RemoveComponent<SystemState>(entity);
        }
    }

    [UpdateInGroup(typeof(AnimSourceUpdateBGroup))]
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

            // TODO (mogensh) find cleaner way to get time
            var globalTime = m_GlobalGameTimeQuery.GetSingleton<GlobalGameTime>();
            var deltaTime = globalTime.frameDuration;

            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);

            Entities
                .WithAll<AnimSource.AllowWrite>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings,
                    ref SystemState state) =>
            {

                if (!characterInterpolatedDataFromEntity.HasComponent(animSource.animStateEntity))
                {
//                    GameDebug.LogWarning(World,"AnimSource does not have Character.InterpolatedData components. Has it been deleted?");
                    return;
                }

                var charInterpolatedState = characterInterpolatedDataFromEntity[animSource.animStateEntity];

                if (charInterpolatedState.charAction == Ability.AbilityAction.Action.PrimaryFire)
                {
                    charInterpolatedState.shootPoseWeight += settings.shootPoseEnterSpeed * deltaTime;
                }
                else
                {
                    charInterpolatedState.shootPoseWeight -= settings.shootPoseExitSpeed * deltaTime;
                }

                charInterpolatedState.shootPoseWeight = math.clamp(charInterpolatedState.shootPoseWeight, 0f, 1f);

                characterInterpolatedDataFromEntity[animSource.animStateEntity] = charInterpolatedState;
            }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(AnimSourceApplyGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class PrepareGraph : JobComponentSystem
    {
        private EntityQuery m_GlobalGameTimeQuery;

        protected override void OnCreate()
        {
            m_GlobalGameTimeQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var nodeSet = World.GetExistingSystem<AnimationGraphSystem>().Set;

            var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

            // Handle rig change
            Entities
                .WithNone<AnimSource.HasValidRig>()
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref SystemState state, ref Settings settings) =>
            {
                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;

                var knockBackClipInstance = ClipManager.Instance.GetClipFor(rig, settings.animShootPose);
                nodeSet.SendMessage(state.ClipNode, ClipNode.SimulationPorts.ClipInstance, knockBackClipInstance);

                var knockBackReferenceClipInstance = ClipManager.Instance.GetClipFor(rig, settings.animReferenceShootPose);
                nodeSet.SendMessage(state.SubtractClipNode, ClipNode.SimulationPorts.ClipInstance, knockBackReferenceClipInstance);

                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.DeltaNode, DeltaNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.OffsetNode, OffsetTransformNode.SimulationPorts.RigDefinition, rig);

                // Remap rig indexes
                BlobAssetReference<AnimationAssetDatabase.RigMap> rigMap;
                AnimationAssetDatabase.GetOrCreateRigMapping(World, settings.rigReference, rig, out rigMap);
                state.currentRigBoneIdx.hipBoneIndex = rigMap.Value.BoneMap[settings.boneReferences.hipBoneIndex];

                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<Character.InterpolatedData>(animSource.animStateEntity))
                {
//                    GameDebug.LogWarning(World,"AnimSource does not have Character.InterpolatedData components. Has it been deleted?");
                    return;
                }

                var charInterpolatedState = EntityManager.GetComponentData<Character.InterpolatedData>(animSource.animStateEntity);

                if (Math.Abs(charInterpolatedState.shootPoseWeight) < 0.01f)
                {
                    nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput1, 0f);
                    nodeSet.SetData(state.OffsetNode, OffsetTransformNode.KernelPorts.Weight, 0f);
                    return;
                }

                // This really should be a spring, since jumping between these can have some large discontuinity. But
                // as it happens, that works well with shooting, which is a shaky act
                float shootPoseWeight;
                if (charInterpolatedState.charAction == Ability.AbilityAction.Action.PrimaryFire)
                {
                    shootPoseWeight = KeyframeCurveEvaluator.Evaluate(charInterpolatedState.shootPoseWeight, settings.shootPoseEnter);
                }
                else
                {
                    shootPoseWeight = KeyframeCurveEvaluator.Evaluate(charInterpolatedState.shootPoseWeight, settings.shootPoseExit);
                }

                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput1, shootPoseWeight * settings.shootPoseMagnitude);

                var yaw = charInterpolatedState.aimYaw;
                var pitch = charInterpolatedState.aimPitch - 90f;
                var aimRotation = quaternion.Euler(math.radians(-pitch), math.radians(yaw - charInterpolatedState.rotation), 0f);
                var knockBackRotation = math.slerp(quaternion.identity, aimRotation, settings.angleMultiplier);
                var knockBackVector = math.mul(knockBackRotation, new float3(0f, 0f, -1f));

                var data = new OffsetTransformNode.OffsetData
                {
                    BoneIndex = state.currentRigBoneIdx.hipBoneIndex,
                    offset = knockBackVector
                };

                nodeSet.SendMessage(state.OffsetNode, OffsetTransformNode.SimulationPorts.OffsetData, data);
                nodeSet.SetData(state.OffsetNode, OffsetTransformNode.KernelPorts.Weight, shootPoseWeight * settings.positionMultiplier);
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}
