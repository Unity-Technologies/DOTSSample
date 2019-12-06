using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Jobs;
using Unity.Sample.Core;
using UnityEngine;


public class AnimSourceActions
{
    [ConfigVar(Name = "animsource.actions.show", DefaultValue = "0", Description = "show")]
    public static ConfigVar ShowDebug;

    [Serializable]
    public struct ActionAnimationDefinitionAuthoring
    {
        public Ability.AbilityAction.Action action;
        public AnimationClip animation;
        public float restartTimeOffset;
    }

    public struct ActionAnimationDefinition
    {
        public Ability.AbilityAction.Action action;
        public BlobAssetReference<Clip> animation;
        public float restartTimeOffset;
    }

    public struct ActionAnimation
    {
        public static ActionAnimation Default => new ActionAnimation();
        public Ability.AbilityAction.Action Action;
        public BlobAssetReference<Clip> Clip;
        public BlobAssetReference<ClipInstance> ClipInstance;
        public float ClipDuration;
        public float RestartTimeOffset;

        public bool IsValid()
        {
            return Action != Ability.AbilityAction.Action.None;
        }
    }


    public struct ActionDefinitions : IBufferElementData
    {
        public ActionAnimationDefinition Value;
    }

    public struct ActionAnimations : IBufferElementData
    {
        public ActionAnimation Value;
    }

    [Serializable]
    public struct AuthoringSettings
    {
        public ActionAnimationDefinitionAuthoring[] ActionDef;
        public AnimationCurve reloadBlendOutAimCurve;
        public AnimationClip ActionAnimationsBasePose;
    }

    public struct Settings : IComponentData
    {
        public static Settings Default => new Settings();
        public BlobAssetReference<KeyframeCurveBlob> ReloadBlendOutAimCurve;
        public BlobAssetReference<Clip> BasePoseClip;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public float ActiveClipTime;

        public NodeHandle<LayerMixerNode> MixerNode;
        public Ability.AbilityAction.Action CurrentAction;
        public Ability.AbilityAction.Action CurrentBlendOutAimAction; // TODO: Can we re-use Current action?
        public float LastActionTime;
        public NodeHandle<ClipNode> ActionClipNode;
        public NodeHandle<ClipNode> BasePoseClipNode;
        public NodeHandle<DeltaNode> ActionDeltaNode;

        public ActionAnimation ReloadActionAnim;
        public float TimeSinceReloadStart;
        public int LastActionTick;
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

            var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob);
            Entities
                .WithNativeDisableContainerSafetyRestriction(cmdBuffer)
                .WithoutBurst()
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(cmdBuffer, entity, m_AnimationGraphSystem, state);
            }).Run();
            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            m_AnimationGraphSystem.RemoveRef();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var nodeSet = m_AnimationGraphSystem.Set;

            var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
            Entities
                .WithNone<SystemState>()
                .WithAll<ActionDefinitions>()
                .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
                .WithoutBurst() // nodeSet.SendMessage()
                .ForEach((Entity entity, ref AnimSource.Data animSource) =>
            {
                var state = SystemState.Default;

                var actionDefinitions = EntityManager.GetBuffer<ActionDefinitions>(entity).AsNativeArray();
                var numDefinitions = actionDefinitions.Length;
                if (numDefinitions == 0)
                    return;

                var actionAnimations = EntityManager.GetBuffer<ActionAnimations>(entity);

                for (var i = 0; i < (int)Ability.AbilityAction.Action.NumActions; i++)
                {
                    var entry = new ActionAnimations { Value = ActionAnimation.Default };
                    actionAnimations.Add(entry);
                }

                for (var i = 0; i < numDefinitions; i++)
                {
                    var def = actionDefinitions[i].Value;

                    if (!def.animation.IsCreated)
                        continue;

                    var actionAnim = ActionAnimation.Default;

                    actionAnim.Action = def.action;
                    actionAnim.Clip = def.animation;
                    actionAnim.ClipDuration = actionAnim.Clip.Value.Duration;
                    actionAnim.RestartTimeOffset = def.restartTimeOffset;

                    if (def.action == Ability.AbilityAction.Action.Reloading)
                    {
                        state.ReloadActionAnim = actionAnim;
                    }

                    var animEntry = new ActionAnimations { Value = actionAnim };
                    actionAnimations[(int)def.action] = animEntry;
                }

                state.MixerNode = AnimationGraphHelper.CreateNode<LayerMixerNode>(m_AnimationGraphSystem,"MixerNode");
                state.ActionClipNode = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"ActionClipNode");
                state.BasePoseClipNode = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"BasePoseClipNode");
                state.ActionDeltaNode = AnimationGraphHelper.CreateNode<DeltaNode>(m_AnimationGraphSystem,"ActionDeltaNode");

                nodeSet.Connect(state.ActionClipNode, ClipNode.KernelPorts.Output, state.ActionDeltaNode, DeltaNode.KernelPorts.Input);
                nodeSet.Connect(state.BasePoseClipNode, ClipNode.KernelPorts.Output, state.ActionDeltaNode, DeltaNode.KernelPorts.Subtract);
                nodeSet.Connect(state.ActionDeltaNode, DeltaNode.KernelPorts.Output, state.MixerNode, LayerMixerNode.KernelPorts.Input1);

                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.BlendModeInput0, BlendingMode.Override);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput0, 1f);

                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput1, 1f);

                // Expose input and outputs
                animSource.inputNode = state.MixerNode;
                animSource.inputPortID = (InputPortID)LayerMixerNode.KernelPorts.Input0;
                animSource.outputNode = state.MixerNode;
                animSource.outputPortID = (OutputPortID)LayerMixerNode.KernelPorts.Output;

                PostUpdateCommands.AddComponent(entity, state);
            }).Run();

            Entities
                .WithNone<ActionDefinitions>()
                .WithoutBurst()
                .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(PostUpdateCommands, entity, m_AnimationGraphSystem, state);
            }).Run();

            PostUpdateCommands.Playback(EntityManager);
            PostUpdateCommands.Dispose();
            return default;
        }

        static void Deinitialize(EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            if (state.MixerNode != default && animGraphSys.Set.Exists(state.MixerNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.MixerNode);

            if (state.MixerNode != default && animGraphSys.Set.Exists(state.BasePoseClipNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.BasePoseClipNode);

            if (state.MixerNode != default && animGraphSys.Set.Exists(state.ActionDeltaNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.ActionDeltaNode);

            if (state.ActionClipNode != default && animGraphSys.Set.Exists(state.ActionClipNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.ActionClipNode);

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
            var globalTime = m_GlobalGameTimeQuery.GetSingleton<GlobalGameTime>();
            var deltaTime = globalTime.frameDuration;

            var CharacterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>();
            Entities
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref SystemState state, ref Settings settings,
                ref AnimSource.AllowWrite allowWrite) =>
            {
                if (!state.ReloadActionAnim.IsValid())
                    return;

                if (allowWrite.FirstUpdate)
                {
                    // Make sure we don't trigger reload blend out on first update. So TimeSinceReloadStart should be at least clip length
                    state.TimeSinceReloadStart = state.ReloadActionAnim.ClipDuration;
                    allowWrite.FirstUpdate = false;
                    return;
                }

                var charInterpolatedState = CharacterInterpolatedDataFromEntity[animSource.animStateEntity];

                // take the blend length
                // TODO: Expose blend durations and create separate ones for in and out

                // Once the actions starts, start counting time and generate a blend value
                if (charInterpolatedState.charAction == Ability.AbilityAction.Action.Reloading &&
                    state.CurrentBlendOutAimAction != Ability.AbilityAction.Action.Reloading)
                {
                    state.TimeSinceReloadStart = 0;
                }

                // Apply the blend value, possibly smoothed or through an animation curve
                charInterpolatedState.blendOutAim = 0f;
                if (charInterpolatedState.charAction == Ability.AbilityAction.Action.Reloading && settings.ReloadBlendOutAimCurve != BlobAssetReference<KeyframeCurveBlob>.Null)
                {
                    var normalizedTime = state.TimeSinceReloadStart / state.ReloadActionAnim.ClipDuration;
                    charInterpolatedState.blendOutAim = 1f - KeyframeCurveEvaluator.Evaluate(normalizedTime, settings.ReloadBlendOutAimCurve);
                }

                state.CurrentBlendOutAimAction = charInterpolatedState.charAction;
                state.TimeSinceReloadStart += deltaTime;

                CharacterInterpolatedDataFromEntity[animSource.animStateEntity] = charInterpolatedState;
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
            var nodeSet = World.GetExistingSystem<AnimationGraphSystem>().Set;

            var globalTime = m_GlobalGameTimeQuery.GetSingleton<GlobalGameTime>();
            var deltaTime = globalTime.frameDuration;

            var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

            // Handle rig change
            Entities
                .WithNativeDisableContainerSafetyRestriction(cmdBuffer)
                .WithoutBurst() // nodeSet.SendMessage()
                .WithNone<AnimSource.HasValidRig>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;

                var actionAnims = EntityManager.GetBuffer<ActionAnimations>(entity);
                for (var i = 0; i < actionAnims.Length; i++)
                {
                    var anim = actionAnims[i].Value;
                    if (anim.IsValid())
                    {
                        var clip = actionAnims[i].Value.Clip;
                        var clipInstance = ClipManager.Instance.GetClipFor(rig, clip);

                        anim.ClipInstance = clipInstance;
                        actionAnims[i] = new ActionAnimations { Value = anim};

                        // Clip player needs an instance, so give it one
                        nodeSet.SendMessage(state.ActionClipNode, ClipNode.SimulationPorts.ClipInstance, clipInstance);
                    }
                }

                nodeSet.SendMessage(state.BasePoseClipNode, ClipNode.SimulationPorts.ClipInstance, ClipManager.Instance.GetClipFor(rig, settings.BasePoseClip));
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.ActionDeltaNode, DeltaNode.SimulationPorts.RigDefinition, rig);

                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            Entities
                .WithoutBurst() // nodeSet.SendMessage()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<Character.InterpolatedData>(animSource.animStateEntity))
                {
                    GameDebug.LogWarning(World,"AnimSource does not have Character.InterpolatedData components. Has it been deleted?");
                    return;
                }

                var charInterpolatedState = EntityManager.GetComponentData<Character.InterpolatedData>(animSource.animStateEntity);

                // TODO: (sunek) This all has been simplified so it no longer supports action restart offsets for running actions. Re-introduce
                // var actionTime = time.DurationSinceTick(charInterpolatedState.charActionTick);

                var actionAnims = EntityManager.GetBuffer<ActionAnimations>(entity);

                // Handle action change. This does not happen when action changes to None
                var newAction = charInterpolatedState.charAction;

                if (charInterpolatedState.charActionTick > state.LastActionTick && actionAnims[(int)newAction].Value.IsValid())
                {
                    // Start the new animation
                    state.CurrentAction = newAction;
                    state.ActiveClipTime = 0f;
                    var currentActionAnim = actionAnims[(int)state.CurrentAction].Value;
                    nodeSet.SendMessage(state.ActionClipNode, ClipNode.SimulationPorts.ClipInstance, currentActionAnim.ClipInstance);

                    GameDebug.Log(World, ShowDebug, "Starting new action: " + newAction + " Action Tick: " + charInterpolatedState.charActionTick + " Last action Tick: "  + state.LastActionTick);
                }

                state.ActiveClipTime += deltaTime;
                nodeSet.SetData(state.ActionClipNode, ClipNode.KernelPorts.Time, state.ActiveClipTime);
                state.LastActionTick = charInterpolatedState.charActionTick;
            }).Run();

            return default;
        }
    }
}
