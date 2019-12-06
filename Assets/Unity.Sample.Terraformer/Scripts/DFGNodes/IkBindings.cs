using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class IkBindings : MonoBehaviour
{
    public Unity.Animation.Hybrid.RigComponent Rig;
    public TwoBoneIKConstraint Definition;
    public TwoBoneIKCProperties Settings;
    public TwoBoneIKData Data;

    public struct IkData : IComponentData
    {
        public Entity LeftArmEntity;
        public Entity LeftFootEntity;
        public Entity RightFootEntity;
    }

    [Serializable]
    public struct TwoBoneIKData : IComponentData
    {
        public int Root;
        public int Mid;
        public int Tip;
        public int Target;
        public int Hint;

        public RigidTransform TargetOffset;
        public float2 LimbLengths;

        public static readonly TwoBoneIKData Null = new TwoBoneIKData() {
            Root = -1, Mid = -1, Tip = -1, Target = -1, Hint = -1, TargetOffset = RigidTransform.identity, LimbLengths = float2.zero
        };
    }

    [Serializable]
    public enum IkWeightCurve
    {
        None,
        LeftLeg,
        RightLeg,
        LeftHand,
        RightHand,
    }

    [Serializable]
    public struct TwoBoneIKCProperties
    {
        [Range(0f, 1f)] public float Weight;
        [Range(0f, 1f)] public float TargetPositionWeight;
        [Range(0f, 1f)] public float TargetRotationWeight;
        [Range(0f, 1f)] public float HintWeight;
#pragma warning disable 649
        [SerializeField] IkWeightCurve useWeightCurve;
#pragma warning restore 649
        public int WeightCurve => (int)useWeightCurve -1;
    }

    public void FindIndexes()
    {
        var skeletonNameToIndexMap = CreateSkeletonNameToIndexMap(Rig);
        var ik = new RuntimeTwoBoneIKConstraint();

//        var data = new Unity.Animation.TwoBoneIKNode.TwoBoneIKData();
        Data.Root = GetIndexFromName(skeletonNameToIndexMap, Definition.Root);
        Data.Mid = GetIndexFromName(skeletonNameToIndexMap, Definition.Mid);
        Data.Tip = GetIndexFromName(skeletonNameToIndexMap, Definition.Tip);
        Data.Target = GetIndexFromName(skeletonNameToIndexMap, Definition.Target);
        Data.Hint = GetIndexFromName(skeletonNameToIndexMap, Definition.Hint);

        Data.LimbLengths.x = math.distance(Rig.Bones[Data.Root].position, Rig.Bones[Data.Mid].position);
        Data.LimbLengths.y = math.distance(Rig.Bones[Data.Mid].position, Rig.Bones[Data.Tip].position);
    }

    static Dictionary<string, int> CreateSkeletonNameToIndexMap(Unity.Animation.Hybrid.RigComponent rig)
    {
        Dictionary<string, int> nameToIndex = new Dictionary<string, int>(rig.Bones.Length);
        for (int i = 0; i < rig.Bones.Length; ++i)
            nameToIndex.Add(rig.Bones[i].name, i);

        return nameToIndex;
    }

    static int GetIndexFromName(Dictionary<string, int> skeletonNameToIndexMap, string name)
    {
        return skeletonNameToIndexMap.TryGetValue(name, out int index) ? index : -1;
    }
}
