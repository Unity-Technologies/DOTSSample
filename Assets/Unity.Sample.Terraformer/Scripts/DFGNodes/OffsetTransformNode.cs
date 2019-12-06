using System;
using UnityEngine;

namespace Unity.Animation
{
    using Burst;
    using Mathematics;
    using DataFlowGraph;
    using Profiling;
    using Entities;

    public class OffsetTransformNode
        : NodeDefinition<OffsetTransformNode.Data, OffsetTransformNode.SimPorts, OffsetTransformNode.KernelData, OffsetTransformNode.KernelDefs, OffsetTransformNode.Kernel>
        , IMsgHandler<BlobAssetReference<RigDefinition>>
        , IMsgHandler<OffsetTransformNode.OffsetData>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<OffsetTransformNode, BlobAssetReference<RigDefinition>> RigDefinition;
            public MessageInput<OffsetTransformNode, OffsetData> OffsetData;
        }

        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.OffsetTransformNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<OffsetTransformNode, Buffer<float>> Input;
            public DataOutput<OffsetTransformNode, Buffer<float>> Output;
            public DataInput<OffsetTransformNode, float> Weight;
        }

        public struct Data : INodeData
        {
        }

        public struct OffsetData
        {
            public int BoneIndex;
            public float3 offset;
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfilerMarker;
            public OffsetData Data;
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
            {
                data.ProfilerMarker.Begin();
                var output = ctx.Resolve(ref ports.Output);
                output.CopyFrom(ctx.Resolve(in ports.Input));

                var weightValue = ctx.Resolve(ports.Weight);
                if (weightValue > 0f)
                {
                    var stream = AnimationStreamProvider.Create(data.RigDefinition, output);
                    if (stream.IsNull)
                    {
                        data.ProfilerMarker.End();
                        return;
                    }

                    var pos = stream.GetLocalToRigTranslation(data.Data.BoneIndex);
                    pos += data.Data.offset * weightValue;
                    stream.SetLocalToRigTranslation(data.Data.BoneIndex, pos);
                }

                data.ProfilerMarker.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfileMarker;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
        {
            GetKernelData(ctx.Handle).RigDefinition = rigBindings;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigBindings.Value.Bindings.CurveCount));
        }

        public void HandleMessage(in MessageContext ctx, in OffsetData data)
        {
            GetKernelData(ctx.Handle).Data = data;
        }
    }
}
