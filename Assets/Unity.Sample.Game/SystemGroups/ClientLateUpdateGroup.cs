using Unity.Entities;
using UnityEngine.Profiling;

[DisableAutoCreation]
public class ClientLateUpdateGroup : ManualComponentSystemGroup
{
    protected override void OnUpdate()
    {
        Profiler.BeginSample("ClientLateUpdateGroup");
        base.OnUpdate();
        Profiler.EndSample();
    }
}