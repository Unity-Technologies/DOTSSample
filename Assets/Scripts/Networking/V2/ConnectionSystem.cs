using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

public struct InitializedConnection : IComponentData
{ }

[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
public class ConnectionSystem : JobComponentSystem
{
    private BeginSimulationEntityCommandBufferSystem m_CommandBuffer;
    private JobHandle m_JobHandle;
    private NativeQueue<int> m_Connections;

    protected override void OnCreate()
    {
        m_CommandBuffer = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_Connections = new NativeQueue<int>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        m_Connections.Dispose();
    }

    [ExcludeComponent(typeof(InitializedConnection))]
    struct NewConnectionJob : IJobForEachWithEntity<NetworkIdComponent>
    {
        public EntityCommandBuffer CommandBuffer;
        public NativeQueue<int> ConnectionList;

        public void Execute(Entity entity, int index, ref NetworkIdComponent netId)
        {
            CommandBuffer.AddComponent(entity, new InitializedConnection());
            //UnityEngine.Debug.Log(">>>> New connection, ID=" + state.Value.InternalId);

            ConnectionList.Enqueue(netId.Value);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        m_JobHandle.Complete();
        var job = new NewConnectionJob();
        job.CommandBuffer = m_CommandBuffer.CreateCommandBuffer();
        job.ConnectionList = m_Connections;
        m_JobHandle = job.ScheduleSingle(this, inputDeps);
        m_CommandBuffer.AddJobHandleForProducer(m_JobHandle);

        return m_JobHandle;
    }
}
