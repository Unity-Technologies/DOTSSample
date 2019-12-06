using System;

using Unity.Animation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Profiling;

public class BankingNode
    : NodeDefinition<BankingNode.Data, BankingNode.SimPorts, BankingNode.KernelData, BankingNode.KernelDefs, BankingNode.Kernel>
    , IMsgHandler<BlobAssetReference<RigDefinition>>
    , IMsgHandler<AnimSourceBanking.Settings>
{

    public struct SimPorts : ISimulationPortDefinition
    {
        public MessageInput<BankingNode, BlobAssetReference<RigDefinition>> RigDefinition;
        public MessageInput<BankingNode, AnimSourceBanking.Settings> BankingSetup;
    }

    static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.BankingNode");
    const int k_numHeadHandles = 2;
    const int k_numSpineHandles = 3;

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<BankingNode, Buffer<float>> Input;
        public DataOutput<BankingNode, Buffer<float>> Output;

        public DataInput<BankingNode, float> BankAmount;
    }

    public struct Data : INodeData
    {

    }

    public struct KernelData : IKernelData
    {
        public BlobAssetReference<RigDefinition> RigDefinition;
        public ProfilerMarker ProfileMarker;

        public AnimSourceBanking.Settings BankingData;
    }

    [BurstCompile]
    public struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
        {
            data.ProfileMarker.Begin();

            var output = context.Resolve(ref ports.Output);
            output.CopyFrom(context.Resolve(in ports.Input));

            var bankAmount = context.Resolve(ports.BankAmount);
            if (math.abs(bankAmount) < 0.001f)
            {
                data.ProfileMarker.End();
                return;
            }

            var stream = AnimationStreamProvider.Create(data.RigDefinition, output);
            if (stream.IsNull)
            {
                data.ProfileMarker.End();
                return;
            }

            var bankPosition = data.BankingData.Position * bankAmount * 0.01f;
            var weightedBankRotation = quaternion.Euler(math.radians(data.BankingData.EulerRotation * bankAmount * (1 - data.BankingData.SpineMultiplier)));
            var bankRotation = quaternion.Euler(math.radians(data.BankingData.EulerRotation * bankAmount));
            var footPosition = data.BankingData.Position * bankAmount * 0.01f * data.BankingData.FootMultiplier;

            //TODO: A multiplier here??
            // Rig axis are reverted for left and right feet.
            var leftFootRotation = quaternion.Euler(math.radians(-1.0f * data.BankingData.EulerRotation * bankAmount * data.BankingData.FootMultiplier));
            var rightFootRotation = quaternion.Euler(math.radians(data.BankingData.EulerRotation * bankAmount * data.BankingData.FootMultiplier));

            // No centre of mass so for now we use the hips
            var hipPos = stream.GetLocalToRigTranslation(data.BankingData.boneReferences.HipsIndex);
            var hipRot = stream.GetLocalToRigRotation(data.BankingData.boneReferences.HipsIndex);
            stream.SetLocalToRigRotation(data.BankingData.boneReferences.HipsIndex, math.mul(weightedBankRotation, hipRot));
            stream.SetLocalToRigTranslation(data.BankingData.boneReferences.HipsIndex, math.mul(bankRotation, hipPos) + bankPosition);

            // Head banking
            var multiplier = bankAmount * 0.075f * data.BankingData.HeadMultiplier / k_numHeadHandles;

            // Head and neck have the same range of movement [-40, 40] degrees.
            var axis = quaternion.AxisAngle(new float3(0f, -1f, -1f), math.radians(40f));
            var weightedRot = mathex.quatWeight(axis, multiplier);
            var neck = stream.GetLocalToRigRotation(data.BankingData.boneReferences.NeckLeftRightIndex);
            stream.SetLocalToRigRotation(data.BankingData.boneReferences.NeckLeftRightIndex, math.mul(weightedRot, neck));
            var head = stream.GetLocalToRigRotation(data.BankingData.boneReferences.HeadLeftRightIndex);
            stream.SetLocalToRigRotation(data.BankingData.boneReferences.HeadLeftRightIndex, math.mul(weightedRot, head));

            // Spine banking
            multiplier = bankAmount * 0.075f * data.BankingData.SpineMultiplier / k_numSpineHandles;

            // Spine and chest have the same range of movement [-40, 40] degrees.
            axis = quaternion.AxisAngle(new float3(0f, 0f, -1f), math.radians(40f));
            weightedRot = mathex.quatWeight(axis, multiplier);
            var spine = stream.GetLocalToRigRotation(data.BankingData.boneReferences.SpineLeftRightIndex);
            var chest = stream.GetLocalToRigRotation(data.BankingData.boneReferences.ChestLeftRightIndex);
            stream.SetLocalToRigRotation(data.BankingData.boneReferences.SpineLeftRightIndex, math.mul(weightedRot, spine));
            stream.SetLocalToRigRotation(data.BankingData.boneReferences.ChestLeftRightIndex, math.mul(weightedRot, chest));
            // Upper chest has a range of movement of [-20, 20] degrees.
            axis = quaternion.AxisAngle(new float3(0f, 0f, -1f), math.radians(20f));
            weightedRot = mathex.quatWeight(axis, multiplier);
            var upperChest = stream.GetLocalToRigRotation(data.BankingData.boneReferences.UpperChestLeftRightIndex);
            stream.SetLocalToRigRotation(data.BankingData.boneReferences.UpperChestLeftRightIndex, math.mul(weightedRot, upperChest));

            // Feet IK
            var leftFootPos = stream.GetLocalToRigTranslation(data.BankingData.boneReferences.LeftFootIKIndex);
            stream.SetLocalToRigTranslation(data.BankingData.boneReferences.LeftFootIKIndex, leftFootPos + footPosition);
            var leftFootRot = stream.GetLocalToRigRotation(data.BankingData.boneReferences.LeftFootIKIndex);
            stream.SetLocalToRigRotation(data.BankingData.boneReferences.LeftFootIKIndex, math.mul(leftFootRot, leftFootRotation));

            var rightFootPos = stream.GetLocalToRigTranslation(data.BankingData.boneReferences.RightFootIKIndex);
            stream.SetLocalToRigTranslation(data.BankingData.boneReferences.RightFootIKIndex, rightFootPos + footPosition);
            var rightFootRot = stream.GetLocalToRigRotation(data.BankingData.boneReferences.RightFootIKIndex);
            stream.SetLocalToRigRotation(data.BankingData.boneReferences.RightFootIKIndex, math.mul(rightFootRot, rightFootRotation));

            data.ProfileMarker.End();
        }
    }

    public override void Init(InitContext ctx)
    {
        ref var kData = ref GetKernelData(ctx.Handle);
        kData.ProfileMarker = k_ProfileMarker;
    }

    public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
    {
        GetKernelData(ctx.Handle).RigDefinition = rigBindings;
        Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigBindings.Value.Bindings.CurveCount));
    }

    public void HandleMessage(in MessageContext ctx, in AnimSourceBanking.Settings data)
    {
        GetKernelData(ctx.Handle).BankingData = data;
    }
}
