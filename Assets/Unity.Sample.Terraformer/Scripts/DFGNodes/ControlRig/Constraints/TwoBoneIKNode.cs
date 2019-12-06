namespace Unity.Animation
{
    using Burst;
    using Mathematics;
    using DataFlowGraph;
    using Profiling;
    using Entities;

    public class TwoBoneIKNode
        : NodeDefinition<TwoBoneIKNode.Data, TwoBoneIKNode.SimPorts, TwoBoneIKNode.KernelData, TwoBoneIKNode.KernelDefs, TwoBoneIKNode.Kernel>
        , IMsgHandler<BlobAssetReference<RigDefinition>>
        , IMsgHandler<TwoBoneIKNode.TwoBoneIKData>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<TwoBoneIKNode, BlobAssetReference<RigDefinition>> RigDefinition;
            public MessageInput<TwoBoneIKNode, TwoBoneIKData> TwoBoneIKSetup;
        }

        const float k_SqEpsilon = 1e-8f;
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.TwoBoneIKNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<TwoBoneIKNode, Buffer<float>> Input;
            public DataOutput<TwoBoneIKNode, Buffer<float>> Output;

            public DataInput<TwoBoneIKNode, float> Weight;
            public DataInput<TwoBoneIKNode, float> TargetPositionWeight;
            public DataInput<TwoBoneIKNode, float> TargetRotationWeight;
            public DataInput<TwoBoneIKNode, float> HintWeight;
        }

        public struct Data : INodeData
        {
        }

        public struct TwoBoneIKData
        {
            public int Root;
            public int Mid;
            public int Tip;
            public int Target;
            public int Hint;
            public int WeightChannelIdx;

            public RigidTransform TargetOffset;
            public float2 LimbLengths;

            public static readonly TwoBoneIKData Null = new TwoBoneIKData
            {
                Root = -1, Mid = -1, Tip = -1, Target = -1, Hint = -1, TargetOffset = RigidTransform.identity,
                LimbLengths = float2.zero, WeightChannelIdx = -1,
            };
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfilerMarker;

            public TwoBoneIKData IKData;
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

                    // Use the curve value if defined
                    if (data.IKData.WeightChannelIdx != -1)
                    {
                        weightValue *= stream.GetFloat(data.IKData.WeightChannelIdx);
                    }

                    weightValue = math.clamp(weightValue, 0f, 1f);

                    var targetPositionWeightValue = ctx.Resolve(ports.TargetPositionWeight);
                    var targetRotationWeightValue = ctx.Resolve(ports.TargetRotationWeight);
                    var hintWeightValue = ctx.Resolve(ports.HintWeight);

                    float3 aPos = stream.GetLocalToRigTranslation(data.IKData.Root);
                    float3 bPos = stream.GetLocalToRigTranslation(data.IKData.Mid);
                    float3 cPos = stream.GetLocalToRigTranslation(data.IKData.Tip);

                    stream.GetLocalToRigTR(data.IKData.Target, out float3 targetPos, out quaternion targetRot);
                    float3 tPos = math.lerp(cPos, targetPos + data.IKData.TargetOffset.pos, targetPositionWeightValue * weightValue);
                    quaternion tRot = math.nlerp(stream.GetLocalToRigRotation(data.IKData.Tip), math.mul(targetRot, data.IKData.TargetOffset.rot), targetRotationWeightValue * weightValue);
                    float hintWeight = hintWeightValue * weightValue;
                    bool hasHint = data.IKData.Hint > -1 && hintWeight > 0f;

                    float3 ab = bPos - aPos;
                    float3 bc = cPos - bPos;
                    float3 ac = cPos - aPos;
                    float3 at = tPos - aPos;

                    float oldAbcAngle = TriangleAngle(math.length(ac), data.IKData.LimbLengths);
                    float newAbcAngle = TriangleAngle(math.length(at), data.IKData.LimbLengths);

                    // Bend normal strategy is to take whatever has been provided in the animation
                    // stream to minimize configuration changes, however if this is collinear
                    // try computing a bend normal given the desired target position.
                    // If this also fails, try resolving axis using hint if provided.
                    float3 axis = math.cross(ab, bc);
                    if (math.lengthsq(axis) < k_SqEpsilon)
                    {
                        axis = math.cross(at, bc);
                        if (math.lengthsq(axis) < k_SqEpsilon)
                            axis = hasHint ? math.cross(stream.GetLocalToRigTranslation(data.IKData.Hint) - aPos, bc) : math.up();
                    }
                    axis = math.normalize(axis);

                    float a = 0.5f * (oldAbcAngle - newAbcAngle);
                    float sin = math.sin(a);
                    float cos = math.cos(a);
                    quaternion deltaRot = new quaternion(axis.x * sin, axis.y * sin, axis.z * sin, cos);
                    stream.SetLocalToRigRotation(data.IKData.Mid, math.mul(deltaRot, stream.GetLocalToRigRotation(data.IKData.Mid)));

                    cPos = stream.GetLocalToRigTranslation(data.IKData.Tip);
                    ac = cPos - aPos;
                    stream.SetLocalToRigRotation(data.IKData.Root, math.mul(mathex.fromTo(ac, at), stream.GetLocalToRigRotation(data.IKData.Root)));

                    if (hasHint)
                    {
                        float acLengthSq = math.lengthsq(ac);
                        if (acLengthSq > 0f)
                        {
                            bPos = stream.GetLocalToRigTranslation(data.IKData.Mid);
                            cPos = stream.GetLocalToRigTranslation(data.IKData.Tip);
                            ab = bPos - aPos;
                            ac = cPos - aPos;

                            float3 acNorm = ac / math.sqrt(acLengthSq);
                            float3 ah = stream.GetLocalToRigTranslation(data.IKData.Hint) - aPos;
                            float3 abProj = ab - acNorm * math.dot(ab, acNorm);
                            float3 ahProj = ah - acNorm * math.dot(ah, acNorm);

                            float maxReach = data.IKData.LimbLengths.x + data.IKData.LimbLengths.y;
                            if (math.lengthsq(abProj) > (maxReach * maxReach * 0.001f) && math.lengthsq(ahProj) > 0f)
                            {
                                quaternion hintRot = mathex.fromTo(abProj, ahProj);
                                hintRot.value.xyz *= hintWeight;
                                stream.SetLocalToRigRotation(data.IKData.Root, math.mul(hintRot, stream.GetLocalToRigRotation(data.IKData.Root)));
                            }
                        }
                    }

                    stream.SetLocalToRigRotation(data.IKData.Tip, tRot);
                }

                data.ProfilerMarker.End();
            }

            static float TriangleAngle(float aLen, float2 limbLengths)
            {
                float c = math.clamp((math.dot(limbLengths, limbLengths) - aLen * aLen) / (limbLengths.x * limbLengths.y) * 0.5f, -1.0f, 1.0f);
                return math.acos(c);
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfileMarker;
            kData.IKData = TwoBoneIKData.Null;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
        {
            GetKernelData(ctx.Handle).RigDefinition = rigBindings;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigBindings.Value.Bindings.CurveCount));
        }

        public void HandleMessage(in MessageContext ctx, in TwoBoneIKData data)
        {
            GetKernelData(ctx.Handle).IKData = data;
        }
    }
}
