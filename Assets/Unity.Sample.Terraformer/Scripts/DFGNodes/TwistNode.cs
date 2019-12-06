using Unity.Animation;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;


public class TwistNode 
    : NodeDefinition<TwistNode.Data, TwistNode.SimPorts, TwistNode.KernelData, TwistNode.KernelDefs, TwistNode.Kernel>
    , IMsgHandler<BlobAssetReference<RigDefinition>>
    , IMsgHandler<AnimSourceTwist.Settings>
{
    public struct SimPorts : ISimulationPortDefinition
    {
        public MessageInput<TwistNode, BlobAssetReference<RigDefinition>> RigDefinition;
        public MessageInput<TwistNode, AnimSourceTwist.Settings> Settings;
    }
    
    static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.TwistNode");

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<TwistNode, Buffer<float>> Input;
        public DataOutput<TwistNode, Buffer<float>> Output;
    }

    public struct Data : INodeData
    {
        
    }

    public struct KernelData : IKernelData
    {
        public BlobAssetReference<RigDefinition> RigDefinition;
        public AnimSourceTwist.Settings Settings;
        public ProfilerMarker ProfileMarker;
    }
    
    [BurstCompile]
    public struct Kernel : IGraphKernel<KernelData, KernelDefs> {
        public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
        {
            data.ProfileMarker.Begin();
            
            var output = context.Resolve(ref ports.Output);
            output.CopyFrom(context.Resolve(in ports.Input));
            
            var stream = AnimationStreamProvider.Create(data.RigDefinition, output);
            if (stream.IsNull)
            {
                data.ProfileMarker.End();
                return;
            }
            
            var driverIndex = data.Settings.boneReferences.DriverIndex;
            var twistIndexA = data.Settings.boneReferences.TwistJointA;
            var twistIndexB = data.Settings.boneReferences.TwistJointB;
            var twistIndexC = data.Settings.boneReferences.TwistJointC;
            
            if (driverIndex != -1)
            {
                var driverRot = stream.GetLocalToParentRotation(driverIndex);
                var driverBindRot = data.RigDefinition.Value.DefaultValues.LocalRotations[driverIndex];

                var driverDelta = math.mul(math.inverse(driverBindRot), driverRot);
                var twist = new quaternion(0.0f, driverDelta.value.y * data.Settings.twistMult, 0.0f, driverDelta.value.w);
                
                if (twistIndexA != -1)
                {
                    var twistRotation = mathex.lerp(quaternion.identity, twist, data.Settings.factors.FactorA);
                    stream.SetLocalToParentRotation(data.Settings.boneReferences.TwistJointA, twistRotation);
                }

                if (twistIndexB != -1)
                {   
                    var twistRotation = mathex.lerp(quaternion.identity, twist, data.Settings.factors.FactorB);
                    stream.SetLocalToParentRotation(data.Settings.boneReferences.TwistJointB, twistRotation);
                }

                if (twistIndexC != -1)
                {
                    var twistRotation = mathex.lerp(quaternion.identity, twist, data.Settings.factors.FactorC);
                    stream.SetLocalToParentRotation(data.Settings.boneReferences.TwistJointC, twistRotation);
                }
            }
            
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
    
    public void HandleMessage(in MessageContext ctx, in AnimSourceTwist.Settings settings)
    {
        // TODO: (sunek) Use structs native to TwistNode!
        GetKernelData(ctx.Handle).Settings = settings;
    }
}
