using System;
using UnityEngine;

using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Animation;

[Serializable]
public struct TwoBoneIKConstraint
{
    public string Root;
    public string Mid;
    public string Tip;

    public string Target;
    public string Hint;

    public bool MaintainPositionOffset;
    public bool MaintainRotationOffset;
}

[Serializable]
public struct BlendConstraint
{
    public string SourceA;
    public string SourceB;
    public string Constrained;

    public bool MaintainPositionOffset;
    public bool MaintainRotationOffset;
}

public interface IConstraintProperties { }

public interface IConstraintNode
{
    void Create(ref NodeSet set, BlobAssetReference<RigDefinition> rig);
    void Update(ref NodeSet set, IConstraintProperties properties);

    NodeHandle Node { get; }
    InputPortID InputPort { get; }
    OutputPortID OutputPort { get; }

    Type PropertyType { get; }
    int PropertyIndex { get; set; }

    void CreateGraphBuffer(ref NodeSet set, out GraphValue<Buffer<float>> buffer);
}

public abstract class ConstraintNode<T> : IConstraintNode
    where T : struct, IConstraintProperties
{
    public abstract void Create(ref NodeSet set, BlobAssetReference<RigDefinition> rig);
    public abstract void Update(ref NodeSet set, ref T properties);

    public abstract NodeHandle Node { get; }
    public abstract InputPortID InputPort { get; }
    public abstract OutputPortID OutputPort { get; }

    public Type PropertyType => typeof(T);
    public abstract int PropertyIndex { get; set; }

    public abstract void CreateGraphBuffer(ref NodeSet set, out GraphValue<Buffer<float>> buffer);

    void IConstraintNode.Update(ref NodeSet set, IConstraintProperties properties)
    {
        UnityEngine.Assertions.Assert.IsTrue(properties is T);
        T tProperties = (T)properties;
        Update(ref set, ref tProperties);
    }
}

[Serializable]
public struct TwoBoneIKConstraintProperties : IConstraintProperties
{
    [Range(0f, 1f)] public float Weight;
    [Range(0f, 1f)] public float TargetPositionWeight;
    [Range(0f, 1f)] public float TargetRotationWeight;
    [Range(0f, 1f)] public float HintWeight;
}

public class RuntimeTwoBoneIKConstraint : ConstraintNode<TwoBoneIKConstraintProperties>
{
    [HideInInspector]
    private NodeHandle<TwoBoneIKNode> m_Node;

    [HideInInspector]
    public TwoBoneIKNode.TwoBoneIKData Data;

    public override void Create(ref NodeSet set, BlobAssetReference<RigDefinition> rig)
    {
        m_Node = set.Create<TwoBoneIKNode>();
        set.SendMessage(m_Node, TwoBoneIKNode.SimulationPorts.RigDefinition, in rig);
        set.SendMessage(m_Node, TwoBoneIKNode.SimulationPorts.TwoBoneIKSetup, in Data);
    }

    public override void Update(ref NodeSet set, ref TwoBoneIKConstraintProperties peoperties)
    {
        set.SetData(m_Node, TwoBoneIKNode.KernelPorts.Weight, peoperties.Weight);
        set.SetData(m_Node, TwoBoneIKNode.KernelPorts.TargetPositionWeight, peoperties.TargetPositionWeight);
        set.SetData(m_Node, TwoBoneIKNode.KernelPorts.TargetRotationWeight, peoperties.TargetRotationWeight);
        set.SetData(m_Node, TwoBoneIKNode.KernelPorts.HintWeight, peoperties.HintWeight);
    }

    public override NodeHandle Node => m_Node;
    public override InputPortID InputPort => (InputPortID)TwoBoneIKNode.KernelPorts.Input;
    public override OutputPortID OutputPort => (OutputPortID)TwoBoneIKNode.KernelPorts.Output;

    public override void CreateGraphBuffer(ref NodeSet set, out GraphValue<Buffer<float>> buffer)
    {
        buffer = set.CreateGraphValue(m_Node, TwoBoneIKNode.KernelPorts.Output);
    }

    public override int PropertyIndex { get; set; }
}

[Serializable]
public struct BlendConstraintProperties : IConstraintProperties
{
    [Range(0f, 1f)] public float Weight;
    [Range(0f, 1f)] public float PositionWeight;
    [Range(0f, 1f)] public float RotationWeight;

    public bool BlendPosition;
    public bool BlendRotation;
}

public class RuntimeBlendConstraint : ConstraintNode<BlendConstraintProperties>
{
    [HideInInspector]
    public NodeHandle<BlendTransformNode> m_Node;

    [HideInInspector]
    public BlendTransformNode.BlendTransformData Data;

    public override void Create(ref NodeSet set, BlobAssetReference<RigDefinition> rig)
    {
        m_Node = set.Create<BlendTransformNode>();
        set.SendMessage(m_Node, BlendTransformNode.SimulationPorts.RigDefinition, in rig);
        set.SendMessage(m_Node, BlendTransformNode.SimulationPorts.BlendTransformSetup, in Data);
    }

    public override void Update(ref NodeSet set, ref BlendConstraintProperties properties)
    {
        set.SetData(m_Node, BlendTransformNode.KernelPorts.Weight, properties.Weight);
        set.SetData(m_Node, BlendTransformNode.KernelPorts.PositionWeight, properties.PositionWeight);
        set.SetData(m_Node, BlendTransformNode.KernelPorts.RotationWeight, properties.RotationWeight);
        set.SetData(m_Node, BlendTransformNode.KernelPorts.BlendPosition, properties.BlendPosition ? 1 : 0);
        set.SetData(m_Node, BlendTransformNode.KernelPorts.BlendRotation, properties.BlendRotation ? 1 : 0);
    }

    public override NodeHandle Node => m_Node;
    public override InputPortID InputPort => (InputPortID)BlendTransformNode.KernelPorts.Input;
    public override OutputPortID OutputPort => (OutputPortID)BlendTransformNode.KernelPorts.Output;

    public override void CreateGraphBuffer(ref NodeSet set, out GraphValue<Buffer<float>> buffer)
    {
        buffer = set.CreateGraphValue(m_Node, BlendTransformNode.KernelPorts.Output);
    }

    public override int PropertyIndex { get; set; }
}
