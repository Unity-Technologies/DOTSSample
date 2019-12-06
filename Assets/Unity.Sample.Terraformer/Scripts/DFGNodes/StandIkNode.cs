using System;
using UnityEngine;

namespace Unity.Animation
{
    using Burst;
    using Mathematics;
    using DataFlowGraph;
    using Profiling;
    using Entities;

    public class StandIkNode
        : NodeDefinition<StandIkNode.Data, StandIkNode.SimPorts, StandIkNode.KernelData, StandIkNode.KernelDefs, StandIkNode.Kernel>
        , IMsgHandler<BlobAssetReference<RigDefinition>>
        , IMsgHandler<StandIkNode.StandIkData>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<StandIkNode, BlobAssetReference<RigDefinition>> RigDefinition;
            public MessageInput<StandIkNode, StandIkData> StandIkSetup;
        }

        const float k_Epsilon = 0.000001F;
        const float k_Rad2Deg = 360 / (math.PI * 2);
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.StandIkNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<StandIkNode, Buffer<float>> Input;
            public DataOutput<StandIkNode, Buffer<float>> Output;
            public DataInput<StandIkNode, float> Weight;
        }

        public struct Data : INodeData
        {
        }

        public struct StandIkData
        {
            public Settings Settings;
            public int LeftToeIdx;
            public int RightToeIdx;
            public int LeftFootIkIdx;
            public int RightFootIkIdx;
            public int HipsIdx;
            public float2 ikOffset;
            public float3 normalLeftFoot;
            public float3 normalRightFoot;
            public float Weight;
        }

        [Serializable]
        public struct Settings
        {
            [Tooltip("Place over toe bone when in it's stand idle pose. " +
                "Note that the Foot IK should not be enabled while adjusting!")]
            public float3 leftToeStandPos;

            [Tooltip("Place over toe bone when in it's stand idle pose. " +
                "Note that the Foot IK should not be enabled while adjusting!")]
            public float3 rightToeStandPos;
            [Range(0, 1)]
            public int debugIdlePos;

            [Space(10)]
            [Range(0, 1)]
            public int enabled;

            [Space(10)]
            [Range(0f, 1f)]
            public float emitRayOffset;
            [Range(0f, 20f)]
            public float maxRayDistance;
            [Range(0, 1)]
            public int debugRayCast;

            [Space(10)]
            [Range(0f, 1f)]
            public float maxStepSize;
            [Range(-90f, 90f)]
            public float weightShiftAngle;
            [Range(-1f, 1f)]
            public float weightShiftHorizontal;
            [Range(-1f, 1f)]
            public float weightShiftVertical;
            [Range(5f, 50f)]
            public float maxFootRotationOffset;

            [Space(10)]
            [Range(0f, 1f)]
            public float enterStateEaseIn;
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfilerMarker;
            public StandIkData Data;
            public float3 VectorForward;
            public float3 VectorOne;
            public quaternion OffsetRotation;
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

                    // TODO: (sunek) Get a real number! Requires replicating and incrementing the IK weight in update
                    // ikWeight = Mathf.Clamp01(ikWeight + (1 - settings.enterStateEaseIn));

                    var ikWeight = data.Data.Weight;
                    var settings = data.Data.Settings;

                    var leftToePos = stream.GetLocalToRigTranslation(data.Data.LeftToeIdx);
                    var rightToePos = stream.GetLocalToRigTranslation(data.Data.RightToeIdx);

                    var leftIkOffset = data.Data.ikOffset.x * ikWeight;
                    var rightIkOffset = data.Data.ikOffset.y * ikWeight;

                    leftToePos += new float3(0f, leftIkOffset, 0f);
                    rightToePos += new float3(0f, rightIkOffset, 0f);

                    var leftAnklePos = stream.GetLocalToRigTranslation(data.Data.LeftFootIkIdx);
                    var rightAnklePos = stream.GetLocalToRigTranslation(data.Data.RightFootIkIdx);
                    var leftAnkleRot = stream.GetLocalToRigRotation(data.Data.LeftFootIkIdx);
                    var rightAnkleRot = stream.GetLocalToRigRotation(data.Data.RightFootIkIdx);

                    var leftAnkleIkPos = new float3(leftAnklePos.x, leftAnklePos.y + leftIkOffset, leftAnklePos.z);
                    var rightAnkleIkPos = new float3(rightAnklePos.x, rightAnklePos.y + rightIkOffset, rightAnklePos.z);

                    var hipHeightOffset = (leftIkOffset + rightIkOffset) * 0.5f;
                    var forwardBackBias = (leftIkOffset - rightIkOffset) * settings.weightShiftHorizontal;

                    // TODO: (sunek) Rework weight shift to move towards actual lower foot?
                    hipHeightOffset += Mathf.Abs(leftIkOffset - rightIkOffset) * settings.weightShiftVertical;
                    var standAngle = math.mul(quaternion.AxisAngle(Vector3.up, math.radians(settings.weightShiftAngle)), data.VectorForward);

                    var hipsPosition = stream.GetLocalToRigTranslation(data.Data.HipsIdx);
                    hipsPosition += new float3(standAngle.x * forwardBackBias, hipHeightOffset, standAngle.z * forwardBackBias);
                    stream.SetLocalToRigTranslation(data.Data.HipsIdx, hipsPosition);

                    // Figure out the normal rotation
                    var leftNormalRot = quaternion.identity;
                    var rightNormalRot = quaternion.identity;

                    if (!data.Data.normalLeftFoot.Equals(float3.zero))
                    {
                        leftNormalRot = quaternion.LookRotationSafe(data.Data.normalLeftFoot, data.VectorForward);
                        rightNormalRot = quaternion.LookRotationSafe(data.Data.normalRightFoot, data.VectorForward);
                        leftNormalRot = math.mul(leftNormalRot, data.OffsetRotation);
                        rightNormalRot = math.mul(rightNormalRot, data.OffsetRotation);
                    }

                    // Clamp normal rotation
                    var leftAngle = Angle(quaternion.identity, leftNormalRot);
                    var rightAngle = Angle(quaternion.identity, rightNormalRot);

                    if (leftAngle > settings.maxFootRotationOffset && settings.maxFootRotationOffset > 0f)
                    {
                        var fraction = settings.maxFootRotationOffset / leftAngle;
                        leftNormalRot = math.nlerp(Quaternion.identity, leftNormalRot, fraction);
                    }

                    if (rightAngle > settings.maxFootRotationOffset && settings.maxFootRotationOffset > 0f)
                    {
                        var fraction = settings.maxFootRotationOffset / rightAngle;
                        rightNormalRot = math.nlerp(Quaternion.identity, rightNormalRot, fraction);
                    }

                    // Apply rotation to ankle
                    var leftToesMatrix = Matrix4x4.TRS(leftToePos, quaternion.identity, data.VectorOne);
                    var rightToesMatrix = Matrix4x4.TRS(rightToePos, quaternion.identity, data.VectorOne);

                    leftNormalRot = math.normalize(leftNormalRot);
                    rightNormalRot = math.normalize(rightNormalRot);

                    leftAnkleRot = math.normalize(leftAnkleRot);
                    rightAnkleRot = math.normalize(rightAnkleRot);

                    var leftToesNormalDeltaMatrix = Matrix4x4.TRS(leftToePos, leftNormalRot, data.VectorOne) * leftToesMatrix.inverse;
                    var rightToesNormalDeltaMatrix = Matrix4x4.TRS(rightToePos, rightNormalRot, data.VectorOne) * rightToesMatrix.inverse;

                    var leftAnkleMatrix = Matrix4x4.TRS(leftAnkleIkPos, leftAnkleRot, data.VectorOne) * leftToesMatrix.inverse;
                    var rightAnkleMatrix = Matrix4x4.TRS(rightAnkleIkPos, rightAnkleRot, data.VectorOne) * rightToesMatrix.inverse;

                    leftAnkleMatrix = leftToesNormalDeltaMatrix * leftAnkleMatrix * leftToesMatrix;
                    rightAnkleMatrix = rightToesNormalDeltaMatrix * rightAnkleMatrix * rightToesMatrix;

                    // Todo:Find a better way to do this?
                    var leftColumn = leftAnkleMatrix.GetColumn(3);
                    leftAnkleIkPos = new float3(leftColumn[0], leftColumn[1], leftColumn[2]); // TODO: (sunek) Is there a slicing syntax instead?
                    var rightColumn = rightAnkleMatrix.GetColumn(3);
                    rightAnkleIkPos = new float3(rightColumn[0], rightColumn[1], rightColumn[2]);

                    leftAnkleRot = math.nlerp(leftAnkleRot, leftAnkleMatrix.rotation, ikWeight);
                    rightAnkleRot = math.nlerp(rightAnkleRot, rightAnkleMatrix.rotation, ikWeight);

                    // Update ik position
                    // TODO: (sunek) Consider combating leg overstretch
                    var leftPosition = math.lerp(leftAnklePos, leftAnkleIkPos, ikWeight);
                    var rightPosition = math.lerp(rightAnklePos, rightAnkleIkPos, ikWeight);

                    stream.SetLocalToRigTranslation(data.Data.LeftFootIkIdx, leftPosition);
                    stream.SetLocalToRigTranslation(data.Data.RightFootIkIdx, rightPosition);

                    stream.SetLocalToRigRotation(data.Data.LeftFootIkIdx, leftAnkleRot);
                    stream.SetLocalToRigRotation(data.Data.RightFootIkIdx, rightAnkleRot);
                }

                data.ProfilerMarker.End();
            }

            private static float Angle(quaternion a, quaternion b)
            {
                // TODO: Return in radians (and convert the user param from deg to rad)?
                float dot = math.dot(a, b);
                return IsEqualUsingDot(dot) ? 0.0f : math.acos(math.min(math.abs(dot), 1.0F)) * 2.0F * k_Rad2Deg;
            }

            private static bool IsEqualUsingDot(float dot)
            {
                // Returns false in the presence of NaN values.
                return dot > 1.0f - k_Epsilon;
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfileMarker;
            kData.VectorForward = new float3(0f, 0f, 1f);
            kData.VectorOne = new float3(1f, 1f, 1f);
            // kData.OffsetRotation = quaternion.Euler(math.radians(-90f), 0f, math.radians(180f));
            kData.OffsetRotation = new quaternion(0f, 0.7f, 0.7f, 0f);
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
        {
            GetKernelData(ctx.Handle).RigDefinition = rigBindings;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigBindings.Value.Bindings.CurveCount));
        }

        public void HandleMessage(in MessageContext ctx, in StandIkData data)
        {
            GetKernelData(ctx.Handle).Data = data;
        }
    }
}
