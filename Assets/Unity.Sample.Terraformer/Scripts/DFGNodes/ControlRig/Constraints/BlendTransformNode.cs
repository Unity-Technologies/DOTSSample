namespace Unity.Animation
{
    using Burst;
    using Mathematics;
    using DataFlowGraph;
    using Profiling;
    using Entities;

    public class BlendTransformNode
        : NodeDefinition<BlendTransformNode.Data, BlendTransformNode.SimPorts, BlendTransformNode.KernelData, BlendTransformNode.KernelDefs, BlendTransformNode.Kernel>
        , IMsgHandler<BlobAssetReference<RigDefinition>>
        , IMsgHandler<BlendTransformNode.BlendTransformData>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<BlendTransformNode, BlobAssetReference<RigDefinition>> RigDefinition;
            public MessageInput<BlendTransformNode, BlendTransformData> BlendTransformSetup;
        }

        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.BlendTransformNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<BlendTransformNode , Buffer<float>> Input;
            public DataOutput<BlendTransformNode, Buffer<float>> Output;

            public DataInput<BlendTransformNode, float> Weight;
            public DataInput<BlendTransformNode, float> PositionWeight;
            public DataInput<BlendTransformNode, float> RotationWeight;
            public DataInput<BlendTransformNode, int> BlendPosition;
            public DataInput<BlendTransformNode, int> BlendRotation;
        }

        public struct Data : INodeData
        {
        }

        public struct BlendTransformData
        {
            public int SourceA;
            public int SourceB;
            public int Constrained;

            public RigidTransform SourceAOffset;
            public RigidTransform SourceBOffset;

            public static readonly BlendTransformData Null = new BlendTransformData() {
                SourceA = -1, SourceB = -1, Constrained = -1, SourceAOffset = RigidTransform.identity, SourceBOffset = RigidTransform.identity
            };
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfilerMarker;

            public BlendTransformData Data;
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
            {
                data.ProfilerMarker.Begin();
                CopyInputToOutputBuffer(ctx, ports.Input, ports.Output);

                var weightValue = ctx.Resolve(ports.Weight);

                if (weightValue > 0f)
                {
                    var ouptutArray = ctx.Resolve(ref ports.Output);
                    var blendPosition = ctx.Resolve(ports.BlendPosition);
                    var blendRotation = ctx.Resolve(ports.BlendRotation);

                    var stream = AnimationStreamProvider.Create(data.RigDefinition, ouptutArray);
                    if (stream.IsNull || (blendPosition == 0 && blendRotation == 0))
                    {
                        data.ProfilerMarker.End();
                        return;
                    }

                    stream.GetLocalToRigTR(data.Data.Constrained, out float3 constrainedT, out quaternion constrainedR);

                    if (blendPosition == 1)
                    {
                        var positionWeight = ctx.Resolve(ports.PositionWeight);

                        float3 posBlend = math.lerp(
                            stream.GetLocalToRigTranslation(data.Data.SourceA) + data.Data.SourceAOffset.pos,
                            stream.GetLocalToRigTranslation(data.Data.SourceB) + data.Data.SourceBOffset.pos,
                            positionWeight
                            );

                        stream.SetLocalToRigTranslation(
                            data.Data.Constrained,
                            math.lerp(constrainedT, posBlend, weightValue)
                            );
                    }

                    if (blendRotation == 1)
                    {
                        var rotationWeight = ctx.Resolve(ports.RotationWeight);

                        quaternion rotBlend = math.nlerp(
                            math.mul(stream.GetLocalToRigRotation(data.Data.SourceA), data.Data.SourceAOffset.rot),
                            math.mul(stream.GetLocalToRigRotation(data.Data.SourceB), data.Data.SourceBOffset.rot),
                            rotationWeight
                            );

                        stream.SetLocalToRigRotation(
                            data.Data.Constrained,
                            math.nlerp(constrainedR, rotBlend, weightValue)
                            );
                    }
                }

                data.ProfilerMarker.End();
            }

            // FIXME: We should have a proper memcpy to prime the output port
            static void CopyInputToOutputBuffer(RenderContext ctx, DataInput<BlendTransformNode, Buffer<float>> input, DataOutput<BlendTransformNode, Buffer<float>> output)
            {
                var inputArray = ctx.Resolve(input);
                var outputArray = ctx.Resolve(ref output);
                for (int i = 0, count = inputArray.Length; i != count; ++i)
                    outputArray[i] = inputArray[i];
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfileMarker;
            kData.Data = BlendTransformData.Null;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
        {
            GetKernelData(ctx.Handle).RigDefinition = rigBindings;
            Set.SetBufferSize(Set.CastHandle<BlendTransformNode>(ctx.Handle), (OutputPortID)KernelPorts.Output, rigBindings.Value.Bindings.CurveCount);
        }

        public void HandleMessage(in MessageContext ctx, in BlendTransformData data)
        {
            GetKernelData(ctx.Handle).Data = data;
        }
    }
}
