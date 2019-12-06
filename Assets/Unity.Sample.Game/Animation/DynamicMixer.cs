using System.Security.Permissions;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.DataFlowGraph;
using UnityEngine;


public struct DynamicMixerInput : ISystemStateBufferElementData
{
    public NodeHandle SourceNode;
    public OutputPortID SourcePortId;
    public Entity SourceEntity;            // TODO (mogensh) these should not be owned by mixer, but they are here for convenience
    public NodeHandle<MixerAddNode> AddNode;
    public float weight;
}

public struct DynamicMixer : ISystemStateComponentData
{
    public NodeHandle<MixerBeginNode> MixerBegin;
    public NodeHandle<MixerEndNode> MixerEnd;
    public BlobAssetReference<RigDefinition> Rig;

    public static DynamicMixer AddComponents(EntityCommandBuffer cmdBuffer, NodeSet set, Entity entity)
    {
        var dynamicMixer = new DynamicMixer
        {
            MixerBegin = set.Create<MixerBeginNode>(),
            MixerEnd = set.Create<MixerEndNode>(),
        };
        cmdBuffer.AddComponent(entity, dynamicMixer);
        cmdBuffer.AddBuffer<DynamicMixerInput>(entity);
        set.Connect(dynamicMixer.MixerBegin, MixerBeginNode.KernelPorts.Output, dynamicMixer.MixerEnd,
            MixerEndNode.KernelPorts.Input);

        return dynamicMixer;
    }

    public void Dispose(EntityManager entityManager, Entity entity, NodeSet set)
    {
        DestroyNodes(set, this, entityManager.GetBuffer<DynamicMixerInput>(entity));
    }

    public static void AddInput(NodeSet set, ref DynamicMixer dynamicMixer, DynamicBuffer<DynamicMixerInput> inputs,
        Entity sourceEntity, NodeHandle sourceNode, OutputPortID sourcePortId)
    {
        // Attempt to find unsused input
        for (int i = 0; i < inputs.Length; i++)
        {
            var input = inputs[i];
            if (input.SourceEntity == Entity.Null)
            {
//                GameDebug.Log("Added input at index:" + i);

                input.SourceEntity = sourceEntity;
                input.SourceNode = sourceNode;
                input.SourcePortId = sourcePortId;
                inputs[i] = input;

                set.Connect(sourceNode, sourcePortId, input.AddNode, (InputPortID) MixerAddNode.KernelPorts.Add);
                return;
            }
        }

        // Create new input
//        GameDebug.Log("Added input. New input index:" + inputs.Length);
        var newNode = set.Create<MixerAddNode>();
        set.SendMessage(newNode, MixerAddNode.SimulationPorts.RigDefinition, dynamicMixer.Rig);
        set.Connect(sourceNode, sourcePortId, newNode, (InputPortID) MixerAddNode.KernelPorts.Add);

        //  First node
        if (inputs.Length == 0)
        {
            set.Disconnect(dynamicMixer.MixerBegin, MixerBeginNode.KernelPorts.Output, dynamicMixer.MixerEnd,
                MixerEndNode.KernelPorts.Input);

            // Connect begin to add node
            set.Connect(dynamicMixer.MixerBegin, MixerBeginNode.KernelPorts.Output, newNode,
                MixerAddNode.KernelPorts.Input);
            set.Connect(dynamicMixer.MixerBegin, MixerBeginNode.KernelPorts.SumWeight, newNode,
                MixerAddNode.KernelPorts.SumWeightInput);

            // Connect Add node to end
            set.Connect(newNode, MixerAddNode.KernelPorts.Output, dynamicMixer.MixerEnd,
                MixerEndNode.KernelPorts.Input);
            set.Connect(newNode, MixerAddNode.KernelPorts.SumWeightOutput, dynamicMixer.MixerEnd,
                MixerEndNode.KernelPorts.SumWeight);
        }
        else
        {
            var prevInput = inputs[inputs.Length - 1];

            // Disconnect prev from end
            set.Disconnect(prevInput.AddNode, MixerAddNode.KernelPorts.Output, dynamicMixer.MixerEnd,
                MixerEndNode.KernelPorts.Input);
            set.Disconnect(prevInput.AddNode, MixerAddNode.KernelPorts.SumWeightOutput, dynamicMixer.MixerEnd,
                MixerEndNode.KernelPorts.SumWeight);

            // Connect prev to new node
            set.Connect(prevInput.AddNode, MixerAddNode.KernelPorts.Output, newNode, MixerAddNode.KernelPorts.Input);
            set.Connect(prevInput.AddNode, MixerAddNode.KernelPorts.SumWeightOutput, newNode,
                MixerAddNode.KernelPorts.SumWeightInput);

            // Connect new node to end
            set.Connect(newNode, MixerAddNode.KernelPorts.Output, dynamicMixer.MixerEnd,
                MixerEndNode.KernelPorts.Input);
            set.Connect(newNode, MixerAddNode.KernelPorts.SumWeightOutput, dynamicMixer.MixerEnd,
                MixerEndNode.KernelPorts.SumWeight);
        }

        var newInput = new DynamicMixerInput
        {
            SourceEntity = sourceEntity,
            SourceNode = sourceNode,
            SourcePortId = sourcePortId,
            AddNode = newNode,
        };
        inputs.Add(newInput);
    }

    public static void RemoveInput(NodeSet set, DynamicBuffer<DynamicMixerInput> inputs, int index)
    {
//        GameDebug.Log("Remove input index:" + index);
        var input = inputs[index];
        set.Disconnect(input.SourceNode, input.SourcePortId, input.AddNode, (InputPortID) MixerAddNode.KernelPorts.Add);
        input.SourceEntity = Entity.Null;
        input.weight = 0;
        inputs[index] = input;
    }

    public static void ApplyWeight(NodeSet nodeSet, DynamicBuffer<DynamicMixerInput> inputs)
    {
        // Apply weight no nodes
        for (int i = 0; i < inputs.Length; i++)
        {
            nodeSet.SetData(inputs[i].AddNode, MixerAddNode.KernelPorts.Weight, inputs[i].weight);
        }
    }


    public static void SetRig(NodeSet set, ref DynamicMixer dynamicMixer, DynamicBuffer<DynamicMixerInput> inputs,
        BlobAssetReference<RigDefinition> rig)
    {
        dynamicMixer.Rig = rig;
        set.SendMessage(dynamicMixer.MixerBegin, MixerBeginNode.SimulationPorts.RigDefinition, rig);
        set.SendMessage(dynamicMixer.MixerEnd, MixerEndNode.SimulationPorts.RigDefinition, rig);
        for (int i = 0; i < inputs.Length; i++)
        {
            set.SendMessage(inputs[i].AddNode, MixerAddNode.SimulationPorts.RigDefinition, rig);
        }
    }


    static void DestroyNodes(NodeSet set, DynamicMixer dynamicMixer, DynamicBuffer<DynamicMixerInput> inputs)
    {
        if (set.Exists(dynamicMixer.MixerBegin))
            set.Destroy(dynamicMixer.MixerBegin);
        if (set.Exists(dynamicMixer.MixerEnd))
            set.Destroy(dynamicMixer.MixerEnd);
        for (int i = 0; i < inputs.Length; i++)
        {
            set.Destroy(inputs[i].AddNode);
        }
    }
}


