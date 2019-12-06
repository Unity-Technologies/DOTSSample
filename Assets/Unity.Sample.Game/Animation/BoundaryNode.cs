using System;
using Unity.DataFlowGraph;
using Unity.Animation;
using Unity.Burst;
using Unity.Entities;

public class BoundaryNode
    : NodeDefinition<BoundaryNode.Data, BoundaryNode.Simports, BoundaryNode.KernelData, BoundaryNode.KernelDefs,
        BoundaryNode.Kernel>,  IMsgHandler<BlobAssetReference<RigDefinition>>
{

    public struct Simports : ISimulationPortDefinition
    {
        public MessageInput<BoundaryNode, BlobAssetReference<RigDefinition>> RigDefinition;
    }

    [Managed]
    public struct Data : INodeData
    {
    }

    public struct KernelData : IKernelData
    {
    }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<BoundaryNode, Buffer<float>> Input;
        public DataOutput<BoundaryNode, Buffer<float>> Output;
    }

    [BurstCompile]
    public struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
        {
            var inputArray = ctx.Resolve(ports.Input);
            var outputArray = ctx.Resolve(ref ports.Output);

			if (inputArray.Length == 0 || outputArray.Length == 0)
				return;
            if (inputArray.Length != outputArray.Length)
                return;
//              throw new InvalidOperationException("BoundryNode needs same amount of inputs as outputs. Inputs:" + inputArray.Length + " Outputs:" + outputArray.Length);

            for (int i = 0; i < inputArray.Length; i++)
                outputArray[i] = inputArray[i];
        }
    }


    public override void Init(InitContext ctx)
    {
    }

    public override void Destroy(NodeHandle handle)
    {
    }

    public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
    {
        Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigBindings.Value.Bindings.CurveCount));
    }
}
