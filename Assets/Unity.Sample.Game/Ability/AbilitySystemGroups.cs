using Unity.Entities;
using UnityEngine.Profiling;


[DisableAutoCreation]
public class AbilityUpdateSystemGroup : ManualComponentSystemGroup
{
    protected override void OnUpdate()
    {
        Profiler.BeginSample("BehaviourUpdate");
        base.OnUpdate();
        Profiler.EndSample();
    }
}


[UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
[DisableAutoCreation]
public class BehaviourRequestPhase : ManualComponentSystemGroup
{
    protected override void OnUpdate()
    {
        Profiler.BeginSample("RequestPhase");
        base.OnUpdate();
        Profiler.EndSample();
    }
}

[UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
[UpdateAfter(typeof(BehaviourRequestPhase))]
[DisableAutoCreation]
public class MovementUpdatePhase : ManualComponentSystemGroup
{
    protected override void OnUpdate()
    {
        Profiler.BeginSample("MovementUpdatePhase");
        base.OnUpdate();
        Profiler.EndSample();
    }
}

[UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
[UpdateAfter(typeof(MovementUpdatePhase))]
[DisableAutoCreation]
public class MovementResolvePhase : ManualComponentSystemGroup
{
    protected override void OnUpdate()
    {
        Profiler.BeginSample("MovementResolvePhase");
        base.OnUpdate();
        Profiler.EndSample();
    }
}


[UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
[UpdateAfter(typeof(MovementResolvePhase))]
[DisableAutoCreation]
public class AbilityPreparePhase : ManualComponentSystemGroup
{
    protected override void OnUpdate()
    {
        Profiler.BeginSample("AbilityPreparePhase");
        base.OnUpdate();
        Profiler.EndSample();
    }
}


[UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
[UpdateAfter(typeof(AbilityPreparePhase))]
[DisableAutoCreation]
public class AbilityUpdatePhase : ManualComponentSystemGroup
{
    protected override void OnUpdate()
    {
        Profiler.BeginSample("AbilityUpdatePhase");
        base.OnUpdate();
        Profiler.EndSample();
    }
}

[UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
[UpdateAfter(typeof(AbilityUpdatePhase))]
[DisableAutoCreation]
[UnityEngine.ExecuteAlways]
public class AbilityUpdateCommandBufferSystem : EntityCommandBufferSystem { }
