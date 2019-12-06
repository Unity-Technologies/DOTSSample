using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

#if UNITY_EDITOR

public class AnimSourceSelectorAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity, IBundledAssetProvider
{
    [Serializable]
    public class TransitionDuration
    {
        public WeakAssetReference From;
        public WeakAssetReference To;
        public float Duration;
    }


    public DecisionTreeNodeConvert AnimSourceDecisionTree;

    public float DefaultTransitionDuration = 0.2f;
    public List<TransitionDuration> TransitionDurations = new List<TransitionDuration>();


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var blobBuilder = new BlobBuilder(Allocator.Temp);
        ref var root = ref blobBuilder.ConstructRoot<AnimSourceSelector.AssetBlob>();

        // Setup transition
        root.DefaultTransitionDuration = DefaultTransitionDuration;
        var transitionDuration = blobBuilder.Allocate(ref root.TransitionDuration, TransitionDurations.Count);
        for(int i = 0; i < TransitionDurations.Count; i++)
        {
            transitionDuration[i] = new AnimSourceSelector.TransitionDuration
            {
                From = TransitionDurations[i].From,
                To = TransitionDurations[i].To,
                Duration = TransitionDurations[i].Duration,
            };
        }

        // Setup animsource assets
        var animSourceReferences = GetComponentsInChildren<AnimSourceReferenceAuthoring>();
        var animSourceAssets = blobBuilder.Allocate(ref root.AnimSourceAssets, animSourceReferences.Length);
        for (int i = 0; i < animSourceReferences.Length; i++)
        {
            animSourceAssets[i] = animSourceReferences[i].animSource;
        }

        var rootRef =  blobBuilder.CreateBlobAssetReference<AnimSourceSelector.AssetBlob>(Allocator.Persistent);

        dstManager.AddComponentData(entity, new AnimSource.Data());

        var inputState = AnimSourceSelector.InputState.Default;
        inputState.AnimSourceDecisionTree = AnimSourceDecisionTree != null ?
            conversionSystem.GetPrimaryEntity(AnimSourceDecisionTree) : Entity.Null;
        inputState.Resource = rootRef;
        dstManager.AddComponentData(entity, inputState);

        var transitions = GetComponents<ITransitionBehaviour>();
        foreach (var transition in transitions)
        {
            transition.AddTransition(dstManager,entity);
        }
    }

    public void AddBundledAssets(BuildType buildType, List<WeakAssetReference> assets)
    {
        var animSourceReferences = GetComponentsInChildren<AnimSourceReferenceAuthoring>();
        foreach (var reference in animSourceReferences)
        {
            assets.Add(reference.animSource);
        }
    }
}

#endif


